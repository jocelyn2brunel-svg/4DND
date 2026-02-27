using System.Collections.Generic;

namespace _4DND;

/// <summary>The ten dragon types available as Dragonborn draconic ancestry (PHB "Draconic Ancestry" table).</summary>
public enum DragonAncestryType
{
    Black,
    Blue,
    Brass,
    Bronze,
    Copper,
    Gold,
    Green,
    Red,
    Silver,
    White
}

/// <summary>The shape of a Dragonborn breath weapon area of effect.</summary>
public enum BreathWeaponShape
{
    /// <summary>5 ft. wide by 30 ft. long line.</summary>
    Line5x30,
    /// <summary>15 ft. cone.</summary>
    Cone15
}

/// <summary>
/// Data for one row of the PHB "Draconic Ancestry" table:
/// the associated damage type, breath weapon shape, and saving throw ability.
/// </summary>
public record DraconicAncestryEntry(
    DamageType DamageType,
    BreathWeaponShape BreathWeaponShape,
    string SavingThrow
);
