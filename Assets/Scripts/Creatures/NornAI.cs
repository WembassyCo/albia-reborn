using UnityEngine;
using UnityEngine.AI;
using Albia.Core;
using Albia.AI;

namespace Albia.Creatures
{
    /// <summary>
    /// AI controller for Norn - bridges NeuralBrain with Organism
    /// MVP: Simple neural-driven behavior
    /// Full: Learned complex behaviors
    /// </summary>
    [RequireComponent(typeof(NeuralBrain))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class NornAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Norn norn;
        [SerializeField] private NeuralBrain brain;
        [SerializeField] private NavMeshAgent agent;
        
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 120f;
        
        // State
        private Vector3 currentDestination;
        private bool hasDestination = false;
        
        void Awake()
        {
            if (norn == null) norn = GetComponent<Norn>();
            if (brain == null) brain = GetComponent<NeuralBrain>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
        }
        
        void Start()
        {
            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.angularSpeed = rotationSpeed;
                agent.acceleration = 8f;
                agent.stoppingDistance = 0.5f;
            }
        }
        
        void Update()
        {
            if (!norn.IsAlive) return;
            
            // NeuralBrain processes its own decisions
            // This class provides helper methods for target validation
            
            UpdateCurrentState();
        }
        
        void UpdateCurrentState()
        {
            // Track if we're moving
            if (hasDestination && agent != null)
            {
                // Check if we arrived
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        hasDestination = false;
                        OnArrivedAtDestination();
                    }
                }
            }
        }
        
        void OnArrivedAtDestination()
        {
            // Check what we arrived at
            // SCALES TO: Check for food, mate, etc.
        }
        
        /// <summary>
        /// Set destination for movement
        /// Called by NeuralBrain
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            if (agent == null) return;
            
            currentDestination = destination;
            hasDestination = true;
            
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
        
        /// <summary>
        /// Stop current movement
        /// </summary>
        public void StopMoving()
        {
            hasDestination = false;
            if (agent != null)
            {
                agent.isStopped = true;
            }
        }
        
        /// <summary>
        /// Check if at destination
        /// </summary>
        public bool HasArrived() => !hasDestination;
        
        /// <summary>
        /// Face a direction
        /// </summary>
        public void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }
    }
}