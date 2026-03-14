using UnityEngine;

namespace AlbiaReborn.Creatures.Genetics
{
    /// <summary>
    /// Handles breeding: crossover and mutation.
    /// </summary>
    public static class GeneticsSystem
    {
        private static System.Random _random = new System.Random();

        /// <summary>
        /// Breed two parents to create offspring genome.
        /// Uses two-point crossover + mutation.
        /// </summary>
        public static GenomeData Breed(GenomeData parentA, GenomeData parentB)
        {
            float[] offspringGenes = new float[GenomeData.GeneCount];
            
            // Two-point crossover
            int point1 = _random.Next(GenomeData.GeneCount);
            int point2 = _random.Next(GenomeData.GeneCount);
            
            if (point1 > point2)
                (point1, point2) = (point2, point1);

            for (int i = 0; i < GenomeData.GeneCount; i++)
            {
                // Take middle segment from parentB, rest from parentA
                if (i >= point1 && i <= point2)
                    offspringGenes[i] = parentB.GetGene(i);
                else
                    offspringGenes[i] = parentA.GetGene(i);
            }

            // Mutation pass
            Mutate(offspringGenes, parentA.GetGene(GenomeData.METABOLISM_RATE));

            // Clamp to valid range
            for (int i = 0; i < GenomeData.GeneCount; i++)
            {
                offspringGenes[i] = Mathf.Clamp01(offspringGenes[i]);
            }

            return new GenomeData(offspringGenes);
        }

        /// <summary>
        /// Apply mutation to genome array.
        /// Mutation rate itself is a gene.
        /// </summary>
        private static void Mutate(float[] genes, float mutationRateGene)
        {
            float baseMutationRate = 0.005f; // 0.5% chance per gene
            float mutationRate = baseMutationRate * (0.5f + mutationRateGene); // Gene modulates rate
            float sigma = 0.05f + mutationRateGene * 0.05f; // Mutation magnitude

            for (int i = 0; i < genes.Length; i++)
            {
                if (_random.NextDouble() < mutationRate)
                {
                    // Gaussian perturbation
                    float perturbation = GaussianRandom(0f, sigma);
                    genes[i] += perturbation;
                }
            }
        }

        /// <summary>
        /// Box-Muller transform for Gaussian random.
        /// </summary>
        private static float GaussianRandom(float mean, float stdDev)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double randStdNormal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                                   System.Math.Sin(2.0 * System.Math.PI * u2);
            return mean + stdDev * (float)randStdNormal;
        }
    }
}
