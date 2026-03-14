using System;
using UnityEngine;
using Albia.Creatures.Neural;

namespace Albia.Creatures
{
    /// <summary>
    /// Represents the physical state of a Norn
    /// </summary>
    [Serializable]
    public struct NornState
    {
        public float Health;
        public float Energy;
        public float Hunger;
        public float Age;
        public bool IsAlive;
        
        public void Initialize()
        {
            Health = 1f;
            Energy = 1f;
            Hunger = 0f;
            Age = 0f;
            IsAlive = true;
        }
    }

    /// <summary>
    /// The main creature class with neural network brain.
    /// Integrates sensory input, neural processing, actions, and learning.
    /// </summary>
    [Serializable]
    public class Norn : MonoBehaviour, IActionConditions
    {
        [Header("Neural Configuration")]
        [SerializeField] private int inputSize = 26;   // SensorySystem.TotalInputs
        [SerializeField] private int hiddenSize = 12;  // Hidden layer size
        [SerializeField] private int outputSize = 8;   // ActionSystem.OutputCount
        
        [Header("State")]
        [SerializeField] private NornState state;
        [SerializeField] private GenomeData genome;
        
        [Header("Systems")]
        [SerializeField] private SensorySystem sensorySystem;
        [SerializeField] private ActionSystem actionSystem;
        [SerializeField] private LearningMemory learningMemory;
        
        // Neural brain
        public NeuralNet Brain { get; private set; }
        
        // Learning system
        public HebbianLearning Learning { get; private set; }
        
        // Current action state
        public OrganismState CurrentActionState { get; private set; }
        
        // Events
        public event Action<float> OnRewardSignal;
        public event Action<float> OnPunishmentSignal;
        public event Action OnDeath;
        
        // Properties
        public NornState State => state;
        public GenomeData Genome => genome;
        public bool IsAlive => state.IsAlive;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the Norn with a new genome
        /// </summary>
        public void Initialize(GenomeData initialGenome = null)
        {
            // Create or use provided genome
            genome = initialGenome ?? new GenomeData();
            
            // Initialize brain
            Brain = new NeuralNet(inputSize, hiddenSize, outputSize, genome);
            
            // Initialize sensory system
            sensorySystem = new SensorySystem();
            
            // Initialize action system
            actionSystem = new ActionSystem();
            actionSystem.Conditions = this;
            
            // Initialize learning system
            Learning = new HebbianLearning();
            
            // Initialize memory
            learningMemory = new LearningMemory(capacity: 10);
            
            // Initialize state
            state.Initialize();
            
            // Clear proximity sensors
            sensorySystem.ClearProximity();
        }

        /// <summary>
        /// Main update loop - processes sensory input and updates state via brain
        /// </summary>
        private void Update()
        {
            if (!state.IsAlive) return;
            
            UpdateState(Time.deltaTime);
        }

        /// <summary>
        /// Updates the creature's state using neural network
        /// Replaces the old switch statement with brain-driven behavior
        /// </summary>
        public void UpdateState(float deltaTime)
        {
            if (Brain == null || sensorySystem == null) return;
            
            // Update age
            state.Age += deltaTime;
            
            // Update sensory system with current world data
            UpdateSensoryInputs(deltaTime);
            
            // Assemble inputs for neural network
            float[] inputs = sensorySystem.AssembleInputs();
            
            // Forward pass through brain
            float[] outputs = Brain.Forward(inputs);
            
            // Map outputs to actions
            CurrentActionState = actionSystem.MapOutputs(outputs);
            
            // Record experience for learning
            int winningAction = actionSystem.GetWinningActionIndex();
            if (winningAction >= 0)
            {
                learningMemory.RecordAction(inputs, outputs, winningAction, Brain);
            }
            
            // Execute actions
            ExecuteActions(deltaTime);
            
            // Update biological state
            UpdateBiology(deltaTime);
        }

        /// <summary>
        /// Updates sensory inputs from the environment
        /// </summary>
        private void UpdateSensoryInputs(float deltaTime)
        {
            // Update chemical state
            var chemicals = new ChemicalState
            {
                Hunger = state.Hunger,
                Energy = state.Energy,
                Fear = sensorySystem.Chemicals.Fear, // Carried over from previous
                Sleepiness = 0f, // Placeholder
                Pain = 1f - state.Health,
                Reward = 0f,
                SexDrive = 0f, // Based on age
                Boredom = 0f,
                Curiosity = 0.5f,
                Comfort = state.Energy * (1f - state.Hunger),
                Aggression = 0f,
                Trust = 0.5f
            };
            
            sensorySystem.Chemicals = chemicals;
        }

        /// <summary>
        /// Executes the current action state
        /// </summary>
        private void ExecuteActions(float deltaTime)
        {
            var actionState = CurrentActionState;
            
            // Movement
            if (actionState.MoveForward > 0.1f || 
                actionState.MoveLeft > 0.1f || 
                actionState.MoveRight > 0.1f)
            {
                ExecuteMovement(actionState, deltaTime);
            }
            
            // Actions
            if (actionState.IsEating)
            {
                ExecuteEating(deltaTime);
            }
            else if (actionState.IsResting)
            {
                ExecuteResting(deltaTime);
            }
            else if (actionState.IsReproducing)
            {
                ExecuteReproduction();
            }
            else if (actionState.IsInteracting)
            {
                ExecuteInteraction();
            }
        }

        /// <summary>
        /// Executes movement based on action state
        /// </summary>
        private void ExecuteMovement(OrganismState actionState, float deltaTime)
        {
            Vector2 movement = actionState.GetMovementVector();
            
            // Consume energy for movement
            float energyCost = movement.magnitude * deltaTime * 0.1f;
            ConsumeEnergy(energyCost);
            
            // Apply movement to transform
            if (movement.sqrMagnitude > 0.01f)
            {
                Vector3 direction = new Vector3(movement.x, 0, movement.y);
                transform.position += direction * deltaTime * 2f;
                
                // Apply rotation
                float turn = actionState.TurnAmount * 90f * deltaTime;
                transform.Rotate(0, turn, 0);
            }
        }

        /// <summary>
        /// Executes eating action
        /// </summary>
        private void ExecuteEating(float deltaTime)
        {
            // Reduce hunger
            state.Hunger = Mathf.Max(0, state.Hunger - deltaTime * 0.5f);
            
            // Trigger reward
            if (state.Hunger < 0.3f)
            {
                TriggerRewardSignal(0.5f);
            }
        }

        /// <summary>
        /// Executes resting action
        /// </summary>
        private void ExecuteResting(float deltaTime)
        {
            // Recover energy
            state.Energy = Mathf.Min(1f, state.Energy + deltaTime * 0.3f);
        }

        /// <summary>
        /// Executes interaction action
        /// </summary>
        private void ExecuteInteraction()
        {
            // Placeholder for interaction logic
        }

        /// <summary>
        /// Executes reproduction action
        /// </summary>
        private void ExecuteReproduction()
        {
            // Placeholder for reproduction logic
        }

        /// <summary>
        /// Updates biological processes (hunger, aging, etc.)
        /// </summary>
        private void UpdateBiology(float deltaTime)
        {
            // Increase hunger over time
            state.Hunger = Mathf.Min(1f, state.Hunger + deltaTime * 0.05f);
            
            // Damage from starvation
            if (state.Hunger > 0.9f)
            {
                TakeDamage(deltaTime * 0.1f);
            }
            
            // Check death
            if (state.Health <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Called when energy is consumed - triggers reward signal for energy conservation
        /// </summary>
        public void OnConsumeEnergy(float amount)
        {
            state.Energy = Mathf.Max(0, state.Energy - amount);
            
            // Small punishment for energy consumption (encourages efficiency)
            if (amount > 0.1f)
            {
                TriggerPunishmentSignal(amount * 0.1f);
            }
        }

        private void ConsumeEnergy(float amount)
        {
            OnConsumeEnergy(amount);
        }

        /// <summary>
        /// Called when damage is taken - triggers punishment signal
        /// </summary>
        public void OnDamage(float amount)
        {
            TakeDamage(amount);
        }

        private void TakeDamage(float amount)
        {
            state.Health = Mathf.Max(0, state.Health - amount);
            
            // Trigger punishment
            TriggerPunishmentSignal(amount);
            
            // Increase fear
            var chemicals = sensorySystem.Chemicals;
            chemicals.Fear = Mathf.Min(1f, chemicals.Fear + amount);
            sensorySystem.Chemicals = chemicals;
        }

        private void Die()
        {
            state.IsAlive = false;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Triggers a reward signal for learning
        /// </summary>
        private void TriggerRewardSignal(float amount)
        {
            if (Learning != null && learningMemory.Count > 0)
            {
                Learning.LearnFromMemory(Brain, learningMemory, amount);
            }
            
            OnRewardSignal?.Invoke(amount);
        }

        /// <summary>
        /// Triggers a punishment signal for learning
        /// </summary>
        private void TriggerPunishmentSignal(float amount)
        {
            if (Learning != null && learningMemory.Count > 0)
            {
                Learning.LearnFromMemory(Brain, learningMemory, -amount);
            }
            
            OnPunishmentSignal?.Invoke(-amount);
        }

        /// <summary>
        /// Feeds the creature (for testing or gameplay)
        /// </summary>
        public void Feed(float amount)
        {
            state.Hunger = Mathf.Max(0, state.Hunger - amount);
            TriggerRewardSignal(0.3f);
        }

        /// <summary>
        /// Heals the creature
        /// </summary>
        public void Heal(float amount)
        {
            state.Health = Mathf.Min(1f, state.Health + amount);
        }

        // IActionConditions implementation
        bool IActionConditions.CanEat() => state.Hunger > 0.3f;
        bool IActionConditions.CanReproduce() => state.Energy > 0.7f && state.Age > 10f;
        bool IActionConditions.CanRest() => state.Energy < 1f;

        /// <summary>
        /// Gets debug information
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Norn Debug ===");
            sb.AppendLine($"Health: {state.Health:F2}");
            sb.AppendLine($"Energy: {state.Energy:F2}");
            sb.AppendLine($"Hunger: {state.Hunger:F2}");
            sb.AppendLine($"Age: {state.Age:F1}");
            sb.AppendLine($"Action: {actionSystem?.GetActionDescription() ?? "N/A"}");
            sb.AppendLine($"Learning Rate: {Learning?.CurrentLearningRate:F4}");
            sb.AppendLine($"Memory: {learningMemory?.GetDebugSummary() ?? "N/A"}");
            return sb.ToString();
        }
    }
}
