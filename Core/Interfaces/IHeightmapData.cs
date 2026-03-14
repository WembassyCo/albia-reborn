namespace Albia.Core.Interfaces
{
    /// <summary>
    /// Interface for heightmap data consumed by the voxel engine.
    /// Provides terrain height and water information for each tile.
    /// </summary>
    public interface IHeightmapData
    {
        /// <summary>
        /// Gets the terrain height at the specified coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Height value (0-255 typical range)</returns>
        byte GetHeight(int x, int y);

        /// <summary>
        /// Gets the water level at the specified coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Water depth (0 if no water)</returns>
        byte GetWaterLevel(int x, int y);

        /// <summary>
        /// Checks if the specified tile is water.
        /// </summary>
        bool IsWater(int x, int y);

        /// <summary>
        /// Checks if the specified tile has a river.
        /// </summary>
        bool IsRiver(int x, int y);

        /// <summary>
        /// Gets the width of the heightmap.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets the height of the heightmap.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the raw tile data for the entire world.
        /// </summary>
        IWorldTile[,] GetTileData();
    }

    /// <summary>
    /// Interface representing a single world tile.
    /// </summary>
    public interface IWorldTile
    {
        /// <summary>
        /// Terrain height (0-255, where 0 is lowest, 255 is highest)
        /// </summary>
        byte Height { get; }

        /// <summary>
        /// True if this tile contains water (ocean, lake, river)
        /// </summary>
        bool IsWater { get; }

        /// <summary>
        /// True if this tile is part of a river network
        /// </summary>
        bool IsRiver { get; }

        /// <summary>
        /// True if this tile is a volcanic hotspot
        /// </summary>
        bool IsVolcanic { get; }

        /// <summary>
        /// Moisture level for climate calculations (0-255)
        /// </summary>
        byte Moisture { get; }
    }
}
