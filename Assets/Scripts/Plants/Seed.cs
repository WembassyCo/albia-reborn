using UnityEngine;

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
        [SerializeField] private float germinationTime = 5f;
        [SerializeField] private float despawnTime = 60f;
        [SerializeField] private float neighborCheckRadius = 2f;
        [SerializeField] private float maxNeighborDistance = 1.5f;
        [SerializeField] private int maxNeighbors = 5;

        [Header("Physics")]
        [SerializeField] private bool usePhysics = true;
        [SerializeField] private float maxFallSpeed = 10f;
        [SerializeField] private float bounceDamping = 0.5f;

        [Header("Visuals")]
        [SerializeField] private GameObject seedMesh;
        [SerializeField] private float visualScale = 0.15f;

        [Header("Germination Effect")]
        [SerializeField] private GameObject germinationEffect;

        public PlantSpecies Species { get; private set; } = PlantSpecies.Carrot;
        public bool IsGerminated { get; private set; } = false;
        public float Age { get; private set; } = 0f;

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
            
            seedCollider = GetComponent<Collider>();
            if (seedCollider == null)
            {
                seedCollider = gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)seedCollider).radius = 0.1f;
                seedCollider.isTrigger = false;
            }
            
            if (seedMesh == null)
            {
                CreateVisualSeed();
            }
            
            transform.localScale = Vector3.one * visualScale;
            lastPosition = transform.position;
        }

        void Start()
        {
            transform.rotation = Random.rotation;
            
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
            CheckIfLanded();

            if (hasLanded)
            {
                germinationTimer += Time.deltaTime;
                
                if (germinationTimer >= germinationTime)
                {
                    TryGerminate();
                }
            }

            if (Age >= despawnTime)
            {
                Destroy(gameObject);
            }

            if (rb != null && rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
            }

            lastPosition = transform.position;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (rb != null)
            {
                rb.velocity *= bounceDamping;
            }
            
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || 
                collision.gameObject.CompareTag("Terrain"))
            {
                hasLanded = true;
                
                if (rb != null)
                {
                    Invoke(nameof(FreezeMovement), 2f);
                }
            }
        }

        public void Initialize(PlantSpecies species)
        {
            Species = species;
            
            switch (species)
            {
                case PlantSpecies.Carrot:
                    germinationTime = 3f;
                    maxNeighbors = 6;
                    break;
                case PlantSpecies.Bush:
                    germinationTime = 6f;
                    maxNeighbors = 4;
                    break;
            }
        }

        private void CheckIfLanded()
        {
            if (hasLanded) return;
            
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.2f))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                    hit.collider.CompareTag("Terrain"))
                {
                    hasLanded = true;
                }
            }
            
            if (rb != null && rb.velocity.magnitude < 0.1f && Age > 1f)
            {
                hasLanded = true;
            }
        }

        private void TryGerminate()
        {
            if (!CheckGrowthConditions())
            {
                canGrow = false;
                return;
            }

            Germinate();
        }

        private bool CheckGrowthConditions()
        {
            if (plantManager != null)
            {
                return plantManager.CanSpawnSeedInArea(transform.position, Species);
            }
            
            Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborCheckRadius);
            int plantCount = 0;
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor.GetComponent<PlantOrganism>() != null)
                {
                    float dist = Vector3.Distance(transform.position, neighbor.transform.position);
                    if (dist < maxNeighborDistance)
                    {
                        return false;
                    }
                    plantCount++;
                }
            }
            
            return plantCount < maxNeighbors;
        }

        private void Germinate()
        {
            IsGerminated = true;

            if (germinationEffect != null)
            {
                Instantiate(germinationEffect, transform.position, Quaternion.identity);
            }

            GameObject plantObj = new GameObject($"Plant_{Species}_{GetInstanceID()}");
            plantObj.transform.position = transform.position;
            plantObj.transform.rotation = Quaternion.identity;

            PlantOrganism plant = plantObj.AddComponent<PlantOrganism>();
            plant.SetSpecies(Species);

            Destroy(gameObject);
        }

        private void FreezeMovement()
        {
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        private void CreateVisualSeed()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "SeedVisual";
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one;
            visual.transform.localRotation = Quaternion.identity;
            
            Destroy(visual.GetComponent<Collider>());
            
            Renderer rend = visual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = GetSeedColor();
            }
            
            seedMesh = visual;
        }

        private Color GetSeedColor()
        {
            return Species switch
            {
                PlantSpecies.Carrot => new Color(0.8f, 0.6f, 0.2f),
                PlantSpecies.Bush => new Color(0.4f, 0.3f, 0.2f),
                _ => Color.gray
            };
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, neighborCheckRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxNeighborDistance);
        }

        public bool IsEdible => false;

        public void DestroySeed()
        {
            Destroy(gameObject);
        }
    }
}
