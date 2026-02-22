# Vision and Light System

This implementation adds D&D 5e-compliant vision and lighting mechanics to the game.

## Core Concepts

### Light Levels
- **Bright Light**: Normal vision, no penalties
- **Dim Light**: Lightly obscured, disadvantage on Wisdom (Perception) checks
- **Darkness**: Heavily obscured, creatures without darkvision are effectively blinded

### Vision Types

#### Normal Vision
- Default range: 120 feet (24 tiles)
- Requires bright light to see normally
- In dim light: can see but with disadvantage on Perception
- In darkness: cannot see (blinded)

#### Darkvision
- Range: typically 60 feet (12 tiles), some races have 120 feet
- **In darkness**: Treats darkness as dim light (sees in shades of gray)
- **In dim light**: Treats dim light as bright light
- **Cannot discern color in darkness, only shades of gray**
- Races with darkvision: Elves, Dwarves, Half-Orcs, Tieflings, Gnomes, Half-Elves, Drow

#### Superior Darkvision
- Range: 120 feet (24 tiles)
- Same as darkvision but longer range
- Races: Drow

#### Blindsight
- Can perceive surroundings without relying on sight
- Works even when blinded
- Can detect invisible creatures
- Can see through vision-blocking effects like Fog Cloud
- Range varies by creature (e.g., Wolf: 30 feet)
- Does not require line of sight

#### Tremorsense
- Can detect and pinpoint the origin of vibrations within a specific radius
- Provided the creature and source are in contact with the same ground or substance
- **Cannot be used to detect flying or incorporeal creatures**
- Works even when blinded
- Many burrowing creatures have tremorsense (e.g., Umber Hulk: 60 feet)

#### Truesight
- Can see in normal and magical darkness
- Can see invisible creatures and objects
- Automatically detects visual illusions and succeeds on saving throws against them
- Perceives the original form of shapechangers or creatures transformed by magic
- Can see into the Ethereal Plane
- Very rare, typically for powerful creatures (e.g., Couatl: 120 feet)

## Light Sources

### Common Light Sources (from D&D 5e)
- **Candle**: 5 ft bright, 5 ft dim
- **Torch**: 20 ft bright, 20 ft dim
- **Lantern**: 30 ft bright, 30 ft dim
- **Light Spell**: 20 ft bright, 20 ft dim
- **Daylight Spell**: 60 ft bright, 60 ft dim

### Moving Light Sources
Light sources can be attached to creatures (e.g., player carrying a torch).
They automatically move with the creature.

## Obscurement

### Lightly Obscured
- Caused by: dim light, patchy fog, moderate foliage
- Effect: disadvantage on Wisdom (Perception) checks that rely on sight

### Heavily Obscured
- Caused by: darkness, opaque fog, dense foliage
- Effect: blocks vision entirely, creatures are blinded when trying to see into the area

## Conditions

### Blinded
- Can't see, automatically fails ability checks requiring sight
- Attack rolls have disadvantage
- Attack rolls against the creature have advantage
- **Blindsight and Tremorsense still work when blinded**

### Invisible
- Creature is heavily obscured for purpose of hiding
- Attack rolls have advantage
- Attack rolls against the creature have disadvantage
- **Can be detected by Blindsight, Tremorsense, and Truesight**

## Spells That Affect Vision

- **Blindness/Deafness** (Level 2): Target must save or be blinded
- **Darkness** (Level 2): Creates 15 ft radius of magical darkness
- **Fog Cloud** (Level 1): Creates 20 ft radius of heavily obscuring fog
- **Light** (Cantrip): Object sheds bright light
- **Daylight** (Level 3): Creates 60 ft radius of bright light

## Controls

- **V**: Toggle vision overlay on/off
- **L**: Toggle global daylight on/off
- **B**: Toggle Blinded condition (for testing)
- **F**: Cast Fog Cloud at player location (for testing)
- **K**: Cast Darkness at player location (for testing)
- **Tab**: Toggle combat UI

## Implementation Details

### VisionSystem Class
Manages lighting calculations and visibility for all creatures.

Key methods:
- `CalculateLighting()`: Computes light levels for all tiles based on light sources
- `CalculateVisibility(Creature)`: Determines which tiles a creature can see
- `IsVisible(x, y)`: Check if a tile is visible to the viewer
- `GetLightLevel(x, y)`: Get the light level at a specific tile
- `HasLineOfSight(x1, y1, x2, y2)`: Check if there's unobstructed line of sight

### Integration with Combat
- Vision is calculated at the start of combat
- Updates automatically when creatures move
- Affects attack rolls (blinded condition gives disadvantage)
- Creatures only render if visible (fog of war)

### Rendering
- Tiles are tinted based on light level:
  - Bright: White (full color)
  - Dim: Gray (128, 128, 128)
  - Darkness with darkvision: Dark blue-gray (64, 64, 96)
  - Complete darkness: Black (invisible)

## Future Enhancements

Potential additions:
- Sunlight Sensitivity (Drow, Kobolds): disadvantage in sunlight
- See Invisibility spell
- Faerie Fire spell (reveals invisible creatures)
- True Seeing spell (grants truesight)
- Fog and weather effects
- Different dungeon lighting configurations
- Torches as inventory items with duration
- Magical darkness vs normal darkness
- Pass Without Trace spell (bonus to Stealth in dim light or darkness)
