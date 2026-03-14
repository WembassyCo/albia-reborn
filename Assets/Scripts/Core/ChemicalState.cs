using System;
using UnityEngine;

namespace Albia.Core
{
    /// <summary>
    /// Biochemistry: 12 chemicals (0.0-1.0) representing internal drives.
    /// Scales to: Neural inputs, reward/punishment signals
    /// </summary>
    [Serializable]
    public class ChemicalState
    {
        // Core chemicals (MVP uses 6, scales to 12)
        [Range(0f, 1f)] public float Hunger = 0.2f;      // Rises over time, falls when eating
        [Range(0f, 1f)] public float Pain = 0f;          // Rises from damage
        [Range(0f, 1f)] public float Fear = 0f;        // Rises from threats/dark
        [Range(0f, 1f)] public float Reward = 0f;      // Spikes on drive satisfaction
        [Range(0f, 1f)] public float Loneliness = 0f;  // Rises without social contact
        [Range(0f, 1f)] public float Boredom = 0.1f;   // Rises without stimulation
        
        // Extended chemicals (for full game)
        [Range(0f, 1f)] public float Discomfort = 0f;  // Temperature extremes
        [Range(0f, 1f)] public float Stress = 0f;     // Multiple conflicting drives
        [Range(0f, 1f)] public float Curiosity = 0f;  // Novelty seeking
        [Range(0f, 1f)] public float Affection = 0f;   // Social bonding
        [Range(0f, 1f)] public float Sleepiness = 0f; // Fatigue
        [Range(0f, 1f)] public float Satisfaction = 0f; // Overall contentment

        // Genome-modulated rates
        private float hungerRate = 0.05f;
        private float fearDecayRate = 0.02f;
        private float lonelinessRate = 0.01f;
        private float boredomRate = 0.03f;
        private float rewardDecayRate = 0.1f;
        private float painDecayRate = 0.05f;

        /// <summary>
        /// Initialize from genome (genes determine chemical rates)
        /// </summary>
        public void InitializeFromGenome(GenomeData genome)
        {
            hungerRate = 0.03f + genome.GetGene(GenomeData.GENE_METABOLISM_RATE) * 0.05f;
            fearDecayRate = 0.01f + genome.GetGene(GenomeData.GENE_FEAR_BASELINE) * 0.03f;
            lonelinessRate = 0.005f + genome.GetGene(GenomeData.GENE_SOCIALITY) * 0.02f;
            boredomRate = 0.01f + genome.GetGene(GenomeData.GENE_CURIOSITY) * 0.04f;
        }

        /// <summary>
        /// Advance biochemistry each tick
        /// </summary>
        public void Tick(float deltaTime, Organism organism)
        {
            // Baseline rises
            Hunger = Mathf.Clamp01(Hunger + hungerRate * deltaTime);
            Loneliness = Mathf.Clamp01(Loneliness + lonelinessRate * deltaTime);
            Boredom = Mathf.Clamp01(Boredom + boredomRate * deltaTime);
            Sleepiness = Mathf.Clamp01(Sleepiness + hungerRate * 0.1f * deltaTime); // Linked to activity

            // Baseline decays
            Fear = Mathf.Clamp01(Fear - fearDecayRate * deltaTime);
            Pain = Mathf.Clamp01(Pain - painDecayRate * deltaTime);
            Reward = Mathf.Clamp01(Reward - rewardDecayRate * deltaTime);

            // Cross-chemical interactions
            // High fear suppresses loneliness (frightened creatures don't seek company)
            if (Fear > 0.5f) Loneliness *= 0.9f;
            
            // High hunger + high fear = panic (suppressed in MVP, used in full neural)
            Stress = CalculateStress();
            
            // Satisfaction composite
            Satisfaction = CalculateSatisfaction();
        }

        /// <summary>
        /// Apply external stimulus
        /// </summary>
        public void ApplyChemical(ChemicalType type, float delta)
        {
            switch (type)
            {
                case ChemicalType.Hunger: Hunger = Clamp(Hunger - delta); break;
                case ChemicalType.Pain: Pain = Clamp(Pain + delta); break;
                case ChemicalType.Fear: Fear = Clamp(Fear + delta); break;
                case ChemicalType.Reward: Reward = Clamp(Reward + delta); break;
                case ChemicalType.Loneliness: Loneliness = Clamp(Loneliness - delta); break;
                case ChemicalType.Boredom: Boredom = Clamp(Boredom - delta); break;
                case ChemicalType.Discomfort: Discomfort = Clamp(Discomfort + delta); break;
                case ChemicalType.Stress: Stress = Clamp(Stress + delta); break;
                case ChemicalType.Curiosity: Curiosity = Clamp(Curiosity + delta); break;
                case ChemicalType.Affection: Affection = Clamp(Affection + delta); break;
                case ChemicalType.Sleepiness: Sleepiness = Clamp(Sleepiness + delta); break;
                case ChemicalType.Satisfaction: Satisfaction = Clamp(Satisfaction + delta); break;
            }
        }

        private float CalculateStress()
        {
            // Multiple conflicting drives
            float driveSum = Hunger + Fear + Pain + Discomfort;
            return Mathf.Clamp01(driveSum * 0.25f);
        }

        private float CalculateSatisfaction()
        {
            // Inverse of negative drives
            float negativeSum = Hunger + Fear + Pain + Loneliness + Discomfort + Stress;
            float positiveSum = Reward + Affection + Curiosity;
            return Mathf.Clamp01(0.5f + (positiveSum - negativeSum) * 0.1f);
        }

        private float Clamp(float val) => Mathf.Clamp01(val);

        /// <summary>
        /// Get an array of all chemicals (for neural input)
        /// </summary>
        public float[] ToArray()
        {
            // Return 12 chemicals in fixed order
            return new float[] {
                Hunger, Pain, Fear, Reward, Loneliness, Boredom,
                Discomfort, Stress, Curiosity, Affection, Sleepiness, Satisfaction
            };
        }

        /// <summary>
        /// Check if a drive is satisfied (dropped significantly)
        /// </summary>
        public bool WasDriveSatisfied(ChemicalType drive, float previousValue, float threshold = 0.2f)
        {
            float current = GetChemicalValue(drive);
            return previousValue - current > threshold;
        }

        private float GetChemicalValue(ChemicalType type)
        {
            return type switch {
                ChemicalType.Hunger => Hunger,
                ChemicalType.Pain => Pain,
                ChemicalType.Fear => Fear,
                ChemicalType.Reward => Reward,
                ChemicalType.Loneliness => Loneliness,
                ChemicalType.Boredom => Boredom,
                ChemicalType.Discomfort => Discomfort,
                ChemicalType.Stress => Stress,
                ChemicalType.Curiosity => Curiosity,
                ChemicalType.Affection => Affection,
                ChemicalType.Sleepiness => Sleepiness,
                ChemicalType.Satisfaction => Satisfaction,
                _ => 0f
            };
        }
    }

    public enum ChemicalType
    {
        Hunger, Pain, Fear, Reward, Loneliness, Boredom,
        Discomfort, Stress, Curiosity, Affection, Sleepiness, Satisfaction
    }
}