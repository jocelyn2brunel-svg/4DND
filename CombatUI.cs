using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DND;

public class CombatUI
{
    private SpriteFont _font;
    private Texture2D _pixel;
    private HashSet<char> _supportedChars;

    public CombatUI(SpriteFont font, Texture2D pixel, HashSet<char> supportedChars)
    {
        _font = font;
        _pixel = pixel;
        _supportedChars = supportedChars;
    }

    private string SafeString(string text)
    {
        if (_font == null || text == null) return text ?? "";
        char[] chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!_supportedChars.Contains(chars[i]))
                chars[i] = '?';
        }
        return new string(chars);
    }

    public void Draw(SpriteBatch spriteBatch, Viewport vp, CombatManager combatManager, List<string> combatLog, bool showVisionOverlay, Creature? playerCreature, int currentViewLevel, CombatAction selectedAction)
    {
        if (_font == null) return;

        // Combat panel at top
        int panelHeight = 220;
        var combatPanel = new Rectangle(0, 0, vp.Width, panelHeight);
        spriteBatch.Draw(_pixel, combatPanel, Color.Black * 0.8f);

        int y = 10;

        // Round counter
        var roundText = $"=== ROUND {combatManager.CurrentRound} ===";
        var roundSize = _font.MeasureString(roundText);
        spriteBatch.DrawString(_font, roundText, new Vector2((vp.Width - roundSize.X) / 2, y), Color.Gold);
        y += 30;

        // Current turn
        var currentCombatant = combatManager.CurrentCombatant;
        if (currentCombatant != null)
        {
            var turnText = $"Turn: {SafeString(currentCombatant.Name)} (HP: {currentCombatant.CurrentHP}/{currentCombatant.MaxHP})";
            spriteBatch.DrawString(_font, turnText, new Vector2(10, y), Color.Yellow);
            y += 25;

            // Action economy display
            var actionIcon = currentCombatant.HasAction ? "[✓]" : "[X]";
            var bonusIcon = currentCombatant.HasBonusAction ? "[✓]" : "[X]";
            var reactionIcon = currentCombatant.HasReaction ? "[✓]" : "[X]";
            var movementText = $"{currentCombatant.MovementRemaining}/{currentCombatant.Speed}ft";

            var actionColor = currentCombatant.HasAction ? Color.Green : Color.DarkGray;
            var bonusColor = currentCombatant.HasBonusAction ? Color.Green : Color.DarkGray;
            var reactionColor = currentCombatant.HasReaction ? Color.Green : Color.DarkGray;
            var movementColor = currentCombatant.MovementRemaining > 0 ? Color.Cyan : Color.DarkGray;

            spriteBatch.DrawString(_font, "Action:", new Vector2(10, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, SafeString(actionIcon), new Vector2(80, y), actionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            spriteBatch.DrawString(_font, "Bonus:", new Vector2(130, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, SafeString(bonusIcon), new Vector2(200, y), bonusColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            spriteBatch.DrawString(_font, "Reaction:", new Vector2(250, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, SafeString(reactionIcon), new Vector2(340, y), reactionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            spriteBatch.DrawString(_font, "Move:", new Vector2(390, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, movementText, new Vector2(450, y), movementColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 25;

            // Initiative order
            var initText = "Initiative: ";
            for (int i = 0; i < combatManager.Combatants.Count && i < 5; i++)
            {
                var c = combatManager.Combatants[i];
                initText += $"{SafeString(c.Name)}({c.Initiative}) ";
            }
            spriteBatch.DrawString(_font, initText, new Vector2(10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 25;

            // Player actions
            if (currentCombatant.IsPlayer)
            {
                spriteBatch.DrawString(_font, "Actions: [1] Move  [2] Attack  [3] End Turn", new Vector2(10, y), Color.White);
                y += 25;

                if (selectedAction != CombatAction.None)
                {
                    var actionText = selectedAction switch
                    {
                        CombatAction.Move => "Click on an empty tile to move",
                        CombatAction.Attack => "Click on an enemy to attack",
                        _ => ""
                    };
                    spriteBatch.DrawString(_font, actionText, new Vector2(10, y), Color.Yellow);
                    y += 25;
                }
            }
            else
            {
                spriteBatch.DrawString(_font, "Enemy turn...", new Vector2(10, y), Color.Red);
                y += 25;
            }
        }

        // Combat log
        y = panelHeight - 100;
        spriteBatch.DrawString(_font, "Combat Log:", new Vector2(10, y), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        y += 20;

        for (int i = Math.Max(0, combatLog.Count - 4); i < combatLog.Count; i++)
        {
            spriteBatch.DrawString(_font, combatLog[i], new Vector2(10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 18;
        }

        // Vision legend (right side)
        if (showVisionOverlay)
        {
            DrawVisionLegend(spriteBatch, vp, playerCreature);
        }

        // Instructions
        var hint = "Press Tab to toggle combat UI | ESC for menu | PageUp/Down: Change level";
        var hintSize = _font.MeasureString(hint);
        spriteBatch.DrawString(_font, hint, new Vector2(vp.Width - hintSize.X - 10, panelHeight - 25), Color.White * 0.7f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        // Test keybinding
        var testHint = "Test: [B]linded [F]og [K]Darkness | [Space]Fly [R]Up [T]Down | [X]STR [Z]Stealth [N]Save";
        var testHintSize = _font.MeasureString(testHint);
        spriteBatch.DrawString(_font, testHint, new Vector2(vp.Width - testHintSize.X - 10, panelHeight - 50), Color.Yellow * 0.6f, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        // Display current view level
        var levelHint = $"View Level: Z{currentViewLevel}";
        if (playerCreature != null && playerCreature.CanFly)
        {
            levelHint += $" | Player: Z{playerCreature.Z} {(playerCreature.IsFlying ? "[FLYING]" : "[GROUND]")}";
        }
        spriteBatch.DrawString(_font, levelHint, new Vector2(10, panelHeight - 25), Color.Cyan * 0.8f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawVisionLegend(SpriteBatch spriteBatch, Viewport vp, Creature? playerCreature)
    {
        int legendX = vp.Width - 280;
        int legendY = 70;

        spriteBatch.DrawString(_font, "Vision Legend:", new Vector2(legendX, legendY), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        legendY += 25;

        // Bright light
        spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.White);
        spriteBatch.DrawString(_font, "Bright Light", new Vector2(legendX + 25, legendY), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        legendY += 25;

        // Dim light
        spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), new Color(128, 128, 128));
        spriteBatch.DrawString(_font, "Dim Light (Lightly Obscured)", new Vector2(legendX + 25, legendY), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        legendY += 25;

        // Darkness with darkvision
        if (playerCreature != null && playerCreature.DarkvisionRange > 0)
        {
            spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), new Color(96, 96, 96));
            spriteBatch.DrawString(_font, "Darkness (Darkvision, Grayscale)", new Vector2(legendX + 25, legendY), new Color(150, 150, 180), 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            legendY += 25;
        }

        // Complete darkness
        spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.Black);
        spriteBatch.DrawString(_font, "Darkness (Heavily Obscured)", new Vector2(legendX + 25, legendY), Color.DarkGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        legendY += 25;

        // Difficult terrain
        spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.Sienna);
        spriteBatch.DrawString(_font, "Difficult Terrain (2x Cost)", new Vector2(legendX + 25, legendY), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        legendY += 25;

        // Wall
        spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.DarkSlateGray);
        spriteBatch.DrawString(_font, "Wall (Blocks Move/Vision)", new Vector2(legendX + 25, legendY), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        legendY += 35;

        // Creature indicators
        spriteBatch.DrawString(_font, "Creature Indicators (top of unit):", new Vector2(legendX, legendY), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        legendY += 20;

        DrawIndicator(spriteBatch, legendX, ref legendY, Color.Gold, "Truesight (See All)");
        DrawIndicator(spriteBatch, legendX, ref legendY, Color.Cyan, "Blindsight");
        DrawIndicator(spriteBatch, legendX, ref legendY, Color.Orange, "Tremorsense");
        DrawIndicator(spriteBatch, legendX, ref legendY, Color.Purple, "Superior Darkvision 120ft");
        DrawIndicator(spriteBatch, legendX, ref legendY, Color.Yellow, "Darkvision 60ft");
        DrawIndicator(spriteBatch, legendX, ref legendY, Color.Orange, "Sunlight Sensitivity");
        DrawIndicator(spriteBatch, legendX, ref legendY, Color.Red, "Has Condition");
    }

    private void DrawIndicator(SpriteBatch spriteBatch, int x, ref int y, Color color, string label)
    {
        spriteBatch.Draw(_pixel, new Rectangle(x, y, 5, 5), color);
        spriteBatch.DrawString(_font, label, new Vector2(x + 10, y - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        y += 18;
    }
}
