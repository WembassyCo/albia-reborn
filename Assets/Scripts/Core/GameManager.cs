using UnityEngine;
using UnityEngine.SceneManagement;

namespace Albia.Core
{
    /// <summary>
    /// GameManager singleton - boots the simulation.
    /// MVP: Basic initialization
    /// Full: World generation, ecology seeding, historical sim
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("World Settings")]
        [SerializeField] private int worldSeed = 0;
        [SerializeField] private bool randomizeSeed = true;
        
        [Header("Spawning")]
        [SerializeField] private GameObject nornPrefab;
        [SerializeField] private GameObject foodPrefab;
        [SerializeField] private Transform spawnParent;
        
        [Header("MVP Test Settings")]
        [SerializeField] private int initialNornCount = 3;
        [SerializeField] private int initialFoodCount = 10;
        [SerializeField] private float foodRespawnInterval = 30f;

        // References
        public TimeManager TimeManager { get; private set; }
        // SCALES TO: WorldGenerator, TerrainManager, EcologySystem, PopulationRegistry

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Seed setup
            if (randomizeSeed)
                worldSeed = Random.Range(int.MinValue, int.MaxValue);
            Random.InitState(worldSeed);
            
            Debug.Log($"[GameManager] World seeded: {worldSeed}");
        }

        private void Start()
        {
            // Bootstrap systems
            BootstrapSystems();
            
            // Spawn initial world state
            SpawnInitialPopulation();
            SpawnFood();

            // Start food respawn
            InvokeRepeating(nameof(SpawnFood), foodRespawnInterval, foodRespawnInterval);
        }

        private void BootstrapSystems()
        {
            // Time manager
            TimeManager = FindFirstObjectByType<TimeManager>();
            if (TimeManager == null)
            {
                var timeGO = new GameObject("TimeManager");
                TimeManager = timeGO.AddComponent<TimeManager>();
            }

            // SCALES TO:
            // - WorldGeneration (terrain)
            // - ClimateSystem (temperature, moisture)
            // - WeatherSystem (rain, sun)
            // - EcologySystem (plants, insects, fauna)
            // - PopulationRegistry (tracking all organisms)
            // - EventBus (decoupled system communication)
        }

        private void SpawnInitialPopulation()
        {
            if (nornPrefab == null)
            {
                Debug.LogError("[GameManager] Norn prefab not assigned!");
                return;
            }

            for (int i = 0; i < initialNornCount; i++)
            {
                Vector3 spawnPos = GetRandomWalkablePosition();
                var norn = Instantiate(nornPrefab, spawnPos, Quaternion.identity, spawnParent);
                norn.name = $"Norn_{i + 1}";
                
                // SCALES TO: PopulationRegistry.Register(norn)
            }
        }

        private void SpawnFood()
        {
            if (foodPrefab == null) return;

            // Count existing food
            int currentFood = GameObject.FindGameObjectsWithTag("Food").Length;
            int toSpawn = initialFoodCount - currentFood;

            for (int i = 0; i < toSpawn; i++)
            {
                Vector3 spawnPos = GetRandomWalkablePosition();
                var food = Instantiate(foodPrefab, spawnPos, Quaternion.identity, spawnParent);
                
                // SCALES TO: Biome-aware food spawning based on terrain
            }
        }

        private Vector3 GetRandomWalkablePosition()
        {
            // MVP: Random position on flat plane
            // Full: NavMesh.SamplePosition on terrain
            
            Vector3 randomPos = new Vector3(
                Random.Range(-50f, 50f),
                0.5f,
                Random.Range(-50f, 50f)
            );
            
            // SCALES TO: NavMesh.SamplePosition validation
            return randomPos;
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // SCALES TO: Save/Load
        // public void SaveWorld(string filename) { }
        // public void LoadWorld(string filename) { }
    }
}