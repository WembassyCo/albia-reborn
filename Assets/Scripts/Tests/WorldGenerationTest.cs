using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AlbiaReborn.World.Generation;
using AlbiaReborn.World.Voxel;
using AlbiaReborn.World.Climate;

namespace AlbiaReborn.Tests
{
    /// <summary>
    /// Integration tests for MVP world generation.
    /// Run via Unity Test Runner.
    /// </summary>    
    public class WorldGenerationTests
    {
        [Test]
        public void HeightmapGenerator_CreatesConsistentData()
        {
            // Arrange
            int seed = 12345;
            int width = 64;
            int height = 64;

            // Act
            var generator = new HeightmapGenerator(seed, width, height);

            // Assert
            Assert.NotNull(generator.GetHeightmap());
            Assert.AreEqual(width, generator.Width);
            Assert.AreEqual(height, generator.Height);
            Assert.AreEqual(seed, generator.Seed);
        }

        [Test]
        public void HeightmapGenerator_IsDeterministic()
        {
            // Same seed = same output
            var gen1 = new HeightmapGenerator(999, 32, 32);
            var gen2 = new HeightmapGenerator(999, 32, 32);

            for (int x = 0; x < 32; x++)
            {
                for (int z = 0; z < 32; z++)
                {
                    Assert.AreEqual(gen1.GetHeightAt(x, z), gen2.GetHeightAt(x, z), 0.0001f);
                }
            }
        }

        [Test]
        public void HeightmapGenerator_DifferentSeeds_DifferentOutput()
        {
            // Different seeds = different output
            var gen1 = new HeightmapGenerator(111, 32, 32);
            var gen2 = new HeightmapGenerator(222, 32, 32);

            bool different = false;
            for (int x = 0; x < 32; x++)
            {
                for (int z = 0; z < 32; z++)
                {
                    if (Mathf.Abs(gen1.GetHeightAt(x, z) - gen2.GetHeightAt(x, z)) > 0.0001f)
                    {
                        different = true;
                        break;
                    }
                }
                if (different) break;
            }

            Assert.IsTrue(different, "Different seeds should produce different heightmaps");
        }

        [Test]
        public void ChunkManager_CreatesChunksFromHeightmap()
        {
            // Arrange
            var heightmap = new HeightmapGenerator(1234, 64, 64);

            // Act
            var chunkManager = new ChunkManager(heightmap);

            // Assert
            Assert.Greater(chunkManager.GetAllChunks().Count, 0);
            
            // Should have ceil(64/16) * ceil(64/16) = 4 * 4 = 16 chunks
            Assert.AreEqual(16, chunkManager.GetAllChunks().Count);
        }

        [Test]
        public void ClimateSystem_AssignsBiomes()
        {
            // Arrange
            var heightmap = new HeightmapGenerator(1234, 64, 64);
            var climate = new ClimateSystem(heightmap);

            // Act - sample various positions
            BiomeType biome1 = climate.GetBiomeAt(new Vector3Int(0, 5, 0));      // Low elevation
            BiomeType biome2 = climate.GetBiomeAt(new Vector3Int(0, 50, 0));     // High elevation
            BiomeType biome3 = climate.GetBiomeAt(new Vector3Int(0, 5, 60));     // Different latitude

            // Assert - should return valid biomes
            Assert.That(biome1, Is.AnyOf(BiomeType.TemperateForest, BiomeType.Grassland, BiomeType.Desert));
            Assert.That(biome2, Is.AnyOf(BiomeType.TemperateForest, BiomeType.Grassland, BiomeType.Desert));
            Assert.That(biome3, Is.AnyOf(BiomeType.TemperateForest, BiomeType.Grassland, BiomeType.Desert));
        }

        [Test]
        public void VoxelModification_ChangesPersist()
        {
            // Arrange
            var heightmap = new HeightmapGenerator(1234, 32, 32);
            var chunks = new ChunkManager(heightmap);

            // Act - dig a hole
            chunks.SetVoxel(new Vector3Int(8, 2, 8), VoxelType.Air);

            // Assert
            Assert.AreEqual(VoxelType.Air, chunks.GetVoxel(new Vector3Int(8, 2, 8)));
        }

        [UnityTest]
        public IEnumerator WorldGeneration_CompletePipeline_Runs()
        {
            // Full integration test
            var heightmap = new HeightmapGenerator(5678, 64, 64);
            var chunks = new ChunkManager(heightmap);
            var climate = new ClimateSystem(heightmap);

            // Verify all systems work together
            Assert.Greater(heightmap.GetHeightmap().Length, 0);
            Assert.Greater(chunks.GetAllChunks().Count, 0);

            yield return null; // Frame for Unity
        }
    }
}
