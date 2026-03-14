using System;
using UnityEngine;

namespace Albia.Creatures.Neural
{
    /// <summary>
    /// Feed-forward neural network for creature decision making.
    /// Uses weights encoded in genome genes 64-191.
    /// </summary>
    [Serializable]
    public class NeuralNet
    {
        // Network architecture
        public int InputSize { get; private set; }
        public int HiddenSize { get; private set; }
        public int OutputSize { get; private set; }
        
        // Weights and biases
        private float[,] weightsInputHidden;
        private float[,] weightsHiddenOutput;
        private float[] hiddenBiases;
        private float[] outputBiases;
        
        // Cached hidden layer activations
        private float[] hiddenActivations;
        
        // Genome reference for learning
        private GenomeData genome;
        
        /// <summary>
        /// Creates a new neural network initialized from genome
        /// </summary>
        public NeuralNet(int inputSize, int hiddenSize, int outputSize, GenomeData genome)
        {
            InputSize = inputSize;
            HiddenSize = hiddenSize;
            OutputSize = outputSize;
            
            this.genome = genome ?? throw new ArgumentNullException(nameof(genome));
            
            InitializeArrays();
            LoadWeightsFromGenome();
        }

        /// <summary>
        /// Initializes weight and bias arrays
        /// </summary>
        private void InitializeArrays()
        {
            weightsInputHidden = new float[InputSize, HiddenSize];
            weightsHiddenOutput = new float[HiddenSize, OutputSize];
            hiddenBiases = new float[HiddenSize];
            outputBiases = new float[OutputSize];
            hiddenActivations = new float[HiddenSize];
        }

        /// <summary>
        /// Loads network weights from genome (genes 64-191)
        /// </summary>
        private void LoadWeightsFromGenome()
        {
            int geneIndex = 0;
            
            // Input to hidden weights
            for (int i = 0; i < InputSize; i++)
            {
                for (int h = 0; h < HiddenSize; h++)
                {
                    weightsInputHidden[i, h] = genome.GetNeuralWeight(geneIndex++);
                }
            }
            
            // Hidden biases
            for (int h = 0; h < HiddenSize; h++)
            {
                hiddenBiases[h] = genome.GetNeuralWeight(geneIndex++);
            }
            
            // Hidden to output weights
            for (int h = 0; h < HiddenSize; h++)
            {
                for (int o = 0; o < OutputSize; o++)
                {
                    weightsHiddenOutput[h, o] = genome.GetNeuralWeight(geneIndex++);
                }
            }
            
            // Output biases
            for (int o = 0; o < OutputSize; o++)
            {
                outputBiases[o] = genome.GetNeuralWeight(geneIndex++);
            }
        }

        /// <summary>
        /// Performs forward pass through the network
        /// </summary>
        /// <param name="inputs">Input values (length must match InputSize)</param>
        /// <returns>Output activations (length = OutputSize, values in range [-1, 1])</returns>
        public float[] Forward(float[] inputs)
        {
            if (inputs == null || inputs.Length != InputSize)
                throw new ArgumentException($"Inputs must have length {InputSize}", nameof(inputs));
            
            // Hidden layer computation
            for (int h = 0; h < HiddenSize; h++)
            {
                float sum = hiddenBiases[h];
                
                for (int i = 0; i < InputSize; i++)
                {
                    sum += inputs[i] * weightsInputHidden[i, h];
                }
                
                // Tanh activation for hidden layer
                hiddenActivations[h] = Tanh(sum);
            }
            
            // Output layer computation
            float[] outputs = new float[OutputSize];
            
            for (int o = 0; o < OutputSize; o++)
            {
                float sum = outputBiases[o];
                
                for (int h = 0; h < HiddenSize; h++)
                {
                    sum += hiddenActivations[h] * weightsHiddenOutput[h, o];
                }
                
                // Tanh activation for output layer (range -1 to 1)
                outputs[o] = Tanh(sum);
            }
            
            return outputs;
        }

        /// <summary>
        /// Hyperbolic tangent activation function
        /// </summary>
        private float Tanh(float x)
        {
            // Using Unity's Mathf.Tanh for consistency
            return Mathf.Clamp(Mathf.Tan(x), -1f, 1f);
        }

        /// <summary>
        /// Gets the weights for a specific connection (for learning)
        /// </summary>
        public float GetInputHiddenWeight(int input, int hidden)
        {
            return weightsInputHidden[input, hidden];
        }

        /// <summary>
        /// Gets the weights for a specific connection (for learning)
        /// </summary>
        public float GetHiddenOutputWeight(int hidden, int output)
        {
            return weightsHiddenOutput[hidden, output];
        }

        /// <summary>
        /// Updates a weight value (used by learning system)
        /// </summary>
        public void UpdateInputHiddenWeight(int input, int hidden, float delta)
        {
            weightsInputHidden[input, hidden] = Math.Clamp(
                weightsInputHidden[input, hidden] + delta, -1f, 1f);
        }

        /// <summary>
        /// Updates a weight value (used by learning system)
        /// </summary>
        public void UpdateHiddenOutputWeight(int hidden, int output, float delta)
        {
            weightsHiddenOutput[hidden, output] = Math.Clamp(
                weightsHiddenOutput[hidden, output] + delta, -1f, 1f);
        }

        /// <summary>
        /// Gets the current hidden layer activations
        /// </summary>
        public float[] GetHiddenActivations()
        {
            float[] copy = new float[HiddenSize];
            Array.Copy(hiddenActivations, copy, HiddenSize);
            return copy;
        }

        /// <summary>
        /// Saves current weights back to genome (call after learning)
        /// </summary>
        public void SaveWeightsToGenome()
        {
            int geneIndex = 0;
            
            // Input to hidden weights
            for (int i = 0; i < InputSize; i++)
            {
                for (int h = 0; h < HiddenSize; h++)
                {
                    genome.SetNeuralWeight(geneIndex++, weightsInputHidden[i, h]);
                }
            }
            
            // Hidden biases
            for (int h = 0; h < HiddenSize; h++)
            {
                genome.SetNeuralWeight(geneIndex++, hiddenBiases[h]);
            }
            
            // Hidden to output weights
            for (int h = 0; h < HiddenSize; h++)
            {
                for (int o = 0; o < OutputSize; o++)
                {
                    genome.SetNeuralWeight(geneIndex++, weightsHiddenOutput[h, o]);
                }
            }
            
            // Output biases
            for (int o = 0; o < OutputSize; o++)
            {
                genome.SetNeuralWeight(geneIndex++, outputBiases[o]);
            }
        }

        /// <summary>
        /// Creates a copy of this neural network with new genome
        /// </summary>
        public NeuralNet Clone()
        {
            var newGenome = genome.Clone();
            var clone = new NeuralNet(InputSize, HiddenSize, OutputSize, newGenome);
            return clone;
        }
    }
}
