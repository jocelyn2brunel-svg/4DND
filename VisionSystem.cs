using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

#nullable enable

namespace _4DND;

public class VisionSystem
{
    public InfiniteGrid3D<TileType>? Grid { get; set; }
    public List<LightSource> _lightSources = new();
    public List<AreaEffect> _areaEffects = new();
    
    private Dictionary<(int, int, int), LightType> _lightMap = new();
    private HashSet<(int, int, int)> _visibleTiles = new();
    private HashSet<(int, int, int)> _exploredTiles = new();
    
    public bool GlobalDaylight { get; set; } = false;
    
    public void AddLightSource(LightSource source)
    {
        _lightSources.Add(source);
    }
    
    public void RemoveLightSource(LightSource source)
    {
        _lightSources.Remove(source);
    }
    
    public void ClearLightSources()
    {
        _lightSources.Clear();
    }
    
    public void AddAreaEffect(AreaEffect effect)
    {
        _areaEffects.Add(effect);
    }
    
    public void RemoveAreaEffect(AreaEffect effect)
    {
        _areaEffects.Remove(effect);
    }
    
    public void ClearAreaEffects()
    {
        _areaEffects.Clear();
    }
    
    public void CalculateLighting()
    {
        _lightMap.Clear();
        
        if (GlobalDaylight)
        {
            return;
        }
        
        foreach (var source in _lightSources)
        {
            if (!source.IsActive) continue;
            
            int brightTiles = Math.Min(source.BrightRadius / 5, 20); // Limit to reasonable range
            int dimTiles = Math.Min(source.DimRadius / 5, 30);
            
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
        
        foreach (var effect in _areaEffects)
        {
            if (!effect.IsActive) continue;
            
            int effectTiles = Math.Min(effect.Radius / 5, 20); // Limit to reasonable range
            
            for (int dz = -effectTiles; dz <= effectTiles; dz++)
            {
                for (int dy = -effectTiles; dy <= effectTiles; dy++)
                {
                    for (int dx = -effectTiles; dx <= effectTiles; dx++)
                    {
                        int tx = effect.X + dx;
                        int ty = effect.Y + dy;
                        int tz = effect.Z + dz;
                        
                        int distance = Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz));
                        
                        if (distance <= effectTiles)
                        {
                            if (effect.EffectType == LightType.Darkness)
                            {
                                SetLightLevel(tx, ty, tz, LightType.Darkness);
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void SetLightLevel(int x, int y, int z, LightType level)
    {
        var key = (x, y, z);
        if (!_lightMap.ContainsKey(key) || level > _lightMap[key])
        {
            _lightMap[key] = level;
        }
    }
    
    public LightType GetLightLevel(int x, int y, int z = 0)
    {
        if (GlobalDaylight)
        {
            foreach (var effect in _areaEffects)
            {
                if (!effect.IsActive) continue;
                if (effect.EffectType != LightType.Darkness) continue;
                
                int distance = Math.Max(Math.Max(Math.Abs(x - effect.X), Math.Abs(y - effect.Y)), Math.Abs(z - effect.Z));
                int effectTiles = effect.Radius / 5;
                
                if (distance <= effectTiles)
                {
                    return LightType.Darkness;
                }
            }
            
            return LightType.Bright;
        }
        
        var key = (x, y, z);
        return _lightMap.ContainsKey(key) ? _lightMap[key] : LightType.Darkness;
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
            
            return;
        }
        
        int visionRange = CalculateVisionRange(observer);
        int visionTiles = Math.Min(visionRange / 5, 30); // Limit to reasonable range
        
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
                    
                    if (distance > visionTiles) continue; // Skip tiles outside range
                    
                    bool blocked = false;

                    // Walls block vision
                    if (!HasLineOfSight(observer.X, observer.Y, observer.Z, tx, ty, tz))
                    {
                        blocked = true;
                    }

                    if (!blocked)
                    {
                        foreach (var effect in _areaEffects)
                        {
                            if (!effect.IsActive || !effect.BlocksVision) continue;

                            // Blindsight can see through most vision-blocking effects (like Fog Cloud)
                            int distToObserver = Math.Max(Math.Max(Math.Abs(tx - observer.X), Math.Abs(ty - observer.Y)), Math.Abs(tz - observer.Z)) * 5;
                            if (observer.HasBlindSight && distToObserver <= observer.BlindSightRange)
                            {
                                continue;
                            }

                            // Truesight can see through magical effects
                            if (observer.HasTrueSight && distToObserver <= observer.TrueSightRange)
                            {
                                continue;
                            }

                            int distToEffect = Math.Max(Math.Max(Math.Abs(tx - effect.X), Math.Abs(ty - effect.Y)), Math.Abs(tz - effect.Z));
                            int effectTiles = effect.Radius / 5;

                            if (distToEffect <= effectTiles)
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }
                    
                    if (!blocked)
                    {
                        _visibleTiles.Add((tx, ty, tz));
                        _exploredTiles.Add((tx, ty, tz));
                    }
                }
            }
        }
    }
    
    private void AddBlindsightVision(Creature observer)
    {
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
                        _exploredTiles.Add((tx, ty, tz));
                    }
                }
            }
        }
    }
    
    private void AddTremorsenseVision(Creature observer)
    {
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
                        _exploredTiles.Add((tx, ty, tz));
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
                return 1000; // Extended range
            }
            else // Bright light
            {
                return 1000; // Normal extended vision
            }
        }
        
        // Normal vision
        if (lightLevel == LightType.Bright)
        {
            return 1000; // Can see far in bright light
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
        return _exploredTiles.Contains((x, y, z));
    }
    
    public Color GetFogOfWarTint(int x, int y, int z, bool isCurrentlyVisible, Creature observer)
    {
        if (isCurrentlyVisible)
        {
            var lightLevel = GetLightLevel(x, y, z);
            
            // Darkvision sees in shades of gray in darkness
            if (lightLevel == LightType.Darkness && observer.DarkvisionRange > 0)
            {
                int distance = Math.Max(Math.Max(Math.Abs(x - observer.X), Math.Abs(y - observer.Y)), Math.Abs(z - observer.Z)) * 5;
                if (distance <= observer.DarkvisionRange)
                {
                    // Gray tint for darkvision - "can't discern color in darkness, only shades of gray"
                    return new Color(96, 96, 96);
                }
            }
            
            return lightLevel switch
            {
                LightType.Bright => Color.White,
                LightType.Dim => new Color(128, 128, 128),
                LightType.Darkness => new Color(64, 64, 96),
                _ => Color.White
            };
        }
        else if (IsExplored(x, y, z))
        {
            return new Color(32, 32, 32);
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
        if (Grid == null) return true;

        int dist = CalculateDistance(x1, y1, z1, x2, y2, z2);
        if (dist <= 1) return true;

        for (int i = 1; i < dist; i++)
        {
            float t = (float)i / dist;
            int cx = (int)Math.Round(x1 + (x2 - x1) * t);
            int cy = (int)Math.Round(y1 + (y2 - y1) * t);
            int cz = (int)Math.Round(z1 + (z2 - z1) * t);

            if (Grid.Get(cx, cy, cz) == TileType.Wall)
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
        _areaEffects.RemoveAll(effect =>
        {
            if (effect.Duration > 0)
            {
                effect.Duration--;
                return effect.Duration <= 0;
            }
            return false;
        });
    }

    public bool IsLightlyObscured(int x, int y, int z, Creature observer)
    {
        var lightLevel = GetLightLevel(x, y, z);
        if (lightLevel == LightType.Dim) return true;

        if (lightLevel == LightType.Darkness)
        {
            int dist = Math.Max(Math.Max(Math.Abs(x - observer.X), Math.Abs(y - observer.Y)), Math.Abs(z - observer.Z)) * 5;
            // Darkness is dim light for creatures with Darkvision
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
