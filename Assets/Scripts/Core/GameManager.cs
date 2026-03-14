using UnityEngine;
using AlbiaReborn.World.Generation;
using AlbiaReborn.World.Voxel;
using AlbiaReborn.World.Climate;
using AlbiaReborn.World.Rendering;
using AlbiaReborn.Core.Persistence;

namespace AlbiaReborn.Core
{
    /// <summary>
    /// Bootstrap and lifecycle manager for Albia Reborn.
    /// Coordinates world generation, rendering, and save/load.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("World Settings")]
        public int Seed = 1337;
        public int WorldWidth = 128;
        public int WorldHeight = 128;
        public bool AutoGenerateOnStart = true;

        [Header("Rendering")]
        public Material DirtMaterial;
        public Material StoneMaterial;
        public Material GrassMaterial;
        public Material SandMaterial;

        // Core systems
        private HeightmapGenerator _heightmap;
        private ChunkManager _chunks;
        private ClimateSystem _climate;
        private SaveManager _saves;
        private ChunkRenderer _renderer;

        public static GameManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _saves = new SaveManager();
            
            if (AutoGenerateOnStart)
            {
                GenerateNewWorld(Seed);
            }
        }

        /// <summary>
        /// Generates a complete new world from seed.
        /// </summary>
        public void GenerateNewWorld(int seed)
        {
            Debug.Log($"Generating world with seed: {seed}");
            Seed = seed;

            // Phase 1: Generate heightmap
            _heightmap = new HeightmapGenerator(seed, WorldWidth, WorldHeight);
            Debug.Log($"Heightmap generated: {_heightmap.Width}x{_heightmap.Height}");

            // Phase 2: Create voxel chunks
            _chunks = new ChunkManager(_heightmap);
            Debug.Log($"Chunks created: {_heightmap.Width / ChunkManager.ChunkSize}x{_heightmap.Height / ChunkManager.ChunkSize}");

            // Phase 3: Climate/biome assignment
            _climate = new ClimateSystem(_heightmap);

            // Phase 4: Render meshes
            SetupRenderer();

            Debug.Log("World generation complete!");
        }

        private void SetupRenderer()
        {
            // Create renderer GameObject
            GameObject rendererObj = new GameObject("WorldRenderer");
            rendererObj.transform.parent = transform;
            
            _renderer = rendererObj.AddComponent<ChunkRenderer>();
            _renderer.Initialize(_chunks, _climate, _heightmap);
            
            // Assign materials
            _renderer.DirtMaterial = DirtMaterial;
            _renderer.StoneMaterial = StoneMaterial;
            _renderer.GrassMaterial = GrassMaterial;
            _renderer.SandMaterial = SandMaterial;
            
            _renderer.GenerateAllChunkMeshes();
        }

        /// <summary>
        /// Save current world to disk.
        /// </summary>
        public async void SaveWorld(string saveName)
        {
            if (_heightmap == null) return;
            
            var data = new WorldData(Seed, _heightmap.Width, _heightmap.Height, _heightmap.GetHeightmap());
            await _saves.SaveWorldAsync(saveName, data);
        }

        /// <summary>
        /// Load world from saved file.
        /// </summary>
        public async void LoadWorld(string fileName)
        {
            var data = await _saves.LoadWorldAsync(fileName);
            if (data != null)
            {
                Seed = data.Seed;
                // Rebuild world from saved heightmap
                GenerateNewWorld(data.Seed);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
