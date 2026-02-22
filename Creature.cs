using System;
using Microsoft.Xna.Framework;

namespace _4DND;

public enum CreatureType
{
    Player,
    Goblin,
    Orc,
    Skeleton,
    Wolf,
    Kobold
}

public class Creature
{
    public string Name { get; set; } = "";
    public CreatureType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    
    // Stats
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int ArmorClass { get; set; }
    public int Speed { get; set; } = 30;
    
    // Ability Scores
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
    
    // Combat
    public int Initiative { get; set; }
    public bool IsPlayer { get; set; }
    public Color DisplayColor { get; set; } = Color.Red;
    
    // Attack info
    public string AttackName { get; set; } = "Attack";
    public int AttackBonus { get; set; } = 2;
    public string DamageDice { get; set; } = "1d6";
    public int DamageBonus { get; set; } = 0;
    
    // Vision properties
    public int DarkvisionRange { get; set; } = 0;  // In feet
    public bool HasSuperiorDarkvision { get; set; } = false;
    public bool HasBlindSight { get; set; } = false;
    public int BlindSightRange { get; set; } = 0;
    public bool HasTrueSight { get; set; } = false;
    public int TrueSightRange { get; set; } = 0;
    public bool HasSunlightSensitivity { get; set; } = false;
    
    // Conditions
    public Condition Conditions { get; set; } = Condition.None;
    
    public int GetAbilityModifier(int score) => (score - 10) / 2;
    
    public bool IsAlive() => CurrentHP > 0;
    
    public bool IsBlinded()
    {
        return Conditions.HasCondition(Condition.Blinded) || Conditions.HasCondition(Condition.Unconscious);
    }
    
    public void TakeDamage(int amount)
    {
        CurrentHP = Math.Max(0, CurrentHP - amount);
    }
    
    public void Heal(int amount)
    {
        CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
    }
    
    public static Creature CreateGoblin(int x, int y)
    {
        return new Creature
        {
            Name = "Goblin",
            Type = CreatureType.Goblin,
            X = x,
            Y = y,
            MaxHP = 7,
            CurrentHP = 7,
            ArmorClass = 15,
            Speed = 30,
            Strength = 8,
            Dexterity = 14,
            Constitution = 10,
            Intelligence = 10,
            Wisdom = 8,
            Charisma = 8,
            AttackName = "Scimitar",
            AttackBonus = 4,
            DamageDice = "1d6",
            DamageBonus = 2,
            DarkvisionRange = 60,
            DisplayColor = Color.Green,
            IsPlayer = false
        };
    }
    
    public static Creature CreateOrc(int x, int y)
    {
        return new Creature
        {
            Name = "Orc",
            Type = CreatureType.Orc,
            X = x,
            Y = y,
            MaxHP = 15,
            CurrentHP = 15,
            ArmorClass = 13,
            Speed = 30,
            Strength = 16,
            Dexterity = 12,
            Constitution = 16,
            Intelligence = 7,
            Wisdom = 11,
            Charisma = 10,
            AttackName = "Greataxe",
            AttackBonus = 5,
            DamageDice = "1d12",
            DamageBonus = 3,
            DarkvisionRange = 60,
            DisplayColor = Color.DarkRed,
            IsPlayer = false
        };
    }
    
    public static Creature CreateSkeleton(int x, int y)
    {
        return new Creature
        {
            Name = "Skeleton",
            Type = CreatureType.Skeleton,
            X = x,
            Y = y,
            MaxHP = 13,
            CurrentHP = 13,
            ArmorClass = 13,
            Speed = 30,
            Strength = 10,
            Dexterity = 14,
            Constitution = 15,
            Intelligence = 6,
            Wisdom = 8,
            Charisma = 5,
            AttackName = "Shortsword",
            AttackBonus = 4,
            DamageDice = "1d6",
            DamageBonus = 2,
            DarkvisionRange = 60,
            DisplayColor = Color.White,
            IsPlayer = false
        };
    }
    
    public static Creature CreateWolf(int x, int y)
    {
        return new Creature
        {
            Name = "Wolf",
            Type = CreatureType.Wolf,
            X = x,
            Y = y,
            MaxHP = 11,
            CurrentHP = 11,
            ArmorClass = 13,
            Speed = 40,
            Strength = 12,
            Dexterity = 15,
            Constitution = 12,
            Intelligence = 3,
            Wisdom = 12,
            Charisma = 6,
            AttackName = "Bite",
            AttackBonus = 4,
            DamageDice = "2d4",
            DamageBonus = 2,
            DarkvisionRange = 0,
            HasBlindSight = true,
            BlindSightRange = 30,
            DisplayColor = Color.Gray,
            IsPlayer = false
        };
    }
    
    public static Creature CreateKobold(int x, int y)
    {
        return new Creature
        {
            Name = "Kobold",
            Type = CreatureType.Kobold,
            X = x,
            Y = y,
            MaxHP = 5,
            CurrentHP = 5,
            ArmorClass = 12,
            Speed = 30,
            Strength = 7,
            Dexterity = 15,
            Constitution = 9,
            Intelligence = 8,
            Wisdom = 7,
            Charisma = 8,
            AttackName = "Dagger",
            AttackBonus = 4,
            DamageDice = "1d4",
            DamageBonus = 2,
            DarkvisionRange = 60,
            HasSunlightSensitivity = true,
            DisplayColor = Color.Brown,
            IsPlayer = false
        };
    }
    
    public static Creature FromCharacter(Character character, int x, int y)
    {
        var creature = new Creature
        {
            Name = character.Name,
            Type = CreatureType.Player,
            X = x,
            Y = y,
            MaxHP = character.MaxHP,
            CurrentHP = character.CurrentHP,
            ArmorClass = character.ArmorClass,
            Speed = character.Speed,
            Strength = character.Strength,
            Dexterity = character.Dexterity,
            Constitution = character.Constitution,
            Intelligence = character.Intelligence,
            Wisdom = character.Wisdom,
            Charisma = character.Charisma,
            DarkvisionRange = character.DarkvisionRange,
            DisplayColor = Color.Blue,
            IsPlayer = true
        };
        
        // Apply race-specific vision traits
        var raceData = _4DND.Race.GetRace(character.Race);
        creature.HasSuperiorDarkvision = raceData.HasSuperiorDarkvision;
        creature.HasSunlightSensitivity = raceData.HasSunlightSensitivity;
        
        // Set attack based on equipped weapon
        if (character.InventoryData.EquippedWeapon != null)
        {
            var weapon = ItemDatabase.GetItem(character.InventoryData.EquippedWeapon);
            creature.AttackName = weapon.Name;
            
            int abilityMod = weapon.IsFinesse 
                ? Math.Max(creature.GetAbilityModifier(creature.Strength), creature.GetAbilityModifier(creature.Dexterity))
                : creature.GetAbilityModifier(creature.Strength);
            
            creature.AttackBonus = abilityMod + character.ProficiencyBonus;
            creature.DamageDice = weapon.DamageDice;
            creature.DamageBonus = abilityMod;
        }
        else
        {
            // Unarmed strike
            creature.AttackName = "Unarmed Strike";
            creature.AttackBonus = creature.GetAbilityModifier(creature.Strength) + character.ProficiencyBonus;
            creature.DamageDice = "1";
            creature.DamageBonus = creature.GetAbilityModifier(creature.Strength);
        }
        
        return creature;
    }
    
    public void UpdateCharacter(Character character)
    {
        character.CurrentHP = CurrentHP;
    }
}
