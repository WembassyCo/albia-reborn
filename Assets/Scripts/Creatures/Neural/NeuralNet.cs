using System;
using System.Linq;
using UnityEngine;

namespace AlbiaReborn.Creatures.Neural
{
    /// <summary>
    /// Feed-forward neural network with learnable weights.
    /// </summary>
    public class NeuralNet
    {
        public int InputCount { get; }
        public int HiddenCount { get; }
        public int OutputCount { get; }

        // Weight matrices
        private float[,] _weightsInputHidden;  // [hidden, input]
        private float[,] _weightsHiddenOutput; // [output, hidden]
        
        // Bias vectors
        private float[] _biasHidden;
        private float[] _biasOutput;

        // Recent activations (for learning)
        private float[] _lastInputs;
        private float[] _lastHidden;
        private float[] _lastOutputs;

        public NeuralNet(int inputs, int hidden, int outputs, Genetics.GenomeData genome)
        {
            InputCount = inputs;
            HiddenCount = hidden;
            OutputCount = outputs;

            _weightsInputHidden = new float[hidden, inputs];
            _weightsHiddenOutput = new float[outputs, hidden];
            _biasHidden = new float[hidden];
            _biasOutput = new float[outputs];

            _lastInputs = new float[inputs];
            _lastHidden = new float[hidden];
            _lastOutputs = new float[outputs];

            InitializeFromGenome(genome);
        }

        /// <summary>
        /// Initialize weights from genome (genes 64-191).
        /// </summary>
        private void InitializeFromGenome(Genetics.GenomeData genome)
        {
            int geneIndex = Genetics.GenomeData.NEURAL_INIT_START;

            // Input -> Hidden weights
            for (int h = 0; h < HiddenCount; h++)
            {
                for (int i = 0; i < InputCount; i++)
                {
                    _weightsInputHidden[h, i] = GenomeToWeight(genome.GetGene(geneIndex++));
                }
                _biasHidden[h] = GenomeToWeight(genome.GetGene(geneIndex++));
            }

            // Hidden -> Output weights
            for (int o = 0; o < OutputCount; o++)
            {
                for (int h = 0; h < HiddenCount; h++)
                {
                    _weightsHiddenOutput[o, h] = GenomeToWeight(genome.GetGene(geneIndex++));
                }
                _biasOutput[o] = GenomeToWeight(genome.GetGene(geneIndex++));
            }
        }

        private float GenomeToWeight(float gene)
        {
            // Map 0-1 genome value to -1 to 1 weight
            return (gene - 0.5f) * 2f;
        }

        /// <summary>
        /// Forward pass: inputs -> hidden (tanh) -> output (tanh).
        /// Returns output activations.
        /// </summary>
        public float[] Forward(float[] inputs)
        {
            if (inputs.Length != InputCount)
                throw new ArgumentException($"Expected {InputCount} inputs, got {inputs.Length}");

            Array.Copy(inputs, _lastInputs, InputCount);

            // Hidden layer
            for (int h = 0; h < HiddenCount; h++)
            {
                float sum = _biasHidden[h];
                for (int i = 0; i < InputCount; i++)
                {
                    sum += _weightsInputHidden[h, i] * inputs[i];
                }
                _lastHidden[h] = Tanh(sum);
            }

            // Output layer
            for (int o = 0; o < OutputCount; o++)
            {
                float sum = _biasOutput[o];
                for (int h = 0; h < HiddenCount; h++)
                {
                    sum += _weightsHiddenOutput[o, h] * _lastHidden[h];
                }
                _lastOutputs[o] = Tanh(sum);
            }

            return (float[])_lastOutputs.Clone();
        }

        /// <summary>
        /// Get index of winning output neuron.
        /// </summary>
        public int GetWinningOutput()
        {
            return _lastOutputs
                .Select((v, i) => new { Value = v, Index = i })
                .OrderByDescending(x => x.Value)
                .First().Index;
        }

        /// <summary>
        /// Get recent activations for learning.
        /// </summary>
        public (float[] inputs, float[] hidden, float[] outputs) GetRecentActivations()
        {
            return (
                (float[])_lastInputs.Clone(),
                (float[])_lastHidden.Clone(),
                (float[])_lastOutputs.Clone()
            );
        }

        private float Tanh(float x)
        {
            // Hyperbolic tangent
            return (float)Math.Tanh(x);
        }

        /// <summary>
        /// Update a specific weight (for learning).
        /// </summary>
        public void UpdateWeight(int layer, int from, int to, float delta)
        {
            if (layer == 0)
            {
                _weightsInputHidden[to, from] = Mathf.Clamp(_weightsInputHidden[to, from] + delta, -1f, 1f);
            }
            else
            {
                _weightsHiddenOutput[to, from] = Mathf.Clamp(_weightsHiddenOutput[to, from] + delta, -1f, 1f);
            }
        }

        /// <summary>
        /// Export weights to array (for save).
        /// </summary>
        public float[] ExportWeights()
        {
            // Flatten all weights
            int size = HiddenCount * InputCount + OutputCount * HiddenCount + HiddenCount + OutputCount;
            float[] weights = new float[size];
            int idx = 0;

            for (int h = 0; h < HiddenCount; h++)
                for (int i = 0; i < InputCount; i++)
                    weights[idx++] = _weightsInputHidden[h, i];

            for (int o = 0; o < OutputCount; o++)
                for (int h = 0; h < HiddenCount; h++)
                    weights[idx++] = _weightsHiddenOutput[o, h];

            for (int h = 0; h < HiddenCount; h++)
                weights[idx++] = _biasHidden[h];

            for (int o = 0; o < OutputCount; o++)
                weights[idx++] = _biasOutput[o];

            return weights;
        }
    }
}
