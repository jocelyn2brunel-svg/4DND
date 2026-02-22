using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace _4DND;

public class AreaEffect
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Radius { get; set; }
    public LightType EffectType { get; set; }
    public bool BlocksVision { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int Duration { get; set; } = -1;
    
    public static AreaEffect FogCloud(int x, int y, int radius = 20)
    {
        return new AreaEffect
        {
            X = x,
            Y = y,
            Radius = radius,
            EffectType = LightType.Darkness,
            BlocksVision = true,
            Duration = 10
        };
    }
    
    public static AreaEffect Darkness(int x, int y, int radius = 15)
    {
        return new AreaEffect
        {
            X = x,
            Y = y,
            Radius = radius,
            EffectType = LightType.Darkness,
            BlocksVision = false,
            Duration = 10
        };
    }
}

public class VisionSystem
{
    public List<LightSource> _lightSources = new();
    public List<AreaEffect> _areaEffects = new();
    
    private Dictionary<(int, int), LightType> _lightMap = new();
    private HashSet<(int, int)> _visibleTiles = new();
    private HashSet<(int, int)> _exploredTiles = new();
    
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
            
            int brightTiles = source.BrightRadius / 5;
            int dimTiles = source.DimRadius / 5;
            
            for (int dy = -dimTiles; dy <= dimTiles; dy++)
            {
                for (int dx = -dimTiles; dx <= dimTiles; dx++)
                {
                    int tx = source.X + dx;
                    int ty = source.Y + dy;
                    
                    int distance = Math.Abs(dx) + Math.Abs(dy);
                    
                    if (distance <= brightTiles)
                    {
                        SetLightLevel(tx, ty, LightType.Bright);
                    }
                    else if (distance <= dimTiles)
                    {
                        if (GetLightLevel(tx, ty) != LightType.Bright)
                        {
                            SetLightLevel(tx, ty, LightType.Dim);
                        }
                    }
                }
            }
        }
        
        foreach (var effect in _areaEffects)
        {
            if (!effect.IsActive) continue;
            
            int effectTiles = effect.Radius / 5;
            
            for (int dy = -effectTiles; dy <= effectTiles; dy++)
            {
                for (int dx = -effectTiles; dx <= effectTiles; dx++)
                {
                    int tx = effect.X + dx;
                    int ty = effect.Y + dy;
                    
                    int distance = Math.Abs(dx) + Math.Abs(dy);
                    
                    if (distance <= effectTiles)
                    {
                        if (effect.EffectType == LightType.Darkness)
                        {
                            SetLightLevel(tx, ty, LightType.Darkness);
                        }
                    }
                }
            }
        }
    }
    
    private void SetLightLevel(int x, int y, LightType level)
    {
        var key = (x, y);
        if (!_lightMap.ContainsKey(key) || level > _lightMap[key])
        {
            _lightMap[key] = level;
        }
    }
    
    public LightType GetLightLevel(int x, int y)
    {
        if (GlobalDaylight)
        {
            foreach (var effect in _areaEffects)
            {
                if (!effect.IsActive) continue;
                if (effect.EffectType != LightType.Darkness) continue;
                
                int distance = Math.Abs(x - effect.X) + Math.Abs(y - effect.Y);
                int effectTiles = effect.Radius / 5;
                
                if (distance <= effectTiles)
                {
                    return LightType.Darkness;
                }
            }
            
            return LightType.Bright;
        }
        
        var key = (x, y);
        return _lightMap.ContainsKey(key) ? _lightMap[key] : LightType.Darkness;
    }
    
    public void CalculateVisibility(Creature observer)
    {
        _visibleTiles.Clear();
        
        if (observer.IsBlinded())
        {
            return;
        }
        
        int visionRange = CalculateVisionRange(observer);
        int visionTiles = visionRange / 5;
        
        for (int dy = -visionTiles; dy <= visionTiles; dy++)
        {
            for (int dx = -visionTiles; dx <= visionTiles; dx++)
            {
                int tx = observer.X + dx;
                int ty = observer.Y + dy;
                
                int distance = Math.Abs(dx) + Math.Abs(dy);
                
                if (distance <= visionTiles)
                {
                    bool blocked = false;
                    
                    foreach (var effect in _areaEffects)
                    {
                        if (!effect.IsActive || !effect.BlocksVision) continue;
                        
                        int distToEffect = Math.Abs(tx - effect.X) + Math.Abs(ty - effect.Y);
                        int effectTiles = effect.Radius / 5;
                        
                        if (distToEffect <= effectTiles)
                        {
                            blocked = true;
                            break;
                        }
                    }
                    
                    if (!blocked)
                    {
                        _visibleTiles.Add((tx, ty));
                        _exploredTiles.Add((tx, ty));
                    }
                }
            }
        }
    }
    
    private int CalculateVisionRange(Creature creature)
    {
        var lightLevel = GetLightLevel(creature.X, creature.Y);
        
        if (creature.HasBlindSight)
        {
            return creature.BlindSightRange;
        }
        
        if (creature.HasTrueSight)
        {
            return creature.TrueSightRange;
        }
        
        if (lightLevel == LightType.Bright)
        {
            return 1000;
        }
        else if (lightLevel == LightType.Dim)
        {
            return creature.DarkvisionRange > 0 ? Math.Max(creature.DarkvisionRange, 60) : 60;
        }
        else
        {
            return creature.DarkvisionRange;
        }
    }
    
    public bool IsVisible(int x, int y)
    {
        return _visibleTiles.Contains((x, y));
    }
    
    public bool IsExplored(int x, int y)
    {
        return _exploredTiles.Contains((x, y));
    }
    
    public Color GetFogOfWarTint(int x, int y, bool isCurrentlyVisible)
    {
        if (isCurrentlyVisible)
        {
            var lightLevel = GetLightLevel(x, y);
            
            return lightLevel switch
            {
                LightType.Bright => Color.White,
                LightType.Dim => new Color(128, 128, 128),
                LightType.Darkness => new Color(64, 64, 96),
                _ => Color.White
            };
        }
        else if (IsExplored(x, y))
        {
            return new Color(32, 32, 32);
        }
        else
        {
            return Color.Black;
        }
    }
    
    public bool CanSee(Creature observer, int targetX, int targetY)
    {
        if (observer.IsBlinded())
        {
            return false;
        }
        
        int distance = (Math.Abs(targetX - observer.X) + Math.Abs(targetY - observer.Y)) * 5;
        int visionRange = CalculateVisionRange(observer);
        
        if (distance > visionRange)
        {
            return false;
        }
        
        foreach (var effect in _areaEffects)
        {
            if (!effect.IsActive || !effect.BlocksVision) continue;
            
            int distToEffect = Math.Abs(targetX - effect.X) + Math.Abs(targetY - effect.Y);
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
        if (target.Conditions.HasCondition(Condition.Invisible))
        {
            if (observer.HasTrueSight)
            {
                int distance = (Math.Abs(target.X - observer.X) + Math.Abs(target.Y - observer.Y)) * 5;
                return distance <= observer.TrueSightRange;
            }
            
            if (observer.HasBlindSight)
            {
                int distance = (Math.Abs(target.X - observer.X) + Math.Abs(target.Y - observer.Y)) * 5;
                return distance <= observer.BlindSightRange;
            }
            
            return false;
        }
        
        return CanSee(observer, target.X, target.Y);
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
}
