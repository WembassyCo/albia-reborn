using System;
using System.Collections.Generic;
using UnityEngine;
using Albia.Core;
using Albia.Lifecycle;

namespace Albia.Creatures
{
    /// <summary>
    /// Spawner for Grendels. Manages population limits and spawning conditions.
    /// </summary>    
    public class GrendelSpawner : MonoBehaviour
    {
        [Header("Population Control")]
        [Tooltip("Maximum number of Grendels allowed")]
        [Range(1, 10)]
        public int maxGrendels = 5;
        
        [Tooltip("Minimum number of Grendels")]
        [Range(0, 5)]
        public int minGrendels = 2;
        
        [Header("Spawn Conditions")]
        [Tooltip("Minimum Norn population before Grendels spawn")]
        [Range(1, 20)]
        public int nornPopulationThreshold = 5;
        
        [Tooltip("Check interval for spawn conditions (seconds)")]
        [Range(2f, 30f)]
        public float checkInterval = 5f;
        
        [Tooltip("Minimum time between spawns")]
        [Range(5f, 60f)]
        public float spawnCooldown = 15f;
        
        [Tooltip("Initial delay before first spawn check")]
        [Range(0f, 30f)]
        public float initialDelay = 5f;

        [Header("Spawn Position")]
        [Tooltip("Spawn at world edges")]
        public bool spawnAtEdges = true;
        
        [Tooltip("Minimum distance from other Grendels")]
        [Range(5f, 30f)]
        public float minDistanceFromOthers = 10f;
        
        [Tooltip("World bounds (if edges enabled)")]
        public Vector3 worldMin = new Vector3(-100, 0, -100);
        public Vector3 worldMax = new Vector3(100, 0, 100);
        
        [Tooltip("Y offset from ground")]
        public float spawnHeight = 1f;

        [Header("Species Configuration")]
        [Tooltip("Grendel species definition")]
        public GrendelSpecies speciesConfig;
        
        [Tooltip("Create species if none assigned")]
        public bool autoCreateSpecies = true;

        [Header("Debug")]
        [Tooltip("Log spawn events")]
        public bool logSpawns = true;

        // Runtime state
        private List<Grendel> activeGrendels = new List<Grendel>();
        private float lastSpawnTime = -999f;
        private float checkTimer = 0f;
        private bool hasSpawnedInitial = false;
        
        // Events
        public event Action<Grendel> OnGrendelSpawned;
        public event Action<Grendel> OnGrendelDied;
        public event Action<int> OnPopulationChanged;

        // Properties
        public int CurrentPopulation => activeGrendels.Count;
        public int MaxPopulation => maxGrendels;
        public bool CanSpawn => Time.time - lastSpawnTime >= spawnCooldown && activeGrendels.Count < maxGrendels;
        
        // Statistics
        public int TotalSpawned { get; private set; }
        public int TotalDied { get; private set; }

        private void Awake()
        {
            // Create default species config if not assigned
            if (speciesConfig == null && autoCreateSpecies)
            {
                speciesConfig = ScriptableObject.CreateInstance<GrendelSpecies>();
                speciesConfig.name = "DefaultGrendelSpecies";
            }
            
            // Initialize lists
            activeGrendels = new List<Grendel>();
        }

        private void Start()
        {
            checkTimer = initialDelay;
        }

        private void Update()
        {
            // Clean up dead Grendels
            CleanupDeadGrendels();
            
            // Check spawn conditions
            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                checkTimer = 0f;
                TrySpawn();
            }
        }

        /// <summary>
        /// Attempts to spawn a Grendel if conditions are met
        /// </summary>
        private void TrySpawn()
        {
            // Check cooldown
            if (Time.time - lastSpawnTime < spawnCooldown) return;
            
            // Check population cap
            if (activeGrendels.Count >= maxGrendels) return;
            
            // Check minimum population (always maintain minimum)
            bool maintainMinimum = activeGrendels.Count < minGrendels;
            
            if (!maintainMinimum)
            {
                // Check Norn population threshold
                int nornCount = GetNornPopulation();
                if (nornCount < nornPopulationThreshold) return;
                
                // Only spawn when Norn population is healthy
                float nornToGrendelRatio = nornCount / Mathf.Max(1, activeGrendels.Count);
                if (nornToGrendelRatio < 3f) return;
            }
            
            // Attempt to spawn
            SpawnGrendel();
        }
        
        /// <summary>
        /// Spawns a new Grendel at an appropriate location
        /// </summary>
        public Grendel SpawnGrendel()
        {
            if (speciesConfig == null)
            {
                Debug.LogError("[GrendelSpawner] No species config assigned!");
                return null;
            }
            
            Vector3 spawnPos = GetSpawnPosition();
            if (spawnPos == Vector3.zero) return null;
            
            // Create Grendel using species config
            var grendel = speciesConfig.CreateGrendel(spawnPos, Quaternion.identity);
            
            if (grendel == null)
            {
                Debug.LogError("[GrendelSpawner] Failed to create Grendel!");
                return null;
            }
            
            // Track the Grendel
            activeGrendels.Add(grendel);
            
            // Subscribe to death event
            grendel.OnDeath += () => OnGrendelDeath(grendel);
            
            // Update tracking
            lastSpawnTime = Time.time;
            TotalSpawned++;
            
            // Fire events
            OnGrendelSpawned?.Invoke(grendel);
            OnPopulationChanged?.Invoke(activeGrendels.Count);
            
            if (logSpawns)
            {
                Debug.Log($"[GrendelSpawner] Spawned Grendel at {spawnPos}. Population: {activeGrendels.Count}/{maxGrendels}");
            }
            
            return grendel;
        }
        
        /// <summary>
        /// Gets a valid spawn position
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            Vector3 spawnPos = Vector3.zero;
            int attempts = 0;
            const int maxAttempts = 20;
            
            while (attempts < maxAttempts)
            {
                attempts++;
                
                if (spawnAtEdges)
                {
                    // Spawn at random edge of world
                    spawnPos = GetEdgePosition();
                }
                else
                {
                    // Random position within bounds
                    spawnPos = new Vector3(
                        UnityEngine.Random.Range(worldMin.x, worldMax.x),
                        spawnHeight,
                        UnityEngine.Random.Range(worldMin.z, worldMax.z)
                    );
                }
                
                // Check distance from other Grendels
                bool tooClose = false;
                foreach (var other in activeGrendels)
                {
                    if (other == null) continue;
                    float dist = Vector3.Distance(spawnPos, other.transform.position);
                    if (dist < minDistanceFromOthers)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (!tooClose)
                {
                    spawnPos.y = spawnHeight;
                    return spawnPos;
                }
            }
            
            // Fallback
            return spawnAtEdges ? GetEdgePosition() : Vector3.zero;
        }
        
        /// <summary>
        /// Gets a random position at world edge
        /// </summary>
        private Vector3 GetEdgePosition()
        {
            int edge = UnityEngine.Random.Range(0, 4);
            float x, z;
            
            switch (edge)
            {
                case 0: // Left edge
                    x = worldMin.x;
                    z = UnityEngine.Random.Range(worldMin.z, worldMax.z);
                    break;
                case 1: // Right edge
                    x = worldMax.x;
                    z = UnityEngine.Random.Range(worldMin.z, worldMax.z);
                    break;
                case 2: // Bottom edge
                    x = UnityEngine.Random.Range(worldMin.x, worldMax.x);
                    z = worldMin.z;
                    break;
                case 3: // Top edge
                default:
                    x = UnityEngine.Random.Range(worldMin.x, worldMax.x);
                    z = worldMax.z;
                    break;
            }
            
            return new Vector3(x, spawnHeight, z);
        }
        
        /// <summary>
        /// Gets the current Norn population count
        /// </summary>
        private int GetNornPopulation()
        {
            // Use PopulationRegistry if available
            if (PopulationRegistry.Instance != null)
            {
                return PopulationRegistry.Instance.GetSpeciesCount("Norn");
            }
            
            // Fallback: count all Norns in scene
            var norns = FindObjectsOfType<Norn>();
            int count = 0;
            foreach (var norn in norns)
            {
                if (norn.IsAlive) count++;
            }
            return count;
        }
        
        /// <summary>
        /// Cleanup dead Grendels from tracking list
        /// </summary>
        private void CleanupDeadGrendels()
        {
            for (int i = activeGrendels.Count - 1; i >= 0; i--)
            {
                var grendel = activeGrendels[i];
                if (grendel == null || !grendel.IsAlive)
                {
                    if (grendel != null)
                    {
                        OnGrendelDeath(grendel);
                    }
                    activeGrendels.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Handles Grendel death
        /// </summary>
        private void OnGrendelDeath(Grendel grendel)
        {
            if (grendel != null)
            {
                TotalDied++;
                OnGrendelDied?.Invoke(grendel);
                OnPopulationChanged?.Invoke(activeGrendels.Count);
                
                if (logSpawns)
                {
                    Debug.Log($"[GrendelSpawner] Grendel died. Population: {activeGrendels.Count}/{maxGrendels}");
                }
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Force spawn a Grendel (ignores population limits)
        /// </summary>
        public Grendel ForceSpawn()
        {
            int oldMax = maxGrendels;
            maxGrendels = CurrentPopulation + 1;
            
            var grendel = SpawnGrendel();
            
            maxGrendels = oldMax;
            return grendel;
        }
        
        /// <summary>
        /// Get all active Grendels
        /// </summary>
        public IReadOnlyList<Grendel> GetActiveGrendels()
        {
            CleanupDeadGrendels();
            return activeGrendels;
        }
        
        /// <summary>
        /// Get nearest Grendel to a position
        /// </summary>
        public Grendel GetNearestGrendel(Vector3 position)
        {
            Grendel nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var grendel in activeGrendels)
            {
                if (grendel == null || !grendel.IsAlive) continue;
                
                float dist = Vector3.Distance(position, grendel.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = grendel;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Kill all Grendels
        /// </summary>
        public void KillAll()
        {
            foreach (var grendel in activeGrendels)
            {
                if (grendel != null && grendel.IsAlive)
                {
                    var dieMethod = grendel.GetType().GetMethod("Die",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    if (dieMethod != null)
                    {
                        dieMethod.Invoke(grendel, null);
                    }
                }
            }
            
            activeGrendels.Clear();
            OnPopulationChanged?.Invoke(0);
        }
        
        /// <summary>
        /// Set max population
        /// </summary>
        public void SetMaxPopulation(int max)
        {
            maxGrendels = Mathf.Max(1, max);
        }
        
        /// <summary>
        /// Get spawner statistics
        /// </summary>
        public GrendelSpawnerStats GetStats()
        {
            CleanupDeadGrendels();
            return new GrendelSpawnerStats
            {
                CurrentPopulation = activeGrendels.Count,
                MaxPopulation = maxGrendels,
                TotalSpawned = TotalSpawned,
                TotalDied = TotalDied,
                NornPopulation = GetNornPopulation(),
                CanSpawn = CanSpawn
            };
        }
        
        #endregion
        
        private void OnDrawGizmosSelected()
        {
            // Draw world bounds
            Gizmos.color = Color.red;
            
            Vector3 p1 = new Vector3(worldMin.x, spawnHeight, worldMin.z);
            Vector3 p2 = new Vector3(worldMax.x, spawnHeight, worldMin.z);
            Vector3 p3 = new Vector3(worldMax.x, spawnHeight, worldMax.z);
            Vector3 p4 = new Vector3(worldMin.x, spawnHeight, worldMax.z);
            
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
            
            // Draw active Grendels
            Gizmos.color = Color.green;
            foreach (var grendel in activeGrendels)
            {
                if (grendel != null)
                {
                    Gizmos.DrawWireSphere(grendel.transform.position, 1f);
                }
            }
        }
    }
    
    /// <summary>
    /// Statistics for Grendel spawner
    /// </summary>
    public struct GrendelSpawnerStats
    {
        public int CurrentPopulation;
        public int MaxPopulation;
        public int TotalSpawned;
        public int TotalDied;
        public int NornPopulation;
        public bool CanSpawn;
        
        public override string ToString()
        {
            return $"Grendels: {CurrentPopulation}/{MaxPopulation} (Spawned: {TotalSpawned}, Died: {TotalDied})";
        }
    }
}