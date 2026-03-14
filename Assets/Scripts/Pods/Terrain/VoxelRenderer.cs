using System.Collections.Generic;
using Albia.Core.Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace Albia.Pods.Terrain
{
    /// <summary>
    /// Renders voxel chunks in Unity using MeshFilter/MeshRenderer.
    /// Handles mesh updates, material assignment, and instanced rendering for performance.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class VoxelRenderer : MonoBehaviour
    {
        [Header("Materials")]
        public Material chunkMaterial;
        public Material transparentMaterial;
        public Material waterMaterial;
        
        [Header("Texture Atlas")]
        public Texture2D voxelTextureAtlas;
        public int atlasTileSize = 32;
        
        [Header("Rendering")]
        public bool castShadows = true;
        public bool receiveShadows = true;
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
        public bool useInstancing = true;
        
        [Header("Collider")]
        public bool generateCollider = true;
        public bool updateColliderOnChange = true;
        
        // Components
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Chunk _chunk;
        
        // Mesh data storage
        private Mesh _currentMesh;
        private int _currentLOD = 0;
        private bool _isDirty = true;
        private bool _isVisible = true;
        
        // Instancing batch data
        private static Dictionary<Material, List<Matrix4x4>> _instanceBatches = new Dictionary<Material, List<Matrix4x4>>();
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Color = Shader.PropertyToID("_Color");
        
        /// <summary>
        /// The chunk this renderer displays
        /// </summary>
        public Chunk Chunk
        {
            get => _chunk;
            set
            {
                if (_chunk != null)
                    _chunk.OnVoxelChanged -= OnVoxelChanged;
                
                _chunk = value;
                
                if (_chunk != null)
                {
                    _chunk.OnVoxelChanged += OnVoxelChanged;
                    _isDirty = true;
                }
            }
        }
        
        /// <summary>
        /// Current LOD level
        /// </summary>
        public int LOD
        {
            get => _currentLOD;
            set
            {
                if (_currentLOD != value)
                {
                    _currentLOD = value;
                    _isDirty = true;
                }
            }
        }
        
        /// <summary>
        /// Mark this renderer for mesh rebuild
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set => _isDirty = value;
        }
        
        /// <summary>
        /// Is this renderer visible?
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                if (_meshRenderer != null)
                    _meshRenderer.enabled = value;
            }
        }
        
        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            
            // Create mesh if needed
            if (_meshFilter.mesh == null)
            {
                _currentMesh = new Mesh
                {
                    name = $"ChunkMesh_{gameObject.name}",
                    indexFormat = IndexFormat.UInt32  // Support >65k vertices
                };
                _meshFilter.mesh = _currentMesh;
            }
            else
            {
                _currentMesh = _meshFilter.mesh;
            }
            
            // Setup material if not assigned
            if (_meshRenderer.sharedMaterial == null && chunkMaterial != null)
            {
                _meshRenderer.material = chunkMaterial;
            }
            
            // Configure shadows
            _meshRenderer.shadowCastingMode = shadowCastingMode;
            _meshRenderer.receiveShadows = receiveShadows;
            
            // Setup instancing
            _meshRenderer.enabled = !useInstancing;
        }
        
        private void Start()
        {
            // Initial mesh generation
            if (_chunk != null && _isDirty)
            {
                GenerateMesh();
            }
        }
        
        private void Update()
        {
            // Check if chunk needs mesh regeneration
            if (_chunk != null && (_isDirty || _chunk.IsDirty))
            {
                GenerateMesh();
                _chunk.IsDirty = false;
                _isDirty = false;
            }
        }
        
        private void OnDestroy()
        {
            if (_chunk != null)
                _chunk.OnVoxelChanged -= OnVoxelChanged;
            
            if (_currentMesh != null)
            {
                Destroy(_currentMesh);
            }
        }
        
        /// <summary>
        /// Called when a voxel in the chunk changes
        /// </summary>
        private void OnVoxelChanged(IChunkData chunk, Vector3Int localPos)
        {
            _isDirty = true;
        }
        
        /// <summary>
        /// Generates or regenerates the mesh for this chunk
        /// </summary>
        public void GenerateMesh()
        {
            if (_chunk == null || !gameObject.activeInHierarchy)
                return;
            
            float[] densities = _chunk.GetDensityArray();
            MeshData meshData = MarchingCubes.GenerateMesh(densities, _currentLOD);
            
            if (meshData.IsEmpty)
            {
                // Clear mesh if empty
                _currentMesh.Clear();
                _meshRenderer.enabled = false;
                return;
            }
            
            // Update mesh
            meshData.ApplyTo(_currentMesh);
            _meshRenderer.enabled = IsVisible;
            
            // Update collider
            if (generateCollider && updateColliderOnChange)
            {
                UpdateCollider();
            }
            
            // Update material properties based on chunk content
            UpdateMaterialProperties();
        }
        
        /// <summary>
        /// Updates the mesh collider
        /// </summary>
        public void UpdateCollider()
        {
            if (_meshCollider != null && generateCollider)
            {
                _meshCollider.sharedMesh = _currentMesh;
            }
        }
        
        /// <summary>
        /// Updates material properties based on voxel types in chunk
        /// </summary>
        private void UpdateMaterialProperties()
        {
            if (_meshRenderer == null || _currentMesh == null)
                return;
            
            // Create material property block for instancing
            var propertyBlock = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(propertyBlock);
            
            // Set chunk position for shader effects
            Vector3 worldPos = new Vector3(
                _chunk.ChunkPosition.x * VoxelData.SIZE,
                _chunk.ChunkPosition.y * VoxelData.SIZE,
                _chunk.ChunkPosition.z * VoxelData.SIZE
            );
            propertyBlock.SetVector("_ChunkPosition", worldPos);
            
            // Set texture atlas if available
            if (voxelTextureAtlas != null)
            {
                propertyBlock.SetTexture(MainTex, voxelTextureAtlas);
            }
            
            _meshRenderer.SetPropertyBlock(propertyBlock);
        }
        
        /// <summary>
        /// Sets the material based on the primary voxel type in the chunk
        /// </summary>
        public void SetMaterialForVoxelType(VoxelType dominantType)
        {
            Material materialToUse = chunkMaterial;
            
            switch (dominantType)
            {
                case VoxelType.Water:
                    materialToUse = waterMaterial ?? chunkMaterial;
                    break;
                case VoxelType.Ice:
                    case VoxelType.Snow:
                    case VoxelType.Water:
                    materialToUse = transparentMaterial ?? chunkMaterial;
                    break;
            }
            
            if (materialToUse != null)
            {
                _meshRenderer.material = materialToUse;
            }
        }
        
        /// <summary>
        /// Sets up this renderer for a specific chunk
        /// </summary>
        public void Setup(Chunk chunk, int lod = 0)
        {
            Chunk = chunk;
            LOD = lod;
            
            // Position the gameobject
            transform.position = new Vector3(
                chunk.ChunkPosition.x * VoxelData.SIZE,
                chunk.ChunkPosition.y * VoxelData.SIZE,
                chunk.ChunkPosition.z * VoxelData.SIZE
            );
            
            _isDirty = true;
        }
        
        /// <summary>
        /// Marks the chunk dirty to trigger mesh rebuild
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }
        
        /// <summary>
        /// Forces immediate mesh regeneration
        /// </summary>
        public void ForceMeshUpdate()
        {
            _isDirty = true;
            GenerateMesh();
        }
        
        // ============================================
        // Static Instancing Methods (for batch rendering)
        // ============================================
        
        /// <summary>
        /// Queues this renderer for instanced batch rendering
        /// Call this before rendering frame with DrawInstanced
        /// </summary>
        public void QueueForInstancing()
        {
            if (!useInstancing || _meshRenderer == null)
                return;
            
            Material mat = _meshRenderer.sharedMaterial;
            if (mat == null)
                return;
            
            if (!_instanceBatches.ContainsKey(mat))
            {
                _instanceBatches[mat] = new List<Matrix4x4>();
            }
            
            _instanceBatches[mat].Add(transform.localToWorldMatrix);
        }
        
        /// <summary>
        /// Renders all queued instances
        /// Call from a manager script during rendering
        /// </summary>
        public static void RenderInstancedBatches(Mesh chunkMesh)
        {
            foreach (var batch in _instanceBatches)
            {
                Material mat = batch.Key;
                List<Matrix4x4> matrices = batch.Value;
                
                // Render in batches of 1023 (Unity's instancing limit per draw call)
                for (int i = 0; i < matrices.Count; i += 1023)
                {
                    int count = Mathf.Min(1023, matrices.Count - i);
                    Matrix4x4[] batchArray = matrices.GetRange(i, count).ToArray();
                    
                    Graphics.DrawMeshInstanced(
                        chunkMesh,
                        0,
                        mat,
                        batchArray,
                        count,
                        null,
                        ShadowCastingMode.On,
                        true
                    );
                }
            }
            
            // Clear batches for next frame
            _instanceBatches.Clear();
        }
        
        /// <summary>
        /// Clears all instancing batches
        /// </summary>
        public static void ClearInstancingBatches()
        {
            _instanceBatches.Clear();
        }
    }
}