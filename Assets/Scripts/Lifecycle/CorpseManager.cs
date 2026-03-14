using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Albia.Lifecycle
{
    /// <summary>
    /// Global manager for all corpses in the world.
    /// Handles cleanup timers, corpse counting, and optimization.
    /// </summary>
    public class CorpseManager : MonoBehaviour
    {
        public static CorpseManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private CorpseConfig defaultCorpseConfig;
        [SerializeField] private GameObject corpsePrefab;
        
        [Header("Limits")]
        [Tooltip("Maximum number of corpses allowed in the world")]
        [SerializeField] private int maxCorpses = 50;
        
        [Tooltip("Maximum corpses per species")]
        [SerializeField] private int maxCorpsesPerSpecies = 20;
        
        [Header("Cleanup")]
        [Tooltip("Enable automatic cleanup of excess corpses")]
        [SerializeField] private bool autoCleanupExcess = true;
        
        [Tooltip("Cleanup check interval in seconds")]
        [SerializeField] private float cleanupCheckInterval = 10f;
        
        [Tooltip("Priority cleanup threshold - corpses older than this get cleaned first")]
        [SerializeField] private float priorityCleanupAge = 60f; // 1 minute

        [Header("Performance")]
        [Tooltip("Update only visible corpses")]
        [SerializeField] private bool cullingEnabled = true;
        
        [Tooltip("Distance for culling corpses")]
        [SerializeField] private float cullDistance = 100f;

        // Runtime data
        private readonly Dictionary<Guid, Corpse> activeCorpses = new();
        private readonly Dictionary<string, List<Guid>> corpsesBySpecies = new();
        private readonly PriorityQueue<Guid, float> cleanupQueue = new();
        private Transform playerTransform;
        private float lastCleanupCheck;
        private int totalCorpsesCreated;
        private int totalCorpsesCleaned;

        // Events
        public event EventHandler<Corpse> OnCorpseCreated;
        public event EventHandler<Corpse> OnCorpseRemoved;
        public event EventHandler OnCorpseLimitReached;

        // Properties
        public int ActiveCorpseCount => activeCorpses.Count;
        public int MaxCorpses => maxCorpses;
        public int TotalCorpsesCreated => totalCorpsesCreated;
        public int TotalCorpsesCleaned => totalCorpsesCleaned;
        public IReadOnlyDictionary<Guid, Corpse> ActiveCorpses => activeCorpses;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Try to find player/camera for culling
            playerTransform = Camera.main?.transform;
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }
        }

        private void Update()
        {
            // Periodic cleanup checks
            if (Time.time - lastCleanupCheck >= cleanupCheckInterval)
            {
                CheckCleanup();
                lastCleanupCheck = Time.time;
            }
            
            // Update culling
            if (cullingEnabled)
                UpdateCulling();
        }

        #region Corpse Management

        /// <summary>
        /// Registers a corpse with the manager
        /// </summary>
        public void RegisterCorpse(Corpse corpse)
        {
            if (corpse == null) return;
            
            var id = corpse.OriginalOrganismId;
            if (id == Guid.Empty)
            {
                id = Guid.NewGuid();
                // Cannot modify corpse ID after creation, so we use the corpse reference
            }
            
            // Check limits before adding
            if (activeCorpses.Count >= maxCorpses)
            {
                if (autoCleanupExcess)
                {
                    CleanupOldestCorpse();
                }
                else
                {
                    OnCorpseLimitReached?.Invoke(this, EventArgs.Empty);
                    corpse.ForceCleanup();
                    return;
                }
            }
            
            // Check species limit
            string species = corpse.SpeciesName ?? "Unknown";
            if (corpsesBySpecies.TryGetValue(species, out var speciesList) && 
                speciesList.Count >= maxCorpsesPerSpecies)
            {
                CleanupOldestCorpseOfSpecies(species);
            }
            
            // Register
            activeCorpses[corpse.GetInstanceID()] = corpse; // Use Unity instance ID as key
            
            if (!corpsesBySpecies.ContainsKey(species))
                corpsesBySpecies[species] = new List<Guid>();
            corpsesBySpecies[species].Add(corpse.GetInstanceID());
            
            // Add to cleanup queue with priority based on age
            cleanupQueue.Enqueue(corpse.GetInstanceID(), Time.time);
            
            totalCorpsesCreated++;
            
            OnCorpseCreated?.Invoke(this, corpse);
        }

        /// <summary>
        /// Unregisters a corpse from the manager
        /// </summary>
        public void UnregisterCorpse(Corpse corpse)
        {
            if (corpse == null) return;
            
            int id = corpse.GetInstanceID();
            if (activeCorpses.Remove(id))
            {
                string species = corpse.SpeciesName ?? "Unknown";
                if (corpsesBySpecies.TryGetValue(species, out var speciesList))
                {
                    speciesList.Remove(id);
                    if (speciesList.Count == 0)
                        corpsesBySpecies.Remove(species);
                }
                
                totalCorpsesCleaned++;
                OnCorpseRemoved?.Invoke(this, corpse);
            }
        }

        /// <summary>
        /// Creates a new corpse at the specified position
        /// </summary>
        public Corpse CreateCorpse(Vector3 position, Quaternion rotation, 
            Guid organismId, string species, float age, GenomeData genome)
        {
            if (corpsePrefab == null)
            {
                Debug.LogError("[CorpseManager] Corpse prefab is not assigned!");
                return null;
            }
            
            // Check if we need to cleanup first
            if (activeCorpses.Count >= maxCorpses && autoCleanupExcess)
            {
                CleanupOldestCorpse();
            }
            
            // Instantiate corpse
            GameObject corpseObj = Instantiate(corpsePrefab, position, rotation);
            
            // Initialize corpse component
            Corpse corpse = corpseObj.GetComponent<Corpse>();
            if (corpse == null)
                corpse = corpseObj.AddComponent<Corpse>();
            
            corpse.Initialize(organismId, species, age, genome, defaultCorpseConfig);
            
            return corpse;
        }

        /// <summary>
        /// Creates a simple Corpse without requiring a prefab (uses default capsule)
        /// </summary>
        public Corpse CreateSimpleCorpse(Vector3 position, Guid organismId, 
            string species, float age, GenomeData genome)
        {
            // Create basic corpse object
            GameObject corpseObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpseObj.name = $"Corpse_{species}_{organismId.ToString().Substring(0, 8)}";
            corpseObj.transform.position = position;
            corpseObj.transform.rotation = Quaternion.Euler(90, 0, 0); // Lie flat
            corpseObj.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
            
            // Remove default collider (Corpse.cs adds its own)
            Destroy(corpseObj.GetComponent<Collider>());
            
            // Add Corpse component
            Corpse corpse = corpseObj.AddComponent<Corpse>();
            corpse.Initialize(organismId, species, age, genome, defaultCorpseConfig);
            
            return corpse;
        }

        /// <summary>
        /// Removes a specific corpse
        /// </summary>
        public void RemoveCorpse(Guid corpseId)
        {
            // Find corpse by original organism ID
            var corpse = activeCorpses.Values.FirstOrDefault(c => c.OriginalOrganismId == corpseId);
            if (corpse != null)
            {
                corpse.ForceCleanup();
            }
        }

        /// <summary>
        /// Gets the nearest corpse to a position
        /// </summary>
        public Corpse GetNearestCorpse(Vector3 position, float maxDistance = float.MaxValue)
        {
            Corpse nearest = null;
            float nearestDist = maxDistance;
            
            foreach (var corpse in activeCorpses.Values)
            {
                if (corpse == null) continue;
                if (!corpse.CanBeEaten) continue;
                
                float dist = Vector3.Distance(position, corpse.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = corpse;
                }
            }
            
            return nearest;
        }

        /// <summary>
        /// Gets all corpses within range
        /// </summary>
        public List<Corpse> GetCorpsesInRange(Vector3 position, float range)
        {
            return activeCorpses.Values
                .Where(c => Vector3.Distance(position, c.transform.position) <= range)
                .ToList();
        }

        /// <summary>
        /// Gets all corpses of a specific species
        /// </summary>
        public List<Corpse> GetCorpsesBySpecies(string species)
        {
            if (!corpsesBySpecies.TryGetValue(species, out var ids))
                return new List<Corpse>();
            
            return ids
                .Select(id => activeCorpses.GetValueOrDefault(id))
                .Where(c => c != null)
                .ToList();
        }

        /// <summary>
        /// Forces cleanup of all corpses
        /// </summary>
        public void CleanupAllCorpses()
        {
            var corpsesToRemove = activeCorpses.Values.ToList();
            foreach (var corpse in corpsesToRemove)
            {
                if (corpse != null)
                    corpse.ForceCleanup();
            }
            
            activeCorpses.Clear();
            corpsesBySpecies.Clear();
            cleanupQueue.Clear();
        }

        /// <summary>
        /// Gets statistics about corpses
        /// </summary>
        public CorpseStats GetStatistics()
        {
            var stats = new CorpseStats
            {
                TotalActive = activeCorpses.Count,
                TotalCreated = totalCorpsesCreated,
                TotalCleaned = totalCorpsesCleaned,
                BySpecies = new Dictionary<string, int>()
            };
            
            foreach (var kvp in corpsesBySpecies)
            {
                stats.BySpecies[kvp.Key] = kvp.Value.Count;
            }
            
            // Calculate average decay
            if (activeCorpses.Count > 0)
            {
                stats.AverageDecay = activeCorpses.Values.Average(c => c.CurrentDecay);
            }
            
            return stats;
        }

        #endregion

        #region Cleanup Methods

        private void CheckCleanup()
        {
            if (!autoCleanupExcess) return;
            
            // Priority cleanup - old corpses first
            while (activeCorpses.Count > maxCorpses * 0.9f && cleanupQueue.Count > 0)
            {
                if (cleanupQueue.TryDequeue(out var corpseId, out var enqueueTime))
                {
                    // Check if still exists and is old
                    if (activeCorpses.TryGetValue(corpseId, out var corpse))
                    {
                        float age = corpse.TimeSinceDeath;
                        if (age > priorityCleanupAge)
                        {
                            corpse.ForceCleanup();
                        }
                        else
                        {
                            // Requeue for later
                            cleanupQueue.Enqueue(corpseId, enqueueTime);
                            break;
                        }
                    }
                }
            }
        }

        private void CleanupOldestCorpse()
        {
            Corpse oldest = null;
            float oldestAge = 0;
            
            foreach (var corpse in activeCorpses.Values)
            {
                if (corpse.TimeSinceDeath > oldestAge)
                {
                    oldestAge = corpse.TimeSinceDeath;
                    oldest = corpse;
                }
            }
            
            oldest?.ForceCleanup();
        }

        private void CleanupOldestCorpseOfSpecies(string species)
        {
            if (!corpsesBySpecies.TryGetValue(species, out var ids)) return;
            
            Corpse oldest = null;
            float oldestAge = 0;
            
            foreach (var id in ids)
            {
                if (activeCorpses.TryGetValue(id, out var corpse))
                {
                    if (corpse.TimeSinceDeath > oldestAge)
                    {
                        oldestAge = corpse.TimeSinceDeath;
                        oldest = corpse;
                    }
                }
            }
            
            oldest?.ForceCleanup();
        }

        private void UpdateCulling()
        {
            if (playerTransform == null) return;
            
            Vector3 playerPos = playerTransform.position;
            
            foreach (var corpse in activeCorpses.Values)
            {
                if (corpse == null) continue;
                
                float distance = Vector3.Distance(playerPos, corpse.transform.position);
                bool shouldBeVisible = distance <= cullDistance;
                
                if (corpse.gameObject.activeSelf != shouldBeVisible)
                {
                    corpse.gameObject.SetActive(shouldBeVisible);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Statistics for corpse tracking
    /// </summary>
    public struct CorpseStats
    {
        public int TotalActive;
        public int TotalCreated;
        public int TotalCleaned;
        public float AverageDecay;
        public Dictionary<string, int> BySpecies;
        
        public override string ToString()
        {
            return $"Corpses: {TotalActive} active, {TotalCreated} created, {TotalCleaned} cleaned";
        }
    }

    /// <summary>
    /// Simple priority queue implementation for cleanup ordering
    /// </summary>
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private readonly List<(TElement Element, TPriority Priority)> elements = new();

        public int Count => elements.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            elements.Add((element, priority));
            elements.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (elements.Count == 0)
            {
                element = default;
                priority = default;
                return false;
            }

            element = elements[0].Element;
            priority = elements[0].Priority;
            elements.RemoveAt(0);
            return true;
        }

        public void Clear()
        {
            elements.Clear();
        }
    }
}
