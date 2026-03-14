using UnityEngine;
using UnityEngine.AI;
using Albia.Core;

namespace Albia.Physics
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class NornCollider : MonoBehaviour
    {
        [Header("Collider Settings")]
        public float colliderRadius = 0.4f;
        public float colliderHeight = 1.8f;
        public Vector3 colliderCenter = new Vector3(0, 0.9f, 0);
        
        [Header("Ground Detection")]
        public float groundCheckDistance = 0.3f;
        public float groundCheckRadius = 0.35f;
        public LayerMask groundLayer = ~0;
        public float groundedOffset = 0.05f;
        
        [Header("Food Detection")]
        public float foodDetectionRadius = 1.5f;
        public LayerMask foodLayer;
        
        [Header("Physics Settings")]
        public bool autoSyncCenterWithAgent = true;
        public bool useGravity = true;
        
        private CapsuleCollider capsuleCollider;
        private Rigidbody rb;
        private NavMeshAgent agent;
        
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; }
        
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
                var physMat = new PhysicMaterial("NornPhysics");
                physMat.dynamicFriction = 0.6f;
                physMat.staticFriction = 0.6f;
                physMat.frictionCombine = PhysicMaterialCombine.Average;
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
            Vector3 checkPos = transform.position + colliderCenter;
            
            bool hitGround = Physics.SphereCast(
                checkPos, 
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
                
                // Snap to prevent falling through
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
                if (foodCollider.CompareTag("Food"))
                    OnFoodDetected?.Invoke(foodCollider.gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Food"))
                OnFoodDetected?.Invoke(other.gameObject);
        }

        void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Food"))
                OnFoodDetected?.Invoke(other.gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Terrain"))
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
    }
}
