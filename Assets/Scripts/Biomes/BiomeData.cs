using UnityEngine;
using System.Linq;

namespace Albia.Biomes
{
    /// <summary>
    /// Biome-specific data and generation
    /// MVP: Simple biome types
    /// Full: Complex generation patterns
    /// </summary>
    public enum BiomeType { Temperate, Desert, Arctic, Rainforest, Volcanic }
    
    [CreateAssetMenu(fileName = "BiomeData", menuName = "Albia/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        public BiomeType biomeType;
        
        [Header("Temperature")]
        public float baseTemperature = 20f;
        public float temperatureVariation = 10f;
        
        [Header("Spawning")]
        public GameObject[] plantPrefabs;
        public int plantDensity = 50;
        public float spawnChance = 0.3f;
        
        [Header("Colors")]
        public Color groundColor = Color.green;
        public Color skyColor = Color.cyan;
        
        public float GetCurrentTemperature(float timeOfDay)
        {
            // Day/night temperature cycle
            float dayFactor = Mathf.Sin(timeOfDay * Mathf.PI);
            return baseTemperature + dayFactor * temperatureVariation;
        }
    }
}