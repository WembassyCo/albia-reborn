using UnityEngine;
using Albia.Core;

namespace Albia.Physics
{
    /// <summary>
    /// Handles Norn collision detection, ground checking, and trigger interactions.
    /// Ensures reliable physics interactions without falling through world geometry.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class NornCollider : MonoBehaviour
    {
        [Header("Collider Settings")]
        [SerializeField] private float colliderRadius = 0.4f;
        [SerializeField] private float colliderHeight = 1.8f;
        [SerializeField] private Vector3 colliderCenter = new Vector3(0, 0.9f, 0);
        
        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private float groundCheckRadius = 0.35f;
        [SerializeField] private LayerMask groundLayer = ~0; // All layers by default
        [SerializeField] private float groundedOffset = 0.05f;
        
        [Header("Food Detection")]
        [SerializeField] private float foodDetectionRadius = 1.5f;
        [SerializeField] private LayerMask foodLayer;
        
        [Header("Physics Settings")]
        [SerializeField] private bool autoSyncCenterWithAgent = true;
        [SerializeField] private bool useGravity = true;
        
        // Cached components
        private CapsuleCollider capsuleCollider;
        private Rigidbody rb;
        private NavMeshAgent agent;
        private Organism organism;
        
        // Ground state
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; }
        public float GroundSlopeAngle { get; private set; }
        
        // Events
        public System.Action<GameObject> OnFoodDetected;
        public System.Action OnGrounded;
        public System.Action OnAirborne;
        
        // Raycast hit info for debugging
        private RaycastHit groundHit;
        private bool wasGroundedLastFrame;

        void Awake()
        {
            InitializeComponents();
            ConfigureCollider();
            ConfigureRigidbody();
        }

        void Update()
        {
            UpdateGroundDetection();
            UpdateFoodDetection();
            SyncWithAgent();
        }

        #region Initialization
        
        private void InitializeComponents()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            organism = GetComponent<Organism>();
            
            if (capsuleCollider == null)
            {
                capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            }
            
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            // Get Food layer if not set
            if (foodLayer == 0)
            {
                foodLayer = LayerMask.GetMask("Food");
            }
        }
        
        private void ConfigureCollider()
        {
            if (capsuleCollider == null) return;
            
            capsuleCollider.radius = colliderRadius;
            capsuleCollider.height = colliderHeight;
            capsuleCollider.center = colliderCenter;
            capsuleCollider.direction = 1; // Y-axis
            capsuleCollider.isTrigger = false;
            
            // Ensure physics material prevents sticking
            if (capsuleCollider.material == null)
            {
                var physMat = new PhysicMaterial("NornPhysicsMat");
                physMat.dynamicFriction = 0.6f;
                physMat.staticFriction = 0.6f;
                physMat.frictionCombine = PhysicMaterialCombine.Average;
                physMat.bounceCombine = PhysicMaterialCombine.Average;
                capsuleCollider.material = physMat;
            }
        }
        
        private void ConfigureRigidbody()
        {
            if (rb == null) return;
            
            // Set up rigidbody for proper physics handling
            rb.useGravity = useGravity;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.freezeRotation = true; // We handle rotation manually
            rb.mass = 5f;
            rb.drag = 2f;
            rb.angularDrag = 0.5f;
            
            // Constrain Y position to prevent flying
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        
        #endregion

        #region Ground Detection
        
        private void UpdateGroundDetection()
        {
            Vector3 checkPosition = transform.position + colliderCenter;
            
            // Sphere cast downward for more reliable ground detection
            bool hitGround = Physics.SphereCast(
                checkPosition, 
                groundCheckRadius, 
                Vector3.down, 
                out groundHit,
                groundCheckDistance + colliderRadius - groundCheckRadius,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
            
            // Alternative: Raycast from feet for precision
            bool rayHitGround = Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out RaycastHit rayHit,
                groundCheckDistance + 0.1f,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
            
            // Combine both checks - if either detects ground, we're grounded
            IsGrounded = hitGround || rayHitGround;
            
            if (IsGrounded)
            {
                // Use the closer hit
                GroundNormal = hitGround ? groundHit.normal : rayHit.normal;
                GroundSlopeAngle = Vector3.Angle(GroundNormal, Vector3.up);
                
                // Snap to ground if too far (prevents falling through)
                if (hitGround && groundHit.distance > colliderRadius + groundedOffset)
                {
                    float snapDistance = groundHit.distance - colliderRadius;
                    if (snapDistance > 0.05f && snapDistance < 0.5f)
                    {
                        transform.position = new Vector3(
                            transform.position.x,
                            groundHit.point.y + colliderRadius + groundedOffset,
                            transform.position.z
                        );
                    }
                }
                
                // Trigger event on state change
                if (!wasGroundedLastFrame)
                {
                    OnGrounded?.Invoke();
                }
            }
            else
            {
                GroundNormal = Vector3.up;
                GroundSlopeAngle = 0f;
                
                if (wasGroundedLastFrame)
                {
                    OnAirborne?.Invoke();
                }
            }
            
            wasGroundedLastFrame = IsGrounded;
        }
        
        private void SyncWithAgent()
        {
            if (agent == null || !autoSyncCenterWithAgent) return;
            
            // Sync collider center with agent height
            float agentHeight = agent.height;
            float agentBase = agent.baseOffset;
            
            // Adjust collider to match agent
            colliderCenter = new Vector3(0, agentHeight / 2f + agentBase, 0);
            capsuleCollider.center = colliderCenter;
        }
        
        #endregion

        #region Food Detection
        
        private void UpdateFoodDetection()
        {
            // Use overlap sphere for food detection around the norn
            Collider[] foodColliders = Physics.OverlapSphere(
                transform.position + Vector3.up * colliderCenter.y,
                foodDetectionRadius,
                foodLayer
            );
            
            foreach (var foodCollider in foodColliders)
            {
                // Check if this is actually food
                if (foodCollider.CompareTag("Food") || foodCollider.gameObject.layer == LayerMask.NameToLayer("Food"))
                {
                    OnFoodDetected?.Invoke(foodCollider.gameObject);
                }
            }
        }
        
        // Called by trigger detection when food enters trigger zone
        public void OnFoodTriggerEnter(Collider foodCollider)
        {
            if (foodCollider.CompareTag("Food") || foodCollider.gameObject.layer == LayerMask.NameToLayer("Food"))
            {
                OnFoodDetected?.Invoke(foodCollider.gameObject);
            }
        }
        
        #endregion

        #region Collision Handling
        
        void OnCollisionEnter(Collision collision)
        {
            // Handle terrain collision
            if (collision.gameObject.CompareTag("Terrain") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                // Check if we hit something from below (ceiling)
                foreach (var contact in collision.contacts)
                {
                    if (Vector3.Dot(contact.normal, Vector3.up) < -0.5f)
                    {
                        // Hit head on ceiling - clamp position
                        Vector3 newPos = transform.position;
                        newPos.y = Mathf.Min(newPos.y, contact.point.y - colliderHeight);
                        transform.position = newPos;
                    }
                }
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            // Catch-all food detection
            if (other.CompareTag("Food") || other.gameObject.layer == LayerMask.NameToLayer("Food"))
            {
                OnFoodDetected?.Invoke(other.gameObject);
            }
        }
        
        void OnTriggerStay(Collider other)
        {
            // Continuous food detection while inside trigger
            if (other.CompareTag("Food") || other.gameObject.layer == LayerMask.NameToLayer("Food"))
            {
                OnFoodDetected?.Invoke(other.gameObject);
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Manually force ground check (useful after teleport)
        /// </summary>
        public void ForceGroundCheck()
        {
            UpdateGroundDetection();
        }
        
        /// <summary>
        /// Check if position is valid (not inside geometry)
        /// </summary>
        public bool IsPositionValid(Vector3 position)
        {
            // Check if capsule at position would collide with anything
            Vector3 checkPos = position + colliderCenter;
            return !Physics.CheckCapsule(
                checkPos - Vector3.up * (colliderHeight / 2f - colliderRadius),
                checkPos + Vector3.up * (colliderHeight / 2f - colliderRadius),
                colliderRadius,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }
        
        /// <summary>
        /// Find closest valid position on navmesh
        /// </summary>
        public bool FindGroundPosition(out Vector3 groundPos)
        {
            groundPos = transform.position;
            
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, groundLayer))
            {
                groundPos = hit.point + Vector3.up * (colliderHeight / 2f + groundedOffset);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Apply an upward force (for jumping/bouncing)
        /// </summary>
        public void AddUpwardForce(float force)
        {
            if (rb != null && IsGrounded)
            {
                rb.AddForce(Vector3.up * force, ForceMode.Impulse);
                IsGrounded = false;
            }
        }
        
        #endregion

        #region Gizmos
        
        void OnDrawGizmos()
        {
            // Draw capsule gizmo
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            
            Vector3 basePos = transform.position + colliderCenter;
            Vector3 topSphere = basePos + Vector3.up * (colliderHeight / 2f - colliderRadius);
            Vector3 bottomSphere = basePos - Vector3.up * (colliderHeight / 2f - colliderRadius);
            
            Gizmos.DrawWireSphere(topSphere, colliderRadius);
            Gizmos.DrawWireSphere(bottomSphere, colliderRadius);
            Gizmos.DrawLine(
                basePos + Vector3.up * (colliderHeight / 2f - colliderRadius),
                basePos - Vector3.up * (colliderHeight / 2f - colliderRadius)
            );
            
            // Draw ground check
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderCenter.y, groundCheckRadius);
            Gizmos.DrawRay(transform.position + Vector3.up * colliderCenter.y, Vector3.down * groundCheckDistance);
            
            // Draw food detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderCenter.y, foodDetectionRadius);
        }
        
        #endregion
    }
}
