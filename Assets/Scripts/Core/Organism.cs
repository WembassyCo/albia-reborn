using System;
using UnityEngine;
using UnityEngine.AI;

namespace Albia.Core
{
    /// <summary>
    /// Base class for all living things in Albia.
    /// MVP: Energy + Lifecycle + Basic Navigation
    /// Full: Will add Genome, ChemicalState, NeuralNet
    /// </summary>
    public abstract class Organism : MonoBehaviour
    {
        [Header("Energy System")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float currentEnergy = 80f;
        [SerializeField] private float metabolismRate = 0.1f;

        [Header("Lifecycle")]
        [SerializeField] private LifecycleStage stage = LifecycleStage.Juvenile;
        [SerializeField] private float age = 0f;
        [SerializeField] private float agePerSecond = 0.01f;

        // Core components
        protected NavMeshAgent Agent { get; private set; }
        
        // State machine (MVP: simple, Full: neural-driven)
        protected virtual OrganismState CurrentState { get; set; } = OrganismState.Idle;
        
        // Events (for UI, ecology system, population tracking)
        public event Action<Organism> OnDeath;
        public event Action<float> OnEnergyChanged;

        // Properties
        public float Energy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public bool IsAlive => currentEnergy > 0;
        public LifecycleStage Stage => stage;
        public float Age => age;

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            if (Agent == null)
            {
                Agent = gameObject.AddComponent<NavMeshAgent>();
                Agent.speed = 3.5f;
                Agent.acceleration = 8f;
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive) return;

            // Energy metabolism
            currentEnergy -= metabolismRate * Time.deltaTime;
            
            if (currentEnergy <= 0)
            {
                Die();
            }

            // Aging
            age += agePerSecond * Time.deltaTime;
            UpdateLifecycleStage();

            // State machine update
            UpdateState();
        }

        /// <summary>
        /// Called every frame to execute current state behavior
        /// MVP: Simple switch statement
        /// Full: Neural network drives behavior
        /// </summary>
        protected virtual void UpdateState()
        {
            switch (CurrentState)
            {
                case OrganismState.Idle:
                    OnIdle();
                    break;
                case OrganismState.SeekingFood:
                    OnSeekingFood();
                    break;
                case OrganismState.Eating:
                    OnEating();
                    break;
                case OrganismState.MovingRandom:
                    OnMovingRandom();
                    break;
            }
        }

        // Virtual methods for state behaviors - override in derived classes
        protected virtual void OnIdle() { }
        protected virtual void OnSeekingFood() { }
        protected virtual void OnEating() { }
        protected virtual void OnMovingRandom() { }

        /// <summary>
        /// Try to consume energy from food source
        /// </summary>
        public virtual void ConsumeEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy);
        }

        /// <summary>
        /// Move to target position using NavMesh
        /// </summary>
        public void MoveTo(Vector3 destination)
        {
            if (Agent.isActiveAndEnabled && Agent.isOnNavMesh)
            {
                Agent.SetDestination(destination);
            }
        }

        /// <summary>
        /// Stop movement
        /// </summary>
        public void StopMoving()
        {
            Agent.isStopped = true;
        }

        /// <summary>
        /// Resume movement
        /// </summary>
        public void ResumeMoving()
        {
            Agent.isStopped = false;
        }

        protected virtual void Die()
        {
            currentEnergy = 0;
            OnDeath?.Invoke(this);
            
            // MVP: Just disable, Full: Death event feeds into ecology/decomposer system
            gameObject.SetActive(false);
        }

        private void UpdateLifecycleStage()
        {
            // MVP: Simple age-based
            if (age >= 50f && stage == LifecycleStage.Juvenile)
                stage = LifecycleStage.Adult;
            else if (age >= 150f && stage == LifecycleStage.Adult)
                stage = LifecycleStage.Elder;
        }

        // Scales up: Called by genetics/biochemistry system
        public virtual void SetMetabolismRate(float rate) => metabolismRate = rate;
    }

    public enum LifecycleStage { Juvenile, Adult, Elder }
    public enum OrganismState { Idle, SeekingFood, Eating, MovingRandom }
}