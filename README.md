# 4DND - D&D 5e Character Manager & Combat System

A MonoGame-based D&D 5th Edition character management and tactical combat system with an isometric grid view.

## Features

### Character Creation
- **Full D&D 5e Racial System**: Choose from 7 races with proper subraces:
  - Human (Versatile - +1 to all abilities)
  - High Elf (+2 DEX, +1 INT, 30 ft speed)
  - Wood Elf (+2 DEX, +1 WIS, 35 ft speed)
  - Hill Dwarf (+2 CON, +1 WIS, 25 ft speed)
  - Mountain Dwarf (+2 STR, +2 CON, 25 ft speed)
  - Lightfoot Halfling (+2 DEX, +1 CHA, 25 ft speed)
  - Stout Halfling (+2 DEX, +1 CON, 25 ft speed)

- **Classes**: Warrior, Mage, Rogue (each with unique proficiencies and starting equipment)

- **Ability Score Rolling**: Standard 4d6 drop lowest method with reroll option

### Inventory & Equipment System
- **Weapons**: Simple and martial weapons with proper damage dice
  - Longsword, Greatsword, Rapier, Dagger, Club, Quarterstaff, Shortbow, etc.
  - Finesse weapons use DEX for attack rolls
  - Versatile weapons have alternate damage when two-handed

- **Armor**: Light, Medium, and Heavy armor
  - Leather, Studded Leather, Chain Shirt, Chain Mail, Plate, etc.
  - Proper AC calculations with DEX modifiers
  - Stealth disadvantage on heavy armor

- **Starting Equipment**: Each class receives appropriate starting gear
  - Warriors: Longsword, Chain Mail, Shield
  - Mages: Quarterstaff, Dagger
  - Rogues: Rapier, Shortbow, Leather Armor, Daggers

### Combat System
- **Turn-Based Tactical Combat**
  - Initiative system with DEX modifiers
  - Grid-based movement (5 feet per tile)
  - Attack rolls vs AC
  - Critical hits (nat 20) and critical misses (nat 1)
  - Damage rolls with proper dice (1d6, 2d4, etc.)

- **Enemies**: Goblins, Orcs, Skeletons, Wolves
  - Each with unique stats and abilities
  - AI movement and attacking

- **Combat UI**
  - Initiative tracker
  - Health bars
  - Combat log showing attack results
  - Visual feedback for hits and misses

### Character Sheet
- Full D&D 5e character sheet display
  - Ability scores with modifiers
  - Skills with proficiency tracking
  - Saving throws
  - Hit points and hit dice
  - Death saves
  - Equipment display
  - Scrollable for all content

### Persistence
- JSON-based character save system
- Multiple character support
- Auto-save after combat

## Controls

### Main Menu
- **Arrow Keys** / **Mouse**: Navigate menu
- **Enter** / **Click**: Select option

### Character Creation
- **Type**: Enter character name
- **Arrow Keys**: Select race and class
- **R**: Reroll abilities
- **Enter**: Confirm and create character
- **Esc**: Cancel

### Gameplay
- **WASD** / **Arrow Keys**: Pan camera
- **Mouse Wheel**: Zoom in/out
- **Q/E**: Rotate camera
- **C**: Toggle character sheet
- **Tab**: Start/Toggle combat
- **Esc**: Pause menu

### Combat
- **1**: Select Move action
- **2**: Select Attack action
- **3**: End turn
- **Click on Grid**: Execute selected action
  - Move: Click empty tile to move
  - Attack: Click enemy to attack

## Technical Details

### File Structure
- `Game1.cs`: Main game loop and state management
- `Character.cs`: Character data model
- `CharacterCreation.cs`: Character creation wizard
- `CharacterSheet.cs`: Character sheet rendering
- `Race.cs`: Racial data and bonuses
- `Item.cs`: Item properties
- `ItemDatabase.cs`: All available items
- `Inventory.cs`: Inventory management and AC calculation
- `Creature.cs`: Combat creature representation
- `CombatManager.cs`: Turn-based combat system
- `InfiniteGrid.cs`: Grid data structure

### D&D 5e Mechanics Implemented
- ? Ability scores (3-18+)
- ? Ability modifiers: (score - 10) / 2
- ? Proficiency bonus: 2 + (level - 1) / 4
- ? Skill checks: ability modifier + proficiency bonus (if proficient)
- ? Saving throws: ability modifier + proficiency bonus (if proficient)
- ? Attack rolls: d20 + ability modifier + proficiency bonus
- ? Damage rolls: weapon dice + ability modifier
- ? Critical hits: double damage dice on natural 20
- ? Armor Class: base AC + DEX modifier (with armor limits)
- ? Hit Points: class hit die + CON modifier
- ? Initiative: d20 + DEX modifier
- ? Movement: 5 feet per grid square

## Future Enhancements
- Spellcasting system
- More character classes and subclasses
- Leveling and XP progression
- Treasure and loot system
- Dungeon generation
- More enemy types
- Status effects and conditions
- Ranged combat
- Area-of-effect spells
- Multiple save slots
- Character portraits

## Requirements
- .NET 6.0 or higher
- MonoGame Framework
- DefaultFont.xnb in Content directory (build with MonoGame Pipeline Tool)

## Building & Running
1. Open the project in Visual Studio or Rider
2. Build the Content.mgcb file using MonoGame Pipeline Tool
3. Build and run the project

## Credits
Built with MonoGame Framework
Based on D&D 5th Edition SRD rules
