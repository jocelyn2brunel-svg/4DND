using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

#nullable enable

namespace _4DND;

/// <summary>
/// Manages D&D 5e combat encounters following the official Order of Combat rules.
/// 
/// <para><b>The Order of Combat</b></para>
/// <para>
/// A typical combat encounter is a clash between two sides, a flurry of weapon swings, feints,
/// parries, footwork, and spellcasting. The game organizes the chaos of combat into a cycle of
/// rounds and turns.
/// </para>
/// <para>
/// A <b>round</b> represents about 6 seconds in the game world. During a round, each participant
/// in a battle takes a <b>turn</b>. The order of turns is determined at the beginning of a combat
/// encounter, when everyone rolls initiative. Once everyone has taken a turn, the fight continues
/// to the next round if neither side has defeated the other.
/// </para>
/// </summary>
public class CombatManager
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

    public readonly record struct TacticalMapNode(int X, int Y, int Z);
    public InfiniteGrid3D<TileType>? TacticalMap { get; set; }
    public WallEdgeSystem? WallEdges { get; set; }
    public Func<int, int, int, DungeonDoorState?>? DoorStateProvider { get; set; }
    private readonly List<Creature> _combatants = new();
    private int _currentTurnIndex = 0;
    private int _currentRound = 0;
    private bool _inCombat = false;
    private readonly Random _random = new();
    private int _pendingXP = 0;

    /// <summary>
    /// Messages generated during turn transitions (rage expiry, etc.) to be consumed by the caller.
    /// </summary>
    public List<string> TurnMessages { get; } = new();

    public bool InCombat => _inCombat;
    public List<Creature> Combatants => _combatants;
    public int CurrentTurnIndex => _currentTurnIndex;
    public Creature? CurrentCombatant
    {
        get
        {
            if (!_inCombat || _combatants.Count == 0)
                return null;

            NormalizeTurnIndex();
            return _combatants[_currentTurnIndex];
        }
    }
    public int CurrentRound => _currentRound;
    
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

    /// <summary>
    /// Determines which creatures are surprised at the start of an encounter.
    /// 
    /// The DM compares the Dexterity (Stealth) check of any hiding creature
    /// against the passive Wisdom (Perception) score of each creature on the opposing side.
    /// Any creature whose passive Perception is not exceeded by the Stealth check is surprised.
    /// 
    /// <para>If neither side is hiding, no one is surprised — every creature notices every other.</para>
    /// </summary>
    /// <param name="stealthySide">Creatures attempting to hide / be stealthy.</param>
    /// <param name="otherSide">Creatures that might be caught off-guard.</param>
    /// <returns>
    /// The set of creatures from <paramref name="otherSide"/> that are surprised,
    /// along with a log of each stealth roll vs. passive perception comparison.
    /// </returns>
    public (HashSet<Creature> Surprised, List<string> Log) RollSurprise(
        List<Creature> stealthySide,
        List<Creature> otherSide)
    {
        var surprised = new HashSet<Creature>();
        var log = new List<string>();

        if (stealthySide.Count == 0 || otherSide.Count == 0)
            return (surprised, log);

        // Each member of the stealthy side rolls Dexterity (Stealth)
        var stealthRolls = new List<(Creature Creature, int Roll)>();
        foreach (var sneaker in stealthySide)
        {
            int dexMod = sneaker.GetAbilityModifier(sneaker.Dexterity);
            int profBonus = sneaker.IsPlayer ? DndMath.GetProficiencyBonus(1) : 2;
            int bonus = dexMod + (sneaker.StealthProficiency ? profBonus : 0);
            int roll = RollD20() + bonus;
            stealthRolls.Add((sneaker, roll));
            log.Add($"{sneaker.Name} Stealth check: {roll} (d20 + {bonus})");
        }

        // Compare the lowest stealth roll on the stealthy side against each defender's passive Perception.
        // A creature is NOT surprised only if at least one stealth roll fails to beat its passive Perception.
        // Per RAW: a creature is surprised when it doesn't notice the threat —
        // i.e. every stealth roll beats its passive Perception.
        foreach (var defender in otherSide)
        {
            int passivePerception = defender.PassivePerception;
            bool allRollsBeatPassive = stealthRolls.All(sr => sr.Roll > passivePerception);

            if (allRollsBeatPassive)
            {
                surprised.Add(defender);
                log.Add($"{defender.Name} is SURPRISED (Passive Perception {passivePerception} beaten by all stealth rolls).");
            }
            else
            {
                log.Add($"{defender.Name} is NOT surprised (Passive Perception {passivePerception}).");
            }
        }

        return (surprised, log);
    }
    // Add this method to the CombatManager class (or appropriate file if handling combat actions here)
    public bool Hide(Creature creature, bool isBonusAction = false, VisionSystem? visionSystem = null)
    {
        // Simple implementation: checks if the creature can hide and applies the hidden state
        if (creature == null || creature.IsHidden)
            return false;

        // Example: checks if the creature has an action or bonus action available
        if ((isBonusAction && !creature.HasBonusAction) || (!isBonusAction && !creature.HasAction))
            return false;

        // Here you can add stealth check logic, visibility, etc.
        creature.IsHidden = true;
        if (isBonusAction)
            creature.HasBonusAction = false;
        else
            creature.HasAction = false;

        return true;
    }

    /// <summary>
    /// Spills ball bearings from their pouch to cover a level 10-foot square area (2×2 tiles).
    /// Costs an action. Each creature that moves through the covered area at normal speed must
    /// succeed on a DC 10 Dexterity saving throw or fall prone (PHB "Adventuring Gear: Ball Bearings").
    /// </summary>
    /// <param name="user">The creature spilling the ball bearings.</param>
    /// <param name="originX">The top-left X coordinate of the 2×2 area to cover.</param>
    /// <param name="originY">The top-left Y coordinate of the 2×2 area to cover.</param>
    /// <param name="originZ">The Z level of the area.</param>
    /// <returns>True if the ball bearings were successfully spilled.</returns>
    public bool SpillBallBearings(Creature user, int originX, int originY, int originZ)
    {
        if (_inCombat && !user.HasAction) return false;
        if (TacticalMap == null) return false;

        for (int dx = 0; dx <= 1; dx++)
        {
            for (int dy = 0; dy <= 1; dy++)
            {
                var existing = TacticalMap.Get(originX + dx, originY + dy, originZ);
                if (existing != TileType.Tree &&
                    existing != TileType.Shrub && existing != TileType.Empty)
                {
                    TacticalMap.Set(originX + dx, originY + dy, originZ, TileType.BallBearings);
                }
            }
        }

        if (_inCombat)
            user.HasAction = false;

        TurnMessages.Add(Loc.Tr("{0} spills ball bearings covering a 10-foot square area!", user.Name));
        return true;
    }

    /// <summary>
    /// Rolls a DC 10 Dexterity saving throw for a creature entering a ball bearings zone.
    /// On failure the creature gains the Prone condition.
    /// </summary>
    /// <returns>True if the creature succeeded on the saving throw.</returns>
    private bool CheckBallBearingsSave(Creature creature)
    {
        var saveCheck = MakeSavingThrow(creature, "DEX", 10);
        bool saved = saveCheck.Success;

        if (!saved)
        {
            creature.Conditions = creature.Conditions.AddCondition(Condition.Prone);
            TurnMessages.Add(Loc.Tr("{0} slips on ball bearings and falls prone! (DEX save {1} vs DC 10)", creature.Name, saveCheck.Total));
        }
        else
        {
            TurnMessages.Add(Loc.Tr("{0} navigates through ball bearings safely. (DEX save {1} vs DC 10)", creature.Name, saveCheck.Total));
        }

        return saved;
    }

    /// <summary>
    /// Begins a new combat encounter.
    /// Each participant rolls initiative (1d20 + Dexterity modifier) to determine turn order.
    /// Combatants are sorted in descending initiative order — the highest roll acts first.
    /// This marks the start of Round 1.
    /// </summary>
    /// <param name="creatures">All participants in the encounter.</param>
    /// <param name="surprisedCreatures">
    /// Optional set of creatures that are surprised.
    /// Surprised creatures cannot move, take actions, or take reactions on their first turn.
    /// </param>
    public void StartCombat(List<Creature> creatures, HashSet<Creature>? surprisedCreatures = null)
    {
        _combatants.Clear();
        _combatants.AddRange(creatures);
        
        // Roll initiative for all creatures
        foreach (var creature in _combatants)
        {
            int dexMod = creature.GetAbilityModifier(creature.Dexterity);
            creature.Initiative = RollD20() + dexMod;
            
            // Clear all resources; each creature receives them at the start of their turn
            creature.HasAction = false;
            creature.HasBonusAction = false;
            creature.HasReaction = false;
            creature.MovementRemaining = 0;
            creature.DiagonalStepsTaken = 0;
            creature.HasFreeObjectInteraction = false;
        }
        
        // Sort by initiative (descending)
        _combatants.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));
        
        _currentTurnIndex = 0;
        _currentRound = 1;
        _inCombat = true;

        // Grant resources to the first combatant — their turn starts immediately
        if (_combatants.Count > 0)
        {
            var first = _combatants[0];
            first.HasAction = true;
            first.HasBonusAction = true;
            first.HasReaction = true;
            first.MovementRemaining = first.Speed;
            first.DiagonalStepsTaken = 0;
            first.HasFreeObjectInteraction = true;
            first.IsDisengaged = false;
            first.IsDodging = false;
            ProcessStartOfTurnEffects(first);
        }

        if (surprisedCreatures != null)
        {
            // Apply the surprised condition
            foreach (var creature in surprisedCreatures)
            {
                if (_combatants.Contains(creature))
                {
                    creature.IsSurprised = true;

                    // Creatures that are surprised cannot do anything on their first turn
                    creature.HasAction = false;
                    creature.HasBonusAction = false;
                    creature.HasReaction = false;
                    creature.MovementRemaining = 0;
                }
            }
        }
    }
    
    /// <summary>
    /// Restores a combat state from saved data.
    /// </summary>
    public void RestoreCombat(int round, int turnIndex, List<Creature> creatures)
    {
        _combatants.Clear();
        _combatants.AddRange(creatures);
        _combatants.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));

        _currentRound = round;
        _currentTurnIndex = turnIndex;
        _inCombat = true;

        // Ensure the current combatant has their resources if it's their turn
        if (CurrentCombatant != null)
        {
            var c = CurrentCombatant;
            if (c.CurrentHP > 0)
            {
                c.HasAction = true;
                c.HasBonusAction = true;
                c.HasReaction = true;
                c.MovementRemaining = c.Speed;
                c.DiagonalStepsTaken = 0;
                c.HasFreeObjectInteraction = true;
            }
        }
    }

    public void EndCombat()
    {
        _inCombat = false;
        _combatants.Clear();
        _currentTurnIndex = 0;
        _currentRound = 0;
    }

    /// <summary>
    /// Removes non-player creatures that are too far from the given position.
    /// Called periodically during exploration to prevent spawned-but-never-engaged
    /// enemies from accumulating in <see cref="Combatants"/> and blocking pathfinding.
    /// </summary>
    /// <param name="centerX">Reference X position (typically the player).</param>
    /// <param name="centerY">Reference Y position.</param>
    /// <param name="centerZ">Reference Z position.</param>
    /// <param name="maxDistance">Chebyshev distance (in tiles) beyond which enemies are removed.</param>
    public void PurgeDistantEnemies(int centerX, int centerY, int centerZ, int maxDistance = 40)
    {
        if (_inCombat) return;

        _combatants.RemoveAll(c =>
            !c.IsPlayer &&
            DndMath.CalculateDistance(c.X, c.Y, c.Z, centerX, centerY, centerZ) > maxDistance);
    }

    /// <summary>
    /// Returns the total XP earned since the last call and resets the counter.
    /// XP is split equally among the players who participated in the battle.
    /// </summary>
    public int CollectPendingXP()
    {
        int xp = _pendingXP;
        _pendingXP = 0;
        return xp;
    }
    
    /// <summary>
    /// Ends the current combatant's turn and advances to the next in initiative order.
    /// When all combatants have taken their turn, a new round begins (~6 seconds of game time).
    /// At the start of their turn, each combatant recovers their action, bonus action,
    /// reaction, and movement. The fight continues until one side is defeated.
    /// </summary>
    /// <returns>True if a new round has begun.</returns>
    public bool NextTurn()
    {
        if (!_inCombat || _combatants.Count == 0) return false;

        // End the current combatant's turn: clear their surprised condition now
        if (CurrentCombatant != null)
            CurrentCombatant.IsSurprised = false;

        // Collect XP from enemies that died this turn
        var dyingEnemies = _combatants.Where(c => !c.IsAlive() && !c.IsPlayer).ToList();
        if (dyingEnemies.Count > 0)
        {
            int playerCount = _combatants.Count(c => c.IsPlayer && c.IsAlive());
            if (playerCount > 0)
            {
                int totalXP = dyingEnemies.Sum(c => c.XPReward);
                _pendingXP += totalXP / playerCount;
            }
        }

        // Remove dead creatures
        _combatants.RemoveAll(c => !c.IsAlive());

        if (_combatants.Count == 0)
        {
            EndCombat();
            return false;
        }

        NormalizeTurnIndex();
        
        // Check if combat should end
        bool hasPlayer = _combatants.Any(c => c.IsPlayer);
        bool hasEnemy = _combatants.Any(c => !c.IsPlayer);
        
        if (!hasPlayer || !hasEnemy)
        {
            EndCombat();
            return false;
        }
        
        // Move to next turn
        _currentTurnIndex++;
        
        bool newRound = false;

        // Check if we completed a round
        if (_currentTurnIndex >= _combatants.Count)
        {
            _currentTurnIndex = 0;
            _currentRound++;
            newRound = true;
        }
        
        // Refresh the incoming combatant's resources at the start of their turn.
        // A surprised creature cannot move, act, or react on its first turn.
        if (CurrentCombatant != null)
        {
            if (!CurrentCombatant.IsSurprised && CurrentCombatant.CurrentHP > 0)
            {
                CurrentCombatant.HasAction = true;
                CurrentCombatant.HasBonusAction = true;
                CurrentCombatant.HasReaction = true;
                CurrentCombatant.MovementRemaining = CurrentCombatant.Speed;
                CurrentCombatant.DiagonalStepsTaken = 0;
                CurrentCombatant.HasFreeObjectInteraction = true;
                CurrentCombatant.IsDisengaged = false;
                CurrentCombatant.IsDodging = false;
                CurrentCombatant.IsHidden = false;
                CurrentCombatant.IsBeingHelped = false; // Help benefit expires at the start of this creature's turn
                CurrentCombatant.HasFiredLoadingWeaponThisTurn = false;
            }

            // Process ongoing effects (poison, burning, etc.)
            ProcessStartOfTurnEffects(CurrentCombatant);
        }

        return newRound;
    }

    private void NormalizeTurnIndex()
    {
        if (_combatants.Count == 0)
        {
            _currentTurnIndex = 0;
            return;
        }

        if (_currentTurnIndex < 0)
        {
            _currentTurnIndex = ((_currentTurnIndex % _combatants.Count) + _combatants.Count) % _combatants.Count;
        }
        else if (_currentTurnIndex >= _combatants.Count)
        {
            _currentTurnIndex %= _combatants.Count;
        }
    }
    
    private void ProcessStartOfTurnEffects(Creature creature)
    {
        // Armor donning/doffing progress
        if (creature.CurrentDonDoffProcess != null && creature.CurrentDonDoffProcess.IsActive)
        {
            var process = creature.CurrentDonDoffProcess;
            process.RoundsRemaining--;
            process.MinutesRemaining -= 0.1; // 6 seconds

            if (process.RoundsRemaining <= 0 || process.MinutesRemaining <= 0)
            {
                process.IsActive = false;
                // The actual equipment change will be handled by the Game loop when it sees IsActive = false
                if (process.IsDoffing)
                    TurnMessages.Add(Loc.Tr("{0} finished doffing {1}.", creature.Name, process.Item?.Name ?? "Armor"));
                else
                    TurnMessages.Add(Loc.Tr("{0} finished donning {1}.", creature.Name, process.Item?.Name ?? "Armor"));
            }
            else
            {
                if (process.IsDoffing)
                    TurnMessages.Add(Loc.Tr("{0} is doffing {1} ({2} rounds left).", creature.Name, process.Item?.Name ?? "Armor", process.RoundsRemaining));
                else
                    TurnMessages.Add(Loc.Tr("{0} is donning {1} ({2} rounds left).", creature.Name, process.Item?.Name ?? "Armor", process.RoundsRemaining));
            }
        }

        // Death Saving Throw: players at 0 HP make a death saving throw at the start of each turn (PHB "Death Saving Throws").
        if (creature.IsPlayer && creature.CurrentHP == 0 && !creature.IsDead && !creature.IsStable)
        {
            var (roll, isSuccess, isNatural20, isNatural1, isStabilized, hasDied) = creature.MakeDeathSavingThrow(_random);

            if (isNatural20)
                TurnMessages.Add($"{creature.Name} rolled a natural 20 on their death saving throw and regains 1 HP!");
            else if (hasDied)
                TurnMessages.Add($"{creature.Name} has died (3 death save failures).");
            else if (isStabilized)
                TurnMessages.Add($"{creature.Name} stabilizes with 3 successes and no longer makes death saving throws.");
            else if (isNatural1)
                TurnMessages.Add($"{creature.Name} rolled a 1 — counts as 2 death save failures! ({creature.DeathSaveSuccesses} successes / {creature.DeathSaveFailures} failures)");
            else if (isSuccess)
                TurnMessages.Add($"{creature.Name} succeeds on their death saving throw (rolled {roll}). ({creature.DeathSaveSuccesses} successes / {creature.DeathSaveFailures} failures)");
            else
                TurnMessages.Add($"{creature.Name} fails their death saving throw (rolled {roll}). ({creature.DeathSaveSuccesses} successes / {creature.DeathSaveFailures} failures)");
        }

        // Flying Movement: a non-hovering flyer falls if it cannot move (PHB "Flying Movement").
        // Check at the start of the turn covers conditions applied mid-turn (paralysis, grapple, etc.).
        CheckFlyingFall(creature);

        // Process ongoing damage effects like poison, burning, etc.
        // This can be extended later for duration-based conditions

        // Example: Poisoned creatures might take damage each turn
        if (creature.Conditions.HasCondition(Condition.Poisoned))
        {
            // Future: implement ongoing poison damage
        }

        // Burning: 1d4 fire damage at the start of the turn (PHB "Adventuring Gear: Alchemist's Fire").
        if (creature.Conditions.HasCondition(Condition.Burning))
        {
            int burnDamage = RollDamage("1d4", 0, false);
            creature.TakeDamage(burnDamage, DamageType.Fire, false);
            TurnMessages.Add(Loc.Tr("{0} takes {1} fire damage from alchemist's fire.", creature.Name, burnDamage));
        }

        // Rage ends immediately if the creature is knocked unconscious
        if (creature.IsRaging && creature.Conditions.HasCondition(Condition.Unconscious))
        {
            EndRage(creature);
            TurnMessages.Add($"{creature.Name}'s rage ends (unconscious).");
            return;
        }

        if (creature.IsRaging)
        {
            // On the first turn of rage (RageTurnsLeft still at its initial value of 10),
            // skip the attack/damage check — the barbarian may not have acted yet.
            bool firstTurnOfRage = creature.RageTurnsLeft == 10;

            if (!firstTurnOfRage && !creature.HasAttackedThisRound && !creature.HasTakenDamageThisRound)
            {
                EndRage(creature);
                TurnMessages.Add($"{creature.Name}'s rage ends (no attack or damage last turn).");
            }
            else if (creature.IsRaging)
            {
                creature.RageTurnsLeft--;
                if (creature.RageTurnsLeft <= 0)
                {
                    EndRage(creature);
                    TurnMessages.Add($"{creature.Name}'s rage ends (1 minute elapsed).");
                }
            }

            // Reset per-turn tracking flags for the new turn
            creature.HasAttackedThisRound = false;
            creature.HasTakenDamageThisRound = false;
        }
    }

    /// <summary>
    /// Returns an integer rank for a creature size, used to compute size difference.
    /// </summary>
    private static int GetSizeIndex(CreatureSize size) => size switch
    {
        CreatureSize.Tiny => 0,
        CreatureSize.Small => 1,
        CreatureSize.Medium => 2,
        CreatureSize.Large => 3,
        CreatureSize.Huge => 4,
        CreatureSize.Gargantuan => 5,
        _ => 2
    };

    /// <summary>
    /// Determines whether <paramref name="mover"/> can pass <em>through</em> the tile at (x, y, z)
    /// during movement (transit only — creatures can never end their move in another's space).
    /// <para>
    /// Rules (PHB "Moving Around Other Creatures"):
    /// <list type="bullet">
    /// <item>You can always move through a nonhostile creature's space.</item>
    /// <item>You can move through a hostile creature's space only if the hostile creature
    ///       is at least two sizes larger or smaller than you.</item>
    /// <item>In all cases the occupied space counts as difficult terrain.</item>
    /// </para>
    /// </summary>
    private bool IsDungeonDoor(TileType type) => type == TileType.DungeonDoorWooden || type == TileType.DungeonDoorStone || type == TileType.DungeonDoorIron || type == TileType.DungeonPortcullis || type == TileType.DungeonSecretDoor;

    private bool IsTileBlocked(TileType type, int x, int y, int z, bool canFly)
    {
        // Solid natural obstacles that fill an entire cell
        if (type == TileType.Wall || type == TileType.Tree || type == TileType.Shrub)
            return true;

        if (type == TileType.Empty && !canFly)
            return true;

        if (IsDungeonDoor(type))
        {
            var door = DoorStateProvider?.Invoke(x, y, z);
            return door == null || !door.IsOpen;
        }

        return false;
    }

    /// <summary>
    /// Check if moving from one tile to an adjacent tile is blocked by a wall edge.
    /// </summary>
    private bool IsWallBlocked(int fromX, int fromY, int fromZ, int toX, int toY, int toZ)
    {
        return WallEdges?.HasWallBetween(fromX, fromY, fromZ, toX, toY, toZ) == true;
    }

    private bool CanPassThrough(Creature mover, int x, int y, int z)
    {
        if (TacticalMap == null) return true;

        var tileType = TacticalMap.Get(x, y, z);

        if (IsTileBlocked(tileType, x, y, z, mover.CanFly))
            return false;

        var occupant = GetCreatureAt(x, y, z);
        if (occupant == null || occupant == mover)
            return true;

        // Nonhostile (same side) — always passable
        if (occupant.IsPlayer == mover.IsPlayer)
            return true;

        // Hostile — only passable when size difference is at least 2
        int sizeDiff = Math.Abs(GetSizeIndex(mover.Size) - GetSizeIndex(occupant.Size));
        return sizeDiff >= 2;
    }

    /// <summary>
    /// Check if a creature of given size can occupy the space starting at (x, y, z).
    /// Large+ creatures need multiple tiles to be available.
    /// Returns whether the creature can fit normally or by squeezing, and sets isSqueeze accordingly.
    /// </summary>
    private bool CanOccupySpace(CreatureSize size, int x, int y, int z, Creature? movingCreature = null, bool allowSqueeze = true)
    {
        if (TacticalMap == null) return true;

        var (width, height) = SizeHelper.GetSpaceInSquares(size);

        // Check all tiles the creature would occupy
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;

                var tileType = TacticalMap.Get(checkX, checkY, z);
                bool canFly = movingCreature?.CanFly == true;

                if (IsTileBlocked(tileType, checkX, checkY, z, canFly))
                {
                    // Normal fit failed — try squeezing (one size smaller) if allowed
                    if (allowSqueeze)
                    {
                        var smallerSize = SizeHelper.GetSmallerSize(size);
                        if (smallerSize.HasValue)
                        {
                            return CanOccupySpace(smallerSize.Value, x, y, z, movingCreature, allowSqueeze: false);
                        }
                    }
                    return false;
                }

                var creatureAtTile = GetCreatureAt(checkX, checkY, z);
                if (creatureAtTile != null && creatureAtTile != movingCreature)
                    return false;
            }
        }

        return true;
    }
    
    /// <summary>
    /// Determines whether a creature must squeeze to occupy the given space.
    /// </summary>
    private bool WouldRequireSqueeze(Creature creature, int x, int y, int z)
    {
        if (TacticalMap == null) return false;

        var (width, height) = SizeHelper.GetSpaceInSquares(creature.Size);

        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                var tileType = TacticalMap.Get(x + dx, y + dy, z);
                if (IsTileBlocked(tileType, x + dx, y + dy, z, creature.CanFly))
                {
                    // Normal fit would fail; if squeezing would work, this is a squeeze
                    var smallerSize = SizeHelper.GetSmallerSize(creature.Size);
                    if (smallerSize.HasValue && CanOccupySpace(smallerSize.Value, x, y, z, creature, allowSqueeze: false))
                        return true;
                    return false;
                }
            }
        }

        return false;
    }
    
    /// <summary>
    /// Check if the target space can accommodate the creature's size
    /// and calculate the effective movement cost (including squeezing penalty if applicable).
    /// Movement is independent from the action: a creature can move before acting,
    /// after acting, or split movement around their action.
    /// </summary>
    public bool CanMove(Creature creature, int targetX, int targetY, int targetZ)
    {
        if (creature.MovementRemaining <= 0)
            return false;

        // Check if the target space can accommodate the creature's size
        if (!CanOccupySpace(creature.Size, targetX, targetY, targetZ, creature))
            return false;

        var path = FindPath(creature, targetX, targetY, targetZ);
        if (path == null)
            return false;

        int totalCost = CalculatePathCost(creature, path);
        
        return totalCost <= creature.MovementRemaining;
    }
    
    /// <summary>
    /// Commands the creature to move towards a target position.
    /// In combat, it enqueues steps that are executed sequentially in the game loop.
    /// </summary>
    public void Move(Creature creature, int targetX, int targetY, int targetZ, VisionSystem? visionSystem = null, bool ignoreCost = false)
    {
        var path = FindPath(creature, targetX, targetY, targetZ);
        if (path == null)
            return;

        int movementSpent = 0;
        int remaining = ignoreCost ? int.MaxValue : creature.MovementRemaining;
        int diagonalCount = creature.DiagonalStepsTaken;

        for (int i = 1; i < path.Count; i++)
        {
            int stepCost = GetMoveCost(creature, path[i - 1], path[i], diagonalCount);

            if (movementSpent + stepCost > remaining)
                break;

            bool isDiagonal = IsDiagonalStep(path[i - 1], path[i]);
            creature.EnqueueStep(path[i].X, path[i].Y, path[i].Z, stepCost, isDiagonal);

            movementSpent += stepCost;
            if (isDiagonal)
                diagonalCount++;
        }
    }

    /// <summary>
    /// Called before a creature starts visually moving into a new tile.
    /// Triggers Opportunity Attacks.
    /// </summary>
    public void OnStepStarting(Creature mover, MovementStep step, VisionSystem? visionSystem = null)
    {
        if (mover.IsDisengaged) return;

        var from = new TacticalMapNode(mover.X, mover.Y, mover.Z);
        var to = new TacticalMapNode(step.X, step.Y, step.Z);
        CheckOpportunityAttacks(mover, from, to, visionSystem);
    }

    /// <summary>
    /// Called after a creature has visually reached its next tile.
    /// Updates logical position, deducts movement cost, and triggers tile effects.
    /// </summary>
    public void OnStepFinished(Creature mover, MovementStep step)
    {
        mover.X = step.X;
        mover.Y = step.Y;
        mover.Z = step.Z;

        if (mover.MovementRemaining >= step.Cost)
            mover.MovementRemaining -= step.Cost;
        else
            mover.MovementRemaining = 0;

        if (step.IsDiagonal)
            mover.DiagonalStepsTaken++;

        // Ball bearings check
        if (TacticalMap != null && TacticalMap.Get(mover.X, mover.Y, mover.Z) == TileType.BallBearings)
        {
            if (!CheckBallBearingsSave(mover))
            {
                mover.InterruptMovement();
            }
        }

        // Squeezing check
        mover.IsSqueezingThrough = WouldRequireSqueeze(mover, mover.X, mover.Y, mover.Z);
    }

    /// <summary>
    /// Checks whether any hostile creature loses melee reach as <paramref name="mover"/> steps
    /// from <paramref name="from"/> to <paramref name="to"/>, and triggers an opportunity attack
    /// for each such creature that still has its reaction.
    /// </summary>
    public void CheckOpportunityAttacks(Creature mover, TacticalMapNode from, TacticalMapNode to, VisionSystem? visionSystem)
    {
        foreach (var hostile in _combatants.ToList())
        {
            if (!mover.IsAlive() || mover.Conditions.HasCondition(Condition.Incapacitated) || mover.Conditions.HasCondition(Condition.Unconscious)) break;
            if (hostile.IsPlayer == mover.IsPlayer) continue;
            if (!hostile.IsAlive()) continue;
            if (!hostile.HasReaction) continue;
            if (!hostile.IsMeleeAttack) continue;
            if (hostile.Conditions.HasCondition(Condition.Incapacitated)) continue;
            if (hostile.Conditions.HasCondition(Condition.Unconscious)) continue;

            // PHB "Opportunity Attacks": you can only react to a creature you can see.
            bool hostileCanSeeMover = visionSystem != null
                ? visionSystem.CanSee(hostile, mover)
                : !hostile.IsBlinded() && !mover.Conditions.HasCondition(Condition.Invisible);
            if (!hostileCanSeeMover) continue;

            bool inRangeBefore = IsInMeleeRangeAt(mover.Size, from.X, from.Y, from.Z, hostile, hostile.IsReachWeapon ? 2 : 1);
            bool inRangeAfter  = IsInMeleeRangeAt(mover.Size, to.X,   to.Y,   to.Z,   hostile, hostile.IsReachWeapon ? 2 : 1);

            if (inRangeBefore && !inRangeAfter)
            {
                var oaResult = MakeOpportunityAttack(hostile, mover, visionSystem);
                TurnMessages.Add($"[OA] {oaResult.GetMessage()}");
            }
        }
    }

    /// <summary>
    /// Returns true when a creature of <paramref name="moverSize"/> positioned at
    /// (<paramref name="moverX"/>, <paramref name="moverY"/>, <paramref name="moverZ"/>)
    /// is within melee reach of <paramref name="target"/>.
    /// </summary>
    private static bool IsInMeleeRangeAt(CreatureSize moverSize, int moverX, int moverY, int moverZ, Creature target, int reach = 1)
    {
        var (moverW, moverH)   = SizeHelper.GetSpaceInSquares(moverSize);
        var (targetW, targetH) = SizeHelper.GetSpaceInSquares(target.Size);

        for (int ax = 0; ax < moverW; ax++)
        for (int ay = 0; ay < moverH; ay++)
        {
            for (int tx = 0; tx < targetW; tx++)
            for (int ty = 0; ty < targetH; ty++)
            {
                int dx = Math.Abs((moverX + ax) - (target.X + tx));
                int dy = Math.Abs((moverY + ay) - (target.Y + ty));
                int dz = Math.Abs(moverZ - target.Z);
                if (dx <= reach && dy <= reach && dz <= reach && (dx + dy + dz) > 0)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Makes an opportunity attack using the attacker's reaction (not their action).
    /// Triggered when a creature voluntarily leaves the attacker's melee reach
    /// without having taken the Disengage action.
    /// </summary>
    public AttackResult MakeOpportunityAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult { Attacker = attacker, Target = target };

        if (!attacker.HasReaction || !attacker.IsMeleeAttack)
        {
            result.IsHit = false;
            return result;
        }

        // Total cover: cannot be targeted directly (PHB "Cover")
        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add($"{target.Name} has total cover and cannot be targeted by the opportunity attack.");
            return result;
        }

        attacker.HasReaction = false;

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(attacker, target)
            : !attacker.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage    = target.Conditions.HasCondition(Condition.Prone)        ||
                               target.Conditions.HasCondition(Condition.Paralyzed)    ||
                               target.Conditions.HasCondition(Condition.Unconscious)  ||
                               target.IsSqueezingThrough ||
                               attacker.IsHidden ||           // Unseen attacker (hidden via stealth)
                               attacker.Conditions.HasCondition(Condition.Invisible); // Unseen attacker (Invisible condition)
        bool hasDisadvantage = !attackerCanSee || attacker.IsSqueezingThrough;

        // Dodge: attacker has disadvantage if the dodging target can see the attacker
        if (target.IsDodging && !target.Conditions.HasCondition(Condition.Incapacitated) && target.Speed > 0)
        {
            bool targetCanSeeAttacker = visionSystem != null
                ? visionSystem.CanSee(target, attacker)
                : !target.IsBlinded() && !attacker.Conditions.HasCondition(Condition.Invisible);
            if (targetCanSeeAttacker)
                hasDisadvantage = true;
        }

        if (visionSystem != null && attacker.HasSunlightSensitivity)
        {
            var lightLevel = visionSystem.GetLightLevel(attacker.X, attacker.Y, attacker.Z);
            if (lightLevel == LightType.Bright)
                hasDisadvantage = true;
        }

        if (attacker.HasPackTactics)
        {
            bool allyNearTarget = _combatants.Any(c =>
                c != attacker &&
                c.IsPlayer == attacker.IsPlayer &&
                c.IsAlive() &&
                !c.Conditions.HasCondition(Condition.Incapacitated) &&
                DndMath.CalculateDistance(c.X, c.Y, c.Z, target.X, target.Y, target.Z) <= 1);
            if (allyNearTarget) hasAdvantage = true;
        }

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName, attacker.AttackBonus, target.ArmorClass + DndMath.GetCoverBonus(target.Cover),
            hasAdvantage, hasDisadvantage, circumstantialBonus: 0);

        result.AttackRoll       = attackCheck.DieRoll;
        result.TotalAttackBonus = attackCheck.BaseModifier;
        result.TotalToHit       = attackCheck.Total;
        result.HasAdvantage     = attackCheck.HasAdvantage;
        result.HasDisadvantage  = attackCheck.HasDisadvantage;
        result.IsCritical       = attackCheck.IsCriticalHit;
        result.IsCriticalMiss   = attackCheck.IsCriticalMiss;
        result.IsHit            = attackCheck.Success;

        if (result.IsHit)
        {
            int damageBonus = attacker.DamageBonus;
            if (attacker.IsRaging && attacker.IsMeleeAttack)
                damageBonus += attacker.RageDamageBonus;

            result.Damage     = RollDamage(attacker.DamageDice, damageBonus, result.IsCritical);
            result.DamageType = attacker.CurrentDamageType;
            target.TakeDamage(result.Damage, result.DamageType, result.IsCritical, attacker.IsSilveredAttack);
            attacker.HasAttackedThisRound = true;
        }

        return result;
    }

    /// <summary>
    /// Gets a path for movement without applying movement cost/action rules.
    /// Used by exploration mode to animate movement tile-by-tile.
    /// </summary>
    public List<(int x, int y, int z)>? GetPath(Creature creature, int targetX, int targetY, int targetZ)
    {
        var path = FindPath(creature, targetX, targetY, targetZ);
        if (path == null)
            return null;

        return path.Select(n => (n.X, n.Y, n.Z)).ToList();
    }

    /// <summary>
    /// Gets all map positions reachable by the creature with its remaining movement.
    /// The start tile is excluded from the result.
    /// </summary>
    public HashSet<(int x, int y, int z)> GetReachablePositions(Creature creature)
    {
        var reachable = new HashSet<(int x, int y, int z)>();

        if (creature.MovementRemaining <= 0)
            return reachable;

        var start = new TacticalMapNode(creature.X, creature.Y, creature.Z);
        int startDiagParity = creature.DiagonalStepsTaken % 2;
        var bestCost = new Dictionary<(TacticalMapNode, int), int> { [(start, startDiagParity)] = 0 };
        var open = new PriorityQueue<(TacticalMapNode, int), int>();
        open.Enqueue((start, startDiagParity), 0);

        while (open.Count > 0)
        {
            var (current, diagParity) = open.Dequeue();
            int currentCost = bestCost.GetValueOrDefault((current, diagParity), int.MaxValue);

            foreach (var neighbor in GetNeighbors(creature, current))
            {
                bool isDiag = IsDiagonalStep(current, neighbor);
                int stepCost = GetMoveCost(creature, current, neighbor, diagParity);
                int totalCost = currentCost + stepCost;

                if (totalCost > creature.MovementRemaining)
                    continue;

                int newDiagParity = isDiag ? 1 - diagParity : diagParity;
                var neighborState = (neighbor, newDiagParity);

                int knownCost = bestCost.GetValueOrDefault(neighborState, int.MaxValue);
                if (totalCost >= knownCost)
                    continue;

                bestCost[neighborState] = totalCost;
                open.Enqueue(neighborState, totalCost);
                reachable.Add((neighbor.X, neighbor.Y, neighbor.Z));
            }
        }

        return reachable;
    }

    public (int x, int y, int z)? GetNextStepTowards(Creature creature, Creature target)
    {
        var bestPath = FindPathToAdjacent(creature, target);

        // Path with only 1 node means creature is already adjacent to the target
        if (bestPath == null || bestPath.Count < 2)
            return null;

        var step = bestPath[1];
        return (step.X, step.Y, step.Z);
    }

    /// <summary>
    /// Returns the farthest tile along the path towards the target that the creature
    /// can reach within its remaining movement budget. Unlike <see cref="GetNextStepTowards"/>
    /// which returns only the immediate next step, this allows the AI to use its full movement.
    /// </summary>
    public (int x, int y, int z)? GetMoveDestinationTowards(Creature creature, Creature target)
    {
        var bestPath = FindPathToAdjacent(creature, target);

        if (bestPath == null || bestPath.Count < 2)
            return null;

        int movementBudget = creature.MovementRemaining;
        int movementSpent = 0;
        int diagonalCount = creature.DiagonalStepsTaken;
        int lastReachable = 0;

        for (int i = 1; i < bestPath.Count; i++)
        {
            int stepCost = GetMoveCost(creature, bestPath[i - 1], bestPath[i], diagonalCount);
            if (movementSpent + stepCost > movementBudget)
                break;

            movementSpent += stepCost;
            if (IsDiagonalStep(bestPath[i - 1], bestPath[i]))
                diagonalCount++;
            lastReachable = i;
        }

        if (lastReachable == 0)
            return null;

        var dest = bestPath[lastReachable];
        return (dest.X, dest.Y, dest.Z);
    }

    /// <summary>
    /// Commands the creature to move away from a target position.
    /// In combat, it enqueues steps that are executed sequentially in the game loop.
    /// </summary>
    public void MoveAway(Creature creature, int targetX, int targetY, int targetZ, VisionSystem? visionSystem = null, bool ignoreCost = false)
    {
        var path = FindPath(creature, targetX, targetY, targetZ);
        if (path == null)
            return;

        int movementSpent = 0;
        int remaining = ignoreCost ? int.MaxValue : creature.MovementRemaining;
        int diagonalCount = creature.DiagonalStepsTaken;

        for (int i = path.Count - 1; i > 0; i--)
        {
            int stepCost = GetMoveCost(creature, path[i], path[i - 1], diagonalCount);

            if (movementSpent + stepCost > remaining)
                break;

            bool isDiagonal = IsDiagonalStep(path[i], path[i - 1]);
            creature.EnqueueStep(path[i - 1].X, path[i - 1].Y, path[i - 1].Z, stepCost, isDiagonal);

            movementSpent += stepCost;
            if (isDiagonal)
                diagonalCount++;
        }
    }

    /// <summary>
    /// Called after a creature has visually moved to a new tile.
    /// Updates logical position, deducts movement cost, and triggers tile effects.
    /// Also handles movement interruptions (falling, obstacles, etc.).
    /// </summary>
    public void OnMoveFinished(Creature mover, MovementStep step, VisionSystem? visionSystem = null)
    {
        mover.X = step.X;
        mover.Y = step.Y;
        mover.Z = step.Z;

        if (mover.MovementRemaining >= step.Cost)
            mover.MovementRemaining -= step.Cost;
        else
            mover.MovementRemaining = 0;

        if (step.IsDiagonal)
            mover.DiagonalStepsTaken++;

        // Ball bearings check
        if (TacticalMap != null && TacticalMap.Get(mover.X, mover.Y, mover.Z) == TileType.BallBearings)
        {
            if (!CheckBallBearingsSave(mover))
            {
                mover.InterruptMovement();
            }
        }

        // Squeezing check
        mover.IsSqueezingThrough = WouldRequireSqueeze(mover, mover.X, mover.Y, mover.Z);

        // Fall check: if the creature's speed is 0 due to conditions (grapple, paralysis, etc.),
        // non-hovering flyers will fall (PHB "Flying Movement").
        CheckFlyingFall(mover);
    }

    /// <summary>
    /// Checks whether a creature can see a target considering obstacles, range, and effects
    /// like blindness or invisibility.
    /// </summary>
    public bool CanSee(Creature creature, Creature target, VisionSystem? visionSystem)
    {
        if (target.Conditions.HasCondition(Condition.Invisible))
            return false; // Targets you cannot see

        if (creature.IsBlinded())
            return false; // You cannot see when you are blinded

        // Check line of sight: no intervening obstacles
        if (visionSystem != null)
            return visionSystem.CanSee(creature, target);

        // Fallback: creatures in the same space are considered visible to each other
        if (creature.X == target.X && creature.Y == target.Y && creature.Z == target.Z)
            return true;

        return false;
    }

    /// <summary>
    /// Rolls a D20 for an attack, ability check, or saving throw with optional advantage or disadvantage.
    /// </summary>
    public int RollD20(float advantageMultiplier = 1.0f, float disadvantageMultiplier = 1.0f)
    {
        float roll = _random.Next(1, 21);
        roll += advantageMultiplier * (RollD20Baseline() - 10);
        roll -= disadvantageMultiplier * (RollD20Baseline() - 10);
        return (int)Clamp(roll, 1, 20);
    }

    private float RollD20Baseline()
    {
        // Roll a D20 with a Gaussian distribution, bell curve around 10.5
        // Most rolls will be around the middle, fewer at the extremes
        double u1 = _random.NextDouble();
        double u2 = _random.NextDouble();
        double gaussianRoll = Sqrt(-2.0 * Log(u1)) * Cos(2.0 * PI * u2);
        double adjustedRoll = 10.5 + gaussianRoll * 4; // Adjust mean to 10.5, stretch to fit 1-20
        return (float)Clamp(adjustedRoll, 1, 20);
    }

    /// <summary>
    /// Rolls weapon damage, applying critical hit rules (PHB "Critical Hits").
    /// On a critical hit all damage dice — including any extra dice from features such as
    /// Sneak Attack or Divine Smite — are rolled twice; modifiers are added only once.
    /// </summary>
    /// <param name="damageDice">Weapon dice notation, e.g. "1d6" or "2d4".</param>
    /// <param name="bonus">Flat modifier added once regardless of critical hit status.</param>
    /// <param name="isCritical">True when a natural 20 was rolled on the attack roll.</param>
    /// <param name="extraDamageDice">
    /// Optional extra dice that are also doubled on a critical hit (e.g. Sneak Attack "2d6",
    /// Divine Smite "2d8"). Pass null or empty string when no extra dice apply.
    /// </param>
    public int RollDamage(string damageDice, int bonus, bool isCritical, string? extraDamageDice = null)
    {
        int total = bonus;
        int rolls = isCritical ? 2 : 1;

        // Roll the main weapon dice (always doubled on a crit)
        if (int.TryParse(damageDice, out int fixedDamage))
        {
            // Fixed-value weapon damage: no dice to double, just add the value
            total += fixedDamage;
        }
        else
        {
            var parts = damageDice.Split('d');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int numDice) &&
                int.TryParse(parts[1], out int diceType))
            {
                for (int i = 0; i < numDice * rolls; i++)
                    total += RollD(diceType);
            }
        }

        // Roll extra damage dice — also doubled on a critical hit (PHB "Critical Hits")
        if (!string.IsNullOrEmpty(extraDamageDice))
        {
            var extraParts = extraDamageDice.Split('d');
            if (extraParts.Length == 2 &&
                int.TryParse(extraParts[0], out int extraNum) &&
                int.TryParse(extraParts[1], out int extraType))
            {
                for (int i = 0; i < extraNum * rolls; i++)
                    total += RollD(extraType);
            }
        }

        return total;
    }

    /// <summary>
    /// Resolves a damaging area spell following PHB "Damage Rolls": damage is rolled once and
    /// applied to every creature within the area. Each creature makes a saving throw; on a success
    /// it takes half damage (or none, depending on <see cref="Spell.HalfDamageOnSave"/>).
    /// Creatures with total cover are not affected (PHB "Cover").
    /// </summary>
    /// <param name="caster">The creature casting the spell.</param>
    /// <param name="spell">The spell being cast (must have <see cref="Spell.DamageDice"/> set).</param>
    /// <param name="targetX">Grid X of the spell's point of origin.</param>
    /// <param name="targetY">Grid Y of the spell's point of origin.</param>
    /// <param name="targetZ">Grid Z of the spell's point of origin.</param>
    public SpellResult CastAreaSpell(Creature caster, Spell spell, int targetX, int targetY, int targetZ = 0)
    {
        var result = new SpellResult
        {
            Caster = caster,
            SpellName = spell.Name,
            DamageType = spell.DamageType
        };

        if (_inCombat && !caster.HasAction)
            return result;

        if (_inCombat)
            caster.HasAction = false;

        // PHB "Damage Rolls": roll damage once for all targets
        result.DamageRolled = RollDamage(spell.DamageDice, 0, false);

        int radiusTiles = spell.AreaRadiusFeet / 5;

        foreach (var creature in _combatants.Where(c => c.IsAlive()))
        {
            int dist = DndMath.CalculateDistance(creature.X, creature.Y, creature.Z, targetX, targetY, targetZ);
            if (dist > radiusTiles) continue;

            // PHB "Cover": total cover prevents targeting by spells
            if (creature.Cover == CoverType.Total) continue;

            int damage = result.DamageRolled;
            bool saved = false;

            if (!string.IsNullOrEmpty(spell.SaveAbility))
            {
                var saveCheck = MakeSavingThrow(creature, spell.SaveAbility, spell.SaveDC);
                saved = saveCheck.Success;
                if (saved)
                    damage = spell.HalfDamageOnSave ? DndMath.Half(damage) : 0;
            }

            creature.TakeDamage(damage, spell.DamageType);
            result.TargetResults.Add((creature, saved, damage));
        }

        TurnMessages.Add(result.GetMessage());
        return result;
    }

    private int RollD(int sides)
    {
        if (sides <= 0) return 0;
        return _random.Next(1, sides + 1);
    }

    /// <summary>
    /// Make a saving throw for a creature, automatically applying squeezing disadvantage
    /// on Dexterity saving throws per the squeezing rules.
    /// </summary>
    public D20Check MakeSavingThrow(Creature creature, string abilityName, int dc, bool hasAdvantage = false, bool hasDisadvantage = false)
    {
        // Squeezing: disadvantage on Dexterity saving throws
        bool isDexSave = abilityName is "DEX" or "Dexterity";
        if (creature.IsSqueezingThrough && isDexSave)
            hasDisadvantage = true;

        // Dodge: advantage on Dexterity saving throws while the benefit is active
        if (creature.IsDodging && isDexSave && !creature.Conditions.HasCondition(Condition.Incapacitated) && creature.Speed > 0)
            hasAdvantage = true;

        return creature.MakeSavingThrow(abilityName, dc, hasAdvantage, hasDisadvantage);
    }
    
    public Creature? GetCreatureAt(int x, int y, int z = 0)
    {
        foreach (var creature in _combatants)
        {
            if (!creature.IsAlive()) continue;
            
            var (width, height) = SizeHelper.GetSpaceInSquares(creature.Size);
            
            // Check if (x, y) is within the creature's occupied space
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    if (creature.X + dx == x && creature.Y + dy == y && creature.Z == z)
                    {
                        return creature;
                    }
                }
            }
        }
        
        return null;
    }
    
    public bool IsInMeleeRange(Creature attacker, Creature target)
    {
        // Get the size of both creatures in squares
        var (attackerWidth, attackerHeight) = SizeHelper.GetSpaceInSquares(attacker.Size);
        var (targetWidth, targetHeight) = SizeHelper.GetSpaceInSquares(target.Size);

        // Reach weapons (PHB "Reach") extend melee reach by 5 feet (1 square).
        int reach = attacker.IsReachWeapon ? 2 : 1;

        // Check if any square occupied by the attacker is within reach of any square occupied by the target
        for (int ax = 0; ax < attackerWidth; ax++)
        for (int ay = 0; ay < attackerHeight; ay++)
        {
            int attackerTileX = attacker.X + ax;
            int attackerTileY = attacker.Y + ay;

            for (int tx = 0; tx < targetWidth; tx++)
            for (int ty = 0; ty < targetHeight; ty++)
            {
                int targetTileX = target.X + tx;
                int targetTileY = target.Y + ty;

                int dx = Math.Abs(attackerTileX - targetTileX);
                int dy = Math.Abs(attackerTileY - targetTileY);
                int dz = Math.Abs(attacker.Z - target.Z);

                if (dx <= reach && dy <= reach && dz <= reach && (dx + dy + dz) > 0)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    public (int x, int y, int z)? FindNearestEnemy(Creature creature)
    {
        Creature? nearest = null;
        int minDist = int.MaxValue;
        
        foreach (var other in _combatants)
        {
            if (other.IsPlayer == creature.IsPlayer || !other.IsAlive()) continue;
            
            int dist = DndMath.CalculateDistance(creature.X, creature.Y, creature.Z, other.X, other.Y, other.Z);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = other;
            }
        }
        
        return nearest != null ? (nearest.X, nearest.Y, nearest.Z) : null;
    }

    public void StartRage(Creature creature)
    {
        if (creature.RagesRemaining <= 0 && creature.IsPlayer)
            return;

        if (creature.IsRaging)
            return;

        creature.IsRaging = true;
        creature.RageTurnsLeft = 10;
        if (creature.RagesRemaining > 0)
            creature.RagesRemaining--;

        TurnMessages.Add($"{creature.Name} enters a RAGE!");
    }

    /// <summary>
    /// Interacts with one object as a free action (PHB "Other Activity on Your Turn").
    /// The first interaction each turn is free; a second one costs the creature's action.
    /// </summary>
    /// <param name="creature">The creature performing the interaction.</param>
    /// <returns>True if the interaction was performed; false if neither the free slot nor an action was available.</returns>
    public bool UseObjectInteraction(Creature creature)
    {
        if (!_inCombat)
            return true;

        if (creature.HasFreeObjectInteraction)
        {
            creature.HasFreeObjectInteraction = false;
            return true;
        }

        if (!creature.HasAction)
            return false;

        creature.HasAction = false;
        TurnMessages.Add($"{creature.Name} uses their action to interact with a second object.");
        return true;
    }

    /// <summary>
    /// Takes the Dash action: the creature expends its action to gain extra movement
    /// equal to its Speed for the current turn (after any modifiers).
    /// </summary>
    /// <param name="creature">The creature taking the Dash action.</param>
    /// <returns>True if the Dash was successfully taken; false if no action is available.</returns>
    public bool Dash(Creature creature)
    {
        if (_inCombat && !creature.HasAction)
            return false;

        if (_inCombat)
            creature.HasAction = false;

        creature.MovementRemaining += creature.Speed;
        TurnMessages.Add($"{creature.Name} uses Dash! (+{creature.Speed}ft movement)");
        return true;
    }

    /// <summary>
    /// Drops the creature prone. This is free and requires no movement or action.
    /// Flying creatures that cannot hover fall to the ground instead (PHB "Flying Movement"):
    /// a non-hovering flyer that is knocked prone loses the ability to stay aloft and falls.
    /// </summary>
    public void DropProne(Creature creature)
    {
        if (creature.IsFlying && !creature.CanHover)
        {
            Fall(creature);
            return;
        }

        creature.Conditions = creature.Conditions.AddCondition(Condition.Prone);
        TurnMessages.Add($"{creature.Name} drops prone.");
    }

    /// <summary>
    /// Causes a flying creature to fall to the ground, taking falling damage (1d6 per 10 ft)
    /// and landing prone (PHB "Flying Movement").
    /// Called when a non-hovering flyer is knocked prone or deprived of movement.
    /// </summary>
    private void Fall(Creature creature)
    {
        int feetFallen = creature.Z * 5;
        creature.Z = 0;
        creature.IsFlying = false;

        TurnMessages.Add(feetFallen > 0
            ? $"{creature.Name} falls {feetFallen} ft.!"
            : $"{creature.Name} falls!");

        if (feetFallen > 0)
        {
            int damage = creature.TakeFallDamage(feetFallen);
            if (damage > 0)
                TurnMessages.Add($"{creature.Name} takes {damage} bludgeoning damage from the fall.");
        }

        // Always land prone regardless of damage dealt
        creature.Conditions = creature.Conditions.AddCondition(Condition.Prone);
    }

    /// <summary>
    /// Checks whether a flying creature needs to fall because its speed has been reduced to 0
    /// by a condition (Grappled, Paralyzed, Restrained, Stunned, Unconscious) or another effect.
    /// Non-hovering flying creatures fall in these circumstances (PHB "Flying Movement").
    /// Call this whenever a creature gains one of those conditions while airborne.
    /// </summary>
    public void CheckFlyingFall(Creature creature)
    {
        if (!creature.IsFlying || creature.CanHover)
            return;

        bool speedSuppressed = creature.Speed == 0 ||
                               creature.Conditions.HasCondition(Condition.Grappled)    ||
                               creature.Conditions.HasCondition(Condition.Paralyzed)   ||
                               creature.Conditions.HasCondition(Condition.Restrained)  ||
                               creature.Conditions.HasCondition(Condition.Stunned)     ||
                               creature.Conditions.HasCondition(Condition.Unconscious);

        if (speedSuppressed)
            Fall(creature);
    }

    /// <summary>
    /// Stands up from prone, spending movement equal to half the creature's speed.
    /// Cannot stand up if movement remaining is less than half speed, or if speed is 0.
    /// </summary>
    /// <returns>True if the creature stood up; false if it lacks the movement to do so.</returns>
    public bool StandUp(Creature creature)
    {
        if (!creature.Conditions.HasCondition(Condition.Prone))
            return false;

        if (_inCombat)
        {
            if (creature.Speed == 0 || creature.MovementRemaining < creature.Speed / 2)
                return false;

            creature.MovementRemaining -= creature.Speed / 2;
        }

        creature.Conditions = creature.Conditions.RemoveCondition(Condition.Prone);
        TurnMessages.Add($"{creature.Name} stands up.");
        return true;
    }

    /// <summary>
    /// Takes the Disengage action: the creature's movement no longer provokes opportunity
    /// attacks for the rest of the current turn.
    /// Nimble Escape allows taking this as a bonus action (<paramref name="isBonusAction"/> = true).
    /// </summary>
    /// <returns>True if the action was successfully taken; false if the required resource is unavailable.</returns>
    public bool Disengage(Creature creature, bool isBonusAction = false)
    {
        if (_inCombat)
        {
            if (isBonusAction)
            {
                if (!creature.HasBonusAction) return false;
                creature.HasBonusAction = false;
            }
            else
            {
                if (!creature.HasAction) return false;
                creature.HasAction = false;
            }
        }

        creature.IsDisengaged = true;
        TurnMessages.Add($"{creature.Name} takes the Disengage action.");
        return true;
    }

    /// <summary>
    /// Takes the Dodge action: until the start of the creature's next turn, attack rolls against it
    /// have disadvantage if it can see the attacker, and it makes Dexterity saving throws with advantage.
    /// This benefit is lost if the creature is incapacitated or its speed drops to 0.
    /// </summary>
    /// <returns>True if the Dodge was successfully taken; false if no action is available.</returns>
    public bool Dodge(Creature creature)
    {
        if (_inCombat && !creature.HasAction)
            return false;

        if (_inCombat)
            creature.HasAction = false;

        creature.IsDodging = true;
        TurnMessages.Add($"{creature.Name} takes the Dodge action.");
        return true;
    }

    /// <summary>
    /// Performs a Long Jump (PHB "Jumping", p.182).
    /// <para>
    /// With a running start (at least 10 ft of movement spent immediately before the jump),
    /// the creature covers a number of feet up to its <b>Strength score</b>.
    /// Without a running start it can only leap half that distance.
    /// Either way, each foot cleared costs one foot of movement.
    /// </para>
    /// <para>
    /// <b>Obstacle:</b> If <paramref name="mustClearObstacle"/> is true the creature must succeed
    /// on a DC 10 Strength (Athletics) check; on a failure it hits the obstacle.
    /// </para>
    /// <para>
    /// <b>Difficult terrain landing:</b> If <paramref name="landInDifficultTerrain"/> is true the
    /// creature must succeed on a DC 10 Dexterity (Acrobatics) check; on a failure it lands prone.
    /// </para>
    /// </summary>
    /// <param name="creature">The creature attempting the jump.</param>
    /// <param name="hasRunningStart">
    /// True if the creature moved at least 10 ft immediately before the jump (default: true).
    /// </param>
    /// <param name="mustClearObstacle">
    /// True if there is a low obstacle (no taller than ¼ the jump distance) to clear.
    /// </param>
    /// <param name="landInDifficultTerrain">
    /// True if the landing square is difficult terrain, requiring an Acrobatics check to stay on feet.
    /// </param>
    /// <returns>
    /// A <see cref="LongJumpResult"/> describing the outcome: distance jumped, movement spent,
    /// whether the obstacle was cleared, and whether the creature landed on its feet.
    /// </returns>
    public LongJumpResult LongJump(Creature creature, bool hasRunningStart = true, bool mustClearObstacle = false, bool landInDifficultTerrain = false)
    {
        var result = new LongJumpResult { Creature = creature };

        // Max distance in feet: Strength score (running) or half (standing)
        int maxDistanceFt = hasRunningStart ? creature.Strength : creature.Strength / 2;
        maxDistanceFt = Math.Max(1, maxDistanceFt);

        // Each foot of the jump costs one foot of movement (PHB p.182)
        int movementCostFt = maxDistanceFt;

        if (_inCombat && creature.MovementRemaining < movementCostFt)
        {
            // Clamp the jump distance to what movement remains
            movementCostFt = creature.MovementRemaining;
            maxDistanceFt = movementCostFt;
        }

        if (maxDistanceFt <= 0)
        {
            TurnMessages.Add($"{creature.Name} has no movement left to jump!");
            result.DistanceFt = 0;
            result.MovementSpentFt = 0;
            result.ClearedObstacle = false;
            result.LandedOnFeet = true;
            return result;
        }

        // Spend the movement
        if (_inCombat)
            creature.MovementRemaining = Math.Max(0, creature.MovementRemaining - movementCostFt);

        result.DistanceFt = maxDistanceFt;
        result.MovementSpentFt = movementCostFt;
        result.HasRunningStart = hasRunningStart;

        string runText = hasRunningStart ? "running" : "standing";
        TurnMessages.Add($"{creature.Name} attempts a {runText} long jump ({maxDistanceFt} ft, STR {creature.Strength}).");

        // Obstacle check: DC 10 Strength (Athletics) to clear a low obstacle (PHB p.182)
        result.ClearedObstacle = true;
        if (mustClearObstacle)
        {
            int athleticsBonus = DndMath.GetAbilityModifier(creature.Strength);
            int roll = _random.Next(1, 21);
            int total = roll + athleticsBonus;
            bool cleared = DndMath.MeetsDC(total, 10);
            result.ClearedObstacle = cleared;
            result.AthleticsRoll = total;

            if (cleared)
                TurnMessages.Add($"{creature.Name} clears the obstacle! (Athletics {roll}+{athleticsBonus}={total} vs DC 10)");
            else
                TurnMessages.Add($"{creature.Name} hits the obstacle! (Athletics {roll}+{athleticsBonus}={total} vs DC 10)");
        }

        // Difficult terrain landing: DC 10 Dexterity (Acrobatics) or land prone (PHB p.182)
        result.LandedOnFeet = true;
        if (landInDifficultTerrain)
        {
            int acrobaticsBonus = DndMath.GetAbilityModifier(creature.Dexterity);
            int roll = _random.Next(1, 21);
            int total = roll + acrobaticsBonus;
            bool onFeet = DndMath.MeetsDC(total, 10);
            result.LandedOnFeet = onFeet;
            result.AcrobaticsRoll = total;

            if (onFeet)
                TurnMessages.Add($"{creature.Name} lands on their feet in difficult terrain. (Acrobatics {roll}+{acrobaticsBonus}={total} vs DC 10)");
            else
            {
                creature.Conditions = creature.Conditions.AddCondition(Condition.Prone);
                TurnMessages.Add($"{creature.Name} lands prone in difficult terrain! (Acrobatics {roll}+{acrobaticsBonus}={total} vs DC 10)");
            }
        }

        return result;
    }

    private void EndRage(Creature creature)
    {
        creature.IsRaging = false;
        creature.RageTurnsLeft = 0;
    }

    /// <summary>
    /// Takes the Help action: distracts a nearby enemy so that the next ally attack against it
    /// has advantage. The target must be within 5 feet (1 tile) of the helper.
    /// </summary>
    /// <returns>True if the action was successfully taken; false if the required resource is unavailable or the target is out of range.</returns>
    public bool Help(Creature helper, Creature target)
    {
        if (_inCombat && !helper.HasAction) return false;
        if (!target.IsAlive()) return false;

        if (!IsInMeleeRange(helper, target))
        {
            TurnMessages.Add($"{helper.Name} cannot Help — {target.Name} is not within 5 feet!");
            return false;
        }

        // Help against enemy
        if (helper.IsPlayer != target.IsPlayer)
        {
            if (_inCombat)
                helper.HasAction = false;
            target.IsBeingHelped = true;
            TurnMessages.Add($"{helper.Name} uses Help to distract {target.Name}! Next attack against {target.Name} has advantage.");
            return true;
        }
        else // Help ally
        {
            if (target.CurrentDonDoffProcess != null && target.CurrentDonDoffProcess.IsActive)
            {
                if (_inCombat)
                    helper.HasAction = false;

                var process = target.CurrentDonDoffProcess;
                // Reduce remaining time by 1 minute (10 rounds)
                int reduction = 10;
                process.RoundsRemaining = Math.Max(0, process.RoundsRemaining - reduction);
                process.MinutesRemaining = Math.Max(0, process.MinutesRemaining - 1.0);

                if (process.RoundsRemaining <= 0 || process.MinutesRemaining <= 0)
                {
                    process.IsActive = false;
                }

                TurnMessages.Add(Loc.Tr("{0} helps {1} with armor!", helper.Name, target.Name));
                return true;
            }
            else
            {
                TurnMessages.Add($"{target.Name} doesn't need help right now.");
                return false;
            }
        }
    }

    /// <summary>
    /// Throws an acid vial at the target as an action (PHB "Adventuring Gear: Acid").
    /// Range: 20 ft. Treated as an improvised ranged weapon: DEX modifier, no proficiency bonus.
    /// On a hit, the target takes 2d6 acid damage. The vial is consumed regardless of the outcome.
    /// </summary>
    /// <param name="thrower">The creature throwing the vial.</param>
    /// <param name="target">The target creature.</param>
    /// <param name="character">The character whose inventory will lose the vial.</param>
    /// <param name="visionSystem">Optional vision system for line-of-sight checks.</param>
    /// <returns>An <see cref="AttackResult"/> describing the outcome.</returns>
    public AttackResult ThrowAcid(Creature thrower, Creature target, Character character, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult { Attacker = thrower, Target = target };

        if (_inCombat && !thrower.HasAction)
        {
            result.IsHit = false;
            return result;
        }

        // Range check: 20 ft = 4 squares
        int distanceSquares = DndMath.CalculateDistance(thrower.X, thrower.Y, thrower.Z, target.X, target.Y, target.Z);
        if (distanceSquares > 4)
        {
            result.IsHit = false;
            TurnMessages.Add(Loc.Tr("{0} cannot throw the acid that far — maximum range is 20 ft.", thrower.Name));
            return result;
        }

        // Total cover: cannot be targeted
        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add(Loc.Tr("{0} has total cover and cannot be targeted.", target.Name));
            return result;
        }

        // Consume the vial regardless of hit or miss
        character.InventoryData.RemoveItem("Acid (vial)");

        if (_inCombat)
            thrower.HasAction = false;

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(thrower, target)
            : !thrower.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage = target.Conditions.HasCondition(Condition.Prone) ||
                            target.Conditions.HasCondition(Condition.Paralyzed) ||
                            target.Conditions.HasCondition(Condition.Unconscious) ||
                            target.Conditions.HasCondition(Condition.Restrained) ||
                            target.IsSqueezingThrough ||
                            thrower.IsHidden ||
                            thrower.Conditions.HasCondition(Condition.Invisible);
        bool hasDisadvantage = !attackerCanSee ||
                               thrower.IsSqueezingThrough ||
                               thrower.Conditions.HasCondition(Condition.Restrained) ||
                               thrower.HasArmorNonProficientBonus;

        if (target.IsDodging && !target.Conditions.HasCondition(Condition.Incapacitated) && target.Speed > 0)
        {
            bool targetCanSeeAttacker = visionSystem != null
                ? visionSystem.CanSee(target, thrower)
                : !target.IsBlinded() && !thrower.Conditions.HasCondition(Condition.Invisible);
            if (targetCanSeeAttacker)
                hasDisadvantage = true;
        }

        if (visionSystem != null && thrower.HasSunlightSensitivity)
        {
            var lightLevel = visionSystem.GetLightLevel(thrower.X, thrower.Y, thrower.Z);
            if (lightLevel == LightType.Bright)
                hasDisadvantage = true;
        }

        thrower.IsHidden = false;

        // Improvised ranged attack: DEX modifier, no proficiency bonus
        int dexMod = thrower.GetAbilityModifier(thrower.Dexterity);
        int attackBonus = dexMod;

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            "Acid (vial)",
            attackBonus,
            target.ArmorClass + DndMath.GetCoverBonus(target.Cover),
            hasAdvantage,
            hasDisadvantage,
            circumstantialBonus: 0);

        result.AttackRoll = attackCheck.DieRoll;
        result.TotalAttackBonus = attackCheck.BaseModifier;
        result.TotalToHit = attackCheck.Total;
        result.HasAdvantage = attackCheck.HasAdvantage;
        result.HasDisadvantage = attackCheck.HasDisadvantage;
        result.IsCritical = attackCheck.IsCriticalHit;
        result.IsCriticalMiss = attackCheck.IsCriticalMiss;
        result.IsHit = attackCheck.Success;
        result.IsNonProficient = true;
        result.DamageType = DamageType.Acid;

        if (result.IsHit)
        {
            result.Damage = RollDamage("2d6", 0, result.IsCritical);
            result.DamageType = DamageType.Acid;
            target.TakeDamage(result.Damage, DamageType.Acid, result.IsCritical);
            thrower.HasAttackedThisRound = true;
        }

        return result;
    }

    /// <summary>
    /// Throws a flask of alchemist's fire at the target as an action (PHB "Adventuring Gear: Alchemist's Fire").
    /// Range: 20 ft. Treated as an improvised ranged weapon: DEX modifier, no proficiency bonus.
    /// On a hit, the target gains the <see cref="Condition.Burning"/> condition and takes 1d4 fire damage
    /// at the start of each of its turns until the flames are extinguished. The flask is consumed regardless.
    /// </summary>
    /// <param name="thrower">The creature throwing the flask.</param>
    /// <param name="target">The target creature.</param>
    /// <param name="character">The character whose inventory will lose the flask.</param>
    /// <param name="visionSystem">Optional vision system for line-of-sight checks.</param>
    /// <returns>An <see cref="AttackResult"/> describing the outcome.</returns>
    public AttackResult ThrowAlchemistsFire(Creature thrower, Creature target, Character character, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult { Attacker = thrower, Target = target };

        if (_inCombat && !thrower.HasAction)
        {
            result.IsHit = false;
            return result;
        }

        // Range check: 20 ft = 4 squares
        int distanceSquares = DndMath.CalculateDistance(thrower.X, thrower.Y, thrower.Z, target.X, target.Y, target.Z);
        if (distanceSquares > 4)
        {
            result.IsHit = false;
            TurnMessages.Add(Loc.Tr("{0} cannot throw the alchemist's fire that far — maximum range is 20 ft.", thrower.Name));
            return result;
        }

        // Total cover: cannot be targeted
        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add(Loc.Tr("{0} has total cover and cannot be targeted.", target.Name));
            return result;
        }

        // Consume the flask regardless of hit or miss
        character.InventoryData.RemoveItem("Alchemist's Fire (flask)");

        if (_inCombat)
            thrower.HasAction = false;

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(thrower, target)
            : !thrower.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage = target.Conditions.HasCondition(Condition.Prone) ||
                            target.Conditions.HasCondition(Condition.Paralyzed) ||
                            target.Conditions.HasCondition(Condition.Unconscious) ||
                            target.Conditions.HasCondition(Condition.Restrained) ||
                            target.IsSqueezingThrough ||
                            thrower.IsHidden ||
                            thrower.Conditions.HasCondition(Condition.Invisible);
        bool hasDisadvantage = !attackerCanSee ||
                               thrower.IsSqueezingThrough ||
                               thrower.Conditions.HasCondition(Condition.Restrained) ||
                               thrower.HasArmorNonProficientBonus;

        if (target.IsDodging && !target.Conditions.HasCondition(Condition.Incapacitated) && target.Speed > 0)
        {
            bool targetCanSeeAttacker = visionSystem != null
                ? visionSystem.CanSee(target, thrower)
                : !target.IsBlinded() && !thrower.Conditions.HasCondition(Condition.Invisible);
            if (targetCanSeeAttacker)
                hasDisadvantage = true;
        }

        thrower.IsHidden = false;

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            "Alchemist's Fire (flask)",
            attackBonus,
            target.ArmorClass + DndMath.GetCoverBonus(target.Cover),
            hasAdvantage,
            hasDisadvantage,
            circumstantialBonus: 0);

        result.AttackRoll       = attackCheck.DieRoll;
        result.TotalAttackBonus = attackCheck.BaseModifier;
        result.TotalToHit       = attackCheck.Total;
        result.HasAdvantage     = attackCheck.HasAdvantage;
        result.HasDisadvantage  = attackCheck.HasDisadvantage;
        result.IsCritical       = attackCheck.IsCriticalHit;
        result.IsCriticalMiss   = attackCheck.IsCriticalMiss;
        result.IsHit            = attackCheck.Success;

        if (result.IsHit)
        {
            target.Conditions = target.Conditions.AddCondition(Condition.Burning);
            thrower.HasAttackedThisRound = true;
            TurnMessages.Add(Loc.Tr("{0} is set ablaze by alchemist's fire!", target.Name));
        }

        return result;
    }

    /// <summary>
    /// Uses an action to attempt to extinguish alchemist's fire (PHB "Adventuring Gear: Alchemist's Fire").
    /// The creature makes a DC 10 Dexterity check; on a success the <see cref="Condition.Burning"/> condition is removed.
    /// </summary>
    /// <param name="creature">The creature using its action to extinguish the flames.</param>
    /// <returns>True if the check succeeds and the flames are extinguished; false otherwise.</returns>
    public bool TryExtinguishFlames(Creature creature)
    {
        if (!creature.Conditions.HasCondition(Condition.Burning))
        {
            TurnMessages.Add(Loc.Tr("{0} is not on fire.", creature.Name));
            return false;
        }

        if (_inCombat && !creature.HasAction)
            return false;

        if (_inCombat)
            creature.HasAction = false;

        var check = creature.MakeAbilityCheck("DEX", 10);
        bool success = check.Success;

        if (success)
        {
            creature.Conditions = creature.Conditions.RemoveCondition(Condition.Burning);
            TurnMessages.Add(Loc.Tr("{0} extinguishes the flames! (rolled {1} vs DC 10)", creature.Name, check.Total));
        }
        else
        {
            TurnMessages.Add(Loc.Tr("{0} fails to extinguish the flames. (rolled {1} vs DC 10)", creature.Name, check.Total));
        }

        return success;
    }

    /// <summary>
    /// Uses an action to attempt to escape a net (PHB "Special Weapons: Net").
    /// The creature makes a DC 10 Strength check; on a success the Restrained condition is removed.
    /// Another creature within reach can also free the restrained target using this method.
    /// </summary>
    /// <param name="creature">The creature using its action (may be the restrained creature or an ally).</param>
    /// <param name="restrainedCreature">The creature currently restrained by the net.</param>
    /// <returns>True if the check succeeds and the creature is freed; false otherwise.</returns>
    public bool TryEscapeNet(Creature creature, Creature restrainedCreature)
    {
        if (!restrainedCreature.Conditions.HasCondition(Condition.Restrained))
        {
            TurnMessages.Add(Loc.Tr("{0} is not restrained by a net.", restrainedCreature.Name));
            return false;
        }

        if (_inCombat && !creature.HasAction)
            return false;

        if (_inCombat)
            creature.HasAction = false;

        int strMod = creature.GetAbilityModifier(creature.Strength);
        int roll = _random.Next(1, 21) + strMod;
        bool success = DndMath.MeetsDC(roll, 10);

        if (success)
        {
            restrainedCreature.Conditions = restrainedCreature.Conditions.RemoveCondition(Condition.Restrained);
            TurnMessages.Add(Loc.Tr("{0} frees {1} from the net! (rolled {2} vs DC 10)", creature.Name, restrainedCreature.Name, roll));
        }
        else
        {
            TurnMessages.Add(Loc.Tr("{0} fails to free {1} from the net. (rolled {2} vs DC 10)", creature.Name, restrainedCreature.Name, roll));
        }

        return success;
    }

    /// <summary>
    /// Attacks the net restraining a creature (PHB "Special Weapons: Net").
    /// The net has AC 10. Dealing 5 or more slashing damage in a single hit destroys the net
    /// and removes the Restrained condition without harming the restrained creature.
    /// </summary>
    /// <param name="attacker">The creature attacking the net.</param>
    /// <param name="restrainedCreature">The creature currently restrained by the net.</param>
    /// <returns>True if the net was destroyed and the creature freed; false otherwise.</returns>
    public bool TryDestroyNet(Creature attacker, Creature restrainedCreature)
    {
        if (!restrainedCreature.Conditions.HasCondition(Condition.Restrained))
        {
            TurnMessages.Add(Loc.Tr("{0} is not restrained by a net.", restrainedCreature.Name));
            return false;
        }

        if (_inCombat && !attacker.HasAction)
            return false;

        if (_inCombat)
            attacker.HasAction = false;

        const int netAC = 10;
        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName,
            attacker.AttackBonus,
            netAC,
            hasAdvantage: false,
            hasDisadvantage: false,
            circumstantialBonus: 0);

        if (!attackCheck.Success)
        {
            TurnMessages.Add(Loc.Tr("{0} attacks the net but misses! (rolled {1} vs AC {2})", attacker.Name, attackCheck.Total, netAC));
            return false;
        }

        int damage = RollDamage(attacker.DamageDice, attacker.DamageBonus, attackCheck.IsCriticalHit);
        if (attacker.CurrentDamageType == DamageType.Slashing && damage >= 5)
        {
            restrainedCreature.Conditions = restrainedCreature.Conditions.RemoveCondition(Condition.Restrained);
            TurnMessages.Add(Loc.Tr("{0} destroys the net, freeing {1}! ({2} slashing damage)", attacker.Name, restrainedCreature.Name, damage));
            return true;
        }

        TurnMessages.Add(Loc.Tr("{0} hits the net but fails to destroy it! ({1} {2} damage — needs 5 slashing to destroy)", attacker.Name, damage, attacker.CurrentDamageType));
        return false;
    }

    private int GetMoveCost(Creature mover, TacticalMapNode from, TacticalMapNode to, int diagonalCount)
    {
        int cost = 5;
        if (IsDiagonalStep(from, to))
        {
            if (diagonalCount % 2 == 1) cost = 10;
        }

        if (TacticalMap != null && TacticalMap.Get(to.X, to.Y, to.Z) == TileType.DifficultTerrain)
            cost *= 2;

        if (mover.IsSqueezingThrough)
            cost *= 2;

        return cost;
    }

    private bool IsDiagonalStep(TacticalMapNode from, TacticalMapNode to)
    {
        int dx = Math.Abs(from.X - to.X);
        int dy = Math.Abs(from.Y - to.Y);
        int dz = Math.Abs(from.Z - to.Z);
        return (dx + dy + dz) > 1 && dx <= 1 && dy <= 1 && dz <= 1;
    }

    private IEnumerable<TacticalMapNode> GetNeighbors(Creature mover, TacticalMapNode node)
    {
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        for (int dz = -1; dz <= 1; dz++)
        {
            if (dx == 0 && dy == 0 && dz == 0) continue;
            int nx = node.X + dx;
            int ny = node.Y + dy;
            int nz = node.Z + dz;
            if (IsWallBlocked(node.X, node.Y, node.Z, nx, ny, nz))
                continue;
            if (CanPassThrough(mover, nx, ny, nz))
                yield return new TacticalMapNode(nx, ny, nz);
        }
    }

    private int CalculatePathCost(Creature creature, List<TacticalMapNode> path)
    {
        int totalCost = 0;
        int diagonalCount = creature.DiagonalStepsTaken;
        for (int i = 1; i < path.Count; i++)
        {
            totalCost += GetMoveCost(creature, path[i - 1], path[i], diagonalCount);
            if (IsDiagonalStep(path[i - 1], path[i]))
                diagonalCount++;
        }
        return totalCost;
    }

    public List<TacticalMapNode>? FindPath(Creature creature, int targetX, int targetY, int targetZ)
    {
        var start = new TacticalMapNode(creature.X, creature.Y, creature.Z);
        var goal = new TacticalMapNode(targetX, targetY, targetZ);

        if (start == goal) return new List<TacticalMapNode> { start };

        var open = new PriorityQueue<(TacticalMapNode node, int diagCount), int>();
        var cameFrom = new Dictionary<(TacticalMapNode node, int), (TacticalMapNode node, int)>();
        var gScore = new Dictionary<(TacticalMapNode node, int), int>();

        int startDiagCount = creature.DiagonalStepsTaken;
        var startState = (start, startDiagCount % 2);
        open.Enqueue(startState, 0);
        gScore[startState] = 0;

        int iterations = 0;
        const int maxIterations = 2000;

        while (open.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            var current = open.Dequeue();

            if (current.node == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(creature, current.node))
            {
                bool isDiag = IsDiagonalStep(current.node, neighbor);
                int cost = GetMoveCost(creature, current.node, neighbor, current.diagCount);
                int tentativeGScore = gScore[current] + cost;

                var nextState = (neighbor, isDiag ? 1 - current.diagCount : current.diagCount);

                if (tentativeGScore < gScore.GetValueOrDefault(nextState, int.MaxValue))
                {
                    cameFrom[nextState] = current;
                    gScore[nextState] = tentativeGScore;
                    int fScore = tentativeGScore + DndMath.CalculateDistance(neighbor.X, neighbor.Y, neighbor.Z, goal.X, goal.Y, goal.Z) * 5;
                    open.Enqueue(nextState, fScore);
                }
            }
        }

        return null;
    }

    private List<TacticalMapNode> ReconstructPath(Dictionary<(TacticalMapNode, int), (TacticalMapNode, int)> cameFrom, (TacticalMapNode node, int diagCount) current)
    {
        var path = new List<TacticalMapNode> { current.node };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current.node);
        }
        path.Reverse();
        return path;
    }

    public List<TacticalMapNode>? FindPathToAdjacent(Creature creature, Creature target)
    {
        var (targetW, targetH) = SizeHelper.GetSpaceInSquares(target.Size);
        List<TacticalMapNode>? bestPath = null;
        int minCost = int.MaxValue;

        for (int dx = 0; dx < targetW; dx++)
        for (int dy = 0; dy < targetH; dy++)
        {
            foreach (var adj in GetNeighbors(creature, new TacticalMapNode(target.X + dx, target.Y + dy, target.Z)))
            {
                if (!CanOccupySpace(creature.Size, adj.X, adj.Y, adj.Z, creature))
                    continue;

                var path = FindPath(creature, adj.X, adj.Y, adj.Z);
                if (path != null)
                {
                    int cost = CalculatePathCost(creature, path);
                    if (cost < minCost)
                    {
                        minCost = cost;
                        bestPath = path;
                    }
                }
            }
        }
        return bestPath;
    }

    public AttackResult MakeAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
    {
        return MakeAttackInternal(attacker, target, visionSystem, isBonusAction: false);
    }

    public AttackResult MakeBonusActionAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
    {
        return MakeAttackInternal(attacker, target, visionSystem, isBonusAction: true);
    }

    private AttackResult MakeAttackInternal(Creature attacker, Creature target, VisionSystem? visionSystem, bool isBonusAction)
    {
        var result = new AttackResult { Attacker = attacker, Target = target };

        if (_inCombat)
        {
            if (isBonusAction)
            {
                if (!attacker.HasBonusAction) { result.IsHit = false; return result; }
                attacker.HasBonusAction = false;
            }
            else
            {
                if (!attacker.HasAction) { result.IsHit = false; return result; }
                attacker.HasAction = false;
            }
        }

        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add(Loc.Tr("{0} has total cover and cannot be targeted.", target.Name));
            return result;
        }

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(attacker, target)
            : !attacker.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage = (target.Conditions.HasCondition(Condition.Prone) && IsInMeleeRange(attacker, target)) ||
                            target.Conditions.HasCondition(Condition.Paralyzed) ||
                            target.Conditions.HasCondition(Condition.Unconscious) ||
                            target.Conditions.HasCondition(Condition.Restrained) ||
                            target.IsBeingHelped ||
                            target.IsSqueezingThrough ||
                            attacker.IsHidden ||
                            attacker.Conditions.HasCondition(Condition.Invisible);

        bool hasDisadvantage = !attackerCanSee ||
                               (target.Conditions.HasCondition(Condition.Prone) && !IsInMeleeRange(attacker, target)) ||
                               attacker.IsSqueezingThrough ||
                               attacker.Conditions.HasCondition(Condition.Restrained) ||
                               attacker.HasArmorNonProficientBonus;

        if (attacker.IsLanceAttack && DndMath.CalculateDistance(attacker.X, attacker.Y, attacker.Z, target.X, target.Y, target.Z) <= 1)
            hasDisadvantage = true;

        if (target.IsDodging && !target.Conditions.HasCondition(Condition.Incapacitated) && target.Speed > 0)
        {
            bool targetCanSeeAttacker = visionSystem != null
                ? visionSystem.CanSee(target, attacker)
                : !target.IsBlinded() && !attacker.Conditions.HasCondition(Condition.Invisible);
            if (targetCanSeeAttacker)
                hasDisadvantage = true;
        }

        if (visionSystem != null && attacker.HasSunlightSensitivity)
        {
            var lightLevel = visionSystem.GetLightLevel(attacker.X, attacker.Y, attacker.Z);
            if (lightLevel == LightType.Bright)
                hasDisadvantage = true;
        }

        if (attacker.HasPackTactics)
        {
            bool allyNearTarget = _combatants.Any(c =>
                c != attacker &&
                c.IsPlayer == attacker.IsPlayer &&
                c.IsAlive() &&
                !c.Conditions.HasCondition(Condition.Incapacitated) &&
                DndMath.CalculateDistance(c.X, c.Y, c.Z, target.X, target.Y, target.Z) <= 1);
            if (allyNearTarget) hasAdvantage = true;
        }

        attacker.IsHidden = false;

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName,
            attacker.AttackBonus,
            target.ArmorClass + DndMath.GetCoverBonus(target.Cover),
            hasAdvantage,
            hasDisadvantage,
            circumstantialBonus: 0);

        result.AttackRoll = attackCheck.DieRoll;
        result.TotalAttackBonus = attackCheck.BaseModifier;
        result.TotalToHit = attackCheck.Total;
        result.HasAdvantage = attackCheck.HasAdvantage;
        result.HasDisadvantage = attackCheck.HasDisadvantage;
        result.IsCritical = attackCheck.IsCriticalHit;
        result.IsCriticalMiss = attackCheck.IsCriticalMiss;
        result.IsHit = attackCheck.Success;

        if (result.IsHit)
        {
            if (attacker.IsNetAttack)
            {
                if (target.Size <= CreatureSize.Large)
                {
                    target.Conditions = target.Conditions.AddCondition(Condition.Restrained);
                    result.TargetRestrained = true;
                }
            }
            else
            {
                int damageBonus = attacker.DamageBonus;
                if (attacker.IsRaging && attacker.IsMeleeAttack && attacker.IsStrengthBasedAttack)
                    damageBonus += attacker.RageDamageBonus;

                result.Damage = RollDamage(attacker.DamageDice, damageBonus, result.IsCritical);
                result.DamageType = attacker.CurrentDamageType;
                target.TakeDamage(result.Damage, result.DamageType, result.IsCritical, attacker.IsSilveredAttack);
            }
            attacker.HasAttackedThisRound = true;
        }

        return result;
    }

    public AttackResult MakeSpellAttack(Creature attacker, Creature target, int attackBonus, string damageDice, DamageType damageType, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult { Attacker = attacker, Target = target };

        if (_inCombat && !attacker.HasAction)
        {
            result.IsHit = false;
            return result;
        }

        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add(Loc.Tr("{0} has total cover and cannot be targeted.", target.Name));
            return result;
        }

        if (_inCombat)
            attacker.HasAction = false;

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(attacker, target)
            : !attacker.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage = target.Conditions.HasCondition(Condition.Paralyzed) ||
                            target.Conditions.HasCondition(Condition.Unconscious) ||
                            target.Conditions.HasCondition(Condition.Restrained) ||
                            target.IsBeingHelped ||
                            target.IsSqueezingThrough ||
                            attacker.IsHidden ||
                            attacker.Conditions.HasCondition(Condition.Invisible);
        bool hasDisadvantage = !attackerCanSee ||
                               attacker.IsSqueezingThrough ||
                               attacker.Conditions.HasCondition(Condition.Restrained) ||
                               attacker.HasArmorNonProficientBonus;

        if (target.IsDodging && !target.Conditions.HasCondition(Condition.Incapacitated) && target.Speed > 0)
        {
            bool targetCanSeeAttacker = visionSystem != null
                ? visionSystem.CanSee(target, attacker)
                : !target.IsBlinded() && !attacker.Conditions.HasCondition(Condition.Invisible);
            if (targetCanSeeAttacker)
                hasDisadvantage = true;
        }

        attacker.IsHidden = false;

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName,
            attackBonus,
            target.ArmorClass + DndMath.GetCoverBonus(target.Cover),
            hasAdvantage,
            hasDisadvantage,
            circumstantialBonus: 0);

        result.AttackRoll       = attackCheck.DieRoll;
        result.TotalAttackBonus = attackCheck.BaseModifier;
        result.TotalToHit       = attackCheck.Total;
        result.HasAdvantage     = attackCheck.HasAdvantage;
        result.HasDisadvantage  = attackCheck.HasDisadvantage;
        result.IsCritical       = attackCheck.IsCriticalHit;
        result.IsCriticalMiss   = attackCheck.IsCriticalMiss;
        result.IsHit            = attackCheck.Success;

        if (result.IsHit)
        {
            result.Damage = RollDamage(damageDice, 0, result.IsCritical);
            result.DamageType = damageType;
            target.TakeDamage(result.Damage, result.DamageType, result.IsCritical);
        }

        return result;
    }

    public (int x, int y, int z)? GetNextStepAwayFrom(Creature creature, Creature target)
    {
        TacticalMapNode current = new TacticalMapNode(creature.X, creature.Y, creature.Z);
        TacticalMapNode? bestStep = null;
        int maxDist = DndMath.CalculateDistance(creature.X, creature.Y, creature.Z, target.X, target.Y, target.Z);

        foreach (var neighbor in GetNeighbors(creature, current))
        {
            if (!CanOccupySpace(creature.Size, neighbor.X, neighbor.Y, neighbor.Z, creature))
                continue;

            int dist = DndMath.CalculateDistance(neighbor.X, neighbor.Y, neighbor.Z, target.X, target.Y, target.Z);
            if (dist > maxDist)
            {
                maxDist = dist;
                bestStep = neighbor;
            }
        }

        if (bestStep.HasValue)
            return (bestStep.Value.X, bestStep.Value.Y, bestStep.Value.Z);
        return null;
    }
}

public class AttackResult
{
    public Creature Attacker { get; set; } = null!;
    public Creature Target { get; set; } = null!;
    public int AttackRoll { get; set; }
    public int TotalAttackBonus { get; set; }
    public int TotalToHit { get; set; }
    public DamageType DamageType { get; set; } = DamageType.None;
    public bool IsHit { get; set; }
    public bool IsCritical { get; set; }
    public bool IsCriticalMiss { get; set; }
    public int Damage { get; set; }
    public bool HasAdvantage { get; set; }
    public bool HasDisadvantage { get; set; }
    public bool IsNonProficient { get; set; }
    /// <summary>True when a net attack hit and applied the Restrained condition to the target.</summary>
    public bool TargetRestrained { get; set; }

    public string GetMessage()
    {
        string advantageText = "";
        if (HasAdvantage) advantageText = " (ADV)";
        if (HasDisadvantage) advantageText = " (DIS)";
        string profText = IsNonProficient ? " (no proficiency)" : "";

        if (IsCriticalMiss)
            return Loc.Tr("{0} critically missed {1}!{2}", Attacker.Name, Target.Name, advantageText + profText);
        if (IsCritical)
            return Loc.Tr("{0} critically hit {1} for {2} damage!{3}", Attacker.Name, Target.Name, Damage, advantageText + profText);
        if (IsHit)
        {
            if (TargetRestrained)
                return Loc.Tr("{0} throws a net and restrains {1}! (AC {2}, rolled {3}+{4}={5}){6}", Attacker.Name, Target.Name, Target.ArmorClass, AttackRoll, TotalAttackBonus, TotalToHit, advantageText + profText);
            return Loc.Tr("{0} hit {1} for {2} damage! (AC {3}, rolled {4}+{5}={6}){7}", Attacker.Name, Target.Name, Damage, Target.ArmorClass, AttackRoll, TotalAttackBonus, TotalToHit, advantageText + profText);
        }

        return Loc.Tr("{0} missed {1}! (AC {2}, rolled {3}+{4}={5}){6}", Attacker.Name, Target.Name, Target.ArmorClass, AttackRoll, TotalAttackBonus, TotalToHit, advantageText + profText);
    }
}

public class SpellResult
{
    public Creature Caster { get; set; } = null!;
    public string SpellName { get; set; } = "";
    public int DamageRolled { get; set; }
    public DamageType DamageType { get; set; } = DamageType.None;
    public List<(Creature Target, bool SavedSuccessfully, int DamageTaken)> TargetResults { get; } = new();

    public string GetMessage()
    {
        if (TargetResults.Count == 0)
            return $"{Caster.Name} casts {SpellName} but hits no targets.";

        var sb = new System.Text.StringBuilder();
        sb.Append($"{Caster.Name} casts {SpellName} for {DamageRolled} {DamageType.ToDisplayString()} damage!");
        foreach (var (target, saved, taken) in TargetResults)
        {
            string saveText = saved ? "saved (half)" : "failed save";
            sb.Append($"\n  {target.Name}: {saveText} → {taken} damage");
        }
        return sb.ToString();
    }
}

public class LongJumpResult
{
    public Creature Creature { get; set; } = null!;
    public int DistanceFt { get; set; }
    public int MovementSpentFt { get; set; }
    public bool ClearedObstacle { get; set; }
    public bool LandedOnFeet { get; set; }
    public bool HasRunningStart { get; set; }
    public int AthleticsRoll { get; set; }
    public int AcrobaticsRoll { get; set; }
}
