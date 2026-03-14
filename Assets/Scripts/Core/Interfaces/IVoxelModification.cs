using UnityEngine;

namespace Albia.Core.Interfaces
{
    /// <summary>
    /// Interface for voxel modification operations.
    /// Implement this for atomic voxel modifications that can be queued/undone.
    /// </summary>
    public interface IVoxelModification
    {
        /// <summary>
        /// World position where modification occurs
        /// </summary>
        Vector3Int WorldPosition { get; }

        /// <summary>
        /// Target voxel type after modification
        /// </summary>
        Shared.VoxelType TargetType { get; }

        /// <summary>
        /// Previous voxel type (set after Apply)
        /// </summary>
        Shared.VoxelType PreviousType { get; set; }

        /// <summary>
        /// Timestamp of when this modification was created
        /// </summary>
        float Timestamp { get; }

        /// <summary>
        /// Priority for modification queue (higher = processed first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Apply the modification to the chunk
        /// </summary>
        /// <param name="chunkManager">The chunk manager to apply through</param>
        /// <returns>True if modification was successful</returns>
        bool Apply(IChunkManager chunkManager);

        /// <summary>
        /// Revert the modification (for undo support)
        /// </summary>
        /// <param name="chunkManager">The chunk manager to revert through</param>
        /// <returns>True if revert was successful</returns>
        bool Revert(IChunkManager chunkManager);

        /// <summary>
        /// Validate if this modification can be applied
        /// </summary>
        /// <param name="chunkManager">The chunk manager to check against</param>
        /// <returns>True if modification is valid</returns>
        bool CanApply(IChunkManager chunkManager);
    }

    /// <summary>
    /// Interface for the chunk manager that handles voxel modifications
    /// </summary>
    public interface IChunkManager
    {
        /// <summary>
        /// Get or load a chunk at the specified world position
        /// </summary>
        IChunkData GetChunk(Vector3Int worldPosition);

        /// <summary>
        /// Get or load a chunk by chunk coordinates
        /// </summary>
        IChunkData GetChunkByCoord(Vector3Int chunkCoord);

        /// <summary>
        /// Sets a voxel at the specified world position
        /// </summary>
        /// <param name="worldPos">World position</param>
        /// <param name="type">Voxel type to set</param>
        /// <returns>True if voxel was set successfully</returns>
        bool SetVoxel(Vector3Int worldPos, Shared.VoxelType type);

        /// <summary>
        /// Gets the voxel at the specified world position
        /// </summary>
        Shared.VoxelType GetVoxel(Vector3Int worldPos);

        /// <summary>
        /// Queues a modification for processing
        /// </summary>
        void QueueModification(IVoxelModification modification);

        /// <summary>
        /// Processes pending modifications
        /// </summary>
        void ProcessModifications(int maxCount = 100);

        /// <summary>
        /// Force immediate mesh update for modified chunks
        /// </summary>
        void UpdateMeshes();

        /// <summary>
        /// Event fired when any voxel is changed
        /// </summary>
        event System.Action<Vector3Int, Shared.VoxelType, Shared.VoxelType> OnVoxelChanged;

        /// <summary>
        /// Event fired when a chunk is modified (for NavMesh updates)
        /// </summary>
        event System.Action<IChunkData> OnChunkModified;
    }
}
