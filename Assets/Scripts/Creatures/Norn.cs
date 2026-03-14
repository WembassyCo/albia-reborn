using UnityEngine;
using UnityEngine.AI;
using Albia.Core;

namespace Albia.Creatures
{
    /// <summary>
    /// MVP Norn implementation.
    /// Scales to: Full genetics, biochemistry, neural network, learning
    /// </summary>
    public class Norn : Organism
    {
        [Header("Norn Settings")]
        [SerializeField] private float hungerThreshold = 60f;
        [SerializeField] private float movementCost = 0.05f;
        [SerializeField] private float foodSearchRadius = 20f;

        // Cached components
        private Collider[] hitColliders = new Collider[10];
        
        // Simple memory for MVP (scales to full neural memory)
        private Transform lastKnownFoodSource;
        private float stateTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            // Norn-specific initialization
        }

        protected override void Update()
        {
            base.Update();
            
            if (!IsAlive) return;

            // State timer for wandering behavior
            stateTimer += Time.deltaTime;
        }

        #region State Machine

        protected override void OnIdle()
        {
            // Check if hungry
            if (Energy < hungerThreshold)
            {
                TransitionTo(OrganismState.SeekingFood);
            }
            else if (stateTimer > 3f)
            {
                // Wander periodically
                stateTimer = 0f;
                TransitionTo(OrganismState.MovingRandom);
            }
        }

        protected override void OnSeekingFood()
        {
            // Simple food detection (scales to sensory system)
            var food = FindNearestFood();
            
            if (food != null)
            {
                lastKnownFoodSource = food.transform;
                MoveTo(food.transform.position);
                
                // Check if close enough to eat
                if (Vector3.Distance(transform.position, food.transform.position) < 1.5f)
                {
                    TransitionTo(OrganismState.Eating);
                }
            }
            else
            {
                // No food found - wander
                if (stateTimer > 5f)
                {
                    stateTimer = 0f;
                    TransitionTo(OrganismState.MovingRandom);
                }
            }
        }

        protected override void OnEating()
        {
            if (lastKnownFoodSource != null)
            {
                // Eat from food source
                // MVP: Destroy food and gain energy
                // Full: Partial consumption, quality modifiers, etc.
                
                ConsumeEnergy(25f);
                Destroy(lastKnownFoodSource.gameObject);
                lastKnownFoodSource = null;
                
                TransitionTo(OrganismState.Idle);
            }
            else
            {
                TransitionTo(OrganismState.SeekingFood);
            }
        }

        protected override void OnMovingRandom()
        {
            // Pick random destination within radius
            if (Agent.remainingDistance < 0.5f || stateTimer > 5f)
            {
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
                randomPoint.y = transform.position.y;
                
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    MoveTo(hit.position);
                }
                
                stateTimer = 0f;
            }

            // Check if hungry while wandering
            if (Energy < hungerThreshold)
            {
                TransitionTo(OrganismState.SeekingFood);
            }
            else if (stateTimer > 8f)
            {
                // Go back to idle
                stateTimer = 0f;
                TransitionTo(OrganismState.Idle);
            }
        }

        #endregion

        #region Helpers

        private GameObject FindNearestFood()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, foodSearchRadius, hitColliders, LayerMask.GetMask("Food"));
            
            GameObject nearest = null;
            float minDist = float.MaxValue;
            
            for (int i = 0; i < count; i++)
            {
                float dist = Vector3.Distance(transform.position, hitColliders[i].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hitColliders[i].gameObject;
                }
            }
            
            return nearest;
        }

        private void TransitionTo(OrganismState newState)
        {
            CurrentState = newState;
            stateTimer = 0f;
        }

        protected override void ConsumeEnergy(float amount)
        {
            base.ConsumeEnergy(amount);
            // Scales to: chemical state update, reward signals
        }

        #endregion

        #region Scales To Full System

        // TODO: Genome data (genetics system)
        // public GenomeData Genome { get; private set; }
        
        // TODO: Chemical state (biochemistry layer)
        // public ChemicalState Chemicals { get; private set; }
        
        // TODO: Neural network (AI system)
        // public NeuralNet Brain { get; private set; }
        
        // TODO: Reproduction
        // public void AttemptReproduction(Organism partner) { }

        #endregion
    }
}