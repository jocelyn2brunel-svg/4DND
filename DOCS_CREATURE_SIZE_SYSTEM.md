# Creature Size System - D&D 5e Implementation

## Overview
This document describes the implementation of D&D 5e creature size rules in the 4DND combat system.

## Creature Size Categories

According to D&D 5e rules, creatures are classified into six size categories, each controlling different amounts of space:

| Size | Space | Grid Squares | Examples |
|------|-------|--------------|----------|
| **Tiny** | 2½ by 2½ ft. | Less than 1 square* | Imp, sprite |
| **Small** | 5 by 5 ft. | 1x1 square | Giant rat, goblin |
| **Medium** | 5 by 5 ft. | 1x1 square | Orc, werewolf, human |
| **Large** | 10 by 10 ft. | 2x2 squares | Hippogriff, ogre |
| **Huge** | 15 by 15 ft. | 3x3 squares | Fire giant, treant |
| **Gargantuan** | 20 by 20 ft. or larger | 4x4 squares (or more) | Kraken, purple worm |

*Note: Tiny creatures occupy less than a full 5-foot square but are treated as occupying 1 square for gameplay purposes in our implementation.

## Implementation Details

### Space Calculation

The `SizeHelper` class provides methods for calculating creature space:

```csharp
public static (float Width, float Height) GetSpaceInFeet(CreatureSize size)
{
    return size switch
    {
        CreatureSize.Tiny => (2.5f, 2.5f),        // 2½ by 2½ ft.
        CreatureSize.Small => (5f, 5f),           // 5 by 5 ft.
        CreatureSize.Medium => (5f, 5f),          // 5 by 5 ft.
        CreatureSize.Large => (10f, 10f),         // 10 by 10 ft.
        CreatureSize.Huge => (15f, 15f),          // 15 by 15 ft.
        CreatureSize.Gargantuan => (20f, 20f),    // 20 by 20 ft. or larger
        _ => (5f, 5f)
    };
}

public static (int Width, int Height) GetSpaceInSquares(CreatureSize size)
{
    return size switch
    {
        CreatureSize.Tiny => (1, 1),           // Treated as 1 square
        CreatureSize.Small => (1, 1),          // 1x1 square
        CreatureSize.Medium => (1, 1),         // 1x1 square
        CreatureSize.Large => (2, 2),          // 2x2 squares
        CreatureSize.Huge => (3, 3),           // 3x3 squares
        CreatureSize.Gargantuan => (4, 4),     // 4x4 squares (or more)
        _ => (1, 1)
    };
}
```

### Movement Validation

When a creature attempts to move, the system checks if the target space can accommodate the creature's size:

```csharp
private bool CanOccupySpace(CreatureSize size, int x, int y, int z)
{
    if (Grid == null) return true;
    
    var (width, height) = SizeHelper.GetSpaceInSquares(size);
    
    // Check all tiles the creature would occupy
    for (int dx = 0; dx < width; dx++)
    {
        for (int dy = 0; dy < height; dy++)
        {
            int checkX = x + dx;
            int checkY = y + dy;
            
            // Check if tile is blocked
            var tileType = Grid.Get(checkX, checkY, z);
            if (tileType == TileType.Wall || tileType == TileType.Empty)
                return false;
            
            // Check if another creature is there
            var creatureAtTile = GetCreatureAt(checkX, checkY, z);
            if (creatureAtTile != null)
                return false;
        }
    }
    
    return true;
}
```

### Melee Range Calculation

The melee range system properly accounts for creatures of different sizes. A creature can attack any enemy that is adjacent to any square it occupies:

```csharp
public bool IsInMeleeRange(Creature attacker, Creature target)
{
    // Get the size of both creatures in squares
    var (attackerWidth, attackerHeight) = SizeHelper.GetSpaceInSquares(attacker.Size);
    var (targetWidth, targetHeight) = SizeHelper.GetSpaceInSquares(target.Size);
    
    // Check if any square occupied by the attacker is adjacent to any square occupied by the target
    for (int ax = 0; ax < attackerWidth; ax++)
    {
        for (int ay = 0; ay < attackerHeight; ay++)
        {
            int attackerTileX = attacker.X + ax;
            int attackerTileY = attacker.Y + ay;
            
            for (int tx = 0; tx < targetWidth; tx++)
            {
                for (int ty = 0; ty < targetHeight; ty++)
                {
                    int targetTileX = target.X + tx;
                    int targetTileY = target.Y + ty;
                    
                    // Check if these tiles are adjacent (including diagonally)
                    int dx = Math.Abs(attackerTileX - targetTileX);
                    int dy = Math.Abs(attackerTileY - targetTileY);
                    int dz = Math.Abs(attacker.Z - target.Z);
                    
                    // Adjacent if within 1 square on each axis (includes diagonals)
                    if (dx <= 1 && dy <= 1 && dz <= 1 && (dx + dy + dz) > 0)
                    {
                        return true;
                    }
                }
            }
        }
    }
    
    return false;
}
```

### Creature Detection

The `GetCreatureAt` method checks all squares occupied by each creature:

```csharp
public Creature? GetCreatureAt(int x, int y, int z = 0)
{
    foreach (var creature in _combatants)
    {
        if (!creature.IsAlive()) continue;
        
        var (width, height) = SizeHelper.GetSpaceInSquares(creature.Size);
        
        // Check if (x, y) is within the creature's occupied space
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                if (creature.X + dx == x && creature.Y + dy == y && creature.Z == z)
                {
                    return creature;
                }
            }
        }
    }
    
    return null;
}
```

## Combat Rules

### Surrounding Creatures

Different sized creatures can be surrounded by different numbers of attackers:

```csharp
public static int GetMaxSurroundingCreatures(CreatureSize size)
{
    return size switch
    {
        CreatureSize.Tiny => 8,         // Same as Medium (can share space)
        CreatureSize.Small => 8,        // 8 squares around a 1x1
        CreatureSize.Medium => 8,       // 8 squares around a 1x1
        CreatureSize.Large => 12,       // 12 squares around a 2x2
        CreatureSize.Huge => 16,        // 16 squares around a 3x3
        CreatureSize.Gargantuan => 20,  // 20 squares around a 4x4
        _ => 8
    };
}
```

### Example Scenarios

#### Scenario 1: Large Creature Movement
A Large ogre (2x2 squares) attempts to move:
- The system checks if all 4 squares at the destination are available
- If any square contains a wall, another creature, or is out of bounds, movement is blocked

#### Scenario 2: Melee Combat
A Medium fighter (1x1) attacks a Huge giant (3x3):
- The fighter needs to be adjacent to ANY of the 9 squares the giant occupies
- This gives the fighter many positions from which to attack
- The giant similarly can attack from any of its 9 squares

#### Scenario 3: Tactical Positioning
Multiple Medium creatures surround a Large creature:
- Up to 12 Medium creatures can surround a Large (2x2) creature
- This represents the increased "reach" around a larger creature
- Allows for realistic crowd control and flanking scenarios

## D&D 5e Rules Reference

### Space (PHB p.191, DMG)
> "A creature's space is the area in feet that it effectively controls in combat, not an expression of its physical dimensions."

Key rules:
1. **Space ? Physical Size**: A 5-foot-wide creature isn't literally 5 feet wide; it controls that much space
2. **Squeezing**: Creatures can squeeze into spaces one size category smaller (not yet implemented)
3. **Multiple Occupants**: Only Tiny creatures can occupy the same space as other creatures under normal circumstances
4. **Reach**: Most creatures have a 5-foot reach (attacking adjacent squares). Some have longer reach (not yet implemented).

### Movement Through Occupied Spaces
- Creatures cannot move through hostile creatures' spaces unless specific conditions are met
- Creatures can move through allies' spaces (treated as difficult terrain - not yet implemented)
- Tiny creatures can move through larger creatures' spaces

## Future Enhancements

Potential additions to the size system:

1. **Reach Weapons**: Some creatures and weapons have longer reach (10 feet or more)
2. **Squeezing**: Allow creatures to squeeze through narrower spaces
3. **Grappling Size Limits**: Size affects grappling rules
4. **Mounting**: Size determines what creatures can be ridden
5. **Creature Facing**: Large+ creatures might have facing rules for rear attacks
6. **Variable Gargantuan Sizes**: Some Gargantuan creatures are much larger (6x6, 8x8, etc.)

## Testing Recommendations

To test the size system:

1. Spawn creatures of different sizes
2. Try moving Large/Huge creatures through narrow corridors
3. Test melee range with various size combinations
4. Verify multiple creatures can't occupy the same space (except Tiny)
5. Test that Large creatures block multiple squares

## References

- Player's Handbook (PHB) Chapter 9: Combat
- Dungeon Master's Guide (DMG) Chapter 8: Running the Game
- Monster Manual: Creature size categories and examples
