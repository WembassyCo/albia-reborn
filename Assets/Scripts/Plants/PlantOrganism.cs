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
        [SerializeField] private float seedGrowTime = 5f;
        [SerializeField] private float sproutGrowTime = 10f;

        [Header("Health & Nutrition")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float nutritionValue = 15f;

        [Header("Reproduction")]
        [SerializeField] private float seedInterval = 15f;
        [SerializeField] private int seedsPerDrop = 3;
        [SerializeField] private float seedSpreadRadius = 3f;
        [SerializeField] private GameObject seedPrefab;

        [Header("Visuals")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject seedVisual;
        [SerializeField] private GameObject sproutVisual;
        [SerializeField] private GameObject matureVisual;

        public event Action<PlantOrganism> OnPlantDeath;
        public event Action<PlantOrganism> OnMature;

        public PlantSpecies Species => species;
        public PlantGrowthStage CurrentStage => currentStage;
        public bool IsMature => currentStage == PlantGrowthStage.Mature;
        public bool IsAlive => currentHealth > 0f;
        public float NutritionValue => currentStage == PlantGrowthStage.Mature ? nutritionValue : nutritionValue * 0.5f;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private float timeSinceLastSeed = 0f;
        private float timeInCurrentStage = 0f;
        private PlantManager plantManager;
        private BoxCollider plantCollider;
        private bool isRegistered = false;

        void Awake()
        {
            plantManager = PlantManager.Instance;
            
            if (visualRoot == null) visualRoot = transform;
            if (seedVisual == null) seedVisual = visualRoot.Find("SeedVisual")?.gameObject;
            if (sproutVisual == null) sproutVisual = visualRoot.Find("SproutVisual")?.gameObject;
            if (matureVisual == null) matureVisual = visualRoot.Find("MatureVisual")?.gameObject;
            
            plantCollider = GetComponent<BoxCollider>();
            if (plantCollider == null)
            {
                plantCollider = gameObject.AddComponent<BoxCollider>();
                plantCollider.isTrigger = true;
            }
            
            gameObject.layer = LayerMask.NameToLayer("Food");
            if (gameObject.layer == 0) gameObject.layer = 8;
            tag = "Food";
        }

        void Start()
        {
            ApplySpeciesSettings();
            
            if (plantManager != null && !isRegistered)
            {
                plantManager.RegisterPlant(this);
                isRegistered = true;
            }
            
            UpdateVisuals();
            UpdateColliderSize();
        }

        void Update()
        {
            if (!IsAlive) return;

            timeInCurrentStage += Time.deltaTime;
            UpdateGrowth();
            
            if (IsMature)
            {
                timeSinceLastSeed += Time.deltaTime;
                if (timeSinceLastSeed >= seedInterval)
                {
                    SpawnSeeds();
                    timeSinceLastSeed = 0f;
                }
            }

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

        private void ApplySpeciesSettings()
        {
            switch (species)
            {
                case PlantSpecies.Carrot:
                    seedGrowTime = 3f;
                    sproutGrowTime = 6f;
                    seedInterval = 12f;
                    seedsPerDrop = 2;
                    nutritionValue = 10f;
                    maxHealth = 50f;
                    break;
                    
                case PlantSpecies.Bush:
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

        private void SpawnSeeds()
        {
            if (seedPrefab == null) return;
            if (plantManager == null) return;

            if (!plantManager.CanSpawnSeedInArea(transform.position, species))
            {
                TakeDamage(maxHealth * 0.1f);
                return;
            }

            for (int i = 0; i < seedsPerDrop; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * seedSpreadRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 1f, randomCircle.y);
                
                GameObject seedObj = Instantiate(seedPrefab, spawnPos, Quaternion.identity);
                
                Seed seed = seedObj.GetComponent<Seed>();
                if (seed != null)
                {
                    seed.Initialize(species);
                }
            }
        }

        public bool TryConsume(out float nutrition)
        {
            nutrition = 0f;
            
            if (!IsAlive) return false;
            if (currentStage == PlantGrowthStage.Seed) return false;

            nutrition = NutritionValue;
            TakeDamage(maxHealth * 0.5f);
            
            return true;
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            currentHealth = 0f;
            OnPlantDeath?.Invoke(this);
            
            if (plantManager != null)
            {
                plantManager.UnregisterPlant(this);
            }
            
            gameObject.SetActive(false);
        }

        private void UpdateVisuals()
        {
            if (seedVisual != null) seedVisual.SetActive(currentStage == PlantGrowthStage.Seed);
            if (sproutVisual != null) sproutVisual.SetActive(currentStage == PlantGrowthStage.Sprout);
            if (matureVisual != null) matureVisual.SetActive(currentStage == PlantGrowthStage.Mature);
        }

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

        public void SetSpecies(PlantSpecies newSpecies)
        {
            species = newSpecies;
            ApplySpeciesSettings();
        }

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
