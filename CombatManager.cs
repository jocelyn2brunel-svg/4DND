using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public class CombatManager
{
    private readonly List<Creature> _combatants = new();
    private int _currentTurnIndex = 0;
    private bool _inCombat = false;
    private readonly Random _random = new();
    
    public bool InCombat => _inCombat;
    public List<Creature> Combatants => _combatants;
    public Creature? CurrentCombatant => _inCombat && _combatants.Count > 0 ? _combatants[_currentTurnIndex] : null;
    
    public void StartCombat(List<Creature> creatures)
    {
        _combatants.Clear();
        _combatants.AddRange(creatures);
        
        // Roll initiative for all creatures
        foreach (var creature in _combatants)
        {
            int dexMod = creature.GetAbilityModifier(creature.Dexterity);
            creature.Initiative = RollD20() + dexMod;
        }
        
        // Sort by initiative (descending)
        _combatants.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));
        
        _currentTurnIndex = 0;
        _inCombat = true;
    }
    
    public void EndCombat()
    {
        _inCombat = false;
        _combatants.Clear();
        _currentTurnIndex = 0;
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
        _currentTurnIndex = (_currentTurnIndex + 1) % _combatants.Count;
    }
    
    public AttackResult MakeAttack(Creature attacker, Creature target, VisionSystem? visionSystem = null)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            Target = target
        };
        
        // Check if attacker can see target (requires VisionSystem)
        bool attackerCanSee = !attacker.IsBlinded();
        bool targetIsInvisible = target.Conditions.HasCondition(Condition.Invisible);
        
        // Roll attack (with advantage/disadvantage)
        int attackRoll = RollD20();
        bool hasAdvantage = targetIsInvisible || target.Conditions.HasCondition(Condition.Prone) || 
                           target.Conditions.HasCondition(Condition.Paralyzed) || 
                           target.Conditions.HasCondition(Condition.Unconscious);
        bool hasDisadvantage = attacker.IsBlinded() || target.Conditions.HasCondition(Condition.Invisible);
        
        // Check for sunlight sensitivity
        if (visionSystem != null && attacker.HasSunlightSensitivity)
        {
            var lightLevel = visionSystem.GetLightLevel(attacker.X, attacker.Y);
            if (visionSystem.GlobalDaylight || lightLevel == LightType.Bright)
            {
                hasDisadvantage = true;
            }
        }
        
        // Apply advantage/disadvantage
        if (hasAdvantage && !hasDisadvantage)
        {
            int roll2 = RollD20();
            attackRoll = Math.Max(attackRoll, roll2);
            result.HasAdvantage = true;
        }
        else if (hasDisadvantage && !hasAdvantage)
        {
            int roll2 = RollD20();
            attackRoll = Math.Min(attackRoll, roll2);
            result.HasDisadvantage = true;
        }
        
        result.AttackRoll = attackRoll;
        result.TotalAttackBonus = attacker.AttackBonus;
        result.TotalToHit = attackRoll + attacker.AttackBonus;
        
        // Check for critical hit or miss
        if (attackRoll == 20)
        {
            result.IsCritical = true;
            result.IsHit = true;
        }
        else if (attackRoll == 1)
        {
            result.IsCriticalMiss = true;
            result.IsHit = false;
        }
        else
        {
            result.IsHit = result.TotalToHit >= target.ArmorClass;
        }
        
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
    
    public (int x, int y)? FindNearestEnemy(Creature creature)
    {
        Creature? nearest = null;
        int minDist = int.MaxValue;
        
        foreach (var other in _combatants)
        {
            if (other.IsPlayer == creature.IsPlayer || !other.IsAlive()) continue;
            
            int dist = Math.Abs(other.X - creature.X) + Math.Abs(other.Y - creature.Y);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = other;
            }
        }
        
        return nearest != null ? (nearest.X, nearest.Y) : null;
    }
    
    public bool IsInMeleeRange(Creature attacker, Creature target)
    {
        int dx = Math.Abs(attacker.X - target.X);
        int dy = Math.Abs(attacker.Y - target.Y);
        return dx <= 1 && dy <= 1 && (dx + dy) > 0; // Adjacent but not same tile
    }
    
    public Creature? GetCreatureAt(int x, int y)
    {
        return _combatants.FirstOrDefault(c => c.X == x && c.Y == y && c.IsAlive());
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
