using UnityEngine;

namespace AlbiaReborn.World
{
    /// <summary>
    /// Simplified climate system with 3 biomes
    /// Temperature based on elevation and latitude (Y position)
    /// </summary>
    public enum BiomeType
    {
        Desert = 0,
        Grassland = 1,
        TemperateForest = 2
    }

    /// <summary>
    /// Climate system for determining biomes from world position
    /// </summary>
    public class ClimateSystem
    {
        // Climate constants
        public const float BaseTemperature = 30f;              // Base temp at sea level equator (°C)
        public const float TempDropPerUnitElevation = 0.0065f;  // °C per meter (standard lapse rate)
        public const float TempDropPerLatitude = 0.5f;           // °C per world unit from equator
        public const float EquatorY = 32f;                      // Y position of equator in 64x64 world
        
        // Biome temperature thresholds (°C)
        public const float DesertMaxTemp = 25f;      // Above this with low moisture = desert
        public const float ForestMinTemp = 10f;      // Below this = forest if moist
        public const float MoistureThreshold = 0.4f; // Moisture needed for forest/grassland
        
        // World dimensions
        public int WorldWidth { get; private set; }
        public int WorldHeight { get; private set; }
        
        /// <summary>
        /// Creates a new climate system
        /// </summary>
        public ClimateSystem(int width = 64, int height = 64)
        {
            WorldWidth = width;
            WorldHeight = height;
        }
        
        /// <summary>
        /// Calculates temperature at a world position
        /// Formula: temp = baseTemp - (elevation * tempDropPerUnit) - (latitude * tempDropPerLatitude)
        /// </summary>
        public float GetTemperature(int x, int y, float elevation)
        {
            // Calculate latitude (distance from equator)
            float latitudeDistance = Mathf.Abs(y - EquatorY);
            
            // Apply formula
            float temp = BaseTemperature 
                - (elevation * TempDropPerUnitElevation) 
                - (latitudeDistance * TempDropPerLatitude);
            
            return temp;
        }
        
        /// <summary>
        /// Gets temperature without elevation (surface level)
        /// </summary>
        public float GetSurfaceTemperature(int x, int y)
        {
            return GetTemperature(x, y, 0f);
        }
        
        /// <summary>
        /// Calculates moisture at a position
        /// Simplified: higher near equator and at moderate elevations
        /// </summary>
        public float GetMoisture(int x, int y, float elevation)
        {
            // Distance from equator affects moisture (tropics are wetter)
            float latitudeDistance = Mathf.Abs(y - EquatorY);
            float latitudeFactor = 1f - (latitudeDistance / (WorldHeight * 0.5f));
            
            // Elevation effect: moderate elevations get more rain (orographic effect)
            float elevationFactor = 1f - Mathf.Abs(elevation - 500f) / 2000f;
            elevationFactor = Mathf.Clamp01(elevationFactor);
            
            // Combine factors
            float moisture = (latitudeFactor * 0.6f) + (elevationFactor * 0.4f);
            
            // Add some noise/variation based on position
            moisture += (Mathf.Sin(x * 0.1f) + Mathf.Cos(y * 0.1f)) * 0.1f;
            
            return Mathf.Clamp01(moisture);
        }
        
        /// <summary>
        /// Determines biome type from temperature and moisture
        /// </summary>
        public BiomeType GetBiome(float temperature, float moisture)
        {
            // Desert: hot and dry, or cold and dry (polar desert)
            if (moisture < MoistureThreshold)
            {
                return BiomeType.Desert;
            }
            
            // Temperate Forest: moderate temperature and wet
            if (temperature < ForestMinTemp && moisture >= MoistureThreshold)
            {
                return BiomeType.TemperateForest;
            }
            
            // Grassland: everything else
            return BiomeType.Grassland;
        }
        
        /// <summary>
        /// Gets biome at a specific world position
        /// </summary>
        public BiomeType GetBiomeAt(int x, int y, float elevation = 0f)
        {
            float temp = GetTemperature(x, y, elevation);
            float moisture = GetMoisture(x, y, elevation);
            return GetBiome(temp, moisture);
        }
        
        /// <summary>
        /// Gets biome at surface level (elevation = 0)
        /// </summary>
        public BiomeType GetSurfaceBiome(int x, int y)
        {
            return GetBiomeAt(x, y, 0f);
        }
        
        /// <summary>
        /// Gets the color associated with each biome
        /// </summary>
        public static Color GetBiomeColor(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Desert => new Color(0.93f, 0.87f, 0.58f),      // Sandy yellow
                BiomeType.Grassland => new Color(0.55f, 0.78f, 0.35f),    // Yellow-green
                BiomeType.TemperateForest => new Color(0.24f, 0.59f, 0.24f), // Forest green
                _ => Color.gray
            };
        }
        
        /// <summary>
        /// Gets biome display name
        /// </summary>
        public static string GetBiomeName(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Desert => "Desert",
                BiomeType.Grassland => "Grassland",
                BiomeType.TemperateForest => "Temperate Forest",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Generates a biome map for the entire world
        /// </summary>
        public BiomeType[,] GenerateBiomeMap(float[,] elevationMap)
        {
            BiomeType[,] biomeMap = new BiomeType[WorldWidth, WorldHeight];
            
            for (int x = 0; x < WorldWidth; x++)
            {
                for (int y = 0; y < WorldHeight; y++)
                {
                    float elevation = elevationMap?[x, y] ?? 0f;
                    biomeMap[x, y] = GetBiomeAt(x, y, elevation);
                }
            }
            
            return biomeMap;
        }
        
        /// <summary>
        /// Generates a temperature map for the entire world
        /// </summary>
        public float[,] GenerateTemperatureMap(float[,] elevationMap)
        {
            float[,] tempMap = new float[WorldWidth, WorldHeight];
            
            for (int x = 0; x < WorldWidth; x++)
            {
                for (int y = 0; y < WorldHeight; y++)
                {
                    float elevation = elevationMap?[x, y] ?? 0f;
                    tempMap[x, y] = GetTemperature(x, y, elevation);
                }
            }
            
            return tempMap;
        }
        
        /// <summary>
        /// Generates a moisture map for the entire world
        /// </summary>
        public float[,] GenerateMoistureMap(float[,] elevationMap)
        {
            float[,] moistureMap = new float[WorldWidth, WorldHeight];
            
            for (int x = 0; x < WorldWidth; x++)
            {
                for (int y = 0; y < WorldHeight; y++)
                {
                    float elevation = elevationMap?[x, y] ?? 0f;
                    moistureMap[x, y] = GetMoisture(x, y, elevation);
                }
            }
            
            return moistureMap;
        }
    }
}
