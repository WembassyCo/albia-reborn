using AlbiaReborn.Climate.Interfaces;

namespace AlbiaReborn.Climate.Models;

/// <summary>
/// Implementation of a weather cell in the 64x64 grid
/// </summary>
public class WeatherCell : IWeatherCell
{
    // Grid position
    public int X { get; }
    public int Y { get; }
    
    // Climate values
    public float Temperature { get; set; }
    public float Moisture { get; set; }
    
    // Weather state
    public float CloudCover { get; set; }
    public float WindDirection { get; set; } // degrees
    public float WindSpeed { get; set; } // m/s
    public float Pressure { get; set; } // hPa (1000 is standard)
    
    // Precipitation
    public PrecipitationType PrecipitationType { get; set; }
    private float _precipitationIntensity;
    public float PrecipitationIntensity 
    { 
        get => _precipitationIntensity;
        set => _precipitationIntensity = Math.Clamp(value, 0f, 1f);
    }
    
    // Biome and climate
    public BiomeType Biome { get; set; }
    public float AverageTemperature { get; set; }
    public float AverageMoisture { get; set; }
    
    // Seasonal modifiers
    public float SeasonPhase { get; set; } // 0-1 where 0.5 is peak of season
    
    // Internal state
    private float _accumulatedPrecipitation;
    private float _timeSinceLastUpdate;
    
    // Constants
    private const float CLOUD_FORMATION_THRESHOLD = 0.4f;
    private const float RAIN_THRESHOLD = 0.5f;
    private const float SNOW_TEMPERATURE = 0f; // °C
    private const float PRECIPITATION_DECAY = 0.1f;
    
    public WeatherCell(int x, int y)
    {
        X = x;
        Y = y;
        
        // Default values
        Temperature = 15f;
        Moisture = 0.5f;
        CloudCover = 0f;
        WindDirection = 0f;
        WindSpeed = 2f;
        Pressure = 1013f;
        PrecipitationType = PrecipitationType.None;
        PrecipitationIntensity = 0f;
        Biome = BiomeType.TemperateForest;
        AverageTemperature = 15f;
        AverageMoisture = 0.5f;
        SeasonPhase = 0.5f;
    }
    
    /// <summary>
    /// Updates the weather cell with time delta
    /// </summary>
    public void Update(float deltaTime)
    {
        _timeSinceLastUpdate += deltaTime;
        
        // Update precipitation based on temperature
        UpdatePrecipitation(deltaTime);
        
        // Natural decay of precipitation
        if (PrecipitationIntensity > 0)
        {
            PrecipitationIntensity = Math.Max(0f, PrecipitationIntensity - PRECIPITATION_DECAY * deltaTime);
            if (PrecipitationIntensity < 0.05f)
            {
                PrecipitationType = PrecipitationType.None;
                PrecipitationIntensity = 0f;
            }
        }
        
        // Natural pressure equalization
        Pressure += (1013f - Pressure) * 0.01f * deltaTime;
        
        // Seasonal temperature drift toward average
        float seasonalModifier = (float)Math.Sin(SeasonPhase * Math.PI * 2);
        float targetTemp = AverageTemperature + (seasonalModifier * 10f); // ±10°C seasonal variation
        Temperature += (targetTemp - Temperature) * 0.05f * deltaTime;
        
        // Moisture equilibrium
        float targetMoisture = AverageMoisture + (seasonalModifier * 0.2f);
        Moisture += (targetMoisture - Moisture) * 0.02f * deltaTime;
        Moisture = Math.Clamp(Moisture, 0f, 1f);
    }
    
    /// <summary>
    /// Updates precipitation based on cloud cover and temperature
    /// </summary>
    private void UpdatePrecipitation(float deltaTime)
    {
        // Only form precipitation if sufficient cloud cover
        if (CloudCover < CLOUD_FORMATION_THRESHOLD)
        {
            return;
        }
        
        // Calculate precipitation chance based on moisture and cloud cover
        float precipChance = (CloudCover * Moisture) - RAIN_THRESHOLD;
        if (precipChance <= 0) return;
        
        // Accumulate potential precipitation
        _accumulatedPrecipitation += precipChance * deltaTime;
        
        // Trigger precipitation when accumulated enough
        if (_accumulatedPrecipitation > 0.3f)
        {
            _accumulatedPrecipitation = 0f;
            
            // Determine precipitation type based on temperature
            if (Temperature < SNOW_TEMPERATURE)
            {
                PrecipitationType = PrecipitationType.Snow;
            }
            else if (Temperature < SNOW_TEMPERATURE + 2f)
            {
                PrecipitationType = PrecipitationType.Sleet;
            }
            else
            {
                PrecipitationType = PrecipitationType.Rain;
            }
            
            PrecipitationIntensity = Math.Min(1f, precipChance * 2f);
        }
    }
    
    /// <summary>
    /// Checks if conditions support storm formation
    /// Criteria:
    /// - High moisture gradients
    /// - Significant temperature differentials  
    /// - Unstable atmosphere (changing pressure)
    /// - Sufficient energy in system
    /// </summary>
    public bool CanFormStorm()
    {
        // Need high moisture
        if (Moisture < 0.4f) return false;
        
        // Need energy (temperature relative to average)
        float tempAnomaly = Math.Abs(Temperature - AverageTemperature);
        if (tempAnomaly < 5f) return false;
        
        // Need instability (pressure anomaly)
        float pressureAnomaly = Math.Abs(Pressure - 1013f);
        if (pressureAnomaly < 10f) return false;
        
        // Storm potential score
        float stormPotential = Moisture * (tempAnomaly / 20f) * (pressureAnomaly / 30f);
        return stormPotential > 0.3f;
    }
    
    /// <summary>
    /// Gets the temperature differential from average
    /// </summary>
    public float GetTemperatureDifferential()
    {
        return Temperature - AverageTemperature;
    }
    
    /// <summary>
    /// Gets the moisture differential from average
    /// </summary>
    public float GetMoistureDifferential()
    {
        return Moisture - AverageMoisture;
    }
    
    /// <summary>
    /// Adds moisture to the cell
    /// </summary>
    public void AddMoisture(float amount)
    {
        Moisture = Math.Min(1f, Moisture + amount);
    }
    
    /// <summary>
    /// Modifies temperature
    /// </summary>
    public void ModifyTemperature(float delta)
    {
        Temperature += delta;
    }
    
    /// <summary>
    /// Simulates precipitation falling and depositing moisture
    /// </summary>
    public void ProcessPrecipitation(float deltaTime)
    {
        if (PrecipitationIntensity <= 0) return;
        
        // Rain adds moisture to ground
        if (PrecipitationType == PrecipitationType.Rain ||
            PrecipitationType == PrecipitationType.Drizzle)
        {
            // But it doesn't directly increase air moisture
            // This could add to ground water table in the future
        }
        
        // Snow/sleet reduces temperature locally
        if (PrecipitationType == PrecipitationType.Snow ||
            PrecipitationType == PrecipitationType.Sleet)
        {
            Temperature -= 0.1f * PrecipitationIntensity * deltaTime;
        }
    }
    
    /// <summary>
    /// Gets a summary of current conditions
    /// </summary>
    public override string ToString()
    {
        return $"[{X},{Y}] {Temperature:F1}°C, {Moisture:P0} moisture, {Biome}, " +
               $"Precip: {PrecipitationType} ({PrecipitationIntensity:P0}), " +
               $"Clouds: {CloudCover:P0}";
    }
    
    // Interface implementation
    PrecipitationType IWeatherCell.Precipitation => PrecipitationType;
    
    float IWeatherCell.PrecipitationIntensity => PrecipitationIntensity;
}
