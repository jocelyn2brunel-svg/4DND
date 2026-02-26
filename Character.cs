using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DND;

public class Character
{
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Race { get; set; } = "";
    public string Class { get; set; } = "";
    public int Level { get; set; }
    public int XP { get; set; }
    
    // Ability Scores
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
    
    // Combat Stats
    public int MaxHP { get; set; } = 10;
    public int CurrentHP { get; set; } = 10;
    public int TempHP { get; set; } = 0;
    public int ArmorClass { get; set; } = 10;
    public int Speed { get; set; } = 30;
    
    // Proficiencies (saved as bitmasks or lists)
    public bool StrengthSaveProficiency { get; set; }
    public bool DexteritySaveProficiency { get; set; }
    public bool ConstitutionSaveProficiency { get; set; }
    public bool IntelligenceSaveProficiency { get; set; }
    public bool WisdomSaveProficiency { get; set; }
    public bool CharismaSaveProficiency { get; set; }
    
    // Skill Proficiencies (D&D 5e has 18 skills)
    public bool AcrobaticsProficiency { get; set; }
    public bool AnimalHandlingProficiency { get; set; }
    public bool ArcanaProficiency { get; set; }
    public bool AthleticsProficiency { get; set; }
    public bool DeceptionProficiency { get; set; }
    public bool HistoryProficiency { get; set; }
    public bool InsightProficiency { get; set; }
    public bool IntimidationProficiency { get; set; }
    public bool InvestigationProficiency { get; set; }
    public bool MedicineProficiency { get; set; }
    public bool NatureProficiency { get; set; }
    public bool PerceptionProficiency { get; set; }
    public bool PerformanceProficiency { get; set; }
    public bool PersuasionProficiency { get; set; }
    public bool ReligionProficiency { get; set; }
    public bool SleightOfHandProficiency { get; set; }
    public bool StealthProficiency { get; set; }
    public bool SurvivalProficiency { get; set; }
    
    // Other Character Info
    public string Background { get; set; } = "";
    public string Alignment { get; set; } = "Neutral";
    public int HitDiceTotal { get; set; } = 1;
    public int HitDiceRemaining { get; set; } = 1;
    public int HitDiceType { get; set; } = 8; // d6, d8, d10, or d12 based on class

    /// <summary>
    /// Whether this character has already taken a long rest in the current 24-hour period.
    /// Reset at the start of a new in-game day.
    /// </summary>
    public bool HasLongRestedToday { get; set; } = false;
    public int DeathSaveSuccesses { get; set; } = 0;
    public int DeathSaveFailures { get; set; } = 0;
    
    // Vision properties
    public int DarkvisionRange { get; set; } = 0;  // In feet
    public bool HasSunlightSensitivity { get; set; } = false;
    
    // Racial traits
    /// <summary>Dwarven Resilience: advantage on saving throws against poison, resistance to poison damage.</summary>
    public bool HasDwarvenResilience { get; set; } = false;
    /// <summary>Stonecunning: double proficiency bonus on Intelligence (History) checks related to stonework.</summary>
    public bool HasStonecunning { get; set; } = false;
    /// <summary>Languages this character can speak, read, and write.</summary>
    public List<string> Languages { get; set; } = new();
    /// <summary>Tool proficiencies this character has (from class, race, or background).</summary>
    public List<string> ToolProficiencies { get; set; } = new();

    // Inventory system
    public Inventory InventoryData { get; set; } = new Inventory();
    public DonDoffProcess? CurrentDonDoffProcess { get; set; }
    
    // Wealth
    public int GoldPieces { get; set; } = 0;

    // Barbarian-specific
    public int RagesRemaining { get; set; } = 0;

    // Proficiencies (armor and weapons)
    public List<string> ArmorProficiencies { get; set; } = new();
    public List<string> WeaponProficiencies { get; set; } = new();

    // Bard-specific
    /// <summary>Die type for Bardic Inspiration (d6, d8, d10, or d12 depending on level).</summary>
    public int BardicInspirationDice { get; set; } = 6;
    /// <summary>Number of Bardic Inspiration uses remaining (recharges on long rest; on short rest at level 5+).</summary>
    public int BardicInspirationUsesRemaining { get; set; } = 0;
    /// <summary>Maximum number of Bardic Inspiration uses (= Charisma modifier, minimum 1).</summary>
    public int BardicInspirationMax { get; set; } = 0;
    /// <summary>Remaining spell slots indexed by spell level (index 0 = 1st-level slots … index 8 = 9th-level slots).</summary>
    public int[] SpellSlotsRemaining { get; set; } = new int[9];
    /// <summary>Maximum spell slots per level at current character level (indices 0–8 for spell levels 1–9).</summary>
    public int[] SpellSlotsMax { get; set; } = new int[9];
    /// <summary>Number of cantrips the bard knows.</summary>
    public int CantripsKnown { get; set; } = 0;
    /// <summary>Number of spells the bard knows (excluding cantrips).</summary>
    public int SpellsKnown { get; set; } = 0;
    /// <summary>Spells the character has learned (for Known-spell classes such as Bard and Sorcerer). Cantrips should be included here.</summary>
    public List<Spell> KnownSpells { get; set; } = new();
    /// <summary>Spells currently prepared (for Prepared-spell classes such as Cleric and Wizard). Cantrips need not be listed here.</summary>
    public List<Spell> PreparedSpells { get; set; } = new();
    /// <summary>True if this character's class uses the spell preparation system (Cleric, Druid, Paladin, Wizard).</summary>
    public bool UsesSpellPreparation => ClassData.GetClass(Class).SpellcastingType == SpellcastingType.Prepared;
    /// <summary>
    /// Maximum number of non-cantrip spells this character can have prepared at once.
    /// Equals the spellcasting ability modifier plus the character's level (or half level for Paladin), minimum 1.
    /// Returns 0 for classes that do not use spell preparation.
    /// </summary>
    public int MaxPreparedSpells
    {
        get
        {
            var classData = ClassData.GetClass(Class);
            int mod = classData.SpellcastingAbility switch
            {
                "Intelligence" => GetAbilityModifier(Intelligence),
                "Wisdom"       => GetAbilityModifier(Wisdom),
                "Charisma"     => GetAbilityModifier(Charisma),
                _              => 0
            };
            return classData.GetMaxPreparedSpells(Level, mod);
        }
    }

    // Survival / Nourishment
    /// <summary>Current exhaustion level (0 = none, 1–5 = cumulative penalties, 6 = death).</summary>
    public int ExhaustionLevel { get; set; } = 0;
    /// <summary>Accumulated days (or fractional days) without adequate food. Resets to 0 after a full-ration day.</summary>
    public float DaysWithoutFood { get; set; } = 0f;
    /// <summary>Pounds of food consumed today (needs 1 lb/day for a full ration).</summary>
    public float FoodConsumedToday { get; set; } = 0f;
    /// <summary>Gallons of water consumed today (needs 1 gal/day; 2 in hot weather).</summary>
    public float WaterConsumedToday { get; set; } = 0f;

    // Derived properties
    public int ProficiencyBonus => DndMath.GetProficiencyBonus(Level);
    
    public int GetAbilityModifier(int score) => DndMath.GetAbilityModifier(score);
    
    public int GetSavingThrow(string ability)
    {
        int mod = ability switch
        {
            "STR" => GetAbilityModifier(Strength),
            "DEX" => GetAbilityModifier(Dexterity),
            "CON" => GetAbilityModifier(Constitution),
            "INT" => GetAbilityModifier(Intelligence),
            "WIS" => GetAbilityModifier(Wisdom),
            "CHA" => GetAbilityModifier(Charisma),
            _ => 0
        };
        
        bool proficient = ability switch
        {
            "STR" => StrengthSaveProficiency,
            "DEX" => DexteritySaveProficiency,
            "CON" => ConstitutionSaveProficiency,
            "INT" => IntelligenceSaveProficiency,
            "WIS" => WisdomSaveProficiency,
            "CHA" => CharismaSaveProficiency,
            _ => false
        };
        
        return mod + (proficient ? ProficiencyBonus : 0);
    }
    
    public int GetSkillBonus(String skill, out String ability)
    {
        (ability, bool proficient) = skill switch
        {
            "Acrobatics" => ("DEX", AcrobaticsProficiency),
            "Animal Handling" => ("WIS", AnimalHandlingProficiency),
            "Arcana" => ("INT", ArcanaProficiency),
            "Athletics" => ("STR", AthleticsProficiency),
            "Deception" => ("CHA", DeceptionProficiency),
            "History" => ("INT", HistoryProficiency),
            "Insight" => ("WIS", InsightProficiency),
            "Intimidation" => ("CHA", IntimidationProficiency),
            "Investigation" => ("INT", InvestigationProficiency),
            "Medicine" => ("WIS", MedicineProficiency),
            "Nature" => ("INT", NatureProficiency),
            "Perception" => ("WIS", PerceptionProficiency),
            "Performance" => ("CHA", PerformanceProficiency),
            "Persuasion" => ("CHA", PersuasionProficiency),
            "Religion" => ("INT", ReligionProficiency),
            "Sleight of Hand" => ("DEX", SleightOfHandProficiency),
            "Stealth" => ("DEX", StealthProficiency),
            "Survival" => ("WIS", SurvivalProficiency),
            _ => ("", false)
        };
        
        int abilityMod = ability switch
        {
            "STR" => GetAbilityModifier(Strength),
            "DEX" => GetAbilityModifier(Dexterity),
            "CON" => GetAbilityModifier(Constitution),
            "INT" => GetAbilityModifier(Intelligence),
            "WIS" => GetAbilityModifier(Wisdom),
            "CHA" => GetAbilityModifier(Charisma),
            _ => 0
        };
        
        // Stonecunning: double proficiency bonus on History checks (treated as always proficient for stonework)
        int profBonus = proficient ? ProficiencyBonus : 0;
        if (HasStonecunning && skill == "History")
            profBonus = proficient ? ProficiencyBonus * 2 : ProficiencyBonus;
        
        return abilityMod + profBonus;
    }
    
    public void SetAbilityScores(int[] scores)
    {
        if (scores.Length != 6) throw new ArgumentException("Exactly 6 scores required.");
        Strength = scores[0];
        Dexterity = scores[1];
        Constitution = scores[2];
        Intelligence = scores[3];
        Wisdom = scores[4];
        Charisma = scores[5];
    }
    
    public void CalculateDerivedStats()
    {
        // Hit Points based on class and Constitution
        var classData = ClassData.GetClass(Class);
        MaxHP = classData.GetHitPointsAtLevel(Level, GetAbilityModifier(Constitution));
        if (CurrentHP > MaxHP) CurrentHP = MaxHP;
        
        // Armor Class from inventory
        ArmorClass = InventoryData.CalculateArmorClass(GetAbilityModifier(Dexterity));
        
        // Speed based on race
        var raceData = _4DND.Race.GetRace(Race);
        Speed = raceData.BaseSpeed;

        // Heavy armor speed penalty
        if (InventoryData.EquippedArmor != null)
        {
            var armorItem = ItemDatabase.GetItem(InventoryData.EquippedArmor.Name);
            if (armorItem.StrengthRequirement > 0 && Strength < armorItem.StrengthRequirement)
            {
                Speed = Math.Max(0, Speed - 10);
            }
        }
        
        // Darkvision from race
        DarkvisionRange = raceData.DarkvisionRange;
        HasSunlightSensitivity = raceData.HasSunlightSensitivity;
        HasDwarvenResilience = raceData.HasDwarvenResilience;
        HasStonecunning = raceData.HasStonecunning;
    }
    
    public int GetPrimaryAbilityModifier()
    {
        var classData = ClassData.GetClass(Class);
        
        return classData.PrimaryAbility switch
        {
            "Strength" => GetAbilityModifier(Strength),
            "Dexterity" => GetAbilityModifier(Dexterity),
            "Constitution" => GetAbilityModifier(Constitution),
            "Intelligence" => GetAbilityModifier(Intelligence),
            "Wisdom" => GetAbilityModifier(Wisdom),
            "Charisma" => GetAbilityModifier(Charisma),
            "Strength or Dexterity" => Math.Max(GetAbilityModifier(Strength), GetAbilityModifier(Dexterity)),
            "Dexterity & Wisdom" => Math.Max(GetAbilityModifier(Dexterity), GetAbilityModifier(Wisdom)),
            "Strength & Charisma" => Math.Max(GetAbilityModifier(Strength), GetAbilityModifier(Charisma)),
            "Wisdom & Charisma" => Math.Max(GetAbilityModifier(Wisdom), GetAbilityModifier(Charisma)),
            _ => 0
        };
    }

    /// <summary>
    /// Returns true if this character can cast a spell of the given level.
    /// Cantrips (level 0) are always available. For levelled spells, the character
    /// must meet the minimum character level and have a remaining slot of that level.
    /// </summary>
    public bool CanCastSpellLevel(int spellLevel)
    {
        if (IsWearingNonProficientArmor) return false;
        if (spellLevel == 0) return true;
        if (spellLevel < 0 || spellLevel > 9) return false;
        if (Level < Spell.MinimumCharacterLevel(spellLevel)) return false;
        return SpellSlotsRemaining[spellLevel - 1] > 0;
    }

    /// <summary>
    /// Returns true if this character can cast the given spell.
    /// Cantrips are always available. Leveled spells require a remaining slot of that level
    /// and must be known (for Known-spell classes) or prepared (for Prepared-spell classes).
    /// </summary>
    public bool CanCastSpell(Spell spell)
    {
        if (IsWearingNonProficientArmor) return false;
        if (spell.IsCantrip) return true;
        if (!CanCastSpellLevel(spell.Level)) return false;
        var classData = ClassData.GetClass(Class);
        return classData.SpellcastingType switch
        {
            SpellcastingType.Known    => KnownSpells.Any(s => s.Name == spell.Name),
            SpellcastingType.Prepared => PreparedSpells.Any(s => s.Name == spell.Name),
            _                         => false
        };
    }

    /// <summary>
    /// Prepares a spell for a Prepared-spell caster. Cantrips require no preparation.
    /// Returns false if the class does not use preparation, the spell is already prepared,
    /// or the maximum number of prepared spells has been reached.
    /// </summary>
    public bool PrepareSpell(Spell spell)
    {
        if (!UsesSpellPreparation || spell.IsCantrip) return false;
        if (PreparedSpells.Any(s => s.Name == spell.Name)) return false;
        if (PreparedSpells.Count >= MaxPreparedSpells) return false;
        PreparedSpells.Add(spell);
        return true;
    }

    /// <summary>
    /// Removes a spell from the prepared list.
    /// Returns false if the spell was not prepared.
    /// </summary>
    public bool UnprepareSpell(Spell spell)
    {
        var existing = PreparedSpells.FirstOrDefault(s => s.Name == spell.Name);
        if (existing == null) return false;
        PreparedSpells.Remove(existing);
        return true;
    }

    public bool IsProficientWithArmor(string armorType)
    {
        if (ArmorProficiencies == null) return false;

        foreach (var prof in ArmorProficiencies)
        {
            if (prof.Equals(armorType, StringComparison.OrdinalIgnoreCase) || 
                prof.Equals("All armor", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if this character is wearing armor or a shield they are not proficient with.
    /// While true: disadvantage on STR/DEX ability checks, saving throws, and attack rolls; cannot cast spells.
    /// </summary>
    public bool IsWearingNonProficientArmor
    {
        get
        {
            var equippedArmor = InventoryData.EquippedArmor;
            if (equippedArmor != null)
            {
                var item = ItemDatabase.GetItem(equippedArmor.Name);
                string? armorTypeName = item.ArmorCategory switch
                {
                    ArmorType.Light  => "Light armor",
                    ArmorType.Medium => "Medium armor",
                    ArmorType.Heavy  => "Heavy armor",
                    _                => null
                };
                if (armorTypeName != null && !IsProficientWithArmor(armorTypeName))
                    return true;
            }

            var equippedShield = InventoryData.EquippedShield;
            if (equippedShield != null && !IsProficientWithArmor("Shields"))
                return true;

            return false;
        }
    }
    
    public bool IsProficientWithWeapon(string weaponName)
    {
        if (WeaponProficiencies == null) return false;

        var item = ItemDatabase.GetItem(weaponName);
        foreach (var prof in WeaponProficiencies)
        {
            if (prof.Equals(weaponName, StringComparison.OrdinalIgnoreCase))
                return true;
            if (item != null)
            {
                if (item.WeaponCategory == WeaponType.Simple &&
                    prof.Equals("Simple weapons", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (item.WeaponCategory == WeaponType.Martial &&
                    prof.Equals("Martial weapons", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    // Cleric-specific
    /// <summary>Number of Channel Divinity uses remaining (recharges on short or long rest).</summary>
    public int ChannelDivinityUsesRemaining { get; set; } = 0;
    /// <summary>Maximum number of Channel Divinity uses at current level (0 before level 2).</summary>
    public int ChannelDivinityUsesMax { get; set; } = 0;

    /// <summary>
    /// Adds XP to this character and levels up if the threshold is reached.
    /// Returns true if the character gained at least one level.
    /// </summary>
    public bool GainXP(int amount)
    {
        if (amount <= 0) return false;

        XP += amount;

        int newLevel = DndMath.GetLevelForXP(XP);
        if (newLevel > Level)
        {
            Level = Math.Min(newLevel, 20);
            CalculateDerivedStats();
            return true;
        }

        return false;
    }
}
