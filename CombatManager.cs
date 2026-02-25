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
    private readonly record struct TacticalMapNode(int X, int Y, int Z);
    public InfiniteGrid3D<TileType>? TacticalMap { get; set; }
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
    
    public void EndCombat()
    {
        _inCombat = false;
        _combatants.Clear();
        _currentTurnIndex = 0;
        _currentRound = 0;
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
    /// </list>
    /// </para>
    /// </summary>
    private bool CanPassThrough(Creature mover, int x, int y, int z)
    {
        if (TacticalMap == null) return true;

        var tileType = TacticalMap.Get(x, y, z);

        // Walls and vegetation are never passable.
        // Empty tiles are only passable if the creature can fly.
        if (tileType == TileType.Wall || tileType == TileType.Tree || tileType == TileType.Shrub ||
            (tileType == TileType.Empty && !mover.CanFly))
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

                // Walls and vegetation are never occupiable.
                // Empty tiles are only occupiable if the creature can fly.
                bool isBlocked = tileType == TileType.Wall || tileType == TileType.Tree || tileType == TileType.Shrub ||
                                 (tileType == TileType.Empty && !canFly);

                if (isBlocked)
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
                if (tileType == TileType.Wall || tileType == TileType.Empty)
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
    
    public void Move(Creature creature, int targetX, int targetY, int targetZ, VisionSystem? visionSystem = null)
    {
        var path = FindPath(creature, targetX, targetY, targetZ);
        if (path == null)
            return;

        int movementSpent = 0;
        int remaining = creature.MovementRemaining;
        int diagonalCount = creature.DiagonalStepsTaken;

        for (int i = 1; i < path.Count; i++)
        {
            int stepCost = GetMoveCost(creature, path[i - 1], path[i], diagonalCount);

            if (movementSpent + stepCost > remaining)
                break;

            // Before each step, check whether any hostile loses melee reach — triggering an OA.
            if (!creature.IsDisengaged)
            {
                CheckOpportunityAttacks(creature, path[i - 1], path[i], visionSystem);
                if (!creature.IsAlive())
                    break;
            }

            movementSpent += stepCost;
            if (IsDiagonalStep(path[i - 1], path[i]))
                diagonalCount++;
            creature.MoveTo(path[i].X, path[i].Y, path[i].Z);
        }

        creature.MovementRemaining = Math.Max(0, remaining - movementSpent);
        creature.DiagonalStepsTaken = diagonalCount;

        // Update squeezing state based on final position
        creature.IsSqueezingThrough = WouldRequireSqueeze(creature, creature.X, creature.Y, creature.Z);
    }

    /// <summary>
    /// Checks whether any hostile creature loses melee reach as <paramref name="mover"/> steps
    /// from <paramref name="from"/> to <paramref name="to"/>, and triggers an opportunity attack
    /// for each such creature that still has its reaction.
    /// </summary>
    private void CheckOpportunityAttacks(Creature mover, TacticalMapNode from, TacticalMapNode to, VisionSystem? visionSystem)
    {
        foreach (var hostile in _combatants.ToList())
        {
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

            bool inRangeBefore = IsInMeleeRangeAt(mover.Size, from.X, from.Y, from.Z, hostile);
            bool inRangeAfter  = IsInMeleeRangeAt(mover.Size, to.X,   to.Y,   to.Z,   hostile);

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
    private static bool IsInMeleeRangeAt(CreatureSize moverSize, int moverX, int moverY, int moverZ, Creature target)
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
                if (dx <= 1 && dy <= 1 && dz <= 1 && (dx + dy + dz) > 0)
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
            if (visionSystem.GlobalDaylight || lightLevel == LightType.Bright)
                hasDisadvantage = true;
        }

        if (attacker.HasPackTactics)
        {
            bool allyNearTarget = _combatants.Any(c =>
                c != attacker &&
                c.IsPlayer == attacker.IsPlayer &&
                c.IsAlive() &&
                !c.Conditions.HasCondition(Condition.Incapacitated) &&
                CalculateDistance(c.X, c.Y, c.Z, target.X, target.Y, target.Z) <= 1);
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
            target.TakeDamage(result.Damage, result.DamageType, result.IsCritical);
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

        if (bestPath == null)
            return null;

        var step = bestPath[1];
        return (step.X, step.Y, step.Z);
    }

    private List<TacticalMapNode>? FindPathToAdjacent(Creature creature, Creature target)
    {
        var start = new TacticalMapNode(creature.X, creature.Y, creature.Z);
        var openSet = new List<TacticalMapNode> { start };
        var cameFrom = new Dictionary<TacticalMapNode, TacticalMapNode>();
        var gScore = new Dictionary<TacticalMapNode, int> { [start] = 0 };
        var turnCount = new Dictionary<TacticalMapNode, int> { [start] = 0 };
        var diagParity = new Dictionary<TacticalMapNode, int> { [start] = creature.DiagonalStepsTaken % 2 };
        var fScore = new Dictionary<TacticalMapNode, int> { [start] = HeuristicToAdjacent(start, target) };

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(n => fScore.GetValueOrDefault(n, int.MaxValue)).First();

            if (IsAdjacentToTarget(current, target) && CanOccupySpace(creature.Size, current.X, current.Y, current.Z, creature))
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(creature, current))
            {
                bool isDiag = IsDiagonalStep(current, neighbor);
                int currentDiagParity = diagParity[current];
                int newDiagParity = isDiag ? 1 - currentDiagParity : currentDiagParity;

                int tentativeTurns = turnCount[current] + GetTurnPenalty(cameFrom, current, neighbor);
                int tentativeG = gScore[current] + GetMoveCost(creature, current, neighbor, currentDiagParity);
                int currentBestG = gScore.GetValueOrDefault(neighbor, int.MaxValue);
                int currentBestTurns = turnCount.GetValueOrDefault(neighbor, int.MaxValue);

                if (tentativeG > currentBestG)
                    continue;
                if (tentativeG == currentBestG && tentativeTurns >= currentBestTurns)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                turnCount[neighbor] = tentativeTurns;
                diagParity[neighbor] = newDiagParity;
                fScore[neighbor] = tentativeG + HeuristicToAdjacent(neighbor, target);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        return null;
    }

    private static bool IsAdjacentToTarget(TacticalMapNode node, Creature target)
    {
        int dx = Abs(node.X - target.X);
        int dy = Abs(node.Y - target.Y);
        int dz = Abs(node.Z - target.Z);
        return Max(Max(dx, dy), dz) == 1;
    }

    private static int HeuristicToAdjacent(TacticalMapNode node, Creature target)
    {
        int dx = Abs(node.X - target.X);
        int dy = Abs(node.Y - target.Y);
        int dz = Abs(node.Z - target.Z);
        int chebyshev = Max(Max(dx, dy), dz);
        return Max(chebyshev - 1, 0) * 5;
    }

    /// <summary>
    /// Returns the best single step that moves <paramref name="creature"/> away from
    /// <paramref name="target"/>, picking the adjacent tile that maximises Chebyshev distance.
    /// Returns null if the creature is cornered and cannot increase distance.
    /// </summary>
    public (int x, int y, int z)? GetNextStepAwayFrom(Creature creature, Creature target)
    {
        (int x, int y, int z)? best = null;
        int bestDist = CalculateDistance(creature.X, creature.Y, creature.Z, target.X, target.Y, target.Z);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = creature.X + dx;
                int ny = creature.Y + dy;
                int nz = creature.Z;

                if (!CanOccupySpace(creature.Size, nx, ny, nz, creature))
                    continue;

                int dist = CalculateDistance(nx, ny, nz, target.X, target.Y, target.Z);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    best = (nx, ny, nz);
                }
            }
        }

        return best;
    }

    private List<TacticalMapNode>? FindPath(Creature creature, int targetX, int targetY, int targetZ)
    {
        var start = new TacticalMapNode(creature.X, creature.Y, creature.Z);
        var goal = new TacticalMapNode(targetX, targetY, targetZ);

        // Guard against unreachable goals in the infinite grid (e.g., hovering/clicking a wall).
        // Without this check, the search may expand indefinitely trying to reach an unoccupiable tile.
        if (!CanOccupySpace(creature.Size, targetX, targetY, targetZ, creature))
            return null;

        if (start == goal)
            return new List<TacticalMapNode> { start };

        var openSet = new List<TacticalMapNode> { start };
        var cameFrom = new Dictionary<TacticalMapNode, TacticalMapNode>();
        var gScore = new Dictionary<TacticalMapNode, int> { [start] = 0 };
        var turnCount = new Dictionary<TacticalMapNode, int> { [start] = 0 };
        var diagParity = new Dictionary<TacticalMapNode, int> { [start] = creature.DiagonalStepsTaken % 2 };
        var fScore = new Dictionary<TacticalMapNode, int> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(n => fScore.GetValueOrDefault(n, int.MaxValue)).First();
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(creature, current))
            {
                bool isDiag = IsDiagonalStep(current, neighbor);
                int currentDiagParity = diagParity[current];
                int newDiagParity = isDiag ? 1 - currentDiagParity : currentDiagParity;

                int tentativeTurns = turnCount[current] + GetTurnPenalty(cameFrom, current, neighbor);
                int tentativeG = gScore[current] + GetMoveCost(creature, current, neighbor, currentDiagParity);
                int currentBestG = gScore.GetValueOrDefault(neighbor, int.MaxValue);
                int currentBestTurns = turnCount.GetValueOrDefault(neighbor, int.MaxValue);

                if (tentativeG > currentBestG)
                    continue;
                if (tentativeG == currentBestG && tentativeTurns >= currentBestTurns)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                turnCount[neighbor] = tentativeTurns;
                diagParity[neighbor] = newDiagParity;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        return null;
    }

    private IEnumerable<TacticalMapNode> GetNeighbors(Creature creature, TacticalMapNode node)
    {
        // Flying creatures and climbing/swimming creatures can all move in the vertical axis.
        // CanOccupySpace filters out Empty (air) tiles for non-flyers, so only accessible
        // tiles at a different Z level (e.g. Climbable) are ever yielded for non-flyers.
        int minDz = -1;
        int maxDz = 1;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = minDz; dz <= maxDz; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    int nx = node.X + dx;
                    int ny = node.Y + dy;
                    int nz = node.Z + dz;

                    // A tile is a valid neighbor if the creature can either occupy it (land there)
                    // or at least pass through it (transit). The pathfinder uses this for routing;
                    // CanMove / FindPath still enforce that the final destination must be occupiable.
                    bool canOccupy = CanOccupySpace(creature.Size, nx, ny, nz, creature);
                    bool canTransit = !canOccupy && CanPassThrough(creature, nx, ny, nz);
                    if (!canOccupy && !canTransit)
                        continue;
                    if (!IsDiagonalMoveAllowed(node.X, node.Y, node.Z, nx, ny, nz))
                        continue;

                    yield return new TacticalMapNode(nx, ny, nz);
                }
            }
        }
    }

    private static int GetTurnPenalty(Dictionary<TacticalMapNode, TacticalMapNode> cameFrom, TacticalMapNode current, TacticalMapNode neighbor)
    {
        if (!cameFrom.TryGetValue(current, out var previous))
            return 0;

        int dx1 = current.X - previous.X;
        int dy1 = current.Y - previous.Y;
        int dz1 = current.Z - previous.Z;

        int dx2 = neighbor.X - current.X;
        int dy2 = neighbor.Y - current.Y;
        int dz2 = neighbor.Z - current.Z;

        return (dx1 == dx2 && dy1 == dy2 && dz1 == dz2) ? 0 : 1;
    }

    private static int Heuristic(TacticalMapNode a, TacticalMapNode b)
    {
        return Max(Max(Abs(b.X - a.X), Abs(b.Y - a.Y)), Abs(b.Z - a.Z)) * 5;
    }

    private static bool IsDiagonalStep(TacticalMapNode from, TacticalMapNode to)
    {
        int axes = 0;
        if (to.X != from.X) axes++;
        if (to.Y != from.Y) axes++;
        if (to.Z != from.Z) axes++;
        return axes > 1;
    }

    private int GetMoveCost(Creature creature, TacticalMapNode from, TacticalMapNode to, int diagonalStepsTaken)
    {
        // Alternating diagonal rule (DMG variant): odd diagonals cost 5 ft, even diagonals cost 10 ft.
        // diagonalStepsTaken tracks how many diagonals have been taken so far this turn.
        bool isDiagonal = IsDiagonalStep(from, to);
        int baseCost = isDiagonal && diagonalStepsTaken % 2 == 1 ? 10 : 5;
        // Keep the original step cost so every "1 extra foot per foot" condition adds additively:
        // prone + difficult terrain = 5 + 5 + 5 = 15 ft (3×), not 5 → 10 → 20 ft (4×).
        int stepCost = baseCost;

        if (TacticalMap != null)
        {
            var tileType = TacticalMap.Get(to.X, to.Y, to.Z);
            if (tileType == TileType.DifficultTerrain || tileType == TileType.Mud || tileType == TileType.Snow || tileType == TileType.Ice)
                baseCost += stepCost;
            // Swimming without a swim speed costs 1 extra foot per foot (PHB "Climbing, Swimming, and Crawling")
            else if (tileType == TileType.Water && creature.SwimSpeed == 0)
                baseCost += stepCost;
            // Climbing without a climb speed costs 1 extra foot per foot
            else if (tileType == TileType.Climbable && creature.ClimbSpeed == 0)
                baseCost += stepCost;
        }

        // Prone creatures must crawl: 1 extra foot per foot (PHB "Climbing, Swimming, and Crawling").
        // Stacks additively with terrain costs (e.g. prone + difficult terrain = 3× base).
        if (creature.Conditions.HasCondition(Condition.Prone))
            baseCost += stepCost;

        // Another creature's space counts as difficult terrain (PHB "Moving Around Other Creatures").
        // Apply only when transiting through the space, not when squeezing (already doubled below).
        var occupant = GetCreatureAt(to.X, to.Y, to.Z);
        if (occupant != null && occupant != creature && !WouldRequireSqueeze(creature, to.X, to.Y, to.Z))
            baseCost *= 2;

        // If squeezing is required, double the movement cost
        if (WouldRequireSqueeze(creature, to.X, to.Y, to.Z))
            baseCost *= 2;

        return baseCost;
    }

    private static List<TacticalMapNode> ReconstructPath(Dictionary<TacticalMapNode, TacticalMapNode> cameFrom, TacticalMapNode current)
    {
        var path = new List<TacticalMapNode> { current };
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private int CalculatePathCost(Creature creature, List<TacticalMapNode> path)
    {
        int cost = 0;
        int diagonalParity = creature.DiagonalStepsTaken % 2;
        for (int i = 1; i < path.Count; i++)
        {
            bool isDiag = IsDiagonalStep(path[i - 1], path[i]);
            cost += GetMoveCost(creature, path[i - 1], path[i], diagonalParity);
            if (isDiag) diagonalParity = 1 - diagonalParity;
        }
        return cost;
    }

    public bool IsPathBlocked(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        if (TacticalMap == null) return false;

        int dist = CalculateDistance(x1, y1, z1, x2, y2, z2);
        if (dist == 0) return false;

        for (int i = 1; i <= dist; i++)
        {
            float t = (float)i / dist;
            int cx = (int)Math.Round(x1 + (x2 - x1) * t);
            int cy = (int)Math.Round(y1 + (y2 - y1) * t);
            int cz = (int)Math.Round(z1 + (z2 - z1) * t);

            var tile = TacticalMap.Get(cx, cy, cz);
            if (tile == TileType.Wall || tile == TileType.Tree || tile == TileType.Shrub)
                return true;

            float tPrev = (float)(i - 1) / dist;
            int px = (int)Math.Round(x1 + (x2 - x1) * tPrev);
            int py = (int)Math.Round(y1 + (y2 - y1) * tPrev);
            int pz = (int)Math.Round(z1 + (z2 - z1) * tPrev);

            if (!IsDiagonalMoveAllowed(px, py, pz, cx, cy, cz))
                return true;
        }

        return false;
    }

    private int CalculateMovementCost(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        int dist = CalculateDistance(x1, y1, z1, x2, y2, z2);
        if (dist == 0) return 0;

        int totalCost = 0;
        int diagonalParity = 0;
        for (int i = 1; i <= dist; i++)
        {
            float tPrev = (float)(i - 1) / dist;
            float tCurr = (float)i / dist;
            var from = new TacticalMapNode(
                (int)Math.Round(x1 + (x2 - x1) * tPrev),
                (int)Math.Round(y1 + (y2 - y1) * tPrev),
                (int)Math.Round(z1 + (z2 - z1) * tPrev));
            var to = new TacticalMapNode(
                (int)Math.Round(x1 + (x2 - x1) * tCurr),
                (int)Math.Round(y1 + (y2 - y1) * tCurr),
                (int)Math.Round(z1 + (z2 - z1) * tCurr));

            bool isDiag = IsDiagonalStep(from, to);
            int stepCost = isDiag && diagonalParity == 1 ? 10 : 5;
            if (isDiag) diagonalParity = 1 - diagonalParity;

            if (TacticalMap != null && TacticalMap.Get(to.X, to.Y, to.Z) == TileType.DifficultTerrain)
                stepCost += stepCost;

            totalCost += stepCost;
        }

        return totalCost;
    }

    private bool IsDiagonalMoveAllowed(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        if (TacticalMap == null) return true;

        int dx = x2 - x1;
        int dy = y2 - y1;
        int dz = z2 - z1;

        // 2D diagonals check
        if (Abs(dx) == 1 && Abs(dy) == 1 && dz == 0)
        {
            if (IsBlockingTile(TacticalMap.Get(x1 + dx, y1, z1)) || IsBlockingTile(TacticalMap.Get(x1, y1 + dy, z1)))
                return false;
        }
        if (Abs(dx) == 1 && Abs(dz) == 1 && dy == 0)
        {
            if (IsBlockingTile(TacticalMap.Get(x1 + dx, y1, z1)) || IsBlockingTile(TacticalMap.Get(x1, y1, z1 + dz)))
                return false;
        }
        if (Abs(dy) == 1 && Abs(dz) == 1 && dx == 0)
        {
            if (IsBlockingTile(TacticalMap.Get(x1, y1 + dy, z1)) || IsBlockingTile(TacticalMap.Get(x1, y1, z1 + dz)))
                return false;
        }

        // 3D diagonal check (all 3 axes change)
        if (Abs(dx) == 1 && Abs(dy) == 1 && Abs(dz) == 1)
        {
            // If any adjacent square that shares a face with the path is a blocking tile, block it
            if (IsBlockingTile(TacticalMap.Get(x1 + dx, y1, z1)) ||
                IsBlockingTile(TacticalMap.Get(x1, y1 + dy, z1)) ||
                IsBlockingTile(TacticalMap.Get(x1, y1, z1 + dz)))
                return false;
        }

        return true;
    }
    
    private static bool IsBlockingTile(TileType tile)
    {
        return tile == TileType.Wall || tile == TileType.Tree || tile == TileType.Shrub;
    }

    public int CalculateDistance(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        // Chebyshev distance (5e grid rules: diagonals cost same as straight)
        return Max(Max(Abs(x2 - x1), Abs(y2 - y1)), Abs(z2 - z1));
    }
    
    public AttackResult MakeAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };
        
        // Check if attacker has an action available
        if (_inCombat && !attacker.HasAction)
        {
            result.IsHit = false;
            return result;
        }

        // Total cover: cannot be targeted directly (PHB "Cover")
        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add($"{target.Name} has total cover and cannot be targeted.");
            return result;
        }

        // Consume the action
        if (_inCombat)
            attacker.HasAction = false;
        
        // Check if attacker can see target
        bool attackerCanSee = true;
        if (visionSystem != null)
        {
            attackerCanSee = visionSystem.CanSee(attacker, target);
        }
        else
        {
            attackerCanSee = !attacker.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);
        }
        
        // Determine advantage/disadvantage
        bool hasAdvantage = target.Conditions.HasCondition(Condition.Prone) ||
                           target.Conditions.HasCondition(Condition.Paralyzed) ||
                           target.Conditions.HasCondition(Condition.Unconscious) ||
                           target.IsSqueezingThrough ||  // Attack rolls against a squeezing creature have advantage
                           attacker.IsHidden ||           // Unseen attacker (hidden via stealth): attack rolls have advantage
                           attacker.Conditions.HasCondition(Condition.Invisible); // Unseen attacker (Invisible condition): PHB "Unseen Attackers and Targets"
        bool hasDisadvantage = !attackerCanSee ||
                               attacker.IsSqueezingThrough;  // Squeezing creature has disadvantage on attack rolls

        // Reveal the attacker after striking — attacking ends the hidden condition (PHB "Unseen Attackers and Targets").
        attacker.IsHidden = false;

        // Help action: a friendly creature distracted this target, granting advantage on this attack.
        if (target.IsBeingHelped)
        {
            hasAdvantage = true;
            target.IsBeingHelped = false; // Benefit consumed by this attack
        }

        // Dodge: attacker has disadvantage if the dodging target can see the attacker
        if (target.IsDodging && !target.Conditions.HasCondition(Condition.Incapacitated) && target.Speed > 0)
        {
            bool targetCanSeeAttacker = visionSystem != null
                ? visionSystem.CanSee(target, attacker)
                : !target.IsBlinded() && !attacker.Conditions.HasCondition(Condition.Invisible);
            if (targetCanSeeAttacker)
                hasDisadvantage = true;
        }

        // Check for sunlight sensitivity (circumstantial disadvantage)
        if (visionSystem != null && attacker.HasSunlightSensitivity)
        {
            var lightLevel = visionSystem.GetLightLevel(attacker.X, attacker.Y, attacker.Z);
            if (visionSystem.GlobalDaylight || lightLevel == LightType.Bright)
            {
                hasDisadvantage = true;
            }
        }

        // Pack Tactics: advantage if a non-incapacitated ally is within 5 ft. of the target
        if (attacker.HasPackTactics)
        {
            bool allyNearTarget = _combatants.Any(c =>
                c != attacker &&
                c.IsPlayer == attacker.IsPlayer &&
                c.IsAlive() &&
                !c.Conditions.HasCondition(Condition.Incapacitated) &&
                CalculateDistance(c.X, c.Y, c.Z, target.X, target.Y, target.Z) <= 1);

            if (allyNearTarget)
                hasAdvantage = true;
        }
        
        // Make attack roll using D20Check system
        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName,
            attacker.AttackBonus,
            target.ArmorClass + DndMath.GetCoverBonus(target.Cover),
            hasAdvantage,
            hasDisadvantage,
            circumstantialBonus: 0
        );

        // Store roll information in result
        result.AttackRoll = attackCheck.DieRoll;
        result.TotalAttackBonus = attackCheck.BaseModifier;
        result.TotalToHit = attackCheck.Total;
        result.HasAdvantage = attackCheck.HasAdvantage;
        result.HasDisadvantage = attackCheck.HasDisadvantage;
        result.IsCritical = attackCheck.IsCriticalHit;
        result.IsCriticalMiss = attackCheck.IsCriticalMiss;
        result.IsHit = attackCheck.Success;
        
        // Roll damage if hit
        if (result.IsHit)
        {
            int damageBonus = attacker.DamageBonus;

            // Barbarian Rage bonus damage: applies to melee weapon attacks using Strength.
            if (attacker.IsRaging && attacker.IsMeleeAttack)
            {
                damageBonus += attacker.RageDamageBonus;
            }

            result.Damage = RollDamage(attacker.DamageDice, damageBonus, result.IsCritical);
            result.DamageType = attacker.CurrentDamageType;
            target.TakeDamage(result.Damage, result.DamageType, result.IsCritical);

            attacker.HasAttackedThisRound = true;
        }

        return result;
    }

    /// <summary>
    /// Makes the Two-Weapon Fighting bonus action attack
    /// The attacker must hold a light melee weapon in each hand; the bonus attack uses
    /// <see cref="Creature.HasBonusAction"/> instead of the main action, and the ability
    /// modifier is <em>not</em> added to the damage roll (unless it is negative).
    /// Barbarian rage damage still applies.
    /// </summary>
    public AttackResult MakeBonusActionAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        if (_inCombat && !attacker.HasBonusAction)
        {
            result.IsHit = false;
            return result;
        }

        // Total cover: cannot be targeted directly (PHB "Cover")
        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add($"{target.Name} has total cover and cannot be targeted.");
            return result;
        }

        if (_inCombat)
            attacker.HasBonusAction = false;

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(attacker, target)
            : !attacker.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage = target.Conditions.HasCondition(Condition.Prone) ||
                            target.Conditions.HasCondition(Condition.Paralyzed) ||
                            target.Conditions.HasCondition(Condition.Unconscious) ||
                            target.IsSqueezingThrough ||
                            attacker.IsHidden ||
                            attacker.Conditions.HasCondition(Condition.Invisible);
        bool hasDisadvantage = !attackerCanSee ||
                               attacker.IsSqueezingThrough;

        attacker.IsHidden = false;

        if (target.IsBeingHelped)
        {
            hasAdvantage = true;
            target.IsBeingHelped = false;
        }

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
            if (visionSystem.GlobalDaylight || lightLevel == LightType.Bright)
                hasDisadvantage = true;
        }

        if (attacker.HasPackTactics)
        {
            bool allyNearTarget = _combatants.Any(c =>
                c != attacker &&
                c.IsPlayer == attacker.IsPlayer &&
                c.IsAlive() &&
                !c.Conditions.HasCondition(Condition.Incapacitated) &&
                CalculateDistance(c.X, c.Y, c.Z, target.X, target.Y, target.Z) <= 1);
            if (allyNearTarget) hasAdvantage = true;
        }

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName,
            attacker.AttackBonus,
            target.ArmorClass + DndMath.GetCoverBonus(target.Cover),
            hasAdvantage,
            hasDisadvantage,
            circumstantialBonus: 0
        );

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
            // TWF: don't add ability modifier to damage unless it is negative (PHB "Two-Weapon Fighting")
            int damageBonus = Math.Min(0, attacker.DamageBonus);

            if (attacker.IsRaging && attacker.IsMeleeAttack)
                damageBonus += attacker.RageDamageBonus;

            result.Damage     = RollDamage(attacker.DamageDice, damageBonus, result.IsCritical);
            result.DamageType = attacker.CurrentDamageType;
            target.TakeDamage(result.Damage, result.DamageType, result.IsCritical);
            attacker.HasAttackedThisRound = true;
        }

        return result;
    }

    /// <summary>
    /// Makes a ranged weapon attack
    /// <para><b>Range:</b> Cannot attack beyond long range. Attack rolls have disadvantage
    /// when the target is beyond normal range but within long range.</para>
    /// <para><b>Ranged Attacks in Close Combat:</b> Attack rolls have disadvantage if the
    /// attacker is within 5 feet of a hostile creature that can see them and isn't incapacitated.</para>
    /// </summary>
    /// <param name="attacker">The attacking creature.</param>
    /// <param name="target">The target creature.</param>
    /// <param name="visionSystem">Optional vision system for line-of-sight checks.</param>
    public AttackResult MakeRangedAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        if (_inCombat && !attacker.HasAction)
        {
            result.IsHit = false;
            return result;
        }

        int distanceFeet = CalculateDistance(attacker.X, attacker.Y, attacker.Z, target.X, target.Y, target.Z) * 5;
        int normalRange = attacker.NormalRange;
        int longRange = attacker.LongRange > 0 ? attacker.LongRange : normalRange;

        // Cannot attack beyond long range
        if (normalRange > 0 && distanceFeet > longRange)
        {
            result.IsHit = false;
            TurnMessages.Add($"{attacker.Name} cannot attack {target.Name} — target is beyond long range ({distanceFeet} ft. / max {longRange} ft.).");
            return result;
        }

        // Total cover: cannot be targeted directly (PHB "Cover")
        if (target.Cover == CoverType.Total)
        {
            result.IsHit = false;
            TurnMessages.Add($"{target.Name} has total cover and cannot be targeted.");
            return result;
        }

        if (_inCombat)
            attacker.HasAction = false;

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(attacker, target)
            : !attacker.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage    = attacker.IsHidden;
        bool hasDisadvantage = !attackerCanSee;

        attacker.IsHidden = false;

        // Help action: a friendly creature distracted this target, granting advantage on this attack.
        if (target.IsBeingHelped)
        {
            hasAdvantage = true;
            target.IsBeingHelped = false;
        }

        // Beyond normal range: attack roll has disadvantage (PHB "Range")
        if (normalRange > 0 && distanceFeet > normalRange)
            hasDisadvantage = true;

        // Ranged attacks in close combat: disadvantage if a hostile within 5 ft. can see the attacker
        // and isn't incapacitated (PHB "Ranged Attacks in Close Combat")
        bool hostileAdjacent = _combatants.Any(c =>
            c != attacker &&
            c.IsPlayer != attacker.IsPlayer &&
            c.IsAlive() &&
            !c.Conditions.HasCondition(Condition.Incapacitated) &&
            CalculateDistance(c.X, c.Y, c.Z, attacker.X, attacker.Y, attacker.Z) <= 1 &&
            (visionSystem != null
                ? visionSystem.CanSee(c, attacker)
                : !c.IsBlinded() && !attacker.Conditions.HasCondition(Condition.Invisible)));

        if (hostileAdjacent)
            hasDisadvantage = true;

        if (visionSystem != null && attacker.HasSunlightSensitivity)
        {
            var lightLevel = visionSystem.GetLightLevel(attacker.X, attacker.Y, attacker.Z);
            if (visionSystem.GlobalDaylight || lightLevel == LightType.Bright)
                hasDisadvantage = true;
        }

        if (attacker.HasPackTactics)
        {
            bool allyNearTarget = _combatants.Any(c =>
                c != attacker &&
                c.IsPlayer == attacker.IsPlayer &&
                c.IsAlive() &&
                !c.Conditions.HasCondition(Condition.Incapacitated) &&
                CalculateDistance(c.X, c.Y, c.Z, target.X, target.Y, target.Z) <= 1);
            if (allyNearTarget) hasAdvantage = true;
        }

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName,
            attacker.AttackBonus,
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
            result.Damage     = RollDamage(attacker.DamageDice, attacker.DamageBonus, result.IsCritical);
            result.DamageType = attacker.CurrentDamageType;
            target.TakeDamage(result.Damage, result.DamageType, result.IsCritical);
            attacker.HasAttackedThisRound = true;
        }

        return result;
    }

    /// <summary>
    /// Makes a spell attack roll (ranged spell attack) following D&amp;D 5e rules.
    /// Uses the provided spell attack bonus and damage dice instead of the creature's weapon stats.
    /// </summary>
    public AttackResult MakeSpellAttack(Creature attacker, Creature target, int spellAttackBonus, string damageDice, DamageType damageType = DamageType.Force, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };

        if (_inCombat && !attacker.HasAction)
        {
            result.IsHit = false;
            return result;
        }

        if (_inCombat)
            attacker.HasAction = false;

        bool attackerCanSee = visionSystem != null
            ? visionSystem.CanSee(attacker, target)
            : !attacker.IsBlinded() && !target.Conditions.HasCondition(Condition.Invisible);

        bool hasAdvantage = attacker.IsHidden;
        bool hasDisadvantage = !attackerCanSee;

        attacker.IsHidden = false;

        // Help action: a friendly creature distracted this target, granting advantage on this attack.
        if (target.IsBeingHelped)
        {
            hasAdvantage = true;
            target.IsBeingHelped = false; // Benefit consumed by this attack
        }

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
            if (visionSystem.GlobalDaylight || lightLevel == LightType.Bright)
                hasDisadvantage = true;
        }

        // Ranged attacks in close combat: disadvantage if a hostile within 5 ft. can see the attacker
        // and isn't incapacitated (PHB "Ranged Attacks in Close Combat")
        bool spellHostileAdjacent = _combatants.Any(c =>
            c != attacker &&
            c.IsPlayer != attacker.IsPlayer &&
            c.IsAlive() &&
            !c.Conditions.HasCondition(Condition.Incapacitated) &&
            CalculateDistance(c.X, c.Y, c.Z, attacker.X, attacker.Y, attacker.Z) <= 1 &&
            (visionSystem != null
                ? visionSystem.CanSee(c, attacker)
                : !c.IsBlinded() && !attacker.Conditions.HasCondition(Condition.Invisible)));

        if (spellHostileAdjacent)
            hasDisadvantage = true;

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            "Spell Attack",
            spellAttackBonus,
            target.ArmorClass,
            hasAdvantage,
            hasDisadvantage,
            circumstantialBonus: 0
        );

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
            result.Damage = RollDamage(damageDice, 0, result.IsCritical);
            result.DamageType = damageType;
            target.TakeDamage(result.Damage, result.DamageType, result.IsCritical);
            attacker.HasAttackedThisRound = true;
        }

        return result;
    }

    public int RollD20()
    {
        return _random.Next(1, 21);
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
            int dist = CalculateDistance(creature.X, creature.Y, creature.Z, targetX, targetY, targetZ);
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
        
        // Check if any square occupied by the attacker is adjacent to any square occupied by the target
        for (int ax = 0; ax < attackerWidth; ax++)
        {
            for (int ay = 0; ay < attackerHeight; ay++)
            {
                int attackerTileX = attacker.X + ax;
                int attackerTileY = attacker.Y + ay;
                
                for (int tx = 0; tx < targetWidth; tx++)
                {
                    for (int ty = 0; ty < targetHeight; ty++)
                    {
                        int targetTileX = target.X + tx;
                        int targetTileY = target.Y + ty;
                        
                        // Check if these tiles are adjacent (including diagonally)
                        int dx = Math.Abs(attackerTileX - targetTileX);
                        int dy = Math.Abs(attackerTileY - targetTileY);
                        int dz = Math.Abs(attacker.Z - target.Z);
                        
                        // Adjacent if within 1 square on each axis (includes diagonals)
                        if (dx <= 1 && dy <= 1 && dz <= 1 && (dx + dy + dz) > 0)
                        {
                            return true;
                        }
                    }
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
            
            int dist = CalculateDistance(creature.X, creature.Y, creature.Z, other.X, other.Y, other.Z);
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

    private void EndRage(Creature creature)
    {
        creature.IsRaging = false;
        creature.RageTurnsLeft = 0;
    }

    /// <summary>
    /// Takes the Hide action: the creature makes a Dexterity (Stealth) check.
    /// If the result exceeds the passive Wisdom (Perception) of all nearby observers,
    /// the creature becomes hidden and gains the benefits of being an unseen attacker.
    /// Requires that no enemy has direct line of sight to the creature (PHB "Unseen Attackers and Targets").
    /// Nimble Escape allows taking this as a bonus action (<paramref name="isBonusAction"/> = true).
    /// </summary>
    /// <returns>True if the action was successfully taken; false if the required resource is unavailable.</returns>
    public bool Hide(Creature creature, bool isBonusAction = false, VisionSystem? visionSystem = null)
    {
        if (_inCombat)
        {
            if (isBonusAction)
            {
                if (!creature.HasBonusAction) return false;
            }
            else
            {
                if (!creature.HasAction) return false;
            }
        }

        // Cannot hide while an enemy has direct line of sight.
        if (visionSystem != null)
        {
            bool visibleToEnemy = _combatants
                .Any(o => o != creature && o.IsAlive() && o.IsPlayer != creature.IsPlayer && visionSystem.CanSee(o, creature));

            if (visibleToEnemy)
            {
                TurnMessages.Add($"{creature.Name} cannot hide — an enemy can see them!");
                return false;
            }
        }

        if (_inCombat)
        {
            if (isBonusAction)
                creature.HasBonusAction = false;
            else
                creature.HasAction = false;
        }

        int dexMod = DndMath.GetAbilityModifier(creature.Dexterity);
        int profBonus = DndMath.GetProficiencyBonus(creature.Level);
        int stealthBonus = dexMod + (creature.StealthProficiency ? profBonus : 0);
        int roll = _random.Next(1, 21);
        int stealthResult = roll + stealthBonus;

        creature.HiddenStealthResult = stealthResult;

        bool detected = _combatants
            .Where(o => o != creature && o.IsAlive() && o.IsPlayer != creature.IsPlayer)
            .Any(o => o.PassivePerception >= stealthResult);

        if (detected)
        {
            creature.IsHidden = false;
            TurnMessages.Add($"{creature.Name} tried to hide (Stealth {stealthResult}) but was detected!");
        }
        else
        {
            creature.IsHidden = true;
            TurnMessages.Add($"{creature.Name} hides! (Stealth check: {roll} + {stealthBonus} = {stealthResult})");
        }

        return true;
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
        if (helper.IsPlayer == target.IsPlayer)
        {
            TurnMessages.Add($"{helper.Name} can only Help against an enemy.");
            return false;
        }

        if (!IsInMeleeRange(helper, target))
        {
            TurnMessages.Add($"{helper.Name} cannot Help — {target.Name} is not within 5 feet!");
            return false;
        }

        if (_inCombat)
            helper.HasAction = false;
        target.IsBeingHelped = true;
        TurnMessages.Add($"{helper.Name} uses Help to distract {target.Name}! Next attack against {target.Name} has advantage.");
        return true;
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
    
    public string GetMessage()
    {
        string advantageText = "";
        if (HasAdvantage) advantageText = " (ADV)";
        if (HasDisadvantage) advantageText = " (DIS)";
        
        if (IsCriticalMiss)
            return $"{Attacker.Name} critically missed {Target.Name}!{advantageText}";
        if (IsCritical)
            return $"{Attacker.Name} critically hit {Target.Name} for {Damage} damage!{advantageText}";
        if (IsHit)
            return $"{Attacker.Name} hit {Target.Name} for {Damage} damage! (AC {Target.ArmorClass}, rolled {AttackRoll}+{TotalAttackBonus}={TotalToHit}){advantageText}";
        
        return $"{Attacker.Name} missed {Target.Name}! (AC {Target.ArmorClass}, rolled {AttackRoll}+{TotalAttackBonus}={TotalToHit}){advantageText}";
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
