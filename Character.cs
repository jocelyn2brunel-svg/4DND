using System;
using System.Collections.Generic;

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
    public int DeathSaveSuccesses { get; set; } = 0;
    public int DeathSaveFailures { get; set; } = 0;
    
    // Vision properties
    public int DarkvisionRange { get; set; } = 0;  // In feet
    public bool HasSunlightSensitivity { get; set; } = false;
    
    // Inventory system
    public Inventory InventoryData { get; set; } = new Inventory();
    
    // Wealth
    public int GoldPieces { get; set; } = 0;

    // Proficiencies (armor and weapons)
    public List<string> ArmorProficiencies { get; set; } = new();
    public List<string> WeaponProficiencies { get; set; } = new();
    
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
        
        return abilityMod + (proficient ? ProficiencyBonus : 0);
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
        
        // Darkvision from race
        DarkvisionRange = raceData.DarkvisionRange;
        HasSunlightSensitivity = raceData.HasSunlightSensitivity;
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
    
    public bool IsProficientWithWeapon(string weaponName)
    {
        if (WeaponProficiencies == null) return false;

        foreach (var prof in WeaponProficiencies)
        {
            if (prof.Equals(weaponName, StringComparison.OrdinalIgnoreCase) ||
                prof.Equals("Simple weapons", StringComparison.OrdinalIgnoreCase) ||
                prof.Equals("Martial weapons", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

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
