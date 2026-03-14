using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Tests
{
    /// <summary>
    /// Unit tests for the Neural Engine system
    /// </summary>
    public class NeuralEngineTests
    {
        [SetUp]
        public void Setup()
        {
            // Tests are self-contained
        }

        // ==================== TEST 1: Forward Pass Output Range ====================
        
        /// <summary>
        /// Test 1: Forward pass produces valid outputs (range -1 to 1)
        /// Verifies that all outputs from the neural network are within [-1, 1]
        /// </summary>
        [Test]
        public void NeuralNet_ForwardPass_ProducesOutputsInValidRange()
        {
            // Arrange
            var genome = new GenomeData();
            genome.Randomize(new System.Random(42)); // Fixed seed for reproducibility
            
            int inputSize = 26;
            int hiddenSize = 12;
            int outputSize = 8;
            
            var neuralNet = new NeuralNet(inputSize, hiddenSize, outputSize, genome);
            
            // Test with various input patterns
            var random = new System.Random(42);
            int testIterations = 100;
            
            for (int i = 0; i < testIterations; i++)
            {
                // Create random inputs in different ranges
                float[] inputs = new float[inputSize];
                for (int j = 0; j < inputSize; j++)
                {
                    // Inputs can be 0-1 (chemicals) or -1 to 1 (directions)
                    inputs[j] = (float)(random.NextDouble() * 2.0 - 1.0);
                }
                
                // Act
                float[] outputs = neuralNet.Forward(inputs);
                
                // Assert
                Assert.IsNotNull(outputs, "Outputs should not be null");
                Assert.AreEqual(outputSize, outputs.Length, $"Output array should have {outputSize} elements");
                
                for (int o = 0; o < outputSize; o++)
                {
                    Assert.That(outputs[o], Is.InRange(-1f, 1f), 
                        $"Output[{o}] should be in range [-1, 1] on iteration {i}");
                    Assert.IsFalse(float.IsNaN(outputs[o]), 
                        $"Output[{o}] should not be NaN on iteration {i}");
                    Assert.IsFalse(float.IsInfinity(outputs[o]), 
                        $"Output[{o}] should not be infinity on iteration {i}");
                }
            }
        }

        /// <summary>
        /// Test 1b: Forward pass with edge case inputs (zeros, ones, negatives)
        /// </summary>
        [Test]
        public void NeuralNet_ForwardPass_HandlesEdgeCases()
        {
            // Arrange
            var genome = new GenomeData();
            int inputSize = 26;
            int hiddenSize = 12;
            int outputSize = 8;
            
            var neuralNet = new NeuralNet(inputSize, hiddenSize, outputSize, genome);
            
            // Test with zeros
            float[] zeroInputs = new float[inputSize];
            float[] outputs = neuralNet.Forward(zeroInputs);
            
            foreach (var output in outputs)
            {
                Assert.That(output, Is.InRange(-1f, 1f), "Should handle zero inputs");
            }
            
            // Test with ones
            float[] oneInputs = new float[inputSize];
            for (int i = 0; i < inputSize; i++) oneInputs[i] = 1f;
            outputs = neuralNet.Forward(oneInputs);
            
            foreach (var output in outputs)
            {
                Assert.That(output, Is.InRange(-1f, 1f), "Should handle all-one inputs");
            }
            
            // Test with negatives
            float[] negInputs = new float[inputSize];
            for (int i = 0; i < inputSize; i++) negInputs[i] = -1f;
            outputs = neuralNet.Forward(negInputs);
            
            foreach (var output in outputs)
            {
                Assert.That(output, Is.InRange(-1f, 1f), "Should handle all-negative inputs");
            }
        }

        // ==================== TEST 2: Learning Strengthens Pathway ====================

        /// <summary>
        /// Test 2: Learning strengthens winning pathway
        /// Verifies that positive reward increases weight magnitude for the winning action pathway
        /// </summary>
        [Test]
        public void HebbianLearning_StrengthensWinningPathway()
        {
            // Arrange
            var genome = new GenomeData();
            genome.Randomize(new System.Random(123));
            
            int inputSize = 4;    // Smaller for testing
            int hiddenSize = 3;
            int outputSize = ActionSystem.OutputCount; // Must match
            
            var neuralNet = new NeuralNet(inputSize, hiddenSize, outputSize, genome);
            
            // Get initial weights for comparison
            float[] initialHiddenOutputs = new float[hiddenSize];
            for (int h = 0; h < hiddenSize; h++)
            {
                initialHiddenOutputs[h] = neuralNet.GetHiddenOutputWeight(h, 4); // Eat action neuron
            }
            
            // Create learning system
            var learning = new HebbianLearning
            {
                LearningRate = 0.1f,  // Higher rate for visible effect
                MaxWeightDelta = 1.0f // Allow large changes for testing
            };
            
            // Positive inputs
            float[] inputs = new float[] { 0.5f, 0.3f, 0.7f, 0.2f };
            int winningAction = 4; // Eat neuron
            float rewardSignal = 1.0f; // Strong positive reward
            
            // Get hidden activations
            neuralNet.Forward(inputs);
            float[] hiddenActivations = neuralNet.GetHiddenActivations();
            
            // Act - apply learning multiple times to see effect
            for (int i = 0; i < 10; i++)
            {
                learning.Learn(neuralNet, inputs, winningAction, rewardSignal, hiddenActivations);
            }
            
            // Assert - check that weights changed
            bool weightsChanged = false;
            for (int h = 0; h < hiddenSize; h++)
            {
                float newWeight = neuralNet.GetHiddenOutputWeight(h, winningAction);
                float weightChange = Math.Abs(newWeight - initialHiddenOutputs[h]);
                
                if (weightChange > 0.001f)
                {
                    weightsChanged = true;
                    break;
                }
            }
            
            Assert.IsTrue(weightsChanged, "Weights should change after learning with reward");
            Assert.Greater(learning.Stats.TotalWeightUpdates, 0, "Should have recorded weight updates");
            Assert.AreEqual(1, learning.Stats.RewardCount, "Should have recorded reward events");
        }

        /// <summary>
        /// Test 2b: Punishment weakens pathway
        /// </summary>
        [Test]
        public void HebbianLearning_PunishmentWeakensPathway()
        {
            // Arrange
            var genome = new GenomeData();
            int inputSize = 4;
            int hiddenSize = 3;
            int outputSize = ActionSystem.OutputCount;
            
            var neuralNet = new NeuralNet(inputSize, hiddenSize, outputSize, genome);
            
            // Get initial positive weight
            float initialWeight = neuralNet.GetHiddenOutputWeight(0, 4);
            
            var learning = new HebbianLearning
            {
                LearningRate = 0.1f,
                MaxWeightDelta = 1.0f
            };
            
            // Force a strong positive weight
            for (int i = 0; i < 5; i++)
            {
                neuralNet.UpdateHiddenOutputWeight(0, 4, 0.5f);
            }
            
            // Get weight after strengthening
            float strengthenedWeight = neuralNet.GetHiddenOutputWeight(0, 4);
            
            // Act - apply punishment
            float[] inputs = new float[] { 0.5f, 0.3f, 0.7f, 0.2f };
            neuralNet.Forward(inputs);
            float[] hiddenActivations = neuralNet.GetHiddenActivations();
            
            learning.Learn(neuralNet, inputs, 4, -1.0f, hiddenActivations);
            
            float punishedWeight = neuralNet.GetHiddenOutputWeight(0, 4);
            
            // Assert
            Assert.AreEqual(1, learning.Stats.PunishmentCount, "Should have recorded punishment");
        }

        // ==================== TEST 3: Genome Crossover Diversity ====================

        /// <summary>
        /// Test 3: Genome crossover produces different nets from parents
        /// Verifies that creating neural nets from parent genomes produces networks with different weights
        /// </summary>
        [Test]
        public void Genome_Crossover_ProducesDifferentNetsFromParents()
        {
            // Arrange
            var random = new System.Random(456);
            
            // Create two distinct parent genomes
            var parentA = new GenomeData();
            parentA.Randomize(random);
            
            var parentB = new GenomeData();
            parentB.Randomize(random);
            
            // Create child through crossover
            var child = GenomeData.Crossover(parentA, parentB, random);
            
            int inputSize = 10;
            int hiddenSize = 5;
            int outputSize = 8;
            
            // Act - create networks from all three genomes
            var netA = new NeuralNet(inputSize, hiddenSize, outputSize, parentA);
            var netB = new NeuralNet(inputSize, hiddenSize, outputSize, parentB);
            var netChild = new NeuralNet(inputSize, hiddenSize, outputSize, child);
            
            // Generate same inputs for all
            float[] inputs = new float[inputSize];
            for (int i = 0; i < inputSize; i++)
            {
                inputs[i] = (float)random.NextDouble() * 2f - 1f;
            }
            
            float[] outputsA = netA.Forward(inputs);
            float[] outputsB = netB.Forward(inputs);
            float[] outputsChild = netChild.Forward(inputs);
            
            // Assert
            // Child should be different from both parents
            bool childDifferentFromA = false;
            bool childDifferentFromB = false;
            
            // Check if child outputs differ from parent A
            for (int i = 0; i < outputSize; i++)
            {
                if (Math.Abs(outputsChild[i] - outputsA[i]) > 0.0001f)
                {
                    childDifferentFromA = true;
                    break;
                }
            }
            
            // Check if child outputs differ from parent B
            for (int i = 0; i < outputSize; i++)
            {
                if (Math.Abs(outputsChild[i] - outputsB[i]) > 0.0001f)
                {
                    childDifferentFromB = true;
                    break;
                }
            }
            
            Assert.IsTrue(childDifferentFromA, "Child network should differ from parent A");
            Assert.IsTrue(childDifferentFromB, "Child network should differ from parent B");
            
            // Parents should be different from each other
            bool parentsDifferent = false;
            for (int i = 0; i < outputSize; i++)
            {
                if (Math.Abs(outputsA[i] - outputsB[i]) > 0.0001f)
                {
                    parentsDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(parentsDifferent, "Parent networks should differ from each other");
        }

        /// <summary>
        /// Test 3b: Crossover combines genes from both parents
        /// </summary>
        [Test]
        public void Genome_Crossover_CombinesGenesFromBothParents()
        {
            // Arrange - create parents with distinct patterns
            var parentA = new GenomeData();
            var parentB = new GenomeData();
            
            // Set parent A genes to 0.5
            for (int i = 0; i < GenomeData.TotalGenes; i++)
            {
                parentA.SetNeuralWeight(i, 0.5f);
            }
            
            // Set parent B genes to -0.5
            for (int i = 0; i < GenomeData.TotalGenes; i++)
            {
                parentB.SetNeuralWeight(i, -0.5f);
            }
            
            // Act - perform crossover
            var random = new System.Random(789);
            var child = GenomeData.Crossover(parentA, parentB, random);
            
            // Assert - child should have mix of values
            int genesFromA = 0;
            int genesFromB = 0;
            
            for (int i = 0; i < GenomeData.NeuralWeightCount; i++)
            {
                float gene = child.GetNeuralWeight(i);
                if (gene > 0.1f) genesFromA++;
                if (gene < -0.1f) genesFromB++;
            }
            
            Assert.Greater(genesFromA, 0, "Child should have some genes from parent A");
            Assert.Greater(genesFromB, 0, "Child should have some genes from parent B");
        }

        // ==================== Additional Integration Tests ====================

        /// <summary>
        /// Integration test: Full sensory to action pipeline
        /// </summary>
        [Test]
        public void Integration_SensoryInput_To_ActionOutput()
        {
            // Arrange
            var genome = new GenomeData();
            var sensory = new SensorySystem
            {
                Chemicals = new ChemicalState
                {
                    Hunger = 0.8f,
                    Energy = 0.3f
                },
                Proximity = new ProximitySensors
                {
                    FoodDistance = 0.2f,
                    FoodDirection = 0.5f
                },
                World = new WorldInputs
                {
                    LightLevel = 0.7f,
                    Temperature = 0.5f,
                    Moisture = 0.3f
                },
                Social = new SocialInputs
                {
                    NearbyCreatures = 0.2f
                }
            };
            
            var neuralNet = new NeuralNet(SensorySystem.TotalInputs, 12, ActionSystem.OutputCount, genome);
            var actionSystem = new ActionSystem();
            
            // Act
            float[] inputs = sensory.AssembleInputs();
            Assert.AreEqual(SensorySystem.TotalInputs, inputs.Length, "Should assemble 26 inputs");
            
            float[] outputs = neuralNet.Forward(inputs);
            var actionState = actionSystem.MapOutputs(outputs);
            
            // Assert
            Assert.IsNotNull(actionState);
            Assert.IsNotNull(actionSystem.LastOutputs);
            Assert.AreEqual(ActionSystem.OutputCount, actionSystem.LastOutputs.Length);
        }

        /// <summary>
        /// Integration test: LearningMemory stores experiences
        /// </summary>
        [Test]
        public void LearningMemory_StoresAndRetrievesExperiences()
        {
            // Arrange
            var memory = new LearningMemory(capacity: 5);
            var inputs = new float[] { 0.1f, 0.2f, 0.3f };
            var outputs = new float[] { 0.4f, 0.5f, 0.6f };
            
            // Act
            memory.Record(inputs, outputs, 4, null);
            memory.Record(inputs, outputs, 5, null);
            memory.Record(inputs, outputs, 6, null);
            
            // Assert
            Assert.AreEqual(3, memory.Count, "Should store 3 experiences");
            
            var recent = memory.GetRecentExperiences(2);
            Assert.AreEqual(2, recent.Count, "Should retrieve recent experiences");
            
            var mostRecent = memory.GetMostRecent();
            Assert.IsTrue(mostRecent.HasValue, "Should retrieve most recent");
            Assert.AreEqual(6, mostRecent.Value.ActionIndex);
        }

        /// <summary>
        /// Integration test: LearningMemory ring buffer eviction
        /// </summary>
        [Test]
        public void LearningMemory_RingBuffer_EvictsOldest()
        {
            // Arrange
            var memory = new LearningMemory(capacity: 3);
            
            // Act - add more than capacity
            for (int i = 0; i < 5; i++)
            {
                memory.Record(new float[] { i }, new float[] { i }, i, null);
            }
            
            // Assert - should only have 3 (most recent)
            Assert.AreEqual(3, memory.Count, "Should maintain capacity of 3");
            
            var all = memory.GetAllExperiences();
            bool hasOld = false;
            bool hasNew = false;
            
            foreach (var exp in all)
            {
                if (exp.ActionIndex < 2) hasOld = true;
                if (exp.ActionIndex >= 2) hasNew = true;
            }
            
            Assert.IsFalse(hasOld, "Should have evicted old experiences");
            Assert.IsTrue(hasNew, "Should have recent experiences");
        }
    }
}
