using Albia.Core.Interfaces;

namespace Albia.Core.Shared
{
    /// <summary>
    /// Represents a single tile in the Albia world.
    /// Stores terrain height, water information, river status, and climate-relevant data.
    /// </summary>
    public struct WorldTile : IWorldTile
    {
        // Core terrain data
        public byte Height { get; set; }
        public bool IsWater { get; set; }
        public bool IsRiver { get; set; }
        public bool IsVolcanic { get; set; }
        public byte Moisture { get; set; }

        // Additional terrain properties
        public byte WaterDepth { get; set; }
        public SlopeDirection Slope { get; set; }
        public byte SlopeIntensity { get; set; }
        public bool IsOcean { get; set; }
        public bool IsLake { get; set; }

        // Climate-relevant data (cached)
        public float ElevationMeters { get; set; }
        public float Roughness { get; set; }
        public int DistanceToWater { get; set; }

        // River data
        public byte RiverVolume { get; set; }
        public int RiverSourceX { get; set; }
        public int RiverSourceY { get; set; }

        // Erosion tracking
        public byte ErosionAmount { get; set; }
        public byte SedimentDeposited { get; set; }

        /// <summary>
        /// Creates a default empty tile.
        /// </summary>
        public static WorldTile Empty = new WorldTile
        {
            Height = 0,
            IsWater = false,
            IsRiver = false,
            IsVolcanic = false,
            Moisture = 0,
            WaterDepth = 0,
            Slope = SlopeDirection.Flat,
            SlopeIntensity = 0,
            IsOcean = false,
            IsLake = false,
            ElevationMeters = 0,
            Roughness = 0,
            DistanceToWater = int.MaxValue,
            RiverVolume = 0,
            RiverSourceX = -1,
            RiverSourceY = -1,
            ErosionAmount = 0,
            SedimentDeposited = 0
        };

        /// <summary>
        /// Creates a tile with specified height.
        /// </summary>
        public static WorldTile Create(byte height)
        {
            return new WorldTile
            {
                Height = height,
                IsWater = false,
                IsRiver = false,
                IsVolcanic = false,
                Moisture = 0,
                ElevationMeters = height * 10 // Rough conversion: 1 height unit = 10 meters
            };
        }

        /// <summary>
        /// Creates a water tile.
        /// </summary>
        public static WorldTile CreateWater(byte height, byte depth, bool isOcean = false)
        {
            return new WorldTile
            {
                Height = height,
                IsWater = true,
                IsOcean = isOcean,
                IsLake = !isOcean,
                WaterDepth = depth,
                Moisture = 255,
                DistanceToWater = 0
            };
        }

        /// <summary>
        /// Marks this tile as part of a river.
        /// </summary>
        public void MarkAsRiver(byte volume, int sourceX, int sourceY)
        {
            IsRiver = true;
            RiverVolume = volume;
            RiverSourceX = sourceX;
            RiverSourceY = sourceY;
            Moisture = Math.Max(Moisture, (byte)200);
        }

        /// <summary>
        /// Marks this tile as volcanic.
        /// </summary>
        public void MarkAsVolcanic()
        {
            IsVolcanic = true;
            ElevationMeters += 500; // Volcanoes are tall
        }

        /// <summary>
        /// Calculates roughness based on local height variations.
        /// </summary>
        public void CalculateRoughness(float localVariance)
        {
            Roughness = Math.Clamp(localVariance / 100f, 0f, 1f);
        }
    }

    /// <summary>
    /// Direction of slope on a tile.
    /// </summary>
    public enum SlopeDirection
    {
        Flat = 0,
        North,
        Northeast,
        East,
        Southeast,
        South,
        Southwest,
        West,
        Northwest
    }
}
