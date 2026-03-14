namespace Albia.Terrain
{
    /// <summary>
    /// Deterministic seed management for world generation.
    /// Provides consistent pseudo-random values based on an initial seed integer.
    /// </summary>
    public sealed class WorldSeed
    {
        private readonly int _seed;
        private readonly Random _random;
        private int _version;

        /// <summary>
        /// The current seed value.
        /// </summary>
        public int Seed => _seed;

        /// <summary>
        /// Version for this seed format (for backwards compatibility).
        /// </summary>
        public int Version => _version;

        /// <summary>
        /// Creates a new WorldSeed with the specified integer seed.
        /// </summary>
        public WorldSeed(int seed, int version = 1)
        {
            _seed = seed;
            _version = version;
            _random = new Random(seed);
        }

        /// <summary>
        /// Creates a new random seed based on current time.
        /// </summary>
        public static WorldSeed Random()
        {
            return new WorldSeed(Environment.TickCount);
        }

        /// <summary>
        /// Creates a seed from a string (hash-based).
        /// </summary>
        public static WorldSeed FromString(string seedString)
        {
            int hash = seedString.GetHashCode();
            return new WorldSeed(hash);
        }

        /// <summary>
        /// Gets the next pseudo-random integer.
        /// </summary>
        public int Next()
        {
            return _random.Next();
        }

        /// <summary>
        /// Gets the next pseudo-random integer in range [min, max).
        /// </summary>
        public int Next(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Gets the next pseudo-random float in range [0, 1).
        /// </summary>
        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Returns a deterministic value for a given position.
        /// Used consistent results for the same world seed.
        /// </summary>
        public float GetPositionValue(int x, int y, int layer = 0)
        {
            // Simple hash combining seed, position, and layer
            uint hash = (uint)_seed;
            hash = hash * 31 + (uint)x;
            hash = hash * 31 + (uint)y;
            hash = hash * 31 + (uint)layer;
            
            // Mix the bits
            hash ^= hash >> 17;
            hash *= 0xed5ad729;
            hash ^= hash >> 11;
            hash *= 0xac4c1b51;
            hash ^= hash >> 15;
            
            return (hash & 0xFFFFFF) / (float)0x1000000;
        }

        /// <summary>
        /// Gets a deterministic integer for a position.
        /// </summary>
        public int GetPositionInt(int x, int y, int layer = 0)
        {
            uint hash = (uint)_seed;
            hash = hash * 31 + (uint)x;
            hash = hash * 31 + (uint)y;
            hash = hash * 31 + (uint)layer;
            hash ^= hash >> 17;
            hash *= 0xed5ad729;
            
            return (int)(hash % int.MaxValue);
        }

        /// <summary>
        /// Resets the random number generator to initial seed state.
        /// </summary>
        public void Reset()
        {
            _random = new Random(_seed);
        }

        /// <summary>
        /// Creates a new Random instance seeded deterministically from position.
        /// </summary>
        public Random GetPositionRandom(int x, int y, int layer = 0)
        {
            int posSeed = GetPositionInt(x, y, layer);
            return new Random(posSeed);
        }
    }
}
