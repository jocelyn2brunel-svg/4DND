#nullable enable

namespace _4DND;

public enum MonsterActionType
{
    MeleeAttack,
    RangedAttack,
    Special
}

/// <summary>
/// Represents a named action in a monster's stat block (MM "Actions").
/// When a monster takes its action, it can choose from these entries or use a
/// standard action available to all creatures (Dash, Hide, etc.) as described in the PHB.
/// </summary>
public class MonsterAction
{
    public string Name { get; init; } = "";
    public MonsterActionType ActionType { get; init; } = MonsterActionType.MeleeAttack;
    public int AttackBonus { get; init; }
    public string DamageDice { get; init; } = "1d4";
    public int DamageBonus { get; init; }
    public DamageType DamageType { get; init; } = DamageType.None;

    /// <summary>Melee reach in squares (1 = 5 ft, 2 = 10 ft).</summary>
    public int Reach { get; init; } = 1;

    /// <summary>Normal range for ranged attacks in feet. 0 for melee-only actions.</summary>
    public int NormalRange { get; init; } = 0;

    /// <summary>Long range for ranged attacks in feet. 0 means equal to NormalRange.</summary>
    public int LongRange { get; init; } = 0;

    /// <summary>Optional flavour text matching the stat block entry.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Applies this action's attack properties to <paramref name="creature"/> so that
    /// <see cref="CombatManager.MakeAttack"/> uses the correct values.
    /// </summary>
    public void ApplyTo(Creature creature)
    {
        creature.AttackName = Name;
        creature.AttackBonus = AttackBonus;
        creature.DamageDice = DamageDice;
        creature.DamageBonus = DamageBonus;
        creature.CurrentDamageType = DamageType;
        creature.IsMeleeAttack = ActionType == MonsterActionType.MeleeAttack;
        creature.IsReachWeapon = Reach > 1;
        creature.NormalRange = NormalRange;
        creature.LongRange = LongRange;
    }
}
