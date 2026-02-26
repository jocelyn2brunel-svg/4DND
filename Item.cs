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
    public bool IsRanged { get; set; }
    public int Range { get; set; }      // Normal range in feet
    public int LongRange { get; set; }  // Maximum range in feet (0 = Range * 3 for most weapons)
    
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
