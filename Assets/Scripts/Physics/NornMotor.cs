using UnityEngine;
using UnityEngine.AI;
using Albia.Core;

namespace Albia.Physics
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NornCollider))]
    public class NornMotor : MonoBehaviour
    {
        [Header("Movement")]
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
        
        [Header("Arrival")]
        public float arrivalThreshold = 0.5f;
        public float slowdownDistance = 3f;
        public AnimationCurve slowdownCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Animation")]
        public Animator animator;
        public string speedParam = "Speed";
        public string movingParam = "IsMoving";
        public string eatTrigger = "Eat";
        public string idleTrigger = "Idle";
        
        private NavMeshAgent agent;
        private NornCollider nornCollider;
        private float currentSpeed;
        private Vector3 currentDestination;
        private Transform currentTarget;
        private bool isMovingToTarget;
        private bool wasMoving;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            nornCollider = GetComponent<NornCollider>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
                
            if (agent != null)
            {
                agent.speed = baseSpeed;
                agent.acceleration = acceleration;
                agent.angularSpeed = rotationSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.autoBraking = true;
                agent.updateRotation = false;
            }
        }

        void Update()
        {
            UpdateMovement();
            UpdateRotation();
            UpdateAnimation();
            CheckArrival();
            
            if (nornCollider != null && !nornCollider.IsGrounded)
                agent.enabled = false;
            else if (nornCollider != null && nornCollider.IsGrounded && !agent.enabled)
            {
                agent.enabled = true;
                if (isMovingToTarget)
                    agent.SetDestination(currentDestination);
            }
        }

        public void MoveTo(Vector3 destination, float? customSpeed = null)
        {
            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                return;
                
            currentDestination = hit.position;
            currentTarget = null;
            isMovingToTarget = true;
            currentSpeed = customSpeed ?? baseSpeed;
            
            if (agent.isActiveAndEnabled)
            {
                agent.SetDestination(currentDestination);
                agent.isStopped = false;
            }
        }
        
        public void MoveToTarget(Transform target, float? customSpeed = null)
        {
            if (target == null) return;
            currentTarget = target;
            MoveTo(target.position, customSpeed);
        }
        
        public void Stop()
        {
            isMovingToTarget = false;
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            
            if (wasMoving && animator != null)
                animator.SetTrigger(idleTrigger);
                
            wasMoving = false;
        }

        void UpdateMovement()
        {
            if (!isMovingToTarget) 
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
                return;
            }
            
            if (agent.isActiveAndEnabled && agent.hasPath)
            {
                float distToTarget = Vector3.Distance(transform.position, currentDestination);
                
                if (distToTarget < slowdownDistance && distToTarget > arrivalThreshold)
                {
                    float t = 1f - (distToTarget / slowdownDistance);
                    agent.speed = baseSpeed * slowdownCurve.Evaluate(t);
                }
                else if (useSmoothAcceleration)
                {
                    float speedDiff = currentSpeed - agent.speed;
                    agent.speed = Mathf.MoveTowards(agent.speed, currentSpeed, 
                        (speedDiff > 0 ? acceleration : deceleration) * Time.deltaTime);
                }
                else
                {
                    agent.speed = currentSpeed;
                }
            }
        }
        
        void UpdateRotation()
        {
            if (!faceMovementDirection) return;
            
            Vector3 desired = agent.desiredVelocity;
            if (desired.sqrMagnitude < 0.01f) return;
            
            Quaternion targetRot = Quaternion.LookRotation(desired.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                rotationDamping * Time.deltaTime * rotationSpeed / 360f);
        }
        
        void UpdateAnimation()
        {
            if (animator == null) return;
            
            float normSpeed = agent.speed / baseSpeed;
            
            if (!string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, normSpeed);
            
            if (!string.IsNullOrEmpty(movingParam))
            {
                bool moving = agent.speed > 0.1f;
                if (moving != wasMoving)
                {
                    animator.SetBool(movingParam, moving);
                    wasMoving = moving;
                }
            }
        }
        
        void CheckArrival()
        {
            if (!isMovingToTarget) return;
            
            float dist = currentTarget != null 
                ? Vector3.Distance(transform.position, currentTarget.position)
                : Vector3.Distance(transform.position, currentDestination);
            
            if (dist <= arrivalThreshold)
                Stop();
        }
        
        public void TriggerEatAnimation()
        {
            if (animator != null)
                animator.SetTrigger(eatTrigger);
        }
    }
}
