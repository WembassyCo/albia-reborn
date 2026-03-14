using UnityEngine;
using System.Collections.Generic;

namespace Albia.Core.Interfaces
{
    /// <summary>
    /// Interface for chemical/biochemical state of organisms and environments.
    /// Handles neurotransmitters, hormones, and metabolic chemicals.
    /// </summary>
    public interface IChemicalState
    {
        float GetConcentration(ChemicalType chemical);
        void SetConcentration(ChemicalType chemical, float amount);
        void AddConcentration(ChemicalType chemical, float delta);
        void DecayChemicals(float deltaTime);
        void DiffuseTo(IChemicalState other, float rate, float deltaTime);
        void GetAllConcentrations(Dictionary<ChemicalType, float> output);
        void Clear();
    }

    public enum ChemicalType : byte
    {
        // Neurotransmitters
        Acetylcholine,
        Dopamine,
        GABA,
        Glutamate,
        Norepinephrine,
        Oxytocin,
        Serotonin,
        
        // Hormones
        Adrenaline,
        Cortisol,
        Estrogen,
        Ghrelin,
        Insulin,
        Leptin,
        Testosterone,
        
        // Metabolic
        ATP,
        Glucose,
        LacticAcid,
        Oxygen,
        
        // Vitals
        Hunger,
        Pain,
        Reward,
        Sleepiness
    }
}