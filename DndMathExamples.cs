// Example usage of DndMath utility class
// This demonstrates the D&D 5e "Round Down" rule in action

using System;

namespace _4DND.Examples
{
    /// <summary>
    /// Demonstrates how D&D mathematical operations always round down.
    /// </summary>
    public static class DndMathExamples
    {
        public static void RunExamples()
        {
            Console.WriteLine("=== D&D 5e Mathematical Operations ===");
            Console.WriteLine("Rule: Always round down, even if the fraction is one-half or greater\n");
            
            // Example 1: Ability Modifiers
            Console.WriteLine("--- Ability Modifiers ---");
            Console.WriteLine($"Strength 15: Modifier = {DndMath.GetAbilityModifier(15)} (not +3!)");
            Console.WriteLine($"Strength 14: Modifier = {DndMath.GetAbilityModifier(14)}");
            Console.WriteLine($"Dexterity 9: Modifier = {DndMath.GetAbilityModifier(9)}");
            Console.WriteLine($"Dexterity 8: Modifier = {DndMath.GetAbilityModifier(8)}\n");
            
            // Example 2: Proficiency Bonus
            Console.WriteLine("--- Proficiency Bonus by Level ---");
            Console.WriteLine($"Level 1: +{DndMath.GetProficiencyBonus(1)}");
            Console.WriteLine($"Level 5: +{DndMath.GetProficiencyBonus(5)}");
            Console.WriteLine($"Level 9: +{DndMath.GetProficiencyBonus(9)}");
            Console.WriteLine($"Level 13: +{DndMath.GetProficiencyBonus(13)}");
            Console.WriteLine($"Level 17: +{DndMath.GetProficiencyBonus(17)}\n");
            
            // Example 3: Damage Resistance
            Console.WriteLine("--- Damage Resistance (Halving) ---");
            Console.WriteLine($"11 fire damage with resistance: {DndMath.Half(11)} damage (not 6!)");
            Console.WriteLine($"9 cold damage with resistance: {DndMath.Half(9)} damage");
            Console.WriteLine($"15 poison damage with resistance: {DndMath.Half(15)} damage");
            Console.WriteLine($"1 acid damage with resistance: {DndMath.Half(1)} damage\n");
            
            // Example 4: Movement Speed
            Console.WriteLine("--- Movement Speed to Grid Tiles ---");
            Console.WriteLine($"30 feet speed: {DndMath.GetUnits(30, 5)} tiles");
            Console.WriteLine($"25 feet speed: {DndMath.GetUnits(25, 5)} tiles");
            Console.WriteLine($"27 feet speed: {DndMath.GetUnits(27, 5)} tiles (not 5.4!)");
            Console.WriteLine($"40 feet speed: {DndMath.GetUnits(40, 5)} tiles");
            Console.WriteLine($"35 feet speed: {DndMath.GetUnits(35, 5)} tiles\n");
            
            // Example 5: Generic Division
            Console.WriteLine("--- Generic Division Examples ---");
            Console.WriteLine($"11 / 2 = {DndMath.Divide(11, 2)} (not 6!)");
            Console.WriteLine($"9 / 2 = {DndMath.Divide(9, 2)}");
            Console.WriteLine($"15 / 4 = {DndMath.Divide(15, 4)}");
            Console.WriteLine($"19 / 5 = {DndMath.Divide(19, 5)}\n");
            
            // Real-world scenario
            Console.WriteLine("--- Real Combat Scenario ---");
            Console.WriteLine("A fighter with 15 Strength attacks with a longsword:");
            int strMod = DndMath.GetAbilityModifier(15);
            int profBonus = DndMath.GetProficiencyBonus(5); // Level 5
            int attackBonus = strMod + profBonus;
            Console.WriteLine($"  - Strength modifier: +{strMod}");
            Console.WriteLine($"  - Proficiency bonus: +{profBonus}");
            Console.WriteLine($"  - Total attack bonus: +{attackBonus}");
            Console.WriteLine($"  - If they deal 11 damage to a resistant enemy: {DndMath.Half(11)} damage taken\n");
        }
    }
}
