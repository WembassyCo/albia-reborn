namespace Albia.Terrain
{
    /// <summary>
    /// Simplex noise implementation with multiple octaves.
    /// Provides smooth, deterministic noise for terrain generation.
    /// </summary>
    public sealed class SimplexNoise
    {
        private readonly WorldSeed _seed;
        private readonly int[] _permutation;
        private readonly int[] _p;

        // Simplex noise constants
        private const float F2 = 0.5f * (1.4142135623730951f - 1f);
        private const float G2 = (3f - 1.4142135623730951f) / 6f;
        private const float F3 = 1f / 3f;
        private const float G3 = 1f / 6f;

        /// <summary>
        /// Creates a Simplex noise generator with the specified seed.
        /// </summary>
        public SimplexNoise(WorldSeed seed)
        {
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            
            // Generate permutation table from seed
            _permutation = GeneratePermutation(seed);
            _p = new int[512];
            for (int i = 0; i < 512; i++)
            {
                _p[i] = _permutation[i % 256];
            }
        }

        /// <summary>
        /// Generates a permutation table from seed.
        /// </summary>
        private static int[] GeneratePermutation(WorldSeed seed)
        {
            var perm = new int[256];
            for (int i = 0; i < 256; i++)
                perm[i] = i;

            // Fisher-Yates shuffle with deterministic seed
            var rng = seed.GetPositionRandom(0, 0, 0);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }

            return perm;
        }

        /// <summary>
        /// 2D simplex noise.
        /// </summary>
        public float Noise(float x, float y)
        {
            float n0 = 0, n1 = 0, n2 = 0;

            float s = (x + y) * F2;
            int i = (int)Math.Floor(x + s);
            int j = (int)Math.Floor(y + s);
            float t = (i + j) * G2;
            float X0 = i - t;
            float Y0 = j - t;
            float x0 = x - X0;
            float y0 = y - Y0;

            int i1, j1;
            if (x0 > y0)
            {
                i1 = 1; j1 = 0;
            }
            else
            {
                i1 = 0; j1 = 1;
            }

            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2;
            float y2 = y0 - 1f + 2f * G2;

            int ii = i & 255;
            int jj = j & 255;

            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0)
                n0 = 0f;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Grad(_p[ii + _p[jj]], x0, y0);
            }

            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0)
                n1 = 0f;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Grad(_p[ii + i1 + _p[jj + j1]], x1, y1);
            }

            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0)
                n2 = 0f;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Grad(_p[ii + 1 + _p[jj + 1]], x2, y2);
            }

            return 70f * (n0 + n1 + n2);
        }

        /// <summary>
        /// 2D simplex noise with multiple octaves.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="octaves">Number of octaves (1-8 recommended)</param>
        /// <param name="persistence">Amplitude reduction per octave (0-1, default 0.5)</param>
        /// <param name="lacunarity">Frequency increase per octave (default 2.0)</param>
        /// <returns>Noise value in range [0, 1]</returns>
        public float OctaveNoise(float x, float y, int octaves = 3, float persistence = 0.5f, float lacunarity = 2f)
        {
            float total = 0;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return (total / maxValue + 1f) * 0.5f;
        }

        /// <summary>
        /// Ridged multifractal noise for terrain features like valleys.
        /// </summary>
        public float RidgedNoise(float x, float y, int octaves = 3, float persistence = 0.5f, float lacunarity = 2f)
        {
            float total = 0;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                float n = Noise(x * frequency, y * frequency);
                n = 1f - Math.Abs(n); // Ridged
                total += n * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }

        /// <summary>
        /// Billowy noise for soft, rolling hills.
        /// </summary>
        public float BillowNoise(float x, float y, int octaves = 3, float persistence = 0.5f, float lacunarity = 2f)
        {
            float n = OctaveNoise(x, y, octaves, persistence, lacunarity);
            return Math.Abs(n * 2f - 1f);
        }

        /// <summary>
        /// Gradient function for simplex noise.
        /// </summary>
        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>
        /// Linear interpolation.
        /// </summary>
        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        /// <summary>
        /// Smooth Hermite interpolation.
        /// </summary>
        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
