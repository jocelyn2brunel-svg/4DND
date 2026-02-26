using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

#nullable enable

namespace _4DND;

public class VisionSystem
{
    private const int MaxExploredTiles = 1000000;
    private const int ExploredRetentionRadius = 10000; // Increased for world-wide persistence

    public InfiniteGrid3D<TileType>? TacticalMap { get; set; }
    public Vector2 WorldOffset { get; set; } = Vector2.Zero;
    public List<LightSource> _lightSources = new();
    public List<AreaEffect> _areaEffects = new();
    
    private Dictionary<(int, int, int), LightType> _lightMap = new();
    private HashSet<(int, int, int)> _visibleTiles = new();
    private HashSet<(int, int, int)> _exploredTiles = new();
    private HashSet<(int, int)> _exploredHexes = new();
    private bool _lightingNeedsUpdate = true;
    
    public LightType AmbientLight { get; set; } = LightType.Bright;

    public void MarkLightingDirty() => _lightingNeedsUpdate = true;
    
    public void AddLightSource(LightSource source)
    {
        _lightSources.Add(source);
        _lightingNeedsUpdate = true;
    }
    
    public void RemoveLightSource(LightSource source)
    {
        _lightSources.Remove(source);
        _lightingNeedsUpdate = true;
    }
    
    public void ClearLightSources()
    {
        _lightSources.Clear();
        _lightingNeedsUpdate = true;
    }
    
    public void AddAreaEffect(AreaEffect effect)
    {
        _areaEffects.Add(effect);
        _lightingNeedsUpdate = true;
    }
    
    public void RemoveAreaEffect(AreaEffect effect)
    {
        _areaEffects.Remove(effect);
        _lightingNeedsUpdate = true;
    }
    
    public void ClearAreaEffects()
    {
        _areaEffects.Clear();
        _lightingNeedsUpdate = true;
    }

    public void ResetExploration()
    {
        _visibleTiles.Clear();
        _exploredTiles.Clear();
        _exploredHexes.Clear();
    }
    
    public void CalculateLighting()
    {
        if (!_lightingNeedsUpdate) return;

        _lightMap.Clear();
        
        // If ambient is already bright, we don't need to process other light sources
        // as they won't make it any brighter (LightType.Bright is the minimum value 0).
        if (AmbientLight == LightType.Bright)
        {
            _lightingNeedsUpdate = false;
            return;
        }
        
        foreach (var source in _lightSources)
        {
            if (!source.IsActive) continue;
            
            int brightTiles = Math.Min(source.BrightRadius / 5, 20); // Limit to reasonable range
            int dimTiles = Math.Min(source.DimRadius / 5, 20);
            
            for (int dz = -dimTiles; dz <= dimTiles; dz++)
            {
                for (int dy = -dimTiles; dy <= dimTiles; dy++)
                {
                    for (int dx = -dimTiles; dx <= dimTiles; dx++)
                    {
                        int tx = source.X + dx;
                        int ty = source.Y + dy;
                        int tz = source.Z + dz;
                        
                        // 3D Chebyshev distance (5e grid rules)
                        int distance = Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz));
                        
                        if (distance <= brightTiles)
                        {
                            SetLightLevel(tx, ty, tz, LightType.Bright);
                        }
                        else if (distance <= dimTiles)
                        {
                            if (GetLightLevel(tx, ty, tz) != LightType.Bright)
                            {
                                SetLightLevel(tx, ty, tz, LightType.Dim);
                            }
                        }
                    }
                }
            }
        }
        
        // Note: AreaEffects that block vision or cause Darkness are handled in GetLightLevel
        // and vision casting logic to ensure they override other light sources properly.

        _lightingNeedsUpdate = false;
    }
    
    private void SetLightLevel(int x, int y, int z, LightType level)
    {
        var key = (x, y, z);
        // Smaller value means brighter light (Bright=0, Dim=1, Darkness=2)
        if (!_lightMap.ContainsKey(key) || level < _lightMap[key])
        {
            _lightMap[key] = level;
        }
    }
    
    public LightType GetLightLevel(int x, int y, int z = 0)
    {
        // 1. Magical Darkness priority override
        foreach (var effect in _areaEffects)
        {
            if (!effect.IsActive || effect.EffectType != LightType.Darkness) continue;

            int distance = Math.Max(Math.Max(Math.Abs(x - effect.X), Math.Abs(y - effect.Y)), Math.Abs(z - effect.Z));
            int effectTiles = effect.Radius / 5;

            if (distance <= effectTiles)
            {
                return LightType.Darkness;
            }
        }
        
        // 2. Best of Ambient and local Light Sources
        var key = (x, y, z);
        LightType sourceLight = _lightMap.TryGetValue(key, out var level) ? level : LightType.Darkness;

        return (LightType)Math.Min((int)AmbientLight, (int)sourceLight);
    }
    
    public void CalculateVisibility(Creature observer)
    {
        _visibleTiles.Clear();
        
        if (observer.IsBlinded())
        {
            // Blindsight and Tremorsense work even when blinded
            if (observer.HasBlindSight)
            {
                AddBlindsightVision(observer);
            }
            
            if (observer.HasTremorsense)
            {
                AddTremorsenseVision(observer);
            }

            TrimExploredTiles(observer);
            
            return;
        }
        
        int visionRange = CalculateVisionRange(observer);
        // Limit to 20 tiles (100ft) for performance on a tactical combat map
        int visionTiles = Math.Min(visionRange / 5, 20);
        // Limit vertical vision — most combat is on a single plane
        int verticalTiles = Math.Min(visionTiles, 4);
        
        // Raycasting to all tiles on the surface of the visibility volume
        // This is O(R^2) rays instead of O(R^3) raycasts, which is significantly faster

        // Top and Bottom faces
        for (int dx = -visionTiles; dx <= visionTiles; dx++)
        {
            for (int dy = -visionTiles; dy <= visionTiles; dy++)
            {
                CastVisibilityRay(observer, observer.X + dx, observer.Y + dy, observer.Z + verticalTiles);
                CastVisibilityRay(observer, observer.X + dx, observer.Y + dy, observer.Z - verticalTiles);
            }
        }

        // Side faces
        for (int dz = -verticalTiles; dz <= verticalTiles; dz++)
        {
            for (int d = -visionTiles; d <= visionTiles; d++)
            {
                CastVisibilityRay(observer, observer.X - visionTiles, observer.Y + d, observer.Z + dz);
                CastVisibilityRay(observer, observer.X + visionTiles, observer.Y + d, observer.Z + dz);
                CastVisibilityRay(observer, observer.X + d, observer.Y - visionTiles, observer.Z + dz);
                CastVisibilityRay(observer, observer.X + d, observer.Y + visionTiles, observer.Z + dz);
            }
        }

        TrimExploredTiles(observer);
    }

    private void TrimExploredTiles(Creature observer)
    {
        if (_exploredTiles.Count <= MaxExploredTiles)
            return;

        int worldObsX = observer.X + (int)(WorldOffset.X * Campaign.TacticalUnitsPerMile);
        int worldObsY = observer.Y + (int)(WorldOffset.Y * Campaign.TacticalUnitsPerMile);

        var toRemove = new List<(int, int, int)>();
        foreach (var tile in _exploredTiles)
        {
            int distance = Math.Max(
                Math.Max(Math.Abs(tile.Item1 - worldObsX), Math.Abs(tile.Item2 - worldObsY)),
                Math.Abs(tile.Item3 - observer.Z));

            if (distance > ExploredRetentionRadius)
                toRemove.Add(tile);
        }

        foreach (var tile in toRemove)
            _exploredTiles.Remove(tile);
    }

    private void CastVisibilityRay(Creature observer, int tx, int ty, int tz)
    {
        int worldXOffset = (int)(WorldOffset.X * Campaign.TacticalUnitsPerMile);
        int worldYOffset = (int)(WorldOffset.Y * Campaign.TacticalUnitsPerMile);

        int dist = Math.Max(Math.Max(Math.Abs(tx - observer.X), Math.Abs(ty - observer.Y)), Math.Abs(tz - observer.Z));
        if (dist == 0)
        {
            _visibleTiles.Add((observer.X, observer.Y, observer.Z));
            _exploredTiles.Add((observer.X + worldXOffset, observer.Y + worldYOffset, observer.Z));
            return;
        }

        float stepX = (float)(tx - observer.X) / dist;
        float stepY = (float)(ty - observer.Y) / dist;
        float stepZ = (float)(tz - observer.Z) / dist;

        float curX = observer.X + 0.5f;
        float curY = observer.Y + 0.5f;
        float curZ = observer.Z + 0.5f;

        for (int i = 0; i <= dist; i++)
        {
            int cx = (int)curX;
            int cy = (int)curY;
            int cz = (int)curZ;

            _visibleTiles.Add((cx, cy, cz));
            _exploredTiles.Add((cx + worldXOffset, cy + worldYOffset, cz));

            if (TacticalMap != null && TacticalMap.Get(cx, cy, cz) == TileType.Wall)
                break;

            // Check area effects that block vision
            bool blockedByEffect = false;
            foreach (var effect in _areaEffects)
            {
                if (!effect.IsActive || !effect.BlocksVision) continue;

                // Blindsight/Truesight can see through certain effects
                int distToObs = Math.Max(Math.Max(Math.Abs(cx - observer.X), Math.Abs(cy - observer.Y)), Math.Abs(cz - observer.Z)) * 5;
                if (observer.HasBlindSight && distToObs <= observer.BlindSightRange) continue;
                if (observer.HasTrueSight && distToObs <= observer.TrueSightRange) continue;

                int distToEff = Math.Max(Math.Max(Math.Abs(cx - effect.X), Math.Abs(cy - effect.Y)), Math.Abs(cz - effect.Z));
                if (distToEff <= effect.Radius / 5)
                {
                    blockedByEffect = true;
                    break;
                }
            }
            if (blockedByEffect) break;

            curX += stepX;
            curY += stepY;
            curZ += stepZ;
        }
    }
    
    private void AddBlindsightVision(Creature observer)
    {
        int worldXOffset = (int)(WorldOffset.X * Campaign.TacticalUnitsPerMile);
        int worldYOffset = (int)(WorldOffset.Y * Campaign.TacticalUnitsPerMile);
        int visionTiles = Math.Min(observer.BlindSightRange / 5, 30); // Limit to reasonable range
        
        for (int dz = -visionTiles; dz <= visionTiles; dz++)
        {
            for (int dy = -visionTiles; dy <= visionTiles; dy++)
            {
                for (int dx = -visionTiles; dx <= visionTiles; dx++)
                {
                    int tx = observer.X + dx;
                    int ty = observer.Y + dy;
                    int tz = observer.Z + dz;
                    
                    int distance = Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz));
                    
                    if (distance <= visionTiles)
                    {
                        _visibleTiles.Add((tx, ty, tz));
                        _exploredTiles.Add((tx + worldXOffset, ty + worldYOffset, tz));
                    }
                }
            }
        }
    }
    
    private void AddTremorsenseVision(Creature observer)
    {
        int worldXOffset = (int)(WorldOffset.X * Campaign.TacticalUnitsPerMile);
        int worldYOffset = (int)(WorldOffset.Y * Campaign.TacticalUnitsPerMile);
        int visionTiles = Math.Min(observer.TremorsenseRange / 5, 30); // Limit to reasonable range
        
        for (int dz = -visionTiles; dz <= visionTiles; dz++)
        {
            for (int dy = -visionTiles; dy <= visionTiles; dy++)
            {
                for (int dx = -visionTiles; dx <= visionTiles; dx++)
                {
                    int tx = observer.X + dx;
                    int ty = observer.Y + dy;
                    int tz = observer.Z + dz;
                    
                    int distance = Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz));
                    
                    if (distance <= visionTiles)
                    {
                        _visibleTiles.Add((tx, ty, tz));
                        _exploredTiles.Add((tx + worldXOffset, ty + worldYOffset, tz));
                    }
                }
            }
        }
    }
    
    private int CalculateVisionRange(Creature creature)
    {
        var lightLevel = GetLightLevel(creature.X, creature.Y, creature.Z);
        
        // Truesight sees everything within range, regardless of lighting or magical darkness
        if (creature.HasTrueSight)
        {
            return creature.TrueSightRange;
        }
        
        // Blindsight perceives surroundings without relying on sight
        if (creature.HasBlindSight)
        {
            return creature.BlindSightRange;
        }
        
        // Tremorsense detects vibrations
        if (creature.HasTremorsense)
        {
            return creature.TremorsenseRange;
        }
        
        // Darkvision: treat darkness as dim light, dim light as bright light
        if (creature.DarkvisionRange > 0)
        {
            if (lightLevel == LightType.Darkness)
            {
                // In darkness, darkvision allows seeing as if in dim light
                return creature.DarkvisionRange;
            }
            else if (lightLevel == LightType.Dim)
            {
                // In dim light, darkvision allows seeing as if in bright light
                return Math.Max(60, creature.DarkvisionRange);
            }
            else
            {
                // Bright light
                return 100;
            }
        }
        
        // Normal vision
        if (lightLevel == LightType.Bright)
        {
            return 100; // Practical tactical range (20 tiles)
        }
        else if (lightLevel == LightType.Dim)
        {
            return 60; // Reduced range in dim light
        }
        else // Darkness
        {
            return 0; // Cannot see in darkness without darkvision
        }
    }
    
    public bool IsVisible(int x, int y, int z = 0)
    {
        return _visibleTiles.Contains((x, y, z));
    }
    
    public bool IsExplored(int x, int y, int z = 0)
    {
        int wx = x + (int)(WorldOffset.X * Campaign.TacticalUnitsPerMile);
        int wy = y + (int)(WorldOffset.Y * Campaign.TacticalUnitsPerMile);
        if (_exploredTiles.Contains((wx, wy, z))) return true;

        if (z == 0)
        {
            var (hq, hr) = Campaign.CartesianToAxial((float)wx / Campaign.TacticalUnitsPerMile, (float)wy / Campaign.TacticalUnitsPerMile);
            if (_exploredHexes.Contains((hq, hr))) return true;
        }

        return false;
    }

    public bool IsHexExplored(int q, int r)
    {
        return _exploredHexes.Contains((q, r));
    }

    public static float GetRevealRadius(Campaign campaign, List<Character> characters)
    {
        if (campaign == null) return 100f;

        var lightLevel = campaign.GetCurrentLightLevel();
        if (lightLevel == LightType.Bright)
        {
            return 2.0f * 5280f; // 2 miles during the day
        }
        else
        {
            // Twilight/Night/Dawn: use Darkvision or light sources
            int maxDarkvision = 0;
            if (characters != null && characters.Count > 0)
            {
                maxDarkvision = characters.Max(c => c.DarkvisionRange);
            }

            float lightSourceRange = 0;
            if (characters != null)
            {
                bool hasLantern = characters.Any(c => c.InventoryData.HasItem("Lantern, hooded") || c.InventoryData.HasItem("Lantern, bullseye"));
                bool hasTorch = characters.Any(c => c.InventoryData.HasItem("Torch"));

                if (hasLantern) lightSourceRange = 60f;
                else if (hasTorch) lightSourceRange = 40f;
            }

            return Math.Max(maxDarkvision, lightSourceRange);
        }
    }

    public void RevealPath(Vector2 startMiles, Vector2 endMiles, float radiusFeet = 100f)
    {
        if (radiusFeet <= 0) return;

        // Use hex-based exploration for large radii (>= 0.5 miles)
        if (radiusFeet >= 5280f * 0.5f)
        {
            RevealPathHex(startMiles, endMiles, radiusFeet / 5280f);
            return;
        }

        Vector2 startTactical = startMiles * Campaign.TacticalUnitsPerMile;
        Vector2 endTactical = endMiles * Campaign.TacticalUnitsPerMile;

        float dist = Vector2.Distance(startTactical, endTactical);
        // Step every 10 feet to balance performance and coverage
        int steps = (int)Math.Ceiling(dist / 10f);

        int radiusTiles = (int)Math.Max(1, radiusFeet / 5f);

        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0 : (float)i / steps;
            Vector2 pos = Vector2.Lerp(startTactical, endTactical, t);

            int cx = (int)Math.Round(pos.X);
            int cy = (int)Math.Round(pos.Y);

            // Reveal a square/circle around this point in world coordinates
            for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
            {
                for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
                {
                    if (dx*dx + dy*dy <= radiusTiles*radiusTiles)
                    {
                        _exploredTiles.Add((cx + dx, cy + dy, 0));
                    }
                }
            }
        }
    }

    private void RevealPathHex(Vector2 startMiles, Vector2 endMiles, float radiusMiles)
    {
        float dist = Vector2.Distance(startMiles, endMiles);
        // Step every 0.5 miles for hex exploration
        int steps = (int)Math.Ceiling(dist / 0.5f);

        int radiusHexes = (int)Math.Ceiling(radiusMiles);

        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0 : (float)i / steps;
            Vector2 pos = Vector2.Lerp(startMiles, endMiles, t);

            var (cq, cr) = Campaign.CartesianToAxial(pos.X, pos.Y);

            // Reveal hexes in range
            for (int dq = -radiusHexes; dq <= radiusHexes; dq++)
            {
                for (int dr = Math.Max(-radiusHexes, -dq - radiusHexes); dr <= Math.Min(radiusHexes, -dq + radiusHexes); dr++)
                {
                    _exploredHexes.Add((cq + dq, cr + dr));
                }
            }
        }
    }
    
    public Color GetFogOfWarTint(int x, int y, int z, bool isCurrentlyVisible, Creature observer)
    {
        var lightLevel = GetLightLevel(x, y, z);

        if (isCurrentlyVisible)
        {
            // Darkvision upgrades
            if (observer.DarkvisionRange > 0)
            {
                int distance = Math.Max(Math.Max(Math.Abs(x - observer.X), Math.Abs(y - observer.Y)), Math.Abs(z - observer.Z)) * 5;
                if (distance <= observer.DarkvisionRange)
                {
                    if (lightLevel == LightType.Dim)
                    {
                        // Sees dim light as bright light (in color)
                        return Color.White;
                    }
                    else if (lightLevel == LightType.Darkness)
                    {
                        // Sees darkness as dim light (grayscale)
                        // Alpha 254 is used as a flag for the rendering system to apply grayscale
                        return new Color(128, 128, 128, 254);
                    }
                }
            }
            
            return lightLevel switch
            {
                LightType.Bright => Color.White,
                LightType.Dim => new Color(128, 128, 128),
                LightType.Darkness => Color.Black,
                _ => Color.White
            };
        }
        else if (IsExplored(x, y, z))
        {
            // Explored tiles take the tint of the local light level (ambient or local sources)
            return lightLevel switch
            {
                LightType.Bright => Color.White,
                LightType.Dim => new Color(128, 128, 128),
                LightType.Darkness => Color.Black,
                _ => Color.White
            };
        }
        else
        {
            return Color.Black;
        }
    }
    
    public bool CanSee(Creature observer, int targetX, int targetY, int targetZ = 0)
    {
        int distance = Math.Max(Math.Max(Math.Abs(targetX - observer.X), Math.Abs(targetY - observer.Y)), Math.Abs(targetZ - observer.Z)) * 5;
        
        // Truesight can see through everything within range
        if (observer.HasTrueSight && distance <= observer.TrueSightRange)
        {
            return true;
        }
        
        // Tremorsense can detect anything touching the ground within range
        if (observer.HasTremorsense && distance <= observer.TremorsenseRange)
        {
            return true;
        }
        
        if (observer.IsBlinded())
        {
            // Blindsight works even when blinded
            if (observer.HasBlindSight && distance <= observer.BlindSightRange)
            {
                return true;
            }
            
            return false;
        }
        
        int visionRange = CalculateVisionRange(observer);
        
        if (distance > visionRange)
        {
            return false;
        }

        // Walls block vision
        if (!HasLineOfSight(observer.X, observer.Y, observer.Z, targetX, targetY, targetZ))
        {
            return false;
        }
        
        foreach (var effect in _areaEffects)
        {
            if (!effect.IsActive || !effect.BlocksVision) continue;
            
            // Blindsight can see through most vision-blocking effects
            if (observer.HasBlindSight && distance <= observer.BlindSightRange)
            {
                continue;
            }
            
            // Truesight can see through magical effects
            if (observer.HasTrueSight && distance <= observer.TrueSightRange)
            {
                continue;
            }

            int distToEffect = Math.Max(Math.Abs(targetX - effect.X), Math.Abs(targetY - effect.Y));
            int effectTiles = effect.Radius / 5;
            
            if (distToEffect <= effectTiles)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public bool CanSee(Creature observer, Creature target)
    {
        int distance = Math.Max(Math.Max(Math.Abs(target.X - observer.X), Math.Abs(target.Y - observer.Y)), Math.Abs(target.Z - observer.Z)) * 5;
        
        // Truesight can see invisible creatures and objects within range
        if (observer.HasTrueSight && distance <= observer.TrueSightRange)
        {
            return true;
        }
        
        // Tremorsense can detect creatures touching the ground (not flying/burrowing)
        if (observer.HasTremorsense && distance <= observer.TremorsenseRange)
        {
            // Tremorsense can't detect flying or incorporeal creatures
            // For now, assume all creatures are touching the ground
            return true;
        }
        
        // Blindsight can detect invisible creatures
        if (target.Conditions.HasCondition(Condition.Invisible))
        {
            if (observer.HasBlindSight && distance <= observer.BlindSightRange)
            {
                return true;
            }
            
            // Invisible creatures can't be seen by normal vision or darkvision
            return false;
        }
        
        return CanSee(observer, target.X, target.Y, target.Z);
    }
    
    public bool HasLineOfSight(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        if (TacticalMap == null) return true;

        int dist = CalculateDistance(x1, y1, z1, x2, y2, z2);
        if (dist <= 1) return true;

        float stepX = (float)(x2 - x1) / dist;
        float stepY = (float)(y2 - y1) / dist;
        float stepZ = (float)(z2 - z1) / dist;

        float curX = x1 + 0.5f;
        float curY = y1 + 0.5f;
        float curZ = z1 + 0.5f;

        for (int i = 1; i < dist; i++)
        {
            curX += stepX;
            curY += stepY;
            curZ += stepZ;

            if (TacticalMap.Get((int)curX, (int)curY, (int)curZ) == TileType.Wall)
                return false;
        }

        return true;
    }

    private int CalculateDistance(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        return Math.Max(Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)), Math.Abs(z2 - z1));
    }

    public void UpdateAreaEffects()
    {
        int countBefore = _areaEffects.Count;
        _areaEffects.RemoveAll(effect =>
        {
            if (effect.Duration > 0)
            {
                effect.Duration--;
                return effect.Duration <= 0;
            }
            return false;
        });
        if (_areaEffects.Count != countBefore)
        {
            _lightingNeedsUpdate = true;
        }
    }

    public bool IsLightlyObscured(int x, int y, int z, Creature observer)
    {
        var lightLevel = GetLightLevel(x, y, z);
        int dist = Math.Max(Math.Max(Math.Abs(x - observer.X), Math.Abs(y - observer.Y)), Math.Abs(z - observer.Z)) * 5;

        if (lightLevel == LightType.Dim)
        {
            // Darkvision sees dim light as bright light (not lightly obscured)
            if (observer.DarkvisionRange > 0 && dist <= observer.DarkvisionRange) return false;
            return true;
        }

        if (lightLevel == LightType.Darkness)
        {
            // Darkness is dim light for creatures with Darkvision (lightly obscured)
            if (observer.DarkvisionRange > 0 && dist <= observer.DarkvisionRange) return true;
        }

        return false;
    }

    public bool IsHeavilyObscured(int x, int y, int z, Creature observer)
    {
        int distToObserver = Math.Max(Math.Max(Math.Abs(x - observer.X), Math.Abs(y - observer.Y)), Math.Abs(z - observer.Z)) * 5;

        // Truesight sees in normal and magical darkness
        if (observer.HasTrueSight && distToObserver <= observer.TrueSightRange)
        {
            return false;
        }
        
        // Tremorsense detects through vibrations, not affected by obscurement
        if (observer.HasTremorsense && distToObserver <= observer.TremorsenseRange)
        {
            return false;
        }

        // Blindsight ignores heavy obscuration within its range
        if (observer.HasBlindSight && distToObserver <= observer.BlindSightRange)
        {
            return false;
        }

        foreach (var effect in _areaEffects)
        {
            if (effect.IsActive && effect.BlocksVision)
            {
                int distance = Math.Max(Math.Max(Math.Abs(x - effect.X), Math.Abs(y - effect.Y)), Math.Abs(z - effect.Z));
                if (distance <= effect.Radius / 5) return true;
            }
        }

        var lightLevel = GetLightLevel(x, y, z);
        if (lightLevel == LightType.Darkness)
        {
            // Darkvision treats darkness as dim light, not heavily obscured
            if (observer.DarkvisionRange > 0 && distToObserver <= observer.DarkvisionRange) return false;
            return true;
        }

        return false;
    }
}
