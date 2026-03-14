using System;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Systems.SaveSystem
{
    /// <summary>
    /// Serializable data container for creature save state.
    /// Stores all essential creature information for persistence.
    /// </summary>
    [Serializable]
    public class CreatureData
    {
        public Guid Id;
        public string PrefabName;
        public Vector3 Position;
        public Quaternion Rotation;
        public NornState NornState;
        public GenomeData Genome;
        public ChemicalState Chemicals;
        public float CurrentLearningRate;
        public int LearningMemoryCount;
    }

    /// <summary>
    /// Serializable data container for food/plant positions.
    /// </summary>
    [Serializable]
    public class FoodData
    {
        public Vector3 Position;
        public float RemainingAmount;
        public string PlantType;
    }

    /// <summary>
    /// Serializable camera state for persistence.
    /// </summary>
    [Serializable]
    public class CameraData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float FieldOfView;
        public float OrthographicSize;
        public bool IsOrthographic;
    }

    /// <summary>
    /// Serializable data container for world seed and terrain state.
    /// </summary>
    [Serializable]
    public class WorldSeedData
    {
        public int Seed;
        public Vector2Int[] ModifiedChunks;
        public byte[] VoxelModifications;
    }

    /// <summary>
    /// Main serializable container for all world data.
    /// This class is designed to work with Unity's JsonUtility for serialization.
    /// </summary>
    [Serializable]
    public class WorldData
    {
        // File format metadata
        public string Version = "1.0.0";
        public DateTime SaveTimestamp;
        public string WorldName;
        
        // World state
        public float WorldTime;
        public float TotalPlayTime;
        
        // Seed and terrain
        public WorldSeedData SeedData;
        
        // Creatures
        public CreatureData[] Creatures;
        
        // Food/Plants
        public FoodData[] Foods;
        
        // Camera state
        public CameraData CameraState;
        
        // Environmental state
        public float WorldTemperature;
        public float WorldMoisture;
        public float LightLevel;
        
        // Game state flags
        public bool IsPaused;
        public float GameSpeed = 1f;
        
        /// <summary>
        /// Creates a new empty WorldData instance with current timestamp.
        /// </summary>
        public static WorldData CreateNew(string worldName)
        {
            return new WorldData
            {
                Version = "1.0.0",
                SaveTimestamp = DateTime.Now,
                WorldName = worldName,
                WorldTime = 0f,
                TotalPlayTime = 0f,
                Creatures = Array.Empty<CreatureData>(),
                Foods = Array.Empty<FoodData>(),
                CameraState = new CameraData
                {
                    Position = Vector3.zero,
                    Rotation = Quaternion.identity,
                    FieldOfView = 60f,
                    OrthographicSize = 10f,
                    IsOrthographic = false
                },
                WorldTemperature = 0.5f,
                WorldMoisture = 0.5f,
                LightLevel = 1f,
                IsPaused = false,
                GameSpeed = 1f
            };
        }
        
        /// <summary>
        /// Validates that the loaded data is compatible with current version.
        /// </summary>
        public bool IsVersionCompatible()
        {
            // For now, accept any 1.x.x version
            return !string.IsNullOrEmpty(Version) && Version.StartsWith("1.");
        }
        
        /// <summary>
        /// Gets the save file metadata for display purposes.
        /// </summary>
        public string GetDisplaySummary()
        {
            int creatureCount = Creatures?.Length ?? 0;
            int foodCount = Foods?.Length ?? 0;
            TimeSpan playTime = TimeSpan.FromSeconds(TotalPlayTime);
            
            return $"{WorldName} - {creatureCount} creatures, {foodCount} plants\n" +
                   $"Play time: {playTime.Hours}h {playTime.Minutes}m {playTime.Seconds}s\n" +
                   $"Saved: {SaveTimestamp:yyyy-MM-dd HH:mm}";
        }
    }
}
