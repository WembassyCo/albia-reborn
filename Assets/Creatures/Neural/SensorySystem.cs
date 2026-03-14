using System;
using UnityEngine;

namespace Albia.Creatures.Neural
{
    /// <summary>
    /// Represents chemical state values for a creature
    /// </summary>
    [Serializable]
    public struct ChemicalState
    {
        public float Hunger;        // 0 = satiated, 1 = starving
        public float Fear;          // 0 = calm, 1 = terrified
        public float Energy;        // 0 = exhausted, 1 = fully energized
        public float Sleepiness;    // 0 = alert, 1 = very sleepy
        public float Pain;          // 0 = no pain, 1 = severe pain
        public float Reward;        // 0 = none, 1 = high reward anticipation
        public float SexDrive;      // 0 = none, 1 = very high
        public float Boredom;       // 0 = engaged, 1 = very bored
        public float Curiosity;     // 0 = uninterested, 1 = very curious
        public float Comfort;       // 0 = uncomfortable, 1 = very comfortable
        public float Aggression;    // 0 = passive, 1 = very aggressive
        public float Trust;         // 0 = suspicious, 1 = trusting
        
        // Total chemical inputs: 12 values
        public const int InputCount = 12;
        
        /// <summary>
        /// Converts chemical state to input array
        /// </summary>
        public float[] ToArray()
        {
            return new float[]
            {
                Mathf.Clamp01(Hunger),
                Mathf.Clamp01(Fear),
                Mathf.Clamp01(Energy),
                Mathf.Clamp01(Sleepiness),
                Mathf.Clamp01(Pain),
                Mathf.Clamp01(Reward),
                Mathf.Clamp01(SexDrive),
                Mathf.Clamp01(Boredom),
                Mathf.Clamp01(Curiosity),
                Mathf.Clamp01(Comfort),
                Mathf.Clamp01(Aggression),
                Mathf.Clamp01(Trust)
            };
        }
    }

    /// <summary>
    /// Proximity sensor data for nearby entities
    /// </summary>
    [Serializable]
    public struct ProximitySensors
    {
        public float FoodDistance;      // 0 = nearby, 1 = far/none
        public float FoodDirection;     // -1 = left, 0 = center, 1 = right
        public float ThreatDistance;    // 0 = nearby, 1 = far/none
        public float ThreatDirection;   // -1 = left, 0 = center, 1 = right
        public float MateDistance;      // 0 = nearby, 1 = far/none
        public float MateDirection;     // -1 = left, 0 = center, 1 = right
        public float FriendDistance;    // 0 = nearby, 1 = far/none
        public float FriendDirection;   // -1 = left, 0 = center, 1 = right
        
        // Total proximity inputs: 8 values
        public const int InputCount = 8;
        
        /// <summary>
        /// Converts proximity sensors to input array
        /// </summary>
        public float[] ToArray()
        {
            return new float[]
            {
                Mathf.Clamp01(FoodDistance),
                Mathf.Clamp(FoodDirection, -1f, 1f),
                Mathf.Clamp01(ThreatDistance),
                Mathf.Clamp(ThreatDirection, -1f, 1f),
                Mathf.Clamp01(MateDistance),
                Mathf.Clamp(MateDirection, -1f, 1f),
                Mathf.Clamp01(FriendDistance),
                Mathf.Clamp(FriendDirection, -1f, 1f)
            };
        }
    }

    /// <summary>
    /// World environmental data
    /// </summary>
    [Serializable]
    public struct WorldInputs
    {
        public float LightLevel;    // 0 = dark, 1 = bright
        public float Temperature;   // 0 = cold, 0.5 = comfortable, 1 = hot
        public float Moisture;      // 0 = dry, 1 = wet
        
        // Total world inputs: 3 values
        public const int InputCount = 3;
        
        /// <summary>
        /// Converts world inputs to array
        /// </summary>
        public float[] ToArray()
        {
            return new float[]
            {
                Mathf.Clamp01(LightLevel),
                Mathf.Clamp01(Temperature),
                Mathf.Clamp01(Moisture)
            };
        }
    }

    /// <summary>
    /// Social context from nearby creatures
    /// </summary>
    [Serializable]
    public struct SocialInputs
    {
        public float NearbyCreatures;   // Normalized count (0 = none, 1 = crowded)
        public float AverageHappiness;  // -1 = miserable, 1 = happy
        public float DominanceRatio;    // -1 = submissive, 1 = dominant
        
        // Total social inputs: 3 values
        public const int InputCount = 3;
        
        /// <summary>
        /// Converts social inputs to array
        /// </summary>
        public float[] ToArray()
        {
            return new float[]
            {
                Mathf.Clamp01(NearbyCreatures),
                Mathf.Clamp(AverageHappiness, -1f, 1f),
                Mathf.Clamp(DominanceRatio, -1f, 1f)
            };
        }
    }

    /// <summary>
    /// Assembles all sensory inputs for the neural network
    /// Total inputs: 12 (chemical) + 8 (proximity) + 3 (world) + 3 (social) = 26 inputs
    /// </summary>
    [Serializable]
    public class SensorySystem
    {
        public ChemicalState Chemicals { get; set; }
        public ProximitySensors Proximity { get; set; }
        public WorldInputs World { get; set; }
        public SocialInputs Social { get; set; }
        
        /// <summary>
        /// Total number of sensory inputs
        /// </summary>
        public const int TotalInputs = ChemicalState.InputCount + 
                                        ProximitySensors.InputCount + 
                                        WorldInputs.InputCount + 
                                        SocialInputs.InputCount; // = 26

        /// <summary>
        /// Assembles all sensory inputs into a single array for the neural network
        /// </summary>
        public float[] AssembleInputs()
        {
            float[] inputs = new float[TotalInputs];
            int index = 0;
            
            // Chemical inputs (12 values)
            float[] chemicalInputs = Chemicals.ToArray();
            Array.Copy(chemicalInputs, 0, inputs, index, chemicalInputs.Length);
            index += chemicalInputs.Length;
            
            // Proximity sensors (8 values)
            float[] proximityInputs = Proximity.ToArray();
            Array.Copy(proximityInputs, 0, inputs, index, proximityInputs.Length);
            index += proximityInputs.Length;
            
            // World inputs (3 values)
            float[] worldInputs = World.ToArray();
            Array.Copy(worldInputs, 0, inputs, index, worldInputs.Length);
            index += worldInputs.Length;
            
            // Social inputs (3 values)
            float[] socialInputs = Social.ToArray();
            Array.Copy(socialInputs, 0, inputs, index, socialInputs.Length);
            
            return inputs;
        }

        /// <summary>
        /// Updates sensory data based on the environment
        /// Call this each tick to refresh sensory inputs
        /// </summary>
        public void Update(float deltaTime, SensoryEnvironment environment)
        {
            if (environment == null) return;
            
            // Update world inputs from environment
            World = new WorldInputs
            {
                LightLevel = environment.LightLevel,
                Temperature = environment.Temperature,
                Moisture = environment.Moisture
            };
            
            // Update social inputs from environment
            Social = new SocialInputs
            {
                NearbyCreatures = environment.GetNearbyCreatureDensity(),
                AverageHappiness = environment.GetAverageHappiness(),
                DominanceRatio = environment.GetDominanceRatio()
            };
            
            // Proximity is typically updated by the creature's sensor system
            // when scanning for nearby entities
        }

        /// <summary>
        /// Updates proximity sensors for a detected entity
        /// </summary>
        public void UpdateProximity(EntityType type, float distance, float direction)
        {
            switch (type)
            {
                case EntityType.Food:
                    Proximity.FoodDistance = distance;
                    Proximity.FoodDirection = direction;
                    break;
                case EntityType.Threat:
                    Proximity.ThreatDistance = distance;
                    Proximity.ThreatDirection = direction;
                    break;
                case EntityType.Mate:
                    Proximity.MateDistance = distance;
                    Proximity.MateDirection = direction;
                    break;
                case EntityType.Friend:
                    Proximity.FriendDistance = distance;
                    Proximity.FriendDirection = direction;
                    break;
            }
        }

        /// <summary>
        /// Clears all proximity sensors (call when no entities detected)
        /// </summary>
        public void ClearProximity()
        {
            Proximity = new ProximitySensors
            {
                FoodDistance = 1f,
                FoodDirection = 0f,
                ThreatDistance = 1f,
                ThreatDirection = 0f,
                MateDistance = 1f,
                MateDirection = 0f,
                FriendDistance = 1f,
                FriendDirection = 0f
            };
        }
    }

    /// <summary>
    /// Types of entities that can be detected
    /// </summary>
    public enum EntityType
    {
        Food,
        Threat,
        Mate,
        Friend
    }

    /// <summary>
    /// Interface for providing environmental sensory data
    /// </summary>
    public interface SensoryEnvironment
    {
        float LightLevel { get; }
        float Temperature { get; }
        float Moisture { get; }
        
        float GetNearbyCreatureDensity();
        float GetAverageHappiness();
        float GetDominanceRatio();
    }
}
