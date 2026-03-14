using System;
using System.Collections.Generic;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Lifecycle
{
    /// <summary>
    /// Represents the reproduction state of an organism
    /// </summary>
    public enum ReproductionState
    {
        NotReady,
        Ready,
        Pregnant,
        GivingBirth
    }

    /// <summary>
    /// Pregnancy data structure for tracking gestation
    /// </summary>
    [Serializable]
    public struct PregnancyData
    {
        public Guid FatherId;
        public float StartTime;
        public float Duration;
        public GenomeData FatherGenome;
        public bool IsReadyToBirth => Time.time - StartTime >= Duration;
        
        public static PregnancyData Create(Guid fatherId, GenomeData fatherGenome, float duration)
        {
            return new PregnancyData
            {
                FatherId = fatherId,
                FatherGenome = fatherGenome.Clone(),
                StartTime = Time.time,
                Duration = duration
            };
        }
    }

    /// <summary>
    /// Configuration for reproduction system
    /// </summary>
    [CreateAssetMenu(fileName = "ReproductionConfig", menuName = "Albia/Reproduction Config")]
    public class ReproductionConfig : ScriptableObject
    {
        [Header("Age Requirements")]
        [Tooltip("Minimum age before organism can reproduce")]
        public float minReproductionAge = 60f; // ~1 minute at 1x speed
        
        [Tooltip("Maximum age where reproduction is possible")]
        public float maxReproductionAge = 600f; // ~10 minutes
        
        [Header("Health/Energy Requirements")]
        [Tooltip("Minimum health required for reproduction")]
        [Range(0f, 1f)] public float minHealth = 0.5f;
        
        [Tooltip("Minimum energy required for reproduction")]
        [Range(0f, 1f)] public float minEnergy = 0.6f;
        
        [Tooltip("Energy consumed during reproduction")]
        [Range(0f, 1f)] public float reproductionEnergyCost = 0.3f;
        
        [Header("Gestation Settings")]
        [Tooltip("Whether to use gestation period (Full) or instant birth (MVP)")]
        public bool useGestation = false;
        
        [Tooltip("Duration of pregnancy in seconds (only used if useGestation is true)")]
        public float baseGestationDuration = 120f; // 2 minutes
        
        [Tooltip("Random variation in gestation duration (+/-)")]
        public float gestationVariation = 20f;
        
        [Header("Cooldown Settings")]
        [Tooltip("Time before organism can reproduce again")]
        public float reproductionCooldown = 180f; // 3 minutes
        
        [Header("Mutation Settings")]
        [Tooltip("Chance of mutation occurring")]
        [Range(0f, 1f)] public float mutationRate = 0.05f;
        
        [Tooltip("Strength of mutations")]
        [Range(0f, 1f)] public float mutationStrength = 0.1f;
    }

    /// <summary>
    /// Interface for organisms that can reproduce
    /// </summary>
    public interface IReproducible
    {
        Guid Id { get; }
        string Species { get; }
        float Age { get; }
        float Health { get; }
        float Energy { get; set; }
        bool IsAlive { get; }
        ReproductionState ReproductionState { get; set; }
        PregnancyData? CurrentPregnancy { get; set; }
        float LastReproductionTime { get; set; }
        GenomeData Genome { get; }
        GameObject gameObject { get; }
        
        event Action<Guid> OnReproductionReady;
        event Action<PregnancyData> OnImpregnated;
        event Action<Guid> OnGiveBirth;
    }

    /// <summary>
    /// Event arguments for reproduction events
    /// </summary>
    public class ReproductionEventArgs : EventArgs
    {
        public Guid MotherId { get; set; }
        public Guid FatherId { get; set; }
        public GenomeData ChildGenome { get; set; }
        public Vector3 BirthPosition { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Manages reproduction logic for organisms in the world.
    /// Handles reproduction eligibility, genome crossover, and pregnancy gestation.
    /// </summary>
    public class ReproductionSystem : MonoBehaviour
    {
        public static ReproductionSystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private ReproductionConfig config;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;

        // Events
        public event EventHandler<ReproductionEventArgs> OnReproductionAttempted;
        public event EventHandler<ReproductionEventArgs> OnReproductionSuccessful;
        public event EventHandler<ReproductionEventArgs> OnBirth;
        public event EventHandler<Guid> OnPregnancyStarted;
        public event EventHandler<Guid> OnPregnancyCompleted;

        // Tracked organisms
        private readonly Dictionary<Guid, IReproducible> trackedOrganisms = new();
        private readonly List<Guid> pregnantOrganisms = new();
        
        // Reproduction candidates by species
        private readonly Dictionary<string, List<Guid>> readyMalesBySpecies = new();
        private readonly Dictionary<string, List<Guid>> readyFemalesBySpecies = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ReproductionConfig>();
                LogDebug("Created default ReproductionConfig");
            }
        }

        private void Update()
        {
            UpdatePregnancies();
            UpdateReproductionStates();
        }

        /// <summary>
        /// Registers an organism with the reproduction system
        /// </summary>
        public void RegisterOrganism(IReproducible organism)
        {
            if (organism == null || !organism.IsAlive) return;
            
            trackedOrganisms[organism.Id] = organism;
            organism.OnReproductionReady += OnOrganismReadyForReproduction;
            
            LogDebug($"Registered organism: {organism.Id} ({organism.Species})");
        }

        /// <summary>
        /// Unregisters an organism from the reproduction system
        /// </summary>
        public void UnregisterOrganism(Guid organismId)
        {
            if (trackedOrganisms.TryGetValue(organismId, out var organism))
            {
                organism.OnReproductionReady -= OnOrganismReadyForReproduction;
                trackedOrganisms.Remove(organismId);
                pregnantOrganisms.Remove(organismId);
                
                // Remove from ready lists
                RemoveFromReadyLists(organismId, organism.Species);
                
                LogDebug($"Unregistered organism: {organismId}");
            }
        }

        /// <summary>
        /// Checks if an organism can reproduce based on age, health, energy, and cooldown
        /// </summary>
        public bool CanReproduce(IReproducible organism)
        {
            if (organism == null || !organism.IsAlive) return false;
            if (organism.ReproductionState == ReproductionState.Pregnant) return false;
            if (organism.ReproductionState == ReproductionState.GivingBirth) return false;
            
            // Age check
            if (organism.Age < config.minReproductionAge || organism.Age > config.maxReproductionAge)
                return false;
            
            // Health check
            if (organism.Health < config.minHealth)
                return false;
            
            // Energy check
            if (organism.Energy < config.minEnergy + config.reproductionEnergyCost)
                return false;
            
            // Cooldown check
            if (Time.time - organism.LastReproductionTime < config.reproductionCooldown)
                return false;
            
            return true;
        }

        /// <summary>
        /// Attempts reproduction between two organisms
        /// </summary>
        public bool TryReproduce(IReproducible mother, IReproducible father)
        {
            if (!CanReproduce(mother) || !CanReproduce(father))
                return false;
            
            if (mother.Species != father.Species)
                return false;
            
            // Verify both are registered
            if (!trackedOrganisms.ContainsKey(mother.Id) || !trackedOrganisms.ContainsKey(father.Id))
                return false;
            
            var eventArgs = new ReproductionEventArgs
            {
                MotherId = mother.Id,
                FatherId = father.Id,
                Timestamp = DateTime.UtcNow
            };
            
            OnReproductionAttempted?.Invoke(this, eventArgs);
            
            // Consume energy
            mother.Energy -= config.reproductionEnergyCost;
            father.Energy -= config.reproductionEnergyCost * 0.5f;
            
            // Create child genome through crossover
            GenomeData childGenome = PerformGenomeCrossover(mother.Genome, father.Genome);
            eventArgs.ChildGenome = childGenome;
            
            if (config.useGestation)
            {
                // Start pregnancy
                StartPregnancy(mother, father, childGenome);
            }
            else
            {
                // MVP: Instant birth
                eventArgs.BirthPosition = mother.gameObject.transform.position;
                OnReproductionSuccessful?.Invoke(this, eventArgs);
                OnBirth?.Invoke(this, eventArgs);
                
                // Trigger immediate birth
                mother.OnGiveBirth?.Invoke(father.Id);
            }
            
            // Update cooldown
            mother.LastReproductionTime = Time.time;
            father.LastReproductionTime = Time.time;
            
            // Remove from ready lists
            RemoveFromReadyLists(mother.Id, mother.Species);
            RemoveFromReadyLists(father.Id, father.Species);
            
            mother.ReproductionState = ReproductionState.NotReady;
            father.ReproductionState = ReproductionState.NotReady;
            
            LogDebug($"Reproduction successful: {mother.Id} + {father.Id}");
            return true;
        }

        /// <summary>
        /// Performs genome crossover between two parent genomes
        /// </summary>
        public GenomeData PerformGenomeCrossover(GenomeData motherGenome, GenomeData fatherGenome)
        {
            return GenomeData.CrossoverWithMutation(
                motherGenome, 
                fatherGenome, 
                config.mutationRate, 
                config.mutationStrength,
                new System.Random()
            );
        }

        /// <summary>
        /// Attempts to find a mate for the given organism
        /// </summary>
        public bool TryFindAndReproduce(IReproducible organism)
        {
            if (!CanReproduce(organism))
                return false;
            
            // Look for compatible mate
            if (TryGetCompatibleMate(organism, out var mate))
            {
                return TryReproduce(organism, mate);
            }
            
            return false;
        }

        /// <summary>
        /// Gets a list of potential mates for an organism
        /// </summary>
        public List<IReproducible> GetPotentialMates(IReproducible organism, float maxDistance = float.MaxValue)
        {
            var mates = new List<IReproducible>();
            Vector3 organismPos = organism.gameObject.transform.position;
            
            foreach (var kvp in trackedOrganisms)
            {
                var candidate = kvp.Value;
                if (candidate.Id == organism.Id) continue;
                if (candidate.Species != organism.Species) continue;
                if (!CanReproduce(candidate)) continue;
                
                // Distance check
                if (maxDistance < float.MaxValue)
                {
                    float distance = Vector3.Distance(organismPos, candidate.gameObject.transform.position);
                    if (distance > maxDistance) continue;
                }
                
                mates.Add(candidate);
            }
            
            return mates;
        }

        /// <summary>
        /// Forces immediate birth (for debugging or special cases)
        /// </summary>
        public void ForceBirth(IReproducible mother)
        {
            if (mother?.CurrentPregnancy == null) return;
            
            CompletePregnancy(mother);
        }

        /// <summary>
        /// Gets statistics about reproduction in the population
        /// </summary>
        public ReproductionStats GetStatistics()
        {
            return new ReproductionStats
            {
                TotalTracked = trackedOrganisms.Count,
                PregnantCount = pregnantOrganisms.Count,
                ReadyToReproduce = CountReadyOrganisms(),
                BySpecies = GetSpeciesBreakdown()
            };
        }

        #region Private Methods

        private void OnOrganismReadyForReproduction(Guid organismId)
        {
            if (!trackedOrganisms.TryGetValue(organismId, out var organism)) return;
            
            organism.ReproductionState = ReproductionState.Ready;
            
            // Add to appropriate ready list
            if (!readyMalesBySpecies.ContainsKey(organism.Species))
                readyMalesBySpecies[organism.Species] = new List<Guid>();
            if (!readyFemalesBySpecies.ContainsKey(organism.Species))
                readyFemalesBySpecies[organism.Species] = new List<Guid>();
            
            // For simplicity, we'll add to both - in a real implementation
            // you'd differentiate based on organism sex
            readyMalesBySpecies[organism.Species].Add(organismId);
            readyFemalesBySpecies[organism.Species].Add(organismId);
        }

        private void StartPregnancy(IReproducible mother, IReproducible father, GenomeData childGenome)
        {
            float duration = config.baseGestationDuration + 
                UnityEngine.Random.Range(-config.gestationVariation, config.gestationVariation);
            
            var pregnancy = PregnancyData.Create(father.Id, father.Genome, duration);
            mother.CurrentPregnancy = pregnancy;
            mother.ReproductionState = ReproductionState.Pregnant;
            pregnantOrganisms.Add(mother.Id);
            
            OnPregnancyStarted?.Invoke(this, mother.Id);
            mother.OnImpregnated?.Invoke(pregnancy);
            
            LogDebug($"Pregnancy started: {mother.Id}, duration: {duration:F1}s");
        }

        private void UpdatePregnancies()
        {
            for (int i = pregnantOrganisms.Count - 1; i >= 0; i--)
            {
                Guid motherId = pregnantOrganisms[i];
                if (!trackedOrganisms.TryGetValue(motherId, out var mother))
                {
                    pregnantOrganisms.RemoveAt(i);
                    continue;
                }
                
                if (mother.CurrentPregnancy?.IsReadyToBirth ?? false)
                {
                    CompletePregnancy(mother);
                    pregnantOrganisms.RemoveAt(i);
                }
            }
        }

        private void CompletePregnancy(IReproducible mother)
        {
            if (mother.CurrentPregnancy == null) return;
            
            mother.ReproductionState = ReproductionState.GivingBirth;
            
            var pregnancy = mother.CurrentPregnancy.Value;
            var eventArgs = new ReproductionEventArgs
            {
                MotherId = mother.Id,
                FatherId = pregnancy.FatherId,
                BirthPosition = mother.gameObject.transform.position,
                Timestamp = DateTime.UtcNow
                // Note: Genome is stored in pregnancy data if needed
            };
            
            OnPregnancyCompleted?.Invoke(this, mother.Id);
            OnBirth?.Invoke(this, eventArgs);
            
            mother.OnGiveBirth?.Invoke(pregnancy.FatherId);
            mother.CurrentPregnancy = null;
            mother.ReproductionState = ReproductionState.NotReady;
            
            LogDebug($"Birth completed: {mother.Id}");
        }

        private void UpdateReproductionStates()
        {
            foreach (var organism in trackedOrganisms.Values)
            {
                if (organism.ReproductionState == ReproductionState.NotReady && CanReproduce(organism))
                {
                    organism.ReproductionState = ReproductionState.Ready;
                    organism.OnReproductionReady?.Invoke(organism.Id);
                }
            }
        }

        private bool TryGetCompatibleMate(IReproducible organism, out IReproducible mate)
        {
            mate = null;
            
            // Try to find a ready mate of the same species
            var potentialMates = GetPotentialMates(organism, maxDistance: 10f);
            if (potentialMates.Count > 0)
            {
                // Pick closest mate
                float closestDistance = float.MaxValue;
                foreach (var candidate in potentialMates)
                {
                    float distance = Vector3.Distance(
                        organism.gameObject.transform.position, 
                        candidate.gameObject.transform.position
                    );
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        mate = candidate;
                    }
                }
                return mate != null;
            }
            
            return false;
        }

        private void RemoveFromReadyLists(Guid organismId, string species)
        {
            if (readyMalesBySpecies.TryGetValue(species, out var males))
                males.Remove(organismId);
            if (readyFemalesBySpecies.TryGetValue(species, out var females))
                females.Remove(organismId);
        }

        private int CountReadyOrganisms()
        {
            int count = 0;
            foreach (var organism in trackedOrganisms.Values)
            {
                if (organism.ReproductionState == ReproductionState.Ready)
                    count++;
            }
            return count;
        }

        private Dictionary<string, int> GetSpeciesBreakdown()
        {
            var breakdown = new Dictionary<string, int>();
            foreach (var organism in trackedOrganisms.Values)
            {
                if (!breakdown.ContainsKey(organism.Species))
                    breakdown[organism.Species] = 0;
                breakdown[organism.Species]++;
            }
            return breakdown;
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogging)
                Debug.Log($"[ReproductionSystem] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Statistics about the reproduction system
    /// </summary>
    public struct ReproductionStats
    {
        public int TotalTracked;
        public int PregnantCount;
        public int ReadyToReproduce;
        public Dictionary<string, int> BySpecies;
    }
}
