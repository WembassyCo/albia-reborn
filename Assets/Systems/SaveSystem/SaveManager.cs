using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.Systems.SaveSystem
{
    /// <summary>
    /// Central manager for world save/load operations with auto-save capability.
    /// Implements singleton pattern for easy access.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        #region Singleton
        
        public static SaveManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        #endregion

        #region Configuration
        
        [Header("Auto-Save Configuration")]
        [Tooltip("Enable automatic saving every N minutes")]
        [SerializeField] private bool enableAutoSave = true;
        
        [Tooltip("Auto-save interval in minutes")]
        [SerializeField, Range(1f, 60f)] private float autoSaveIntervalMinutes = 5f;
        
        [Tooltip("Number of auto-save slots to rotate through")]
        [SerializeField, Range(1, 10)] private int autoSaveSlots = 3;
        
        [Header("Save Configuration")]
        [Tooltip("Maximum number of manual save slots")]
        [SerializeField] private int maxManualSaveSlots = 10;
        
        [Tooltip("File extension for save files")]
        [SerializeField] private string saveFileExtension = ".albiasave";
        
        // Events
        public event Action OnSaveStarted;
        public event Action<bool> OnSaveCompleted;
        public event Action<bool> OnLoadCompleted;
        
        // State
        private string _saveDirectory;
        private Coroutine _autoSaveCoroutine;
        private int _currentAutoSaveSlot = 0;
        private float _sessionPlayTime = 0f;
        private DateTime _sessionStartTime;
        private bool _isSaving = false;
        
        // Properties
        public string SaveDirectory => _saveDirectory;
        public bool IsSaving => _isSaving;
        public float SessionPlayTime => _sessionPlayTime;
        
        #endregion

        #region Initialization
        
        private void Initialize()
        {
            // Use persistent data path as specified in constraints
            _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            
            // Ensure save directory exists
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
                Debug.Log($"[SaveManager] Created save directory: {_saveDirectory}");
            }
            
            _sessionStartTime = DateTime.Now;
            
            if (enableAutoSave)
            {
                StartAutoSave();
            }
            
            Debug.Log($"[SaveManager] Initialized. Save directory: {_saveDirectory}");
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
            }
        }
        
        private void Update()
        {
            // Track session play time
            _sessionPlayTime += Time.unscaledDeltaTime;
        }
        
        #endregion

        #region Auto-Save
        
        private void StartAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
            }
            
            _autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
            Debug.Log($"[SaveManager] Auto-save started ({autoSaveIntervalMinutes} minute interval)");
        }
        
        private IEnumerator AutoSaveRoutine()
        {
            float intervalSeconds = autoSaveIntervalMinutes * 60f;
            
            while (true)
            {
                yield return new WaitForSecondsRealtime(intervalSeconds);
                
                if (_isSaving) continue;
                
                // Rotate through auto-save slots
                _currentAutoSaveSlot = (_currentAutoSaveSlot % autoSaveSlots) + 1;
                string filename = $"autosave_{_currentAutoSaveSlot}";
                
                Debug.Log($"[SaveManager] Auto-saving to slot {_currentAutoSaveSlot}...");
                
                // Fire and forget - don't block the coroutine
                StartCoroutine(AutoSaveCoroutine(filename));
            }
        }
        
        private IEnumerator AutoSaveCoroutine(string filename)
        {
            // Build data in background
            yield return StartCoroutine(BuildWorldDataRoutine(null, (data) =>
            {
                if (data != null)
                {
                    string filepath = GetSaveFilePath(filename);
                    string json = JsonUtility.ToJson(data, true);
                    
                    try
                    {
                        File.WriteAllText(filepath, json);
                        Debug.Log($"[SaveManager] Auto-saved successfully to {filepath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveManager] Auto-save failed: {ex.Message}");
                    }
                }
            }));
        }
        
        public void StopAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
                Debug.Log("[SaveManager] Auto-save stopped");
            }
        }
        
        public void RestartAutoSave()
        {
            StartAutoSave();
        }
        
        #endregion

        #region Manual Save
        
        /// <summary>
        /// Saves the current world state to a file.
        /// </summary>
        /// <param name="filename">Name of the save file (without extension)</param>
        public void SaveWorld(string filename)
        {
            if (_isSaving)
            {
                Debug.LogWarning("[SaveManager] Save already in progress, cannot start new save");
                return;
            }
            
            StartCoroutine(SaveWorldRoutine(filename));
        }
        
        private IEnumerator SaveWorldRoutine(string filename)
        {
            _isSaving = true;
            OnSaveStarted?.Invoke();
            
            bool success = false;
            
            yield return StartCoroutine(BuildWorldDataRoutine(filename, (data) =>
            {
                if (data == null)
                {
                    Debug.LogError("[SaveManager] Failed to build world data");
                    return;
                }
                
                try
                {
                    string filepath = GetSaveFilePath(filename);
                    string json = JsonUtility.ToJson(data, true);
                    File.WriteAllText(filepath, json);
                    
                    success = true;
                    Debug.Log($"[SaveManager] World saved successfully to {filepath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] Failed to save world: {ex.Message}");
                }
            }));
            
            _isSaving = false;
            OnSaveCompleted?.Invoke(success);
        }
        
        /// <summary>
        /// Coroutine that builds world data from current game state.
        /// </summary>
        private IEnumerator BuildWorldDataRoutine(string filename, Action<WorldData> callback)
        {
            string worldName = !string.IsNullOrEmpty(filename) ? filename : "World";
            WorldData worldData = WorldData.CreateNew(worldName);
            
            // Update world time and play time
            worldData.WorldTime = Time.time;
            worldData.TotalPlayTime = _sessionPlayTime;
            worldData.GameSpeed = Time.timeScale;
            
            // Build creature data - only serialize public properties
            var creatures = FindObjectsOfType<Norn>();
            var creatureDataList = new List<CreatureData>();
            
            foreach (var creature in creatures)
            {
                if (creature != null)
                {
                    creatureDataList.Add(SerializeCreature(creature));
                }
                
                // Yield every few creatures to prevent frame drops
                if (creatureDataList.Count % 5 == 0)
                {
                    yield return null;
                }
            }
            
            worldData.Creatures = creatureDataList.ToArray();
            
            // TODO: Serialize food/plants (implement when food system is ready)
            worldData.Foods = new FoodData[0];
            
            // Serialize camera state
            worldData.CameraState = SerializeCameraState();
            
            callback?.Invoke(worldData);
        }
        
        private CreatureData SerializeCreature(Norn creature)
        {
            var state = creature.State;
            var genome = creature.Genome;
            
            return new CreatureData
            {
                Id = Guid.NewGuid(),
                PrefabName = creature.name,
                Position = creature.transform.position,
                Rotation = creature.transform.rotation,
                NornState = state,
                Genome = genome != null ? genome.Clone() : new GenomeData(),
                Chemicals = new ChemicalState(), // Use default since we can't access private fields
                CurrentLearningRate = 0f,
                LearningMemoryCount = 0
            };
        }
        
        private CameraData SerializeCameraState()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return new CameraData
                {
                    Position = Vector3.zero,
                    Rotation = Quaternion.identity,
                    FieldOfView = 60f,
                    OrthographicSize = 10f,
                    IsOrthographic = false
                };
            }
            
            return new CameraData
            {
                Position = mainCamera.transform.position,
                Rotation = mainCamera.transform.rotation,
                FieldOfView = mainCamera.fieldOfView,
                OrthographicSize = mainCamera.orthographicSize,
                IsOrthographic = mainCamera.orthographic
            };
        }
        
        #endregion

        #region Load
        
        /// <summary>
        /// Loads a world from a save file.
        /// </summary>
        /// <param name="filename">Name of the save file (without extension)</param>
        public void LoadWorld(string filename)
        {
            StartCoroutine(LoadWorldRoutine(filename));
        }
        
        private IEnumerator LoadWorldRoutine(string filename)
        {
            bool success = false;
            string filepath = GetSaveFilePath(filename);
            
            if (!File.Exists(filepath))
            {
                Debug.LogError($"[SaveManager] Save file not found: {filepath}");
                OnLoadCompleted?.Invoke(false);
                yield break;
            }
            
            yield return null;
            
            try
            {
                string json = File.ReadAllText(filepath);
                WorldData worldData = JsonUtility.FromJson<WorldData>(json);
                
                if (worldData == null)
                {
                    Debug.LogError("[SaveManager] Failed to deserialize save file");
                    OnLoadCompleted?.Invoke(false);
                    yield break;
                }
                
                if (!worldData.IsVersionCompatible())
                {
                    Debug.LogError($"[SaveManager] Save file version {worldData.Version} is incompatible");
                    OnLoadCompleted?.Invoke(false);
                    yield break;
                }
                
                // Apply loaded data
                yield return StartCoroutine(ApplyWorldDataRoutine(worldData));
                
                success = true;
                Debug.Log($"[SaveManager] World loaded successfully from {filepath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load world: {ex.Message}");
            }
            
            OnLoadCompleted?.Invoke(success);
        }
        
        private IEnumerator ApplyWorldDataRoutine(WorldData worldData)
        {
            // Clear existing creatures
            var existingCreatures = FindObjectsOfType<Norn>();
            foreach (var creature in existingCreatures)
            {
                if (creature != null && creature.gameObject != null)
                {
                    Destroy(creature.gameObject);
                }
            }
            
            yield return null;
            
            // Restore session data
            _sessionPlayTime = worldData.TotalPlayTime;
            Time.timeScale = worldData.GameSpeed;
            
            // Restore creatures
            foreach (var creatureData in worldData.Creatures)
            {
                if (creatureData == null) continue;
                
                SpawnCreature(creatureData);
                yield return null;
            }
            
            // Restore camera state
            if (Camera.main != null && worldData.CameraState != null)
            {
                Camera.main.transform.position = worldData.CameraState.Position;
                Camera.main.transform.rotation = worldData.CameraState.Rotation;
                Camera.main.fieldOfView = worldData.CameraState.FieldOfView;
                Camera.main.orthographicSize = worldData.CameraState.OrthographicSize;
                Camera.main.orthographic = worldData.CameraState.IsOrthographic;
            }
            
            yield return null;
        }
        
        private void SpawnCreature(CreatureData data)
        {
            Debug.Log($"[SaveManager] Would spawn creature at {data.Position} with genome {data.Genome?.Genes?.Length ?? 0} genes");
            // TODO: Implement creature spawning when prefab system is ready
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Gets the full path for a save file.
        /// </summary>
        public string GetSaveFilePath(string filename)
        {
            string sanitized = string.Concat(filename
                .Split(Path.GetInvalidFileNameChars())
                .Where(c => !char.IsWhiteSpace(c))
                .Select(c => c.ToString()));
            
            return Path.Combine(_saveDirectory, sanitized + saveFileExtension);
        }
        
        /// <summary>
        /// Gets a list of all available save files.
        /// </summary>
        public List<SaveFileInfo> GetSaveFiles()
        {
            var saves = new List<SaveFileInfo>();
            
            try
            {
                var files = Directory.GetFiles(_saveDirectory, "*" + saveFileExtension);
                
                foreach (var filepath in files)
                {
                    try
                    {
                        string json = File.ReadAllText(filepath);
                        WorldData data = JsonUtility.FromJson<WorldData>(json);
                        
                        if (data != null)
                        {
                            saves.Add(new SaveFileInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(filepath),
                                FilePath = filepath,
                                LastModified = File.GetLastWriteTime(filepath),
                                WorldData = data,
                                FileSize = new FileInfo(filepath).Length
                            });
                        }
                    }
                    catch { /* Skip corrupted saves */ }
                }
                
                saves.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to list save files: {ex.Message}");
            }
            
            return saves;
        }
        
        /// <summary>
        /// Deletes a save file.
        /// </summary>
        public bool DeleteSave(string filename)
        {
            try
            {
                string filepath = GetSaveFilePath(filename);
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    Debug.Log($"[SaveManager] Deleted save file: {filepath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to delete save: {ex.Message}");
            }
            return false;
        }
        
        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        public bool SaveExists(string filename)
        {
            return File.Exists(GetSaveFilePath(filename));
        }
        
        #endregion
    }
    
    /// <summary>
    /// Information about a save file.
    /// </summary>
    [Serializable]
    public class SaveFileInfo
    {
        public string Name;
        public string FilePath;
        public DateTime LastModified;
        public WorldData WorldData;
        public long FileSize;
        
        public string DisplayName => $"{Name} - {LastModified:yyyy-MM-dd HH:mm}";
    }
}
