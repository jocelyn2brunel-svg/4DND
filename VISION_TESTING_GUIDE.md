# Vision System Testing Guide

## How to Test the Vision System

### Starting Combat
1. Start a new game or continue an existing character
2. Press **Tab** to start combat
   - This will spawn 2-3 random enemies
   - Combat UI will appear at the top
   - Vision system will activate

### Testing Vision Types

#### Normal Vision
- Create a **Human** character (no darkvision)
- In darkness, you cannot see anything (completely black tiles)
- In bright light (near torches/lanterns), you can see normally

#### Darkvision
- Create an **Elf**, **Dwarf**, **Half-Orc**, or **Tiefling** (60 ft darkvision)
- In darkness, you can see in a gray tint up to 60 feet
- In dim light, you can see normally

#### Superior Darkvision
- Create a **Drow** character (120 ft darkvision)
- See twice as far in darkness as normal darkvision
- Note: Drow has Sunlight Sensitivity (disadvantage in bright light)

#### Blindsight
- Wolves have 30 ft blindsight
- They can "see" even in complete darkness without darkvision

### Testing Light Conditions

#### Toggle Daylight
- Press **L** to toggle global daylight on/off
- In daylight: everything is bright light, no fog of war
- At night: only light sources provide illumination

#### Light Sources
- Player automatically carries a torch (20 ft bright, 20 ft dim)
- Torch moves with the player
- Static lanterns spawn around the battlefield (30 ft bright, 30 ft dim)

### Testing Obscurement

#### Fog Cloud (Heavily Obscuring)
- Press **F** to create a Fog Cloud at player's position
- Creates 20 ft radius of heavily obscuring fog
- Blocks line of sight completely
- Creatures inside are blinded

#### Darkness Spell (Magical Darkness)
- Press **K** to cast Darkness at player's position
- Creates 15 ft radius of magical darkness
- Overrides light sources
- Even with darkvision, you see in dim light mode

### Testing Conditions

#### Blinded Condition
- Press **B** to toggle Blinded condition on player
- When blinded:
  - Cannot see anything (all tiles black)
  - Attacks have disadvantage
  - Attacks against you have advantage
  - Red indicator appears on player creature

### Testing Sunlight Sensitivity

#### Drow Character
1. Create a Drow character
2. Toggle daylight on with **L**
3. Try attacking - you'll have disadvantage

#### Kobold Enemy
- Kobolds have sunlight sensitivity
- In bright light, their attacks have disadvantage

### Testing Combat Integration

#### Visibility-Based Gameplay
1. Start combat
2. Turn off vision overlay (**V**) to see the grid normally
3. Turn on vision overlay (**V**) to see fog of war
4. Only visible enemies can be targeted
5. Move around to reveal different areas

#### Advantage/Disadvantage
- Attack while blinded: disadvantage
- Attack target in bright light: normal
- Attack with sunlight sensitivity in daylight: disadvantage
- Check combat log for (ADV) or (DIS) indicators

### UI Elements

#### Vision Legend (Top Right)
Shows color coding:
- White: Bright Light
- Gray: Dim Light (Lightly Obscured)
- Dark Gray: Darkness (using Darkvision)
- Black: Complete Darkness (Heavily Obscured)

#### Vision Info (Bottom Left)
- Shows current vision overlay state
- Shows daylight state
- Shows your light level
- Shows your darkvision range
- Shows active conditions

#### Creature Indicators
- Yellow dot: Normal darkvision (60 ft)
- Purple dot: Superior darkvision (120 ft)
- Cyan dot: Blindsight
- Red dot: Has active condition

#### Tile Tooltip
Hover over any tile to see:
- Tile coordinates
- Light level (Bright/Dim/Darkness)
- Visibility status
- Creature info (if present and visible)

### Test Scenarios

#### Scenario 1: Dungeon Crawl
1. Create Human character (no darkvision)
2. Start combat
3. Move away from light sources
4. Observe complete darkness
5. Move back to light to see again

#### Scenario 2: Darkvision Advantage
1. Create Elf character (60 ft darkvision)
2. Start combat
3. Turn off global daylight
4. Observe that you can still see in darkness (gray tint)
5. Check darkvision range indicator (purple circle)

#### Scenario 3: Fog of War
1. Start combat
2. Press **F** to create fog cloud
3. Try to target enemies in the fog
4. Move out of fog to see clearly

#### Scenario 4: Magical Darkness
1. Start combat
2. Press **K** to cast darkness
3. Even with darkvision, visibility is reduced
4. Move to escape the darkness area

#### Scenario 5: Blinded Combat
1. Start combat
2. Press **B** to become blinded
3. Try to attack (disadvantage)
4. Observe enemy attacks (they have advantage)
5. Press **B** again to remove blindness

### Expected Behaviors

#### Light Propagation
- Light sources create circular areas of bright/dim light
- Light blocked by line of sight obstacles
- Multiple light sources stack (brightest wins)

#### Darkvision
- Only works in darkness
- Converts darkness to dim light
- Converts dim light to bright light
- Shows in grayscale

#### Blindsight
- Ignores line of sight
- Works in any lighting condition
- Cannot be blocked by fog or darkness

#### Combat Rules
- Blinded: disadvantage on attacks, advantage to attackers
- Invisible: advantage on attacks, disadvantage to attackers
- Sunlight Sensitivity: disadvantage in bright light
- Lightly Obscured (Dim Light): disadvantage on Perception

### Performance Notes
- Vision calculated once per action
- Updates on creature movement
- Light sources update positions dynamically
- Area effects persist for their duration

## Controls Summary

| Key | Action |
|-----|--------|
| Tab | Start/Toggle Combat UI |
| V | Toggle Vision Overlay |
| L | Toggle Global Daylight |
| B | Toggle Blinded Condition |
| F | Create Fog Cloud |
| K | Cast Darkness Spell |
| C | Toggle Character Sheet |
| Esc | Pause Menu |
| 1 | Move Action (in combat) |
| 2 | Attack Action (in combat) |
| 3 | End Turn (in combat) |
