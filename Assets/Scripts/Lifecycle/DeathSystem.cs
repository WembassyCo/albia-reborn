using System;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Lifecycle
{
    /// <summary>
    /// Event arguments for death events
    /// </summary>
    public class DeathEventArgs : EventArgs
    {
        public Guid OrganismId { get; set; }
        public string Species { get; set; }
        public string Name { get; set; }
        public float Age { get; set; }
        public string CauseOfDeath { get; set; }
        public Vector3 DeathPosition { get; set; }
        public GenomeData Genome { get; set; }
        public DateTime Timestamp { get; set; }
        public bool CreateCorpse { get; set; } = true;
    }

    /// <summary>
    /// Component that handles death logic for an organism.
    /// Integrates with PopulationRegistry and CorpseManager.
    /// </summary>
    [RequireComponent(typeof(Norn))]
    public class DeathSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Whether to create a corpse on death")]
        [SerializeField] private bool createCorpseOnDeath = true;
        
        [Tooltip("Delay before death is processed (allows for animations)")]
        [SerializeField] private float deathDelay = 0f;
        
        [Header("Corpse Settings")]
        [Tooltip("Override corpse prefab (uses default if null)")]
        [SerializeField] private GameObject customCorpsePrefab;
        
        [Tooltip("Custom corpse configuration")]
        [SerializeField] private CorpseConfig customCorpseConfig;

        // References
        private Norn norn;
        private bool isDying = false;
        private float deathTime;

        // Events
        public event EventHandler<DeathEventArgs> OnDeathTriggered;
        public event EventHandler<DeathEventArgs> OnDeathCompleted;
        public event EventHandler OnDeathAnimationStart;
        public event EventHandler OnCorpseCreated;

        // Properties
        public bool IsDying => isDying;
        public float TimeSinceDeathStarted => isDying ? Time.time - deathTime : 0f;

        private void Awake()
        {
            norn = GetComponent<Norn>();
            if (norn == null)
            {
                Debug.LogError("[DeathSystem] Norn component not found!");
                enabled = false;
                return;
            }
            
            // Subscribe to Norn death event
            norn.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            if (norn != null)
                norn.OnDeath -= HandleDeath;
        }

        /// <summary>
        /// Triggers death manually (can be called for debug or special cases)
        /// </summary>
        public void TriggerDeath(string cause = "Manual")
        {
            if (isDying || !norn.IsAlive) return;
            
            HandleDeath();
        }

        /// <summary>
        /// Triggers death with a specific cause
        /// </summary>
        public void TriggerDeath(DeathEventArgs args)
        {
            if (isDying || !norn.IsAlive) return;
            
            ProcessDeath(args);
        }

        private void HandleDeath()
        {
            if (isDying) return;
            
            isDying = true;
            deathTime = Time.time;
            
            var args = new DeathEventArgs
            {
                OrganismId = Guid.NewGuid(), // Use appropriate ID from Norn
                Species = "Norn", // Could be dynamic based on Norn type
                Name = gameObject.name,
                Age = norn.State.Age,
                CauseOfDeath = DetermineCauseOfDeath(),
                DeathPosition = transform.position,
                Genome = norn.Genome?.Clone(),
                Timestamp = DateTime.UtcNow,
                CreateCorpse = createCorpseOnDeath
            };
            
            OnDeathTriggered?.Invoke(this, args);
            OnDeathAnimationStart?.Invoke(this, EventArgs.Empty);
            
            if (deathDelay > 0)
            {
                Invoke(nameof(CompleteDeath), deathDelay);
            }
            else
            {
                ProcessDeath(args);
            }
        }

        private void CompleteDeath()
        {
            var args = new DeathEventArgs
            {
                OrganismId = Guid.NewGuid(),
                Species = "Norn",
                Name = gameObject.name,
                Age = norn.State.Age,
                CauseOfDeath = DetermineCauseOfDeath(),
                DeathPosition = transform.position,
                Genome = norn.Genome?.Clone(),
                Timestamp = DateTime.UtcNow,
                CreateCorpse = createCorpseOnDeath
            };
            
            ProcessDeath(args);
        }

        private void ProcessDeath(DeathEventArgs args)
        {
            // Create corpse if enabled
            if (args.CreateCorpse)
            {
                CreateCorpse(args);
            }
            
            // Notify PopulationRegistry
            PopulationRegistry.Instance?.UpdateOrganismRecord(args.OrganismId);
            
            // Fire completion event
            OnDeathCompleted?.Invoke(this, args);
            
            // Disable this organism
            gameObject.SetActive(false);
        }

        private void CreateCorpse(DeathEventArgs args)
        {
            Corpse corpse = null;
            
            if (customCorpsePrefab != null)
            {
                // Use custom prefab
                GameObject corpseObj = Instantiate(customCorpsePrefab, args.DeathPosition, transform.rotation);
                corpse = corpseObj.GetComponent<Corpse>();
                if (corpse == null)
                    corpse = corpseObj.AddComponent<Corpse>();
                corpse.Initialize(args.OrganismId, args.Species, args.Age, args.Genome, customCorpseConfig);
            }
            else if (CorpseManager.Instance != null)
            {
                // Use CorpseManager to create
                corpse = CorpseManager.Instance.CreateCorpse(
                    args.DeathPosition, 
                    transform.rotation,
                    args.OrganismId,
                    args.Species,
                    args.Age,
                    args.Genome
                );
            }
            else
            {
                // Create simple corpse
                corpse = CorpseManager.Instance?.CreateSimpleCorpse(
                    args.DeathPosition,
                    args.OrganismId,
                    args.Species,
                    args.Age,
                    args.Genome
                );
            }
            
            if (corpse != null)
            {
                OnCorpseCreated?.Invoke(this, EventArgs.Empty);
            }
        }

        private string DetermineCauseOfDeath()
        {
            var state = norn.State;
            
            if (state.Health <= 0)
                return "Health Depletion";
            if (state.Hunger >= 1f)
                return "Starvation";
            if (state.Energy <= 0)
                return "Exhaustion";
            if (state.Age > 600) // Example max age
                return "Old Age";
            
            return "Unknown";
        }
    }

    /// <summary>
    /// MonoBehaviour that can be added to any GameObject to make it die and become a corpse
    /// </summary>
    public static class DeathHelper
    {
        /// <summary>
        /// Kills an organism and creates a corpse
        /// </summary>
        public static void Kill(GameObject organism, string cause = "Unknown")
        {
            var deathSystem = organism.GetComponent<DeathSystem>();
            if (deathSystem != null)
            {
                deathSystem.TriggerDeath(cause);
            }
            else
            {
                // Fallback: directly disable and create corpse
                organism.SetActive(false);
            }
        }

        /// <summary>
        /// Creates a corpse at a position
        /// </summary>
        public static Corpse CreateCorpse(Vector3 position, string species = "Unknown", float age = 0)
        {
            return CorpseManager.Instance?.CreateSimpleCorpse(
                position, 
                Guid.NewGuid(), 
                species, 
                age, 
                null
            );
        }
    }
}
