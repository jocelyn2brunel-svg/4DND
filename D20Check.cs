using System;

namespace _4DND;

/// <summary>
/// Type of d20 check being performed.
/// </summary>
public enum D20CheckType
{
    AbilityCheck,
    AttackRoll,
    SavingThrow
}

/// <summary>
/// Represents the result of a d20 roll, following the three-step process:
/// 1. Roll the die and add a modifier
/// 2. Apply circumstantial bonuses/penalties
/// 3. Compare the total to a target number (DC or AC)
/// </summary>
public class D20Check
{
    public D20CheckType CheckType { get; set; }
    public string Description { get; set; } = "";
    
    // Step 1: Roll the die and add a modifier
    public int DieRoll { get; set; }
    public int BaseModifier { get; set; }
    
    // Step 2: Apply circumstantial bonuses/penalties
    public int CircumstantialBonus { get; set; } = 0;
    public bool HasAdvantage { get; set; } = false;
    public bool HasDisadvantage { get; set; } = false;
    public int SecondRoll { get; set; } = 0;  // Used for advantage/disadvantage
    
    // Step 3: Compare the total to a target number
    public int TargetNumber { get; set; }
    public int Total => DieRoll + BaseModifier + CircumstantialBonus;
    public bool Success => CheckType == D20CheckType.AttackRoll
        ? (IsCriticalHit || (!IsCriticalMiss && DndMath.MeetsDC(Total, TargetNumber)))
        : DndMath.MeetsDC(Total, TargetNumber);
    
    // Special cases
    public bool IsNaturalOne => DieRoll == 1;
    public bool IsNaturalTwenty => DieRoll == 20;
    public bool IsCriticalHit => CheckType == D20CheckType.AttackRoll && IsNaturalTwenty;
    public bool IsCriticalMiss => CheckType == D20CheckType.AttackRoll && IsNaturalOne;
    
    /// <summary>
    /// Returns a detailed description of the d20 check result.
    /// </summary>
    public string GetDetailedMessage()
    {
        string advantageText = "";
        if (HasAdvantage && !HasDisadvantage)
            advantageText = $" [ADV: {DieRoll}/{SecondRoll}]";
        else if (HasDisadvantage && !HasAdvantage)
            advantageText = $" [DIS: {DieRoll}/{SecondRoll}]";
        else if (HasAdvantage && HasDisadvantage)
            advantageText = " [ADV/DIS canceled]";
        
        string circumstantialText = CircumstantialBonus != 0 ? $" + {CircumstantialBonus} (situational)" : "";
        string result = Success ? "SUCCESS" : "FAILURE";
        
        string typeText = CheckType switch
        {
            D20CheckType.AttackRoll => "Attack",
            D20CheckType.SavingThrow => "Save",
            D20CheckType.AbilityCheck => "Check",
            _ => "Roll"
        };
        
        string specialText = "";
        if (IsCriticalHit)
            specialText = " [CRITICAL HIT!]";
        else if (IsCriticalMiss)
            specialText = " [CRITICAL MISS!]";
        else if (IsNaturalTwenty && CheckType != D20CheckType.AttackRoll)
            specialText = " [Natural 20!]";
        else if (IsNaturalOne && CheckType != D20CheckType.AttackRoll)
            specialText = " [Natural 1!]";
        
        string targetText = CheckType == D20CheckType.AttackRoll ? "AC" : "DC";
        
        return $"{Description} {typeText}{advantageText}: {DieRoll} + {BaseModifier}{circumstantialText} = {Total} vs {targetText} {TargetNumber} - {result}{specialText}";
    }
    
    /// <summary>
    /// Returns a simple message for the check result.
    /// </summary>
    public string GetSimpleMessage()
    {
        if (IsCriticalHit)
            return $"{Description} - Critical Hit!";
        if (IsCriticalMiss)
            return $"{Description} - Critical Miss!";
        if (Success)
            return $"{Description} - Success ({Total} vs {TargetNumber})";
        return $"{Description} - Failure ({Total} vs {TargetNumber})";
    }
}

/// <summary>
/// Factory class for creating various types of d20 checks.
/// </summary>
public static class D20CheckFactory
{
    private static readonly Random _random = new Random();
    
    /// <summary>
    /// Rolls a d20 with advantage/disadvantage.
    /// </summary>
    private static (int roll, int secondRoll, bool hasAdvantage, bool hasDisadvantage) RollD20WithAdvantage(bool hasAdvantage, bool hasDisadvantage)
    {
        int roll1 = _random.Next(1, 21);
        
        // If both advantage and disadvantage, they cancel out
        if (hasAdvantage == hasDisadvantage)
        {
            return (roll1, 0, false, false);
        }
        
        // Roll second die for advantage/disadvantage
        int roll2 = _random.Next(1, 21);
        
        if (hasAdvantage)
        {
            return (Math.Max(roll1, roll2), roll2, true, false);
        }
        else // hasDisadvantage
        {
            return (Math.Min(roll1, roll2), roll2, false, true);
        }
    }
    
    /// <summary>
    /// Make an ability check (Strength check, Dexterity check, etc.).
    /// </summary>
    public static D20Check MakeAbilityCheck(string abilityName, int abilityScore, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        var (roll, secondRoll, adv, dis) = RollD20WithAdvantage(hasAdvantage, hasDisadvantage);
        int modifier = DndMath.GetAbilityModifier(abilityScore);
        
        return new D20Check
        {
            CheckType = D20CheckType.AbilityCheck,
            Description = $"{abilityName} Check",
            DieRoll = roll,
            BaseModifier = modifier,
            CircumstantialBonus = circumstantialBonus,
            HasAdvantage = adv,
            HasDisadvantage = dis,
            SecondRoll = secondRoll,
            TargetNumber = dc
        };
    }
    
    /// <summary>
    /// Make a skill check (Athletics, Stealth, Perception, etc.).
    /// </summary>
    public static D20Check MakeSkillCheck(string skillName, int abilityScore, bool isProficient, int proficiencyBonus, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        var (roll, secondRoll, adv, dis) = RollD20WithAdvantage(hasAdvantage, hasDisadvantage);
        int modifier = DndMath.GetAbilityModifier(abilityScore);
        int bonus = modifier + (isProficient ? proficiencyBonus : 0);
        
        return new D20Check
        {
            CheckType = D20CheckType.AbilityCheck,
            Description = $"{skillName}",
            DieRoll = roll,
            BaseModifier = bonus,
            CircumstantialBonus = circumstantialBonus,
            HasAdvantage = adv,
            HasDisadvantage = dis,
            SecondRoll = secondRoll,
            TargetNumber = dc
        };
    }
    
    /// <summary>
    /// Make a saving throw.
    /// </summary>
    public static D20Check MakeSavingThrow(string abilityName, int abilityScore, bool isProficient, int proficiencyBonus, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        var (roll, secondRoll, adv, dis) = RollD20WithAdvantage(hasAdvantage, hasDisadvantage);
        int modifier = DndMath.GetAbilityModifier(abilityScore);
        int bonus = modifier + (isProficient ? proficiencyBonus : 0);
        
        return new D20Check
        {
            CheckType = D20CheckType.SavingThrow,
            Description = $"{abilityName} Save",
            DieRoll = roll,
            BaseModifier = bonus,
            CircumstantialBonus = circumstantialBonus,
            HasAdvantage = adv,
            HasDisadvantage = dis,
            SecondRoll = secondRoll,
            TargetNumber = dc
        };
    }
    
    /// <summary>
    /// Make an attack roll.
    /// </summary>
    public static D20Check MakeAttackRoll(string attackName, int attackBonus, int targetAC, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        var (roll, secondRoll, adv, dis) = RollD20WithAdvantage(hasAdvantage, hasDisadvantage);
        
        return new D20Check
        {
            CheckType = D20CheckType.AttackRoll,
            Description = attackName,
            DieRoll = roll,
            BaseModifier = attackBonus,
            CircumstantialBonus = circumstantialBonus,
            HasAdvantage = adv,
            HasDisadvantage = dis,
            SecondRoll = secondRoll,
            TargetNumber = targetAC
        };
    }
}

/// <summary>
/// Extension methods for creatures to make d20 checks.
/// </summary>
public static class CreatureD20Extensions
{
    /// <summary>
    /// Make an ability check for a creature.
    /// </summary>
    public static D20Check MakeAbilityCheck(this Creature creature, string abilityName, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        int abilityScore = abilityName switch
        {
            "STR" or "Strength" => creature.Strength,
            "DEX" or "Dexterity" => creature.Dexterity,
            "CON" or "Constitution" => creature.Constitution,
            "INT" or "Intelligence" => creature.Intelligence,
            "WIS" or "Wisdom" => creature.Wisdom,
            "CHA" or "Charisma" => creature.Charisma,
            _ => 10
        };

        // Barbarian Rage grants advantage on Strength checks
        if (creature.IsRaging && (abilityName == "STR" || abilityName == "Strength"))
        {
            hasAdvantage = true;
        }
        
        return D20CheckFactory.MakeAbilityCheck(abilityName, abilityScore, dc, hasAdvantage, hasDisadvantage, circumstantialBonus);
    }
    
    /// <summary>
    /// Make a saving throw for a creature.
    /// </summary>
    public static D20Check MakeSavingThrow(this Creature creature, string abilityName, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        (int abilityScore, bool proficient) = abilityName switch
        {
            "STR" or "Strength" => (creature.Strength, creature.StrengthSaveProficiency),
            "DEX" or "Dexterity" => (creature.Dexterity, creature.DexteritySaveProficiency),
            "CON" or "Constitution" => (creature.Constitution, creature.ConstitutionSaveProficiency),
            "INT" or "Intelligence" => (creature.Intelligence, creature.IntelligenceSaveProficiency),
            "WIS" or "Wisdom" => (creature.Wisdom, creature.WisdomSaveProficiency),
            "CHA" or "Charisma" => (creature.Charisma, creature.CharismaSaveProficiency),
            _ => (10, false)
        };

        // Barbarian Rage grants advantage on Strength saving throws
        if (creature.IsRaging && (abilityName == "STR" || abilityName == "Strength"))
        {
            hasAdvantage = true;
        }
        
        // Creatures typically don't have proficiency bonus unless they're players
        // For monsters, proficiency is already baked into their proficiency flags
        int proficiencyBonus = creature.IsPlayer ? DndMath.GetProficiencyBonus(1) : 2; // Default +2 for monsters
        
        return D20CheckFactory.MakeSavingThrow(abilityName, abilityScore, proficient, proficiencyBonus, dc, hasAdvantage, hasDisadvantage, circumstantialBonus);
    }
}

/// <summary>
/// Extension methods for characters to make d20 checks.
/// </summary>
public static class CharacterD20Extensions
{
    /// <summary>
    /// Make an ability check for a character.
    /// </summary>
    public static D20Check MakeAbilityCheck(this Character character, string abilityName, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        if (character.IsWearingNonProficientArmor)
            hasDisadvantage = true;

        int abilityScore = abilityName switch
        {
            "STR" or "Strength" => character.Strength,
            "DEX" or "Dexterity" => character.Dexterity,
            "CON" or "Constitution" => character.Constitution,
            "INT" or "Intelligence" => character.Intelligence,
            "WIS" or "Wisdom" => character.Wisdom,
            "CHA" or "Charisma" => character.Charisma,
            _ => 10
        };
        
        return D20CheckFactory.MakeAbilityCheck(abilityName, abilityScore, dc, hasAdvantage, hasDisadvantage, circumstantialBonus);
    }
    
    /// <summary>
    /// Make a skill check for a character.
    /// </summary>
    public static D20Check MakeSkillCheck(this Character character, string skillName, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        if (character.IsWearingNonProficientArmor)
            hasDisadvantage = true;

        (string ability, bool proficient, int abilityScore) = skillName switch
        {
            "Acrobatics" => ("DEX", character.AcrobaticsProficiency, character.Dexterity),
            "Animal Handling" => ("WIS", character.AnimalHandlingProficiency, character.Wisdom),
            "Arcana" => ("INT", character.ArcanaProficiency, character.Intelligence),
            "Athletics" => ("STR", character.AthleticsProficiency, character.Strength),
            "Deception" => ("CHA", character.DeceptionProficiency, character.Charisma),
            "History" => ("INT", character.HistoryProficiency, character.Intelligence),
            "Insight" => ("WIS", character.InsightProficiency, character.Wisdom),
            "Intimidation" => ("CHA", character.IntimidationProficiency, character.Charisma),
            "Investigation" => ("INT", character.InvestigationProficiency, character.Intelligence),
            "Medicine" => ("WIS", character.MedicineProficiency, character.Wisdom),
            "Nature" => ("INT", character.NatureProficiency, character.Intelligence),
            "Perception" => ("WIS", character.PerceptionProficiency, character.Wisdom),
            "Performance" => ("CHA", character.PerformanceProficiency, character.Charisma),
            "Persuasion" => ("CHA", character.PersuasionProficiency, character.Charisma),
            "Religion" => ("INT", character.ReligionProficiency, character.Intelligence),
            "Sleight of Hand" => ("DEX", character.SleightOfHandProficiency, character.Dexterity),
            "Stealth" => ("DEX", character.StealthProficiency, character.Dexterity),
            "Survival" => ("WIS", character.SurvivalProficiency, character.Wisdom),
            _ => ("", false, 10)
        };
        
        return D20CheckFactory.MakeSkillCheck(skillName, abilityScore, proficient, character.ProficiencyBonus, dc, hasAdvantage, hasDisadvantage, circumstantialBonus);
    }
    
    /// <summary>
    /// Make a saving throw for a character.
    /// </summary>
    public static D20Check MakeSavingThrow(this Character character, string abilityName, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        if (character.IsWearingNonProficientArmor)
            hasDisadvantage = true;

        (int abilityScore, bool proficient) = abilityName switch
        {
            "STR" or "Strength" => (character.Strength, character.StrengthSaveProficiency),
            "DEX" or "Dexterity" => (character.Dexterity, character.DexteritySaveProficiency),
            "CON" or "Constitution" => (character.Constitution, character.ConstitutionSaveProficiency),
            "INT" or "Intelligence" => (character.Intelligence, character.IntelligenceSaveProficiency),
            "WIS" or "Wisdom" => (character.Wisdom, character.WisdomSaveProficiency),
            "CHA" or "Charisma" => (character.Charisma, character.CharismaSaveProficiency),
            _ => (10, false)
        };
        
        return D20CheckFactory.MakeSavingThrow(abilityName, abilityScore, proficient, character.ProficiencyBonus, dc, hasAdvantage, hasDisadvantage, circumstantialBonus);
    }
    
    /// <summary>
    /// Make a saving throw for a character against a specific damage type.
    /// Automatically applies Dwarven Resilience advantage on poison saves.
    /// </summary>
    public static D20Check MakeSavingThrow(this Character character, string abilityName, int dc, DamageType damageContext, bool hasAdvantage = false, bool hasDisadvantage = false, int circumstantialBonus = 0)
    {
        if (character.HasDwarvenResilience && damageContext == DamageType.Poison)
            hasAdvantage = true;
        
        return character.MakeSavingThrow(abilityName, dc, hasAdvantage, hasDisadvantage, circumstantialBonus);
    }
}
