using System;
using System.Collections.Generic;
using UnityEngine;
using Albia.Creatures.Neural;

namespace Albia.AI
{
    /// <summary>
    /// Bridge component that connects NeuralNet outputs to creature actions.
    /// Maps 16 neural outputs to physical behaviors with learning feedback.
    /// Implements INeuralBrain for loose coupling with Creatures assembly.
    /// </summary>
    [Serializable]
    public class NeuralBrain : MonoBehaviour, Albia.Creatures.INeuralBrain
    {
        [Header("Network Configuration")]
        [Tooltip("Size of sensory input layer (24-32 typical)")]
        [SerializeField] private int inputSize = 24;
        
        [Tooltip("Size of hidden layer")]
        [SerializeField] private int hiddenSize = 16;
        
        [Tooltip("Size of output layer (16 actions)")]
        [SerializeField] private int outputSize = 16;

        [Header("Action Thresholds")]
        [Tooltip("Minimum activation to trigger an action")]
        [SerializeField] private float actionThreshold = 0.3f;
        
        [Tooltip("Activation threshold for movement outputs")]
        [SerializeField] private float moveThreshold = 0.1f;

        [Header("Learning Configuration")]
        [SerializeField] private float learningRate = 0.01f;
        [SerializeField] private float maxWeightDelta = 0.1f;
        [SerializeField] private int memoryCapacity = 10;

        // Neural network core
        public NeuralNet Network { get; private set; }
        
        // Learning system
        public HebbianLearning Learning { get; private set; }
        
        // Action memory for credit assignment
        public LearningMemory Memory { get; private set; }
        
        // Genome reference
        public GenomeData Genome { get; private set; }
        
        // Current outputs cached
        public float[] CurrentOutputs { get; private set; }
        public float[] CurrentInputs { get; private set; }
        public int LastWinningAction { get; private set; } = -1;
        
        // Sensory input system (dependency injected)
        public SensoryInput Sensory { get; set; }
        
        // Events for learning signals
        public event Action<float> OnReward;
        public event Action<float> OnPunishment;
        public event Action<CreatureAction> OnActionExecuted;

        // Output neuron mapping (16 outputs):
        // 0-3:   Movement (forward, backward, left, right)
        // 4-7:   Rotation (turn left, turn right, look up, look down) 
        // 8-11:  Actions (eat, mate, attack, rest)
        // 12-15: Social/Context (flee, follow, explore, idle)
        public enum OutputNeuron
        {
            MoveForward = 0,
            MoveBackward = 1,
            MoveLeft = 2,
            MoveRight = 3,
            TurnLeft = 4,
            TurnRight = 5,
            LookUp = 6,
            LookDown = 7,
            Eat = 8,
            Mate = 9,
            Attack = 10,
            Rest = 11,
            Flee = 12,
            Follow = 13,
            Explore = 14,
            Idle = 15
        }

        /// <summary>
        /// Current action being performed
        /// </summary>
        public CreatureAction CurrentAction { get; private set; }

        /// <summary>
        /// Initialize the neural brain with a genome
        /// </summary>
        public void Initialize(GenomeData genome)
        {
            Genome = genome ?? new GenomeData();
            Network = new NeuralNet(inputSize, hiddenSize, outputSize, Genome);
            Learning = new HebbianLearning
            {
                LearningRate = learningRate,
                MaxWeightDelta = maxWeightDelta
            };
            Memory = new LearningMemory(capacity: memoryCapacity);
            CurrentOutputs = new float[outputSize];
            CurrentInputs = new float[inputSize];
            
            Debug.Log($"[NeuralBrain] Initialized with {inputSize} inputs, {hiddenSize} hidden, {outputSize} outputs");
        }

        /// <summary>
        /// Process one frame: gather inputs, run network, execute action
        /// </summary>
        public void ProcessFrame()
        {
            if (Network == null)
            {
                Debug.LogWarning("[NeuralBrain] Network not initialized");
                return;
            }

            // Gather sensory inputs
            GatherSensoryInputs();
            
            // Run neural network forward pass
            CurrentOutputs = Network.Forward(CurrentInputs);
            
            // Record experience for learning
            int winningAction = GetWinningAction();
            if (winningAction >= 0)
            {
                Memory.RecordAction(CurrentInputs, CurrentOutputs, winningAction, Network);
                LastWinningAction = winningAction;
            }
            
            // Execute the highest-activation action
            ExecuteAction();
        }

        /// <summary>
        /// Gather sensory inputs from environment
        /// 24 sensory inputs: chemical (6) + vision (6) + proximity (4) + state (8)
        /// </summary>
        protected virtual void GatherSensoryInputs()
        {
            if (Sensory == null)
            {
                // Fallback to zeros if no sensory system attached
                Array.Clear(CurrentInputs, 0, inputSize);
                return;
            }

            int idx = 0;
            
            // Chemical inputs (6): hunger, energy, fear, pain, curiosity, comfort
            CurrentInputs[idx++] = Sensory.Hunger;
            CurrentInputs[idx++] = Sensory.Energy;
            CurrentInputs[idx++] = Sensory.Fear;
            CurrentInputs[idx++] = Sensory.Pain;
            CurrentInputs[idx++] = Sensory.Curiosity;
            CurrentInputs[idx++] = Sensory.Comfort;
            
            // Vision inputs (6): food seen, food distance, threat seen, threat distance, 
            //                   creature seen, nearest creature distance
            CurrentInputs[idx++] = Sensory.CanSeeFood ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.FoodDistance;
            CurrentInputs[idx++] = Sensory.CanSeeThreat ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.ThreatDistance;
            CurrentInputs[idx++] = Sensory.CanSeeCreature ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.NearestCreatureDistance;
            
            // Proximity inputs (4): wall front, wall left, wall right, obstacle detected
            CurrentInputs[idx++] = Sensory.WallInFront ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.WallToLeft ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.WallToRight ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.ObstacleDetected ? 1f : 0f;
            
            // State inputs (8): can eat, can mate, can rest, time since last action,
            //                   nearby creatures count, health, age normalized, random/novelty
            CurrentInputs[idx++] = Sensory.CanEat ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.CanMate ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.CanRest ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.TimeSinceLastAction;
            CurrentInputs[idx++] = Sensory.NearbyCreaturesCount;
            CurrentInputs[idx++] = Sensory.Health;
            CurrentInputs[idx++] = Sensory.AgeNormalized;
            CurrentInputs[idx++] = UnityEngine.Random.value; // Novelty/dither input
        }

        /// <summary>
        /// Get the winning action from neural outputs
        /// Returns -1 if no action exceeds threshold
        /// </summary>
        protected int GetWinningAction()
        {
            int bestAction = -1;
            float bestActivation = actionThreshold;
            
            // Check action neurons (8-15) first
            for (int i = (int)OutputNeuron.Eat; i < outputSize; i++)
            {
                if (CurrentOutputs[i] > bestActivation)
                {
                    bestActivation = CurrentOutputs[i];
                    bestAction = i;
                }
            }
            
            // Also consider movement as action if strong enough
            if (bestAction < 0)
            {
                for (int i = 0; i < (int)OutputNeuron.Eat; i++)
                {
                    if (CurrentOutputs[i] > bestActivation)
                    {
                        bestActivation = CurrentOutputs[i];
                        bestAction = i;
                    }
                }
            }
            
            return bestAction;
        }

        /// <summary>
        /// Execute the currently selected action
        /// </summary>
        protected virtual void ExecuteAction()
        {
            // Determine action type from outputs
            CreatureAction action;
            OutputNeuron primaryNeuron;
            
            // Check for discrete actions first (Eat, Mate, Attack, Rest)
            if (CurrentOutputs[(int)OutputNeuron.Eat] > actionThreshold && CanPerformAction(CreatureAction.Eat))
            {
                action = CreatureAction.Eat;
                primaryNeuron = OutputNeuron.Eat;
            }
            else if (CurrentOutputs[(int)OutputNeuron.Mate] > actionThreshold && CanPerformAction(CreatureAction.Mate))
            {
                action = CreatureAction.Mate;
                primaryNeuron = OutputNeuron.Mate;
            }
            else if (CurrentOutputs[(int)OutputNeuron.Attack] > actionThreshold && CanPerformAction(CreatureAction.Attack))
            {
                action = CreatureAction.Attack;
                primaryNeuron = OutputNeuron.Attack;
            }
            else if (CurrentOutputs[(int)OutputNeuron.Rest] > actionThreshold && CanPerformAction(CreatureAction.Rest))
            {
                action = CreatureAction.Rest;
                primaryNeuron = OutputNeuron.Rest;
            }
            else if (CurrentOutputs[(int)OutputNeuron.Flee] > actionThreshold)
            {
                action = CreatureAction.Flee;
                primaryNeuron = OutputNeuron.Flee;
            }
            else
            {
                // Default to movement based on strongest movement neuron
                action = CreatureAction.Move;
                primaryNeuron = OutputNeuron.MoveForward;
            }
            
            CurrentAction = action;
            OnActionExecuted?.Invoke(action);
            
            // Execute the specific action logic
            ExecuteMovement();
            ExecuteDiscreteAction(action);
        }

        /// <summary>
        /// Execute movement based on movement neuron outputs
        /// </summary>
        protected virtual void ExecuteMovement()
        {
            // Calculate movement vector from outputs 0-3
            float forward = CurrentOutputs[(int)OutputNeuron.MoveForward];
            float backward = CurrentOutputs[(int)OutputNeuron.MoveBackward];
            float left = CurrentOutputs[(int)OutputNeuron.MoveLeft];
            float right = CurrentOutputs[(int)OutputNeuron.MoveRight];
            
            // Apply threshold
            if (Mathf.Abs(forward) < moveThreshold && Mathf.Abs(backward) < moveThreshold &&
                Mathf.Abs(left) < moveThreshold && Mathf.Abs(right) < moveThreshold)
            {
                return; // No significant movement
            }
            
            // Net movement
            float moveZ = forward - backward;
            float moveX = right - left;
            
            // Apply movement to transform
            Vector3 moveDirection = new Vector3(moveX, 0, moveZ);
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection.Normalize();
                transform.position += moveDirection * Time.deltaTime * 2f;
            }
            
            // Rotation from outputs 4-5
            float turnLeft = CurrentOutputs[(int)OutputNeuron.TurnLeft];
            float turnRight = CurrentOutputs[(int)OutputNeuron.TurnRight];
            float rotation = (turnRight - turnLeft) * 90f * Time.deltaTime;
            transform.Rotate(0, rotation, 0);
        }

        /// <summary>
        /// Execute discrete actions (Eat, Mate, Attack, Rest, etc.)
        /// </summary>
        protected virtual void ExecuteDiscreteAction(CreatureAction action)
        {
            switch (action)
            {
                case CreatureAction.Eat:
                    PerformEat();
                    break;
                case CreatureAction.Mate:
                    PerformMate();
                    break;
                case CreatureAction.Attack:
                    PerformAttack();
                    break;
                case CreatureAction.Rest:
                    PerformRest();
                    break;
                case CreatureAction.Flee:
                    PerformFlee();
                    break;
            }
        }

        /// <summary>
        /// Check if creature can perform this action
        /// </summary>
        protected virtual bool CanPerformAction(CreatureAction action)
        {
            if (Sensory == null) return false;
            
            return action switch
            {
                CreatureAction.Eat => Sensory.CanEat,
                CreatureAction.Mate => Sensory.CanMate,
                CreatureAction.Attack => Sensory.CanAttack,
                CreatureAction.Rest => Sensory.CanRest,
                _ => true
            };
        }

        // Action implementations (override in derived classes or wire to other systems)
        protected virtual void PerformEat() 
        {
            Debug.Log("[NeuralBrain] Executing: Eat");
            // Will trigger reward through feedback
        }
        
        protected virtual void PerformMate() 
        {
            Debug.Log("[NeuralBrain] Executing: Mate");
        }
        
        protected virtual void PerformAttack() 
        {
            Debug.Log("[NeuralBrain] Executing: Attack");
        }
        protected virtual void PerformRest() 
        {
            Debug.Log("[NeuralBrain] Executing: Rest");
        }
        
        protected virtual void PerformFlee()
        {
            Debug.Log("[NeuralBrain] Executing: Flee");
            // Move away from threat
        }

        /// <summary>
        /// Trigger reward signal - strengthens recent pathways
        /// Call this when creature successfully eats, mates, finds food, etc.
        /// </summary>
        public void TriggerReward(float amount)
        {
            if (Learning == null || Memory == null || Memory.Count == 0) return;
            
            Learning.LearnFromMemory(Network, Memory, amount);
            OnReward?.Invoke(amount);
            
            Debug.Log($"[NeuralBrain] Reward triggered: {amount:F3}");
        }

        /// <summary>
        /// Trigger punishment signal - weakens recent pathways
        /// Call this when creature takes damage, starves, hits wall, etc.
        /// </summary>
        public void TriggerPunishment(float amount)
        {
            if (Learning == null || Memory == null || Memory.Count == 0) return;
            
            Learning.LearnFromMemory(Network, Memory, -amount);
            OnPunishment?.Invoke(-amount);
            
            Debug.Log($"[NeuralBrain] Punishment triggered: {amount:F3}");
        }

        /// <summary>
        /// Save learned weights back to genome
        /// Call before saving creature or reproduction
        /// </summary>
        public void SaveToGenome()
        {
            Network?.SaveWeightsToGenome();
        }

        /// <summary>
        /// Get debug information
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== NeuralBrain ===");
            sb.AppendLine($"Current Action: {CurrentAction}");
            sb.AppendLine($"Learning Rate: {Learning?.CurrentLearningRate:F4}");
            
            if (CurrentOutputs != null)
            {
                sb.AppendLine("Top Outputs:");
                for (int i = 0; i < outputSize; i++)
                {
                    if (CurrentOutputs[i] > 0.3f)
                    {
                        sb.AppendLine($"  {(OutputNeuron)i}: {CurrentOutputs[i]:F2}");
                    }
                }
            }
            
            sb.AppendLine($"Memory: {Memory?.GetDebugSummary() ?? "N/A"}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Actions that can be performed by creatures
    /// </summary>
    public enum CreatureAction
    {
        Move,
        Eat,
        Mate,
        Attack,
        Rest,
        Flee
    }
}
