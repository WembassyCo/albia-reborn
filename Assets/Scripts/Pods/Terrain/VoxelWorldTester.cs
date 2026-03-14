using UnityEngine;

namespace Albia.Pods.Terrain
{
    /// <summary>
    /// Test script for voxel world initialization.
    /// Attach this to a GameObject in the scene to test the voxel system.
    /// </summary>
    public class VoxelWorldTester : MonoBehaviour
    {
        [Header("Test Settings")]
        public int testChunksX = 4;
        public int testChunksY = 2;
        public int testChunksZ = 4;
        public bool generateOnStart = true;
        public bool logPerformance = true;
        
        [Header("Material")]
        public Material voxelMaterial;
        
        private VoxelWorld _voxelWorld;
        private float _startTime;
        
        void Start()
        {
            if (generateOnStart)
            {
                GenerateTestWorld();
            }
        }
        
        /// <summary>
        /// Generates a test world with a few chunks
        /// </summary>
        [ContextMenu("Generate Test World")]
        public void GenerateTestWorld()
        {
            _startTime = Time.realtimeSinceStartup;
            
            // Create VoxelWorld GameObject
            GameObject worldObj = new GameObject("VoxelWorld");
            _voxelWorld = worldObj.AddComponent<VoxelWorld>();
            _voxelWorld.viewDistance = Mathf.Max(testChunksX, testChunksZ) / 2;
            _voxelWorld.voxelMaterial = voxelMaterial;
            _voxelWorld.worldHeight = testChunksY * VoxelData.SIZE;
            _voxelWorld.useThreadedGeneration = false; // Sync for testing
            
            // Set viewer to this camera
            _voxelWorld.viewer = Camera.main?.transform ?? transform;
            
            // Initialize
            _voxelWorld.InitializeWorld();
            
            if (logPerformance)
            {
                float elapsed = Time.realtimeSinceStartup - _startTime;
                Debug.Log($"Test world generated in {elapsed:F3}s");
                
                // Log chunk stats
                int chunkCount = _voxelWorld.ActiveChunkCount;
                int totalVoxels = chunkCount * VoxelData.SIZE * VoxelData.SIZE * VoxelData.SIZE;
                Debug.Log($"Chunks: {chunkCount}, Total voxels: {totalVoxels:N0}");
            }
        }
        
        /// <summary>
        /// Tests voxel modification
        /// </summary>
        [ContextMenu("Test Voxel Modification")]
        public void TestVoxelModification()
        {
            if (_voxelWorld == null)
            {
                Debug.LogError("VoxelWorld not initialized!");
                return;
            }
            
            // Modify a sphere of voxels
            int centerX = VoxelData.SIZE * 2;
            int centerY = VoxelData.SIZE;
            int centerZ = VoxelData.SIZE * 2;
            int radius = 5;
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        if (x*x + y*y + z*z <= radius*radius)
                        {
                            Vector3Int pos = new Vector3Int(centerX + x, centerY + y, centerZ + z);
                            (_voxelWorld as Core.Interfaces.ITerrainQuery).SetVoxel(
                                pos, 
                                Core.Shared.VoxelType.Stone, 
                                Core.Interfaces.ChangeSource.Script
                            );
                        }
                    }
                }
            }
            
            Debug.Log($"Modified sphere of voxels at ({centerX}, {centerY}, {centerZ})");
        }
        
        /// <summary>
        /// Clears the test world
        /// </summary>
        [ContextMenu("Clear Test World")]
        public void ClearTestWorld()
        {
            if (_voxelWorld != null)
            {
                Destroy(_voxelWorld.gameObject);
                _voxelWorld = null;
            }
            
            // Clean up any remaining chunk objects
            var chunkObjects = GameObject.FindGameObjectsWithTag("Chunk");
            foreach (var obj in chunkObjects)
            {
                Destroy(obj);
            }
        }
        
        void OnGUI()
        {
            if (!logPerformance || _voxelWorld == null)
                return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("=== Voxel World Stats ===");
            GUILayout.Label($"Active Chunks: {_voxelWorld.ActiveChunkCount}");
            GUILayout.Label($"Active Mesh Jobs: {_voxelWorld.ActiveMeshJobs}");
            GUILayout.Label($"Pending Mesh Jobs: {_voxelWorld.PendingMeshJobs}");
            
            if (GUILayout.Button("Regenerate"))
            {
                ClearTestWorld();
                GenerateTestWorld();
            }
            
            if (GUILayout.Button("Test Modification"))
            {
                TestVoxelModification();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}