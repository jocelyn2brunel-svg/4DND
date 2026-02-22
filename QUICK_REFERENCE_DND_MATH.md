# Quick Reference: DndMath Usage

## When to Use DndMath

| Situation | Method | Example |
|-----------|--------|---------|
| Calculating ability modifier | `DndMath.GetAbilityModifier(score)` | `DndMath.GetAbilityModifier(15)` ? 2 |
| Calculating proficiency bonus | `DndMath.GetProficiencyBonus(level)` | `DndMath.GetProficiencyBonus(7)` ? 3 |
| Applying damage resistance | `DndMath.Half(damage)` | `DndMath.Half(11)` ? 5 |
| Converting speed to tiles | `DndMath.GetUnits(speed, 5)` | `DndMath.GetUnits(30, 5)` ? 6 |
| Any other division | `DndMath.Divide(a, b)` | `DndMath.Divide(11, 2)` ? 5 |

## Common Formulas

### Ability Modifier
```csharp
int modifier = DndMath.GetAbilityModifier(abilityScore);
// Formula: (score - 10) / 2, rounded down
```

### Attack Roll
```csharp
int attackBonus = DndMath.GetAbilityModifier(strength) + proficiencyBonus;
int attackRoll = Dice.Roll(20) + attackBonus;
bool hits = attackRoll >= targetAC;
```

### Damage Roll with Resistance
```csharp
var damageRoll = Dice.RollNotation("2d6+3");
int finalDamage = hasResistance ? DndMath.Half(damageRoll.Total) : damageRoll.Total;
```

### Skill Check
```csharp
int skillBonus = DndMath.GetAbilityModifier(relevantAbility) + 
                 (isProficient ? DndMath.GetProficiencyBonus(level) : 0);
int skillCheck = Dice.Roll(20) + skillBonus;
```

### Saving Throw
```csharp
int saveBonus = DndMath.GetAbilityModifier(abilityScore) +
                (isProficient ? DndMath.GetProficiencyBonus(level) : 0);
int savingThrow = Dice.Roll(20) + saveBonus;
```

## Comparison: Before vs After

### Before (Incorrect)
```csharp
// ? Wrong - could round incorrectly for negative numbers
int modifier = (abilityScore - 10) / 2;

// ? Wrong - Math.Round would round 0.5 up
int halfDamage = (int)Math.Round(damage / 2.0);

// ? Wrong - inconsistent rounding
int profBonus = 2 + (level - 1) / 4;
```

### After (Correct)
```csharp
// ? Correct - always rounds down per D&D rules
int modifier = DndMath.GetAbilityModifier(abilityScore);

// ? Correct - always rounds down
int halfDamage = DndMath.Half(damage);

// ? Correct - uses DndMath internally
int profBonus = DndMath.GetProficiencyBonus(level);
```

## Real Combat Example

```csharp
// Fighter attacks with longsword
Character fighter = new Character 
{ 
    Strength = 16, 
    Level = 5,
    // ... other properties
};

// Calculate attack bonus
int strMod = fighter.GetAbilityModifier(fighter.Strength);      // +3
int profBonus = fighter.ProficiencyBonus;                        // +3
int attackBonus = strMod + profBonus;                            // +6

// Roll attack
int attackRoll = Dice.Roll(20);
int totalAttack = attackRoll + attackBonus;

// Roll damage
var damageRoll = Dice.RollNotation("1d8+3");  // Longsword + STR
int damage = damageRoll.Total;

// Enemy has resistance to slashing
int finalDamage = DndMath.Half(damage);  // Always rounds down!

Console.WriteLine($"Attack: {attackRoll} + {attackBonus} = {totalAttack}");
Console.WriteLine($"Damage: {damage} ? {finalDamage} (after resistance)");
```

## Movement Example

```csharp
Creature goblin = Creature.CreateGoblin(0, 0);
goblin.Speed = 30;  // 30 feet

// Calculate how many 5-foot tiles the goblin can move
int maxTiles = DndMath.GetUnits(goblin.Speed, 5);  // 6 tiles

// If goblin had 27 speed (from difficult terrain penalty)
goblin.Speed = 27;
maxTiles = DndMath.GetUnits(goblin.Speed, 5);  // 5 tiles (not 5.4!)
```

## Remember

- **Always use DndMath** for any division in game mechanics
- **Never use Math.Round** - D&D always rounds down
- **Document edge cases** - explain why you're rounding in comments
- **Test with fractions** - especially 0.5 cases (should round down!)

## Quick Test

Run these to verify correct behavior:
```csharp
Console.WriteLine(DndMath.GetAbilityModifier(15));  // Should be 2
Console.WriteLine(DndMath.GetAbilityModifier(9));   // Should be -1
Console.WriteLine(DndMath.Half(11));                 // Should be 5
Console.WriteLine(DndMath.GetUnits(27, 5));         // Should be 5
Console.WriteLine(DndMath.GetProficiencyBonus(7));  // Should be 3
```
