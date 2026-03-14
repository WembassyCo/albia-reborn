using UnityEngine;
using AlbiaReborn.Creatures.Neural;

namespace AlbiaReborn.Creatures
{
    /// <summary>
    /// Norn - the player's creatures.
    /// Full complexity: genetics, biochemistry, neural learning, action execution.
    /// </summary>
    public class Norn : Organism
    {
        [Header("Norn Specific")]
        public float LearningRate = 0.1f;
        public HebbianLearning Learner;

        // Systems
        private SensorySystem _sensory;
        private ActionSystem _actionSystem;
        private float[] _sensoryInputs;

        // Timing
        private float _neuralTimer = 0f;
        private const float NeuralTickRate = 0.25f; // 4Hz

        protected override void InitializeFromGenome()
        {
            base.InitializeFromGenome();

            // Initialize systems
            _sensory = new SensorySystem(this);
            
            _actionSystem = GetComponent<ActionSystem>();
            if (_actionSystem == null)
            {
                _actionSystem = gameObject.AddComponent<ActionSystem>();
            }
            _actionSystem.Initialize(this, _sensory);

            // Learning setup
            LearningRate = Genome.GetGene(Genetics.GenomeData.LEARNING_RATE);
            if (Brain != null)
            {
                Learner = new HebbianLearning(Brain, LearningRate);
            }

            _sensoryInputs = new float[Brain?.InputCount ?? 30];

            OrganismName = GenerateName();
        }

        protected override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            // Scan environment
            _sensory?.ScanEnvironment();

            // Learning from chemicals
            if (Learner != null)
            {
                float rewardSignal = Chemicals.GetChemical(Biochemistry.ChemicalType.Reward);
                float painSignal = Chemicals.GetChemical(Biochemistry.ChemicalType.Pain);

                if (rewardSignal > 0.5f)
                {
                    Learner.LearnFromReward(rewardSignal);
                    Chemicals.SetChemical(Biochemistry.ChemicalType.Reward, rewardSignal * 0.5f);
                }

                if (painSignal > 0.7f)
                {
                    Learner.LearnFromPain(painSignal);
                    Chemicals.SetChemical(Biochemistry.ChemicalType.Pain, painSignal * 0.5f);
                }
            }

            // Neural tick (4Hz)
            _neuralTimer += deltaTime;
            if (_neuralTimer >= NeuralTickRate)
            {
                _neuralTimer = 0f;
                UpdateNeuralAndAct();
            }
        }

        /// <summary>
        /// Assemble sensory inputs for neural net.
        /// </summary>
        private float[] AssembleInputs()
        {
            int idx = 0;

            // Chemical inputs (12)
            foreach (var chem in System.Enum.GetValues(typeof(Biochemistry.ChemicalType)))
            {
                _sensoryInputs[idx++] = Chemicals.GetChemical((Biochemistry.ChemicalType)chem);
            }

            // Environmental sensors (filled if room)
            if (idx < _sensoryInputs.Length)
                _sensoryInputs[idx++] = _sensory.GetNearestFood();
            if (idx < _sensoryInputs.Length)
                _sensoryInputs[idx++] = _sensory.GetNearestWater();
            if (idx < _sensoryInputs.Length)
                _sensoryInputs[idx++] = _sensory.GetNearestSameSpecies();
            if (idx < _sensoryInputs.Length)
                _sensoryInputs[idx++] = _sensory.GetNearestThreat();
            if (idx < _sensoryInputs.Length)
                _sensoryInputs[idx++] = _sensory.GetTemperature();
            if (idx < _sensoryInputs.Length)
                _sensoryInputs[idx++] = _sensory.GetLightLevel();

            // Fill remaining with zeros
            while (idx < _sensoryInputs.Length)
            {
                _sensoryInputs[idx++] = 0f;
            }

            return _sensoryInputs;
        }

        private void UpdateNeuralAndAct()
        {
            if (Brain == null || _actionSystem == null) return;

            float[] inputs = AssembleInputs();
            float[] outputs = Brain.Forward(inputs);
            int actionIndex = Brain.GetWinningOutput();

            // Check if preconditions met
            ActionType desiredAction = (ActionType)actionIndex;
            if (!_actionSystem.CanExecute(desiredAction))
            {
                // Try next highest activation
                // (simplified: just pick next index)
                actionIndex = (actionIndex + 1) % System.Enum.GetValues(typeof(ActionType)).Length;
                desiredAction = (ActionType)actionIndex;
            }

            // Record for learning
            Learner?.RecordAction(actionIndex, inputs);

            // Execute action
            _actionSystem.ExecuteAction(desiredAction);
        }

        private string GenerateName()
        {
            string[] prefixes = { "Al", "Bel", "Cor", "Dor", "Em", "Fir", "Gor", "Hal", "Ith", "Jor" };
            string[] suffixes = { "ia", "an", "os", "in", "ar", "el", "on", "is", "an", "el" };
            
            int hash = OrganismId.GetHashCode();
            string prefix = prefixes[Mathf.Abs(hash) % prefixes.Length];
            string suffix = suffixes[Mathf.Abs(hash >> 8) % suffixes.Length];
            
            return prefix + suffix;
        }

        /// <summary>
        /// Override death to create corpse.
        /// </summary>
        protected override void Die()
        {
            // Create corpse before destroying
            if (CorpsePrefab != null)
            {
                GameObject corpse = Instantiate(CorpsePrefab, transform.position, Quaternion.identity);
                Corpse corpseScript = corpse.GetComponent<Corpse>();
                if (corpseScript != null)
                {
                    corpseScript.TotalBiomass = MaxEnergy * 0.5f; // Half energy as biomass
                }
            }

            base.Die();
        }

        public GameObject CorpsePrefab;
    }
}
