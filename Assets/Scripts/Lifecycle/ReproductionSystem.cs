using UnityEngine;
using Albia.Core;
using Albia.Creatures;
using System;

namespace Albia.Lifecycle
{
    /// <summary>
    /// Handles reproduction with genetic crossover.
    /// MVP: Simple spawn with genome merging
    /// Full: Pregnancy duration, mate selection, gestation
    /// </summary>
    public class ReproductionSystem : MonoBehaviour
    {
        public static ReproductionSystem Instance { get; private set; }

        [Header("Reproduction Settings")]
        [SerializeField] private GameObject nornPrefab;
        [SerializeField] private float reproductionCost = 30f; // Energy cost
        [SerializeField] private float reproductionThreshold = 70f; // Min energy
        [SerializeField] private int minAgeForReproduction = 20; // Min age (adult)

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Check if two organisms can reproduce
        /// </summary>
        public bool CanReproduce(Norn parentA, Norn parentB)
        {
            if (parentA == null || parentB == null) return false;
            if (parentA == parentB) return false; // No self-reproduction
            if (!parentA.IsAlive || !parentB.IsAlive) return false;
            
            // Must both be adults
            if (parentA.Stage != LifecycleStage.Adult || parentB.Stage != LifecycleStage.Adult)
                return false;
            
            // Must have enough energy
            if (parentA.Energy < reproductionThreshold || parentB.Energy < reproductionThreshold)
                return false;
            
            // Must be old enough
            if (parentA.Age < minAgeForReproduction || parentB.Age < minAgeForReproduction)
                return false;
            
            // MVP: No gender check, simple binary reproduction
            // Full: Gender, species compatibility
            
            return true;
        }

        /// <summary>
        /// Attempt reproduction between two Norns
        /// </summary>
        public bool TryReproduce(Norn parentA, Norn parentB)
        {
            if (!CanReproduce(parentA, parentB)) return false;

            // Create offspring genome
            GenomeData offspringGenome = GenomeData.Breed(parentA.Genome, parentB.Genome);

            // Calculate position
            Vector3 offspringPos = Vector3.Lerp(parentA.transform.position, parentB.transform.position, 0.5f);
            
            // Spawn offspring
            SpawnOffspring(offspringPos, offspringGenome, parentA, parentB);

            // Energy cost
            // Use reflection or modify base class - for MVP just log
            Debug.Log($"[Reproduction] {parentA.name} and {parentB.name} reproduced");

            // SCALES TO: Pregnancy system, gestation, birth notification
            
            return true;
        }

        /// <summary>
        /// Spawn offspring with given genome
        /// </summary>
        private GameObject SpawnOffspring(Vector3 position, GenomeData genome, Organism parentA, Organism parentB)
        {
            if (nornPrefab == null)
            {
                Debug.LogError("[Reproduction] Norn prefab not assigned!");
                return null;
            }

            GameObject offspring = Instantiate(nornPrefab, position, Quaternion.identity);
            Norn norn = offspring.GetComponent<Norn>();
            
            if (norn != null)
            {
                // Initialize with genome
                norn.InitializeFromGenome(genome, parentA.name, parentB.name);
                
                // Register with population
                PopulationRegistry.Instance?.RegisterOrganism(norn);
                
                // Fire event
                // SCALES TO: PopulationRegistry event
            }

            return offspring;
        }

        /// <summary>
        /// Spawn initial population with random genomes
        /// </summary>
        public GameObject SpawnRandom(Vector3 position)
        {
            if (nornPrefab == null) return null;

            GenomeData randomGenome = GenomeData.GenerateRandom(new Albia.Core.ForestNorn());
            
            GameObject offspring = Instantiate(nornPrefab, position, Quaternion.identity);
            Norn norn = offspring.GetComponent<Norn>();
            
            if (norn != null)
            {
                norn.InitializeFromGenome(randomGenome, null, null);
                PopulationRegistry.Instance?.RegisterOrganism(norn);
            }

            return offspring;
        }
    }
}