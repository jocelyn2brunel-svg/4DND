using System;
using System.Collections.Generic;

namespace _4DND;

[Flags]
public enum Condition
{
    None = 0,
    Blinded = 1 << 0,         // Can't see, auto-fail sight checks, disadvantage on attacks, attacks against have advantage
    Deafened = 1 << 1,        // Can't hear, auto-fail hearing checks
    Frightened = 1 << 2,      // Disadvantage on ability checks and attacks while source visible
    Grappled = 1 << 3,        // Speed = 0
    Incapacitated = 1 << 4,   // Can't take actions or reactions
    Invisible = 1 << 5,       // Heavily obscured, advantage on attacks, attacks against have disadvantage
    Paralyzed = 1 << 6,       // Incapacitated, auto-fail STR/DEX saves, attacks have advantage, critical on hit if within 5 ft
    Petrified = 1 << 7,       // Incapacitated, resistance to all damage, immune to poison and disease
    Poisoned = 1 << 8,        // Disadvantage on attack rolls and ability checks
    Prone = 1 << 9,           // Disadvantage on attacks, attacks have advantage if within 5 ft, disadvantage if beyond
    Restrained = 1 << 10,     // Speed = 0, disadvantage on attacks and DEX saves, attacks have advantage
    Stunned = 1 << 11,        // Incapacitated, auto-fail STR/DEX saves, attacks have advantage
    Unconscious = 1 << 12     // Incapacitated, prone, auto-fail STR/DEX saves, attacks have advantage, critical on hit if within 5 ft
}

public static class ConditionExtensions
{
    public static bool HasCondition(this Condition conditions, Condition check)
    {
        return (conditions & check) != 0;
    }
    
    public static Condition AddCondition(this Condition conditions, Condition toAdd)
    {
        return conditions | toAdd;
    }
    
    public static Condition RemoveCondition(this Condition conditions, Condition toRemove)
    {
        return conditions & ~toRemove;
    }
    
    public static string GetDescription(this Condition condition)
    {
        return condition switch
        {
            Condition.Blinded => "Blinded: Can't see, auto-fail sight checks, disadvantage on attacks",
            Condition.Deafened => "Deafened: Can't hear, auto-fail hearing checks",
            Condition.Frightened => "Frightened: Disadvantage on checks and attacks",
            Condition.Grappled => "Grappled: Speed is 0",
            Condition.Incapacitated => "Incapacitated: Can't take actions or reactions",
            Condition.Invisible => "Invisible: Heavily obscured, advantage on attacks",
            Condition.Paralyzed => "Paralyzed: Incapacitated, auto-fail STR/DEX saves",
            Condition.Petrified => "Petrified: Turned to stone, resistance to all damage",
            Condition.Poisoned => "Poisoned: Disadvantage on attacks and checks",
            Condition.Prone => "Prone: Disadvantage on attacks",
            Condition.Restrained => "Restrained: Speed is 0, disadvantage on attacks",
            Condition.Stunned => "Stunned: Incapacitated, auto-fail STR/DEX saves",
            Condition.Unconscious => "Unconscious: Incapacitated, prone, auto-fail saves",
            _ => "Unknown condition"
        };
    }
    
    public static List<string> GetActiveConditionNames(this Condition conditions)
    {
        var active = new List<string>();
        
        foreach (Condition c in Enum.GetValues(typeof(Condition)))
        {
            if (c != Condition.None && conditions.HasCondition(c))
            {
                active.Add(c.ToString());
            }
        }
        
        return active;
    }
}
