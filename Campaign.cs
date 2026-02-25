using System;
using System.Collections.Generic;

namespace _4DND
{
    /// <summary>
    /// Map scale for campaign mapping (DMG p.14-16)
    /// </summary>
    public enum MapScale
    {
        Province,    // 1 hex = 1 mile (detailed local exploration)
        Kingdom,     // 1 hex = 6 miles (regional travel)
        Continent    // 1 hex = 60 miles (continental overview)
    }

    /// <summary>
    /// Travel pace for overland movement (PHB p.182).
    /// Fast: 4 miles/hour, 30 miles/day, -5 penalty to passive Perception.
    /// Normal: 3 miles/hour, 24 miles/day, no effect.
    /// Slow: 2 miles/hour, 18 miles/day, able to use stealth.
    /// </summary>
    public enum TravelPace
    {
        Fast,    // 4 miles/hour, 30 miles/day
        Normal,  // 3 miles/hour, 24 miles/day
        Slow     // 2 miles/hour, 18 miles/day
    }
    
    /// <summary>
    /// Represents the type of settlement in the campaign world.
    /// </summary>
    public enum SettlementType
    {
        Village,        // Small settlement (20-1000 people)
        Town,          // Medium settlement (1000-6000 people)
        City,          // Large settlement (6000-25000 people)
        Metropolis,    // Huge settlement (25000+ people)
        Hamlet,        // Tiny settlement (less than 20 people)
        Fort,          // Military outpost
        Castle,        // Fortified stronghold
        Monastery,     // Religious retreat
        Dungeon,       // Underground complex
        Wilderness     // Natural location
    }
    
    /// <summary>
    /// Represents a location in the campaign world.
    /// </summary>
    public class Location
    {
        public string Name { get; set; } = "";
        public SettlementType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Description { get; set; } = "";
        public bool IsHomeBase { get; set; }
        public bool IsDiscovered { get; set; }
        
        // Population (approximate)
        public int Population { get; set; }
        
        // Notable features
        public List<string> Features { get; set; } = new();
        
        // NPCs in this location
        public List<string> NPCs { get; set; } = new();
        
        // Scale visibility - at what scales this location is visible
        public MapScale MinimumScale { get; set; } = MapScale.Province;
        
        /// <summary>
        /// Creates a location with automatically assigned scale based on settlement type
        /// </summary>
        public static Location Create(string name, SettlementType type, int x, int y)
        {
            var location = new Location
            {
                Name = name,
                Type = type,
                X = x,
                Y = y,
                Description = Campaign.GetDefaultDescription(type),
                Population = Campaign.GetTypicalPopulation(type),
                MinimumScale = GetAppropriateScale(type),
                IsDiscovered = false
            };
            
            return location;
        }
        
        /// <summary>
        /// Determines the appropriate minimum scale for a settlement type
        /// </summary>
        private static MapScale GetAppropriateScale(SettlementType type)
        {
            return type switch
            {
                // Province scale only (too small for regional maps)
                SettlementType.Hamlet => MapScale.Province,
                SettlementType.Village => MapScale.Province,
                SettlementType.Monastery => MapScale.Province,
                SettlementType.Dungeon => MapScale.Province,
                SettlementType.Wilderness => MapScale.Province,
                
                // Kingdom scale (regional importance)
                SettlementType.Town => MapScale.Kingdom,
                SettlementType.Fort => MapScale.Kingdom,
                SettlementType.Castle => MapScale.Kingdom,
                
                // Visible at all scales (major importance)
                SettlementType.City => MapScale.Kingdom,
                SettlementType.Metropolis => MapScale.Continent,
                
                _ => MapScale.Province
            };
        }
    }
    
    /// <summary>
    /// Represents a region in the campaign world.
    /// </summary>
    public class Region
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int Radius { get; set; } = 50; // In grid units (hexes/squares)
        
        // Terrain type
        public string Terrain { get; set; } = "Mixed";
        
        // Locations in this region
        public List<Location> Locations { get; set; } = new();
        
        // Scale this region is defined at
        public MapScale Scale { get; set; } = MapScale.Province;
    }
    
    /// <summary>
    /// Represents a D&D campaign with its world, locations, and progress.
    /// Follows DMG guidance for starting small and expanding.
    /// </summary>
    public class Campaign
    {
        public string Name { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime LastPlayedDate { get; set; }
        
        // Starting point
        public Location HomeBase { get; set; } = null!;
        public Region LocalRegion { get; set; } = null!;
        
        // World exploration
        public List<Region> Regions { get; set; } = new();
        public List<Location> AllLocations { get; set; } = new();
        
        // Campaign progress
        public int SessionCount { get; set; }
        public string CurrentObjective { get; set; } = "";
        public string AdventureHook { get; set; } = "";
        public string AdventureMiddle { get; set; } = "";
        public string AdventureEnding { get; set; } = "";
        public List<string> CompletedObjectives { get; set; } = new();
        
        // Party information
        public List<string> PartyMembers { get; set; } = new(); // Character names

        // World generation
        public int Seed { get; set; }
        public float PartyX { get; set; }
        public float PartyY { get; set; }

        // Time tracking
        public double TotalGameMinutes { get; set; } = 8 * 60; // Start at 8:00 AM on Day 1

        public int GameHour => (int)(TotalGameMinutes / 60) % 24;
        public int GameMinute => (int)(TotalGameMinutes % 60);
        public int GameDay => (int)(TotalGameMinutes / (24 * 60)) + 1;

        public string GameTimeDisplay => $"Jour {GameDay}, {GameHour:D2}:{GameMinute:D2}";

        public double LastSurvivalCheckMinutes { get; set; } = 8 * 60;
        public float HoursTraveledToday { get; set; } = 0f;

        // Campaign notes and lore
        public List<string> CampaignNotes { get; set; } = new();
        public Dictionary<string, string> Lore { get; set; } = new();
        
        // Current map viewing scale
        public MapScale CurrentScale { get; set; } = MapScale.Province;

        /// <summary>
        /// Difficulty of the current (or most recent) combat encounter (DMG p.82).
        /// Updated by <see cref="CombatManager.GetEncounterDifficulty"/> when combat starts.
        /// </summary>
        public EncounterDifficulty? CurrentEncounterDifficulty { get; set; }

        // Tactical scale constants
        public const int TacticalUnitsPerMile = 1056; // 1 mile = 5280 feet, 1 square = 5 feet => 1056 squares
        
        /// <summary>
        /// Gets the distance represented by one hex at the given scale (in miles)
        /// </summary>
        public static int GetHexSize(MapScale scale)
        {
            return scale switch
            {
                MapScale.Province => 1,    // 1 hex = 1 mile
                MapScale.Kingdom => 6,     // 1 hex = 6 miles  
                MapScale.Continent => 60,  // 1 hex = 60 miles
                _ => 1
            };
        }
        
        /// <summary>
        /// Converts coordinates from one scale to another
        /// </summary>
        public static (int x, int y) ConvertCoordinates(int x, int y, MapScale fromScale, MapScale toScale)
        {
            int fromHexSize = GetHexSize(fromScale);
            int toHexSize = GetHexSize(toScale);
            
            // Convert to "world units" (miles) then to target scale
            float worldX = x * fromHexSize;
            float worldY = y * fromHexSize;
            
            return ((int)(worldX / toHexSize), (int)(worldY / toHexSize));
        }
        
        /// <summary>
        /// Gets all regions visible at the given scale
        /// </summary>
        public List<Region> GetRegionsAtScale(MapScale scale)
        {
            return Regions.FindAll(r => r.Scale == scale);
        }
        
        /// <summary>
        /// Gets all locations visible at the given scale
        /// </summary>
        public List<Location> GetLocationsAtScale(MapScale scale)
        {
            // Important locations (higher MinimumScale) should be visible at all scales.
            // Small locations (lower MinimumScale) should only be visible at detailed scales.
            return AllLocations.FindAll(l => l.IsDiscovered && (int)l.MinimumScale >= (int)scale);
        }

        /// <summary>
        /// Creates a basic starting campaign with a home base.
        /// </summary>
        public static Campaign CreateStartingCampaign(string campaignName, string homeBaseName, SettlementType homeBaseType)
        {
            var campaign = new Campaign
            {
                Name = campaignName,
                CreatedDate = DateTime.Now,
                LastPlayedDate = DateTime.Now,
                SessionCount = 0,
                CurrentScale = MapScale.Province, // Start at province scale (local)
                Seed = new Random().Next()
            };

            // Find a safe starting location on land
            var (startQ, startR) = FindStartingLandHex(campaign.Seed);
            var (pmilesX, pmilesY) = AxialToMiles(startQ, startR);
            campaign.PartyX = pmilesX;
            campaign.PartyY = pmilesY;
            
            // Create home base
            campaign.HomeBase = new Location
            {
                Name = homeBaseName,
                Type = homeBaseType,
                X = startQ,
                Y = startR,
                IsHomeBase = true,
                IsDiscovered = true,
                Description = GetDefaultDescription(homeBaseType),
                Population = GetTypicalPopulation(homeBaseType),
                MinimumScale = MapScale.Province
            };
            
            // Create local region (1 hex = 1 mile, radius 30 hexes = 30 miles)
            campaign.LocalRegion = new Region
            {
                Name = $"{homeBaseName} Region",
                Description = "The area surrounding your home base.",
                CenterX = startQ,
                CenterY = startR,
                Radius = 30,
                Terrain = "Mixed",
                Scale = MapScale.Province
            };
            
            campaign.LocalRegion.Locations.Add(campaign.HomeBase);
            campaign.AllLocations.Add(campaign.HomeBase);
            campaign.Regions.Add(campaign.LocalRegion);
            
            return campaign;
        }
        
        /// <summary>
        /// Adds a new location to the campaign.
        /// </summary>
        public void AddLocation(Location location)
        {
            AllLocations.Add(location);
            
            // Find which region this belongs to
            foreach (var region in Regions)
            {
                int distance = GetHexDistance(location.X, location.Y, region.CenterX, region.CenterY);
                if (distance <= region.Radius)
                {
                    region.Locations.Add(location);
                    break;
                }
            }
        }

        /// <summary>
        /// Computes distance between two axial hex coordinates.
        /// </summary>
        public static int GetHexDistance(int q1, int r1, int q2, int r2)
        {
            int dq = q1 - q2;
            int dr = r1 - r2;
            int ds = -(q1 + r1) + (q2 + r2);
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
        }

        /// <summary>
        /// Rounds fractional axial coordinates to the nearest integer axial hex.
        /// </summary>
        public static (int q, int r) RoundToHex(float q, float r)
        {
            float s = -q - r;
            int iq = (int)Math.Round(q);
            int ir = (int)Math.Round(r);
            int is_ = (int)Math.Round(s);

            float dq = Math.Abs(iq - q);
            float dr = Math.Abs(ir - r);
            float ds = Math.Abs(is_ - s);

            if (dq > dr && dq > ds)
                iq = -ir - is_;
            else if (dr > ds)
                ir = -iq - is_;

            return (iq, ir);
        }

        /// <summary>
        /// Converts Cartesian coordinates (miles) to axial hex coordinates.
        /// Assumes 1 hex unit = 1 mile distance between centers.
        /// </summary>
        public static (int q, int r) CartesianToAxial(float x, float y)
        {
            var (q_float, r_float) = MilesToAxial(x, y);
            return RoundToHex(q_float, r_float);
        }

        /// <summary>
        /// Converts Cartesian miles to fractional axial coordinates.
        /// </summary>
        public static (float q, float r) MilesToAxial(float x, float y)
        {
            // Inverse of AxialToMiles:
            // y = r * sqrt(3)/2  => r = y / (sqrt(3)/2)
            // x = q + r/2        => q = x - r/2
            float r = y / ((float)Math.Sqrt(3) / 2f);
            float q = x - r / 2f;
            return (q, r);
        }

        /// <summary>
        /// Converts axial hex coordinates to Cartesian miles.
        /// Assumes 1 hex unit = 1 mile distance between centers.
        /// </summary>
        public static (float x, float y) AxialToMiles(float q, float r)
        {
            // Standard pointy-top hex math with distance between centers = 1.0
            // The horizontal distance between centers is 1.0.
            // The vertical distance between centers is sqrt(3)/2.
            float x = q + r / 2f;
            float y = r * ((float)Math.Sqrt(3) / 2f);
            return (x, y);
        }

        /// <summary>
        /// Finds a starting hex that is on land (not in an Ocean biome).
        /// Searches in expanding rings around (0,0).
        /// </summary>
        public static (int q, int r) FindStartingLandHex(int seed)
        {
            // Search up to 500 miles away
            for (int radius = 0; radius < 500; radius++)
            {
                // Simple axial spiral/ring search
                for (int q = -radius; q <= radius; q++)
                {
                    int r1 = Math.Max(-radius, -q - radius);
                    int r2 = Math.Min(radius, -q + radius);

                    // Check boundaries of the hex "ring" at this radius
                    foreach (int r in new[] { r1, r2 })
                    {
                        var (x, y) = AxialToMiles(q, r);
                        if (WorldGenerator.GetBiome(x, y, seed) != BiomeType.Ocean)
                            return (q, r);
                    }

                    if (q == -radius || q == radius)
                    {
                        for (int r = r1 + 1; r < r2; r++)
                        {
                            var (x, y) = AxialToMiles(q, r);
                            if (WorldGenerator.GetBiome(x, y, seed) != BiomeType.Ocean)
                                return (q, r);
                        }
                    }
                }
            }

            return (0, 0); // Fallback to origin
        }

        /// <summary>
        /// Converts tactical grid coordinates (5ft squares) to campaign hex coordinates (1 mile hexes).
        /// </summary>
        public static (int q, int r) TacticalToHex(int x, int y)
        {
            // We want one hex to be TacticalUnitsPerMile wide (center-to-center).
            return CartesianToAxial((float)x / TacticalUnitsPerMile, (float)y / TacticalUnitsPerMile);
        }
        
        /// <summary>
        /// Gets a default description for a settlement type.
        /// </summary>
        public static string GetDefaultDescription(SettlementType type)
        {
            return type switch
            {
                SettlementType.Village => "A small, peaceful village with farms and simple homes.",
                SettlementType.Town => "A bustling town with markets, inns, and craftsmen.",
                SettlementType.City => "A large city with towering walls and diverse districts.",
                SettlementType.Metropolis => "A massive metropolis, center of trade and culture.",
                SettlementType.Hamlet => "A tiny hamlet with just a few families.",
                SettlementType.Fort => "A military fort with guards and defenses.",
                SettlementType.Castle => "A fortified castle, seat of local power.",
                SettlementType.Monastery => "A quiet monastery dedicated to contemplation.",
                SettlementType.Dungeon => "A dark dungeon filled with danger.",
                SettlementType.Wilderness => "An untamed wilderness location.",
                _ => "An interesting location."
            };
        }
        
        /// <summary>
        /// Gets typical population for a settlement type.
        /// </summary>
        public static int GetTypicalPopulation(SettlementType type)
        {
            return type switch
            {
                SettlementType.Hamlet => 15,
                SettlementType.Village => 500,
                SettlementType.Town => 3000,
                SettlementType.City => 15000,
                SettlementType.Metropolis => 50000,
                SettlementType.Fort => 200,
                SettlementType.Castle => 500,
                SettlementType.Monastery => 100,
                _ => 0
            };
        }
    }
}
