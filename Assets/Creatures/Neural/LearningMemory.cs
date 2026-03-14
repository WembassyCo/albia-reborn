using System;
using System.Collections.Generic;
using UnityEngine;

namespace Albia.Creatures.Neural
{
    /// <summary>
    /// Represents a single experience/memory entry for credit assignment
    /// </summary>
    [Serializable]
    public struct Experience
    {
        /// <summary>
        /// Timestamp when this experience occurred
        /// </summary>
        public float Timestamp { get; set; }
        
        /// <summary>
        /// Input values at the time of action
        /// </summary>
        public float[] Inputs { get; set; }
        
        /// <summary>
        /// Output neuron values
        /// </summary>
        public float[] Outputs { get; set; }
        
        /// <summary>
        /// Index of the winning action neuron
        /// </summary>
        public int ActionIndex { get; set; }
        
        /// <summary>
        /// Hidden layer activations (for learning)
        /// </summary>
        public float[] HiddenActivations { get; set; }
        
        /// <summary>
        /// Age of this experience in seconds
        /// </summary>
        public float Age => Time.time - Timestamp;

        public static Experience Create(float[] inputs, float[] outputs, int actionIndex, float[] hiddenActivations)
        {
            return new Experience
            {
                Timestamp = Time.time,
                Inputs = (float[])inputs?.Clone(),
                Outputs = (float[])outputs?.Clone(),
                ActionIndex = actionIndex,
                HiddenActivations = hiddenActivations != null ? (float[])hiddenActivations.Clone() : null
            };
        }
    }

    /// <summary>
    /// Ring buffer for storing recent experiences.
    /// Used for credit assignment in reinforcement learning.
    /// Capacity: 10 actions by default
    /// </summary>
    [Serializable]
    public class LearningMemory
    {
        /// <summary>
        /// Maximum number of experiences to store
        /// </summary>
        public int Capacity { get; private set; }
        
        /// <summary>
        /// Current number of stored experiences
        /// </summary>
        public int Count { get; private set; }
        
        /// <summary>
        /// Buffer storage (ring buffer)
        /// </summary>
        private Experience[] buffer;
        
        /// <summary>
        /// Current write position
        /// </summary>
        private int head;
        
        /// <summary>
        /// Oldest valid position
        /// </summary>
        private int tail;
        
        /// <summary>
        /// Maximum age in seconds before an experience is considered stale
        /// </summary>
        public float MaxAgeSeconds { get; set; } = 30f;
        
        /// <summary>
        /// Whether to automatically prune stale experiences
        /// </summary>
        public bool AutoPrune { get; set; } = true;

        public LearningMemory(int capacity = 10)
        {
            if (capacity < 1)
                throw new ArgumentException("Capacity must be at least 1", nameof(capacity));
            
            Capacity = capacity;
            buffer = new Experience[capacity];
            head = 0;
            tail = 0;
            Count = 0;
        }

        /// <summary>
        /// Records a new experience in the buffer
        /// </summary>
        public void Record(float[] inputs, float[] outputs, int actionIndex, float[] hiddenActivations = null)
        {
            // Auto-prune if enabled
            if (AutoPrune) PruneStale();
            
            // Create new experience
            var experience = Experience.Create(inputs, outputs, actionIndex, hiddenActivations);
            
            // Write to buffer
            buffer[head] = experience;
            
            // Advance head
            head = (head + 1) % Capacity;
            
            // If we've wrapped around, advance tail too (overwrite oldest)
            if (Count == Capacity)
            {
                tail = head;
            }
            else
            {
                Count++;
            }
        }

        /// <summary>
        /// Records an experience from an action result
        /// </summary>
        public void RecordAction(float[] inputs, float[] outputs, int actionIndex, NeuralNet neuralNet)
        {
            float[] hiddenActivations = neuralNet?.GetHiddenActivations();
            Record(inputs, outputs, actionIndex, hiddenActivations);
        }

        /// <summary>
        /// Gets the most recent experience
        /// Returns null if buffer is empty
        /// </summary>
        public Experience? GetMostRecent()
        {
            if (Count == 0) return null;
            
            int index = (head - 1 + Capacity) % Capacity;
            return buffer[index];
        }

        /// <summary>
        /// Gets the N most recent experiences (in order from newest to oldest)
        /// </summary>
        public List<Experience> GetRecentExperiences(int count)
        {
            count = Mathf.Min(count, Count);
            var result = new List<Experience>(count);
            
            for (int i = 0; i < count; i++)
            {
                int index = (head - 1 - i + Capacity) % Capacity;
                result.Add(buffer[index]);
            }
            
            return result;
        }

        /// <summary>
        /// Gets all experiences in chronological order (oldest first)
        /// </summary>
        public IEnumerable<Experience> GetAllExperiences()
        {
            if (Count == 0) yield break;
            
            for (int i = 0; i < Count; i++)
            {
                int index = (tail + i) % Capacity;
                yield return buffer[index];
            }
        }

        /// <summary>
        /// Gets all experiences of a specific action type
        /// </summary>
        public List<Experience> GetExperiencesByAction(int actionIndex)
        {
            var result = new List<Experience>();
            
            foreach (var exp in GetAllExperiences())
            {
                if (exp.ActionIndex == actionIndex)
                    result.Add(exp);
            }
            
            return result;
        }

        /// <summary>
        /// Clears all experiences
        /// </summary>
        public void Clear()
        {
            Array.Clear(buffer, 0, buffer.Length);
            head = 0;
            tail = 0;
            Count = 0;
        }

        /// <summary>
        /// Removes experiences that are too old
        /// </summary>
        public void PruneStale()
        {
            float currentTime = Time.time;
            
            while (Count > 0)
            {
                var oldest = buffer[tail];
                if (currentTime - oldest.Timestamp > MaxAgeSeconds)
                {
                    // Remove oldest
                    buffer[tail] = default;
                    tail = (tail + 1) % Capacity;
                    Count--;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Calculates the average reward signal for recent actions
        /// Useful for tracking learning progress
        /// </summary>
        public Dictionary<int, float> GetActionSuccessRates()
        {
            var rates = new Dictionary<int, float>();
            var actionCounts = new Dictionary<int, int>();
            
            foreach (var exp in GetAllExperiences())
            {
                int action = exp.ActionIndex;
                actionCounts[action] = actionCounts.GetValueOrDefault(action) + 1;
            }
            
            foreach (var kvp in actionCounts)
            {
                rates[kvp.Key] = (float)kvp.Value / Count;
            }
            
            return rates;
        }

        /// <summary>
        /// Gets a summary of memory contents for debugging
        /// </summary>
        public string GetDebugSummary()
        {
            var recent = GetRecentExperiences(Mathf.Min(3, Count));
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"LearningMemory: {Count}/{Capacity} entries");
            
            int i = 1;
            foreach (var exp in recent)
            {
                string actionName = ActionSystem.GetActionFromNeuron(exp.ActionIndex).ToString();
                sb.AppendLine($"  [{i}] {actionName} (age: {exp.Age:F1}s)");
                i++;
            }
            
            return sb.ToString();
        }
    }
}
