using System.Collections.Generic;
using System.Linq;

namespace _4DND;

public static class ItemDatabase
{
    public static readonly Dictionary<string, Item> Items = new()
    {
        // Simple Melee Weapons
        ["Club"] = new Item
        {
            Name = "Club",
            Description = "A simple wooden club",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d4",
            DamageType = DamageType.Bludgeoning,
            IsLight = true,
            Weight = 2,
            Value = 10,
            IsEquippable = true
        },
        ["Dagger"] = new Item
        {
            Name = "Dagger",
            Description = "A small blade",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d4",
            DamageType = DamageType.Piercing,
            IsLight = true,
            IsFinesse = true,
            IsThrown = true,
            Range = 20,
            LongRange = 60,
            Weight = 1,
            Value = 200,
            IsEquippable = true
        },
        ["Greatclub"] = new Item
        {
            Name = "Greatclub",
            Description = "A large, heavy club",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d8",
            DamageType = DamageType.Bludgeoning,
            IsTwoHanded = true,
            Weight = 10,
            Value = 20,
            IsEquippable = true
        },
        ["Handaxe"] = new Item
        {
            Name = "Handaxe",
            Description = "A small throwing axe",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d6",
            DamageType = DamageType.Slashing,
            IsLight = true,
            IsThrown = true,
            Range = 20,
            LongRange = 60,
            Weight = 2,
            Value = 500,
            IsEquippable = true
        },
        ["Javelin"] = new Item
        {
            Name = "Javelin",
            Description = "A light throwing spear",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d6",
            DamageType = DamageType.Piercing,
            IsThrown = true,
            Range = 30,
            LongRange = 120,
            Weight = 2,
            Value = 50,
            IsEquippable = true
        },
        ["Light Hammer"] = new Item
        {
            Name = "Light Hammer",
            Description = "A small hammer",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d4",
            DamageType = DamageType.Bludgeoning,
            IsLight = true,
            IsThrown = true,
            Range = 20,
            LongRange = 60,
            Weight = 2,
            Value = 200,
            IsEquippable = true
        },
        ["Mace"] = new Item
        {
            Name = "Mace",
            Description = "A metal-headed club",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d6",
            DamageType = DamageType.Bludgeoning,
            Weight = 4,
            Value = 500,
            IsEquippable = true
        },
        ["Quarterstaff"] = new Item
        {
            Name = "Quarterstaff",
            Description = "A versatile wooden staff",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d6",
            VersatileDamageDice = "1d8",
            DamageType = DamageType.Bludgeoning,
            IsVersatile = true,
            Weight = 4,
            Value = 20,
            IsEquippable = true
        },
        ["Sickle"] = new Item
        {
            Name = "Sickle",
            Description = "A curved farming tool",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d4",
            DamageType = DamageType.Slashing,
            IsLight = true,
            Weight = 2,
            Value = 100,
            IsEquippable = true
        },
        ["Spear"] = new Item
        {
            Name = "Spear",
            Description = "A versatile spear",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d6",
            VersatileDamageDice = "1d8",
            DamageType = DamageType.Piercing,
            IsVersatile = true,
            IsThrown = true,
            Range = 20,
            LongRange = 60,
            Weight = 3,
            Value = 100,
            IsEquippable = true
        },
        ["Unarmed Strike"] = new Item
        {
            Name = "Unarmed Strike",
            Description = "A punch or kick",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1",
            DamageType = DamageType.Bludgeoning,
            Weight = 0,
            Value = 0,
            IsEquippable = false
        },
        
        // Simple Ranged Weapons
        ["Crossbow, Light"] = new Item
        {
            Name = "Crossbow, Light",
            Description = "A light crossbow",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d8",
            DamageType = DamageType.Piercing,
            IsTwoHanded = true,
            IsRanged = true,
            IsAmmunition = true,
            IsLoading = true,
            AmmoType = "Ammunition - Crossbow bolts (20)",
            Range = 80,
            LongRange = 320,
            Weight = 5,
            Value = 2500,
            IsEquippable = true
        },
        ["Dart"] = new Item
        {
            Name = "Dart",
            Description = "A small throwing dart",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d4",
            DamageType = DamageType.Piercing,
            IsRanged = true,
            IsThrown = true,
            Range = 20,
            LongRange = 60,
            IsFinesse = true,
            Weight = 0,
            Value = 5,
            IsEquippable = true
        },
        ["Shortbow"] = new Item
        {
            Name = "Shortbow",
            Description = "A simple ranged weapon",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d6",
            DamageType = DamageType.Piercing,
            IsTwoHanded = true,
            IsRanged = true,
            IsAmmunition = true,
            AmmoType = "Ammunition - Arrows (20)",
            Range = 80,
            LongRange = 320,
            Weight = 2,
            Value = 2500,
            IsEquippable = true
        },
        ["Sling"] = new Item
        {
            Name = "Sling",
            Description = "A leather strap for hurling stones",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d4",
            DamageType = DamageType.Bludgeoning,
            IsRanged = true,
            IsAmmunition = true,
            AmmoType = "Ammunition - Sling bullets (20)",
            Range = 30,
            LongRange = 120,
            Weight = 0,
            Value = 10,
            IsEquippable = true
        },
        
        // Martial Melee Weapons
        ["Battleaxe"] = new Item
        {
            Name = "Battleaxe",
            Description = "A versatile axe",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            VersatileDamageDice = "1d10",
            DamageType = DamageType.Slashing,
            IsVersatile = true,
            Weight = 4,
            Value = 1000,
            IsEquippable = true
        },
        ["Flail"] = new Item
        {
            Name = "Flail",
            Description = "A spiked ball on a chain",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            DamageType = DamageType.Bludgeoning,
            Weight = 2,
            Value = 1000,
            IsEquippable = true
        },
        ["Glaive"] = new Item
        {
            Name = "Glaive",
            Description = "A heavy polearm",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d10",
            DamageType = DamageType.Slashing,
            IsHeavy = true,
            IsReach = true,
            IsTwoHanded = true,
            Weight = 6,
            Value = 2000,
            IsEquippable = true
        },
        ["Greataxe"] = new Item
        {
            Name = "Greataxe",
            Description = "A massive two-handed axe",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d12",
            DamageType = DamageType.Slashing,
            IsHeavy = true,
            IsTwoHanded = true,
            Weight = 7,
            Value = 3000,
            IsEquippable = true
        },
        ["Greatsword"] = new Item
        {
            Name = "Greatsword",
            Description = "A massive two-handed sword",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "2d6",
            DamageType = DamageType.Slashing,
            IsHeavy = true,
            IsTwoHanded = true,
            Weight = 6,
            Value = 5000,
            IsEquippable = true
        },
        ["Halberd"] = new Item
        {
            Name = "Halberd",
            Description = "A heavy polearm with an axe blade",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d10",
            DamageType = DamageType.Slashing,
            IsHeavy = true,
            IsReach = true,
            IsTwoHanded = true,
            Weight = 6,
            Value = 2000,
            IsEquippable = true
        },
        ["Lance"] = new Item
        {
            Name = "Lance",
            Description = "A long cavalry weapon",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d12",
            DamageType = DamageType.Piercing,
            IsReach = true,
            IsSpecial = true,
            Weight = 6,
            Value = 1000,
            IsEquippable = true
        },
        ["Longsword"] = new Item
        {
            Name = "Longsword",
            Description = "A versatile blade",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            VersatileDamageDice = "1d10",
            DamageType = DamageType.Slashing,
            IsVersatile = true,
            Weight = 3,
            Value = 1500,
            IsEquippable = true
        },
        ["Maul"] = new Item
        {
            Name = "Maul",
            Description = "A heavy two-handed hammer",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "2d6",
            DamageType = DamageType.Bludgeoning,
            IsHeavy = true,
            IsTwoHanded = true,
            Weight = 10,
            Value = 1000,
            IsEquippable = true
        },
        ["Morningstar"] = new Item
        {
            Name = "Morningstar",
            Description = "A spiked club",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            DamageType = DamageType.Piercing,
            Weight = 4,
            Value = 1500,
            IsEquippable = true
        },
        ["Pike"] = new Item
        {
            Name = "Pike",
            Description = "A very long spear",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d10",
            DamageType = DamageType.Piercing,
            IsHeavy = true,
            IsReach = true,
            IsTwoHanded = true,
            Weight = 18,
            Value = 500,
            IsEquippable = true
        },
        ["Rapier"] = new Item
        {
            Name = "Rapier",
            Description = "A slender, finesse blade",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            DamageType = DamageType.Piercing,
            IsFinesse = true,
            Weight = 2,
            Value = 2500,
            IsEquippable = true
        },
        ["Scimitar"] = new Item
        {
            Name = "Scimitar",
            Description = "A curved, finesse sword",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d6",
            DamageType = DamageType.Slashing,
            IsLight = true,
            IsFinesse = true,
            Weight = 3,
            Value = 2500,
            IsEquippable = true
        },
        ["Shortsword"] = new Item
        {
            Name = "Shortsword",
            Description = "A short, finesse blade",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d6",
            DamageType = DamageType.Piercing,
            IsLight = true,
            IsFinesse = true,
            Weight = 2,
            Value = 1000,
            IsEquippable = true
        },
        ["Trident"] = new Item
        {
            Name = "Trident",
            Description = "A three-pronged spear",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d6",
            VersatileDamageDice = "1d8",
            DamageType = DamageType.Piercing,
            IsVersatile = true,
            IsThrown = true,
            Range = 20,
            LongRange = 60,
            Weight = 4,
            Value = 500,
            IsEquippable = true
        },
        ["War Pick"] = new Item
        {
            Name = "War Pick",
            Description = "A war pick",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            DamageType = DamageType.Piercing,
            Weight = 2,
            Value = 500,
            IsEquippable = true
        },
        ["Warhammer"] = new Item
        {
            Name = "Warhammer",
            Description = "A versatile war hammer",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            VersatileDamageDice = "1d10",
            DamageType = DamageType.Bludgeoning,
            IsVersatile = true,
            Weight = 2,
            Value = 1500,
            IsEquippable = true
        },
        ["Whip"] = new Item
        {
            Name = "Whip",
            Description = "A flexible finesse weapon",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d4",
            DamageType = DamageType.Slashing,
            IsFinesse = true,
            IsReach = true,
            Weight = 3,
            Value = 200,
            IsEquippable = true
        },
        
        // Martial Ranged Weapons
        ["Blowgun"] = new Item
        {
            Name = "Blowgun",
            Description = "A tube for shooting darts",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1",
            DamageType = DamageType.Piercing,
            IsRanged = true,
            IsAmmunition = true,
            IsLoading = true,
            AmmoType = "Ammunition - Blowgun needles (50)",
            Range = 25,
            LongRange = 100,
            Weight = 1,
            Value = 1000,
            IsEquippable = true
        },
        ["Crossbow, Hand"] = new Item
        {
            Name = "Crossbow, Hand",
            Description = "A small hand crossbow",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d6",
            DamageType = DamageType.Piercing,
            IsLight = true,
            IsRanged = true,
            IsAmmunition = true,
            IsLoading = true,
            AmmoType = "Ammunition - Crossbow bolts (20)",
            Range = 30,
            LongRange = 120,
            Weight = 3,
            Value = 7500,
            IsEquippable = true
        },
        ["Crossbow, Heavy"] = new Item
        {
            Name = "Crossbow, Heavy",
            Description = "A heavy crossbow",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d10",
            DamageType = DamageType.Piercing,
            IsHeavy = true,
            IsTwoHanded = true,
            IsRanged = true,
            IsAmmunition = true,
            IsLoading = true,
            AmmoType = "Ammunition - Crossbow bolts (20)",
            Range = 100,
            LongRange = 400,
            Weight = 18,
            Value = 5000,
            IsEquippable = true
        },
        ["Longbow"] = new Item
        {
            Name = "Longbow",
            Description = "A powerful longbow",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "1d8",
            DamageType = DamageType.Piercing,
            IsHeavy = true,
            IsTwoHanded = true,
            IsRanged = true,
            IsAmmunition = true,
            AmmoType = "Ammunition - Arrows (20)",
            Range = 150,
            LongRange = 600,
            Weight = 2,
            Value = 5000,
            IsEquippable = true
        },
        ["Net"] = new Item
        {
            Name = "Net",
            Description = "A Large or smaller creature hit by a net is restrained until freed. Has no effect on Huge or larger creatures. A creature can use its action to make a DC 10 Strength check, freeing itself or another within reach on a success. Dealing 5 slashing damage to the net (AC 10) also frees it. Only one attack per turn can be made with a net.",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Martial,
            DamageDice = "0",
            DamageType = DamageType.None,
            IsThrown = true,
            IsLoading = true,  // Only one attack per turn with a net (PHB "Special Weapons: Net")
            IsSpecial = true,
            Range = 5,
            LongRange = 15,
            Weight = 3,
            Value = 100,
            IsEquippable = true
        },

        // Armor - Light
        ["Padded"] = new Item
        {
            Name = "Padded",
            Description = "Padded armor consists of quilted layers of cloth and batting",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Light,
            ArmorClass = 11,
            StealthDisadvantage = true,
            Weight = 8,
            Value = 500,
            IsEquippable = true
        },
        ["Leather Armor"] = new Item
        {
            Name = "Leather Armor",
            Description = "The breastplate and shoulder protectors of this armor are made of leather that has been stiffened by being boiled in oil. The rest of the armor is made of softer and more flexible materials.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Light,
            ArmorClass = 11,
            Weight = 10,
            Value = 1000,
            IsEquippable = true
        },
        ["Studded Leather"] = new Item
        {
            Name = "Studded Leather",
            Description = "Made from tough but flexible leather, studded leather is reinforced with close-set rivets or spikes.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Light,
            ArmorClass = 12,
            Weight = 13,
            Value = 4500,
            IsEquippable = true
        },
        
        // Armor - Medium
        ["Hide Armor"] = new Item
        {
            Name = "Hide Armor",
            Description = "This crude armor consists of thick furs and pelts. It is commonly worn by barbarian tribes, evil humanoids, and other folk who lack access to the tools and materials needed to create better armor.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Medium,
            ArmorClass = 12,
            MaxDexBonus = 2,
            Weight = 12,
            Value = 1000,
            IsEquippable = true
        },
        ["Chain Shirt"] = new Item
        {
            Name = "Chain Shirt",
            Description = "Made of interlocking metal rings, a chain shirt is worn between layers of clothing or leather. This armor offers modest protection to the wearer's upper body and allows the sound of the rings rubbing against one another to be muffled by outer layers.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Medium,
            ArmorClass = 13,
            MaxDexBonus = 2,
            Weight = 20,
            Value = 5000,
            IsEquippable = true
        },
        ["Scale Mail"] = new Item
        {
            Name = "Scale Mail",
            Description = "This armor consists of a coat and leggings (and perhaps a separate skirt) of leather covered with overlapping pieces of metal, much like the scales of a fish. The suit includes gauntlets.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Medium,
            ArmorClass = 14,
            MaxDexBonus = 2,
            StealthDisadvantage = true,
            Weight = 45,
            Value = 5000,
            IsEquippable = true
        },
        ["Breastplate"] = new Item
        {
            Name = "Breastplate",
            Description = "This armor consists of a fitted metal chest piece worn with supple leather. Although it leaves the legs and arms relatively unprotected, this armor provides good protection for the wearer's vital organs while leaving the wearer relatively unencumbered.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Medium,
            ArmorClass = 14,
            MaxDexBonus = 2,
            Weight = 20,
            Value = 40000,
            IsEquippable = true
        },
        ["Half Plate"] = new Item
        {
            Name = "Half Plate",
            Description = "Half plate consists of shaped metal plates that cover most of the wearer's body. It does not include leg protection beyond simple greaves that are attached with leather straps.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Medium,
            ArmorClass = 15,
            MaxDexBonus = 2,
            StealthDisadvantage = true,
            Weight = 40,
            Value = 75000,
            IsEquippable = true
        },
        
        // Armor - Heavy
        ["Ring Mail"] = new Item
        {
            Name = "Ring Mail",
            Description = "This armor is leather armor with heavy rings sewn into it. The rings help reinforce the armor against blows from swords and axes. Ring mail is inferior to chain mail, and it's usually worn only by those who can't afford better armor.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Heavy,
            ArmorClass = 14,
            MaxDexBonus = 0,
            StealthDisadvantage = true,
            Weight = 40,
            Value = 3000,
            IsEquippable = true
        },
        ["Chain Mail"] = new Item
        {
            Name = "Chain Mail",
            Description = "Made of interlocking metal rings, chain mail includes a layer of quilted fabric worn underneath the mail to prevent chafing and to cushion the impact of blows. The suit includes gauntlets.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Heavy,
            ArmorClass = 16,
            MaxDexBonus = 0,
            StrengthRequirement = 13,
            StealthDisadvantage = true,
            Weight = 55,
            Value = 7500,
            IsEquippable = true
        },
        ["Splint"] = new Item
        {
            Name = "Splint",
            Description = "This armor is made of narrow vertical strips of metal riveted to a backing of leather that is worn over cloth padding. Flexible chain mail protects the joints.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Heavy,
            ArmorClass = 17,
            MaxDexBonus = 0,
            StrengthRequirement = 15,
            StealthDisadvantage = true,
            Weight = 60,
            Value = 20000,
            IsEquippable = true
        },
        ["Plate Armor"] = new Item
        {
            Name = "Plate Armor",
            Description = "Plate consists of shaped, interlocking metal plates to cover the entire body. A suit of plate includes gauntlets, heavy leather boots, a visored helmet, and thick layers of padding underneath the armor. Buckles and straps distribute the weight over the body.",
            Type = ItemType.Armor,
            ArmorCategory = ArmorType.Heavy,
            ArmorClass = 18,
            MaxDexBonus = 0,
            StrengthRequirement = 15,
            StealthDisadvantage = true,
            Weight = 65,
            Value = 150000,
            IsEquippable = true
        },
        
        // Shields
        ["Shield"] = new Item
        {
            Name = "Shield",
            Description = "A wooden or metal shield",
            Type = ItemType.Shield,
            ArmorCategory = ArmorType.Shield,
            ArmorClass = 2,
            Weight = 6,
            Value = 1000,
            IsEquippable = true
        },
        
        // Adventuring Gear
        ["Abacus"] = new Item
        {
            Name = "Abacus",
            Description = "A counting device",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 200,
            IsEquippable = false
        },
        ["Acid (vial)"] = new Item
        {
            Name = "Acid (vial)",
            Description = "As an action, throw this vial at a creature within 20 ft. Make a ranged attack (improvised weapon). On a hit, the target takes 2d6 acid damage.",
            Type = ItemType.Consumable,
            Weight = 1,
            Value = 2500,
            IsEquippable = false
        },
        ["Alchemist's Fire (flask)"] = new Item
        {
            Name = "Alchemist's Fire (flask)",
            Description = "As an action, throw this flask up to 20 feet, shattering it on impact. Make a ranged attack against a creature or object, treating the alchemist's fire as an improvised weapon. On a hit, the target takes 1d4 fire damage at the start of each of its turns. A creature can end this damage by using its action to make a DC 10 Dexterity check to extinguish the flames.",
            Type = ItemType.Consumable,
            Weight = 1,
            Value = 5000,
            IsEquippable = false
        },
        ["Ammunition - Arrows (20)"] = new Item
        {
            Name = "Arrows (20)",
            Description = "Arrows for bows",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 100,
            IsEquippable = false,
            DefaultQuantity = 20
        },
        ["Ammunition - Blowgun needles (50)"] = new Item
        {
            Name = "Blowgun needles (50)",
            Description = "Needles for blowguns",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 100,
            IsEquippable = false,
            DefaultQuantity = 50
        },
        ["Ammunition - Crossbow bolts (20)"] = new Item
        {
            Name = "Crossbow bolts (20)",
            Description = "Bolts for crossbows",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 100,
            IsEquippable = false,
            DefaultQuantity = 20
        },
        ["Ammunition - Sling bullets (20)"] = new Item
        {
            Name = "Sling bullets (20)",
            Description = "Bullets for slings",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 4,
            IsEquippable = false,
            DefaultQuantity = 20
        },
        ["Antitoxin (vial)"] = new Item
        {
            Name = "Antitoxin (vial)",
            Description = "A creature that drinks this vial of liquid gains advantage on saving throws against poison for 1 hour. It confers no benefit to undead or constructs.",
            Type = ItemType.Consumable,
            Weight = 0,
            Value = 5000,
            IsEquippable = false
        },
        ["Arcane Focus - Crystal"] = new Item
        {
            Name = "Crystal",
            Description = "Arcane spellcasting focus",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 1000,
            IsEquippable = false
        },
        ["Arcane Focus - Orb"] = new Item
        {
            Name = "Orb",
            Description = "Arcane spellcasting focus",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 2000,
            IsEquippable = false
        },
        ["Arcane Focus - Rod"] = new Item
        {
            Name = "Rod",
            Description = "Arcane spellcasting focus",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 1000,
            IsEquippable = false
        },
        ["Arcane Focus - Staff"] = new Item
        {
            Name = "Staff",
            Description = "Arcane spellcasting focus",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 500,
            IsEquippable = false
        },
        ["Arcane Focus - Wand"] = new Item
        {
            Name = "Wand",
            Description = "Arcane spellcasting focus",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 1000,
            IsEquippable = false
        },
        ["Backpack"] = new Item
        {
            Name = "Backpack",
            Description = "Can hold 1 cubic foot / 30 pounds of gear",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 200,
            IsEquippable = false
        },
        ["Ball bearings (bag of 1,000)"] = new Item
        {
            Name = "Ball bearings (bag of 1,000)",
            Description = "Spreads ball bearings over a 10-foot square area",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 100,
            IsEquippable = false
        },
        ["Barrel"] = new Item
        {
            Name = "Barrel",
            Description = "Holds 40 gallons of liquid or 4 cubic feet of solid",
            Type = ItemType.Misc,
            Weight = 70,
            Value = 200,
            IsEquippable = false
        },
        ["Basket"] = new Item
        {
            Name = "Basket",
            Description = "Can hold 2 cubic feet / 40 pounds of gear",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 40,
            IsEquippable = false
        },
        ["Bedroll"] = new Item
        {
            Name = "Bedroll",
            Description = "Provides warmth and comfort while sleeping",
            Type = ItemType.Misc,
            Weight = 7,
            Value = 100,
            IsEquippable = false
        },
        ["Bell"] = new Item
        {
            Name = "Bell",
            Description = "A small bell",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 100,
            IsEquippable = false
        },
        ["Blanket"] = new Item
        {
            Name = "Blanket",
            Description = "A warm blanket",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 50,
            IsEquippable = false
        },
        ["Block and tackle"] = new Item
        {
            Name = "Block and tackle",
            Description = "A set of pulleys with a cable for lifting",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 100,
            IsEquippable = false
        },
        ["Book"] = new Item
        {
            Name = "Book",
            Description = "A book with blank or written pages",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 2500,
            IsEquippable = false
        },
        ["Bottle, glass"] = new Item
        {
            Name = "Bottle, glass",
            Description = "Holds 1.5 pints of liquid",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 200,
            IsEquippable = false
        },
        ["Bucket"] = new Item
        {
            Name = "Bucket",
            Description = "Holds 3 gallons of liquid or half a cubic foot of solid",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 5,
            IsEquippable = false
        },
        ["Caltrops (bag of 20)"] = new Item
        {
            Name = "Caltrops (bag of 20)",
            Description = "Covers a 5-foot square area with spikes",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 100,
            IsEquippable = false
        },
        ["Candle"] = new Item
        {
            Name = "Candle",
            Description = "Sheds bright light in a 5-foot radius for 1 hour",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 1,
            IsEquippable = false
        },
        ["Case, crossbow bolt"] = new Item
        {
            Name = "Case, crossbow bolt",
            Description = "Holds up to 20 crossbow bolts",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 100,
            IsEquippable = false
        },
        ["Case, map or scroll"] = new Item
        {
            Name = "Case, map or scroll",
            Description = "Holds up to 10 sheets of paper or 5 sheets of parchment",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 100,
            IsEquippable = false
        },
        ["Chain (10 feet)"] = new Item
        {
            Name = "Chain (10 feet)",
            Description = "Has 10 hit points. Can be broken with a DC 20 Strength check",
            Type = ItemType.Misc,
            Weight = 10,
            Value = 500,
            IsEquippable = false
        },
        ["Chalk (1 piece)"] = new Item
        {
            Name = "Chalk (1 piece)",
            Description = "For drawing or writing",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 1,
            IsEquippable = false
        },
        ["Chest"] = new Item
        {
            Name = "Chest",
            Description = "Can hold 12 cubic feet / 300 pounds of gear",
            Type = ItemType.Misc,
            Weight = 25,
            Value = 500,
            IsEquippable = false
        },
        ["Climber's kit"] = new Item
        {
            Name = "Climber's kit",
            Description = "Includes pitons, boot tips, gloves, and harness",
            Type = ItemType.Misc,
            Weight = 12,
            Value = 2500,
            IsEquippable = false
        },
        ["Clothes, common"] = new Item
        {
            Name = "Clothes, common",
            Description = "Common clothing",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 50,
            IsEquippable = false
        },
        ["Clothes, costume"] = new Item
        {
            Name = "Clothes, costume",
            Description = "Costume clothing",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 500,
            IsEquippable = false
        },
        ["Clothes, fine"] = new Item
        {
            Name = "Clothes, fine",
            Description = "Fine clothing",
            Type = ItemType.Misc,
            Weight = 6,
            Value = 1500,
            IsEquippable = false
        },
        ["Clothes, traveler's"] = new Item
        {
            Name = "Clothes, traveler's",
            Description = "Traveling clothing",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 200,
            IsEquippable = false
        },
        ["Component pouch"] = new Item
        {
            Name = "Component pouch",
            Description = "Contains material components for spellcasting",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 2500,
            IsEquippable = false
        },
        ["Crowbar"] = new Item
        {
            Name = "Crowbar",
            Description = "Grants advantage on Strength checks for prying",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 200,
            IsEquippable = false
        },
        ["Druidic Focus - Sprig of mistletoe"] = new Item
        {
            Name = "Sprig of mistletoe",
            Description = "Druidic spellcasting focus",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 100,
            IsEquippable = false
        },
        ["Druidic Focus - Totem"] = new Item
        {
            Name = "Totem",
            Description = "Druidic spellcasting focus",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 100,
            IsEquippable = false
        },
        ["Druidic Focus - Wooden staff"] = new Item
        {
            Name = "Wooden staff",
            Description = "Druidic spellcasting focus",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 500,
            IsEquippable = false
        },
        ["Druidic Focus - Yew wand"] = new Item
        {
            Name = "Yew wand",
            Description = "Druidic spellcasting focus",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 1000,
            IsEquippable = false
        },
        ["Fishing tackle"] = new Item
        {
            Name = "Fishing tackle",
            Description = "Includes a wooden rod, silken line, corkwood bobbers, steel hooks, lead sinkers, and velvet lure",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 100,
            IsEquippable = false
        },
        ["Flask or tankard"] = new Item
        {
            Name = "Flask or tankard",
            Description = "Holds 1 pint of liquid",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 2,
            IsEquippable = false
        },
        ["Grappling hook"] = new Item
        {
            Name = "Grappling hook",
            Description = "Hooks onto ledges or edges",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 200,
            IsEquippable = false
        },
        ["Hammer"] = new Item
        {
            Name = "Hammer",
            Description = "A basic tool hammer",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 100,
            IsEquippable = false
        },
        ["Hammer, sledge"] = new Item
        {
            Name = "Hammer, sledge",
            Description = "A large heavy hammer",
            Type = ItemType.Misc,
            Weight = 10,
            Value = 200,
            IsEquippable = false
        },
        ["Healer's kit"] = new Item
        {
            Name = "Healer's kit",
            Description = "Has 10 uses. Can be used to stabilize dying creature",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 500,
            IsEquippable = false
        },
        ["Holy symbol - Amulet"] = new Item
        {
            Name = "Amulet",
            Description = "Holy symbol for divine spellcasting",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 500,
            IsEquippable = false
        },
        ["Holy symbol - Emblem"] = new Item
        {
            Name = "Emblem",
            Description = "Holy symbol for divine spellcasting",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 500,
            IsEquippable = false
        },
        ["Holy symbol - Reliquary"] = new Item
        {
            Name = "Reliquary",
            Description = "Holy symbol for divine spellcasting",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 500,
            IsEquippable = false
        },
        ["Holy water (flask)"] = new Item
        {
            Name = "Holy water (flask)",
            Description = "Deals 2d6 radiant damage to fiends and undead",
            Type = ItemType.Consumable,
            Weight = 1,
            Value = 2500,
            IsEquippable = false
        },
        ["Hourglass"] = new Item
        {
            Name = "Hourglass",
            Description = "Measures time in one-hour increments",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 2500,
            IsEquippable = false
        },
        ["Hunting trap"] = new Item
        {
            Name = "Hunting trap",
            Description = "Can be set to restrain a creature",
            Type = ItemType.Misc,
            Weight = 25,
            Value = 500,
            IsEquippable = false
        },
        ["Ink (1 ounce bottle)"] = new Item
        {
            Name = "Ink (1 ounce bottle)",
            Description = "Ink for writing",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 1000,
            IsEquippable = false
        },
        ["Ink pen"] = new Item
        {
            Name = "Ink pen",
            Description = "A writing instrument",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 2,
            IsEquippable = false
        },
        ["Jug or pitcher"] = new Item
        {
            Name = "Jug or pitcher",
            Description = "Holds 1 gallon of liquid",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 2,
            IsEquippable = false
        },
        ["Ladder (10-foot)"] = new Item
        {
            Name = "Ladder (10-foot)",
            Description = "A wooden ladder",
            Type = ItemType.Misc,
            Weight = 25,
            Value = 10,
            IsEquippable = false
        },
        ["Lamp"] = new Item
        {
            Name = "Lamp",
            Description = "Sheds bright light in a 15-foot radius for 6 hours on 1 pint of oil",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 50,
            IsEquippable = false
        },
        ["Lantern, bullseye"] = new Item
        {
            Name = "Lantern, bullseye",
            Description = "Sheds bright light in a 60-foot cone for 6 hours on 1 pint of oil",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 1000,
            IsEquippable = false
        },
        ["Lantern, hooded"] = new Item
        {
            Name = "Lantern, hooded",
            Description = "Sheds bright light in a 30-foot radius for 6 hours on 1 pint of oil",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 500,
            IsEquippable = false
        },
        ["Lock"] = new Item
        {
            Name = "Lock",
            Description = "Comes with a key. Can be picked with thieves' tools",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 1000,
            IsEquippable = false
        },
        ["Magnifying glass"] = new Item
        {
            Name = "Magnifying glass",
            Description = "Grants advantage on ability checks to appraise or inspect small items",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 10000,
            IsEquippable = false
        },
        ["Manacles"] = new Item
        {
            Name = "Manacles",
            Description = "Binds a Small or Medium creature. DC 20 to escape",
            Type = ItemType.Misc,
            Weight = 6,
            Value = 200,
            IsEquippable = false
        },
        ["Mess kit"] = new Item
        {
            Name = "Mess kit",
            Description = "Includes a tin box containing a cup and simple cutlery",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 20,
            IsEquippable = false
        },
        ["Mirror, steel"] = new Item
        {
            Name = "Mirror, steel",
            Description = "A polished steel mirror",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 500,
            IsEquippable = false
        },
        ["Oil (flask)"] = new Item
        {
            Name = "Oil (flask)",
            Description = "Can fuel lanterns or be thrown as improvised weapon",
            Type = ItemType.Consumable,
            Weight = 1,
            Value = 10,
            IsEquippable = false
        },
        ["Paper (one sheet)"] = new Item
        {
            Name = "Paper (one sheet)",
            Description = "One sheet of paper",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 20,
            IsEquippable = false
        },
        ["Parchment (one sheet)"] = new Item
        {
            Name = "Parchment (one sheet)",
            Description = "One sheet of parchment",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 10,
            IsEquippable = false
        },
        ["Perfume (vial)"] = new Item
        {
            Name = "Perfume (vial)",
            Description = "A vial of perfume",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 500,
            IsEquippable = false
        },
        ["Pick, miner's"] = new Item
        {
            Name = "Pick, miner's",
            Description = "Used for mining",
            Type = ItemType.Misc,
            Weight = 10,
            Value = 200,
            IsEquippable = false
        },
        ["Piton"] = new Item
        {
            Name = "Piton",
            Description = "A spike hammered into rock for climbing",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 5,
            IsEquippable = false
        },
        ["Poison, basic (vial)"] = new Item
        {
            Name = "Poison, basic (vial)",
            Description = "Can be applied to weapons. DC 10 Con save or take 1d4 poison damage",
            Type = ItemType.Consumable,
            Weight = 0,
            Value = 10000,
            IsEquippable = false
        },
        ["Pole (10-foot)"] = new Item
        {
            Name = "Pole (10-foot)",
            Description = "A long wooden pole",
            Type = ItemType.Misc,
            Weight = 7,
            Value = 5,
            IsEquippable = false
        },
        ["Pot, iron"] = new Item
        {
            Name = "Pot, iron",
            Description = "An iron cooking pot",
            Type = ItemType.Misc,
            Weight = 10,
            Value = 200,
            IsEquippable = false
        },
        ["Potion of healing"] = new Item
        {
            Name = "Potion of healing",
            Description = "Restores 2d4+2 hit points",
            Type = ItemType.Consumable,
            Weight = 0,
            Value = 5000,
            IsEquippable = false
        },
        ["Pouch"] = new Item
        {
            Name = "Pouch",
            Description = "Holds 1/5 cubic foot / 6 pounds of gear",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 50,
            IsEquippable = false
        },
        ["Quiver"] = new Item
        {
            Name = "Quiver",
            Description = "Holds up to 20 arrows",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 100,
            IsEquippable = false
        },
        ["Ram, portable"] = new Item
        {
            Name = "Ram, portable",
            Description = "Grants +4 bonus on Strength checks to break down doors",
            Type = ItemType.Misc,
            Weight = 35,
            Value = 400,
            IsEquippable = false
        },
        ["Rations (1 day)"] = new Item
        {
            Name = "Rations (1 day)",
            Description = "Dry foods for long journeys",
            Type = ItemType.Consumable,
            Weight = 2,
            Value = 50,
            IsEquippable = false
        },
        ["Robes"] = new Item
        {
            Name = "Robes",
            Description = "Simple robes",
            Type = ItemType.Misc,
            Weight = 4,
            Value = 100,
            IsEquippable = false
        },
        ["Rope, hempen (50 feet)"] = new Item
        {
            Name = "Rope, hempen (50 feet)",
            Description = "Has 2 hit points. Can be burst with DC 17 Strength check",
            Type = ItemType.Misc,
            Weight = 10,
            Value = 100,
            IsEquippable = false
        },
        ["Rope, silk (50 feet)"] = new Item
        {
            Name = "Rope, silk (50 feet)",
            Description = "Has 2 hit points. Can be burst with DC 17 Strength check",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 1000,
            IsEquippable = false
        },
        ["Sack"] = new Item
        {
            Name = "Sack",
            Description = "Holds 1 cubic foot / 30 pounds of gear",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 1,
            IsEquippable = false
        },
        ["Scale, merchant's"] = new Item
        {
            Name = "Scale, merchant's",
            Description = "Includes a small balance and pans",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 500,
            IsEquippable = false
        },
        ["Sealing wax"] = new Item
        {
            Name = "Sealing wax",
            Description = "Used to seal letters and documents",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 50,
            IsEquippable = false
        },
        ["Shovel"] = new Item
        {
            Name = "Shovel",
            Description = "Used for digging",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 200,
            IsEquippable = false
        },
        ["Signal whistle"] = new Item
        {
            Name = "Signal whistle",
            Description = "Can be heard up to 300 feet away",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 5,
            IsEquippable = false
        },
        ["Signet ring"] = new Item
        {
            Name = "Signet ring",
            Description = "Used to impress a design in sealing wax",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 500,
            IsEquippable = false
        },
        ["Soap"] = new Item
        {
            Name = "Soap",
            Description = "For bathing and cleaning",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 2,
            IsEquippable = false
        },
        ["Spellbook"] = new Item
        {
            Name = "Spellbook",
            Description = "Essential for wizards. Has 100 blank pages",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 5000,
            IsEquippable = false
        },
        ["Spikes, iron (10)"] = new Item
        {
            Name = "Spikes, iron (10)",
            Description = "Iron spikes",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 100,
            IsEquippable = false
        },
        ["Spyglass"] = new Item
        {
            Name = "Spyglass",
            Description = "Objects viewed are magnified to twice their size",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 100000,
            IsEquippable = false
        },
        ["Tent, two-person"] = new Item
        {
            Name = "Tent, two-person",
            Description = "Simple shelter for two",
            Type = ItemType.Misc,
            Weight = 20,
            Value = 200,
            IsEquippable = false
        },
        ["Tinderbox"] = new Item
        {
            Name = "Tinderbox",
            Description = "Used to start fires. Takes an action",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 50,
            IsEquippable = false
        },
        ["Torch"] = new Item
        {
            Name = "Torch",
            Description = "Sheds bright light in a 20-foot radius for 1 hour; can be wielded as an improvised light weapon",
            Type = ItemType.Weapon,
            WeaponCategory = WeaponType.Simple,
            DamageDice = "1d4",
            DamageType = DamageType.Bludgeoning,
            IsLight = true,
            Weight = 1,
            Value = 1,
            IsEquippable = true
        },
        ["Vial"] = new Item
        {
            Name = "Vial",
            Description = "Holds 4 ounces of liquid",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 100,
            IsEquippable = false
        },
        ["Waterskin"] = new Item
        {
            Name = "Waterskin",
            Description = "Holds 4 pints of liquid",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 20,
            IsEquippable = false
        },
        ["Whetstone"] = new Item
        {
            Name = "Whetstone",
            Description = "Used to sharpen blades",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 1,
            IsEquippable = false
        },
        
        // ARTISAN'S TOOLS
        ["Alchemist's supplies"] = new Item
        {
            Name = "Alchemist's supplies",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 8,
            Value = 5000,
            IsEquippable = false
        },
        ["Brewer's supplies"] = new Item
        {
            Name = "Brewer's supplies",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 9,
            Value = 2000,
            IsEquippable = false
        },
        ["Calligrapher's supplies"] = new Item
        {
            Name = "Calligrapher's supplies",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 1000,
            IsEquippable = false
        },
        ["Carpenter's tools"] = new Item
        {
            Name = "Carpenter's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 6,
            Value = 800,
            IsEquippable = false
        },
        ["Cartographer's tools"] = new Item
        {
            Name = "Cartographer's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 6,
            Value = 1500,
            IsEquippable = false
        },
        ["Cobbler's tools"] = new Item
        {
            Name = "Cobbler's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 500,
            IsEquippable = false
        },
        ["Cook's utensils"] = new Item
        {
            Name = "Cook's utensils",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 8,
            Value = 100,
            IsEquippable = false
        },
        ["Glassblower's tools"] = new Item
        {
            Name = "Glassblower's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 3000,
            IsEquippable = false
        },
        ["Jeweler's tools"] = new Item
        {
            Name = "Jeweler's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 2500,
            IsEquippable = false
        },
        ["Leatherworker's tools"] = new Item
        {
            Name = "Leatherworker's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 500,
            IsEquippable = false
        },
        ["Mason's tools"] = new Item
        {
            Name = "Mason's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 8,
            Value = 1000,
            IsEquippable = false
        },
        ["Painter's supplies"] = new Item
        {
            Name = "Painter's supplies",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 1000,
            IsEquippable = false
        },
        ["Potter's tools"] = new Item
        {
            Name = "Potter's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 1000,
            IsEquippable = false
        },
        ["Smith's tools"] = new Item
        {
            Name = "Smith's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 8,
            Value = 2000,
            IsEquippable = false
        },
        ["Tinker's tools"] = new Item
        {
            Name = "Tinker's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 10,
            Value = 5000,
            IsEquippable = false
        },
        ["Weaver's tools"] = new Item
        {
            Name = "Weaver's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 100,
            IsEquippable = false
        },
        ["Woodcarver's tools"] = new Item
        {
            Name = "Woodcarver's tools",
            Description = "These special tools include items needed to pursue a craft or trade. Proficiency lets you add your proficiency bonus to any ability checks you make when using these tools",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 100,
            IsEquippable = false
        },
        
        // SPECIAL TOOLS
        ["Disguise kit"] = new Item
        {
            Name = "Disguise kit",
            Description = "This pouch of cosmetics, hair dye, and small props lets you create disguises. Proficiency with this kit lets you add your proficiency bonus to ability checks you make to create a visual disguise",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 2500,
            IsEquippable = false
        },
        ["Forgery kit"] = new Item
        {
            Name = "Forgery kit",
            Description = "This small box contains a variety of papers and parchments, pens and inks, seals and sealing wax, gold and silver leaf, and other supplies. Proficiency with this kit lets you add your proficiency bonus to any ability checks you make to create a physical forgery of a document",
            Type = ItemType.Misc,
            Weight = 5,
            Value = 1500,
            IsEquippable = false
        },
        ["Herbalism kit"] = new Item
        {
            Name = "Herbalism kit",
            Description = "This kit contains pouches to store herbs, clippers, mortar and pestle, and pouches. Proficiency with this kit lets you add your proficiency bonus to any ability checks you make to identify or apply herbs",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 500,
            IsEquippable = false
        },
        ["Navigator's tools"] = new Item
        {
            Name = "Navigator's tools",
            Description = "This set of instruments is used for navigation at sea. Proficiency with navigator's tools lets you chart a ship's course and follow navigation charts. In addition, these tools allow you to add your proficiency bonus to any ability checks you make to avoid getting lost at sea",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 2500,
            IsEquippable = false
        },
        ["Poisoner's kit"] = new Item
        {
            Name = "Poisoner's kit",
            Description = "A poisoner's kit includes the vials, chemicals, and other equipment necessary for the creation of poisons. Proficiency with this kit lets you add your proficiency bonus to any ability checks you make to craft or use poisons",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 5000,
            IsEquippable = false
        },
        ["Thieves' tools"] = new Item
        {
            Name = "Thieves' tools",
            Description = "This set of tools includes a small file, a set of lock picks, a small mirror mounted on a metal handle, a set of narrow-bladed scissors, and a pair of pliers. Proficiency with these tools lets you add your proficiency bonus to any ability checks you make to disarm traps or open locks",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 2500,
            IsEquippable = false
        },
        
        // GAMING SETS
        ["Dice set"] = new Item
        {
            Name = "Dice set",
            Description = "A set of dice for games of chance",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 10,
            IsEquippable = false
        },
        ["Dragonchess set"] = new Item
        {
            Name = "Dragonchess set",
            Description = "A strategic board game",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 100,
            IsEquippable = false
        },
        ["Playing card set"] = new Item
        {
            Name = "Playing card set",
            Description = "A deck of cards",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 50,
            IsEquippable = false
        },
        ["Three-Dragon Ante set"] = new Item
        {
            Name = "Three-Dragon Ante set",
            Description = "A popular card game",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 100,
            IsEquippable = false
        },
        
        // MUSICAL INSTRUMENTS
        ["Bagpipes"] = new Item
        {
            Name = "Bagpipes",
            Description = "A wind musical instrument. Several of the most common types of musical instruments are shown on the table. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 6,
            Value = 3000,
            IsEquippable = false
        },
        ["Drum"] = new Item
        {
            Name = "Drum",
            Description = "A percussion musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 3,
            Value = 600,
            IsEquippable = false
        },
        ["Dulcimer"] = new Item
        {
            Name = "Dulcimer",
            Description = "A stringed musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 10,
            Value = 2500,
            IsEquippable = false
        },
        ["Flute"] = new Item
        {
            Name = "Flute",
            Description = "A wind musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 200,
            IsEquippable = false
        },
        ["Lute"] = new Item
        {
            Name = "Lute",
            Description = "A stringed musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 3500,
            IsEquippable = false
        },
        ["Lyre"] = new Item
        {
            Name = "Lyre",
            Description = "A stringed musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 3000,
            IsEquippable = false
        },
        ["Horn"] = new Item
        {
            Name = "Horn",
            Description = "A wind musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 300,
            IsEquippable = false
        },
        ["Pan flute"] = new Item
        {
            Name = "Pan flute",
            Description = "A wind musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 2,
            Value = 1200,
            IsEquippable = false
        },
        ["Shawm"] = new Item
        {
            Name = "Shawm",
            Description = "A wind musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 200,
            IsEquippable = false
        },
        ["Viol"] = new Item
        {
            Name = "Viol",
            Description = "A stringed musical instrument. If you have proficiency with a given musical instrument, you can add your proficiency bonus to any ability checks you make to play music with the instrument",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 3000,
            IsEquippable = false
        },
        
        // RELIGIOUS ITEMS
        ["Alms box"] = new Item
        {
            Name = "Alms box",
            Description = "A small box for collecting charitable donations",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 0,
            IsEquippable = false
        },
        ["Incense"] = new Item
        {
            Name = "Incense",
            Description = "Aromatic substance burned during religious ceremonies",
            Type = ItemType.Misc,
            Weight = 0,
            Value = 0,
            IsEquippable = false
        },
        ["Censer"] = new Item
        {
            Name = "Censer",
            Description = "A vessel for burning incense",
            Type = ItemType.Misc,
            Weight = 1,
            Value = 0,
            IsEquippable = false
        }
    };
    
    public static Item GetItem(string name)
    {
        return Items.TryGetValue(name, out var item) ? item : new Item { Name = name };
    }
    
    public static List<string> GetWeapons()
    {
        return Items.Where(i => i.Value.Type == ItemType.Weapon).Select(i => i.Key).ToList();
    }
    
    public static List<string> GetArmor()
    {
        return Items.Where(i => i.Value.Type == ItemType.Armor).Select(i => i.Key).ToList();
    }
    
    public static List<string> GetAdventuringGear()
    {
        return Items.Where(i => i.Value.Type == ItemType.Misc).Select(i => i.Key).ToList();
    }
    
    public static List<string> GetConsumables()
    {
        return Items.Where(i => i.Value.Type == ItemType.Consumable).Select(i => i.Key).ToList();
    }
}
