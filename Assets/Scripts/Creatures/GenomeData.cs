using System;
using UnityEngine;

namespace AlbiaReborn.Creatures.Genetics
{
    /// <summary>
    /// 256-gene genome data structure.
    /// Immutable after creation - only offspring get new genomes.
    /// </summary>
    [Serializable]
    public class GenomeData
    {
        public const int GeneCount = 256;
        private float[] _genes;

        // Gene region constants
        public const int PHYSICAL_TRAITS_START = 0;      // 0-15
        public const int PHYSICAL_TRAITS_END = 15;
        public const int BIOCHEMISTRY_START = 16;        // 16-63
        public const int BIOCHEMISTRY_END = 63;
        public const int NEURAL_INIT_START = 64;         // 64-191
        public const int NEURAL_INIT_END = 191;
        public const int APPEARANCE_START = 192;         // 192-223
        public const int APPEARANCE_END = 223;
        public const int SPECIES_MARKERS_START = 224;      // 224-255
        public const int SPECIES_MARKERS_END = 255;

        // Named gene indices
        public const int METABOLISM_RATE = 16;
        public const int FEAR_BASELINE = 17;
        public const int REWARD_SENSITIVITY = 18;
        public const int SENSORY_RANGE = 19;
        public const int LEARNING_RATE = 20;
        public const int AGGRESSION_RATE = 21;
        public const int TERRITORIAL_STRESS = 22;
        public const int LIFESPAN_BASE = 23;
        public const int REPRODUCTION_DRIVE = 24;
        public const int FEAR_DECAY = 25;

        public float[] Genes => _genes;

        public GenomeData()
        {
            _genes = new float[GeneCount];
        }

        public GenomeData(float[] genes)
        {
            if (genes.Length != GeneCount)
                throw new ArgumentException($"Genome must have {GeneCount} genes");
            
            _genes = new float[GeneCount];
            Array.Copy(genes, _genes, GeneCount);
        }

        public float GetGene(int index)
        {
            if (index < 0 || index >= GeneCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _genes[index];
        }

        public void SetGene(int index, float value)
        {
            if (index < 0 || index >= GeneCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            _genes[index] = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Generate genome from template with randomization.
        /// </summary>
        public static GenomeData FromTemplate(SpeciesTemplate template)
        {
            var genome = new GenomeData();
            System.Random rand = new System.Random();

            for (int i = 0; i < GeneCount; i++)
            {
                (float min, float max) = template.GetGeneRange(i);
                genome._genes[i] = min + (float)rand.NextDouble() * (max - min);
            }

            return genome;
        }

        /// <summary>
        /// Calculate genetic distance between two genomes.
        /// </summary>
        public float Distance(GenomeData other)
        {
            float sumSquaredDiff = 0f;
            for (int i = 0; i < GeneCount; i++)
            {
                float diff = _genes[i] - other._genes[i];
                sumSquaredDiff += diff * diff;
            }
            return Mathf.Sqrt(sumSquaredDiff / GeneCount);
        }

        /// <summary>
        /// Returns normalized array for visualization.
        /// </summary>
        public float[] Visualize()
        {
            // Return copy for safety
            float[] copy = new float[GeneCount];
            Array.Copy(_genes, copy, GeneCount);
            return copy;
        }
    }
}
