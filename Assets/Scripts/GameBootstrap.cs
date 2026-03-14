using UnityEngine;
using Albia.Core;
using Albia.World;

namespace Albia
{
    /// <summary>
    /// Bootstrap entry point for the game.
    /// Initializes all systems and starts world generation.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        public GameManager GameManager;
        public CreatureSpawner Spawner;
        public TimeManager TimeManager;

        [Header("Settings")]
        public int Seed = 1337;
        public bool SpawnCreaturesOnStart = true;

        void Awake()
        {
            // Ensure managers exist
            if (GameManager == null)
                GameManager = FindObjectOfType<GameManager>();
            
            if (TimeManager == null)
                TimeManager = FindObjectOfType<TimeManager>();

            // Create managers if missing
            if (GameManager == null)
            {
                GameObject managerObj = new GameObject("GameManager");
                GameManager = managerObj.AddComponent<GameManager>();
            }
            
            if (TimeManager == null)
            {
                GameObject timeObj = new GameObject("TimeManager");
                TimeManager = timeObj.AddComponent<TimeManager>();
            }
        }

        void Start()
        {
            Debug.Log($"=== Albia Reborn Bootstrap ===");
            Debug.Log($"Seed: {Seed}");

            // Generate world
            if (GameManager != null)
            {
                GameManager.Seed = Seed;
                GameManager.AutoGenerateOnStart = true;
                GameManager.GenerateNewWorld(Seed);
            }

            // Spawn creatures
            if (SpawnCreaturesOnStart && Spawner != null)
            {
                Spawner.SpawnInitialPopulation();
            }

            Debug.Log($"Bootstrap complete. World ready.");
        }
    }
}
