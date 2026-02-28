using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public partial class CombatManager
{
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
                TurnMessages.Add($"{creature.Name} hits the obstacle! (Athletes {roll}+{athleticsBonus}={total} vs DC 10)");
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
}
