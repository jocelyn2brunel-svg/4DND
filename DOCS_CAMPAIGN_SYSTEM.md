# Campaign System Documentation

## Overview

The campaign system implements the Dungeon Master's Guide advice for creating D&D campaigns:
1. **Start Small** - Begin with a home base and local area
2. **Create a Local Region** - Define the area within about 1 mile (10 hexes) of the home base
3. **Start a Dungeon** - Give players an immediate adventure location
4. **Expand Organically** - As the campaign progresses, add new locations and regions

## Files

- **Campaign.cs** - Core campaign data structures
  - `Campaign` class - Main campaign with locations, regions, and progress
  - `Location` class - Settlements, dungeons, and points of interest
  - `Region` class - Geographic areas containing multiple locations
  - `SettlementType` enum - Types of settlements (Village, Town, City, etc.)

- **CampaignCreation.cs** - UI for creating new campaigns
  - 4-step wizard for campaign setup
  - Name campaign ? Create home base ? Define region ? Choose starting adventure

- **CampaignMapViewer.cs** - Interactive world map
  - Hexagonal grid visualization
  - Shows discovered locations and regions
  - Pan (WASD), zoom (+/-), and navigation

## Usage

### Creating a Campaign

When a new character is created, the game now flows into campaign creation:

1. **Campaign Name** - Enter a name for your adventure
2. **Home Base** - Name your starting settlement and choose its type:
   - Hamlet (< 20 people)
   - Village (20-1000 people)
   - Town (1000-6000 people)
   - City (6000-25000 people)
   - Fort (Military outpost)
   - Castle (Fortified stronghold)
   - Monastery (Religious retreat)

3. **Local Region** - Choose the terrain type:
   - Forest
   - Hills
   - Mountains
   - Plains
   - Swamp
   - Desert
   - Coast

4. **Starting Adventure** - Select your first objective:
   - Explore a nearby dungeon
   - Investigate mysterious disappearances
   - Clear out a bandit camp
   - Rescue a kidnapped merchant
   - Hunt a dangerous beast
   - Recover a lost artifact
   - Custom adventure

### In-Game Controls

- **M** - Toggle campaign map view
- **C** - Toggle character sheet
- **ESC** - Open/close pause menu

### Campaign Map

The campaign map shows:
- **Gold Star** - Your home base
- **Colored Markers** - Other locations (color by type)
- **Yellow Circle** - Local region boundary
- **Hexagonal Grid** - World grid for navigation

Controls:
- **WASD** - Pan the map
- **+/-** - Zoom in/out
- **M** - Close map

## Campaign Data Structure

### Campaign Class
```csharp
public class Campaign
{
    public string Name { get; set; }
    public Location HomeBase { get; set; }
    public Region LocalRegion { get; set; }
    public List<Location> AllLocations { get; set; }
    public List<Region> Regions { get; set; }
    public int SessionCount { get; set; }
    public string CurrentObjective { get; set; }
    public List<string> CompletedObjectives { get; set; }
    public List<string> PartyMembers { get; set; }
}
```

### Location Class
```csharp
public class Location
{
    public string Name { get; set; }
    public SettlementType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Description { get; set; }
    public bool IsHomeBase { get; set; }
    public bool IsDiscovered { get; set; }
    public int Population { get; set; }
    public List<string> Features { get; set; }
    public List<string> NPCs { get; set; }
}
```

### Region Class
```csharp
public class Region
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int CenterX { get; set; }
    public int CenterY { get; set; }
    public int Radius { get; set; }
    public string Terrain { get; set; }
    public List<Location> Locations { get; set; }
}
```

## Saving and Loading

Campaigns are automatically saved to `saves/campaigns.json` when:
- A new campaign is created
- The game is closed (future enhancement)
- Manual save is triggered (future enhancement)

The save file contains:
- Campaign metadata (name, dates, session count)
- All locations and regions
- Party composition
- Current and completed objectives
- Campaign notes and lore

## Future Enhancements

### Immediate Improvements
1. **Campaign Selection** - Add UI to choose between multiple campaigns
2. **Location Discovery** - Implement mechanics for finding new locations
3. **Travel System** - Calculate travel time and random encounters
4. **Session Tracking** - Auto-increment session count and save progress
5. **Campaign Notes** - Add in-game journal for DM notes

### Advanced Features
1. **Random Location Generation** - Procedurally generate settlements and dungeons
2. **NPC System** - Track NPCs in each location
3. **Faction System** - Implement political factions and relationships
4. **World Events** - Time-based events that change the world
5. **Multiple Parties** - Support for different parties in same campaign
6. **Region Types** - Different region templates (kingdom, wilderness, underdark, etc.)

### DMG-Inspired Additions
1. **Settlement Details** - Generate government, defenses, commerce, temples
2. **Random Encounters** - Travel encounter tables by terrain type
3. **Weather System** - Track weather and its effects on travel
4. **Calendar** - Track in-game dates and seasons
5. **Quest Board** - Generate and track side quests

## Design Philosophy

Following the DMG's advice:
- **Start with the local area** - Players begin knowing only their home base
- **Expand organically** - New locations are added as players explore
- **Keep it manageable** - Start with 1-mile radius, grow as needed
- **Focus on play** - The system supports gameplay, doesn't overwhelm with detail
- **Flexible structure** - Easy to add custom content and modifications

## Example Campaign Flow

1. Create character "Thorin the Dwarf"
2. Create campaign "Lost Mine of Phandelver"
3. Choose home base "Phandalin" (Village)
4. Set region to Forest terrain
5. Choose objective "Explore a nearby dungeon"
6. Game creates:
   - Phandalin at (0, 0) - Home base
   - Local forest region with 10-hex radius
   - Nearby dungeon at (5, 5) - Undiscovered
7. Player presses M to view campaign map
8. Player sees Phandalin (gold star) and regional boundary
9. As player explores, new locations become visible
10. Campaign grows organically with player progress

## Integration with Existing Systems

The campaign system integrates with:
- **Character System** - Characters belong to campaigns
- **Combat System** - Battles occur at campaign locations
- **Vision System** - Lighting for exploring locations
- **Inventory System** - Treasure from campaign locations
- **Spell System** - Spells for campaign challenges
- **Creature System** - Enemies inhabit campaign locations

## Technical Notes

### Coordinate System
- Uses hexagonal grid (offset coordinates)
- Each hex = 5 feet in D&D terms
- Home base always at (0, 0) for simplicity
- Regions defined by center point and radius

### Settlement Population
Based on D&D settlement sizes:
- Hamlet: < 20
- Village: 20-1,000
- Town: 1,000-6,000
- City: 6,000-25,000
- Metropolis: 25,000+

### Serialization
Campaign data is JSON-serialized for human readability and easy editing.

## See Also
- DMG Chapter 1: "A World of Your Own"
- DMG Chapter 4: "Creating Nonplayer Characters"
- DMG Chapter 5: "Adventure Environments"
