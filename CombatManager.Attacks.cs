using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public partial class CombatManager
{
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
                               attacker.HasArmorNonProficiencyPenalty;

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
                               attacker.HasArmorNonProficiencyPenalty;

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
                               thrower.HasArmorNonProficiencyPenalty;

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
        int attackBonusVal = dexMod;

        var attackCheck = D20CheckFactory.MakeAttackRoll(
            "Acid (vial)",
            attackBonusVal,
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
                               thrower.HasArmorNonProficiencyPenalty;

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
            thrower.GetAbilityModifier(thrower.Dexterity), // Correction ici
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
            result.Damage = RollDamage("1d4", 0, result.IsCritical);
            result.DamageType = DamageType.Fire;
            target.TakeDamage(result.Damage, DamageType.Fire, result.IsCritical);
            target.Conditions = target.Conditions.AddCondition(Condition.Burning);
            thrower.HasAttackedThisRound = true;
            TurnMessages.Add(Loc.Tr("{0} is set ablaze by alchemist's fire!", target.Name));
        }

        return result;
    }
}
