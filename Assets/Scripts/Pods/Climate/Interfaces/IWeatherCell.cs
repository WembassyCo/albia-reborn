namespace AlbiaReborn.Climate.Interfaces;

/// <summary>
/// Interface representing a single cell in the weather grid
/// </summary>
public interface IWeatherCell
{
    /// <summary>
    /// X coordinate in the grid
    /// </summary>
    int X { get; }
    
    /// <summary>
    /// Y coordinate in the grid
    /// </summary>
    int Y { get; }
    
    /// <summary>
    /// Current temperature in Celsius
    /// </summary>
    float Temperature { get; set; }
    
    /// <summary>
    /// Current moisture level (0.0 - 1.0)
    /// </summary>
    float Moisture { get; set; }
    
    /// <summary>
    /// Current precipitation type
    /// </summary>
    PrecipitationType Precipitation { get; }
    
    /// <summary>
    /// Precipitation intensity (0.0 - 1.0)
    /// </summary>
    float PrecipitationIntensity { get; }
    
    /// <summary>
    /// Cloud cover (0.0 - 1.0)
    /// </summary>
    float CloudCover { get; set; }
    
    /// <summary>
    /// Wind direction in degrees (0-360)
    /// </summary>
    float WindDirection { get; set; }
    
    /// <summary>
    /// Wind speed in m/s
    /// </summary>
    float WindSpeed { get; set; }
    
    /// <summary>
    /// Atmospheric pressure in hPa
    /// </summary>
    float Pressure { get; set; }
    
    /// <summary>
    /// Updates the weather cell state
    /// </summary>
    void Update(float deltaTime);
    
    /// <summary>
    /// Checks if conditions support storm formation
    /// </summary>
    bool CanFormStorm();
}

/// <summary>
/// Types of precipitation
/// </summary>
public enum PrecipitationType
{
    None,
    Rain,
    Snow,
    Hail,
    Sleet
}
