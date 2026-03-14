using UnityEngine;
using UnityEngine.AI;
using AlbiaReborn.Creatures.Ecology;

namespace AlbiaReborn.Creatures
{
    /// <summary>
    /// Executes actions selected by neural network.
    /// </summary>
    public class ActionSystem : MonoBehaviour
    {
        private Organism _organism;
        private SensorySystem _sensory;
        private NavMeshAgent _agent;
        
        // Configuration
        public float MoveSpeed = 3.5f;
        public float EatRange = 1.5f;
        public float InteractionRange = 2f;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
                _agent.speed = MoveSpeed;
                _agent.angularSpeed = 120f;
                _agent.acceleration = 8f;
            }
        }

        public void Initialize(Organism organism, SensorySystem sensory)
        {
            _organism = organism;
            _sensory = sensory;
        }

        /// <summary>
        /// Execute selected action.
        /// </summary>
        public void ExecuteAction(ActionType action)
        {
            if (_organism == null || !_organism.gameObject.activeSelf) return;

            switch (action)
            {
                case ActionType.MoveToward:
                    MoveTowardFood();
                    break;
                case ActionType.MoveAway:
                    MoveAwayFromThreat();
                    break;
                case ActionType.Eat:
                    AttemptEat();
                    break;
                case ActionType.Rest:
                    Rest();
                    break;
                case ActionType.Sleep:
                    Sleep();
                    break;
                case ActionType.Vocalize:
                    Vocalize();
                    break;
                // TODO: Other actions
                default:
                    Rest(); // Default fallback
                    break;
            }
        }

        void MoveTowardFood()
        {
            PlantOrganism plant;
            float foodStrength = _sensory.GetNearestFood(out plant);
            
            if (plant != null && foodStrength > 0.01f)
            {
                Vector3 targetPos = plant.transform.position;
                
                if (_agent != null && _agent.isActiveAndEnabled)
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(targetPos);
                }
                else
                {
                    // Fallback: direct movement
                    Vector3 dir = (targetPos - transform.position).normalized;
                    transform.position += dir * MoveSpeed * Time.deltaTime * 0.1f;
                }
            }
        }

        void MoveAwayFromThreat()
        {
            // Get opposite direction of threat
            // TODO: Flee from detected threat position
            Vector3 fleeDir = -transform.forward; // Simplified
            
            if (_agent != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(transform.position + fleeDir * 5f);
            }
        }

        void AttemptEat()
        {
            PlantOrganism plant;
            float foodValue = _sensory.GetNearestFood(out plant);
            
            // Check if in range
            if (plant != null && Vector3.Distance(transform.position, plant.transform.position) <= EatRange)
            {
                // Consume plant
                float energyGained = Mathf.Min(plant.Energy, 20f); // Max 20 energy per bite
                plant.Consume(energyGained);
                _organism.Consume(energyGained);
                
                // Reward signal
                _organism.Chemicals.Apply(Biochemistry.ChemicalType.Hunger, -0.3f);
                _organism.Chemicals.Apply(Biochemistry.ChemicalType.Reward, 0.5f);
                
                // Stop moving
                if (_agent != null)
                    _agent.isStopped = true;
            }
            else if (plant != null)
            {
                // Not in range yet, move toward
                MoveTowardFood();
            }
        }

        void Rest()
        {
            if (_agent != null)
            {
                _agent.isStopped = true;
            }
            
            // Slow metabolism while resting
            _organism.Chemicals.Apply(Biochemistry.ChemicalType.Sleepiness, 0.001f);
        }

        void Sleep()
        {
            // Full rest mode - accelerated recovery
            if (_agent != null)
            {
                _agent.isStopped = true;
            }
            
            _organism.Chemicals.Apply(Biochemistry.ChemicalType.Sleepiness, -0.01f);
            _organism.Chemicals.Apply(Biochemistry.ChemicalType.Discomfort, -0.005f);
        }

        void Vocalize()
        {
            // TODO: Broadcast word, trigger audio
            Debug.Log($"{_organism.OrganismName} vocalizes");
        }

        /// <summary>
        /// Check if action preconditions met.
        /// </summary>
        public bool CanExecute(ActionType action)
        {
            return action switch
            {
                ActionType.Eat => _sensory.GetNearestFood() > 0.01f,
                ActionType.MoveToward => _sensory.GetNearestFood() > 0.01f,
                ActionType.MoveAway => _sensory.GetNearestThreat() > 0.01f,
                _ => true
            };
        }
    }
}
