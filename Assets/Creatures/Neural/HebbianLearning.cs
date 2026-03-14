using System;
using UnityEngine;

namespace Albia.Creatures.Neural
{
    /// <summary>
    /// Implements Hebbian learning with reward/punishment modulation.
    /// Strengthens connections that lead to positive outcomes,
    /// weakens connections that lead to negative outcomes.
    /// </summary>
    [Serializable]
    public class HebbianLearning
    {
        /// <summary>
        /// Base learning rate (how fast weights change)
        /// </summary>
        public float LearningRate { get; set; } = 0.01f;
        
        /// <summary>
        /// Maximum weight adjustment per learning step
        /// </summary>
        public float MaxWeightDelta { get; set; } = 0.1f;
        
        /// <summary>
        /// Decay factor for learning over time (prevents overfitting)
        /// </summary>
        public float DecayFactor { get; set; } = 0.999f;
        
        /// <summary>
        /// Current effective learning rate (decays over time)
        /// </summary>
        public float CurrentLearningRate { get; private set; }
        
        /// <summary>
        /// Statistics for debugging
        /// </summary>
        public LearningStats Stats { get; private set; }
        
        public HebbianLearning()
        {
            CurrentLearningRate = LearningRate;
            Stats = new LearningStats();
        }

        /// <summary>
        /// Performs a learning update based on reward/punishment signal.
        /// Uses Hebbian rule: strengthen winning pathway if reward, weaken if punishment.
        /// </summary>
        /// <param name="neuralNet">The neural network to update</param>
        /// <param name="recentInputs">Inputs that led to the action</param>
        /// <param name="winningAction">Index of the action neuron that fired (4-7)</param>
        /// <param name="rewardSignal">Positive for reward, negative for punishment, 0 for neutral</param>
        /// <param name="hiddenActivations">Hidden layer activations from forward pass</param>
        public void Learn(
            NeuralNet neuralNet,
            float[] recentInputs, 
            int winningAction, 
            float rewardSignal,
            float[] hiddenActivations = null)
        {
            if (neuralNet == null) throw new ArgumentNullException(nameof(neuralNet));
            if (recentInputs == null) throw new ArgumentNullException(nameof(recentInputs));
            if (winningAction < 0 || winningAction >= ActionSystem.OutputCount)
                throw new ArgumentOutOfRangeException(nameof(winningAction));
            
            // Skip learning if signal is too weak
            if (Mathf.Abs(rewardSignal) < 0.01f) return;
            
            // Get hidden activations if not provided
            hiddenActivations ??= neuralNet.GetHiddenActivations();
            if (hiddenActivations == null || hiddenActivations.Length != neuralNet.HiddenSize)
                throw new ArgumentException("Invalid hidden activations");
            
            // Calculate learning magnitude
            // Update magnitude = learningRate * signalStrength * activation
            float signalStrength = Mathf.Clamp(rewardSignal, -1f, 1f);
            float magnitude = CurrentLearningRate * signalStrength;
            
            // Apply decay to learning rate
            CurrentLearningRate *= DecayFactor;
            CurrentLearningRate = Mathf.Max(CurrentLearningRate, LearningRate * 0.1f);
            
            // Update hidden->output weights for the winning action
            for (int h = 0; h < neuralNet.HiddenSize; h++)
            {
                float activation = hiddenActivations[h];
                float delta = magnitude * activation;
                
                // Clamp delta to prevent extreme changes
                delta = Mathf.Clamp(delta, -MaxWeightDelta, MaxWeightDelta);
                
                // Get current weight and update
                float currentWeight = neuralNet.GetHiddenOutputWeight(h, winningAction);
                float newWeight = Mathf.Clamp(currentWeight + delta, -1f, 1f);
                
                // Apply the update
                neuralNet.UpdateHiddenOutputWeight(h, winningAction, delta);
                
                // Track stats
                Stats.TotalWeightUpdates++;
                Stats.TotalWeightChange += Mathf.Abs(delta);
            }
            
            // Update input->hidden weights (strengthen pathways that contributed)
            for (int i = 0; i < neuralNet.InputSize; i++)
            {
                float inputActivation = Mathf.Abs(recentInputs[i]);
                
                for (int h = 0; h < neuralNet.HiddenSize; h++)
                {
                    // Only strengthen inputs that were active AND
                    // connected to hidden units that contributed to the winning action
                    float hiddenActivation = hiddenActivations[h];
                    float correlation = inputActivation * hiddenActivation;
                    
                    float delta = magnitude * correlation * 0.5f; // Scale down input layer updates
                    delta = Mathf.Clamp(delta, -MaxWeightDelta, MaxWeightDelta);
                    
                    neuralNet.UpdateInputHiddenWeight(i, h, delta);
                    Stats.TotalWeightUpdates++;
                    Stats.TotalWeightChange += Mathf.Abs(delta);
                }
            }
            
            // Update learning stats
            if (signalStrength > 0)
            {
                Stats.RewardCount++;
                Stats.TotalReward += signalStrength;
            }
            else
            {
                Stats.PunishmentCount++;
                Stats.TotalPunishment += Mathf.Abs(signalStrength);
            }
        }

        /// <summary>
        /// Simplified overload when hidden activations aren't cached externally
        /// </summary>
        public void Learn(
            NeuralNet neuralNet,
            float[] recentInputs, 
            int winningAction, 
            float rewardSignal)
        {
            Learn(neuralNet, recentInputs, winningAction, rewardSignal, null);
        }

        /// <summary>
        /// Applies reward to recent actions from memory
        /// </summary>
        public void LearnFromMemory(
            NeuralNet neuralNet,
            LearningMemory memory,
            float rewardSignal)
        {
            if (memory == null || memory.Count == 0) return;
            
            // Get recent experiences (with higher weight for more recent actions)
            var experiences = memory.GetRecentExperiences(5);
            
            foreach (var exp in experiences)
            {
                // Temporal discounting - older actions get less credit/blame
                float timeFactor = Mathf.Exp(-0.1f * exp.Age);
                float discountedSignal = rewardSignal * timeFactor;
                
                Learn(neuralNet, exp.Inputs, exp.ActionIndex, discountedSignal, exp.HiddenActivations);
            }
        }

        /// <summary>
        /// Resets learning rate to base value
        /// </summary>
        public void ResetLearningRate()
        {
            CurrentLearningRate = LearningRate;
            Stats.Reset();
        }

        /// <summary>
        /// Gets the average weight change for debugging
        /// </summary>
        public float GetAverageWeightChange()
        {
            if (Stats.TotalWeightUpdates == 0) return 0f;
            return Stats.TotalWeightChange / Stats.TotalWeightUpdates;
        }
    }

    /// <summary>
    /// Learning statistics for debugging and analysis
    /// </summary>
    [Serializable]
    public class LearningStats
    {
        public int RewardCount { get; set; }
        public int PunishmentCount { get; set; }
        public float TotalReward { get; set; }
        public float TotalPunishment { get; set; }
        public int TotalWeightUpdates { get; set; }
        public float TotalWeightChange { get; set; }
        
        public float AverageReward => RewardCount > 0 ? TotalReward / RewardCount : 0f;
        public float AveragePunishment => PunishmentCount > 0 ? TotalPunishment / PunishmentCount : 0f;
        public int TotalLearningEvents => RewardCount + PunishmentCount;
        
        public void Reset()
        {
            RewardCount = 0;
            PunishmentCount = 0;
            TotalReward = 0f;
            TotalPunishment = 0f;
            TotalWeightUpdates = 0;
            TotalWeightChange = 0f;
        }
    }
}
