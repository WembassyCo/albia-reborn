using System;
using System.Collections.Generic;
using UnityEngine;

namespace Albia.AI
{
    /// <summary>
    /// Helper class that provides sensory input data for creature AI.
    /// Handles raycast vision, proximity detection, and state normalization.
    /// </summary>
    [Serializable]
    public class SensoryInput : MonoBehaviour
    {
        [Header("Vision Raycast")]
        [Tooltip("Number of raycast directions for vision")]
        [SerializeField] private int raycastDirections = 8;
        
        [Tooltip("Maximum raycast distance")]
        [SerializeField] private float visionRange = 10f;
        
        [Tooltip("Layers to check for vision")]
        [SerializeField] private LayerMask visionLayers = ~0; // Everything
        
        [Tooltip("Layers containing food objects")]
        [SerializeField] private LayerMask foodLayer;
        
        [Tooltip("Layers containing creatures")]
        [SerializeField] private LayerMask creatureLayer;
        
        [Tooltip("Layers containing threats/obstacles")]
        [SerializeField] private LayerMask threatLayer;
        
        [Tooltip("Layers containing walls")]
        [SerializeField] private LayerMask wallLayer;

        [Header("Proximity Sensors")]
        [Tooltip("Distance for wall detection in front")]
        [SerializeField] private float wallDetectionRange = 1.5f;
        
        [Tooltip("Nearby creature detection range")]
        [SerializeField] private float nearbyCreatureRange = 5f;

        [Header("State References")]
        [Tooltip("Reference to creature state for energy/hunger/etc")]
        [SerializeField] 
        internal CreatureState creatureState;

        // Cached raycast results
        private RaycastHit[] visionHits;
        private Collider[] proximityColliders;
        
        // Raycast visualization (for debugging)
        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugRays = false;
        [SerializeField] private Color foodRayColor = Color.green;
        [SerializeField] private Color threatRayColor = Color.red;
        [SerializeField] private Color wallRayColor = Color.yellow;

        // Internal state tracking
        private float timeSinceLastAction = 0f;
        private Dictionary<CreatureAction, float> actionTimers = new();

        #region Sensory Properties
        
        // Chemical/Internal State (6 outputs)
        public float Hunger => creatureState?.Hunger ?? 0f;
        public float Energy => creatureState?.Energy ?? 1f;
        public float Fear { get; private set; }
        public float Pain => 1f - (creatureState?.Health ?? 1f);
        public float Curiosity { get; private set; }
        public float Comfort { get; private set; }
        
        // Vision Data (6 outputs)
        public bool CanSeeFood { get; private set; }
        public float FoodDistance { get; private set; } = 1f;
        public Vector3 FoodDirection { get; private set; }
        
        public bool CanSeeThreat { get; private set; }
        public float ThreatDistance { get; private set; } = 1f;
        public Vector3 ThreatDirection { get; private set; }
        
        public bool CanSeeCreature { get; private set; }
        public float NearestCreatureDistance { get; private set; } = 1f;
        public Vector3 NearestCreatureDirection { get; private set; }
        
        // Proximity Detection (4 outputs)
        public bool WallInFront { get; private set; }
        public bool WallToLeft { get; private set; }
        public bool WallToRight { get; private set; }
        public bool ObstacleDetected { get; private set; }
        
        // Context/State (8 outputs)
        public bool CanEat => Hunger > 0.3f && CanSeeFood;
        public bool CanMate => Energy > 0.7f && AgeNormalized > 0.1f;
        public bool CanAttack => Energy > 0.3f;
        public bool CanRest => Energy < 1f;
        
        public float TimeSinceLastAction => 1f - Mathf.Exp(-timeSinceLastAction * 0.1f);
        public int NearbyCreaturesCount { get; private set; }
        public float Health => creatureState?.Health ?? 1f;
        public float AgeNormalized => creatureState?.NormalizedAge ?? 0f;
        
        #endregion

        private void Awake()
        {
            visionHits = new RaycastHit[raycastDirections];
            proximityColliders = new Collider[32];
        }

        private void Update()
        {
            timeSinceLastAction += Time.deltaTime;
            UpdateSensoryData();
        }

        private void OnDrawGizmos()
        {
            if (!showDebugRays || !Application.isPlaying) return;
            
            DrawVisionRays();
        }

        /// <summary>
        /// Updates all sensory data by scanning environment
        /// </summary>
        public void UpdateSensoryData()
        {
            ScanVision();
            ScanProximity();
            ScanNearbyCreatures();
            UpdateInternalChemicals();
        }

        /// <summary>
        /// Perform raycast vision sweep
        /// </summary>
        private void ScanVision()
        {
            CanSeeFood = false;
            CanSeeThreat = false;
            CanSeeCreature = false;
            FoodDistance = 1f;
            ThreatDistance = 1f;
            NearestCreatureDistance = 1f;
            
            // Cast rays in a fan pattern
            for (int i = 0; i < raycastDirections; i++)
            {
                float angle = (360f / raycastDirections) * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, visionRange, visionLayers))
                {
                    // Check what was hit
                    float normalizedDist = hit.distance / visionRange;
                    
                    if (IsInLayerMask(hit.collider.gameObject.layer, foodLayer))
                    {
                        if (!CanSeeFood || normalizedDist < FoodDistance)
                        {
                            CanSeeFood = true;
                            FoodDistance = normalizedDist;
                            FoodDirection = (hit.point - transform.position).normalized;
                        }
                    }
                    else if (IsInLayerMask(hit.collider.gameObject.layer, creatureLayer))
                    {
                        if (!CanSeeCreature || normalizedDist < NearestCreatureDistance)
                        {
                            CanSeeCreature = true;
                            NearestCreatureDistance = normalizedDist;
                            NearestCreatureDirection = (hit.point - transform.position).normalized;
                        }
                    }
                    else if (IsInLayerMask(hit.collider.gameObject.layer, threatLayer))
                    {
                        if (!CanSeeThreat || normalizedDist < ThreatDistance)
                        {
                            CanSeeThreat = true;
                            ThreatDistance = normalizedDist;
                            ThreatDirection = (hit.point - transform.position).normalized;
                        }
                    }
                }
            }
            
            DebugVision();
        }

        /// <summary>
        /// Scan for nearby walls and obstacles
        /// </summary>
        private void ScanProximity()
        {
            // Wall in front
            WallInFront = Physics.Raycast(transform.position, transform.forward, wallDetectionRange, wallLayer);
            
            // Wall to left (45 deg)
            Vector3 leftDir = Quaternion.Euler(0, -45, 0) * transform.forward;
            WallToLeft = Physics.Raycast(transform.position, leftDir, wallDetectionRange, wallLayer);
            
            // Wall to right (45 deg)
            Vector3 rightDir = Quaternion.Euler(0, 45, 0) * transform.forward;
            WallToRight = Physics.Raycast(transform.position, rightDir, wallDetectionRange, wallLayer);
            
            // General obstacle detection
            ObstacleDetected = WallInFront || WallToLeft || WallToRight;
        }

        /// <summary>
        /// Scan for nearby creatures using overlap sphere
        /// </summary>
        private void ScanNearbyCreatures()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, 
                nearbyCreatureRange, 
                proximityColliders, 
                creatureLayer
            );
            
            // Exclude self
            NearbyCreaturesCount = Mathf.Max(0, count - 1);
        }

        /// <summary>
        /// Update internal chemical states
        /// </summary>
        private void UpdateInternalChemicals()
        {
            // Fear based on threats and pain
            float fearInput = CanSeeThreat ? (1f - ThreatDistance) * 0.8f : 0f;
            fearInput += Pain * 0.5f;
            Fear = Mathf.Clamp01(fearInput);
            
            // Curiosity - opposite of boredom, based on novelty
            Curiosity = 0.5f; // Baseline curiosity
            if (CanSeeFood) Curiosity += 0.2f;
            if (NearbyCreaturesCount > 0) Curiosity += 0.1f;
            Curiosity = Mathf.Clamp01(Curiosity);
            
            // Comfort based on energy, health, and lack of fear
            Comfort = Energy * Health * (1f - Fear);
            Comfort = Mathf.Clamp01(Comfort);
        }

        /// <summary>
        /// Notify that an action was performed (resets action timer)
        /// </summary>
        public void NotifyActionPerformed(CreatureAction action)
        {
            timeSinceLastAction = 0f;
            actionTimers[action] = Time.time;
        }

        /// <summary>
        /// Get time since specific action was performed
        /// </summary>
        public float GetTimeSinceAction(CreatureAction action)
        {
            if (actionTimers.TryGetValue(action, out float time))
            {
                return Time.time - time;
            }
            return float.MaxValue;
        }

        /// <summary>
        /// Check if a layer is in a layer mask
        /// </summary>
        private bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        /// <summary>
        /// Draw debug visualization rays
        /// </summary>
        private void DrawVisionRays()
        {
            // Vision rays
            for (int i = 0; i < raycastDirections; i++)
            {
                float angle = (360f / raycastDirections) * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                
                Color rayColor = Color.gray;
                float drawDist = visionRange * 0.3f;
                
                if (CanSeeFood && Vector3.Angle(direction, FoodDirection) < 30f)
                {
                    rayColor = foodRayColor;
                    drawDist = visionRange * FoodDistance;
                }
                else if (CanSeeThreat && Vector3.Angle(direction, ThreatDirection) < 30f)
                {
                    rayColor = threatRayColor;
                    drawDist = visionRange * ThreatDistance;
                }
                
                Debug.DrawRay(transform.position, direction * drawDist, rayColor);
            }
            
            // Proximity rays
            Debug.DrawRay(transform.position, transform.forward * wallDetectionRange, 
                WallInFront ? wallRayColor : Color.gray);
        }

        private void DebugVision()
        {
            if (showDebugRays)
            {
                if (CanSeeFood)
                    Debug.DrawLine(transform.position, transform.position + FoodDirection * visionRange * FoodDistance, foodRayColor);
                if (CanSeeThreat)
                    Debug.DrawLine(transform.position, transform.position + ThreatDirection * visionRange * ThreatDistance, threatRayColor);
            }
        }

        /// <summary>
        /// Get a summary of current sensory state
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== SensoryInput ===");
            sb.AppendLine($"Hunger: {Hunger:F2} | Energy: {Energy:F2} | Fear: {Fear:F2}");
            sb.AppendLine($"Food: {(CanSeeFood ? $"YES at {FoodDistance:F2}" : "NO")}");
            sb.AppendLine($"Threat: {(CanSeeThreat ? $"YES at {ThreatDistance:F2}" : "NO")}");
            sb.AppendLine($"Creatures: {NearbyCreaturesCount} nearby");
            sb.AppendLine($"Walls: F={WallInFront} L={WallToLeft} R={WallToRight}");
            sb.AppendLine($"Can Eat: {CanEat} | Can Mate: {CanMate}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Reference to creature state for sensory system
    /// Used to decouple SensoryInput from specific creature implementations
    /// </summary>
    [Serializable]
    public class CreatureState
    {
        public float Health = 1f;
        public float Energy = 1f;
        public float Hunger = 0f;
        public float Age = 0f;
        public float LifeSpan = 300f; // seconds
        
        public float NormalizedAge => Mathf.Clamp01(Age / LifeSpan);
    }
}
