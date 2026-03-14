using System;
using UnityEngine;
using AlbiaReborn.Creatures.Genetics;

namespace AlbiaReborn.Creatures
{
    /// <summary>
    /// Handles reproduction: breeding cooldowns, pregnancy, offspring spawning.
    /// </summary>
    public class ReproductionSystem : MonoBehaviour
    {
        [Header("Requirements")]
        public float MinBreedingAge = 120f; // 2 minutes
        public float BreedingCooldown = 300f; // 5 minutes between breedings
        public float MinEnergyToBreed = 60f;
        public float ReproductionDriveThreshold = 0.7f;
        
        [Header("State")]
        public bool IsPregnant = false;
        public float PregnancyProgress = 0f;
        public float PregnancyDuration = 180f; // 3 minutes gestation
        
        private Organism _organism;
        private float _lastBreedingTime = -999f;
        private GenomeData _partnerGenome;

        // Events
        public event Action OnBecamePregnant;
        public event Action OnGaveBirth;

        void Awake()
        {
            _organism = GetComponent<Organism>();
        }

        void Update()
        {
            if (IsPregnant)
            {
                UpdatePregnancy();
            }
            else
            {
                CheckBreedingReadiness();
            }
        }

        void CheckBreedingReadiness()
        {
            if (_organism == null) return;

            // Check conditions
            if (_organism.Stage != LifecycleStage.Adult) return;
            if (_organism.Age < MinBreedingAge) return;
            if (Time.time - _lastBreedingTime < BreedingCooldown) return;
            if (_organism.Energy < MinEnergyToBreed) return;
            
            // Drive-based readiness
            // Chemical state drives reproduction desire
        }

        /// <summary>
        /// Attempt to breed with nearby partner.
        /// </summary>
        public bool TryBreed(Organism partner)
        {
            if (!CanBreed(partner)) return false;

            // Both need high reproduction drive
            float myDrive = _organism.Chemicals.GetChemical(Biochemistry.ChemicalType.Affection);
            float partnerDrive = partner.Chemicals.GetChemical(Biochemistry.ChemicalType.Affection);
            
            if (myDrive < ReproductionDriveThreshold || partnerDrive < ReproductionDriveThreshold)
                return false;

            // Become pregnant (female initiates pregnancy)
            // For simplicity: this organism becomes pregnant
            Impregnate(partner.Genome);
            
            // Partner enters cooldown
            var partnerRepro = partner.GetComponent<ReproductionSystem>();
            if (partnerRepro != null)
            {
                partnerRepro._lastBreedingTime = Time.time;
            }

            return true;
        }

        bool CanBreed(Organism partner)
        {
            if (partner == null) return false;
            if (partner.Species != _organism.Species) return false;
            if (partner.Stage != LifecycleStage.Adult) return false;
            if (partner.GetComponent<ReproductionSystem>()?.IsPregnant == true) return false;
            
            return true;
        }

        void Impregnate(GenomeData partnerGenome)
        {
            IsPregnant = true;
            PregnancyProgress = 0f;
            _partnerGenome = partnerGenome;
            _lastBreedingTime = Time.time;
            
            OnBecamePregnant?.Invoke();
            
            Debug.Log($"{_organism.OrganismName} is pregnant!");
        }

        void UpdatePregnancy()
        {
            PregnancyProgress += Time.deltaTime / PregnancyDuration;
            
            // Energy cost of pregnancy
            _organism.Energy -= 0.05f * Time.deltaTime;
            
            if (PregnancyProgress >= 1f)
            {
                GiveBirth();
            }
        }

        void GiveBirth()
        {
            IsPregnant = false;
            
            // Create offspring genome via crossover
            GenomeData offspringGenome = GeneticsSystem.Breed(_organism.Genome, _partnerGenome);
            
            // Spawn offspring
            SpawnOffspring(offspringGenome);
            
            // Energy cost
            _organism.Energy -= 20f;
            
            // Chemicals
            _organism.Chemicals.Apply(Biochemistry.ChemicalType.Reward, 0.8f);
            _organism.Chemicals.SetChemical(Biochemistry.ChemicalType.Affection, 0.5f);
            
            OnGaveBirth?.Invoke();
            
            Debug.Log($"{_organism.OrganismName} gave birth!");
        }

        void SpawnOffspring(GenomeData genome)
        {
            // Spawn near parent
            Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * 2f;
            spawnPos.y = transform.position.y + 1f;

            // Create offspring gameobject (instantiate parent prefab)
            GameObject offspringObj = Instantiate(gameObject, spawnPos, Quaternion.identity);
            
            // Get components
            var offspring = offspringObj.GetComponent<Norn>();
            if (offspring != null)
            {
                offspring.Genome = genome;
                offspring.Species = _organism.Species;
                offspring.Stage = LifecycleStage.Juvenile;
                offspring.Age = 0f;
                offspring.Energy = offspring.MaxEnergy * 0.8f;
                
                // Initialize systems
                offspring.InitializeFromGenome();
            }
            
            // Register with population
            PopulationRegistry.Instance?.Register(offspring);
        }

        /// <summary>
        /// Get pregnancy progress (0-1).
        /// </summary>
        public float GetPregnancyProgress()
        {
            return IsPregnant ? PregnancyProgress : 0f;
        }
    }
}
