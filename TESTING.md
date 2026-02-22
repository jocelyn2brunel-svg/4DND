# Quick Start Guide - Testing New Features

## Testing the Racial Subraces

1. Launch the game
2. Select "New Game" from main menu
3. Enter a character name
4. **Race Selection**: 
   - Use arrow keys to scroll through all 7 races
   - Notice each race shows its bonuses in the description
   - Try creating characters with different races to see bonus differences
5. Select a class and confirm
6. On ability screen, note the rolled base stats
7. After creation, press **C** to open character sheet
8. Check the ability scores - they should include racial bonuses

### Expected Racial Bonuses:
- **Human**: All abilities +1
- **High Elf**: DEX +2, INT +1
- **Wood Elf**: DEX +2, WIS +1, Speed 35
- **Hill Dwarf**: CON +2, WIS +1, Speed 25
- **Mountain Dwarf**: STR +2, CON +2, Speed 25
- **Lightfoot Halfling**: DEX +2, CHA +1, Speed 25
- **Stout Halfling**: DEX +2, CON +1, Speed 25

## Testing the Equipment System

1. Create a new character of each class
2. Press **C** to open character sheet
3. Look at the "EQUIPMENT & INVENTORY" section (top right)

### Expected Starting Equipment:

**Warrior:**
- Weapon: Longsword (1d8/1d10 versatile)
- Armor: Chain Mail (AC 16)
- Shield: Shield (+2 AC)
- **Total AC should be 18**

**Mage:**
- Weapon: Quarterstaff (1d6/1d8 versatile)
- Additional: Dagger (1d4)
- **AC should be 10 + DEX modifier**

**Rogue:**
- Weapon: Rapier (1d8, finesse)
- Ranged: Shortbow (1d6)
- Armor: Leather Armor (AC 11)
- Additional: 2x Daggers
- **AC should be 11 + DEX modifier**

## Testing Combat System

### Starting Combat:
1. Create a character and start playing
2. Press **Tab** to initiate combat
3. 2-3 random enemies will spawn nearby
4. Combat UI panel appears at the top

### Combat Actions:

**Moving:**
1. Press **1** to select Move action
2. Click on an empty grid tile within range
3. Character moves to that tile
4. Movement range = Speed ÷ 5 (e.g., 30 speed = 6 tiles)

**Attacking:**
1. Press **2** to select Attack action
2. Click on an enemy creature (colored circle)
3. Watch the combat log for attack roll and damage
4. Enemy health bar updates

**Ending Turn:**
1. Press **3** to end your turn
2. AI enemies take their turns automatically
3. Combat continues until all enemies or player is defeated

### Combat UI Elements:
- **Top left**: Current turn, character HP
- **Initiative order**: Shows turn order with initiative values
- **Combat log**: Last 5 combat actions
- **Creatures**: Colored circles with health bars
  - Blue = Player
  - Green = Goblins
  - Dark Red = Orcs
  - White = Skeletons
  - Gray = Wolves

### Testing Attack Rolls:
- Check combat log for attack rolls
- Natural 20 = Critical Hit (double damage)
- Natural 1 = Critical Miss
- Hit if: Attack Roll + Bonus ? Enemy AC

### Example Combat Sequence:
1. Start combat with Tab
2. Press 2 to attack
3. Click on a Goblin (AC 15)
4. Watch combat log: "Warrior hit Goblin for 8 damage!" or "Warrior missed Goblin (14 vs AC 15)"
5. If hit, goblin's health bar decreases
6. AI enemy turn: Enemy moves toward you or attacks
7. Your turn again
8. Repeat until combat ends

### Winning Combat:
- Defeat all enemies to end combat
- Your character's HP is saved
- Press Tab to hide combat UI
- Character sheet (C) will show updated HP

### Losing Combat:
- If player HP reaches 0, combat ends
- Character HP is saved at 0
- Death saves system is implemented in character sheet but not yet in combat

## Testing Character Sheet

1. Press **C** during gameplay to open character sheet
2. **Mouse wheel** to scroll through content
3. Press **C** again to close

### Check These Sections:
- **Header**: Name, Class & Level, Race, Alignment, XP
- **Left Column**: 
  - All 6 ability scores with modifiers
  - Saving throw proficiencies (marked with checkbox)
  - Proficiency bonus
  - Passive Perception
- **Middle Column**:
  - AC (should match equipped armor)
  - Initiative modifier
  - Speed (should match race)
  - Current/Max HP
  - Hit Dice
  - Death Saves
  - All 18 skills with bonuses
- **Right Column**:
  - Equipment & Inventory (NEW!)
  - Shows equipped weapon, armor, shield
  - Lists inventory items
  - Shows total weight

## Common Issues & Solutions

### "No enemies appear"
- Enemies spawn when you press Tab to start combat
- Look for colored circles on the grid near the origin

### "Can't attack enemy"
- Make sure you pressed **2** to select Attack action
- Click directly on the enemy creature (colored circle)
- Enemy must be within your weapon's range (melee = adjacent tile)

### "Character sheet shows wrong AC"
- AC is calculated from equipped armor
- Check Equipment section to see what's equipped
- Formula: Armor AC + min(DEX mod, armor max DEX) + shield AC

### "Can't see combat UI"
- Press Tab to toggle combat UI on/off
- Combat UI only shows when combat is active

### "Race bonuses not applied"
- Racial bonuses are added during character creation
- Create a new character to test
- Check character sheet ability scores (not just rolled scores)

## Advanced Testing

### Test Different Race/Class Combinations:
- **Mountain Dwarf Warrior**: High STR and CON, lower speed
- **Wood Elf Rogue**: High DEX, extra speed, good stealth
- **High Elf Mage**: High INT and DEX, perfect for wizard
- **Human Any Class**: Balanced stats across the board

### Test Combat Scenarios:
1. **Tank Test**: Create Warrior with high AC, survive multiple hits
2. **Damage Test**: Create character with high STR/DEX, deal maximum damage
3. **Speed Test**: Wood Elf can move farther per turn (7 tiles vs 6)
4. **Multiple Enemy Test**: Fight against varied enemy types

### Verify D&D 5e Mechanics:
- Attack roll: d20 + ability mod + proficiency bonus
- Damage: weapon dice + ability mod
- AC: armor base + DEX (limited by armor type)
- Initiative: d20 + DEX mod
- Skills: ability mod + proficiency (if proficient)

## Performance Testing
- Create multiple characters
- Delete characters
- Start/end combat multiple times
- Open/close character sheet frequently
- Zoom and pan camera during combat
