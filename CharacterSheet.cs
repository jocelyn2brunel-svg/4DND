#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _4DND;

public class CharacterSheet
{
    private SpriteFont _font;
    private Texture2D _pixel;
    private float _scrollOffset = 0f;
    private int _prevScrollValue = 0;
    private const int Margin = 20;
    private const int ScrollbarWidth = 20;
    private const int CloseButtonWidth = 120;
    private const int CloseButtonHeight = 36;
    private Point _mousePosition;
    private string? _hoverTooltip;
    private HashSet<char> _supportedChars;
    private MouseState _prevMouseState;
    private readonly List<(Rectangle Rect, string WeaponName, bool IsEquipped)> _weaponItemRects = new();
    private bool _showWeaponContextMenu;
    private Rectangle _weaponContextMenuRect;
    private string? _contextWeaponName;
    private bool _contextWeaponIsEquipped;
    private string? _inspectWeaponText;
    private Rectangle _inspectPopupRect;

    public CharacterSheet(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
        _supportedChars = font != null ? new HashSet<char>(font.Characters) : new HashSet<char>();
    }

    public void Update(MouseState mouse, Character? character = null)
    {
        _mousePosition = mouse.Position;

        if (_prevScrollValue == 0)
        {
            _prevScrollValue = mouse.ScrollWheelValue;
            return;
        }
        
        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta * 0.5f;
            _prevScrollValue = mouse.ScrollWheelValue;
        }

        if (character == null)
        {
            _prevMouseState = mouse;
            return;
        }

        bool rightClick = mouse.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released;
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released;

        if (rightClick)
        {
            var clickedWeapon = _weaponItemRects.FirstOrDefault(w => w.Rect.Contains(_mousePosition));
            if (!string.IsNullOrEmpty(clickedWeapon.WeaponName))
            {
                _contextWeaponName = clickedWeapon.WeaponName;
                _contextWeaponIsEquipped = clickedWeapon.IsEquipped;
                _showWeaponContextMenu = true;
                _inspectWeaponText = null;
                _weaponContextMenuRect = BuildContextMenuRect(_mousePosition);
            }
            else
            {
                _showWeaponContextMenu = false;
            }
        }

        if (leftClick)
        {
            if (!string.IsNullOrEmpty(_inspectWeaponText) && !_inspectPopupRect.Contains(_mousePosition))
            {
                _inspectWeaponText = null;
            }

            if (_showWeaponContextMenu)
            {
                var option = GetContextMenuOptionAt(_mousePosition);
                switch (option)
                {
                    case "Équiper":
                        if (!string.IsNullOrEmpty(_contextWeaponName))
                        {
                            character.InventoryData.EquipItem(_contextWeaponName);
                            character.CalculateDerivedStats();
                        }
                        _showWeaponContextMenu = false;
                        break;
                    case "Déséquiper":
                        if (!string.IsNullOrEmpty(_contextWeaponName))
                        {
                            character.InventoryData.UnequipItem(_contextWeaponName);
                            character.CalculateDerivedStats();
                        }
                        _showWeaponContextMenu = false;
                        break;
                    case "Lancer":
                        if (!string.IsNullOrEmpty(_contextWeaponName))
                        {
                            character.InventoryData.UnequipItem(_contextWeaponName);
                            character.InventoryData.RemoveItem(_contextWeaponName);
                            character.CalculateDerivedStats();
                        }
                        _showWeaponContextMenu = false;
                        break;
                    case "Examiner":
                        if (!string.IsNullOrEmpty(_contextWeaponName))
                        {
                            _inspectWeaponText = BuildItemTooltip(_contextWeaponName, _contextWeaponIsEquipped);
                        }
                        _showWeaponContextMenu = false;
                        break;
                    default:
                        if (!_weaponContextMenuRect.Contains(_mousePosition))
                        {
                            _showWeaponContextMenu = false;
                        }
                        break;
                }
            }
        }

        _prevMouseState = mouse;
    }

    public void ResetScroll()
    {
        _scrollOffset = 0f;
        _prevScrollValue = 0;
    }

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphics, Character character, Campaign? campaign = null)
    {
        var vp = graphics.Viewport;
        _hoverTooltip = null;

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(20, 20, 20));

        if (_font != null && character != null)
        {
            _weaponItemRects.Clear();
            var c = character;
            int margin = Margin;
            int padding = 10;
            int scrollbarWidth = ScrollbarWidth;
            
            int sheetWidth = vp.Width - margin * 2 - scrollbarWidth - 10;
            int sheetHeight = vp.Height - margin * 2;
            int sheetX = margin;
            int sheetY = margin;
            
            // Layout calculations
            int col1Width = (int)(sheetWidth * 0.25f);
            int col2Width = (int)(sheetWidth * 0.35f);
            int col3Width = sheetWidth - col1Width - col2Width - padding * 2;

            // 1. Calculate heights without drawing (spriteBatch = null)
            int headerHeight = DrawHeader(null, c, sheetX, 0, sheetWidth);
            int col1Height = DrawLeftColumn(null, c, 0, 0, col1Width);
            int col2Height = DrawMiddleColumn(null, c, 0, 0, col2Width);
            int col3Height = DrawRightColumn(null, c, 0, 0, col3Width);

            int maxColHeight = Math.Max(col1Height, Math.Max(col2Height, col3Height));
            int totalContentHeight = headerHeight + maxColHeight + 40;

            int maxScroll = Math.Max(0, totalContentHeight - sheetHeight);
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, maxScroll);
            
            var scissorRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
            var previousScissor = graphics.ScissorRectangle;
            graphics.ScissorRectangle = Rectangle.Intersect(scissorRect, graphics.Viewport.Bounds);
            
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = true });
            
            var sheetRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
            spriteBatch.Draw(_pixel, sheetRect, new Color(240, 235, 225));
            
            int scrollY = sheetY - (int)_scrollOffset;
            
            // 2. Actual Drawing
            DrawHeader(spriteBatch, c, sheetX, scrollY, sheetWidth);
            
            int contentY = scrollY + headerHeight + padding;
            int col1X = sheetX + padding;
            int col2X = col1X + col1Width + padding;
            int col3X = col2X + col2Width + padding;
            
            DrawLeftColumn(spriteBatch, c, col1X, contentY, col1Width);
            DrawMiddleColumn(spriteBatch, c, col2X, contentY, col2Width);
            DrawRightColumn(spriteBatch, c, col3X, contentY, col3Width);
            
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            graphics.ScissorRectangle = previousScissor;
            
            if (totalContentHeight > sheetHeight)
            {
                DrawScrollbar(spriteBatch, sheetX + sheetWidth + 5, sheetY, scrollbarWidth, sheetHeight, maxScroll, totalContentHeight);
            }
            
            var hint = "Appuyez sur 'C' pour fermer | Molette pour défiler";
            var hintSize = _font.MeasureString(hint) * 0.8f;
            spriteBatch.DrawString(_font, hint, new Vector2((vp.Width - hintSize.X) / 2, vp.Height - 30), Color.White * 0.8f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            DrawCloseButton(spriteBatch, vp);
            DrawTooltip(spriteBatch, vp);
            DrawWeaponContextMenu(spriteBatch, vp);
            DrawInspectPopup(spriteBatch, vp);
        }
    }

    public Rectangle GetCloseButtonRect(Viewport viewport)
    {
        int x = viewport.Width - Margin - CloseButtonWidth;
        int y = Margin + 4;
        return new Rectangle(x, y, CloseButtonWidth, CloseButtonHeight);
    }

    private void DrawCloseButton(SpriteBatch spriteBatch, Viewport viewport)
    {
        var closeButtonRect = GetCloseButtonRect(viewport);
        spriteBatch.Draw(_pixel, closeButtonRect, new Color(140, 40, 40));
        DrawBorder(spriteBatch, closeButtonRect, Color.Black, 2);
        RegisterTooltip(closeButtonRect, "Fermer la feuille de personnage (raccourci: C).");

        var text = "Quitter";
        var textSize = _font.MeasureString(text);
        var textPos = new Vector2(
            closeButtonRect.X + (closeButtonRect.Width - textSize.X * 0.7f) * 0.5f,
            closeButtonRect.Y + (closeButtonRect.Height - textSize.Y * 0.7f) * 0.5f
        );

        spriteBatch.DrawString(_font, text, textPos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private void DrawScrollbar(SpriteBatch spriteBatch, int x, int y, int width, int height, int maxScroll, int contentHeight)
    {
        var trackRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, trackRect, new Color(60, 60, 60));
        DrawBorder(spriteBatch, trackRect, Color.Black, 1);
        
        float contentRatio = (float)height / contentHeight;
        int thumbHeight = (int)(height * contentRatio);
        thumbHeight = Math.Max(thumbHeight, 30);
        
        float scrollRatio = maxScroll > 0 ? _scrollOffset / maxScroll : 0;
        int thumbY = y + (int)((height - thumbHeight) * scrollRatio);
        
        var thumbRect = new Rectangle(x + 2, thumbY, width - 4, thumbHeight);
        spriteBatch.Draw(_pixel, thumbRect, new Color(180, 180, 180));
        DrawBorder(spriteBatch, thumbRect, new Color(100, 100, 100), 1);
    }

    private int DrawHeader(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int padding = 10;
        int nameAreaHeight = 60;
        
        if (spriteBatch != null)
        {
            var logoRect = new Rectangle(x + padding, y + padding, 120, 40);
            spriteBatch.Draw(_pixel, logoRect, Color.Black * 0.2f);
            spriteBatch.DrawString(_font, "D&D 5E", new Vector2(logoRect.X + 10, logoRect.Y + 10), Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            int nameX = x + padding + 130;
            int nameY = y + padding + 5;
            spriteBatch.DrawString(_font, SafeString(c.Name), new Vector2(nameX, nameY), Color.Black, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
        }
        
        int infoStartY = y + padding + 50;
        int fieldWidth = (width - padding * 2) / 6;
        int maxFieldHeight = 0;
        
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "CLASS & LEVEL", $"{c.Class} {c.Level}", x + padding, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "BACKGROUND", c.Background, x + padding + fieldWidth, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "PLAYER NAME", "", x + padding + fieldWidth * 2, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "RACE", c.Race, x + padding + fieldWidth * 3, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "ALIGNMENT", c.Alignment, x + padding + fieldWidth * 4, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "EXPERIENCE POINTS", c.XP.ToString(), x + padding + fieldWidth * 5, infoStartY, fieldWidth - 5));
        
        return padding + 50 + maxFieldHeight + 10;
    }

    private int DrawHeaderField(SpriteBatch? spriteBatch, string label, string value, int x, int y, int width)
    {
        string safeValue = SafeString(value);
        string wrappedValue = WrapText(_font, safeValue, width, 0.7f);
        var valueSize = _font.MeasureString(wrappedValue) * 0.7f;
        int height = (int)valueSize.Y + 15;
        height = Math.Max(height, 30);

        if (spriteBatch != null)
        {
            var fieldRect = new Rectangle(x, y, width, height);
            spriteBatch.DrawString(_font, label, new Vector2(x, y), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, wrappedValue, new Vector2(x + 2, y + 10), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            RegisterTooltip(fieldRect, string.IsNullOrWhiteSpace(value)
                ? $"{label}: champ à renseigner."
                : $"{label}: {value}.");

            var lineRect = new Rectangle(x, y + height - 2, width, 1);
            spriteBatch.Draw(_pixel, lineRect, Color.Black * 0.3f);
        }
        
        return height;
    }

    private int DrawLeftColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int currentY = y;
        int boxSize = 70;
        int boxSpacing = 8;
        
        var abilities = new[] 
        {
            ("STRENGTH", c.Strength, c.StrengthSaveProficiency),
            ("DEXTERITY", c.Dexterity, c.DexteritySaveProficiency),
            ("CONSTITUTION", c.Constitution, c.ConstitutionSaveProficiency),
            ("INTELLIGENCE", c.Intelligence, c.IntelligenceSaveProficiency),
            ("WISDOM", c.Wisdom, c.WisdomSaveProficiency),
            ("CHARISMA", c.Charisma, c.CharismaSaveProficiency)
        };
        
        foreach (var (name, score, saveProficiency) in abilities)
        {
            if (spriteBatch != null) DrawAbilityBox(spriteBatch, c, name, score, saveProficiency, x, currentY, width, boxSize);
            currentY += boxSize + boxSpacing;
        }
        
        currentY += 10;
        if (spriteBatch != null)
        {
            DrawSmallBox(spriteBatch, "INSPIRATION", "", x, currentY, width / 2 - 5, 50, "Inspiration: avantage sur un jet important quand le MJ l'accorde.");
            DrawSmallBox(spriteBatch, "PROFICIENCY BONUS", FormatModifier(c.ProficiencyBonus), x + width / 2 + 5, currentY, width / 2 - 5, 50, $"Bonus de maîtrise actuel: {FormatModifier(c.ProficiencyBonus)}.");
        }
        currentY += 60;
        
        int passivePerception = 10 + c.GetAbilityModifier(c.Wisdom) + (c.PerceptionProficiency ? c.ProficiencyBonus : 0);
        if (spriteBatch != null) DrawSmallBox(spriteBatch, "PASSIVE WISDOM (PERCEPTION)", passivePerception.ToString(), x, currentY, width, 40, $"Perception passive = 10 + mod. Sagesse + maîtrise éventuelle = {passivePerception}.");
        currentY += 50;

        currentY += DrawProficienciesBox(spriteBatch, c, x, currentY, width);

        return currentY - y;
    }
    
    private int DrawProficienciesBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int padding = 10;
        int contentY = 25;
        
        // Armor proficiencies
        if (c.ArmorProficiencies != null && c.ArmorProficiencies.Count > 0)
        {
            contentY += 15;
            foreach (var armor in c.ArmorProficiencies)
            {
                contentY += 14;
            }
            contentY += 5;
        }
        
        // Weapon proficiencies
        if (c.WeaponProficiencies != null && c.WeaponProficiencies.Count > 0)
        {
            contentY += 15;
            foreach (var weapon in c.WeaponProficiencies)
            {
                contentY += 14;
            }
            contentY += 5;
        }
        
        // Class info
        contentY += 15 + 14 + 14;

        // Barbarian-specific level features
        if (c.Class == "Barbarian")
        {
            var classData = ClassData.GetClass(c.Class);
            var levelData = classData.GetLevelData(c.Level);
            if (levelData != null)
            {
                contentY += 5 + 15 + 14 + 14 + 14;
            }
        }

        int height = contentY + 10;

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);

            spriteBatch.DrawString(_font, "PROFICIENCIES & LANGUAGES", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            int drawY = y + 25;
            if (c.ArmorProficiencies != null && c.ArmorProficiencies.Count > 0)
            {
                spriteBatch.DrawString(_font, "Armor:", new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                drawY += 15;
                foreach (var armor in c.ArmorProficiencies)
                {
                    spriteBatch.DrawString(_font, SafeString($"• {armor}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), $"Maîtrise d'armure: {armor}.");
                    drawY += 14;
                }
                drawY += 5;
            }

            if (c.WeaponProficiencies != null && c.WeaponProficiencies.Count > 0)
            {
                spriteBatch.DrawString(_font, "Weapons:", new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                drawY += 15;
                foreach (var weapon in c.WeaponProficiencies)
                {
                    spriteBatch.DrawString(_font, SafeString($"• {weapon}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), $"Maîtrise d'arme: {weapon}.");
                    drawY += 14;
                }
                drawY += 5;
            }

            var classData = ClassData.GetClass(c.Class);
            spriteBatch.DrawString(_font, "Class Info:", new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            drawY += 15;
            spriteBatch.DrawString(_font, SafeString($"• Hit Die: d{c.HitDiceType}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), $"Dé de vie de classe: d{c.HitDiceType}.");
            drawY += 14;
            spriteBatch.DrawString(_font, SafeString($"• Primary Ability: {classData.PrimaryAbility}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), $"Capacité principale de la classe: {classData.PrimaryAbility}.");
            drawY += 14;

            if (c.Class == "Barbarian")
            {
                var levelData = classData.GetLevelData(c.Level);
                if (levelData != null)
                {
                    drawY += 5;
                    spriteBatch.DrawString(_font, "Barbarian:", new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    drawY += 15;
                    string ragesMax = levelData.Rages == -1 ? "Illimité" : levelData.Rages.ToString();
                    spriteBatch.DrawString(_font, SafeString($"• Rages: {c.RagesRemaining}/{ragesMax}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), $"Rages par jour: {ragesMax}. Restantes: {c.RagesRemaining}.");
                    drawY += 14;
                    spriteBatch.DrawString(_font, SafeString($"• Rage Damage: +{levelData.RageDamage}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), $"Bonus de dégâts en rage: +{levelData.RageDamage}.");
                    drawY += 14;
                    spriteBatch.DrawString(_font, SafeString($"• {levelData.Features}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), $"Capacités de niveau {c.Level}: {levelData.Features}.");
                }
            }
        }

        return height;
    }

    private void DrawAbilityBox(SpriteBatch spriteBatch, Character c, string name, int score, bool saveProficiency, int x, int y, int width, int height)
    {
        var outerRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, outerRect, Color.White);
        DrawBorder(spriteBatch, outerRect, Color.Black, 2);
        
        var nameSize = _font.MeasureString(name);
        spriteBatch.DrawString(_font, name, new Vector2(x + (width - nameSize.X * 0.5f) / 2, y + 2), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        
        int circleSize = 40;
        int circleX = x + (width - circleSize) / 2;
        int circleY = y + 15;
        DrawCircle(spriteBatch, circleX, circleY, circleSize, Color.Black, 2);
        
        int modifier = c.GetAbilityModifier(score);
        string modText = FormatModifier(modifier);
        var modSize = _font.MeasureString(modText);
        spriteBatch.DrawString(_font, modText, new Vector2(circleX + (circleSize - modSize.X) / 2, circleY + (circleSize - modSize.Y) / 2), Color.Black);
        
        string scoreText = score.ToString();
        var scoreSize = _font.MeasureString(scoreText);
        spriteBatch.DrawString(_font, scoreText, new Vector2(x + (width - scoreSize.X * 0.7f) / 2, y + height - 18), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        int checkboxSize = 12;
        int checkboxX = x + 5;
        int checkboxY = y + height / 2 - checkboxSize / 2;
        DrawCheckbox(spriteBatch, checkboxX, checkboxY, checkboxSize, saveProficiency);

        RegisterTooltip(outerRect, $"{name}: score {score} ({modText}). Jet de sauvegarde {(saveProficiency ? "maîtrisé" : "non maîtrisé")}.");
        
        spriteBatch.DrawString(_font, "SAVING THROWS", new Vector2(x + 2, y + height - 10), Color.Black * 0.4f, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
    }

    private int DrawMiddleColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int currentY = y;
        int smallBoxSize = 60;
        int topBoxWidth = (width - 20) / 3;
        
        if (spriteBatch != null)
        {
            DrawHexBox(spriteBatch, "ARMOR CLASS", c.ArmorClass.ToString(), x, currentY, topBoxWidth, smallBoxSize, $"Classe d'armure: {c.ArmorClass}.");
            DrawCircleBox(spriteBatch, "INITIATIVE", FormatModifier(c.GetAbilityModifier(c.Dexterity)), x + topBoxWidth + 10, currentY, topBoxWidth, smallBoxSize, $"Initiative: {FormatModifier(c.GetAbilityModifier(c.Dexterity))}.");
            DrawCircleBox(spriteBatch, "SPEED", $"{c.Speed}", x + topBoxWidth * 2 + 20, currentY, topBoxWidth, smallBoxSize, $"Vitesse: {c.Speed} ft.");
        }
        currentY += smallBoxSize + 10;
        
        int hpHeight = 80;
        if (spriteBatch != null) DrawHPBox(spriteBatch, c, x, currentY, width, hpHeight);
        currentY += hpHeight + 10;
        
        int hdWidth = width / 2 - 5;
        if (spriteBatch != null)
        {
            DrawHitDiceBox(spriteBatch, c, x, currentY, hdWidth, 80);
            DrawDeathSavesBox(spriteBatch, c, x + hdWidth + 10, currentY, hdWidth, 80);
        }
        currentY += 90;
        
        currentY += DrawAttacksBox(spriteBatch, c, x, currentY, width);
        currentY += 10;

        currentY += DrawEquipmentBox(spriteBatch, c, x, currentY, width);
        
        return currentY - y;
    }

    private int DrawRightColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int currentY = y;
        
        currentY += DrawSkillsBox(spriteBatch, c, x, currentY, width);
        currentY += 10;
        
        currentY += DrawTextBox(spriteBatch, "PERSONALITY TRAITS", "", x, currentY, width);
        currentY += 10;
        
        currentY += DrawTextBox(spriteBatch, "IDEALS", "", x, currentY, width);
        currentY += 10;

        currentY += DrawTextBox(spriteBatch, "BONDS", "", x, currentY, width);
        currentY += 10;
        
        currentY += DrawTextBox(spriteBatch, "FLAWS", "", x, currentY, width);
        
        return currentY - y;
    }
    
    private int DrawAttacksBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int entryHeight = 20;
        int headerHeight = 45;
        int entryCount = Math.Max(3, (c.InventoryData.EquippedWeapon != null ? 1 : 0) + 2);
        int height = headerHeight + (entryCount * entryHeight) + 10;

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, "ATTACKS & SPELLCASTING", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            
            int headerY = y + 30;
            int nameCol = x + 10;
            int atkBonusCol = x + width / 2;
            int damageCol = x + width / 2 + 80;

            spriteBatch.DrawString(_font, "NOM", new Vector2(nameCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "BONUS ATK", new Vector2(atkBonusCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "DÉGÂTS/TYPE", new Vector2(damageCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

            spriteBatch.Draw(_pixel, new Rectangle(x + 5, headerY + 15, width - 10, 1), Color.Black * 0.3f);

            int entryY = headerY + 20;
            if (c.InventoryData.EquippedWeapon != null)
            {
                string weapon = c.InventoryData.EquippedWeapon;
                int atkBonus = c.GetAbilityModifier(c.Strength) + c.ProficiencyBonus;
                string damage = GetWeaponDamage(weapon);
                spriteBatch.DrawString(_font, SafeString(weapon), new Vector2(nameCol, entryY), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, FormatModifier(atkBonus), new Vector2(atkBonusCol, entryY), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, SafeString(damage), new Vector2(damageCol, entryY), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                RegisterTooltip(new Rectangle(nameCol, entryY, width - 20, entryHeight), BuildWeaponTooltip(weapon, atkBonus, damage));
            }
        }
        
        return height;
    }
    
    private int DrawEquipmentBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int lineHeight = 18;
        int inventoryCount = c.InventoryData.Items.Count;
        int height = 60 + (inventoryCount / 2 * lineHeight) + 40;
        height = Math.Max(150, height);

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, "EQUIPMENT", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            
            int itemY = y + 30;
            int col1 = x + 10;
            int col2 = x + width / 2 + 5;
            int currentItemY = itemY;
            bool left = true;

            foreach (var item in c.InventoryData.Items)
            {
                int curX = left ? col1 : col2;
                var itemRect = new Rectangle(curX, currentItemY, width / 2 - 15, lineHeight);
                spriteBatch.DrawString(_font, SafeString($"• {item}"), new Vector2(curX, currentItemY), Color.Black * 0.8f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                RegisterTooltip(itemRect, BuildItemTooltip(item, item == c.InventoryData.EquippedArmor || item == c.InventoryData.EquippedShield || item == c.InventoryData.EquippedWeapon));

                var itemData = ItemDatabase.GetItem(item);
                if (itemData.Type == ItemType.Weapon)
                {
                    bool isEquippedWeapon = item == c.InventoryData.EquippedWeapon;
                    _weaponItemRects.Add((itemRect, item, isEquippedWeapon));
                }

                if (!left) currentItemY += lineHeight;
                left = !left;
            }

            int totalWeight = c.InventoryData.GetTotalWeight();
            spriteBatch.DrawString(_font, $"Poids Total: {totalWeight} lbs", new Vector2(x + 10, y + height - 35), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, $"Or: {c.GoldPieces} gp", new Vector2(x + 10, y + height - 20), Color.DarkGoldenrod, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
        
        return height;
    }

    private void DrawHexBox(SpriteBatch spriteBatch, string label, string value, int x, int y, int width, int height, string? tooltipText = null)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        var valueSize = _font.MeasureString(value);
        spriteBatch.DrawString(_font, value, new Vector2(x + (width - valueSize.X * 1.5f) / 2, y + 10), Color.Black, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
        var labelSize = _font.MeasureString(label);
        spriteBatch.DrawString(_font, label, new Vector2(x + (width - labelSize.X * 0.5f) / 2, y + height - 15), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        if (tooltipText != null) RegisterTooltip(rect, tooltipText);
    }

    private void DrawCircleBox(SpriteBatch spriteBatch, string label, string value, int x, int y, int width, int height, string? tooltipText = null)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        var valueSize = _font.MeasureString(value);
        spriteBatch.DrawString(_font, value, new Vector2(x + (width - valueSize.X) / 2, y + 15), Color.Black);
        var labelSize = _font.MeasureString(label);
        spriteBatch.DrawString(_font, label, new Vector2(x + (width - labelSize.X * 0.5f) / 2, y + height - 15), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        if (tooltipText != null) RegisterTooltip(rect, tooltipText);
    }

    private void DrawHPBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        spriteBatch.DrawString(_font, "PV Max", new Vector2(x + 5, y + 5), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, c.MaxHP.ToString(), new Vector2(x + width - 40, y + 5), Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        var hpRect = new Rectangle(x + 5, y + 25, width - 10, 30);
        spriteBatch.Draw(_pixel, hpRect, new Color(220, 220, 220));
        DrawBorder(spriteBatch, hpRect, Color.Black, 1);
        string currentHpText = c.CurrentHP.ToString();
        var hpSize = _font.MeasureString(currentHpText);
        spriteBatch.DrawString(_font, currentHpText, new Vector2(x + (width - hpSize.X * 1.2f) / 2, y + 28), Color.Black, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, "POINTS DE VIE ACTUELS", new Vector2(x + 10, y + 58), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        RegisterTooltip(rect, $"PV: {c.CurrentHP}/{c.MaxHP}");
    }

    private void DrawHitDiceBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        spriteBatch.DrawString(_font, "Total", new Vector2(x + 5, y + 5), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, $"d{c.HitDiceType}", new Vector2(x + 40, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        var diceRect = new Rectangle(x + 5, y + 25, width - 10, 30);
        spriteBatch.Draw(_pixel, diceRect, new Color(220, 220, 220));
        DrawBorder(spriteBatch, diceRect, Color.Black, 1);
        string diceText = $"{c.HitDiceRemaining}/{c.HitDiceTotal}";
        var diceSize = _font.MeasureString(diceText);
        spriteBatch.DrawString(_font, diceText, new Vector2(x + (width - diceSize.X) / 2, y + 30), Color.Black);
        spriteBatch.DrawString(_font, "DÉS DE VIE", new Vector2(x + 10, y + height - 15), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        RegisterTooltip(rect, $"Dés de vie: {diceText} (d{c.HitDiceType}). Utilisés pendant les repos courts.");
    }

    private void DrawDeathSavesBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        spriteBatch.DrawString(_font, "JETS DE MORT", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        int circleSize = 16;
        int startY = y + 30;
        spriteBatch.DrawString(_font, "Succès", new Vector2(x + 10, startY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        for (int i = 0; i < 3; i++) {
            int cx = x + width - 70 + i * 22;
            DrawCircle(spriteBatch, cx, startY, circleSize, Color.Black, 1);
            if (i < c.DeathSaveSuccesses) spriteBatch.Draw(_pixel, new Rectangle(cx + 4, startY + 4, circleSize - 8, circleSize - 8), Color.Black);
        }
        startY += 25;
        spriteBatch.DrawString(_font, "Échecs", new Vector2(x + 10, startY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        for (int i = 0; i < 3; i++) {
            int cx = x + width - 70 + i * 22;
            DrawCircle(spriteBatch, cx, startY, circleSize, Color.Black, 1);
            if (i < c.DeathSaveFailures) spriteBatch.Draw(_pixel, new Rectangle(cx + 4, startY + 4, circleSize - 8, circleSize - 8), Color.Black);
        }
        RegisterTooltip(rect, $"Jets de mort: {c.DeathSaveSuccesses} succès, {c.DeathSaveFailures} échecs.");
    }

    private int DrawSkillsBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int lineHeight = 18;
        var skills = new[] {
            ("Acrobatics", "DEX"), ("Animal Handling", "WIS"), ("Arcana", "INT"), ("Athletics", "STR"),
            ("Deception", "CHA"), ("History", "INT"), ("Insight", "WIS"), ("Intimidation", "CHA"),
            ("Investigation", "INT"), ("Medicine", "WIS"), ("Nature", "INT"), ("Perception", "WIS"),
            ("Performance", "CHA"), ("Persuasion", "CHA"), ("Religion", "INT"), ("Sleight of Hand", "DEX"),
            ("Stealth", "DEX"), ("Survival", "WIS")
        };
        int height = 25 + (skills.Length * lineHeight) + 10;

        if (spriteBatch != null) {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, "SKILLS", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            int skillY = y + 25;
            foreach (var (skillName, ability) in skills) {
                int bonus = c.GetSkillBonus(skillName, out _);
                bool proficient = GetSkillProficiency(c, skillName);
                DrawCheckbox(spriteBatch, x + 8, skillY, 10, proficient);
                string bonusText = FormatModifier(bonus);
                spriteBatch.DrawString(_font, bonusText, new Vector2(x + 25, skillY - 2), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, skillName, new Vector2(x + 60, skillY - 2), proficient ? Color.Black : Color.Black * 0.6f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, $"({ability})", new Vector2(x + width - 45, skillY - 2), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                RegisterTooltip(new Rectangle(x + 4, skillY - 2, width - 8, lineHeight), $"{skillName} ({ability}): {bonusText}");
                skillY += lineHeight;
            }
        }
        return height;
    }

    private int DrawTextBox(SpriteBatch? spriteBatch, string label, string content, int x, int y, int width)
    {
        string wrappedContent = WrapText(_font, content, width - 20, 0.6f);
        var contentSize = _font.MeasureString(wrappedContent) * 0.6f;
        int height = (int)Math.Max(60, 35 + contentSize.Y);

        if (spriteBatch != null) {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, label, new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            if (!string.IsNullOrEmpty(wrappedContent))
                spriteBatch.DrawString(_font, wrappedContent, new Vector2(x + 10, y + 25), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            RegisterTooltip(rect, string.IsNullOrWhiteSpace(content)
                ? $"{label}: aucune note renseignée."
                : $"{label}: {content}");
        }
        return height;
    }

    private void RegisterTooltip(Rectangle area, string text) {
        if (_hoverTooltip == null && area.Contains(_mousePosition)) _hoverTooltip = text;
    }

    private void DrawTooltip(SpriteBatch spriteBatch, Viewport viewport) {
        if (string.IsNullOrWhiteSpace(_hoverTooltip)) return;
        const int padding = 8;
        var safeTooltip = SafeString(_hoverTooltip);
        var textSize = _font.MeasureString(safeTooltip) * 0.8f;
        int width = (int)textSize.X + padding * 2;
        int height = (int)textSize.Y + padding * 2;
        int x = _mousePosition.X + 16, y = _mousePosition.Y + 18;
        if (x + width > viewport.Width - 4) x = viewport.Width - width - 4;
        if (y + height > viewport.Height - 4) y = _mousePosition.Y - height - 8;
        x = Math.Max(4, x); y = Math.Max(4, y);
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, new Color(30, 30, 30, 240));
        DrawBorder(spriteBatch, rect, new Color(220, 220, 220), 1);
        spriteBatch.DrawString(_font, safeTooltip, new Vector2(x + padding, y + padding), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private Rectangle BuildContextMenuRect(Point mousePosition)
    {
        const int width = 180;
        const int optionHeight = 30;
        int optionCount = GetContextMenuOptions().Length;
        return new Rectangle(mousePosition.X, mousePosition.Y, width, optionHeight * optionCount);
    }

    private string? GetContextMenuOptionAt(Point position)
    {
        if (!_weaponContextMenuRect.Contains(position)) return null;

        int relativeY = position.Y - _weaponContextMenuRect.Y;
        int optionIndex = relativeY / 30;
        var options = GetContextMenuOptions();
        return optionIndex >= 0 && optionIndex < options.Length ? options[optionIndex] : null;
    }

    private string[] GetContextMenuOptions()
    {
        if (_contextWeaponIsEquipped)
        {
            return new[] { "Déséquiper", "Lancer", "Examiner" };
        }

        return new[] { "Équiper", "Examiner" };
    }

    private void DrawWeaponContextMenu(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (!_showWeaponContextMenu || string.IsNullOrEmpty(_contextWeaponName)) return;

        int menuX = Math.Clamp(_weaponContextMenuRect.X, 8, viewport.Width - _weaponContextMenuRect.Width - 8);
        int menuY = Math.Clamp(_weaponContextMenuRect.Y, 8, viewport.Height - _weaponContextMenuRect.Height - 8);
        _weaponContextMenuRect = new Rectangle(menuX, menuY, _weaponContextMenuRect.Width, _weaponContextMenuRect.Height);

        spriteBatch.Draw(_pixel, _weaponContextMenuRect, new Color(35, 35, 35, 245));
        DrawBorder(spriteBatch, _weaponContextMenuRect, new Color(220, 220, 220), 1);

        var options = GetContextMenuOptions();
        for (int i = 0; i < options.Length; i++)
        {
            var optionRect = new Rectangle(_weaponContextMenuRect.X, _weaponContextMenuRect.Y + i * 30, _weaponContextMenuRect.Width, 30);
            bool hovered = optionRect.Contains(_mousePosition);
            if (hovered)
            {
                spriteBatch.Draw(_pixel, optionRect, new Color(90, 90, 90, 230));
            }

            spriteBatch.DrawString(_font, SafeString(options[i]), new Vector2(optionRect.X + 10, optionRect.Y + 7), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
    }

    private void DrawInspectPopup(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (string.IsNullOrWhiteSpace(_inspectWeaponText)) return;

        string title = "Examiner l'arme";
        string content = WrapText(_font, SafeString(_inspectWeaponText), 320, 0.65f);
        int width = 360;
        int height = 160;
        int x = (viewport.Width - width) / 2;
        int y = (viewport.Height - height) / 2;
        _inspectPopupRect = new Rectangle(x, y, width, height);

        spriteBatch.Draw(_pixel, _inspectPopupRect, new Color(20, 20, 20, 245));
        DrawBorder(spriteBatch, _inspectPopupRect, Color.White, 2);
        spriteBatch.DrawString(_font, SafeString(title), new Vector2(x + 12, y + 10), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, content, new Vector2(x + 12, y + 40), Color.White * 0.95f, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, "(Cliquez à l'extérieur pour fermer)", new Vector2(x + 12, y + height - 24), Color.White * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }

    private void DrawSmallBox(SpriteBatch spriteBatch, string label, string value, int x, int y, int width, int height, string? tooltipText = null) {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        if (!string.IsNullOrEmpty(value)) {
            var vs = _font.MeasureString(value);
            spriteBatch.DrawString(_font, value, new Vector2(x + (width - vs.X) / 2, y + 8), Color.Black);
        }
        var ls = _font.MeasureString(label);
        spriteBatch.DrawString(_font, label, new Vector2(x + (width - ls.X * 0.45f) / 2, y + height - 12), Color.Black, 0f, Vector2.Zero, 0.45f, SpriteEffects.None, 0f);
        if (tooltipText != null) RegisterTooltip(rect, tooltipText);
    }

    private void DrawCheckbox(SpriteBatch spriteBatch, int x, int y, int size, bool checked_) {
        var box = new Rectangle(x, y, size, size);
        spriteBatch.Draw(_pixel, box, Color.White);
        DrawBorder(spriteBatch, box, Color.Black, 1);
        if (checked_) spriteBatch.Draw(_pixel, new Rectangle(x + 2, y + 2, size - 4, size - 4), Color.Black);
    }

    private void DrawCircle(SpriteBatch spriteBatch, int x, int y, int size, Color color, int thickness) {
        int r = size / 2;
        for (int dy = 0; dy < size; dy++) {
            for (int dx = 0; dx < size; dx++) {
                float dist = (float)Math.Sqrt((dx - r) * (dx - r) + (dy - r) * (dy - r));
                if (dist >= r - thickness && dist <= r) spriteBatch.Draw(_pixel, new Rectangle(x + dx, y + dy, 1, 1), color);
            }
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness) {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private string FormatModifier(int value) => value >= 0 ? $"+{value}" : $"{value}";

    private string SafeString(string? text) {
        if (_font == null || text == null) return text ?? "";
        var chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++) if (!_supportedChars.Contains(chars[i])) chars[i] = '?';
        return new string(chars);
    }
    
    private string GetWeaponDamage(string weapon) {
        var item = ItemDatabase.GetItem(weapon);
        return (item != null && !string.IsNullOrEmpty(item.DamageDice)) ? $"{item.DamageDice} {item.DamageType.ToLower()}" : "1d6";
    }

    private string BuildWeaponTooltip(string weaponName, int attackBonus, string damage) {
        var item = ItemDatabase.GetItem(weaponName);
        if (item == null) return weaponName;
        string rangeInfo = item.IsRanged ? $"Portée: {item.Range}/{item.Range * 3} ft." : "Attaque de mêlée.";
        return $"{weaponName} (équipée)\nBonus: {FormatModifier(attackBonus)}\nDégâts: {damage}\n{rangeInfo}";
    }

    private string BuildItemTooltip(string itemName, bool isEquipped = false) {
        var item = ItemDatabase.GetItem(itemName);
        if (item == null) return itemName;
        string eq = isEquipped ? " (équipé)" : "";
        string details = $"{item.Name}{eq}\nPoids: {item.Weight} lbs | Valeur: {item.Value} gp";
        if (!string.IsNullOrWhiteSpace(item.Description)) details += $"\n{item.Description}";
        return details;
    }

    private string WrapText(SpriteFont font, string text, float maxLineWidth, float scale = 0.7f) {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(' ');
        string result = "", currentLine = "";
        foreach (string word in words) {
            if (font.MeasureString(currentLine + word).X * scale < maxLineWidth) currentLine += word + " ";
            else { result += currentLine + "\n"; currentLine = word + " "; }
        }
        return result + currentLine;
    }

    private bool GetSkillProficiency(Character c, string skill) => skill switch {
        "Acrobatics" => c.AcrobaticsProficiency, "Animal Handling" => c.AnimalHandlingProficiency,
        "Arcana" => c.ArcanaProficiency, "Athletics" => c.AthleticsProficiency,
        "Deception" => c.DeceptionProficiency, "History" => c.HistoryProficiency,
        "Insight" => c.InsightProficiency, "Intimidation" => c.IntimidationProficiency,
        "Investigation" => c.InvestigationProficiency, "Medicine" => c.MedicineProficiency,
        "Nature" => c.NatureProficiency, "Perception" => c.PerceptionProficiency,
        "Performance" => c.PerformanceProficiency, "Persuasion" => c.PersuasionProficiency,
        "Religion" => c.ReligionProficiency, "Sleight of Hand" => c.SleightOfHandProficiency,
        "Stealth" => c.StealthProficiency, "Survival" => c.SurvivalProficiency,
        _ => false
    };
}
