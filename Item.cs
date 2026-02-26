namespace _4DND;

public enum ItemType
{
    Weapon,
    Armor,
    Shield,
    Consumable,
    Treasure,
    Misc
}

public enum WeaponType
{
    None,
    Simple,
    Martial
}

public enum ArmorType
{
    None,
    Light,
    Medium,
    Heavy,
    Shield
}

public class Item
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ItemType Type { get; set; }
    public int Weight { get; set; } // in pounds
    public int Value { get; set; } // in gold pieces
    public bool IsEquippable { get; set; }
    
    // Weapon properties
    public WeaponType WeaponCategory { get; set; }
    public string DamageDice { get; set; } = ""; // e.g., "1d8"
    public DamageType DamageType { get; set; } = DamageType.None;
    public bool IsLight { get; set; }
    public bool IsFinesse { get; set; }
    public bool IsTwoHanded { get; set; }
    public bool IsVersatile { get; set; }
    public string VersatileDamageDice { get; set; } = "";
    /// <summary>True for dedicated ranged weapons (bow, crossbow, sling, blowgun).</summary>
    public bool IsRanged { get; set; }
    /// <summary>
    /// True for melee weapons that can also be thrown (e.g. Dagger, Handaxe, Javelin).
    /// Thrown weapons are melee by default; the thrown range is stored in Range/LongRange.
    /// </summary>
    public bool IsThrown { get; set; }
    public int Range { get; set; }      // Normal range in feet
    public int LongRange { get; set; }  // Maximum range in feet (0 = Range * 3 for most weapons)
    /// <summary>
    /// True for weapons with the Ammunition property (bows, crossbows, sling, blowgun).
    /// Requires ammunition from the inventory to make a ranged attack (PHB "Ammunition").
    /// When used to make a melee attack, the weapon is treated as an improvised weapon.
    /// </summary>
    public bool IsAmmunition { get; set; }
    /// <summary>
    /// The ItemDatabase key for the ammunition this weapon requires (e.g. "Ammunition - Arrows (20)").
    /// Empty string for weapons that do not have the Ammunition property.
    /// </summary>
    public string AmmoType { get; set; } = "";
    /// <summary>
    /// For ammunition items: the number of units in a full pack (e.g. 20 for arrows, 50 for blowgun needles).
    /// 0 for non-ammunition items.
    /// </summary>
    public int DefaultQuantity { get; set; } = 0;
    
    // Armor properties
    public ArmorType ArmorCategory { get; set; }
    public int ArmorClass { get; set; }
    public int MaxDexBonus { get; set; } = 10; // 10 means unlimited
    public int StrengthRequirement { get; set; } // Minimum Strength score required to wear
    public bool StealthDisadvantage { get; set; }
    
    // Stat modifiers
    public int StrengthModifier { get; set; }
    public int DexterityModifier { get; set; }
    public int ConstitutionModifier { get; set; }
    public int IntelligenceModifier { get; set; }
    public int WisdomModifier { get; set; }
    public int CharismaModifier { get; set; }
}
