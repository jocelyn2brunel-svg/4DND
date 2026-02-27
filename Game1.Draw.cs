using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace _4DND;

public partial class Game1
{
    private void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        float distance = Vector2.Distance(start, end);
        float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        sb.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(distance, thickness), SpriteEffects.None, 0f);
    }
    
    
    private string SafeString(string text)
    {
        if (_font == null || text == null) return text ?? "";
        
        char[] chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '\n' || chars[i] == '\r') continue;
            if (!_supportedChars.Contains(chars[i]))
                chars[i] = '?';
        }
        return new string(chars);
    }

    private Rectangle GetInventoryButtonRect(Viewport viewport)
    {
        const int buttonWidth = 170;
        const int buttonHeight = 40;
        const int margin = 12;
        return new Rectangle(viewport.Width - buttonWidth - margin, margin, buttonWidth, buttonHeight);
    }

    private Rectangle GetMapButtonRect(Viewport viewport)
    {
        const int buttonWidth = 170;
        const int buttonHeight = 40;
        const int margin = 12;
        const int topOffset = 52;
        return new Rectangle(viewport.Width - buttonWidth - margin, margin + topOffset, buttonWidth, buttonHeight);
    }

    private Rectangle GetSpawnButtonRect(Viewport viewport)
    {
        const int buttonWidth = 170;
        const int buttonHeight = 40;
        const int margin = 12;
        const int topOffset = 104;
        return new Rectangle(viewport.Width - buttonWidth - margin, margin + topOffset, buttonWidth, buttonHeight);
    }

    private Rectangle GetRotateLeftButtonRect(Viewport viewport)
    {
        const int buttonWidth = 80;
        const int buttonHeight = 40;
        const int margin = 12;
        const int spacing = 8;
        return new Rectangle(viewport.Width - buttonWidth * 2 - margin - spacing, viewport.Height - buttonHeight - margin, buttonWidth, buttonHeight);
    }

    private Rectangle GetRotateRightButtonRect(Viewport viewport)
    {
        const int buttonWidth = 80;
        const int buttonHeight = 40;
        const int margin = 12;
        return new Rectangle(viewport.Width - buttonWidth - margin, viewport.Height - buttonHeight - margin, buttonWidth, buttonHeight);
    }

    private Rectangle GetCombatMoveButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int buttonHeight = 36;
        const int spacing = 10;
        int totalWidth = (buttonWidth * 5) + (spacing * 4);
        int startX = (viewport.Width - totalWidth) / 2;
        int y = viewport.Height - buttonHeight - 20;
        return new Rectangle(startX, y, buttonWidth, buttonHeight);
    }

    private Rectangle GetCombatAttackButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int spacing = 10;
        var moveRect = GetCombatMoveButtonRect(viewport);
        return new Rectangle(moveRect.Right + spacing, moveRect.Y, buttonWidth, moveRect.Height);
    }

    private Rectangle GetCombatCastSpellButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int spacing = 10;
        var attackRect = GetCombatAttackButtonRect(viewport);
        return new Rectangle(attackRect.Right + spacing, attackRect.Y, buttonWidth, attackRect.Height);
    }

    private Rectangle GetCombatBonusActionButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int spacing = 10;
        var castSpellRect = GetCombatCastSpellButtonRect(viewport);
        return new Rectangle(castSpellRect.Right + spacing, castSpellRect.Y, buttonWidth, castSpellRect.Height);
    }

    private Rectangle GetCombatEndTurnButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int spacing = 10;
        var bonusRect = GetCombatBonusActionButtonRect(viewport);
        return new Rectangle(bonusRect.Right + spacing, bonusRect.Y, buttonWidth, bonusRect.Height);
    }

    private Rectangle GetCombatDashButtonRect(Viewport viewport)
    {
        var moveRect = GetCombatMoveButtonRect(viewport);
        return new Rectangle(moveRect.X, moveRect.Y - moveRect.Height - 5, moveRect.Width, moveRect.Height);
    }

    private Rectangle GetCombatGrappleButtonRect(Viewport viewport)
    {
        var attackRect = GetCombatAttackButtonRect(viewport);
        return new Rectangle(attackRect.X, attackRect.Y - attackRect.Height - 5, attackRect.Width, attackRect.Height);
    }

    private Rectangle GetCombatThrowAcidButtonRect(Viewport viewport)
    {
        var grappleRect = GetCombatGrappleButtonRect(viewport);
        return new Rectangle(grappleRect.Right + 10, grappleRect.Y, grappleRect.Width, grappleRect.Height);
    }

    private Rectangle GetCombatDisengageButtonRect(Viewport viewport)
    {
        var dashRect = GetCombatDashButtonRect(viewport);
        return new Rectangle(dashRect.Right + 10, dashRect.Y, dashRect.Width, dashRect.Height);
    }

    private Rectangle GetCombatDodgeButtonRect(Viewport viewport)
    {
        var disengageRect = GetCombatDisengageButtonRect(viewport);
        return new Rectangle(disengageRect.Right + 10, disengageRect.Y, disengageRect.Width, disengageRect.Height);
    }

    private Rectangle GetCombatHideButtonRect(Viewport viewport)
    {
        var dodgeRect = GetCombatDodgeButtonRect(viewport);
        return new Rectangle(dodgeRect.Right + 10, dodgeRect.Y, dodgeRect.Width, dodgeRect.Height);
    }

    private Rectangle GetCombatHelpActionButtonRect(Viewport viewport)
    {
        var hideRect = GetCombatHideButtonRect(viewport);
        return new Rectangle(hideRect.Right + 10, hideRect.Y, hideRect.Width, hideRect.Height);
    }

    private Rectangle GetCombatRageButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int buttonHeight = 34;
        var bonusRect = GetCombatBonusActionButtonRect(viewport);
        // Position Rage above the Bonus Action button
        return new Rectangle(bonusRect.X, bonusRect.Y - buttonHeight - 5, buttonWidth, buttonHeight);
    }

    private Rectangle GetCombatBonusHideButtonRect(Viewport viewport)
    {
        var rageRect = GetCombatRageButtonRect(viewport);
        return new Rectangle(rageRect.Right + 10, rageRect.Y, rageRect.Width, rageRect.Height);
    }

    private void DrawCombatActionButton(Rectangle rect, string label, Color baseColor, bool isSelected)
    {
        var mouse = Mouse.GetState();
        bool isHovered = rect.Contains(mouse.Position);
        Color fillColor = isSelected
            ? new Color(Math.Min(baseColor.R + 30, 255), Math.Min(baseColor.G + 30, 255), Math.Min(baseColor.B + 30, 255))
            : baseColor;

        if (isHovered)
        {
            fillColor = new Color(Math.Min(fillColor.R + 20, 255), Math.Min(fillColor.G + 20, 255), Math.Min(fillColor.B + 20, 255));
        }

        _spriteBatch.Draw(_pixel, rect, fillColor * 0.95f);
        DrawBorder(_spriteBatch, _pixel, rect, Color.Black * 0.7f, 2);

        if (_font == null)
            return;

        var labelSize = _font.MeasureString(label);
        var labelPos = new Vector2(
            rect.X + (rect.Width - labelSize.X * 0.65f) / 2,
            rect.Y + (rect.Height - labelSize.Y * 0.65f) / 2);
        _spriteBatch.DrawString(_font, label, labelPos, Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
    }

    private void FlushTurnMessages()
    {
        foreach (var msg in _combatManager.TurnMessages)
            AddToCombatLog(msg);
        _combatManager.TurnMessages.Clear();
    }

    private void CompleteDonDoff(Character character)
    {
        if (character.CurrentDonDoffProcess == null) return;
        var process = character.CurrentDonDoffProcess;
        var item = process.Item;
        if (item == null) return;

        if (process.IsDoffing)
        {
            character.InventoryData.UnequipItemInstance(item);
            AddToCombatLog(Loc.Tr("{0} finished doffing {1}.", character.Name, item.Name));
        }
        else
        {
            if (character.InventoryData.EquipItemInstance(item))
            {
                AddToCombatLog(Loc.Tr("{0} finished donning {1}.", character.Name, item.Name));
            }
        }

        character.CalculateDerivedStats();
        character.CurrentDonDoffProcess = null;

        // Synchronize to creature if it exists
        if (_playerCreature != null && _playerCreature.Name == character.Name)
        {
            _playerCreature.ArmorClass = character.ArmorClass;
            _playerCreature.Speed = character.Speed;
            _playerCreature.DarkvisionRange = character.DarkvisionRange;
            _playerCreature.HasArmorNonProficiencyPenalty = character.IsWearingNonProficientArmor;
            _playerCreature.CurrentDonDoffProcess = null;
            UpdateVision();
        }
    }

    private void EndCurrentPlayerTurn(Creature currentCombatant)
    {
        _showBonusActionMenu = false;
        int prevRound = _combatManager.CurrentRound;
        _combatManager.NextTurn();
        FlushTurnMessages();
        int newRound = _combatManager.CurrentRound;

        int xpEarned = _combatManager.CollectPendingXP();
        if (xpEarned > 0 && _currentCharacter != null)
        {
            bool leveledUp = _currentCharacter.GainXP(xpEarned);
                AddToCombatLog(Loc.Tr("Gained {0} XP!", xpEarned));
            if (leveledUp)
                    AddToCombatLog(Loc.Tr("Level up! Now level {0}!", _currentCharacter.Level));
        }

        if (newRound > prevRound)
        {
            AddToCombatLog(Loc.Tr("=== Round {0} ===", newRound));
        }

        AddToCombatLog(Loc.Tr("{0} ended turn", currentCombatant.Name));
        _selectedAction = CombatAction.Move;
    }

    private void DrawInventoryButton(Viewport viewport)
    {
        var buttonRect = GetInventoryButtonRect(viewport);
        var mouse = Mouse.GetState();
        bool isHovered = buttonRect.Contains(mouse.Position);

        Color buttonColor = isHovered ? new Color(75, 110, 170) : new Color(45, 70, 120);
        _spriteBatch.Draw(_pixel, buttonRect, buttonColor * 0.95f);

        if (_font != null)
        {
            string label = Loc.Tr("Inventory [C]");
            var labelSize = _font.MeasureString(label);
            var labelPos = new Vector2(
                buttonRect.X + (buttonRect.Width - labelSize.X * 0.75f) / 2,
                buttonRect.Y + (buttonRect.Height - labelSize.Y * 0.75f) / 2);
            _spriteBatch.DrawString(_font, label, labelPos, Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }
    }

    private void DrawMapButton(Viewport viewport, bool isCloseButton)
    {
        var buttonRect = GetMapButtonRect(viewport);
        var mouse = Mouse.GetState();
        bool isHovered = buttonRect.Contains(mouse.Position);

        Color buttonColor = isCloseButton
            ? (isHovered ? new Color(165, 80, 80) : new Color(130, 60, 60))
            : (isHovered ? new Color(90, 130, 85) : new Color(65, 105, 60));

        _spriteBatch.Draw(_pixel, buttonRect, buttonColor * 0.95f);

        if (_font != null)
        {
            string label = Loc.Tr(isCloseButton ? "Close Map [M]" : "Open Map [M]");
            var labelSize = _font.MeasureString(label);
            var labelPos = new Vector2(
                buttonRect.X + (buttonRect.Width - labelSize.X * 0.75f) / 2,
                buttonRect.Y + (buttonRect.Height - labelSize.Y * 0.75f) / 2);
            _spriteBatch.DrawString(_font, label, labelPos, Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }
    }

    private void DrawSpawnButton(Viewport viewport)
    {
        var buttonRect = GetSpawnButtonRect(viewport);
        var mouse = Mouse.GetState();
        bool isHovered = buttonRect.Contains(mouse.Position);

        Color buttonColor = isHovered ? new Color(150, 95, 45) : new Color(120, 70, 30);
        _spriteBatch.Draw(_pixel, buttonRect, buttonColor * 0.95f);

        if (_font != null)
        {
            const string label = "Spawn";
            var labelSize = _font.MeasureString(label);
            var labelPos = new Vector2(
                buttonRect.X + (buttonRect.Width - labelSize.X * 0.8f) / 2,
                buttonRect.Y + (buttonRect.Height - labelSize.Y * 0.8f) / 2);
            _spriteBatch.DrawString(_font, label, labelPos, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
    }

    private void DrawRotationButtons(Viewport viewport)
    {
        DrawRotationButton(GetRotateLeftButtonRect(viewport), "Q");
        DrawRotationButton(GetRotateRightButtonRect(viewport), "E");
    }

    private void DrawRotationButton(Rectangle buttonRect, string label)
    {
        var mouse = Mouse.GetState();
        bool isHovered = buttonRect.Contains(mouse.Position);
        Color buttonColor = isHovered ? new Color(130, 130, 160) : new Color(95, 95, 120);
        _spriteBatch.Draw(_pixel, buttonRect, buttonColor * 0.95f);

        if (_font == null)
            return;

        var labelSize = _font.MeasureString(label);
        var labelPos = new Vector2(
            buttonRect.X + (buttonRect.Width - labelSize.X * 0.8f) / 2,
            buttonRect.Y + (buttonRect.Height - labelSize.Y * 0.8f) / 2);
        _spriteBatch.DrawString(_font, label, labelPos, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawTravelProgress(SpriteBatch sb, Viewport vp, Campaign campaign)
    {
        if (!campaign.IsTraveling) return;

        var currentPos = new Vector2(campaign.PartyX, campaign.PartyY);
        var targetPos = new Vector2(campaign.TargetPartyX, campaign.TargetPartyY);
        float totalDist = Vector2.Distance(campaign.StartPartyPos, targetPos);
        float currentDist = Vector2.Distance(currentPos, targetPos);
        float progress = totalDist > 0.1f ? 1f - (currentDist / totalDist) : 1f;
        progress = MathHelper.Clamp(progress, 0f, 1f);

        var barRect = new Rectangle(vp.Width / 2 - 200, vp.Height - 100, 400, 20);
        sb.Draw(_pixel, barRect, Color.Black * 0.5f);
        sb.Draw(_pixel, new Rectangle(barRect.X, barRect.Y, (int)(barRect.Width * progress), barRect.Height), Color.LimeGreen);
        DrawBorder(sb, _pixel, barRect, Color.White, 1);

        string msg = Loc.Tr("Traveling... {0:P0}", progress);
        if (_font != null)
        {
            var size = _font.MeasureString(msg) * 0.8f;
            sb.DrawString(_font, msg, new Vector2(vp.Width / 2 - size.X / 2, barRect.Y - 25), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
    }

    private void DrawTravelMessage(SpriteBatch sb, Viewport vp, Campaign campaign)
    {
        if (string.IsNullOrEmpty(campaign.LastTravelMessage) || _font == null) return;

        var size = _font.MeasureString(campaign.LastTravelMessage) * 0.9f;
        var pos = new Vector2(vp.Width / 2 - size.X / 2, 100);

        sb.Draw(_pixel, new Rectangle((int)pos.X - 10, (int)pos.Y - 5, (int)size.X + 20, (int)size.Y + 10), Color.Black * 0.7f);
        DrawBorder(sb, _pixel, new Rectangle((int)pos.X - 10, (int)pos.Y - 5, (int)size.X + 20, (int)size.Y + 10), Color.Orange, 1);
        sb.DrawString(_font, campaign.LastTravelMessage, pos, Color.Yellow, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
    }

    private bool IsMouseOverCombatUi(Point mousePosition, Viewport viewport)
    {
        if (!_showCombatUI)
            return false;

        if (mousePosition.Y <= _combatTopPanelHeight)
            return true;

        if (_combatLogWindowRect.Contains(mousePosition))
            return true;

        var currentCombatant = _combatManager.InCombat ? _combatManager.CurrentCombatant : _playerCreature;
        if (currentCombatant == null || !currentCombatant.IsPlayer)
            return false;

        if (GetCombatMoveButtonRect(viewport).Contains(mousePosition)
            || GetCombatAttackButtonRect(viewport).Contains(mousePosition)
            || GetCombatCastSpellButtonRect(viewport).Contains(mousePosition)
            || GetCombatBonusActionButtonRect(viewport).Contains(mousePosition)
            || GetCombatEndTurnButtonRect(viewport).Contains(mousePosition))
            return true;

        if (_selectedAction == CombatAction.Move && currentCombatant.HasAction)
        {
            if (GetCombatDashButtonRect(viewport).Contains(mousePosition)
                || GetCombatDisengageButtonRect(viewport).Contains(mousePosition)
                || GetCombatDodgeButtonRect(viewport).Contains(mousePosition)
                || GetCombatHideButtonRect(viewport).Contains(mousePosition)
                || GetCombatHelpActionButtonRect(viewport).Contains(mousePosition))
                return true;
        }

        if (_showBonusActionMenu && GetCombatRageButtonRect(viewport).Contains(mousePosition))
            return true;

        if (_showBonusActionMenu && GetCombatBonusHideButtonRect(viewport).Contains(mousePosition))
            return true;

        if (_selectedAction == CombatAction.Attack && currentCombatant.HasAction)
        {
            if (GetCombatGrappleButtonRect(viewport).Contains(mousePosition))
                return true;
            if (GetCombatThrowAcidButtonRect(viewport).Contains(mousePosition))
                return true;
        }

        return false;
    }
    

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        UpdateCameraMatrices();

        int? hoveredX = null;
        int? hoveredY = null;
        int? hoveredZ = null;

        if (_state == AppState.Playing && !_showCharacterSheet && !_showCampaignMap)
        {
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Draw3DGrid(_currentViewLevel);

            // Draw grid outlines with depth testing enabled so they are hidden by walls/objects
            Draw3DGridOutlines(_currentViewLevel);
            DrawPlayerMovementPerimeter();

            DrawCreatureTileOutlines();
            Draw3DCreatures();
            Draw3DGroundItems();
            DrawEnemySightLinesToPlayer();
            var hovered = GetHoveredTile();
            DrawHoveredMovementPath(hovered);
            if (hovered.HasValue)
            {
                hoveredX = hovered.Value.x;
                hoveredY = hovered.Value.y;
                hoveredZ = hovered.Value.z;
                Draw3DTileOutline(hoveredX.Value, hoveredY.Value, hoveredZ.Value, Color.Yellow);
            }
            Draw3DLine(Vector3.Zero, new Vector3(5, 0, 0), Color.Red);
            Draw3DLine(Vector3.Zero, new Vector3(0, 5, 0), Color.Lime);
            Draw3DLine(Vector3.Zero, new Vector3(0, 0, 5), Color.Blue);
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var vp = GraphicsDevice.Viewport;

        if (_state == AppState.Playing && !_showCharacterSheet && !_showCampaignMap)
        {
            if (_combatManager.InCombat) { foreach (var creature in _combatManager.Combatants) if (creature.IsAlive()) Draw3DCreatureUI(creature); }
            else if (_playerCreature != null) { Draw3DCreatureUI(_playerCreature); foreach (var creature in _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive())) Draw3DCreatureUI(creature); }

            DrawFloatingTooltips();

            DrawInventoryButton(vp);
            DrawMapButton(vp, false);
            DrawSpawnButton(vp);
            DrawRotationButtons(vp);
            DrawEnemyContextMenu(vp);
            DrawEnemyExaminePopup(vp);
            DrawDoorContextMenu(vp);

            if (_currentCampaign.IsTraveling)
            {
                DrawTravelProgress(_spriteBatch, vp, _currentCampaign);
            }
            if (!string.IsNullOrEmpty(_currentCampaign.LastTravelMessage))
            {
                DrawTravelMessage(_spriteBatch, vp, _currentCampaign);
            }

            // Vision/status debug markers intentionally hidden to keep the tactical UI clean.
        }

        // CHARACTER SHEET
        if (_showCharacterSheet && _state == AppState.Playing && _currentCharacter != null)
        {
            _characterSheet.Draw(_spriteBatch, GraphicsDevice, _currentCharacter, _currentCampaign, _playerCreature);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        if (_showJournal && _state == AppState.Playing && _currentCampaign != null)
        {
            _journalUI.Draw(_spriteBatch, GraphicsDevice, _currentCampaign);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // CAMPAIGN MAP
        if (_showCampaignMap && _state == AppState.Playing && _currentCampaign != null)
        {
            _campaignMapViewer.Draw(_spriteBatch, GraphicsDevice, _currentCampaign);
            DrawMapButton(GraphicsDevice.Viewport, true);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // 3D GAMEPLAY RENDERING (HANDLED ABOVE SPRITEBATCH)
        if (_state == AppState.Playing && !_showCharacterSheet && !_showCampaignMap)
        {
             // Already drawn in 3D pass
        }
        if (_state == AppState.MainMenu)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            void DrawMainMenu(string[] items, int selectedIndex, MenuView view, bool dim = false)
            {
                int menuWidth = 480;
                int itemHeight = 48;
                int padding = 12;
                int titleHeight = 120;
                int menuHeight = titleHeight + items.Length * (itemHeight + padding) + padding;
                int x = (vp.Width - menuWidth) / 2;
                if (view == MenuView.Language) x += 260;
                var menuRect = new Rectangle(x, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

                Color baseColor = dim ? Color.DarkSlateGray * 0.5f : Color.DarkSlateGray * 0.95f;
                _spriteBatch.Draw(_pixel, menuRect, baseColor);

                if (_font != null)
                {
                    string title = view == MenuView.Main ? "4DND" : (view == MenuView.Options ? Loc.Tr("Options") : Loc.Tr("Language"));
                    var titleSize = _font.MeasureString(title);
                    float titleScale = view == MenuView.Main ? 2f : 1.2f;
                    var titlePos = new Vector2(menuRect.X + (menuWidth - titleSize.X * titleScale) / 2, menuRect.Y + 12);
                    _spriteBatch.DrawString(_font, title, titlePos, Color.White * (dim ? 0.5f : 1.0f), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
                }

                for (int i = 0; i < items.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                    var col = (i == selectedIndex && !dim) ? Color.LightGray : Color.Gray * (dim ? 0.5f : 1.0f);
                    _spriteBatch.Draw(_pixel, itemRect, col);

                    if (_font != null)
                    {
                        var text = items[i];
                        var size = _font.MeasureString(text);
                        var pos = new Vector2(itemRect.X + (itemRect.Width - size.X) / 2, itemRect.Y + (itemRect.Height - size.Y) / 2);
                        var textCol = (i == selectedIndex && !dim) ? Color.Black : Color.White * (dim ? 0.5f : 1.0f);
                        _spriteBatch.DrawString(_font, text, pos, textCol);
                    }
                }
            }

            if (_currentMenuView == MenuView.Main)
            {
                DrawMainMenu(_cachedMainMenuItems, _mainMenuIndex, MenuView.Main);
            }
            else if (_currentMenuView == MenuView.Options)
            {
                DrawMainMenu(_cachedOptionsMenuItems, _mainMenuIndex, MenuView.Options);
            }
            else if (_currentMenuView == MenuView.Language)
            {
                DrawMainMenu(_cachedOptionsMenuItems, 0, MenuView.Options, true);
                DrawMainMenu(_cachedLanguageMenuItems, _mainMenuIndex, MenuView.Language);
            }

            DrawDeleteConfirmationDialog();

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // CHARACTER SELECT
        if (_state == AppState.CharacterSelect)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            int menuWidth = 980;
            int listWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int headerHeight = 110;
            int footerHeight = 32;
            int menuHeightFromList = headerHeight + GetCharacterMenuItemCount() * (itemHeight + padding) + padding + footerHeight;
            int previewMinimumHeight = 430;
            int menuHeight = Math.Max(menuHeightFromList, previewMinimumHeight);
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            _spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

            if (_font != null)
            {
                var title = _isMultiplayerMode ? Loc.Tr("Choose a Character (Multiplayer)") : Loc.Tr("Choose a Character (Single Player)");
                var size = _font.MeasureString(title);
                var pos = new Vector2(menuRect.X + (menuWidth - size.X) / 2, menuRect.Y + 12);
                _spriteBatch.DrawString(_font, title, pos, Color.White);

                // Back button
                var backRect = new Rectangle(menuRect.X + padding, menuRect.Y + 48, 110, 30);
                var mouse = Mouse.GetState();
                var backColor = backRect.Contains(mouse.Position) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, backRect, backColor);
                var backText = "< " + Loc.Tr("Back");
                var backTextSize = _font.MeasureString(backText);
                _spriteBatch.DrawString(_font, backText, new Vector2(backRect.X + (backRect.Width - backTextSize.X) / 2, backRect.Y + (backRect.Height - backTextSize.Y) / 2), Color.White);

                // Hint at bottom
                var hint = Loc.Tr("Click Delete button to remove character | Esc to go back");
                var hintSize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(menuRect.X + (listWidth - hintSize.X) / 2, menuRect.Y + menuHeight - 28), Color.White * 0.7f);
            }

            for (int i = 0; i < GetCharacterMenuItemCount(); i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + headerHeight + padding + i * (itemHeight + padding), listWidth - padding * 2, itemHeight);
                var col = (i == _characterIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    string label = IsExistingCharacterIndex(i) ? _characters[i].Name : Loc.Tr("Create New Character");
                    var m = _font.MeasureString(label);
                    var p = new Vector2(itemRect.X + 12, itemRect.Y + (itemRect.Height - m.Y) / 2);
                    var textCol = (i == _characterIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, label, p, textCol);

                    // Delete button for existing characters
                    if (IsExistingCharacterIndex(i))
                    {
                        var mouse = Mouse.GetState();
                        var deleteRect = new Rectangle(itemRect.X + itemRect.Width - 70, itemRect.Y + (itemRect.Height - 30) / 2, 60, 30);
                        var deleteColor = deleteRect.Contains(mouse.Position) ? Color.DarkRed : Color.Red * 0.7f;
                        _spriteBatch.Draw(_pixel, deleteRect, deleteColor);
                        
                        var deleteText = Loc.Tr("Delete");
                        var deleteSize = _font.MeasureString(deleteText);
                        _spriteBatch.DrawString(_font, deleteText, new Vector2(deleteRect.X + (deleteRect.Width - deleteSize.X) / 2, deleteRect.Y + (deleteRect.Height - deleteSize.Y) / 2), Color.White);
                    }
                }
            }

            var previewRect = new Rectangle(menuRect.X + listWidth + padding, menuRect.Y + 48, menuWidth - listWidth - padding * 2, menuHeight - 60);
            _spriteBatch.Draw(_pixel, previewRect, Color.Black * 0.2f);

            if (IsExistingCharacterIndex(_characterIndex) && _font != null)
            {
                DrawCharacterSelectionPreview(_spriteBatch, previewRect, _characters[_characterIndex]);
            }
            else if (_font != null)
            {
                var previewTitle = Loc.Tr("Preview");
                _spriteBatch.DrawString(_font, previewTitle, new Vector2(previewRect.X + 14, previewRect.Y + 12), Color.Gold, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                var message = Loc.Tr("Select an existing character to see their quick profile.");
                DrawWrappedText(message, previewRect.X + 14, previewRect.Y + 48, previewRect.Width - 28, Color.White * 0.9f, 0.65f);
            }

            DrawDeleteConfirmationDialog();

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // CHARACTER CREATION
        if (_state == AppState.CharacterCreate)
        {
            _characterCreation.Draw(gameTime, _spriteBatch, GraphicsDevice);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }
        
        // CAMPAIGN SELECT
        if (_state == AppState.CampaignSelect)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int headerHeight = 110;
            int footerHeight = 40;
            int menuHeight = headerHeight + Math.Max(1, _campaigns.Count + 1) * (itemHeight + padding) + padding + footerHeight;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            _spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

            if (_font != null)
            {
                var title = Loc.Tr("Select a Campaign");
                var size = _font.MeasureString(title);
                var pos = new Vector2(menuRect.X + (menuWidth - size.X) / 2, menuRect.Y + 12);
                _spriteBatch.DrawString(_font, title, pos, Color.White);
                
                // Back button
                var backRect = new Rectangle(menuRect.X + padding, menuRect.Y + 48, 110, 30);
                var mouse = Mouse.GetState();
                var backColor = backRect.Contains(mouse.Position) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, backRect, backColor);
                var backText = "< " + Loc.Tr("Back");
                var backTextSize = _font.MeasureString(backText);
                _spriteBatch.DrawString(_font, backText, new Vector2(backRect.X + (backRect.Width - backTextSize.X) / 2, backRect.Y + (backRect.Height - backTextSize.Y) / 2), Color.White);

                // Hint at bottom
                var hint = Loc.Tr("Click Delete button to remove campaign | Esc to go back");
                var hintSize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(menuRect.X + (menuWidth - hintSize.X) / 2, menuRect.Y + menuHeight - 28), Color.White * 0.7f);
            }

            for (int i = 0; i < _campaigns.Count; i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + headerHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (i == _campaignIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    var campaign = _campaigns[i];
                    var label = $"{campaign.Name} ({campaign.PartyMembers.Count} members)";
                    var m = _font.MeasureString(label);
                    var p = new Vector2(itemRect.X + 12, itemRect.Y + (itemRect.Height - m.Y) / 2);
                    var textCol = (i == _campaignIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, label, p, textCol);

                    // Delete button
                    var deleteRect = new Rectangle(itemRect.X + itemRect.Width - 70, itemRect.Y + (itemRect.Height - 30) / 2, 60, 30);
                    var deleteColor = deleteRect.Contains(Mouse.GetState().Position) ? Color.DarkRed : Color.Red * 0.7f;
                    _spriteBatch.Draw(_pixel, deleteRect, deleteColor);
                    
                    var deleteText = Loc.Tr("Delete");
                    var deleteSize = _font.MeasureString(deleteText);
                    _spriteBatch.DrawString(_font, deleteText, new Vector2(deleteRect.X + (deleteRect.Width - deleteSize.X) / 2, deleteRect.Y + (deleteRect.Height - deleteSize.Y) / 2), Color.White);
                }
            }

            // "Create New" option
            {
                int newIndex = _campaigns.Count;
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + headerHeight + padding + newIndex * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (newIndex == _campaignIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    var label = Loc.Tr("Create New Campaign");
                    var m = _font.MeasureString(label);
                    var p = new Vector2(itemRect.X + 12, itemRect.Y + (itemRect.Height - m.Y) / 2);
                    var textCol = (newIndex == _campaignIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, label, p, textCol);
                }
            }

            // Draw campaign summary for selected campaign
            if (_campaignIndex >= 0 && _campaignIndex < _campaigns.Count)
            {
                var selCampaign = _campaigns[_campaignIndex];
                int summaryWidth = 400;
                int summaryHeight = menuHeight;
                var summaryRect = new Rectangle(menuRect.Right + 20, menuRect.Y, summaryWidth, summaryHeight);

                _spriteBatch.Draw(_pixel, summaryRect, Color.DarkSlateGray * 0.9f);
                DrawBorder(_spriteBatch, _pixel, summaryRect, Color.Yellow * 0.5f, 2);

                int sy = summaryRect.Y + 20;
                _spriteBatch.DrawString(_font, Loc.Tr("Adventure Summary"), new Vector2(summaryRect.X + 20, sy), Color.Yellow);
                sy += 40;

                DrawSummarySection("HOOK", selCampaign.AdventureHook, summaryRect.X + 20, ref sy, summaryWidth - 40);
                sy += 20;
                DrawSummarySection("MIDDLE", selCampaign.AdventureMiddle, summaryRect.X + 20, ref sy, summaryWidth - 40);
                sy += 20;
                DrawSummarySection("ENDING", selCampaign.AdventureEnding, summaryRect.X + 20, ref sy, summaryWidth - 40);
            }

            DrawDeleteConfirmationDialog();

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }
        
        // CAMPAIGN CREATION
        if (_state == AppState.CampaignCreate)
        {
            _campaignCreation.Draw(gameTime, _spriteBatch, GraphicsDevice);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }


        
        // Combat UI
        if (_showCombatUI)
        {
            // Combat panel at top
            int panelHeight = _combatTopPanelHeight;
            var combatPanel = new Rectangle(0, 0, vp.Width, panelHeight);
            _spriteBatch.Draw(_pixel, combatPanel, Color.Black * 0.8f);
            
            if (_font != null)
            {
                int y = 10;
                
                // Round counter
                var roundText = _combatManager.InCombat ? Loc.Tr("=== ROUND {0} ===", _combatManager.CurrentRound) : Loc.Tr("=== EXPLORATION ===");
                var roundSize = _font.MeasureString(roundText);
                _spriteBatch.DrawString(_font, roundText, new Vector2((vp.Width - roundSize.X) / 2, y), Color.Gold);
                y += 30;
                
                // Current turn
                var currentCombatant = _combatManager.InCombat ? _combatManager.CurrentCombatant : _playerCreature;
                if (currentCombatant != null)
                {
                    var turnLabel = Loc.Tr(_combatManager.InCombat ? "Turn:" : "Active:");
                    var turnText = $"{turnLabel} {SafeString(currentCombatant.Name)} ({Loc.Tr("HP: {0}/{1}", currentCombatant.CurrentHP, currentCombatant.MaxHP)})";
                    _spriteBatch.DrawString(_font, turnText, new Vector2(10, y), Color.Yellow);
                    y += 25;
                    
                    // Action economy display
                    bool showActionReady = currentCombatant.HasAction || !_combatManager.InCombat;
                    bool showBonusReady = currentCombatant.HasBonusAction || !_combatManager.InCombat;
                    bool showReactionReady = currentCombatant.HasReaction || !_combatManager.InCombat;
                    bool showMovementReady = currentCombatant.MovementRemaining > 0 || !_combatManager.InCombat;

                    var actionStatus = Loc.Tr(showActionReady ? "Ready" : "Used");
                    var bonusStatus = Loc.Tr(showBonusReady ? "Ready" : "Used");
                    var reactionStatus = Loc.Tr(showReactionReady ? "Ready" : "Used");
                    var movementText = _combatManager.InCombat
                        ? $"{currentCombatant.MovementRemaining}/{currentCombatant.Speed}ft"
                        : $"{currentCombatant.Speed}/{currentCombatant.Speed}ft";
                    
                    var actionColor = showActionReady ? Color.Green : Color.DarkGray;
                    var bonusColor = showBonusReady ? Color.Green : Color.DarkGray;
                    var reactionColor = showReactionReady ? Color.Green : Color.DarkGray;
                    var movementColor = showMovementReady ? Color.LimeGreen : Color.DarkGray;
                    
                    _spriteBatch.DrawString(_font, Loc.Tr("Action:"), new Vector2(10, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, SafeString(actionStatus), new Vector2(80, y), actionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, Loc.Tr("Bonus:"), new Vector2(130, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, SafeString(bonusStatus), new Vector2(200, y), bonusColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, Loc.Tr("Reaction:"), new Vector2(250, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, SafeString(reactionStatus), new Vector2(340, y), reactionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, Loc.Tr("Move:"), new Vector2(390, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, movementText, new Vector2(450, y), movementColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    if (currentCombatant.IsHidden)
                        _spriteBatch.DrawString(_font, Loc.Tr("[HIDDEN]"), new Vector2(540, y), Color.LimeGreen, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    y += 25;
                    
                    // Initiative order
                    var initText = Loc.Tr("Initiative: ");
                    if (_combatManager.InCombat)
                    {
                        for (int i = 0; i < _combatManager.Combatants.Count && i < 5; i++)
                        {
                            var c = _combatManager.Combatants[i];
                            initText += $"{SafeString(c.Name)}({c.Initiative}) ";
                        }
                    }
                    else
                    {
                        initText = "Exploration Mode - No combat active";
                    }
                    _spriteBatch.DrawString(_font, initText, new Vector2(10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    y += 25;
                    
                    // Player actions
                    if (currentCombatant.IsPlayer)
                    {
                        var moveLabel = "Move";
                        var moveColor = new Color(45, 95, 145);
                        bool isDashingMove = _combatManager.InCombat && currentCombatant.MovementRemaining == 0 && currentCombatant.HasAction;
                        if (isDashingMove)
                        {
                            moveLabel = "Dash?";
                            moveColor = new Color(160, 100, 40);
                        }

                        DrawCombatActionButton(
                            GetCombatMoveButtonRect(vp),
                            moveLabel,
                            moveColor,
                            _selectedAction == CombatAction.Move);

                        if (_selectedAction == CombatAction.Move && currentCombatant.HasAction)
                        {
                            DrawCombatActionButton(
                                GetCombatDashButtonRect(vp),
                                "Dash (Action)",
                                new Color(160, 100, 40),
                                false);
                            DrawCombatActionButton(
                                GetCombatDisengageButtonRect(vp),
                                currentCombatant.IsDisengaged ? "Disengaged" : "Disengage",
                                currentCombatant.IsDisengaged ? Color.Gray : new Color(100, 130, 60),
                                currentCombatant.IsDisengaged);
                            DrawCombatActionButton(
                                GetCombatDodgeButtonRect(vp),
                                currentCombatant.IsDodging ? "Dodging" : "Dodge",
                                currentCombatant.IsDodging ? Color.Gray : new Color(70, 115, 145),
                                currentCombatant.IsDodging);
                            DrawCombatActionButton(
                                GetCombatHideButtonRect(vp),
                                currentCombatant.IsHidden ? "Hidden!" : "Hide",
                                currentCombatant.IsHidden ? Color.Gray : new Color(60, 90, 60),
                                currentCombatant.IsHidden);
                            DrawCombatActionButton(
                                GetCombatHelpActionButtonRect(vp),
                                "Help",
                                new Color(70, 100, 130),
                                _selectedAction == CombatAction.Help);
                        }

                        DrawCombatActionButton(
                            GetCombatAttackButtonRect(vp),
                            "Attack",
                            new Color(130, 70, 50),
                            _selectedAction == CombatAction.Attack);

                        if (_selectedAction == CombatAction.Attack && currentCombatant.HasAction)
                        {
                            DrawCombatActionButton(
                                GetCombatGrappleButtonRect(vp),
                                "Grapple",
                                new Color(100, 60, 130),
                                _selectedAction == CombatAction.Grapple);

                            bool hasAcid = _currentCharacter?.InventoryData.HasItem("Acid (vial)") == true;
                            DrawCombatActionButton(
                                GetCombatThrowAcidButtonRect(vp),
                                "Throw Acid",
                                hasAcid ? new Color(60, 120, 80) : Color.DarkGray,
                                _selectedAction == CombatAction.ThrowAcid);
                        }

                        bool canCastSpell = IsSpellcasterClass(_currentCharacter!.Class);
                        DrawCombatActionButton(
                            GetCombatCastSpellButtonRect(vp),
                            "Cast Spell",
                            canCastSpell ? new Color(75, 50, 130) : Color.DarkGray,
                            _selectedAction == CombatAction.CastSpell);

                        DrawCombatActionButton(
                            GetCombatBonusActionButtonRect(vp),
                            "Bonus Action",
                            new Color(45, 145, 95),
                            _showBonusActionMenu);
                        DrawCombatActionButton(
                            GetCombatEndTurnButtonRect(vp),
                            "End Turn",
                            new Color(90, 70, 115),
                            false);

                        if (_showBonusActionMenu)
                        {
                            bool isBarbarian = _currentCharacter?.Class == "Barbarian";
                            if (isBarbarian)
                            {
                                bool canRage = currentCombatant.HasBonusAction && currentCombatant.RagesRemaining > 0 && !currentCombatant.IsRaging;
                                DrawCombatActionButton(
                                    GetCombatRageButtonRect(vp),
                                    $"Rage ({currentCombatant.RagesRemaining})",
                                    canRage ? Color.DarkRed : Color.Gray,
                                    false);
                            }

                            if (currentCombatant.HasNimbleEscape)
                            {
                                DrawCombatActionButton(
                                    GetCombatBonusHideButtonRect(vp),
                                    currentCombatant.IsHidden ? "Hidden!" : "Hide (BA)",
                                    currentCombatant.IsHidden ? Color.Gray : new Color(60, 90, 60),
                                    currentCombatant.IsHidden);
                            }
                        }

                        var moveRect = GetCombatMoveButtonRect(vp);

                        if (_selectedAction != CombatAction.None && _selectedAction != CombatAction.BonusAction)
                        {
                            var actionText = _selectedAction switch
                            {
                                CombatAction.Move => "Click on an empty tile to move",
                                CombatAction.Attack => "Click on an enemy to attack",
                                CombatAction.Grapple => "Click on an adjacent enemy to grapple",
                                CombatAction.CastSpell => "Click on an enemy to cast a spell",
                                CombatAction.Help => "Click on an adjacent enemy to distract (Help action)",
                                CombatAction.ThrowAcid => "Click on an enemy to throw acid (20 ft, 2d6 acid)",
                                _ => ""
                            };
                            var actionTextSize = _font.MeasureString(actionText) * 0.8f;
                            _spriteBatch.DrawString(_font, actionText, new Vector2((vp.Width - actionTextSize.X) / 2, moveRect.Y - 55), Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        _spriteBatch.DrawString(_font, "Enemy turn...", new Vector2(10, y), Color.Red);
                        y += 25;
                    }
                }
                
                // Draggable Combat Log Window
                _spriteBatch.Draw(_pixel, _combatLogWindowRect, Color.Black * 0.7f);
                DrawBorder(_spriteBatch, _pixel, _combatLogWindowRect, Color.Gray * 0.5f, 1);

                int logX = _combatLogWindowRect.X + 8;
                int logY = _combatLogWindowRect.Y + 6;
                _spriteBatch.DrawString(_font, Loc.Tr("Combat Log"), new Vector2(logX, logY), Color.White * 0.8f, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
                logY += 20;
                
                for (int i = Math.Max(0, _combatLog.Count - 5); i < _combatLog.Count; i++)
                {
                    _spriteBatch.DrawString(_font, _combatLog[i], new Vector2(logX, logY), Color.LightGray, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
                    logY += 16;
                }

                _diceRollAnimation.Draw(_spriteBatch, _pixel, _font, new Rectangle(0, 0, vp.Width, vp.Height));
                
                
                // Bottom screen hints (moved from top panel)
                var hint = Loc.Tr("Gameplay Hints");
                var hintSize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(vp.Width - hintSize.X - 10, vp.Height - 25), Color.White * 0.7f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                
                
                // Display current view level
                string levelHint;
                if (_playerCreature != null && _playerCreature.CanFly)
                {
                    levelHint = Loc.Tr("View Level Detail", _currentViewLevel, _playerCreature.Z, Loc.Tr(_playerCreature.IsFlying ? "[FLYING]" : "[GROUND]"));
                }
                else
                {
                    levelHint = Loc.Tr("View Level: Z{0}", _currentViewLevel);
                }
                var levelHintSize = _font.MeasureString(levelHint);
                _spriteBatch.DrawString(_font, levelHint, new Vector2(10, vp.Height - 25), Color.Cyan * 0.8f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
        
        // Tile tooltip (outside combat panel)
        var tooltipMouse = Mouse.GetState();
        if (_font != null
            && hoveredX.HasValue
            && hoveredY.HasValue
            && hoveredZ.HasValue
            && _combatManager.InCombat
            && _showVisionOverlay
            && !IsMouseOverCombatUi(tooltipMouse.Position, vp))
        {
            int tx = hoveredX.Value;
            int ty = hoveredY.Value;
            int tz = hoveredZ.Value;
            
            var tileType = _tacticalMap.Get(tx, ty, tz);
            var lightLevel = _visionSystem.GetLightLevel(tx, ty, tz);
            var isVisible = _visionSystem.IsVisible(tx, ty, tz);
            
            var creature = _combatManager.GetCreatureAt(tx, ty, tz);
            
            var tooltip = Loc.Tr("Tile Tooltip", tx, ty, tz, tileType, lightLevel, isVisible);
            if (creature != null && isVisible)
            {
                var sizeDesc = SizeHelper.GetSpaceDescription(creature.Size);
                var flyingStatus = creature.IsFlying ? Loc.Tr("Flying Status") : "";
                tooltip += Loc.Tr("Creature Tooltip", creature.Name, creature.Size, sizeDesc, creature.CurrentHP, creature.MaxHP, flyingStatus);
            }
            
            var tooltipSize = _font.MeasureString(tooltip);
            var tooltipPos = new Vector2(tooltipMouse.X + 15, tooltipMouse.Y + 15);
            
            // Make sure tooltip stays on screen
            if (tooltipPos.X + tooltipSize.X > vp.Width)
                tooltipPos.X = tooltipMouse.X - tooltipSize.X - 15;
            if (tooltipPos.Y + tooltipSize.Y > vp.Height)
                tooltipPos.Y = tooltipMouse.Y - tooltipSize.Y - 15;

            if (_showCombatUI)
                tooltipPos.Y = MathF.Max(_combatTopPanelHeight + 8, tooltipPos.Y);
            
            // Draw background
            _spriteBatch.Draw(_pixel, new Rectangle((int)tooltipPos.X - 5, (int)tooltipPos.Y - 3, (int)tooltipSize.X + 10, (int)tooltipSize.Y + 6), Color.Black * 0.9f);
            _spriteBatch.DrawString(_font, tooltip, tooltipPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        // PAUSE MENU
        if (_isMenuOpen)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.6f);

            void DrawPauseMenu(string[] items, int selectedIndex, MenuView view, bool dim = false)
            {
                int menuWidth2 = 360;
                int itemHeight2 = 48;
                int padding2 = 12;
                int menuHeight2 = items.Length * (itemHeight2 + padding2) + padding2;
                int x = (vp.Width - menuWidth2) / 2;
                if (view == MenuView.Language) x += 200;
                var menuRect2 = new Rectangle(x, (vp.Height - menuHeight2) / 2, menuWidth2, menuHeight2);

                _spriteBatch.Draw(_pixel, menuRect2, Color.DarkSlateGray * (dim ? 0.5f : 0.95f));

                for (int i = 0; i < items.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect2.X + padding2, menuRect2.Y + padding2 + i * (itemHeight2 + padding2), menuWidth2 - padding2 * 2, itemHeight2);
                    var col = (i == selectedIndex && !dim) ? Color.LightGray : Color.Gray * (dim ? 0.5f : 1.0f);
                    _spriteBatch.Draw(_pixel, itemRect, col);
                    var barRect = new Rectangle(itemRect.X + 6, itemRect.Y + 6, 8, itemRect.Height - 12);
                    _spriteBatch.Draw(_pixel, barRect, (i == selectedIndex && !dim) ? Color.Orange : Color.DarkOrange * (dim ? 0.5f : 1.0f));
                    if (_font != null)
                    {
                        var textPos = new Vector2(itemRect.X + 24 + 8, itemRect.Y + (itemRect.Height - _font.LineSpacing) / 2);
                        var textCol = (i == selectedIndex && !dim) ? Color.Black : Color.White * (dim ? 0.5f : 1.0f);
                        _spriteBatch.DrawString(_font, items[i], textPos, textCol);
                    }
                }
            }

            if (_currentMenuView == MenuView.Main)
            {
                DrawPauseMenu(_cachedPauseMenuItems, _menuIndex, MenuView.Main);
            }
            else if (_currentMenuView == MenuView.Options)
            {
                DrawPauseMenu(_cachedOptionsMenuItems, _menuIndex, MenuView.Options);
            }
            else if (_currentMenuView == MenuView.Language)
            {
                DrawPauseMenu(_cachedOptionsMenuItems, 0, MenuView.Options, true);
                DrawPauseMenu(_cachedLanguageMenuItems, _menuIndex, MenuView.Language);
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }


    private void DrawCharacterSelectionPreview(SpriteBatch sb, Rectangle previewRect, Character character)
    {
        DrawBorder(sb, _pixel, previewRect, Color.Gold * 0.35f, 2);

        int x = previewRect.X + 14;
        int y = previewRect.Y + 12;

        sb.DrawString(_font, Loc.Tr("Preview"), new Vector2(x, y), Color.Gold, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        y += 34;

        sb.DrawString(_font, character.Name, new Vector2(x, y), Color.White, 0f, Vector2.Zero, 0.95f, SpriteEffects.None, 0f);
        y += 32;

        string identity = $"Lvl {character.Level} {character.Race} {character.Class}";
        sb.DrawString(_font, identity, new Vector2(x, y), Color.LightBlue, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);
        y += 32;

        string combatLine = $"HP {character.CurrentHP}/{character.MaxHP} | AC {character.ArmorClass} | Speed {character.Speed}";
        sb.DrawString(_font, combatLine, new Vector2(x, y), Color.LightGreen, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
        y += 28;

        sb.DrawString(_font, $"XP: {character.XP}", new Vector2(x, y), Color.LightGray, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        y += 28;

        sb.DrawString(_font, Loc.Tr("Abilities:"), new Vector2(x, y), Color.Orange, 0f, Vector2.Zero, 0.68f, SpriteEffects.None, 0f);
        y += 26;

        string[] stats =
        {
            $"{Loc.Tr("STR")} {character.Strength} ({FormatModifier(character.GetAbilityModifier(character.Strength))})",
            $"{Loc.Tr("DEX")} {character.Dexterity} ({FormatModifier(character.GetAbilityModifier(character.Dexterity))})",
            $"{Loc.Tr("CON")} {character.Constitution} ({FormatModifier(character.GetAbilityModifier(character.Constitution))})",
            $"{Loc.Tr("INT")} {character.Intelligence} ({FormatModifier(character.GetAbilityModifier(character.Intelligence))})",
            $"{Loc.Tr("WIS")} {character.Wisdom} ({FormatModifier(character.GetAbilityModifier(character.Wisdom))})",
            $"{Loc.Tr("CHA")} {character.Charisma} ({FormatModifier(character.GetAbilityModifier(character.Charisma))})"
        };

        foreach (var stat in stats)
        {
            sb.DrawString(_font, stat, new Vector2(x + 8, y), Color.White, 0f, Vector2.Zero, 0.62f, SpriteEffects.None, 0f);
            y += 22;
        }

        y += 6;
        string survival = character.DarkvisionRange > 0
            ? Loc.Tr("Darkvision: {0} ft", character.DarkvisionRange)
             : Loc.Tr("Darkvision: none");
        sb.DrawString(_font, survival, new Vector2(x, y), Color.Yellow, 0f, Vector2.Zero, 0.62f, SpriteEffects.None, 0f);
        y += 22;

        if (character.HasSunlightSensitivity)
        {
            sb.DrawString(_font, Loc.Tr("Sunlight Sensitivity"), new Vector2(x, y), Color.OrangeRed, 0f, Vector2.Zero, 0.62f, SpriteEffects.None, 0f);
        }
    }

    private static string FormatModifier(int modifier) => modifier >= 0 ? $"+{modifier}" : modifier.ToString();

    private void DrawWrappedText(string text, int x, int y, int maxWidth, Color color, float scale)
    {
        string wrapped = WrapText(text, maxWidth);
        _spriteBatch.DrawString(_font, wrapped, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawDeleteConfirmationDialog()
    {
        if (!HasPendingDeleteConfirmation || _font == null)
            return;

        var vp = GraphicsDevice.Viewport;
        GetDeleteConfirmationRects(vp, out var dialogRect, out var confirmRect, out var cancelRect);

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.6f);
        _spriteBatch.Draw(_pixel, dialogRect, Color.DarkSlateGray);
        DrawBorder(_spriteBatch, _pixel, dialogRect, Color.White * 0.6f, 2);

        var entityType = Loc.Tr(_pendingDeleteType == PendingDeleteType.Character ? "character" : "campaign");
        var title = Loc.Tr("Confirm deletion");
        var message = Loc.Tr("Delete {0} '{1}'?", entityType, GetPendingDeleteEntityName());
        var warning = Loc.Tr("This action cannot be undone.");
        var controls = Loc.Tr("Click Delete to confirm, Esc = cancel");

        const int textPaddingX = 20;
        const int textTopPadding = 16;
        const int sectionSpacing = 8;
        float lineHeight = _font.LineSpacing;
        float titleY = dialogRect.Y + textTopPadding;
        float messageY = titleY + lineHeight + 12;
        float warningY = messageY + lineHeight + sectionSpacing;
        float controlsY = warningY + lineHeight + sectionSpacing;

        _spriteBatch.DrawString(_font, title, new Vector2(dialogRect.X + textPaddingX, titleY), Color.White);
        _spriteBatch.DrawString(_font, message, new Vector2(dialogRect.X + textPaddingX, messageY), Color.LightGray);
        _spriteBatch.DrawString(_font, warning, new Vector2(dialogRect.X + textPaddingX, warningY), Color.OrangeRed);
        _spriteBatch.DrawString(_font, controls, new Vector2(dialogRect.X + textPaddingX, controlsY), Color.White * 0.8f);

        var mousePos = Mouse.GetState().Position;
        var confirmColor = confirmRect.Contains(mousePos) ? Color.DarkRed : Color.Red * 0.8f;
        var cancelColor = cancelRect.Contains(mousePos) ? Color.DarkGray : Color.Gray * 0.9f;
        _spriteBatch.Draw(_pixel, confirmRect, confirmColor);
        _spriteBatch.Draw(_pixel, cancelRect, cancelColor);

        string confirmText = Loc.Tr("Delete");
        string cancelText = Loc.Tr("Cancel");
        var confirmSize = _font.MeasureString(confirmText);
        var cancelSize = _font.MeasureString(cancelText);

        _spriteBatch.DrawString(_font, confirmText, new Vector2(confirmRect.X + (confirmRect.Width - confirmSize.X) / 2, confirmRect.Y + (confirmRect.Height - confirmSize.Y) / 2), Color.White);
        _spriteBatch.DrawString(_font, cancelText, new Vector2(cancelRect.X + (cancelRect.Width - cancelSize.X) / 2, cancelRect.Y + (cancelRect.Height - cancelSize.Y) / 2), Color.White);
    }

    private void DrawBorder(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void DrawSummarySection(string title, string content, int x, ref int y, int width)
    {
        _spriteBatch.DrawString(_font, title + ":", new Vector2(x, y), Color.Orange, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        y += 20;

        string text = string.IsNullOrEmpty(content) ? "No data available." : content;
        string wrapped = WrapText(text, width);
        _spriteBatch.DrawString(_font, wrapped, new Vector2(x + 10, y), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

        var textSize = _font.MeasureString(wrapped) * 0.6f;
        y += (int)textSize.Y + 10;
    }

    private string WrapText(string text, float maxLineWidth)
    {
        if (_font == null || string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(' ');
        string result = "";
        string currentLine = "";

        foreach (string word in words)
        {
            if (_font.MeasureString(currentLine + word).X * 0.6f < maxLineWidth)
            {
                currentLine += word + " ";
            }
            else
            {
                result += currentLine + "\n";
                currentLine = word + " ";
            }
        }
        return result + currentLine;
    }

    private static bool IsSpellcasterClass(string className) =>
        className is "Bard" or "Cleric" or "Druid" or "Paladin" or "Ranger" or "Sorcerer" or "Warlock" or "Wizard";

    private static int GetSpellcastingAbilityModifier(Character character)
    {
        int score = character.Class switch
        {
            "Wizard" => character.Intelligence,
            "Cleric" or "Druid" or "Ranger" => character.Wisdom,
            _ => character.Charisma
        };
        return character.GetAbilityModifier(score);
    }

    private static string GetCantripDamageDice(int level) =>
        level >= 17 ? "4d10" : level >= 11 ? "3d10" : level >= 5 ? "2d10" : "1d10";

    private string BuildEnemyExamineText(Creature creature)
    {
        string senses = "Normal";
        var senseList = new List<string>();
        if (creature.DarkvisionRange > 0) senseList.Add($"Darkvision {creature.DarkvisionRange}ft");
        if (creature.HasBlindSight && creature.BlindSightRange > 0) senseList.Add($"Blindsight {creature.BlindSightRange}ft");
        if (creature.HasTremorsense && creature.TremorsenseRange > 0) senseList.Add($"Tremorsense {creature.TremorsenseRange}ft");
        if (creature.HasTrueSight && creature.TrueSightRange > 0) senseList.Add($"Truesight {creature.TrueSightRange}ft");
        if (senseList.Count > 0) senses = string.Join(", ", senseList);

        string activeConditions = creature.Conditions == Condition.None
            ? "None"
            : string.Join(", ", creature.Conditions.GetActiveConditionNames());

        return
            $"{creature.Name} ({creature.Type})\n" +
            Loc.Tr("Alignment: {0}\n", AlignmentHelper.GetDescription(creature.Alignment)) +
            Loc.Tr("Size: {0} ({1})\n", creature.Size, SizeHelper.GetSpaceDescription(creature.Size)) +
            Loc.Tr("HP: {0}/{1} | AC: {2} | Speed: {3}ft\n", creature.CurrentHP, creature.MaxHP, creature.ArmorClass, creature.Speed) +
            Loc.Tr("Attack: {0} +{1} ({2}+{3} {4})\n", creature.AttackName, creature.AttackBonus, creature.DamageDice, creature.DamageBonus, creature.CurrentDamageType) +
            Loc.Tr("Senses: {0} | Passive Perception: {1}\n", senses, creature.PassivePerception) +
            Loc.Tr("Conditions: {0}", activeConditions);
    }

    private void DrawEnemyContextMenu(Viewport viewport)
    {
        if (!_showEnemyContextMenu || _font == null)
            return;

        int x = Math.Clamp(_enemyContextMenuRect.X, 8, viewport.Width - _enemyContextMenuRect.Width - 8);
        int y = Math.Clamp(_enemyContextMenuRect.Y, 8, viewport.Height - _enemyContextMenuRect.Height - 8);
        _enemyContextMenuRect = new Rectangle(x, y, _enemyContextMenuRect.Width, _enemyContextMenuRect.Height);
        _enemyExamineOptionRect = new Rectangle(x + 6, y + 6, _enemyContextMenuRect.Width - 12, _enemyContextMenuRect.Height - 12);

        bool isHovered = _enemyExamineOptionRect.Contains(Mouse.GetState().Position);
        _spriteBatch.Draw(_pixel, _enemyContextMenuRect, new Color(20, 20, 20, 240));
        DrawBorder(_spriteBatch, _pixel, _enemyContextMenuRect, Color.White, 2);
        _spriteBatch.Draw(_pixel, _enemyExamineOptionRect, isHovered ? Color.DarkGoldenrod : Color.DarkSlateGray);
        _spriteBatch.DrawString(_font, Loc.Tr("Inspect"), new Vector2(_enemyExamineOptionRect.X + 10, _enemyExamineOptionRect.Y + 5), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private DungeonDoorState? GetDoorState(int x, int y, int z)
    {
        if (_currentCampaign == null) return null;
        var partyHex = Campaign.CartesianToAxial(_currentCampaign.PartyX, _currentCampaign.PartyY);
        foreach (var dungeon in _currentCampaign.Dungeons)
        {
            if (Campaign.GetHexDistance(partyHex.q, partyHex.r, dungeon.WorldX, dungeon.WorldY) <= 1)
            {
                var (worldX_miles, worldY_miles) = Campaign.AxialToMiles(dungeon.WorldX, dungeon.WorldY);
                int ox = (int)((worldX_miles - _currentCampaign.PartyX) * Campaign.TacticalUnitsPerMile);
                int oy = (int)((worldY_miles - _currentCampaign.PartyY) * Campaign.TacticalUnitsPerMile);
                int rx = x - ox, ry = y - oy;
                return dungeon.Doors.FirstOrDefault(d => d.X == rx && d.Y == ry && d.Z == z);
            }
        }

        // Check for urban doors
        float milesX = ((float)x / Campaign.TacticalUnitsPerMile) + _currentCampaign.PartyX;
        float milesY = ((float)y / Campaign.TacticalUnitsPerMile) + _currentCampaign.PartyY;
        if (WorldGenerator.GetUrbanLocation(milesX, milesY, _currentCampaign) != null)
        {
            if (_tacticalMap.Get(x, y, z) == TileType.DungeonDoorWooden)
            {
                if (!_urbanDoorStates.TryGetValue((x, y, z), out var door))
                {
                    door = new DungeonDoorState { X = x, Y = y, Z = z, Type = DungeonDoorType.Wooden, IsOpen = false, MaxHP = 20, CurrentHP = 20 };
                    _urbanDoorStates[(x, y, z)] = door;
                }
                return door;
            }
        }

        return null;
    }

    private void ToggleDoor(int x, int y, int z)
    {
        var door = GetDoorState(x, y, z);
        if (door == null) return;

        if (door.IsLocked || door.IsBarred)
        {
            AddToCombatLog(Loc.Tr("The door is locked or barred."));
            return;
        }

        door.IsOpen = !door.IsOpen;
        AddToCombatLog(Loc.Tr(door.IsOpen ? "You opened the door." : "You closed the door."));
        UpdateVision();
    }

    private void ShowDoorContextMenu(int x, int y, int z, Point screenPos)
    {
        _contextTargetDoor = (x, y, z);
        _showDoorContextMenu = true;
        _doorMenuOptionRects.Clear();

        string[] options = { "Open", "Lockpick", "Destroy", "Examine" };
        int menuWidth = 140;
        int itemHeight = 32;
        int menuHeight = options.Length * itemHeight + 12;

        int mx = Math.Clamp(screenPos.X, 8, GraphicsDevice.Viewport.Width - menuWidth - 8);
        int my = Math.Clamp(screenPos.Y, 8, GraphicsDevice.Viewport.Height - menuHeight - 8);
        _doorContextMenuRect = new Rectangle(mx, my, menuWidth, menuHeight);

        for (int i = 0; i < options.Length; i++)
        {
            _doorMenuOptionRects[options[i]] = new Rectangle(mx + 6, my + 6 + i * itemHeight, menuWidth - 12, itemHeight - 4);
        }
    }

    private bool HandleDoorContextMenu(Point mousePos)
    {
        foreach (var option in _doorMenuOptionRects)
        {
            if (option.Value.Contains(mousePos))
            {
                ExecuteDoorAction(option.Key);
                _showDoorContextMenu = false;
                return true;
            }
        }
        return false;
    }

    private void ExecuteDoorAction(string action)
    {
        var door = GetDoorState(_contextTargetDoor.x, _contextTargetDoor.y, _contextTargetDoor.z);
        if (door == null) return;

        switch (action)
        {
            case "Open":
                ToggleDoor(_contextTargetDoor.x, _contextTargetDoor.y, _contextTargetDoor.z);
                break;
            case "Lockpick":
                if (door.IsLocked)
                {
                    int roll = Dice.Roll(20);
                    int bonus = _currentCharacter?.GetSkillBonus("Sleight of Hand", out _) ?? 0;
                    if (roll + bonus >= door.LockDC)
                    {
                        door.IsLocked = false;
                        AddToCombatLog(Loc.Tr("You successfully picked the lock!"));
                    }
                    else AddToCombatLog(Loc.Tr("You failed to pick the lock."));
                }
                else AddToCombatLog(Loc.Tr("The door is not locked."));
                break;
            case "Destroy":
                AddToCombatLog(Loc.Tr("You attack the door!"));
                // Simple destruction for now
                door.CurrentHP -= 10;
                if (door.CurrentHP <= 0)
                {
                    _tacticalMap.Set(_contextTargetDoor.x, _contextTargetDoor.y, _contextTargetDoor.z, TileType.DungeonFloor);
                    AddToCombatLog(Loc.Tr("The door is destroyed!"));
                    UpdateVision();
                }
                break;
            case "Examine":
                string status = door.IsOpen ? Loc.Tr("Open") : Loc.Tr("Closed");
                if (door.IsLocked) status += ", " + Loc.Tr("Locked");
                if (door.IsBarred) status += ", " + Loc.Tr("Barred");
                AddToCombatLog(Loc.Tr("Door ({0}): {1}, HP {2}/{3}", door.Type, status, door.CurrentHP, door.MaxHP));
                break;
        }
    }

    private void DrawDoorContextMenu(Viewport viewport)
    {
        if (!_showDoorContextMenu || _font == null) return;
        _spriteBatch.Draw(_pixel, _doorContextMenuRect, new Color(20, 20, 20, 240));
        DrawBorder(_spriteBatch, _pixel, _doorContextMenuRect, Color.White, 2);

        foreach (var option in _doorMenuOptionRects)
        {
            bool isHovered = option.Value.Contains(Mouse.GetState().Position);
            _spriteBatch.Draw(_pixel, option.Value, isHovered ? Color.DarkGoldenrod : Color.DarkSlateGray);
            _spriteBatch.DrawString(_font, Loc.Tr(option.Key), new Vector2(option.Value.X + 10, option.Value.Y + 5), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
    }

    private DungeonStairs? GetStairsState(int x, int y, int z)
    {
        if (_currentCampaign == null) return null;
        var partyHex = Campaign.CartesianToAxial(_currentCampaign.PartyX, _currentCampaign.PartyY);
        foreach (var dungeon in _currentCampaign.Dungeons)
        {
            if (Campaign.GetHexDistance(partyHex.q, partyHex.r, dungeon.WorldX, dungeon.WorldY) <= 1)
            {
                var (worldX_miles, worldY_miles) = Campaign.AxialToMiles(dungeon.WorldX, dungeon.WorldY);
                int ox = (int)((worldX_miles - _currentCampaign.PartyX) * Campaign.TacticalUnitsPerMile);
                int oy = (int)((worldY_miles - _currentCampaign.PartyY) * Campaign.TacticalUnitsPerMile);
                int rx = x - ox, ry = y - oy;
                return dungeon.Stairs.FirstOrDefault(s => s.X == rx && s.Y == ry && s.Z == z);
            }
        }

        // Check for urban stairs
        float milesX = ((float)x / Campaign.TacticalUnitsPerMile) + _currentCampaign.PartyX;
        float milesY = ((float)y / Campaign.TacticalUnitsPerMile) + _currentCampaign.PartyY;
        if (WorldGenerator.GetUrbanLocation(milesX, milesY, _currentCampaign) != null)
        {
            var type = _tacticalMap.Get(x, y, z);
            if (type == TileType.DungeonStairsUp)
                return new DungeonStairs { X = x, Y = y, Z = z, TargetZ = z + 1, IsUp = true };
            if (type == TileType.DungeonStairsDown)
                return new DungeonStairs { X = x, Y = y, Z = z, TargetZ = z - 1, IsUp = false };
        }

        return null;
    }

    private void DrawEnemyExaminePopup(Viewport viewport)
    {
        if (string.IsNullOrWhiteSpace(_enemyExamineText) || _font == null)
            return;

        string wrapped = WrapText(SafeString(_enemyExamineText), 360);
        var textSize = _font.MeasureString(wrapped) * 0.65f;
        int width = Math.Min(420, (int)textSize.X + 24);
        int height = (int)textSize.Y + 24;
        int x = Math.Clamp(viewport.Width - width - 20, 8, viewport.Width - width - 8);
        int minY = _showCombatUI ? _combatTopPanelHeight + 8 : 8;
        int y = Math.Clamp(viewport.Height - height - 20, minY, viewport.Height - height - 8);

        _enemyExaminePopupRect = new Rectangle(x, y, width, height);
        _spriteBatch.Draw(_pixel, _enemyExaminePopupRect, new Color(18, 18, 18, 245));
        DrawBorder(_spriteBatch, _pixel, _enemyExaminePopupRect, Color.White, 2);
        _spriteBatch.DrawString(_font, wrapped, new Vector2(x + 10, y + 10), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
    }
}
