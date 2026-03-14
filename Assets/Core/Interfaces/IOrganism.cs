using UnityEngine;
using System;

namespace AlbiaReborn.Core.Interfaces
{
    /// <summary>
    /// Interface for biological organisms in the ecosystem.
    /// Defines the contract between Ecosystem and Genetics pods.
    /// </summary>
    public interface IOrganism
    {
        Guid OrganismId { get; }
        string SpeciesName { get; }
        IGenome Genome { get; }
        IOrganismState State { get; }
        Vector3 Position { get; }
        float Age { get; }
        float Health { get; }
        float Energy { get; }
        bool IsAlive { get; }
        
        event Action<IOrganism> OnDeath;
        event Action<IOrganism> OnReproduce;
        
        void Tick(float deltaTime);
        void ApplyDamage(float damage, DamageType type);
        void ConsumeEnergy(float amount);
        void RestoreHealth(float amount);
    }

    public interface IOrganismState
    {
        OrganismLifeStage LifeStage { get; }
        IMetabolism Metabolism { get; }
        IReproduction Reproduction { get; }
        ISensorySystem Senses { get; }
        IChemicalState ChemicalState { get; }
    }

    public enum OrganismLifeStage
    {
        Embryo,
        Infant,
        Child,
        Adolescent,
        Adult,
        Elder,
        Deceased
    }

    public enum DamageType
    {
        Physical,
        Chemical,
        Thermal,
        Nutritional
    }
}