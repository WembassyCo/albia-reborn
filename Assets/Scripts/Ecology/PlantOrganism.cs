using UnityEngine;
using Albia.Creatures;

namespace Albia.Ecology
{
    /// <summary>
    /// Represents a plant organism in the ecosystem.
    /// Handles growth, maturation, and seed production.
    /// MVP: Simple growth, Full: Biome-aware, seasonal
    /// </summary>
    public class PlantOrganism : Organism
    {
        [Header("Plant Configuration")]
        [Tooltip("Base growth rate in units per second")]
        [SerializeField] private float baseGrowthRate = 0.1f;
        
        [Tooltip("Growth rate multiplier when conditions are optimal")]
        [SerializeField] private float growthMultiplier = 1f;
        
        [Tooltip("Size when fully mature")]
        [SerializeField] private Vector3 matureScale = new Vector3(1f, 1f, 1f);
        
        [Tooltip("Time in seconds to reach maturity")]
        [SerializeField] private float timeToMature = 60f;
        
        [Tooltip("Energy value when eaten")]
        [SerializeField] private float foodEnergyValue = 25f;
        
        [Tooltip("Prefab for seeds/spawned offspring")]
        [SerializeField] private GameObject seedPrefab;
        
        [Tooltip("Chance to drop seed on death (0-1)")]
        [SerializeField] private float seedDropChance = 0.3f;
        
        [Tooltip("Number of seeds to spawn")]
        [SerializeField] private int seedsPerSpawn = 1;
        
        [Header("MVP: Simple Spawn Settings")]
        [Tooltip("Spawn radius for seeds")]
        [SerializeField] private float seedSpawnRadius = 5f;
        
        [Tooltip("Minimum distance from parent")]
        [SerializeField] private float minSeedDistance = 2f;
        
        [Header("Optional: Biome Settings (Full Version)")]
        [Tooltip("Preferred biome type (empty = any)")]
        [SerializeField] private string preferredBiome = "";
        
        [Tooltip("Required sunlight level (0-1)")]
        [SerializeField] private float requiredSunlight = 0.3f;
        
        [Tooltip("Current sunlight level at plant position")]
        [SerializeField] private float currentSunlight = 1f;
        
        [Tooltip("Growth penalty in wrong biome")]
        [SerializeField] private float biomeMismatchPenalty = 0.5f;

        // Properties
        public float GrowthRate > { get; private set; }
        public bool IsMature > { get; private set; }
        public GameObject SeedPrefab
        {
            get => seedPrefab;
            set => seedPrefab = value;
        }
        public float FoodEnergyValue => foodEnergyValue;
        public string PreferredBiome => preferredBiome;
        public float CurrentSunlight { get; private set; } = 1f;
        
        // Private state
        private float growthProgress = 0f;
        private Vector3 initialScale;
        private Transform visualTransform;
        
        protected override void Awake()
        {
            base.Awake();
            visualTransform = transform.Find("Visual") ?? transform;
            initialScale = visualTransform != null ? visualTransform.localScale : transform.localScale;
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            GrowthRate = baseGrowthRate;
            IsMature = false;
            growthProgress = 0f;
            
            // Start small
            if (visualTransform != null)
            {
                visualTransform.localScale = initialScale * 0.1f;
            }
        }
        
        /// <summary>
        /// Main biological update - handles growth and maturation
        /// </summary>
        protected override void UpdateBiology(float deltaTime)
        {
            if (IsMature) return;
            
            // Calculate effective growth rate
            float effectiveGrowthRate = CalculateGrowthRate();
            GrowthRate = effectiveGrowthRate;
            
            // Update growth
            float growthThisFrame = effectiveGrowthRate * deltaTime;
            growthProgress += growthThisFrame;
            
            // Update visual scale based on growth progress
            float growthRatio = Mathf.Clamp01(growthProgress / timeToMature);
            Vector3 currentScale = Vector3.Lerp(initialScale * 0.1f, matureScale, growthRatio);
            
            if (visualTransform != null)
            {
                visualTransform.localScale = currentScale;
            }
            else
            {
                transform.localScale = currentScale;
            }
            
            // Check for maturation
            if (growthProgress >= timeToMature)
            {
                Mature();
            }
        }
        
        /// <summary>
        /// Calculates the effective growth rate based on environmental conditions
        /// MVP: Simple rate, Full: Biome-aware with seasonal factors
        /// </summary>
        private float CalculateGrowthRate()
        {
            float rate = baseGrowthRate * growthMultiplier;
            
            // Apply sunlight factor (MVP)
            float sunlightFactor = Mathf.Clamp01(CurrentSunlight / requiredSunlight);
            rate *= sunlightFactor;
            
            // Apply energy factor (plants need energy to grow)
            float energyFactor = Mathf.Clamp01(Energy / MaxEnergy);
            rate *= energyFactor;
            
            // Full version: Apply biome penalty
            if (!string.IsNullOrEmpty(preferredBiome) && GetCurrentBiome() != preferredBiome)
            {
                rate *= (1f - biomeMismatchPenalty);
            }
            
            // Full version: Apply seasonal factor
            float seasonalFactor = GetSeasonalGrowthFactor();
            rate *= seasonalFactor;
            
            return rate;
        }
        
        /// <summary>
        /// Marks the plant as mature and triggers events
        /// </summary>
        private void Mature()
        {
            IsMature = true;
            GrowthRate = 0f;
            
            // Ensure final scale
            if (visualTransform != null)
            {
                visualTransform.localScale = matureScale;
            }
            else
            {
                transform.localScale = matureScale;
            }
        }
        
        /// <summary>
        /// Called when the plant is eaten. Reduces health and energy.
        /// Returns the actual energy consumed.
        /// </summary>
        public virtual float Consume(float amount)
        {
            if (!IsAlive) return 0f;
            
            // Calculate actual consumption based on plant state
            float energyConsumed = Mathf.Min(amount, Energy);
            float healthDamage = Mathf.Min(amount * 0.5f, Health);
            
            ModifyEnergy(-energyConsumed);
            ModifyHealth(-healthDamage);
            
            // Shrink visual when consumed
            ReduceVisual(0.7f);
            
            return energyConsumed;
        }
        
        /// <summary>
        /// Reduces the visual size of the plant
        /// </summary>
        private void ReduceVisual(float factor)
        {
            if (visualTransform != null)
            {
                visualTransform.localScale *= factor;
            }
            else
            {
                transform.localScale *= factor;
            }
            
            // If scaled too small, die
            if ((visualTransform?.localScale.magnitude ?? transform.localScale.magnitude) < 0.1f)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Attempts to spawn seeds around the plant
        /// </summary>
        public virtual void SpawnSeeds(int count = -1)
        {
            if (seedPrefab == null) return;
            
            int seedsToSpawn = count < 0 ? seedsPerSpawn : count;
            
            for (int i = 0; i < seedsToSpawn; i++)
            {
                // Random position within spawn radius
                Vector2 randomCircle = Random.insideUnitCircle * seedSpawnRadius;
                // Ensure minimum distance
                if (randomCircle.magnitude < minSeedDistance)
                {
                    randomCircle = randomCircle.normalized * minSeedDistance;
                }
                
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                spawnPosition.y = GetGroundHeight(spawnPosition);
                
                GameObject seed = Instantiate(seedPrefab, spawnPosition, Quaternion.identity);
                
                // Initialize the new plant
                PlantOrganism newPlant = seed.GetComponent<PlantOrganism>();
                if (newPlant != null)
                {
                    newPlant.Initialize();
                }
            }
        }
        
        /// <summary>
        /// Harvests seeds from the plant without killing it
        /// </summary>
        public virtual bool HarvestSeeds(out GameObject[] seeds)
        {
            seeds = null;
            
            if (!IsMature || seedPrefab == null) return false;
            
            seeds = new GameObject[seedsPerSpawn];
            for (int i = 0; i < seedsPerSpawn; i++)
            {
                seeds[i] = Instantiate(seedPrefab, transform.position, Quaternion.identity);
            }
            
            // Consuming energy for seed production
            ModifyEnergy(-10f);
            
            return true;
        }
        
        /// <summary>
        /// Gets the ground height at a position (simplified)
        /// </summary>
        protected virtual float GetGroundHeight(Vector3 position)
        {
            // Simple raycast down to find ground
            if (Physics.Raycast(new Vector3(position.x, 100f, position.z), Vector3.down, out RaycastHit hit, 200f))
            {
                return hit.point.y;
            }
            return position.y;
        }
        
        /// <summary>
        /// Gets the current biome (placeholder for full version)
        /// </summary>
        protected virtual string GetCurrentBiome()
        {
            // MVP: Return empty, not biome-aware
            // Full: Query biome system
            return "";
        }
        
        /// <summary>
        /// Gets seasonal growth factor (placeholder for full version)
        /// </summary>
        protected virtual float GetSeasonalGrowthFactor()
        {
            // MVP: Always return 1 (no seasonal effect)
            // Full: Query season manager
            return 1f;
        }
        
        public override void Die()
        {
            // Chance to drop seeds on death
            if (Random.value < seedDropChance)
            {
                SpawnSeeds(1);
            }
            
            base.Die();
        }
        
        /// <summary>
        /// Updates environmental conditions (called by EcologyManager)
        /// </summary>
        public virtual void UpdateEnvironmentalConditions(float sunlight, string biome = "")
        {
            CurrentSunlight = sunlight;
            currentSunlight = sunlight;
            
            if (!string.IsNullOrEmpty(biome))
            {
                // Biome checking for full version
            }
        }
        
        /// <summary>
        /// Gets debug information about the plant
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Growth: {growthProgress:F1}/{timeToMature:F1} | " +
                   $"Rate: {GrowthRate:F2} | " +
                   $"Mature: {IsMature} | " +
                   $"Energy: {Energy:F1}/{MaxEnergy:F1}";
        }
        
        // Visual gizmos
        protected virtual void OnDrawGizmos()
        {
            // Show mature size
            Gizmos.color = IsMature ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, matureScale.magnitude * 0.5f);
            
            // Show seed spawn radius
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, seedSpawnRadius);
        }
    }
}
