using UnityEngine;
using UnityEngine.AI;
using Albia.Core;
using System.Collections;

namespace Albia.Physics
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NornCollider))]
    public class NornMotor : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float baseSpeed = 3.5f;
        public float rotationSpeed = 360f;
        public float stoppingDistance = 0.5f;
        
        [Header("Acceleration")]
        public float acceleration = 8f;
        public float deceleration = 10f;
        public bool useSmoothAcceleration = true;
        public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Rotation")]
        public float rotationDamping = 0.15f;
        public bool faceMovementDirection = true;
        public float maxRotationAngle = 120f;
        
        [Header("Target Arrival")]
        public float arrivalThreshold = 0.5f;
        public float slowdownDistance = 3f;
        public AnimationCurve slowdownCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Animation")]
        public Animator animator;
        public string speedParam = "Speed";
        public string movingParam = "IsMoving";
        public string eatTrigger = "Eat";
        public string idleTrigger = "Idle";
        
        [Header("Advanced")]
        public bool autoRepath = true;
        public float repathInterval = 0.5f;
        public bool debugDrawPath = false;
        
        private NavMeshAgent agent;
        private NornCollider nornCollider;
        private Organism organism;
        
        private float currentSpeed;
        private float targetSpeed;
        private Vector3 currentDestination;
        private Transform currentTarget;
        private bool hasDestination;
        private bool isMovingToTarget;
        private bool wasMoving;
        private float repathTimer;
        
        public System.Action OnDestinationReached;
        public System.Action OnStartedMoving;
        public System.Action OnStoppedMoving;

        public bool IsMoving => isMovingToTarget;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            nornCollider = GetComponent<NornCollider>();
            organism = GetComponent<Organism>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
                
            if (agent != null)
            {
                agent.speed = baseSpeed;
                agent.acceleration = acceleration;
                agent.angularSpeed = rotationSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.autoBraking = true;
                agent.autoRepath = false;
                agent.updateRotation = false;
            }
        }

        void Update()
        {
            UpdateMovement();
            UpdateRotation();
            UpdateAnimation();
            CheckArrival();
            UpdateRepathing();
            
            if (nornCollider != null && !nornCollider.IsGrounded && agent.isActiveAndEnabled)
                agent.enabled = false;
            else if (nornCollider != null && nornCollider.IsGrounded && !agent.enabled)
            {
                agent.enabled = true;
                if (hasDestination)
                    agent.SetDestination(currentDestination);
            }
        }

        public void MoveTo(Vector3 destination, float? customSpeed = null)
        {
            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                return;
                
            destination = hit.position;
            currentDestination = destination;
            currentTarget = null;
            hasDestination = true;
            isMovingToTarget = true;
            targetSpeed = customSpeed ?? baseSpeed;
            
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                agent.isStopped = false;
            }
            
            if (!wasMoving)
                OnStartedMoving?.Invoke();
        }
        
        public void MoveToTarget(Transform target, float? customSpeed = null)
        {
            if (target == null) return;
            currentTarget = target;
            MoveTo(target.position, customSpeed);
        }
        
        public void Stop()
        {
            targetSpeed = 0f;
            hasDestination = false;
            currentTarget = null;
            isMovingToTarget = false;
            
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            
            if (wasMoving)
            {
                OnStoppedMoving?.Invoke();
                if (animator != null)
                    animator.SetTrigger(idleTrigger);
            }
        }

        void UpdateMovement()
        {
            if (!isMovingToTarget) 
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
                if (agent.isActiveAndEnabled)
                    agent.speed = currentSpeed;
                return;
            }
            
            if (agent.isActiveAndEnabled && agent.hasPath)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentDestination);
                
                if (distanceToTarget < slowdownDistance && distanceToTarget > arrivalThreshold)
                {
                    float t = 1f - (distanceToTarget / slowdownDistance);
                    targetSpeed = baseSpeed * slowdownCurve.Evaluate(t);
                }
                
                if (useSmoothAcceleration)
                {
                    float speedDiff = targetSpeed - currentSpeed;
                    float accelStep = (speedDiff > 0 ? acceleration : deceleration) * Time.deltaTime;
                    currentSpeed += Mathf.Clamp(speedDiff, -accelStep, accelStep);
                }
                else
                {
                    currentSpeed = targetSpeed;
                }
                
                agent.speed = currentSpeed;
            }
        }
        
        void UpdateRotation()
        {
            if (!faceMovementDirection) return;
            
            Vector3 desiredVelocity = agent.desiredVelocity;
            if (desiredVelocity.sqrMagnitude < 0.01f) return;
            
            Quaternion targetRot = Quaternion.LookRotation(desiredVelocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationDamping * Time.deltaTime * rotationSpeed / 360f
            );
        }
        
        void UpdateAnimation()
        {
            if (animator == null) return;
            
            float normalizedSpeed = currentSpeed / baseSpeed;
            if (!string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, normalizedSpeed);
            
            if (!string.IsNullOrEmpty(movingParam))
            {
                bool isNowMoving = currentSpeed > 0.1f;
                if (isNowMoving != wasMoving)
                {
                    animator.SetBool(movingParam, isNowMoving);
                    wasMoving = isNowMoving;
                }
            }
        }
        
        void CheckArrival()
        {
            if (!hasDestination || !isMovingToTarget) return;
            
            float distanceToTarget = currentTarget != null 
                ? Vector3.Distance(transform.position, currentTarget.position)
                : Vector3.Distance(transform.position, currentDestination);
            
            if (distanceToTarget <= arrivalThreshold)
            {
                OnDestinationReached?.Invoke();
                Stop();
            }
        }
        
        void UpdateRepathing()
        {
            if (!autoRepath || !isMovingToTarget || currentTarget == null) return;
            
            repathTimer += Time.deltaTime;
            if (repathTimer >= repathInterval)
            {
                repathTimer = 0f;
                MoveTo(currentTarget.position);
            }
        }
        
        public void TriggerEatAnimation()
        {
            if (animator != null)
                animator.SetTrigger(eatTrigger);
        }
    }
}
