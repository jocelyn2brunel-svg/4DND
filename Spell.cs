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
    public int Z { get; set; }
    public AreaShape Shape { get; set; }
    public int Radius { get; set; }  // In feet
    public int Duration { get; set; } = -1; // In rounds, -1 for infinite
    public LightType EffectType { get; set; }
    public bool BlocksVision { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    public static AreaEffect FogCloud(int x, int y, int z = 0, int radius = 20)
    {
        return new AreaEffect
        {
            X = x,
            Y = y,
            Z = z,
            Shape = AreaShape.Sphere,
            Radius = radius,
            Duration = 10, // 1 minute = 10 rounds
            EffectType = LightType.Darkness,
            BlocksVision = true,
            IsActive = true
        };
    }
    
    public static AreaEffect Darkness(int x, int y, int z = 0, int radius = 15)
    {
        return new AreaEffect
        {
            X = x,
            Y = y,
            Z = z,
            Shape = AreaShape.Sphere,
            Radius = radius,
            Duration = 10,
            EffectType = LightType.Darkness,
            BlocksVision = false,
            IsActive = true
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

    // Damage properties
    public string DamageDice { get; set; } = "";       // e.g., "8d6"
    public DamageType DamageType { get; set; } = DamageType.None;
    public string SaveAbility { get; set; } = "";       // e.g., "DEX"; empty = no saving throw
    public int SaveDC { get; set; } = 0;
    public int AreaRadiusFeet { get; set; } = 0;        // 0 = single target, >0 = sphere radius
    public bool HalfDamageOnSave { get; set; } = true;  // true: half damage on save; false: no damage on save

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

    /// <summary>
    /// Fireball (PHB): 3rd-level Evocation. A bright streak of flame explodes in a 20-foot radius sphere.
    /// Each creature in the area makes a DEX saving throw, taking 8d6 fire damage on a failure,
    /// or half as much on a success. The spell's save DC must be set by the caster at cast time.
    /// </summary>
    public static Spell Fireball(int saveDC) => new()
    {
        Name = "Fireball",
        Level = 3,
        School = "Evocation",
        RequiresConcentration = false,
        Range = 150,
        Duration = 0,
        Effect = SpellEffect.None,
        DamageDice = "8d6",
        DamageType = DamageType.Fire,
        SaveAbility = "DEX",
        SaveDC = saveDC,
        AreaRadiusFeet = 20,
        HalfDamageOnSave = true,
        Description = "8d6 fire damage in a 20 ft radius; DEX save for half"
    };

    /// <summary>
    /// Flame Strike (PHB): 5th-level Evocation. A vertical column of divine flame. Each creature in a
    /// 10-foot radius, 40-foot high cylinder makes a DEX save. On a failure, 4d6 fire + 4d6 radiant damage;
    /// half on a success. Simplified here as 8d6 Fire (combined pool).
    /// </summary>
    public static Spell FlameStrike(int saveDC) => new()
    {
        Name = "Flame Strike",
        Level = 5,
        School = "Evocation",
        RequiresConcentration = false,
        Range = 60,
        Duration = 0,
        Effect = SpellEffect.None,
        DamageDice = "8d6",
        DamageType = DamageType.Fire,
        SaveAbility = "DEX",
        SaveDC = saveDC,
        AreaRadiusFeet = 10,
        HalfDamageOnSave = true,
        Description = "8d6 fire/radiant damage in a 10 ft radius; DEX save for half"
    };
}
