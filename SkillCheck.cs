using System;

namespace _4DND;

public enum SkillCheckType
{
    Standard,
    Advantage,
    Disadvantage
}

/// <summary>
/// Legacy SkillCheck class for backward compatibility.
/// New code should use D20Check and D20CheckFactory instead.
/// </summary>
public class SkillCheck
{
    public string SkillName { get; set; } = "";
    public int DC { get; set; }
    public int Roll { get; set; }
    public int Bonus { get; set; }
    public int Total { get; set; }
    public bool Success { get; set; }
    public SkillCheckType CheckType { get; set; } = SkillCheckType.Standard;
    public string Ability { get; set; } = "";
    
    public static SkillCheck MakeCheck(Character character, string skill, int dc, SkillCheckType checkType = SkillCheckType.Standard)
    {
        // Convert to new D20Check system
        bool hasAdvantage = checkType == SkillCheckType.Advantage;
        bool hasDisadvantage = checkType == SkillCheckType.Disadvantage;
        
        var d20Check = character.MakeSkillCheck(skill, dc, hasAdvantage, hasDisadvantage);
        
        // Convert back to legacy format
        return new SkillCheck
        {
            SkillName = skill,
            DC = dc,
            Roll = d20Check.DieRoll,
            Bonus = d20Check.BaseModifier,
            Total = d20Check.Total,
            Success = d20Check.Success,
            CheckType = checkType,
            Ability = GetSkillAbility(skill)
        };
    }
    
    private static string GetSkillAbility(string skill)
    {
        return skill switch
        {
            "Acrobatics" => "DEX",
            "Animal Handling" => "WIS",
            "Arcana" => "INT",
            "Athletics" => "STR",
            "Deception" => "CHA",
            "History" => "INT",
            "Insight" => "WIS",
            "Intimidation" => "CHA",
            "Investigation" => "INT",
            "Medicine" => "WIS",
            "Nature" => "INT",
            "Perception" => "WIS",
            "Performance" => "CHA",
            "Persuasion" => "CHA",
            "Religion" => "INT",
            "Sleight of Hand" => "DEX",
            "Stealth" => "DEX",
            "Survival" => "WIS",
            _ => ""
        };
    }
    
    public string GetMessage()
    {
        string typeText = CheckType switch
        {
            SkillCheckType.Advantage => " (ADV)",
            SkillCheckType.Disadvantage => " (DIS)",
            _ => ""
        };
        
        string result = Success ? "Success" : "Failure";
        return $"{SkillName} check{typeText}: {Roll} + {Bonus} = {Total} vs DC {DC} - {result}";
    }
}

public static class VisionSkillChecks
{
    public static D20Check MakePerceptionCheck(Character character, VisionSystem visionSystem, int x, int y, int z, int dc)
    {
        var playerCreature = Creature.FromCharacter(character, x, y, z);
        
        bool hasAdvantage = false;
        bool hasDisadvantage = false;
        
        // Disadvantage if in dim light or lightly obscured
        if (visionSystem.IsLightlyObscured(x, y, z, playerCreature))
        {
            hasDisadvantage = true;
        }
        
        // Auto-fail if heavily obscured (no need to roll)
        if (visionSystem.IsHeavilyObscured(x, y, z, playerCreature))
        {
            return new D20Check
            {
                CheckType = D20CheckType.AbilityCheck,
                Description = "Perception",
                DieRoll = 1,
                BaseModifier = character.GetSkillBonus("Perception", out _),
                CircumstantialBonus = 0,
                HasAdvantage = false,
                HasDisadvantage = false,
                SecondRoll = 0,
                TargetNumber = dc
            };
        }
        
        return character.MakeSkillCheck("Perception", dc, hasAdvantage, hasDisadvantage);
    }
}
