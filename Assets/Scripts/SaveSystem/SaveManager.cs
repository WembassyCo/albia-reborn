using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Albia.Core;
using Albia.Creatures;

namespace Albia.SaveSystem
{
    /// <summary>
    /// Save/load world state.
    /// MVP: JSON serialization
    /// Full: Binary compression, versioning
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Auto Save")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private int saveSlotCount = 3;

        private float autoSaveTimer = 0f;
        private int currentSlot = 0;

        public string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

        void Awake()
        {
            Instance = this;
            Directory.CreateDirectory(SaveDirectory);
        }

        void Update()
        {
            if (!autoSave) return;
            
            autoSaveTimer += Time.unscaledDeltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;
                AutoSave();
            }
        }

        /// <summary>
        /// Save current world state
        /// </summary>
        public void SaveWorld(string filename)
        {
            WorldData data = new WorldData();
            data.SaveCurrentState();
            
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(SaveDirectory, $"{filename}.json");
            
            File.WriteAllText(path, json);
            Debug.Log($"[SaveManager] Saved to {path}");
        }

        /// <summary>
        /// Load world state
        /// </summary>
        public bool LoadWorld(string filename)
        {
            string path = Path.Combine(SaveDirectory, $"{filename}.json");
            
            if (!File.Exists(path))
            {
                Debug.LogError($"[SaveManager] Save file not found: {path}");
                return false;
            }

            string json = File.ReadAllText(path);
            WorldData data = JsonUtility.FromJson<WorldData>(json);
            
            data.RestoreState();
            Debug.Log($"[SaveManager] Loaded from {path}");
            return true;
        }

        /// <summary>
        /// Auto-save to rotating slots
        /// </summary>
        private void AutoSave()
        {
            currentSlot = (currentSlot + 1) % saveSlotCount;
            SaveWorld($"autosave_{currentSlot}");
        }

        /// <summary>
        /// Get list of save files
        /// </summary>
        public string[] GetSaveFiles()
        {
            if (!Directory.Exists(SaveDirectory)) return Array.Empty<string>();
            return Directory.GetFiles(SaveDirectory, "*.json");
        }
    }
}

[Serializable]
public class WorldData
{
    public int worldSeed;
    public float totalGameTime;
    public int gameDay;
    public float dayTime;
    
    public CreatureSaveData[] creatures;
    public FoodSaveData[] food;
    // SCALES TO: Terrain, plants, structures

    /// <summary>
    /// Capture current world state
    /// </summary>
    public void SaveCurrentState()
    {
        // Seed and time
        worldSeed = WorldSeed.CurrentSeed;
        if (TimeManager.Instance != null)
        {
            totalGameTime = TimeManager.Instance.TotalGameTime;
            gameDay = TimeManager.Instance.GameDay;
            dayTime = TimeManager.Instance.DayTime;
        }

        // Creatures
        var creaturesList = new List<CreatureSaveData>();
        foreach (var norn in GameObject.FindObjectsOfType<Norn>())
        {
            creaturesList.Add(new CreatureSaveData(norn));
        }
        creatures = creaturesList.ToArray();

        // Food
        var foodList = new List<FoodSaveData>();
        foreach (var foodSource in GameObject.FindObjectsOfType<Albia.Ecology.FoodSource>())
        {
            foodList.Add(new FoodSaveData(foodSource));
        }
        food = foodList.ToArray();
    }

    /// <summary>
    /// Restore world state (called after load)
    /// </summary>
    public void RestoreState()
    {
        // SCALES TO: Clear existing, instantiate saved
        // For MVP: Just log
        Debug.Log($"[WorldData] Restored {creatures?.Length ?? 0} creatures, {food?.Length ?? 0} food");
        
        // Apply time
        if (TimeManager.Instance != null)
        {
            // TimeManager would need deserialize support
        }

        // SCALES TO: Full instantiation with reconstruction
    }
}

[Serializable]
public class CreatureSaveData
{
    public string name;
    public float[] position;
    public float energy;
    public float maxEnergy;
    public float age;
    public int stage;
    public string genomeJson;
    public string chemicalJson;
    // SCALES TO: Neural weights, memory, lineage

    public CreatureSaveData(Norn norn)
    {
        name = norn.name;
        position = new float[] { norn.transform.position.x, norn.transform.position.y, norn.transform.position.z };
        energy = norn.Energy;
        maxEnergy = norn.MaxEnergy;
        age = norn.Age;
        stage = (int)norn.Stage;
        
        if (norn.Genome != null)
            genomeJson = norn.Genome.ToJson();
        
        if (norn.Chemicals != null)
            chemicalJson = JsonUtility.ToJson(norn.Chemicals);
    }
}

[Serializable]
public class FoodSaveData
{
    public float[] position;
    public bool isConsumed;
    
    public FoodSaveData(Albia.Ecology.FoodSource food)
    {
        position = new float[] { food.transform.position.x, food.transform.position.y, food.transform.position.z };
    }
}