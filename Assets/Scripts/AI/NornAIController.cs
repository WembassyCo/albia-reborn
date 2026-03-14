using System;
using System.Collections.Generic;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.AI
{
    /// <summary>
    /// Neural-driven AI controller for Norn creatures.
    /// Replaces traditional hunger state machine with brain-driven behavior.
    /// Processes neural outputs every frame, executes actions, and emits learning rewards.
    /// Implements INornAIController for loose coupling with Creatures assembly.
    /// </summary>
    [RequireComponent(typeof(NeuralBrain))]
    [RequireComponent(typeof(SensoryInput))]
    public class NornAIController : MonoBehaviour, Albia.Creatures.INornAIController
    {
        [Header("References")]
        [Tooltip("Reference to the NeuralBrain component (auto-assigned if null)")]
        [SerializeField] private NeuralBrain brain;
        
        [Tooltip("Reference to SensoryInput component (auto-assigned if null)")]
        [SerializeField] private SensoryInput sensory;
        
        [Tooltip("Reference to creature state")]
        [SerializeField] private CreatureState state;

        [Header("Neural Configuration")]
        [Tooltip("Input size - must match NeuralBrain config")]
        [SerializeField] private int inputSize = 24;
        
        [Tooltip("Hidden layer size")]
        [SerializeField] private int hiddenSize = 16;
        
        [Tooltip("Output size - must match action count")]
        [SerializeField] private int outputSize = 16;

        [Header("Learning Rewards")]
        [Tooltip("Reward for eating food")]
        [SerializeField] private float eatReward = 0.5f;
        
        [Tooltip("Punishment for taking damage")]
        [SerializeField] private float damagePunishment = 0.8f;
        
        [Tooltip("Small punishment for energy consumption")]
        [SerializeField] private float energyCostPunishment = 0.05f;
        
        [Tooltip("Reward for successful mating")]
        [SerializeField] private float mateReward = 1.0f;
        
        [Tooltip("Reward for finding food")]
        [SerializeField] private float findFoodReward = 0.2f;

        [Header("State Machine Override")]
        [Tooltip("If true, neural brain completely replaces state machine")]
        [SerializeField] private bool neuralOverride = true;
        
        [Tooltip("If neuralOverride is false, blend neural and state machine decisions")]
        [SerializeField] [Range(0f, 1f)] private float neuralBlendWeight = 1f;

        // Norn reference for integration
        private Norn nornCreature;
        
        // State tracking for reward emission
        private float lastHunger;
        private float lastHealth;
        private float lastEnergy;
        private float totalDistanceTraveled;
        private Vector3 lastPosition;
        private bool isInitialized = false;
        
        // Action cooldowns
        private Dictionary<CreatureAction, float> actionCooldowns = new();
        private const float DEFAULT_COOLDOWN = 1f;

        // Events
        public event Action<CreatureAction> OnActionStarted;
        public event Action<CreatureAction> OnActionCompleted;
        public event Action<float> OnRewardEmitted;
        public event Action<float> OnPunishmentEmitted;

        // Public accessors
        public NeuralBrain Brain => brain;
        public SensoryInput Sensory => sensory;
        public CreatureState State => state;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            // Auto-assign required components
            if (brain == null) brain = GetComponent<NeuralBrain>();
            if (sensory == null) sensory = GetComponent<SensoryInput>();
            if (state == null) state = new CreatureState();
            
            // Try to get Norn component for integration
            nornCreature = GetComponent<Norn>();
            
            lastPosition = transform.position;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the neural brain with genome
        /// Can be called with specific genome for reproduction
        /// </summary>
        public void Initialize(GenomeData genome = null)
        {
            if (isInitialized) return;
            
            // Create genome if not provided (for new creatures)
            genome ??= new GenomeData();
            
            // Initialize brain
            brain ??= gameObject.AddComponent<NeuralBrain>();
            brain.Initialize(genome);
            
            // Wire sensory system to brain
            sensory ??= gameObject.AddComponent<SensoryInput>();
            sensory.creatureState = state;
            brain.Sensory = sensory;
            
            // Subscribe to brain events for learning
            brain.OnActionExecuted += HandleActionExecuted;
            
            // Initialize state tracking
            lastHunger = state.Hunger;
            lastHealth = state.Health;
            lastEnergy = state.Energy;
            totalDistanceTraveled = 0f;
            
            isInitialized = true;
            
            Debug.Log($"[NornAIController] Initialized on {gameObject.name}");
        }

        private void Update()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[NornAIController] Update called before initialization");
                return;
            }
            
            // Update cooldowns
            UpdateCooldowns();
            
            // Update creature state from Norn if available
            SyncStateFromNorn();
            
            // Main neural processing loop
            ProcessFrame();
            
            // Check for chemical state changes that emit rewards
            CheckChemicalChanges();
            
            // Track distance for movement rewards/punishment
            TrackMovement();
        }

        /// <summary>
        /// Process one frame of neural decision making
        /// </summary>
        protected void ProcessFrame()
        {
            // Update sensory data
            sensory.UpdateSensoryData();
            
            // Let brain process and execute action
            brain.ProcessFrame();
        }

        /// <summary>
        /// Sync state between Norn component and AI controller
        /// </summary>
        private void SyncStateFromNorn()
        {
            if (nornCreature != null)
            {
                // Sync from Norn to controller state
                var nornState = nornCreature.State;
                state.Health = nornState.Health;
                state.Energy = nornState.Energy;
                state.Hunger = nornState.Hunger;
                state.Age = nornState.Age;
            }
            else
            {
                // Update age
                state.Age += Time.deltaTime;
            }
        }

        /// <summary>
        /// Check for chemical state changes and emit rewards
        /// </summary>
        private void CheckChemicalChanges()
        {
            // Hunger decrease = reward (ate food)
            float hungerDelta = state.Hunger - lastHunger;
            if (hungerDelta < -0.1f)
            {
                EmitReward(eatReward * Mathf.Abs(hungerDelta));
                Debug.Log("[NornAIController] Reward: Hunger decreased (ate)");
            }
            
            // Health decrease = punishment (took damage)
            float healthDelta = lastHealth - state.Health;
            if (healthDelta > 0.01f)
            {
                EmitPunishment(damagePunishment * healthDelta);
                Debug.Log("[NornAIController] Punishment: Health decreased");
            }
            
            // Energy decrease = small punishment (inefficiency)
            float energyDelta = lastEnergy - state.Energy;
            if (energyDelta > 0.01f)
            {
                // Only punish large drops from actions, not passive drain
                if (energyDelta > 0.1f)
                {
                    EmitPunishment(energyCostPunishment * energyDelta);
                }
            }
            
            // Food proximity = small reward (found food)
            if (sensory.CanSeeFood && !sensory.CanSeeFood)
            {
                EmitReward(findFoodReward);
                Debug.Log("[NornAIController] Reward: Found food");
            }
            
            // Store current values for next frame
            lastHunger = state.Hunger;
            lastHealth = state.Health;
            lastEnergy = state.Energy;
        }

        /// <summary>
        /// Track movement for energy punishment
        /// </summary>
        private void TrackMovement()
        {
            float distance = Vector3.Distance(transform.position, lastPosition);
            totalDistanceTraveled += distance;
            
            // Energy cost for movement
            if (distance > 0.01f)
            {
                state.Energy = Mathf.Clamp01(state.Energy - distance * 0.01f);
            }
            
            // Hunger increases over time
            state.Hunger = Mathf.Clamp01(state.Hunger + Time.deltaTime * 0.001f);
            
            lastPosition = transform.position;
        }

        /// <summary>
        /// Handle action execution from neural brain
        /// </summary>
        private void HandleActionExecuted(CreatureAction action)
        {
            if (IsOnCooldown(action))
            {
                return;
            }
            
            SetCooldown(action, DEFAULT_COOLDOWN);
            sensory.NotifyActionPerformed(action);
            OnActionStarted?.Invoke(action);
            
            // Execute specific logic
            ExecuteAction(action);
            
            OnActionCompleted?.Invoke(action);
        }

        /// <summary>
        /// Execute the specific action
        /// </summary>
        protected virtual void ExecuteAction(CreatureAction action)
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
                case CreatureAction.Move:
                    // Movement is handled by brain automatically
                    break;
            }
        }

        /// <summary>
        /// Perform eating action
        /// </summary>
        protected virtual void PerformEat()
        {
            // Trigger norn eat if available
            nornCreature?.ExecuteEating(Time.deltaTime);
            
            // Reduce hunger
            state.Hunger = Mathf.Clamp01(state.Hunger - 0.3f);
            
            Debug.Log("[NornAIController] Executed: Eat");
        }

        /// <summary>
        /// Perform mating action
        /// </summary>
        protected virtual void PerformMate()
        {
            // Energy cost
            state.Energy = Mathf.Clamp01(state.Energy - 0.2f);
            
            // Reward for successful mating
            EmitReward(mateReward);
            
            Debug.Log("[NornAIController] Executed: Mate");
        }

        /// <summary>
        /// Perform attack action
        /// </summary>
        protected virtual void PerformAttack()
        {
            // Energy cost
            state.Energy = Mathf.Clamp01(state.Energy - 0.15f);
            
            Debug.Log("[NornAIController] Executed: Attack");
        }

        /// <summary>
        /// Perform rest action
        /// </summary>
        protected virtual void PerformRest()
        {
            // Recover energy and health slowly
            state.Energy = Mathf.Clamp01(state.Energy + Time.deltaTime * 0.1f);
            state.Health = Mathf.Clamp01(state.Health + Time.deltaTime * 0.02f);
            
            Debug.Log("[NornAIController] Executed: Rest");
        }

        /// <summary>
        /// Perform flee action
        /// </summary>
        protected virtual void PerformFlee()
        {
            // Move away from threat if detected
            if (sensory.CanSeeThreat)
            {
                Vector3 fleeDirection = -sensory.ThreatDirection;
                fleeDirection.y = 0;
                transform.position += fleeDirection.normalized * Time.deltaTime * 3f;
            }
            
            // Energy cost for fleeing
            state.Energy = Mathf.Clamp01(state.Energy - Time.deltaTime * 0.05f);
            
            Debug.Log("[NornAIController] Executed: Flee");
        }

        /// <summary>
        /// Emit reward signal to brain
        /// </summary>
        public void EmitReward(float amount)
        {
            brain?.TriggerReward(amount);
            OnRewardEmitted?.Invoke(amount);
            
            // Also notify Norn
            if (nornCreature != null)
            {
                // Access reward through reflection or make public in Norn
                nornCreature.ReceiveRewardSignal(amount);
            }
        }

        /// <summary>
        /// Emit punishment signal to brain
        /// </summary>
        public void EmitPunishment(float amount)
        {
            brain?.TriggerPunishment(amount);
            OnPunishmentEmitted?.Invoke(amount);
            
            // Also notify Norn
            nornCreature?.ReceivePunishmentSignal(amount);
        }

        #region Cooldown Management

        private void UpdateCooldowns()
        {
            var keys = new List<CreatureAction>(actionCooldowns.Keys);
            foreach (var key in keys)
            {
                actionCooldowns[key] -= Time.deltaTime;
                if (actionCooldowns[key] <= 0f)
                {
                    actionCooldowns.Remove(key);
                }
            }
        }

        private bool IsOnCooldown(CreatureAction action)
        {
            return actionCooldowns.ContainsKey(action) && actionCooldowns[action] > 0f;
        }

        private void SetCooldown(CreatureAction action, float cooldown)
        {
            actionCooldowns[action] = cooldown;
        }

        #endregion

        /// <summary>
        /// Apply damage to creature and emit punishment
        /// </summary>
        public void TakeDamage(float amount)
        {
            state.Health = Mathf.Clamp01(state.Health - amount);
            EmitPunishment(damagePunishment * amount);
            
            if (nornCreature != null)
            {
                nornCreature.OnDamage(amount);
            }
        }

        /// <summary>
        /// Heal creature
        /// </summary>
        public void Heal(float amount)
        {
            state.Health = Mathf.Clamp01(state.Health + amount);
        }

        /// <summary>
        /// Feed creature
        /// </summary>
        public void Feed(float amount)
        {
            state.Hunger = Mathf.Clamp01(state.Hunger - amount);
            EmitReward(eatReward);
        }

        /// <summary>
        /// Get debug information
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== NornAIController ===");
            sb.AppendLine($"State: Health={state.Health:F2} Energy={state.Energy:F2} Hunger={state.Hunger:F2}");
            sb.AppendLine($"Age: {state.Age:F1}s Normalized: {state.NormalizedAge:F2}");
            sb.AppendLine($"Active Cooldowns: {actionCooldowns.Count}");
            
            if (brain != null)
            {
                sb.AppendLine(brain.GetDebugInfo());
            }
            
            if (sensory != null)
            {
                sb.AppendLine(sensory.GetDebugInfo());
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Save neural weights back to genome before reproduction
        /// </summary>
        public GenomeData SaveToGenome()
        {
            brain?.SaveToGenome();
            return brain?.Genome;
        }
    }
}
