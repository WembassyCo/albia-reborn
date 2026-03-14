using System;
using AlbiaReborn.Core.Interfaces;
using AlbiaReborn.Core.Shared;
using UnityEngine;

namespace AlbiaReborn.Pods.Terrain
{
    /// <summary>
    /// Represents a 16x16x16 voxel chunk with density-based mesh generation
    /// </summary>
    public class Chunk : IChunkData
    {
        public const int SIZE = 16;
        public const int SIZE_MASK = SIZE - 1;  // 15, for fast modulo
        
        private readonly VoxelType[] _voxels;
        private readonly float[] _densities;
        private IChunkData[] _neighbors;
        
        public Vector3Int ChunkPosition { get; }
        public bool IsDirty { get; set; }
        public bool IsGenerated { get; private set; }
        public float LastModifiedTime { get; private set; }
        
        public event Action<IChunkData, Vector3Int> OnVoxelChanged;

        /// <summary>
        /// Creates a new chunk at the specified world chunk position
        /// </summary>
        public Chunk(Vector3Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
            _voxels = new VoxelType[SIZE * SIZE * SIZE];
            _densities = new float[SIZE * SIZE * SIZE];
            _neighbors = new IChunkData[6]; // -x, +x, -y, +y, -z, +z
            
            // Initialize as air
            Array.Fill(_voxels, VoxelType.Air);
            Array.Fill(_densities, -1f);
            
            IsDirty = true;
            IsGenerated = false;
            LastModifiedTime = Time.time;
        }

        /// <summary>
        /// Get array index from local coordinates
        /// </summary>
        private int GetIndex(int x, int y, int z)
        {
            return (y * SIZE + z) * SIZE + x;
        }

        public VoxelType GetVoxel(int x, int y, int z)
        {
            if (IsLocalCoordValid(x, y, z))
                return _voxels[GetIndex(x, y, z)];
            return VoxelType.Air;
        }

        public VoxelType GetVoxel(Vector3Int localPos)
        {
            return GetVoxel(localPos.x, localPos.y, localPos.z);
        }

        public void SetVoxel(int x, int y, int z, VoxelType type)
        {
            if (!IsLocalCoordValid(x, y, z))
                return;

            int index = GetIndex(x, y, z);
            VoxelType oldType = _voxels[index];
            
            if (oldType == type)
                return;

            _voxels[index] = type;
            _densities[index] = type.GetDensity();
            
            LastModifiedTime = Time.time;
            IsDirty = true;
            
            var localPos = new Vector3Int(x, y, z);
            OnVoxelChanged?.Invoke(this, localPos);

            // Mark neighbor chunks dirty if we're on an edge
            MarkEdgeNeighborsDirty(x, y, z);
        }

        public void SetVoxel(Vector3Int localPos, VoxelType type)
        {
            SetVoxel(localPos.x, localPos.y, localPos.z, type);
        }

        public float GetDensity(int x, int y, int z)
        {
            if (IsLocalCoordValid(x, y, z))
                return _densities[GetIndex(x, y, z)];
            return -1f;  // Air outside chunk bounds
        }

        /// <summary>
        /// Set density directly (for terrain generation)
        /// </summary>
        public void SetDensity(int x, int y, int z, float density)
        {
            if (!IsLocalCoordValid(x, y, z))
                return;
            
            _densities[GetIndex(x, y, z)] = density;
            IsDirty = true;
        }

        public Vector3Int LocalToWorld(Vector3Int localPos)
        {
            return new Vector3Int(
                ChunkPosition.x * SIZE + localPos.x,
                ChunkPosition.y * SIZE + localPos.y,
                ChunkPosition.z * SIZE + localPos.z
            );
        }

        public Vector3Int? WorldToLocal(Vector3Int worldPos)
        {
            Vector3Int localPos = new Vector3Int(
                worldPos.x - ChunkPosition.x * SIZE,
                worldPos.y - ChunkPosition.y * SIZE,
                worldPos.z - ChunkPosition.z * SIZE
            );

            if (!IsLocalCoordValid(localPos.x, localPos.y, localPos.z))
                return null;

            return localPos;
        }

        public IChunkData GetNeighbor(Vector3Int direction)
        {
            int index = GetNeighborIndex(direction);
            return index >= 0 ? _neighbors[index] : null;
        }

        public void SetNeighbor(Vector3Int direction, IChunkData neighbor)
        {
            int index = GetNeighborIndex(direction);
            if (index >= 0)
            {
                _neighbors[index] = neighbor;
            }
        }

        /// <summary>
        /// Initializes all voxel types from densities (called after generation)
        /// </summary>
        public void FinalizeGeneration()
        {
            for (int i = 0; i < _voxels.Length; i++)
            {
                float density = _densities[i];
                _voxels[i] = density > 0 ? VoxelType.Stone : VoxelType.Air;
                if (density > 0)
                {
                    // TODO: Set proper type based on height/biome
                    _voxels[i] = GetTypeFromDensity(density);
                }
            }
            
            IsGenerated = true;
            IsDirty = true;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        private VoxelType GetTypeFromDensity(float density)
        {
            return density > 0.5f ? VoxelType.Stone : VoxelType.Dirt;
        }

        private bool IsLocalCoordValid(int x, int y, int z)
        {
            return (x & SIZE_MASK) == x && 
                   (y & SIZE_MASK) == y && 
                   (z & SIZE_MASK) == z;
        }

        private void MarkEdgeNeighborsDirty(int x, int y, int z)
        {
            if (x == 0 && _neighbors[0] != null) _neighbors[0].MarkDirty();
            if (x == SIZE - 1 && _neighbors[1] != null) _neighbors[1].MarkDirty();
            if (y == 0 && _neighbors[2] != null) _neighbors[2].MarkDirty();
            if (y == SIZE - 1 && _neighbors[3] != null) _neighbors[3].MarkDirty();
            if (z == 0 && _neighbors[4] != null) _neighbors[4].MarkDirty();
            if (z == SIZE - 1 && _neighbors[5] != null) _neighbors[5].MarkDirty();
        }

        private int GetNeighborIndex(Vector3Int direction)
        {
            if (direction.x == -1 && direction.y == 0 && direction.z == 0) return 0;
            if (direction.x == 1 && direction.y == 0 && direction.z == 0) return 1;
            if (direction.x == 0 && direction.y == -1 && direction.z == 0) return 2;
            if (direction.x == 0 && direction.y == 1 && direction.z == 0) return 3;
            if (direction.x == 0 && direction.y == 0 && direction.z == -1) return 4;
            if (direction.x == 0 && direction.y == 0 && direction.z == 1) return 5;
            return -1;
        }

        /// <summary>
        /// Get raw density array for mesh generation
        /// </summary>
        public float[] GetDensityArray()
        {
            return _densities;
        }
    }
}
