using System;

namespace Albia.World
{
    /// <summary>
    /// Deterministic seed management for world generation.
    /// Provides consistent pseudo-random values based on an initial seed integer.
    /// </summary>
    public sealed class WorldSeed
    {
        private readonly int _seed;
        private readonly Random _random;

        /// <summary>
        /// The current seed value.
        /// </summary>
        public int Seed => _seed;

        /// <summary>
        /// Creates a new WorldSeed with the specified integer seed.
        /// </summary>
        public WorldSeed(int seed)
        {
            _seed = seed;
            _random = new Random(seed);
        }

        /// <summary>
        /// Gets the next pseudo-random float in range [0, 1).
        /// </summary>
        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Gets the next pseudo-random integer.
        /// </summary>
        public int Next()
        {
            return _random.Next();
        }

        /// <summary>
        /// Returns a deterministic value for a given position.
        /// </summary>
        public float GetPositionValue(int x, int y, int layer = 0)
        {
            uint hash = (uint)_seed;
            hash = hash * 31 + (uint)x;
            hash = hash * 31 + (uint)y;
            hash = hash * 31 + (uint)layer;
            hash ^= hash >> 17;
            hash *= 0xed5ad729;
            hash ^= hash >> 11;
            hash *= 0xac4c1b51;
            hash ^= hash >> 15;
            return (hash & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
