using UnityEngine;
using System.Collections.Generic;
using Albia.Core;
using Albia.Creatures;

namespace Albia.Ecology
{
    /// <summary>
    /// Manages food and plant populations.
    /// MVP: Simple food spawning
    /// Full: Nutrient cycles, carrying capacity, biome-aware spawning
    /// </summary>
    public class EcologyManager : MonoBehaviour
    {
        public static EcologyManager Instance { get; private set; }

        [Header("Food Settings")]
        [SerializeField] private GameObject foodPrefab;
        [SerializeField] private int maxFoodCount = 50;
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private float spawnRadius = 40f;

        [Header("Carrying Capacity (MVP: Simple)")]
        [SerializeField] private int maxCreatures = 20;

        private List<FoodSource> activeFood = new List<FoodSource>();
        private float spawnTimer = 0f;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            // Simple food respawn timer
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                TrySpawnFood();
            }
        }

        void Start()
        {
            // Initial spawn
            for (int i = 0; i < 20; i++)
            {
                SpawnFood();
            }
        }

        /// <summary>
        /// Spawn food at random valid position
        /// </summary>
        public void SpawnFood()
        {
            if (activeFood.Count >= maxFoodCount) return;
            if (foodPrefab == null) return;

            Vector3 spawnPos = GetRandomSpawnPosition();
            
            // Validate position (MVP: simple bounds check)
            if (IsValidSpawnPosition(spawnPos))
            {
                GameObject food = Instantiate(foodPrefab, spawnPos, Quaternion.identity, transform);
                FoodSource source = food.GetComponent<FoodSource>();
                if (source != null)
                {
                    activeFood.Add(source);
                }
            }
        }

        /// <summary>
        /// Try to spawn food if under capacity
        /// </summary>
        private void TrySpawnFood()
        {
            // Remove consumed/destroyed food from tracking
            activeFood.RemoveAll(f => f == null || !f.gameObject.activeSelf);
            
            if (activeFood.Count < maxFoodCount)
            {
                SpawnFood();
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            return new Vector3(randomCircle.x, 0.5f, randomCircle.y);
            
            // SCALES TO: Terrain height sampling
            // float height = TerrainManager.Instance.GetHeight(position);
            // return new Vector3(position.x, height, position.z);
        }

        private bool IsValidSpawnPosition(Vector3 pos)
        {
            // MVP: Basic bounds check
            // Full: NavMesh validation, height check, water check
            return true;
        }

        /// <summary>
        /// Check if creature population is at capacity
        /// </summary>
        public bool IsAtCreatureCapacity()
        {
            int currentCreatures = GameObject.FindObjectsOfType<Norn>().Length;
            return currentCreatures >= maxCreatures;
        }

        /// <summary>
        /// Get nearest food to position
        /// SCALES TO: Spatial query system
        /// </summary>
        public FoodSource GetNearestFood(Vector3 position)
        {
            FoodSource nearest = null;
            float minDist = float.MaxValue;

            foreach (var food in activeFood)
            {
                if (food == null || !food.gameObject.activeSelf) continue;
                
                float dist = Vector3.Distance(position, food.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = food;
                }
            }

            return nearest;
        }
    }
}