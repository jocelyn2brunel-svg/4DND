# Creature Size Reference

## Size Categories

Based on D&D 5e rules, creatures are categorized into six size categories that affect their space control in combat:

### Size Chart

| Size        | Space           | Examples                    |
|-------------|-----------------|----------------------------|
| Tiny        | 2½ by 2½ ft.   | Imp, sprite                |
| Small       | 5 by 5 ft.     | Giant rat, goblin          |
| Medium      | 5 by 5 ft.     | Orc, werewolf              |
| Large       | 10 by 10 ft.   | Hippogriff, ogre           |
| Huge        | 15 by 15 ft.   | Fire giant, treant         |
| Gargantuan  | 20 by 20 ft.   | Kraken, purple worm        |

## Implementation

### CreatureSize Enum
```csharp
public enum CreatureSize
{
    Tiny,
    Small,
    Medium,
    Large,
    Huge,
    Gargantuan
}
```

### SizeHelper Class
The `SizeHelper` static class provides utility methods for working with creature sizes:

- `GetSpaceInFeet(CreatureSize)`: Returns the (width, height) space in feet
- `GetSpaceDescription(CreatureSize)`: Returns a formatted string like "5 by 5 ft."
- `GetExamples(CreatureSize)`: Returns example creatures of that size

### Creature Size Property
All creatures have a `Size` property that defaults to `Medium`.

### Race Sizes
Player character races have appropriate sizes:
- **Small**: Halflings (Lightfoot, Stout), Gnomes
- **Medium**: Humans, Elves (High, Wood, Drow), Dwarves (Hill, Mountain), Half-Orcs, Half-Elves, Tieflings, Dragonborn

### Monster Sizes
Current monsters in the game:
- **Small**: Goblins, Kobolds
- **Medium**: Orcs, Skeletons, Wolves

## Visual Representation

In the game, creature sizes are represented by:
1. **Circle radius scaling**: Creatures are drawn with circles that scale based on their size
   - Tiny: 0.5x base radius
   - Small: 0.8x base radius
   - Medium: 1.0x base radius
   - Large: 1.5x base radius
   - Huge: 2.0x base radius
   - Gargantuan: 2.5x base radius

2. **Size prefix in name**: Creatures display their size as a prefix
   - [T] for Tiny
   - [S] for Small
   - [M] for Medium
   - [L] for Large
   - [H] for Huge
   - [G] for Gargantuan

3. **Tooltip information**: Hovering over a creature shows its size and space dimensions

## Combat Implications

Creature size affects:
- **Space Control**: How much area the creature controls on the battlefield
- **Reach**: Larger creatures typically have greater reach
- **Grappling**: Size differences affect grappling rules
- **Squeezing**: Creatures can squeeze through spaces one size smaller

## Usage Example

```csharp
// Create a creature with specific size
var goblin = Creature.CreateGoblin(5, 5);
Console.WriteLine(goblin.Size); // Small
Console.WriteLine(SizeHelper.GetSpaceDescription(goblin.Size)); // "5 by 5 ft."

// Get space for calculations
var (width, height) = SizeHelper.GetSpaceInFeet(CreatureSize.Large);
// width = 10, height = 10
```
