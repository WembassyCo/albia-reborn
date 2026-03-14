using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Albia.Systems.SaveSystem;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for the Save/Load system.
    /// These tests validate that creatures save and load correctly,
    /// world time persists, and genome serialization works properly.
    /// </summary>
    public class SaveSystemTests
    {
        private const string TestWorldName = "TestWorld";
        private const string TestFilename = "TestSaveFile";
        private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
        
        [SetUp]
        public void Setup()
        {
            // Ensure clean test environment
            if (Directory.Exists(SaveDirectory))
            {
                var files = Directory.GetFiles(SaveDirectory, "*" + GetSaveExtension());
                foreach (var file in files)
                {
                    if (file.Contains("Test") || file.Contains("autosave"))
                    {
                        File.Delete(file);
                    }
                }
            }
            
            // Reset SaveManager play time
            if (SaveManager.Instance != null)
            {
                var type = typeof(SaveManager);
                var field = type.GetField("_sessionPlayTime", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                field?.SetValue(SaveManager.Instance, 0f);
            }
        }
        
        private string GetSaveExtension() => ".albiasave";
        
        [TearDown]
        public void Teardown()
        {
            // Clean up after tests
            if (Directory.Exists(SaveDirectory))
            {
                var files = Directory.GetFiles(SaveDirectory, "*" + GetSaveExtension());
                foreach (var file in files)
                {
                    if (file.Contains("Test") || file.Contains("autosave"))
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        
        #region Tests
        
        /// <summary>
        /// TEST: WorldData can be serialized and deserialized correctly.
        /// </summary>
        [UnityTest]
        public IEnumerator WorldData_Serialization_BasicRoundTrip()
        {
            // Arrange
            var original = WorldData.CreateNew(TestWorldName);
            original.WorldTime = 123.45f;
            original.WorldTemperature = 0.75f;
            original.Creatures = new[]
            {
                new CreatureData
                {
                    Id = Guid.NewGuid(),
                    PrefabName = "TestNorn",
                    Position = new Vector3(10f, 5f, 3f),
                    Rotation = Quaternion.Euler(0f, 90f, 0f),
                    Genome = new GenomeData()
                }
            };
            
            // Act
            string json = JsonUtility.ToJson(original);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(original.WorldName, deserialized.WorldName);
            Assert.AreEqual(original.WorldTime, deserialized.WorldTime, 0.001f);
            Assert.AreEqual(original.Creatures.Length, deserialized.Creatures.Length);
            Assert.AreEqual(original.Creatures[0].Position, deserialized.Creatures[0].Position);
            Assert.AreEqual(original.Creatures[0].Rotation.eulerAngles, deserialized.Creatures[0].Rotation.eulerAngles);
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: Genome data serializes correctly with all 192 genes.
        /// </summary>
        [UnityTest]
        public IEnumerator GenomeData_Serialization_PreservesAllGenes()
        {
            // Arrange
            var original = new GenomeData();
            // Set specific test values
            for (int i = 0; i < GenomeData.TotalGenes; i++)
            {
                original.Genes[i] = (float)i / GenomeData.TotalGenes;
            }
            
            var creatureData = new CreatureData
            {
                Id = Guid.NewGuid(),
                Genome = original
            };
            
            var worldData = WorldData.CreateNew(TestWorldName);
            worldData.Creatures = new[] { creatureData };
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Creatures);
            Assert.AreEqual(1, deserialized.Creatures.Length);
            var deserializedGenome = deserialized.Creatures[0].Genome;
            Assert.IsNotNull(deserializedGenome);
            Assert.AreEqual(GenomeData.TotalGenes, deserializedGenome.Genes.Length);
            
            // Verify all genes preserved
            for (int i = 0; i < GenomeData.TotalGenes; i++)
            {
                float expected = (float)i / GenomeData.TotalGenes;
                Assert.AreEqual(expected, deserializedGenome.Genes[i], 0.001f, 
                    $"Gene {i} should preserve its value");
            }
            
            // Verify neural weights specifically (genes 64-191)
            for (int i = GenomeData.NeuralWeightStartIndex; i < GenomeData.TotalGenes; i++)
            {
                float expected = (float)i / GenomeData.TotalGenes;
                float actual = deserializedGenome.GetNeuralWeight(i - GenomeData.NeuralWeightStartIndex);
                Assert.AreEqual(expected, actual, 0.001f, 
                    $"Neural weight at index {i - GenomeData.NeuralWeightStartIndex} should be correct");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: Norn state (health, energy, hunger, age) serializes and deserializes correctly.
        /// </summary>
        [UnityTest]
        public IEnumerator NornState_Serialization_PreservesStateValues()
        {
            // Arrange
            var nornState = new NornState();
            nornState.Initialize();
            nornState.Health = 0.75f;
            nornState.Energy = 0.60f;
            nornState.Hunger = 0.25f;
            nornState.Age = 300f;
            nornState.IsAlive = true;
            
            var creatureData = new CreatureData
            {
                Id = Guid.NewGuid(),
                PrefabName = "TestNorn",
                Position = Vector3.zero,
                NornState = nornState
            };
            
            var worldData = WorldData.CreateNew(TestWorldName);
            worldData.Creatures = new[] { creatureData };
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.Creatures.Length);
            var deserializedState = deserialized.Creatures[0].NornState;
            
            Assert.AreEqual(nornState.Health, deserializedState.Health, 0.001f);
            Assert.AreEqual(nornState.Energy, deserializedState.Energy, 0.001f);
            Assert.AreEqual(nornState.Hunger, deserializedState.Hunger, 0.001f);
            Assert.AreEqual(nornState.Age, deserializedState.Age, 0.001f);
            Assert.AreEqual(nornState.IsAlive, deserializedState.IsAlive);
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: World time persists through save/load operations.
        /// </summary>
        [UnityTest]
        public IEnumerator WorldTime_Persistence_SavesAndLoadsCorrectly()
        {
            // Arrange - simulate elapsed world time
            var worldData = WorldData.CreateNew(TestWorldName);
            worldData.WorldTime = 1234.56f;
            worldData.TotalPlayTime = 3600f; // 1 hour
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(worldData.WorldTime, deserialized.WorldTime, 0.001f, 
                "World time should be preserved");
            Assert.AreEqual(worldData.TotalPlayTime, deserialized.TotalPlayTime, 0.001f, 
                "Total play time should be preserved");
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: SaveManager can save a world and file exists afterward.
        /// </summary>
        [UnityTest]
        public IEnumerator SaveManager_SaveWorld_CreatesFile()
        {
            // This test requires the SaveManager singleton
            // Skip if not in PlayMode scene with SaveManager
            if (SaveManager.Instance == null)
            {
                Debug.Log("[SaveSystemTests] SaveManager not present, skipping integration test");
                Assert.Ignore("SaveManager not available");
                yield break;
            }
            
            // Arrange
            string filename = "IntegrationTestSave";
            string filepath = SaveManager.Instance.GetSaveFilePath(filename);
            
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
            
            bool saveCompleted = false;
            bool saveSuccess = false;
            
            SaveManager.Instance.OnSaveCompleted += (success) =>
            {
                saveCompleted = true;
                saveSuccess = success;
            };
            
            // Act
            SaveManager.Instance.SaveWorld(filename);
            
            // Wait for save to complete
            float timeout = 5f;
            float elapsed = 0f;
            while (!saveCompleted && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            // Assert
            Assert.IsTrue(saveCompleted, "Save should complete within timeout");
            Assert.IsTrue(saveSuccess, "Save should succeed");
            Assert.IsTrue(File.Exists(filepath), "Save file should exist");
            
            // Verify file contains valid JSON
            string content = File.ReadAllText(filepath);
            var loadedData = JsonUtility.FromJson<WorldData>(content);
            Assert.IsNotNull(loadedData, "Save file should contain valid WorldData");
            
            // Cleanup
            SaveManager.Instance.OnSaveCompleted -= (success) => { };
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }
        
        /// <summary>
        /// TEST: Multiple creatures serialize independently.
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleCreatures_SerializeIndependently()
        {
            // Arrange
            var creatures = new[]
            {
                new CreatureData
                {
                    Id = Guid.NewGuid(),
                    PrefabName = "Norn_A",
                    Position = new Vector3(1f, 2f, 3f),
                    Genome = new GenomeData(),
                    NornState = new NornState { Health = 1.0f, Age = 10f }
                },
                new CreatureData
                {
                    Id = Guid.NewGuid(),
                    PrefabName = "Norn_B",
                    Position = new Vector3(4f, 5f, 6f),
                    Genome = new GenomeData(),
                    NornState = new NornState { Health = 0.8f, Age = 50f }
                },
                new CreatureData
                {
                    Id = Guid.NewGuid(),
                    PrefabName = "Norn_C",
                    Position = new Vector3(7f, 8f, 9f),
                    Genome = new GenomeData(),
                    NornState = new NornState { Health = 0.5f, Age = 100f, IsAlive = false }
                }
            };
            
            var worldData = WorldData.CreateNew(TestWorldName);
            worldData.Creatures = creatures;
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(3, deserialized.Creatures.Length);
            
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(creatures[i].Id, deserialized.Creatures[i].Id, 
                    $"Creature {i} ID should match");
                Assert.AreEqual(creatures[i].PrefabName, deserialized.Creatures[i].PrefabName, 
                    $"Creature {i} prefab name should match");
                Assert.AreEqual(creatures[i].Position, deserialized.Creatures[i].Position, 
                    $"Creature {i} position should match");
                Assert.AreEqual(creatures[i].NornState.Health, deserialized.Creatures[i].NornState.Health, 0.001f, 
                    $"Creature {i} health should match");
                Assert.AreEqual(creatures[i].NornState.Age, deserialized.Creatures[i].NornState.Age, 0.001f, 
                    $"Creature {i} age should match");
                Assert.AreEqual(creatures[i].NornState.IsAlive, deserialized.Creatures[i].NornState.IsAlive, 
                    $"Creature {i} isAlive should match");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: Empty world serializes correctly.
        /// </summary>
        [UnityTest]
        public IEnumerator EmptyWorld_Serialization_Works()
        {
            // Arrange
            var worldData = WorldData.CreateNew("EmptyWorld");
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Creatures);
            Assert.IsNotNull(deserialized.Foods);
            Assert.AreEqual(0, deserialized.Creatures.Length);
            Assert.AreEqual(0, deserialized.Foods.Length);
            Assert.AreEqual("EmptyWorld", deserialized.WorldName);
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: World version compatibility check works correctly.
        /// </summary>
        [UnityTest]
        public IEnumerator WorldData_VersionCompatibility_ChecksCorrectly()
        {
            // Current version should be compatible
            var current = WorldData.CreateNew(TestWorldName);
            Assert.IsTrue(current.IsVersionCompatible(), "Current version should be compatible");
            
            // Future minor version should be compatible
            current.Version = "1.5.0";
            Assert.IsTrue(current.IsVersionCompatible(), "Future minor version should be compatible");
            
            // Major version 2 should be incompatible
            current.Version = "2.0.0";
            Assert.IsFalse(current.IsVersionCompatible(), "Major version 2 should be incompatible");
            
            // Null version should be incompatible
            current.Version = null;
            Assert.IsFalse(current.IsVersionCompatible(), "Null version should be incompatible");
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: Camera state serializes correctly.
        /// </summary>
        [UnityTest]
        public IEnumerator CameraState_Serialization_PreservesSettings()
        {
            // Arrange
            var cameraData = new CameraData
            {
                Position = new Vector3(10f, 20f, 30f),
                Rotation = Quaternion.Euler(15f, 30f, 45f),
                FieldOfView = 75f,
                OrthographicSize = 25f,
                IsOrthographic = true
            };
            
            var worldData = WorldData.CreateNew(TestWorldName);
            worldData.CameraState = cameraData;
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.CameraState);
            Assert.AreEqual(cameraData.Position, deserialized.CameraState.Position);
            Assert.AreEqual(cameraData.Rotation.eulerAngles, deserialized.CameraState.Rotation.eulerAngles);
            Assert.AreEqual(cameraData.FieldOfView, deserialized.CameraState.FieldOfView, 0.001f);
            Assert.AreEqual(cameraData.OrthographicSize, deserialized.CameraState.OrthographicSize, 0.001f);
            Assert.AreEqual(cameraData.IsOrthographic, deserialized.CameraState.IsOrthographic);
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: Chemical state serializes correctly.
        /// </summary>
        [UnityTest]
        public IEnumerator ChemicalState_Serialization_PreservesValues()
        {
            // Arrange
            var chemicals = new ChemicalState
            {
                Hunger = 0.8f,
                Fear = 0.3f,
                Energy = 0.6f,
                Sleepiness = 0.2f,
                Pain = 0.1f,
                Reward = 0.5f,
                SexDrive = 0.4f,
                Boredom = 0.7f,
                Curiosity = 0.9f,
                Comfort = 0.5f,
                Aggression = 0.1f,
                Trust = 0.8f
            };
            
            var creatureData = new CreatureData
            {
                Id = Guid.NewGuid(),
                Chemicals = chemicals
            };
            
            var worldData = WorldData.CreateNew(TestWorldName);
            worldData.Creatures = new[] { creatureData };
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.Creatures.Length);
            var deserializedChemicals = deserialized.Creatures[0].Chemicals;
            
            Assert.AreEqual(chemicals.Hunger, deserializedChemicals.Hunger, 0.001f);
            Assert.AreEqual(chemicals.Fear, deserializedChemicals.Fear, 0.001f);
            Assert.AreEqual(chemicals.Energy, deserializedChemicals.Energy, 0.001f);
            Assert.AreEqual(chemicals.Sleepiness, deserializedChemicals.Sleepiness, 0.001f);
            Assert.AreEqual(chemicals.Pain, deserializedChemicals.Pain, 0.001f);
            Assert.AreEqual(chemicals.Reward, deserializedChemicals.Reward, 0.001f);
            Assert.AreEqual(chemicals.SexDrive, deserializedChemicals.SexDrive, 0.001f);
            Assert.AreEqual(chemicals.Boredom, deserializedChemicals.Boredom, 0.001f);
            Assert.AreEqual(chemicals.Curiosity, deserializedChemicals.Curiosity, 0.001f);
            Assert.AreEqual(chemicals.Comfort, deserializedChemicals.Comfort, 0.001f);
            Assert.AreEqual(chemicals.Aggression, deserializedChemicals.Aggression, 0.001f);
            Assert.AreEqual(chemicals.Trust, deserializedChemicals.Trust, 0.001f);
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: Food/plant data serializes correctly.
        /// </summary>
        [UnityTest]
        public IEnumerator FoodData_Serialization_PreservesValues()
        {
            // Arrange
            var foods = new[]
            {
                new FoodData
                {
                    Position = new Vector3(5f, 0f, 10f),
                    RemainingAmount = 0.75f,
                    PlantType = "Carrot"
                },
                new FoodData
                {
                    Position = new Vector3(-5f, 0f, -10f),
                    RemainingAmount = 0.5f,
                    PlantType = "Lemon"
                }
            };
            
            var worldData = WorldData.CreateNew(TestWorldName);
            worldData.Foods = foods;
            
            // Act
            string json = JsonUtility.ToJson(worldData);
            var deserialized = JsonUtility.FromJson<WorldData>(json);
            
            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(2, deserialized.Foods.Length);
            Assert.AreEqual(foods[0].Position, deserialized.Foods[0].Position);
            Assert.AreEqual(foods[0].RemainingAmount, deserialized.Foods[0].RemainingAmount, 0.001f);
            Assert.AreEqual(foods[0].PlantType, deserialized.Foods[0].PlantType);
            Assert.AreEqual(foods[1].Position, deserialized.Foods[1].Position);
            Assert.AreEqual(foods[1].RemainingAmount, deserialized.Foods[1].RemainingAmount, 0.001f);
            Assert.AreEqual(foods[1].PlantType, deserialized.Foods[1].PlantType);
            
            yield return null;
        }
        
        /// <summary>
        /// TEST: SaveManager correctly lists available save files.
        /// </summary>
        [UnityTest]
        public IEnumerator SaveManager_GetSaveFiles_ListsCorrectFiles()
        {
            // Skip if SaveManager not available
            if (SaveManager.Instance == null)
            {
                Assert.Ignore("SaveManager not available");
                yield break;
            }
            
            // Arrange - create test save files
            var testData = WorldData.CreateNew("TestSave_A");
            testData.SaveTimestamp = DateTime.Now.AddHours(-1);
            testData.TotalPlayTime = 3600f;
            
            string jsonA = JsonUtility.ToJson(testData);
            File.WriteAllText(Path.Combine(SaveDirectory, "TestSave_A" + GetSaveExtension()), jsonA);
            
            testData = WorldData.CreateNew("TestSave_B");
            testData.SaveTimestamp = DateTime.Now;
            testData.TotalPlayTime = 7200f;
            
            string jsonB = JsonUtility.ToJson(testData);
            File.WriteAllText(Path.Combine(SaveDirectory, "TestSave_B" + GetSaveExtension()), jsonB);
            
            // Wait for file system
            yield return new WaitForSeconds(0.1f);
            
            // Act
            var saves = SaveManager.Instance.GetSaveFiles();
            
            // Assert
            var foundTestA = saves.Exists(s => s.Name == "TestSave_A");
            var foundTestB = saves.Exists(s => s.Name == "TestSave_B");
            
            Assert.IsTrue(foundTestA, "Should find TestSave_A");
            Assert.IsTrue(foundTestB, "Should find TestSave_B");
            
            // Verify order (newest first)
            var testBIndex = saves.FindIndex(s => s.Name == "TestSave_B");
            var testAIndex = saves.FindIndex(s => s.Name == "TestSave_A");
            Assert.Less(testBIndex, testAIndex, "Newer save should come first");
            
            // Cleanup
            SaveManager.Instance.DeleteSave("TestSave_A");
            SaveManager.Instance.DeleteSave("TestSave_B");
        }
        
        /// <summary>
        /// TEST: SaveManager correctly deletes save files.
        /// </summary>
        [UnityTest]
        public IEnumerator SaveManager_DeleteSave_RemovesFile()
        {
            // Skip if SaveManager not available
            if (SaveManager.Instance == null)
            {
                Assert.Ignore("SaveManager not available");
                yield break;
            }
            
            // Arrange
            string filename = "DeleteTest";
            string filepath = SaveManager.Instance.GetSaveFilePath(filename);
            
            // Create test file
            var testData = WorldData.CreateNew(filename);
            File.WriteAllText(filepath, JsonUtility.ToJson(testData));
            
            Assert.IsTrue(File.Exists(filepath), "Test file should exist before deletion");
            
            // Act
            bool result = SaveManager.Instance.DeleteSave(filename);
            
            // Assert
            Assert.IsTrue(result, "Delete should succeed");
            Assert.IsFalse(File.Exists(filepath), "File should not exist after deletion");
        }
        
        #endregion
    }
}
