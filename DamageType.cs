namespace _4DND;

/// <summary>
/// The 13 official damage types from D&amp;D 5e (PHB "Damage Types").
/// Damage types have no rules of their own, but other rules — such as
/// damage resistance, immunity, and vulnerability — rely on them.
/// </summary>
public enum DamageType
{
    None,
    /// <summary>Corrosive spray, dissolving enzymes (e.g., black dragon breath, black pudding).</summary>
    Acid,
    /// <summary>Blunt force attacks — hammers, falling, constriction (e.g., club, maul).</summary>
    Bludgeoning,
    /// <summary>Infernal chill radiating from ice devils or a white dragon's breath.</summary>
    Cold,
    /// <summary>Red dragon breath, spells that conjure flames (e.g., fireball).</summary>
    Fire,
    /// <summary>Pure magical energy — most effects that deal force damage are spells (e.g., magic missile, spiritual weapon).</summary>
    Force,
    /// <summary>Lightning bolt spell, blue dragon breath.</summary>
    Lightning,
    /// <summary>Dealt by certain undead and spells such as chill touch.</summary>
    Necrotic,
    /// <summary>Puncturing and impaling attacks — spears, monsters' bites (e.g., dagger, rapier).</summary>
    Piercing,
    /// <summary>Venomous stings, toxic gas of a green dragon's breath.</summary>
    Poison,
    /// <summary>Mental abilities such as a mind flayer's psionic blast.</summary>
    Psychic,
    /// <summary>A cleric's flame strike, an angel's smiting weapon.</summary>
    Radiant,
    /// <summary>Swords, axes, monsters' claws (e.g., longsword, greataxe).</summary>
    Slashing,
    /// <summary>A concussive burst of sound — thunderwave and similar spells.</summary>
    Thunder
}

public static class DamageTypeExtensions
{
    /// <summary>
    /// Returns the lowercase display name of the damage type, matching D&amp;D conventions.
    /// </summary>
    public static string ToDisplayString(this DamageType damageType) => damageType switch
    {
        DamageType.None          => "",
        DamageType.Acid          => "acid",
        DamageType.Bludgeoning   => "bludgeoning",
        DamageType.Cold          => "cold",
        DamageType.Fire          => "fire",
        DamageType.Force         => "force",
        DamageType.Lightning     => "lightning",
        DamageType.Necrotic      => "necrotic",
        DamageType.Piercing      => "piercing",
        DamageType.Poison        => "poison",
        DamageType.Psychic       => "psychic",
        DamageType.Radiant       => "radiant",
        DamageType.Slashing      => "slashing",
        DamageType.Thunder       => "thunder",
        _                        => damageType.ToString().ToLowerInvariant()
    };
}
