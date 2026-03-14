using UnityEngine;
using System.Collections.Generic;
using AlbiaReborn.Creatures.Ecology;

namespace AlbiaReborn.Creatures
{
    /// <summary>
    /// Assembles sensory inputs for neural networks.
    /// Detects: food, water, threats, same species, terrain.
    /// </summary>
    public class SensorySystem
    {
        private Organism _organism;
        private float _sensoryRange;
        
        // Cached for neural input assembly
        private float _nearestFoodDist = float.MaxValue;
        private float _nearestWaterDist = float.MaxValue;
        private float _nearestSameSpeciesDist = float.MaxValue;
        private float _nearestThreatDist = float.MaxValue;
        private Vector3 _nearestFoodPos;
        private PlantOrganism _nearestPlant;

        public SensorySystem(Organism organism)
        {
            _organism = organism;
            _sensoryRange = 10f + organism.Genome?.GetGene(Genetics.GenomeData.SENSORY_RANGE) * 20f ?? 10f;
        }

        /// <summary>
        /// Update called before neural tick.
        /// </summary>
        public void ScanEnvironment()
        {
            _nearestFoodDist = float.MaxValue;
            _nearestWaterDist = float.MaxValue;
            _nearestSameSpeciesDist = float.MaxValue;
            _nearestThreatDist = float.MaxValue;
            _nearestPlant = null;

            // Use Physics.OverlapSphere for spatial query (efficient)
            Collider[] nearby = Physics.OverlapSphere(_organism.transform.position, _sensoryRange);
            
            foreach (var col in nearby)
            {
                // Skip self
                if (col.gameObject == _organism.gameObject) continue;

                float dist = Vector3.Distance(_organism.transform.position, col.transform.position);
                float normalizedDist = 1f - (dist / _sensoryRange);

                // Detect food (plants)
                PlantOrganism plant = col.GetComponent<PlantOrganism>();
                if (plant != null && plant.Energy > 0)
                {
                    if (dist < _nearestFoodDist)
                    {
                        _nearestFoodDist = dist;
                        _nearestFoodPos = plant.transform.position;
                        _nearestPlant = plant;
                    }
                }

                // Detect water (based on voxel type)
                // TODO: Query voxel system for water

                // Detect same species
                Organism other = col.GetComponent<Organism>();
                if (other != null && other.Species?.SpeciesName == _organism.Species?.SpeciesName)
                {
                    if (dist < _nearestSameSpeciesDist)
                    {
                        _nearestSameSpeciesDist = dist;
                    }
                }

                // Detect threats (Grendels, predators)
                if (other != null && other.Species?.SpeciesName == "Grendel")
                {
                    if (dist < _nearestThreatDist)
                    {
                        _nearestThreatDist = dist;
                    }
                }
            }
        }

        /// <summary>
        /// Returns normalized value (1.0 = at tile, 0.0 = out of range).
        /// </summary>
        public float GetNearestFood(out PlantOrganism plant)
        {
            plant = _nearestPlant;
            return _nearestFoodDist < float.MaxValue 
                ? 1f - (_nearestFoodDist / _sensoryRange) 
                : 0f;
        }

        public float GetNearestFood()
        {
            return _nearestFoodDist < float.MaxValue 
                ? 1f - (_nearestFoodDist / _sensoryRange) 
                : 0f;
        }

        public float GetNearestWater()
        {
            return _nearestWaterDist < float.MaxValue
                ? 1f - (_nearestWaterDist / _sensoryRange)
                : 0f;
        }

        public float GetNearestSameSpecies()
        {
            return _nearestSameSpeciesDist < float.MaxValue
                ? 1f - (_nearestSameSpeciesDist / _sensoryRange)
                : 0f;
        }

        public float GetNearestThreat()
        {
            return _nearestThreatDist < float.MaxValue
                ? 1f - (_nearestThreatDist / _sensoryRange)
                : 0f;
        }

        public Vector3 GetNearestFoodPosition()
        {
            return _nearestFoodPos;
        }

        public float GetTemperature()
        {
            // TODO: Query ClimateSystem
            return 0.5f;
        }

        public float GetLightLevel()
        {
            // TODO: Query based on time of day
            return 1f;
        }

        public Vector3 GetNearestFoodDirection()
        {
            if (_nearestFoodDist >= float.MaxValue)
                return Vector3.zero;
            
            return (_nearestFoodPos - _organism.transform.position).normalized;
        }
    }
}
