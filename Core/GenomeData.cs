using System;
using UnityEngine;

namespace Albia.Core
{
    /// <summary>
    /// Genome: 256 genes (0.0-1.0) encoding all heritable traits.
    /// Scales to: Crossover, mutation, lineage tracking
    /// </summary>
    [Serializable]
    public class GenomeData
    {
        public const int GENOME_SIZE = 256;
        
        [SerializeField] private float[] genes = new float[GENOME_SIZE];
        
        // Named indices for readability (genes 0-63 used for biochemistry/physical traits)
        public const int GENE_METABOLISM_RATE = 0;
        public const int GENE_MAX_ENERGY = 1;  // Scales max energy capacity
        public const int GENE_HUNGER_SENSITIVITY = 2;  // How strongly hunger affects neural input
        public const int GENE_FEAR_BASELINE = 3;  // Fear decay rate
        public const int GENE_LEARNING_RATE = 4;  // Hebbian learning speed
        public const int GENE_REPRODUCTION_DRIVE = 5;  // Base reproduction drive
        public const int GENE_LIFESPAN = 6;  // Age before Elder stage
        public const int GENE_SOCIALITY = 7;  // Loneliness sensitivity
        public const int GENE_CURIOSITY = 8;  // Boredom/curiosity drive
        public const int GENE_AGGRESSION = 9;  // Used for Grendels (0 for Norns)
        
        // Neural initialization genes (64-191 = 128 genes for neural weights)
        public const int GENE_NEURAL_START = 64;
        public const int GENE_NEURAL_END = 191;
        
        // Appearance genes (192-223 = body proportions, color)
        public const int GENE_APPEARANCE_START = 192;
        public const int GENE_APPEARANCE_END = 223;
        
        // Species markers (224-255)
        public const int GENE_SPECIES_START = 224;
        public const int GENE_SPECIES_END = 255;

        // Access
        public float GetGene(int index) => index >= 0 && index < GENOME_SIZE ? genes[index] : 0.5f;
        public void SetGene(int index, float value) => genes[index] = Mathf.Clamp01(value);
        public float[] Genes => genes;

        /// <summary>
        /// Generate random genome within species ranges
        /// </summary>
        public static GenomeData GenerateRandom(NornSpecies species)
        {
            var genome = new GenomeData();
            var random = new System.Random(WorldSeed.CurrentSeed + Environment.TickCount);
            
            for (int i = 0; i < GENOME_SIZE; i++)
            {
                genome.genes[i] = (float)random.NextDouble();
            }
            
            // Apply species constraints
            species?.ApplyConstraints(genome);
            
            return genome;
        }

        /// <summary>
        /// Create genome from parents (crossover + mutation)
        /// </summary>
        public static GenomeData Breed(GenomeData parentA, GenomeData parentB, float mutationRate = 0.02f)
        {
            var offspring = new GenomeData();
            var random = new System.Random(WorldSeed.CurrentSeed + Environment.TickCount);
            
            // Two-point crossover
            int pointA = random.Next(GENOME_SIZE);
            int pointB = random.Next(GENOME_SIZE);
            if (pointA > pointB) { int temp = pointA; pointA = pointB; pointB = temp; }
            
            for (int i = 0; i < GENOME_SIZE; i++)
            {
                // Swap parent contribution between points
                float source = (i >= pointA && i <= pointB) ? parentA.genes[i] : parentB.genes[i];
                
                // Mutation
                if (random.NextDouble() < mutationRate)
                {
                    float mutation = ((float)random.NextDouble() - 0.5f) * 0.1f; // ±0.05 mut
                    source = Mathf.Clamp01(source + mutation);
                }
                
                offspring.genes[i] = source;
            }
            
            return offspring;
        }

        /// <summary>
        /// Distance metric for genetic diversity tracking
        /// </summary>
        public float Distance(GenomeData other)
        {
            float sum = 0;
            for (int i = 0; i < GENOME_SIZE; i++)
            {
                float diff = genes[i] - other.genes[i];
                sum += diff * diff;
            }
            return Mathf.Sqrt(sum / GENOME_SIZE);
        }

        /// <summary>
        /// Serialize for save files
        /// </summary>
        public string ToJson() => JsonUtility.ToJson(this);
        public static GenomeData FromJson(string json) => JsonUtility.FromJson<GenomeData>(json);
    }

    /// <summary>
    /// Base class for species constraints
    /// </summary>
    public abstract class NornSpecies
    {
        public abstract void ApplyConstraints(GenomeData genome);
    }

    /// <summary>
    /// Standard Forest Norn
    /// </summary>
    public class ForestNorn : NornSpecies
    {
        public override void ApplyConstraints(GenomeData genome)
        {
            genome.SetGene(GenomeData.GENE_METABOLISM_RATE, Random.Range(0.3f, 0.7f));
            genome.SetGene(GenomeData.GENE_LEARNING_RATE, Random.Range(0.4f, 0.8f));
            genome.SetGene(GenomeData.GENE_AGGRESSION, 0f); // Norns are non-aggressive
        }
    }

    /// <summary>
    /// Seed management for deterministic generation
    /// </summary>
    public static class WorldSeed
    {
        public static int CurrentSeed { get; set; } = 0;
    }
}