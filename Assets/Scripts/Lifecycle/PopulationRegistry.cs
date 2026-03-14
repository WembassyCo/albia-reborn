using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Albia.Lifecycle
{
    /// <summary>
    /// Represents the current state of a living organism in the population
    /// </summary>
    [Serializable]
    public struct OrganismRecord
    {
        public Guid Id;
        public string Species;
        public string Name;
        public float Age;
        public float Health;
        public float Energy;
        public Vector3 Position;
        public bool IsAlive;
        public bool IsPregnant;
        public DateTime BirthTime;
        public DateTime LastUpdateTime;
        public int Generation;
        public Guid? ParentA;
        public Guid? ParentB;
        
        public override string ToString()
        {
            return $"{Name} ({Species}) - Age: {Age:F1}, Health: {Health:P0}, Energy: {Energy:P0}";
        }
    }

    /// <summary>
    /// Event arguments for organism-related events
    /// </summary>
    public class OrganismEventArgs : EventArgs
    {
        public OrganismRecord Organism { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for population change events
    /// </summary>
    public class PopulationChangeEventArgs : EventArgs
    {
        public int PreviousCount { get; set; }
        public int CurrentCount { get; set; }
        public int Change { get; set; }
        public string Species { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Query options for filtering organisms
    /// </summary>
    public struct OrganismQueryOptions
    {
        public string SpeciesFilter;
        public float? MinHealth;
        public float? MaxHealth;
        public float? MinAge;
        public float? MaxAge;
        public bool? IsAliveOnly;
        public bool? IsPregnant;
        public Vector3? NearPosition;
        public float? MaxDistance;
        public int? Generation;
        public int? Limit;
        public Func<OrganismRecord, bool> CustomFilter;
    }

    /// <summary>
    /// Interface for organisms that can be tracked by the population registry
    /// </summary>
    public interface ITrackedOrganism
    {
        Guid Id { get; }
        string Species { get; }
        string Name { get; }
        float Age { get; }
        float Health { get; }
        float Energy { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        bool IsPregnant { get; }
        DateTime BirthTime { get; }
        int Generation { get; }
        Guid? ParentA { get; }
        Guid? ParentB { get; }
        
        event Action OnDeath;
    }

    /// <summary>
    /// Central registry that tracks all living organisms in the world.
    /// Provides query methods and events for population changes.
    /// </summary>
    public class PopulationRegistry : MonoBehaviour
    {
        public static PopulationRegistry Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool autoCleanupCorpses = true;
        [SerializeField] private float updateInterval = 1f;
        
        [Header("Events")]
        public bool logPopulationChanges = true;

        // Core data structures
        private readonly Dictionary<Guid, OrganismRecord> organisms = new();
        private readonly Dictionary<Guid, ITrackedOrganism> organismReferences = new();
        private readonly Dictionary<string, HashSet<Guid>> speciesIndex = new();
        private readonly HashSet<Guid> deadOrganisms = new();
        private readonly Queue<Guid> organismsToCleanup = new();

        // Event tracking
        private int lastPopulationCount;
        private float lastUpdateTime;

        // Events
        public event EventHandler<OrganismEventArgs> OnOrganismRegistered;
        public event EventHandler<OrganismEventArgs> OnOrganismDied;
        public event EventHandler<OrganismEventArgs> OnOrganismRemoved;
        public event EventHandler<PopulationChangeEventArgs> OnPopulationChanged;
        public event EventHandler OnPopulationEmpty;
        public event EventHandler<OrganismEventArgs> OnOrganismUpdated;

        // Statistics
        public int TotalBirths { get; private set; }
        public int TotalDeaths { get; private set; }
        public DateTime SessionStartTime { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            SessionStartTime = DateTime.UtcNow;
            lastPopulationCount = 0;
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateRecords();
                lastUpdateTime = Time.time;
            }
            
            if (autoCleanupCorpses)
            {
                ProcessCorpseCleanup();
            }
        }

        #region Registration

        /// <summary>
        /// Registers a new organism in the population registry
        /// </summary>
        public bool RegisterOrganism(ITrackedOrganism organism)
        {
            if (organism == null) return false;
            if (organisms.ContainsKey(organism.Id)) return false;
            
            var record = new OrganismRecord
            {
                Id = organism.Id,
                Species = organism.Species,
                Name = organism.Name,
                Age = organism.Age,
                Health = organism.Health,
                Energy = organism.Energy,
                Position = organism.Position,
                IsAlive = organism.IsAlive,
                IsPregnant = organism.IsPregnant,
                BirthTime = organism.BirthTime,
                LastUpdateTime = DateTime.UtcNow,
                Generation = organism.Generation,
                ParentA = organism.ParentA,
                ParentB = organism.ParentB
            };
            
            organisms[organism.Id] = record;
            organismReferences[organism.Id] = organism;
            
            // Add to species index
            if (!speciesIndex.ContainsKey(organism.Species))
                speciesIndex[organism.Species] = new HashSet<Guid>();
            speciesIndex[organism.Species].Add(organism.Id);
            
            // Subscribe to death event
            organism.OnDeath += () => OnOrganismDeath(organism.Id);
            
            TotalBirths++;
            
            var args = new OrganismEventArgs
            {
                Organism = record,
                Timestamp = DateTime.UtcNow
            };
            OnOrganismRegistered?.Invoke(this, args);
            
            CheckPopulationChange(1, organism.Species);
            
            if (logPopulationChanges)
                Debug.Log($"[PopulationRegistry] Registered: {record}");
            
            return true;
        }

        /// <summary>
        /// Registers a new organism with complete record data
        /// </summary>
        public bool RegisterOrganism(OrganismRecord record, ITrackedOrganism reference = null)
        {
            if (organisms.ContainsKey(record.Id)) return false;
            
            record.LastUpdateTime = DateTime.UtcNow;
            organisms[record.Id] = record;
            
            if (reference != null)
                organismReferences[record.Id] = reference;
            
            if (!speciesIndex.ContainsKey(record.Species))
                speciesIndex[record.Species] = new HashSet<Guid>();
            speciesIndex[record.Species].Add(record.Id);
            
            TotalBirths++;
            
            var args = new OrganismEventArgs
            {
                Organism = record,
                Timestamp = DateTime.UtcNow
            };
            OnOrganismRegistered?.Invoke(this, args);
            
            CheckPopulationChange(1, record.Species);
            
            return true;
        }

        /// <summary>
        /// Removes an organism from the registry
        /// </summary>
        public bool RemoveOrganism(Guid organismId)
        {
            if (!organisms.TryGetValue(organismId, out var record)) return false;
            
            string species = record.Species;
            
            // Remove from data structures
            organisms.Remove(organismId);
            organismReferences.Remove(organismId);
            deadOrganisms.Remove(organismId);
            
            // Remove from species index
            if (speciesIndex.TryGetValue(species, out var speciesSet))
            {
                speciesSet.Remove(organismId);
                if (speciesSet.Count == 0)
                    speciesIndex.Remove(species);
            }
            
            var args = new OrganismEventArgs
            {
                Organism = record,
                Timestamp = DateTime.UtcNow
            };
            OnOrganismRemoved?.Invoke(this, args);
            
            CheckPopulationChange(-1, species);
            
            return true;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets all organisms matching the query options
        /// </summary>
        public List<OrganismRecord> QueryOrganisms(OrganismQueryOptions? options = null)
        {
            var results = new List<OrganismRecord>();
            var opts = options ?? new OrganismQueryOptions();
            
            IEnumerable<KeyValuePair<Guid, OrganismRecord>> query = organisms;
            
            // Apply species filter
            if (!string.IsNullOrEmpty(opts.SpeciesFilter))
            {
                if (speciesIndex.TryGetValue(opts.SpeciesFilter, out var speciesIds))
                {
                    query = query.Where(kvp => speciesIds.Contains(kvp.Key));
                }
                else
                {
                    return results; // No organisms of this species
                }
            }
            
            foreach (var kvp in query)
            {
                var record = kvp.Value;
                
                // Apply filters
                if (opts.IsAliveOnly.HasValue && record.IsAlive != opts.IsAliveOnly.Value) continue;
                if (opts.IsPregnant.HasValue && record.IsPregnant != opts.IsPregnant.Value) continue;
                if (opts.MinHealth.HasValue && record.Health < opts.MinHealth.Value) continue;
                if (opts.MaxHealth.HasValue && record.Health > opts.MaxHealth.Value) continue;
                if (opts.MinAge.HasValue && record.Age < opts.MinAge.Value) continue;
                if (opts.MaxAge.HasValue && record.Age > opts.MaxAge.Value) continue;
                if (opts.Generation.HasValue && record.Generation != opts.Generation.Value) continue;
                
                // Distance filter
                if (opts.NearPosition.HasValue && opts.MaxDistance.HasValue)
                {
                    float distance = Vector3.Distance(record.Position, opts.NearPosition.Value);
                    if (distance > opts.MaxDistance.Value) continue;
                }
                
                // Custom filter
                if (opts.CustomFilter != null && !opts.CustomFilter(record)) continue;
                
                results.Add(record);
                
                if (opts.Limit.HasValue && results.Count >= opts.Limit.Value)
                    break;
            }
            
            return results;
        }

        /// <summary>
        /// Gets a specific organism by ID
        /// </summary>
        public OrganismRecord? GetOrganism(Guid organismId)
        {
            if (organisms.TryGetValue(organismId, out var record))
                return record;
            return null;
        }

        /// <summary>
        /// Gets the live reference for an organism
        /// </summary>
        public ITrackedOrganism GetOrganismReference(Guid organismId)
        {
            organismReferences.TryGetValue(organismId, out var reference);
            return reference;
        }

        /// <summary>
        /// Gets all living organisms
        /// </summary>
        public List<OrganismRecord> GetAllLiving()
        {
            return QueryOrganisms(new OrganismQueryOptions { IsAliveOnly = true });
        }

        /// <summary>
        /// Gets all organisms of a specific species
        /// </summary>
        public List<OrganismRecord> GetBySpecies(string species)
        {
            return QueryOrganisms(new OrganismQueryOptions { SpeciesFilter = species });
        }

        /// <summary>
        /// Gets all organisms within a distance from a position
        /// </summary>
        public List<OrganismRecord> GetNearby(Vector3 position, float maxDistance, string speciesFilter = null)
        {
            return QueryOrganisms(new OrganismQueryOptions
            {
                NearPosition = position,
                MaxDistance = maxDistance,
                SpeciesFilter = speciesFilter
            });
        }

        /// <summary>
        /// Gets organisms sorted by age
        /// </summary>
        public List<OrganismRecord> GetByAge(bool oldestFirst = true, int? limit = null)
        {
            var query = organisms.Values.AsEnumerable();
            query = oldestFirst 
                ? query.OrderByDescending(o => o.Age) 
                : query.OrderBy(o => o.Age);
            
            if (limit.HasValue)
                query = query.Take(limit.Value);
            
            return query.ToList();
        }

        /// <summary>
        /// Gets organisms sorted by health
        /// </summary>
        public List<OrganismRecord> GetByHealth(bool lowestFirst = true, int? limit = null)
        {
            var query = organisms.Values.AsEnumerable();
            query = lowestFirst 
                ? query.OrderBy(o => o.Health) 
                : query.OrderByDescending(o => o.Health);
            
            if (limit.HasValue)
                query = query.Take(limit.Value);
            
            return query.ToList();
        }

        /// <summary>
        /// Gets the oldest organism
        /// </summary>
        public OrganismRecord? GetOldest(string speciesFilter = null)
        {
            return GetByAge(oldestFirst: true, limit: 1).FirstOrDefault();
        }

        /// <summary>
        /// Gets the youngest organism
        /// </summary>
        public OrganismRecord? GetYoungest(string speciesFilter = null)
        {
            var query = organisms.Values.AsEnumerable();
            if (!string.IsNullOrEmpty(speciesFilter))
                query = query.Where(o => o.Species == speciesFilter);
            
            return query.OrderBy(o => o.Age).FirstOrDefault();
        }

        /// <summary>
        /// Gets offspring of a specific organism
        /// </summary>
        public List<OrganismRecord> GetOffspring(Guid parentId)
        {
            return organisms.Values
                .Where(o => o.ParentA == parentId || o.ParentB == parentId)
                .ToList();
        }

        /// <summary>
        /// Gets siblings of an organism (same parents)
        /// </summary>
        public List<OrganismRecord> GetSiblings(Guid organismId)
        {
            if (!organisms.TryGetValue(organismId, out var record)) 
                return new List<OrganismRecord>();
            
            var siblings = organisms.Values
                .Where(o => o.Id != organismId && 
                           (o.ParentA == record.ParentA || o.ParentB == record.ParentB) &&
                           (record.ParentA != null || record.ParentB != null))
                .ToList();
            
            return siblings;
        }

        /// <summary>
        /// Gets organisms by generation
        /// </summary>
        public List<OrganismRecord> GetByGeneration(int generation)
        {
            return QueryOrganisms(new OrganismQueryOptions { Generation = generation });
        }

        /// <summary>
        /// Gets pregnant organisms
        /// </summary>
        public List<OrganismRecord> GetPregnantOrganisms()
        {
            return QueryOrganisms(new OrganismQueryOptions { IsPregnant = true });
        }

        /// <summary>
        /// Counts organisms matching the query
        /// </summary>
        public int Count(OrganismQueryOptions? options = null)
        {
            return QueryOrganisms(options).Count;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets population statistics
        /// </summary>
        public PopulationStats GetStatistics()
        {
            var stats = new PopulationStats
            {
                TotalOrganisms = organisms.Count,
                LivingOrganisms = organisms.Values.Count(o => o.IsAlive),
                DeadOrganisms = deadOrganisms.Count,
                TotalBirths = TotalBirths,
                TotalDeaths = TotalDeaths,
                SpeciesCount = speciesIndex.Count,
                AverageHealth = organisms.Count > 0 ? organisms.Values.Average(o => o.Health) : 0,
                AverageAge = organisms.Count > 0 ? organisms.Values.Average(o => o.Age) : 0,
                PregnantCount = organisms.Values.Count(o => o.IsPregnant),
                OldestOrganism = GetOldest()
            };
            
            stats.SpeciesBreakdown = new Dictionary<string, SpeciesStats>();
            foreach (var species in speciesIndex.Keys)
            {
                var speciesOrganisms = GetBySpecies(species);
                stats.SpeciesBreakdown[species] = new SpeciesStats
                {
                    Count = speciesOrganisms.Count,
                    Living = speciesOrganisms.Count(o => o.IsAlive),
                    AverageHealth = speciesOrganisms.Count > 0 ? speciesOrganisms.Average(o => o.Health) : 0,
                    AverageAge = speciesOrganisms.Count > 0 ? speciesOrganisms.Average(o => o.Age) : 0,
                    Oldest = speciesOrganisms.Count > 0 ? speciesOrganisms.Max(o => o.Age) : 0
                };
            }
            
            return stats;
        }

        /// <summary>
        /// Gets all species names in the population
        /// </summary>
        public List<string> GetAllSpecies()
        {
            return speciesIndex.Keys.ToList();
        }

        /// <summary>
        /// Checks if a species exists in the population
        /// </summary>
        public bool HasSpecies(string species)
        {
            return speciesIndex.ContainsKey(species) && speciesIndex[species].Count > 0;
        }

        /// <summary>
        /// Gets the count of a specific species
        /// </summary>
        public int GetSpeciesCount(string species)
        {
            if (speciesIndex.TryGetValue(species, out var ids))
                return ids.Count;
            return 0;
        }

        #endregion

        #region Event Handling

        private void OnOrganismDeath(Guid organismId)
        {
            if (!organisms.TryGetValue(organismId, out var record)) return;
            
            // Update record
            record.IsAlive = false;
            organisms[organismId] = record;
            
            deadOrganisms.Add(organismId);
            organismsToCleanup.Enqueue(organismId);
            TotalDeaths++;
            
            var args = new OrganismEventArgs
            {
                Organism = record,
                Timestamp = DateTime.UtcNow
            };
            OnOrganismDied?.Invoke(this, args);
            
            CheckPopulationChange(0, record.Species);
            
            if (logPopulationChanges)
                Debug.Log($"[PopulationRegistry] Died: {record}");
        }

        private void CheckPopulationChange(int delta, string species)
        {
            int currentCount = organisms.Count;
            if (currentCount != lastPopulationCount)
            {
                var args = new PopulationChangeEventArgs
                {
                    PreviousCount = lastPopulationCount,
                    CurrentCount = currentCount,
                    Change = currentCount - lastPopulationCount,
                    Species = species,
                    Timestamp = DateTime.UtcNow
                };
                OnPopulationChanged?.Invoke(this, args);
                
                lastPopulationCount = currentCount;
                
                if (currentCount == 0)
                    OnPopulationEmpty?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Updates & Cleanup

        /// <summary>
        /// Updates all organism records from their live references
        /// </summary>
        public void UpdateRecords()
        {
            foreach (var kvp in organismReferences)
            {
                var organism = kvp.Value;
                if (organism == null) continue;
                
                if (organisms.TryGetValue(kvp.Key, out var record))
                {
                    record.Age = organism.Age;
                    record.Health = organism.Health;
                    record.Energy = organism.Energy;
                    record.Position = organism.Position;
                    record.IsAlive = organism.IsAlive;
                    record.IsPregnant = organism.IsPregnant;
                    record.LastUpdateTime = DateTime.UtcNow;
                    
                    organisms[kvp.Key] = record;
                }
            }
        }

        /// <summary>
        /// Updates a specific organism's record
        /// </summary>
        public void UpdateOrganismRecord(Guid organismId)
        {
            if (!organismReferences.TryGetValue(organismId, out var organism)) return;
            if (!organisms.TryGetValue(organismId, out var record)) return;
            
            record.Age = organism.Age;
            record.Health = organism.Health;
            record.Energy = organism.Energy;
            record.Position = organism.Position;
            record.IsAlive = organism.IsAlive;
            record.IsPregnant = organism.IsPregnant;
            record.LastUpdateTime = DateTime.UtcNow;
            
            organisms[organismId] = record;
            
            OnOrganismUpdated?.Invoke(this, new OrganismEventArgs
            {
                Organism = record,
                Timestamp = DateTime.UtcNow
            });
        }

        private void ProcessCorpseCleanup()
        {
            while (organismsToCleanup.Count > 0)
            {
                Guid organismId = organismsToCleanup.Dequeue();
                RemoveOrganism(organismId);
            }
        }

        /// <summary>
        /// Clears all organisms from the registry
        /// </summary>
        public void Clear()
        {
            organisms.Clear();
            organismReferences.Clear();
            speciesIndex.Clear();
            deadOrganisms.Clear();
            organismsToCleanup.Clear();
            
            lastPopulationCount = 0;
            TotalBirths = 0;
            TotalDeaths = 0;
        }

        #endregion
    }

    /// <summary>
    /// Population statistics data structure
    /// </summary>
    public struct PopulationStats
    {
        public int TotalOrganisms;
        public int LivingOrganisms;
        public int DeadOrganisms;
        public int TotalBirths;
        public int TotalDeaths;
        public int SpeciesCount;
        public float AverageHealth;
        public float AverageAge;
        public int PregnantCount;
        public OrganismRecord? OldestOrganism;
        public Dictionary<string, SpeciesStats> SpeciesBreakdown;
        
        public override string ToString()
        {
            return $"Population: {LivingOrganisms} living, {DeadOrganisms} dead, {SpeciesCount} species";
        }
    }

    /// <summary>
    /// Statistics for a specific species
    /// </summary>
    public struct SpeciesStats
    {
        public int Count;
        public int Living;
        public float AverageHealth;
        public float AverageAge;
        public float Oldest;
        
        public override string ToString()
        {
            return $"Count: {Count}, Living: {Living}, Avg Health: {AverageHealth:P0}, Avg Age: {AverageAge:F1}";
        }
    }
}
