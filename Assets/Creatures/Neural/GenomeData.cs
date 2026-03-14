using System;

namespace Albia.Creatures.Neural
{
    /// <summary>
    /// Represents genetic data for a creature.
    /// Genes 0-63: Physical traits
    /// Genes 64-191: Neural network weights (128 genes = 128 floats)
    /// </summary>
    [Serializable]
    public class GenomeData
    {
        public float[] Genes { get; private set; }
        public const int TotalGenes = 192;
        public const int NeuralWeightStartIndex = 64;
        public const int NeuralWeightCount = 128;

        public GenomeData()
        {
            Genes = new float[TotalGenes];
            Randomize();
        }

        public GenomeData(float[] genes)
        {
            if (genes == null || genes.Length != TotalGenes)
                throw new ArgumentException($"Genes array must have exactly {TotalGenes} elements");
            
            Genes = new float[TotalGenes];
            Array.Copy(genes, Genes, TotalGenes);
        }

        /// <summary>
        /// Randomizes all genes with values between -1 and 1
        /// </summary>
        public void Randomize(Random random = null)
        {
            random ??= new Random();
            for (int i = 0; i < TotalGenes; i++)
            {
                Genes[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }
        }

        /// <summary>
        /// Gets neural network weight from genome at specified index (0-127)
        /// </summary>
        public float GetNeuralWeight(int index)
        {
            if (index < 0 || index >= NeuralWeightCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            return Genes[NeuralWeightStartIndex + index];
        }

        /// <summary>
        /// Sets neural network weight in genome at specified index (0-127)
        /// </summary>
        public void SetNeuralWeight(int index, float value)
        {
            if (index < 0 || index >= NeuralWeightCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            Genes[NeuralWeightStartIndex + index] = Math.Clamp(value, -1f, 1f);
        }

        /// <summary>
        /// Performs crossover between two parent genomes
        /// </summary>
        public static GenomeData Crossover(GenomeData parentA, GenomeData parentB, Random random = null)
        {
            random ??= new Random();
            float[] childGenes = new float[TotalGenes];
            
            for (int i = 0; i < TotalGenes; i++)
            {
                // 50/50 chance to inherit from either parent
                childGenes[i] = random.NextDouble() < 0.5 ? parentA.Genes[i] : parentB.Genes[i];
            }
            
            return new GenomeData(childGenes);
        }

        /// <summary>
        /// Performs crossover with mutation
        /// </summary>
        public static GenomeData CrossoverWithMutation(GenomeData parentA, GenomeData parentB, 
            float mutationRate = 0.01f, float mutationStrength = 0.1f, Random random = null)
        {
            random ??= new Random();
            var child = Crossover(parentA, parentB, random);
            
            // Apply mutations
            for (int i = 0; i < TotalGenes; i++)
            {
                if (random.NextDouble() < mutationRate)
                {
                    float mutation = (float)(random.NextDouble() * 2.0 - 1.0) * mutationStrength;
                    child.Genes[i] = Math.Clamp(child.Genes[i] + mutation, -1f, 1f);
                }
            }
            
            return child;
        }

        /// <summary>
        /// Creates a deep copy of this genome
        /// </summary>
        public GenomeData Clone()
        {
            return new GenomeData(Genes);
        }
    }
}
