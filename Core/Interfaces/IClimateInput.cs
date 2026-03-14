namespace Albia.Core.Interfaces
{
    /// <summary>
    /// Interface for climate-relevant data flowing from terrain to climate system.
    /// Provides necessary inputs for temperature, precipitation, and weather calculations.
    /// </summary>
    public interface IClimateInput
    {
        /// <summary>
        /// Gets the width of the climate grid.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets the height of the climate grid.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the elevation at the specified coordinates.
        /// Higher elevations have lower temperatures.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Elevation in meters (approximate)</returns>
        float GetElevation(int x, int y);

        /// <summary>
        /// Gets the moisture availability at the specified coordinates.
        /// Higher values indicate more water available for evaporation.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Moisture level (0-1 range)</returns>
        float GetMoisture(int x, int y);

        /// <summary>
        /// Checks if the tile is water (ocean, lake, river).
        /// </summary>
        bool IsWater(int x, int y);

        /// <summary>
        /// Gets the terrain roughness/freeness at the specified coordinates.
        /// Used for wind calculations and temperature variations.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Roughness value (0-1 range)</returns>
        float GetRoughness(int x, int y);

        /// <summary>
        /// Gets the latitude-based temperature modifier.
        /// Returns 1.0 at equator, 0.0 at poles (adjustable).
        /// </summary>
        float GetLatitudeFactor(int y);

        /// <summary>
        /// Gets distance to nearest ocean/water body from this tile.
        /// Used for continental climate calculations.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Distance in tile units</returns>
        int GetDistanceToWater(int x, int y);
    }
}
