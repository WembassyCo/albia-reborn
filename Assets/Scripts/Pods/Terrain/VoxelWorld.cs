using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Albia.Core.Interfaces;
using Albia.Core.Shared;
using UnityEngine;

namespace Albia.Pods.Terrain
{
    /// <summary>
    /// Central manager for the voxel world. Handles chunk loading, LOD system,
    /// async mesh generation, and provides ITerrainQuery interface.
    /// </summary>
    public class VoxelWorld : MonoBehaviour, ITerrainQuery
    {
        [Header("World Settings")]
        public int worldWidth = 1024;
        public int worldDepth = 1024;
        public int worldHeight = 64;
        public int viewDistance = 8;
        
        [Header("LOD Settings")]
        public int[] lodDistances = new int[] { 2, 4, 8, 16 };
        public int[] lodSteps = new int[] { 0, 1, 2, 3 };
        
        [Header("Performance")]
        public int chunksPerFrame = 4;
        public int maxConcurrentMeshJobs = 4;
        public bool useThreadedGeneration = true;
        
        [Header("References")]
        public Material voxelMaterial;
        public Transform viewer;
        
        // Chunk storage
        private readonly ConcurrentDictionary<Vector3Int, Chunk> _chunks = new ConcurrentDictionary<Vector3Int, Chunk>();
        private readonly ConcurrentDictionary<Vector3Int, MeshData> _meshQueue = new ConcurrentDictionary<Vector3Int, MeshData>();
        
        // Async generation
        private readonly ConcurrentQueue<ChunkMeshJob> _generationQueue = new ConcurrentQueue<ChunkMeshJob>();
        private readonly List<Task> _activeTasks = new List<Task>();
        private CancellationTokenSource _cancellationTokenSource;
        
        // Tracking
        private Vector3Int _currentViewerChunk;
        private float _chunkSize;
        private bool _isInitialized = false;
        private int _totalVoxels;
        
        // Simplex noise for terrain (static seed for reproducibility)
        private static System.Random _noiseRandom = new System.Random(1337);
        
        // Events
        public event Action<Vector3Int, VoxelType> OnVoxelChanged;
        
        // Performance metrics
        public int ActiveChunkCount => _chunks.Count;
        public int ActiveMeshJobs => _activeTasks.Count;
        public int PendingMeshJobs => _generationQueue.Count;
        
        /// <summary>
        /// Single instance access
        /// </summary>
        public static VoxelWorld Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            _chunkSize = VoxelData.SIZE;
            _totalVoxels = worldWidth * worldDepth * worldHeight;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        private void Start()
        {
            InitializeWorld();
        }
        
        private void Update()
        {
            if (!_isInitialized)
                return;
            
            // Update viewer chunk position
            Vector3Int newViewerChunk = GetChunkCoordFromPosition(viewer.position);
            if (newViewerChunk != _currentViewerChunk)
            {
                _currentViewerChunk = newViewerChunk;
                UpdateChunkVisibility();
            }
            
            // Process mesh queue
            ProcessMeshQueue();
            
            // Handle async generation
            if (useThreadedGeneration)
            {
                ProcessGenerationQueue();
            }
        }
        
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            Instance = null;
        }
        
        /// <summary>
        /// Initializes the world and generates initial chunks
        /// </summary>
        public void InitializeWorld()
        {
            if (viewer == null)
            {
                viewer = Camera.main?.transform ?? transform;
            }
            
            _currentViewerChunk = GetChunkCoordFromPosition(viewer.position);
            
            // Generate initial chunks around viewer
            for (int y = 0; y < worldHeight / VoxelData.SIZE; y++)
            {
                for (int z = -viewDistance; z <= viewDistance; z++)
                {
                    for (int x = -viewDistance; x <= viewDistance; x++)
                    {
                        Vector3Int chunkCoord = new Vector3Int(
                            _currentViewerChunk.x + x,
                            y,
                            _currentViewerChunk.z + z
                        );
                        
                        LoadChunk(chunkCoord);
                    }
                }
            }
            
            // Link neighbors
            UpdateChunkNeighbors();
            
            // Generate meshes for visible chunks
            GenerateInitialMeshes();
            
            _isInitialized = true;
            Debug.Log($"VoxelWorld initialized: {_chunks.Count} chunks, {_totalVoxels} voxels capacity");
        }
        
        /// <summary>
        /// Gets or creates a chunk at the specified chunk coordinate
        /// </summary>
        public Chunk LoadChunk(Vector3Int chunkCoord)
        {
            if (_chunks.TryGetValue(chunkCoord, out Chunk existingChunk))
                return existingChunk;
            
            // Create new chunk
            Chunk chunk = new Chunk(chunkCoord);
            chunk.OnVoxelChanged += (c, pos) => 
            {
                Vector3Int worldPos = c.LocalToWorld(pos);
                OnVoxelChanged?.Invoke(worldPos, c.GetVoxel(pos));
            };
            
            // Generate terrain data
            GenerateChunkTerrain(chunk);
            
            _chunks[chunkCoord] = chunk;
            
            return chunk;
        }
        
        /// <summary>
        /// Generates terrain data for a chunk using simple noise
        /// </summary>
        private void GenerateChunkTerrain(Chunk chunk)
        {
            Vector3Int worldPos = chunk.ChunkPosition * VoxelData.SIZE;
            
            for (int y = 0; y < VoxelData.SIZE; y++)
            {
                for (int z = 0; z < VoxelData.SIZE; z++)
                {
                    for (int x = 0; x < VoxelData.SIZE; x++)
                    {
                        float worldX = worldPos.x + x;
                        float worldY = worldPos.y + y;
                        float worldZ = worldPos.z + z;
                        
                        // Simple terrain generation
                        float height = GetHeightAt(new Vector2Int((int)worldX, (int)worldZ));
                        float density = worldY < height ? 1f : -1f;
                        
                        // Add noise for caves/variety
                        float noise = PerlinNoise(worldX * 0.1f, worldY * 0.1f, worldZ * 0.1f);
                        if (worldY < height - 5)
                            density += noise * 0.3f;
                        
                        chunk.SetDensity(x, y, z, density);
                    }
                }
            }
            
            // Finalize with voxel types
            chunk.FinalizeGeneration((density, x, y, z) => 
            {
                Vector3Int pos = chunk.LocalToWorld(new Vector3Int(x, y, z));
                return GetVoxelTypeFromDensity(density, pos.y);
            });
        }
        
        /// <summary>
        /// Simple Perlin-like noise function
        /// </summary>
        private static float PerlinNoise(float x, float y, float z)
        {
            return Mathf.PerlinNoise(x + y, z + x) * 2f - 1f;
        }
        
        /// <summary>
        /// Updates which chunks are visible based on viewer position
        /// </summary>
        private void UpdateChunkVisibility()
        {
            var chunksToUnload = new List<Vector3Int>();
            var chunksToLoad = new List<Vector3Int>();
            
            // Mark chunks outside view distance for unloading
            foreach (var kvp in _chunks)
            {
                int distX = Mathf.Abs(kvp.Key.x - _currentViewerChunk.x);
                int distZ = Mathf.Abs(kvp.Key.z - _currentViewerChunk.z);
                
                if (distX > viewDistance * 2 || distZ > viewDistance * 2)
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            // Schedule new chunks to load
            for (int y = 0; y < worldHeight / VoxelData.SIZE; y++)
            {
                for (int z = -viewDistance; z <= viewDistance; z++)
                {
                    for (int x = -viewDistance; x <= viewDistance; x++)
                    {
                        Vector3Int chunkCoord = new Vector3Int(
                            _currentViewerChunk.x + x,
                            y,
                            _currentViewerChunk.z + z
                        );
                        
                        if (!_chunks.ContainsKey(chunkCoord))
                        {
                            chunksToLoad.Add(chunkCoord);
                        }
                    }
                }
            }
            
            // Unload distant chunks
            foreach (var coord in chunksToUnload)
            {
                if (_chunks.TryRemove(coord, out Chunk chunk))
                {
                    // Notify chunk disposal
                    Debug.Log($"Unloaded chunk {coord}");
                }
            }
            
            // Load new chunks
            foreach (var coord in chunksToLoad)
            {
                LoadChunk(coord);
            }
            
            // Update neighbor links
            UpdateChunkNeighbors();
        }
        
        /// <summary>
        /// Links chunks to their neighbors for edge queries
        /// </summary>
        private void UpdateChunkNeighbors()
        {
            foreach (var kvp in _chunks)
            {
                var chunk = kvp.Value;
                var coord = kvp.Key;
                
                // Check 6 neighbors
                SetNeighborIfExists(chunk, coord, new Vector3Int(-1, 0, 0));
                SetNeighborIfExists(chunk, coord, new Vector3Int(1, 0, 0));
                SetNeighborIfExists(chunk, coord, new Vector3Int(0, -1, 0));
                SetNeighborIfExists(chunk, coord, new Vector3Int(0, 1, 0));
                SetNeighborIfExists(chunk, coord, new Vector3Int(0, 0, -1));
                SetNeighborIfExists(chunk, coord, new Vector3Int(0, 0, 1));
            }
        }
        
        private void SetNeighborIfExists(Chunk chunk, Vector3Int coord, Vector3Int direction)
        {
            Vector3Int neighborCoord = coord + direction;
            if (_chunks.TryGetValue(neighborCoord, out Chunk neighbor))
            {
                chunk.SetNeighbor(direction, neighbor);
            }
        }
        
        /// <summary>
        /// Schedule mesh generation for initial visible chunks
        /// </summary>
        private void GenerateInitialMeshes()
        {
            foreach (var kvp in _chunks)
            {
                var chunk = kvp.Value;
                int lod = CalculateLOD(kvp.Key);
                
                if (useThreadedGeneration)
                {
                    _generationQueue.Enqueue(new ChunkMeshJob
                    {
                        Chunk = chunk,
                        LOD = lod
                    });
                }
                else
                {
                    GenerateMeshImmediate(chunk, lod);
                }
            }
        }
        
        /// <summary>
        /// Processes the async mesh generation queue
        /// </summary>
        private void ProcessGenerationQueue()
        {
            // Clear completed tasks
            _activeTasks.RemoveAll(t => t.IsCompleted);
            
            // Start new tasks up to max concurrent
            while (_activeTasks.Count < maxConcurrentMeshJobs && _generationQueue.TryDequeue(out ChunkMeshJob job))
            {
                var task = Task.Run(() => GenerateMeshAsync(job), _cancellationTokenSource.Token);
                _activeTasks.Add(task);
            }
        }
        
        /// <summary>
        /// Generates mesh data on a background thread
        /// </summary>
        private void GenerateMeshAsync(ChunkMeshJob job)
        {
            try
            {
                float[] densities = job.Chunk.GetDensityArray();
                MeshData meshData = MarchingCubes.GenerateMesh(densities, job.LOD);
                
                if (!meshData.IsEmpty)
                {
                    _meshQueue[job.Chunk.ChunkPosition] = meshData;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Mesh generation failed for chunk {job.Chunk.ChunkPosition}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Generates mesh immediately on main thread
        /// </summary>
        private void GenerateMeshImmediate(Chunk chunk, int lod)
        {
            float[] densities = chunk.GetDensityArray();
            MeshData meshData = MarchingCubes.GenerateMesh(densities, lod);
            
            if (!meshData.IsEmpty)
            {
                _meshQueue[chunk.ChunkPosition] = meshData;
            }
        }
        
        /// <summary>
        /// Applies queued meshes to world
        /// </summary>
        private void ProcessMeshQueue()
        {
            int processed = 0;
            while (processed < chunksPerFrame && _meshQueue.Count > 0)
            {
                // Find a mesh to process
                foreach (var kvp in _meshQueue)
                {
                    if (_meshQueue.TryRemove(kvp.Key, out MeshData meshData))
                    {
                        // Create or update mesh renderer
                        CreateMeshRenderer(kvp.Key, meshData);
                        processed++;
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates or updates a mesh renderer for a chunk
        /// </summary>
        private void CreateMeshRenderer(Vector3Int chunkCoord, MeshData meshData)
        {
            string chunkName = $"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";
            GameObject chunkObj = GameObject.Find(chunkName);
            
            if (chunkObj == null)
            {
                chunkObj = new GameObject(chunkName);
                chunkObj.transform.SetParent(transform);
                chunkObj.transform.position = new Vector3(
                    chunkCoord.x * VoxelData.SIZE,
                    chunkCoord.y * VoxelData.SIZE,
                    chunkCoord.z * VoxelData.SIZE
                );
                
                chunkObj.AddComponent<MeshFilter>();
                chunkObj.AddComponent<MeshRenderer>();
                chunkObj.AddComponent<MeshCollider>();
            }
            
            var meshFilter = chunkObj.GetComponent<MeshFilter>();
            var meshRenderer = chunkObj.GetComponent<MeshRenderer>();
            var meshCollider = chunkObj.GetComponent<MeshCollider>();
            
            // Apply mesh data
            if (meshFilter.mesh == null)
            {
                meshFilter.mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            }
            
            meshData.ApplyTo(meshFilter.mesh);
            
            // Setup material
            if (voxelMaterial != null)
            {
                meshRenderer.material = voxelMaterial;
            }
            
            // Update collider
            meshCollider.sharedMesh = meshFilter.mesh;
            
            Debug.Log($"Mesh created for chunk {chunkCoord}: {meshData.Vertices.Count} vertices");
        }
        
        /// <summary>
        /// Calculates LOD level based on distance from viewer
        /// </summary>
        private int CalculateLOD(Vector3Int chunkCoord)
        {
            int distance = Mathf.Max(
                Mathf.Abs(chunkCoord.x - _currentViewerChunk.x),
                Mathf.Abs(chunkCoord.z - _currentViewerChunk.z)
            );
            
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance <= lodDistances[i])
                    return lodSteps[i];
            }
            
            return lodSteps[lodSteps.Length - 1];
        }
        
        /// <summary>
        /// Converts world position to chunk coordinate
        /// </summary>
        private Vector3Int GetChunkCoordFromPosition(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / _chunkSize),
                Mathf.FloorToInt(position.y / _chunkSize),
                Mathf.FloorToInt(position.z / _chunkSize)
            );
        }
        
        /// <summary>
        /// Gets voxel type based on density and height
        /// </summary>
        private VoxelType GetVoxelTypeFromDensity(float density, int worldHeight)
        {
            if (density <= 0)
                return VoxelType.Air;
            
            if (worldHeight > 30)
                return VoxelType.Snow;
            if (worldHeight > 20)
                return VoxelType.Stone;
            if (worldHeight > 10)
                return VoxelType.Dirt;
            
            return VoxelType.Sand;
        }
        
        // ============================================
        // ITerrainQuery Implementation
        // ============================================
        
        VoxelType ITerrainQuery.GetVoxel(Vector3Int pos)
        {
            Vector3Int chunkCoord = new Vector3Int(
                Mathf.FloorToInt(pos.x / (float)VoxelData.SIZE),
                Mathf.FloorToInt(pos.y / (float)VoxelData.SIZE),
                Mathf.FloorToInt(pos.z / (float)VoxelData.SIZE)
            );
            
            if (_chunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                return chunk.GetVoxelWithNeighbors(
                    pos.x - chunkCoord.x * VoxelData.SIZE,
                    pos.y - chunkCoord.y * VoxelData.SIZE,
                    pos.z - chunkCoord.z * VoxelData.SIZE
                );
            }
            
            return VoxelType.Air;
        }
        
        void ITerrainQuery.SetVoxel(Vector3Int pos, VoxelType type, ChangeSource source)
        {
            Vector3Int chunkCoord = new Vector3Int(
                Mathf.FloorToInt(pos.x / (float)VoxelData.SIZE),
                Mathf.FloorToInt(pos.y / (float)VoxelData.SIZE),
                Mathf.FloorToInt(pos.z / (float)VoxelData.SIZE)
            );
            
            if (_chunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                Vector3Int localPos = new Vector3Int(
                    pos.x - chunkCoord.x * VoxelData.SIZE,
                    pos.y - chunkCoord.y * VoxelData.SIZE,
                    pos.z - chunkCoord.z * VoxelData.SIZE
                );
                
                chunk.SetVoxel(localPos, type);
                
                // Schedule mesh rebuild
                _generationQueue.Enqueue(new ChunkMeshJob
                {
                    Chunk = chunk,
                    LOD = CalculateLOD(chunkCoord)
                });
            }
        }
        
        float ITerrainQuery.GetHeight(Vector2Int tilePos)
        {
            // Use Perlin-like noise for height generation
            return 20f + PerlinNoise(tilePos.x * 0.01f, 0, tilePos.y * 0.01f) * 10f;
        }
        
        Biome ITerrainQuery.GetBiome(Vector2Int tilePos)
        {
            float height = GetHeightAt(tilePos);
            float moisture = PerlinNoise(tilePos.x * 0.005f + 100, 0, tilePos.y * 0.005f);
            
            if (height > 35)
                return Biome.Mountain;
            if (height > 25)
                return moisture > 0 ? Biome.Forest : Biome.Plains;
            if (height > 15)
                return moisture > 0 ? Biome.Swamp : Biome.Desert;
            
            return moisture > 0.5f ? Biome.Ocean : Biome.Beach;
        }
        
        /// <summary>
        /// Helper to get height
        /// </summary>
        private float GetHeightAt(Vector2Int tilePos)
        {
            return 20f + PerlinNoise(tilePos.x * 0.01f, 0, tilePos.y * 0.01f) * 10f;
        }
        
        // Internal structure for mesh generation jobs
        private struct ChunkMeshJob
        {
            public Chunk Chunk;
            public int LOD;
        }
    }
}