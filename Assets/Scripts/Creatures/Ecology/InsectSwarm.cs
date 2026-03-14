using System.Collections.Generic;
using UnityEngine;

namespace AlbiaReborn.Creatures.Ecology
{
    /// <summary>
    /// Insect swarm using boids algorithm.
    /// Week 14: Full Ecology milestone.
    /// </summary>
    public class InsectSwarm : MonoBehaviour
    {
        [Header("Swarm Settings")]
        public int SwarmSize = 100;
        public float SwarmRadius = 10f;
        public float Energy = 1000f; // Collective energy
        
        [Header("Boids Parameters")]
        public float SeparationWeight = 1.0f;
        public float AlignmentWeight = 1.0f;
        public float CohesionWeight = 1.0f;
        public float AvoidanceWeight = 2.0f;
        
        [Header("Movement")]
        public float MaxSpeed = 5f;
        public float MaxForce = 0.5f;
        public float NeighborRadius = 5f;
        
        // Individual "agents"
        private List<SwarmMember> _members;
        private Transform _transform;

        void Start()
        {
            _transform = transform;
            InitializeSwarm();
        }

        void InitializeSwarm()
        {
            _members = new List<SwarmMember>();
            
            for (int i = 0; i < SwarmSize; i++)
            {
                _members.Add(new SwarmMember
                {
                    Position = _transform.position + Random.insideUnitSphere * SwarmRadius,
                    Velocity = Random.insideUnitSphere * MaxSpeed
                });
            }
        }

        void Update()
        {
            // Skip if no energy
            if (Energy <= 0) return;
            
            // Update all members
            foreach (var member in _members)
            {
                UpdateMember(member);
            }
        }

        void UpdateMember(SwarmMember member)
        {
            // Calculate boids forces
            Vector3 separation = CalculateSeparation(member) * SeparationWeight;
            Vector3 alignment = CalculateAlignment(member) * AlignmentWeight;
            Vector3 cohesion = CalculateCohesion(member) * CohesionWeight;
            
            Vector3 deltaVelocity = separation + alignment + cohesion;
            deltaVelocity = Vector3.ClampMagnitude(deltaVelocity, MaxForce);
            
            member.Velocity += deltaVelocity;
            member.Velocity = Vector3.ClampMagnitude(member.Velocity, MaxSpeed);
            member.Position += member.Velocity * Time.deltaTime;
            
            // Keep near center
            if (Vector3.Distance(member.Position, _transform.position) > SwarmRadius * 2)
            {
                member.Velocity += (_transform.position - member.Position).normalized * MaxForce;
            }
        }

        Vector3 CalculateSeparation(SwarmMember member)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var other in _members)
            {
                if (other == member) continue;
                
                float dist = Vector3.Distance(member.Position, other.Position);
                if (dist < NeighborRadius * 0.5f)
                {
                    Vector3 diff = member.Position - other.Position;
                    sum += diff.normalized / dist; // Weight by inverse distance
                    count++;
                }
            }
            
            if (count > 0)
                return (sum / count).normalized * MaxSpeed;
            
            return Vector3.zero;
        }

        Vector3 CalculateAlignment(SwarmMember member)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var other in _members)
            {
                if (other == member) continue;
                
                if (Vector3.Distance(member.Position, other.Position) < NeighborRadius)
                {
                    sum += other.Velocity;
                    count++;
                }
            }
            
            if (count > 0)
            {
                Vector3 avg = sum / count;
                return (avg.normalized * MaxSpeed - member.Velocity).normalized * MaxForce;
            }
            
            return Vector3.zero;
        }

        Vector3 CalculateCohesion(SwarmMember member)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var other in _members)
            {
                if (other == member) continue;
                
                if (Vector3.Distance(member.Position, other.Position) < NeighborRadius)
                {
                    sum += other.Position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                Vector3 center = sum / count;
                return (center - member.Position).normalized * MaxSpeed;
            }
            
            return Vector3.zero;
        }

        /// <summary>
        /// Swarm consumed by creature.
        /// </summary>
        public void Consume(float amount)
        {
            Energy -= amount;
            if (Energy <= 0)
            {
                // Disperses
                Destroy(gameObject);
            }
        }

        void OnDrawGizmos()
        {
            if (_members == null) return;
            
            // Draw swarm center
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, SwarmRadius);
            
            // Draw members
            Gizmos.color = Color.blue;
            foreach (var member in _members)
            {
                Gizmos.DrawWireSphere(member.Position, 0.1f);
            }
        }
    }

    public class SwarmMember
    {
        public Vector3 Position;
        public Vector3 Velocity;
    }
}
