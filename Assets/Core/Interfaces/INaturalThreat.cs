using UnityEngine;
using System;

namespace AlbiaReborn.Core.Interfaces
{
    /// <summary>
    /// Interface for natural threats and environmental dangers.
    /// Defines hazards that organisms and agents must respond to.
    /// </summary>
    public interface INaturalThreat
    {
        Guid ThreatId { get; }
        string ThreatName { get; }
        ThreatCategory Category { get; }
        Vector3 Epicenter { get; }
        float ThreatRadius { get; }
        float CurrentIntensity { get; }
        float MaxIntensity { get; }
        bool IsActive { get; }
        
        event Action<INaturalThreat> OnThreatActivated;
        event Action<INaturalThreat> OnThreatNeutralized;
        
        void Activate();
        void Deactivate();
        float CalculateDangerAt(Vector3 position);
        void Tick(float deltaTime);
    }

    public enum ThreatCategory
    {
        Biological,     // Pathogens, toxins, parasites
        Physical,       // Landslides, falling objects
        Weather,        // Storms, drought, floods
        Thermal,        // Fire, extreme temperatures
        Chemical,       // Pollution, acid rain
        Predatory,      // Apex predators
        Environmental   // Radiation, vacuum
    }

    public interface IThreatResponse
    {
        float FearLevel { get; set; }
        float Urgency { get; }
        IThreatResponseAction[] EvaluateResponses(INaturalThreat threat);
        void ExecuteResponse(IThreatResponseAction action);
    }

    public interface IThreatResponseAction
    {
        string ActionName { get; }
        float SuccessProbability { get; }
        float EnergyCost { get; }
        void Execute();
    }
}