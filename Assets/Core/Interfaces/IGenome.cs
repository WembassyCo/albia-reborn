using System;
using System.Collections.Generic;

namespace AlbiaReborn.Core.Interfaces
{
    /// <summary>
    /// Interface for genetic/genomic data representation.
    /// Defines the contract for genetic inheritance and mutation.
    /// </summary>
    public interface IGenome
    {
        Guid GenomeId { get; }
        byte[] GeneticSequence { get; }
        
        IGene GetGene(string geneName);
        IEnumerable<IGene> GetAllGenes();
        IEnumerable<IGene> GetExpressedGenes();
        
        IGenome Crossover(IGenome other);
        IGenome Mutate(float mutationRate);
        IGenome Clone();
        
        float CalculateSimilarity(IGenome other);
        string GetPhenotypeHash();
    }

    /// <summary>
    /// Represents a single gene within a genome.
    /// </summary>
    public interface IGene
    {
        string GeneName { get; }
        byte[] AlleleData { get; }
        GeneType Type { get; }
        bool IsExpressed { get; }
        float DominanceFactor { get; }
    }

    public enum GeneType
    {
        Morphology,
        Metabolism,
        Behavior,
        Reproduction,
        Sensory,
        Immune,
        Regulatory
    }
}