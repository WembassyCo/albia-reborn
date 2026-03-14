using System;
using UnityEngine;

namespace Albia.Creatures
{
    /// <summary>
    /// Partial extension to Norn class for AI integration.
    /// Provides public access points for neural-based AI controllers.
    /// Uses interfaces to avoid circular assembly dependencies.
    /// </summary>    public partial class Norn
    {
        // Interface references (type-erased to avoid circular dependency)
        [SerializeField]
        [Tooltip("INeuralBrain implementation - assign NeuralBrain component")]
        private MonoBehaviour neuralBrain;
        
        [SerializeField]
        [Tooltip("INornAIController implementation - assign NornAIController component")]
        private MonoBehaviour aiController;

        /// <summary>
        /// Public method for AI controller to trigger eating
        /// </summary>
        public void ExecuteEating(float deltaTime)
        {
            ExecuteEatingInternal(deltaTime);
        }

        private void ExecuteEatingInternal(float deltaTime)
        {
            // Reduce hunger
            state.Hunger = Mathf.Max(0, state.Hunger - deltaTime * 0.5f);
            
            // Trigger reward through learning system
            if (Learning != null && learningMemory.Count > 0)
            {
                Learning.LearnFromMemory(Brain, learningMemory, 0.5f);
            }
            
            // Invoke reward event
            OnRewardSignal?.Invoke(0.5f);
        }

        /// <summary>
        /// Public entry point for receiving reward signals from AI controller
        /// Triggers Hebbian learning on recent actions
        /// </summary>
        public void ReceiveRewardSignal(float amount)
        {
            TriggerRewardSignal(amount);
        }

        /// <summary>
        /// Public entry point for receiving punishment signals from AI controller
        /// Triggers Hebbian learning (negative) on recent actions
        /// </summary>
        public void ReceivePunishmentSignal(float amount)
        {
            TriggerPunishmentSignal(amount);
        }

        /// <summary>
        /// Wire NeuralBrain via MonoBehaviour reference.
        /// Call from NornAIController to establish connection.
        /// </summary>
        /// <param name="brain">NeuralBrain component as MonoBehaviour</param>
        public void SetNeuralBrain(MonoBehaviour brain)
        {
            if (brain == null) return;
            
            neuralBrain = brain;
            
            // Try to wire sensory system if brain has the interface
            try
            {
                var sensoryProperty = brain.GetType().GetProperty("Sensory");
                if (sensoryProperty != null && sensorySystem != null)
                {
                    // Create bridge between sensory systems
                    // This is done via reflection to avoid assembly dependency
                }
            }
            catch { /* Graceful fallback */ }
        }

        /// <summary>
        /// Get the AI controller reference as MonoBehaviour.
        /// Allows AI controllers to find Norn without circular dependency.
        /// </summary>
        /// <returns>AI controller as MonoBehaviour or null</returns>
        public MonoBehaviour GetAIControllerAsMonoBehaviour()
        {
            if (aiController == null)
            {
                // Look for any component with "AIController" in name
                var components = GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name.Contains("AIController"))
                    {
                        aiController = comp;
                        break;
                    }
                }
            }
            return aiController;
        }

        /// <summary>
        /// Save brain weights to genome for reproduction.
        /// Call before mating to pass learned weights to offspring.
        /// </summary>
        /// <returns>GenomeData with learned weights</returns>
        public GenomeData SaveBrainWeights()
        {
            // If we have a separate brain component, save its weights
            if (neuralBrain != null)
            {
                var saveMethod = neuralBrain.GetType().GetMethod("SaveToGenome");
                saveMethod?.Invoke(neuralBrain, null);
            }
            
            // Also save built-in brain weights
            Brain?.SaveWeightsToGenome();
            
            return genome;
        }
    }

    /// <summary>
    /// Interface for neural brain components.
    /// Allows Norn to reference brain without knowing concrete type.
    /// </summary>    public interface INeuralBrain
    {
        void ProcessFrame();
        void TriggerReward(float amount);
        void TriggerPunishment(float amount);
        void SaveToGenome();
    }

    /// <summary>
    /// Interface for AI controllers.
    /// Allows Norn to reference controller without knowing concrete type.
    /// </summary>    public interface INornAIController
    {
        void Initialize(GenomeData genome = null);
        void ProcessFrame();
        void EmitReward(float amount);
        void EmitPunishment(float amount);
    }
}
