using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public partial class CombatManager
{
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
    /// Attempts to move the creature to the target position, returning true if the move was valid.
    /// Combines CanMove + Move into a single pathfinding call to avoid redundant A* searches.
    /// </summary>
    public bool TryMove(Creature creature, int targetX, int targetY, int targetZ, VisionSystem? visionSystem = null, bool ignoreCost = false)
    {
        if (!ignoreCost && creature.MovementRemaining <= 0)
            return false;

        if (!CanOccupySpace(creature.Size, targetX, targetY, targetZ, creature))
            return false;

        var path = FindPath(creature, targetX, targetY, targetZ);
        if (path == null)
            return false;

        if (!ignoreCost)
        {
            int totalCost = CalculatePathCost(creature, path);
            if (totalCost > creature.MovementRemaining)
                return false;
        }

        EnqueuePathSteps(creature, path, ignoreCost);
        return true;
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

        EnqueuePathSteps(creature, path, ignoreCost);
    }

    private void EnqueuePathSteps(Creature creature, List<TacticalMapNode> path, bool ignoreCost)
    {
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

    // --- Pathfinding ---

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
        int dx = System.Math.Abs(from.X - to.X);
        int dy = System.Math.Abs(from.Y - to.Y);
        int dz = System.Math.Abs(from.Z - to.Z);
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
}
