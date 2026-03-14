namespace AlbiaReborn.Climate;

/// <summary>
/// Whittaker biome classification system
/// Based on temperature and moisture gradients
/// </summary>
public enum BiomeType
{
    // Cold biomes (temperature < 0°C average)
    Tundra,           // Cold, dry - treeless plains
    BorealForest,     // Cold, moderate moisture - coniferous forests (taiga)
    
    // Cool/Mild biomes (0-15°C average)
    TemperateGrassland,  // Cool, dry - grasslands, prairies
    TemperateForest,     // Cool, moderate moisture - deciduous forests
    TemperateRainforest, // Cool, wet - evergreen forests
    
    // Warm biomes (15-25°C average)
    WoodlandShrubland,   // Warm, dry - scrub, chaparral
    
    // Hot biomes (> 25°C average)
    Desert,           // Hot, dry - arid, sparse vegetation
    TropicalGrassland, // Hot, moderate moisture - savannas
    TropicalRainforest // Hot, wet - dense jungles
}

/// <summary>
/// Helper class for biome classification
/// </summary>
public static class BiomeHelper
{
    // Temperature thresholds (Celsius)
    public const float FREEZING = 0f;
    public const float COOL = 10f;
    public const float TEMPERATE = 15f;
    public const float WARM = 25f;
    
    // Moisture thresholds (0.0 - 1.0 scale)
    public const float ARID = 0.25f;
    public const float SEMI_ARID = 0.4f;
    public const float MODERATE = 0.6f;
    public const float WET = 0.75f;
    
    /// <summary>
    /// Gets the biome based on average temperature and moisture
    /// Implements Whittaker biome classification
    /// </summary>
    public static BiomeType GetBiomeFromClimate(float avgTemp, float avgMoisture)
    {
        // Clamp values to valid ranges
        avgTemp = Math.Clamp(avgTemp, -30f, 45f);
        avgMoisture = Math.Clamp(avgMoisture, 0f, 1f);
        
        if (avgTemp < FREEZING)
        {
            // Cold region
            return avgMoisture > MODERATE ? BiomeType.BorealForest : BiomeType.Tundra;
        }
        else if (avgTemp < TEMPERATE)
        {
            // Coo/Temperate region
            if (avgMoisture < ARID) return BiomeType.TemperateGrassland;
            if (avgMoisture < WET) return BiomeType.TemperateForest;
            return BiomeType.TemperateRainforest;
        }
        else if (avgTemp < WARM)
        {
            // Warm region
            if (avgMoisture < SEMI_ARID) return BiomeType.Desert;
            if (avgMoisture < WET) return BiomeType.WoodlandShrubland;
            return BiomeType.TropicalRainforest;
        }
        else
        {
            // Hot region
            if (avgMoisture < MODERATE) return BiomeType.Desert;
            if (avgMoisture < WET) return BiomeType.TropicalGrassland;
            return BiomeType.TropicalRainforest;
        }
    }
    
    /// <summary>
    /// Gets a display name for the biome
    /// </summary>
    public static string GetBiomeName(BiomeType biome) => biome switch
    {
        BiomeType.Tundra => "Tundra",
        BiomeType.BorealForest => "Boreal Forest (Taiga)",
        BiomeType.TemperateGrassland => "Temperate Grassland",
        BiomeType.TemperateForest => "Temperate Forest",
        BiomeType.TemperateRainforest => "Temperate Rainforest",
        BiomeType.WoodlandShrubland => "Woodland/Shrubland",
        BiomeType.Desert => "Desert",
        BiomeType.TropicalGrassland => "Tropical Grassland (Savanna)",
        BiomeType.TropicalRainforest => "Tropical Rainforest",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Gets color for visualization (R,G,B values 0-255)
    /// </summary>
    public static (byte r, byte g, byte b) GetBiomeColor(BiomeType biome) => biome switch
    {
        BiomeType.Tundra => (200, 220, 240),        // Pale blue-white
        BiomeType.BorealForest => (40, 100, 40),     // Dark green
        BiomeType.TemperateGrassland => (210, 200, 120), // Yellow-green
        BiomeType.TemperateForest => (60, 150, 60),    // Medium green
        BiomeType.TemperateRainforest => (80, 180, 100), // Bright green
        BiomeType.WoodlandShrubland => (180, 150, 80),  // Brown-green
        BiomeType.Desert => (240, 220, 150),          // Sandy
        BiomeType.TropicalGrassland => (200, 180, 80), // Yellow-brown
        BiomeType.TropicalRainforest => (30, 120, 30),  // Deep green
        _ => (128, 128, 128)
    };
}
