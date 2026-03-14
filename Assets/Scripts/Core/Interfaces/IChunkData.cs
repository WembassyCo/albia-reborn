using UnityEngine;

namespace Albia.Core.Interfaces
{
    /// <summary>
    /// Interface for chunk data access and manipulation.
    /// Chunks are 16x16x16 voxel grids.
    /// </summary>
    public interface IChunkData
    {
        /// <summary>
        /// World position of this chunk (in chunk coordinates)
        /// </summary>
        Vector3Int ChunkPosition { get; }

        /// <summary>
        /// Gets the voxel type at local coordinates (0-15)
        /// </summary>
        /// <param name="x">Local X (0-15)</param>
        /// <param name="y">Local Y (0-15)</param>
        /// <param name="z">Local Z (0-15)</param>
        /// <returns>The voxel type at the specified position</returns>
        Shared.VoxelType GetVoxel(int x, int y, int z);

        /// <summary>
        /// Gets the voxel type at local coordinates
        /// </summary>
        Shared.VoxelType GetVoxel(Vector3Int localPos);

        /// <summary>
        /// Sets the voxel type at local coordinates (0-15)
        /// </summary>
        /// <param name="x">Local X (0-15)</param>
        /// <param name="y">Local Y (0-15)</param>
        /// <param name="z">Local Z (0-15)</param>
        /// <param name="type">The voxel type to set</param>
        void SetVoxel(int x, int y, int z, Shared.VoxelType type);

        /// <summary>
        /// Sets the voxel type at local coordinates
        /// </summary>
        void SetVoxel(Vector3Int localPos, Shared.VoxelType type);

        /// <summary>
        /// Returns true if this chunk has been modified from its generated state
        /// </summary>
        bool IsDirty { get; set; }

        /// <summary>
        /// Returns true if this chunk has been generated/populated
        /// </summary>
        bool IsGenerated { get; }

        /// <summary>
        /// Timestamp of last modification (for culling decisions)
        /// </summary>
        float LastModifiedTime { get; }

        /// <summary>
        /// Converts local position to world position
        /// </summary>
        Vector3Int LocalToWorld(Vector3Int localPos);

        /// <summary>
        /// Converts world position to local position (-1 if outside chunk)
        /// </summary>
        Vector3Int? WorldToLocal(Vector3Int worldPos);

        /// <summary>
        /// Gets the density value at a local position for mesh generation
        /// </summary>
        float GetDensity(int x, int y, int z);

        /// <summary>
        /// Gets neighbor chunk data if available
        /// </summary>
        /// <param name="direction">Neighbor direction (dx, dy, dz)</param>
        /// <returns>Neighbor chunk or null if not loaded</returns>
        IChunkData GetNeighbor(Vector3Int direction);

        /// <summary>
        /// Marks the chunk for mesh regeneration
        /// </summary>
        void MarkDirty();

        /// <summary>
        /// Called when chunk data has been modified
        /// </summary>
        event System.Action<IChunkData, Vector3Int> OnVoxelChanged;
    }
}
