# D&D Round Down Implementation - Summary

## What Was Done

I've successfully implemented the D&D 5e "Round Down" rule throughout your game. This is one of the fundamental mathematical rules in D&D.

### Files Created

1. **DndMath.cs** - Core utility class for D&D mathematical operations
   - `Divide(int, int)` - Integer division that always rounds down
   - `GetAbilityModifier(int)` - Calculates ability modifiers
   - `GetProficiencyBonus(int)` - Calculates proficiency bonus by level
   - `Half(int)` - Halves a value (for resistance)
   - `GetUnits(int, int)` - Converts to discrete units (e.g., feet to tiles)

2. **DOCS_DND_MATH.md** - Comprehensive documentation explaining:
   - The D&D rounding rule
   - How each method works
   - Common use cases and examples
   - Why this matters for gameplay
   - Common misconceptions

3. **DndMathExamples.cs** - Runnable examples demonstrating the utility

### Files Modified

1. **Character.cs**
   - Updated `ProficiencyBonus` property to use `DndMath.GetProficiencyBonus()`
   - Updated `GetAbilityModifier()` method to use `DndMath.GetAbilityModifier()`

2. **Creature.cs**
   - Updated `GetAbilityModifier()` method to use `DndMath.GetAbilityModifier()`

3. **Game1.cs**
   - Updated movement calculation to use `DndMath.GetUnits()` for converting speed to tiles

4. **Dice.cs** (from previous update)
   - Already had `RollD100()` method for percentile dice

## The D&D Rule

> **"Whenever you divide a number in the game, round down if you end up with a fraction, even if the fraction is one-half or greater."**

This means:
- 11 ÷ 2 = **5** (not 6!)
- 9 ÷ 2 = **4** (not 5!)
- 15 ÷ 4 = **3** (not 4!)

## Key Examples

### Ability Modifiers
```csharp
// Strength 15 gives +2, not +3
// Formula: (15 - 10) / 2 = 2.5 ? 2
int mod = DndMath.GetAbilityModifier(15); // Returns 2
```

### Proficiency Bonus
```csharp
// Level 7 character has +3 proficiency
// Formula: 2 + (7 - 1) / 4 = 2 + 1.5 ? 3
int bonus = DndMath.GetProficiencyBonus(7); // Returns 3
```

### Damage Resistance
```csharp
// 11 fire damage halved = 5 damage
// Formula: 11 / 2 = 5.5 ? 5
int damage = DndMath.Half(11); // Returns 5
```

### Movement
```csharp
// 27 feet of movement = 5 tiles
// Formula: 27 / 5 = 5.4 ? 5
int tiles = DndMath.GetUnits(27, 5); // Returns 5
```

## How to Use

### In Character Creation
```csharp
Character hero = new Character { Strength = 15, Level = 5 };
int strMod = hero.GetAbilityModifier(hero.Strength);  // Uses DndMath
int profBonus = hero.ProficiencyBonus;                 // Uses DndMath
```

### In Combat
```csharp
// Calculate attack bonus
int attackBonus = creature.GetAbilityModifier(creature.Strength) + profBonus;

// Calculate movement range
int maxTiles = DndMath.GetUnits(creature.Speed, 5);

// Apply resistance
int resistedDamage = DndMath.Half(originalDamage);
```

## Testing

To see examples in action, you can call:
```csharp
DndMathExamples.RunExamples();
```

This will output various scenarios demonstrating the rounding rules.

## Benefits

? **Accurate D&D Rules** - All calculations match official D&D 5e rules
? **Consistency** - All division operations use the same utility
? **Maintainability** - Easy to update or fix in one place
? **Documentation** - Clear examples and explanations
? **Type Safety** - Compile-time checking prevents errors

## Common Pitfalls Avoided

? Using `(score - 10) / 2` directly (could round incorrectly)
? Using `Math.Round()` (would round 0.5 up to 1)
? Inconsistent rounding across different parts of the code

? Using `DndMath` methods ensures correctness everywhere

## Next Steps

The DndMath utility is now integrated into your game. All future code that needs division should use:
- `DndMath.Divide()` for general division
- `DndMath.GetAbilityModifier()` for ability scores
- `DndMath.GetProficiencyBonus()` for character level
- `DndMath.Half()` for resistances/vulnerabilities
- `DndMath.GetUnits()` for unit conversions

## References

- D&D 5e Player's Handbook, "Round Down" section
- D&D 5e Basic Rules, page 5
