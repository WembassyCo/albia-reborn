using UnityEngine;
using UnityEngine.AI;
using Albia.Creatures;
using Albia.Core;
using Albia.Ecology;

namespace Albia.AI
{
    /// <summary>
    /// Bridges NeuralNet outputs to Norn actions.
    /// MVP: Map 16 outputs to discrete actions
    /// Full: Continuous control, learned behaviors
    /// </summary>
    [RequireComponent(typeof(Norn))]
    public class NeuralBrain : MonoBehaviour
    {
        [Header("Neural Network")]
        [SerializeField] private NeuralNet neuralNet;
        [SerializeField] private float decisionInterval = 0.1f; // 10 decisions/sec
        
        [Header("Motor Control")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float turnSpeed = 120f;
        [SerializeField] private float actionThreshold = 0.3f;
        
        // References
        private Norn norn;
        private NavMeshAgent agent;
        private ChemicalState chemicals;
        
        // Sensory State
        private float[] sensoryInputs = new float[24];
        private float decisionTimer = 0f;
        
        // Action outputs
        private Vector3 targetPosition;
        private bool hasTarget = false;
        
        // Memory for learning
        private float lastEnergy;
        private float[] lastChemicals;
        
        void Awake()
        {
            norn = GetComponent<Norn>();
            agent = GetComponent<NavMeshAgent>();
            chemicals = norn.Chemicals;
            
            // Initialize neural net from genome if available
            if (norn.Genome != null)
            {
                InitializeFromGenome();
            }
            else
            {
                // Random initialization for MVP
                neuralNet = NeuralNet.CreateRandom(24, 36, 16, 0.001f);
            }
            
            lastEnergy = norn.Energy;
            lastChemicals = new float[12];
        }
        
        void Start()
        {
            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.angularSpeed = turnSpeed;
            }
        }
        
        void Update()
        {
            if (neuralNet == null || !norn.IsAlive) return;
            
            decisionTimer += Time.deltaTime;
            
            // Process sensory information every frame
            GatherSensoryInputs();
            
            // Make decision at interval
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0f;
                ProcessDecision();
                ApplyLearningSignals();
            }
            
            // Execute current target
            ExecuteMovement();
        }
        
        /// <summary>
        /// Initialize neural weights from genome (genes 64-191)
        /// </summary>
        private void InitializeFromGenome()
        {
            var genome = norn.Genome;
            float[] weights = new float[36 * 24 + 16 * 36]; // Input-hidden + hidden-output
            
            // Extract weights from genome
            int geneIndex = 64;
            for (int i = 0; i < weights.Length && geneIndex < 192; i++, geneIndex++)
            {
                weights[i] = genome.GetGene(geneIndex);
            }
            
            neuralNet = new NeuralNet(24, 36, 16, weights, 0.001f);
        }
        
        /// <summary>
        /// Gather all sensory inputs for neural network
        /// </summary>
        private void GatherSensoryInputs()
        {
            // 0-3: Energy (normalized 0-1, split into 4 bins for better resolution)
            float energyNorm = norn.Energy / norn.MaxEnergy;
            sensoryInputs[0] = energyNorm < 0.25f ? 1f : 0f; // Critical
            sensoryInputs[1] = (energyNorm >= 0.25f && energyNorm < 0.5f) ? 1f : 0f; // Low
            sensoryInputs[2] = (energyNorm >= 0.5f && energyNorm < 0.75f) ? 1f : 0f; // Medium
            sensoryInputs[3] = energyNorm >= 0.75f ? 1f : 0f; // High
            
            // 4-7: Chemicals (hunger, fear, curiosity, reward)
            if (chemicals != null)
            {
                sensoryInputs[4] = chemicals.GetLevel(ChemicalType.Hunger) / 100f;
                sensoryInputs[5] = chemicals.GetLevel(ChemicalType.Fear) / 100f;
                sensoryInputs[6] = chemicals.GetLevel(ChemicalType.Curiosity) / 100f;
                sensoryInputs[7] = chemicals.GetLevel(ChemicalType.Reward) / 100f;
            }
            
            // 8-15: Proximity sensors (8 directions)
            UpdateProximitySensors();
            
            // 16-19: Nearest food
            Vector3? nearestFood = FindNearestFood();
            if (nearestFood.HasValue)
            {
                Vector3 dir = (nearestFood.Value - transform.position).normalized;
                sensoryInputs[16] = dir.x; // Food X direction
                sensoryInputs[17] = dir.z; // Food Z direction
                float dist = Vector3.Distance(transform.position, nearestFood.Value);
                sensoryInputs[18] = 1f / (1f + dist * 0.1f); // Food proximity (closer = higher)
            }
            else
            {
                sensoryInputs[16] = sensoryInputs[17] = sensoryInputs[18] = 0f;
            }
            sensoryInputs[19] = nearestFood.HasValue ? 1f : 0f; // Food exists
            
            // 20-22: Nearest creature (mate/rival)
            // 23: Random noise (for exploration)
            sensoryInputs[23] = Random.value;
        }
        
        /// <summary>
        /// Update 8-direction proximity sensors
        /// </summary>
        private void UpdateProximitySensors()
        {
            float sensorRange = 10f;
            int layerMask = ~0; // All layers
            
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                
                if (Physics.Raycast(transform.position + Vector3.up, dir, out RaycastHit hit, sensorRange, layerMask))
                {
                    float proximity = 1f - (hit.distance / sensorRange);
                    sensoryInputs[8 + i] = proximity;
                }
                else
                {
                    sensoryInputs[8 + i] = 0f;
                }
            }
        }
        
        private Vector3? FindNearestFood()
        {
            // Use EcologyManager or direct search
            Collider[] hits = Physics.OverlapSphere(transform.position, 20f);
            Vector3? nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Food"))
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = hit.transform.position;
                    }
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Process neural outputs and decide action
        /// </summary>
        private void ProcessDecision()
        {
            float[] outputs = neuralNet.Forward(sensoryInputs);
            
            // Find highest output action
            int bestAction = 0;
            float bestValue = outputs[0];
            
            for (int i = 1; i < 16; i++)
            {
                if (outputs[i] > bestValue)
                {
                    bestValue = outputs[i];
                    bestAction = i;
                }
            }
            
            // Only act if above threshold
            if (bestValue < actionThreshold)
            {
                hasTarget = false;
                return;
            }
            
            // Execute action
            ExecuteAction(bestAction, outputs);
        }
        
        /// <summary>
        /// Map neural output to game action
        /// </summary>
        private void ExecuteAction(int action, float[] outputs)
        {
            switch (action)
            {
                case 0: // Move forward
                    MoveDirection(transform.forward);
                    break;
                    
                case 1: // Move backward
                    MoveDirection(-transform.forward);
                    break;
                    
                case 2: // Turn left
                    Turn(-1);
                    break;
                    
                case 3: // Turn right
                    Turn(1);
                    break;
                    
                case 4: // Move to food
                    Vector3? food = FindNearestFood();
                    if (food.HasValue)
                    {
                        SetTarget(food.Value);
                    }
                    break;
                    
                case 5: // Eat (if food nearby)
                    TryEat();
                    break;
                    
                case 6: // Wait/rest
                    hasTarget = false;
                    if (agent != null) agent.isStopped = true;
                    break;
                    
                case 7: // Explore (random direction)
                    SetTarget(transform.position + Random.insideUnitSphere * 10f);
                    break;
                    
                // Actions 8-15: Reserved for future use (mate, flee, etc.)
                default:
                    hasTarget = false;
                    break;
            }
        }
        
        private void MoveDirection(Vector3 dir)
        {
            SetTarget(transform.position + dir * 5f);
        }
        
        private void Turn(int direction)
        {
            transform.Rotate(0, turnSpeed * Time.deltaTime * direction, 0);
        }
        
        private void SetTarget(Vector3 target)
        {
            targetPosition = target;
            targetPosition.y = transform.position.y; // Keep on ground
            hasTarget = true;
            
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                agent.SetDestination(targetPosition);
            }
        }
        
        private void ExecuteMovement()
        {
            if (!hasTarget || agent == null) return;
            
            // Check if reached target
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                hasTarget = false;
                agent.isStopped = true;
            }
        }
        
        /// <summary>
        /// Try to eat nearby food
        /// </summary>
        private void TryEat()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
            foreach (var hit in hits)
            {
                var food = hit.GetComponent<FoodSource>();
                if (food != null && !food.IsConsumed)
                {
                    if (food.TryConsume(norn))
                    {
                        // Trigger learning reward
                        neuralNet?.ApplyReward(1.0f);
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// Apply learning signals based on state changes
        /// </summary>
        private void ApplyLearningSignals()
        {
            if (neuralNet == null) return;
            
            // Energy increased = reward
            float energyDelta = norn.Energy - lastEnergy;
            if (energyDelta > 0)
            {
                neuralNet.ApplyReward(energyDelta / norn.MaxEnergy);
            }
            else if (energyDelta < -0.5f) // Significant energy loss
            {
                neuralNet.ApplyPunishment(-energyDelta / norn.MaxEnergy);
            }
            lastEnergy = norn.Energy;
            
            // Chemical-based learning
            if (chemicals != null)
            {
                // Reward for reward chemical
                float rewardChem = chemicals.GetLevel(ChemicalType.Reward);
                if (rewardChem > lastChemicals[2])
                {
                    neuralNet.ApplyReward((rewardChem - lastChemicals[2]) / 100f);
                }
                
                // Punishment for pain
                float pain = chemicals.GetLevel(ChemicalType.Pain);
                if (pain > 50f)
                {
                    neuralNet.ApplyPunishment(pain / 100f);
                }
                
                // Store current values
                lastChemicals[2] = rewardChem;
                lastChemicals[4] = pain;
            }
            
            // Hebbian update
            neuralNet.UpdateWeights();
        }
        
        /// <summary>
        /// Called when Norn takes damage
        /// </summary>
        public void OnDamageTaken(float amount)
        {
            neuralNet?.ApplyPunishment(amount / 100f);
        }
        
        /// <summary>
        /// Called when Norn successfuly mates
        /// </summary>
        public void OnReproductionSuccess()
        {
            neuralNet?.ApplyReward(0.5f);
        }
        
        void OnDestroy()
        {
            // Cleanup
        }
        
        // SCALES TO: Memory system, more complex actions, social behaviors
    }
}