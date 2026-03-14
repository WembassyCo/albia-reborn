using UnityEngine;
using System.Collections.Generic;
using AlbiaReborn.Creatures;

namespace AlbiaReborn.Creatures.Grendels
{
    /// <summary>
    /// Grendel - genetically aggressive, attacks Norns.
    /// Week 16: Grendel Raid.
    /// </summary>
    public class Grendel : Organism
    {
        [Header("Grendel Traits")]
        public float AttackRange = 2f;
        public float AttackDamage = 15f;
        public float ScentMarkingRate = 0.5f;
        public float TerritoryRadius = 30f;
        
        [Header("Raiding")]
        public float StructureInterest = 0.7f; // Gene-based
        public bool IsRaiding = false;

        private float _lastScentMark = 0f;
        private float _lastAttackTime = 0f;
        private GameObject _territoryMarkerPrefab;

        protected override void InitializeFromGenome()
        {
            // Override with Grendel defaults
            if (Genome != null)
            {
                // Force high aggression
                Genome.SetGene(Genetics.GenomeData.AGGRESSION_RATE, Random.Range(0.6f, 0.9f));
                Genome.SetGene(Genetics.GenomeData.FEAR_DECAY, Random.Range(0.7f, 0.95f));
                Genome.SetGene(Genetics.GenomeData.TERRITORIAL_STRESS, Random.Range(0.5f, 0.8f));
            }
            
            base.InitializeFromGenome();
            
            // Grendel-specific values from genes
            if (Genome != null)
            {
                StructureInterest = Genome.GetGene(50); // Reserved gene
                AttackDamage = 10f + Genome.GetGene(Genetics.GenomeData.AGGRESSION_RATE) * 20f;
            }
        }

        protected override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Scent marking
            if (Time.time - _lastScentMark > ScentMarkingRate)
            {
                MarkScent();
                _lastScentMark = Time.time;
            }

            // Hunt Norns
            if (!IsRaiding)
            {
                FindAndAttackNorn();
            }
        }

        void MarkScent()
        {
            // Leave scent at current tile
            Vector3Int tile = Vector3Int.FloorToInt(transform.position);
            // TODO: Add to global scent manager
            // ScentManager.Instance.MarkTile(tile, this, 1.0f);
        }

        void FindAndAttackNorn()
        {
            var nearby = PopulationRegistry.Instance?.GetOrganismsInRange(transform.position, TerritoryRadius);
            if (nearby == null) return;
            
            foreach (var other in nearby)
            {
                if (other is Norn norn)
                {
                    float dist = Vector3.Distance(transform.position, norn.transform.position);
                    
                    if (dist < AttackRange && Time.time - _lastAttackTime > 1f)
                    {
                        AttackNorn(norn);
                        _lastAttackTime = Time.time;
                    }
                    else if (dist < TerritoryRadius)
                    {
                        // Move toward
                        Vector3 dir = (norn.transform.position - transform.position).normalized;
                        transform.position += dir * MoveSpeed * Time.deltaTime;
                    }
                    
                    break; // Focus on one target
                }
            }
        }

        void AttackNorn(Norn norn)
        {
            norn.TakeDamage(AttackDamage);
            Energy += AttackDamage * 0.3f;
            
            // Fear to nearby Norns
            var nearby = PopulationRegistry.Instance?.GetOrganismsInRange(transform.position, 10f);
            if (nearby != null)
            {
                foreach (var other in nearby)
                {
                    if (other is Norn nearbyNorn)
                    {
                        nearbyNorn.Chemicals?.Apply(Biochemistry.ChemicalType.Fear, 0.5f);
                    }
                }
            }
            
            // Reward for Grendel
            Chemicals.Apply(Biochemistry.ChemicalType.Reward, 0.5f);
        }

        float MoveSpeed => 5f;
    }
}
