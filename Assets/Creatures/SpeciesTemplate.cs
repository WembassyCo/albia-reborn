using UnityEngine;

namespace Albia.Creatures.Genetics
{
    /// <summary>
    /// ScriptableObject defining species characteristics.
    /// Editable in Unity Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpecies", menuName = "Albia/Species Template")]
    public class SpeciesTemplate : ScriptableObject
    {
        [Header("Basic Info")]
        public string SpeciesName = "Norn";
        public int NeuralInputCount = 30;
        public int NeuralHiddenCount = 15;
        public int NeuralOutputCount = 15;
        public float BaseMetabolism = 1.0f;
        public Vector2 LifespanRange = new Vector2(600f, 1200f); // seconds

        [Header("Gene Ranges (Min/Max)")]
        [Range(0f, 1f)] public float MetabolismMin = 0.1f;
        [Range(0f, 1f)] public float MetabolismMax = 0.5f;
        
        [Range(0f, 1f)] public float FearBaselineMin = 0.1f;
        [Range(0f, 1f)] public float FearBaselineMax = 0.5f;
        
        [Range(0f, 1f)] public float RewardSensitivityMin = 0.3f;
        [Range(0f, 1f)] public float RewardSensitivityMax = 0.8f;
        
        [Range(0f, 1f)] public float LearningRateMin = 0.1f;
        [Range(0f, 1f)] public float LearningRateMax = 0.5f;
        
        [Range(0f, 1f)] public float AggressionRateMin = 0.1f;
        [Range(0f, 1f)] public float AggressionRateMax = 0.3f;
        
        [Range(0f, 1f)] public float TerritorialStressMin = 0.1f;
        [Range(0f, 1f)] public float TerritorialStressMax = 0.3f;

        [Header("Visual Traits")]
        public Color DefaultFurColor = Color.white;

        /// <summary>
        /// Gets valid gene range for specified index.
        /// </summary>
        public (float min, float max) GetGeneRange(int index)
        {
            // Named genes
            return index switch
            {
                GenomeData.METABOLISM_RATE => (MetabolismMin, MetabolismMax),
                GenomeData.FEAR_BASELINE => (FearBaselineMin, FearBaselineMax),
                GenomeData.REWARD_SENSITIVITY => (RewardSensitivityMin, RewardSensitivityMax),
                GenomeData.LEARNING_RATE => (LearningRateMin, LearningRateMax),
                GenomeData.AGGRESSION_RATE => (AggressionRateMin, AggressionRateMax),
                GenomeData.TERRITORIAL_STRESS => (TerritorialStressMin, TerritorialStressMax),
                // Default for unnamed genes
                _ => (0f, 1f)
            };
        }

        void OnValidate()
        {
            // Ensure ranges are valid
            if (MetabolismMin > MetabolismMax) MetabolismMin = MetabolismMax;
            if (FearBaselineMin > FearBaselineMax) FearBaselineMin = FearBaselineMax;
            if (RewardSensitivityMin > RewardSensitivityMax) RewardSensitivityMin = RewardSensitivityMax;
        }
    }
}
