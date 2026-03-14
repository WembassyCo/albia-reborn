using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Albia.Core;

namespace Albia.Creatures
{
    /// <summary>
    /// Grendel - Predator creature that hunts Norns.
    /// Fast, aggressive, shorter lifespan than Norns.
    /// Creates population pressure and ecosystem dynamics.
    /// </summary>
    public class Grendel : Organism
    {
        [Header("Grendel Settings")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float nornDetectionRadius = 25f;
        [SerializeField] private float speedMultiplier = 1.4f;
        [SerializeField] private float maxLifespan = 80f; // Shorter than Norns
        [SerializeField] private float cannibalismThreshold = 0.3f; // Energy level to eat other Grendels
        
        [Header("Aggr Behavior")]
        [SerializeField] private float aggressionLevel = 0.8f; // 0-1, higher = more aggressive
        [SerializeField] private bool enableCannibalism = true;
        
        // Runtime state
        private Transform targetNorn;
        private float lastAttackTime = 0f;
        private float stateTimer = 0f;
        private bool isAttacking = false;
        
        // Cached components
        private Collider[] hitColliders = new Collider[15];
        
        // Events
        public event Action<Grendel, Organism> OnAttackLanded;
        public event Action<Grendel> OnTargetAcquired;
        public event Action<Grendel> OnTargetLost;

        protected override void Awake()
        {
            base.Awake();
            
            // Configure NavMeshAgent for Grendel traits
            if (Agent != null)
            {
                Agent.speed = 3.5f * speedMultiplier;
                Agent.acceleration = 8f * speedMultiplier;
                Agent.angularSpeed = 360f;
            }
            
            // Override base metabolism for faster energy drain
            SetMetabolismRate(0.2f); // 2x normal
        }

        protected override void Update()
        {
            base.Update();
            
            if (!IsAlive) return;
            
            stateTimer += Time.deltaTime;
            
            // Check for death by old age
            if (Age >= maxLifespan)
            {
                Die();
                return;
            }
        }

        #region State Machine Overrides

        protected override void UpdateState()
        {
            // Grendel specific state handling
            // States: Hunting, Attacking, Patrolling, Idle
            
            switch (CurrentState)
            {
                case OrganismState.Idle:
                    OnIdle();
                    break;
                case OrganismState.SeekingFood:
                    OnHunting(); // Override seeking food to hunt Norns
                    break;
                case OrganismState.Eating:
                    OnAttacking(); // Override eating to attacking
                    break;
                default:
                    // Custom states
                    OnHunting();
                    break;
            }
        }

        protected override void OnIdle()
        {
            // Grendels are always hungry for Norns
            if (stateTimer > 2f)
            {
                stateTimer = 0f;
                TransitionTo(OrganismState.SeekingFood); // Start hunting
            }
        }

        /// <summary>
        /// Hunting behavior - seeks nearest Norn
        /// </summary>
        private void OnHunting()
        {
            if (targetNorn == null)
            {
                FindNearestNorn();
                
                if (targetNorn == null)
                {
                    // No Norns found - patrol
                    OnPatrolling();
                    return;
                }
                
                OnTargetAcquired?.Invoke(this);
            }
            
            if (targetNorn != null)
            {
                // Check if target is still alive
                var targetOrganism = targetNorn.GetComponent<Organism>();
                if (targetOrganism == null || !targetOrganism.IsAlive)
                {
                    targetNorn = null;
                    OnTargetLost?.Invoke(this);
                    return;
                }
                
                // Move to target
                Agent.SetDestination(targetNorn.position);
                
                // Check attack range
                float distance = Vector3.Distance(transform.position, targetNorn.position);
                if (distance < attackRange)
                {
                    TransitionTo(OrganismState.Eating); // Using Eating state for attacking
                }
                
                // Periodically re-evaluate target (pick closer Norn)
                if (stateTimer > 3f)
                {
                    stateTimer = 0f;
                    FindNearestNorn(); // May find closer target
                }
            }
        }

        /// <summary>
        /// Attacking behavior - deal damage on contact
        /// </summary>
        private void OnAttacking()
        {
            if (targetNorn == null)
            {
                TransitionTo(OrganismState.SeekingFood);
                return;
            }
            
            // Face target
            transform.LookAt(targetNorn);
            
            // Attack if cooldown elapsed
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
            }
            
            // Keep moving toward target while attacking
            float distance = Vector3.Distance(transform.position, targetNorn.position);
            if (distance > attackRange * 1.5f)
            {
                TransitionTo(OrganismState.SeekingFood);
            }
        }

        /// <summary>
        /// Patrol behavior when no targets found
        /// </summary>
        private void OnPatrolling()
        {
            if (Agent.remainingDistance < 0.5f || stateTimer > 5f)
            {
                Vector3 randomPoint = transform.position + UnityEngine.Random.insideUnitSphere * 15f;
                randomPoint.y = transform.position.y;
                
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    MoveTo(hit.position);
                }
                
                stateTimer = 0f;
            }
            
            // Periodically look for targets
            if (stateTimer > 2f)
            {
                FindNearestNorn();
                if (targetNorn != null)
                {
                    TransitionTo(OrganismState.SeekingFood);
                }
            }
        }

        #endregion

        #region Combat

        private void PerformAttack()
        {
            if (targetNorn == null) return;
            
            lastAttackTime = Time.time;
            
            // Deal damage to target
            var targetOrganism = targetNorn.GetComponent<Organism>();
            if (targetOrganism != null && targetOrganism.IsAlive)
            {
                // Use reflection or interface to call TakeDamage if available
                // For now, try calling a damage method
                ApplyDamageToTarget(targetOrganism, attackDamage);
                
                // Visual feedback
                OnAttackLanded?.Invoke(this, targetOrganism);
                
                // Gain energy from attack (predator satisfaction)
                ConsumeEnergy(attackDamage * 0.1f);
            }
        }

        private void ApplyDamageToTarget(Organism target, float damage)
        {
            // Try to find a health component or damage method
            // First, check for Norn-specific component
            var norn = target as Norn;
            if (norn != null)
            {
                // Access Norn's TakeDamage through reflection or public method
                var takeDamageMethod = norn.GetType().GetMethod("TakeDamage", 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (takeDamageMethod != null)
                {
                    takeDamageMethod.Invoke(norn, new object[] { damage });
                }
                else
                {
                    // Fallback: reduce energy as proxy for damage
                    var energyField = norn.GetType().GetField("currentEnergy", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (energyField != null)
                    {
                        float currentEnergy = (float)energyField.GetValue(norn);
                        energyField.SetValue(norn, currentEnergy - damage);
                    }
                }
            }
            else
            {
                // Generic organism - try energy reduction
                var energyProp = target.GetType().GetProperty("Energy");
                if (energyProp != null)
                {
                    float currentEnergy = (float)energyProp.GetValue(target);
                    var maxEnergyProp = target.GetType().GetProperty("MaxEnergy");
                    float maxEnergy = maxEnergyProp != null ? (float)maxEnergyProp.GetValue(target) : 100f;
                    
                    // Use reflection to set protected field
                    var energyField = target.GetType().GetField("currentEnergy", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (energyField != null)
                    {
                        energyField.SetValue(target, Mathf.Max(0, currentEnergy - damage));
                    }
                }
            }
        }

        #endregion

        #region Targeting

        /// <summary>
        /// Finds the nearest Norn (or other target)
        /// </summary>
        private void FindNearestNorn()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, 
                nornDetectionRadius, 
                hitColliders, 
                LayerMask.GetMask("Creatures")
            );
            
            Transform nearest = null;
            float minDist = float.MaxValue;
            
            for (int i = 0; i < count; i++)
            {
                var collider = hitColliders[i];
                if (collider == null) continue;
                
                // Skip self
                if (collider.transform == transform) continue;
                
                // Check if it's a valid target
                var target = collider.GetComponent<Organism>();
                if (target == null || !target.IsAlive) continue;
                
                // Skip other Grendels unless cannibalism is enabled and desperate
                var otherGrendel = target as Grendel;
                if (otherGrendel != null)
                {
                    if (!enableCannibalism || Energy > maxEnergy * cannibalismThreshold)
                        continue;
                }
                
                // Calculate distance
                float dist = Vector3.Distance(transform.position, collider.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = collider.transform;
                }
            }
            
            if (nearest != null && nearest != targetNorn)
            {
                targetNorn = nearest;
                OnTargetAcquired?.Invoke(this);
            }
        }

        /// <summary>
        /// Gets the current target
        /// </summary>
        public Transform CurrentTarget => targetNorn;

        /// <summary>
        /// Manually set a target (for testing or scripting)
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetNorn = target;
            if (target != null)
            {
                OnTargetAcquired?.Invoke(this);
                TransitionTo(OrganismState.SeekingFood);
            }
        }

        #endregion

        #region Lifecycle

        protected override void Die()
        {
            // Fire event before base Die()
            OnDeath?.Invoke(this);
            
            // Create corpse
            CreateCorpse();
            
            // Base die will disable game object
            base.Die();
        }

        private void CreateCorpse()
        {
            // Use CorpseManager if available
            if (CorpseManager.Instance != null)
            {
                CorpseManager.Instance.CreateSimpleCorpse(
                    transform.position,
                    Guid.NewGuid(),
                    "Grendel",
                    Age,
                    null
                );
            }
            else
            {
                // Fallback: create simple corpse
                var corpseObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                corpseObj.name = $"Corpse_Grendel_{Guid.NewGuid().ToString().Substring(0, 8)}";
                corpseObj.transform.position = transform.position;
                corpseObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                corpseObj.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
                
                // Different color for Grendel corpses
                var renderer = corpseObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.3f, 0.6f, 0.3f);
                }
                
                Destroy(corpseObj.GetComponent<Collider>());
                
                var corpse = corpseObj.AddComponent<Corpse>();
                if (corpse != null)
                {
                    corpse.Initialize(Guid.NewGuid(), "Grendel", Age, null);
                }
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configure Grendel from genome
        /// </summary>
        public void ConfigureFromGenome(float aggression, float speed, float lifespan)
        {
            aggressionLevel = Mathf.Clamp01(aggression);
            speedMultiplier = Mathf.Clamp(speed, 1.0f, 2.0f);
            maxLifespan = Mathf.Clamp(lifespan, 40f, 120f);
            
            // Apply to agent
            if (Agent != null)
            {
                Agent.speed = 3.5f * speedMultiplier;
                Agent.acceleration = 8f * speedMultiplier;
            }
        }

        /// <summary>
        /// Set cannibalism enabled/disabled
        /// </summary>
        public void SetCannibalism(bool enabled)
        {
            enableCannibalism = enabled;
        }

        #endregion

        #region Helpers

        private void TransitionTo(OrganismState newState)
        {
            CurrentState = newState;
            stateTimer = 0f;
        }

        /// <summary>
        /// Debug info for inspector
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Grendel - Energy: {Energy:F1}, Target: {(targetNorn != null ? targetNorn.name : "None")}, " +
                   $"State: {CurrentState}, Age: {Age:F1}/{maxLifespan:F1}";
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, nornDetectionRadius);
            
            // Draw attack range
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw line to target
            if (targetNorn != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, targetNorn.position);
            }
        }

        #endregion
    }
    
    /// <summary>
    /// Additional states for Grendel behavior
    /// </summary>
    public enum GrendelState
    {
        Patrolling,
        Hunting,
        Attacking,
        Fleeing
    }
}