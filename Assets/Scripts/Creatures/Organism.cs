using System;
using UnityEngine;
using AlbiaReborn.Creatures.Genetics;
using AlbiaReborn.Creatures.Biochemistry;
using AlbiaReborn.Creatures.Neural;

namespace AlbiaReborn.Creatures
{
    /// <summary>
    /// Base class for all living organisms.
    /// Manages energy, lifecycle, genome, chemicals.
    /// </summary>
    public class Organism : MonoBehaviour
    {
        [Header("Identification")]
        public string OrganismName;
        public SpeciesTemplate Species;
        public Guid OrganismId { get; private set; }
        
        [Header("Lifecycle")]
        public LifecycleStage Stage = LifecycleStage.Juvenile;
        public float Age = 0f; // seconds
        
        [Header("Energy")]
        public float Energy = 100f;
        public float MaxEnergy = 100f;
        public float MetabolismRate = 1f;

        [Header("Systems")]
        public GenomeData Genome;
        public ChemicalState Chemicals { get; protected set; }
        public NeuralNet Brain { get; protected set; }

        // Lifecycle thresholds (from genome)
        private float _adultThreshold = 120f; // 2 minutes
        private float _elderThreshold = 600f; // 10 minutes

        // Events
        public event Action OnDeath;
        public event Action OnReproduce;
        public event Action OnStageChanged;

        // Timing
        private float _biochemistryTimer = 0f;
        private const float BiochemistryTickRate = 0.5f; // 2Hz

        void Awake()
        {
            OrganismId = Guid.NewGuid();
            Chemicals = new ChemicalState();
        }

        void Start()
        {
            if (Genome == null && Species != null)
            {
                Genome = GenomeData.FromTemplate(Species);
            }
            
            InitializeFromGenome();
            PopulationRegistry.Instance?.Register(this);
        }

        /// <summary>
        /// Initialize systems from genome.
        /// </summary>
        protected virtual void InitializeFromGenome()
        {
            if (Genome == null)
            {
                Debug.LogError($"{name} has no genome!");
                return;
            }

            // Metabolism from genome
            float metabolismGene = Genome.GetGene(GenomeData.METABOLISM_RATE);
            MetabolismRate = 0.5f + metabolismGene * 2f;
            MaxEnergy = 80f + Genome.GetGene(0) * 40f; // Size gene
            Energy = MaxEnergy * 0.8f; // Start at 80%

            // Initialize neural net
            if (Species != null)
            {
                Brain = new NeuralNet(
                    Species.NeuralInputCount,
                    Species.NeuralHiddenCount,
                    Species.NeuralOutputCount,
                    Genome
                );
            }

            // Chemical baselines from genome
            Chemicals.SetChemical(ChemicalType.Fear, Genome.GetGene(GenomeData.FEAR_BASELINE) * 0.2f);
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Main tick - metabolism, chemicals, lifecycle.
        /// </summary>
        protected virtual void Tick(float deltaTime)
        {
            // Age
            Age += deltaTime;

            // Energy drain (metabolism)
            float energyCost = MetabolismRate * deltaTime;
            Energy -= energyCost;

            // Chemical update (2Hz)
            _biochemistryTimer += deltaTime;
            if (_biochemistryTimer >= BiochemistryTickRate)
            {
                _biochemistryTimer = 0f;
                Chemicals.Tick(BiochemistryTickRate, Genome);
            }

            // Update lifecycle stage
            UpdateLifecycleStage();

            // Check death
            if (Energy <= 0f)
            {
                Die();
            }
        }

        private void UpdateLifecycleStage()
        {
            LifecycleStage newStage = Stage;
            
            if (Age > _elderThreshold)
                newStage = LifecycleStage.Elder;
            else if (Age > _adultThreshold)
                newStage = LifecycleStage.Adult;

            if (newStage != Stage)
            {
                Stage = newStage;
                OnStageChanged?.Invoke();
            }
        }

        /// <summary>
        /// Add energy from eating.
        /// </summary>
        public void Consume(float energy)
        {
            Energy = Mathf.Min(Energy + energy, MaxEnergy);
            Chemicals.Apply(ChemicalType.Hunger, -0.3f);
            Chemicals.Apply(ChemicalType.Satisfaction, 0.1f);
        }

        /// <summary>
        /// Take damage.
        /// </summary>
        public void TakeDamage(float damage)
        {
            Energy -= damage;
            Chemicals.Apply(ChemicalType.Pain, damage / MaxEnergy);
        }

        /// <summary>
        /// Die and cleanup.
        /// </summary>
        protected virtual void Die()
        {
            Energy = 0f;
            OnDeath?.Invoke();
            PopulationRegistry.Instance?.Unregister(this);
            
            // Deactivate - cleanup happens in subclasses (corpse creation)
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            PopulationRegistry.Instance?.Unregister(this);
        }
    }

    public enum LifecycleStage
    {
        Juvenile,
        Adult,
        Elder
    }
}
