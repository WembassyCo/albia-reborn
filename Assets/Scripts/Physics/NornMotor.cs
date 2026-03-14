using UnityEngine;
using UnityEngine.AI;
using Albia.Core;
using System.Collections;

namespace Albia.Physics
{
    /// <summary>
    /// Controls Norn movement with smooth acceleration/deceleration,
    /// rotation damping, and precise arrival at targets.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NornCollider))]
    public class NornMotor : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float stoppingDistance = 0.5f;
        
        [Header("Acceleration")]
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private bool useSmoothAcceleration = true;
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Rotation")]
        [SerializeField] private float rotationDamping = 0.15f;
        [SerializeField] private bool faceMovementDirection = true;
        [SerializeField] private float maxRotationAngle = 120f;
        
        [Header("Target Arrival")]
        [SerializeField] private float arrivalThreshold = 0.5f;
        [SerializeField] private float slowdownDistance = 3f;
        [SerializeField] private AnimationCurve slowdownCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string movingParam = "IsMoving";
        [SerializeField] private string eatTrigger = "Eat";
        [SerializeField] private string idleTrigger = "Idle";
        
        [Header("Advanced")]
        [SerializeField] private bool autoRepath = true;
        [SerializeField] private float repathInterval = 0.5f;
        [SerializeField] private bool debugDrawPath = false;
        
        private NavMeshAgent agent;
        private NornCollider nornCollider;
        private Organism organism;
        
        private float currentSpeed;
        private float targetSpeed;
        private Vector3 currentVelocity;
        private Quaternion targetRotation;
        
        private Transform currentTarget;
        private Vector3 currentDestination;
        private bool hasDestination;
        private bool isMovingToTarget;
        
        private float lastSpeed;
        private bool wasMoving;
        private float repathTimer;
        
        public System.Action OnDestinationReached;
        public System.Action OnStartedMoving;
        public System.Action OnStoppedMoving;
        public System.Action OnTargetReached;
        
        public bool IsMoving => isMovingToTarget;
        public bool HasDestination => hasDestination;
        public float CurrentSpeed => currentSpeed;
        public Transform CurrentTarget => currentTarget;
        public Vector3 CurrentDestination => currentDestination;

        void Awake()
        {
            InitializeComponents();
            ConfigureAgent();
        }

        void Update()
        {
            UpdateMovement();
            UpdateRotation();
            UpdateAnimation();
            CheckArrival();
            UpdateRepathing();
            SyncAgentWithPhysics();
        }

        void OnDisable()
        {
            if (agent != null && agent.isActiveAndEnabled)
                agent.isStopped = true;
        }

        void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();
            nornCollider = GetComponent<NornCollider>();
            organism = GetComponent<Organism>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }
        
        void ConfigureAgent()
        {
            if (agent == null) return;
            
            agent.speed = baseSpeed;
            agent.acceleration = acceleration;
            agent.angularSpeed = rotationSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.autoBraking = true;
            agent.autoRepath = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.baseOffset = 0;
            agent.avoidancePriority = 50;
            agent.updateRotation = false;
        }

        public void MoveTo(Vector3 destination, float? customSpeed = null)
        {
            if (!IsValidDestination(destination))
            {
                if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    destination = hit.position;
                else
                    return;
            }
            
            currentDestination = destination;
            currentTarget = null;
            hasDestination = true;
            isMovingToTarget = true;
            targetSpeed = customSpeed ?? baseSpeed;
            
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                agent.isStopped = false;
                agent.stoppingDistance = 0f;
            }
            
            if (!wasMoving)
                OnStartedMoving?.Invoke();
        }
        
        public void MoveToTarget(Transform target, float? customSpeed = null)
        {
            if (target == null) return;
            
            currentTarget = target;
            hasDestination = true;
            isMovingToTarget = true;
            targetSpeed = customSpeed ?? baseSpeed;
            
            MoveTo(target.position, customSpeed);
        }
        
        public void Stop(bool immediate = false)
        {
            if (immediate)
            {
                currentSpeed = 0f;
                currentVelocity = Vector3.zero;
                targetSpeed = 0f;
            }
            else
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
                TriggerAnimation(idleTrigger);
            }
        }
        
        public void MoveRelative(Vector2 input, float? customSpeed = null)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                targetSpeed = 0f;
                return;
            }
            
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
            Vector3 destination = transform.position + moveDirection * 2f;
            
            MoveTo(destination, customSpeed);
        }
        
        public void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            targetRotation = Quaternion.LookRotation(direction.normalized);
        }
        
        public void FacePosition(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0;
            FaceDirection(direction);
        }
        
        public void Rotate(float angle)
        {
            targetRotation = transform.rotation * Quaternion.Euler(0, angle, 0);
        }

        void UpdateMovement()
        {
            if (!isMovingToTarget) 
            {
                Decelerate();
                return;
            }
            
            if (agent.isActiveAndEnabled && agent.hasPath)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentDestination);
                
                if (distanceToTarget < slowdownDistance && distanceToTarget > arrivalThreshold)
                {
                    float t = 1f - (distanceToTarget / slowdownDistance);
                    float slowdownFactor = slowdownCurve.Evaluate(t);
                    targetSpeed = baseSpeed * slowdownFactor;
                }
                
                if (agent.remainingDistance > arrivalThreshold)
                {
                    Vector3 desiredVelocity = agent.desiredVelocity;
                    
                    if (useSmoothAcceleration)
                    {
                        float speedDiff = targetSpeed - currentSpeed;
                        float accelStep = (speedDiff > 0 ? acceleration : deceleration) * Time.deltaTime;
                        currentSpeed += Mathf.Clamp(speedDiff, -accelStep, accelStep);
                        
                        float t = Mathf.InverseLerp(0, baseSpeed, currentSpeed);
                        currentSpeed = baseSpeed * accelerationCurve.Evaluate(t);
                    }
                    else
                        currentSpeed = targetSpeed;
                    
                    if (desiredVelocity.sqrMagnitude > 0.01f)
                        currentVelocity = desiredVelocity.normalized * currentSpeed;
                    
                    agent.speed = currentSpeed;
                }
                else
                    Decelerate();
            }
            else
                Decelerate();
            
            lastSpeed = currentSpeed;
        }
        
        void Decelerate()
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            
            if (currentSpeed < 0.01f)
            {
                currentSpeed = 0f;
                currentVelocity = Vector3.zero;
            }
            
            if (agent.isActiveAndEnabled)
                agent.speed = currentSpeed;
        }
        
        void UpdateRotation()
        {
            if (!faceMovementDirection && targetRotation == Quaternion.identity) return;
            
            Quaternion targetRot;
            
            if (faceMovementDirection && currentVelocity.sqrMagnitude > 0.01f)
                targetRot = Quaternion.LookRotation(currentVelocity.normalized);
            else if (targetRotation != Quaternion.identity)
                targetRot = targetRotation;
            else
                return;
            
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationDamping * Time.deltaTime * rotationSpeed / 360f
            );
            
            float angleDiff = Quaternion.Angle(transform.rotation, targetRot);
            float maxAngle = maxRotationAngle * Time.deltaTime;
            
            if (angleDiff > maxAngle)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    maxAngle
                );
            }
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
                if (currentTarget != null)
                    OnTargetReached?.Invoke();
                Stop(true);
            }
        }
        
        void UpdateRepathing()
        {
            if (!autoRepath || !isMovingToTarget) return;
            
            if (currentTarget != null)
            {
                repathTimer += Time.deltaTime;
                if (repathTimer >= repathInterval)
                {
                    repathTimer = 0f;
                    MoveTo(currentTarget.position);
                }
            }
        }
        
        void SyncAgentWithPhysics()
        {
            if (nornCollider != null && !nornCollider.IsGrounded && agent.isActiveAndEnabled)
                agent.enabled = false;
            else if (nornCollider != null && nornCollider.IsGrounded && !agent.enabled)
            {
                agent.enabled = true;
                if (hasDestination)
                    agent.SetDestination(currentDestination);
            }
        }
        
        bool IsValidDestination(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas);
        }
        
        void TriggerAnimation(string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName)) return;
            animator.SetTrigger(triggerName);
        }

        public void TriggerEatAnimation()
        {
            TriggerAnimation(eatTrigger);
        }
        
        public bool CanReach(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
                return path.status == NavMeshPathStatus.PathComplete;
            return false;
        }
        
        public float GetRemainingDistance()
        {
            if (!hasDestination) return 0f;
            
            return agent.isActiveAndEnabled && agent.hasPath 
                ? agent.remainingDistance 
                : Vector3.Distance(transform.position, currentDestination);
        }
        
        public IEnumerator SpeedBoost(float multiplier, float duration)
        {
            float originalSpeed = baseSpeed;
            baseSpeed *= multiplier;
            yield return new WaitForSeconds(duration);
            baseSpeed = originalSpeed;
        }
        
        public void SetStoppingThresholds(float arrival, float slowdown)
        {
            arrivalThreshold = arrival;
            slowdownDistance = slowdown;
        }

        void OnDrawGizmos()
        {
            if (!debugDrawPath) return;
            
            if (hasDestination)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentDestination, 0.3f);
                Gizmos.DrawLine(transform.position, currentDestination);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currentDestination, arrivalThreshold);
            }
            
            if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.cyan;
                Vector3[] corners = agent.path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);
        }
    }
}
