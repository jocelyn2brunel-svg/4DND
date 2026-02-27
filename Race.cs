using System.Collections.Generic;

namespace _4DND;

public class Race
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public CreatureSize Size { get; set; } = CreatureSize.Medium;
    public int StrengthBonus { get; set; }
    public int DexterityBonus { get; set; }
    public int ConstitutionBonus { get; set; }
    public int IntelligenceBonus { get; set; }
    public int WisdomBonus { get; set; }
    public int CharismaBonus { get; set; }
    public int BaseSpeed { get; set; } = 30;
    public string Description { get; set; } = "";
    
    // Vision traits
    public int DarkvisionRange { get; set; } = 0;  // In feet, 0 means no darkvision
    public bool HasSuperiorDarkvision { get; set; } = false;
    public bool HasSunlightSensitivity { get; set; } = false;
    
    // Dwarf-specific traits
    /// <summary>Dwarven Resilience: advantage on saving throws against poison, resistance to poison damage.</summary>
    public bool HasDwarvenResilience { get; set; } = false;
    /// <summary>Dwarven Combat Training: proficiency with battleaxe, handaxe, light hammer, and warhammer.</summary>
    public bool HasDwarvenCombatTraining { get; set; } = false;
    /// <summary>Stonecunning: double proficiency bonus on Intelligence (History) checks related to stonework.</summary>
    public bool HasStonecunning { get; set; } = false;

    // Elf-specific traits
    /// <summary>Keen Senses: proficiency in the Perception skill (PHB "Elf Traits").</summary>
    public bool HasKeenSenses { get; set; } = false;
    /// <summary>Fey Ancestry: advantage on saving throws against being charmed, and magic can't put you to sleep (PHB "Elf Traits").</summary>
    public bool HasFeyAncestry { get; set; } = false;
    /// <summary>Trance: elves don't need to sleep; they meditate for 4 hours a day to gain the benefits of 8 hours of sleep (PHB "Elf Traits").</summary>
    public bool HasTrance { get; set; } = false;
    /// <summary>Elf Weapon Training: proficiency with longsword, shortsword, shortbow, and longbow (High Elf and Wood Elf).</summary>
    public bool HasElfWeaponTraining { get; set; } = false;
    /// <summary>High Elf Cantrip: knows one cantrip of their choice from the wizard spell list; Intelligence is the spellcasting ability.</summary>
    public bool HasHighElfCantrip { get; set; } = false;
    /// <summary>Extra Language: can speak, read, and write one extra language of their choice (High Elf).</summary>
    public bool HasExtraLanguage { get; set; } = false;
    /// <summary>Mask of the Wild: can attempt to hide even when only lightly obscured by foliage, heavy rain, falling snow, mist, and other natural phenomena (Wood Elf).</summary>
    public bool HasMaskOfTheWild { get; set; } = false;
    /// <summary>Drow Magic: knows the dancing lights cantrip; can cast faerie fire once per day at 3rd level, and darkness once per day at 5th level. Charisma is the spellcasting ability (Drow).</summary>
    public bool HasDrowMagic { get; set; } = false;
    /// <summary>Drow Weapon Training: proficiency with rapiers, shortswords, and hand crossbows (Drow).</summary>
    public bool HasDrowWeaponTraining { get; set; } = false;

    // Halfling-specific traits
    /// <summary>Lucky: when you roll a 1 on an attack roll, ability check, or saving throw, you can reroll the die and must use the new roll.</summary>
    public bool HasLucky { get; set; } = false;
    /// <summary>Brave: you have advantage on saving throws against being frightened.</summary>
    public bool HasBrave { get; set; } = false;
    /// <summary>Halfling Nimbleness: you can move through the space of any creature that is of a size larger than yours.</summary>
    public bool HasHalflingNimbleness { get; set; } = false;

    /// <summary>Languages this race speaks, reads, and writes.</summary>
    public List<string> Languages { get; set; } = new();
    /// <summary>Tool proficiency options to choose from (player picks one). Empty means no choice.</summary>
    public List<string> ToolProficiencyChoices { get; set; } = new();

    public static readonly Dictionary<string, Race> AllRaces = new()
    {
        // Human
        ["Human"] = new Race
        {
            Name = "Human",
            DisplayName = Loc.Tr("Human"),
            Size = CreatureSize.Medium,
            StrengthBonus = 1,
            DexterityBonus = 1,
            ConstitutionBonus = 1,
            IntelligenceBonus = 1,
            WisdomBonus = 1,
            CharismaBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 0,
            Description = Loc.Tr("Versatile and adaptable (+1 to all abilities)")
        },
        
        // Elves
        ["High Elf"] = new Race
        {
            Name = "High Elf",
            DisplayName = Loc.Tr("Elf (High)"),
            Size = CreatureSize.Medium,
            DexterityBonus = 2,
            IntelligenceBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            HasKeenSenses = true,
            HasFeyAncestry = true,
            HasTrance = true,
            HasElfWeaponTraining = true,
            HasHighElfCantrip = true,
            HasExtraLanguage = true,
            Languages = new List<string> { "Common", "Elvish" },
            Description = Loc.Tr("Graceful and intelligent (+2 DEX, +1 INT, Darkvision 60 ft, Keen Senses, Fey Ancestry, Trance, Elf Weapon Training, Cantrip, Extra Language)")
        },
        ["Wood Elf"] = new Race
        {
            Name = "Wood Elf",
            DisplayName = Loc.Tr("Elf (Wood)"),
            Size = CreatureSize.Medium,
            DexterityBonus = 2,
            WisdomBonus = 1,
            BaseSpeed = 35,
            DarkvisionRange = 60,
            HasKeenSenses = true,
            HasFeyAncestry = true,
            HasTrance = true,
            HasElfWeaponTraining = true,
            HasMaskOfTheWild = true,
            Languages = new List<string> { "Common", "Elvish" },
            Description = Loc.Tr("Swift and wise (+2 DEX, +1 WIS, 35 ft speed, Darkvision 60 ft, Keen Senses, Fey Ancestry, Trance, Elf Weapon Training, Mask of the Wild)")
        },
        
        ["Drow"] = new Race
        {
            Name = "Drow",
            DisplayName = Loc.Tr("Elf (Drow)"),
            Size = CreatureSize.Medium,
            DexterityBonus = 2,
            CharismaBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 120,
            HasSuperiorDarkvision = true,
            HasSunlightSensitivity = true,
            HasKeenSenses = true,
            HasFeyAncestry = true,
            HasTrance = true,
            HasDrowMagic = true,
            HasDrowWeaponTraining = true,
            Languages = new List<string> { "Common", "Elvish" },
            Description = Loc.Tr("Dark elf with superior darkvision (+2 DEX, +1 CHA, Darkvision 120 ft, Sunlight Sensitivity, Keen Senses, Fey Ancestry, Trance, Drow Magic, Drow Weapon Training)")
        },
        
        // Dwarves
        ["Hill Dwarf"] = new Race
        {
            Name = "Hill Dwarf",
            DisplayName = Loc.Tr("Dwarf (Hill)"),
            Size = CreatureSize.Medium,
            ConstitutionBonus = 2,
            WisdomBonus = 1,
            BaseSpeed = 25,
            DarkvisionRange = 60,
            HasDwarvenResilience = true,
            HasDwarvenCombatTraining = true,
            HasStonecunning = true,
            Languages = new List<string> { "Common", "Dwarvish" },
            ToolProficiencyChoices = new List<string> { "Smith's tools", "Brewer's supplies", "Mason's tools" },
            Description = Loc.Tr("Tough and wise (+2 CON, +1 WIS, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)")
        },
        ["Mountain Dwarf"] = new Race
        {
            Name = "Mountain Dwarf",
            DisplayName = Loc.Tr("Dwarf (Mountain)"),
            Size = CreatureSize.Medium,
            StrengthBonus = 2,
            ConstitutionBonus = 2,
            BaseSpeed = 25,
            DarkvisionRange = 60,
            HasDwarvenResilience = true,
            HasDwarvenCombatTraining = true,
            HasStonecunning = true,
            Languages = new List<string> { "Common", "Dwarvish" },
            ToolProficiencyChoices = new List<string> { "Smith's tools", "Brewer's supplies", "Mason's tools" },
            Description = Loc.Tr("Strong and hardy (+2 STR, +2 CON, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)")
        },
        
        // Halflings
        ["Lightfoot Halfling"] = new Race
        {
            Name = "Lightfoot Halfling",
            DisplayName = Loc.Tr("Halfling (Lightfoot)"),
            Size = CreatureSize.Small,
            DexterityBonus = 2,
            CharismaBonus = 1,
            BaseSpeed = 25,
            DarkvisionRange = 0,
            HasLucky = true,
            HasBrave = true,
            HasHalflingNimbleness = true,
            Languages = new List<string> { "Common", "Halfling" },
            Description = Loc.Tr("Nimble and charming (+2 DEX, +1 CHA, Lucky, Brave, Halfling Nimbleness)")
        },
        ["Stout Halfling"] = new Race
        {
            Name = "Stout Halfling",
            DisplayName = Loc.Tr("Halfling (Stout)"),
            Size = CreatureSize.Small,
            DexterityBonus = 2,
            ConstitutionBonus = 1,
            BaseSpeed = 25,
            DarkvisionRange = 0,
            HasLucky = true,
            HasBrave = true,
            HasHalflingNimbleness = true,
            HasDwarvenResilience = true,
            Languages = new List<string> { "Common", "Halfling" },
            Description = Loc.Tr("Nimble and resilient (+2 DEX, +1 CON, Lucky, Brave, Halfling Nimbleness, Stout Resilience)")
        },
        
        ["Half-Orc"] = new Race
        {
            Name = "Half-Orc",
            DisplayName = Loc.Tr("Half-Orc"),
            Size = CreatureSize.Medium,
            StrengthBonus = 2,
            ConstitutionBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            Description = Loc.Tr("Strong and tough (+2 STR, +1 CON, Darkvision 60 ft)")
        },
        
        ["Tiefling"] = new Race
        {
            Name = "Tiefling",
            DisplayName = Loc.Tr("Tiefling"),
            Size = CreatureSize.Medium,
            CharismaBonus = 2,
            IntelligenceBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            Description = Loc.Tr("Infernal heritage with darkvision (+2 CHA, +1 INT, Darkvision 60 ft)")
        },
        
        ["Dragonborn"] = new Race
        {
            Name = "Dragonborn",
            DisplayName = Loc.Tr("Dragonborn"),
            Size = CreatureSize.Medium,
            StrengthBonus = 2,
            CharismaBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 0,
            Description = Loc.Tr("Draconic heritage (+2 STR, +1 CHA)")
        },
        
        ["Gnome"] = new Race
        {
            Name = "Gnome",
            DisplayName = Loc.Tr("Gnome"),
            Size = CreatureSize.Small,
            IntelligenceBonus = 2,
            BaseSpeed = 25,
            DarkvisionRange = 60,
            Description = Loc.Tr("Small and clever (+2 INT, Darkvision 60 ft)")
        },
        
        ["Half-Elf"] = new Race
        {
            Name = "Half-Elf",
            DisplayName = Loc.Tr("Half-Elf"),
            Size = CreatureSize.Medium,
            CharismaBonus = 2,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            HasFeyAncestry = true,
            Languages = new List<string> { "Common", "Elvish" },
            Description = Loc.Tr("Versatile and charismatic (+2 CHA, +1 to two other abilities, Darkvision 60 ft, Fey Ancestry)")
        }
    };
    
    public static Race GetRace(string name)
    {
        return AllRaces.TryGetValue(name, out var race) ? race : AllRaces["Human"];
    }
    
    public static List<string> GetAllRaceNames()
    {
        return new List<string>(AllRaces.Keys);
    }
}
