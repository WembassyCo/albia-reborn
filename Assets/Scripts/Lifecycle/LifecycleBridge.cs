using System;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Lifecycle
{
    /// <summary>
    /// Bridges the Norn creature with the lifecycle systems (Reproduction, PopulationRegistry, Death).
    /// Adds the required interfaces to make Norn compatible with lifecycle management.
    /// </summary>    
    [RequireComponent(typeof(Norn))]
    [RequireComponent(typeof(DeathSystem))]
    public class LifecycleBridge : MonoBehaviour, IReproducible, ITrackedOrganism
    {
        [Header("Identity")]
        [SerializeField] private Guid organismId;
        [SerializeField] private string species = "Norn";
        [SerializeField] private string creatureName;
        [SerializeField] private int generation = 1;
        
        [Header("Lifecycle State")]
        [SerializeField] private ReproductionState reproductionState = ReproductionState.NotReady;
        [SerializeField] private PregnancyData? currentPregnancy;
        [SerializeField] private float lastReproductionTime = -999f;
        
        // References
        private Norn norn;
        private DeathSystem deathSystem;
        
        // Events
        public event Action<Guid> OnReproductionReady;
        public event Action<PregnancyData> OnImpregnated;
        public event Action<Guid> OnGiveBirth;
        public event Action OnDeath;
        
        // IReproducible implementation
        public Guid Id 
        { 
            get 
            { 
                if (organismId == Guid.Empty)
                    organismId = Guid.NewGuid();
                return organismId; 
            } 
        }
        public string Species => species;
        public float Age => norn?.State.Age ?? 0f;
        public float Health => norn?.State.Health ?? 0f;
        public float Energy 
        { 
            get => norn?.State.Energy ?? 0f;
            set 
            { 
                // Energy is read-only on NornState, would need a method to set it
                // This is a limitation we'd need to address in the base Norn class
            }
        }
        public bool IsAlive => norn?.IsAlive ?? false;
        public ReproductionState ReproductionState 
        { 
            get => reproductionState; 
            set => reproductionState = value; 
        }
        public PregnancyData? CurrentPregnancy 
        { 
            get => currentPregnancy; 
            set => currentPregnancy = value; 
        }
        public float LastReproductionTime 
        { 
            get => lastReproductionTime; 
            set => lastReproductionTime = value; 
        }
        public GenomeData Genome => norn?.Genome;
        
        // ITrackedOrganism implementation
        public string Name => creatureName ?? gameObject.name;
        public Vector3 Position => transform.position;
        public bool IsPregnant => currentPregnancy.HasValue;
        public DateTime BirthTime { get; private set; }
        public int Generation => generation;
        public Guid? ParentA { get; set; }
        public Guid? ParentB { get; set; }

        private void Awake()
        {
            norn = GetComponent<Norn>();
            deathSystem = GetComponent<DeathSystem>();
            
            if (norn == null)
            {
                Debug.LogError("[LifecycleBridge] Norn component required!");
                enabled = false;
                return;
            }
            
            // Generate ID if empty
            if (organismId == Guid.Empty)
                organismId = Guid.NewGuid();
            
            // Set birth time
            BirthTime = DateTime.UtcNow;
            
            // Set default name
            if (string.IsNullOrEmpty(creatureName))
                creatureName = $"{species}_{organismId.ToString().Substring(0, 8)}";
        }

        private void Start()
        {
            // Register with systems
            ReproductionSystem.Instance?.RegisterOrganism(this);
            PopulationRegistry.Instance?.RegisterOrganism(this);
            
            // Subscribe to Norn death
            if (norn != null)
                norn.OnDeath += HandleDeath;
        }

        private void Update()
        {
            // Check reproduction readiness
            if (reproductionState == ReproductionState.NotReady)
            {
                if (ReproductionSystem.Instance?.CanReproduce(this) ?? false)
                {
                    reproductionState = ReproductionState.Ready;
                    OnReproductionReady?.Invoke(organismId);
                }
            }
            
            // Check pregnancy completion with gestation
            if (currentPregnancy.HasValue && currentPregnancy.Value.IsReadyToBirth)
            {
                CompleteBirth();
            }
        }

        private void OnDestroy()
        {
            if (norn != null)
                norn.OnDeath -= HandleDeath;
            
            ReproductionSystem.Instance?.UnregisterOrganism(organismId);
            PopulationRegistry.Instance?.RemoveOrganism(organismId);
        }

        /// <summary>
        /// Initializes the lifecycle bridge with parent information
        /// </summary>
        public void Initialize(Guid? parentA = null, Guid? parentB = null, int gen = 1)
        {
            ParentA = parentA;
            ParentB = parentB;
            generation = gen;
            
            if (parentA.HasValue || parentB.HasValue)
            {
                int parentGen = 1;
                if (ParentA.HasValue)
                {
                    var parent = PopulationRegistry.Instance?.GetOrganism(ParentA.Value);
                    if (parent.HasValue)
                        parentGen = Math.Max(parentGen, parent.Value.Generation);
                }
                generation = parentGen + 1;
            }
        }

        /// <summary>
        /// Attempts to find and mate with a nearby compatible partner
        /// </summary>
        public bool TryReproduce()
        {
            return ReproductionSystem.Instance?.TryFindAndReproduce(this) ?? false;
        }

        /// <summary>
        /// Called when this organism is impregnated
        /// </summary>
        public void Impregnate(PregnancyData pregnancy)
        {
            currentPregnancy = pregnancy;
            reproductionState = ReproductionState.Pregnant;
            OnImpregnated?.Invoke(pregnancy);
            
            Debug.Log($"[LifecycleBridge] {Name} is now pregnant!");
        }

        /// <summary>
        /// Completes birth and spawns offspring
        /// </summary>
        private void CompleteBirth()
        {
            if (!currentPregnancy.HasValue) return;
            
            var pregnancy = currentPregnancy.Value;
            reproductionState = ReproductionState.GivingBirth;
            
            // Spawn offspring
            SpawnOffspring(pregnancy);
            
            // Reset state
            reproductionState = ReproductionState.NotReady;
            currentPregnancy = null;
            lastReproductionTime = Time.time;
            
            OnGiveBirth?.Invoke(pregnancy.FatherId);
            Debug.Log($"[LifecycleBridge] {Name} gave birth!");
        }

        /// <summary>
        /// Spawns a new offspring at this position
        /// </summary>
        private void SpawnOffspring(PregnancyData pregnancy)
        {
            // Create offspring genome
            GenomeData childGenome = ReproductionSystem.Instance?.PerformGenomeCrossover(
                Genome, 
                pregnancy.FatherGenome
            );
            
            // Spawn position (slightly offset)
            Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * 0.5f;
            spawnPos.y = Mathf.Max(0, spawnPos.y);
            
            // Instantiate new Norn (would need prefab reference or factory)
            // This is a placeholder - actual implementation would spawn the creature
            Debug.Log($"[LifecycleBridge] Spawning offspring at {spawnPos}");
            
            // Notify that offspring should be spawned
            // In a full implementation, this would trigger the actual spawn
        }

        private void HandleDeath()
        {
            OnDeath?.Invoke();
        }
    }
}
