using UnityEngine;
using Albia.Plants;

namespace Albia.Plants
{
    /// <summary>
    /// Seed that falls, germinates after time.
    /// Chance to grow based on neighbor density.
    /// Small visual that can roll/fall with physics.
    /// </summary>
    public class Seed : MonoBehaviour
    {
        [Header("Seed Settings")]
        [SerializeField] private float germinationTime = 5f;      // Time before sprouting
        [SerializeField] private float despawnTime = 60f;         // Time before seed dies unused
        [SerializeField] private float neighborCheckRadius = 2f;  // For density calculation
        [SerializeField] private float maxNeighborDistance = 1.5f; // Too close = won't grow
        [SerializeField] private int maxNeighbors = 5;           // Max plants nearby for growth

        [Header("Physics")]
        [SerializeField] private bool usePhysics = true;
        [SerializeField] private float maxFallSpeed = 10f;
        [SerializeField] private float bounceDamping = 0.5f;

        [Header("Visuals")]
        [SerializeField] private GameObject seedMesh;
        [SerializeField] private float visualScale = 0.15f;

        [Header("Germination Effect")]
        [SerializeField] private GameObject germinationEffect;

        // Properties
        public PlantSpecies Species { get; private set; } = PlantSpecies.Carrot;
        public bool IsGerminated { get; private set; } = false;
        public float Age { get; private set; } = 0f;

        // Private state
        private float germinationTimer = 0f;
        private bool hasLanded = false;
        private bool canGrow = true;
        private PlantManager plantManager;
        private Rigidbody rb;
        private Collider seedCollider;
        private Vector3 lastPosition;

        void Awake()
        {
            plantManager = PlantManager.Instance;
            
            // Setup physics
            rb = GetComponent<Rigidbody>();
            if (rb == null && usePhysics)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.drag = 0.5f;
                rb.angularDrag = 0.5f;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            // Setup collider
            seedCollider = GetComponent<Collider>();
            if (seedCollider == null)
            {
                seedCollider = gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)seedCollider).radius = 0.1f;
                seedCollider.isTrigger = false;
            }
            
            // Visual setup
            if (seedMesh == null)
            {
                CreateVisualSeed();
            }
            
            transform.localScale = Vector3.one * visualScale;
            lastPosition = transform.position;
        }

        void Start()
        {
            // Random slight rotation
            transform.rotation = Random.rotation;
            
            // Small random initial velocity for natural fall
            if (rb != null)
            {
                rb.velocity = new Vector3(
                    Random.Range(-0.5f, 0.5f), 
                    -1f, 
                    Random.Range(-0.5f, 0.5f)
                );
            }
        }

        void Update()
        {
            if (IsGerminated) return;

            Age += Time.deltaTime;

            // Check if landed
            CheckIfLanded();

            // Update germination timer only after landing
            if (hasLanded)
            {
                germinationTimer += Time.deltaTime;
                
                // Check growth conditions
                if (germinationTimer >= germinationTime)
                {
                    TryGerminate();
                }
            }

            // Despawn if too old
            if (Age >= despawnTime)
            {
                Destroy(gameObject);
            }

            // Cap fall speed
            if (rb != null && rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
            }

            lastPosition = transform.position;
        }

        void OnCollisionEnter(Collision collision)
        {
            // Bounce damping
            if (rb != null)
            {
                rb.velocity *= bounceDamping;
            }
            
            // Check if we hit ground
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || 
                collision.gameObject.CompareTag("Terrain"))
            {
                hasLanded = true;
                
                // Stop sliding after landing
                if (rb != null)
                {
                    Invoke(nameof(FreezeMovement), 2f);
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // Check if seed is eaten by Norn
            if (other.CompareTag("Norn") || other.GetComponent<Albia.Creatures.Norn>() != null)
            {
                // Optional: seeds can be eaten for small nutrition
                // For now, just let them pass through or be consumed
            }
        }

        /// <summary>
        /// Initialize seed with species
        /// </summary>
        public void Initialize(PlantSpecies species)
        {
            Species = species;
            
            // Adjust settings based on species
            switch (species)
            {
                case PlantSpecies.Carrot:
                    germinationTime = 3f;
                    maxNeighbors = 6;
                    break;
                case PlantSpecies.Bush:
                    germinationTime = 6f;
                    maxNeighbors = 4; // Bushes need more space
                    break;
            }
        }

        /// <summary>
        /// Check if seed has landed on surface
        /// </summary>
        private void CheckIfLanded()
        {
            if (hasLanded) return;
            
            // Raycast down to check for ground
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.2f))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                    hit.collider.CompareTag("Terrain"))
                {
                    hasLanded = true;
                }
            }
            
            // Also consider landed if not moving much
            if (rb != null && rb.velocity.magnitude < 0.1f && Age > 1f)
            {
                hasLanded = true;
            }
        }

        /// <summary>
        /// Try to germinate into a plant
        /// </summary>
        private void TryGerminate()
        {
            // Check neighbor density
            if (!CheckGrowthConditions())
            {
                // Too crowded, seed will eventually die
                canGrow = false;
                return;
            }

            Germinate();
        }

        /// <summary>
        /// Check if conditions are good for growth
        /// </summary>
        private bool CheckGrowthConditions()
        {
            // Use PlantManager if available
            if (plantManager != null)
            {
                return plantManager.CanSpawnSeedInArea(transform.position, Species);
            }
            
            // Fallback: manual neighbor check
            Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborCheckRadius);
            int plantCount = 0;
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor.GetComponent<PlantOrganism>() != null)
                {
                    // Check if too close
                    float dist = Vector3.Distance(transform.position, neighbor.transform.position);
                    if (dist < maxNeighborDistance)
                    {
                        return false; // Too close to existing plant
                    }
                    plantCount++;
                }
            }
            
            return plantCount < maxNeighbors;
        }

        /// <summary>
        /// Germinate into a new plant
        /// </summary>
        private void Germinate()
        {
            IsGerminated = true;

            // Spawn germination effect
            if (germinationEffect != null)
            {
                Instantiate(germinationEffect, transform.position, Quaternion.identity);
            }

            // Create the actual plant
            GameObject plantObj = new GameObject($"Plant_{Species}_{GetInstanceID()}");
            plantObj.transform.position = transform.position;
            plantObj.transform.rotation = Quaternion.identity;

            PlantOrganism plant = plantObj.AddComponent<PlantOrganism>();
            plant.SetSpecies(Species);
            
            // Start as sprout (skip seed stage since this IS the seed)
            // plant will start growing immediately

            // Destroy the seed
            Destroy(gameObject);
        }

        /// <summary>
        /// Stop physics movement
        /// </summary>
        private void FreezeMovement()
        {
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        /// <summary>
        /// Create a simple visual seed mesh
        /// </summary>
        private void CreateVisualSeed()
        {
            // Create a simple sphere for the seed
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "SeedVisual";
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one;
            visual.transform.localRotation = Quaternion.identity;
            
            // Remove collider from visual
            Destroy(visual.GetComponent<Collider>());
            
            // Apply material
            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = GetSeedColor();
            }
            
            seedMesh = visual;
        }

        /// <summary>
        /// Get color based on species
        /// </summary>
        private Color GetSeedColor()
        {
            return Species switch
            {
                PlantSpecies.Carrot => new Color(0.8f, 0.6f, 0.2f), // Brown/orange
                PlantSpecies.Bush => new Color(0.4f, 0.3f, 0.2f),     // Dark brown
                _ => Color.gray
            };
        }

        void OnDrawGizmosSelected()
        {
            // Draw neighbor check radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, neighborCheckRadius);
            
            // Draw minimum distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxNeighborDistance);
        }

        /// <summary>
        /// Can this seed be eaten?
        /// </summary>
        public bool IsEdible => false; // Seeds aren't food, they're for planting

        /// <summary>
        /// Force destruction (e.g., eaten or stepped on)
        /// </summary>
        public void DestroySeed()
        {
            Destroy(gameObject);
        }
    }
}
