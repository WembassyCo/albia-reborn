using UnityEngine;

namespace AlbiaReborn.Creatures.Neural
{
    /// <summary>
    /// Hebbian reinforcement learning.
    /// Reward strengthens recent pathways, pain weakens them.
    /// </summary>
    public class HebbianLearning
    {
        private NeuralNet _net;
        private float _learningRate;
        private float _maxDelta = 0.05f;
        
        // Action history for learning
        private ActionHistoryEntry[] _actionHistory;
        private int _historyIndex;
        private const int HistorySize = 10;

        public HebbianLearning(NeuralNet net, float learningRate)
        {
            _net = net;
            _learningRate = learningRate;
            _actionHistory = new ActionHistoryEntry[HistorySize];
        }

        /// <summary>
        /// Record an action for potential learning.
        /// </summary>
        public void RecordAction(int actionIndex, float[] inputs)
        {
            _actionHistory[_historyIndex] = new ActionHistoryEntry
            {
                ActionIndex = actionIndex,
                Inputs = inputs,
                Timestamp = Time.time
            };
            _historyIndex = (_historyIndex + 1) % HistorySize;
        }

        /// <summary>
        /// Learn from reward signal.
        /// Strengthen pathways that produced recent winning actions.
        /// </summary>
        public void LearnFromReward(float rewardMagnitude)
        {
            if (rewardMagnitude <= 0.001f) return;

            float now = Time.time;
            float deltaBase = _learningRate * rewardMagnitude;

            foreach (var entry in _actionHistory)
            {
                if (entry == null) continue;

                float timeDelta = now - entry.Timestamp;
                if (timeDelta > 5f) continue; // Only recent actions

                // Decay influence by time
                float timeDecay = 1f - (timeDelta / 5f);
                float delta = deltaBase * timeDecay * _maxDelta;

                // Strengthen connections that led to this action
                StrengthenPathway(entry.ActionIndex, entry.Inputs, delta);
            }
        }

        /// <summary>
        /// Learn from pain signal.
        /// Weaken pathways that produced actions before pain.
        /// </summary>
        public void LearnFromPain(float painMagnitude)
        {
            if (painMagnitude <= 0.001f) return;

            float now = Time.time;
            float deltaBase = _learningRate * painMagnitude;

            foreach (var entry in _actionHistory)
            {
                if (entry == null) continue;

                float timeDelta = now - entry.Timestamp;
                if (timeDelta > 3f) continue;

                float timeDecay = 1f - (timeDelta / 3f);
                float delta = -deltaBase * timeDecay * _maxDelta; // Negative = weaken

                WeakenPathway(entry.ActionIndex, entry.Inputs, delta);
            }
        }

        /// <summary>
        /// Strengthen weights connecting active inputs to output.
        /// </summary>
        private void StrengthenPathway(int actionIndex, float[] inputs, float delta)
        {
            var (_, hidden, outputs) = _net.GetRecentActivations();
            
            // Strengthen hidden-output weights for winning output
            for (int h = 0; h < _net.HiddenCount; h++)
            {
                float activationDelta = delta * hidden[h];
                _net.UpdateWeight(1, h, actionIndex, activationDelta);
            }

            // Strengthen input-hidden weights
            for (int i = 0; i < _net.InputCount; i++)
            {
                if (inputs[i] > 0.1f) // Only strong inputs
                {
                    for (int h = 0; h < _net.HiddenCount; h++)
                    {
                        float activationDelta = delta * inputs[i] * 0.1f;
                        _net.UpdateWeight(0, i, h, activationDelta);
                    }
                }
            }
        }

        private void WeakenPathway(int actionIndex, float[] inputs, float delta)
        {
            // Same as strengthen but with negative delta
            var (_, hidden, outputs) = _net.GetRecentActivations();
            
            for (int h = 0; h < _net.HiddenCount; h++)
            {
                float activationDelta = delta * hidden[h];
                _net.UpdateWeight(1, h, actionIndex, activationDelta);
            }
        }

        private class ActionHistoryEntry
        {
            public int ActionIndex;
            public float[] Inputs;
            public float Timestamp;
        }
    }
}
