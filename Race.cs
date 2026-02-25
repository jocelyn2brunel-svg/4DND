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
            DisplayName = "Human",
            Size = CreatureSize.Medium,
            StrengthBonus = 1,
            DexterityBonus = 1,
            ConstitutionBonus = 1,
            IntelligenceBonus = 1,
            WisdomBonus = 1,
            CharismaBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 0,
            Description = "Versatile and adaptable (+1 to all abilities)"
        },
        
        // Elves
        ["High Elf"] = new Race
        {
            Name = "High Elf",
            DisplayName = "Elf (High)",
            Size = CreatureSize.Medium,
            DexterityBonus = 2,
            IntelligenceBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            Description = "Graceful and intelligent (+2 DEX, +1 INT, Darkvision 60 ft)"
        },
        ["Wood Elf"] = new Race
        {
            Name = "Wood Elf",
            DisplayName = "Elf (Wood)",
            Size = CreatureSize.Medium,
            DexterityBonus = 2,
            WisdomBonus = 1,
            BaseSpeed = 35,
            DarkvisionRange = 60,
            Description = "Swift and wise (+2 DEX, +1 WIS, 35 ft speed, Darkvision 60 ft)"
        },
        
        ["Drow"] = new Race
        {
            Name = "Drow",
            DisplayName = "Elf (Drow)",
            Size = CreatureSize.Medium,
            DexterityBonus = 2,
            CharismaBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 120,
            HasSuperiorDarkvision = true,
            HasSunlightSensitivity = true,
            Description = "Dark elf with superior darkvision (+2 DEX, +1 CHA, Darkvision 120 ft, Sunlight Sensitivity)"
        },
        
        // Dwarves
        ["Hill Dwarf"] = new Race
        {
            Name = "Hill Dwarf",
            DisplayName = "Dwarf (Hill)",
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
            Description = "Tough and wise (+2 CON, +1 WIS, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)"
        },
        ["Mountain Dwarf"] = new Race
        {
            Name = "Mountain Dwarf",
            DisplayName = "Dwarf (Mountain)",
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
            Description = "Strong and hardy (+2 STR, +2 CON, Darkvision 60 ft, Dwarven Resilience, Dwarven Combat Training)"
        },
        
        // Halflings
        ["Lightfoot Halfling"] = new Race
        {
            Name = "Lightfoot Halfling",
            DisplayName = "Halfling (Lightfoot)",
            Size = CreatureSize.Small,
            DexterityBonus = 2,
            CharismaBonus = 1,
            BaseSpeed = 25,
            DarkvisionRange = 0,
            Description = "Nimble and charming (+2 DEX, +1 CHA)"
        },
        ["Stout Halfling"] = new Race
        {
            Name = "Stout Halfling",
            DisplayName = "Halfling (Stout)",
            Size = CreatureSize.Small,
            DexterityBonus = 2,
            ConstitutionBonus = 1,
            BaseSpeed = 25,
            DarkvisionRange = 0,
            Description = "Nimble and resilient (+2 DEX, +1 CON)"
        },
        
        ["Half-Orc"] = new Race
        {
            Name = "Half-Orc",
            DisplayName = "Half-Orc",
            Size = CreatureSize.Medium,
            StrengthBonus = 2,
            ConstitutionBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            Description = "Strong and tough (+2 STR, +1 CON, Darkvision 60 ft)"
        },
        
        ["Tiefling"] = new Race
        {
            Name = "Tiefling",
            DisplayName = "Tiefling",
            Size = CreatureSize.Medium,
            CharismaBonus = 2,
            IntelligenceBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            Description = "Infernal heritage with darkvision (+2 CHA, +1 INT, Darkvision 60 ft)"
        },
        
        ["Dragonborn"] = new Race
        {
            Name = "Dragonborn",
            DisplayName = "Dragonborn",
            Size = CreatureSize.Medium,
            StrengthBonus = 2,
            CharismaBonus = 1,
            BaseSpeed = 30,
            DarkvisionRange = 0,
            Description = "Draconic heritage (+2 STR, +1 CHA)"
        },
        
        ["Gnome"] = new Race
        {
            Name = "Gnome",
            DisplayName = "Gnome",
            Size = CreatureSize.Small,
            IntelligenceBonus = 2,
            BaseSpeed = 25,
            DarkvisionRange = 60,
            Description = "Small and clever (+2 INT, Darkvision 60 ft)"
        },
        
        ["Half-Elf"] = new Race
        {
            Name = "Half-Elf",
            DisplayName = "Half-Elf",
            Size = CreatureSize.Medium,
            CharismaBonus = 2,
            BaseSpeed = 30,
            DarkvisionRange = 60,
            Description = "Versatile and charismatic (+2 CHA, +1 to two other abilities, Darkvision 60 ft)"
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
