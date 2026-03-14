using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Albia.Ecology
{
    /// <summary>
    /// Manages the ecosystem - plant populations, food respawn, and carrying capacity.
    /// MVP: Simple random spawn in valid areas
    /// </summary>
    public class EcologyManager : MonoBehaviour
    {
        [Header("System Configuration")]
        [Tooltip("Enable/disable entire ecosystem")]
        [SerializeField] private bool ecosystemEnabled = true;
        
        [Tooltip("Update interval for ecosystem calculations (seconds)")]
        [SerializeField] private float updateInterval = 1f;

        [Header("Plant Population")]
        [Tooltip("Maximum plants per region")]
        [SerializeField] private int maxPlantsPerRegion = 50;
        
        [Tooltip("Global maximum plants (soft limit)")]
        [SerializeField] private int globalMaxPlants = 200;
        
        [Tooltip("Plant prefabs to spawn")]
        [SerializeField] private GameObject[] plantPrefabs;
        
        [Tooltip("Initial plant spawn count")]
        [SerializeField] private int initialPlantCount = 20;

        [Header("Food Sources")]
        [Tooltip("Food prefab to spawn")]
        [SerializeField] private GameObject foodPrefab;
        
        [Tooltip("Maximum food items per region")]
        [SerializeField] private int maxFoodPerRegion = 30;
        
        [Tooltip("Global maximum food items")]
        [SerializeField] private int globalMaxFood = 100;
        
        [Tooltip("Initial food spawn count")]
        [SerializeField] private int initialFoodCount = 15;
        
        [Tooltip("Auto-replace consumed food")]
        [SerializeField] private bool autoReplenishFood = true;
        
        [Tooltip("Minimum time between food spawns")]
        [SerializeField] private float minFoodSpawnInterval = 5f;

        [Header("Spawn Regions")]
        [Tooltip("Define valid spawn regions (leave empty for world bounds)")]
        [SerializeField] private SpawnRegion[] spawnRegions;
        
        [Tooltip("Minimum spawn height (ground level)")]
        [SerializeField] private float minSpawnHeight = 0.5f;
        
        [Tooltip("Maximum spawn height")]
        [SerializeField] private float maxSpawnHeight = 50f;
        
        [Tooltip("Ground layer mask for raycasting")]
        [SerializeField] private LayerMask groundLayer = ~0; // All layers by default

        [Header("Seasonal Settings (Full Version)")]
        [Tooltip("Enable seasonal cycles")]
        [SerializeField] private bool enableSeasons = false;
        
        [Tooltip("Season length in game minutes")]
        [SerializeField] private float seasonLength = 10f;
        
        [Tooltip("Growth modifier per season")]
        [SerializeField] private AnimationCurve seasonalGrowthCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Events")]
        public UnityEvent<GameObject> OnPlantSpawned;
        public UnityEvent<GameObject> OnFoodSpawned;
        public UnityEvent<GameObject> OnPlantDied;
        public UnityEvent<GameObject> OnFoodConsumed;

        [System.Serializable]
        public struct SpawnRegion
        {
            public string regionName;
            public Vector3 center;
            public Vector3 size;
            public int maxPlants;
            public int maxFood;
            public float spawnWeight; // Probability weight
            [Tooltip("Biome type for this region")]
            public string biomeType;
            [Tooltip("Extra carrying capacity modifier")]
            public float capacityModifier;
        }

        // Runtime data
        private List<PlantOrganism> activePlants = new List<PlantOrganism>();
        private List<FoodSource> activeFood = new List<FoodSource>();
        private Dictionary<SpawnRegion, List<PlantOrganism>> plantsByRegion = new Dictionary<SpawnRegion, List<PlantOrganism>>();
        private float lastUpdateTime;
        private float lastFoodSpawnTime;
        private float currentSeasonTime = 0f;
        private int currentSeason = 0; // 0=Spring, 1=Summer, 2=Fall, 3=Winter
        
        // Properties
        public int PlantCount => activePlants.Count;
        public int FoodCount => activeFood.Count;
        public bool IsEnabled => ecosystemEnabled;
        public int CurrentSeason => currentSeason;
        public static EcologyManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Validate spawn regions
            if (spawnRegions == null || spawnRegions.Length == 0)
            {
                CreateDefaultRegion();
            }
        }

        private void Start()
        {
            if (ecosystemEnabled)
            {
                InitializeEcosystem();
            }
        }

        private void Update()
        {
            if (!ecosystemEnabled) return;

            // Periodic updates
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateEcosystem();
            }

            // Season update
            if (enableSeasons)
            {
                UpdateSeason(Time.deltaTime);
            }
        }

        /// <summary>
        /// Creates a default spawn region if none defined
        /// </summary>
        private void CreateDefaultRegion()
        {
            spawnRegions = new SpawnRegion[1];
            spawnRegions[0] = new SpawnRegion
            {
                regionName = "Default",
                center = Vector3.zero,
                size = new Vector3(100f, 50f, 100f),
                maxPlants = maxPlantsPerRegion,
                maxFood = maxFoodPerRegion,
                spawnWeight = 1f,
                biomeType = "Temperate",
                capacityModifier = 1f
            };
        }

        /// <summary>
        /// Initializes the ecosystem with initial spawns
        /// </summary>
        public void InitializeEcosystem()
        {
            ClearEcosystem();

            // Spawn initial plants
            for (int i = 0; i < initialPlantCount; i++)
            {
                SpawnRandomPlant();
            }

            // Spawn initial food
            for (int i = 0; i < initialFoodCount; i++)
            {
                SpawnRandomFood();
            }

            Debug.Log($"[EcologyManager] Initialized with {activePlants.Count} plants and {activeFood.Count} food sources");
        }

        /// <summary>
        /// Main ecosystem update loop
        /// </summary>
        private void UpdateEcosystem()
        {
            // Clean up dead/null references
            CleanupLists();

            // Replenish food if needed
            if (autoReplenishFood && Time.time - lastFoodSpawnTime >= minFoodSpawnInterval)
            {
                MaintainFoodPopulation();
            }

            // Check regional carrying capacities
            EnforceCarryingCapacity();

            // Update plant environmental conditions
            UpdatePlantConditions();
        }

        /// <summary>
        /// Updates seasonal cycle
        /// </summary>
        private void UpdateSeason(float deltaTime)
        {
            currentSeasonTime += deltaTime;
            
            if (currentSeasonTime >= seasonLength * 60f) // Convert to seconds
            {
                currentSeasonTime = 0f;
                currentSeason = (currentSeason + 1) % 4;
                OnSeasonChanged();
            }
        }

        /// <summary>
        /// Called when season changes
        /// </summary>
        private void OnSeasonChanged()
        {
            string[] seasonNames = { "Spring", "Summer", "Fall", "Winter" };
            Debug.Log($"[EcologyManager] Season changed to {seasonNames[currentSeason]}");
            
            // Notify all plants
            float growthModifier = GetSeasonalGrowthModifier();
            foreach (var plant in activePlants)
            {
                if (plant != null)
                {
                    // Apply seasonal modifier to plants
                    // This integrates with PlantOrganism's growth calculation
                }
            }
        }

        /// <summary>
        /// Gets the current seasonal growth modifier
        /// </summary>
        public float GetSeasonalGrowthModifier()
        {
            if (!enableSeasons) return 1f;
            float seasonProgress = currentSeasonTime / (seasonLength * 60f);
            return seasonalGrowthCurve.Evaluate((currentSeason + seasonProgress) / 4f);
        }

        /// <summary>
        /// Spawns a random plant in a valid region
        /// </summary>
        public PlantOrganism SpawnRandomPlant(SpawnRegion? specificRegion = null)
        {
            if (activePlants.Count >= globalMaxPlants) return null;
            if (plantPrefabs == null || plantPrefabs.Length == 0)
            {
                Debug.LogWarning("[EcologyManager] No plant prefabs assigned!");
                return null;
            }

            // Select region
            SpawnRegion region = specificRegion ?? SelectRandomRegion();
            
            // Check regional capacity
            int regionCount = GetPlantCountInRegion(region);
            int regionalMax = Mathf.RoundToInt(region.maxPlants * region.capacityModifier);
            if (regionCount >= regionalMax)
            {
                return null;
            }

            // Select random prefab
            GameObject prefab = plantPrefabs[Random.Range(0, plantPrefabs.Length)];
            
            // Calculate spawn position
            Vector3 spawnPos = GetRandomPositionInRegion(region);
            
            if (spawnPos == Vector3.zero) return null;

            // Spawn plant
            GameObject plantObj = Instantiate(prefab, spawnPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            PlantOrganism plant = plantObj.GetComponent<PlantOrganism>();
            
            if (plant == null)
            {
                plant = plantObj.AddComponent<PlantOrganism>();
            }

            // Initialize plant
            plant.Initialize();
            
            // Set environmental conditions
            plant.UpdateEnvironmentalConditions(GetSunlightAtPosition(spawnPos), region.biomeType);

            // Track plant
            activePlants.Add(plant);
            AddPlantToRegion(region, plant);
            
            // Events
            OnPlantSpawned?.Invoke(plantObj);
            plant.OnDeath += () => OnPlantDied?.Invoke(plantObj);

            return plant;
        }

        /// <summary>
        /// Spawns random food in a valid region
        /// </summary>
        public FoodSource SpawnRandomFood(SpawnRegion? specificRegion = null)
        {
            if (activeFood.Count >= globalMaxFood) return null;
            if (foodPrefab == null)
            {
                Debug.LogWarning("[EcologyManager] No food prefab assigned!");
                return null;
            }

            // Select region
            SpawnRegion region = specificRegion ?? SelectRandomRegion();
            
            // Check regional capacity
            int regionCount = GetFoodCountInRegion(region);
            int regionalMax = Mathf.RoundToInt(region.maxFood * region.capacityModifier);
            if (regionCount >= regionalMax)
            {
                return null;
            }

            // Calculate spawn position
            Vector3 spawnPos = GetRandomPositionInRegion(region);
            
            if (spawnPos == Vector3.zero) return null;

            // Spawn food
            GameObject foodObj = Instantiate(foodPrefab, spawnPos, Quaternion.identity);
            FoodSource food = foodObj.GetComponent<FoodSource>();
            
            if (food == null)
            {
                Debug.LogWarning($"[EcologyManager] Spawned food object missing FoodSource component: {foodObj.name}");
                Destroy(foodObj);
                return null;
            }

            // Configure food
            lastFoodSpawnTime = Time.time;

            // Track food
            activeFood.Add(food);
            
            // Events
            OnFoodSpawned?.Invoke(foodObj);
            food.OnConsumed += (go) => 
            {
                OnFoodConsumed?.Invoke(go);
                activeFood.Remove(food);
            };

            return food;
        }

        /// <summary>
        /// Selects a random spawn region based on weights
        /// </summary>
        private SpawnRegion SelectRandomRegion()
        {
            if (spawnRegions.Length == 1) return spawnRegions[0];

            float totalWeight = 0f;
            foreach (var region in spawnRegions)
            {
                totalWeight += region.spawnWeight;
            }

            float random = Random.value * totalWeight;
            float current = 0f;

            foreach (var region in spawnRegions)
            {
                current += region.spawnWeight;
                if (random <= current)
                {
                    return region;
                }
            }

            return spawnRegions[spawnRegions.Length - 1];
        }

        /// <summary>
        /// Gets a random valid position within a region
        /// </summary>
        private Vector3 GetRandomPositionInRegion(SpawnRegion region)
        {
            for (int attempts = 0; attempts < 10; attempts++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-region.size.x / 2f, region.size.x / 2f),
                    Random.Range(-region.size.y / 2f, region.size.y / 2f),
                    Random.Range(-region.size.z / 2f, region.size.z / 2f)
                );

                Vector3 worldPos = region.center + randomPos;
                
                // Validate ground height
                float groundHeight = GetGroundHeight(worldPos);
                
                if (groundHeight >= 0 && groundHeight >= minSpawnHeight && groundHeight <= maxSpawnHeight)
                {
                    worldPos.y = groundHeight + 0.1f; // Slightly above ground
                    return worldPos;
                }
            }

            return Vector3.zero; // Failed to find valid position
        }

        /// <summary>
        /// Gets the ground height at a world position
        /// </summary>
        private float GetGroundHeight(Vector3 worldPos)
        {
            if (Physics.Raycast(new Vector3(worldPos.x, maxSpawnHeight, worldPos.z), 
                              Vector3.down, out RaycastHit hit, maxSpawnHeight * 2f, groundLayer))
            {
                return hit.point.y;
            }

            return -1f; // Invalid (no ground found)
        }

        /// <summary>
        /// Gets sunlight level at position (placeholder - integrate with lighting system)
        /// </summary>
        private float GetSunlightAtPosition(Vector3 position)
        {
            // Simple raycast to check for sky
            if (Physics.Raycast(position, Vector3.up, out _, 100f, groundLayer))
            {
                return 0.5f; // Shade
            }
            return 1f; // Full sun
        }

        /// <summary>
        /// Maintains food population at target levels
        /// </summary>
        private void MaintainFoodPopulation()
        {
            // Count consumed/depleted food
            int missingFood = 0;
            foreach (var food in activeFood)
            {
                if (food == null || !food.IsAvailable)
                {
                    missingFood++;
                }
            }

            // Spawn replacement food
            int foodToSpawn = Mathf.Min(missingFood, 3); // Max 3 per update
            for (int i = 0; i < foodToSpawn; i++)
            {
                SpawnRandomFood();
            }
        }

        /// <summary>
        /// Enforces regional carrying capacity
        /// </summary>
        private void EnforceCarryingCapacity()
        {
            // Check each region
            foreach (var region in spawnRegions)
            {
                int plantCount = GetPlantCountInRegion(region);
                int regionalMax = Mathf.RoundToInt(region.maxPlants * region.capacityModifier);
                
                if (plantCount > regionalMax * 1.2f) // Allow 20% overflow before pruning
                {
                    // Remove oldest plants first
                    PrunePlantsInRegion(region, plantCount - regionalMax);
                }
            }
        }

        /// <summary>
        /// Removes excess plants from a region
        /// </summary>
        private void PrunePlantsInRegion(SpawnRegion region, int count)
        {
            if (!plantsByRegion.TryGetValue(region, out var plants)) return;
            
            // Sort by age and remove oldest
            plants.Sort((a, b) => b.Age.CompareTo(a.Age));
            
            for (int i = 0; i < Mathf.Min(count, plants.Count); i++)
            {
                if (plants[i] != null)
                {
                    plants[i].Die();
                }
            }
        }

        /// <summary>
        /// Updates environmental conditions for all plants
        /// </summary>
        private void UpdatePlantConditions()
        {
            foreach (var plant in activePlants)
            {
                if (plant == null) continue;
                
                Vector3 pos = plant.transform.position;
                float sunlight = GetSunlightAtPosition(pos);
                string biome = GetBiomeAtPosition(pos);
                
                plant.UpdateEnvironmentalConditions(sunlight, biome);
            }
        }

        /// <summary>
        /// Gets the biome at a position
        /// </summary>
        private string GetBiomeAtPosition(Vector3 position)
        {
            // Find which region contains this position
            foreach (var region in spawnRegions)
            {
                if (IsPointInRegion(position, region))
                {
                    return region.biomeType;
                }
            }
            return "Unknown";
        }

        /// <summary>
        /// Checks if a point is within a region
        /// </summary>
        private bool IsPointInRegion(Vector3 point, SpawnRegion region)
        {
            Vector3 local = point - region.center;
            return Mathf.Abs(local.x) <= region.size.x / 2f &&
                   Mathf.Abs(local.y) <= region.size.y / 2f &&
                   Mathf.Abs(local.z) <= region.size.z / 2f;
        }

        /// <summary>
        /// Gets plant count in a specific region
        /// </summary>
        private int GetPlantCountInRegion(SpawnRegion region)
        {
            if (!plantsByRegion.TryGetValue(region, out var plants)) return 0;
            return plants.Count;
        }

        /// <summary>
        /// Gets food count in a specific region
        /// </summary>
        private int GetFoodCountInRegion(SpawnRegion region)
        {
            int count = 0;
            foreach (var food in activeFood)
            {
                if (food != null && IsPointInRegion(food.transform.position, region))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Adds plant to region tracking
        /// </summary>
        private void AddPlantToRegion(SpawnRegion region, PlantOrganism plant)
        {
            if (!plantsByRegion.ContainsKey(region))
            {
                plantsByRegion[region] = new List<PlantOrganism>();
            }
            plantsByRegion[region].Add(plant);
        }

        /// <summary>
        /// Cleans up null references in lists
        /// </summary>
        private void CleanupLists()
        {
            activePlants.RemoveAll(p => p == null);
            activeFood.RemoveAll(f => f == null);
            
            foreach (var region in plantsByRegion.Keys)
            {
                plantsByRegion[region].RemoveAll(p => p == null);
            }
        }

        /// <summary>
        /// Clears all ecosystem objects
        /// </summary>
        public void ClearEcosystem()
        {
            foreach (var plant in activePlants)
            {
                if (plant != null)
                {
                    Destroy(plant.gameObject);
                }
            }
            activePlants.Clear();
            plantsByRegion.Clear();

            foreach (var food in activeFood)
            {
                if (food != null)
                {
                    Destroy(food.gameObject);
                }
            }
            activeFood.Clear();
        }

        /// <summary>
        /// Gets nearest food source to a position
        /// </summary>
        public FoodSource GetNearestFood(Vector3 position, float maxDistance = float.MaxValue)
        {
            FoodSource nearest = null;
            float nearestDist = maxDistance;

            foreach (var food in activeFood)
            {
                if (food == null || !food.IsAvailable) continue;
                
                float dist = Vector3.Distance(position, food.transform.position);
                if (dist < nearestDist)
                {
                    nearest = food;
                    nearestDist = dist;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Gets all plants within a radius
        /// </summary>
        public List<PlantOrganism> GetPlantsInRadius(Vector3 center, float radius)
        {
            List<PlantOrganism> result = new List<PlantOrganism>();
            float sqrRadius = radius * radius;

            foreach (var plant in activePlants)
            {
                if (plant == null) continue;
                
                if ((plant.transform.position - center).sqrMagnitude <= sqrRadius)
                {
                    result.Add(plant);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets ecosystem statistics
        /// </summary>
        public string GetStats()
        {
            int maturePlants = 0;
            foreach (var plant in activePlants)
            {
                if (plant != null && plant.IsMature) maturePlants++;
            }

            int availableFood = 0;
            foreach (var food in activeFood)
            {
                if (food != null && food.IsAvailable) availableFood++;
            }

            return $"Plants: {activePlants.Count} ({maturePlants} mature) | " +
                   $"Food: {availableFood}/{activeFood.Count} | " +
                   $"Season: {(enableSeasons ? currentSeason.ToString() : "N/A")}";
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Visual gizmos for regions
        private void OnDrawGizmosSelected()
        {
            if (spawnRegions == null) return;

            foreach (var region in spawnRegions)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawCube(region.center, region.size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(region.center, region.size);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(region.center + Vector3.up * region.size.y * 0.6f, 
                    $"{region.regionName}\nPlants: {region.maxPlants} | Food: {region.maxFood}");
                #endif
            }
        }
    }
}
