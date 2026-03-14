using System;
using UnityEngine;
using Albia.Creatures.Neural;

namespace Albia.Lifecycle
{
    /// <summary>
    /// Configuration for corpse behavior
    /// </summary>
    [CreateAssetMenu(fileName = "CorpseConfig", menuName = "Albia/Corpse Config")]
    public class CorpseConfig : ScriptableObject
    {
        [Header("Decay Settings")]
        [Tooltip("Time in seconds before corpse starts decaying")]
        public float decayDelay = 30f;
        
        [Tooltip("Total time for corpse to fully decay")]
        public float decayDuration = 60f;
        
        [Tooltip("Whether corpses can be eaten by other creatures")]
        public bool canBeEaten = true;
        
        [Tooltip("Nutritional value of corpse (0-1)")]
        [Range(0f, 1f)] public float nutritionalValue = 0.5f;
        
        [Header("Visual Settings")]
        [Tooltip("Color shift as corpse decays")]
        public Color decayColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        [Tooltip("Scale reduction as corpse decays")]
        public float minScale = 0.5f;
        
        [Header("Cleanup")]
        [Tooltip("Whether to automatically cleanup corpses")]
        public bool autoCleanup = true;
        
        [Tooltip("Time before corpse is removed from world")]
        public float cleanupDelay = 120f;
    }

    /// <summary>
    /// Represents a corpse in the world. Handles decay, cleanup, and interaction.
    /// </summary>    
    [RequireComponent(typeof(Rigidbody))]
    public class Corpse : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CorpseConfig config;
        
        [Header("Runtime Data")]
        [SerializeField] private Guid originalOrganismId;
        [SerializeField] private string speciesName;
        [SerializeField] private float ageAtDeath;
        [SerializeField] private GenomeData genomeSnapshot;
        [SerializeField] private float deathTime;
        
        // Decay state
        private float currentDecay = 0f;
        private float decayStartTime;
        private float cleanupTime;
        private bool isDecaying = false;
        private bool isBeingEaten = false;
        
        // Visual references (set up in prefab)
        private Renderer[] renderers;
        private Transform visualTransform;
        private Vector3 initialScale;
        private Color[] initialColors;
        
        // Events
        public event Action OnDecayStart;
        public event Action OnFullyDecayed;
        public event Action OnCleanup;
        public event Action<float> OnEaten; // Parameter is amount consumed (0-1)

        // Properties
        public Guid OriginalOrganismId => originalOrganismId;
        public string SpeciesName => speciesName;
        public float AgeAtDeath => ageAtDeath;
        public float CurrentDecay => currentDecay;
        public float CurrentNutritionalValue => config?.nutritionalValue * (1f - currentDecay) ?? 0f;
        public bool IsDecaying => isDecaying;
        public bool IsFullyDecayed => currentDecay >= 1f;
        public bool CanBeEaten => config?.canBeEaten && !isBeingEaten && currentDecay < 0.8f;
        public float TimeSinceDeath => Time.time - deathTime;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            visualTransform = transform.Find("Visual") ?? transform;
            initialScale = visualTransform.localScale;
            
            // Store initial colors
            if (renderers.Length > 0)
            {
                initialColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].material.HasProperty("_Color"))
                        initialColors[i] = renderers[i].material.color;
                }
            }
            
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CorpseConfig>();
            }
        }

        private void Start()
        {
            deathTime = Time.time;
            decayStartTime = deathTime + config.decayDelay;
            cleanupTime = deathTime + config.cleanupDelay;
            
            // Make corpse interactable
            SetupCollider();
            
            // Notify death system
            CorpseManager.Instance?.RegisterCorpse(this);
        }

        private void Update()
        {
            UpdateDecay();
            UpdateCleanup();
        }

        /// <summary>
        /// Initializes the corpse with data from the deceased organism
        /// </summary>
        public void Initialize(Guid organismId, string species, float age, GenomeData genome, CorpseConfig corpseConfig = null)
        {
            originalOrganismId = organismId;
            speciesName = species;
            ageAtDeath = age;
            genomeSnapshot = genome?.Clone();
            
            if (corpseConfig != null)
                config = corpseConfig;
            
            deathTime = Time.time;
            decayStartTime = deathTime + (config?.decayDelay ?? 30f);
            cleanupTime = deathTime + (config?.cleanupDelay ?? 120f);
        }

        /// <summary>
        /// Attempts to eat from the corpse. Returns amount consumed.
        /// </summary>
        public float Eat(float amount)
        {
            if (!CanBeEaten) return 0f;
            
            isBeingEaten = true;
            
            // Calculate actual nutritional value remaining
            float availableNutrition = CurrentNutritionalValue;
            float consumed = Mathf.Min(amount, availableNutrition);
            
            // Accelerate decay when eaten
            currentDecay += consumed * 0.5f;
            
            OnEaten?.Invoke(consumed);
            
            isBeingEaten = false;
            return consumed;
        }

        /// <summary>
        /// Forces immediate decay
        /// </summary>
        public void ForceDecay()
        {
            currentDecay = 1f;
            decayStartTime = Time.time - 1f;
            UpdateVisuals();
            OnFullyDecayed?.Invoke();
        }

        /// <summary>
        /// Forces immediate cleanup
        /// </summary>
        public void ForceCleanup()
        {
            cleanupTime = Time.time;
        }

        /// <summary>
        /// Gets the corpse data as a record
        /// </summary>
        public CorpseRecord GetRecord()
        {
            return new CorpseRecord
            {
                Id = originalOrganismId,
                Species = speciesName,
                Position = transform.position,
                DeathTime = DateTime.UtcNow.AddSeconds(-TimeSinceDeath),
                CurrentDecay = currentDecay,
                NutritionalValue = CurrentNutritionalValue
            };
        }

        private void UpdateDecay()
        {
            if (isFullyDecayed) return;
            
            // Check if decay should start
            if (!isDecaying && Time.time >= decayStartTime)
            {
                isDecaying = true;
                OnDecayStart?.Invoke();
            }
            
            if (isDecaying)
            {
                // Calculate decay progress
                float decayProgress = (Time.time - decayStartTime) / config.decayDuration;
                currentDecay = Mathf.Clamp01(decayProgress);
                
                UpdateVisuals();
                
                if (currentDecay >= 1f)
                {
                    OnFullyDecayed?.Invoke();
                }
            }
        }

        private void UpdateVisuals()
        {
            if (config == null) return;
            
            // Update scale
            float scale = Mathf.Lerp(1f, config.minScale, currentDecay);
            visualTransform.localScale = initialScale * scale;
            
            // Update color
            if (renderers.Length > 0 && initialColors.Length > 0)
            {
                Color targetColor = Color.Lerp(initialColors[0], config.decayColor, currentDecay);
                foreach (var renderer in renderers)
                {
                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = targetColor;
                    }
                }
            }
        }

        private void UpdateCleanup()
        {
            if (Time.time >= cleanupTime)
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            OnCleanup?.Invoke();
            CorpseManager.Instance?.UnregisterCorpse(this);
            Destroy(gameObject);
        }

        private void SetupCollider()
        {
            // Ensure there's a collider for interaction
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(0.5f, 0.3f, 0.5f);
                boxCollider.center = new Vector3(0, 0.15f, 0);
            }
            
            // Set up as trigger for eating
            var triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
                triggerCollider.radius = 1.5f;
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Optional: signal to nearby creatures that food is available
            // This can be used by the sensory system
        }

        private void OnDestroy()
        {
            CorpseManager.Instance?.UnregisterCorpse(this);
        }
    }

    /// <summary>
    /// Record structure for corpse data
    /// </summary>
    [Serializable]
    public struct CorpseRecord
    {
        public Guid Id;
        public string Species;
        public Vector3 Position;
        public DateTime DeathTime;
        public float CurrentDecay;
        public float NutritionalValue;
    }
}
