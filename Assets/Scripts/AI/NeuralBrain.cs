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
        [SerializeField] private int inputSize = 24;
        [SerializeField] private int hiddenSize = 16;
        [SerializeField] private int outputSize = 16;

        [Header("Action Thresholds")]
        [SerializeField] private float actionThreshold = 0.3f;
        [SerializeField] private float moveThreshold = 0.1f;

        [Header("Learning Configuration")]
        [SerializeField] private float learningRate = 0.01f;
        [SerializeField] private float maxWeightDelta = 0.1f;
        [SerializeField] private int memoryCapacity = 10;

        public NeuralNet Network { get; private set; }
        public HebbianLearning Learning { get; private set; }
        public LearningMemory Memory { get; private set; }
        public GenomeData Genome { get; private set; }
        
        public float[] CurrentOutputs { get; private set; }
        public float[] CurrentInputs { get; private set; }
        public int LastWinningAction { get; private set; } = -1;
        
        public SensoryInput Sensory { get; set; }
        
        public event Action<float> OnReward;
        public event Action<float> OnPunishment;
        public event Action<CreatureAction> OnActionExecuted;

        public enum OutputNeuron
        {
            MoveForward = 0, MoveBackward = 1, MoveLeft = 2, MoveRight = 3,
            TurnLeft = 4, TurnRight = 5, LookUp = 6, LookDown = 7,
            Eat = 8, Mate = 9, Attack = 10, Rest = 11,
            Flee = 12, Follow = 13, Explore = 14, Idle = 15
        }

        public CreatureAction CurrentAction { get; private set; }

        public void Initialize(GenomeData genome)
        {
            Genome = genome ?? new GenomeData();
            Network = new NeuralNet(inputSize, hiddenSize, outputSize, Genome);
            Learning = new HebbianLearning { LearningRate = learningRate, MaxWeightDelta = maxWeightDelta };
            Memory = new LearningMemory(capacity: memoryCapacity);
            CurrentOutputs = new float[outputSize];
            CurrentInputs = new float[inputSize];
        }

        public void ProcessFrame()
        {
            if (Network == null) return;
            GatherSensoryInputs();
            CurrentOutputs = Network.Forward(CurrentInputs);
            int winningAction = GetWinningAction();
            if (winningAction >= 0)
            {
                Memory.RecordAction(CurrentInputs, CurrentOutputs, winningAction, Network);
                LastWinningAction = winningAction;
            }
            ExecuteAction();
        }

        protected void GatherSensoryInputs()
        {
            if (Sensory == null)
            {
                Array.Clear(CurrentInputs, 0, inputSize);
                return;
            }
            int idx = 0;
            CurrentInputs[idx++] = Sensory.Hunger;
            CurrentInputs[idx++] = Sensory.Energy;
            CurrentInputs[idx++] = Sensory.Fear;
            CurrentInputs[idx++] = Sensory.Pain;
            CurrentInputs[idx++] = Sensory.Curiosity;
            CurrentInputs[idx++] = Sensory.Comfort;
            CurrentInputs[idx++] = Sensory.CanSeeFood ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.FoodDistance;
            CurrentInputs[idx++] = Sensory.CanSeeThreat ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.ThreatDistance;
            CurrentInputs[idx++] = Sensory.CanSeeCreature ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.NearestCreatureDistance;
            CurrentInputs[idx++] = Sensory.WallInFront ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.WallToLeft ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.WallToRight ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.ObstacleDetected ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.CanEat ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.CanMate ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.CanRest ? 1f : 0f;
            CurrentInputs[idx++] = Sensory.TimeSinceLastAction;
            CurrentInputs[idx++] = Sensory.NearbyCreaturesCount;
            CurrentInputs[idx++] = Sensory.Health;
            CurrentInputs[idx++] = Sensory.AgeNormalized;
            CurrentInputs[idx++] = UnityEngine.Random.value;
        }

        protected int GetWinningAction()
        {
            int bestAction = -1;
            float bestActivation = actionThreshold;
            for (int i = (int)OutputNeuron.Eat; i < outputSize; i++)
            {
                if (CurrentOutputs[i] > bestActivation)
                {
                    bestActivation = CurrentOutputs[i];
                    bestAction = i;
                }
            }
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

        protected void ExecuteAction()
        {
            CreatureAction action;
            if (CurrentOutputs[(int)OutputNeuron.Eat] > actionThreshold && CanPerformAction(CreatureAction.Eat))
                action = CreatureAction.Eat;
            else if (CurrentOutputs[(int)OutputNeuron.Mate] > actionThreshold && CanPerformAction(CreatureAction.Mate))
                action = CreatureAction.Mate;
            else if (CurrentOutputs[(int)OutputNeuron.Attack] > actionThreshold && CanPerformAction(CreatureAction.Attack))
                action = CreatureAction.Attack;
            else if (CurrentOutputs[(int)OutputNeuron.Rest] > actionThreshold && CanPerformAction(CreatureAction.Rest))
                action = CreatureAction.Rest;
            else if (CurrentOutputs[(int)OutputNeuron.Flee] > actionThreshold)
                action = CreatureAction.Flee;
            else
                action = CreatureAction.Move;

            CurrentAction = action;
            OnActionExecuted?.Invoke(action);
            ExecuteMovement();
            ExecuteDiscreteAction(action);
        }

        protected void ExecuteMovement()
        {
            float forward = CurrentOutputs[(int)OutputNeuron.MoveForward];
            float backward = CurrentOutputs[(int)OutputNeuron.MoveBackward];
            float left = CurrentOutputs[(int)OutputNeuron.MoveLeft];
            float right = CurrentOutputs[(int)OutputNeuron.MoveRight];

            float moveZ = forward - backward;
            float moveX = right - left;

            Vector3 moveDirection = new Vector3(moveX, 0, moveZ);
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection.Normalize();
                transform.position += moveDirection * Time.deltaTime * 2f;
            }

            float turnLeft = CurrentOutputs[(int)OutputNeuron.TurnLeft];
            float turnRight = CurrentOutputs[(int)OutputNeuron.TurnRight];
            float rotation = (turnRight - turnLeft) * 90f * Time.deltaTime;
            transform.Rotate(0, rotation, 0);
        }

        protected void ExecuteDiscreteAction(CreatureAction action)
        {
            switch (action)
            {
                case CreatureAction.Eat: PerformEat(); break;
                case CreatureAction.Mate: PerformMate(); break;
                case CreatureAction.Attack: PerformAttack(); break;
                case CreatureAction.Rest: PerformRest(); break;
                case CreatureAction.Flee: PerformFlee(); break;
            }
        }

        protected bool CanPerformAction(CreatureAction action)
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

        protected virtual void PerformEat() { }
        protected virtual void PerformMate() { }
        protected virtual void PerformAttack() { }
        protected virtual void PerformRest() { }
        protected virtual void PerformFlee() { }

        public void TriggerReward(float amount)
        {
            if (Learning == null || Memory == null || Memory.Count == 0) return;
            Learning.LearnFromMemory(Network, Memory, amount);
            OnReward?.Invoke(amount);
        }

        public void TriggerPunishment(float amount)
        {
            if (Learning == null || Memory == null || Memory.Count == 0) return;
            Learning.LearnFromMemory(Network, Memory, -amount);
            OnPunishment?.Invoke(-amount);
        }

        public void SaveToGenome() => Network?.SaveWeightsToGenome();

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
                    if (CurrentOutputs[i] > 0.3f)
                        sb.AppendLine($"  {(OutputNeuron)i}: {CurrentOutputs[i]:F2}");
            }
            sb.AppendLine($"Memory: {Memory?.GetDebugSummary() ?? "N/A"}");
            return sb.ToString();
        }
    }

    public enum CreatureAction
    {
        Move, Eat, Mate, Attack, Rest, Flee
    }
}
