using System.Collections.Generic;
using UnityEngine;
using Albia.Ecology;

namespace Albia.Plants
{
    /// <summary>
    /// Manages all plants in the ecosystem.
    /// - Tracks plant populations
    /// - Species: Carrot (fast), Bush (slow, lots of food)
    /// - Enforces carrying capacity per region
    /// - Integrates with EcologyManager
    /// </summary>
    public class PlantManager : MonoBehaviour
    {
        public static PlantManager Instance { get; private set; }

        [System.Serializable]
        public class SpeciesSettings
        {
            public PlantSpecies species;
            public int maxPopulation = 50;
            public float carryingCapacityRadius = 10f;
            public int maxPlantsInRadius = 8;
            public float spawnRate = 1f;
        }

        [Header("Species Configuration")]
        [SerializeField] private List<SpeciesSettings> speciesSettings = new List<SpeciesSettings>
        {
            new SpeciesSettings { 
                species = PlantSpecies.Carrot, 
                maxPopulation = 60,
                carryingCapacityRadius = 8f,
                maxPlantsInRadius = 10,
                spawnRate = 1.2f
            },
            new SpeciesSettings { 
                species = PlantSpecies.Bush, 
                maxPopulation = 30,
                carryingCapacityRadius = 12f,
                maxPlantsInRadius = 5,
                spawnRate = 0.7f
            }
        };

        [Header("Global Limits")]
        [SerializeField] private int globalMaxPlants = 100;
        [SerializeField] private float worldRadius = 50f;

        [Header("Plant Prefabs")]
        [SerializeField] private GameObject carrotPlantPrefab;
        [SerializeField] private GameObject bushPlantPrefab;
        [SerializeField] private GameObject seedPrefab;

        [Header("Ecosystem Integration")]
        [SerializeField] private bool autoSpawnInitialPlants = true;
        [SerializeField] private int initialCarrotCount = 15;
        [SerializeField] private int initialBushCount = 8;

        // Tracking
        private List<PlantOrganism> allPlants = new List<PlantOrganism>();
        private Dictionary<PlantSpecies, List<PlantOrganism>> plantsBySpecies = new Dictionary<PlantSpecies, List<PlantOrganism>>();
        private Dictionary<Vector2Int, RegionData> regionGrid = new Dictionary<Vector2Int, RegionData>();
        private float regionSize = 10f;

        // Integration
        private EcologyManager ecologyManager;

        public IReadOnlyList<PlantOrganism> AllPlants => allPlants;
        public int TotalPlantCount => allPlants.Count;

        void Awake()
        {
            Instance = this;
            InitializeDictionaries();
        }

        void Start()
        {
            ecologyManager = EcologyManager.Instance;
            
            if (autoSpawnInitialPlants)
            {
                SpawnInitialPlants();
            }
        }

        void Update()
        {
            // Periodic cleanup and region updates
            if (Time.frameCount % 60 == 0)
            {
                CleanupDeadPlants();
                UpdateRegionData();
            }
        }

        /// <summary>
        /// Initialize tracking dictionaries
        /// </summary>
        private void InitializeDictionaries()
        {
            plantsBySpecies[PlantSpecies.Carrot] = new List<PlantOrganism>();
            plantsBySpecies[PlantSpecies.Bush] = new List<PlantOrganism>();
        }

        /// <summary>
        /// Spawn initial ecosystem plants
        /// </summary>
        private void SpawnInitialPlants()
        {
            // Carrots - fast growing, scattered
            for (int i = 0; i < initialCarrotCount; i++)
            {
                SpawnPlantAtRandomLocation(PlantSpecies.Carrot, PlantGrowthStage.Mature);
            }

            // Bushes - fewer, more spread out
            for (int i = 0; i < initialBushCount; i++)
            {
                SpawnPlantAtRandomLocation(PlantSpecies.Bush, PlantGrowthStage.Mature);
            }
        }

        /// <summary>
        /// Spawn a plant at a random valid location
        /// </summary>
        public PlantOrganism SpawnPlantAtRandomLocation(PlantSpecies species, PlantGrowthStage startStage = PlantGrowthStage.Seed)
        {
            if (allPlants.Count >= globalMaxPlants) return null;

            Vector2 randomCircle = Random.insideUnitCircle * worldRadius;
            Vector3 spawnPos = new Vector3(randomCircle.x, 0.5f, randomCircle.y);
            
            // Validate position
            if (!IsValidPlantLocation(spawnPos, species))
            {
                return null;
            }

            return SpawnPlant(species, spawnPos, startStage);
        }

        /// <summary>
        /// Spawn a plant at specific position
        /// </summary>
        public PlantOrganism SpawnPlant(PlantSpecies species, Vector3 position, PlantGrowthStage startStage = PlantGrowthStage.Seed)
        {
            if (!CanSpawnSeedInArea(position, species)) return null;

            GameObject prefab = GetPrefabForSpecies(species);
            if (prefab == null)
            {
                // Create ad-hoc if no prefab
                GameObject plantObj = new GameObject($"Plant_{species}");
                plantObj.transform.position = position;
                
                PlantOrganism plant = plantObj.AddComponent<PlantOrganism>();
                plant.SetSpecies(species);
                
                RegisterPlant(plant);
                return plant;
            }

            GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
            PlantOrganism newPlant = instance.GetComponent<PlantOrganism>();
            
            if (newPlant != null)
            {
                newPlant.SetSpecies(species);
                
                // Force to specific stage if needed
                if (startStage != PlantGrowthStage.Seed)
                {
                    // Plant will progress naturally, but we could force it here
                }
                
                RegisterPlant(newPlant);
            }

            return newPlant;
        }

        /// <summary>
        /// Register a plant with the manager
        /// </summary>
        public void RegisterPlant(PlantOrganism plant)
        {
            if (plant == null || allPlants.Contains(plant)) return;

            allPlants.Add(plant);
            
            if (plantsBySpecies.ContainsKey(plant.Species))
            {
                plantsBySpecies[plant.Species].Add(plant);
            }

            // Subscribe to events
            plant.OnPlantDeath += OnPlantDied;
            
            // Update region data
            UpdatePlantRegion(plant);
        }

        /// <summary>
        /// Unregister a plant
        /// </summary>
        public void UnregisterPlant(PlantOrganism plant)
        {
            if (plant == null) return;

            plant.OnPlantDeath -= OnPlantDied;
            allPlants.Remove(plant);
            
            if (plantsBySpecies.ContainsKey(plant.Species))
            {
                plantsBySpecies[plant.Species].Remove(plant);
            }
        }

        /// <summary>
        /// Check if a seed can grow in this area (carrying capacity)
        /// </summary>
        public bool CanSpawnSeedInArea(Vector3 position, PlantSpecies species)
        {
            SpeciesSettings settings = GetSettingsForSpecies(species);
            
            // Global limit
            if (allPlants.Count >= globalMaxPlants) return false;
            
            // Species limit
            if (plantsBySpecies.ContainsKey(species) && 
                plantsBySpecies[species].Count >= settings.maxPopulation)
            {
                return false;
            }

            // Local density check
            Collider[] nearby = Physics.OverlapSphere(position, settings.carryingCapacityRadius);
            int plantCount = 0;
            
            foreach (var hit in nearby)
            {
                if (hit.GetComponent<PlantOrganism>() != null)
                {
                    plantCount++;
                    if (plantCount >= settings.maxPlantsInRadius)
                    {
                        return false; // Too crowded
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if location is valid for planting
        /// </summary>
        public bool IsValidPlantLocation(Vector3 position, PlantSpecies species)
        {
            // Check carrying capacity
            if (!CanSpawnSeedInArea(position, species)) return false;

            // Check for obstacles
            if (Physics.CheckSphere(position, 0.5f, LayerMask.GetMask("Obstacle")))
            {
                return false;
            }

            // Check for valid ground
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f))
            {
                // Make sure we hit ground/terrain
                return hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                       hit.collider.CompareTag("Terrain");
            }

            return false;
        }

        /// <summary>
        /// Get nearest food source for creatures
        /// </summary>
        public PlantOrganism GetNearestEdiblePlant(Vector3 position, float maxDistance = float.MaxValue)
        {
            PlantOrganism nearest = null;
            float minDist = maxDistance;

            foreach (var plant in allPlants)
            {
                if (plant == null || !plant.IsAlive || plant.CurrentStage == PlantGrowthStage.Seed) continue;

                float dist = Vector3.Distance(position, plant.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = plant;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Get total nutrition available in ecosystem
        /// </summary>
        public float GetTotalNutritionAvailable()
        {
            float total = 0f;
            foreach (var plant in allPlants)
            {
                if (plant != null && plant.IsAlive && plant.IsMature)
                {
                    total += plant.NutritionValue;
                }
            }
            return total;
        }

        /// <summary>
        /// Get population count for species
        /// </summary>
        public int GetPopulationCount(PlantSpecies species)
        {
            if (plantsBySpecies.ContainsKey(species))
            {
                return plantsBySpecies[species].Count;
            }
            return 0;
        }

        /// <summary>
        /// Called when a plant dies
        /// </summary>
        private void OnPlantDied(PlantOrganism plant)
        {
            UnregisterPlant(plant);
        }

        /// <summary>
        /// Cleanup destroyed/null plants
        /// </summary>
        private void CleanupDeadPlants()
        {
            allPlants.RemoveAll(p => p == null);
            
            foreach (var species in plantsBySpecies.Keys)
            {
                plantsBySpecies[species].RemoveAll(p => p == null);
            }
        }

        /// <summary>
        /// Update region grid data
        /// </summary>
        private void UpdateRegionData()
        {
            regionGrid.Clear();
            
            foreach (var plant in allPlants)
            {
                if (plant == null) continue;
                UpdatePlantRegion(plant);
            }
        }

        /// <summary>
        /// Update which region a plant belongs to
        /// </summary>
        private void UpdatePlantRegion(PlantOrganism plant)
        {
            Vector3 pos = plant.transform.position;
            Vector2Int regionCoord = new Vector2Int(
                Mathf.FloorToInt(pos.x / regionSize),
                Mathf.FloorToInt(pos.z / regionSize)
            );

            if (!regionGrid.ContainsKey(regionCoord))
            {
                regionGrid[regionCoord] = new RegionData();
            }
            
            regionGrid[regionCoord].AddPlant(plant);
        }

        /// <summary>
        /// Get settings for species
        /// </summary>
        private SpeciesSettings GetSettingsForSpecies(PlantSpecies species)
        {
            foreach (var setting in speciesSettings)
            {
                if (setting.species == species) return setting;
            }
            return speciesSettings[0]; // Default to first
        }

        /// <summary>
        /// Get prefab for species
        /// </summary>
        private GameObject GetPrefabForSpecies(PlantSpecies species)
        {
            return species switch
            {
                PlantSpecies.Carrot => carrotPlantPrefab,
                PlantSpecies.Bush => bushPlantPrefab,
                _ => null
            };
        }

        /// <summary>
        /// Get seed prefab
        /// </summary>
        public GameObject GetSeedPrefab()
        {
            return seedPrefab;
        }

        /// <summary>
        /// Debug info about ecosystem
        /// </summary>
        public string GetEcosystemReport()
        {
            int carrots = GetPopulationCount(PlantSpecies.Carrot);
            int bushes = GetPopulationCount(PlantSpecies.Bush);
            float nutrition = GetTotalNutritionAvailable();
            
            return $"Plants: {TotalPlantCount}/{globalMaxPlants} | " +
                   $"Carrots: {carrots} | Bushes: {bushes} | " +
                   $"Nutrition: {nutrition:F1}";
        }

        void OnDrawGizmosSelected()
        {
            // Draw ecosystem bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(worldRadius * 2, 2, worldRadius * 2));

            // Draw plant positions
            Gizmos.color = Color.yellow;
            foreach (var plant in allPlants)
            {
                if (plant != null)
                {
                    Gizmos.DrawWireSphere(plant.transform.position, 0.5f);
                }
            }
        }

        /// <summary>
        /// Region data for spatial partitioning
        /// </summary>
        private class RegionData
        {
            public List<PlantOrganism> Plants { get; private set; } = new List<PlantOrganism>();
            
            public void AddPlant(PlantOrganism plant)
            {
                if (!Plants.Contains(plant))
                {
                    Plants.Add(plant);
                }
            }
        }
    }
}
