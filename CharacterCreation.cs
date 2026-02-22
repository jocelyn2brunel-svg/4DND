using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace _4DND;

public class CharacterCreation
{
    private string _createName = string.Empty;
    private int _createStep = 0; // 0: name, 1: race, 2: class, 3: abilities, 4: final review
    private List<string> _races = Race.GetAllRaceNames();
    private readonly string[] _classes = new[] { "Barbarian", "Bard", "Cleric", "Druid", "Fighter", "Monk", "Paladin", "Ranger", "Rogue", "Sorcerer", "Warlock", "Wizard" };
    private int _raceIndex = 0;
    private int _classIndex = 0;
    private int[] _rolledAbilities = new int[6];
    private readonly string[] _abilityNames = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

    private SpriteFont _font;
    private Texture2D _pixel;
    
    // Scrolling for race list
    private int _raceScrollOffset = 0;
    private const int MAX_VISIBLE_RACES = 8;
    
    // Scrolling for class list
    private int _classScrollOffset = 0;
    private const int MAX_VISIBLE_CLASSES = 8;
    
    // Mouse state tracking
    private MouseState _prevMouse;
    
    // Animation for dice rolling
    private bool _isRolling = false;
    private double _rollTimer = 0;
    private const double ROLL_DURATION = 1.0;
    
    // Hover tooltip
    private string _tooltipText = "";
    private Vector2 _tooltipPosition;
    private static readonly string[] _namePrefixes =
    {
        "Ara", "Bel", "Cor", "Dra", "Ela", "Fen", "Gal", "Ira", "Kae", "Lun",
        "Myr", "Nym", "Or", "Phae", "Quin", "Riv", "Syl", "Ther", "Val", "Zan"
    };
    private static readonly string[] _nameSuffixes =
    {
        "dor", "wyn", "ric", "thas", "lia", "rin", "mond", "dris", "vash", "riel",
        "thorn", "dell", "gorn", "mere", "nox", "dain", "stra", "len", "mir", "vek"
    };
    private readonly Random _random = new();

    public CharacterCreation(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Reset()
    {
        _createName = string.Empty;
        _createStep = 0;
        _raceIndex = 0;
        _classIndex = 0;
        _rolledAbilities = new int[6];
        _raceScrollOffset = 0;
        _classScrollOffset = 0;
        _isRolling = false;
        _rollTimer = 0;
    }

    public bool Update(GameTime gameTime, GraphicsDevice graphics, KeyboardState kb, KeyboardState prevKb, out Character createdCharacter)
    {
        createdCharacter = null;
        var mouse = Mouse.GetState();
        var vp = graphics.Viewport;

        // Larger window for better visibility
        int menuWidthC = 1000;
        int menuHeightC = 600;
        int paddingC = 20;
        int titleH = 80;
        var menuRectC = new Rectangle((vp.Width - menuWidthC) / 2, (vp.Height - menuHeightC) / 2, menuWidthC, menuHeightC);

        _tooltipText = "";
        
        // Update rolling animation
        if (_isRolling)
        {
            _rollTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_rollTimer >= ROLL_DURATION)
            {
                _isRolling = false;
                _rollTimer = 0;
            }
        }

        // Back/Cancel button
        var backRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + paddingC + 35, 100, 35);
        if (backRect.Contains(mouse.Position))
            _tooltipText = "Cancel character creation";
        if (IsMouseClicked(mouse, _prevMouse, backRect))
        {
            _prevMouse = mouse;
            return false;
        }

        // Escape to cancel (kept as shortcut)
        if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
        {
            _prevMouse = mouse;
            return false;
        }

        // Navigation buttons
        var nextRect = new Rectangle(menuRectC.X + menuRectC.Width - paddingC - 120, menuRectC.Y + menuRectC.Height - paddingC - 45, 120, 40);
        var prevRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + menuRectC.Height - paddingC - 45, 120, 40);
        
        bool canGoNext = CanProceedToNextStep();
        bool canGoPrev = _createStep > 0;

        if (canGoPrev && prevRect.Contains(mouse.Position))
            _tooltipText = "Go back to previous step";
        if (canGoNext && nextRect.Contains(mouse.Position))
            _tooltipText = GetNextStepTooltip();

        // Previous button
        if (canGoPrev && IsMouseClicked(mouse, _prevMouse, prevRect))
        {
            _createStep = Math.Max(0, _createStep - 1);
        }

        // Next button
        if (canGoNext && IsMouseClicked(mouse, _prevMouse, nextRect))
        {
            if (_createStep == 2) // Before going to abilities, roll them
            {
                RollAbilities();
                _createStep = 3; // Advance to abilities step
            }
            else if (_createStep == 3) // After abilities, go to final review
            {
                _createStep = 4;
                _prevMouse = mouse;
                return true;
            }
            else if (_createStep == 4) // Final confirmation
            {
                createdCharacter = CreateCharacterFromData();
                _prevMouse = mouse;
                return true;
            }
            else
            {
                _createStep++;
            }
        }

        // Step 0: Name input
        if (_createStep == 0)
        {
            var nameRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH + 40, menuWidthC - paddingC * 2, 50);
            var randomNameRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH + 100, 240, 40);
            
            // Click to focus
            if (IsMouseClicked(mouse, _prevMouse, nameRect))
            {
                // Already focused on this step
            }

            if (randomNameRect.Contains(mouse.Position))
                _tooltipText = "Generer un nom aleatoire";

            if (IsMouseClicked(mouse, _prevMouse, randomNameRect))
            {
                _createName = GenerateRandomName();
            }
            
            foreach (var k in kb.GetPressedKeys())
            {
                if (!prevKb.IsKeyDown(k))
                {
                    if (k == Keys.Back && _createName.Length > 0)
                        _createName = _createName[..^1];
                    else if (k == Keys.Space)
                        _createName += ' ';
                    else if (k >= Keys.A && k <= Keys.Z)
                    {
                        bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
                        if (OperatingSystem.IsWindows())
                        {
                            shift = shift || Console.CapsLock;
                        }
                        char c = (char)('a' + (k - Keys.A));
                        _createName += shift ? char.ToUpper(c) : c;
                    }
                    else if (k >= Keys.D0 && k <= Keys.D9)
                    {
                        char c = (char)('0' + (k - Keys.D0));
                        _createName += c;
                    }
                    else if (k == Keys.OemMinus)
                        _createName += '-';
                    else if (k == Keys.OemPeriod)
                        _createName += '.';
                }
            }
        }
        // Step 1: Race selection
        else if (_createStep == 1)
        {
            var racesRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH + 40, 460, MAX_VISIBLE_RACES * 42 + 10);
            int visibleCount = Math.Min(MAX_VISIBLE_RACES, _races.Count - _raceScrollOffset);
            
            // Mouse wheel scrolling
            var scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (racesRect.Contains(mouse.Position) && scrollDelta != 0)
            {
                if (scrollDelta > 0 && _raceScrollOffset > 0)
                    _raceScrollOffset--;
                else if (scrollDelta < 0 && _raceScrollOffset < _races.Count - MAX_VISIBLE_RACES)
                    _raceScrollOffset++;
            }
            
            for (int i = 0; i < visibleCount; i++)
            {
                int raceIdx = _raceScrollOffset + i;
                var item = new Rectangle(racesRect.X + 4, racesRect.Y + i * 42 + 4, 452, 38);
                
                if (item.Contains(mouse.Position))
                {
                    var race = Race.GetRace(_races[raceIdx]);
                    _tooltipText = $"Speed: {race.BaseSpeed} ft" + (race.DarkvisionRange > 0 ? $" | Darkvision: {race.DarkvisionRange} ft" : "");
                }
                
                if (IsMouseClicked(mouse, _prevMouse, item))
                {
                    _raceIndex = raceIdx;
                }
            }
            
            // Scroll arrows
            if (_raceScrollOffset > 0)
            {
                var upArrow = new Rectangle(racesRect.X + racesRect.Width - 30, racesRect.Y, 25, 25);
                if (IsMouseClicked(mouse, _prevMouse, upArrow))
                    _raceScrollOffset--;
            }
            if (_raceScrollOffset < _races.Count - MAX_VISIBLE_RACES)
            {
                var downArrow = new Rectangle(racesRect.X + racesRect.Width - 30, racesRect.Y + racesRect.Height - 25, 25, 25);
                if (IsMouseClicked(mouse, _prevMouse, downArrow))
                    _raceScrollOffset++;
            }
        }
        // Step 2: Class selection
        else if (_createStep == 2)
        {
            var classesRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH + 40, 460, MAX_VISIBLE_CLASSES * 42 + 10);
            int visibleClassCount = Math.Min(MAX_VISIBLE_CLASSES, _classes.Length - _classScrollOffset);
            
            // Mouse wheel scrolling
            var scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (classesRect.Contains(mouse.Position) && scrollDelta != 0)
            {
                if (scrollDelta > 0 && _classScrollOffset > 0)
                    _classScrollOffset--;
                else if (scrollDelta < 0 && _classScrollOffset < _classes.Length - MAX_VISIBLE_CLASSES)
                    _classScrollOffset++;
            }
            
            for (int i = 0; i < visibleClassCount; i++)
            {
                int classIdx = _classScrollOffset + i;
                var item = new Rectangle(classesRect.X + 4, classesRect.Y + i * 42 + 4, 452, 38);
                
                if (item.Contains(mouse.Position))
                {
                    var classData = ClassData.GetClass(_classes[classIdx]);
                    _tooltipText = $"Hit Die: d{classData.HitDice} | Primary: {classData.PrimaryAbility}";
                }
                
                if (IsMouseClicked(mouse, _prevMouse, item))
                {
                    _classIndex = classIdx;
                }
            }
            
            // Scroll arrows
            if (_classScrollOffset > 0)
            {
                var upArrow = new Rectangle(classesRect.X + classesRect.Width - 30, classesRect.Y, 25, 25);
                if (IsMouseClicked(mouse, _prevMouse, upArrow))
                    _classScrollOffset--;
            }
            if (_classScrollOffset < _classes.Length - MAX_VISIBLE_CLASSES)
            {
                var downArrow = new Rectangle(classesRect.X + classesRect.Width - 30, classesRect.Y + classesRect.Height - 25, 25, 25);
                if (IsMouseClicked(mouse, _prevMouse, downArrow))
                    _classScrollOffset++;
            }
        }
        // Step 3: Ability rolls
        else if (_createStep == 3)
        {
            // Reroll button - position matches DrawAbilitiesStep
            var rerollRect = new Rectangle(menuRectC.X + menuWidthC / 2 - 80, menuRectC.Y + titleH + 250, 160, 40);
            if (rerollRect.Contains(mouse.Position))
                _tooltipText = "Roll new ability scores (4d6 drop lowest)";
                
            if (!_isRolling && IsMouseClicked(mouse, _prevMouse, rerollRect))
            {
                RollAbilities();
                _isRolling = true;
                _rollTimer = 0;
            }
        }

        _prevMouse = mouse;
        return true;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics)
    {
        var vp = graphics.Viewport;
        
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

        int menuWidthC = 1000;
        int menuHeightC = 600;
        int paddingC = 20;
        int titleH = 80;
        var menuRectC = new Rectangle((vp.Width - menuWidthC) / 2, (vp.Height - menuHeightC) / 2, menuWidthC, menuHeightC);
        
        // Menu background with border
        spriteBatch.Draw(_pixel, menuRectC, Color.DarkSlateGray * 0.95f);
        DrawBorder(spriteBatch, menuRectC, 2, Color.Gold * 0.6f);

        if (_font != null)
        {
            var mouse = Mouse.GetState();
            
            // Title with step indicator
            var title = $"Create Character - Step {_createStep + 1}/5";
            var tsize = _font.MeasureString(title);
            spriteBatch.DrawString(_font, title, new Vector2(menuRectC.X + (menuWidthC - tsize.X) / 2, menuRectC.Y + 12), Color.Gold);
            
            // Progress bar
            DrawProgressBar(spriteBatch, menuRectC, paddingC);

            // Back/Cancel button
            var backRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + paddingC + 35, 100, 35);
            var backColor = backRect.Contains(mouse.Position) ? Color.DarkRed : Color.Gray;
            spriteBatch.Draw(_pixel, backRect, backColor);
            DrawBorder(spriteBatch, backRect, 1, Color.White * 0.5f);
            var backText = "? Cancel";
            var backTextSize = _font.MeasureString(backText);
            spriteBatch.DrawString(_font, backText, new Vector2(backRect.X + (backRect.Width - backTextSize.X * 0.6f) / 2, backRect.Y + (backRect.Height - backTextSize.Y * 0.6f) / 2), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            // Draw current step
            switch (_createStep)
            {
                case 0:
                    DrawNameStep(spriteBatch, gameTime, menuRectC, paddingC, titleH, mouse);
                    break;
                case 1:
                    DrawRaceStep(spriteBatch, menuRectC, paddingC, titleH, mouse);
                    break;
                case 2:
                    DrawClassStep(spriteBatch, menuRectC, paddingC, titleH, mouse);
                    break;
                case 3:
                    DrawAbilitiesStep(spriteBatch, gameTime, menuRectC, paddingC, titleH, mouse);
                    break;
                case 4:
                    DrawFinalReviewStep(spriteBatch, menuRectC, paddingC, titleH, mouse);
                    break;
            }

            // Navigation buttons
            DrawNavigationButtons(spriteBatch, menuRectC, paddingC, mouse);
            
            // Tooltip
            if (!string.IsNullOrEmpty(_tooltipText))
            {
                DrawTooltip(spriteBatch, _tooltipText, mouse.Position);
            }
        }
    }

    private void DrawProgressBar(SpriteBatch spriteBatch, Rectangle menuRect, int padding)
    {
        int barWidth = menuRect.Width - padding * 2 - 140;
        int barX = menuRect.X + padding + 120;
        int barY = menuRect.Y + 50;
        int barHeight = 8;
        
        // Background bar
        var barRect = new Rectangle(barX, barY, barWidth, barHeight);
        spriteBatch.Draw(_pixel, barRect, Color.DarkGray * 0.5f);
        
        // Progress fill
        float progress = (_createStep + 1) / 5f;
        var fillRect = new Rectangle(barX, barY, (int)(barWidth * progress), barHeight);
        spriteBatch.Draw(_pixel, fillRect, Color.LightGreen);
        
        // Step labels
        string[] stepLabels = { "Name", "Race", "Class", "Abilities", "Review" };
        for (int i = 0; i < 5; i++)
        {
            int stepX = barX + (barWidth * i / 4);
            var stepColor = i <= _createStep ? Color.Gold : Color.Gray;
            var stepLabel = stepLabels[i];
            var labelSize = _font.MeasureString(stepLabel);
            spriteBatch.DrawString(_font, stepLabel, new Vector2(stepX - labelSize.X * 0.4f / 2, barY - 18), stepColor, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 0f);
            
            // Step circle
            var circle = new Rectangle(stepX - 6, barY - 2, 12, 12);
            spriteBatch.Draw(_pixel, circle, i <= _createStep ? Color.LightGreen : Color.DarkGray);
        }
    }

    private void DrawNameStep(SpriteBatch spriteBatch, GameTime gameTime, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var instructionText = "Enter your character's name:";
        spriteBatch.DrawString(_font, instructionText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        var nameRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 40, menuRect.Width - padding * 2, 50);
        var nameColor = nameRect.Contains(mouse.Position) ? Color.White : Color.LightGray;
        spriteBatch.Draw(_pixel, nameRect, nameColor);
        DrawBorder(spriteBatch, nameRect, 2, Color.Gold * 0.4f);

        var randomNameRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 100, 240, 40);
        var randomNameColor = randomNameRect.Contains(mouse.Position) ? Color.MediumPurple : Color.DarkSlateBlue;
        spriteBatch.Draw(_pixel, randomNameRect, randomNameColor);
        DrawBorder(spriteBatch, randomNameRect, 1, Color.White * 0.7f);

        var randomNameText = "Nom aleatoire";
        var randomNameTextSize = _font.MeasureString(randomNameText);
        spriteBatch.DrawString(_font, randomNameText, new Vector2(randomNameRect.X + (randomNameRect.Width - randomNameTextSize.X * 0.65f) / 2, randomNameRect.Y + (randomNameRect.Height - randomNameTextSize.Y * 0.65f) / 2), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        
        var displayName = string.IsNullOrEmpty(_createName) ? "Type your character's name..." : _createName + (((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2) == 0 ? "|" : "");
        spriteBatch.DrawString(_font, displayName, new Vector2(nameRect.X + 12, nameRect.Y + 12), string.IsNullOrEmpty(_createName) ? Color.Gray : Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        
        // Tips
        var tipText = "?? Tips: Choose a name that fits your character's background and personality.";
        spriteBatch.DrawString(_font, tipText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 150), Color.LightBlue, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }

    private void DrawRaceStep(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var instructionText = "Choose your character's race:";
        spriteBatch.DrawString(_font, instructionText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        // Races list (left side)
        var racesRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 40, 460, MAX_VISIBLE_RACES * 42 + 10);
        spriteBatch.Draw(_pixel, racesRect, Color.Gray * 0.7f);
        DrawBorder(spriteBatch, racesRect, 2, Color.Gold * 0.4f);
        
        int visibleCount = Math.Min(MAX_VISIBLE_RACES, _races.Count - _raceScrollOffset);
        for (int i = 0; i < visibleCount; i++)
        {
            int raceIdx = _raceScrollOffset + i;
            var item = new Rectangle(racesRect.X + 4, racesRect.Y + i * 42 + 4, 452, 38);
            
            bool isHovered = item.Contains(mouse.Position);
            bool isSelected = raceIdx == _raceIndex;
            
            Color itemColor = isSelected ? Color.Gold * 0.8f : (isHovered ? Color.LightGray * 0.8f : Color.DarkGray);
            spriteBatch.Draw(_pixel, item, itemColor);
            
            if (isSelected)
                DrawBorder(spriteBatch, item, 2, Color.Gold);
            
            var race = Race.GetRace(_races[raceIdx]);
            spriteBatch.DrawString(_font, race.DisplayName, new Vector2(item.X + 10, item.Y + 6), isSelected ? Color.Black : Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }
        
        // Scroll indicators
        DrawScrollArrows(spriteBatch, racesRect, _raceScrollOffset, _races.Count, MAX_VISIBLE_RACES, mouse);
        
        // Race details panel (right side)
        DrawRaceDetails(spriteBatch, menuRect, padding, titleH, mouse);
    }

    private void DrawRaceDetails(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        if (_raceIndex < 0 || _raceIndex >= _races.Count) return;
        
        var selectedRace = Race.GetRace(_races[_raceIndex]);
        var detailsRect = new Rectangle(menuRect.X + padding + 480, menuRect.Y + titleH + 40, 480, 420);
        spriteBatch.Draw(_pixel, detailsRect, Color.DarkSlateGray * 0.8f);
        DrawBorder(spriteBatch, detailsRect, 2, Color.Gold * 0.4f);
        
        int yOffset = detailsRect.Y + 12;
        
        // Race name
        spriteBatch.DrawString(_font, selectedRace.DisplayName, new Vector2(detailsRect.X + 12, yOffset), Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        yOffset += 35;
        
        // Description
        DrawWrappedText(spriteBatch, selectedRace.Description, new Vector2(detailsRect.X + 12, yOffset), detailsRect.Width - 24, Color.White, 0.55f);
        yOffset += 60;
        
        // Ability bonuses
        spriteBatch.DrawString(_font, "Ability Score Increases:", new Vector2(detailsRect.X + 12, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 25;
        
        var bonuses = new List<string>();
        if (selectedRace.StrengthBonus != 0) bonuses.Add($"STR {FormatBonus(selectedRace.StrengthBonus)}");
        if (selectedRace.DexterityBonus != 0) bonuses.Add($"DEX {FormatBonus(selectedRace.DexterityBonus)}");
        if (selectedRace.ConstitutionBonus != 0) bonuses.Add($"CON {FormatBonus(selectedRace.ConstitutionBonus)}");
        if (selectedRace.IntelligenceBonus != 0) bonuses.Add($"INT {FormatBonus(selectedRace.IntelligenceBonus)}");
        if (selectedRace.WisdomBonus != 0) bonuses.Add($"WIS {FormatBonus(selectedRace.WisdomBonus)}");
        if (selectedRace.CharismaBonus != 0) bonuses.Add($"CHA {FormatBonus(selectedRace.CharismaBonus)}");
        
        foreach (var bonus in bonuses)
        {
            spriteBatch.DrawString(_font, "   " + bonus, new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        yOffset += 10;
        
        // Racial traits
        spriteBatch.DrawString(_font, "Traits:", new Vector2(detailsRect.X + 12, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 25;
        
        spriteBatch.DrawString(_font, $"   Speed: {selectedRace.BaseSpeed} feet", new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        yOffset += 22;
        
        if (selectedRace.DarkvisionRange > 0)
        {
            var darkvisionText = selectedRace.HasSuperiorDarkvision ? $"   Superior Darkvision: {selectedRace.DarkvisionRange} feet" : $"   Darkvision: {selectedRace.DarkvisionRange} feet";
            spriteBatch.DrawString(_font, darkvisionText, new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        if (selectedRace.HasSunlightSensitivity)
        {
            spriteBatch.DrawString(_font, "   Sunlight Sensitivity", new Vector2(detailsRect.X + 12, yOffset), Color.Orange, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
    }

    private void DrawClassStep(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var instructionText = "Choose your character's class:";
        spriteBatch.DrawString(_font, instructionText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        // Classes list (left side)
        var classesRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 40, 460, MAX_VISIBLE_CLASSES * 42 + 10);
        spriteBatch.Draw(_pixel, classesRect, Color.Gray * 0.7f);
        DrawBorder(spriteBatch, classesRect, 2, Color.Gold * 0.4f);
        
        int visibleClassCount = Math.Min(MAX_VISIBLE_CLASSES, _classes.Length - _classScrollOffset);
        for (int i = 0; i < visibleClassCount; i++)
        {
            int classIdx = _classScrollOffset + i;
            var item = new Rectangle(classesRect.X + 4, classesRect.Y + i * 42 + 4, 452, 38);
            
            bool isHovered = item.Contains(mouse.Position);
            bool isSelected = classIdx == _classIndex;
            
            Color itemColor = isSelected ? Color.Gold * 0.8f : (isHovered ? Color.LightGray * 0.8f : Color.DarkGray);
            spriteBatch.Draw(_pixel, item, itemColor);
            
            if (isSelected)
                DrawBorder(spriteBatch, item, 2, Color.Gold);
            
            var classData = ClassData.GetClass(_classes[classIdx]);
            var classLabel = $"{classData.Name} (d{classData.HitDice})";
            spriteBatch.DrawString(_font, classLabel, new Vector2(item.X + 10, item.Y + 6), isSelected ? Color.Black : Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }
        
        // Scroll indicators
        DrawScrollArrows(spriteBatch, classesRect, _classScrollOffset, _classes.Length, MAX_VISIBLE_CLASSES, mouse);
        
        // Class details panel (right side)
        DrawClassDetails(spriteBatch, menuRect, padding, titleH, mouse);
    }

    private void DrawClassDetails(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        if (_classIndex < 0 || _classIndex >= _classes.Length) return;
        
        var selectedClass = ClassData.GetClass(_classes[_classIndex]);
        var detailsRect = new Rectangle(menuRect.X + padding + 480, menuRect.Y + titleH + 40, 480, 420);
        spriteBatch.Draw(_pixel, detailsRect, Color.DarkSlateGray * 0.8f);
        DrawBorder(spriteBatch, detailsRect, 2, Color.Gold * 0.4f);
        
        int yOffset = detailsRect.Y + 12;
        
        // Class name
        spriteBatch.DrawString(_font, selectedClass.Name, new Vector2(detailsRect.X + 12, yOffset), Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        yOffset += 35;
        
        // Description
        DrawWrappedText(spriteBatch, selectedClass.Description, new Vector2(detailsRect.X + 12, yOffset), detailsRect.Width - 24, Color.White, 0.55f);
        yOffset += 60;
        
        // Hit Die
        spriteBatch.DrawString(_font, $"Hit Die: d{selectedClass.HitDice}", new Vector2(detailsRect.X + 12, yOffset), Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 25;
        
        // Primary Ability
        spriteBatch.DrawString(_font, $"Primary Ability: {selectedClass.PrimaryAbility}", new Vector2(detailsRect.X + 12, yOffset), Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 30;
        
        // Saving Throws
        spriteBatch.DrawString(_font, "Saving Throw Proficiencies:", new Vector2(detailsRect.X + 12, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 22;
        foreach (var save in selectedClass.SavingThrowProficiencies)
        {
            spriteBatch.DrawString(_font, $"   {save}", new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 20;
        }
        
        yOffset += 10;
        
        // Armor Proficiencies
        if (selectedClass.ArmorProficiencies.Count > 0)
        {
            spriteBatch.DrawString(_font, "Armor Proficiencies:", new Vector2(detailsRect.X + 12, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 22;
            var armorText = "  " + string.Join(", ", selectedClass.ArmorProficiencies);
            DrawWrappedText(spriteBatch, armorText, new Vector2(detailsRect.X + 12, yOffset), detailsRect.Width - 24, Color.White, 0.5f);
            yOffset += 40;
        }
        
        // Weapon Proficiencies
        if (selectedClass.WeaponProficiencies.Count > 0)
        {
            spriteBatch.DrawString(_font, "Weapon Proficiencies:", new Vector2(detailsRect.X + 12, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 22;
            var weaponText = "  " + string.Join(", ", selectedClass.WeaponProficiencies);
            DrawWrappedText(spriteBatch, weaponText, new Vector2(detailsRect.X + 12, yOffset), detailsRect.Width - 24, Color.White, 0.5f);
        }
    }

    private void DrawAbilitiesStep(SpriteBatch spriteBatch, GameTime gameTime, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var instructionText = "Your ability scores (4d6 drop lowest):";
        spriteBatch.DrawString(_font, instructionText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        var selectedRace = Race.GetRace(_races[_raceIndex]);
        var selectedClass = ClassData.GetClass(_classes[_classIndex]);
        
        // Ability scores in 2 rows
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;
            int x = menuRect.X + padding + col * 310;
            int y = menuRect.Y + titleH + 50 + row * 75;
            
            var abilityBox = new Rectangle(x, y, 300, 65);
            spriteBatch.Draw(_pixel, abilityBox, Color.Gray * 0.7f);
            DrawBorder(spriteBatch, abilityBox, 2, Color.Gold * 0.3f);
            
            // Show rolling animation or final value
            int displayValue = _isRolling ? new Random(DateTime.Now.Millisecond + i).Next(3, 18) : _rolledAbilities[i];
            
            // Get racial bonus for this ability
            int racialBonus = i switch
            {
                0 => selectedRace.StrengthBonus,
                1 => selectedRace.DexterityBonus,
                2 => selectedRace.ConstitutionBonus,
                3 => selectedRace.IntelligenceBonus,
                4 => selectedRace.WisdomBonus,
                5 => selectedRace.CharismaBonus,
                _ => 0
            };
            
            int finalValue = displayValue + racialBonus;
            int modifier = (finalValue - 10) / 2;
            string modText = modifier >= 0 ? $"+{modifier}" : $"{modifier}";
            
            string abilityText = $"{_abilityNames[i]}: {displayValue}";
            if (racialBonus != 0)
                abilityText += $" {FormatBonus(racialBonus)} = {finalValue}";
            
            var textColor = _isRolling ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, abilityText, new Vector2(x + 12, y + 10), textColor, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, $"Modifier: {modText}", new Vector2(x + 12, y + 32), Color.LightGray, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        }
        
        // Race info
        int raceInfoY = menuRect.Y + titleH + 210;
        spriteBatch.DrawString(_font, $"Racial Bonuses from {selectedRace.DisplayName}", new Vector2(menuRect.X + padding, raceInfoY), Color.Gold, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        
        // Reroll button
        var rerollRectRev = new Rectangle(menuRect.X + menuRect.Width / 2 - 80, menuRect.Y + titleH + 250, 160, 40);
        var rerollColorRev = rerollRectRev.Contains(mouse.Position) ? Color.Orange : Color.DarkOrange;
        spriteBatch.Draw(_pixel, rerollRectRev, rerollColorRev);
        DrawBorder(spriteBatch, rerollRectRev, 2, Color.Gold * 0.6f);
        
        var rerollTextRev = _isRolling ? "Rolling..." : "?? Reroll";
        var rerollTextSizeRev = _font.MeasureString(rerollTextRev);
        spriteBatch.DrawString(_font, rerollTextRev, new Vector2(rerollRectRev.X + (rerollRectRev.Width - rerollTextSizeRev.X * 0.7f) / 2, rerollRectRev.Y + (rerollRectRev.Height - rerollTextSizeRev.Y * 0.7f) / 2), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        // Info text
        var infoText = "These scores will be modified by your racial bonuses shown above.";
        var infoSize = _font.MeasureString(infoText);
        spriteBatch.DrawString(_font, infoText, new Vector2(menuRect.X + (menuRect.Width - infoSize.X * 0.5f) / 2, menuRect.Y + titleH + 310), Color.LightBlue * 0.8f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }

    private void DrawFinalReviewStep(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var selectedRace = Race.GetRace(_races[_raceIndex]);
        var selectedClass = ClassData.GetClass(_classes[_classIndex]);
        
        var instructionText = "Review your character before finalizing:";
        spriteBatch.DrawString(_font, instructionText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.Gold, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        int leftX = menuRect.X + padding;
        int rightX = menuRect.X + menuRect.Width / 2 + 10;
        int yOffset = menuRect.Y + titleH + 50;
        
        // Left column - Basic info
        spriteBatch.DrawString(_font, $"Name: {_createName.Trim()}", new Vector2(leftX, yOffset), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        yOffset += 30;
        spriteBatch.DrawString(_font, $"Race: {selectedRace.DisplayName}", new Vector2(leftX, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        yOffset += 28;
        spriteBatch.DrawString(_font, $"Class: {selectedClass.Name}", new Vector2(leftX, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        yOffset += 28;
        spriteBatch.DrawString(_font, $"Level: 1", new Vector2(leftX, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        yOffset += 35;
        
        // Abilities with final values
        spriteBatch.DrawString(_font, "Final Ability Scores:", new Vector2(leftX, yOffset), Color.Gold, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        yOffset += 25;
        
        var tempChar = CreateTempCharacterForPreview();
        var abilities = new[] {
            ("STR", tempChar.Strength),
            ("DEX", tempChar.Dexterity),
            ("CON", tempChar.Constitution),
            ("INT", tempChar.Intelligence),
            ("WIS", tempChar.Wisdom),
            ("CHA", tempChar.Charisma)
        };
        
        foreach (var (name, value) in abilities)
        {
            int modifier = (value - 10) / 2;
            string modText = modifier >= 0 ? $"+{modifier}" : $"{modifier}";
            spriteBatch.DrawString(_font, $"  {name}: {value} ({modText})", new Vector2(leftX, yOffset), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        yOffset += 15;
        
        // HP and AC
        spriteBatch.DrawString(_font, $"Max HP: {tempChar.MaxHP}", new Vector2(leftX, yOffset), Color.Red, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        yOffset += 25;
        spriteBatch.DrawString(_font, $"Armor Class: {tempChar.ArmorClass}", new Vector2(leftX, yOffset), Color.Orange, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        yOffset += 25;
        spriteBatch.DrawString(_font, $"Speed: {tempChar.Speed} ft", new Vector2(leftX, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        // Right column - Proficiencies and equipment
        yOffset = menuRect.Y + titleH + 50;
        
        spriteBatch.DrawString(_font, "Saving Throws:", new Vector2(rightX, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        yOffset += 22;
        foreach (var save in selectedClass.SavingThrowProficiencies)
        {
            spriteBatch.DrawString(_font, $"   {save}", new Vector2(rightX, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 20;
        }
        
        yOffset += 10;
        
        spriteBatch.DrawString(_font, "Armor Proficiencies:", new Vector2(rightX, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 22;
        var armorText = "  " + string.Join(", ", selectedClass.ArmorProficiencies);
        DrawWrappedText(spriteBatch, armorText, new Vector2(rightX, yOffset), 450, Color.White, 0.5f);
        yOffset += 45;
        
        spriteBatch.DrawString(_font, "Weapon Proficiencies:", new Vector2(rightX, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 22;
        var weaponText = "  " + string.Join(", ", selectedClass.WeaponProficiencies);
        DrawWrappedText(spriteBatch, weaponText, new Vector2(rightX, yOffset), 450, Color.White, 0.5f);
        yOffset += 45;
        
        // Starting equipment preview
        var startingEquipment = StartingEquipment.GetStartingEquipment(selectedClass.Name);
        spriteBatch.DrawString(_font, "Starting Equipment:", new Vector2(rightX, yOffset), Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 22;
        
        int equipCount = 0;
        foreach (var item in startingEquipment.EquippedItems)
        {
            if (equipCount >= 3) break;
            spriteBatch.DrawString(_font, $"   {item} (equipped)", new Vector2(rightX, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            yOffset += 18;
            equipCount++;
        }
        foreach (var item in startingEquipment.Items)
        {
            if (equipCount >= 6) break;
            spriteBatch.DrawString(_font, $"   {item}", new Vector2(rightX, yOffset), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            yOffset += 18;
            equipCount++;
        }
        if (startingEquipment.Items.Count + startingEquipment.EquippedItems.Count > 6)
        {
            spriteBatch.DrawString(_font, "  ...and more", new Vector2(rightX, yOffset), Color.Gray, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }

    private void DrawNavigationButtons(SpriteBatch spriteBatch, Rectangle menuRect, int padding, MouseState mouse)
    {
        bool canGoNext = CanProceedToNextStep();
        bool canGoPrev = _createStep > 0;
        
        var nextRect = new Rectangle(menuRect.X + menuRect.Width - padding - 120, menuRect.Y + menuRect.Height - padding - 45, 120, 40);
        var prevRect = new Rectangle(menuRect.X + padding, menuRect.Y + menuRect.Height - padding - 45, 120, 40);
        
        // Previous button
        if (canGoPrev)
        {
            var prevColor = prevRect.Contains(mouse.Position) ? Color.LightGray : Color.Gray;
            spriteBatch.Draw(_pixel, prevRect, prevColor);
            DrawBorder(spriteBatch, prevRect, 2, Color.White * 0.5f);
            var prevText = "? Previous";
            var prevTextSize = _font.MeasureString(prevText);
            spriteBatch.DrawString(_font, prevText, new Vector2(prevRect.X + (prevRect.Width - prevTextSize.X * 0.6f) / 2, prevRect.Y + (prevRect.Height - prevTextSize.Y * 0.6f) / 2), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
        
        // Next button
        if (canGoNext)
        {
            var nextColor = nextRect.Contains(mouse.Position) ? Color.Green * 1.2f : Color.Green;
            spriteBatch.Draw(_pixel, nextRect, nextColor);
            DrawBorder(spriteBatch, nextRect, 2, Color.LightGreen);
            
            string nextText = _createStep == 4 ? "? Create!" : "Next ?";
            var nextTextSize = _font.MeasureString(nextText);
            spriteBatch.DrawString(_font, nextText, new Vector2(nextRect.X + (nextRect.Width - nextTextSize.X * 0.7f) / 2, nextRect.Y + (nextRect.Height - nextTextSize.Y * 0.7f) / 2), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        else
        {
            // Disabled next button
            spriteBatch.Draw(_pixel, nextRect, Color.DarkGray * 0.5f);
            DrawBorder(spriteBatch, nextRect, 2, Color.Gray * 0.3f);
            var nextText = "Next ?";
            var nextTextSize = _font.MeasureString(nextText);
            spriteBatch.DrawString(_font, nextText, new Vector2(nextRect.X + (nextRect.Width - nextTextSize.X * 0.7f) / 2, nextRect.Y + (nextRect.Height - nextTextSize.Y * 0.7f) / 2), Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
    }

    private void DrawScrollArrows(SpriteBatch spriteBatch, Rectangle listRect, int scrollOffset, int totalItems, int maxVisible, MouseState mouse)
    {
        // Up arrow
        if (scrollOffset > 0)
        {
            var upArrow = new Rectangle(listRect.X + listRect.Width - 30, listRect.Y + 5, 25, 25);
            var upColor = upArrow.Contains(mouse.Position) ? Color.Gold : Color.LightGray;
            spriteBatch.Draw(_pixel, upArrow, upColor);
            spriteBatch.DrawString(_font, "?", new Vector2(upArrow.X + 4, upArrow.Y - 2), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        
        // Down arrow
        if (scrollOffset < totalItems - maxVisible)
        {
            var downArrow = new Rectangle(listRect.X + listRect.Width - 30, listRect.Y + listRect.Height - 30, 25, 25);
            var downColor = downArrow.Contains(mouse.Position) ? Color.Gold : Color.LightGray;
            spriteBatch.Draw(_pixel, downArrow, downColor);
            spriteBatch.DrawString(_font, "?", new Vector2(downArrow.X + 4, downArrow.Y - 2), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        
        // Scroll indicator
        if (totalItems > maxVisible)
        {
            int barHeight = listRect.Height - 70;
            int barY = listRect.Y + 35;
            float scrollPercent = (float)scrollOffset / (totalItems - maxVisible);
            int indicatorY = barY + (int)(barHeight * scrollPercent);
            
            var scrollBar = new Rectangle(listRect.X + listRect.Width - 28, indicatorY, 21, 20);
            spriteBatch.Draw(_pixel, scrollBar, Color.Gold * 0.6f);
        }
    }

    private void DrawTooltip(SpriteBatch spriteBatch, string text, Point mousePos)
    {
        var textSize = _font.MeasureString(text);
        var tooltipWidth = (int)(textSize.X * 0.5f) + 16;
        var tooltipHeight = (int)(textSize.Y * 0.5f) + 12;
        
        var tooltipRect = new Rectangle(mousePos.X + 15, mousePos.Y + 15, tooltipWidth, tooltipHeight);
        
        // Keep tooltip on screen
        var vp = spriteBatch.GraphicsDevice.Viewport;
        if (tooltipRect.Right > vp.Width)
            tooltipRect.X = mousePos.X - tooltipWidth - 15;
        if (tooltipRect.Bottom > vp.Height)
            tooltipRect.Y = mousePos.Y - tooltipHeight - 15;
        
        spriteBatch.Draw(_pixel, tooltipRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, tooltipRect, 1, Color.Gold);
        spriteBatch.DrawString(_font, text, new Vector2(tooltipRect.X + 8, tooltipRect.Y + 6), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void DrawWrappedText(SpriteBatch spriteBatch, string text, Vector2 position, int maxWidth, Color color, float scale)
    {
        var words = text.Split(' ');
        float x = position.X;
        float y = position.Y;
        float spaceWidth = _font.MeasureString(" ").X * scale;
        float lineHeight = _font.MeasureString("A").Y * scale;
        
        foreach (var word in words)
        {
            var wordSize = _font.MeasureString(word);
            float wordWidth = wordSize.X * scale;
            
            if (x + wordWidth > position.X + maxWidth)
            {
                x = position.X;
                y += lineHeight;
            }
            
            spriteBatch.DrawString(_font, word, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            x += wordWidth + spaceWidth;
        }
    }

    private bool IsMouseClicked(MouseState current, MouseState previous, Rectangle rect)
    {
        return rect.Contains(current.Position) && 
               current.LeftButton == ButtonState.Released && 
               previous.LeftButton == ButtonState.Pressed;
    }

    private bool CanProceedToNextStep()
    {
        return _createStep switch
        {
            0 => !string.IsNullOrWhiteSpace(_createName),
            1 => true,
            2 => true,
            3 => !_isRolling,
            4 => true,
            _ => false
        };
    }

    private string GetNextStepTooltip()
    {
        return _createStep switch
        {
            0 => "Proceed to race selection",
            1 => "Proceed to class selection",
            2 => "Roll ability scores",
            3 => "Review your character",
            4 => "Finalize and create character",
            _ => "Next"
        };
    }

    private string FormatBonus(int bonus)
    {
        return bonus >= 0 ? $"+{bonus}" : $"{bonus}";
    }

    private void RollAbilities()
    {
        for (int i = 0; i < 6; i++)
        {
            // Roll 4d6, drop lowest (standard D&D 5e method)
            int[] rolls = Dice.RollMany(4, 6);
            Array.Sort(rolls);
            // Sum top 3 (drop the lowest)
            _rolledAbilities[i] = rolls[1] + rolls[2] + rolls[3];
        }
    }

    private Character CreateTempCharacterForPreview()
    {
        var selectedRace = Race.GetRace(_races[_raceIndex]);
        var selectedClass = ClassData.GetClass(_classes[_classIndex]);
        
        var c = new Character 
        { 
            Name = _createName.Trim(), 
            CreatedAt = DateTime.UtcNow, 
            Race = selectedRace.Name, 
            Class = selectedClass.Name, 
            Level = 1, 
            XP = 0,
            Strength = _rolledAbilities[0] + selectedRace.StrengthBonus,
            Dexterity = _rolledAbilities[1] + selectedRace.DexterityBonus,
            Constitution = _rolledAbilities[2] + selectedRace.ConstitutionBonus,
            Intelligence = _rolledAbilities[3] + selectedRace.IntelligenceBonus,
            Wisdom = _rolledAbilities[4] + selectedRace.WisdomBonus,
            Charisma = _rolledAbilities[5] + selectedRace.CharismaBonus,
            Speed = selectedRace.BaseSpeed,
            HitDiceType = selectedClass.HitDice,
            ArmorProficiencies = new List<string>(selectedClass.ArmorProficiencies),
            WeaponProficiencies = new List<string>(selectedClass.WeaponProficiencies)
        };
        
        int conMod = c.GetAbilityModifier(c.Constitution);
        c.MaxHP = selectedClass.HitDice + conMod;
        c.CurrentHP = c.MaxHP;
        c.HitDiceTotal = 1;
        c.HitDiceRemaining = 1;
        
        // Calculate AC
        c.CalculateDerivedStats();
        
        return c;
    }

    private string GenerateRandomName()
    {
        var prefix = _namePrefixes[_random.Next(_namePrefixes.Length)];
        var suffix = _nameSuffixes[_random.Next(_nameSuffixes.Length)];
        return prefix + suffix;
    }

    private Character CreateCharacterFromData()
    {
        var selectedRace = Race.GetRace(_races[_raceIndex]);
        var selectedClass = ClassData.GetClass(_classes[_classIndex]);
        
        var c = new Character 
        { 
            Name = _createName.Trim(), 
            CreatedAt = DateTime.UtcNow, 
            Race = selectedRace.Name, 
            Class = selectedClass.Name, 
            Level = 1, 
            XP = 0,
            Strength = _rolledAbilities[0],
            Dexterity = _rolledAbilities[1],
            Constitution = _rolledAbilities[2],
            Intelligence = _rolledAbilities[3],
            Wisdom = _rolledAbilities[4],
            Charisma = _rolledAbilities[5]
        };
        
        // Apply racial bonuses
        c.Strength += selectedRace.StrengthBonus;
        c.Dexterity += selectedRace.DexterityBonus;
        c.Constitution += selectedRace.ConstitutionBonus;
        c.Intelligence += selectedRace.IntelligenceBonus;
        c.Wisdom += selectedRace.WisdomBonus;
        c.Charisma += selectedRace.CharismaBonus;
        c.Speed = selectedRace.BaseSpeed;
        
        // Set hit dice type and calculate HP
        c.HitDiceType = selectedClass.HitDice;
        int conMod = c.GetAbilityModifier(c.Constitution);
        c.MaxHP = selectedClass.HitDice + conMod;
        c.HitDiceTotal = 1;
        c.HitDiceRemaining = 1;
        
        // Apply armor and weapon proficiencies from class
        c.ArmorProficiencies = new List<string>(selectedClass.ArmorProficiencies);
        c.WeaponProficiencies = new List<string>(selectedClass.WeaponProficiencies);
        
        // Apply saving throw proficiencies from class
        foreach (var save in selectedClass.SavingThrowProficiencies)
        {
            switch (save)
            {
                case "Strength":
                    c.StrengthSaveProficiency = true;
                    break;
                case "Dexterity":
                    c.DexteritySaveProficiency = true;
                    break;
                case "Constitution":
                    c.ConstitutionSaveProficiency = true;
                    break;
                case "Intelligence":
                    c.IntelligenceSaveProficiency = true;
                    break;
                case "Wisdom":
                    c.WisdomSaveProficiency = true;
                    break;
                case "Charisma":
                    c.CharismaSaveProficiency = true;
                    break;
            }
        }
        
        // Apply starting skill proficiencies based on class (basic selection)
        switch (c.Class)
        {
            case "Barbarian":
                c.AthleticsProficiency = true;
                c.IntimidationProficiency = true;
                break;
            case "Bard":
                c.PerceptionProficiency = true;
                c.PerformanceProficiency = true;
                c.PersuasionProficiency = true;
                break;
            case "Cleric":
                c.MedicineProficiency = true;
                c.ReligionProficiency = true;
                break;
            case "Druid":
                c.NatureProficiency = true;
                c.SurvivalProficiency = true;
                break;
            case "Fighter":
                c.AthleticsProficiency = true;
                c.IntimidationProficiency = true;
                break;
            case "Monk":
                c.AcrobaticsProficiency = true;
                c.StealthProficiency = true;
                break;
            case "Paladin":
                c.AthleticsProficiency = true;
                c.ReligionProficiency = true;
                break;
            case "Ranger":
                c.SurvivalProficiency = true;
                c.NatureProficiency = true;
                break;
            case "Rogue":
                c.StealthProficiency = true;
                c.SleightOfHandProficiency = true;
                c.AcrobaticsProficiency = true;
                c.PerceptionProficiency = true;
                break;
            case "Sorcerer":
                c.ArcanaProficiency = true;
                c.PersuasionProficiency = true;
                break;
            case "Warlock":
                c.ArcanaProficiency = true;
                c.DeceptionProficiency = true;
                break;
            case "Wizard":
                c.ArcanaProficiency = true;
                c.InvestigationProficiency = true;
                break;
        }
        
        // Add starting equipment based on class
        var startingEquipment = StartingEquipment.GetStartingEquipment(c.Class);
        foreach (var itemName in startingEquipment.Items)
        {
            c.InventoryData.AddItem(itemName);
        }
        foreach (var itemName in startingEquipment.EquippedItems)
        {
            c.InventoryData.EquipItem(itemName);
        }
        c.GoldPieces = startingEquipment.GoldPieces;
        
        c.CurrentHP = c.MaxHP;
        
        // Calculate AC from equipment
        c.CalculateDerivedStats();
        
        return c;
    }
}
