using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AlbiaReborn.Core
{
    /// <summary>
    /// Manages saving and loading of heightmap data.
    /// Provides async operations to avoid blocking the main thread.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [SerializeField]
        private string saveDirectory = "~/Projects/AlbiaReborn/Saves";
        
        private string _resolvedSavePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private CancellationTokenSource _saveCts;
        
        // Events for save/load progress
        public event Action<float> OnSaveProgress;
        public event Action<bool, string> OnSaveComplete;
        public event Action<bool, string, HeightmapData> OnLoadComplete;
        
        private void Awake()
        {
            // Resolve save directory (handle ~ expansion)
            _resolvedSavePath = ResolveSavePath(saveDirectory);
            
            // Ensure save directory exists
            if (!Directory.Exists(_resolvedSavePath))
            {
                Directory.CreateDirectory(_resolvedSavePath);
                Debug.Log($"[SaveManager] Created save directory: {_resolvedSavePath}");
            }
            
            // Configure JSON options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            Debug.Log($"[SaveManager] Initialized with save path: {_resolvedSavePath}");
        }
        
        private void OnDestroy()
        {
            _saveCts?.Cancel();
            _saveCts?.Dispose();
        }
        
        /// <summary>
        /// Resolves the save path, expanding ~ to home directory
        /// </summary>
        private string ResolveSavePath(string path)
        {
            if (path.StartsWith("~/"))
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = Path.Combine(home, path.Substring(2));
            }
            return Path.GetFullPath(path);
        }
        
        /// <summary>
        /// Generates a filename based on timestamp and seed
        /// </summary>
        private string GenerateFilename(int seed)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{timestamp}_{seed}.json";
        }
        
        /// <summary>
        /// Parses filename to extract timestamp and seed
        /// </summary>
        private bool TryParseFilename(string filename, out DateTime timestamp, out int seed)
        {
            timestamp = DateTime.MinValue;
            seed = 0;
            
            try
            {
                // Remove extension
                string name = Path.GetFileNameWithoutExtension(filename);
                string[] parts = name.Split('_');
                
                if (parts.Length != 2) return false;
                
                // Parse timestamp from format yyyyMMdd_HHmmss
                if (parts[0].Length == 8 && parts[1].Length == 6)
                {
                    string ts = parts[0] + "_" + parts[1];
                    timestamp = DateTime.ParseExact(ts, "yyyyMMdd_HHmmss", null);
                    seed = int.Parse(parts[1]); // Actually seed is the second part after underscore
                }
                else
                {
                    return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Saves a heightmap to JSON file asynchronously
        /// </summary>
        /// <param name="heightmap">2D float array representing terrain height</param>
        /// <param name="seed">World generation seed</param>
        /// <param name="progressCallback">Optional progress callback (0-1)</param>
        public async Task<bool> SaveHeightmapAsync(float[,] heightmap, int seed, IProgress<float> progress = null)
        {
            if (heightmap == null)
            {
                Debug.LogError("[SaveManager] Cannot save null heightmap");
                return false;
            }
            
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;
            
            string filename = GenerateFilename(seed);
            string filepath = Path.Combine(_resolvedSavePath, filename);
            
            float startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Report start
                progress?.Report(0.1f);
                OnSaveProgress?.Invoke(0.1f);
                
                // Convert 2D array to serializable format on background thread
                HeightmapData data = await Task.Run(() => 
                {
                    token.ThrowIfCancellationRequested();
                    return ConvertToSerializable(heightmap, seed);
                }, token);
                
                progress?.Report(0.4f);
                OnSaveProgress?.Invoke(0.4f);
                
                // Serialize to JSON on background thread
                string json = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    return JsonSerializer.Serialize(data, _jsonOptions);
                }, token);
                
                progress?.Report(0.7f);
                OnSaveProgress?.Invoke(0.7f);
                
                // Write to file
                await File.WriteAllTextAsync(filepath, json, token);
                
                progress?.Report(1.0f);
                
                float duration = (Time.realtimeSinceStartup - startTime) * 1000;
                string message = $"Saved heightmap to {filename} ({json.Length} bytes, {duration:F1}ms)";
                Debug.Log($"[SaveManager] {message}");
                
                OnSaveComplete?.Invoke(true, message);
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SaveManager] Save operation cancelled");
                OnSaveComplete?.Invoke(false, "Save cancelled");
                return false;
            }
            catch (Exception ex)
            {
                string error = $"Failed to save heightmap: {ex.Message}";
                Debug.LogError($"[SaveManager] {error}");
                OnSaveComplete?.Invoke(false, error);
                return false;
            }
        }
        
        /// <summary>
        /// Unity-compatible save method that can be called from UI/buttons
        /// </summary>
        public void SaveHeightmap(float[,] heightmap, int seed)
        {
            _ = SaveHeightmapAsync(heightmap, seed);
        }
        
        /// <summary>
        /// Loads a heightmap from JSON file asynchronously
        /// </summary>
        /// <param name="seed">World seed to identify the save</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns>Heightmap data or null on failure</returns>
        public async Task<HeightmapData> LoadHeightmapAsync(int seed, IProgress<float> progress = null)
        {
            // Find the most recent save with matching seed
            string filename = await Task.Run(() => FindMostRecentSave(seed));
            
            if (string.IsNullOrEmpty(filename))
            {
                string msg = $"No save found for seed {seed}";
                Debug.LogWarning($"[SaveManager] {msg}");
                OnLoadComplete?.Invoke(false, msg, null);
                return null;
            }
            
            return await LoadFromFileAsync(filename, progress);
        }
        
        /// <summary>
        /// Load heightmap by specific filename
        /// </summary>
        public async Task<HeightmapData> LoadFromFileAsync(string filename, IProgress<float> progress = null)
        {
            string filepath = Path.Combine(_resolvedSavePath, filename);
            
            if (!File.Exists(filepath))
            {
                string msg = $"Save file not found: {filepath}";
                Debug.LogError($"[SaveManager] {msg}");
                OnLoadComplete?.Invoke(false, msg, null);
                return null;
            }
            
            float startTime = Time.realtimeSinceStartup;
            
            try
            {
                progress?.Report(0.2f);
                
                // Read file asynchronously
                string json = await File.ReadAllTextAsync(filepath);
                
                progress?.Report(0.5f);
                
                // Deserialize on background thread
                HeightmapData data = await Task.Run(() =>
                {
                    return JsonSerializer.Deserialize<HeightmapData>(json, _jsonOptions);
                });
                
                progress?.Report(0.8f);
                
                if (data?.Heights == null)
                {
                    string msg = "Invalid save data structure";
                    Debug.LogError($"[SaveManager] {msg}");
                    OnLoadComplete?.Invoke(false, msg, null);
                    return null;
                }
                
                // Restore 2D array
                data.HeightmapArray = ConvertFromSerializable(data);
                
                progress?.Report(1.0f);
                
                float duration = (Time.realtimeSinceStartup - startTime) * 1000;
                string message = $"Loaded heightmap from {filename} ({data.Width}x{data.Height}, {duration:F1}ms)";
                Debug.Log($"[SaveManager] {message}");
                
                OnLoadComplete?.Invoke(true, message, data);
                return data;
            }
            catch (Exception ex)
            {
                string error = $"Failed to load heightmap: {ex.Message}";
                Debug.LogError($"[SaveManager] {error}");
                OnLoadComplete?.Invoke(false, error, null);
                return null;
            }
        }
        
        /// <summary>
        /// Unity-compatible load method
        /// </summary>
        public void LoadHeightmap(int seed)
        {
            _ = LoadHeightmapAsync(seed);
        }
        
        /// <summary>
        /// Finds the most recent save file for a given seed
        /// </summary>
        private string FindMostRecentSave(int seed)
        {
            if (!Directory.Exists(_resolvedSavePath))
                return null;
            
            string[] files = Directory.GetFiles(_resolvedSavePath, "*.json");
            string mostRecent = null;
            DateTime mostRecentTime = DateTime.MinValue;
            
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                if (TryParseFilename(filename, out DateTime timestamp, out int fileSeed))
                {
                    // Parse seed from filename - format is {timestamp}_{seed}.json
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
                    string[] parts = nameWithoutExt.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int parsedSeed))
                    {
                        if (parsedSeed == seed && timestamp > mostRecentTime)
                        {
                            mostRecentTime = timestamp;
                            mostRecent = filename;
                        }
                    }
                }
            }
            
            return mostRecent;
        }
        
        /// <summary>
        /// Converts 2D float array to serializable format
        /// </summary>
        private HeightmapData ConvertToSerializable(float[,] heightmap, int seed)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // Flatten to 1D array for JSON serialization
            float[] flat = new float[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    flat[x * height + y] = heightmap[x, y];
                }
            }
            
            return new HeightmapData
            {
                Seed = seed,
                Width = width,
                Height = height,
                Heights = flat,
                SavedAt = DateTime.UtcNow,
                Version = 1
            };
        }
        
        /// <summary>
        /// Converts serializable format back to 2D float array
        /// </summary>
        private float[,] ConvertFromSerializable(HeightmapData data)
        {
            float[,] heightmap = new float[data.Width, data.Height];
            
            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    heightmap[x, y] = data.Heights[x * data.Height + y];
                }
            }
            
            return heightmap;
        }
        
        /// <summary>
        /// Lists all available save files
        /// </summary>
        public string[] ListSaves()
        {
            if (!Directory.Exists(_resolvedSavePath))
                return new string[0];
            
            return Directory.GetFiles(_resolvedSavePath, "*.json")
                .Select(Path.GetFileName)
                .OrderByDescending(f => f)
                .ToArray();
        }
        
        /// <summary>
        /// Deletes a specific save file
        /// </summary>
        public bool DeleteSave(string filename)
        {
            try
            {
                string filepath = Path.Combine(_resolvedSavePath, filename);
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    Debug.Log($"[SaveManager] Deleted save: {filename}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to delete save: {ex.Message}");
            }
            return false;
        }
    }
    
    /// <summary>
    /// Serializable container for heightmap data
    /// </summary>
    [Serializable]
    public class HeightmapData
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }
        
        [JsonPropertyName("seed")]
        public int Seed { get; set; }
        
        [JsonPropertyName("width")]
        public int Width { get; set; }
        
        [JsonPropertyName("height")]
        public int Height { get; set; }
        
        [JsonPropertyName("heights")]
        public float[] Heights { get; set; }
        
        [JsonPropertyName("savedAt")]
        public DateTime SavedAt { get; set; }
        
        [JsonPropertyName("metadata")]
        public SaveMetadata Metadata { get; set; }
        
        // Non-serialized reference to loaded array
        [JsonIgnore]
        public float[,] HeightmapArray { get; set; }
    }
    
    [Serializable]
    public class SaveMetadata
    {
        [JsonPropertyName("worldName")]
        public string WorldName { get; set; }
        
        [JsonPropertyName("playTime")]
        public float PlayTime { get; set; }
        
        [JsonPropertyName("biome")]
        public string Biome { get; set; }
    }
}