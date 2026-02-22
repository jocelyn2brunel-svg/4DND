# D20 Check System

This document explains the D20 check system implementation in 4DND, which follows the official D&D 5e rules.

## The Three-Step Process

All d20 checks in D&D 5e (ability checks, attack rolls, and saving throws) follow these three steps:

### 1. Roll the die and add a modifier
- Roll a d20
- Add the relevant modifier:
  - **Ability checks & saving throws**: Ability modifier (+ proficiency bonus if proficient)
  - **Attack rolls**: Attack bonus (ability modifier + proficiency bonus if proficient)

### 2. Apply circumstantial bonuses/penalties
- Apply any additional bonuses or penalties from spells, features, or circumstances
- Apply advantage or disadvantage:
  - **Advantage**: Roll two d20s and use the higher result
  - **Disadvantage**: Roll two d20s and use the lower result
  - If you have both advantage and disadvantage, they cancel out (roll normally)

### 3. Compare the total to a target number
- The DM sets a **Difficulty Class (DC)** for ability checks and saving throws
- For attack rolls, compare to the target's **Armor Class (AC)**
- If your total equals or exceeds the target number, the check succeeds

## Difficulty Classes

Standard DC values:

| Task Difficulty | DC |
|----------------|-----|
| Very Easy | 5 |
| Easy | 10 |
| Medium | 15 |
| Hard | 20 |
| Very Hard | 25 |
| Nearly Impossible | 30 |

These are available as constants in `DndMath.DifficultyClass`.

## Using the D20Check System

### For Characters

```csharp
// Make an ability check
var strengthCheck = character.MakeAbilityCheck("Strength", DndMath.DifficultyClass.Medium);
Console.WriteLine(strengthCheck.GetDetailedMessage());

// Make a skill check
var stealthCheck = character.MakeSkillCheck("Stealth", DndMath.DifficultyClass.Hard, hasAdvantage: true);

// Make a saving throw
var dexSave = character.MakeSavingThrow("Dexterity", 15, hasDisadvantage: true);
```

### For Creatures

```csharp
// Make an ability check
var wisdomCheck = creature.MakeAbilityCheck("Wisdom", 12);

// Make a saving throw
var conSave = creature.MakeSavingThrow("Constitution", 18);
```

### Custom D20 Checks

```csharp
// Create a custom attack roll
var attackRoll = D20CheckFactory.MakeAttackRoll(
    "Longsword",
    attackBonus: 5,
    targetAC: 15,
    hasAdvantage: true,
    hasDisadvantage: false,
    circumstantialBonus: 2
);

if (attackRoll.Success)
{
    Console.WriteLine(attackRoll.GetDetailedMessage());
}
```

## D20Check Properties

- `DieRoll`: The d20 roll result (after applying advantage/disadvantage)
- `BaseModifier`: The base modifier (ability modifier + proficiency if applicable)
- `CircumstantialBonus`: Additional bonuses or penalties
- `Total`: The final total (DieRoll + BaseModifier + CircumstantialBonus)
- `TargetNumber`: The DC or AC to meet
- `Success`: Whether the check succeeded (Total >= TargetNumber)
- `HasAdvantage`: Whether advantage was applied
- `HasDisadvantage`: Whether disadvantage was applied
- `IsNaturalOne`: Whether a natural 1 was rolled
- `IsNaturalTwenty`: Whether a natural 20 was rolled
- `IsCriticalHit`: Whether this is a critical hit (natural 20 on attack roll)
- `IsCriticalMiss`: Whether this is a critical miss (natural 1 on attack roll)

## Special Rules

### Natural 1 and Natural 20
- **Attack Rolls**: Natural 1 = automatic miss, Natural 20 = automatic hit (critical hit)
- **Ability Checks & Saving Throws**: Natural 1 or 20 don't have special rules (just add to total as normal)

### Critical Hits
- Only apply to attack rolls
- Roll all damage dice twice
- Add modifiers only once

### Advantage and Disadvantage
- Roll two d20s and take the higher (advantage) or lower (disadvantage)
- Multiple sources of advantage don't stack (same for disadvantage)
- If you have both advantage and disadvantage, they cancel out regardless of how many sources

## Integration with Combat System

The combat system automatically uses D20Checks for attack rolls, including:
- Advantage from target conditions (prone, paralyzed, unconscious)
- Disadvantage from attacker conditions (blinded) or target invisibility
- Disadvantage from sunlight sensitivity in bright light
- Critical hit on natural 20
- Automatic miss on natural 1

## Integration with Vision System

The vision system affects skill checks:
- Perception checks have disadvantage in dim light or lightly obscured areas
- Perception automatically fails in heavily obscured areas (when applicable)
- Attack rolls have disadvantage when the attacker is blinded
- Attack rolls have advantage when the target is invisible to the attacker

## Examples

### Breaking down a door (Strength check)
```csharp
var check = character.MakeAbilityCheck("Strength", DndMath.DifficultyClass.Hard);
// Output: "Strength Check: 15 + 3 = 18 vs DC 20 - FAILURE"
```

### Sneaking past guards (Stealth check with advantage)
```csharp
var check = character.MakeSkillCheck("Stealth", 14, hasAdvantage: true);
// Output: "Stealth [ADV: 18/12]: 18 + 5 = 23 vs DC 14 - SUCCESS"
```

### Dodging a fireball (Dexterity saving throw)
```csharp
var save = character.MakeSavingThrow("Dexterity", 15);
// Output: "Dexterity Save: 10 + 2 = 12 vs DC 15 - FAILURE"
```

### Attack roll with advantage
```csharp
var attack = D20CheckFactory.MakeAttackRoll("Longsword", 5, 16, hasAdvantage: true);
// Output: "Longsword Attack [ADV: 17/8]: 17 + 5 = 22 vs AC 16 - SUCCESS"
```
