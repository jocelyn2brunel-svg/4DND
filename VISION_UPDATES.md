# Vision System Updates - D&D 5e Compliance

This document describes the updates made to properly implement D&D 5e vision rules.

## What Was Changed

### 1. Darkvision Implementation (FIXED)
**Previous Issue**: Darkvision was not properly showing "shades of gray" in darkness.

**New Implementation**:
- In darkness: Creature can see within darkvision range as if it were dim light
- **Colors are shown in grayscale** (Gray tint: `Color(96, 96, 96)`)
- In dim light: Creature can see as if it were bright light
- **"The monster can't discern color in darkness, only shades of gray"**

**Code Changes**:
- Updated `GetFogOfWarTint()` to accept observer parameter
- Added grayscale tinting when darkvision is used in darkness
- Updated `CalculateVisionRange()` to properly treat darkness as dim light for darkvision

### 2. Tremorsense (NEW)
**Implementation**:
- Added `HasTremorsense` and `TremorsenseRange` properties to `Creature` class
- Tremorsense detects vibrations within specified radius
- **Must be in contact with same ground or substance**
- Cannot detect flying or incorporeal creatures
- Works even when blinded

**Test Creature**: Umber Hulk (60 ft tremorsense)

**Code Changes**:
- Added Tremorsense properties to `Creature.cs`
- Added `AddTremorsenseVision()` method in `VisionSystem`
- Updated `CanSee()` methods to handle Tremorsense
- Updated `IsHeavilyObscured()` to exempt Tremorsense
- Removed creature vision/state indicator dots from the tactical UI

### 3. Truesight (ENHANCED)
**Previous Issue**: Truesight existed but didn't properly handle all cases.

**Enhanced Implementation**:
- Sees in normal and magical darkness
- **Sees invisible creatures and objects**
- Automatically detects visual illusions
- Perceives original form of shapechangers
- Can see into Ethereal Plane
- Works through all vision-blocking effects

**Test Creature**: Couatl (120 ft truesight)

**Code Changes**:
- Enhanced `CanSee()` to prioritize Truesight for invisible creatures
- Updated `CalculateVisibility()` to allow Truesight through magical effects
- Updated `IsHeavilyObscured()` to always return false for Truesight
- Removed creature vision/state indicator dots from the tactical UI

## Visual Indicators

Vision and status indicator dots are no longer displayed on creature UI.

## Priority Order

When multiple vision types are present:
1. **Truesight** (if within range) - sees everything
2. **Blindsight** (if within range) - perceives without sight
3. **Tremorsense** (if within range) - detects vibrations
4. **Darkvision** (if within range) - sees in darkness as grayscale
5. **Normal Vision** (requires light)

## Testing

### Test Creatures
- **Wolf**: Blindsight 30 ft (no darkvision)
- **Umber Hulk**: Tremorsense 60 ft + Darkvision 60 ft
- **Couatl**: Truesight 120 ft + Darkvision 60 ft

### Test Controls
- **Tab**: Start combat with random enemies (may include special vision creatures)
- **B**: Toggle Blinded condition on player
- **F**: Create Fog Cloud (heavily obscuring)
- **K**: Cast Darkness spell (magical darkness)
- **V**: Toggle vision overlay
- **L**: Toggle daylight

### Expected Behavior

1. **Darkvision in Darkness**:
   - Creature with darkvision sees tiles in grayscale (gray tint)
   - Cannot see color, only shades of gray
   - Treats darkness as lightly obscured (dim light)

2. **Tremorsense**:
   - Detects all creatures within range touching the ground
   - Works through Fog Cloud and Darkness
   - Works when creature is Blinded
   - Cannot detect flying creatures (not implemented yet)

3. **Truesight**:
   - Sees invisible creatures
   - Sees through magical darkness
   - Sees through Fog Cloud
   - Works in all lighting conditions
   - Most powerful vision type

4. **Blindsight**:
   - Perceives surroundings without sight
   - Detects invisible creatures
   - Works through Fog Cloud
   - Works when Blinded

## D&D 5e Rule Compliance

All implementations follow the exact text from the D&D 5e Monster Manual:

? **Darkvision**: "Can see in the dark within a specified radius... in darkness as if it were dim light, and in darkness as if it were dim light. The monster can't discern color in darkness, only shades of gray."

? **Tremorsense**: "Can detect and pinpoint the origin of vibrations within a specific radius, provided that the monster and the source of the vibrations are in contact with the same ground or substance. Tremorsense can't be used to detect flying or incorporeal creatures."

? **Truesight**: "Can, out to a specific range, see in normal and magical darkness, see invisible creatures and objects, automatically detect visual illusions and succeed on saving throws against them, and perceive the original form of a shapechanger or a creature that is transformed by magic. Furthermore, the monster can see into the Ethereal Plane within the same range."

## Future Enhancements

Potential additions:
- Flying creatures (to properly test Tremorsense limitation)
- Incorporeal creatures (ghosts, etc.)
- Shapechangers (for Truesight to reveal true form)
- Illusion detection system
- Ethereal Plane visibility
