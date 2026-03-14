using System;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Albia.Core
{
    /// <summary>
    /// Test component for validating SaveManager functionality.
    /// Attach to a GameObject in the scene to run tests.
    /// </summary>
    public class SaveManagerTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private int testSeed = 12345;
        [SerializeField] private int mapWidth = 128;
        [SerializeField] private int mapHeight = 128;
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool runStressTest = false;
        
        [Header("UI References")]
        [SerializeField] private bool showDebugGUI = true;
        
        private SaveManager _saveManager;
        private float[,] _testHeightmap;
        private string _lastTestResult = "Ready";
        private float _lastTestDuration = 0f;
        private float _currentProgress = 0f;
        private float[,] _loadedHeightmap;
        private bool _isRunning = false;
        private int _testsPassed = 0;
        private int _testsFailed = 0;
        
        private void Start()
        {
            _saveManager = FindObjectOfType<SaveManager>();
            if (_saveManager == null)
            {
                gameObject.AddComponent<SaveManager>();
                _saveManager = GetComponent<SaveManager>();
            }
            
            // Subscribe to events
            _saveManager.OnSaveProgress += (progress) => _currentProgress = progress;
            _saveManager.OnSaveComplete += (success, message) => 
            {
                Debug.Log($"[SaveManagerTester] Save complete: {success} - {message}");
            };
            _saveManager.OnLoadComplete += (success, message, data) => 
            {
                Debug.Log($"[SaveManagerTester] Load complete: {success} - {message}");
            };
            
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 500), "SaveManager Tests", "box");
            
            GUILayout.Label($"Status: {_lastTestResult}");
            GUILayout.Label($"Progress: {_currentProgress:P0}");
            GUILayout.Label($"Tests: {_testsPassed} passed, {_testsFailed} failed");
            GUILayout.Space(10);
            
            if (_isRunning)
            {
                GUILayout.Label("Running tests...");
            }
            else
            {
                if (GUILayout.Button("Run All Tests"))
                {
                    _ = RunAllTests();
                }
                
                if (GUILayout.Button("Test Save Only"))
                {
                    _ = TestSaveHeightmap();
                }
                
                if (GUILayout.Button("Test Load Only"))
                {
                    _ = TestLoadHeightmap();
                }
                
                if (GUILayout.Button("Generate Random Heightmap"))
                {
                    GenerateRandomHeightmap();
                }
                
                if (runStressTest && GUILayout.Button("Stress Test (10 cycles)"))
                {
                    _ = RunStressTest();
                }
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"Test Dimensions: {mapWidth}x{mapHeight}");
            GUILayout.Label($"Seed: {testSeed}");
            
            if (_loadedHeightmap != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Loaded Heightmap Preview:");
                DrawHeightmapPreview(_loadedHeightmap, 200, 100);
            }
            
            GUILayout.EndArea();
        }
        
        private void DrawHeightmapPreview(float[,] heightmap, int width, int height)
        {
            // Simple color-based preview
            int mapW = heightmap.GetLength(0);
            int mapH = heightmap.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                float mapY = (y / (float)height) * mapH;
                for (int x = 0; x < width; x++)
                {
                    float mapX = (x / (float)width) * mapW;
                    float h = heightmap[(int)mapX, (int)mapY];
                    
                    // Map height to grayscale (assuming 0-50 range)
                    byte gray = (byte)Mathf.Clamp(h * 5, 0, 255);
                    GUI.color = new Color32(gray, gray, gray, 255);
                    GUI.DrawTexture(
                        new Rect(10 + x * 1.5f, 350 + y * 1.5f, 1.5f, 1.5f), 
                        Texture2D.whiteTexture
                    );
                }
            }
            GUI.color = Color.white;
        }
        
        private async Task RunAllTests()
        {
            _isRunning = true;
            _testsPassed = 0;
            _testsFailed = 0;
            _lastTestResult = "Running...";
            
            Debug.Log("=== SaveManager Test Suite ===");
            
            // Test 1: Generate and Save
            var success = await TestSaveHeightmap();
            if (success) _testsPassed++; else _testsFailed++;
            
            await Task.Delay(500); // Small delay between tests
            
            // Test 2: Load and Verify
            success = await TestLoadHeightmap();
            if (success) _testsPassed++; else _testsFailed++;
            
            await Task.Delay(500);
            
            // Test 3: Data Integrity
            success = TestDataIntegrity();
            if (success) _testsPassed++; else _testsFailed++;
            
            _isRunning = false;
            _lastTestResult = $"Complete - {_testsPassed}/{_testsPassed + _testsFailed} tests passed";
            
            Debug.Log($"=== Test Suite Complete: {_testsPassed} passed, {_testsFailed} failed ===");
        }
        
        private async Task<bool> TestSaveHeightmap()
        {
            Debug.Log("[SaveManagerTester] Starting SAVE test...");
            float startTime = Time.realtimeSinceStartup;
            
            try
            {
                // Generate test heightmap
                GenerateRandomHeightmap();
                
                // Save it
                Progress<float> progress = new Progress<float>(p => 
                {
                    _currentProgress = p;
                });
                
                bool success = await _saveManager.SaveHeightmapAsync(_testHeightmap, testSeed, progress);
                
                _lastTestDuration = (Time.realtimeSinceStartup - startTime) * 1000;
                
                if (success)
                {
                    Debug.Log($"[SaveManagerTester] SAVE TEST PASSED ({_lastTestDuration:F1}ms)");
                    return true;
                }
                else
                {
                    Debug.LogError("[SaveManagerTester] SAVE TEST FAILED");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManagerTester] SAVE TEST EXCEPTION: {ex}");
                return false;
            }
        }
        
        private async Task<bool> TestLoadHeightmap()
        {
            Debug.Log("[SaveManagerTester] Starting LOAD test...");
            float startTime = Time.realtimeSinceStartup;
            
            try
            {
                Progress<float> progress = new Progress<float>(p => 
                {
                    _currentProgress = p;
                });
                
                var data = await _saveManager.LoadHeightmapAsync(testSeed, progress);
                
                _lastTestDuration = (Time.realtimeSinceStartup - startTime) * 1000;
                
                if (data != null)
                {
                    _loadedHeightmap = data.HeightmapArray;
                    Debug.Log($"[SaveManagerTester] LOAD TEST PASSED ({_lastTestDuration:F1}ms)");
                    Debug.Log($"  - Dimensions: {data.Width}x{data.Height}");
                    Debug.Log($"  - Seed: {data.Seed}");
                    Debug.Log($"  - Saved at: {data.SavedAt}");
                    return true;
                }
                else
                {
                    Debug.LogError("[SaveManagerTester] LOAD TEST FAILED - No data returned");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManagerTester] LOAD TEST EXCEPTION: {ex}");
                return false;
            }
        }
        
        private bool TestDataIntegrity()
        {
            Debug.Log("[SaveManagerTester] Starting DATA INTEGRITY test...");
            
            if (_testHeightmap == null || _loadedHeightmap == null)
            {
                Debug.LogError("[SaveManagerTester] Cannot verify integrity - missing data");
                return false;
            }
            
            int width = _testHeightmap.GetLength(0);
            int height = _testHeightmap.GetLength(1);
            
            if (_loadedHeightmap.GetLength(0) != width || _loadedHeightmap.GetLength(1) != height)
            {
                Debug.LogError($"[SaveManagerTester] DIMENSION MISMATCH!");
                Debug.LogError($"  Original: {width}x{height}");
                Debug.LogError($"  Loaded: {_loadedHeightmap.GetLength(0)}x{_loadedHeightmap.GetLength(1)}");
                return false;
            }
            
            int differences = 0;
            float maxDifference = 0f;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float diff = Mathf.Abs(_testHeightmap[x, y] - _loadedHeightmap[x, y]);
                    if (diff > 0.0001f) // Allow for floating point precision
                    {
                        differences++;
                        maxDifference = Mathf.Max(maxDifference, diff);
                    }
                }
            }
            
            if (differences == 0)
            {
                Debug.Log("[SaveManagerTester] DATA INTEGRITY TEST PASSED - 100% match");
                return true;
            }
            else
            {
                float percentMatch = 100f * (1f - (float)differences / (width * height));
                Debug.LogWarning($"[SaveManagerTester] DATA INTEGRITY PARTIAL MATCH: {percentMatch:F2}%");
                Debug.LogWarning($"  Differences: {differences}, Max diff: {maxDifference:F6}");
                return percentMatch > 99.9f; // Pass if > 99.9% match
            }
        }
        
        private async Task RunStressTest()
        {
            Debug.Log("[SaveManagerTester] Starting STRESS TEST (10 cycles)...");
            _isRunning = true;
            
            int passed = 0;
            int failed = 0;
            
            for (int i = 0; i < 10; i++)
            {
                testSeed = Random.Range(1, 99999);
                Debug.Log($"[SaveManagerTester] Stress cycle {i + 1}/10 - Seed: {testSeed}");
                
                bool saveOk = await TestSaveHeightmap();
                bool loadOk = await TestLoadHeightmap();
                bool integrityOk = TestDataIntegrity();
                
                if (saveOk && loadOk && integrityOk) passed++;
                else failed++;
                
                await Task.Delay(100); // Brief pause between cycles
            }
            
            _isRunning = false;
            _lastTestResult = $"Stress Test: {passed}/10 passed, {failed}/10 failed";
            Debug.Log($"=== Stress Test Complete: {passed} passed, {failed} failed ===");
        }
        
        private void GenerateRandomHeightmap()
        {
            _testHeightmap = new float[mapWidth, mapHeight];
            Random.InitState(testSeed);
            
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // Generate terrain-like height using multiple octaves
                    float height = 0f;
                    float amplitude = 1f;
                    float frequency = 0.01f;
                    
                    for (int o = 0; o < 4; o++)
                    {
                        height += amplitude * Mathf.PerlinNoise(
                            x * frequency + testSeed,
                            y * frequency + testSeed
                        );
                        amplitude *= 0.5f;
                        frequency *= 2f;
                    }
                    
                    _testHeightmap[x, y] = height * 20f; // Scale to 0-20 range
                }
            }
            
            Debug.Log($"[SaveManagerTester] Generated heightmap: {mapWidth}x{mapHeight} with seed {testSeed}");
        }
        
        // Public methods for external testing
        public void RunTests() => _ = RunAllTests();
        public void TriggerSave() => _ = TestSaveHeightmap();
        public void TriggerLoad() => _ = TestLoadHeightmap();
        public float[,] GetTestHeightmap() => _testHeightmap;
        public float[,] GetLoadedHeightmap() => _loadedHeightmap;
    }
}