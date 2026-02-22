using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public class CombatManager
{
    public InfiniteGrid3D<TileType>? Grid { get; set; }
    private readonly List<Creature> _combatants = new();
    private int _currentTurnIndex = 0;
    private int _currentRound = 0;
    private bool _inCombat = false;
    private readonly Random _random = new();
    
    public bool InCombat => _inCombat;
    public List<Creature> Combatants => _combatants;
    public Creature? CurrentCombatant => _inCombat && _combatants.Count > 0 ? _combatants[_currentTurnIndex] : null;
    public int CurrentRound => _currentRound;
    
    public void StartCombat(List<Creature> creatures)
    {
        _combatants.Clear();
        _combatants.AddRange(creatures);
        
        // Roll initiative for all creatures
        foreach (var creature in _combatants)
        {
            int dexMod = creature.GetAbilityModifier(creature.Dexterity);
            creature.Initiative = RollD20() + dexMod;
            
            // Reset turn resources
            creature.HasAction = true;
            creature.HasBonusAction = true;
            creature.HasReaction = true;
            creature.MovementRemaining = creature.Speed;
        }
        
        // Sort by initiative (descending)
        _combatants.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));
        
        _currentTurnIndex = 0;
        _currentRound = 1;
        _inCombat = true;
    }
    
    public void EndCombat()
    {
        _inCombat = false;
        _combatants.Clear();
        _currentTurnIndex = 0;
        _currentRound = 0;
    }
    
    public void NextTurn()
    {
        if (!_inCombat || _combatants.Count == 0) return;
        
        // Remove dead creatures
        _combatants.RemoveAll(c => !c.IsAlive());
        
        // Check if combat should end
        bool hasPlayer = _combatants.Any(c => c.IsPlayer);
        bool hasEnemy = _combatants.Any(c => !c.IsPlayer);
        
        if (!hasPlayer || !hasEnemy)
        {
            EndCombat();
            return;
        }
        
        // Move to next turn
        _currentTurnIndex++;
        
        // Check if we completed a round
        if (_currentTurnIndex >= _combatants.Count)
        {
            _currentTurnIndex = 0;
            _currentRound++;
            
            // Start of new round - refresh all combatants' actions
            foreach (var creature in _combatants)
            {
                creature.HasAction = true;
                creature.HasBonusAction = true;
                creature.HasReaction = true;
                creature.MovementRemaining = creature.Speed;
                
                // Process ongoing effects (poison, etc.)
                ProcessStartOfTurnEffects(creature);
            }
        }
        
        // Refresh current combatant's actions (redundant for first turn of round but safe)
        if (CurrentCombatant != null)
        {
            CurrentCombatant.HasAction = true;
            CurrentCombatant.HasBonusAction = true;
            CurrentCombatant.HasReaction = true;
            CurrentCombatant.MovementRemaining = CurrentCombatant.Speed;
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
    
    public bool CanMove(Creature creature, int targetX, int targetY, int targetZ)
    {
        if (!creature.HasAction && creature.MovementRemaining <= 0)
            return false;

        if (Grid != null && Grid.Get(targetX, targetY, targetZ) == TileType.Wall)
            return false;
        
        int distance = CalculateDistance(creature.X, creature.Y, creature.Z, targetX, targetY, targetZ);
        if (distance == 0) return true;

        // Check corners for single-step diagonal movement
        if (distance == 1 && !IsDiagonalMoveAllowed(creature.X, creature.Y, creature.Z, targetX, targetY, targetZ))
            return false;

        int totalCost = CalculateMovementCost(creature.X, creature.Y, creature.Z, targetX, targetY, targetZ);
        
        return totalCost <= creature.MovementRemaining;
    }
    
    public void Move(Creature creature, int targetX, int targetY, int targetZ)
    {
        int totalCost = CalculateMovementCost(creature.X, creature.Y, creature.Z, targetX, targetY, targetZ);
        
        creature.X = targetX;
        creature.Y = targetY;
        creature.Z = targetZ;
        creature.MovementRemaining = Math.Max(0, creature.MovementRemaining - totalCost);
    }

    private int CalculateMovementCost(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        int distance = CalculateDistance(x1, y1, z1, x2, y2, z2);
        if (distance == 0) return 0;

        // Base cost: 5ft per square (Chebyshev)
        int cost = distance * 5;

        // If target square is difficult terrain, it costs 5ft extra
        if (Grid != null && Grid.Get(x2, y2, z2) == TileType.DifficultTerrain)
            cost += 5;

        return cost;
    }

    private bool IsDiagonalMoveAllowed(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        if (Grid == null) return true;

        int dx = x2 - x1;
        int dy = y2 - y1;
        int dz = z2 - z1;

        // 2D diagonals check
        if (Math.Abs(dx) == 1 && Math.Abs(dy) == 1 && dz == 0)
        {
            if (Grid.Get(x1 + dx, y1, z1) == TileType.Wall || Grid.Get(x1, y1 + dy, z1) == TileType.Wall)
                return false;
        }
        if (Math.Abs(dx) == 1 && Math.Abs(dz) == 1 && dy == 0)
        {
            if (Grid.Get(x1 + dx, y1, z1) == TileType.Wall || Grid.Get(x1, y1, z1 + dz) == TileType.Wall)
                return false;
        }
        if (Math.Abs(dy) == 1 && Math.Abs(dz) == 1 && dx == 0)
        {
            if (Grid.Get(x1, y1 + dy, z1) == TileType.Wall || Grid.Get(x1, y1, z1 + dz) == TileType.Wall)
                return false;
        }

        // 3D diagonal check (all 3 axes change)
        if (Math.Abs(dx) == 1 && Math.Abs(dy) == 1 && Math.Abs(dz) == 1)
        {
            // If any adjacent square that shares a face with the path is a wall, block it
            if (Grid.Get(x1 + dx, y1, z1) == TileType.Wall ||
                Grid.Get(x1, y1 + dy, z1) == TileType.Wall ||
                Grid.Get(x1, y1, z1 + dz) == TileType.Wall)
                return false;
        }

        return true;
    }
    
    public int CalculateDistance(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        // Chebyshev distance (5e Grid Rules: diagonals cost same as straight)
        return Math.Max(Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)), Math.Abs(z2 - z1));
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
        
        // Check if attacker can see target (requires VisionSystem)
        bool attackerCanSee = !attacker.IsBlinded();
        bool targetIsInvisible = target.Conditions.HasCondition(Condition.Invisible);
        
        // Determine advantage/disadvantage
        bool hasAdvantage = targetIsInvisible || target.Conditions.HasCondition(Condition.Prone) || 
                           target.Conditions.HasCondition(Condition.Paralyzed) || 
                           target.Conditions.HasCondition(Condition.Unconscious);
        bool hasDisadvantage = attacker.IsBlinded() || target.Conditions.HasCondition(Condition.Invisible);
        
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
        if (parts.Length == 2 && int.TryParse(parts[0], out int numDice) && int.TryParse(parts[1], out int diceSize))
        {
            for (int r = 0; r < rolls; r++)
            {
                for (int i = 0; i < numDice; i++)
                {
                    total += _random.Next(1, diceSize + 1);
                }
            }
        }
        
        return Math.Max(1, total); // Minimum 1 damage
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
    
    public bool IsInMeleeRange(Creature attacker, Creature target)
    {
        int dist = CalculateDistance(attacker.X, attacker.Y, attacker.Z, target.X, target.Y, target.Z);
        return dist == 1;
    }
    
    public Creature? GetCreatureAt(int x, int y, int z = 0)
    {
        return _combatants.FirstOrDefault(c => c.X == x && c.Y == y && c.Z == z && c.IsAlive());
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
            return $"{Attacker.Name} hit {Target.Name} for {Damage} damage!{advantageText}";
        return $"{Attacker.Name} missed {Target.Name} ({TotalToHit} vs AC {Target.ArmorClass}){advantageText}";
    }
}
