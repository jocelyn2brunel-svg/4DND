using Microsoft.Xna.Framework;
using System;

namespace _4DND
{
    public enum BiomeType
    {
        Arctic,
        Coastal,
        Desert,
        Forest,
        Grassland,
        Hill,
        Mountain,
        Swamp,
        Underdark,
        Underwater,
        Urban
    }

    /// <summary>
    /// Handles procedural generation of biomes and terrain using multi-octave noise.
    /// Provides consistent results based on a world seed.
    /// </summary>
    public static class WorldGenerator
    {
        public static BiomeType GetBiome(float x, float y, int seed, int floor = 0, Campaign? campaign = null)
        {
            if (floor < 0)
            {
                float alt = GetNoise(x, y, seed, 1500.0f, 4);
                return alt < 0.35f ? BiomeType.Underwater : BiomeType.Underdark;
            }

            if (campaign != null && IsUrban(x, y, campaign)) return BiomeType.Urban;

            float altitude = GetNoise(x, y, seed, 1500.0f, 4);
            float temperature = GetNoise(x, y, seed + 123, 4000.0f, 3);
            float moisture = GetNoise(x, y, seed + 456, 3000.0f, 3);

            // Latitude influence on temperature (assuming y=0 is equator, but we'll just use raw noise for simplicity
            // or maybe a slight gradient if we wanted a globe feel).

            // 1. Water vs Land
            if (altitude < 0.35f) return BiomeType.Underwater;
            if (altitude < 0.40f) return BiomeType.Coastal;

            // 2. High Altitude (Mountains / Hills)
            if (altitude > 0.75f)
            {
                if (temperature < 0.4f || altitude > 0.85f) return BiomeType.Arctic;
                return BiomeType.Mountain;
            }
            if (altitude > 0.60f)
            {
                if (temperature < 0.3f) return BiomeType.Arctic;
                return BiomeType.Hill;
            }

            // 3. Biomes based on Temperature and Moisture
            if (temperature < 0.35f) return BiomeType.Arctic;

            if (temperature > 0.65f)
            {
                if (moisture < 0.35f) return BiomeType.Desert;
                if (moisture > 0.70f) return BiomeType.Swamp;
                return BiomeType.Forest;
            }

            // Moderate temperature
            if (moisture < 0.40f) return BiomeType.Grassland;
            if (moisture > 0.60f) return BiomeType.Forest;

            // Default/Mixed
            return BiomeType.Grassland;
        }

        public static int GetTargetHeight(float milesX, float milesY, int seed, BiomeType biome, float absX = 0, float absY = 0)
        {
            float baseHeight = 0;
            float maxHeight = 0;

            float altitude = GetNoise(milesX, milesY, seed, 1500.0f, 4);

            if (biome == BiomeType.Mountain || (biome == BiomeType.Arctic && altitude > 0.75f))
            {
                baseHeight = 0.75f;
                maxHeight = 200; // 1000 ft
            }
            else if (biome == BiomeType.Hill)
            {
                baseHeight = 0.60f;
                maxHeight = 20; // 100 ft
            }

            if (maxHeight > 0)
            {
                // Add local noise to create interesting slopes and terrain on the tactical map
                float localNoise = GetNoise(absX, absY, seed + 888, 80.0f, 3);
                float normalizedHeight = MathHelper.Clamp((altitude - baseHeight) / (1.0f - baseHeight), 0, 1);

                // Blend global altitude with local variation (local noise adds up to 10% variation)
                float finalHeightFactor = MathHelper.Clamp(normalizedHeight + (localNoise - 0.5f) * 0.2f, 0, 1);
                return (int)(finalHeightFactor * maxHeight);
            }
            return 0;
        }

        public static Location? GetUrbanLocation(float milesX, float milesY, Campaign? campaign)
        {
            if (campaign == null) return null;
            var (q, r) = Campaign.CartesianToAxial(milesX, milesY);
            foreach (var loc in campaign.AllLocations)
            {
                if (loc.Type == SettlementType.City || loc.Type == SettlementType.Metropolis || loc.Type == SettlementType.Town || loc.Type == SettlementType.Village)
                {
                    int dist = Campaign.GetHexDistance(q, r, loc.X, loc.Y);
                    if (dist == 0) return loc;
                    if (loc.Type == SettlementType.Metropolis && dist <= 2) return loc;
                    if (loc.Type == SettlementType.City && dist <= 1) return loc;
                }
            }
            return null;
        }

        private static bool IsUrban(float milesX, float milesY, Campaign? campaign)
        {
            return GetUrbanLocation(milesX, milesY, campaign) != null;
        }

        public static Color GetBiomeColor(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Underwater => new Color(20, 60, 140),
                BiomeType.Coastal => new Color(210, 190, 130),
                BiomeType.Desert => new Color(230, 200, 100),
                BiomeType.Grassland => new Color(100, 180, 80),
                BiomeType.Forest => new Color(40, 100, 40),
                BiomeType.Swamp => new Color(60, 60, 40),
                BiomeType.Arctic => new Color(180, 210, 220),
                BiomeType.Mountain => new Color(100, 100, 110),
                BiomeType.Hill => new Color(120, 150, 90),
                BiomeType.Underdark => new Color(40, 20, 60),
                BiomeType.Urban => new Color(130, 130, 140),
                _ => Color.Magenta
            };
        }

        public static TileType GetTileType(int x, int y, int z, int seed, float worldOffsetX = 0, float worldOffsetY = 0, int floor = 0, Campaign? campaign = null)
        {
            // Calculate absolute tactical coordinates in the world to ensure local noise
            // varies when traveling even within the same biome.
            float absX = (float)x + worldOffsetX * Campaign.TacticalUnitsPerMile;
            float absY = (float)y + worldOffsetY * Campaign.TacticalUnitsPerMile;

            // Use miles for biome calculation to have large regions
            float milesX = ((float)x / Campaign.TacticalUnitsPerMile) + worldOffsetX;
            float milesY = ((float)y / Campaign.TacticalUnitsPerMile) + worldOffsetY;

            BiomeType biome = GetBiome(milesX, milesY, seed, floor, campaign);

            // 1. Handle Height for Hill and Mountain biomes
            if (floor >= 0 && z > 0)
            {
                int targetZ = GetTargetHeight(milesX, milesY, seed, biome, absX, absY);
                if (z > targetZ) return TileType.Empty;
            }

            // Overworld: no tiles below ground level (z < 0 is void)
            if (floor >= 0 && z < 0) return TileType.Empty;

            if (floor < 0 && z > 0) return TileType.Empty;

            // Local noise for tile variation using absolute world coordinates
            float localNoise = GetNoise(absX, absY, seed + 999, 10.0f, 1);
            float detailNoise = GetNoise(absX, absY, seed + 777, 2.0f, 1);

            if (floor < 0)
            {
                if (biome == BiomeType.Underwater)
                {
                    return localNoise > 0.93f ? TileType.Rock : (localNoise > 0.85f ? TileType.Shrub : TileType.Sand);
                }
                if (biome == BiomeType.Underdark)
                {
                    float cavernNoise = GetNoise(absX, absY, seed + 666, 15.0f, 2);
                    if (cavernNoise > 0.62f) return TileType.Wall;
                    return localNoise > 0.85f ? TileType.DifficultTerrain : TileType.Floor;
                }
            }

            return biome switch
            {
                BiomeType.Underwater => TileType.Water,

                BiomeType.Coastal => detailNoise > 0.7f ? TileType.Water : TileType.Sand,

                BiomeType.Desert => localNoise > 0.95f ? TileType.Shrub : (localNoise > 0.8f ? TileType.Rock : TileType.Sand),

                BiomeType.Grassland => localNoise > 0.97f ? TileType.Tree : (localNoise > 0.92f ? TileType.Shrub : (localNoise > 0.85f ? TileType.DifficultTerrain : (localNoise > 0.4f ? TileType.Grass : TileType.Floor))),

                BiomeType.Forest => localNoise > 0.85f ? TileType.Tree : (localNoise > 0.75f ? TileType.Shrub : (localNoise > 0.4f ? TileType.DifficultTerrain : TileType.Grass)),

                BiomeType.Swamp => localNoise > 0.92f ? TileType.Tree : (localNoise > 0.85f ? TileType.Shrub : (detailNoise > 0.6f ? TileType.Water : (localNoise > 0.5f ? TileType.Mud : TileType.DifficultTerrain))),

                BiomeType.Arctic => localNoise > 0.98f ? TileType.Shrub : (detailNoise > 0.7f ? TileType.Ice : TileType.Snow),

                BiomeType.Mountain => localNoise > 0.95f ? TileType.Tree : (localNoise > 0.6f ? TileType.Rock : TileType.DifficultTerrain),

                BiomeType.Hill => localNoise > 0.96f ? TileType.Tree : (localNoise > 0.9f ? TileType.Shrub : (localNoise > 0.8f ? TileType.DifficultTerrain : TileType.Grass)),

                BiomeType.Underdark => localNoise > 0.8f ? TileType.Rock : (localNoise > 0.6f ? TileType.DifficultTerrain : TileType.Floor),

                BiomeType.Urban => GetUrbanTile(x, y, z, seed, worldOffsetX, worldOffsetY, campaign),

                _ => TileType.Floor
            };
        }

        private static TileType GetUrbanTile(int x, int y, int z, int seed, float worldOffsetX, float worldOffsetY, Campaign? campaign)
        {
            float absX = (float)x + worldOffsetX * Campaign.TacticalUnitsPerMile;
            float absY = (float)y + worldOffsetY * Campaign.TacticalUnitsPerMile;
            float milesX = (absX / Campaign.TacticalUnitsPerMile);
            float milesY = (absY / Campaign.TacticalUnitsPerMile);

            var loc = GetUrbanLocation(milesX, milesY, campaign);
            if (loc == null) return TileType.Floor;

            // Lot size: 16x16 tiles
            const int lotSize = 16;
            int lotX = (int)Math.Floor(absX / lotSize);
            int lotY = (int)Math.Floor(absY / lotSize);
            int localX = (int)Math.Floor(absX) % lotSize;
            if (localX < 0) localX += lotSize;
            int localY = (int)Math.Floor(absY) % lotSize;
            if (localY < 0) localY += lotSize;

            // Deterministic hash for this lot
            float lotHash = Hash(lotX, lotY, seed ^ StableStringHash(loc.Name));

            // Population influences density
            float densityThreshold = loc.Type switch
            {
                SettlementType.Metropolis => 0.8f,
                SettlementType.City => 0.6f,
                SettlementType.Town => 0.4f,
                SettlementType.Village => 0.2f,
                _ => 0.1f
            };

            if (lotHash > densityThreshold) return TileType.Floor;

            // Distribution of building sizes/heights
            // 1/4 normal, 1/4 taller, 1/4 larger, 1/4 both
            float distHash = Hash(lotX, lotY, seed ^ StableStringHash(loc.Name) ^ 999);
            int buildingWidth = 6;
            int buildingHeight = 6;
            int buildingFloors = 1;

            if (distHash <= 0.25f) // Normal
            {
                buildingWidth = 6;
                buildingHeight = 6;
                buildingFloors = 1;
            }
            else if (distHash <= 0.50f) // Taller
            {
                buildingWidth = 6;
                buildingHeight = 6;
                buildingFloors = 3;
            }
            else if (distHash <= 0.75f) // Larger
            {
                buildingWidth = 10;
                buildingHeight = 10;
                buildingFloors = 1;
            }
            else // Both
            {
                buildingWidth = 10;
                buildingHeight = 10;
                buildingFloors = 4;
            }

            // Margin for streets
            int marginX = (lotSize - buildingWidth) / 2;
            int marginY = (lotSize - buildingHeight) / 2;

            if (localX >= marginX && localX < marginX + buildingWidth &&
                localY >= marginY && localY < marginY + buildingHeight)
            {
                int bx = localX - marginX;
                int by = localY - marginY;

                if (z < buildingFloors)
                {
                    // Door on one side (still a tile - doors occupy a cell)
                    if (by == 0 && bx == buildingWidth / 2 && z == 0)
                        return TileType.DungeonDoorWooden;

                    // Stairs
                    if (bx == 1 && by == 1)
                    {
                        if (z < buildingFloors - 1) return TileType.DungeonStairsUp;
                    }
                    if (bx == 1 && by == 2)
                    {
                        if (z > 0) return TileType.DungeonStairsDown;
                    }

                    // All interior and perimeter tiles are now walkable floor
                    return TileType.Floor;
                }
                return TileType.Empty;
            }

            return TileType.Floor;
        }

        /// <summary>
        /// Compute urban wall edges for a given tile position. Call this alongside GetTileType
        /// to populate wall edges for procedurally generated urban buildings.
        /// </summary>
        public static void AddUrbanWallEdges(int x, int y, int z, int seed, float worldOffsetX, float worldOffsetY, Campaign? campaign, WallEdgeSystem wallEdges)
        {
            float absX = (float)x + worldOffsetX * Campaign.TacticalUnitsPerMile;
            float absY = (float)y + worldOffsetY * Campaign.TacticalUnitsPerMile;
            float milesX = (absX / Campaign.TacticalUnitsPerMile);
            float milesY = (absY / Campaign.TacticalUnitsPerMile);

            var loc = GetUrbanLocation(milesX, milesY, campaign);
            if (loc == null) return;

            const int lotSize = 16;
            int lotX = (int)Math.Floor(absX / lotSize);
            int lotY = (int)Math.Floor(absY / lotSize);
            int localX = (int)Math.Floor(absX) % lotSize;
            if (localX < 0) localX += lotSize;
            int localY = (int)Math.Floor(absY) % lotSize;
            if (localY < 0) localY += lotSize;

            float lotHash = Hash(lotX, lotY, seed ^ StableStringHash(loc.Name));
            float densityThreshold = loc.Type switch
            {
                SettlementType.Metropolis => 0.8f,
                SettlementType.City => 0.6f,
                SettlementType.Town => 0.4f,
                SettlementType.Village => 0.2f,
                _ => 0.1f
            };
            if (lotHash > densityThreshold) return;

            float distHash = Hash(lotX, lotY, seed ^ StableStringHash(loc.Name) ^ 999);
            int buildingWidth = 6, buildingHeight = 6, buildingFloors = 1;
            if (distHash <= 0.25f) { buildingWidth = 6; buildingHeight = 6; buildingFloors = 1; }
            else if (distHash <= 0.50f) { buildingWidth = 6; buildingHeight = 6; buildingFloors = 3; }
            else if (distHash <= 0.75f) { buildingWidth = 10; buildingHeight = 10; buildingFloors = 1; }
            else { buildingWidth = 10; buildingHeight = 10; buildingFloors = 4; }

            int marginX = (lotSize - buildingWidth) / 2;
            int marginY = (lotSize - buildingHeight) / 2;

            if (localX >= marginX && localX < marginX + buildingWidth &&
                localY >= marginY && localY < marginY + buildingHeight)
            {
                int bx = localX - marginX;
                int by = localY - marginY;

                if (z < buildingFloors)
                {
                    // Skip the door position
                    if (by == 0 && bx == buildingWidth / 2 && z == 0) return;

                    // Add wall edges on the building perimeter
                    if (by == 0) wallEdges.SetWall(x, y, z, WallSide.South);
                    if (by == buildingHeight - 1) wallEdges.SetWall(x, y, z, WallSide.North);
                    if (bx == 0) wallEdges.SetWall(x, y, z, WallSide.West);
                    if (bx == buildingWidth - 1) wallEdges.SetWall(x, y, z, WallSide.East);
                }
            }
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

        /// <summary>
        /// Deterministic string hash (FNV-1a). Unlike string.GetHashCode(),
        /// this returns the same value across process restarts so that
        /// procedural generation based on location names is stable.
        /// </summary>
        private static int StableStringHash(string s)
        {
            unchecked
            {
                const int fnvOffsetBasis = unchecked((int)2166136261);
                const int fnvPrime = 16777619;
                int hash = fnvOffsetBasis;
                foreach (char c in s)
                {
                    hash ^= c;
                    hash *= fnvPrime;
                }
                return hash;
            }
        }
    }
}
