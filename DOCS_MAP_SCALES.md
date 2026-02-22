# Campaign Map Scale System

## Overview

The 4DND campaign system now supports multiple map scales for world-building, following the D&D 5e Dungeon Master's Guide recommendations (DMG pages 14-16). This allows Dungeon Masters to map their world at different levels of detail, from local province exploration to continental overviews.

## Map Scale Types

### Province Scale (Detailed Local)
- **1 hex = 1 mile**
- **Use for**: Day-to-day exploration, local area mapping
- **Keyboard shortcut**: `[1]`
- **Details visible**: Individual buildings, dungeons, small villages, terrain features

At province scale, you can map:
- The immediate area around your home base
- Detailed dungeon locations
- Individual farms and hamlets
- Small terrain features (hills, forests, rivers)
- Travel distances measured in hours

**Example**: A party exploring a 30-mile radius around their starting village would use province scale. Each hex represents roughly 2 hours of travel on foot.

### Kingdom Scale (Regional Travel)
- **1 hex = 6 miles**
- **Use for**: Regional mapping, travel between major settlements
- **Keyboard shortcut**: `[2]`
- **Details visible**: Major towns, cities, fortifications, large geographical features

At kingdom scale, you can map:
- Multiple regions and provinces
- Major cities and towns (villages too small to appear)
- Large castles and fortifications
- Major rivers, mountain ranges, forests
- Travel distances measured in days

**Example**: A kingdom spanning 100-200 miles would fit on a kingdom-scale map. Each hex represents about half a day's travel on horseback.

### Continent Scale (World Overview)
- **1 hex = 60 miles**
- **Use for**: Continental geography, major empires, long-distance travel
- **Keyboard shortcut**: `[3]`
- **Details visible**: Major cities, capitals, mountain ranges, seas

At continent scale, you can map:
- Multiple kingdoms and empires
- Major metropolises only (cities too small to appear)
- Large geographical features (mountain ranges, major rivers, seas)
- Trade routes between distant lands
- Travel distances measured in weeks

**Example**: An entire continent spanning thousands of miles. Each hex represents 3-4 days of travel. Perfect for planning long journeys or showing political boundaries between nations.

## Implementation Details

### Location Visibility by Scale

Locations have a `MinimumScale` property that determines at which scales they appear:

```csharp
public MapScale MinimumScale { get; set; } = MapScale.Province;
```

- **Province scale**: All discovered locations visible
- **Kingdom scale**: Only locations with `MinimumScale <= Kingdom` visible (Towns, Cities, Forts, etc.)
- **Continent scale**: Only major locations visible (Cities, Metropolises, Castles)

### Automatic Scale Assignment

When creating locations, the system automatically assigns appropriate minimum scales:

| Settlement Type | Minimum Scale | Reasoning |
|----------------|---------------|-----------|
| Hamlet | Province | Too small for regional maps |
| Village | Province | Local interest only |
| Fort | Province/Kingdom | Military outposts |
| Monastery | Province | Religious sites |
| Dungeon | Province | Adventure locations |
| Town | Kingdom | Regional importance |
| Castle | Kingdom | Major fortifications |
| City | Kingdom/Continent | Major settlements |
| Metropolis | Continent | Continental importance |

### Coordinate Conversion

The system automatically converts coordinates between scales:

```csharp
public static (int x, int y) ConvertCoordinates(int x, int y, MapScale fromScale, MapScale toScale)
{
    int fromHexSize = GetHexSize(fromScale);
    int toHexSize = GetHexSize(toScale);
    
    // Convert to "world units" (miles) then to target scale
    float worldX = x * fromHexSize;
    float worldY = y * fromHexSize;
    
    return ((int)(worldX / toHexSize), (int)(worldY / toHexSize));
}
```

**Example**: A location at province coordinates (30, 20) would be at kingdom coordinates (5, 3) because:
- Province: 30 hexes × 1 mile = 30 miles
- Kingdom: 30 miles ÷ 6 miles/hex = 5 hexes

## Usage in Game

### Switching Scales

Press the number keys while viewing the campaign map:
- `[1]` - Province Scale (1 mile/hex)
- `[2]` - Kingdom Scale (6 miles/hex)
- `[3]` - Continent Scale (60 miles/hex)

### Scale Indicator

The map displays a scale indicator in the top-right corner showing:
- Current active scale (highlighted in yellow)
- All available scales (gray when inactive)
- Description of current scale's purpose
- Hex-to-mile conversion

### Visual Changes by Scale

As you zoom out through scales, you'll notice:
- **Province ? Kingdom**: Villages disappear, only towns and larger settlements remain
- **Kingdom ? Continent**: Towns disappear, only cities and major landmarks remain
- **Grid density**: Same number of hexes but each represents more distance
- **Region visibility**: Regions defined at different scales appear/disappear appropriately

## DMG Design Philosophy

The multi-scale approach follows these DMG principles:

### 1. Start Small (Province Scale)
> "When you first start building your campaign world, keep it small. The characters need to know only about the city, town, or village where they start the game, and perhaps the dungeon they delve into on their first adventure."
> — DMG, p. 14

Start with a province-scale map of the immediate area. Add details as players explore.

### 2. Build Outward (Kingdom Scale)
> "As the adventurers explore the wider area around their starting location, you can sketch out neighboring lands."
> — DMG, p. 15

Once players travel beyond the local area, switch to kingdom scale to show regional geography.

### 3. Think Big (Continent Scale)
> "For a continent-spanning campaign, a larger scale is appropriate."
> — DMG, p. 16

Eventually, you may want to show the whole continent for long-distance travel or to give players a sense of the world.

### 4. Combining Scales

You don't need to map everything at every scale. The DMG recommends:
- **Province map**: Detailed map of starting area (30-mile radius)
- **Kingdom map**: Broader view showing nearby kingdoms (200-mile radius)
- **Continent map**: Optional overview for epic campaigns

In 4DND, this is automatic! Create locations at province scale, and the system will show appropriate locations at kingdom and continent scales.

## Travel Time by Scale

### Province Scale (1 mile/hex)
- **On foot**: 1 hex = 20 minutes (casual), 30 minutes (difficult terrain)
- **Mounted**: 1 hex = 10 minutes (normal terrain)
- **Fast travel**: 3 miles/hour = 3 hexes/hour

### Kingdom Scale (6 miles/hex)
- **On foot**: 1 hex = 2 hours (casual), 3 hours (difficult terrain)
- **Mounted**: 1 hex = 1 hour (normal terrain)
- **Fast travel**: 1 hex/hour mounted, 2 hexes/day on foot

### Continent Scale (60 miles/hex)
- **On foot**: 1 hex = 3 days (24 miles/day walking pace)
- **Mounted**: 1 hex = 2 days (30 miles/day riding pace)
- **Ship**: 1 hex = 1 day (60 miles/day with good winds)

## Best Practices

### For DMs

1. **Start with province scale** - Map the immediate area around your starting location
2. **Add locations gradually** - As players explore, add new locations they discover
3. **Mark discoveries** - Use `IsDiscovered` property to track what players have found
4. **Use appropriate scales** - Don't put hamlets on continent maps, they're too small
5. **Think about travel time** - Use the hex-to-mile conversion to calculate journey lengths

### For Players

1. **Use province scale for daily exploration** - See detailed local geography
2. **Switch to kingdom scale for travel planning** - See major destinations and routes
3. **Use continent scale for grand campaigns** - Understand the big picture
4. **Press M to open/close the map** - Access anytime during gameplay
5. **Check the scale indicator** - Know what distance each hex represents

## Future Enhancements

Potential additions to the scale system:

1. **Automatic region zooming** - Click a region to zoom to that area
2. **Distance measurement tool** - Click two points to see travel distance/time
3. **Route planning** - Draw travel routes and calculate journey time
4. **Fog of war by scale** - Unexplored areas appear blank until discovered
5. **Scale-specific random encounters** - Different encounter tables per scale
6. **Terrain overlay** - Show elevation, biomes, climate zones
7. **Political boundaries** - Show kingdoms, empires, territories at kingdom/continent scale

## Examples

### Example 1: Starting Campaign

```csharp
// Create starting campaign at province scale
var campaign = Campaign.CreateStartingCampaign("Lost Mine", "Phandalin", SettlementType.Village);

// Add nearby locations (province scale, 1 mile = 1 hex)
var tresendorManor = new Location
{
    Name = "Tresendar Manor",
    Type = SettlementType.Dungeon,
    X = 2,  // 2 miles east
    Y = 1,  // 1 mile north
    MinimumScale = MapScale.Province,
    IsDiscovered = false
};

var cragmawCastle = new Location
{
    Name = "Cragmaw Castle",
    Type = SettlementType.Castle,
    X = 20, // 20 miles east
    Y = 15, // 15 miles north
    MinimumScale = MapScale.Kingdom, // Important enough for kingdom map
    IsDiscovered = false
};
```

### Example 2: Regional Campaign

```csharp
// Switch to kingdom scale for regional view
campaign.CurrentScale = MapScale.Kingdom;

// Add major cities (kingdom scale, 6 miles = 1 hex)
var neverwinter = new Location
{
    Name = "Neverwinter",
    Type = SettlementType.City,
    X = 15, // 90 miles (15 × 6) from home base
    Y = -10, // 60 miles (10 × 6) south
    MinimumScale = MapScale.Kingdom,
    Population = 20000,
    IsDiscovered = true
};
```

### Example 3: Continental Campaign

```csharp
// Switch to continent scale for epic journey
campaign.CurrentScale = MapScale.Continent;

// Add distant metropolis (continent scale, 60 miles = 1 hex)
var waterdeep = new Location
{
    Name = "Waterdeep",
    Type = SettlementType.Metropolis,
    X = 8,  // 480 miles (8 × 60) from home base
    Y = -5, // 300 miles (5 × 60) south
    MinimumScale = MapScale.Continent,
    Population = 130000,
    IsDiscovered = false
};
```

## References

- **Dungeon Master's Guide**, Chapter 1: A World of Your Own (pages 9-35)
- **Dungeon Master's Guide**, "Mapping Your Campaign" (pages 14-16)
- **Player's Handbook**, Chapter 8: Adventuring (pages 181-203) - Travel rules

## Technical Notes

### Hex Grid System

The campaign uses a pointy-top hex grid:
```
     /\
    /  \
    \  /
     \/
```

Coordinates are stored in offset coordinates for simplicity. The system handles:
- Hex-to-screen conversion
- Distance calculation between hexes
- Adjacent hex finding

### Performance Considerations

- Only locations at the current scale are rendered
- Filtering by `MinimumScale` reduces draw calls at larger scales
- Region drawing is optimized for visible regions only

### Save Data

All scale information is saved with the campaign:
- `Campaign.CurrentScale` - Last viewed scale
- `Location.MinimumScale` - Visibility rules
- `Region.Scale` - Scale regions are defined at

## Summary

The map scale system brings D&D's world-building philosophy into 4DND:

? **Start small** with province-scale local exploration  
? **Expand gradually** to kingdom-scale regional travel  
? **Think big** with continent-scale epic campaigns  
? **Seamlessly switch** between scales with number keys  
? **Automatic filtering** shows appropriate locations per scale  
? **DMG-accurate** with 1-mile, 6-mile, and 60-mile hexes  

Press `[1]`, `[2]`, or `[3]` on the campaign map to experience different scales of your D&D world!
