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
public partial class CombatManager
{
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

    // --- Shared utility methods used across partial files ---

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

    private bool IsDungeonDoor(TileType type) => type == TileType.DungeonDoorWooden || type == TileType.DungeonDoorStone || type == TileType.DungeonDoorIron || type == TileType.DungeonPortcullis || type == TileType.DungeonSecretDoor;

    private bool IsTileBlocked(TileType type, int x, int y, int z, bool canFly)
    {
        if (type == TileType.Tree || type == TileType.Shrub)
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

    public Creature? GetCreatureAt(int x, int y, int z = 0)
    {
        foreach (var creature in _combatants)
        {
            if (!creature.IsAlive()) continue;
            
            var (width, height) = SizeHelper.GetSpaceInSquares(creature.Size);
            
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
        var (attackerWidth, attackerHeight) = SizeHelper.GetSpaceInSquares(attacker.Size);
        var (targetWidth, targetHeight) = SizeHelper.GetSpaceInSquares(target.Size);

        int reach = attacker.IsReachWeapon ? 2 : 1;

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

    public bool CanSee(Creature creature, Creature target, VisionSystem? visionSystem)
    {
        if (target.Conditions.HasCondition(Condition.Invisible))
            return false;

        if (creature.IsBlinded())
            return false;

        if (visionSystem != null)
            return visionSystem.CanSee(creature, target);

        if (creature.X == target.X && creature.Y == target.Y && creature.Z == target.Z)
            return true;

        return false;
    }

    /// <summary>
    /// Rolls a D20 for an attack, ability check, or saving throw.
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
        double u1 = _random.NextDouble();
        double u2 = _random.NextDouble();
        double gaussianRoll = Sqrt(-2.0 * Log(u1)) * Cos(2.0 * PI * u2);
        double adjustedRoll = 10.5 + gaussianRoll * 4;
        return (float)Clamp(adjustedRoll, 1, 20);
    }

    /// <summary>
    /// Rolls weapon damage, applying critical hit rules (PHB "Critical Hits").
    /// </summary>
    public int RollDamage(string damageDice, int bonus, bool isCritical, string? extraDamageDice = null)
    {
        int total = bonus;
        int rolls = isCritical ? 2 : 1;

        if (int.TryParse(damageDice, out int fixedDamage))
        {
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
        bool isDexSave = abilityName is "DEX" or "Dexterity";
        if (creature.IsSqueezingThrough && isDexSave)
            hasDisadvantage = true;

        if (creature.IsDodging && isDexSave && !creature.Conditions.HasCondition(Condition.Incapacitated) && creature.Speed > 0)
            hasAdvantage = true;

        return creature.MakeSavingThrow(abilityName, dc, hasAdvantage, hasDisadvantage);
    }
}
