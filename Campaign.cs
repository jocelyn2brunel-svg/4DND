using System;
using System.Collections.Generic;

namespace _4DND
{
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
                SessionCount = 0
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
                Population = GetTypicalPopulation(homeBaseType)
            };
            
            // Create local region (1 mile radius = about 10 hexes)
            campaign.LocalRegion = new Region
            {
                Name = $"{homeBaseName} Region",
                Description = "The area surrounding your home base.",
                CenterX = 0,
                CenterY = 0,
                Radius = 10,
                Terrain = "Mixed"
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
        private static string GetDefaultDescription(SettlementType type)
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
        private static int GetTypicalPopulation(SettlementType type)
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
