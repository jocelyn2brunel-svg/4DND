using System;
using System.Collections.Generic;

namespace _4DND
{
    /// <summary>
    /// The four encounter difficulty categories from the DMG (p.82).
    /// </summary>
    public enum EncounterDifficulty
    {
        /// <summary>Doesn't tax character resources; victory is almost guaranteed.</summary>
        Easy,
        /// <summary>One or two scary moments; party should emerge victorious.</summary>
        Medium,
        /// <summary>Could go badly; weaker characters might fall, slim chance of death.</summary>
        Hard,
        /// <summary>Could be lethal; survival requires tactics and quick thinking.</summary>
        Deadly,
        /// <summary>Beyond deadly; for the most experienced and well-equipped heroes.</summary>
        Mythic
    }

    /// <summary>
    /// Result returned by <see cref="DndMath.CalculateEncounterDifficulty"/>.
    /// </summary>
    public sealed class EncounterDifficultyResult
    {
        /// <summary>Overall difficulty category of the encounter.</summary>
        public EncounterDifficulty Difficulty { get; init; }

        /// <summary>Raw sum of all monster XP rewards.</summary>
        public int TotalMonsterXP { get; init; }

        /// <summary>Adjusted XP after applying the monster-count multiplier (used for comparison).</summary>
        public int AdjustedXP { get; init; }

        /// <summary>Multiplier applied to the raw XP total based on the number of monsters.</summary>
        public float Multiplier { get; init; }

        /// <summary>XP thresholds for the party at each difficulty tier.</summary>
        public int EasyThreshold { get; init; }
        public int MediumThreshold { get; init; }
        public int HardThreshold { get; init; }
        public int DeadlyThreshold { get; init; }
        public int MythicThreshold { get; init; }
    }

    /// <summary>
    /// Utility class for D&D 5e mathematical operations.
    /// Follows the core rule: "Whenever you divide a number in the game,
    /// round down if you end up with a fraction, even if the fraction is one-half or greater."
    /// </summary>
    public static class DndMath
    {
        /// <summary>
        /// Divides two integers following D&D rounding rules (always round down).
        /// </summary>
        /// <param name="dividend">The number to be divided</param>
        /// <param name="divisor">The number to divide by</param>
        /// <returns>The result rounded down to the nearest integer</returns>
        public static int Divide(int dividend, int divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException("Cannot divide by zero");
            
            // Integer division in C# already rounds towards zero
            // For positive results, this is equivalent to floor
            // For negative results, we need to explicitly floor
            if (dividend >= 0 && divisor > 0)
            {
                return dividend / divisor;
            }
            else if (dividend < 0 && divisor > 0)
            {
                // Round down (more negative) for negative dividends
                return (int)Math.Floor((double)dividend / divisor);
            }
            else
            {
                // For consistency, always round towards negative infinity
                return (int)Math.Floor((double)dividend / divisor);
            }
        }
        
        /// <summary>
        /// Calculates an ability modifier from an ability score (always round down).
        /// Formula: (ability_score - 10) / 2
        /// </summary>
        /// <param name="abilityScore">The ability score (e.g., Strength, Dexterity)</param>
        /// <returns>The ability modifier</returns>
        public static int GetAbilityModifier(int abilityScore)
        {
            return Divide(abilityScore - 10, 2);
        }
        
        /// <summary>
        /// Calculates proficiency bonus based on character level (always round down).
        /// Formula: 2 + (level - 1) / 4
        /// </summary>
        /// <param name="level">Character level (1-20)</param>
        /// <returns>The proficiency bonus</returns>
        public static int GetProficiencyBonus(int level)
        {
            return 2 + Divide(level - 1, 4);
        }
        
        /// <summary>
        /// Halves a value following D&D rounding rules (always round down).
        /// Commonly used for resistance to damage.
        /// </summary>
        /// <param name="value">The value to halve</param>
        /// <returns>Half the value, rounded down</returns>
        public static int Half(int value)
        {
            return Divide(value, 2);
        }
        
        /// <summary>
        /// Calculates how many full units fit into a value.
        /// Example: Converting movement speed to grid tiles (speed / 5 feet per tile)
        /// </summary>
        /// <param name="total">The total amount</param>
        /// <param name="unitSize">The size of each unit</param>
        /// <returns>Number of complete units, rounded down</returns>
        public static int GetUnits(int total, int unitSize)
        {
            return Divide(total, unitSize);
        }
        
        /// <summary>
        /// Standard Difficulty Class values per D&D 5e rules.
        /// </summary>
        public static class DifficultyClass
        {
            public const int VeryEasy = 5;
            public const int Easy = 10;
            public const int Medium = 15;
            public const int Hard = 20;
            public const int VeryHard = 25;
            public const int NearlyImpossible = 30;
        }
        
        /// <summary>
        /// Checks if a total roll (d20 + modifiers) meets or exceeds the target DC.
        /// </summary>
        /// <param name="total">The total of the roll plus all modifiers</param>
        /// <param name="dc">The Difficulty Class (target number)</param>
        /// <returns>True if the total equals or exceeds the DC (success), false otherwise</returns>
        public static bool MeetsDC(int total, int dc)
        {
            return total >= dc;
        }

        /// <summary>
        /// Returns the AC and Dexterity saving throw bonus granted by the given degree of cover (PHB "Cover").
        /// Half cover grants +2; three-quarters cover grants +5; total cover grants no numeric bonus
        /// (it prevents targeting entirely, handled at the attack level).
        /// </summary>
        public static int GetCoverBonus(CoverType cover) => cover switch
        {
            CoverType.Half => 2,
            CoverType.ThreeQuarters => 5,
            _ => 0
        };

        /// <summary>
        /// XP required to reach each level (D&D 5e Character Advancement table, PHB p.15).
        /// Index = level (1-based), value = total XP needed to reach that level.
        /// </summary>
        private static readonly int[] XPThresholds = new int[]
        {
                 0,   //  1
               300,   //  2
               900,   //  3
             2_700,   //  4
             6_500,   //  5
            14_000,   //  6
            23_000,   //  7
            34_000,   //  8
            48_000,   //  9
            64_000,   // 10
            85_000,   // 11
           100_000,   // 12
           120_000,   // 13
           140_000,   // 14
           165_000,   // 15
           195_000,   // 16
           225_000,   // 17
           265_000,   // 18
           305_000,   // 19
           355_000,   // 20
        };

        /// <summary>
        /// Returns the minimum XP required to reach <paramref name="level"/> (1–20).
        /// </summary>
        public static int GetXPThreshold(int level)
        {
            int index = Math.Clamp(level, 1, 20) - 1;
            return XPThresholds[index];
        }

        /// <summary>
        /// Returns the XP required to reach the next level after <paramref name="currentLevel"/>.
        /// Returns -1 if already at max level (20).
        /// </summary>
        public static int GetNextLevelXP(int currentLevel)
        {
            if (currentLevel >= 20) return -1;
            return XPThresholds[Math.Clamp(currentLevel, 1, 19)];
        }

        /// <summary>
        /// Returns the character level (1–20) that corresponds to <paramref name="totalXP"/>.
        /// </summary>
        public static int GetLevelForXP(int totalXP)
        {
            int level = 1;
            for (int i = XPThresholds.Length - 1; i >= 0; i--)
            {
                if (totalXP >= XPThresholds[i])
                {
                    level = i + 1;
                    break;
                }
            }
            return level;
        }

        /// <summary>
        /// Encounter XP thresholds per character level (DMG p.82).
        /// Outer index = level - 1 (0-based).  Inner index: 0=Easy, 1=Medium, 2=Hard, 3=Deadly.
        /// </summary>
        private static readonly int[,] EncounterXPThresholds = new int[20, 4]
        {
            //  Easy  Medium  Hard  Deadly
            {    25,     50,   75,    100 }, // Level  1
            {    50,    100,  150,    200 }, // Level  2
            {    75,    150,  225,    400 }, // Level  3
            {   125,    250,  375,    500 }, // Level  4
            {   250,    500,  750,  1_100 }, // Level  5
            {   300,    600,  900,  1_400 }, // Level  6
            {   350,    750, 1100,  1_700 }, // Level  7
            {   450,    900, 1400,  2_100 }, // Level  8
            {   550,  1_100, 1600,  2_400 }, // Level  9
            {   600,  1_200, 1900,  2_800 }, // Level 10
            {   800,  1_600, 2400,  3_600 }, // Level 11
            {  1000,  2_000, 3000,  4_500 }, // Level 12
            {  1100,  2_200, 3400,  5_100 }, // Level 13
            {  1250,  2_500, 3800,  5_700 }, // Level 14
            {  1400,  2_800, 4300,  6_400 }, // Level 15
            {  1600,  3_200, 4800,  7_200 }, // Level 16
            {  2000,  3_900, 5900,  8_800 }, // Level 17
            {  2100,  4_200, 6300,  9_500 }, // Level 18
            {  2400,  4_900, 7300, 10_900 }, // Level 19
            {  2800,  5_700, 8500, 12_700 }, // Level 20
        };

        /// <summary>
        /// XP multipliers by number of monsters (DMG p.82).
        /// Base values: 1 = ×1, 2 = ×1.5, 3–6 = ×2, 7–10 = ×2.5, 11–14 = ×3, 15+ = ×4.
        /// For a small party (≤2 PCs), use one step higher; for a large party (≥6 PCs), use one step lower.
        /// </summary>
        public static float GetMonsterCountMultiplier(int monsterCount, int partySize)
        {
            // Base multiplier index (0–5) from the DMG table
            int baseIndex = monsterCount switch
            {
                1      => 0,  // ×1
                2      => 1,  // ×1.5
                <= 6   => 2,  // ×2
                <= 10  => 3,  // ×2.5
                <= 14  => 4,  // ×3
                _      => 5,  // ×4
            };

            // Adjust one step for very small or very large parties
            int adjustedIndex = partySize <= 2 ? baseIndex + 1
                              : partySize >= 6 ? baseIndex - 1
                              : baseIndex;

            adjustedIndex = Math.Clamp(adjustedIndex, 0, 5);

            return adjustedIndex switch
            {
                0 => 1.0f,
                1 => 1.5f,
                2 => 2.0f,
                3 => 2.5f,
                4 => 3.0f,
                _ => 4.0f,
            };
        }

        /// <summary>
        /// Returns the Easy/Medium/Hard/Deadly XP threshold for a character of the given level.
        /// </summary>
        /// <param name="level">Character level (1–20).</param>
        /// <param name="difficulty">Which tier to retrieve.</param>
        public static int GetEncounterXPThreshold(int level, EncounterDifficulty difficulty)
        {
            int index = Math.Clamp(level, 1, 20) - 1;
            if (difficulty == EncounterDifficulty.Mythic)
            {
                // Mythic is 1.5x Deadly budget
                return (int)(EncounterXPThresholds[index, 3] * 1.5f);
            }
            return EncounterXPThresholds[index, (int)difficulty];
        }

        /// <summary>
        /// Calculates the difficulty of a combat encounter following DMG p.82-84.
        ///
        /// <para>Steps performed:</para>
        /// <list type="number">
        ///   <item>Sum all party XP thresholds at each tier to get party thresholds.</item>
        ///   <item>Sum raw monster XP rewards.</item>
        ///   <item>Multiply by the monster-count multiplier (adjusted for party size).</item>
        ///   <item>Compare adjusted XP against party thresholds to determine difficulty.</item>
        /// </list>
        /// </summary>
        /// <param name="partyLevels">Level of each player character in the party.</param>
        /// <param name="monsterXPRewards">XP reward of each monster in the encounter.</param>
        /// <returns>A <see cref="EncounterDifficultyResult"/> with full breakdown.</returns>
        public static EncounterDifficultyResult CalculateEncounterDifficulty(
            IEnumerable<int> partyLevels,
            IEnumerable<int> monsterXPRewards)
        {
            // 1. Compute cumulative party thresholds
            int easyThreshold = 0, mediumThreshold = 0, hardThreshold = 0, deadlyThreshold = 0, mythicThreshold = 0;
            int partySize = 0;
            foreach (int level in partyLevels)
            {
                int idx = Math.Clamp(level, 1, 20) - 1;
                easyThreshold += EncounterXPThresholds[idx, 0];
                mediumThreshold += EncounterXPThresholds[idx, 1];
                hardThreshold += EncounterXPThresholds[idx, 2];
                deadlyThreshold += EncounterXPThresholds[idx, 3];
                mythicThreshold += (int)(EncounterXPThresholds[idx, 3] * 1.5f);
                partySize++;
            }

            // 2. Sum raw monster XP
            int totalMonsterXP = 0;
            int monsterCount = 0;
            foreach (int xp in monsterXPRewards)
            {
                totalMonsterXP += xp;
                monsterCount++;
            }

            // 3. Apply multiplier
            float multiplier = monsterCount == 0
                ? 1.0f
                : GetMonsterCountMultiplier(monsterCount, partySize);
            int adjustedXP = (int)(totalMonsterXP * multiplier);

            // 4. Determine difficulty
            EncounterDifficulty difficulty;
            if (adjustedXP >= mythicThreshold)
                difficulty = EncounterDifficulty.Mythic;
            else if (adjustedXP >= deadlyThreshold)
                difficulty = EncounterDifficulty.Deadly;
            else if (adjustedXP >= hardThreshold)
                difficulty = EncounterDifficulty.Hard;
            else if (adjustedXP >= mediumThreshold)
                difficulty = EncounterDifficulty.Medium;
            else
                difficulty = EncounterDifficulty.Easy;

            return new EncounterDifficultyResult
            {
                Difficulty = difficulty,
                TotalMonsterXP = totalMonsterXP,
                AdjustedXP = adjustedXP,
                Multiplier = multiplier,
                EasyThreshold = easyThreshold,
                MediumThreshold = mediumThreshold,
                HardThreshold = hardThreshold,
                DeadlyThreshold = deadlyThreshold,
                MythicThreshold = mythicThreshold,
            };
        }
    }
}
