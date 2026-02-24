using Microsoft.Xna.Framework;
using System;

namespace _4DND
{
    public enum BiomeType
    {
        Ocean,
        Coast,
        Desert,
        Plains,
        Forest,
        Swamp,
        Tundra,
        Mountain,
        SnowMountain
    }

    /// <summary>
    /// Handles procedural generation of biomes and terrain using multi-octave noise.
    /// Provides consistent results based on a world seed.
    /// </summary>
    public static class WorldGenerator
    {
        public static BiomeType GetBiome(float x, float y, int seed)
        {
            float altitude = GetNoise(x, y, seed, 100.0f, 4);
            float temperature = GetNoise(x, y, seed + 123, 400.0f, 3);
            float moisture = GetNoise(x, y, seed + 456, 250.0f, 3);

            // Latitude influence on temperature (assuming y=0 is equator, but we'll just use raw noise for simplicity
            // or maybe a slight gradient if we wanted a globe feel).

            // 1. Water vs Land
            if (altitude < 0.35f) return BiomeType.Ocean;
            if (altitude < 0.40f) return BiomeType.Coast;

            // 2. High Altitude (Mountains)
            if (altitude > 0.75f)
            {
                if (temperature < 0.4f || altitude > 0.85f) return BiomeType.SnowMountain;
                return BiomeType.Mountain;
            }

            // 3. Biomes based on Temperature and Moisture
            if (temperature < 0.35f) return BiomeType.Tundra;

            if (temperature > 0.65f)
            {
                if (moisture < 0.35f) return BiomeType.Desert;
                if (moisture > 0.70f) return BiomeType.Swamp;
                return BiomeType.Forest;
            }

            // Moderate temperature
            if (moisture < 0.40f) return BiomeType.Plains;
            if (moisture > 0.60f) return BiomeType.Forest;

            // Default/Mixed
            return BiomeType.Plains;
        }

        public static Color GetBiomeColor(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => new Color(20, 60, 140),
                BiomeType.Coast => new Color(210, 190, 130),
                BiomeType.Desert => new Color(230, 200, 100),
                BiomeType.Plains => new Color(100, 180, 80),
                BiomeType.Forest => new Color(40, 100, 40),
                BiomeType.Swamp => new Color(60, 60, 40),
                BiomeType.Tundra => new Color(180, 210, 220),
                BiomeType.Mountain => new Color(100, 100, 110),
                BiomeType.SnowMountain => new Color(220, 230, 240),
                _ => Color.Magenta
            };
        }

        public static TileType GetTileType(int x, int y, int z, int seed, float worldOffsetX = 0, float worldOffsetY = 0)
        {
            if (z > 0) return TileType.Empty;

            // Use miles for biome calculation to have large regions
            float milesX = ((float)x / Campaign.TacticalUnitsPerMile) + worldOffsetX;
            float milesY = ((float)y / Campaign.TacticalUnitsPerMile) + worldOffsetY;

            BiomeType biome = GetBiome(milesX, milesY, seed);

            // Local noise for tile variation
            float localNoise = GetNoise(x, y, seed + 999, 10.0f, 1);
            float detailNoise = GetNoise(x, y, seed + 777, 2.0f, 1);

            return biome switch
            {
                BiomeType.Ocean => TileType.Water,

                BiomeType.Coast => detailNoise > 0.7f ? TileType.Water : TileType.Sand,

                BiomeType.Desert => localNoise > 0.8f ? TileType.Rock : TileType.Sand,

                BiomeType.Plains => localNoise > 0.85f ? TileType.DifficultTerrain : (localNoise > 0.4f ? TileType.Grass : TileType.Floor),

                BiomeType.Forest => localNoise > 0.4f ? TileType.DifficultTerrain : TileType.Grass,

                BiomeType.Swamp => detailNoise > 0.6f ? TileType.Water : (localNoise > 0.5f ? TileType.Mud : TileType.DifficultTerrain),

                BiomeType.Tundra => detailNoise > 0.7f ? TileType.Ice : TileType.Snow,

                BiomeType.Mountain => localNoise > 0.6f ? TileType.Rock : TileType.DifficultTerrain,

                BiomeType.SnowMountain => localNoise > 0.5f ? TileType.Snow : TileType.Rock,

                _ => TileType.Floor
            };
        }

        private static float GetNoise(float x, float y, int seed, float scale, int octaves)
        {
            float result = 0;
            float amplitude = 1.0f;
            float frequency = 1.0f / scale;
            float maxAmplitude = 0;

            for (int i = 0; i < octaves; i++)
            {
                result += ValNoise(x * frequency, y * frequency, seed + i) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.0f;
            }

            return result / maxAmplitude;
        }

        private static float ValNoise(float x, float y, int seed)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            float sx = x - x0;
            float sy = y - y0;

            // Cubic interpolation for smoother transitions
            float u = sx * sx * (3.0f - 2.0f * sx);
            float v = sy * sy * (3.0f - 2.0f * sy);

            float n00 = Hash(x0, y0, seed);
            float n10 = Hash(x0 + 1, y0, seed);
            float n01 = Hash(x0, y0 + 1, seed);
            float n11 = Hash(x0 + 1, y0 + 1, seed);

            float nx0 = MathHelper.Lerp(n00, n10, u);
            float nx1 = MathHelper.Lerp(n01, n11, u);

            return MathHelper.Lerp(nx0, nx1, v);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                uint u = (uint)h;
                return (u & 0x00FFFFFF) / 16777215f;
            }
        }
    }
}
