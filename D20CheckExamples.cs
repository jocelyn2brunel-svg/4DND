using System;

namespace _4DND;

/// <summary>
/// Example usage of the D20Check system following D&D 5e rules.
/// </summary>
public static class D20CheckExamples
{
    /// <summary>
    /// Example 1: Simple ability check (breaking down a door)
    /// A character with Strength 16 (+3 modifier) tries to break down a door (DC 15).
    /// </summary>
    public static void Example1_SimpleAbilityCheck()
    {
        var character = new Character 
        { 
            Name = "Conan",
            Strength = 16,  // +3 modifier
            Level = 1
        };
        
        // Make a Strength check against DC 15
        var check = character.MakeAbilityCheck("Strength", DndMath.DifficultyClass.Medium);
        
        Console.WriteLine(check.GetDetailedMessage());
        // Example output: "Strength Check: 14 + 3 = 17 vs DC 15 - SUCCESS"
        
        if (check.Success)
        {
            Console.WriteLine("The door breaks open!");
        }
        else
        {
            Console.WriteLine("The door holds firm.");
        }
    }
    
    /// <summary>
    /// Example 2: Skill check with proficiency (Stealth)
    /// A rogue with Dexterity 18 (+4) and proficiency in Stealth tries to sneak past guards (DC 14).
    /// </summary>
    public static void Example2_SkillCheckWithProficiency()
    {
        var character = new Character 
        { 
            Name = "Shadow",
            Dexterity = 18,  // +4 modifier
            Level = 3,       // +2 proficiency bonus
            StealthProficiency = true
        };
        
        // Make a Stealth check
        var check = character.MakeSkillCheck("Stealth", 14);
        
        Console.WriteLine(check.GetDetailedMessage());
        // Example output: "Stealth: 11 + 6 = 17 vs DC 14 - SUCCESS"
        // The +6 comes from: +4 (Dex modifier) + 2 (proficiency bonus)
        
        if (check.Success)
        {
            Console.WriteLine("You sneak past unnoticed!");
        }
        else
        {
            Console.WriteLine("A guard spots you!");
        }
    }
    
    /// <summary>
    /// Example 3: Check with advantage (attacking a prone enemy)
    /// An attack roll against a prone enemy has advantage.
    /// </summary>
    public static void Example3_AttackWithAdvantage()
    {
        // Attack roll: +5 attack bonus against AC 16 with advantage
        var attackRoll = D20CheckFactory.MakeAttackRoll(
            "Longsword",
            attackBonus: 5,
            targetAC: 16,
            hasAdvantage: true
        );
        
        Console.WriteLine(attackRoll.GetDetailedMessage());
        // Example output: "Longsword Attack [ADV: 17/8]: 17 + 5 = 22 vs AC 16 - SUCCESS"
        
        if (attackRoll.IsCriticalHit)
        {
            Console.WriteLine("Critical hit! Roll damage dice twice!");
        }
        else if (attackRoll.Success)
        {
            Console.WriteLine("Hit! Roll damage normally.");
        }
    }
    
    /// <summary>
    /// Example 4: Saving throw against a spell (Fireball)
    /// A character must make a Dexterity saving throw to avoid a fireball's full damage (DC 15).
    /// </summary>
    public static void Example4_SavingThrow()
    {
        var character = new Character 
        { 
            Name = "Gandalf",
            Dexterity = 14,  // +2 modifier
            Level = 5,       // +3 proficiency bonus
            DexteritySaveProficiency = true  // Wizards have Dex saves
        };
        
        // Make a Dexterity saving throw
        var save = character.MakeSavingThrow("Dexterity", 15);
        
        Console.WriteLine(save.GetDetailedMessage());
        // Example output: "Dexterity Save: 12 + 5 = 17 vs DC 15 - SUCCESS"
        // The +5 comes from: +2 (Dex modifier) + 3 (proficiency bonus)
        
        if (save.Success)
        {
            Console.WriteLine("You dodge! Take half damage.");
        }
        else
        {
            Console.WriteLine("Direct hit! Take full damage.");
        }
    }
    
    /// <summary>
    /// Example 5: Multiple sources of advantage and disadvantage cancel out
    /// </summary>
    public static void Example5_AdvantageDisadvantageCancellation()
    {
        var character = new Character 
        { 
            Name = "Blinded Archer",
            Dexterity = 16,
            Level = 4
        };
        
        // Attacker is blinded (disadvantage) but target is prone (advantage)
        // They cancel out - roll normally
        var attackRoll = D20CheckFactory.MakeAttackRoll(
            "Longbow",
            attackBonus: 6,
            targetAC: 14,
            hasAdvantage: true,    // Target is prone
            hasDisadvantage: true  // Attacker is blinded
        );
        
        Console.WriteLine(attackRoll.GetDetailedMessage());
        // Example output: "Longbow Attack [ADV/DIS canceled]: 13 + 6 = 19 vs AC 14 - SUCCESS"
    }
    
    /// <summary>
    /// Example 6: Circumstantial bonus (Guidance spell)
    /// A cleric casts Guidance on an ally, adding 1d4 to their next ability check.
    /// </summary>
    public static void Example6_CircumstantialBonus()
    {
        var character = new Character 
        { 
            Name = "Cleric",
            Wisdom = 16,
            Level = 3,
            MedicineProficiency = true
        };
        
        // Roll 1d4 for Guidance spell
        int guidanceBonus = new Random().Next(1, 5);  // 1d4
        
        // Make a Medicine check with Guidance bonus
        var check = character.MakeSkillCheck(
            "Medicine", 
            DndMath.DifficultyClass.Medium,
            circumstantialBonus: guidanceBonus
        );
        
        Console.WriteLine(check.GetDetailedMessage());
        // Example output: "Medicine: 10 + 5 + 3 (situational) = 18 vs DC 15 - SUCCESS"
    }
    
    /// <summary>
    /// Example 7: Using standard Difficulty Classes
    /// </summary>
    public static void Example7_DifficultyClasses()
    {
        var character = new Character 
        { 
            Name = "Adventurer",
            Intelligence = 14,
            Level = 2,
            ArcanaProficiency = true
        };
        
        // Very Easy: Recognize a common magic item
        var veryEasy = character.MakeSkillCheck("Arcana", DndMath.DifficultyClass.VeryEasy);
        Console.WriteLine($"Very Easy (DC {DndMath.DifficultyClass.VeryEasy}): {veryEasy.GetSimpleMessage()}");
        
        // Easy: Recall common knowledge about magic
        var easy = character.MakeSkillCheck("Arcana", DndMath.DifficultyClass.Easy);
        Console.WriteLine($"Easy (DC {DndMath.DifficultyClass.Easy}): {easy.GetSimpleMessage()}");
        
        // Medium: Identify an uncommon spell
        var medium = character.MakeSkillCheck("Arcana", DndMath.DifficultyClass.Medium);
        Console.WriteLine($"Medium (DC {DndMath.DifficultyClass.Medium}): {medium.GetSimpleMessage()}");
        
        // Hard: Understand ancient magical runes
        var hard = character.MakeSkillCheck("Arcana", DndMath.DifficultyClass.Hard);
        Console.WriteLine($"Hard (DC {DndMath.DifficultyClass.Hard}): {hard.GetSimpleMessage()}");
        
        // Very Hard: Decipher a legendary artifact
        var veryHard = character.MakeSkillCheck("Arcana", DndMath.DifficultyClass.VeryHard);
        Console.WriteLine($"Very Hard (DC {DndMath.DifficultyClass.VeryHard}): {veryHard.GetSimpleMessage()}");
        
        // Nearly Impossible: Comprehend god-level magic
        var nearlyImpossible = character.MakeSkillCheck("Arcana", DndMath.DifficultyClass.NearlyImpossible);
        Console.WriteLine($"Nearly Impossible (DC {DndMath.DifficultyClass.NearlyImpossible}): {nearlyImpossible.GetSimpleMessage()}");
    }
    
    /// <summary>
    /// Example 8: Critical hits and misses (attack rolls only)
    /// </summary>
    public static void Example8_CriticalHitsAndMisses()
    {
        // Natural 20 on attack roll = critical hit
        // Natural 1 on attack roll = automatic miss
        
        var attacker = Creature.CreateGoblin(0, 0, 0);
        var target = Creature.CreateOrc(1, 1, 0);
        
        // The attack system automatically handles critical hits/misses
        // A natural 20 is always a hit, regardless of AC
        // A natural 1 is always a miss, regardless of attack bonus
        
        Console.WriteLine("Making attack rolls...");
        for (int i = 0; i < 10; i++)
        {
            var attackRoll = D20CheckFactory.MakeAttackRoll(
                attacker.AttackName,
                attacker.AttackBonus,
                target.ArmorClass
            );
            
            if (attackRoll.IsCriticalHit)
            {
                Console.WriteLine($"Roll {i+1}: {attackRoll.GetDetailedMessage()} - DOUBLE DAMAGE!");
            }
            else if (attackRoll.IsCriticalMiss)
            {
                Console.WriteLine($"Roll {i+1}: {attackRoll.GetDetailedMessage()}");
            }
            else
            {
                Console.WriteLine($"Roll {i+1}: {attackRoll.GetSimpleMessage()}");
            }
        }
    }
    
    /// <summary>
    /// Example 9: Creature saving throws
    /// </summary>
    public static void Example9_CreatureSavingThrows()
    {
        var goblin = Creature.CreateGoblin(0, 0, 0);
        var couatl = Creature.CreateCouatl(5, 5, 0);
        
        // Goblin makes a Wisdom save against a spell (DC 13)
        var goblinSave = goblin.MakeSavingThrow("Wisdom", 13);
        Console.WriteLine($"Goblin: {goblinSave.GetDetailedMessage()}");
        
        // Couatl makes a Wisdom save (has proficiency)
        var couatlSave = couatl.MakeSavingThrow("Wisdom", 13);
        Console.WriteLine($"Couatl: {couatlSave.GetDetailedMessage()}");
    }
    
    /// <summary>
    /// Example 10: Complex check with all modifiers
    /// </summary>
    public static void Example10_ComplexCheck()
    {
        var character = new Character 
        { 
            Name = "Bard",
            Charisma = 18,  // +4 modifier
            Level = 7,      // +3 proficiency bonus
            PersuasionProficiency = true
        };
        
        // Trying to convince a hostile guard to let you pass
        // You have advantage (used Charm Person spell)
        // You have +1d4 from Guidance spell (rolled a 3)
        var check = character.MakeSkillCheck(
            "Persuasion", 
            DndMath.DifficultyClass.Hard,  // DC 20
            hasAdvantage: true,
            circumstantialBonus: 3  // Guidance spell rolled a 3
        );
        
        Console.WriteLine(check.GetDetailedMessage());
        // Example output: "Persuasion [ADV: 15/7]: 15 + 7 + 3 (situational) = 25 vs DC 20 - SUCCESS"
        // Breakdown:
        // - Rolled d20 twice (advantage): 15 and 7, took 15
        // - Base modifier: +7 (+4 Charisma + 3 proficiency)
        // - Circumstantial: +3 (Guidance)
        // - Total: 25 vs DC 20 = Success!
    }
}
