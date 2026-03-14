using UnityEngine;

namespace AlbiaReborn.Core.Interfaces
{
    /// <summary>
    /// Interface for heightmap/terrain elevation data operations.
    /// Defines the contract between Terrain pod and consuming systems.
    /// </summary>
    public interface IHeightmapData
    {
        float GetElevationAt(Vector2Int worldPosition);
        float GetElevationAt(float worldX, float worldZ);
        void SetElevation(Vector2Int worldPosition, float elevation);
        Vector3 GetNormalAt(Vector2Int worldPosition);
        Vector2Int GetWorldSize();
        float GetMinElevation();
        float GetMaxElevation();
        float SampleBilinear(float worldX, float worldZ);
    }
}