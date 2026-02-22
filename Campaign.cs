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
        
        // Campaign notes and lore
        public List<string> CampaignNotes { get; set; } = new();
        public Dictionary<string, string> Lore { get; set; } = new();
        
        // Current map viewing scale
        public MapScale CurrentScale { get; set; } = MapScale.Province;
        
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
            return AllLocations.FindAll(l => l.IsDiscovered && (int)l.MinimumScale <= (int)scale);
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
                CurrentScale = MapScale.Province // Start at province scale (local)
            };
            
            // Create home base (starting at origin)
            campaign.HomeBase = new Location
            {
                Name = homeBaseName,
                Type = homeBaseType,
                X = 0,
                Y = 0,
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
                CenterX = 0,
                CenterY = 0,
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
                int distance = Math.Abs(location.X - region.CenterX) + Math.Abs(location.Y - region.CenterY);
                if (distance <= region.Radius)
                {
                    region.Locations.Add(location);
                    break;
                }
            }
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
