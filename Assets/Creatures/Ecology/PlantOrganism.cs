using UnityEngine;
using AlbiaReborn.Core.Interfaces;
using AlbiaReborn.World.Voxel;

namespace AlbiaReborn.Creatures.Ecology
{
    /// <summary>
    /// Plant organism - producer in food chain.
    /// No neural network, simple growth rules.
    /// </summary>
    public class PlantOrganism : MonoBehaviour
    {
        [Header("Plant State")]
        public float Energy = 50f;
        public float MaxEnergy = 100f;
        public float GrowthRate = 1f;
        public float SeedDispersalRadius = 5f;
        public float MaturityThreshold = 80f;

        [Header("Requirements")]
        public float MinMoisture = 0.2f;
        public float MinTemperature = 5f;
        public float OptimalTemperature = 20f;

        // Position
        public Vector3Int TilePosition { get; private set; }
        
        // Climate
        private IClimateQuery _climate;
        private System.Random _rand = new System.Random();

        void Start()
        {
            TilePosition = Vector3Int.FloorToInt(transform.position);
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        void Tick(float deltaTime)
        {
            // Environment check
            float temp = _climate?.GetTemperatureAt(TilePosition) ?? 15f;
            float moisture = _climate?.GetMoistureAt(TilePosition) ?? 0.5f;

            // Dormancy check
            if (temp < MinTemperature)
            {
                // Dormant in winter
                return;
            }

            // Calculate growth
            float tempFitness = 1f - Mathf.Abs(temp - OptimalTemperature) / 30f;
            tempFitness = Mathf.Clamp01(tempFitness);

            float moisturePenalty = moisture < MinMoisture ? 0.5f : 1f;

            float growth = GrowthRate * tempFitness * moisturePenalty * deltaTime;
            Energy = Mathf.Min(Energy + growth, MaxEnergy);

            // Dispersal
            if (Energy >= MaturityThreshold && _rand.NextDouble() < 0.01) // 1% chance per frame
            {
                AttemptSeedDispersal();
            }
        }

        void AttemptSeedDispersal()
        {
            // Pick random direction
            Vector2 dir = Random.insideUnitCircle * SeedDispersalRadius;
            Vector3 newPos = transform.position + new Vector3(dir.x, 0, dir.y);
            Vector3Int newTile = Vector3Int.FloorToInt(newPos);

            // TODO: Check if tile valid
            // Spawn plant (if conditions met)
        }

        public void Consume(float amount)
        {
            Energy -= amount;
            if (Energy <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            // Add organic matter to tile
            // TODO: SoilNutrientSystem interaction
            Destroy(gameObject);
        }
    }
}
