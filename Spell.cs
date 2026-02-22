using System;
using System.Collections.Generic;

namespace _4DND;

public enum AreaShape
{
    Sphere,
    Cube,
    Cylinder,
    Cone,
    Line
}

public class AreaEffect
{
    public int X { get; set; }
    public int Y { get; set; }
    public AreaShape Shape { get; set; }
    public int Radius { get; set; }  // In feet
    public int Duration { get; set; } // In rounds
    public LightType EffectType { get; set; }
    public bool BlocksVision { get; set; } = false;
    
    public static AreaEffect FogCloud(int x, int y, int radius = 20)
    {
        return new AreaEffect
        {
            X = x,
            Y = y,
            Shape = AreaShape.Sphere,
            Radius = radius,
            Duration = 10, // 1 minute = 10 rounds
            EffectType = LightType.Darkness,
            BlocksVision = true
        };
    }
    
    public static AreaEffect Darkness(int x, int y, int radius = 15)
    {
        return new AreaEffect
        {
            X = x,
            Y = y,
            Shape = AreaShape.Sphere,
            Radius = radius,
            Duration = 10,
            EffectType = LightType.Darkness,
            BlocksVision = false
        };
    }
}

public enum SpellEffect
{
    None,
    Blindness,
    Light,
    Darkness,
    FogCloud,
    Daylight
}

public class Spell
{
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string School { get; set; } = "";
    public bool RequiresConcentration { get; set; }
    public int Range { get; set; } // In feet
    public int Duration { get; set; } // In rounds, 0 = instantaneous
    public SpellEffect Effect { get; set; }
    public string Description { get; set; } = "";
    
    // Vision-affecting spells from D&D 5e
    public static Spell Blindness => new()
    {
        Name = "Blindness/Deafness",
        Level = 2,
        School = "Necromancy",
        RequiresConcentration = false,
        Range = 30,
        Duration = 1, // 1 minute
        Effect = SpellEffect.Blindness,
        Description = "Target must make a CON save or be blinded or deafened"
    };
    
    public static Spell Light => new()
    {
        Name = "Light",
        Level = 0,
        School = "Evocation",
        RequiresConcentration = false,
        Range = 0, // Touch
        Duration = 60, // 1 hour
        Effect = SpellEffect.Light,
        Description = "Object sheds bright light in 20 ft radius, dim light 20 ft beyond"
    };
    
    public static Spell DarknessSpell => new()
    {
        Name = "Darkness",
        Level = 2,
        School = "Evocation",
        RequiresConcentration = true,
        Range = 60,
        Duration = 10, // 10 minutes
        Effect = SpellEffect.Darkness,
        Description = "15 ft radius sphere of magical darkness"
    };
    
    public static Spell FogCloud => new()
    {
        Name = "Fog Cloud",
        Level = 1,
        School = "Conjuration",
        RequiresConcentration = true,
        Range = 120,
        Duration = 10, // 1 hour
        Effect = SpellEffect.FogCloud,
        Description = "20 ft radius sphere of fog that heavily obscures"
    };
    
    public static Spell Daylight => new()
    {
        Name = "Daylight",
        Level = 3,
        School = "Evocation",
        RequiresConcentration = false,
        Range = 60,
        Duration = 60, // 1 hour
        Effect = SpellEffect.Daylight,
        Description = "60 ft radius sphere of bright light, 60 ft beyond is dim"
    };
}
