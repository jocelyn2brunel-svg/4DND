# Example Campaign: Lost Mine of Phandelver with Map Scales

This example shows how to use the map scale system for a real D&D campaign (Lost Mine of Phandelver).

## Campaign Setup

### Session 0: Creating the Campaign

**DM**: Creates campaign at **Province Scale**
```csharp
var campaign = Campaign.CreateStartingCampaign(
    "Lost Mine of Phandelver", 
    "Phandalin", 
    SettlementType.Village
);
```

**Result**: 
- Home base: Phandalin village at (0, 0)
- Province scale active (1 hex = 1 mile)
- Local region radius: 30 miles

## Adding Locations (Province Scale)

### Nearby Adventure Sites

All coordinates in **province scale** (1 mile per hex):

```csharp
// Goblin Ambush site (1 mile southeast of Phandalin)
var ambushSite = Location.Create("Goblin Ambush", SettlementType.Wilderness, 1, -1);

// Cragmaw Hideout (6 miles northeast of ambush)
var hideout = Location.Create("Cragmaw Hideout", SettlementType.Dungeon, 7, 5);

// Tresendar Manor (in Phandalin)
var manor = Location.Create("Tresendar Manor", SettlementType.Dungeon, 0, 0);

// Old Owl Well (12 miles northeast)
var oldOwlWell = Location.Create("Old Owl Well", SettlementType.Wilderness, 8, 8);

// Ruins of Thundertree (20 miles northwest)
var thundertree = Location.Create("Thundertree", SettlementType.Wilderness, -15, 15);

// Wyvern Tor (30 miles east)
var wyvernTor = Location.Create("Wyvern Tor", SettlementType.Wilderness, 30, 0);

// Cragmaw Castle (20 miles northeast - important fort!)
var cragmawCastle = Location.Create("Cragmaw Castle", SettlementType.Castle, 20, 15);
cragmawCastle.MinimumScale = MapScale.Kingdom; // Visible on regional maps

// Wave Echo Cave (15 miles east)
var waveEchoCave = Location.Create("Wave Echo Cave", SettlementType.Dungeon, 15, -5);
```

### What Players See

**Province Scale View** (`1` key):
- All locations visible
- Detailed local geography
- Perfect for session-to-session planning

**Travel Example**: Phandalin ? Cragmaw Hideout
- Distance: 7 hexes east, 5 hexes north ? 9 miles direct
- Travel time: ~3 hours on foot
- DM notes encounters along the way

## Session 2-3: Local Exploration

**Players explore nearby sites at province scale:**

1. **Goblin Ambush** (1 mile SE) - 20 minutes walk
2. **Cragmaw Hideout** (7 miles NE) - 2.5 hours walk
3. **Tresendar Manor** (in town) - Immediate
4. **Old Owl Well** (12 miles NE) - 4 hours walk

**DM uses province scale** to:
- Plan random encounters (1 hex = 20 min travel)
- Show exact dungeon locations
- Track day-to-day movement

## Expanding to Kingdom Scale

### Session 4-5: Regional Context

**DM adds nearby settlements at kingdom scale:**

```csharp
// Switch to kingdom scale to add regional locations
// Coordinates now in kingdom scale (6 miles per hex)

// Neverwinter (major city, 90 miles southwest)
var neverwinter = Location.Create("Neverwinter", SettlementType.City, -15, -10);
neverwinter.Population = 20000;
neverwinter.MinimumScale = MapScale.Kingdom;

// Leilon (small town, 30 miles west)
var leilon = Location.Create("Leilon", SettlementType.Town, -5, 0);

// Triboar (town, 60 miles east)
var triboar = Location.Create("Triboar", SettlementType.Town, 10, 0);

// Yartar (city, 120 miles northeast)  
var yartar = Location.Create("Yartar", SettlementType.City, 15, 10);
yartar.Population = 8000;
```

### Coordinate Conversion Example

**Cragmaw Castle** exists at both scales:
- **Province coords**: (20, 15) - 20 miles east, 15 miles north
- **Kingdom coords**: (3, 2) - calculated as 20÷6?3, 15÷6?2
  
When you switch scales, the system automatically converts!

### What Players See

**Kingdom Scale View** (`2` key):
- Phandalin still visible (village too small, but marked as home base)
- Cragmaw Castle visible (it's a castle - regional importance)
- Small dungeons disappear (Cragmaw Hideout, Wave Echo Cave)
- Major cities appear (Neverwinter, Yartar)

**Use kingdom scale for:**
- Planning trips to major cities
- Understanding regional politics
- Showing players "the bigger picture"

## Travel Planning Example

### Journey to Neverwinter

**Using Kingdom Scale:**

```
Start: Phandalin (0, 0)
Destination: Neverwinter (-15, -10)

Distance: ?(15² + 10²) ? 18 hexes
Real distance: 18 hexes × 6 miles = 108 miles

Travel time:
- On foot (24 mi/day): 108 ÷ 24 = 4.5 days (5 days)
- Mounted (48 mi/day): 108 ÷ 48 = 2.25 days (2-3 days)
- With cart (slow): 108 ÷ 18 = 6 days
```

**DM**: Switches to **province scale** when party gets close to Neverwinter to show detailed approach.

## Session 10+: Going Continental

### Adding World Context

**DM adds continent-scale locations:**

```csharp
// Switch to continent scale (60 miles per hex)

// Waterdeep (major metropolis, 500 miles south)
var waterdeep = Location.Create("Waterdeep", SettlementType.Metropolis, 0, -8);
waterdeep.Population = 130000;
waterdeep.MinimumScale = MapScale.Continent;

// Luskan (city, 240 miles northwest)
var luskan = Location.Create("Luskan", SettlementType.City, -3, 3);
luskan.MinimumScale = MapScale.Continent;

// Mirabar (city, 480 miles north)
var mirabar = Location.Create("Mirabar", SettlementType.City, 0, 8);
mirabar.MinimumScale = MapScale.Continent;
```

### What Players See

**Continent Scale View** (`3` key):
- Only major cities visible (Neverwinter, Waterdeep, Luskan, Mirabar)
- Small locations completely hidden
- Shows Sword Coast geography
- Useful for grand campaigns or sequels

**Use continent scale for:**
- Showing players the world
- Planning epic journeys
- Understanding political geography
- Sequel campaign hints

## Real Play Example

### Session 6: "Let's Go to Neverwinter"

**Player**: "We want to travel to Neverwinter to sell our loot."

**DM**: 
1. Opens map (`M`)
2. Switches to **Kingdom Scale** (`2`)
3. Points out Neverwinter (15 hexes southwest)
4. Calculates: 15 hexes × 6 miles = 90 miles
5. Estimates: 90 ÷ 24 = 3.75 days on foot

**Player**: "Can we see what's along the way?"

**DM**: 
1. Stays in **Kingdom Scale**
2. Shows Leilon (5 hexes west) as a stop-off point
3. Marks route: Phandalin ? Leilon ? Neverwinter
4. Total: 5 hexes + 12 hexes = 17 hexes = 102 miles = 4-5 days

**During Travel**:
- DM uses **Kingdom Scale** for daily progress (4 hexes/day)
- When party reaches a specific encounter, switch to **Province Scale** for details
- Combat uses tactical 3D grid (5ft squares, not hexes)

## Visual Scale Comparison

### Same Region, Different Scales

**Province Scale** (Phandalin area):
```
[H] = Hamlet    [V] = Village    [D] = Dungeon    [W] = Wilderness

Scale: 1 hex = 1 mile, showing 20×20 mile area

    [D]Thundertree (ruins)
         |
         |
    [W]OldOwlWell??[D]Cragmaw??[W]WyvernTor
         |          Hideout
         |
    [V]Phandalin
    [D]Manor
         |
    [W]Ambush
```

**Kingdom Scale** (Sword Coast North):
```
Scale: 1 hex = 6 miles, showing 120×120 mile area

         [C]Luskan
              |
              |
         [C]Neverwinter
              |
         [T]Leilon??[F]Cragmaw Castle??[T]Triboar
              |           |
              |      [V]Phandalin
              |
         [C]Waterdeep
              
[C]=City [T]=Town [F]=Fort [V]=Village (home base only)
```

**Continent Scale** (Sword Coast):
```
Scale: 1 hex = 60 miles, showing 600×600 mile area

         [M]Icewind Dale
              |
         [C]Luskan
              |
         [C]Neverwinter
              |
         [M]Waterdeep
              |
         [C]Baldur's Gate

[M]=Metropolis [C]=City
(Phandalin not visible - too small!)
```

## DM Tips from This Campaign

### What Worked Well

1. **Started at Province Scale**
   - Detailed local area
   - Easy for players to understand
   - All dungeons clearly visible

2. **Introduced Kingdom Scale naturally**
   - When players asked "what's nearby?"
   - Showed regional context
   - Made travel planning easier

3. **Saved Continent Scale for later**
   - Only used when campaign went epic
   - Shows "the wider world"
   - Hints at future adventures

### Common Mistakes to Avoid

1. ? **Starting at Continent Scale**
   - Too much information
   - Players feel lost
   - No detail where they are
   - **Solution**: Start province, expand gradually

2. ? **Putting hamlets on Kingdom maps**
   - They're invisible at that scale!
   - Clutters the map
   - **Solution**: Hamlets = province scale only

3. ? **Not using scale conversion**
   - Same location at different scales should line up
   - System does this automatically
   - **Solution**: Trust the coordinate conversion

4. ? **Switching scales too often**
   - Confuses players
   - Breaks immersion
   - **Solution**: Pick appropriate scale and stick with it for the session

## Session-by-Session Scale Usage

| Sessions | Primary Scale | Secondary Scale | Purpose |
|----------|---------------|-----------------|---------|
| 1-3 | Province | — | Local exploration, learn mechanics |
| 4-6 | Province | Kingdom | Travel to nearby towns |
| 7-12 | Kingdom | Province | Regional adventures, return to details |
| 13-15 | Kingdom | Province | Major regional events |
| 16+ | Continent | Kingdom | Epic quests, world-spanning campaigns |

## Travel Log Example

**Session 5: Journey to Neverwinter**

| Day | Start | End | Distance | Scale Used | Events |
|-----|-------|-----|----------|------------|--------|
| 1 | Phandalin | Leilon | 30 mi (5 kingdom hexes) | Kingdom | Uneventful |
| 2 | Leilon | Halfway | 30 mi (5 kingdom hexes) | Kingdom | Bandit encounter |
| 3 | Halfway | Neverwinter outskirts | 30 mi | Kingdom | Heavy rain |
| 4 | Outskirts | Neverwinter | — | Province | Entered city gates |

**DM Notes**:
- Used **Kingdom Scale** for days 1-3 (long-distance travel)
- Switched to **Province Scale** on day 4 (approaching city)
- Bandit encounter: Temporarily used tactical grid (5ft squares)

## Conclusion

The map scale system lets you:

? **Start small** - Province scale for beginners  
? **Expand naturally** - Kingdom scale as they explore  
? **Think big** - Continent scale for epic campaigns  
? **Switch easily** - `1`, `2`, `3` keys  
? **Auto-filter** - Settlements appear/disappear by importance  
? **Plan realistically** - Accurate travel times and distances  

Following the Lost Mine example:
- Sessions 1-5: Province Scale (local exploration)
- Sessions 6-12: Kingdom Scale (regional travel)  
- Sessions 13+: Optional Continent Scale (if campaign continues)

**Remember**: The map scales match how D&D adventures naturally progress - from local to regional to epic!
