using System;
using UnityEngine;
using AlbiaReborn.Core.Interfaces;

namespace AlbiaReborn.World.Generation
{
    /// <summary>
    /// Generates heightmaps using Simplex noise with 3 octaves.
    /// </summary>
    public class HeightmapGenerator : IHeightmapData
    {
        private float[,] _heightmap;
        private readonly int _seed;
        private readonly Vector2 _offset;
        
        public int Width => _heightmap?.GetLength(0) ?? 0;
        public int Height => _heightmap?.GetLength(1) ?? 0;
        public int Seed => _seed;

        public HeightmapGenerator(int seed = 0, int width = 512, int height = 512)
        {
            _seed = seed != 0 ? seed : UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _offset = new Vector2(_seed * 0.1f, _seed * 0.1f);
            
            Generate(width, height);
        }

        /// <summary>
        /// Generates heightmap with 3 octaves of noise.
        /// </summary>
        private void Generate(int width, int height)
        {
            _heightmap = new float[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    _heightmap[x, z] = GetNoiseValue(x, z);
                }
            }
            
            // Apply erosion pass
            ApplyErosion();
        }

        private float GetNoiseValue(int x, int z)
        {
            float nx = (x + _offset.x) * 0.01f;
            float nz = (z + _offset.y) * 0.01f;
            
            // 3 octaves: continental, regional, local
            float continental = SimplexNoise(nx * 0.1f, nz * 0.1f) * 0.5f;
            float regional = SimplexNoise(nx * 0.5f, nz * 0.5f) * 0.3f;
            float local = SimplexNoise(nx * 2.0f, nz * 2.0f) * 0.2f;
            
            // Combine with bias toward continental
            float height = continental + regional + local;
            return Mathf.Clamp01((height + 1.0f) * 0.5f); // Normalize to 0-1
        }

        /// <summary>
        /// Simple hydraulic erosion simulation.
        /// Water flows downhill, carves valleys.
        /// </summary>
        private void ApplyErosion()
        {
            int passes = 50;
            for (int p = 0; p < passes; p++)
            {
                for (int x = 1; x < Width - 1; x++)
                {
                    for (int z = 1; z < Height - 1; z++)
                    {
                        float current = _heightmap[x, z];
                        float lowest = current;
                        int lowX = x, lowZ = z;
                        
                        // Find lowest neighbor
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                float neighbor = _heightmap[x + dx, z + dz];
                                if (neighbor < lowest)
                                {
                                    lowest = neighbor;
                                    lowX = x + dx;
                                    lowZ = z + dz;
                                }
                            }
                        }
                        
                        // If water can flow, erode slightly
                        if (lowest < current)
                        {
                            float erosion = (current - lowest) * 0.1f;
                            _heightmap[x, z] -= erosion * 0.01f;
                            _heightmap[lowX, lowZ] += erosion * 0.005f; // Deposit
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Simplex noise implementation (simplified).
        /// For production, consider FastNoiseLite package.
        /// </summary>
        private float SimplexNoise(float x, float y)
        {
            // Permutation table for pseudo-random gradients
            int[] perm = new int[256];
            System.Random rand = new System.Random(_seed);
            for (int i = 0; i < 256; i++) perm[i] = i;
            for (int i = 255; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }
            
            // Simplified noise - gradient-based interpolation
            int xi = Mathf.FloorToInt(x) & 255;
            int yi = Mathf.FloorToInt(y) & 255;
            
            float xf = x - Mathf.Floor(x);
            float yf = y - Mathf.Floor(y);
            
            float u = Fade(xf);
            float v = Fade(yf);
            
            int a = perm[xi] + yi;
            int aa = perm[a % 256];
            int ab = perm[(a + 1) % 256];
            int b = perm[(xi + 1) % 256] + yi;
            int ba = perm[b % 256];
            int bb = perm[(b + 1) % 256];
            
            float x1 = Grad(perm[aa], xf, yf);
            float x2 = Grad(perm[ab], xf, yf - 1);
            float y1 = Grad(perm[ba], xf - 1, yf);
            float y2 = Grad(perm[bb], xf - 1, yf - 1);
            
            return Lerp(v, Lerp(u, x1, y1), Lerp(u, x2, y2));
        }

        private float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private float Lerp(float t, float a, float b) => a + t * (b - a);
        private float Grad(int hash, float x, float y)
        {
            int h = hash & 3;
            float u = h < 2 ? x : y;
            float v = h < 2 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public float[,] GetHeightmap() => _heightmap;
        
        public float GetHeightAt(int x, int z)
        {
            if (_heightmap == null || x < 0 || x >= Width || z < 0 || z >= Height)
                return 0f;
            return _heightmap[x, z];
        }
        
        public float GetHeightAt(Vector2Int position) => GetHeightAt(position.x, position.y);
    }
}
