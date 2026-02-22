using System;
using System.Collections.Generic;

namespace _4DND;

public static class StartingEquipment
{
    public class EquipmentPackage
    {
        public List<string> Items { get; set; } = new();
        public List<string> EquippedItems { get; set; } = new();
        public int GoldPieces { get; set; } = 0;
    }
    
    private static readonly Dictionary<string, string> _startingWealthByClass = new()
    {
        ["Barbarian"] = "2d4*10",
        ["Bard"] = "5d4*10",
        ["Cleric"] = "5d4*10",
        ["Druid"] = "2d4*10",
        ["Fighter"] = "5d4*10",
        ["Monk"] = "5d4",
        ["Paladin"] = "5d4*10",
        ["Ranger"] = "5d4*10",
        ["Rogue"] = "4d4*10",
        ["Sorcerer"] = "3d4*10",
        ["Warlock"] = "4d4*10",
        ["Wizard"] = "4d4*10",
        ["Warrior"] = "5d4*10", // Alias for Fighter
        ["Mage"] = "4d4*10" // Alias for Wizard
    };
    
    public static int RollStartingWealth(string className)
    {
        if (!_startingWealthByClass.TryGetValue(className, out var formula))
        {
            return 0;
        }
        
        // Parse formula like "2d4*10" or "5d4"
        var parts = formula.Split('*');
        var diceNotation = parts[0];
        int multiplier = parts.Length > 1 ? int.Parse(parts[1]) : 1;
        
        var result = Dice.RollNotation(diceNotation);
        return result.Total * multiplier;
    }
    
    public static EquipmentPackage GetStartingEquipment(string className)
    {
        var package = new EquipmentPackage();
        
        switch (className)
        {
            case "Warrior":
            case "Fighter":
                // Chain mail + weapon + shield + adventuring gear
                package.Items.AddRange(new[] 
                { 
                    "Chain Mail", 
                    "Longsword",
                    "Shield",
                    "Crossbow, Light",
                    "Crossbow bolts (20)",
                    "Backpack",
                    "Bedroll",
                    "Mess kit",
                    "Tinderbox",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Rope, hempen (50 feet)"
                });
                package.EquippedItems.AddRange(new[] { "Longsword", "Chain Mail", "Shield" });
                package.GoldPieces = 10;
                break;
                
            case "Barbarian":
                // Greataxe + javelins + explorer's pack
                package.Items.AddRange(new[] 
                { 
                    "Greataxe", 
                    "Handaxe",
                    "Handaxe",
                    "Javelin",
                    "Javelin",
                    "Javelin",
                    "Javelin",
                    "Backpack",
                    "Bedroll",
                    "Mess kit",
                    "Tinderbox",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Rope, hempen (50 feet)"
                });
                package.EquippedItems.AddRange(new[] { "Greataxe" });
                package.GoldPieces = 10;
                break;
                
            case "Mage":
            case "Wizard":
                // Quarterstaff or dagger + spellbook + scholar's pack
                package.Items.AddRange(new[] 
                { 
                    "Quarterstaff",
                    "Dagger",
                    "Component pouch",
                    "Spellbook",
                    "Backpack",
                    "Book",
                    "Ink (1 ounce bottle)",
                    "Ink pen",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Sack"
                });
                package.EquippedItems.AddRange(new[] { "Quarterstaff" });
                package.GoldPieces = 10;
                break;
                
            case "Rogue":
                // Rapier + shortbow + burglar's pack + leather armor + daggers
                package.Items.AddRange(new[] 
                { 
                    "Rapier",
                    "Shortbow",
                    "Quiver",
                    "Arrows (20)",
                    "Leather Armor",
                    "Dagger",
                    "Dagger",
                    "Backpack",
                    "Ball bearings (bag of 1,000)",
                    "Bell",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Crowbar",
                    "Hammer",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Lantern, hooded",
                    "Oil (flask)",
                    "Oil (flask)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Tinderbox",
                    "Waterskin",
                    "Rope, hempen (50 feet)",
                    "Thieves' tools"
                });
                package.EquippedItems.AddRange(new[] { "Rapier", "Leather Armor" });
                package.GoldPieces = 15;
                break;
                
            case "Cleric":
                // Mace + scale mail + shield + holy symbol
                package.Items.AddRange(new[] 
                { 
                    "Mace",
                    "Scale Mail",
                    "Shield",
                    "Crossbow, Light",
                    "Crossbow bolts (20)",
                    "Amulet",
                    "Backpack",
                    "Blanket",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Tinderbox",
                    "Alms box",
                    "Incense",
                    "Incense",
                    "Censer",
                    "Robes",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin"
                });
                package.EquippedItems.AddRange(new[] { "Mace", "Scale Mail", "Shield" });
                package.GoldPieces = 15;
                break;
                
            case "Druid":
                // Wooden shield + simple weapon + leather armor + druidic focus
                package.Items.AddRange(new[] 
                { 
                    "Shield",
                    "Scimitar",
                    "Leather Armor",
                    "Wooden staff",
                    "Backpack",
                    "Bedroll",
                    "Mess kit",
                    "Tinderbox",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Rope, hempen (50 feet)",
                    "Herbalism kit"
                });
                package.EquippedItems.AddRange(new[] { "Scimitar", "Leather Armor", "Shield" });
                package.GoldPieces = 10;
                break;
                
            case "Monk":
                // Shortsword or simple weapon + dungeoneer's pack
                package.Items.AddRange(new[] 
                { 
                    "Shortsword",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Dart",
                    "Backpack",
                    "Crowbar",
                    "Hammer",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Tinderbox",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Rope, hempen (50 feet)"
                });
                package.EquippedItems.AddRange(new[] { "Shortsword" });
                package.GoldPieces = 5;
                break;
                
            case "Paladin":
                // Martial weapon + shield + chain mail + holy symbol
                package.Items.AddRange(new[] 
                { 
                    "Longsword",
                    "Shield",
                    "Chain Mail",
                    "Javelin",
                    "Javelin",
                    "Javelin",
                    "Javelin",
                    "Javelin",
                    "Amulet",
                    "Backpack",
                    "Bedroll",
                    "Mess kit",
                    "Tinderbox",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Rope, hempen (50 feet)"
                });
                package.EquippedItems.AddRange(new[] { "Longsword", "Chain Mail", "Shield" });
                package.GoldPieces = 25;
                break;
                
            case "Ranger":
                // Scale mail + weapons + explorer's pack
                package.Items.AddRange(new[] 
                { 
                    "Scale Mail",
                    "Shortsword",
                    "Shortsword",
                    "Longbow",
                    "Quiver",
                    "Arrows (20)",
                    "Backpack",
                    "Bedroll",
                    "Mess kit",
                    "Tinderbox",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Rope, hempen (50 feet)"
                });
                package.EquippedItems.AddRange(new[] { "Shortsword", "Scale Mail" });
                package.GoldPieces = 10;
                break;
                
            case "Bard":
                // Rapier + lute + leather armor
                package.Items.AddRange(new[] 
                { 
                    "Rapier",
                    "Lute",
                    "Leather Armor",
                    "Dagger",
                    "Backpack",
                    "Bedroll",
                    "Clothes, costume",
                    "Clothes, costume",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Candle",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Disguise kit"
                });
                package.EquippedItems.AddRange(new[] { "Rapier", "Leather Armor" });
                package.GoldPieces = 15;
                break;
                
            case "Sorcerer":
                // Crossbow or simple weapon + component pouch + dungeoneer's pack
                package.Items.AddRange(new[] 
                { 
                    "Crossbow, Light",
                    "Crossbow bolts (20)",
                    "Dagger",
                    "Dagger",
                    "Component pouch",
                    "Arcane Focus - Crystal",
                    "Backpack",
                    "Crowbar",
                    "Hammer",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Piton",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Torch",
                    "Tinderbox",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin",
                    "Rope, hempen (50 feet)"
                });
                package.EquippedItems.AddRange(new[] { "Dagger" });
                package.GoldPieces = 10;
                break;
                
            case "Warlock":
                // Simple weapon + component pouch + scholar's pack + leather armor
                package.Items.AddRange(new[] 
                { 
                    "Crossbow, Light",
                    "Crossbow bolts (20)",
                    "Dagger",
                    "Dagger",
                    "Component pouch",
                    "Arcane Focus - Rod",
                    "Leather Armor",
                    "Backpack",
                    "Book",
                    "Ink (1 ounce bottle)",
                    "Ink pen",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Parchment (one sheet)",
                    "Sack"
                });
                package.EquippedItems.AddRange(new[] { "Dagger", "Leather Armor" });
                package.GoldPieces = 10;
                break;
                
            default:
                // Basic equipment for unknown classes
                package.Items.AddRange(new[] 
                { 
                    "Club",
                    "Backpack",
                    "Bedroll",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Rations (1 day)",
                    "Waterskin"
                });
                package.EquippedItems.AddRange(new[] { "Club" });
                package.GoldPieces = 10;
                break;
        }
        
        return package;
    }
}
