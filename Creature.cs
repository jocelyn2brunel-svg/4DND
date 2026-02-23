using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace _4DND;

public enum CreatureType
{
    Player,
    Goblin,
    Orc,
    Skeleton,
    Wolf,
    Kobold,
    Umber_Hulk,  // Tremorsense
    Couatl       // Truesight
}

public enum CreatureSize
{
    Tiny,       // 2½ by 2½ ft. (e.g., imp, sprite)
    Small,      // 5 by 5 ft. (e.g., giant rat, goblin)
    Medium,     // 5 by 5 ft. (e.g., orc, werewolf)
    Large,      // 10 by 10 ft. (e.g., hippogriff, ogre)
    Huge,       // 15 by 15 ft. (e.g., fire giant, treant)
    Gargantuan  // 20 by 20 ft. or larger (e.g., kraken, purple worm)
}

public enum Alignment
{
    LawfulGood,
    NeutralGood,
    ChaoticGood,
    LawfulNeutral,
    TrueNeutral,
    ChaoticNeutral,
    LawfulEvil,
    NeutralEvil,
    ChaoticEvil,
    Unaligned
}

public static class AlignmentHelper
{
    public static string GetDescription(Alignment alignment)
    {
        return alignment switch
        {
            Alignment.LawfulGood => "Lawful Good",
            Alignment.NeutralGood => "Neutral Good",
            Alignment.ChaoticGood => "Chaotic Good",
            Alignment.LawfulNeutral => "Lawful Neutral",
            Alignment.TrueNeutral => "True Neutral",
            Alignment.ChaoticNeutral => "Chaotic Neutral",
            Alignment.LawfulEvil => "Lawful Evil",
            Alignment.NeutralEvil => "Neutral Evil",
            Alignment.ChaoticEvil => "Chaotic Evil",
            Alignment.Unaligned => "Unaligned",
            _ => "Unknown"
        };
    }
    
    public static string GetBehaviorNote(Alignment alignment)
    {
        return alignment switch
        {
            Alignment.LawfulGood => "Acts with honor and compassion, follows rules and helps others",
            Alignment.NeutralGood => "Does good without bias toward law or chaos",
            Alignment.ChaoticGood => "Acts according to conscience with little regard for rules",
            Alignment.LawfulNeutral => "Acts in accordance with law, tradition, or personal codes",
            Alignment.TrueNeutral => "Prefers to steer clear of moral questions and take balanced stance",
            Alignment.ChaoticNeutral => "Follows whims, values personal freedom above all",
            Alignment.LawfulEvil => "Methodically takes what they want within bounds of tradition or order",
            Alignment.NeutralEvil => "Does whatever they can get away with, without compassion or qualms",
            Alignment.ChaoticEvil => "Acts with arbitrary violence, spurred by greed, hatred, or bloodlust",
            Alignment.Unaligned => "Lacks capacity for moral or ethical choices",
            _ => ""
        };
    }
    
    public static Alignment ParseAlignment(string alignmentString)
    {
        return alignmentString?.ToLowerInvariant() switch
        {
            "lawful good" => Alignment.LawfulGood,
            "neutral good" => Alignment.NeutralGood,
            "chaotic good" => Alignment.ChaoticGood,
            "lawful neutral" => Alignment.LawfulNeutral,
            "true neutral" or "neutral" => Alignment.TrueNeutral,
            "chaotic neutral" => Alignment.ChaoticNeutral,
            "lawful evil" => Alignment.LawfulEvil,
            "neutral evil" => Alignment.NeutralEvil,
            "chaotic evil" => Alignment.ChaoticEvil,
            "unaligned" => Alignment.Unaligned,
            _ => Alignment.TrueNeutral
        };
    }
}

public static class SizeHelper
{
    public static (float Width, float Height) GetSpaceInFeet(CreatureSize size)
    {
        return size switch
        {
            CreatureSize.Tiny => (2.5f, 2.5f),        // 2½ by 2½ ft.
            CreatureSize.Small => (5f, 5f),           // 5 by 5 ft.
            CreatureSize.Medium => (5f, 5f),          // 5 by 5 ft.
            CreatureSize.Large => (10f, 10f),         // 10 by 10 ft.
            CreatureSize.Huge => (15f, 15f),          // 15 by 15 ft.
            CreatureSize.Gargantuan => (20f, 20f),    // 20 by 20 ft. or larger
            _ => (5f, 5f)
        };
    }
    
    /// <summary>
    /// Returns the number of 5-foot squares a creature occupies on each axis
    /// </summary>
    public static (int Width, int Height) GetSpaceInSquares(CreatureSize size)
    {
        return size switch
        {
            CreatureSize.Tiny => (1, 1),           // Occupies less than 1 square, but treated as 1 for simplicity
            CreatureSize.Small => (1, 1),          // 1x1 square
            CreatureSize.Medium => (1, 1),         // 1x1 square
            CreatureSize.Large => (2, 2),          // 2x2 squares
            CreatureSize.Huge => (3, 3),           // 3x3 squares
            CreatureSize.Gargantuan => (4, 4),     // 4x4 squares (or more)
            _ => (1, 1)
        };
    }
    
    public static string GetSpaceDescription(CreatureSize size)
    {
        var (width, height) = GetSpaceInFeet(size);
        if (size == CreatureSize.Tiny)
            return $"{width:0.#} by {height:0.#} ft.";
        return $"{(int)width} by {(int)height} ft.";
    }
    
    public static string GetExamples(CreatureSize size)
    {
        return size switch
        {
            CreatureSize.Tiny => "Imp, sprite",
            CreatureSize.Small => "Giant rat, goblin",
            CreatureSize.Medium => "Orc, werewolf",
            CreatureSize.Large => "Hippogriff, ogre",
            CreatureSize.Huge => "Fire giant, treant",
            CreatureSize.Gargantuan => "Kraken, purple worm",
            _ => ""
        };
    }
    
    /// <summary>
    /// Returns the maximum number of Medium creatures that can surround this creature
    /// </summary>
    public static int GetMaxSurroundingCreatures(CreatureSize size)
    {
        return size switch
        {
            CreatureSize.Tiny => 8,         // Same as Medium (Tiny can share space with other creatures)
            CreatureSize.Small => 8,        // 8 squares around a 1x1
            CreatureSize.Medium => 8,       // 8 squares around a 1x1
            CreatureSize.Large => 12,       // 12 squares around a 2x2
            CreatureSize.Huge => 16,        // 16 squares around a 3x3
            CreatureSize.Gargantuan => 20,  // 20 squares around a 4x4
            _ => 8
        };
    }
    
    /// <summary>
    /// Returns the visual center offset for a creature of the given size.
    /// Medium (1x1) has 0 offset, Large (2x2) has 0.5 offset, etc.
    /// </summary>
    public static Vector2 GetCenterOffset(CreatureSize size)
    {
        var (width, height) = GetSpaceInSquares(size);
        return new Vector2((width - 1) / 2.0f, (height - 1) / 2.0f);
    }

    /// <summary>
    /// Returns the size one step smaller than the given size, or null if already Tiny.
    /// Used for squeezing: a creature can squeeze into a space one size smaller.
    /// </summary>
    public static CreatureSize? GetSmallerSize(CreatureSize size)
    {
        return size switch
        {
            CreatureSize.Small => CreatureSize.Tiny,
            CreatureSize.Medium => CreatureSize.Small,
            CreatureSize.Large => CreatureSize.Medium,
            CreatureSize.Huge => CreatureSize.Large,
            CreatureSize.Gargantuan => CreatureSize.Huge,
            _ => null
        };
    }

    /// <summary>
    /// Returns true if a creature of <paramref name="creatureSize"/> can squeeze through
    /// a space sized for <paramref name="spaceSize"/>.
    /// A creature can squeeze through a space large enough for a creature one size smaller.
    /// </summary>
    public static bool CanSqueezeInto(CreatureSize creatureSize, CreatureSize spaceSize)
    {
        var smallerSize = GetSmallerSize(creatureSize);
        return smallerSize.HasValue && smallerSize.Value == spaceSize;
    }
}

public class Creature
{
    private readonly Queue<Vector3> _movementWaypoints = new();

    public string Name { get; set; } = "";
    public CreatureType Type { get; set; }
    public CreatureSize Size { get; set; } = CreatureSize.Medium;
    public Alignment Alignment { get; set; } = Alignment.TrueNeutral;
    
    // Grid position (target position)
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    
    // Visual position for smooth movement animation
    public float VisualX { get; set; }
    public float VisualY { get; set; }
    public float VisualZ { get; set; }
    
    // Movement animation speed (units per second)
    public float MovementSpeed { get; set; } = 8.0f;
    
    // Flight capabilities
    public bool CanFly { get; set; } = false;
    public int FlySpeed { get; set; } = 0;
    public bool IsFlying { get; set; } = false;
    
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
    
    // Action economy (reset each turn)
    public bool HasAction { get; set; } = true;
    public bool HasBonusAction { get; set; } = true;
    public bool HasReaction { get; set; } = true;
    public int MovementRemaining { get; set; } = 30;
    
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
    public bool HasTremorsense { get; set; } = false;
    public int TremorsenseRange { get; set; } = 0;
    public bool HasTrueSight { get; set; } = false;
    public int TrueSightRange { get; set; } = 0;
    public bool HasSunlightSensitivity { get; set; } = false;
    
    // Conditions
    public Condition Conditions { get; set; } = Condition.None;
    
    // Saving throw proficiencies (for monsters)
    public bool StrengthSaveProficiency { get; set; } = false;
    public bool DexteritySaveProficiency { get; set; } = false;
    public bool ConstitutionSaveProficiency { get; set; } = false;
    public bool IntelligenceSaveProficiency { get; set; } = false;
    public bool WisdomSaveProficiency { get; set; } = false;
    public bool CharismaSaveProficiency { get; set; } = false;

    // Skill proficiencies relevant to surprise
    public bool StealthProficiency { get; set; } = false;
    public bool PerceptionProficiency { get; set; } = false;

    /// <summary>
    /// Passive Wisdom (Perception): 10 + Wisdom modifier + proficiency bonus if proficient.
    /// Used to determine whether a creature notices a hidden threat at the start of combat.
    /// </summary>
    public int PassivePerception
    {
        get
        {
            int wisdomMod = GetAbilityModifier(Wisdom);
            int profBonus = IsPlayer ? DndMath.GetProficiencyBonus(1) : 2;
            return 10 + wisdomMod + (PerceptionProficiency ? profBonus : 0);
        }
    }

    /// <summary>
    /// Whether this creature is surprised at the start of combat.
    /// A surprised creature cannot move, take actions, or take reactions on its first turn.
    /// The surprised condition ends at the end of its first turn.
    /// </summary>
    public bool IsSurprised { get; set; } = false;

    /// <summary>
    /// Whether this creature is currently squeezing through a smaller space.
    /// While squeezing: movement costs 1 extra foot per foot moved (double cost),
    /// disadvantage on attack rolls and Dexterity saving throws,
    /// and attack rolls against this creature have advantage.
    /// </summary>
    public bool IsSqueezingThrough { get; set; } = false;

    public int GetAbilityModifier(int score) => DndMath.GetAbilityModifier(score);
    
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
    
    /// <summary>
    /// Updates the visual position to smoothly move towards the target grid position
    /// </summary>
    public void UpdateMovementAnimation(float deltaTime)
    {
        Vector3 target = _movementWaypoints.Count > 0
            ? _movementWaypoints.Peek()
            : new Vector3(X, Y, Z);

        float dx = target.X - VisualX;
        float dy = target.Y - VisualY;
        float dz = target.Z - VisualZ;
        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        
        if (distance > 0.01f)
        {
            float moveAmount = MovementSpeed * deltaTime;
            if (moveAmount >= distance)
            {
                // Snap to target if we're close enough
                VisualX = target.X;
                VisualY = target.Y;
                VisualZ = target.Z;

                if (_movementWaypoints.Count > 0)
                {
                    _movementWaypoints.Dequeue();
                }
            }
            else
            {
                // Move towards target
                float t = moveAmount / distance;
                VisualX += dx * t;
                VisualY += dy * t;
                VisualZ += dz * t;
            }
        }
    }
    
    /// <summary>
    /// Checks if the creature is currently moving (visual position != target position)
    /// </summary>
    public bool IsMoving()
    {
        if (_movementWaypoints.Count > 0)
            return true;

        float dx = X - VisualX;
        float dy = Y - VisualY;
        float dz = Z - VisualZ;
        return (dx * dx + dy * dy + dz * dz) > 0.01f;
    }

    /// <summary>
    /// Returns the remaining waypoints in the movement queue.
    /// </summary>
    public List<Vector3> GetRemainingWaypoints()
    {
        return _movementWaypoints.ToList();
    }
    
    /// <summary>
    /// Teleports the creature to a new position immediately (no animation)
    /// </summary>
    public void TeleportTo(int x, int y, int z)
    {
        _movementWaypoints.Clear();
        X = x;
        Y = y;
        Z = z;
        VisualX = x;
        VisualY = y;
        VisualZ = z;
    }
    
    /// <summary>
    /// Moves the creature to a new position with animation
    /// </summary>
    public void MoveTo(int x, int y, int z)
    {
        var waypoint = new Vector3(x, y, z);
        if (_movementWaypoints.Count == 0 || _movementWaypoints.Peek() != waypoint)
        {
            _movementWaypoints.Enqueue(waypoint);
        }

        X = x;
        Y = y;
        Z = z;
        // VisualX/Y/Z will be updated by UpdateMovementAnimation
    }
    
    public static Creature CreateGoblin(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Goblin",
            Type = CreatureType.Goblin,
            Size = CreatureSize.Small,
            Alignment = Alignment.NeutralEvil,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
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
            IsPlayer = false,
        };
    }
    
    public static Creature CreateOrc(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Orc",
            Type = CreatureType.Orc,
            Size = CreatureSize.Medium,
            Alignment = Alignment.ChaoticEvil,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
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
            IsPlayer = false,
        };
    }
    
    public static Creature CreateSkeleton(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Skeleton",
            Type = CreatureType.Skeleton,
            Size = CreatureSize.Medium,
            Alignment = Alignment.LawfulEvil,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
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
            IsPlayer = false,
        };
    }
    
    public static Creature CreateWolf(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Wolf",
            Type = CreatureType.Wolf,
            Size = CreatureSize.Medium,
            Alignment = Alignment.Unaligned,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
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
    
    public static Creature CreateKobold(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Kobold",
            Type = CreatureType.Kobold,
            Size = CreatureSize.Small,
            Alignment = Alignment.LawfulEvil,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
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
    
    public static Creature CreateUmberHulk(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Umber Hulk",
            Type = CreatureType.Umber_Hulk,
            Size = CreatureSize.Large,
            Alignment = Alignment.ChaoticEvil,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
            MaxHP = 100,
            CurrentHP = 100,
            ArmorClass = 15,
            Speed = 30,
            Strength = 20,
            Dexterity = 10,
            Constitution = 15,
            Intelligence = 2,
            Wisdom = 10,
            Charisma = 1,
            AttackName = "Bite",
            AttackBonus = 8,
            DamageDice = "2d10",
            DamageBonus = 6,
            DarkvisionRange = 60,
            HasTremorsense = true,
            TremorsenseRange = 60,
            DisplayColor = Color.DarkGray,
            IsPlayer = false
        };
    }
    
    public static Creature CreateCouatl(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Couatl",
            Type = CreatureType.Couatl,
            Size = CreatureSize.Large,
            Alignment = Alignment.LawfulGood,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
            MaxHP = 97,
            CurrentHP = 97,
            ArmorClass = 15,
            Speed = 30,
            FlySpeed = 90,
            CanFly = true,
            Strength = 15,
            Dexterity = 15,
            Constitution = 15,
            Intelligence = 16,
            Wisdom = 15,
            Charisma = 16,
            AttackName = "Bite",
            AttackBonus = 6,
            DamageDice = "1d8",
            DamageBonus = 3,
            DarkvisionRange = 60,
            HasTrueSight = true,
            TrueSightRange = 120,
            DisplayColor = Color.LightGoldenrodYellow,
            IsPlayer = false,
            CharismaSaveProficiency = true,
            WisdomSaveProficiency = true
        };
    }
    
    public static Creature FromCharacter(Character character, int x, int y, int z = 0)
    {
        var raceData = _4DND.Race.GetRace(character.Race);
        
        var creature = new Creature
        {
            Name = character.Name,
            Type = CreatureType.Player,
            Size = raceData.Size,
            Alignment = AlignmentHelper.ParseAlignment(character.Alignment),
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
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
            IsPlayer = true,
            StrengthSaveProficiency = character.StrengthSaveProficiency,
            DexteritySaveProficiency = character.DexteritySaveProficiency,
            ConstitutionSaveProficiency = character.ConstitutionSaveProficiency,
            IntelligenceSaveProficiency = character.IntelligenceSaveProficiency,
            WisdomSaveProficiency = character.WisdomSaveProficiency,
            CharismaSaveProficiency = character.CharismaSaveProficiency,
            StealthProficiency = character.StealthProficiency,
            PerceptionProficiency = character.PerceptionProficiency
        };
        
        // Apply race-specific vision traits
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
