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
            if (!CurrentCombatant.IsSurprised)
            {
                CurrentCombatant.HasAction = true;
                CurrentCombatant.HasBonusAction = true;
                CurrentCombatant.HasReaction = true;
                CurrentCombatant.MovementRemaining = CurrentCombatant.Speed;
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
        // Process ongoing damage effects like poison, burning, etc.
        // This can be extended later for duration-based conditions
        
        // Example: Poisoned creatures might take damage each turn
        if (creature.Conditions.HasCondition(Condition.Poisoned))
        {
            // Future: implement ongoing poison damage
        }
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
                if (tileType == TileType.Wall || tileType == TileType.Empty)
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
    
    public void Move(Creature creature, int targetX, int targetY, int targetZ)
    {
        var path = FindPath(creature, targetX, targetY, targetZ);
        if (path == null)
            return;

        int movementSpent = 0;
        int remaining = creature.MovementRemaining;

        for (int i = 1; i < path.Count; i++)
        {
            int stepCost = GetMoveCost(creature, path[i]);

            if (movementSpent + stepCost > remaining)
                break;

            movementSpent += stepCost;
            creature.MoveTo(path[i].X, path[i].Y, path[i].Z);
        }

        creature.MovementRemaining = Math.Max(0, remaining - movementSpent);

        // Update squeezing state based on final position
        creature.IsSqueezingThrough = WouldRequireSqueeze(creature, creature.X, creature.Y, creature.Z);
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
        var bestCost = new Dictionary<TacticalMapNode, int> { [start] = 0 };
        var open = new PriorityQueue<TacticalMapNode, int>();
        open.Enqueue(start, 0);

        while (open.Count > 0)
        {
            var current = open.Dequeue();
            int currentCost = bestCost[current];

            foreach (var neighbor in GetNeighbors(creature, current))
            {
                int stepCost = GetMoveCost(creature, neighbor);
                int totalCost = currentCost + stepCost;

                if (totalCost > creature.MovementRemaining)
                    continue;

                int knownCost = bestCost.GetValueOrDefault(neighbor, int.MaxValue);
                if (totalCost >= knownCost)
                    continue;

                bestCost[neighbor] = totalCost;
                open.Enqueue(neighbor, totalCost);
                reachable.Add((neighbor.X, neighbor.Y, neighbor.Z));
            }
        }

        return reachable;
    }

    public (int x, int y, int z)? GetNextStepTowards(Creature creature, Creature target)
    {
        List<TacticalMapNode>? bestPath = null;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = (creature.CanFly ? -1 : 0); dz <= (creature.CanFly ? 1 : 0); dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    int tx = target.X + dx;
                    int ty = target.Y + dy;
                    int tz = target.Z + dz;

                    if (!CanOccupySpace(creature.Size, tx, ty, tz, creature))
                        continue;

                    var path = FindPath(creature, tx, ty, tz);
                    if (path == null || path.Count < 2)
                        continue;

                    if (bestPath == null || CalculatePathCost(creature, path) < CalculatePathCost(creature, bestPath))
                        bestPath = path;
                }
            }
        }

        if (bestPath == null)
            return null;

        var step = bestPath[1];
        return (step.X, step.Y, step.Z);
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
        var fScore = new Dictionary<TacticalMapNode, int> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            var current = openSet.OrderBy(n => fScore.GetValueOrDefault(n, int.MaxValue)).First();
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(creature, current))
            {
                int tentativeTurns = turnCount[current] + GetTurnPenalty(cameFrom, current, neighbor);
                int tentativeG = gScore[current] + GetMoveCost(creature, neighbor);
                int currentBestG = gScore.GetValueOrDefault(neighbor, int.MaxValue);
                int currentBestTurns = turnCount.GetValueOrDefault(neighbor, int.MaxValue);

                if (tentativeG > currentBestG)
                    continue;
                if (tentativeG == currentBestG && tentativeTurns >= currentBestTurns)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                turnCount[neighbor] = tentativeTurns;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        return null;
    }

    private IEnumerable<TacticalMapNode> GetNeighbors(Creature creature, TacticalMapNode node)
    {
        int minDz = creature.CanFly ? -1 : 0;
        int maxDz = creature.CanFly ? 1 : 0;

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

                    if (!CanOccupySpace(creature.Size, nx, ny, nz, creature))
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

    private int GetMoveCost(Creature creature, TacticalMapNode node)
    {
        int baseCost = TacticalMap != null && TacticalMap.Get(node.X, node.Y, node.Z) == TileType.DifficultTerrain ? 10 : 5;

        // If squeezing is required, double the movement cost
        if (WouldRequireSqueeze(creature, node.X, node.Y, node.Z))
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
        for (int i = 1; i < path.Count; i++)
            cost += GetMoveCost(creature, path[i]);

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

            if (TacticalMap.Get(cx, cy, cz) == TileType.Wall)
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
        for (int i = 1; i <= dist; i++)
        {
            float t = (float)i / dist;
            int cx = (int)Math.Round(x1 + (x2 - x1) * t);
            int cy = (int)Math.Round(y1 + (y2 - y1) * t);
            int cz = (int)Math.Round(z1 + (z2 - z1) * t);

            totalCost += 5;
            if (TacticalMap != null && TacticalMap.Get(cx, cy, cz) == TileType.DifficultTerrain)
                totalCost += 5;
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
            if (TacticalMap.Get(x1 + dx, y1, z1) == TileType.Wall || TacticalMap.Get(x1, y1 + dy, z1) == TileType.Wall)
                return false;
        }
        if (Abs(dx) == 1 && Abs(dz) == 1 && dy == 0)
        {
            if (TacticalMap.Get(x1 + dx, y1, z1) == TileType.Wall || TacticalMap.Get(x1, y1, z1 + dz) == TileType.Wall)
                return false;
        }
        if (Abs(dy) == 1 && Abs(dz) == 1 && dx == 0)
        {
            if (TacticalMap.Get(x1, y1 + dy, z1) == TileType.Wall || TacticalMap.Get(x1, y1, z1 + dz) == TileType.Wall)
                return false;
        }

        // 3D diagonal check (all 3 axes change)
        if (Abs(dx) == 1 && Abs(dy) == 1 && Abs(dz) == 1)
        {
            // If any adjacent square that shares a face with the path is a wall, block it
            if (TacticalMap.Get(x1 + dx, y1, z1) == TileType.Wall ||
                TacticalMap.Get(x1, y1 + dy, z1) == TileType.Wall ||
                TacticalMap.Get(x1, y1, z1 + dz) == TileType.Wall)
                return false;
        }

        return true;
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
        if (!attacker.HasAction)
        {
            result.IsHit = false;
            return result;
        }
        
        // Consume the action
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
                           target.IsSqueezingThrough;  // Attack rolls against a squeezing creature have advantage
        bool hasDisadvantage = !attackerCanSee ||
                               attacker.IsSqueezingThrough;  // Squeezing creature has disadvantage on attack rolls
        
        // Check for sunlight sensitivity (circumstantial disadvantage)
        if (visionSystem != null && attacker.HasSunlightSensitivity)
        {
            var lightLevel = visionSystem.GetLightLevel(attacker.X, attacker.Y, attacker.Z);
            if (visionSystem.GlobalDaylight || lightLevel == LightType.Bright)
            {
                hasDisadvantage = true;
            }
        }
        
        // Make attack roll using D20Check system
        var attackCheck = D20CheckFactory.MakeAttackRoll(
            attacker.AttackName,
            attacker.AttackBonus,
            target.ArmorClass,
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
            result.Damage = RollDamage(attacker.DamageDice, attacker.DamageBonus, result.IsCritical);
            target.TakeDamage(result.Damage);
        }
        
        return result;
    }
    
    public int RollD20()
    {
        return _random.Next(1, 21);
    }
    
    public int RollDamage(string damageDice, int bonus, bool isCritical)
    {
        int total = bonus;
        int rolls = isCritical ? 2 : 1;
        
        // Parse dice string (e.g., "1d6", "2d4")
        if (int.TryParse(damageDice, out int fixedDamage))
        {
            return fixedDamage + bonus;
        }
        
        var parts = damageDice.Split('d');
        if (parts.Length != 2) return total;
        
        if (int.TryParse(parts[0], out int numDice) && int.TryParse(parts[1], out int diceType))
        {
            // Roll the dice
            for (int i = 0; i < numDice * rolls; i++)
            {
                total += RollD(diceType);
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
        // Squeezing: disadvantage on Dexterity saving throws
        bool isDexSave = abilityName is "DEX" or "Dexterity";
        if (creature.IsSqueezingThrough && isDexSave)
            hasDisadvantage = true;

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
}

public class AttackResult
{
    public Creature Attacker { get; set; } = null!;
    public Creature Target { get; set; } = null!;
    public int AttackRoll { get; set; }
    public int TotalAttackBonus { get; set; }
    public int TotalToHit { get; set; }
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
