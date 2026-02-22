using System;

namespace _4DND;

public enum SkillCheckType
{
    Standard,
    Advantage,
    Disadvantage
}

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
        var random = new Random();
        int roll1 = random.Next(1, 21);
        int roll2 = checkType != SkillCheckType.Standard ? random.Next(1, 21) : roll1;
        
        int finalRoll = checkType switch
        {
            SkillCheckType.Advantage => Math.Max(roll1, roll2),
            SkillCheckType.Disadvantage => Math.Min(roll1, roll2),
            _ => roll1
        };
        
        int bonus = character.GetSkillBonus(skill, out string ability);
        int total = finalRoll + bonus;
        
        return new SkillCheck
        {
            SkillName = skill,
            DC = dc,
            Roll = finalRoll,
            Bonus = bonus,
            Total = total,
            Success = total >= dc,
            CheckType = checkType,
            Ability = ability
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
    public static SkillCheck MakePerceptionCheck(Character character, VisionSystem visionSystem, int x, int y, int dc)
    {
        var checkType = SkillCheckType.Standard;
        
        // Disadvantage if in dim light or lightly obscured
        if (visionSystem.IsLightlyObscured(x, y))
        {
            checkType = SkillCheckType.Disadvantage;
        }
        
        // Auto-fail if heavily obscured
        var playerCreature = Creature.FromCharacter(character, x, y);
        if (visionSystem.IsHeavilyObscured(x, y, playerCreature))
        {
            return new SkillCheck
            {
                SkillName = "Perception",
                DC = dc,
                Roll = 0,
                Bonus = 0,
                Total = 0,
                Success = false,
                CheckType = checkType,
                Ability = "WIS"
            };
        }
        
        return SkillCheck.MakeCheck(character, "Perception", dc, checkType);
    }
}
