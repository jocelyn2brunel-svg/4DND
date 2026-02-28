using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public partial class CombatManager
{
    public static List<CreatureType> GenerateEncounter(
        CampaignDifficulty difficulty,
        BiomeType biome,
        IEnumerable<int> partyLevels,
        Random? random = null)
    {
        random ??= new Random();
        var partyList = partyLevels.ToList();
        int partySize = partyList.Count;

        // 1. Determine target XP budget based on campaign difficulty
        int budget = 0;
        foreach (int level in partyList)
        {
            var tier = difficulty switch
            {
                CampaignDifficulty.Apprentice => EncounterDifficulty.Easy,
                CampaignDifficulty.Adventurer => EncounterDifficulty.Medium,
                CampaignDifficulty.Heroic => EncounterDifficulty.Hard,
                CampaignDifficulty.Legendary => EncounterDifficulty.Deadly,
                CampaignDifficulty.Mythic => EncounterDifficulty.Mythic,
                _ => EncounterDifficulty.Medium
            };
            budget += DndMath.GetEncounterXPThreshold(level, tier);
        }

        // 2. Filter available monsters for the biome
        var possibleTypes = Creature.GetTypesForBiome(biome)
            .Select(t => new { Type = t, XP = Creature.GetXPForType(t) })
            .Where(t => t.XP <= budget) // Can't have a single monster more expensive than the whole budget
            .OrderByDescending(t => t.XP)
            .ToList();

        if (possibleTypes.Count == 0) return new List<CreatureType>();

        // 3. Try to fill the budget
        var selectedEnemies = new List<CreatureType>();
        int currentTotalXP = 0;

        // Strategy: Start with one "stronger" enemy if possible, then fill with smaller ones
        // or just pick randomly from affordable ones.

        // To handle the multiplier, we need to track adjusted XP
        bool budgetFull = false;
        int attempts = 0;
        while (!budgetFull && attempts < 20)
        {
            attempts++;
            // Only consider types that won't push us over the adjusted budget
            var affordable = possibleTypes.Where(t => {
                int nextCount = selectedEnemies.Count + 1;
                float multiplier = DndMath.GetMonsterCountMultiplier(nextCount, partySize);
                int nextAdjustedXP = (int)((currentTotalXP + t.XP) * multiplier);
                return nextAdjustedXP <= budget;
            }).ToList();

            if (affordable.Count == 0)
            {
                budgetFull = true;
                break;
            }

            // Prefer variety or quantity? Let's pick randomly from affordable
            var picked = affordable[random.Next(affordable.Count)];
            selectedEnemies.Add(picked.Type);
            currentTotalXP += picked.XP;
        }

        return selectedEnemies;
    }

    /// <summary>
    /// Calculates the difficulty of an encounter (DMG p.82-84) given the player characters
    /// and enemy creatures involved.
    /// </summary>
    /// <param name="playerCreatures">The player characters participating in the encounter.</param>
    /// <param name="enemyCreatures">The enemy creatures in the encounter.</param>
    /// <returns>A full difficulty breakdown including thresholds and adjusted XP.</returns>
    public static EncounterDifficultyResult GetEncounterDifficulty(
        IEnumerable<Creature> playerCreatures,
        IEnumerable<Creature> enemyCreatures)
    {
        var partyLevels = playerCreatures.Select(c => c.Level);
        var monsterXP = enemyCreatures.Select(c => c.XPReward);
        return DndMath.CalculateEncounterDifficulty(partyLevels, monsterXP);
    }
}
