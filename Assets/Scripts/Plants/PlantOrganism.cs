using System;
using UnityEngine;

namespace Albia.Plants
{
    /// <summary>
    /// Base class for all plants in Albia ecosystem.
    /// Growth stages: Seed -> Sprout -> Mature
    /// Reproduces by spreading seeds periodically
    /// Can be eaten by Norns (provides nutrition)
    /// Dies if overcrowded
    /// </summary>
    public class PlantOrganism : MonoBehaviour
    {
        [Header("Plant Species")]
        [SerializeField] private PlantSpecies species = PlantSpecies.Carrot;

        [Header("Growth Settings")]
        [SerializeField] private PlantGrowthStage currentStage = PlantGrowthStage.Seed;
        [SerializeField] private float growthProgress = 0f;
        [SerializeField] private float seedGrowTime = 5f;      // Time to sprout
        [SerializeField] private float sproutGrowTime = 10f;   // Time to mature

        [Header("Health & Nutrition")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float nutritionValue = 15f;   // Energy given when eaten

        [Header("Reproduction")]
        [SerializeField] private float seedInterval = 15f;     // Seconds between seed drops
        [SerializeField] private int seedsPerDrop = 3;
        [SerializeField] private float seedSpreadRadius = 3f;
        [SerializeField] private GameObject seedPrefab;

        [Header("Visuals")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject seedVisual;
        [SerializeField] private GameObject sproutVisual;
        [SerializeField] private GameObject matureVisual;

        // Events
        public event Action<PlantOrganism> OnPlantDeath;
        public event Action<PlantOrganism> OnMature;

        // Properties
        public PlantSpecies Species => species;
        public PlantGrowthStage CurrentStage => currentStage;
        public bool IsMature => currentStage == PlantGrowthStage.Mature;
        public bool IsAlive => currentHealth > 0f;
        public float NutritionValue => currentStage == PlantGrowthStage.Mature ? nutritionValue : nutritionValue * 0.5f;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        // Private state
        private float timeSinceLastSeed = 0f;
        private float timeInCurrentStage = 0f;
        private PlantManager plantManager;
        private BoxCollider plantCollider;
        private bool isRegistered = false;

        void Awake()
        {
            plantManager = PlantManager.Instance;
            
            // Find visuals if not assigned
            if (visualRoot == null) visualRoot = transform;
            if (seedVisual == null) seedVisual = visualRoot.Find("SeedVisual")?.gameObject;
            if (sproutVisual == null) sproutVisual = visualRoot.Find("SproutVisual")?.gameObject;
            if (matureVisual == null) matureVisual = visualRoot.Find("MatureVisual")?.gameObject;
            
            // Get or add collider for eating/detection
            plantCollider = GetComponent<BoxCollider>();
            if (plantCollider == null)
            {
                plantCollider = gameObject.AddComponent<BoxCollider>();
                plantCollider.isTrigger = true;
            }
            
            // Set layer for detection
            gameObject.layer = LayerMask.NameToLayer("Food");
            if (gameObject.layer == 0) gameObject.layer = 8; // Fallback
            tag = "Food";
        }

        void Start()
        {
            // Apply species settings
            ApplySpeciesSettings();
            
            // Register with manager
            if (plantManager != null && !isRegistered)
            {
                plantManager.RegisterPlant(this);
                isRegistered = true;
            }
            
            // Initialize visual state
            UpdateVisuals();
            UpdateColliderSize();
        }

        void Update()
        {
            if (!IsAlive) return;

            timeInCurrentStage += Time.deltaTime;

            // Growth progression
            UpdateGrowth();
            
            // Reproduction for mature plants
            if (IsMature)
            {
                timeSinceLastSeed += Time.deltaTime;
                if (timeSinceLastSeed >= seedInterval)
                {
                    SpawnSeeds();
                    timeSinceLastSeed = 0f;
                }
            }

            // Visual updates
            UpdateVisuals();
        }

        void OnEnable()
        {
            if (plantManager != null && !isRegistered)
            {
                plantManager.RegisterPlant(this);
                isRegistered = true;
            }
        }

        void OnDisable()
        {
            if (plantManager != null && isRegistered)
            {
                plantManager.UnregisterPlant(this);
                isRegistered = false;
            }
        }

        void OnDestroy()
        {
            if (plantManager != null && isRegistered)
            {
                plantManager.UnregisterPlant(this);
            }
        }

        /// <summary>
        /// Apply species-specific settings
        /// </summary>
        private void ApplySpeciesSettings()
        {
            switch (species)
            {
                case PlantSpecies.Carrot:
                    // Fast growing, less nutrition
                    seedGrowTime = 3f;
                    sproutGrowTime = 6f;
                    seedInterval = 12f;
                    seedsPerDrop = 2;
                    nutritionValue = 10f;
                    maxHealth = 50f;
                    break;
                    
                case PlantSpecies.Bush:
                    // Slow growing, lots of food
                    seedGrowTime = 8f;
                    sproutGrowTime = 20f;
                    seedInterval = 25f;
                    seedsPerDrop = 4;
                    nutritionValue = 35f;
                    maxHealth = 150f;
                    break;
            }
            
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Progress through growth stages
        /// </summary>
        private void UpdateGrowth()
        {
            switch (currentStage)
            {
                case PlantGrowthStage.Seed:
                    if (timeInCurrentStage >= seedGrowTime)
                    {
                        TransitionToStage(PlantGrowthStage.Sprout);
                    }
                    break;
                    
                case PlantGrowthStage.Sprout:
                    if (timeInCurrentStage >= sproutGrowTime)
                    {
                        TransitionToStage(PlantGrowthStage.Mature);
                    }
                    break;
            }
        }

        /// <summary>
        /// Transition to a new growth stage
        /// </summary>
        private void TransitionToStage(PlantGrowthStage newStage)
        {
            currentStage = newStage;
            timeInCurrentStage = 0f;
            
            if (newStage == PlantGrowthStage.Mature)
            {
                OnMature?.Invoke(this);
            }
            
            UpdateColliderSize();
        }

        /// <summary>
        /// Spawn seeds for reproduction
        /// </summary>
        private void SpawnSeeds()
        {
            if (seedPrefab == null) return;
            if (plantManager == null) return;

            // Check carrying capacity
            if (!plantManager.CanSpawnSeedInArea(transform.position, species))
            {
                // Overcrowded - take damage instead
                TakeDamage(maxHealth * 0.1f);
                return;
            }

            for (int i = 0; i < seedsPerDrop; i++)
            {
                // Random position around plant
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * seedSpreadRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 1f, randomCircle.y);
                
                // Create seed
                GameObject seedObj = Instantiate(seedPrefab, spawnPos, Quaternion.identity);
                
                Seed seed = seedObj.GetComponent<Seed>();
                if (seed != null)
                {
                    seed.Initialize(species);
                }
            }
        }

        /// <summary>
        /// Called when eaten by a Norn
        /// </summary>
        public bool TryConsume(out float nutrition)
        {
            nutrition = 0f;
            
            if (!IsAlive) return false;
            if (currentStage == PlantGrowthStage.Seed) return false; // Seeds not edible

            nutrition = NutritionValue;
            TakeDamage(maxHealth * 0.5f); // Eating damages the plant
            
            return true;
        }

        /// <summary>
        /// Apply damage to the plant
        /// </summary>
        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Die from damage/disease/overcrowding
        /// </summary>
        private void Die()
        {
            currentHealth = 0f;
            OnPlantDeath?.Invoke(this);
            
            // Notify ecology system
            if (plantManager != null)
            {
                plantManager.UnregisterPlant(this);
            }
            
            // Disable or destroy
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Update visuals based on current growth stage
        /// </summary>
        private void UpdateVisuals()
        {
            if (seedVisual != null) seedVisual.SetActive(currentStage == PlantGrowthStage.Seed);
            if (sproutVisual != null) sproutVisual.SetActive(currentStage == PlantGrowthStage.Sprout);
            if (matureVisual != null) matureVisual.SetActive(currentStage == PlantGrowthStage.Mature);
        }

        /// <summary>
        /// Update collider size based on stage
        /// </summary>
        private void UpdateColliderSize()
        {
            if (plantCollider == null) return;
            
            switch (currentStage)
            {
                case PlantGrowthStage.Seed:
                    plantCollider.size = new Vector3(0.2f, 0.2f, 0.2f);
                    break;
                case PlantGrowthStage.Sprout:
                    plantCollider.size = new Vector3(0.4f, 0.6f, 0.4f);
                    break;
                case PlantGrowthStage.Mature:
                    plantCollider.size = new Vector3(0.8f, 1.2f, 0.8f);
                    break;
            }
        }

        /// <summary>
        /// Set the plant species (must be called before Start)
        /// </summary>
        public void SetSpecies(PlantSpecies newSpecies)
        {
            species = newSpecies;
            ApplySpeciesSettings();
        }

        /// <summary>
        /// Set prefab references
        /// </summary>
        public void SetPrefabs(GameObject seed, GameObject sprout, GameObject mature)
        {
            seedPrefab = seed;
            seedVisual = seed;
            sproutVisual = sprout;
            matureVisual = mature;
        }
    }

    public enum PlantSpecies { Carrot, Bush }
    public enum PlantGrowthStage { Seed, Sprout, Mature }
}
