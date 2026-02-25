using System;
using System.Collections.Generic;

namespace _4DND;

public record ClassLevelData(int Level, string Features, int Rages, int RageDamage);

/// <summary>
/// Bard level progression data following D&amp;D 5e rules.
/// BardicInspirationDice is the die type (6, 8, 10, or 12).
/// SpellSlots is a per-level array [slot1, slot2, slot3, slot4, slot5].
/// </summary>
public record BardLevelData(int Level, string Features, int BardicInspirationDice, int CantripsKnown, int SpellsKnown, int[] SpellSlots);

/// <summary>
/// Cleric level progression data following D&amp;D 5e rules.
/// CantripsKnown is the number of cantrips known.
/// SpellSlots is a per-level array [slot1, slot2, slot3, slot4, slot5, slot6, slot7, slot8, slot9].
/// ChannelDivinityUses is the number of Channel Divinity uses per rest (0 if not yet unlocked).
/// </summary>
public record ClericLevelData(int Level, string Features, int CantripsKnown, int[] SpellSlots, int ChannelDivinityUses);

/// <summary>
/// Determines how a class accesses its spells.
/// Known-spell casters (Bard, Sorcerer, Warlock, Ranger) always have their learned spells available to cast.
/// Prepared-spell casters (Cleric, Druid, Paladin, Wizard) must choose which spells to prepare after a long rest;
/// the number they can prepare depends on their level and spellcasting ability modifier.
/// </summary>
public enum SpellcastingType
{
    None,       // Non-casters (Barbarian, Fighter, Monk, etc.)
    Known,      // Bard, Sorcerer, Warlock, Ranger: spells known are always available to cast
    Prepared    // Cleric, Druid, Paladin, Wizard: must prepare a subset of known spells each day
}

public class ClassData
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int HitDice { get; set; } = 6;
    public string PrimaryAbility { get; set; } = "";
    public List<string> SavingThrowProficiencies { get; set; } = new();
    public List<string> ArmorProficiencies { get; set; } = new();
    public List<string> WeaponProficiencies { get; set; } = new();
    public List<string> ToolProficiencies { get; set; } = new();
    public int SkillChoicesCount { get; set; } = 0;
    public List<string> SkillChoiceOptions { get; set; } = new();
    public List<ClassLevelData> LevelProgression { get; set; } = new();
    public List<BardLevelData> BardLevelProgression { get; set; } = new();
    public List<ClericLevelData> ClericLevelProgression { get; set; } = new();
    /// <summary>How this class accesses its spells (None for non-casters, Known for Bard/Sorcerer/Warlock/Ranger, Prepared for Cleric/Druid/Paladin/Wizard).</summary>
    public SpellcastingType SpellcastingType { get; set; } = SpellcastingType.None;
    /// <summary>The ability score used for spellcasting ("Intelligence", "Wisdom", or "Charisma"). Empty for non-casters.</summary>
    public string SpellcastingAbility { get; set; } = "";

    public ClassLevelData GetLevelData(int level)
    {
        foreach (var entry in LevelProgression)
            if (entry.Level == level) return entry;
        return null;
    }
    
    public BardLevelData GetBardLevelData(int level)
    {
        foreach (var entry in BardLevelProgression)
            if (entry.Level == level) return entry;
        return null;
    }

    public ClericLevelData GetClericLevelData(int level)
    {
        foreach (var entry in ClericLevelProgression)
            if (entry.Level == level) return entry;
        return null;
    }

    /// <summary>
    /// Returns the maximum number of non-cantrip spells this class can have prepared at the given character level.
    /// For most Prepared casters the cap is the spellcasting ability modifier plus character level;
    /// for Paladins it is the modifier plus half the character level (rounded down). Minimum is always 1.
    /// Returns 0 for non-Prepared casters.
    /// </summary>
    public int GetMaxPreparedSpells(int level, int spellcastingAbilityModifier)
    {
        if (SpellcastingType != SpellcastingType.Prepared) return 0;
        int max = Name == "Paladin"
            ? spellcastingAbilityModifier + (level / 2)
            : spellcastingAbilityModifier + level;
        return Math.Max(1, max);
    }

    private static readonly Dictionary<string, ClassData> _classDatabase = new();
    
    static ClassData()
    {
        InitializeClasses();
    }
    
    private static void InitializeClasses()
    {
        _classDatabase["Barbarian"] = new ClassData
        {
            Name = "Barbarian",
            Description = "A fierce warrior of primitive background who can enter a battle rage",
            HitDice = 12,
            PrimaryAbility = "Strength",
            SavingThrowProficiencies = new List<string> { "Strength", "Constitution" },
            ArmorProficiencies = new List<string> { "Light armor", "Medium armor", "Shields" },
            WeaponProficiencies = new List<string> { "Simple weapons", "Martial weapons" },
            ToolProficiencies = new List<string>(),
            SkillChoicesCount = 2,
            SkillChoiceOptions = new List<string>
            {
                "Animal Handling",
                "Athletics",
                "Intimidation",
                "Nature",
                "Perception",
                "Survival"
            },
            LevelProgression = new List<ClassLevelData>
            {
                new(1,  "Rage, Unarmored Defense",        2,  2),
                new(2,  "Reckless Attack, Danger Sense",  2,  2),
                new(3,  "Primal Path",                    3,  2),
                new(4,  "Ability Score Improvement",      3,  2),
                new(5,  "Extra Attack, Fast Movement",    3,  2),
                new(6,  "Path feature",                   4,  2),
                new(7,  "Feral Instinct",                 4,  2),
                new(8,  "Ability Score Improvement",      4,  2),
                new(9,  "Brutal Critical (1 die)",        4,  3),
                new(10, "Path feature",                   4,  3),
                new(11, "Relentless Rage",                4,  3),
                new(12, "Ability Score Improvement",      5,  3),
                new(13, "Brutal Critical (2 dice)",       5,  3),
                new(14, "Path feature",                   5,  3),
                new(15, "Persistent Rage",                5,  3),
                new(16, "Ability Score Improvement",      5,  4),
                new(17, "Brutal Critical (3 dice)",       6,  4),
                new(18, "Indomitable Might",              6,  4),
                new(19, "Ability Score Improvement",      6,  4),
                new(20, "Primal Champion",               -1,  4), // -1 = Unlimited
            }
        };
        
        _classDatabase["Bard"] = new ClassData
        {
            Name = "Bard",
            Description = "An inspiring magician whose power echoes the music of creation",
            HitDice = 8,
            PrimaryAbility = "Charisma",
            SavingThrowProficiencies = new List<string> { "Dexterity", "Charisma" },
            ArmorProficiencies = new List<string> { "Light armor" },
            WeaponProficiencies = new List<string> { "Simple weapons", "Hand crossbows", "Longswords", "Rapiers", "Shortswords" },
            ToolProficiencies = new List<string> { "Three musical instruments of your choice" },
            SkillChoicesCount = 3,
            SkillChoiceOptions = new List<string>
            {
                "Acrobatics", "Animal Handling", "Arcana", "Athletics", "Deception",
                "History", "Insight", "Intimidation", "Investigation", "Medicine",
                "Nature", "Perception", "Performance", "Persuasion", "Religion",
                "Sleight of Hand", "Stealth", "Survival"
            },
            BardLevelProgression = new List<BardLevelData>
            {
                //                                                          Slots: 1  2  3  4  5  6  7  8  9
                new(1,  "Bardic Inspiration (d6), Spellcasting",        6, 2,  4, new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 }),
                new(2,  "Jack of All Trades, Song of Rest (d6)",        6, 2,  5, new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 }),
                new(3,  "Bard College, Expertise",                      6, 2,  6, new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 }),
                new(4,  "Ability Score Improvement",                    6, 3,  7, new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 }),
                new(5,  "Bardic Inspiration (d8), Font of Inspiration", 8, 3,  8, new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 }),
                new(6,  "Countercharm, Bard College feature",           8, 3,  9, new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 }),
                new(7,  "",                                            8, 3, 10, new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 }),
                new(8,  "Ability Score Improvement",                    8, 3, 11, new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 }),
                new(9,  "Song of Rest (d8)",                            8, 3, 12, new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 }),
                new(10, "Bardic Inspiration (d10), Expertise, Magical Secrets", 10, 4, 14, new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 }),
                new(11, "",                                           10, 4, 15, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 }),
                new(12, "Ability Score Improvement",                   10, 4, 15, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 }),
                new(13, "Song of Rest (d10)",                          10, 4, 16, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 }),
                new(14, "Magical Secrets, Bard College feature",       10, 4, 18, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 }),
                new(15, "Bardic Inspiration (d12)",                    12, 4, 19, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 }),
                new(16, "Ability Score Improvement",                   12, 4, 19, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 }),
                new(17, "Song of Rest (d12)",                          12, 4, 20, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 1 }),
                new(18, "Magical Secrets",                             12, 4, 22, new[] { 4, 3, 3, 3, 3, 1, 1, 1, 1 }),
                new(19, "Ability Score Improvement",                   12, 4, 22, new[] { 4, 3, 3, 3, 3, 2, 1, 1, 1 }),
                new(20, "Superior Inspiration",                        12, 4, 22, new[] { 4, 3, 3, 3, 3, 2, 2, 1, 1 }),
            },
            SpellcastingType = SpellcastingType.Known,
            SpellcastingAbility = "Charisma"
        };

        _classDatabase["Cleric"] = new ClassData
        {
            Name = "Cleric",
            Description = "A priestly champion who wields divine magic in service of a higher power",
            HitDice = 8,
            PrimaryAbility = "Wisdom",
            SavingThrowProficiencies = new List<string> { "Wisdom", "Charisma" },
            ArmorProficiencies = new List<string> { "Light armor", "Medium armor", "Shields" },
            WeaponProficiencies = new List<string> { "Simple weapons" },
            SkillChoicesCount = 2,
            SkillChoiceOptions = new List<string>
            {
                "History", "Insight", "Medicine", "Persuasion", "Religion"
            },
            SpellcastingType = SpellcastingType.Prepared,
            SpellcastingAbility = "Wisdom",
            ClericLevelProgression = new List<ClericLevelData>
            {
                //                                                              Cantrips  Slots: 1  2  3  4  5  6  7  8  9   Channel
                new(1,  "Spellcasting, Divine Domain",                         3, new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 }, 0),
                new(2,  "Channel Divinity (1/rest), Divine Domain feature",    3, new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 }, 1),
                new(3,  "\x97",                                                   3, new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 }, 1),
                new(4,  "Ability Score Improvement",                           4, new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 }, 1),
                new(5,  "Destroy Undead (CR 1/2)",                             4, new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 }, 1),
                new(6,  "Channel Divinity (2/rest), Divine Domain feature",    4, new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 }, 2),
                new(7,  "\x97",                                                   4, new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 }, 2),
                new(8,  "Ability Score Improvement, Destroy Undead (CR 1), Divine Domain feature", 4, new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 }, 2),
                new(9,  "\x97",                                                   4, new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 }, 2),
                new(10, "Divine Intervention",                                 5, new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 }, 2),
                new(11, "Destroy Undead (CR 2)",                               5, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 }, 2),
                new(12, "Ability Score Improvement",                           5, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 }, 2),
                new(13, "\x97",                                                   5, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 }, 2),
                new(14, "Destroy Undead (CR 3)",                               5, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 }, 2),
                new(15, "\x97",                                                   5, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 }, 2),
                new(3,  "",                                                   3, new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 }, 1),
                new(4,  "Ability Score Improvement",                           4, new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 }, 1),
                new(5,  "Destroy Undead (CR 1/2)",                             4, new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 }, 1),
                new(6,  "Channel Divinity (2/rest), Divine Domain feature",    4, new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 }, 2),
                new(7,  "",                                                   4, new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 }, 2),
                new(8,  "Ability Score Improvement, Destroy Undead (CR 1), Divine Domain feature", 4, new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 }, 2),
                new(9,  "",                                                   4, new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 }, 2),
                new(10, "Divine Intervention",                                 5, new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 }, 2),
                new(11, "Destroy Undead (CR 2)",                               5, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 }, 2),
                new(12, "Ability Score Improvement",                           5, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 }, 2),
                new(13, "",                                                   5, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 }, 2),
                new(14, "Destroy Undead (CR 3)",                               5, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 }, 2),
                new(15, "",                                                   5, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 }, 2),
                new(16, "Ability Score Improvement",                           5, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 }, 2),
                new(17, "Destroy Undead (CR 4), Divine Domain feature",        5, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 1 }, 2),
                new(18, "Channel Divinity (3/rest)",                           5, new[] { 4, 3, 3, 3, 3, 1, 1, 1, 1 }, 3),
                new(19, "Ability Score Improvement",                           5, new[] { 4, 3, 3, 3, 3, 2, 1, 1, 1 }, 3),
                new(20, "Divine Intervention improvement",                     5, new[] { 4, 3, 3, 3, 3, 2, 2, 1, 1 }, 3),
            }
        };

        _classDatabase["Druid"] = new ClassData
        {
            Name = "Druid",
            Description = "A priest of the Old Faith, wielding the powers of nature - moonlight and plant growth, fire and lightning - and adopting animal forms",
            HitDice = 8,
            PrimaryAbility = "Wisdom",
            SavingThrowProficiencies = new List<string> { "Intelligence", "Wisdom" },
            ArmorProficiencies = new List<string> { "Light armor (nonmetal)", "Medium armor (nonmetal)", "Shields (nonmetal)" },
            WeaponProficiencies = new List<string> { "Clubs", "Daggers", "Darts", "Javelins", "Maces", "Quarterstaffs", "Scimitars", "Sickles", "Slings", "Spears" },
            SpellcastingType = SpellcastingType.Prepared,
            SpellcastingAbility = "Wisdom"
        };

        _classDatabase["Fighter"] = new ClassData
        {
            Name = "Fighter",
            Description = "A master of martial combat, skilled with a variety of weapons and armor",
            HitDice = 10,
            PrimaryAbility = "Strength or Dexterity",
            SavingThrowProficiencies = new List<string> { "Strength", "Constitution" },
            ArmorProficiencies = new List<string> { "All armor", "Shields" },
            WeaponProficiencies = new List<string> { "Simple weapons", "Martial weapons" }
        };
        
        _classDatabase["Monk"] = new ClassData
        {
            Name = "Monk",
            Description = "A master of martial arts, harnessing the power of the body in pursuit of physical and spiritual perfection",
            HitDice = 8,
            PrimaryAbility = "Dexterity and Wisdom",
            SavingThrowProficiencies = new List<string> { "Strength", "Dexterity" },
            ArmorProficiencies = new List<string>(),
            WeaponProficiencies = new List<string> { "Simple weapons", "Shortswords" }
        };
        
        _classDatabase["Paladin"] = new ClassData
        {
            Name = "Paladin",
            Description = "A holy warrior bound to a sacred oath",
            HitDice = 10,
            PrimaryAbility = "Strength and Charisma",
            SavingThrowProficiencies = new List<string> { "Wisdom", "Charisma" },
            ArmorProficiencies = new List<string> { "All armor", "Shields" },
            WeaponProficiencies = new List<string> { "Simple weapons", "Martial weapons" },
            SpellcastingType = SpellcastingType.Prepared,
            SpellcastingAbility = "Charisma"
        };

        _classDatabase["Ranger"] = new ClassData
        {
            Name = "Ranger",
            Description = "A warrior who uses martial prowess and nature magic to combat threats on the edges of civilization",
            HitDice = 10,
            PrimaryAbility = "Dexterity and Wisdom",
            SavingThrowProficiencies = new List<string> { "Strength", "Dexterity" },
            ArmorProficiencies = new List<string> { "Light armor", "Medium armor", "Shields" },
            WeaponProficiencies = new List<string> { "Simple weapons", "Martial weapons" },
            SpellcastingType = SpellcastingType.Known,
            SpellcastingAbility = "Wisdom"
        };

        _classDatabase["Rogue"] = new ClassData
        {
            Name = "Rogue",
            Description = "A scoundrel who uses stealth and trickery to overcome obstacles and enemies",
            HitDice = 8,
            PrimaryAbility = "Dexterity",
            SavingThrowProficiencies = new List<string> { "Dexterity", "Intelligence" },
            ArmorProficiencies = new List<string> { "Light armor" },
            WeaponProficiencies = new List<string> { "Simple weapons", "Hand crossbows", "Longswords", "Rapiers", "Shortswords" }
        };
        
        _classDatabase["Sorcerer"] = new ClassData
        {
            Name = "Sorcerer",
            Description = "A spellcaster who draws on inherent magic from a gift or bloodline",
            HitDice = 6,
            PrimaryAbility = "Charisma",
            SavingThrowProficiencies = new List<string> { "Constitution", "Charisma" },
            ArmorProficiencies = new List<string>(),
            WeaponProficiencies = new List<string> { "Daggers", "Darts", "Slings", "Quarterstaffs", "Light crossbows" },
            SpellcastingType = SpellcastingType.Known,
            SpellcastingAbility = "Charisma"
        };

        _classDatabase["Warlock"] = new ClassData
        {
            Name = "Warlock",
            Description = "A wielder of magic that is derived from a bargain with an extraplanar entity",
            HitDice = 8,
            PrimaryAbility = "Charisma",
            SavingThrowProficiencies = new List<string> { "Wisdom", "Charisma" },
            ArmorProficiencies = new List<string> { "Light armor" },
            WeaponProficiencies = new List<string> { "Simple weapons" },
            SpellcastingType = SpellcastingType.Known,
            SpellcastingAbility = "Charisma"
        };

        _classDatabase["Wizard"] = new ClassData
        {
            Name = "Wizard",
            Description = "A scholarly magic-user capable of manipulating the structures of reality",
            HitDice = 6,
            PrimaryAbility = "Intelligence",
            SavingThrowProficiencies = new List<string> { "Intelligence", "Wisdom" },
            ArmorProficiencies = new List<string>(),
            WeaponProficiencies = new List<string> { "Daggers", "Darts", "Slings", "Quarterstaffs", "Light crossbows" },
            SpellcastingType = SpellcastingType.Prepared,
            SpellcastingAbility = "Intelligence"
        };

        // Aliases
        _classDatabase["Warrior"] = _classDatabase["Fighter"];
        _classDatabase["Mage"] = _classDatabase["Wizard"];
    }
    
    public static ClassData GetClass(string className)
    {
        if (_classDatabase.TryGetValue(className, out var classData))
        {
            return classData;
        }
        
        return new ClassData { Name = className, HitDice = 8 };
    }
    
    public static List<string> GetAllClassNames()
    {
        return new List<string>
        {
            "Barbarian",
            "Bard",
            "Cleric",
            "Druid",
            "Fighter",
            "Monk",
            "Paladin",
            "Ranger",
            "Rogue",
            "Sorcerer",
            "Warlock",
            "Wizard"
        };
    }
    
    public int GetHitPointsAtLevel(int level, int constitutionModifier)
    {
        if (level <= 0) return 0;
        
        int hp = HitDice + constitutionModifier;
        
        for (int i = 2; i <= level; i++)
        {
            int avgRoll = (HitDice / 2) + 1;
            hp += avgRoll + constitutionModifier;
        }
        
        return Math.Max(1, hp);
    }
}
