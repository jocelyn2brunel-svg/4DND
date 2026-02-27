using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace _4DND;

/// <summary>
/// The degree of cover a creature benefits from (PHB "Cover").
/// Only the most protective degree applies; bonuses are not added together.
/// </summary>
public enum CoverType
{
    None,
    Half,           // +2 bonus to AC and Dexterity saving throws
    ThreeQuarters,  // +5 bonus to AC and Dexterity saving throws
    Total           // Cannot be targeted directly by an attack or a spell
}

public enum CreatureType
{
    Player,
    Goblin,
    Orc,
    Skeleton,
    Zombie,
    Wolf,
    Kobold,
    Umber_Hulk,  // Tremorsense
    Couatl,      // Truesight
    // Arctic monsters
    BloodHawk,        // CR 1/8
    IceMephit,        // CR 1/2
    BrownBear,        // CR 1
    WinterWolf,       // CR 3
    Mammoth,          // CR 6
    YoungWhiteDragon, // CR 6
    FrostGiant,       // CR 8
    AbominableYeti    // CR 9
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
    /// <summary>
    /// Whether this creature can hover in place (e.g. levitate, certain creatures).
    /// Hovering creatures do not fall when knocked prone or deprived of movement.
    /// </summary>
    public bool CanHover { get; set; } = false;

    // Climbing speed (0 = no special climbing speed; movement costs double in climbing terrain)
    public int ClimbSpeed { get; set; } = 0;

    // Swimming speed (0 = no special swimming speed; movement costs double in water)
    public int SwimSpeed { get; set; } = 0;

    // Stats
    public int Level { get; set; } = 1;
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    /// <summary>
    /// Temporary hit points that act as a buffer against damage (PHB "Temporary Hit Points").
    /// Damage is applied to TempHP first; excess carries over to CurrentHP.
    /// Temporary HP cannot be restored by healing and do not stack — keep the higher value.
    /// </summary>
    public int TempHP { get; set; } = 0;
    public int ArmorClass { get; set; }
    public int Speed { get; set; } = 30;
    public int XPReward { get; set; } = 0;

    /// <summary>
    /// The degree of cover this creature currently benefits from (PHB "Cover").
    /// Half cover grants +2 to AC and Dex saves; three-quarters grants +5;
    /// total cover prevents direct targeting by attacks and spells.
    /// </summary>
    public CoverType Cover { get; set; } = CoverType.None;
    
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
    public int DiagonalStepsTaken { get; set; } = 0;
    /// <summary>
    /// Free object interaction available this turn (PHB "Other Activity on Your Turn").
    /// Each creature may interact with one object for free per turn.
    /// A second interaction requires spending the action.
    /// </summary>
    public bool HasFreeObjectInteraction { get; set; } = true;

    // Attack info
    public string AttackName { get; set; } = "Attack";
    public int AttackBonus { get; set; } = 2;
    public string DamageDice { get; set; } = "1d6";
    public int DamageBonus { get; set; } = 0;
    public DamageType CurrentDamageType { get; set; } = DamageType.Bludgeoning;
    public bool IsMeleeAttack { get; set; } = true;
    public bool IsOffhandAttack { get; set; } = false;

    /// <summary>
    /// Whether the current attack uses Strength as its ability modifier.
    /// False for ranged attacks (DEX) and finesse weapons where DEX was chosen.
    /// Barbarian Rage bonus damage only applies to melee attacks using Strength (PHB "Rage").
    /// </summary>
    public bool IsStrengthBasedAttack { get; set; } = true;

    /// <summary>
    /// Normal range for ranged attacks, in feet. 0 means no ranged attack.
    /// Attack rolls against targets beyond normal range (but within long range) have disadvantage.
    /// </summary>
    public int NormalRange { get; set; } = 0;

    /// <summary>
    /// Maximum (long) range for ranged attacks, in feet.
    /// 0 means equal to NormalRange (single-range attack, e.g. a spell).
    /// Attacks beyond this range are impossible.
    /// </summary>
    public int LongRange { get; set; } = 0;
    
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
    public bool HasPackTactics { get; set; } = false;
    public bool HasKeenSenses { get; set; } = false;
    
    // Conditions
    public Condition Conditions { get; set; } = Condition.None;

    /// <summary>Damage types this creature isimmune to (takes 0 damage).</summary>
    public HashSet<DamageType> DamageImmunities { get; set; } = new();

    /// <summary>Damage types this creature is resistant to (takes half damage).</summary>
    public HashSet<DamageType> DamageResistances { get; set; } = new();

    /// <summary>Damage types this creature is vulnerable to (takes double damage).</summary>
    public HashSet<DamageType> DamageVulnerabilities { get; set; } = new();

    /// <summary>
    /// Damage types this creature is immune to from nonmagical, non-silvered attacks (PHB "Silvered Weapons").
    /// Silvered weapons (and magical weapons) bypass this immunity.
    /// Typical for lycanthropes, certain fiends, and undead.
    /// </summary>
    public HashSet<DamageType> NonmagicalImmunities { get; set; } = new();

    /// <summary>
    /// Damage types this creature is resistant to from nonmagical, non-silvered attacks (PHB "Silvered Weapons").
    /// Silvered weapons (and magical weapons) bypass this resistance.
    /// </summary>
    public HashSet<DamageType> NonmagicalResistances { get; set; } = new();

    /// <summary>Conditions this creature cannot be affected by.</summary>
    public Condition ConditionImmunities { get; set; } = Condition.None;
    
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
            int profBonus = DndMath.GetProficiencyBonus(Level);
            int keenBonus = HasKeenSenses ? 5 : 0; // PHB p.175: advantage on perception = +5 to passive
            return 10 + wisdomMod + (PerceptionProficiency ? profBonus : 0) + keenBonus;
        }
    }

    /// <summary>
    /// Whether this creature is surprised at the start of combat.
    /// A surprised creature cannot move, take actions, or take reactions on its first turn.
    /// The surprised condition ends at the end of its first turn.
    /// </summary>
    public bool IsSurprised { get; set; } = false;

    /// <summary>
    /// Number of death saving throw successes (0–3).
    /// Resets when the creature regains hit points or becomes stable.
    /// </summary>
    public int DeathSaveSuccesses { get; set; } = 0;

    /// <summary>
    /// Number of death saving throw failures (0–3). Three failures means the creature dies.
    /// Resets when the creature regains hit points.
    /// </summary>
    public int DeathSaveFailures { get; set; } = 0;

    /// <summary>
    /// Whether this creature has stabilized (three death save successes without healing).
    /// A stable creature remains unconscious at 0 HP but no longer makes death saving throws.
    /// </summary>
    public bool IsStable { get; set; } = false;

    /// <summary>
    /// Whether this creature is truly dead.
    /// For non-player creatures, set immediately at 0 HP.
    /// For players, set after three death save failures or instant death.
    /// </summary>
    public bool IsDead { get; set; } = false;

    /// <summary>Whether the creature has taken the Disengage action this turn (PHB p.192).</summary>
    public bool IsDisengaged { get; set; } = false;

    /// <summary>Whether the creature has taken the Dodge action this turn (PHB p.192).</summary>
    public bool IsDodging { get; set; } = false;

    /// <summary>Whether the creature is currently hidden from others (PHB p.177).</summary>
    public bool IsHidden { get; set; } = false;

    /// <summary>The result of the Dexterity (Stealth) check made to hide.</summary>
    public int HiddenStealthResult { get; set; } = 0;

    /// <summary>Whether the creature is being helped by an ally, granting advantage on its next attack.</summary>
    public bool IsBeingHelped { get; set; } = false;

    /// <summary>
    /// Whether this creature is wearing armor or a shield it is not proficient with.
    /// Causes disadvantage on STR/DEX ability checks, saving throws, and attack rolls; cannot cast spells.
    /// </summary>
    public bool HasArmorNonProficiencyPenalty { get; set; } = false;

    /// <summary>
    /// Whether this creature is attacking with a weapon it is not proficient with.
    /// When true, the proficiency bonus is not added to the attack roll (PHB "Weapon Proficiency").
    /// </summary>
    public bool HasWeaponNonProficiencyPenalty { get; set; } = false;

    /// <summary>
    /// Whether the current attack uses a weapon with the Reach property (PHB "Reach").
    /// Adds 5 feet (1 square) to the creature's melee reach for attacks and opportunity attacks.
    /// </summary>
    public bool IsReachWeapon { get; set; } = false;

    /// <summary>
    /// Whether the current attack uses a Heavy weapon (PHB "Heavy").
    /// Small and Tiny creatures have disadvantage on attack rolls with heavy weapons.
    /// </summary>
    public bool IsHeavyWeaponAttack { get; set; } = false;

    /// <summary>
    /// Whether the current attack uses a weapon with the Loading property (PHB "Loading").
    /// Only one piece of ammunition can be fired per action, bonus action, or reaction,
    /// regardless of the number of attacks you can normally make.
    /// </summary>
    public bool IsLoadingWeapon { get; set; } = false;

    /// <summary>
    /// Whether the current attack uses a weapon with the Special property (PHB "Special Weapons").
    /// For the Lance specifically: attack rolls have disadvantage when the attacker is within
    /// 5 feet of a hostile creature other than the target.
    /// </summary>
    public bool IsLanceAttack { get; set; } = false;

    /// <summary>
    /// Whether the current attack uses a Net (PHB "Special Weapons: Net").
    /// On a hit, a Large or smaller target is restrained until freed; no damage is dealt.
    /// Has no effect on Huge or larger creatures. Only one net attack can be made per turn.
    /// A restrained creature can use its action to make a DC 10 Strength check to free itself,
    /// or another creature within reach can do so. The net can also be destroyed by dealing
    /// 5 slashing damage to it (AC 10).
    /// </summary>
    public bool IsNetAttack { get; set; } = false;

    /// <summary>
    /// Whether the current attack is an improvised weapon attack (PHB "Improvised Weapons").
    /// Improvised attacks deal 1d4 damage and do not add the proficiency bonus to the roll.
    /// </summary>
    public bool IsImprovisedWeaponAttack { get; set; } = false;

    /// <summary>
    /// Whether the current attack uses a silvered weapon (PHB "Silvered Weapons").
    /// Silvered weapons bypass immunity or resistance to nonmagical attacks
    /// for susceptible creatures (e.g. werewolves, certain fiends and undead).
    /// </summary>
    public bool IsSilveredAttack { get; set; } = false;

    /// <summary>
    /// Whether this creature is currently squeezing through a smaller space.
    /// While squeezing: movement costs 1 extra foot per foot moved (double cost),
    /// disadvantage on attack rolls and Dexterity saving throws,
    /// and attack rolls against this creature have advantage.
    /// </summary>
    public bool IsSqueezingThrough { get; set; } = false;

    /// <summary>
    /// Whether this creature has the Nimble Escape trait.
    /// Allows the creature to take the Disengage or Hide action as a bonus action on each of its turns.
    /// </summary>
    public bool HasNimbleEscape { get; set; } = false;

    /// <summary>
    /// Whether this creature has the Undead Fortitude trait (MM "Zombie").
    /// If damage reduces this creature to 0 HP, it makes a Constitution saving throw (DC = 5 + damage taken).
    /// On a success, the creature drops to 1 HP instead.
    /// Does not apply to radiant damage or critical hits.
    /// </summary>
    public bool HasUndeadFortitude { get; set; } = false;

    /// <summary>
    /// Whether this creature is currently in a Barbarian Rage.
    /// Grants damage bonus to melee attacks and resistance to bludgeoning, piercing, and slashing damage.
    /// </summary>
    public bool IsRaging { get; set; } = false;

    /// <summary>
    /// Number of Rage uses remaining until a long rest.
    /// </summary>
    public int RagesRemaining { get; set; } = 0;

    /// <summary>
    /// Bonus damage added to melee weapon attacks while raging.
    /// Determined by Barbarian level.
    /// </summary>
    public int RageDamageBonus { get; set; } = 0;

    /// <summary>
    /// Number of turns remaining in the current rage (max 10 turns = 1 minute).
    /// </summary>
    public int RageTurnsLeft { get; set; } = 0;

    /// <summary>
    /// Whether this creature attacked a hostile creature since its last turn.
    /// Used to determine whether the rage continues.
    /// </summary>
    public bool HasAttackedThisRound { get; set; } = false;

    /// <summary>
    /// Whether this creature took damage since its last turn.
    /// Used to determine whether the rage continues.
    /// </summary>
    public bool HasTakenDamageThisRound { get; set; } = false;

    /// <summary>
    /// Whether this creature has already fired a Loading weapon this turn.
    /// Resets at the start of each new turn. Prevents firing more than once per turn
    /// with crossbows, blowguns, and other Loading weapons (PHB "Loading").
    /// </summary>
    public bool HasFiredLoadingWeaponThisTurn { get; set; } = false;

    // Bard-specific
    /// <summary>Die type for Bardic Inspiration (d6/d8/d10/d12).</summary>
    public int BardicInspirationDice { get; set; } = 6;
    /// <summary>Bardic Inspiration uses remaining until a long rest.</summary>
    public int BardicInspirationUsesRemaining { get; set; } = 0;
    /// <summary>Maximum Bardic Inspiration uses (= Charisma modifier, minimum 1).</summary>
    public int BardicInspirationMax { get; set; } = 0;
    /// <summary>Remaining spell slots per level (index 0 = 1st-level slots … index 8 = 9th-level slots).</summary>
    public int[] SpellSlotsRemaining { get; set; } = new int[9];
    /// <summary>Maximum spell slots per level (indices 0–8 for spell levels 1–9).</summary>
    public int[] SpellSlotsMax { get; set; } = new int[9];

    // Armor donning/doffing
    public DonDoffProcess? CurrentDonDoffProcess { get; set; }

    // Cleric-specific
    /// <summary>Channel Divinity uses remaining (recharges on short or long rest).</summary>
    public int ChannelDivinityUsesRemaining { get; set; } = 0;
    /// <summary>Maximum Channel Divinity uses at current level.</summary>
    public int ChannelDivinityUsesMax { get; set; } = 0;

    // Survival / Nourishment
    /// <summary>Current exhaustion level (0 = none, 1–5 = cumulative penalties, 6 = death).</summary>
    public int ExhaustionLevel { get; set; } = 0;
    /// <summary>Accumulated days (or fractional days) without adequate food. Resets to 0 after a full-ration day.</summary>
    public float DaysWithoutFood { get; set; } = 0f;
    /// <summary>Pounds of food consumed today (needs 1 lb/day for a full ration).</summary>
    public float FoodConsumedToday { get; set; } = 0f;
    /// <summary>Gallons of water consumed today (needs 1 gal/day; 2 in hot weather).</summary>
    public float WaterConsumedToday { get; set; } = 0f;

    public int GetAbilityModifier(int score) => DndMath.GetAbilityModifier(score);

    public bool IsAlive() => !IsDead;
    
    public bool IsBlinded()
    {
        return Conditions.HasCondition(Condition.Blinded) || Conditions.HasCondition(Condition.Unconscious);
    }
    
    public void TakeDamage(int amount, DamageType damageType = DamageType.None, bool isCriticalHit = false, bool isSilvered = false)
    {
        // Immunity: no damage
        if (damageType != DamageType.None && DamageImmunities.Contains(damageType))
            return;

        // Nonmagical immunity (PHB "Silvered Weapons"): bypassed by silvered (or magical) weapons.
        if (damageType != DamageType.None && NonmagicalImmunities.Contains(damageType) && !isSilvered)
            return;

        // Resistance: half damage (PHB "Damage Resistance").
        // Multiple sources of resistance still count as only one instance.
        bool hasResistance = damageType != DamageType.None && DamageResistances.Contains(damageType);
        bool hasNonmagicalResistance = damageType != DamageType.None && NonmagicalResistances.Contains(damageType) && !isSilvered;
        bool hasRageResistance = IsRaging && (damageType == DamageType.Bludgeoning || damageType == DamageType.Piercing || damageType == DamageType.Slashing);
        if (hasResistance || hasNonmagicalResistance || hasRageResistance)
            amount /= 2;

        // Vulnerability: double damage (PHB "Damage Vulnerability").
        if (damageType != DamageType.None && DamageVulnerabilities.Contains(damageType))
            amount *= 2;

        // Temporary Hit Points absorb damage first (PHB "Temporary Hit Points").
        // Damage depletes TempHP first; any leftover carries over to normal hit points.
        if (TempHP > 0)
        {
            int absorbed = Math.Min(TempHP, amount);
            TempHP -= absorbed;
            amount -= absorbed;
            if (amount == 0)
            {
                HasTakenDamageThisRound = true;
                return;
            }
        }

        // Damage at 0 Hit Points (PHB "Damage at 0 Hit Points"):
        // taking any damage while at 0 HP causes a death saving throw failure.
        if (CurrentHP == 0 && IsPlayer && !IsDead)
        {
            // Damage equals or exceeds HP maximum: instant death
            if (amount >= MaxHP)
                IsDead = true;
            else
                AddDeathSaveFailures(isCriticalHit ? 2 : 1);

            HasTakenDamageThisRound = true;
            return;
        }

        if (amount >= CurrentHP)
        {
            int remaining = amount - CurrentHP;
            CurrentHP = 0;

            // Undead Fortitude: If damage reduces the creature to 0 HP, make a Constitution saving throw
            // (DC = 5 + damage taken). On a success, the creature drops to 1 HP instead.
            // Does not apply to radiant damage or critical hits (MM "Undead Fortitude").
            if (HasUndeadFortitude && !isCriticalHit && damageType != DamageType.Radiant)
            {
                int dc = 5 + amount;
                var save = this.MakeSavingThrow("CON", dc);
                if (save.Success)
                {
                    CurrentHP = 1;
                    HasTakenDamageThisRound = true;
                    return;
                }
            }

            // Instant Death: remaining damage equals or exceeds the creature's hit point maximum (PHB "Instant Death")
            if (remaining >= MaxHP)
            {
                IsDead = true;
            }
            else if (!IsPlayer)
            {
                // Non-player creatures die immediately at 0 HP (no death saves)
                IsDead = true;
            }
            else
            {
                // Falling Unconscious: player drops to 0 HP and begins making death saves
                Conditions = Conditions.AddCondition(Condition.Unconscious)
                                       .AddCondition(Condition.Prone);
            }
        }
        else
        {
            CurrentHP -= amount;
        }

        HasTakenDamageThisRound = true;
    }
    
    /// <summary>
    /// Grants temporary hit points. Temporary HP do not stack: if the new amount is greater
    /// than the current TempHP, it replaces it; otherwise the existing value is kept (PHB "Temporary Hit Points").
    /// </summary>
    /// <param name="amount">The number of temporary hit points to grant.</param>
    public void GainTempHP(int amount)
    {
        if (amount > TempHP)
            TempHP = amount;
    }

    public void Heal(int amount)
    {
        // A dead creature can't regain hit points until magic has restored it to life (PHB "Healing").
        if (IsDead)
            return;

        bool wasUnconscious = CurrentHP == 0;
        CurrentHP = Math.Min(MaxHP, CurrentHP + amount);

        // Unconsciousness ends when the creature regains any hit points (PHB "Falling Unconscious")
        if (wasUnconscious && CurrentHP > 0)
        {
            Conditions = Conditions.RemoveCondition(Condition.Unconscious);
            DeathSaveSuccesses = 0;
            DeathSaveFailures = 0;
            IsStable = false;
        }
    }

    /// <summary>
    /// Restores a dead creature to life with a given number of hit points (PHB "Healing").
    /// Used by spells such as <em>revivify</em> or <em>raise dead</em>.
    /// Has no effect if the creature is not dead.
    /// </summary>
    /// <param name="hp">Hit points to restore (clamped to 1–MaxHP).</param>
    public void Revive(int hp)
    {
        if (!IsDead)
            return;

        IsDead = false;
        CurrentHP = Math.Clamp(hp, 1, MaxHP);
        DeathSaveSuccesses = 0;
        DeathSaveFailures = 0;
        IsStable = false;
        Conditions = Conditions.RemoveCondition(Condition.Unconscious);
    }

    /// <summary>
    /// Applies falling damage: 1d6 bludgeoning per 10 feet fallen, maximum 20d6.
    /// The creature lands prone unless it takes no damage.
    /// </summary>
    /// <param name="feetFallen">The distance fallen in feet.</param>
    /// <returns>The total damage dealt.</returns>
    public int TakeFallDamage(int feetFallen)
    {
        int diceCount = Math.Clamp(feetFallen / 10, 0, 20);
        if (diceCount == 0)
            return 0;

        int damage = 0;
        for (int i = 0; i < diceCount; i++)
            damage += Dice.Roll(6);

        int hpBefore = CurrentHP;
        TakeDamage(damage, DamageType.Bludgeoning);

        if (CurrentHP < hpBefore)
            Conditions = Conditions.AddCondition(Condition.Prone);

        return damage;
    }

    private void AddDeathSaveFailures(int count)
    {
        DeathSaveFailures = Math.Min(3, DeathSaveFailures + count);
        if (DeathSaveFailures >= 3)
            IsDead = true;
    }

    /// <summary>
    /// Makes a death saving throw at the start of the creature's turn (PHB "Death Saving Throws").
    /// Roll a d20: 10 or higher is a success, below 10 is a failure.
    /// Rolling a 1 counts as two failures. Rolling a 20 causes the creature to regain 1 HP.
    /// Three successes stabilize the creature; three failures kill it.
    /// </summary>
    /// <param name="rng">Random number generator for the d20 roll.</param>
    /// <returns>
    /// A tuple with the <c>Roll</c>, whether it was a <c>IsSuccess</c>, a natural <c>IsNatural20</c>,
    /// a natural <c>IsNatural1</c>, whether the creature is now <c>IsStabilized</c>, and whether it <c>HasDied</c>.
    /// </returns>
    public (int Roll, bool IsSuccess, bool IsNatural20, bool IsNatural1, bool IsStabilized, bool HasDied)
        MakeDeathSavingThrow(Random rng)
    {
        int roll = rng.Next(1, 21);

        if (roll == 20)
        {
            // Rolling 20: regain 1 HP (PHB "Rolling 1 or 20")
            Heal(1); // Heal resets death save counters
            return (roll, true, true, false, false, false);
        }

        if (roll == 1)
        {
            // Rolling 1: counts as two failures (PHB "Rolling 1 or 20")
            AddDeathSaveFailures(2);
            return (roll, false, false, true, false, IsDead);
        }

        if (roll >= 10)
        {
            DeathSaveSuccesses++;
            if (DeathSaveSuccesses >= 3)
            {
                // Three successes: creature stabilizes (PHB "Death Saving Throws")
                IsStable = true;
                DeathSaveSuccesses = 0;
                DeathSaveFailures = 0;
                return (roll, true, false, false, true, false);
            }
            return (roll, true, false, false, false, false);
        }
        else
        {
            AddDeathSaveFailures(1);
            return (roll, false, false, false, false, IsDead);
        }
    }

    /// <summary>
    /// Increases the exhaustion level by <paramref name="levels"/> (capped at 6).
    /// At level 6 the creature dies.
    /// </summary>
    public void GainExhaustionLevel(int levels = 1)
    {
        ExhaustionLevel = Math.Clamp(ExhaustionLevel + levels, 0, 6);
        if (ExhaustionLevel >= 1)
            Conditions = Conditions.AddCondition(Condition.Exhaustion);
        if (ExhaustionLevel >= 6)
            CurrentHP = 0;
    }

    /// <summary>
    /// Decreases the exhaustion level by <paramref name="levels"/> (minimum 0).
    /// Removes the Exhaustion condition when the level reaches 0.
    /// </summary>
    public void ReduceExhaustionLevel(int levels = 1)
    {
        ExhaustionLevel = Math.Clamp(ExhaustionLevel - levels, 0, 6);
        if (ExhaustionLevel == 0)
            Conditions = Conditions.RemoveCondition(Condition.Exhaustion);
    }

    /// <summary>
    /// Records that the creature ate <paramref name="pounds"/> of food today.
    /// </summary>
    public void ConsumeFood(float pounds)
    {
        FoodConsumedToday += pounds;
    }

    /// <summary>
    /// Records that the creature drank <paramref name="gallons"/> of water today.
    /// </summary>
    public void ConsumeWater(float gallons)
    {
        WaterConsumedToday += gallons;
    }

    /// <summary>
    /// Resets the daily food and water counters. Call at the start of each new in-game day.
    /// </summary>
    public void ResetDailyNourishment()
    {
        FoodConsumedToday = 0f;
        WaterConsumedToday = 0f;
    }

    /// <summary>
    /// Processes food and water needs at the end of a day and applies exhaustion as per PHB rules.
    /// <para>Food: needs 1 lb/day. Half ration (≥ 0.5 lb) counts as half a day without food.
    /// After 3 + CON modifier consecutive days (minimum 1) without food, gains 1 exhaustion level per extra day.</para>
    /// <para>Water: needs 1 gal/day (2 in hot weather). Drinking less than half causes automatic
    /// exhaustion; drinking half to full requires a DC 15 CON save or gain exhaustion.
    /// Already-exhausted creatures gain 2 levels instead of 1.</para>
    /// </summary>
    /// <param name="isHotWeather">Whether the weather is hot (doubles the water requirement).</param>
    /// <param name="rng">Random number generator for the water saving throw.</param>
    /// <returns>
    /// A tuple with <c>FoodExhaustionGained</c>, <c>WaterExhaustionGained</c>,
    /// <c>WaterSaveRequired</c>, and <c>WaterSaveSucceeded</c>.
    /// </returns>
    public (int FoodExhaustionGained, int WaterExhaustionGained, bool WaterSaveRequired, bool WaterSaveSucceeded)
        ProcessEndOfDayNourishment(bool isHotWeather, Random rng)
    {
        int foodExhaustion = ProcessFoodNourishment();
        var (waterExhaustion, saveRequired, saveSucceeded) = ProcessWaterNourishment(isHotWeather, rng);
        return (foodExhaustion, waterExhaustion, saveRequired, saveSucceeded);
    }

    private int ProcessFoodNourishment()
    {
        const float fullRation = 1.0f;
        const float halfRation = 0.5f;

        if (FoodConsumedToday >= fullRation)
        {
            DaysWithoutFood = 0f;
            return 0;
        }

        DaysWithoutFood += FoodConsumedToday >= halfRation ? 0.5f : 1.0f;

        int limit = Math.Max(1, 3 + GetAbilityModifier(Constitution));
        if (DaysWithoutFood > limit)
        {
            GainExhaustionLevel(1);
            return 1;
        }

        return 0;
    }

    private (int ExhaustionGained, bool SaveRequired, bool SaveSucceeded) ProcessWaterNourishment(bool isHotWeather, Random rng)
    {
        float required = isHotWeather ? 2.0f : 1.0f;
        float halfRequired = required / 2f;

        if (WaterConsumedToday >= required)
            return (0, false, false);

        int levelsToGain = ExhaustionLevel >= 1 ? 2 : 1;

        if (WaterConsumedToday < halfRequired)
        {
            // Less than half the required amount: automatic exhaustion
            GainExhaustionLevel(levelsToGain);
            return (levelsToGain, false, false);
        }

        // Half to full required: DC 15 Constitution saving throw
        int profBonus = ConstitutionSaveProficiency ? (IsPlayer ? DndMath.GetProficiencyBonus(1) : 2) : 0;
        int roll = rng.Next(1, 21) + GetAbilityModifier(Constitution) + profBonus;
        bool saved = DndMath.MeetsDC(roll, 15);

        if (!saved)
        {
            GainExhaustionLevel(levelsToGain);
            return (levelsToGain, true, false);
        }

        return (0, true, true);
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

    /// <summary>
    /// Interrupts current movement, keeping only the immediate next waypoint.
    /// Logical position (X, Y, Z) is updated to this next waypoint.
    /// </summary>
    public void InterruptMovement()
    {
        if (_movementWaypoints.Count > 0)
        {
            Vector3 nextWaypoint = _movementWaypoints.Peek();
            _movementWaypoints.Clear();
            _movementWaypoints.Enqueue(nextWaypoint);
            X = (int)nextWaypoint.X;
            Y = (int)nextWaypoint.Y;
            Z = (int)nextWaypoint.Z;
        }
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
            CurrentDamageType = DamageType.Slashing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            StealthProficiency = true,
            HasNimbleEscape = true,
            XPReward = 50,  // CR 1/4
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
            CurrentDamageType = DamageType.Slashing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            XPReward = 100,  // CR 1/2
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
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            DamageImmunities = new HashSet<DamageType> { DamageType.Poison },
            DamageVulnerabilities = new HashSet<DamageType> { DamageType.Bludgeoning },
            ConditionImmunities = Condition.Poisoned | Condition.Exhaustion,
            XPReward = 50,  // CR 1/4
            DisplayColor = Color.White,
            IsPlayer = false,
        };
    }

    public static Creature CreateZombie(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Zombie",
            Type = CreatureType.Zombie,
            Size = CreatureSize.Medium,
            Alignment = Alignment.NeutralEvil,
            X = x,
            Y = y,
            Z = z,
            VisualX = x,
            VisualY = y,
            VisualZ = z,
            MaxHP = 22,
            CurrentHP = 22,
            ArmorClass = 8,
            Speed = 20,
            Strength = 13,
            Dexterity = 6,
            Constitution = 16,
            Intelligence = 3,
            Wisdom = 6,
            Charisma = 5,
            AttackName = "Slam",
            AttackBonus = 3,
            DamageDice = "1d6",
            DamageBonus = 1,
            CurrentDamageType = DamageType.Bludgeoning,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            DamageImmunities = new HashSet<DamageType> { DamageType.Poison },
            ConditionImmunities = Condition.Poisoned,
            WisdomSaveProficiency = true,
            HasUndeadFortitude = true,
            XPReward = 50,  // CR 1/4
            DisplayColor = Color.DarkOliveGreen,
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
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            DarkvisionRange = 0,
            StealthProficiency = true,
            PerceptionProficiency = true,
            HasPackTactics = true,
            HasKeenSenses = true,
            XPReward = 50,  // CR 1/4
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
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            HasSunlightSensitivity = true,
            HasPackTactics = true,
            XPReward = 25,  // CR 1/8
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
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            HasTremorsense = true,
            TremorsenseRange = 60,
            XPReward = 1800,  // CR 5
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
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            HasTrueSight = true,
            TrueSightRange = 120,
            XPReward = 1100,  // CR 4
            DisplayColor = Color.LightGoldenrodYellow,
            IsPlayer = false,
            CharismaSaveProficiency = true,
            WisdomSaveProficiency = true
        };
    }

    public static Creature CreateBloodHawk(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Blood Hawk",
            Type = CreatureType.BloodHawk,
            Size = CreatureSize.Small,
            Alignment = Alignment.Unaligned,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 7, CurrentHP = 7,
            ArmorClass = 12,
            Speed = 10,
            FlySpeed = 60,
            CanFly = true,
            Strength = 6, Dexterity = 14, Constitution = 10,
            Intelligence = 3, Wisdom = 14, Charisma = 5,
            AttackName = "Beak",
            AttackBonus = 4,
            DamageDice = "1d4",
            DamageBonus = 2,
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            HasPackTactics = true,
            HasKeenSenses = true,
            XPReward = 25,  // CR 1/8
            DisplayColor = Color.DarkRed,
            IsPlayer = false
        };
    }

    public static Creature CreateIceMephit(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Ice Mephit",
            Type = CreatureType.IceMephit,
            Size = CreatureSize.Small,
            Alignment = Alignment.NeutralEvil,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 21, CurrentHP = 21,
            ArmorClass = 11,
            Speed = 30,
            FlySpeed = 30,
            CanFly = true,
            Strength = 7, Dexterity = 13, Constitution = 10,
            Intelligence = 9, Wisdom = 11, Charisma = 12,
            AttackName = "Claws",
            AttackBonus = 3,
            DamageDice = "2d4",
            DamageBonus = 1,
            CurrentDamageType = DamageType.Slashing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            DamageImmunities = new HashSet<DamageType> { DamageType.Cold, DamageType.Poison },
            DamageVulnerabilities = new HashSet<DamageType> { DamageType.Fire, DamageType.Bludgeoning },
            ConditionImmunities = Condition.Poisoned,
            XPReward = 100,  // CR 1/2
            DisplayColor = new Color(180, 220, 240),
            IsPlayer = false
        };
    }

    public static Creature CreateBrownBear(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Brown Bear",
            Type = CreatureType.BrownBear,
            Size = CreatureSize.Large,
            Alignment = Alignment.Unaligned,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 34, CurrentHP = 34,
            ArmorClass = 11,
            Speed = 40,
            Strength = 19, Dexterity = 10, Constitution = 16,
            Intelligence = 2, Wisdom = 13, Charisma = 7,
            AttackName = "Bite",
            AttackBonus = 5,
            DamageDice = "1d8",
            DamageBonus = 4,
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            HasKeenSenses = true,
            XPReward = 200,  // CR 1
            DisplayColor = new Color(139, 90, 43),
            IsPlayer = false
        };
    }

    public static Creature CreateWinterWolf(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Winter Wolf",
            Type = CreatureType.WinterWolf,
            Size = CreatureSize.Large,
            Alignment = Alignment.NeutralEvil,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 75, CurrentHP = 75,
            ArmorClass = 13,
            Speed = 50,
            Strength = 18, Dexterity = 13, Constitution = 14,
            Intelligence = 7, Wisdom = 12, Charisma = 8,
            AttackName = "Bite",
            AttackBonus = 6,
            DamageDice = "2d6",
            DamageBonus = 4,
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            DamageImmunities = new HashSet<DamageType> { DamageType.Cold },
            HasPackTactics = true,
            HasKeenSenses = true,
            StealthProficiency = true,
            PerceptionProficiency = true,
            XPReward = 700,  // CR 3
            DisplayColor = new Color(220, 230, 240),
            IsPlayer = false
        };
    }

    public static Creature CreateMammoth(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Mammoth",
            Type = CreatureType.Mammoth,
            Size = CreatureSize.Huge,
            Alignment = Alignment.Unaligned,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 126, CurrentHP = 126,
            ArmorClass = 13,
            Speed = 40,
            Strength = 24, Dexterity = 9, Constitution = 21,
            Intelligence = 3, Wisdom = 11, Charisma = 6,
            AttackName = "Gore",
            AttackBonus = 10,
            DamageDice = "4d8",
            DamageBonus = 7,
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            XPReward = 2300,  // CR 6
            DisplayColor = new Color(100, 80, 60),
            IsPlayer = false
        };
    }

    public static Creature CreateYoungWhiteDragon(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Young White Dragon",
            Type = CreatureType.YoungWhiteDragon,
            Size = CreatureSize.Large,
            Alignment = Alignment.ChaoticEvil,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 133, CurrentHP = 133,
            ArmorClass = 17,
            Speed = 40,
            FlySpeed = 80,
            CanFly = true,
            Strength = 18, Dexterity = 10, Constitution = 18,
            Intelligence = 6, Wisdom = 11, Charisma = 12,
            AttackName = "Bite",
            AttackBonus = 7,
            DamageDice = "2d10",
            DamageBonus = 4,
            CurrentDamageType = DamageType.Piercing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            DamageImmunities = new HashSet<DamageType> { DamageType.Cold },
            PerceptionProficiency = true,
            StealthProficiency = true,
            ConstitutionSaveProficiency = true,
            WisdomSaveProficiency = true,
            CharismaSaveProficiency = true,
            XPReward = 2300,  // CR 6
            DisplayColor = new Color(200, 220, 240),
            IsPlayer = false
        };
    }

    public static Creature CreateFrostGiant(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Frost Giant",
            Type = CreatureType.FrostGiant,
            Size = CreatureSize.Huge,
            Alignment = Alignment.NeutralEvil,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 138, CurrentHP = 138,
            ArmorClass = 15,
            Speed = 40,
            Strength = 23, Dexterity = 9, Constitution = 21,
            Intelligence = 9, Wisdom = 10, Charisma = 12,
            AttackName = "Greataxe",
            AttackBonus = 9,
            DamageDice = "3d12",
            DamageBonus = 6,
            CurrentDamageType = DamageType.Slashing,
            IsMeleeAttack = true,
            DamageImmunities = new HashSet<DamageType> { DamageType.Cold },
            StrengthSaveProficiency = true,
            ConstitutionSaveProficiency = true,
            PerceptionProficiency = true,
            XPReward = 3900,  // CR 8
            DisplayColor = new Color(150, 190, 220),
            IsPlayer = false
        };
    }

    public static Creature CreateAbominableYeti(int x, int y, int z = 0)
    {
        return new Creature
        {
            Name = "Abominable Yeti",
            Type = CreatureType.AbominableYeti,
            Size = CreatureSize.Huge,
            Alignment = Alignment.ChaoticEvil,
            X = x, Y = y, Z = z,
            VisualX = x, VisualY = y, VisualZ = z,
            MaxHP = 137, CurrentHP = 137,
            ArmorClass = 15,
            Speed = 40,
            Strength = 24, Dexterity = 10, Constitution = 22,
            Intelligence = 9, Wisdom = 13, Charisma = 9,
            AttackName = "Claw",
            AttackBonus = 10,
            DamageDice = "2d8",
            DamageBonus = 7,
            CurrentDamageType = DamageType.Slashing,
            IsMeleeAttack = true,
            DarkvisionRange = 60,
            DamageImmunities = new HashSet<DamageType> { DamageType.Cold },
            HasKeenSenses = true,
            PerceptionProficiency = true,
            StealthProficiency = true,
            XPReward = 5000,  // CR 9
            DisplayColor = new Color(240, 245, 250),
            IsPlayer = false
        };
    }

    public static int GetXPForType(CreatureType type) => type switch
    {
        CreatureType.Kobold => 25,
        CreatureType.BloodHawk => 25,
        CreatureType.Goblin => 50,
        CreatureType.Skeleton => 50,
        CreatureType.Zombie => 50,
        CreatureType.Wolf => 50,
        CreatureType.IceMephit => 100,
        CreatureType.Orc => 100,
        CreatureType.BrownBear => 200,
        CreatureType.Couatl => 1100,
        CreatureType.WinterWolf => 700,
        CreatureType.Mammoth => 2300,
        CreatureType.YoungWhiteDragon => 2300,
        CreatureType.FrostGiant => 3900,
        CreatureType.AbominableYeti => 5000,
        CreatureType.Umber_Hulk => 1800,
        _ => 0
    };

    public static List<CreatureType> GetTypesForBiome(BiomeType biome) => biome switch
    {
        BiomeType.Arctic => new() { CreatureType.Wolf, CreatureType.Orc, CreatureType.Skeleton, CreatureType.Zombie },
        BiomeType.Coastal => new() { CreatureType.Kobold, CreatureType.Goblin, CreatureType.Couatl },
        BiomeType.Desert => new() { CreatureType.Kobold, CreatureType.Orc, CreatureType.Umber_Hulk },
        BiomeType.Forest => new() { CreatureType.Goblin, CreatureType.Orc, CreatureType.Wolf, CreatureType.Kobold, CreatureType.Umber_Hulk },
        BiomeType.Grassland => new() { CreatureType.Goblin, CreatureType.Orc, CreatureType.Wolf, CreatureType.Kobold, CreatureType.Skeleton, CreatureType.Zombie },
        BiomeType.Hill => new() { CreatureType.Orc, CreatureType.Kobold, CreatureType.Wolf, CreatureType.Goblin },
        BiomeType.Mountain => new() { CreatureType.Orc, CreatureType.Kobold, CreatureType.Wolf, CreatureType.Umber_Hulk, CreatureType.Couatl },
        BiomeType.Swamp => new() { CreatureType.Kobold, CreatureType.Zombie, CreatureType.Orc, CreatureType.Umber_Hulk },
        BiomeType.Underdark => new() { CreatureType.Umber_Hulk, CreatureType.Goblin, CreatureType.Skeleton, CreatureType.Zombie, CreatureType.Kobold },
        BiomeType.Underwater => new() { CreatureType.Couatl }, // Flyers/Swimmers
        BiomeType.Urban => new() { CreatureType.Goblin, CreatureType.Skeleton, CreatureType.Zombie, CreatureType.Kobold },
        _ => new() { CreatureType.Goblin }
    };

    public static Creature Create(CreatureType type, int x, int y, int z = 0) => type switch
    {
        CreatureType.Goblin => CreateGoblin(x, y, z),
        CreatureType.Orc => CreateOrc(x, y, z),
        CreatureType.Skeleton => CreateSkeleton(x, y, z),
        CreatureType.Zombie => CreateZombie(x, y, z),
        CreatureType.Wolf => CreateWolf(x, y, z),
        CreatureType.Kobold => CreateKobold(x, y, z),
        CreatureType.Umber_Hulk => CreateUmberHulk(x, y, z),
        CreatureType.Couatl => CreateCouatl(x, y, z),
        CreatureType.BloodHawk => CreateBloodHawk(x, y, z),
        CreatureType.IceMephit => CreateIceMephit(x, y, z),
        CreatureType.BrownBear => CreateBrownBear(x, y, z),
        CreatureType.WinterWolf => CreateWinterWolf(x, y, z),
        CreatureType.Mammoth => CreateMammoth(x, y, z),
        CreatureType.YoungWhiteDragon => CreateYoungWhiteDragon(x, y, z),
        CreatureType.FrostGiant => CreateFrostGiant(x, y, z),
        CreatureType.AbominableYeti => CreateAbominableYeti(x, y, z),
        _ => CreateGoblin(x, y, z)
    };

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
            Level = character.Level,
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
            PerceptionProficiency = character.PerceptionProficiency,
            CurrentDonDoffProcess = character.CurrentDonDoffProcess
        };
        
        // Apply race-specific vision traits
        creature.HasSuperiorDarkvision = raceData.HasSuperiorDarkvision;
        creature.HasSunlightSensitivity = raceData.HasSunlightSensitivity;

        // Armor non-proficiency penalty
        creature.HasArmorNonProficiencyPenalty = character.IsWearingNonProficientArmor;

        // Apply nourishment state
        creature.ExhaustionLevel = character.ExhaustionLevel;
        creature.DaysWithoutFood = character.DaysWithoutFood;
        creature.FoodConsumedToday = character.FoodConsumedToday;
        creature.WaterConsumedToday = character.WaterConsumedToday;
        if (creature.ExhaustionLevel > 0)
            creature.Conditions = creature.Conditions.AddCondition(Condition.Exhaustion);
        
        // Set attack based on equipped weapon
        if (character.InventoryData.EquippedWeapon != null)
        {
            var weapon = ItemDatabase.GetItem(character.InventoryData.EquippedWeapon.Name);
            creature.AttackName = weapon.Name;

            // Thrown weapons (e.g. Dagger, Handaxe) are melee weapons by default.
            // Pure ranged weapons (bow, crossbow) use Dexterity.
            bool isEffectivelyRanged = weapon.IsRanged && !weapon.IsThrown;
            int abilityMod = weapon.IsFinesse
                ? Math.Max(creature.GetAbilityModifier(creature.Strength), creature.GetAbilityModifier(creature.Dexterity))
                : isEffectivelyRanged
                    ? creature.GetAbilityModifier(creature.Dexterity)
                    : creature.GetAbilityModifier(creature.Strength);

            creature.AttackBonus = abilityMod + character.ProficiencyBonus;

            bool isVersatileTwoHanded = weapon.IsVersatile && character.InventoryData.OffhandWeapon == null && character.InventoryData.EquippedShield == null;
            creature.DamageDice = isVersatileTwoHanded ? weapon.VersatileDamageDice : weapon.DamageDice;

            creature.DamageBonus = abilityMod;
            creature.CurrentDamageType = weapon.DamageType;
            creature.IsMeleeAttack = !isEffectivelyRanged;
            creature.NormalRange = isEffectivelyRanged ? weapon.Range : 0;
            creature.LongRange = isEffectivelyRanged ? weapon.LongRange : 0;
            creature.IsHeavyWeaponAttack = weapon.IsHeavy;
            creature.IsLoadingWeapon = weapon.IsLoading;
            creature.IsReachWeapon = weapon.IsReach;
            creature.IsLanceAttack = weapon.IsSpecial && weapon.Name == "Lance";
            creature.IsNetAttack = weapon.IsSpecial && weapon.Name == "Net";
            creature.IsStrengthBasedAttack = weapon.IsFinesse
                ? creature.GetAbilityModifier(creature.Strength) >= creature.GetAbilityModifier(creature.Dexterity)
                : !isEffectivelyRanged;
        }
        else
        {
            // Unarmed strike
            creature.AttackName = "Unarmed Strike";
            creature.AttackBonus = creature.GetAbilityModifier(creature.Strength) + character.ProficiencyBonus;
            creature.DamageDice = "1";
            creature.DamageBonus = creature.GetAbilityModifier(creature.Strength);
            creature.CurrentDamageType = DamageType.Bludgeoning;
            creature.IsMeleeAttack = true;
        }

        creature.IsSilveredAttack = character.InventoryData.EquippedWeapon?.IsSilvered ?? false;

        if (character.Class == "Barbarian")
        {
            creature.RagesRemaining = character.RagesRemaining;
            var barbarianData = ClassData.GetClass("Barbarian");
            var levelData = barbarianData.GetLevelData(character.Level);
            if (levelData != null)
                creature.RageDamageBonus = levelData.RageDamage;
        }

        if (character.Class == "Bard")
        {
            creature.BardicInspirationDice = character.BardicInspirationDice;
            creature.BardicInspirationUsesRemaining = character.BardicInspirationUsesRemaining;
            creature.BardicInspirationMax = character.BardicInspirationMax;
            creature.SpellSlotsRemaining = (int[])character.SpellSlotsRemaining.Clone();
            creature.SpellSlotsMax = (int[])character.SpellSlotsMax.Clone();
        }

        if (character.Class == "Cleric")
        {
            creature.SpellSlotsRemaining = (int[])character.SpellSlotsRemaining.Clone();
            creature.SpellSlotsMax = (int[])character.SpellSlotsMax.Clone();
            creature.ChannelDivinityUsesRemaining = character.ChannelDivinityUsesRemaining;
            creature.ChannelDivinityUsesMax = character.ChannelDivinityUsesMax;
        }

        return creature;
    }

    public void SetupAttackFromWeapon(string weaponName, Character character, bool isOffhand = false, bool isThrownAttack = false, bool isTwoHanded = false, bool isImprovisedMelee = false)
    {
        var weapon = ItemDatabase.GetItem(weaponName);
        IsOffhandAttack = isOffhand;

        // Detect whether the specific weapon instance has been silvered.
        IsSilveredAttack = false;
        var weaponInstance = isOffhand
            ? character.InventoryData.OffhandWeapon
            : character.InventoryData.EquippedWeapon;
        if (weaponInstance?.Name == weaponName)
            IsSilveredAttack = weaponInstance.IsSilvered;

        if (weapon == null || weapon.Type != ItemType.Weapon)
        {
            // Fallback to unarmed strike — always proficient
            AttackName = "Unarmed Strike";
            int strMod = GetAbilityModifier(Strength);
            AttackBonus = strMod + character.ProficiencyBonus;
            DamageDice = "1";
            DamageBonus = strMod;
            CurrentDamageType = DamageType.Bludgeoning;
            IsMeleeAttack = true;
            NormalRange = 0;
            LongRange = 0;
            IsOffhandAttack = false;
            HasWeaponNonProficiencyPenalty = false;
            IsImprovisedWeaponAttack = false;
            return;
        }

        AttackName = weapon.Name;

        // Improvised weapon cases (PHB "Improvised Weapons"):
        // 1. Throwing a melee weapon without the Thrown property → 1d4, 20/60 ft range, no proficiency.
        // 2. Using a ranged weapon as a melee attack → 1d4, STR-based, no proficiency.
        bool isImprovisedThrow = isThrownAttack && !weapon.IsThrown && !weapon.IsRanged;
        if (isImprovisedThrow || isImprovisedMelee)
        {
            IsImprovisedWeaponAttack = true;
            int impStrMod = GetAbilityModifier(Strength);
            HasWeaponNonProficiencyPenalty = true;
            AttackBonus = impStrMod;
            DamageDice = "1d4";
            DamageBonus = 0;
            CurrentDamageType = DamageType.Bludgeoning;
            IsMeleeAttack = !isImprovisedThrow;
            NormalRange = isImprovisedThrow ? 20 : 0;
            LongRange = isImprovisedThrow ? 60 : 0;
            IsHeavyWeaponAttack = false;
            IsLoadingWeapon = false;
            IsReachWeapon = false;
            IsLanceAttack = false;
            IsNetAttack = false;
            IsStrengthBasedAttack = true;
            return;
        }

        IsImprovisedWeaponAttack = false;

        // Pure ranged weapons (bow, crossbow) use Dexterity and are always ranged.
        // Thrown weapons (e.g. Dagger, Handaxe) are melee by default but can be thrown as a ranged attack.
        // When isThrownAttack is true, a thrown weapon uses its thrown range and the same ability modifier
        // as for a melee attack: STR for non-finesse weapons, or the better of STR/DEX for finesse weapons (PHB "Thrown").
        bool isEffectivelyRanged = (weapon.IsRanged && !weapon.IsThrown) || (weapon.IsThrown && isThrownAttack);
        int abilityMod = weapon.IsFinesse
            ? Math.Max(GetAbilityModifier(Strength), GetAbilityModifier(Dexterity))
            : (isEffectivelyRanged && !isThrownAttack)  // Pure ranged weapons use DEX; thrown weapons use STR
                ? GetAbilityModifier(Dexterity)
                : GetAbilityModifier(Strength);

        // Only add proficiency bonus when the character is proficient with this weapon (PHB "Weapon Proficiency").
        bool isProficient = character.IsProficientWithWeapon(weapon.Name);
        HasWeaponNonProficiencyPenalty = !isProficient;
        AttackBonus = abilityMod + (isProficient ? character.ProficiencyBonus : 0);
        IsHeavyWeaponAttack = weapon.IsHeavy;
        IsLoadingWeapon = weapon.IsLoading;
        IsReachWeapon = weapon.IsReach;
        IsLanceAttack = weapon.IsSpecial && weapon.Name == "Lance";
        IsNetAttack = weapon.IsSpecial && weapon.Name == "Net";

        // A thrown weapon is released with one hand, so versatile weapons always use the one-handed die when thrown.
        // When isTwoHanded is explicitly true, use the versatile die regardless of other conditions.
        bool isVersatileTwoHanded = weapon.IsVersatile && !isThrownAttack &&
            (isTwoHanded || (character.InventoryData.OffhandWeapon == null && character.InventoryData.EquippedShield == null));
        DamageDice = isVersatileTwoHanded ? weapon.VersatileDamageDice : weapon.DamageDice;

        // TWF: don't add ability modifier to damage (unless negative)
        DamageBonus = isOffhand ? Math.Min(0, abilityMod) : abilityMod;

        CurrentDamageType = weapon.DamageType;
        // Thrown attacks and pure ranged attacks are not melee.
        IsMeleeAttack = !isEffectivelyRanged;
        // Finesse: same modifier for attack and damage (PHB "Finesse").
        // Thrown weapons use the same modifier as for a melee attack: STR for non-finesse, or best of STR/DEX
        // for finesse weapons (PHB "Thrown"). Rage bonus only applies to STR-based melee attacks (PHB "Rage").
        IsStrengthBasedAttack = weapon.IsFinesse
            ? GetAbilityModifier(Strength) >= GetAbilityModifier(Dexterity)
            : !isEffectivelyRanged || isThrownAttack;
        // Pure ranged weapons and thrown attacks use the weapon's range; melee use has no range.
        NormalRange = isEffectivelyRanged ? weapon.Range : 0;
        LongRange   = isEffectivelyRanged ? weapon.LongRange : 0;
    }

    public void SetupAttackFromSpell(Spell spell, Character character)
    {
        AttackName = spell.Name;
        int mod = character.GetPrimaryAbilityModifier();
        AttackBonus = mod + character.ProficiencyBonus;
        DamageDice = spell.DamageDice;
        DamageBonus = 0; // Spell damage usually doesn't add modifier unless special feature
        CurrentDamageType = spell.DamageType;
        IsMeleeAttack = false;
        NormalRange = spell.Range;
        LongRange = 0;
    }
    
    public void UpdateCharacter(Character character)
    {
        character.CurrentHP = CurrentHP;
        character.RagesRemaining = RagesRemaining;
        character.ExhaustionLevel = ExhaustionLevel;
        character.DaysWithoutFood = DaysWithoutFood;
        character.FoodConsumedToday = FoodConsumedToday;
        character.WaterConsumedToday = WaterConsumedToday;

        character.CurrentDonDoffProcess = CurrentDonDoffProcess;

        if (character.Class == "Bard")
        {
            character.BardicInspirationUsesRemaining = BardicInspirationUsesRemaining;
            character.SpellSlotsRemaining = (int[])SpellSlotsRemaining.Clone();
        }

        if (character.Class == "Cleric")
        {
            character.SpellSlotsRemaining = (int[])SpellSlotsRemaining.Clone();
            character.ChannelDivinityUsesRemaining = ChannelDivinityUsesRemaining;
        }
    }
}
