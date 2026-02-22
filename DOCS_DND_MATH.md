# D&D 5e Mathematical Rules Implementation

## Overview

This document describes how the game implements the core D&D 5e rounding rule:

> **"Whenever you divide a number in the game, round down if you end up with a fraction, even if the fraction is one-half or greater."**
> 
> — D&D 5e Player's Handbook, "Round Down" section

## Implementation

All division operations in the game use the `DndMath` utility class to ensure consistent rounding behavior.

### Core Methods

#### `DndMath.Divide(int dividend, int divisor)`
Performs integer division following D&D rounding rules (always round down).

```csharp
// Examples:
DndMath.Divide(11, 2);  // Returns 5 (not 6!)
DndMath.Divide(9, 2);   // Returns 4
DndMath.Divide(15, 4);  // Returns 3
```

#### `DndMath.GetAbilityModifier(int abilityScore)`
Calculates ability modifiers using the formula: `(score - 10) / 2`

This is one of the most common uses of division in D&D.

| Ability Score | Modifier |
|--------------|----------|
| 8-9          | -1       |
| 10-11        | +0       |
| 12-13        | +1       |
| 14-15        | +2       |
| 16-17        | +3       |
| 18-19        | +4       |
| 20-21        | +5       |

```csharp
// Examples:
DndMath.GetAbilityModifier(15);  // Returns 2 ((15-10)/2 = 2.5 ? 2)
DndMath.GetAbilityModifier(9);   // Returns -1 ((9-10)/2 = -0.5 ? -1)
```

#### `DndMath.GetProficiencyBonus(int level)`
Calculates proficiency bonus using the formula: `2 + (level - 1) / 4`

| Level Range | Proficiency Bonus |
|-------------|------------------|
| 1-4         | +2              |
| 5-8         | +3              |
| 9-12        | +4              |
| 13-16       | +5              |
| 17-20       | +6              |

```csharp
// Examples:
DndMath.GetProficiencyBonus(1);   // Returns 2
DndMath.GetProficiencyBonus(5);   // Returns 3
DndMath.GetProficiencyBonus(9);   // Returns 4
```

#### `DndMath.Half(int value)`
Halves a value, commonly used for:
- Damage resistance
- Half-cover bonuses
- Spell effects

```csharp
// Examples:
DndMath.Half(11);  // Returns 5 (11 damage reduced to 5 by resistance)
DndMath.Half(9);   // Returns 4 (not 5!)
DndMath.Half(1);   // Returns 0
```

#### `DndMath.GetUnits(int total, int unitSize)`
Converts values to discrete units, such as:
- Movement speed to grid tiles
- Duration to rounds
- Range to squares

```csharp
// Examples:
DndMath.GetUnits(30, 5);  // Returns 6 (30 feet ÷ 5 feet/tile = 6 tiles)
DndMath.GetUnits(27, 5);  // Returns 5 (27 feet ÷ 5 feet/tile = 5 tiles, not 5.4!)
```

## Usage in Codebase

### Character.cs
```csharp
// Ability modifiers
public int GetAbilityModifier(int score) => DndMath.GetAbilityModifier(score);

// Proficiency bonus
public int ProficiencyBonus => DndMath.GetProficiencyBonus(Level);
```

### Creature.cs
```csharp
// Monster ability modifiers
public int GetAbilityModifier(int score) => DndMath.GetAbilityModifier(score);
```

### Game1.cs
```csharp
// Movement calculation (speed to tiles)
int maxMove = DndMath.GetUnits(currentCombatant.Speed, 5); // 5 feet per tile
```

## Why This Matters

### Example 1: Ability Scores
A character with 15 Strength has a modifier of +2, not +3:
- Formula: (15 - 10) / 2 = 2.5
- **Round down to 2** (not 3!)

### Example 2: Damage Resistance
A creature takes 11 fire damage but has fire resistance:
- Halved damage: 11 / 2 = 5.5
- **Round down to 5 damage** (not 6!)

### Example 3: Movement
A character with 27 feet of movement:
- Grid tiles: 27 / 5 = 5.4 tiles
- **Round down to 5 tiles** (not 6!)

### Example 4: Proficiency Bonus
A 7th level character:
- Formula: 2 + (7 - 1) / 4 = 2 + 1.5
- **Round down to 3** (not 4!)

## Common Misconceptions

? **WRONG**: "Half of 11 is 6 because 5.5 rounds to 6"
? **CORRECT**: "Half of 11 is 5 because D&D always rounds down"

? **WRONG**: "A 15 Strength gives +3 because (15-10)/2 = 2.5, which rounds to 3"
? **CORRECT**: "A 15 Strength gives +2 because D&D always rounds down"

## Testing

The `DndMathTests.cs` file contains comprehensive unit tests that verify:
- Division always rounds down
- Ability modifiers are calculated correctly
- Proficiency bonus scales properly
- Halving works as expected
- Unit conversions are accurate

Run tests with:
```bash
dotnet test
```

## References

- D&D 5e Player's Handbook, Chapter 7: "Round Down"
- D&D 5e Basic Rules, page 5: "Round Down"

## Additional Notes

### Negative Numbers
When dividing negative numbers, D&D still rounds down (towards negative infinity):
- -11 / 2 = -5.5 ? **-6** (not -5!)
- This is consistent with "rounding down" meaning "towards the more negative value"

### Integer Division in C#
C# integer division naturally truncates towards zero, which gives us the correct behavior for positive numbers. The `DndMath` class handles edge cases and negative numbers correctly.
