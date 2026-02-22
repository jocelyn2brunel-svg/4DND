# Campaign System - Quick Reference

## Creating a Campaign

### Flow
```
New Game ? Create Character ? Create Campaign ? Play
```

### Campaign Creation Steps
1. **Name** - Type campaign name (e.g., "Dragon's Curse")
2. **Home Base** - Type settlement name (e.g., "Waterdeep")
3. **Settlement Type** - Choose from:
   ```
   Hamlet     - Tiny (< 20 people)
   Village    - Small (20-1000)
   Town       - Medium (1000-6000)
   City       - Large (6000-25000)
   Fort       - Military outpost
   Castle     - Fortified stronghold
   Monastery  - Religious retreat
   ```

4. **Terrain** - Choose local region type:
   ```
   Forest, Hills, Mountains, Plains, Swamp, Desert, Coast
   ```

5. **Adventure** - Choose starting objective:
   ```
   - Explore a nearby dungeon
   - Investigate mysterious disappearances
   - Clear out a bandit camp
   - Rescue a kidnapped merchant
   - Hunt a dangerous beast
   - Recover a lost artifact
   - Custom adventure
   ```

## In-Game Controls

| Key | Action |
|-----|--------|
| M | Toggle campaign map |
| C | Toggle character sheet |
| Tab | Toggle combat UI |
| ESC | Open pause menu |

### Campaign Map Controls
| Key | Action |
|-----|--------|
| WASD | Pan camera |
| +/- | Zoom in/out |
| M | Close map |

## Map Symbols

| Symbol | Meaning |
|--------|---------|
| ? (Gold Star) | Home Base |
| ?? (Green) | Village/Town/City |
| ?? (Red) | Fort/Castle |
| ?? (Purple) | Dungeon |
| ?? (Blue) | Monastery |
| ? (White) | Wilderness |
| ? (Yellow Circle) | Region Boundary |

## Campaign Data

### Automatic Tracking
- Session count
- Locations discovered
- Party members
- Current objective
- Completed objectives

### Saved Data Location
```
saves/campaigns.json
```

## Quick Tips

### Starting Small
? Begin with just your home base
? Local region is 1 mile (10 hexes) radius
? One starting dungeon nearby
? Add locations as you explore

### Expanding Your World
1. Explore local area first
2. Add nearby settlements (3-10 hexes away)
3. Create regional dungeons and landmarks
4. Expand to neighboring regions
5. Build political/faction relationships

### Best Practices
- **Name your home base** - Something memorable
- **Choose appropriate size** - Village/Town for most campaigns
- **Match terrain to story** - Forest for ranger, mountains for dwarves
- **Start with clear objective** - Gives players direction
- **Add locations gradually** - Don't overwhelm with detail

## Example Campaigns

### Classic Starter (Village-Based)
```
Name: Lost Mine of Phandelver
Home: Phandalin (Village)
Terrain: Forest
Adventure: Explore a nearby dungeon
```

### Urban Campaign (City-Based)
```
Name: Waterdeep Dragon Heist
Home: Waterdeep (Metropolis)
Terrain: Coast
Adventure: Investigate mysterious disappearances
```

### Military Campaign (Fort-Based)
```
Name: Defense of the Realm
Home: Borderwatch (Fort)
Terrain: Plains
Adventure: Clear out a bandit camp
```

### Wilderness Campaign
```
Name: Into the Wild
Home: Ranger's Lodge (Hamlet)
Terrain: Forest
Adventure: Hunt a dangerous beast
```

## DMG References

Based on these DMG sections:
- **Creating a Campaign** (DMG p. 35-37)
- **Start Small** (DMG p. 35)
- **Settlement Size** (DMG p. 16)
- **Creating Adventures** (DMG p. 71-106)

## Common Questions

**Q: Can I have multiple campaigns?**
A: Yes! Each campaign is saved separately.

**Q: How do I add new locations?**
A: Currently manual (future: in-game location editor).

**Q: Can I edit campaign details later?**
A: Yes, by editing `saves/campaigns.json`.

**Q: What happens when I explore beyond the local region?**
A: New regions can be added as your campaign grows.

**Q: Can multiple characters share a campaign?**
A: Yes! Characters are tracked in campaign's party list.

## Keyboard Cheat Sheet

### Character Creation
- Arrow Keys - Navigate options
- Enter - Confirm/Type
- Backspace - Delete character
- Escape - Cancel

### Campaign Creation
- Arrow Keys - Choose options
- Enter - Proceed/Type
- Backspace - Delete character
- Escape - Cancel

### Gameplay
- WASD/Arrows - Move camera
- Q/E - Rotate view
- Mouse Wheel - Zoom
- M - Campaign map
- C - Character sheet
- Tab - Combat UI
- ESC - Menu

### Campaign Map
- WASD - Pan
- +/- - Zoom
- M - Close
