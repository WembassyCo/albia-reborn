using UnityEngine;

namespace Albia.Ecology
{
    /// <summary>
    /// Simple food source that Norns can eat.
    /// Handles visual representation, collision detection, and respawn.
    /// </summary>    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class FoodSource : MonoBehaviour
    {
        [Header("Food Configuration")]
        [Tooltip("Energy value provided when consumed")]
        [SerializeField] private float energyValue = 30f;
        
        [Tooltip("Health restoration when consumed")]
        [SerializeField] private float healthValue = 5f;
        
        [Tooltip("Maximum amount that can be consumed at once")]
        [SerializeField] private float maxConsumption = 10f;
        
        [Tooltip("Visual mesh type")]
        [SerializeField] private FoodMeshType meshType = FoodMeshType.Capsule;
        
        [Tooltip("Base color of the food")]
        [SerializeField] private Color foodColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        
        [Tooltip("Color when consumed/depleted")]
        [SerializeField] private Color depletedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Respawn Settings")]
        [Tooltip("Enable automatic respawn after consumption")]
        [SerializeField] private bool autoRespawn = true;
        
        [Tooltip("Time in seconds before respawning")]
        [SerializeField] private float respawnTime = 30f;
        
        [Tooltip("Randomize respawn time (+/- this value)")]
        [SerializeField] private float respawnRandomization = 5f;
        
        [Tooltip("Should food regrow gradually instead of instant respawn")]
        [SerializeField] private bool gradualRegrowth = false;
        
        [Tooltip("Time for full regrowth in gradual mode")]
        [SerializeField] private float regrowthTime = 60f;

        [Header("Visual Settings")]
        [Tooltip("Scale of the food object")]
        [SerializeField] private float foodScale = 0.3f;
        
        [Tooltip("Should rotate slowly for visual interest")]
        [SerializeField] private bool rotateAnimation = true;
        
        [Tooltip("Rotation speed")]
        [SerializeField] private float rotationSpeed = 30f;
        
        [Tooltip("Should bob up and down")]
        [SerializeField] private bool bobAnimation = true;
        
        [Tooltip("Bob height")]
        [SerializeField] private float bobHeight = 0.1f;
        
        [Tooltip("Bob speed")]
        [SerializeField] private float bobSpeed = 2f;

        // Nested enum for mesh type
        public enum FoodMeshType
        {
            Sphere,
            Capsule
        }

        // Properties
        public float EnergyValue => energyValue;
        public float HealthValue => healthValue;
        public float MaxConsumption => maxConsumption;
        public bool IsAvailable => currentEnergyValue > 0;
        public float CurrentEnergyValue => currentEnergyValue;
        
        // Layer constant for detection
        public const string FOOD_LAYER_NAME = "Food";
        public const int FOOD_LAYER = 8; // Default food layer

        // Private state
        private float currentEnergyValue;
        private float remainingRespawnTime;
        private float currentRegrowthProgress;
        private bool isConsumed = false;
        private MeshRenderer foodRenderer;
        private SphereCollider foodCollider;
        private Rigidbody foodRigidbody;
        private Transform visualTransform;
        private Vector3 basePosition;
        private float bobOffset;

        // Events
        public System.Action<GameObject> OnConsumed;
        public System.Action OnRespawned;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            basePosition = transform.position;
            bobOffset = Random.Range(0f, Mathf.PI * 2f);
            currentEnergyValue = energyValue;
            currentRegrowthProgress = 1f;
            
            // Ensure on Food layer
            gameObject.layer = FOOD_LAYER;
            
            // Tag for easy finding
            if (string.IsNullOrEmpty(gameObject.tag))
            {
                gameObject.tag = "Food";
            }
            
            CreateVisuals();
        }

        private void Update()
        {
            if (isConsumed)
            {
                HandleRespawn();
            }
            else
            {
                HandleAnimations();
            }
        }

        /// <summary>
        /// Initializes required components
        /// </summary>
        private void InitializeComponents()
        {
            // Setup collider as trigger for detection
            foodCollider = GetComponent<SphereCollider>();
            foodCollider.isTrigger = true;
            foodCollider.radius = foodScale * 1.5f;

            // Setup rigidbody (kinematic for trigger)
            foodRigidbody = GetComponent<Rigidbody>();
            foodRigidbody.isKinematic = true;
            foodRigidbody.useGravity = false;
            
            // Setup layer
            gameObject.layer = FOOD_LAYER;
        }

        /// <summary>
        /// Creates the visual mesh
        /// </summary>
        private void CreateVisuals()
        {
            // Create visual child object
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visualTransform = visual.transform;

            // Add mesh filter and renderer
            MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            foodRenderer = renderer;

            // Create appropriate mesh
            switch (meshType)
            {
                case FoodMeshType.Sphere:
                    meshFilter.mesh = CreateSphereMesh();
                    break;
                case FoodMeshType.Capsule:
                    meshFilter.mesh = CreateCapsuleMesh();
                    break;
            }

            // Create material
            Material material = new Material(Shader.Find("Standard"));
            material.color = foodColor;
            material.SetFloat("_Glossiness", 0.5f);
            material.SetFloat("_Metallic", 0);
            renderer.material = material;

            // Set scale
            visual.transform.localScale = Vector3.one * foodScale;

            // Add glow effect
            Light glowLight = gameObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = foodColor;
            glowLight.intensity = 0.5f;
            glowLight.range = foodScale * 3f;
        }

        /// <summary>
        /// Creates a simple sphere mesh
        /// </summary>
        private Mesh CreateSphereMesh()
        {
            Mesh mesh = new Mesh();
            
            // Simple low-poly sphere
            Vector3[] vertices = new Vector3[12];
            Vector2[] uv = new Vector2[12];
            int[] triangles = new int[]
            {
                0, 1, 2,    0, 2, 3,    0, 3, 4,    0, 4, 1,
                5, 2, 1,    5, 3, 2,    5, 4, 3,    5, 1, 4,
                6, 7, 8,    9, 10, 11
            };
            
            // Create icosphere approximation
            float phi = (1f + Mathf.Sqrt(5f)) / 2f;
            float scale = 0.5f;
            
            vertices[0] = new Vector3(0, scale, phi).normalized * scale;
            vertices[1] = new Vector3(scale, phi, 0).normalized * scale;
            vertices[2] = new Vector3(phi, 0, scale).normalized * scale;
            vertices[3] = new Vector3(-scale, phi, 0).normalized * scale;
            vertices[4] = new Vector3(-phi, 0, scale).normalized * scale;
            vertices[5] = new Vector3(0, -scale, -phi).normalized * scale;
            
            for (int i = 0; i < 6; i++)
            {
                uv[i] = new Vector2((float)i / 6f, 0);
            }
            
            // Use built-in sphere mesh instead for better quality
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            
            return sphereMesh;
        }

        /// <summary>
        /// Creates a simple capsule mesh
        /// </summary>
        private Mesh CreateCapsuleMesh()
        {
            // Use built-in capsule mesh
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Mesh capsuleMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            
            return capsuleMesh;
        }

        /// <summary>
        /// Handles rotation and bob animations
        /// </summary>
        private void HandleAnimations()
        {
            if (visualTransform == null) return;

            // Rotation
            if (rotateAnimation)
            {
                visualTransform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            }

            // Bobbing
            if (bobAnimation)
            {
                float yOffset = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobHeight;
                visualTransform.position = basePosition + Vector3.up * yOffset;
            }
        }

        /// <summary>
        /// Handles respawn logic
        /// </summary>
        private void HandleRespawn()
        {
            if (!autoRespawn) return;

            if (gradualRegrowth)
            {
                // Gradual regrowth
                currentRegrowthProgress += Time.deltaTime / regrowthTime;
                
                if (currentRegrowthProgress >= 1f)
                {
                    Respawn();
                }
                else
                {
                    // Update visual scale
                    if (visualTransform != null)
                    {
                        visualTransform.localScale = Vector3.one * foodScale * currentRegrowthProgress;
                    }
                }
            }
            else
            {
                // Instant respawn after timer
                remainingRespawnTime -= Time.deltaTime;
                
                if (remainingRespawnTime <= 0f)
                {
                    Respawn();
                }
            }
        }

        /// <summary>
        /// Consumes food and returns the energy consumed
        /// </summary>
        public virtual float Consume(float amount)
        {
            if (!IsAvailable) return 0f;

            float consumed = Mathf.Min(amount, maxConsumption, currentEnergyValue);
            currentEnergyValue -= consumed;

            // Update visual
            UpdateVisualState();

            // Check if fully consumed
            if (currentEnergyValue <= 0)
            {
                Deplete();
            }

            return consumed;
        }

        /// <summary>
        /// Fully consumes the food source
        /// </summary>
        public virtual void ConsumeFully()
        {
            Consume(currentEnergyValue);
        }

        /// <summary>
        /// Depletes the food (sets up for respawn)
        /// </summary>
        private void Deplete()
        {
            isConsumed = true;
            currentEnergyValue = 0f;
            
            remainingRespawnTime = respawnTime + Random.Range(-respawnRandomization, respawnRandomization);
            currentRegrowthProgress = 0f;

            // Hide visual
            if (visualTransform != null)
            {
                visualTransform.gameObject.SetActive(false);
            }

            // Disable collider
            if (foodCollider != null)
            {
                foodCollider.enabled = false;
            }

            OnConsumed?.Invoke(gameObject);
        }

        /// <summary>
        /// Respawns the food
        /// </summary>
        public virtual void Respawn()
        {
            isConsumed = false;
            currentEnergyValue = energyValue;
            currentRegrowthProgress = 1f;
            remainingRespawnTime = 0f;

            // Show visual
            if (visualTransform != null)
            {
                visualTransform.gameObject.SetActive(true);
                visualTransform.localScale = Vector3.one * foodScale;
            }

            // Enable collider
            if (foodCollider != null)
            {
                foodCollider.enabled = true;
            }

            // Reset material color
            if (foodRenderer != null && foodRenderer.material != null)
            {
                foodRenderer.material.color = foodColor;
            }

            // Randomize position slightly
            Vector2 randomOffset = Random.insideUnitCircle * 2f;
            transform.position = basePosition + new Vector3(randomOffset.x, 0, randomOffset.y);

            OnRespawned?.Invoke();
        }

        /// <summary>
        /// Updates visual based on remaining food
        /// </summary>
        private void UpdateVisualState()
        {
            if (foodRenderer == null) return;

            float ratio = currentEnergyValue / energyValue;
            foodRenderer.material.color = Color.Lerp(depletedColor, foodColor, ratio);
            
            if (visualTransform != null && ratio > 0)
            {
                visualTransform.localScale = Vector3.one * foodScale * (0.5f + 0.5f * ratio);
            }
        }

        /// <summary>
        /// Force respawn with custom delay
        /// </summary>
        public void ForceRespawn(float delay = 0f)
        {
            if (delay <= 0f)
            {
                Respawn();
            }
            else
            {
                isConsumed = true;
                currentEnergyValue = 0f;
                remainingRespawnTime = delay;
                
                if (visualTransform != null)
                {
                    visualTransform.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Gets information about the food source
        /// </summary>
        public string GetInfo()
        {
            return $"Food: {currentEnergyValue:F1}/{energyValue:F1} | " +
                   $"Available: {IsAvailable} | " +
                   $"Health: +{healthValue:F1}";
        }

        // Trigger detection for proximity sensing
        protected virtual void OnTriggerEnter(Collider other)
        {
            // Can be used to signal nearby creatures
            // Will be detected by Norn SensorySystem
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            // Continuous detection
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            // Exit detection
        }

        // Visual gizmos
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = IsAvailable ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position, foodScale * 0.5f);
            
            // Energy value indicator
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                float barHeight = 0.1f;
                float barWidth = foodScale;
                Vector3 barPos = transform.position + Vector3.up * (foodScale * 0.8f);
                
                float energyRatio = currentEnergyValue / energyValue;
                Gizmos.DrawCube(barPos, new Vector3(barWidth * energyRatio, barHeight, 0.05f));
            }
        }

        protected virtual void OnDestroy()
        {
            // Cleanup
            if (foodRenderer != null && foodRenderer.material != null)
            {
                Destroy(foodRenderer.material);
            }
        }
    }
}
