using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DND;

/// <summary>
/// Records the outcome of a short rest.
/// </summary>
public class ShortRestResult
{
    public bool Success { get; set; }
    public string FailReason { get; set; } = "";
    public int HitDiceSpent { get; set; }
    public List<int> HitDiceRolls { get; set; } = new();
    public int HPRestored { get; set; }
    public bool BardicInspirationRestored { get; set; }
    public List<string> Log { get; set; } = new();
}

/// <summary>
/// Records the outcome of a long rest.
/// </summary>
public class LongRestResult
{
    public bool Success { get; set; }
    public string FailReason { get; set; } = "";
    public int HPRestored { get; set; }
    public int HitDiceRestored { get; set; }
    public int RagesRestored { get; set; }
    public bool BardicInspirationRestored { get; set; }
    public int[] SpellSlotsRestored { get; set; } = Array.Empty<int>();
    public bool ExhaustionReduced { get; set; }
    public List<string> Log { get; set; } = new();
}

/// <summary>
/// Manages D&D 5e resting rules for player characters.
///
/// <para><b>Short Rest</b>: At least 1 hour of downtime. The character can spend one or more
/// Hit Dice (up to their maximum), rolling each die and adding their Constitution modifier
/// to regain hit points.</para>
///
/// <para><b>Long Rest</b>: At least 8 hours of downtime. The character regains all lost hit points,
/// recovers up to half their total Hit Dice (minimum 1), and restores class-specific resources
/// (Rage, Bardic Inspiration, spell slots). Reduces one level of exhaustion.
/// Limited to once per 24-hour period; requires at least 1 hit point.</para>
/// </summary>
public static class RestManager
{
    /// <summary>
    /// Takes a short rest and optionally spends Hit Dice to recover hit points.
    /// </summary>
    /// <param name="character">The character taking the short rest.</param>
    /// <param name="hitDiceToSpend">Number of Hit Dice to spend (may be 0 to rest without spending dice).</param>
    /// <returns>A <see cref="ShortRestResult"/> describing what happened.</returns>
    public static ShortRestResult ShortRest(Character character, int hitDiceToSpend)
    {
        var result = new ShortRestResult();

        if (character.CurrentHP <= 0)
        {
            result.Success = false;
            result.FailReason = "You must have at least 1 hit point to benefit from a short rest.";
            result.Log.Add(result.FailReason);
            return result;
        }

        int diceToSpend = Math.Clamp(hitDiceToSpend, 0, character.HitDiceRemaining);
        int conMod = DndMath.GetAbilityModifier(character.Constitution);
        int hpBefore = character.CurrentHP;

        for (int i = 0; i < diceToSpend; i++)
        {
            int roll = Dice.Roll(character.HitDiceType);
            int healed = Math.Max(0, roll + conMod);
            result.HitDiceRolls.Add(roll);
            character.CurrentHP = Math.Min(character.MaxHP, character.CurrentHP + healed);
            result.Log.Add($"Hit Die: d{character.HitDiceType} → {roll} + {conMod} (CON) = {healed} HP restored.");
        }

        character.HitDiceRemaining = Math.Max(0, character.HitDiceRemaining - diceToSpend);
        result.HitDiceSpent = diceToSpend;
        result.HPRestored = character.CurrentHP - hpBefore;

        // Bard: Font of Inspiration (level 5+) restores Bardic Inspiration on a short rest
        if (character.Class == "Bard" && character.Level >= 5)
        {
            character.BardicInspirationUsesRemaining = character.BardicInspirationMax;
            result.BardicInspirationRestored = true;
            result.Log.Add($"Font of Inspiration: Bardic Inspiration uses restored ({character.BardicInspirationMax}).");
        }

        // Cleric: Channel Divinity recharges on a short rest
        if (character.Class == "Cleric" && character.ChannelDivinityUsesMax > 0)
        {
            character.ChannelDivinityUsesRemaining = character.ChannelDivinityUsesMax;
            result.Log.Add($"Channel Divinity uses restored ({character.ChannelDivinityUsesMax}).");
        }

        result.Success = true;
        string summary = diceToSpend > 0
            ? $"{character.Name} takes a short rest, spending {diceToSpend} Hit {(diceToSpend == 1 ? "Die" : "Dice")} and regaining {result.HPRestored} HP."
            : $"{character.Name} takes a short rest.";
        result.Log.Insert(0, summary);
        return result;
    }

    /// <summary>
    /// Takes a long rest, restoring hit points, Hit Dice, and class resources.
    /// </summary>
    /// <param name="character">The character taking the long rest.</param>
    /// <returns>A <see cref="LongRestResult"/> describing what was restored.</returns>
    public static LongRestResult LongRest(Character character)
    {
        var result = new LongRestResult();

        if (character.CurrentHP <= 0)
        {
            result.Success = false;
            result.FailReason = "You must have at least 1 hit point to benefit from a long rest.";
            result.Log.Add(result.FailReason);
            return result;
        }

        if (character.HasLongRestedToday)
        {
            result.Success = false;
            result.FailReason = "A character can't benefit from more than one long rest in a 24-hour period.";
            result.Log.Add(result.FailReason);
            return result;
        }

        // Restore all lost hit points
        int hpBefore = character.CurrentHP;
        character.CurrentHP = character.MaxHP;
        result.HPRestored = character.MaxHP - hpBefore;
        if (result.HPRestored > 0)
            result.Log.Add($"Hit points fully restored: {hpBefore} → {character.MaxHP}.");

        // Temporary hit points expire on a long rest (PHB "Temporary Hit Points")
        if (character.TempHP > 0)
        {
            character.TempHP = 0;
            result.Log.Add("Temporary hit points expired.");
        }

        // Restore spent Hit Dice: up to half the character's total (minimum 1), rounded down
        int spentDice = character.HitDiceTotal - character.HitDiceRemaining;
        int diceToRestore = Math.Min(spentDice, Math.Max(1, DndMath.Half(character.HitDiceTotal)));
        if (diceToRestore > 0)
        {
            character.HitDiceRemaining = Math.Min(character.HitDiceTotal, character.HitDiceRemaining + diceToRestore);
            result.HitDiceRestored = diceToRestore;
            result.Log.Add($"Hit Dice restored: {diceToRestore} (now {character.HitDiceRemaining}/{character.HitDiceTotal}).");
        }

        // Reduce exhaustion by 1 level
        if (character.ExhaustionLevel > 0)
        {
            character.ExhaustionLevel--;
            result.ExhaustionReduced = true;
            result.Log.Add($"Exhaustion reduced to level {character.ExhaustionLevel}.");
        }

        // Restore class-specific resources
        RestoreClassResources(character, result);

        character.HasLongRestedToday = true;
        result.Success = true;
        result.Log.Insert(0, $"{character.Name} completes a long rest.");
        return result;
    }

    /// <summary>
    /// Resets the long rest tracker at the start of a new in-game day.
    /// Call this alongside <c>ResetDailyNourishment</c> when advancing the calendar.
    /// </summary>
    public static void ResetLongRestTracker(Character character)
    {
        character.HasLongRestedToday = false;
    }

    private static void RestoreClassResources(Character character, LongRestResult result)
    {
        switch (character.Class)
        {
            case "Barbarian":
                var barbarianData = ClassData.GetClass("Barbarian");
                var barbarianLevel = barbarianData?.GetLevelData(character.Level);
                if (barbarianLevel != null)
                {
                    if (barbarianLevel.Rages == -1)
                    {
                        result.Log.Add("Rage uses: unlimited (level 20).");
                    }
                    else
                    {
                        int restored = barbarianLevel.Rages - character.RagesRemaining;
                        if (restored > 0)
                        {
                            character.RagesRemaining = barbarianLevel.Rages;
                            result.RagesRestored = restored;
                            result.Log.Add($"Rage uses restored: {character.RagesRemaining}.");
                        }
                    }
                }
                break;

            case "Bard":
                character.BardicInspirationUsesRemaining = character.BardicInspirationMax;
                result.BardicInspirationRestored = true;
                result.Log.Add(Loc.Tr("Bardic Inspiration uses restored: {0}.", character.BardicInspirationMax));

                result.SpellSlotsRestored = new int[character.SpellSlotsMax.Length];
                for (int i = 0; i < character.SpellSlotsMax.Length; i++)
                {
                    result.SpellSlotsRestored[i] = character.SpellSlotsMax[i] - character.SpellSlotsRemaining[i];
                    character.SpellSlotsRemaining[i] = character.SpellSlotsMax[i];
                }
                if (result.SpellSlotsRestored.Any(s => s > 0))
                    result.Log.Add(Loc.Tr("Spell slots fully restored."));
                break;

            case "Cleric":
                result.SpellSlotsRestored = new int[character.SpellSlotsMax.Length];
                for (int i = 0; i < character.SpellSlotsMax.Length; i++)
                {
                    result.SpellSlotsRestored[i] = character.SpellSlotsMax[i] - character.SpellSlotsRemaining[i];
                    character.SpellSlotsRemaining[i] = character.SpellSlotsMax[i];
                }
                if (result.SpellSlotsRestored.Any(s => s > 0))
                    result.Log.Add(Loc.Tr("Spell slots fully restored."));

                if (character.ChannelDivinityUsesMax > 0)
                {
                    character.ChannelDivinityUsesRemaining = character.ChannelDivinityUsesMax;
                    result.Log.Add(Loc.Tr("Channel Divinity uses restored ({0}).", character.ChannelDivinityUsesMax));
                }
                break;
        }
    }
}
