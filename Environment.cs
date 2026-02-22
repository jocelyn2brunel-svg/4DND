namespace _4DND;

public enum EnvironmentType
{
    Dungeon,
    Cave,
    Forest,
    Swamp,
    Desert,
    Underground,
    Underwater
}

public class Environment
{
    public EnvironmentType Type { get; set; }
    public bool HasNaturalLight { get; set; }
    public bool HasFog { get; set; }
    public bool HasDenseVegetation { get; set; }
    
    public static Environment Dungeon => new()
    {
        Type = EnvironmentType.Dungeon,
        HasNaturalLight = false,
        HasFog = false,
        HasDenseVegetation = false
    };
    
    public static Environment Cave => new()
    {
        Type = EnvironmentType.Cave,
        HasNaturalLight = false,
        HasFog = false,
        HasDenseVegetation = false
    };
    
    public static Environment Forest => new()
    {
        Type = EnvironmentType.Forest,
        HasNaturalLight = true,
        HasFog = false,
        HasDenseVegetation = true
    };
    
    public static Environment Swamp => new()
    {
        Type = EnvironmentType.Swamp,
        HasNaturalLight = true,
        HasFog = true,
        HasDenseVegetation = true
    };
    
    public static Environment Underground => new()
    {
        Type = EnvironmentType.Underground,
        HasNaturalLight = false,
        HasFog = false,
        HasDenseVegetation = false
    };
}
