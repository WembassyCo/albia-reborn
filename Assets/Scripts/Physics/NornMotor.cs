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
<<<<<<< HEAD
        [SerializeField] private float maxRotationAngle = 120f; // Degrees per second
=======
        [SerializeField] private float maxRotationAngle = 120f;
>>>>>>> feature/grendels
        
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
        
<<<<<<< HEAD
        // Cached components
=======
>>>>>>> feature/grendels
        private NavMeshAgent agent;
        private NornCollider nornCollider;
        private Organism organism;
        
<<<<<<< HEAD
        // Movement state
=======
>>>>>>> feature/grendels
        private float currentSpeed;
        private float targetSpeed;
        private Vector3 currentVelocity;
        private Quaternion targetRotation;
        
<<<<<<< HEAD
        // Target state
=======
>>>>>>> feature/grendels
        private Transform currentTarget;
        private Vector3 currentDestination;
        private bool hasDestination;
        private bool isMovingToTarget;
        
<<<<<<< HEAD
        // Animation
        private float lastSpeed;
        private bool wasMoving;
        
        // Timers
        private float repathTimer;
        
        // Events
=======
        private float lastSpeed;
        private bool wasMoving;
        private float repathTimer;
        
>>>>>>> feature/grendels
        public System.Action OnDestinationReached;
        public System.Action OnStartedMoving;
        public System.Action OnStoppedMoving;
        public System.Action OnTargetReached;
        
<<<<<<< HEAD
        // Properties
=======
>>>>>>> feature/grendels
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
<<<<<<< HEAD
            
=======
>>>>>>> feature/grendels
            SyncAgentWithPhysics();
        }

        void OnDisable()
        {
<<<<<<< HEAD
            // Ensure agent stops when disabled
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
            }
        }

        #region Initialization
        
        private void InitializeComponents()
=======
            if (agent != null && agent.isActiveAndEnabled)
                agent.isStopped = true;
        }

        void InitializeComponents()
>>>>>>> feature/grendels
        {
            agent = GetComponent<NavMeshAgent>();
            nornCollider = GetComponent<NornCollider>();
            organism = GetComponent<Organism>();
            
            if (animator == null)
<<<<<<< HEAD
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        private void ConfigureAgent()
        {
            if (agent == null) return;
            
            // Core settings
=======
                animator = GetComponentInChildren<Animator>();
        }
        
        void ConfigureAgent()
        {
            if (agent == null) return;
            
>>>>>>> feature/grendels
            agent.speed = baseSpeed;
            agent.acceleration = acceleration;
            agent.angularSpeed = rotationSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.autoBraking = true;
<<<<<<< HEAD
            agent.autoRepath = false; // We handle this manually
            
            // Quality settings
=======
            agent.autoRepath = false;
>>>>>>> feature/grendels
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.baseOffset = 0;
            agent.avoidancePriority = 50;
<<<<<<< HEAD
            
            // Disable automatic rotation - we handle it for smoother control
            agent.updateRotation = false;
        }
        
        #endregion

        #region Movement Control
        
        /// <summary>
        /// Move to a specific world position
        /// </summary>
        public void MoveTo(Vector3 destination, float? customSpeed = null)
        {
            // Validate destination
            if (!IsValidDestination(destination))
            {
                // Try to find nearest valid position
                if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    destination = hit.position;
                }
                else
                {
                    return; // Can't find valid position
                }
=======
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
>>>>>>> feature/grendels
            }
            
            currentDestination = destination;
            currentTarget = null;
            hasDestination = true;
            isMovingToTarget = true;
<<<<<<< HEAD
            
            targetSpeed = customSpeed ?? baseSpeed;
            
            // Set navmesh destination
=======
            targetSpeed = customSpeed ?? baseSpeed;
            
>>>>>>> feature/grendels
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                agent.isStopped = false;
<<<<<<< HEAD
                agent.stoppingDistance = 0f; // We handle stopping ourselves
            }
            
            // Trigger event
            if (!wasMoving)
            {
                OnStartedMoving?.Invoke();
            }
        }
        
        /// <summary>
        /// Move toward a target transform (continuously updates destination)
        /// </summary>
=======
                agent.stoppingDistance = 0f;
            }
            
            if (!wasMoving)
                OnStartedMoving?.Invoke();
        }
        
>>>>>>> feature/grendels
        public void MoveToTarget(Transform target, float? customSpeed = null)
        {
            if (target == null) return;
            
            currentTarget = target;
            hasDestination = true;
            isMovingToTarget = true;
            targetSpeed = customSpeed ?? baseSpeed;
            
            MoveTo(target.position, customSpeed);
        }
        
<<<<<<< HEAD
        /// <summary>
        /// Stop movement immediately or with deceleration
        /// </summary>
=======
>>>>>>> feature/grendels
        public void Stop(bool immediate = false)
        {
            if (immediate)
            {
                currentSpeed = 0f;
                currentVelocity = Vector3.zero;
                targetSpeed = 0f;
            }
            else
<<<<<<< HEAD
            {
                targetSpeed = 0f;
            }
=======
                targetSpeed = 0f;
>>>>>>> feature/grendels
            
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
        
<<<<<<< HEAD
        /// <summary>
        /// Move in a direction relative to current facing
        /// </summary>
=======
>>>>>>> feature/grendels
        public void MoveRelative(Vector2 input, float? customSpeed = null)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                targetSpeed = 0f;
                return;
            }
            
<<<<<<< HEAD
            // Convert input to world direction
=======
>>>>>>> feature/grendels
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
<<<<<<< HEAD
            
            // Calculate destination
=======
>>>>>>> feature/grendels
            Vector3 destination = transform.position + moveDirection * 2f;
            
            MoveTo(destination, customSpeed);
        }
        
<<<<<<< HEAD
        /// <summary>
        /// Face a specific direction
        /// </summary>
        public void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            
            targetRotation = Quaternion.LookRotation(direction.normalized);
        }
        
        /// <summary>
        /// Face a specific position
        /// </summary>
=======
        public void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            targetRotation = Quaternion.LookRotation(direction.normalized);
        }
        
>>>>>>> feature/grendels
        public void FacePosition(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0;
            FaceDirection(direction);
        }
        
<<<<<<< HEAD
        /// <summary>
        /// Rotate by a specific angle
        /// </summary>
=======
>>>>>>> feature/grendels
        public void Rotate(float angle)
        {
            targetRotation = transform.rotation * Quaternion.Euler(0, angle, 0);
        }
<<<<<<< HEAD
        
        #endregion

        #region Update Loop
        
        private void UpdateMovement()
=======

        void UpdateMovement()
>>>>>>> feature/grendels
        {
            if (!isMovingToTarget) 
            {
                Decelerate();
                return;
            }
            
            if (agent.isActiveAndEnabled && agent.hasPath)
            {
<<<<<<< HEAD
                // Calculate remaining distance
                float distanceToTarget = Vector3.Distance(transform.position, currentDestination);
                
                // Apply slowdown curve as we approach target
=======
                float distanceToTarget = Vector3.Distance(transform.position, currentDestination);
                
>>>>>>> feature/grendels
                if (distanceToTarget < slowdownDistance && distanceToTarget > arrivalThreshold)
                {
                    float t = 1f - (distanceToTarget / slowdownDistance);
                    float slowdownFactor = slowdownCurve.Evaluate(t);
                    targetSpeed = baseSpeed * slowdownFactor;
                }
                
<<<<<<< HEAD
                // Calculate desired velocity
=======
>>>>>>> feature/grendels
                if (agent.remainingDistance > arrivalThreshold)
                {
                    Vector3 desiredVelocity = agent.desiredVelocity;
                    
                    if (useSmoothAcceleration)
                    {
<<<<<<< HEAD
                        // Smooth acceleration
=======
>>>>>>> feature/grendels
                        float speedDiff = targetSpeed - currentSpeed;
                        float accelStep = (speedDiff > 0 ? acceleration : deceleration) * Time.deltaTime;
                        currentSpeed += Mathf.Clamp(speedDiff, -accelStep, accelStep);
                        
<<<<<<< HEAD
                        // Apply acceleration curve
=======
>>>>>>> feature/grendels
                        float t = Mathf.InverseLerp(0, baseSpeed, currentSpeed);
                        currentSpeed = baseSpeed * accelerationCurve.Evaluate(t);
                    }
                    else
<<<<<<< HEAD
                    {
                        currentSpeed = targetSpeed;
                    }
                    
                    // Apply velocity
                    if (desiredVelocity.sqrMagnitude > 0.01f)
                    {
                        currentVelocity = desiredVelocity.normalized * currentSpeed;
                    }
                    
                    // Update agent speed
                    agent.speed = currentSpeed;
                }
                else
                {
                    Decelerate();
                }
            }
            else
            {
                Decelerate();
            }
=======
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
>>>>>>> feature/grendels
            
            lastSpeed = currentSpeed;
        }
        
<<<<<<< HEAD
        private void Decelerate()
=======
        void Decelerate()
>>>>>>> feature/grendels
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            
            if (currentSpeed < 0.01f)
            {
                currentSpeed = 0f;
                currentVelocity = Vector3.zero;
            }
            
            if (agent.isActiveAndEnabled)
<<<<<<< HEAD
            {
                agent.speed = currentSpeed;
            }
        }
        
        private void UpdateRotation()
=======
                agent.speed = currentSpeed;
        }
        
        void UpdateRotation()
>>>>>>> feature/grendels
        {
            if (!faceMovementDirection && targetRotation == Quaternion.identity) return;
            
            Quaternion targetRot;
            
            if (faceMovementDirection && currentVelocity.sqrMagnitude > 0.01f)
<<<<<<< HEAD
            {
                targetRot = Quaternion.LookRotation(currentVelocity.normalized);
            }
            else if (targetRotation != Quaternion.identity)
            {
                targetRot = targetRotation;
            }
            else
            {
                return;
            }
            
            // Apply rotation damping
=======
                targetRot = Quaternion.LookRotation(currentVelocity.normalized);
            else if (targetRotation != Quaternion.identity)
                targetRot = targetRotation;
            else
                return;
            
>>>>>>> feature/grendels
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationDamping * Time.deltaTime * rotationSpeed / 360f
            );
            
<<<<<<< HEAD
            // Clamp rotation speed
=======
>>>>>>> feature/grendels
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
        
<<<<<<< HEAD
        private void UpdateAnimation()
        {
            if (animator == null) return;
            
            // Calculate normalized speed
            float normalizedSpeed = currentSpeed / baseSpeed;
            
            // Update parameters
            if (!string.IsNullOrEmpty(speedParam))
            {
                animator.SetFloat(speedParam, normalizedSpeed);
            }
=======
        void UpdateAnimation()
        {
            if (animator == null) return;
            
            float normalizedSpeed = currentSpeed / baseSpeed;
            
            if (!string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, normalizedSpeed);
>>>>>>> feature/grendels
            
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
        
<<<<<<< HEAD
        private void CheckArrival()
        {
            if (!hasDestination || !isMovingToTarget) return;
            
            // Check if we've reached the destination
=======
        void CheckArrival()
        {
            if (!hasDestination || !isMovingToTarget) return;
            
>>>>>>> feature/grendels
            float distanceToTarget = currentTarget != null 
                ? Vector3.Distance(transform.position, currentTarget.position)
                : Vector3.Distance(transform.position, currentDestination);
            
            if (distanceToTarget <= arrivalThreshold)
            {
<<<<<<< HEAD
                // We've arrived
                OnDestinationReached?.Invoke();
                
                if (currentTarget != null)
                {
                    OnTargetReached?.Invoke();
                }
                
=======
                OnDestinationReached?.Invoke();
                if (currentTarget != null)
                    OnTargetReached?.Invoke();
>>>>>>> feature/grendels
                Stop(true);
            }
        }
        
<<<<<<< HEAD
        private void UpdateRepathing()
        {
            if (!autoRepath || !isMovingToTarget) return;
            
            // Repath to moving targets
=======
        void UpdateRepathing()
        {
            if (!autoRepath || !isMovingToTarget) return;
            
>>>>>>> feature/grendels
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
        
<<<<<<< HEAD
        private void SyncAgentWithPhysics()
        {
            // Ensure agent stays on navmesh when grounded
            if (nornCollider != null && !nornCollider.IsGrounded && agent.isActiveAndEnabled)
            {
                // Disable agent temporarily when airborne
                agent.enabled = false;
            }
=======
        void SyncAgentWithPhysics()
        {
            if (nornCollider != null && !nornCollider.IsGrounded && agent.isActiveAndEnabled)
                agent.enabled = false;
>>>>>>> feature/grendels
            else if (nornCollider != null && nornCollider.IsGrounded && !agent.enabled)
            {
                agent.enabled = true;
                if (hasDestination)
<<<<<<< HEAD
                {
                    agent.SetDestination(currentDestination);
                }
            }
        }
        
        #endregion

        #region Helper Methods
        
        private bool IsValidDestination(Vector3 position)
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(position, out hit, 1f, NavMesh.AllAreas);
        }
        
        private void TriggerAnimation(string triggerName)
=======
                    agent.SetDestination(currentDestination);
            }
        }
        
        bool IsValidDestination(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas);
        }
        
        void TriggerAnimation(string triggerName)
>>>>>>> feature/grendels
        {
            if (animator == null || string.IsNullOrEmpty(triggerName)) return;
            animator.SetTrigger(triggerName);
        }
<<<<<<< HEAD
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Trigger eat animation
        /// </summary>
=======

>>>>>>> feature/grendels
        public void TriggerEatAnimation()
        {
            TriggerAnimation(eatTrigger);
        }
        
<<<<<<< HEAD
        /// <summary>
        /// Check if we can reach the destination
        /// </summary>
=======
>>>>>>> feature/grendels
        public bool CanReach(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
<<<<<<< HEAD
            {
                return path.status == NavMeshPathStatus.PathComplete;
            }
            return false;
        }
        
        /// <summary>
        /// Get remaining distance to destination
        /// </summary>
=======
                return path.status == NavMeshPathStatus.PathComplete;
            return false;
        }
        
>>>>>>> feature/grendels
        public float GetRemainingDistance()
        {
            if (!hasDestination) return 0f;
            
            return agent.isActiveAndEnabled && agent.hasPath 
                ? agent.remainingDistance 
                : Vector3.Distance(transform.position, currentDestination);
        }
        
<<<<<<< HEAD
        /// <summary>
        /// Temporarily boost speed
        /// </summary>
=======
>>>>>>> feature/grendels
        public IEnumerator SpeedBoost(float multiplier, float duration)
        {
            float originalSpeed = baseSpeed;
            baseSpeed *= multiplier;
<<<<<<< HEAD
            
            yield return new WaitForSeconds(duration);
            
            baseSpeed = originalSpeed;
        }
        
        /// <summary>
        /// Set custom stopping thresholds
        /// </summary>
=======
            yield return new WaitForSeconds(duration);
            baseSpeed = originalSpeed;
        }
        
>>>>>>> feature/grendels
        public void SetStoppingThresholds(float arrival, float slowdown)
        {
            arrivalThreshold = arrival;
            slowdownDistance = slowdown;
        }
<<<<<<< HEAD
        
        #endregion

        #region Gizmos
        
=======

>>>>>>> feature/grendels
        void OnDrawGizmos()
        {
            if (!debugDrawPath) return;
            
<<<<<<< HEAD
            // Draw destination
=======
>>>>>>> feature/grendels
            if (hasDestination)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentDestination, 0.3f);
                Gizmos.DrawLine(transform.position, currentDestination);
<<<<<<< HEAD
                
                // Draw arrival threshold
=======
>>>>>>> feature/grendels
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currentDestination, arrivalThreshold);
            }
            
<<<<<<< HEAD
            // Draw navmesh path
=======
>>>>>>> feature/grendels
            if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.cyan;
                Vector3[] corners = agent.path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
<<<<<<< HEAD
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }
            
            // Draw facing direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);
        }
        
        #endregion
=======
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);
        }
>>>>>>> feature/grendels
    }
}
