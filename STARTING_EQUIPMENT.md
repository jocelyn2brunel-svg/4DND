# Starting Equipment System

This document describes the starting equipment system implemented for the 4DND game based on D&D 5th Edition rules.

## Overview

When creating a new character, they automatically receive starting equipment based on their chosen class. The system includes:

1. **Class-based Equipment Packages** - Each class gets appropriate starting weapons, armor, and adventuring gear
2. **Starting Gold** - Characters receive starting gold pieces (gp) based on their class
3. **Automatic Equipment** - Equipment is automatically added to inventory and appropriate items are equipped

## Starting Wealth by Class

Characters receive the following starting gold (in addition to their equipment):

| Class     | Starting Gold |
|-----------|---------------|
| Barbarian | 10 gp         |
| Bard      | 15 gp         |
| Cleric    | 15 gp         |
| Druid     | 10 gp         |
| Fighter   | 10 gp         |
| Monk      | 5 gp          |
| Paladin   | 25 gp         |
| Ranger    | 10 gp         |
| Rogue     | 15 gp         |
| Sorcerer  | 10 gp         |
| Warlock   | 10 gp         |
| Wizard    | 10 gp         |

**Note:** The D&D 5e rules provide dice formulas for rolling starting wealth (e.g., Barbarian gets 2d4×10 gp). The current implementation gives average values for simplicity, but the `StartingEquipment.RollStartingWealth()` method is available if you want random starting gold in the future.

## Equipment by Class

### Barbarian
- **Weapons:** Greataxe, 2 Handaxes, 4 Javelins
- **Gear:** Explorer's pack (backpack, bedroll, mess kit, tinderbox, 10 torches, 10 rations, waterskin, rope)

### Bard
- **Weapons:** Rapier, Dagger
- **Armor:** Leather Armor
- **Tools:** Lute (musical instrument)
- **Gear:** Entertainer's pack (backpack, bedroll, 2 costumes, 5 candles, 5 rations, waterskin, disguise kit)

### Cleric
- **Weapons:** Mace, Light Crossbow with 20 bolts
- **Armor:** Scale Mail, Shield
- **Holy Symbol:** Amulet
- **Gear:** Priest's pack (backpack, blanket, 10 candles, tinderbox, alms box, 2 incense, censer, robes, 2 rations, waterskin)

### Druid
- **Weapons:** Scimitar
- **Armor:** Leather Armor, Shield
- **Druidic Focus:** Wooden staff
- **Gear:** Explorer's pack + herbalism kit

### Fighter (Warrior)
- **Weapons:** Longsword, Light Crossbow with 20 bolts
- **Armor:** Chain Mail, Shield
- **Gear:** Dungeoneer's pack (backpack, crowbar, hammer, 10 pitons, 10 torches, tinderbox, 10 rations, waterskin, rope)

### Monk
- **Weapons:** Shortsword, 10 Darts
- **Gear:** Dungeoneer's pack

### Paladin
- **Weapons:** Longsword, 5 Javelins
- **Armor:** Chain Mail, Shield
- **Holy Symbol:** Amulet
- **Gear:** Explorer's pack

### Ranger
- **Weapons:** 2 Shortswords, Longbow with Quiver and 20 Arrows
- **Armor:** Scale Mail
- **Gear:** Explorer's pack

### Rogue
- **Weapons:** Rapier, Shortbow with Quiver and 20 Arrows, 2 Daggers
- **Armor:** Leather Armor
- **Tools:** Thieves' tools
- **Gear:** Burglar's pack (backpack, ball bearings, bell, 5 candles, crowbar, hammer, 10 pitons, hooded lantern, 2 oil flasks, 5 rations, tinderbox, waterskin, rope)

### Sorcerer
- **Weapons:** Light Crossbow with 20 bolts, 2 Daggers
- **Focus:** Component pouch, Crystal (arcane focus)
- **Gear:** Dungeoneer's pack

### Warlock
- **Weapons:** Light Crossbow with 20 bolts, 2 Daggers
- **Armor:** Leather Armor
- **Focus:** Component pouch, Rod (arcane focus)
- **Gear:** Scholar's pack (backpack, book, ink, ink pen, 10 parchment sheets, sack)

### Wizard (Mage)
- **Weapons:** Quarterstaff, Dagger
- **Focus:** Component pouch
- **Spellbook:** Essential for wizards
- **Gear:** Scholar's pack

## Implementation Details

### Files Modified

1. **Character.cs**
   - Added `GoldPieces` property to track character wealth

2. **CharacterCreation.cs**
   - Expanded class list to include all 12 D&D 5e classes
   - Updated `CreateCharacterFromData()` to assign starting equipment via `StartingEquipment` class
   - Added HP values and proficiencies for all classes
   - Added scrolling support for class selection UI

3. **CharacterSheet.cs**
   - Added gold display in equipment section
   - Updated `GetHitDieSize()` to return correct hit die for all classes
   - Updated `GetWeaponDamage()` to read from ItemDatabase

4. **Inventory.cs**
   - Increased capacity from 20 to 50 to accommodate starting equipment

5. **ItemDatabase.cs**
   - Added religious items: Alms box, Incense, Censer

### Files Created

1. **StartingEquipment.cs**
   - Static class that defines starting equipment packages for each class
   - `GetStartingEquipment(className)` - Returns equipment package for a class
   - `RollStartingWealth(className)` - Rolls starting wealth using D&D dice formulas (available for future use)
   - `EquipmentPackage` - Data structure containing items, equipped items, and gold

## Usage

The system is fully automatic. When a player creates a new character:

1. Choose a name, race, and class
2. Roll ability scores
3. Upon confirmation, the character is created with:
   - Appropriate starting equipment in their inventory
   - Key equipment items automatically equipped (weapons, armor, shields)
   - Starting gold pieces added to their wealth

## Viewing Equipment

Players can view their character's equipment and gold by:
- Pressing `C` during gameplay to open the character sheet
- The equipment section shows all inventory items
- Gold is displayed at the bottom of the equipment box
- Equipped items are shown at the top of the equipment list

## Future Enhancements

Potential improvements to consider:

1. **Equipment Choices** - Add a UI step during character creation to let players choose between equipment options (e.g., longsword vs. martial weapon)
2. **Random Wealth** - Option to roll starting wealth instead of using fixed values
3. **Background Equipment** - Add equipment packages based on character backgrounds
4. **Equipment Packs** - Implement named equipment packs (Explorer's Pack, Scholar's Pack, etc.) as single items that expand when added
