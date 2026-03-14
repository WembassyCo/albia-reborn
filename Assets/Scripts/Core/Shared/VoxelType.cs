namespace Albia.Core.Shared
{
    /// <summary>
    /// Enumeration of all voxel types in the Albia world.
    /// Values are stored as bytes in chunk data for memory efficiency.
    /// </summary>
    public enum VoxelType : byte
    {
        Air = 0,      // Empty space
        Dirt = 1,     // Soil layer
        Stone = 2,    // Bedrock/mountain
        Sand = 3,     // Beaches/deserts
        Clay = 4,     // Riverbeds/wetlands
        Snow = 5,     // Snowy regions
        Ice = 6,      // Frozen water
        Ash = 7,      // Volcanic regions
        Water = 8     // Flowing/static water
    }

    /// <summary>
    /// Extension methods for VoxelType
    /// </summary>
    public static class VoxelTypeExtensions
    {
        /// <summary>
        /// Returns true if the voxel type is solid (blocks movement)
        /// </summary>
        public static bool IsSolid(this VoxelType type)
        {
            return type != VoxelType.Air && type != VoxelType.Water;
        }

        /// <summary>
        /// Returns true if the voxel type is liquid (flows)
        /// </summary>
        public static bool IsLiquid(this VoxelType type)
        {
            return type == VoxelType.Water;
        }

        /// <summary>
        /// Returns true if the voxel type is empty/air
        /// </summary>
        public static bool IsEmpty(this VoxelType type)
        {
            return type == VoxelType.Air;
        }

        /// <summary>
        /// Returns the density value for SurfaceNets mesh generation.
        /// Positive = solid, Negative/Zero = empty space
        /// </summary>
        public static float GetDensity(this VoxelType type)
        {
            return type switch
            {
                VoxelType.Air => -1f,
                VoxelType.Water => 0.2f,  // Water has positive but low density
                _ => 1f  // All solids have full density
            };
        }

        /// <summary>
        /// Returns the color tint for this voxel type (for visualization)
        /// </summary>
        public static UnityEngine.Color GetColor(this VoxelType type)
        {
            return type switch
            {
                VoxelType.Dirt => new UnityEngine.Color(0.55f, 0.35f, 0.20f),
                VoxelType.Stone => new UnityEngine.Color(0.5f, 0.5f, 0.5f),
                VoxelType.Sand => new UnityEngine.Color(0.93f, 0.88f, 0.69f),
                VoxelType.Clay => new UnityEngine.Color(0.69f, 0.44f, 0.32f),
                VoxelType.Snow => new UnityEngine.Color(0.98f, 0.98f, 1.0f),
                VoxelType.Ice => new UnityEngine.Color(0.72f, 0.87f, 0.95f),
                VoxelType.Ash => new UnityEngine.Color(0.2f, 0.2f, 0.2f),
                VoxelType.Water => new UnityEngine.Color(0.2f, 0.4f, 0.8f, 0.6f),
                _ => UnityEngine.Color.clear
            };
        }

        /// <summary>
        /// Returns the material index for batch rendering
        /// </summary>
        public static int GetMaterialIndex(this VoxelType type)
        {
            return (int)type;
        }
    }
}
