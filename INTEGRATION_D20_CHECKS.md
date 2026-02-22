# D20 Check System Integration

This document explains how the D20 check system is integrated into the 4DND game.

## Key Components

### 1. Core Classes
- **`D20Check`**: Represents the result of any d20 roll (ability check, attack roll, or saving throw)
- **`D20CheckFactory`**: Factory methods for creating different types of d20 checks
- **`DndMath.DifficultyClass`**: Standard DC constants (Very Easy = 5, Easy = 10, Medium = 15, Hard = 20, Very Hard = 25, Nearly Impossible = 30)

### 2. Extension Methods
Extension methods make it easy to perform checks on characters and creatures:

```csharp
// Character extensions
character.MakeAbilityCheck(abilityName, dc, advantage, disadvantage, bonus)
character.MakeSkillCheck(skillName, dc, advantage, disadvantage, bonus)
character.MakeSavingThrow(abilityName, dc, advantage, disadvantage, bonus)

// Creature extensions
creature.MakeAbilityCheck(abilityName, dc, advantage, disadvantage, bonus)
creature.MakeSavingThrow(abilityName, dc, advantage, disadvantage, bonus)
```

## Combat System Integration

### Attack Rolls
The `CombatManager.MakeAttack()` method has been updated to use the D20Check system:

```csharp
public AttackResult MakeAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
{
    // Determine advantage/disadvantage from conditions
    bool hasAdvantage = target.Conditions.HasCondition(Condition.Prone) || ...;
    bool hasDisadvantage = attacker.IsBlinded() || ...;
    
    // Make attack roll using D20Check
    var attackCheck = D20CheckFactory.MakeAttackRoll(
        attacker.AttackName,
        attacker.AttackBonus,
        target.ArmorClass,
        hasAdvantage,
        hasDisadvantage
    );
    
    // Critical hits, critical misses, and normal hits are all handled automatically
    if (attackCheck.IsCriticalHit) { /* double damage dice */ }
    if (attackCheck.Success) { /* deal damage */ }
    
    return result;
}
```

**Automatic Features:**
- Natural 1 = automatic miss
- Natural 20 = automatic hit (critical hit, double damage dice)
- Advantage/disadvantage from conditions (prone, blinded, paralyzed, etc.)
- Disadvantage from sunlight sensitivity in bright light
- All results are logged to combat log

### Saving Throws
Creatures can now make saving throws against spells and effects:

```csharp
// Player makes a Dexterity save against Fireball (DC 15)
var save = playerCreature.MakeSavingThrow("Dexterity", 15);

if (save.Success)
{
    // Take half damage
    int halfDamage = DndMath.Half(totalDamage);
    playerCreature.TakeDamage(halfDamage);
}
else
{
    // Take full damage
    playerCreature.TakeDamage(totalDamage);
}
```

## Vision System Integration

The vision system affects d20 checks:

### Perception Checks
The `VisionSkillChecks.MakePerceptionCheck()` method automatically applies:
- **Disadvantage** in dim light (lightly obscured)
- **Auto-fail** in darkness without darkvision (heavily obscured)

```csharp
// Make a perception check considering lighting conditions
var check = VisionSkillChecks.MakePerceptionCheck(
    character, 
    visionSystem, 
    x, y, z, 
    DndMath.DifficultyClass.Medium
);
```

### Attack Rolls
Attack rolls automatically consider vision:
- **Disadvantage** when attacker is blinded
- **Advantage** when target is invisible (but attacker can somehow detect them)
- **Disadvantage** from sunlight sensitivity in bright light

## Testing the D20 System

The game includes test keybindings to demonstrate the D20 check system:

| Key | Action | Description |
|-----|--------|-------------|
| X | Strength Check | Makes a Strength ability check vs DC 15 |
| Z | Stealth Check | Makes a Stealth skill check vs DC 20 |
| N | Dexterity Save | Makes a Dexterity saving throw vs DC 20 |

Results are displayed in the combat log and console output.

### Example Test Sequence
1. Start the game and create/select a character
2. Enter a campaign (single player mode)
3. Press Tab to start combat
4. Press X to make a Strength check
   - Check the combat log for the result
   - Check console for detailed breakdown
5. Press Z to make a Stealth check
6. Press N to make a saving throw

## Saving Throw Proficiencies

### Characters
Character saving throw proficiencies are determined by their class:
- **Fighter**: Strength, Constitution
- **Rogue**: Dexterity, Intelligence
- **Wizard**: Intelligence, Wisdom
- **Cleric**: Wisdom, Charisma
- etc.

### Monsters
Monster saving throw proficiencies are defined in their stat blocks:
- **Goblin**: None
- **Orc**: None
- **Couatl**: Wisdom +5, Charisma +5
- etc.

## Using D20 Checks in Your Code

### Example 1: Breaking Down a Door
```csharp
if (_currentCharacter != null)
{
    var check = _currentCharacter.MakeAbilityCheck("Strength", DndMath.DifficultyClass.Hard);
    
    if (check.Success)
    {
        AddToCombatLog("You break down the door!");
        // Open the door
    }
    else
    {
        AddToCombatLog(check.GetSimpleMessage());
    }
}
```

### Example 2: Sneaking Past Guards
```csharp
// Check if in dim light (apply disadvantage)
bool hasDisadvantage = visionSystem.IsLightlyObscured(x, y, playerCreature);

var check = character.MakeSkillCheck("Stealth", guardPerceptionDC, hasDisadvantage: hasDisadvantage);

if (check.Success)
{
    AddToCombatLog("You sneak past unnoticed!");
}
else
{
    AddToCombatLog("A guard spots you!");
    StartCombat();
}
```

### Example 3: Dodging a Trap
```csharp
// Dexterity save to dodge spikes (DC 15)
if (_playerCreature != null)
{
    var save = _playerCreature.MakeSavingThrow("Dexterity", 15);
    
    if (save.Success)
    {
        AddToCombatLog("You dodge the spikes!");
    }
    else
    {
        int damage = Dice.RollNotation("2d6").Total;
        _playerCreature.TakeDamage(damage);
        AddToCombatLog($"You take {damage} damage from spikes! {save.GetSimpleMessage()}");
    }
}
```

### Example 4: Spell Save DC
When implementing spells, calculate the spell save DC:
```csharp
// Spell Save DC = 8 + proficiency bonus + spellcasting ability modifier
int spellSaveDC = 8 + character.ProficiencyBonus + character.GetAbilityModifier(character.Intelligence);

// Target makes a saving throw
var save = target.MakeSavingThrow("Constitution", spellSaveDC);

if (!save.Success)
{
    // Apply spell effect
}
```

## Advantage and Disadvantage Sources

### Common Sources of Advantage
- Target is prone (melee attacks)
- Target is paralyzed, unconscious, or restrained
- Attacking an invisible creature you can detect
- Help action from ally
- Situational bonuses (flanking, higher ground, etc.)

### Common Sources of Disadvantage
- Attacker is blinded
- Attacker is poisoned
- Attacker is prone (ranged attacks)
- Attacking in dim light or darkness (without darkvision)
- Attacking while restrained
- Sunlight sensitivity in bright light
- Target is invisible and you can't detect them

### Cancellation
If you have both advantage and disadvantage (from any number of sources):
- They cancel out completely
- Roll one d20 normally
- This is automatic in the D20Check system

## Future Enhancements

The D20Check system is designed to be extensible for future features:

1. **Bardic Inspiration**: Add as circumstantial bonus
2. **Bless/Bane Spells**: Add/subtract 1d4 as circumstantial bonus
3. **Guidance Spell**: Add 1d4 to ability checks
4. **Cover System**: Apply circumstantial bonuses to AC
5. **Expertise**: Double proficiency bonus for certain skills
6. **Jack of All Trades**: Add half proficiency to unproficient checks
7. **Reliable Talent**: Treat rolls below 10 as 10 for proficient skills

All of these can be implemented by modifying the circumstantialBonus parameter or extending the D20Check class.
