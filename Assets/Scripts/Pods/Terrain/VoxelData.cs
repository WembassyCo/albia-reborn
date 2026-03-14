using System;
using System.Collections.Generic;
using Albia.Core.Interfaces;
using Albia.Core.Shared;
using UnityEngine;

namespace Albia.Pods.Terrain
{
    /// <summary>
    /// Represents a 16x16x16 voxel chunk with run-length encoding for compression.
    /// Optimized for memory efficiency and fast access.
    /// </summary>
    public class VoxelData : IChunkData
    {
        public const int SIZE = 16;
        public const int SIZE_MASK = SIZE - 1;
        public const int VOXELS_PER_CHUNK = SIZE * SIZE * SIZE; // 4096

        // Run-Length Encoding entry: stores (count, voxelType) pairs
        private struct RLEntry
        {
            public ushort Count;
            public VoxelType Type;
        }

        // Sparse storage - only stores when beneficial
        private VoxelType[] _denseData;
        private List<RLEntry> _rleData;
        private float[] _densities;
        
        private bool _isCompressed = false;
        private IChunkData[] _neighbors;
        
        public Vector3Int ChunkPosition { get; }
        public bool IsDirty { get; set; }
        public bool IsGenerated { get; private set; }
        public float LastModifiedTime { get; private set; }
        
        public event Action<IChunkData, Vector3Int> OnVoxelChanged;

        /// <summary>
        /// Creates a new voxel data chunk at the specified world chunk position
        /// </summary>
        public VoxelData(Vector3Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
            _denseData = new VoxelType[VOXELS_PER_CHUNK];
            _densities = new float[VOXELS_PER_CHUNK];
            _neighbors = new IChunkData[6];
            
            // Initialize as air
            Array.Fill(_denseData, VoxelType.Air);
            Array.Fill(_densities, -1f);
            
            IsDirty = true;
            IsGenerated = false;
            LastModifiedTime = Time.time;
        }

        /// <summary>
        /// Get array index from local coordinates
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private int GetIndex(int x, int y, int z)
        {
            return (y * SIZE + z) * SIZE + x;
        }

        /// <summary>
        /// Gets voxel at local coordinates (0-15)
        /// </summary>
        public VoxelType GetVoxel(int x, int y, int z)
        {
            if (!IsLocalCoordValid(x, y, z))
                return VoxelType.Air;

            if (_isCompressed)
                Decompress();
            
            return _denseData[GetIndex(x, y, z)];
        }

        public VoxelType GetVoxel(Vector3Int localPos)
        {
            return GetVoxel(localPos.x, localPos.y, localPos.z);
        }

        /// <summary>
        /// Sets voxel at local coordinates, triggering events and mesh rebuild
        /// </summary>
        public void SetVoxel(int x, int y, int z, VoxelType type)
        {
            if (!IsLocalCoordValid(x, y, z))
                return;

            if (_isCompressed)
                Decompress();

            int index = GetIndex(x, y, z);
            VoxelType oldType = _denseData[index];
            
            if (oldType == type)
                return;

            _denseData[index] = type;
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

        /// <summary>
        /// Sets a region of voxels efficiently
        /// </summary>
        public void SetVoxelRegion(Vector3Int min, Vector3Int max, VoxelType type)
        {
            if (_isCompressed)
                Decompress();

            bool changed = false;
            
            for (int y = min.y; y <= max.y && y < SIZE; y++)
            {
                for (int z = min.z; z <= max.z && z < SIZE; z++)
                {
                    for (int x = min.x; x <= max.x && x < SIZE; x++)
                    {
                        int index = GetIndex(x, y, z);
                        if (_denseData[index] != type)
                        {
                            _denseData[index] = type;
                            _densities[index] = type.GetDensity();
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                LastModifiedTime = Time.time;
                IsDirty = true;
            }
        }

        /// <summary>
        /// Gets density value for mesh generation
        /// </summary>
        public float GetDensity(int x, int y, int z)
        {
            if (!IsLocalCoordValid(x, y, z))
                return -1f;

            return _densities[GetIndex(x, y, z)];
        }

        /// <summary>
        /// Set density directly (for terrain generation)
        /// </summary>
        public void SetDensity(int x, int y, int z, float density)
        {
            if (!IsLocalCoordValid(x, y, z))
                return;

            if (_isCompressed)
                Decompress();

            _densities[GetIndex(x, y, z)] = density;
            IsDirty = true;
        }

        /// <summary>
        /// Batch set densities for terrain generation
        /// </summary>
        public void SetDensities(float[] densities)
        {
            if (densities.Length != VOXELS_PER_CHUNK)
                throw new ArgumentException($"Density array must have {VOXELS_PER_CHUNK} elements");

            if (_isCompressed)
                Decompress();

            Array.Copy(densities, _densities, VOXELS_PER_CHUNK);
            IsDirty = true;
        }

        /// <summary>
        /// Converts local position to world position
        /// </summary>
        public Vector3Int LocalToWorld(Vector3Int localPos)
        {
            return new Vector3Int(
                ChunkPosition.x * SIZE + localPos.x,
                ChunkPosition.y * SIZE + localPos.y,
                ChunkPosition.z * SIZE + localPos.z
            );
        }

        /// <summary>
        /// Converts world position to local position (null if outside chunk)
        /// </summary>
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
        /// Compresses the voxel data using run-length encoding
        /// Call this on chunks that haven't been modified recently
        /// </summary>
        public void Compress()
        {
            if (_isCompressed || _denseData == null)
                return;

            _rleData = new List<RLEntry>();
            VoxelType currentType = _denseData[0];
            ushort count = 1;

            for (int i = 1; i < VOXELS_PER_CHUNK; i++)
            {
                if (_denseData[i] == currentType && count < ushort.MaxValue)
                {
                    count++;
                }
                else
                {
                    _rleData.Add(new RLEntry { Count = count, Type = currentType });
                    currentType = _denseData[i];
                    count = 1;
                }
            }
            _rleData.Add(new RLEntry { Count = count, Type = currentType });

            // Only keep compressed if it's actually smaller
            int rleSize = _rleData.Count * 3; // 2 bytes count + 1 byte type
            int denseSize = VOXELS_PER_CHUNK; // 1 byte per voxel

            if (rleSize < denseSize * 0.7f) // Compress if < 70% of original
            {
                _denseData = null;
                _isCompressed = true;
            }
            else
            {
                _rleData = null; // Discard, not worth it
            }
        }

        /// <summary>
        /// Decompresses RLE data back to dense array
        /// </summary>
        public void Decompress()
        {
            if (!_isCompressed || _rleData == null)
                return;

            _denseData = new VoxelType[VOXELS_PER_CHUNK];
            int index = 0;

            foreach (var entry in _rleData)
            {
                for (int i = 0; i < entry.Count && index < VOXELS_PER_CHUNK; i++)
                {
                    _denseData[index++] = entry.Type;
                }
            }

            _rleData = null;
            _isCompressed = false;
        }

        /// <summary>
        /// Finalizes terrain generation - converts densities to voxel types
        /// </summary>
        public void FinalizeGeneration(System.Func<float, int, int, int, VoxelType> typeSelector)
        {
            if (_isCompressed)
                Decompress();

            for (int y = 0; y < SIZE; y++)
            {
                for (int z = 0; z < SIZE; z++)
                {
                    for (int x = 0; x < SIZE; x++)
                    {
                        int index = GetIndex(x, y, z);
                        float density = _densities[index];
                        
                        if (density > 0)
                        {
                            _denseData[index] = typeSelector(density, x, y, z);
                        }
                        else
                        {
                            _denseData[index] = VoxelType.Air;
                        }
                    }
                }
            }

            IsGenerated = true;
            IsDirty = true;
        }

        /// <summary>
        /// Gets raw voxel array for mesh generation (ensures decompressed)
        /// </summary>
        public VoxelType[] GetVoxelArray()
        {
            if (_isCompressed)
                Decompress();
            return _denseData;
        }

        /// <summary>
        /// Gets raw density array for mesh generation
        /// </summary>
        public float[] GetDensityArray()
        {
            return _densities;
        }

        /// <summary>
        /// Get voxel at world position (queries neighbors if needed)
        /// </summary>
        public VoxelType GetVoxelWithNeighbors(int x, int y, int z)
        {
            if (IsLocalCoordValid(x, y, z))
                return GetVoxel(x, y, z);

            // Check neighbors
            Vector3Int neighborDir = Vector3Int.zero;
            Vector3Int localPos = new Vector3Int(x, y, z);

            if (x < 0) { neighborDir.x = -1; localPos.x += SIZE; }
            else if (x >= SIZE) { neighborDir.x = 1; localPos.x -= SIZE; }
            
            if (y < 0) { neighborDir.y = -1; localPos.y += SIZE; }
            else if (y >= SIZE) { neighborDir.y = 1; localPos.y -= SIZE; }
            
            if (z < 0) { neighborDir.z = -1; localPos.z += SIZE; }
            else if (z >= SIZE) { neighborDir.z = 1; localPos.z -= SIZE; }

            var neighbor = GetNeighbor(neighborDir);
            if (neighbor != null)
                return neighbor.GetVoxel(localPos);

            return VoxelType.Air;
        }

        /// <summary>
        /// Get density at world position (queries neighbors if needed)
        /// </summary>
        public float GetDensityWithNeighbors(int x, int y, int z)
        {
            if (IsLocalCoordValid(x, y, z))
                return GetDensity(x, y, z);

            Vector3Int neighborDir = Vector3Int.zero;
            Vector3Int localPos = new Vector3Int(x, y, z);

            if (x < 0) { neighborDir.x = -1; localPos.x += SIZE; }
            else if (x >= SIZE) { neighborDir.x = 1; localPos.x -= SIZE; }
            
            if (y < 0) { neighborDir.y = -1; localPos.y += SIZE; }
            else if (y >= SIZE) { neighborDir.y = 1; localPos.y -= SIZE; }
            
            if (z < 0) { neighborDir.z = -1; localPos.z += SIZE; }
            else if (z >= SIZE) { neighborDir.z = 1; localPos.z -= SIZE; }

            var neighbor = GetNeighbor(neighborDir);
            if (neighbor != null)
                return neighbor.GetDensity(localPos.x, localPos.y, localPos.z);

            return -1f;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Calculate memory usage in bytes
        /// </summary>
        public int GetMemoryUsage()
        {
            int usage = _denseData != null ? _denseData.Length : 0;
            usage += _densities != null ? _densities.Length * sizeof(float) : 0;
            usage += _rleData != null ? _rleData.Count * 3 : 0; // 2 + 1 bytes per entry
            return usage;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool IsLocalCoordValid(int x, int y, int z)
        {
            return (uint)x < SIZE && (uint)y < SIZE && (uint)z < SIZE;
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
    }
}