using UnityEngine;
using System.Collections.Generic;

namespace AlbiaReborn.Core.Interfaces
{
    /// <summary>
    /// Interface for voxel world modifications.
    /// Defines the contract for block-level terrain changes.
    /// </summary>
    public interface IVoxelModification
    {
        void SetVoxel(Vector3Int voxelPosition, IVoxelData voxelData);
        IVoxelData GetVoxel(Vector3Int voxelPosition);
        void RemoveVoxel(Vector3Int voxelPosition);
        bool IsVoxelEmpty(Vector3Int voxelPosition);
        void SetVoxelRange(BoundsInt voxelBounds, IVoxelData voxelData);
        void RemoveVoxelRange(BoundsInt voxelBounds);
        void InvalidateChunkAt(Vector3Int voxelPosition);
        void GetVoxelsInRange(Vector3Int center, int radius, List<IVoxelData> results);
    }

    /// <summary>
    /// Represents individual voxel data.
    /// </summary>
    public interface IVoxelData
    {
        byte BlockId { get; set; }
        byte Metadata { get; set; }
        float Integrity { get; set; }
        IVoxelMaterial Material { get; }
    }

    /// <summary>
    /// Represents voxel material properties.
    /// </summary>
    public interface IVoxelMaterial
    {
        string MaterialName { get; }
        float Hardness { get; }
        bool IsSolid { get; }
        float ThermalConductivity { get; }
        bool IsFluid { get; }
    }
}