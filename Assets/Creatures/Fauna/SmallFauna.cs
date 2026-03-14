using System.Collections.Generic;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Genetics;
using Albia.Creatures.Neural;

namespace Albia.Creatures.Fauna
{
    /// <summary>
    /// Small fauna (rodents, fish, birds) - 15 neurons.
    /// Part of the food web.
    /// </summary>
    public class SmallFauna : Organism
    {
        [Header("Fauna Settings")]
        public FaunaType Type = FaunaType.Rodent;
        public float FleeDistance = 15f;
        
        private Organism _nearestPredator;
        private float _reproductionCooldown = 0f;

        protected override void InitializeFromGenome()
        {
            // Simplified neural: 15 neurons
            Brain = new NeuralNet(15, 8, 8, Genome);
            
            base.InitializeFromGenome();
        }

        protected override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Scan for predators
            _nearestPredator = FindNearestPredator();
            if (_nearestPredator != null)
            {
                float dist = Vector3.Distance(transform.position, _nearestPredator.transform.position);
                if (dist < FleeDistance)
                {
                    Chemicals.Apply(Biochemistry.ChemicalType.Fear, 0.1f);
                    FleeFrom(_nearestPredator.transform.position);
                }
            }

            // Breeding
            _reproductionCooldown -= deltaTime;
            if (_reproductionCooldown <= 0 && Stage == LifecycleStage.Adult)
            {
                TryBreed();
                _reproductionCooldown = 60f; // 1 minute
            }
        }

        Organism FindNearestPredator()
        {
            var nearby = PopulationRegistry.Instance?.GetOrganismsInRange(transform.position, FleeDistance);
            if (nearby == null) return null;
            
            Organism nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var other in nearby)
            {
                // Predators have high aggression gene
                if (other.Genome?.GetGene(GenomeData.AGGRESSION_RATE) > 0.6f)
                {
                    float dist = Vector3.Distance(transform.position, other.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = other;
                    }
                }
            }
            
            return nearest;
        }

        void FleeFrom(Vector3 threatPos)
        {
            Vector3 fleeDir = (transform.position - threatPos).normalized;
            transform.position += fleeDir * MoveSpeed * Time.deltaTime;
        }

        void TryBreed()
        {
            // Find nearby same species
            var nearby = PopulationRegistry.Instance?.GetOrganismsInRange(transform.position, 5f);
            if (nearby == null) return;
            
            foreach (var other in nearby)
            {
                if (other == this) continue;
                if (other.Species != Species) continue;
                if ((other as SmallFauna)?._reproductionCooldown > 0) continue;
                
                // Breed
                var offspringGenome = GeneticsSystem.Breed(Genome, other.Genome);
                SpawnOffspring(offspringGenome);
                break;
            }
        }

        void SpawnOffspring(GenomeData genome)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 3f;
            
            GameObject offspring = Instantiate(gameObject, spawnPos, Quaternion.identity);
            var faun = offspring.GetComponent<SmallFauna>();
            if (faun != null)
            {
                faun.Genome = genome;
                faun.Species = Species;
                faun.Stage = LifecycleStage.Juvenile;
                faun.Energy = MaxEnergy * 0.5f;
                faun.InitializeFromGenome();
            }
        }

        float MoveSpeed => 3f + Genome?.GetGene(0) * 5f ?? 3f;
    }

    public enum FaunaType
    {
        Rodent,
        Fish,
        Bird
    }
}
