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
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private float groundedOffset = 0.05f;
        
        [Header("Food Detection")]
        [SerializeField] private float foodDetectionRadius = 1.5f;
        [SerializeField] private LayerMask foodLayer;
        
        [Header("Physics Settings")]
        [SerializeField] private bool autoSyncCenterWithAgent = true;
        [SerializeField] private bool useGravity = true;
        
        private CapsuleCollider capsuleCollider;
        private Rigidbody rb;
        private NavMeshAgent agent;
        private Organism organism;
        
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; }
        public float GroundSlopeAngle { get; private set; }
        
        public System.Action<GameObject> OnFoodDetected;
        public System.Action OnGrounded;
        public System.Action OnAirborne;
        
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

        void InitializeComponents()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            organism = GetComponent<Organism>();
            
            if (capsuleCollider == null)
                capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            
            if (foodLayer == 0)
                foodLayer = LayerMask.GetMask("Food");
        }
        
        void ConfigureCollider()
        {
            if (capsuleCollider == null) return;
            
            capsuleCollider.radius = colliderRadius;
            capsuleCollider.height = colliderHeight;
            capsuleCollider.center = colliderCenter;
            capsuleCollider.direction = 1;
            capsuleCollider.isTrigger = false;
            
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
        
        void ConfigureRigidbody()
        {
            if (rb == null) return;
            
            rb.useGravity = useGravity;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.freezeRotation = true;
            rb.mass = 5f;
            rb.drag = 2f;
            rb.angularDrag = 0.5f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        void UpdateGroundDetection()
        {
            Vector3 checkPosition = transform.position + colliderCenter;
            
            bool hitGround = Physics.SphereCast(
                checkPosition, 
                groundCheckRadius, 
                Vector3.down, 
                out groundHit,
                groundCheckDistance + colliderRadius - groundCheckRadius,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
            
            bool rayHitGround = Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out RaycastHit rayHit,
                groundCheckDistance + 0.1f,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
            
            IsGrounded = hitGround || rayHitGround;
            
            if (IsGrounded)
            {
                GroundNormal = hitGround ? groundHit.normal : rayHit.normal;
                GroundSlopeAngle = Vector3.Angle(GroundNormal, Vector3.up);
                
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
                
                if (!wasGroundedLastFrame)
                    OnGrounded?.Invoke();
            }
            else
            {
                GroundNormal = Vector3.up;
                GroundSlopeAngle = 0f;
                
                if (wasGroundedLastFrame)
                    OnAirborne?.Invoke();
            }
            
            wasGroundedLastFrame = IsGrounded;
        }
        
        void SyncWithAgent()
        {
            if (agent == null || !autoSyncCenterWithAgent) return;
            
            float agentHeight = agent.height;
            float agentBase = agent.baseOffset;
            colliderCenter = new Vector3(0, agentHeight / 2f + agentBase, 0);
            capsuleCollider.center = colliderCenter;
        }

        void UpdateFoodDetection()
        {
            Collider[] foodColliders = Physics.OverlapSphere(
                transform.position + Vector3.up * colliderCenter.y,
                foodDetectionRadius,
                foodLayer
            );
            
            foreach (var foodCollider in foodColliders)
            {
                if (foodCollider.CompareTag("Food") || foodCollider.gameObject.layer == LayerMask.NameToLayer("Food"))
                {
                    OnFoodDetected?.Invoke(foodCollider.gameObject);
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Food") || other.gameObject.layer == LayerMask.NameToLayer("Food"))
            {
                OnFoodDetected?.Invoke(other.gameObject);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Food") || other.gameObject.layer == LayerMask.NameToLayer("Food"))
            {
                OnFoodDetected?.Invoke(other.gameObject);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Terrain") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                foreach (var contact in collision.contacts)
                {
                    if (Vector3.Dot(contact.normal, Vector3.up) < -0.5f)
                    {
                        Vector3 newPos = transform.position;
                        newPos.y = Mathf.Min(newPos.y, contact.point.y - colliderHeight);
                        transform.position = newPos;
                    }
                }
            }
        }

        public void ForceGroundCheck()
        {
            UpdateGroundDetection();
        }
        
        public bool IsPositionValid(Vector3 position)
        {
            Vector3 checkPos = position + colliderCenter;
            return !Physics.CheckCapsule(
                checkPos - Vector3.up * (colliderHeight / 2f - colliderRadius),
                checkPos + Vector3.up * (colliderHeight / 2f - colliderRadius),
                colliderRadius,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }
        
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
        
        public void AddUpwardForce(float force)
        {
            if (rb != null && IsGrounded)
            {
                rb.AddForce(Vector3.up * force, ForceMode.Impulse);
                IsGrounded = false;
            }
        }

        void OnDrawGizmos()
        {
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
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderCenter.y, groundCheckRadius);
            Gizmos.DrawRay(transform.position + Vector3.up * colliderCenter.y, Vector3.down * groundCheckDistance);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderCenter.y, foodDetectionRadius);
        }
    }
}
