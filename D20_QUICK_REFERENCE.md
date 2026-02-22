# D20 System Quick Reference

## Standard Difficulty Classes

| Difficulty | DC | Example Tasks |
|------------|----|--------------| 
| Very Easy | 5 | Notice something large in plain sight |
| Easy | 10 | Climb a rope, recognize a common person |
| Medium | 15 | Pick a simple lock, climb a rough surface |
| Hard | 20 | Swim in stormy water, climb a slippery wall |
| Very Hard | 25 | Convince a hostile guard, track an expert |
| Nearly Impossible | 30 | Jump an impossible chasm, recall obscure lore |

## Quick Usage

### In Code
```csharp
// Ability Check
var check = character.MakeAbilityCheck("Strength", DndMath.DifficultyClass.Medium);

// Skill Check  
var check = character.MakeSkillCheck("Stealth", 15, hasAdvantage: true);

// Saving Throw
var save = character.MakeSavingThrow("Dexterity", 18);

// Attack Roll (handled automatically in combat)
var attack = D20CheckFactory.MakeAttackRoll("Longsword", 5, targetAC, hasAdvantage);
```

### In Game (Test Keys)
- **X**: Strength check (DC 15)
- **Z**: Stealth check (DC 20)
- **N**: Dexterity save (DC 20)

Results appear in combat log and console.

## D20 Check Properties

```csharp
check.DieRoll          // The d20 result (1-20)
check.BaseModifier     // Ability mod + proficiency
check.CircumstantialBonus  // Additional bonuses
check.Total            // Final total
check.TargetNumber     // DC or AC
check.Success          // true if Total >= TargetNumber
check.IsNaturalOne     // Rolled a 1
check.IsNaturalTwenty  // Rolled a 20
check.IsCriticalHit    // Natural 20 on attack
check.IsCriticalMiss   // Natural 1 on attack

// Display results
check.GetSimpleMessage()   // Short message
check.GetDetailedMessage() // Full breakdown
```

## Advantage & Disadvantage

### How It Works
- **Advantage**: Roll 2d20, take higher
- **Disadvantage**: Roll 2d20, take lower
- **Both**: They cancel out, roll 1d20

### Common Sources
**Advantage:**
- Prone target (melee)
- Paralyzed/unconscious target
- Help action from ally
- Invisibility (attacking visible foes)

**Disadvantage:**
- Blinded attacker
- Prone attacker (ranged)
- Poisoned condition
- Dim light/darkness (without darkvision)
- Sunlight sensitivity (in bright light)

## Critical Hits (Attack Rolls Only)

- **Natural 20**: Automatic hit, roll all damage dice twice
- **Natural 1**: Automatic miss (regardless of bonuses)

## Modifiers

### Ability Modifier
```
(Ability Score - 10) / 2 (round down)
```

### Proficiency Bonus
```
2 + (Level - 1) / 4 (round down)
```

| Level | Bonus |
|-------|-------|
| 1-4   | +2 |
| 5-8   | +3 |
| 9-12  | +4 |
| 13-16 | +5 |
| 17-20 | +6 |

### Skill Bonus
```
Ability Modifier + (Proficiency Bonus if proficient)
```

### Saving Throw Bonus
```
Ability Modifier + (Proficiency Bonus if proficient)
```

## The Three Steps (from PHB)

1. **Roll the die and add a modifier**
   - Roll d20
   - Add ability modifier (and proficiency if applicable)

2. **Apply circumstantial bonuses/penalties**
   - Spells (Bless, Guidance, etc.)
   - Features (Bardic Inspiration, etc.)
   - Advantage/Disadvantage

3. **Compare the total to a target number**
   - If Total >= DC/AC: Success
   - If Total < DC/AC: Failure

## Success and Failure

The DM determines the DC based on:
- Task difficulty
- Circumstances
- Character preparation

A check succeeds if:
```
Total = DieRoll + BaseModifier + CircumstantialBonus >= DC
```

## Examples with Breakdown

### Example 1: Picking a Lock (Medium Difficulty)
- **Character**: Rogue with Dexterity 18 (+4), Level 3 (+2 proficiency), Thieves' Tools proficient
- **Task**: Pick a standard lock
- **DC**: 15 (Medium)
- **Roll**: d20 = 12
- **Calculation**: 12 (roll) + 4 (Dex) + 2 (proficiency) = 18
- **Result**: 18 >= 15 = SUCCESS

### Example 2: Dodging Fireball (with Advantage)
- **Character**: Monk with Dexterity 16 (+3), Level 5 (+3 proficiency), Dex save proficient
- **Task**: Dodge a Fireball spell
- **DC**: 15
- **Special**: Monk has Evasion (advantage on Dex saves)
- **Roll**: 2d20 = 8 and 14, take 14 (advantage)
- **Calculation**: 14 (roll) + 3 (Dex) + 3 (proficiency) = 20
- **Result**: 20 >= 15 = SUCCESS (take half damage from Evasion feature)

### Example 3: Attacking a Prone Enemy
- **Attacker**: Fighter with Strength 16 (+3), Level 4 (+2 proficiency), longsword proficient
- **Target**: Orc with AC 13, currently prone
- **Special**: Melee attacks against prone targets have advantage
- **Roll**: 2d20 = 5 and 17, take 17 (advantage)
- **Calculation**: 17 (roll) + 3 (Str) + 2 (proficiency) = 22
- **Result**: 22 >= 13 = HIT

### Example 4: Attacking While Blinded (with Disadvantage)
- **Attacker**: Ranger with Dexterity 14 (+2), Level 3 (+2 proficiency), blinded condition
- **Target**: Goblin with AC 15
- **Special**: Attacks while blinded have disadvantage
- **Roll**: 2d20 = 16 and 7, take 7 (disadvantage)
- **Calculation**: 7 (roll) + 2 (Dex) + 2 (proficiency) = 11
- **Result**: 11 < 15 = MISS

## Implementation Notes

### Why This System?
The D20Check system follows the official D&D 5e rules exactly:
1. It's the same system used in the Player's Handbook
2. It handles all edge cases (advantage/disadvantage, critical hits, etc.)
3. It's extensible for future features (spells, conditions, etc.)
4. It provides detailed feedback for players

### Backward Compatibility
- The old `SkillCheck` class still works (it internally uses D20Check)
- Existing code using `CombatManager.MakeAttack()` works without changes
- Attack rolls automatically use the new system

### Performance
- D20 checks are lightweight (just a few random rolls and arithmetic)
- No noticeable performance impact
- Results can be cached if needed

## Related Files

- **`D20Check.cs`**: Core implementation
- **`D20CheckExamples.cs`**: 10 detailed examples
- **`GUIDE_D20_CHECKS.md`**: Complete documentation
- **`INTEGRATION_D20_CHECKS.md`**: Integration guide
- **`DndMath.cs`**: DC constants and helper methods
- **`CombatManager.cs`**: Attack roll integration
- **`SkillCheck.cs`**: Backward compatibility wrapper
