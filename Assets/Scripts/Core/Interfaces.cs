using System;
using UnityEngine;

namespace AlbiaReborn.Core.Interfaces
{
    /// <summary>
    /// Interface for heightmap data providers.
    /// Contract between world-gen and voxel-engine pods.
    /// </summary>
    public interface IHeightmapData
    {
        float[,] GetHeightmap();
        float GetHeightAt(int x, int z);
        float GetHeightAt(Vector2Int position);
        int Width { get; }
        int Height { get; }
        int Seed { get; }
    }

    /// <summary>
    /// Interface for climate queries.
    /// Contract between climate-weather and creature biochemistry pods.
    /// </summary>
    public interface IClimateQuery
    {
        float GetTemperatureAt(Vector3Int worldPosition);
        float GetMoistureAt(Vector3Int worldPosition);
        BiomeType GetBiomeAt(Vector3Int worldPosition);
    }

    /// <summary>
    /// Interface for voxel modification.
    /// Contract between creature actions / player tools and voxel-engine.
    /// </summary>
    public interface IVoxelModification
    {
        bool SetVoxel(Vector3Int position, VoxelType type);
        VoxelType GetVoxel(Vector3Int position);
        event Action<Vector3Int, VoxelType, VoxelType> OnVoxelChanged;
    }

    /// <summary>
    /// Interface for saveable entities.
    /// Contract for persistence system.
    /// </summary>
    public interface ISaveable
    {
        string SaveKey { get; }
        string Serialize();
        void Deserialize(string data);
    }

    /// <summary>
    /// Core voxel types for MVP.
    /// </summary>
    public enum VoxelType
    {
        Air = 0,
        Dirt = 1,
        Stone = 2,
        Sand = 3,
        Grass = 4,
        Water = 5
    }

    /// <summary>
    /// Biome types for MVP (3 only).
    /// </summary>
    public enum BiomeType
    {
        TemperateForest,
        Grassland,
        Desert
    }
}
