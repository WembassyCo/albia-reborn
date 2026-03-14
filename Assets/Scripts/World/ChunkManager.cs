using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AlbiaReborn.World
{
    /// <summary>
    /// Simple chunked voxel manager for Albia Reborn.
    /// Supports heightmap-based generation and cube stacking.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [SerializeField] public int CHUNK_SIZE = 16;
        
        [Header("Voxel Types")]
        [SerializeField] public Material airMaterial;
        [SerializeField] public Material dirtMaterial;
        [SerializeField] public Material stoneMaterial;
        
        [Header("Mesh Generation")]
        [SerializeField] public Mesh voxelMesh;
        [SerializeField] public bool useCubeStacking = true;
        
        [Header("Navigation")]
        [SerializeField] public bool rebuildNavMesh = true;
        [SerializeField] public float navMeshRebuildDelay = 0.5f;
        
        private Dictionary<Vector3Int, Chunk> chunks = new();
        private NavMeshSurface navMeshSurface;
        private float lastNavMeshRebuild;
        private bool navMeshNeedsRebuild;
        
        private void Awake()
        {
            navMeshSurface = GetComponent<NavMeshSurface>() ?? gameObject.AddComponent<NavMeshSurface>();
            if (voxelMesh == null)
                voxelMesh = CreateDefaultCubeMesh();
        }
        
        private void Update()
        {
            if (navMeshNeedsRebuild && Time.time - lastNavMeshRebuild > navMeshRebuildDelay)
            {
                RebuildNavMesh();
                navMeshNeedsRebuild = false;
            }
        }
        
        /// <summary>
        /// Generate voxels from a heightmap.
        /// Height 0.5 at position -> solid below, air above.
        /// </summary>
        public void GenerateFromHeightmap(float[,] heightmap, Vector2Int worldOffset)
        {
            int width = heightmap.GetLength(0);
            int depth = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int height = Mathf.FloorToInt(heightmap[x, z] * CHUNK_SIZE);
                    GenerateVoxelColumn(x + worldOffset.x, height, z + worldOffset.y);
                }
            }
            
            navMeshNeedsRebuild = true;
            lastNavMeshRebuild = Time.time;
        }
        
        /// <summary>
        /// Generate a voxel column from y=0 up to maxHeight.
        /// </summary>
        public void GenerateVoxelColumn(int worldX, int maxHeight, int worldZ)
        {
            Vector3Int baseCoord = WorldToChunkCoord(new Vector3Int(worldX, 0, worldZ));
            
            for (int y = 0; y <= maxHeight; y++)
            {
                Vector3Int worldPos = new Vector3Int(worldX, y, worldZ);
                VoxelType type = y == maxHeight ? VoxelType.Dirt : VoxelType.Stone;
                SetVoxel(worldPos, type);
            }
        }
        
        /// <summary>
        /// Set a voxel at world position.
        /// </summary>
        public void SetVoxel(Vector3Int worldPos, VoxelType type)
        {
            Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
            Chunk chunk = GetOrCreateChunk(chunkCoord);
            
            Vector3Int localPos = new Vector3Int(
                worldPos.x - chunkCoord.x * CHUNK_SIZE,
                worldPos.y - chunkCoord.y * CHUNK_SIZE,
                worldPos.z - chunkCoord.z * CHUNK_SIZE
            );
            
            if (localPos.x < 0 || localPos.x >= CHUNK_SIZE ||
                localPos.y < 0 || localPos.y >= CHUNK_SIZE ||
                localPos.z < 0 || localPos.z >= CHUNK_SIZE)
                return;
                
            chunk.SetVoxel(localPos, type);
            chunk.MarkDirty();
        }
        
        /// <summary>
        /// Get voxel at world position.
        /// </summary>
        public VoxelType GetVoxel(Vector3Int worldPos)
        {
            Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
            
            if (!chunks.TryGetValue(chunkCoord, out Chunk chunk))
                return VoxelType.Air;
            
            Vector3Int localPos = new Vector3Int(
                worldPos.x - chunkCoord.x * CHUNK_SIZE,
                worldPos.y - chunkCoord.y * CHUNK_SIZE,
                worldPos.z - chunkCoord.z * CHUNK_SIZE
            );
            
            return chunk.GetVoxel(localPos);
        }
        
        /// <summary>
        /// Force rebuild of all chunk meshes.
        /// </summary>
        public void UpdateMeshes()
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.IsDirty)
                {
                    RebuildChunkMesh(chunk);
                }
            }
        }
        
        private Chunk GetOrCreateChunk(Vector3Int coord)
        {
            if (!chunks.TryGetValue(coord, out Chunk chunk))
            {
                chunk = new Chunk(coord, CHUNK_SIZE);
                chunks[coord] = chunk;
            }
            return chunk;
        }
        
        private Vector3Int WorldToChunkCoord(Vector3Int worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / (float)CHUNK_SIZE),
                Mathf.FloorToInt(worldPos.y / (float)CHUNK_SIZE),
                Mathf.FloorToInt(worldPos.z / (float)CHUNK_SIZE)
            );
        }
        
        private void RebuildChunkMesh(Chunk chunk)
        {
            string chunkName = $"Chunk_{chunk.Coord.x}_{chunk.Coord.y}_{chunk.Coord.z}";
            Transform chunkTransform = transform.Find(chunkName);
            
            GameObject chunkObj;
            if (chunkTransform == null)
            {
                chunkObj = new GameObject(chunkName);
                chunkObj.transform.parent = transform;
                chunkObj.transform.localPosition = new Vector3(
                    chunk.Coord.x * CHUNK_SIZE,
                    chunk.Coord.y * CHUNK_SIZE,
                    chunk.Coord.z * CHUNK_SIZE
                );
            }
            else
            {
                chunkObj = chunkTransform.gameObject;
                // Clear existing children
                foreach (Transform child in chunkObj.transform)
                    Destroy(child.gameObject);
            }
            
            if (useCubeStacking)
            {
                BuildCubeStackingMesh(chunk, chunkObj);
            }
            else
            {
                BuildGreedyMesh(chunk, chunkObj);
            }
            
            chunk.ClearDirty();
        }
        
        private void BuildCubeStackingMesh(Chunk chunk, GameObject chunkObj)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    for (int x = 0; x < CHUNK_SIZE; x++)
                    {
                        VoxelType voxel = chunk.GetVoxel(new Vector3Int(x, y, z));
                        if (voxel == VoxelType.Air) continue;
                        
                        GameObject voxelObj = new GameObject($"Voxel_{x}_{y}_{z}");
                        voxelObj.transform.parent = chunkObj.transform;
                        voxelObj.transform.localPosition = new Vector3(x, y, z);
                        voxelObj.transform.localScale = Vector3.one;
                        
                        MeshFilter filter = voxelObj.AddComponent<MeshFilter>();
                        MeshRenderer renderer = voxelObj.AddComponent<MeshRenderer>();
                        filter.mesh = voxelMesh;
                        
                        Material mat = voxel == VoxelType.Dirt ? dirtMaterial :
                                       voxel == VoxelType.Stone ? stoneMaterial : airMaterial;
                        renderer.material = mat;
                        
                        // Only add collider to surface voxels
                        if (IsSurfaceVoxel(chunk, x, y, z))
                        {
                            voxelObj.AddComponent<BoxCollider>();
                        }
                    }
                }
            }
        }
        
        private void BuildGreedyMesh(Chunk chunk, GameObject chunkObj)
        {
            // Simplified mesh combining - combine all solid voxels into one mesh
            // This is a basic version; can be optimized further
            List<CombineInstance> combine = new List<CombineInstance>();
            
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    for (int x = 0; x < CHUNK_SIZE; x++)
                    {
                        VoxelType voxel = chunk.GetVoxel(new Vector3Int(x, y, z));
                        if (voxel == VoxelType.Air) continue;
                        
                        CombineInstance ci = new CombineInstance
                        {
                            mesh = voxelMesh,
                            transform = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, Vector3.one)
                        };
                        combine.Add(ci);
                    }
                }
            }
            
            if (combine.Count > 0)
            {
                Mesh combinedMesh = new Mesh();
                combinedMesh.CombineMeshes(combine.ToArray());
                
                MeshFilter filter = chunkObj.AddComponent<MeshFilter>();
                MeshRenderer renderer = chunkObj.AddComponent<MeshRenderer>();
                MeshCollider collider = chunkObj.AddComponent<MeshCollider>();
                
                filter.mesh = combinedMesh;
                collider.sharedMesh = combinedMesh;
                renderer.material = dirtMaterial;
            }
        }
        
        private bool IsSurfaceVoxel(Chunk chunk, int x, int y, int z)
        {
            Vector3Int[] neighbors = {
                new Vector3Int(x + 1, y, z), new Vector3Int(x - 1, y, z),
                new Vector3Int(x, y + 1, z), new Vector3Int(x, y - 1, z),
                new Vector3Int(x, y, z + 1), new Vector3Int(x, y, z - 1)
            };
            
            foreach (var n in neighbors)
            {
                if (n.x < 0 || n.x >= CHUNK_SIZE ||
                    n.y < 0 || n.y >= CHUNK_SIZE ||
                    n.z < 0 || n.z >= CHUNK_SIZE)
                    return true;
                    
                if (chunk.GetVoxel(n) == VoxelType.Air)
                    return true;
            }
            
            return false;
        }
        
        private void RebuildNavMesh()
        {
            if (navMeshSurface != null && rebuildNavMesh)
            {
                navMeshSurface.BuildNavMesh();
            }
        }
        
        private Mesh CreateDefaultCubeMesh()
        {
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0), // Front
                new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), // Back
            };
            
            int[] triangles = {
                0, 2, 1, 0, 3, 2,       // Front
                4, 5, 6, 4, 6, 7,       // Back
                0, 1, 5, 0, 5, 4,       // Bottom
                2, 3, 7, 2, 7, 6,       // Top
                0, 4, 7, 0, 7, 3,       // Left
                1, 2, 6, 1, 6, 5        // Right
            };
            
            Vector2[] uvs = {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            
            return mesh;
        }
    }
    
    /// <summary>
    /// Voxel types for Albia Reborn.
    /// </summary>
    public enum VoxelType : byte
    {
        Air = 0,
        Dirt = 1,
        Stone = 2
    }
    
    /// <summary>
    /// 16x16x16 voxel chunk.
    /// </summary>
    public class Chunk
    {
        public Vector3Int Coord { get; }
        public int Size { get; }
        public bool IsDirty { get; private set; }
        
        private VoxelType[] voxels;
        
        public Chunk(Vector3Int coord, int size)
        {
            Coord = coord;
            Size = size;
            voxels = new VoxelType[size * size * size];
            IsDirty = true;
        }
        
        public VoxelType GetVoxel(Vector3Int localPos)
        {
            return voxels[GetIndex(localPos.x, localPos.y, localPos.z)];
        }
        
        public void SetVoxel(Vector3Int localPos, VoxelType type)
        {
            voxels[GetIndex(localPos.x, localPos.y, localPos.z)] = type;
            IsDirty = true;
        }
        
        public void MarkDirty() => IsDirty = true;
        public void ClearDirty() => IsDirty = false;
        
        private int GetIndex(int x, int y, int z)
        {
            return (y * Size + z) * Size + x;
        }
    }
}
