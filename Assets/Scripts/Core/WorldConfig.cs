using UnityEngine;

namespace Albia.Core
{
    /// <summary>
    /// Global world configuration
    /// MVP: Static configuration
    /// Full: ScriptableObject with multiple presets
    /// </summary>
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "Albia/World Config")]
    public class WorldConfig : ScriptableObject
    {
        [Header("World Generation")]
        public int worldSeed = 12345;
        public Vector3Int worldSize = new Vector3Int(100, 32, 100);
        public float seaLevel = 8f;
        
        [Header("Time")]
        public float secondsPerGameDay = 300f;
        public float dayLength = 150f; // Day/night split
        
        [Header("Creature Limits")]
        public int maxNorns = 20;
        public int maxGrendels = 5;
        public int maxPlants = 100;
        
        [Header("Spawning")]
        public bool autoSpawnNorns = true;
        public bool autoSpawnGrendels = true;
        public float spawnCheckInterval = 10f;
        
        public static WorldConfig Default => new WorldConfig();
    }
}