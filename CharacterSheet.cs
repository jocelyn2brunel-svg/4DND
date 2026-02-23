#nullable enable
using System.Collections.Generic;
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
    private const int HeaderHeight = 110;
    private Point _mousePosition;
    private string? _hoverTooltip;

    public CharacterSheet(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Update(MouseState mouse)
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
            var c = character;
            int margin = Margin;
            int padding = 10;
            int scrollbarWidth = ScrollbarWidth;
            
            int sheetWidth = vp.Width - margin * 2 - scrollbarWidth - 10;
            int sheetHeight = vp.Height - margin * 2;
            int sheetX = margin;
            int sheetY = margin;
            
            int scrollY = sheetY - (int)_scrollOffset;

            int col1Width = (int)(sheetWidth * 0.25f);
            int col2Width = (int)(sheetWidth * 0.35f);
            int col3Width = sheetWidth - col1Width - col2Width - padding * 2;

            int col1X = sheetX + padding;
            int col2X = col1X + col1Width + padding;
            int col3X = col2X + col2Width + padding;

            // Dry run or height calculation pass
            int headerHeight = DrawHeader(null, c, sheetX, scrollY, sheetWidth);
            int contentY = scrollY + headerHeight;

            int h1 = DrawLeftColumn(null, c, col1X, contentY, col1Width);
            int h2 = DrawMiddleColumn(null, c, col2X, contentY, col2Width);
            int h3 = DrawRightColumn(null, c, col3X, contentY, col3Width);

            int maxColHeight = System.Math.Max(h1, System.Math.Max(h2, h3));
            int totalContentHeight = headerHeight + maxColHeight + 40;

            int maxScroll = System.Math.Max(0, totalContentHeight - sheetHeight);
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, maxScroll);
            
            var scissorRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
            var previousScissor = graphics.ScissorRectangle;
            graphics.ScissorRectangle = scissorRect;
            
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = true });
            
            var sheetRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
            spriteBatch.Draw(_pixel, sheetRect, new Color(240, 235, 225));
            
            // Actual draw pass
            scrollY = sheetY - (int)_scrollOffset;
            headerHeight = DrawHeader(spriteBatch, c, sheetX, scrollY, sheetWidth);
            contentY = scrollY + headerHeight;

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
            
            var hint = "Press C to close | Mouse wheel to scroll";
            var hintSize = _font.MeasureString(hint);
            spriteBatch.DrawString(_font, hint, new Vector2((vp.Width - hintSize.X) / 2, vp.Height - 30), Color.White * 0.8f);

            DrawCloseButton(spriteBatch, vp);
            DrawTooltip(spriteBatch, vp);
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
        thumbHeight = System.Math.Max(thumbHeight, 30);
        
        float scrollRatio = maxScroll > 0 ? _scrollOffset / maxScroll : 0;
        int thumbY = y + (int)((height - thumbHeight) * scrollRatio);
        
        var thumbRect = new Rectangle(x + 2, thumbY, width - 4, thumbHeight);
        spriteBatch.Draw(_pixel, thumbRect, new Color(180, 180, 180));
        DrawBorder(spriteBatch, thumbRect, new Color(100, 100, 100), 1);
    }

    private int DrawHeader(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int padding = 10;
        
        if (spriteBatch != null)
        {
            var logoRect = new Rectangle(x + padding, y + padding, 120, 40);
            spriteBatch.Draw(_pixel, logoRect, Color.Black * 0.2f);
            spriteBatch.DrawString(_font, "D&D 5E", new Vector2(logoRect.X + 10, logoRect.Y + 10), Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            int nameX = x + padding + 130;
            int nameY = y + padding + 5;
            spriteBatch.DrawString(_font, c.Name, new Vector2(nameX, nameY), Color.Black, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
        }
        
        int infoY = y + padding + 50;
        int infoSpacing = (width - padding * 2) / 6;
        
        int h1 = DrawHeaderField(spriteBatch, "CLASS & LEVEL", $"{c.Class} {c.Level}", x + padding, infoY, infoSpacing - 5);
        int h2 = DrawHeaderField(spriteBatch, "BACKGROUND", c.Background, x + padding + infoSpacing, infoY, infoSpacing - 5);
        int h3 = DrawHeaderField(spriteBatch, "PLAYER NAME", "", x + padding + infoSpacing * 2, infoY, infoSpacing - 5);
        int h4 = DrawHeaderField(spriteBatch, "RACE", c.Race, x + padding + infoSpacing * 3, infoY, infoSpacing - 5);
        int h5 = DrawHeaderField(spriteBatch, "ALIGNMENT", c.Alignment, x + padding + infoSpacing * 4, infoY, infoSpacing - 5);
        int h6 = DrawHeaderField(spriteBatch, "EXPERIENCE POINTS", c.XP.ToString(), x + padding + infoSpacing * 5, infoY, infoSpacing - 5);

        int maxFieldHeight = System.Math.Max(h1, System.Math.Max(h2, System.Math.Max(h3, System.Math.Max(h4, System.Math.Max(h5, h6)))));

        return 50 + maxFieldHeight + 20;
    }

    private int DrawHeaderField(SpriteBatch? spriteBatch, string label, string value, int x, int y, int width)
    {
        string wrappedValue = WrapText(_font, value, width, 0.7f);
        var valueSize = _font.MeasureString(wrappedValue) * 0.7f;
        int fieldHeight = (int)System.Math.Max(26, 12 + valueSize.Y);

        if (spriteBatch != null)
        {
            var fieldRect = new Rectangle(x, y, width, fieldHeight);
            spriteBatch.DrawString(_font, label, new Vector2(x, y), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, wrappedValue, new Vector2(x + 2, y + 10), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            RegisterTooltip(fieldRect, string.IsNullOrWhiteSpace(value)
                ? $"{label}: champ à renseigner."
                : $"{label}: {value}.");

            var lineRect = new Rectangle(x, y + fieldHeight - 2, width, 1);
            spriteBatch.Draw(_pixel, lineRect, Color.Black * 0.3f);
        }
        
        return fieldHeight;
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
        // First pass: Calculate height
        int currentY = y + 25;
        if (c.ArmorProficiencies != null && c.ArmorProficiencies.Count > 0)
        {
            currentY += 15 + c.ArmorProficiencies.Count * 14 + 5;
        }
        if (c.WeaponProficiencies != null && c.WeaponProficiencies.Count > 0)
        {
            currentY += 15 + c.WeaponProficiencies.Count * 14 + 5;
        }
        currentY += 15 + 14 + 14 + 14; // Class info header + 3 lines

        int totalHeight = currentY - y + 10;

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, totalHeight);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);

            spriteBatch.DrawString(_font, "PROFICIENCIES & LANGUAGES", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            int contentY = y + 25;
            // Armor proficiencies
            if (c.ArmorProficiencies != null && c.ArmorProficiencies.Count > 0)
            {
                spriteBatch.DrawString(_font, "Armor:", new Vector2(x + 10, contentY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                contentY += 15;
                foreach (var armor in c.ArmorProficiencies)
                {
                    spriteBatch.DrawString(_font, $"? {armor}", new Vector2(x + 15, contentY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, contentY - 2, width - 24, 14), $"Maîtrise d'armure: {armor}.");
                    contentY += 14;
                }
                contentY += 5;
            }
            // Weapon proficiencies
            if (c.WeaponProficiencies != null && c.WeaponProficiencies.Count > 0)
            {
                spriteBatch.DrawString(_font, "Weapons:", new Vector2(x + 10, contentY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                contentY += 15;
                foreach (var weapon in c.WeaponProficiencies)
                {
                    spriteBatch.DrawString(_font, $"? {weapon}", new Vector2(x + 15, contentY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, contentY - 2, width - 24, 14), $"Maîtrise d'arme: {weapon}.");
                    contentY += 14;
                }
                contentY += 5;
            }
            // Class info
            var classData = ClassData.GetClass(c.Class);
            spriteBatch.DrawString(_font, "Class Info:", new Vector2(x + 10, contentY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            contentY += 15;
            spriteBatch.DrawString(_font, $"? Hit Die: d{c.HitDiceType}", new Vector2(x + 15, contentY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 12, contentY - 2, width - 24, 14), $"Dé de vie de classe: d{c.HitDiceType}.");
            contentY += 14;
            spriteBatch.DrawString(_font, $"? Primary Ability: {classData.PrimaryAbility}", new Vector2(x + 15, contentY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 12, contentY - 2, width - 24, 14), $"Capacité principale de la classe: {classData.PrimaryAbility}.");
            contentY += 14;
        }

        return totalHeight;
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

        RegisterTooltip(outerRect, $"{name}: score {score} ({modText}). Saving throw {(saveProficiency ? "proficient" : "not proficient")}.");
        
        spriteBatch.DrawString(_font, "SAVING THROWS", new Vector2(x + 2, y + height - 10), Color.Black * 0.4f, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
    }

    private int DrawMiddleColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int currentY = y;
        int smallBoxSize = 60;
        
        int topBoxWidth = (width - 20) / 3;
        if (spriteBatch != null)
        {
            DrawHexBox(spriteBatch, "ARMOR CLASS", c.ArmorClass.ToString(), x, currentY, topBoxWidth, smallBoxSize, $"Classe d'armure: {c.ArmorClass}. Plus elle est haute, plus vous êtes difficile à toucher.");
            DrawCircleBox(spriteBatch, "INITIATIVE", FormatModifier(c.GetAbilityModifier(c.Dexterity)), x + topBoxWidth + 10, currentY, topBoxWidth, smallBoxSize, $"Initiative basée sur la Dextérité: {FormatModifier(c.GetAbilityModifier(c.Dexterity))}.");
            DrawCircleBox(spriteBatch, "SPEED", $"{c.Speed}", x + topBoxWidth * 2 + 20, currentY, topBoxWidth, smallBoxSize, $"Vitesse de déplacement par tour: {c.Speed} ft.");
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
        
        // Attacks & Spellcasting section
        currentY += DrawAttacksBox(spriteBatch, c, x, currentY, width);
        currentY += 10;
        
        // Equipment section
        currentY += DrawEquipmentBox(spriteBatch, c, x, currentY, width);

        return currentY - y;
    }

    private int DrawRightColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int currentY = y;
        
        // Skills section
        currentY += DrawSkillsBox(spriteBatch, c, x, currentY, width);
        currentY += 10;
        
        // Personality Traits (dynamic height)
        // Note: content is currently empty in Draw call, but I should pass actual character traits if they existed
        currentY += DrawTextBox(spriteBatch, "PERSONALITY TRAITS", "", x, currentY, width);
        currentY += 10;
        
        // Ideals
        currentY += DrawTextBox(spriteBatch, "IDEALS", "", x, currentY, width);
        currentY += 10;
        
        // Bonds
        currentY += DrawTextBox(spriteBatch, "BONDS", "", x, currentY, width);
        currentY += 10;
        
        // Flaws
        currentY += DrawTextBox(spriteBatch, "FLAWS", "", x, currentY, width);

        return currentY - y;
    }
    
    private int DrawAttacksBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int height = 250;
        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            
            spriteBatch.DrawString(_font, "ATTACKS & SPELLCASTING", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            // Table headers
            int headerY = y + 30;
            int nameCol = x + 10;
            int atkBonusCol = x + width / 2;
            int damageCol = x + width / 2 + 80;

            spriteBatch.DrawString(_font, "NAME", new Vector2(nameCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "ATK BONUS", new Vector2(atkBonusCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "DAMAGE/TYPE", new Vector2(damageCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

            // Draw separator line
            var lineRect = new Rectangle(x + 5, headerY + 15, width - 10, 1);
            spriteBatch.Draw(_pixel, lineRect, Color.Black * 0.3f);

            // Draw weapon entries
            int entryY = headerY + 20;
            int lineHeight = 20;

            if (c.InventoryData.EquippedWeapon != null)
            {
                string weapon = c.InventoryData.EquippedWeapon;
                int atkBonus = c.GetAbilityModifier(c.Strength) + c.ProficiencyBonus;
                string damage = GetWeaponDamage(weapon);
                var weaponRect = new Rectangle(nameCol - 4, entryY - 2, width - 20, lineHeight);

                spriteBatch.DrawString(_font, weapon, new Vector2(nameCol, entryY), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, FormatModifier(atkBonus), new Vector2(atkBonusCol, entryY), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, damage, new Vector2(damageCol, entryY), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                RegisterTooltip(weaponRect, BuildWeaponTooltip(weapon, atkBonus, damage));
                entryY += lineHeight;
            }
            else
            {
                RegisterTooltip(new Rectangle(x + 6, headerY + 18, width - 12, lineHeight), "Aucune arme équipée. Équipez une arme pour afficher son bonus d'attaque et ses dégâts.");
            }

            // Draw empty lines for additional attacks
            for (int i = 0; i < 8; i++)
            {
                var entryLineRect = new Rectangle(x + 5, entryY + lineHeight - 2, width - 10, 1);
                spriteBatch.Draw(_pixel, entryLineRect, Color.Black * 0.15f);
                entryY += lineHeight;
            }
        }
        return height;
    }
    
    private int DrawEquipmentBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int lineHeight = 16;
        int maxItemsPerCol = 15;
        
        int itemCount = c.InventoryData.Items.Count + (c.InventoryData.EquippedArmor != null ? 1 : 0) + (c.InventoryData.EquippedShield != null ? 1 : 0);
        int itemsInLeftCol = System.Math.Min(itemCount, maxItemsPerCol);
        int itemsInRightCol = System.Math.Max(0, itemCount - maxItemsPerCol);
        
        int itemYHeight = 30 + (itemsInLeftCol * (lineHeight - 2));
        int rightItemYHeight = 30 + (itemsInRightCol * (lineHeight - 2));

        int height = System.Math.Max(itemYHeight, rightItemYHeight) + 45;

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);

            spriteBatch.DrawString(_font, "EQUIPMENT", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            int col1 = x + 10;
            int col2 = x + width / 2 + 5;
            int itemY = y + 30;
            int leftColCount = 0;
            int rightItemY = y + 30;

            // Equipped items
            if (c.InventoryData.EquippedArmor != null)
            {
                string armorName = c.InventoryData.EquippedArmor;
                spriteBatch.DrawString(_font, $"• {armorName}", new Vector2(col1, itemY), Color.Black, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
                RegisterTooltip(new Rectangle(col1 - 2, itemY - 2, width / 2 - 12, lineHeight), BuildItemTooltip(armorName, true));
                itemY += lineHeight;
                leftColCount++;
            }
            
            if (c.InventoryData.EquippedShield != null)
            {
                string shieldName = c.InventoryData.EquippedShield;
                spriteBatch.DrawString(_font, $"• {shieldName}", new Vector2(col1, itemY), Color.Black, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
                RegisterTooltip(new Rectangle(col1 - 2, itemY - 2, width / 2 - 12, lineHeight), BuildItemTooltip(shieldName, true));
                itemY += lineHeight;
                leftColCount++;
            }

            foreach (var item in c.InventoryData.Items)
            {
                if (item == c.InventoryData.EquippedWeapon ||
                    item == c.InventoryData.EquippedArmor ||
                    item == c.InventoryData.EquippedShield)
                    continue;

                if (leftColCount < maxItemsPerCol)
                {
                    spriteBatch.DrawString(_font, $"• {item}", new Vector2(col1, itemY), Color.Black * 0.8f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(col1 - 2, itemY - 2, width / 2 - 12, lineHeight), BuildItemTooltip(item));
                    itemY += lineHeight - 2;
                    leftColCount++;
                }
                else
                {
                    spriteBatch.DrawString(_font, $"• {item}", new Vector2(col2, rightItemY), Color.Black * 0.8f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(col2 - 2, rightItemY - 2, width / 2 - 12, lineHeight), BuildItemTooltip(item));
                    rightItemY += lineHeight - 2;
                }
            }

            // Weight info
            int totalWeight = c.InventoryData.GetTotalWeight();
            spriteBatch.DrawString(_font, $"Total Weight: {totalWeight} lbs", new Vector2(x + 10, y + height - 35), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 8, y + height - 37, width - 16, 14), $"Poids transporté: {totalWeight} lbs. Capacité d'inventaire: {c.InventoryData.Capacity} objets.");

            // Gold pieces
            spriteBatch.DrawString(_font, $"Gold: {c.GoldPieces} gp", new Vector2(x + 10, y + height - 20), Color.DarkGoldenrod, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 8, y + height - 22, width - 16, 14), $"Richesse disponible: {c.GoldPieces} pièces d'or.");
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

        if (!string.IsNullOrWhiteSpace(tooltipText))
        {
            RegisterTooltip(rect, tooltipText);
        }
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

        if (!string.IsNullOrWhiteSpace(tooltipText))
        {
            RegisterTooltip(rect, tooltipText);
        }
    }

    private void DrawHPBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        
        spriteBatch.DrawString(_font, "Hit Point Maximum", new Vector2(x + 5, y + 5), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, c.MaxHP.ToString(), new Vector2(x + width - 40, y + 5), Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        
        var hpRect = new Rectangle(x + 5, y + 25, width - 10, 30);
        spriteBatch.Draw(_pixel, hpRect, new Color(220, 220, 220));
        DrawBorder(spriteBatch, hpRect, Color.Black, 1);
        
        string currentHpText = c.CurrentHP.ToString();
        var hpSize = _font.MeasureString(currentHpText);
        spriteBatch.DrawString(_font, currentHpText, new Vector2(x + (width - hpSize.X * 1.2f) / 2, y + 28), Color.Black, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        
        spriteBatch.DrawString(_font, "CURRENT HIT POINTS", new Vector2(x + 10, y + 58), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        
        if (c.TempHP > 0)
        {
            spriteBatch.DrawString(_font, $"Temporary HP: {c.TempHP}", new Vector2(x + width - 100, y + 58), Color.Blue * 0.8f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }

        RegisterTooltip(rect, $"Points de vie: {c.CurrentHP}/{c.MaxHP}{(c.TempHP > 0 ? $", avec {c.TempHP} PV temporaires" : string.Empty)}.");
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
        
        spriteBatch.DrawString(_font, "HIT DICE", new Vector2(x + 10, y + height - 15), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        RegisterTooltip(rect, $"Dés de vie restants: {c.HitDiceRemaining}/{c.HitDiceTotal} (d{c.HitDiceType}).");
    }

    private void DrawDeathSavesBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        
        spriteBatch.DrawString(_font, "DEATH SAVES", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        
        int circleSize = 16;
        int startY = y + 30;
        spriteBatch.DrawString(_font, "Successes", new Vector2(x + 10, startY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        for (int i = 0; i < 3; i++)
        {
            int cx = x + width - 70 + i * 22;
            DrawCircle(spriteBatch, cx, startY, circleSize, Color.Black, 1);
            if (i < c.DeathSaveSuccesses)
                spriteBatch.Draw(_pixel, new Rectangle(cx + 4, startY + 4, circleSize - 8, circleSize - 8), Color.Black);
        }

        RegisterTooltip(new Rectangle(x + 8, y + 28, width - 16, 20), $"Succès aux jets de mort: {c.DeathSaveSuccesses}/3.");
        
        startY += 25;
        spriteBatch.DrawString(_font, "Failures", new Vector2(x + 10, startY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        for (int i = 0; i < 3; i++)
        {
            int cx = x + width - 70 + i * 22;
            DrawCircle(spriteBatch, cx, startY, circleSize, Color.Black, 1);
            if (i < c.DeathSaveFailures)
                spriteBatch.Draw(_pixel, new Rectangle(cx + 4, startY + 4, circleSize - 8, circleSize - 8), Color.Black);
        }

        RegisterTooltip(new Rectangle(x + 8, y + 53, width - 16, 20), $"Échecs aux jets de mort: {c.DeathSaveFailures}/3.");
    }

    private int DrawSkillsBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int skillY = y + 25;
        int lineHeight = 18;
        
        var skills = new[]
        {
            ("Acrobatics", "DEX"),
            ("Animal Handling", "WIS"),
            ("Arcana", "INT"),
            ("Athletics", "STR"),
            ("Deception", "CHA"),
            ("History", "INT"),
            ("Insight", "WIS"),
            ("Intimidation", "CHA"),
            ("Investigation", "INT"),
            ("Medicine", "WIS"),
            ("Nature", "INT"),
            ("Perception", "WIS"),
            ("Performance", "CHA"),
            ("Persuasion", "CHA"),
            ("Religion", "INT"),
            ("Sleight of Hand", "DEX"),
            ("Stealth", "DEX"),
            ("Survival", "WIS")
        };
        
        foreach (var (skillName, ability) in skills)
        {
            if (spriteBatch != null)
            {
                int bonus = c.GetSkillBonus(skillName, out _);
                bool proficient = GetSkillProficiency(c, skillName);

                DrawCheckbox(spriteBatch, x + 8, skillY, 10, proficient);

                string bonusText = FormatModifier(bonus);
                spriteBatch.DrawString(_font, bonusText, new Vector2(x + 25, skillY - 2), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

                spriteBatch.DrawString(_font, skillName, new Vector2(x + 60, skillY - 2), proficient ? Color.Black : Color.Black * 0.6f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

                spriteBatch.DrawString(_font, $"({ability})", new Vector2(x + width - 45, skillY - 2), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

                var skillRect = new Rectangle(x + 4, skillY - 2, width - 8, lineHeight);
                RegisterTooltip(skillRect, $"{skillName} ({ability}) : bonus {bonusText}. {(proficient ? "Vous êtes compétent." : "Pas de maîtrise." )}");
            }
            
            skillY += lineHeight;
        }

        int height = skillY - y + 5;
        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, "SKILLS", new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        return height;
    }

    private void RegisterTooltip(Rectangle area, string text)
    {
        if (_hoverTooltip == null && area.Contains(_mousePosition))
        {
            _hoverTooltip = text;
        }
    }

    private void DrawTooltip(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (string.IsNullOrWhiteSpace(_hoverTooltip))
        {
            return;
        }

        const int padding = 8;
        var textSize = _font.MeasureString(_hoverTooltip);
        int width = (int)textSize.X + padding * 2;
        int height = (int)textSize.Y + padding * 2;

        int x = _mousePosition.X + 16;
        int y = _mousePosition.Y + 18;

        if (x + width > viewport.Width - 4)
        {
            x = viewport.Width - width - 4;
        }

        if (y + height > viewport.Height - 4)
        {
            y = _mousePosition.Y - height - 8;
        }

        x = System.Math.Max(4, x);
        y = System.Math.Max(4, y);

        var tooltipRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, tooltipRect, new Color(30, 30, 30, 240));
        DrawBorder(spriteBatch, tooltipRect, new Color(220, 220, 220), 1);
        spriteBatch.DrawString(_font, _hoverTooltip, new Vector2(x + padding, y + padding), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
    }

    private void DrawSmallBox(SpriteBatch spriteBatch, string label, string value, int x, int y, int width, int height, string? tooltipText = null)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        
        if (!string.IsNullOrEmpty(value))
        {
            var valueSize = _font.MeasureString(value);
            spriteBatch.DrawString(_font, value, new Vector2(x + (width - valueSize.X) / 2, y + 8), Color.Black);
        }
        
        var labelSize = _font.MeasureString(label);
        spriteBatch.DrawString(_font, label, new Vector2(x + (width - labelSize.X * 0.45f) / 2, y + height - 12), Color.Black, 0f, Vector2.Zero, 0.45f, SpriteEffects.None, 0f);

        if (!string.IsNullOrWhiteSpace(tooltipText))
        {
            RegisterTooltip(rect, tooltipText);
        }
    }

    private int DrawTextBox(SpriteBatch? spriteBatch, string label, string content, int x, int y, int width)
    {
        string wrappedContent = WrapText(_font, content, width - 20, 0.6f);
        var contentSize = _font.MeasureString(wrappedContent) * 0.6f;
        int height = (int)System.Math.Max(60, 35 + contentSize.Y);

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);

            spriteBatch.DrawString(_font, label, new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            if (!string.IsNullOrEmpty(wrappedContent))
            {
                spriteBatch.DrawString(_font, wrappedContent, new Vector2(x + 10, y + 25), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }

            RegisterTooltip(rect, $"Section de roleplay: {label.ToLowerInvariant()}.");
        }

        return height;
    }

    private void DrawCheckbox(SpriteBatch spriteBatch, int x, int y, int size, bool checked_)
    {
        var box = new Rectangle(x, y, size, size);
        spriteBatch.Draw(_pixel, box, Color.White);
        DrawBorder(spriteBatch, box, Color.Black, 1);
        
        if (checked_)
        {
            spriteBatch.Draw(_pixel, new Rectangle(x + 2, y + 2, size - 4, size - 4), Color.Black);
        }
    }

    private void DrawCircle(SpriteBatch spriteBatch, int x, int y, int size, Color color, int thickness)
    {
        int radius = size / 2;
        int cx = x + radius;
        int cy = y + radius;
        
        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int distX = dx - radius;
                int distY = dy - radius;
                float dist = (float)System.Math.Sqrt(distX * distX + distY * distY);
                
                if (dist >= radius - thickness && dist <= radius)
                {
                    spriteBatch.Draw(_pixel, new Rectangle(x + dx, y + dy, 1, 1), color);
                }
            }
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private string FormatModifier(int value)
    {
        return value >= 0 ? $"+{value}" : $"{value}";
    }
    
    private string GetWeaponDamage(string weapon)
    {
        var item = ItemDatabase.GetItem(weapon);
        if (item != null && !string.IsNullOrEmpty(item.DamageDice))
        {
            return $"{item.DamageDice} {item.DamageType.ToLower()}";
        }
        return "1d6";
    }

    private string BuildWeaponTooltip(string weaponName, int attackBonus, string damage)
    {
        var item = ItemDatabase.GetItem(weaponName);
        string rangeInfo = item.IsRanged ? $"Portée: {item.Range}/{item.Range * 3} ft." : "Attaque de mêlée.";
        string properties = GetWeaponProperties(item);

        return $"{weaponName} (équipée)\n" +
               $"Bonus d'attaque: {FormatModifier(attackBonus)} (carac + maîtrise).\n" +
               $"Dégâts: {damage}.\n" +
               $"{rangeInfo}\n" +
               $"{properties}";
    }

    private string BuildItemTooltip(string itemName, bool isEquipped = false)
    {
        var item = ItemDatabase.GetItem(itemName);
        string equippedText = isEquipped ? " (équipé)" : "";
        string details = $"{item.Name}{equippedText}\nType: {GetItemTypeLabel(item.Type)}\nPoids: {item.Weight} lbs | Valeur: {item.Value} gp";

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            details += $"\n{item.Description}";
        }

        if (item.Type == ItemType.Weapon)
        {
            details += $"\nDégâts: {item.DamageDice} {item.DamageType.ToLower()}";
            details += $"\n{GetWeaponProperties(item)}";
        }
        else if (item.Type == ItemType.Armor)
        {
            string dexCap = item.MaxDexBonus >= 10 ? "sans limite" : $"max {FormatModifier(item.MaxDexBonus)}";
            details += $"\nCA: {item.ArmorClass} | Bonus DEX: {dexCap}";
            if (item.StealthDisadvantage)
            {
                details += "\nDésavantage en Discrétion.";
            }
        }
        else if (item.Type == ItemType.Shield)
        {
            details += $"\nBonus de CA: +{item.ArmorClass}";
        }

        return details;
    }

    private string GetWeaponProperties(Item item)
    {
        List<string> properties = new();
        if (item.IsFinesse) properties.Add("Finesse");
        if (item.IsVersatile && !string.IsNullOrWhiteSpace(item.VersatileDamageDice)) properties.Add($"Polyvalente ({item.VersatileDamageDice})");
        if (item.IsRanged) properties.Add("Distance");

        return properties.Count > 0
            ? $"Propriétés: {string.Join(", ", properties)}."
            : "Propriétés: aucune.";
    }

    private string GetItemTypeLabel(ItemType type)
    {
        return type switch
        {
            ItemType.Weapon => "Arme",
            ItemType.Armor => "Armure",
            ItemType.Shield => "Bouclier",
            ItemType.Consumable => "Consommable",
            ItemType.Treasure => "Trésor",
            ItemType.Misc => "Divers",
            _ => "Inconnu"
        };
    }

    private string WrapText(SpriteFont font, string text, float maxLineWidth, float scale = 0.7f)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(' ');
        string result = "";
        string currentLine = "";

        foreach (string word in words)
        {
            if (font.MeasureString(currentLine + word).X * scale < maxLineWidth)
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

    private bool GetSkillProficiency(Character c, string skill)
    {
        return skill switch
        {
            "Acrobatics" => c.AcrobaticsProficiency,
            "Animal Handling" => c.AnimalHandlingProficiency,
            "Arcana" => c.ArcanaProficiency,
            "Athletics" => c.AthleticsProficiency,
            "Deception" => c.DeceptionProficiency,
            "History" => c.HistoryProficiency,
            "Insight" => c.InsightProficiency,
            "Intimidation" => c.IntimidationProficiency,
            "Investigation" => c.InvestigationProficiency,
            "Medicine" => c.MedicineProficiency,
            "Nature" => c.NatureProficiency,
            "Perception" => c.PerceptionProficiency,
            "Performance" => c.PerformanceProficiency,
            "Persuasion" => c.PersuasionProficiency,
            "Religion" => c.ReligionProficiency,
            "Sleight of Hand" => c.SleightOfHandProficiency,
            "Stealth" => c.StealthProficiency,
            "Survival" => c.SurvivalProficiency,
            _ => false
        };
    }
}
