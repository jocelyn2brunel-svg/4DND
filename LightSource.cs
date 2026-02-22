using Microsoft.Xna.Framework;

#nullable enable

namespace _4DND;

public enum LightType
{
    Bright,      // Normal vision
    Dim,         // Lightly obscured, disadvantage on Perception
    Darkness     // Heavily obscured, blinded condition
}

public class LightSource
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public int BrightRadius { get; set; }  // In feet (tiles * 5)
    public int DimRadius { get; set; }      // In feet (tiles * 5)
    public Color LightColor { get; set; } = Color.White;
    public bool IsActive { get; set; } = true;
    public Creature? AttachedTo { get; set; } = null;  // If attached to a creature, moves with them
    
    public LightSource(int x, int y, int z, int brightRadius, int dimRadius)
    {
        X = x;
        Y = y;
        Z = z;
        BrightRadius = brightRadius;
        DimRadius = dimRadius;
    }
    
    // Common light sources from D&D 5e
    public static LightSource Torch(int x, int y, int z = 0) => new(x, y, z, 20, 40) { LightColor = Color.Orange };
    public static LightSource Lantern(int x, int y, int z = 0) => new(x, y, z, 30, 60) { LightColor = Color.Yellow };
    public static LightSource Candle(int x, int y, int z = 0) => new(x, y, z, 5, 10) { LightColor = Color.Orange };
    public static LightSource LightSpell(int x, int y, int z = 0) => new(x, y, z, 20, 40) { LightColor = Color.White };
    public static LightSource DaylightSpell(int x, int y, int z = 0) => new(x, y, z, 60, 120) { LightColor = Color.White };
}
