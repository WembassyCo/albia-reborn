using System.Collections.Generic;
using UnityEngine;

namespace AlbiaReborn.Creatures.Biochemistry
{
    /// <summary>
    /// Chemical state for organisms - 12 core chemicals.
    /// The medium where genetics, environment, and learning interact.
    /// </summary>
    public class ChemicalState
    {
        private Dictionary<ChemicalType, float> _chemicals;
        private Dictionary<ChemicalType, float> _intrinsicRates;

        public ChemicalState()
        {
            _chemicals = new Dictionary<ChemicalType, float>();
            _intrinsicRates = new Dictionary<ChemicalType, float>();
            
            // Initialize all chemicals at 0
            foreach (ChemicalType type in System.Enum.GetValues(typeof(ChemicalType)))
            {
                _chemicals[type] = 0f;
                _intrinsicRates[type] = 0.001f; // Default rise rate
            }
        }

        public float GetChemical(ChemicalType type)
        {
            return _chemicals.TryGetValue(type, out float value) ? value : 0f;
        }

        public void SetChemical(ChemicalType type, float value)
        {
            _chemicals[type] = Mathf.Clamp01(value);
        }

        public void Apply(ChemicalType type, float delta)
        {
            if (_chemicals.ContainsKey(type))
            {
                _chemicals[type] = Mathf.Clamp01(_chemicals[type] + delta);
            }
        }

        /// <summary>
        /// Tick all chemicals with intrinsic rates and cross-interactions.
        /// Called each biochemistry tick (2Hz).
        /// </summary>
        public void Tick(float deltaTime, GenomeData genome)
        {
            // Apply intrinsic rise rates
            foreach (var chemical in _chemicals.Keys)
            {
                float riseRate = _intrinsicRates[chemical] * GetGenomeModifier(genome, chemical);
                float decayRate = GetDecayRate(genome, chemical);
                
                _chemicals[chemical] += (riseRate - decayRate * _chemicals[chemical]) * deltaTime;
            }

            // Apply cross-chemical interactions
            ApplyInteractions();

            // Clamp all values
            foreach (var key in new List<ChemicalType>(_chemicals.Keys))
            {
                _chemicals[key] = Mathf.Clamp01(_chemicals[key]);
            }
        }

        /// <summary>
        /// Genome-modulated intrinsic rate.
        /// </summary>
        private float GetGenomeModifier(GenomeData genome, ChemicalType chemical)
        {
            return chemical switch
            {
                ChemicalType.Hunger => 1f + genome.GetGene(GenomeData.METABOLISM_RATE) * 2f,
                ChemicalType.Fear => 1f + genome.GetGene(GenomeData.FEAR_BASELINE),
                _ => 1f
            };
        }

        private float GetDecayRate(GenomeData genome, ChemicalType chemical)
        {
            return chemical switch
            {
                ChemicalType.Fear => 0.5f + genome.GetGene(GenomeData.FEAR_DECAY) * 0.5f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Cross-chemical interactions.
        /// </summary>
        private void ApplyInteractions()
        {
            // High fear suppresses loneliness
            if (_chemicals[ChemicalType.Fear] > 0.5f)
            {
                float suppression = _chemicals[ChemicalType.Fear] * 0.3f;
                _chemicals[ChemicalType.Loneliness] -= suppression;
            }

            // High pain + high fear = panic
            if (_chemicals[ChemicalType.Pain] > 0.7f && _chemicals[ChemicalType.Fear] > 0.7f)
            {
                _chemicals[ChemicalType.Stress] += 0.1f;
            }

            // High satisfaction reduces stress
            if (_chemicals[ChemicalType.Satisfaction] > 0.6f)
            {
                _chemicals[ChemicalType.Stress] -= 0.05f;
            }
        }

        /// <summary>
        /// Detect if a drive was satisfied (for reward signal).
        /// </summary>
        public bool WasDriveSatisfied(ChemicalType drive, float threshold = 0.3f)
        {
            // Drive satisfied = chemical was high and dropped significantly
            return false; // Tracked externally via deltas
        }
    }

    /// <summary>
    /// All chemical types in the simulation.
    /// </summary>
    public enum ChemicalType
    {
        Hunger,         // Rises with time, falls with eating
        Pain,           // Rises with damage, falls with time
        Fear,           // Rises with threats, falls with safety
        Loneliness,     // Rises without social contact
        Boredom,        // Rises with low stimulation
        Discomfort,     // Temperature/extreme environmental
        Reward,         // Spike when drive satisfied
        Stress,         // Multiple high drives, conflict
        Curiosity,      // Rises with novel stimuli
        Affection,      // Rises in trusted social contexts
        Sleepiness,     // Rises with activity
        Satisfaction    // Composite of overall fulfillment
    }
}
