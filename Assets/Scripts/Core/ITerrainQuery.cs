using System;
using Albia.Core.Shared;
using UnityEngine;

namespace Albia.Core.Interfaces
{
    /// <summary>
    /// Interface for querying and modifying terrain voxels.
    /// Implemented by VoxelWorld to provide unified access to the voxel system.
    /// </summary>
    public interface ITerrainQuery
    {
        /// <summary>
        /// Gets the voxel type at a specific world position
        /// </summary>
        /// <param name="pos">World position in voxels</param>
        /// <returns>The voxel type at the specified position</returns>
        VoxelType GetVoxel(Vector3Int pos);
        
        /// <summary>
        /// Sets the voxel type at a specific world position
        /// </summary>
        /// <param name="pos">World position in voxels</param>
        /// <param name="type">The voxel type to set</param>
        /// <param name="source">Source of the change (for logging/replication)</param>
        void SetVoxel(Vector3Int pos, VoxelType type, ChangeSource source);
        
        /// <summary>
        /// Event raised when a voxel changes
        /// </summary>
        event Action<Vector3Int, VoxelType> OnVoxelChanged;
        
        /// <summary>
        /// Gets the terrain height at a specific 2D tile position
        /// </summary>
        /// <param name="tilePos">X,Z position in world coordinates</param>
        /// <returns>Height in voxel units</returns>
        float GetHeight(Vector2Int tilePos);
        
        /// <summary>
        /// Gets the biome at a specific 2D tile position
        /// </summary>
        /// <param name="tilePos">X,Z position in world coordinates</param>
        /// <returns>The biome type at the specified position</returns>
        Biome GetBiome(Vector2Int tilePos);
    }
    
    /// <summary>
    /// Source of voxel modification
    /// </summary>
    public enum ChangeSource
    {
        Player,
        Script,
        Simulation,
        Generation,
        Network
    }
    
    /// <summary>
    /// Biome enumeration for world generation
    /// </summary>
    public enum Biome
    {
        Ocean,
        Beach,
        Desert,
        Plains,
        Forest,
        Swamp,
        Mountain,
        Snow
    }
}