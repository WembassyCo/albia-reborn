using System;
using System.Collections.Generic;
using Albia.Core.Interfaces;
using Albia.Core.Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Albia.Pods.Terrain
{
    /// <summary>
    /// Manages chunk loading, voxel modifications, and mesh generation.
    /// Provides the main API for voxel world interaction.
    /// </summary>
    public class ChunkManager : MonoBehaviour, IChunkManager
    {
        [Header("Chunk Settings")]
        [SerializeField] private int _maxChunksPerFrame = 2;
        [SerializeField] private int _maxModificationsPerFrame = 500;
        
        [Header("Mesh Generation")]
        [SerializeField] private Material _terrainMaterial;
        [SerializeField] private float _isoLevel = 0f;
        
        [Header("Water Flow")]
        [SerializeField] private float _waterFlowInterval = 0.5f;
        [SerializeField] private int _maxWaterUpdatesPerFrame = 100;

        private readonly Dictionary<Vector3Int, Chunk> _chunks = new();
        private readonly Queue<IVoxelModification> _modificationQueue = new();
        private readonly Queue<Vector3Int> _waterUpdateQueue = new();
        private readonly HashSet<Vector3Int> _waterSources = new();
        
        private SurfaceNetsMeshGenerator _meshGenerator;
        private readonly object _chunkLock = new();
        
        public event Action<Vector3Int, VoxelType, VoxelType> OnVoxelChanged;
        public event Action<IChunkData> OnChunkModified;
        
        private float _lastWaterFlowTime;
        private NavMeshSurface _navMeshSurface;
        private bool _navMeshNeedsRebuild;
        private float _navMeshRebuildDelay = 0.5f;
        private float _lastNavMeshRebuild;

        private void Awake()
        {
            _meshGenerator = new SurfaceNetsMeshGenerator(_isoLevel);
            _navMeshSurface = GetComponent<NavMeshSurface>() ?? gameObject.AddComponent<NavMeshSurface>();
            _navMeshSurface voxelSize = 0.1f;
            _navMeshSurface.buildHeightMesh = true;
        }

        private void Update()
        {
            ProcessTerrainGeneration();
            ProcessModifications(_maxModificationsPerFrame);
            ProcessWaterFlow();
            ProcessNavMeshUpdates();
        }

        #region IChunkManager Implementation

        public IChunkData GetChunk(Vector3Int worldPosition)
        {
            Vector3Int chunkCoord = WorldToChunkCoord(worldPosition);
            return GetOrCreateChunk(chunkCoord);
        }

        public IChunkData GetChunkByCoord(Vector3Int chunkCoord)
        {
            return GetOrCreateChunk(chunkCoord);
        }

        public bool SetVoxel(Vector3Int worldPos, VoxelType type)
        {
            Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
            Chunk chunk = GetOrCreateChunk(chunkCoord);
            
            if (chunk == null) return false;

            Vector3Int? localPos = chunk.WorldToLocal(worldPos);
            if (!localPos.HasValue) return false;

            VoxelType oldType = chunk.GetVoxel(localPos.Value);
            if (oldType == type) return true;

            // Perform the set
            chunk.SetVoxel(localPos.Value, type);
            
            // Handle water source tracking
            if (type == VoxelType.Water)
            {
                _waterSources.Add(worldPos);
                _waterUpdateQueue.Enqueue(worldPos);
            }
            else if (oldType == VoxelType.Water)
            {
                _waterSources.Remove(worldPos);
            }

            // Notify listeners
            OnVoxelChanged?.Invoke(worldPos, oldType, type);
            OnChunkModified?.Invoke(chunk);
            
            // Flag NavMesh for rebuild
            _navMeshNeedsRebuild = true;
            _lastNavMeshRebuild = Time.time;

            // Update visualization
            UpdateChunkVisual(chunk);

            return true;
        }

        public VoxelType GetVoxel(Vector3Int worldPos)
        {
            Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
            
            lock (_chunkLock)
            {
                if (!_chunks.TryGetValue(chunkCoord, out Chunk chunk))
                    return VoxelType.Air;

                Vector3Int? localPos = chunk.WorldToLocal(worldPos);
                if (!localPos.HasValue) return VoxelType.Air;

                return chunk.GetVoxel(localPos.Value);
            }
        }

        public void QueueModification(IVoxelModification modification)
        {
            lock (_modificationQueue)
            {
                _modificationQueue.Enqueue(modification);
            }
        }

        public void ProcessModifications(int maxCount = 100)
        {
            lock (_modificationQueue)
            {
                int count = 0;
                while (_modificationQueue.Count > 0 && count < maxCount)
                {
                    IVoxelModification mod = _modificationQueue.Dequeue();
                    mod.Apply(this);
                    count++;
                }
            }
        }

        public void UpdateMeshes()
        {
            lock (_chunkLock)
            {
                foreach (var chunk in _chunks.Values)
                {
                    if (chunk.IsDirty)
                    {
                        UpdateChunkVisual(chunk);
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private Chunk GetOrCreateChunk(Vector3Int chunkCoord)
        {
            lock (_chunkLock)
            {
                if (_chunks.TryGetValue(chunkCoord, out Chunk chunk))
                    return chunk;

                // Create new chunk
                chunk = new Chunk(chunkCoord);
                _chunks[chunkCoord] = chunk;

                // Set up neighbors
                LinkChunkNeighbors(chunk, chunkCoord);

                // Initialize with simple noise (placeholder for terrain generation)
                InitializeChunkDensities(chunk);

                return chunk;
            }
        }

        private void LinkChunkNeighbors(Chunk chunk, Vector3Int coord)
        {
            Vector3Int[] directions = {
                new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0),
                new Vector3Int(0, -1, 0), new Vector3Int(0, 1, 0),
                new Vector3Int(0, 0, -1), new Vector3Int(0, 0, 1)
            };

            foreach (var dir in directions)
            {
                Vector3Int neighborCoord = coord + dir;
                if (_chunks.TryGetValue(neighborCoord, out Chunk neighbor))
                {
                    chunk.SetNeighbor(dir, neighbor);
                    neighbor.SetNeighbor(new Vector3Int(-dir.x, -dir.y, -dir.z), chunk);
                }
            }
        }

        private void InitializeChunkDensities(Chunk chunk)
        {
            // Simple procedural generation using Perlin-like noise
            // In production, this should use proper noise functions
            for (int y = 0; y < Chunk.SIZE; y++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    for (int x = 0; x < Chunk.SIZE; x++)
                    {
                        Vector3Int worldPos = chunk.LocalToWorld(new Vector3Int(x, y, z));
                        float density = CalculateDensity(worldPos);
                        chunk.SetDensity(x, y, z, density);
                    }
                }
            }

            chunk.FinalizeGeneration();
        }

        private float CalculateDensity(Vector3Int worldPos)
        {
            // Simple height-based terrain
            float height = Mathf.PerlinNoise(worldPos.x * 0.03f, worldPos.z * 0.03f) * 20f;
            return worldPos.y < height ? 1f : -1f;
        }

        private Vector3Int WorldToChunkCoord(Vector3Int worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / (float)Chunk.SIZE),
                Mathf.FloorToInt(worldPos.y / (float)Chunk.SIZE),
                Mathf.FloorToInt(worldPos.z / (float)Chunk.SIZE)
            );
        }

        private void UpdateChunkVisual(Chunk chunk)
        {
            // Create or update chunk GameObject with mesh
            string chunkName = $"Chunk_{chunk.ChunkPosition.x}_{chunk.ChunkPosition.y}_{chunk.ChunkPosition.z}";
            Transform chunkTransform = transform.Find(chunkName);
            
            GameObject chunkObj;
            MeshFilter filter;
            MeshRenderer renderer;
            MeshCollider collider;

            if (chunkTransform == null)
            {
                chunkObj = new GameObject(chunkName);
                chunkObj.transform.parent = transform;
                chunkObj.transform.localPosition = new Vector3(
                    chunk.ChunkPosition.x * Chunk.SIZE,
                    chunk.ChunkPosition.y * Chunk.SIZE,
                    chunk.ChunkPosition.z * Chunk.SIZE
                );

                filter = chunkObj.AddComponent<MeshFilter>();
                renderer = chunkObj.AddComponent<MeshRenderer>();
                collider = chunkObj.AddComponent<MeshCollider>();
                
                if (_terrainMaterial != null)
                    renderer.material = _terrainMaterial;
            }
            else
            {
                chunkObj = chunkTransform.gameObject;
                filter = chunkObj.GetComponent<MeshFilter>();
                collider = chunkObj.GetComponent<MeshCollider>();
            }

            // Generate mesh using Surface Nets
            Mesh mesh = _meshGenerator.GenerateMesh(chunk);
            
            if (mesh.vertexCount > 0)
            {
                filter.mesh = mesh;
                collider.sharedMesh = mesh;
                chunkObj.SetActive(true);
            }
            else
            {
                chunkObj.SetActive(false);
            }

            chunk.IsDirty = false;
        }

        #endregion

        #region Water Flow

        private void ProcessWaterFlow()
        {
            if (Time.time - _lastWaterFlowTime < _waterFlowInterval)
                return;

            _lastWaterFlowTime = Time.time;

            // Process water sources
            int updatesProcessed = 0;
            var positionsToCheck = new Queue<Vector3Int>(_waterUpdateQueue);
            _waterUpdateQueue.Clear();

            while (positionsToCheck.Count > 0 && updatesProcessed < _maxWaterUpdatesPerFrame)
            {
                Vector3Int waterPos = positionsToCheck.Dequeue();
                
                if (GetVoxel(waterPos) != VoxelType.Water)
                    continue;

                TryFlowWater(waterPos, positionsToCheck);
                updatesProcessed++;
            }
        }

        private void TryFlowWater(Vector3Int waterPos, Queue<Vector3Int> updateQueue)
        {
            Vector3Int[] flowDirections = {
                new Vector3Int(0, -1, 0),   // Down first (gravity)
                new Vector3Int(-1, 0, 0),   // Then spread horizontally
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 0, -1),
                new Vector3Int(0, 0, 1)
            };

            foreach (var dir in flowDirections)
            {
                Vector3Int neighborPos = waterPos + dir;
                VoxelType neighbor = GetVoxel(neighborPos);

                if (neighbor == VoxelType.Air)
                {
                    SetVoxel(neighborPos, VoxelType.Water);
                    updateQueue.Enqueue(neighborPos);
                    
                    // If we flowed down, stop further spreading from this voxel
                    if (dir.y < 0)
                        break;
                }
            }
        }

        #endregion

        #region NavMesh Integration

        private void ProcessNavMeshUpdates()
        {
            if (_navMeshNeedsRebuild && Time.time - _lastNavMeshRebuild > _navMeshRebuildDelay)
            {
                RebuildNavMesh();
                _navMeshNeedsRebuild = false;
            }
        }

        private void RebuildNavMesh()
        {
            if (_navMeshSurface != null)
            {
                _navMeshSurface.BuildNavMesh();
            }
        }

        #endregion

        #region Terrain Generation

        private void ProcessTerrainGeneration()
        {
            // Process chunks that need generation
            // This could be expanded for async generation
            int chunksProcessed = 0;
            
            lock (_chunkLock)
            {
                foreach (var chunk in _chunks.Values)
                {
                    if (!chunk.IsGenerated && chunksProcessed < _maxChunksPerFrame)
                    {
                        InitializeChunkDensities(chunk);
                        UpdateChunkVisual(chunk);
                        chunksProcessed++;
                    }
                }
            }
        }

        #endregion
    }
}
