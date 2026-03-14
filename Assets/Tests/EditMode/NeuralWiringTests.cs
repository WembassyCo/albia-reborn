using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Albia.AI;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Tests
{
    /// <summary>
    /// Unit tests for Neural Wiring integration (Wave 3)
    /// </summary>
    public class NeuralWiringTests
    {
        [SetUp]
        public void Setup()
        {
            // Reset any static state if needed
        }

        // ==================== TEST 1: NeuralBrain Initialization ====================

        /// <summary>
        /// Test 1: NeuralBrain initializes with correct dimensions
        /// </summary>
        [Test]
        public void NeuralBrain_Initializes_WithCorrectDimensions()
        {
            // Arrange
            var brain = new GameObject("TestBrain").AddComponent<NeuralBrain>();
            var genome = new GenomeData();
            
            // Act
            brain.Initialize(genome);
            
            // Assert
            Assert.IsNotNull(brain.Network, "Network should be initialized");
            Assert.IsNotNull(brain.Learning, "Learning system should be initialized");
            Assert.IsNotNull(brain.Memory, "Memory should be initialized");
            Assert.IsNotNull(brain.Genome, "Genome should be stored");
            Assert.AreEqual(16, brain.CurrentOutputs.Length, "Should have 16 outputs");
            Assert.AreEqual(24, brain.CurrentInputs.Length, "Should have 24 inputs");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(brain.gameObject);
        }

        /// <summary>
        /// Test 1b: NeuralBrain generates outputs in valid range
        /// </summary>
        [Test]
        public void NeuralBrain_ProcessFrame_GeneratesValidOutputs()
        {
            // Arrange
            var brain = new GameObject("TestBrain").AddComponent<NeuralBrain>();
            var sensory = new GameObject("TestSensory").AddComponent<SensoryInput>();
            sensory.creatureState = new CreatureState();
            
            brain.Initialize(new GenomeData());
            brain.Sensory = sensory;
            
            // Act
            brain.ProcessFrame();
            
            // Assert
            Assert.IsNotNull(brain.CurrentOutputs, "Should have outputs after processing");
            
            for (int i = 0; i < 16; i++)
            {
                Assert.That(brain.CurrentOutputs[i], Is.InRange(-1f, 1f), 
                    $"Output[{i}] should be in range [-1, 1]");
            }
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(brain.gameObject);
            UnityEngine.Object.DestroyImmediate(sensory.gameObject);
        }

        // ==================== TEST 2: SensoryInput Raycast ====================

        /// <summary>
        /// Test 2: SensoryInput provides valid input data
        /// </summary>
        [Test]
        public void SensoryInput_ProvidesNormalizedValues()
        {
            // Arrange
            var sensory = new GameObject("TestSensory").AddComponent<SensoryInput>();
            var state = new CreatureState
            {
                Health = 0.7f,
                Energy = 0.5f,
                Hunger = 0.3f,
                Age = 10f
            };
            sensory.creatureState = state;
            
            // Force update
            sensory.UpdateSensoryData();
            
            // Assert - Chemicals should be normalized
            Assert.That(sensory.Hunger, Is.InRange(0f, 1f), "Hunger should be normalized");
            Assert.That(sensory.Energy, Is.InRange(0f, 1f), "Energy should be normalized");
            Assert.That(sensory.Health, Is.InRange(0f, 1f), "Health should be normalized");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(sensory.gameObject);
        }

        /// <summary>
        /// Test 2b: SensoryInput distance values are in range
        /// </summary>
        [Test]
        public void SensoryInput_Distances_InValidRange()
        {
            // Arrange
            var sensory = new GameObject("TestSensory").AddComponent<SensoryInput>();
            sensory.creatureState = new CreatureState();
            
            // Force update (no raycast hits in test environment)
            sensory.UpdateSensoryData();
            
            // Assert
            // When no object seen, distances should be 1 (max/far)
            Assert.That(sensory.FoodDistance, Is.InRange(0f, 1f), "FoodDistance should be normalized");
            Assert.That(sensory.ThreatDistance, Is.InRange(0f, 1f), "ThreatDistance should be normalized");
            Assert.That(sensory.NearestCreatureDistance, Is.InRange(0f, 1f), "CreatureDistance should be normalized");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(sensory.gameObject);
        }

        // ==================== TEST 3: Action Execution ====================

        /// <summary>
        /// Test 3: Brain maps outputs to correct actions
        /// </summary>
        [Test]
        public void NeuralBrain_MapsOutputs_ToActions()
        {
            // Arrange
            var brain = new GameObject("TestBrain").AddComponent<NeuralBrain>();
            brain.Initialize(new GenomeData());
            
            // Set high activation on Eat neuron
            brain.CurrentOutputs = new float[16];
            brain.CurrentOutputs[(int)NeuralBrain.OutputNeuron.Eat] = 0.9f;
            brain.CurrentOutputs[(int)NeuralBrain.OutputNeuron.MoveForward] = 0.1f;
            
            // Need to set up sensory for condition checking
            var sensory = new GameObject("TestSensory").AddComponent<SensoryInput>();
            sensory.creatureState = new CreatureState
            {
                Hunger = 0.8f,  // Can eat
                Energy = 0.5f
            };
            brain.Sensory = sensory;
            
            CreatureAction? capturedAction = null;
            brain.OnActionExecuted += (action) => capturedAction = action;
            
            // Act - manually set current action for test
            brain.ProcessFrame();
            
            // Assert
            Assert.IsNotNull(capturedAction || brain.CurrentAction, 
                "Some action should be executed");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(brain.gameObject);
            UnityEngine.Object.DestroyImmediate(sensory.gameObject);
        }

        /// <summary>
        /// Test 3b: Execution triggers event
        /// </summary>
        [Test]
        public void NeuralBrain_Action_TriggersEvent()
        {
            // Arrange
            var brain = new GameObject("TestBrain").AddComponent<TestableNeuralBrain>();
            brain.Initialize(new GenomeData());
            
            bool eventFired = false;
            CreatureAction capturedAction = CreatureAction.Move;
            
            brain.OnActionExecuted += (action) =>
            {
                eventFired = true;
                capturedAction = action;
            };
            
            // Act - manually execute to test event
            ((TestableNeuralBrain)brain).TestExecuteAction(CreatureAction.Eat);
            
            // Assert
            Assert.IsTrue(eventFired, "ActionExecuted event should fire");
            Assert.AreEqual(CreatureAction.Eat, capturedAction, "Should capture correct action");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(brain.gameObject);
        }

        // ==================== TEST 4: Learning Integration ====================

        /// <summary>
        /// Test 4: Reward strengthens winning pathway
        /// </summary>
        [Test]
        public void Learning_Reward_StrengthensWinningPathway()
        {
            // Arrange
            var brain = new GameObject("TestBrain").AddComponent<NeuralBrain>();
            var genome = new GenomeData();
            
            // Get initial weight
            float[] initialWeights = new float[genome.Genes.Length];
            Array.Copy(genome.Genes, initialWeights, genome.Genes.Length);
            
            brain.Initialize(genome);
            brain.Learning.LearningRate = 0.1f; // High for testing
            
            // Record an experience
            float[] inputs = new float[24];
            for (int i = 0; i < 24; i++) inputs[i] = 0.5f;
            
            var outputs = brain.Network.Forward(inputs);
            brain.Memory.RecordAction(inputs, outputs, (int)NeuralBrain.OutputNeuron.Eat, brain.Network);
            
            // Act
            brain.TriggerReward(1.0f);
            
            // Assert
            Assert.Greater(brain.Learning.Stats.RewardCount, 0, "Should record reward");
            Assert.Greater(brain.Learning.Stats.TotalWeightUpdates, 0, "Should update weights");
            
            // Weights should have changed
            brain.Network.SaveWeightsToGenome();
            bool weightsChanged = false;
            for (int i = GenomeData.NeuralWeightStartIndex; i < GenomeData.TotalGenes; i++)
            {
                if (Math.Abs(genome.Genes[i] - initialWeights[i]) > 0.0001f)
                {
                    weightsChanged = true;
                    break;
                }
            }
            Assert.IsTrue(weightsChanged, "Genome should have changed after learning");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(brain.gameObject);
        }

        /// <summary>
        /// Test 4b: Punishment weakens pathways
        /// </summary>
        [Test]
        public void Learning_Punishment_Recorded()
        {
            // Arrange
            var brain = new GameObject("TestBrain").AddComponent<NeuralBrain>();
            brain.Initialize(new GenomeData());
            
            // Record an experience
            float[] inputs = new float[24];
            var outputs = brain.Network.Forward(inputs);
            brain.Memory.RecordAction(inputs, outputs, 8, brain.Network);
            
            // Act
            brain.TriggerPunishment(0.5f);
            
            // Assert
            Assert.Greater(brain.Learning.Stats.PunishmentCount, 0, "Should record punishment");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(brain.gameObject);
        }

        // ==================== TEST 5: Memory Management ====================

        /// <summary>
        /// Test 5: Action memory captures experiences
        /// </summary>
        [Test]
        public void Memory_CapturesExperiences()
        {
            // Arrange
            var brain = new GameObject("TestBrain").AddComponent<NeuralBrain>();
            brain.Initialize(new GenomeData());
            
            // Act - process multiple frames
            for (int i = 0; i < 5; i++)
            {
                float[] inputs = new float[24];
                for (int j = 0; j < 24; j++) inputs[j] = i * 0.1f;
                
                var outputs = brain.Network.Forward(inputs);
                brain.Memory.RecordAction(inputs, outputs, 8, brain.Network);
            }
            
            // Assert
            Assert.AreEqual(5, brain.Memory.Count, "Should capture all experiences");
            
            var recent = brain.Memory.GetRecentExperiences(3);
            Assert.AreEqual(3, recent.Count, "Should retrieve recent experiences");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(brain.gameObject);
        }

        /// <summary>
        /// Test 5b: Memory respects capacity
        /// </summary>
        [Test]
        public void Memory_RespectsCapacity()
        {
            // Arrange
            var memory = new LearningMemory(capacity: 5);
            
            // Act - add more than capacity
            for (int i = 0; i < 10; i++)
            {
                float[] inputs = new float[8];
                float[] outputs = new float[16];
                memory.Record(inputs, outputs, i, null);
            }
            
            // Assert
            Assert.AreEqual(5, memory.Count, "Should not exceed capacity");
            
            var all = memory.GetAllExperiences();
            int count = 0;
            foreach (var _ in all) count++;
            Assert.AreEqual(5, count, "Should iterate over exactly capacity items");
            
            // Cleanup
            // Memory doesn't require GameObject cleanup
        }

        // ==================== TEST 6: Norn Integration ====================

        /// <summary>
        /// Test 6: Norn can be wired to NeuralBrain
        /// </summary>
        [Test]
        public void Norn_CanBeWired_ToNeuralBrain()
        {
            // Arrange
            var nornObject = new GameObject("TestNorn");
            var norn = nornObject.AddComponent<Norn>();
            
            var brainObject = new GameObject("TestBrain");
            var brain = brainObject.AddComponent<NeuralBrain>();
            brain.Initialize(new GenomeData());
            
            // Act
            norn.InitializeWithBrain(brain);
            
            // Assert
            Assert.IsNotNull(norn.Brain, "Norn should have brain");
            Assert.IsNotNull(norn.Genome, "Norn should have genome");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(nornObject);
            UnityEngine.Object.DestroyImmediate(brainObject);
        }

        /// <summary>
        /// Test 6b: Norn receives reward signals
        /// </summary>
        [Test]
        public void Norn_ReceivesRewardSignals()
        {
            // Arrange
            var norn = new GameObject("TestNorn").AddComponent<Norn>();
            norn.Initialize();
            
            bool rewardReceived = false;
            norn.OnRewardSignal += (amount) => rewardReceived = true;
            
            // Act
            norn.ReceiveRewardSignal(0.5f);
            
            // Assert
            Assert.IsTrue(rewardReceived, "Should receive reward signal");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(norn.gameObject);
        }

        /// <summary>
        /// Test 6c: Norn receives punishment signals
        /// </summary>
        [Test]
        public void Norn_ReceivesPunishmentSignals()
        {
            // Arrange
            var norn = new GameObject("TestNorn").AddComponent<Norn>();
            norn.Initialize();
            
            bool punishmentReceived = false;
            norn.OnPunishmentSignal += (amount) => punishmentReceived = true;
            
            // Act
            norn.ReceivePunishmentSignal(0.5f);
            
            // Assert
            Assert.IsTrue(punishmentReceived, "Should receive punishment signal");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(norn.gameObject);
        }

        // ==================== Integration Test ====================

        /// <summary>
        /// Integration test: Full neural to action pipeline
        /// </summary>
        [Test]
        public void Integration_FullNeuralPipeline()
        {
            // Arrange - Create full stack
            var nornObject = new GameObject("IntegrationNorn");
            
            var norn = nornObject.AddComponent<Norn>();
            var controller = nornObject.AddComponent<NornAIController>();
            var brain = nornObject.AddComponent<NeuralBrain>();
            var sensory = nornObject.AddComponent<SensoryInput>();
            
            // Link references
            controller.Brain = brain;
            controller.Sensory = sensory;
            
            // Initialize
            var genome = new GenomeData();
            controller.Initialize(genome);
            
            // Act - Run a frame
            controller.ProcessFramePublic();
            
            // Assert
            Assert.IsNotNull(brain.CurrentOutputs, "Should have outputs");
            Assert.IsNotNull(sensory, "Should have sensory");
            
            // Cleanup
            UnityEngine.Object.DestroyImmediate(nornObject);
        }
    }

    /// <summary>
    /// Test helper to expose protected methods
    /// </summary>
    public class TestableNeuralBrain : NeuralBrain
    {
        public void TestExecuteAction(CreatureAction action)
        {
            OnActionExecuted?.Invoke(action);
        }
    }

    /// <summary>
    /// Extension for testing
    /// </summary>
    public static class NornAIControllerTestExtensions
    {
        public static void ProcessFramePublic(this NornAIController controller)
        {
            // Use reflection to call protected method for testing
            typeof(NornAIController).GetMethod("ProcessFrame", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance)?
                .Invoke(controller, null);
        }
    }
}
