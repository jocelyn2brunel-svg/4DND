using System.Collections.Generic;

#nullable enable

namespace _4DND;

public class SpellResult
{
    public Creature Caster { get; set; } = null!;
    public string SpellName { get; set; } = "";
    public int DamageRolled { get; set; }
    public DamageType DamageType { get; set; } = DamageType.None;
    public List<(Creature Target, bool SavedSuccessfully, int DamageTaken)> TargetResults { get; } = new();

    public string GetMessage()
    {
        if (TargetResults.Count == 0)
            return $"{Caster.Name} casts {SpellName} but hits no targets.";

        var sb = new System.Text.StringBuilder();
        sb.Append($"{Caster.Name} casts {SpellName} for {DamageRolled} {DamageType.ToDisplayString()} damage!");
        foreach (var (target, saved, taken) in TargetResults)
        {
            string saveText = saved ? "saved (half)" : "failed save";
            sb.Append($"\n  {target.Name}: {saveText} ? {taken} damage");
        }
        return sb.ToString();
    }
}
