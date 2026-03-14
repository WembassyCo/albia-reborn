using System;
using System.Collections.Generic;
using UnityEngine;

namespace Albia.AI
{
    /// <summary>
    /// Provides sensory input data (raycast vision, proximity detection).
    /// </summary>
    [Serializable]
    public class SensoryInput : MonoBehaviour
    {
        [Header("Vision")]
        [SerializeField] private int raycastDirections = 8;
        [SerializeField] private float visionRange = 10f;
        [SerializeField] private LayerMask visionLayers = ~0;
        [SerializeField] private LayerMask foodLayer;
        [SerializeField] private LayerMask creatureLayer;
        [SerializeField] private LayerMask threatLayer;
        [SerializeField] private LayerMask wallLayer;

        [Header("Proximity")]
        [SerializeField] private float wallDetectionRange = 1.5f;
        [SerializeField] private float nearbyCreatureRange = 5f;

        [Header("State")]
        [SerializeField] internal CreatureState creatureState;

        private Collider[] proximityColliders;
        private float timeSinceLastAction = 0f;
        private Dictionary<CreatureAction, float> actionTimers = new();

        // Chemical State (6)
        public float Hunger => creatureState?.Hunger ?? 0f;
        public float Energy => creatureState?.Energy ?? 1f;
        public float Fear { get; private set; }
        public float Pain => 1f - (creatureState?.Health ?? 1f);
        public float Curiosity { get; private set; }
        public float Comfort { get; private set; }

        // Vision (6)
        public bool CanSeeFood { get; private set; }
        public float FoodDistance { get; private set; } = 1f;
        public Vector3 FoodDirection { get; private set; }
        public bool CanSeeThreat { get; private set; }
        public float ThreatDistance { get; private set; } = 1f;
        public Vector3 ThreatDirection { get; private set; }
        public bool CanSeeCreature { get; private set; }
        public float NearestCreatureDistance { get; private set; } = 1f;
        public Vector3 NearestCreatureDirection { get; private set; }

        // Proximity (4)
        public bool WallInFront { get; private set; }
        public bool WallToLeft { get; private set; }
        public bool WallToRight { get; private set; }
        public bool ObstacleDetected { get; private set; }

        // Context (8)
        public bool CanEat => Hunger > 0.3f && CanSeeFood;
        public bool CanMate => Energy > 0.7f && AgeNormalized > 0.1f;
        public bool CanAttack => Energy > 0.3f;
        public bool CanRest => Energy < 1f;
        public float TimeSinceLastAction => 1f - Mathf.Exp(-timeSinceLastAction * 0.1f);
        public int NearbyCreaturesCount { get; private set; }
        public float Health => creatureState?.Health ?? 1f;
        public float AgeNormalized => creatureState?.NormalizedAge ?? 0f;

        private void Awake()
        {
            proximityColliders = new Collider[32];
        }

        private void Update()
        {
            timeSinceLastAction += Time.deltaTime;
            UpdateSensoryData();
        }

        public void UpdateSensoryData()
        {
            ScanVision();
            ScanProximity();
            ScanNearbyCreatures();
            UpdateInternalChemicals();
        }

        private void ScanVision()
        {
            CanSeeFood = false;
            CanSeeThreat = false;
            CanSeeCreature = false;
            FoodDistance = 1f;
            ThreatDistance = 1f;
            NearestCreatureDistance = 1f;

            for (int i = 0; i < raycastDirections; i++)
            {
                float angle = (360f / raycastDirections) * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, visionRange, visionLayers))
                {
                    float normalizedDist = hit.distance / visionRange;
                    
                    if (IsInLayerMask(hit.collider.gameObject.layer, foodLayer) && (!CanSeeFood || normalizedDist < FoodDistance))
                    {
                        CanSeeFood = true;
                        FoodDistance = normalizedDist;
                        FoodDirection = (hit.point - transform.position).normalized;
                    }
                    else if (IsInLayerMask(hit.collider.gameObject.layer, creatureLayer) && (!CanSeeCreature || normalizedDist < NearestCreatureDistance))
                    {
                        CanSeeCreature = true;
                        NearestCreatureDistance = normalizedDist;
                        NearestCreatureDirection = (hit.point - transform.position).normalized;
                    }
                    else if (IsInLayerMask(hit.collider.gameObject.layer, threatLayer) && (!CanSeeThreat || normalizedDist < ThreatDistance))
                    {
                        CanSeeThreat = true;
                        ThreatDistance = normalizedDist;
                        ThreatDirection = (hit.point - transform.position).normalized;
                    }
                }
            }
        }

        private void ScanProximity()
        {
            WallInFront = Physics.Raycast(transform.position, transform.forward, wallDetectionRange, wallLayer);
            WallToLeft = Physics.Raycast(transform.position, Quaternion.Euler(0, -45, 0) * transform.forward, wallDetectionRange, wallLayer);
            WallToRight = Physics.Raycast(transform.position, Quaternion.Euler(0, 45, 0) * transform.forward, wallDetectionRange, wallLayer);
            ObstacleDetected = WallInFront || WallToLeft || WallToRight;
        }

        private void ScanNearbyCreatures()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, nearbyCreatureRange, proximityColliders, creatureLayer);
            NearbyCreaturesCount = Mathf.Max(0, count - 1);
        }

        private void UpdateInternalChemicals()
        {
            Fear = Mathf.Clamp01((CanSeeThreat ? 1f - ThreatDistance : 0f) * 0.8f + Pain * 0.5f);
            Curiosity = Mathf.Clamp01(0.5f + (CanSeeFood ? 0.2f : 0f) + (NearbyCreaturesCount > 0 ? 0.1f : 0f));
            Comfort = Mathf.Clamp01(Energy * Health * (1f - Fear));
        }

        public void NotifyActionPerformed(CreatureAction action)
        {
            timeSinceLastAction = 0f;
            actionTimers[action] = Time.time;
        }

        private bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        public string GetDebugInfo()
        {
            return $"Hunger: {Hunger:F2} Energy: {Energy:F2} Fear: {Fear:F2}\n" +
                   $"Food: {(CanSeeFood ? "YES" : "NO")} Creatures: {NearbyCreaturesCount}\n" +
                   $"Walls: F={WallInFront} L={WallToLeft} R={WallToRight}";
        }
    }

    [Serializable]
    public class CreatureState
    {
        public float Health = 1f;
        public float Energy = 1f;
        public float Hunger = 0f;
        public float Age = 0f;
        public float LifeSpan = 300f;
        public float NormalizedAge => Mathf.Clamp01(Age / LifeSpan);
    }
}
