using UnityEngine;
using AlbiaReborn.Creatures;
using AlbiaReborn.Creatures.Genetics;

namespace AlbiaReborn.Creatures.Predators
{
    /// <summary>
    /// Apex predator - 40 neurons, territorial hunting.
    /// Week 14: Full Ecology.
    /// </summary>
    public class ApexPredator : Organism
    {
        [Header("Predator")]
        public float TerritoryRadius = 20f;
        public float AttackRange = 1.5f;
        public float AttackStrength = 10f;
        
        [Header("State")]
        public Organism Target;
        public Vector3 TerritoryCenter;
        public bool IsInTerritory;

        private float _territorialStress;

        protected override void Awake()
        {
            base.Awake();
            TerritoryCenter = transform.position;
        }

        protected override void InitializeFromGenome()
        {
            // 40-neuron brain
            Brain = new Neural.NeuralNet(20, 12, 10, Genome);
            
            // Aggression from genome
            AttackStrength = 10f + Genome.GetGene(GenomeData.AGGRESSION_RATE) * 20f;
            
            base.InitializeFromGenome();
        }

        protected override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Update territory status
            float distToCenter = Vector3.Distance(transform.position, TerritoryCenter);
            IsInTerritory = distToCenter < TerritoryRadius;

            // Territorial stress
            if (!IsInTerritory)
            {
                Chemicals.Apply(Biochemistry.ChemicalType.Stress, 0.01f);
            }

            // Hunt
            if (Target == null)
            {
                FindPrey();
            }
            else
            {
                HuntTarget();
            }
        }

        void FindPrey()
        {
            var nearby = PopulationRegistry.Instance?.GetOrganismsInRange(transform.position, TerritoryRadius);
            if (nearby == null) return;
            
            foreach (var potential in nearby)
            {
                if (potential == this) continue;
                if (potential is SmallFauna || potential is Organism && !(potential is ApexPredator))
                {
                    Target = potential;
                    Debug.Log($"{name} targeting {Target.name}");
                    return;
                }
            }
        }

        void HuntTarget()
        {
            if (Target == null || Target.Energy <= 0)
            {
                Target = null;
                return;
            }

            // Move toward target
            Vector3 dir = (Target.transform.position - transform.position).normalized;
            transform.position += dir * MoveSpeed * Time.deltaTime;

            // Attack if in range
            float dist = Vector3.Distance(transform.position, Target.transform.position);
            if (dist < AttackRange)
            {
                Attack(Target);
            }
        }

        void Attack(Organism prey)
        {
            prey.TakeDamage(AttackStrength * Time.deltaTime);
            Energy += AttackStrength * 0.5f * Time.deltaTime; // Gain from damage
            
            if (prey.Energy <= 0)
            {
                // Kill
                Energy += prey.Energy * 0.3f; // Gain 30% of remaining
                Target = null;
            }
        }

        void OnDrawGizmos()
        {
            // Territory
            Gizmos.color = Color.red * 0.3f;
            Gizmos.DrawWireSphere(TerritoryCenter, TerritoryRadius);
            
            // Target
            if (Target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, Target.transform.position);
            }
        }

        float MoveSpeed => 4f;
    }
}
