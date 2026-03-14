namespace Albia.Climate.Interfaces;

/// <summary>
/// Interface for querying climate data from the world
/// </summary>
public interface IClimateQuery
{
    /// <summary>
    /// Gets the temperature at a specific world coordinate
    /// </summary>
    float GetTemperature(int x, int y);
    
    /// <summary>
    /// Gets the moisture level at a specific world coordinate
    /// </summary>
    float GetMoisture(int x, int y);
    
    /// <summary>
    /// Gets the biome type at a specific world coordinate
    /// </summary>
    BiomeType GetBiome(int x, int y);
    
    /// <summary>
    /// Gets the current season factor (0.0 = winter, 0.5 = summer)
    /// </summary>
    float GetSeasonFactor();
    
    /// <summary>
    /// Gets the average yearly temperature at a location
    /// </summary>
    float GetAverageTemperature(int x, int y);
    
    /// <summary>
    /// Gets the average yearly moisture at a location
    /// </summary>
    float GetAverageMoisture(int x, int y);
}
