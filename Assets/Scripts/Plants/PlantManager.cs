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

        private List<PlantOrganism> allPlants = new List<PlantOrganism>();
        private Dictionary<PlantSpecies, List<PlantOrganism>> plantsBySpecies = new Dictionary<PlantSpecies, List<PlantOrganism>>();
        private Dictionary<Vector2Int, RegionData> regionGrid = new Dictionary<Vector2Int, RegionData>();
        private float regionSize = 10f;

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
            if (Time.frameCount % 60 == 0)
            {
                CleanupDeadPlants();
                UpdateRegionData();
            }
        }

        private void InitializeDictionaries()
        {
            plantsBySpecies[PlantSpecies.Carrot] = new List<PlantOrganism>();
            plantsBySpecies[PlantSpecies.Bush] = new List<PlantOrganism>();
        }

        private void SpawnInitialPlants()
        {
            for (int i = 0; i < initialCarrotCount; i++)
            {
                SpawnPlantAtRandomLocation(PlantSpecies.Carrot, PlantGrowthStage.Mature);
            }

            for (int i = 0; i < initialBushCount; i++)
            {
                SpawnPlantAtRandomLocation(PlantSpecies.Bush, PlantGrowthStage.Mature);
            }
        }

        public PlantOrganism SpawnPlantAtRandomLocation(PlantSpecies species, PlantGrowthStage startStage = PlantGrowthStage.Seed)
        {
            if (allPlants.Count >= globalMaxPlants) return null;

            Vector2 randomCircle = Random.insideUnitCircle * worldRadius;
            Vector3 spawnPos = new Vector3(randomCircle.x, 0.5f, randomCircle.y);
            
            if (!IsValidPlantLocation(spawnPos, species))
            {
                return null;
            }

            return SpawnPlant(species, spawnPos, startStage);
        }

        public PlantOrganism SpawnPlant(PlantSpecies species, Vector3 position, PlantGrowthStage startStage = PlantGrowthStage.Seed)
        {
            if (!CanSpawnSeedInArea(position, species)) return null;

            GameObject prefab = GetPrefabForSpecies(species);
            if (prefab == null)
            {
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
                RegisterPlant(newPlant);
            }

            return newPlant;
        }

        public void RegisterPlant(PlantOrganism plant)
        {
            if (plant == null || allPlants.Contains(plant)) return;

            allPlants.Add(plant);
            
            if (plantsBySpecies.ContainsKey(plant.Species))
            {
                plantsBySpecies[plant.Species].Add(plant);
            }

            plant.OnPlantDeath += OnPlantDied;
            UpdatePlantRegion(plant);
        }

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

        public bool CanSpawnSeedInArea(Vector3 position, PlantSpecies species)
        {
            SpeciesSettings settings = GetSettingsForSpecies(species);
            
            if (allPlants.Count >= globalMaxPlants) return false;
            
            if (plantsBySpecies.ContainsKey(species) && 
                plantsBySpecies[species].Count >= settings.maxPopulation)
            {
                return false;
            }

            Collider[] nearby = Physics.OverlapSphere(position, settings.carryingCapacityRadius);
            int plantCount = 0;
            
            foreach (var hit in nearby)
            {
                if (hit.GetComponent<PlantOrganism>() != null)
                {
                    plantCount++;
                    if (plantCount >= settings.maxPlantsInRadius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsValidPlantLocation(Vector3 position, PlantSpecies species)
        {
            if (!CanSpawnSeedInArea(position, species)) return false;

            if (Physics.CheckSphere(position, 0.5f, LayerMask.GetMask("Obstacle")))
            {
                return false;
            }

            if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f))
            {
                return hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                       hit.collider.CompareTag("Terrain");
            }

            return false;
        }

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

        public int GetPopulationCount(PlantSpecies species)
        {
            if (plantsBySpecies.ContainsKey(species))
            {
                return plantsBySpecies[species].Count;
            }
            return 0;
        }

        private void OnPlantDied(PlantOrganism plant)
        {
            UnregisterPlant(plant);
        }

        private void CleanupDeadPlants()
        {
            allPlants.RemoveAll(p => p == null);
            
            foreach (var species in plantsBySpecies.Keys)
            {
                plantsBySpecies[species].RemoveAll(p => p == null);
            }
        }

        private void UpdateRegionData()
        {
            regionGrid.Clear();
            
            foreach (var plant in allPlants)
            {
                if (plant == null) continue;
                UpdatePlantRegion(plant);
            }
        }

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

        private SpeciesSettings GetSettingsForSpecies(PlantSpecies species)
        {
            foreach (var setting in speciesSettings)
            {
                if (setting.species == species) return setting;
            }
            return speciesSettings[0];
        }

        private GameObject GetPrefabForSpecies(PlantSpecies species)
        {
            return species switch
            {
                PlantSpecies.Carrot => carrotPlantPrefab,
                PlantSpecies.Bush => bushPlantPrefab,
                _ => null
            };
        }

        public GameObject GetSeedPrefab()
        {
            return seedPrefab;
        }

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
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(worldRadius * 2, 2, worldRadius * 2));

            Gizmos.color = Color.yellow;
            foreach (var plant in allPlants)
            {
                if (plant != null)
                {
                    Gizmos.DrawWireSphere(plant.transform.position, 0.5f);
                }
            }
        }

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
