using UnityEngine;
using AlbiaReborn.World.Voxel;
using AlbiaReborn.World.Climate;
using AlbiaReborn.Core.Interfaces;

namespace AlbiaReborn.World.Rendering
{
    /// <summary>
    /// Renders chunks as Unity meshes.
    /// MVP: Simple cube stacking, not marching cubes.
    /// </summary>
    public class ChunkRenderer : MonoBehaviour
    {
        [Header("Materials")]
        public Material AirMaterial;
        public Material DirtMaterial;
        public Material StoneMaterial;
        public Material GrassMaterial;
        public Material SandMaterial;

        private ChunkManager _chunkManager;
        private ClimateSystem _climateSystem;
        private IHeightmapData _heightmap;

        public void Initialize(ChunkManager chunkManager, ClimateSystem climateSystem, IHeightmapData heightmap)
        {
            _chunkManager = chunkManager;
            _climateSystem = climateSystem;
            _heightmap = heightmap;
        }

        /// <summary>
        /// Generate meshes for all chunks (call once at world creation).
        /// </summary>
        public void GenerateAllChunkMeshes()
        {
            foreach (var chunk in _chunkManager.GetAllChunks())
            {
                GenerateChunkMesh(chunk);
            }
        }

        private void GenerateChunkMesh(Chunk chunk)
        {
            GameObject chunkObj = new GameObject($"Chunk_{chunk.Position.x}_{chunk.Position.y}_{chunk.Position.z}");
            chunkObj.transform.parent = transform;
            chunkObj.transform.position = new Vector3(
                chunk.Position.x * ChunkManager.ChunkSize,
                chunk.Position.y * ChunkManager.ChunkSize,
                chunk.Position.z * ChunkManager.ChunkSize
            );

            // Build mesh data
            MeshBuilder builder = new MeshBuilder();
            
            for (int x = 0; x < ChunkManager.ChunkSize; x++)
            {
                for (int y = 0; y < ChunkManager.ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkManager.ChunkSize; z++)
                    {
                        Vector3Int localPos = new Vector3Int(x, y, z);
                        VoxelType voxel = chunk.GetVoxel(localPos);
                        
                        if (voxel != VoxelType.Air)
                        {
                            Vector3 worldPos = chunkObj.transform.position + new Vector3(x, y, z);
                            BiomeType biome = _climateSystem.GetBiomeAt(Vector3Int.FloorToInt(worldPos));
                            Material mat = GetMaterialForVoxel(voxel, biome);
                            
                            // Only render visible faces (with air neighbors)
                            if (IsVisible(chunk, x, y, z))
                            {
                                builder.AddCube(new Vector3(x, y, z), mat);
                            }
                        }
                    }
                }
            }

            // Create mesh
            MeshFilter filter = chunkObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = chunkObj.AddComponent<MeshRenderer>();
            
            filter.mesh = builder.CreateMesh();
            renderer.material = GetDefaultMaterial();
        }

        private bool IsVisible(Chunk chunk, int x, int y, int z)
        {
            // Check 6 neighbors - if any is air, this voxel is visible
            Vector3Int[] neighbors = new Vector3Int[]
            {
                new Vector3Int(x+1, y, z), new Vector3Int(x-1, y, z),
                new Vector3Int(x, y+1, z), new Vector3Int(x, y-1, z),
                new Vector3Int(x, y, z+1), new Vector3Int(x, y, z-1)
            };

            foreach (var n in neighbors)
            {
                if (n.x < 0 || n.x >= ChunkManager.ChunkSize ||
                    n.y < 0 || n.y >= ChunkManager.ChunkSize ||
                    n.z < 0 || n.z >= ChunkManager.ChunkSize)
                    return true; // At chunk edge, assume visible
                
                if (chunk.GetVoxel(n) == VoxelType.Air)
                    return true;
            }
            
            return false;
        }

        private Material GetMaterialForVoxel(VoxelType voxel, BiomeType biome)
        {
            return voxel switch
            {
                VoxelType.Grass => GrassMaterial,
                VoxelType.Dirt => DirtMaterial,
                VoxelType.Stone => StoneMaterial,
                VoxelType.Sand => SandMaterial,
                _ => DirtMaterial
            };
        }

        private Material GetDefaultMaterial()
        {
            return DirtMaterial ?? new Material(Shader.Find("Standard"));
        }
    }

    /// <summary>
    /// Helper class to build meshes from cubes.
    /// </summary>
    public class MeshBuilder
    {
        private System.Collections.Generic.List<Vector3> _vertices = new();
        private System.Collections.Generic.List<int> _triangles = new();
        private System.Collections.Generic.List<Vector2> _uvs = new();
        private int _vertexIndex = 0;

        public void AddCube(Vector3 position, Material material)
        {
            // Front face
            AddFace(position, Vector3.forward, Vector3.up, Vector3.right);
            // Back face
            AddFace(position + Vector3.back, Vector3.back, Vector3.up, Vector3.right);
            // Top face
            AddFace(position + Vector3.up, Vector3.up, Vector3.right, Vector3.forward);
            // Bottom face
            AddFace(position, Vector3.down, Vector3.right, Vector3.back);
            // Right face
            AddFace(position + Vector3.right, Vector3.right, Vector3.up, Vector3.back);
            // Left face
            AddFace(position, Vector3.left, Vector3.up, Vector3.forward);
        }

        private void AddFace(Vector3 origin, Vector3 normal, Vector3 up, Vector3 right)
        {
            _vertices.Add(origin + up + right);
            _vertices.Add(origin + up);
            _vertices.Add(origin);
            _vertices.Add(origin + right);

            _uvs.Add(new Vector2(1, 1));
            _uvs.Add(new Vector2(0, 1));
            _uvs.Add(new Vector2(0, 0));
            _uvs.Add(new Vector2(1, 0));

            // Two triangles per face
            _triangles.Add(_vertexIndex);
            _triangles.Add(_vertexIndex + 1);
            _triangles.Add(_vertexIndex + 2);
            _triangles.Add(_vertexIndex);
            _triangles.Add(_vertexIndex + 2);
            _triangles.Add(_vertexIndex + 3);

            _vertexIndex += 4;
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
