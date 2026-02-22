# Changelog - 4DND Combat & Inventory Update

## Version 2.0 - Combat & Equipment System

### New Features

#### 1. Racial Subraces System
**Files Added:**
- `Race.cs` - Complete racial data system

**Changes:**
- Expanded from 3 basic races to 7 detailed subraces
- Proper D&D 5e racial ability bonuses:
  - Human: +1 to all abilities
  - High Elf: +2 DEX, +1 INT
  - Wood Elf: +2 DEX, +1 WIS, 35 ft speed
  - Hill Dwarf: +2 CON, +1 WIS, 25 ft speed
  - Mountain Dwarf: +2 STR, +2 CON, 25 ft speed
  - Lightfoot Halfling: +2 DEX, +1 CHA, 25 ft speed
  - Stout Halfling: +2 DEX, +1 CON, 25 ft speed
- Race-specific base movement speeds
- Scrollable race selection UI in character creation

**Modified Files:**
- `CharacterCreation.cs` - Updated to use new Race system with scrolling UI
- `Character.cs` - Integrated with Race system for speed calculation

#### 2. Inventory & Equipment System
**Files Added:**
- `Item.cs` - Item data structure with weapon/armor properties
- `ItemDatabase.cs` - Database of all available items (20+ items)
- `Inventory.cs` - Inventory management with equipment slots

**Items Added:**
**Weapons:**
- Simple: Club, Dagger, Quarterstaff, Shortbow
- Martial: Longsword, Greatsword, Rapier

**Armor:**
- Light: Leather Armor, Studded Leather
- Medium: Hide Armor, Chain Shirt
- Heavy: Ring Mail, Chain Mail, Plate Armor

**Other:**
- Shields
- Healing Potions

**Features:**
- Equipment slots (Weapon, Armor, Shield)
- Automatic AC calculation based on equipped armor
- DEX modifier limits based on armor type
- Weight tracking
- Equip/unequip functionality
- Starting equipment for each class

**Modified Files:**
- `Character.cs` - Added `Inventory InventoryData` property and AC calculation
- `CharacterCreation.cs` - Give appropriate starting equipment to new characters
- `CharacterSheet.cs` - Display equipped items and inventory in right column

#### 3. Turn-Based Combat System
**Files Added:**
- `Creature.cs` - Combat creature representation for player and monsters
- `CombatManager.cs` - Complete turn-based combat system with AI

**Combat Features:**
- Initiative system with DEX modifiers
- Turn order display
- Player actions: Move, Attack, End Turn
- Grid-based movement (5 feet per tile)
- Attack rolls: d20 + modifiers vs AC
- Damage rolls with proper dice parsing (1d6, 2d4, etc.)
- Critical hits (natural 20) - double damage dice
- Critical misses (natural 1)
- Health tracking and death
- Combat log showing all actions

**Enemy Types:**
- Goblins (HP: 7, AC: 15, Speed: 30)
- Orcs (HP: 15, AC: 13, Speed: 30)
- Skeletons (HP: 13, AC: 13, Speed: 30)
- Wolves (HP: 11, AC: 13, Speed: 40)

**AI System:**
- Enemies move towards player
- Attack when in melee range
- Turn management

**Modified Files:**
- `Game1.cs` - Major additions:
  - Added `_combatManager`, `_playerCreature`, `_combatLog`
  - Combat state management
  - Tab key to start combat
  - Player combat controls (1, 2, 3 keys)
  - AI turn processing
  - Combat UI rendering
  - Creature visualization on grid
  - Health bars and names
  - Combat panel with initiative tracker
  - Combat log display

#### 4. Visual Improvements
- Creatures rendered as colored circles on grid
- Health bars above creatures
- Creature names displayed
- Combat UI panel showing:
  - Current turn
  - Initiative order
  - Available actions
  - Combat log (last 5 messages)
  - Instructions
- Equipment display on character sheet

### Technical Improvements
- Added LINQ using statement to Game1.cs
- Improved character creation UI with scrolling
- Better state management for combat
- Proper integration between Character and Creature
- JSON serialization support for Inventory

### Controls Added
- **Tab**: Start/Toggle combat mode
- **1**: Select Move action
- **2**: Select Attack action
- **3**: End Turn
- **Click**: Execute selected action (move/attack)

### Bug Fixes
- Fixed AC calculation to use equipped armor
- Fixed speed to use racial base speed
- Fixed racial bonuses to match D&D 5e rules

### Balance Changes
- Warrior starting AC: 10 + DEX ? 18 (Chain Mail + Shield)
- Mage starting AC: 10 + DEX ? 10 + DEX (no armor)
- Rogue starting AC: 11 + DEX ? 11 + DEX (Leather Armor)
- All classes now have appropriate starting equipment

### Known Issues
- Combat is basic (no ranged attacks, no spells, no opportunity attacks)
- AI is simplistic (always moves toward player)
- No inventory UI for managing items during gameplay
- No equipment weight encumbrance
- No ability to loot enemies

### Next Steps (Not Implemented)
- Spellcasting system
- Ranged combat with distance calculations
- Inventory management UI
- Loot system
- More complex AI behaviors
- Status effects and conditions
- Multiple save slots with individual character saves
