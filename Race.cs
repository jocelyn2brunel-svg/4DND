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
    /// <summary>Naturally Stealthy: you can attempt to hide even when obscured only by a creature at least one size larger than you (Lightfoot Halfling).</summary>
    public bool HasNaturallyStealthy { get; set; } = false;
    /// <summary>Stout Resilience: advantage on saving throws against poison, and resistance against poison damage (Stout Halfling).</summary>
    public bool HasStoutResilience { get; set; } = false;

    // Gnome-specific traits
    /// <summary>Gnome Cunning: advantage on all Intelligence, Wisdom, and Charisma saving throws against magic.</summary>
    public bool HasGnomeCunning { get; set; } = false;
    /// <summary>Natural Illusionist: knows the minor illusion cantrip; Intelligence is the spellcasting ability (Forest Gnome).</summary>
    public bool HasNaturalIllusionist { get; set; } = false;
    /// <summary>Speak with Small Beasts: through sounds and gestures, can communicate simple ideas with Small or smaller beasts (Forest Gnome).</summary>
    public bool HasSpeakWithSmallBeasts { get; set; } = false;
    /// <summary>Artificer's Lore: add twice the proficiency bonus on Intelligence (History) checks related to magic items, alchemical objects, or technological devices (Rock Gnome).</summary>
    public bool HasArtificersLore { get; set; } = false;
    /// <summary>Tinker: proficiency with artisan's tools (tinker's tools); can spend 1 hour and 10 gp to construct a Tiny clockwork device (Rock Gnome).</summary>
    public bool HasTinker { get; set; } = false;

    // Half-Orc-specific traits
    /// <summary>Menacing: you gain proficiency in the Intimidation skill (Half-Orc).</summary>
    public bool HasMenacing { get; set; } = false;
    /// <summary>Relentless Endurance: when reduced to 0 hit points but not killed outright, you can drop to 1 hit point instead. Usable once per long rest (Half-Orc).</summary>
    public bool HasRelentlessEndurance { get; set; } = false;
    /// <summary>Savage Attacks: when you score a critical hit with a melee weapon attack, you can roll one of the weapon's damage dice one additional time and add it to the extra damage of the critical hit (Half-Orc).</summary>
    public bool HasSavageAttacks { get; set; } = false;

    // Tiefling-specific traits
    /// <summary>Hellish Resistance: you have resistance to fire damage (Tiefling).</summary>
    public bool HasHellishResistance { get; set; } = false;
    /// <summary>Infernal Legacy: you know the thaumaturgy cantrip; at 3rd level you can cast hellish rebuke once per day as a 2nd-level spell; at 5th level you can cast darkness once per day. Charisma is your spellcasting ability (Tiefling).</summary>
    public bool HasInfernalLegacy { get; set; } = false;

    // Half-Elf-specific traits
    /// <summary>Skill Versatility: you gain proficiency in two skills of your choice (Half-Elf).</summary>
    public bool HasSkillVersatility { get; set; } = false;

    // Dragonborn-specific traits
    /// <summary>Draconic Ancestry: you have draconic ancestry of a chosen dragon type, which determines your breath weapon and damage resistance.</summary>
    public bool HasDraconicAncestry { get; set; } = false;
    /// <summary>Breath Weapon: you can use your action to exhale destructive energy. DC = 8 + CON modifier + proficiency bonus; damage scales with level (2d6 → 3d6 at 6th, 4d6 at 11th, 5d6 at 16th). Usable once per short or long rest.</summary>
    public bool HasBreathWeapon { get; set; } = false;
    /// <summary>Damage Resistance: you have resistance to the damage type associated with your draconic ancestry.</summary>
    public bool HasDamageResistance { get; set; } = false;

    /// <summary>Age at which members of this race are considered adults (no longer young). 0 = unspecified.</summary>
    public int MaturityAge { get; set; } = 0;
    /// <summary>Average maximum lifespan for members of this race in years. 0 = unspecified.</summary>
    public int MaxAge { get; set; } = 0;

    /// <summary>Languages this race speaks, reads, and writes.</summary>
    public List<string> Languages { get; set; } = new();
    /// <summary>Tool proficiency options to choose from (player picks one). Empty means no choice.</summary>
    public List<string> ToolProficiencyChoices { get; set; } = new();

    /// <summary>Traditional male names for this race.</summary>
    public List<string> MaleNames { get; set; } = new();
    /// <summary>Traditional female names for this race.</summary>
    public List<string> FemaleNames { get; set; } = new();
    /// <summary>Clan or family names for this race.</summary>
    public List<string> ClanNames { get; set; } = new();

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
            HasExtraLanguage = true,
            MaturityAge = 18,
            MaxAge = 100,
            Languages = new List<string> { "Common" },
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
            MaturityAge = 100,
            MaxAge = 750,
            Languages = new List<string> { "Common", "Elvish" },
            MaleNames = new List<string> { "Adran", "Aelar", "Aramil", "Arannis", "Aust", "Beiro", "Berrian", "Carric", "Enialis", "Erdan", "Erevan", "Galinndan", "Hadarai", "Heian", "Himo", "Immeral", "Ivellios", "Laucian", "Mindartis", "Paelias", "Peren", "Quarion", "Riardon", "Rolen", "Soveliss", "Thamior", "Tharivol", "Theren", "Varis" },
            FemaleNames = new List<string> { "Adrie", "Althaea", "Anastrianna", "Andraste", "Antinua", "Bethrynna", "Birel", "Caelynn", "Drusilia", "Enna", "Felosial", "Ielenia", "Jelenneth", "Keyleth", "Leshanna", "Lia", "Meriele", "Mialee", "Naivara", "Quelenna", "Quillathe", "Sariel", "Shanairra", "Shava", "Silaqui", "Theirastra", "Thia", "Vadania", "Valanthe", "Xanaphia" },
            ClanNames = new List<string> { "Amakiir", "Amastacia", "Galanodel", "Holimion", "Ilphelkiir", "Liadon", "Meliamne", "Na\u00eflo", "Siannodel", "Xiloscient" },
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
            MaturityAge = 100,
            MaxAge = 750,
            Languages = new List<string> { "Common", "Elvish" },
            MaleNames = new List<string> { "Adran", "Aelar", "Aramil", "Arannis", "Aust", "Beiro", "Berrian", "Carric", "Enialis", "Erdan", "Erevan", "Galinndan", "Hadarai", "Heian", "Himo", "Immeral", "Ivellios", "Laucian", "Mindartis", "Paelias", "Peren", "Quarion", "Riardon", "Rolen", "Soveliss", "Thamior", "Tharivol", "Theren", "Varis" },
            FemaleNames = new List<string> { "Adrie", "Althaea", "Anastrianna", "Andraste", "Antinua", "Bethrynna", "Birel", "Caelynn", "Drusilia", "Enna", "Felosial", "Ielenia", "Jelenneth", "Keyleth", "Leshanna", "Lia", "Meriele", "Mialee", "Naivara", "Quelenna", "Quillathe", "Sariel", "Shanairra", "Shava", "Silaqui", "Theirastra", "Thia", "Vadania", "Valanthe", "Xanaphia" },
            ClanNames = new List<string> { "Amakiir", "Amastacia", "Galanodel", "Holimion", "Ilphelkiir", "Liadon", "Meliamne", "Na\u00eflo", "Siannodel", "Xiloscient" },
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
            MaturityAge = 100,
            MaxAge = 750,
            Languages = new List<string> { "Common", "Elvish" },
            MaleNames = new List<string> { "Adran", "Aelar", "Aramil", "Arannis", "Aust", "Beiro", "Berrian", "Carric", "Enialis", "Erdan", "Erevan", "Galinndan", "Hadarai", "Heian", "Himo", "Immeral", "Ivellios", "Laucian", "Mindartis", "Paelias", "Peren", "Quarion", "Riardon", "Rolen", "Soveliss", "Thamior", "Tharivol", "Theren", "Varis" },
            FemaleNames = new List<string> { "Adrie", "Althaea", "Anastrianna", "Andraste", "Antinua", "Bethrynna", "Birel", "Caelynn", "Drusilia", "Enna", "Felosial", "Ielenia", "Jelenneth", "Keyleth", "Leshanna", "Lia", "Meriele", "Mialee", "Naivara", "Quelenna", "Quillathe", "Sariel", "Shanairra", "Shava", "Silaqui", "Theirastra", "Thia", "Vadania", "Valanthe", "Xanaphia" },
            ClanNames = new List<string> { "Amakiir", "Amastacia", "Galanodel", "Holimion", "Ilphelkiir", "Liadon", "Meliamne", "Na\u00eflo", "Siannodel", "Xiloscient" },
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
            MaturityAge = 50,
            MaxAge = 350,
            Languages = new List<string> { "Common", "Dwarvish" },
            ToolProficiencyChoices = new List<string> { "Smith's tools", "Brewer's supplies", "Mason's tools" },
            MaleNames = new List<string> { "Adrik", "Alberich", "Baern", "Barendd", "Brottor", "Bruenor", "Dain", "Darrak", "Delg", "Eberk", "Einkil", "Fargrim", "Flint", "Gardain", "Harbek", "Kildrak", "Morgran", "Orsik", "Oskar", "Rangrim", "Rurik", "Taklinn", "Thoradin", "Thorin", "Tordek", "Traubon", "Travok", "Ulfgar", "Veit", "Vondal" },
            FemaleNames = new List<string> { "Amber", "Artin", "Audhild", "Bardryn", "Dagnal", "Diesa", "Eldeth", "Falkrunn", "Finellen", "Gunnloda", "Gurdis", "Helja", "Hlin", "Kathra", "Kristryd", "Ilde", "Liftrasa", "Mardred", "Riswynn", "Sannl", "Torbera", "Torgga", "Vistra" },
            ClanNames = new List<string> { "Balderk", "Battlehammer", "Brawnanvil", "Dankil", "Fireforge", "Frostbeard", "Gorunn", "Holderhek", "Ironfist", "Loderr", "Lutgehr", "Rumnaheim", "Strakeln", "Torunn", "Ungart" },
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
            MaturityAge = 50,
            MaxAge = 350,
            Languages = new List<string> { "Common", "Dwarvish" },
            ToolProficiencyChoices = new List<string> { "Smith's tools", "Brewer's supplies", "Mason's tools" },
            MaleNames = new List<string> { "Adrik", "Alberich", "Baern", "Barendd", "Brottor", "Bruenor", "Dain", "Darrak", "Delg", "Eberk", "Einkil", "Fargrim", "Flint", "Gardain", "Harbek", "Kildrak", "Morgran", "Orsik", "Oskar", "Rangrim", "Rurik", "Taklinn", "Thoradin", "Thorin", "Tordek", "Traubon", "Travok", "Ulfgar", "Veit", "Vondal" },
            FemaleNames = new List<string> { "Amber", "Artin", "Audhild", "Bardryn", "Dagnal", "Diesa", "Eldeth", "Falkrunn", "Finellen", "Gunnloda", "Gurdis", "Helja", "Hlin", "Kathra", "Kristryd", "Ilde", "Liftrasa", "Mardred", "Riswynn", "Sannl", "Torbera", "Torgga", "Vistra" },
            ClanNames = new List<string> { "Balderk", "Battlehammer", "Brawnanvil", "Dankil", "Fireforge", "Frostbeard", "Gorunn", "Holderhek", "Ironfist", "Loderr", "Lutgehr", "Rumnaheim", "Strakeln", "Torunn", "Ungart" },
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
            HasNaturallyStealthy = true,
            MaturityAge = 20,
            MaxAge = 150,
            Languages = new List<string> { "Common", "Halfling" },
            MaleNames = new List<string> { "Alton", "Ander", "Cade", "Corrin", "Eldon", "Errich", "Finnan", "Garret", "Lindal", "Lyle", "Merric", "Milo", "Osborn", "Perrin", "Reed", "Roscoe", "Wellby" },
            FemaleNames = new List<string> { "Andry", "Bree", "Callie", "Cora", "Euphemia", "Jillian", "Kithri", "Lavinia", "Lidda", "Merla", "Nedda", "Paela", "Portia", "Seraphina", "Shaena", "Trym", "Vani", "Verna" },
            ClanNames = new List<string> { "Brushgather", "Goodbarrel", "Greenbottle", "High-hill", "Hilltopple", "Leagallow", "Tealeaf", "Thorngage", "Tosscobble", "Underbough" },
            Description = Loc.Tr("Nimble and charming (+2 DEX, +1 CHA, Lucky, Brave, Halfling Nimbleness, Naturally Stealthy)")
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
            HasStoutResilience = true,
            MaturityAge = 20,
            MaxAge = 150,
            Languages = new List<string> { "Common", "Halfling" },
            MaleNames = new List<string> { "Alton", "Ander", "Cade", "Corrin", "Eldon", "Errich", "Finnan", "Garret", "Lindal", "Lyle", "Merric", "Milo", "Osborn", "Perrin", "Reed", "Roscoe", "Wellby" },
            FemaleNames = new List<string> { "Andry", "Bree", "Callie", "Cora", "Euphemia", "Jillian", "Kithri", "Lavinia", "Lidda", "Merla", "Nedda", "Paela", "Portia", "Seraphina", "Shaena", "Trym", "Vani", "Verna" },
            ClanNames = new List<string> { "Brushgather", "Goodbarrel", "Greenbottle", "High-hill", "Hilltopple", "Leagallow", "Tealeaf", "Thorngage", "Tosscobble", "Underbough" },
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
            HasMenacing = true,
            HasRelentlessEndurance = true,
            HasSavageAttacks = true,
            MaturityAge = 14,
            MaxAge = 75,
            Languages = new List<string> { "Common", "Orc" },
            Description = Loc.Tr("Strong and tough (+2 STR, +1 CON, Darkvision 60 ft, Menacing, Relentless Endurance, Savage Attacks)")
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
            HasHellishResistance = true,
            HasInfernalLegacy = true,
            MaturityAge = 18,
            MaxAge = 100,
            Languages = new List<string> { "Common", "Infernal" },
            Description = Loc.Tr("Infernal heritage (+2 CHA, +1 INT, Darkvision 60 ft, Hellish Resistance, Infernal Legacy)")
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
            HasDraconicAncestry = true,
            HasBreathWeapon = true,
            HasDamageResistance = true,
            MaturityAge = 15,
            MaxAge = 80,
            Languages = new List<string> { "Common", "Draconic" },
            MaleNames = new List<string> { "Arjhan", "Balasar", "Bharash", "Donaar", "Ghesh", "Heskan", "Kriv", "Medrash", "Mehen", "Nadarr", "Pandjed", "Patrin", "Rhogar", "Shamash", "Shedinn", "Tarhun", "Torinn" },
            FemaleNames = new List<string> { "Akra", "Biri", "Daar", "Farideh", "Harann", "Havilar", "Jheri", "Kava", "Korinn", "Mishann", "Nala", "Perra", "Raiann", "Sora", "Surina", "Thava", "Uadjit" },
            ClanNames = new List<string> { "Clethtinthiallor", "Daardendrian", "Delmirev", "Drachedandion", "Fenkenkabradon", "Kepeshkmolik", "Kerrhylon", "Kimbatuul", "Linxakasendalor", "Myastan", "Nemmonis", "Norixius", "Ophinshtalajiir", "Prexijandilin", "Shestendeliath", "Turnuroth", "Verthisathurgiesh", "Yarjerit" },
            Description = Loc.Tr("Draconic heritage (+2 STR, +1 CHA, Draconic Ancestry, Breath Weapon, Damage Resistance)")
        },
        
        ["Rock Gnome"] = new Race
        {
            Name = "Rock Gnome",
            DisplayName = Loc.Tr("Gnome (Rock)"),
            Size = CreatureSize.Small,
            IntelligenceBonus = 2,
            ConstitutionBonus = 1,
            BaseSpeed = 25,
            DarkvisionRange = 60,
            HasGnomeCunning = true,
            HasArtificersLore = true,
            HasTinker = true,
            MaturityAge = 40,
            MaxAge = 500,
            Languages = new List<string> { "Common", "Gnomish" },
            Description = Loc.Tr("Inventive and hardy (+2 INT, +1 CON, Darkvision 60 ft, Gnome Cunning, Artificer's Lore, Tinker)")
        },
        ["Forest Gnome"] = new Race
        {
            Name = "Forest Gnome",
            DisplayName = Loc.Tr("Gnome (Forest)"),
            Size = CreatureSize.Small,
            IntelligenceBonus = 2,
            DexterityBonus = 1,
            BaseSpeed = 25,
            DarkvisionRange = 60,
            HasGnomeCunning = true,
            HasNaturalIllusionist = true,
            HasSpeakWithSmallBeasts = true,
            MaturityAge = 40,
            MaxAge = 500,
            Languages = new List<string> { "Common", "Gnomish" },
            Description = Loc.Tr("Stealthy and illusionist (+2 INT, +1 DEX, Darkvision 60 ft, Gnome Cunning, Natural Illusionist, Speak with Small Beasts)")
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
            HasSkillVersatility = true,
            HasExtraLanguage = true,
            MaturityAge = 20,
            MaxAge = 180,
            Languages = new List<string> { "Common", "Elvish" },
            Description = Loc.Tr("Versatile and charismatic (+2 CHA, +1 to two other abilities, Darkvision 60 ft, Fey Ancestry, Skill Versatility, Extra Language)")
        }
    };
    
    /// <summary>
    /// Lookup table for the PHB "Draconic Ancestry" table.
    /// Maps each <see cref="DragonAncestryType"/> to its damage type, breath weapon shape, and saving throw.
    /// </summary>
    public static readonly Dictionary<DragonAncestryType, DraconicAncestryEntry> DraconicAncestryTable = new()
    {
        [DragonAncestryType.Black]  = new DraconicAncestryEntry(DamageType.Acid,      BreathWeaponShape.Line5x30, "Dexterity"),
        [DragonAncestryType.Blue]   = new DraconicAncestryEntry(DamageType.Lightning, BreathWeaponShape.Line5x30, "Dexterity"),
        [DragonAncestryType.Brass]  = new DraconicAncestryEntry(DamageType.Fire,      BreathWeaponShape.Line5x30, "Dexterity"),
        [DragonAncestryType.Bronze] = new DraconicAncestryEntry(DamageType.Lightning, BreathWeaponShape.Line5x30, "Dexterity"),
        [DragonAncestryType.Copper] = new DraconicAncestryEntry(DamageType.Acid,      BreathWeaponShape.Line5x30, "Dexterity"),
        [DragonAncestryType.Gold]   = new DraconicAncestryEntry(DamageType.Fire,      BreathWeaponShape.Cone15,   "Dexterity"),
        [DragonAncestryType.Green]  = new DraconicAncestryEntry(DamageType.Poison,    BreathWeaponShape.Cone15,   "Constitution"),
        [DragonAncestryType.Red]    = new DraconicAncestryEntry(DamageType.Fire,      BreathWeaponShape.Cone15,   "Dexterity"),
        [DragonAncestryType.Silver] = new DraconicAncestryEntry(DamageType.Cold,      BreathWeaponShape.Cone15,   "Constitution"),
        [DragonAncestryType.White]  = new DraconicAncestryEntry(DamageType.Cold,      BreathWeaponShape.Cone15,   "Constitution"),
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
