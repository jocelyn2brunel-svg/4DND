#nullable enable

namespace _4DND;

public class AttackResult
{
    public Creature Attacker { get; set; } = null!;
    public Creature Target { get; set; } = null!;
    public int AttackRoll { get; set; }
    public int TotalAttackBonus { get; set; }
    public int TotalToHit { get; set; }
    public DamageType DamageType { get; set; } = DamageType.None;
    public bool IsHit { get; set; }
    public bool IsCritical { get; set; }
    public bool IsCriticalMiss { get; set; }
    public int Damage { get; set; }
    public bool HasAdvantage { get; set; }
    public bool HasDisadvantage { get; set; }
    public bool IsNonProficient { get; set; }
    /// <summary>True when a net attack hit and applied the Restrained condition to the target.</summary>
    public bool TargetRestrained { get; set; }

    public string GetMessage()
    {
        string advantageText = "";
        if (HasAdvantage) advantageText = " (ADV)";
        if (HasDisadvantage) advantageText = " (DIS)";
        string profText = IsNonProficient ? " (no proficiency)" : "";

        if (IsCriticalMiss)
            return Loc.Tr("{0} critically missed {1}!{2}", Attacker.Name, Target.Name, advantageText + profText);
        if (IsCritical)
            return Loc.Tr("{0} critically hit {1} for {2} damage!{3}", Attacker.Name, Target.Name, Damage, advantageText + profText);
        if (IsHit)
        {
            if (TargetRestrained)
                return Loc.Tr("{0} throws a net and restrains {1}! (AC {2}, rolled {3}+{4}={5}){6}", Attacker.Name, Target.Name, Target.ArmorClass, AttackRoll, TotalAttackBonus, TotalToHit, advantageText + profText);
            return Loc.Tr("{0} hit {1} for {2} damage! (AC {3}, rolled {4}+{5}={6}){7}", Attacker.Name, Target.Name, Damage, Target.ArmorClass, AttackRoll, TotalAttackBonus, TotalToHit, advantageText + profText);
        }

        return Loc.Tr("{0} missed {1}! (AC {2}, rolled {3}+{4}={5}){6}", Attacker.Name, Target.Name, Target.ArmorClass, AttackRoll, TotalAttackBonus, TotalToHit, advantageText + profText);
    }
}
