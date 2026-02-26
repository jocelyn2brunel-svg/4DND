using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace _4DND;

public class CharacterCreation
{
    private string _createName = string.Empty;
    private int _createStep = 0; // 0: name, 1: race, 2: class, 3: tool proficiency (dwarves), 4: skills, 5: abilities, 6: final review
    private List<string> _races = Race.GetAllRaceNames();
    private readonly string[] _classes = new[] { "Barbarian", "Bard", "Cleric", "Druid", "Fighter", "Monk", "Paladin", "Ranger", "Rogue", "Sorcerer", "Warlock", "Wizard" };
    private int _raceIndex = 0;
    private int _classIndex = 0;
    private int _skillIndex = 0;
    private int[] _rolledAbilities = new int[6];
    private readonly string[] _abilityNames = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
    private int _selectedToolIndex = 0; // index of the chosen tool in the race's ToolProficiencyChoices

    // Starting wealth toggle (step 6)
    private bool _useStartingWealth = false;
    private int _rolledStartingWealth = 0;

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
    private readonly Dictionary<string, HashSet<string>> _selectedClassSkills = new();

    private static readonly Dictionary<string, string> _skillDescriptions = new()
    {
        ["Acrobatics"] = "Your Dexterity (Acrobatics) check covers your attempt to stay on your feet in a tricky situation, such as when you're trying to run across a sheet of ice, balance on a tightrope, or stay upright on a rocking ship's deck.",
        ["Animal Handling"] = "When there is any question whether you can calm down a domesticated animal, keep a mount from getting spooked, or intuit an animal's intentions, the DM might call for a Wisdom (Animal Handling) check.",
        ["Arcana"] = "Your Intelligence (Arcana) check measures your ability to recall lore about spells, magic items, phenomenological traditions, the planes of existence, and the inhabitants of those planes.",
        ["Athletics"] = "Your Strength (Athletics) check covers difficult situations you encounter while climbing, jumping, or swimming.",
        ["Deception"] = "Your Charisma (Deception) check determines whether you can convincingly hide the truth, either verbally or through your actions.",
        ["History"] = "Your Intelligence (History) check measures your ability to recall lore about historical events, legendary people, ancient kingdoms, past disputes, recent wars, and lost civilizations.",
        ["Insight"] = "Your Wisdom (Insight) check decides whether you can determine the true intentions of a creature, such as when searching out a lie or predicting someone's next move.",
        ["Intimidation"] = "When you attempt to influence someone through overt threats, hostile actions, and physical violence, the DM might call for a Charisma (Intimidation) check.",
        ["Investigation"] = "When you look around for clues and make deductions based on those clues, you make an Intelligence (Investigation) check.",
        ["Medicine"] = "A Wisdom (Medicine) check lets you try to stabilize a dying companion or diagnose an illness.",
        ["Nature"] = "Your Intelligence (Nature) check measures your ability to recall lore about terrain, plants and animals, the weather, and natural cycles.",
        ["Perception"] = "Your Wisdom (Perception) check lets you spot, hear, or otherwise detect the presence of something. It measures your general awareness of your surroundings and the keenness of your senses.",
        ["Performance"] = "Your Charisma (Performance) check determines how well you can delight an audience with music, dance, acting, storytelling, or some other form of entertainment.",
        ["Persuasion"] = "When you attempt to influence someone or a group of people with tact, social graces, or good nature, the DM might call for a Charisma (Persuasion) check.",
        ["Religion"] = "Your Intelligence (Religion) check measures your ability to recall lore about deities, rites and prayers, religious hierarchies, holy symbols, and the practices of secret cults.",
        ["Sleight of Hand"] = "Whenever you attempt an act of legerdemain or manual trickery, such as planting something on someone else or concealing an object on your person, you make a Dexterity (Sleight of Hand) check.",
        ["Stealth"] = "Make a Dexterity (Stealth) check when you attempt to conceal yourself from enemies, slink past guards, slip away without being noticed, or sneak up on someone without being seen or heard.",
        ["Survival"] = "The DM might guide you to make a Wisdom (Survival) check to follow tracks, hunt wild game, guide your group through frozen wastelands, identify signs that owlbears live nearby, predict the weather, or avoid quicksand and other natural hazards."
    };

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
        _skillIndex = 0;
        _rolledAbilities = new int[6];
        _raceScrollOffset = 0;
        _classScrollOffset = 0;
        _isRolling = false;
        _rollTimer = 0;
        _selectedClassSkills.Clear();
        _useStartingWealth = false;
        _rolledStartingWealth = 0;
    }

    public bool Update(GameTime gameTime, GraphicsDevice graphics, KeyboardState kb, KeyboardState prevKb, out Character? createdCharacter)
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
            if (_createStep == 4) // Before going to abilities, roll them
            {
                RollAbilities();
                _createStep = 5; // Advance to abilities step
            }
            else if (_createStep == 5) // After abilities, go to final review
            {
                _createStep = 6;
                _prevMouse = mouse;
                return true;
            }
            else if (_createStep == 6) // Final confirmation
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
                _tooltipText = "Generate a random fantasy name";

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
                    _skillIndex = 0;
                    _rolledStartingWealth = 0;
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
        // Step 3: Tool proficiency selection — interaction handled in DrawToolProficiencyStep
        // Step 4: Skill selection
        else if (_createStep == 4)
        {
            var selectedClass = ClassData.GetClass(_classes[_classIndex]);
            var skillsRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH + 40, 460, 420);

            if (selectedClass.SkillChoicesCount > 0 && selectedClass.SkillChoiceOptions.Count > 0)
            {
                for (int i = 0; i < selectedClass.SkillChoiceOptions.Count; i++)
                {
                    int col = i / 10;
                    int row = i % 10;
                    var item = new Rectangle(skillsRect.X + 4 + col * 228, skillsRect.Y + row * 40 + 4, 224, 36);

                    if (item.Contains(mouse.Position))
                    {
                        _skillIndex = i;
                    }

                    if (IsMouseClicked(mouse, _prevMouse, item))
                    {
                        ToggleSkillSelection(selectedClass.Name, selectedClass.SkillChoiceOptions[i], selectedClass.SkillChoicesCount);
                    }
                }
            }
        }
        // Step 5: Ability rolls
        else if (_createStep == 5)
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
            var title = $"Create Character - Step {_createStep + 1}/7";
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
                    DrawToolProficiencyStep(spriteBatch, menuRectC, paddingC, titleH, mouse);
                    break;
                case 4:
                    DrawSkillsStep(spriteBatch, menuRectC, paddingC, titleH, mouse);
                    break;
                case 5:
                    DrawAbilitiesStep(spriteBatch, gameTime, menuRectC, paddingC, titleH, mouse);
                    break;
                case 6:
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
        float progress = (_createStep + 1) / 7f;
        var fillRect = new Rectangle(barX, barY, (int)(barWidth * progress), barHeight);
        spriteBatch.Draw(_pixel, fillRect, Color.LightGreen);
        
        // Step labels
        string[] stepLabels = { Loc.Tr("Name"), Loc.Tr("Race"), Loc.Tr("Class"), Loc.Tr("Tools"), Loc.Tr("Skills"), Loc.Tr("Abilities"), Loc.Tr("Review") };
        for (int i = 0; i < 7; i++)
        {
            int stepX = barX + (barWidth * i / 6);
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
        var instructionText = Loc.Tr("Enter your character's name:");
        spriteBatch.DrawString(_font, instructionText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        var nameRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 40, menuRect.Width - padding * 2, 50);
        var nameColor = nameRect.Contains(mouse.Position) ? Color.White : Color.LightGray;
        spriteBatch.Draw(_pixel, nameRect, nameColor);
        DrawBorder(spriteBatch, nameRect, 2, Color.Gold * 0.4f);

        var randomNameRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 100, 240, 40);
        var randomNameColor = randomNameRect.Contains(mouse.Position) ? Color.MediumPurple : Color.DarkSlateBlue;
        spriteBatch.Draw(_pixel, randomNameRect, randomNameColor);
        DrawBorder(spriteBatch, randomNameRect, 1, Color.White * 0.7f);

        var randomNameText = "[D6] " + Loc.Tr("Random name");
        var randomNameTextSize = _font.MeasureString(randomNameText);
        spriteBatch.DrawString(_font, randomNameText, new Vector2(randomNameRect.X + (randomNameRect.Width - randomNameTextSize.X * 0.65f) / 2, randomNameRect.Y + (randomNameRect.Height - randomNameTextSize.Y * 0.65f) / 2), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        
        var displayName = string.IsNullOrEmpty(_createName) ? Loc.Tr("Type your character's name...") : _createName + (((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2) == 0 ? "|" : "");
        spriteBatch.DrawString(_font, displayName, new Vector2(nameRect.X + 12, nameRect.Y + 12), string.IsNullOrEmpty(_createName) ? Color.Gray : Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        
        // Tips
        var tipText = Loc.Tr("Name Tip");
        spriteBatch.DrawString(_font, tipText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 150), Color.LightBlue, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }

    private void DrawRaceStep(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var instructionText = Loc.Tr("Choose your character's race:");
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
        spriteBatch.DrawString(_font, Loc.Tr("Ability Score Increases:"), new Vector2(detailsRect.X + 12, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
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
            spriteBatch.DrawString(_font, "  - " + bonus, new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        yOffset += 10;
        
        // Racial traits
        spriteBatch.DrawString(_font, Loc.Tr("Traits:"), new Vector2(detailsRect.X + 12, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 25;
        
        spriteBatch.DrawString(_font, "  - " + Loc.Tr("Speed: {0} feet", selectedRace.BaseSpeed), new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        yOffset += 22;
        
        if (selectedRace.DarkvisionRange > 0)
        {
            var darkvisionText = selectedRace.HasSuperiorDarkvision ? "  - " + Loc.Tr("Superior Darkvision: {0} feet", selectedRace.DarkvisionRange) : "  - " + Loc.Tr("Darkvision: {0} feet", selectedRace.DarkvisionRange);
            spriteBatch.DrawString(_font, darkvisionText, new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        if (selectedRace.HasSunlightSensitivity)
        {
            spriteBatch.DrawString(_font, "  - Sunlight Sensitivity", new Vector2(detailsRect.X + 12, yOffset), Color.Orange, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        if (selectedRace.HasDwarvenResilience)
        {
            spriteBatch.DrawString(_font, "  - Dwarven Resilience: advantage on poison saves, resistance to poison damage", new Vector2(detailsRect.X + 12, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        if (selectedRace.HasDwarvenCombatTraining)
        {
            spriteBatch.DrawString(_font, "  - Dwarven Combat Training: proficiency with battleaxe, handaxe, light hammer, warhammer", new Vector2(detailsRect.X + 12, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        if (selectedRace.HasStonecunning)
        {
            spriteBatch.DrawString(_font, "  - Stonecunning: double proficiency bonus on History (stonework) checks", new Vector2(detailsRect.X + 12, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        if (selectedRace.ToolProficiencyChoices.Count > 0)
        {
            string choices = string.Join(", ", selectedRace.ToolProficiencyChoices);
            spriteBatch.DrawString(_font, $"  - Tool Proficiency (choice): {choices}", new Vector2(detailsRect.X + 12, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            yOffset += 22;
        }
        
        if (selectedRace.Languages.Count > 0)
        {
            string langs = string.Join(", ", selectedRace.Languages);
            spriteBatch.DrawString(_font, $"  - Languages: {langs}", new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
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

    private void DrawToolProficiencyStep(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var selectedRace = Race.GetRace(_races[_raceIndex]);
        if (selectedRace.ToolProficiencyChoices.Count == 0)
        {
            // Race has no tool proficiency choice — skip this step automatically
            _createStep = 4;
            return;
        }

        spriteBatch.DrawString(_font, "Choose your Artisan Tool Proficiency:", new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, "(Smith's tools, Brewer's supplies, or Mason's tools)", new Vector2(menuRect.X + padding, menuRect.Y + titleH + 35), Color.LightGray, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);

        var toolsRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 70, 460, selectedRace.ToolProficiencyChoices.Count * 52 + 10);
        spriteBatch.Draw(_pixel, toolsRect, Color.Gray * 0.7f);
        DrawBorder(spriteBatch, toolsRect, 2, Color.Gold * 0.4f);

        for (int i = 0; i < selectedRace.ToolProficiencyChoices.Count; i++)
        {
            var item = new Rectangle(toolsRect.X + 4, toolsRect.Y + i * 52 + 4, 452, 46);
            bool isSelected = (i == _selectedToolIndex);
            bool isHovered = item.Contains(mouse.Position);

            Color itemColor = isSelected ? Color.Gold * 0.8f : (isHovered ? Color.LightGray * 0.8f : Color.DarkGray);
            spriteBatch.Draw(_pixel, item, itemColor);
            if (isSelected)
                DrawBorder(spriteBatch, item, 2, Color.Gold);

            string toolName = selectedRace.ToolProficiencyChoices[i];
            spriteBatch.DrawString(_font, toolName, new Vector2(item.X + 12, item.Y + 12), isSelected ? Color.Black : Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            if (isHovered)
            {
                _tooltipText = $"Gain proficiency with {toolName}";
                if (IsMouseClicked(mouse, _prevMouse, item))
                    _selectedToolIndex = i;
            }
        }
    }

    private void DrawSkillsStep(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var selectedClass = ClassData.GetClass(_classes[_classIndex]);
        var instructionText = $"Choose {selectedClass.SkillChoicesCount} skills for your {selectedClass.Name}:";
        spriteBatch.DrawString(_font, instructionText, new Vector2(menuRect.X + padding, menuRect.Y + titleH + 10), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        var skillsRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleH + 40, 460, 420);
        spriteBatch.Draw(_pixel, skillsRect, Color.Gray * 0.7f);
        DrawBorder(spriteBatch, skillsRect, 2, Color.Gold * 0.4f);

        var selectedSkills = GetSelectedSkillsForClass(selectedClass);
        if (selectedClass.SkillChoicesCount > 0 && selectedClass.SkillChoiceOptions.Count > 0)
        {
            for (int i = 0; i < selectedClass.SkillChoiceOptions.Count; i++)
            {
                int col = i / 10;
                int row = i % 10;
                var item = new Rectangle(skillsRect.X + 4 + col * 228, skillsRect.Y + row * 40 + 4, 224, 36);

                string skill = selectedClass.SkillChoiceOptions[i];
                bool isSelected = selectedSkills.Contains(skill);
                bool isHovered = item.Contains(mouse.Position);

                Color itemColor = isSelected ? Color.Gold * 0.8f : (isHovered ? Color.LightGray * 0.8f : Color.DarkGray);
                spriteBatch.Draw(_pixel, item, itemColor);
                if (isSelected)
                    DrawBorder(spriteBatch, item, 2, Color.Gold);

                spriteBatch.DrawString(_font, skill, new Vector2(item.X + 8, item.Y + 10), isSelected ? Color.Black : Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

                if (isHovered)
                {
                    _skillIndex = i;
                    if (IsMouseClicked(mouse, _prevMouse, item))
                        ToggleSkillSelection(selectedClass.Name, skill, selectedClass.SkillChoicesCount);
                }
            }
        }

        DrawSkillDetails(spriteBatch, menuRect, padding, titleH, mouse);
    }

    private void DrawSkillDetails(SpriteBatch spriteBatch, Rectangle menuRect, int padding, int titleH, MouseState mouse)
    {
        var selectedClass = ClassData.GetClass(_classes[_classIndex]);
        if (_skillIndex < 0 || _skillIndex >= selectedClass.SkillChoiceOptions.Count) return;

        var skillName = selectedClass.SkillChoiceOptions[_skillIndex];
        var detailsRect = new Rectangle(menuRect.X + padding + 480, menuRect.Y + titleH + 40, 480, 420);
        spriteBatch.Draw(_pixel, detailsRect, Color.DarkSlateGray * 0.8f);
        DrawBorder(spriteBatch, detailsRect, 2, Color.Gold * 0.4f);

        int yOffset = detailsRect.Y + 12;

        // Skill name
        spriteBatch.DrawString(_font, skillName, new Vector2(detailsRect.X + 12, yOffset), Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        yOffset += 35;

        // Ability
        string ability = GetSkillAbility(skillName);
        spriteBatch.DrawString(_font, $"Ability: {ability}", new Vector2(detailsRect.X + 12, yOffset), Color.Yellow, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        yOffset += 30;

        // Description
        if (_skillDescriptions.TryGetValue(skillName, out var desc))
        {
            DrawWrappedText(spriteBatch, desc, new Vector2(detailsRect.X + 12, yOffset), detailsRect.Width - 24, Color.White, 0.55f);
        }
    }

    private string GetSkillAbility(string skill)
    {
        return skill switch
        {
            "Acrobatics" => "Dexterity",
            "Animal Handling" => "Wisdom",
            "Arcana" => "Intelligence",
            "Athletics" => "Strength",
            "Deception" => "Charisma",
            "History" => "Intelligence",
            "Insight" => "Wisdom",
            "Intimidation" => "Charisma",
            "Investigation" => "Intelligence",
            "Medicine" => "Wisdom",
            "Nature" => "Intelligence",
            "Perception" => "Wisdom",
            "Performance" => "Charisma",
            "Persuasion" => "Charisma",
            "Religion" => "Intelligence",
            "Sleight of Hand" => "Dexterity",
            "Stealth" => "Dexterity",
            "Survival" => "Wisdom",
            _ => "Unknown"
        };
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
            spriteBatch.DrawString(_font, $"  - {save}", new Vector2(detailsRect.X + 12, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
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
            yOffset += 40;
        }

        // Tool Proficiencies
        spriteBatch.DrawString(_font, "Tools:", new Vector2(detailsRect.X + 12, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        yOffset += 22;
        var toolText = selectedClass.ToolProficiencies.Count > 0
            ? "  " + string.Join(", ", selectedClass.ToolProficiencies)
            : "  None";
        DrawWrappedText(spriteBatch, toolText, new Vector2(detailsRect.X + 12, yOffset), detailsRect.Width - 24, Color.White, 0.5f);
        yOffset += 40;

        // Skill Choices
        if (selectedClass.SkillChoicesCount > 0 && selectedClass.SkillChoiceOptions.Count > 0)
        {
            spriteBatch.DrawString(_font, "Skills:", new Vector2(detailsRect.X + 12, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 22;
            var skillsText = $"  You will choose {selectedClass.SkillChoicesCount} skills in the next step.";
            DrawWrappedText(spriteBatch, skillsText, new Vector2(detailsRect.X + 12, yOffset), detailsRect.Width - 24, Color.White, 0.5f);
            yOffset += 40;
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
            spriteBatch.DrawString(_font, $"  - {save}", new Vector2(rightX, yOffset), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
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

        var selectedSkills = GetSelectedSkillsForClass(selectedClass);
        if (selectedSkills.Count > 0)
        {
            spriteBatch.DrawString(_font, "Class Skills:", new Vector2(rightX, yOffset), Color.LightBlue, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 22;
            var classSkillText = "  " + string.Join(", ", selectedSkills);
            DrawWrappedText(spriteBatch, classSkillText, new Vector2(rightX, yOffset), 450, Color.White, 0.5f);
            yOffset += 45;
        }
        
        // Toggle: Starting Equipment vs Roll for Gold
        var equipToggleRect = new Rectangle(rightX, yOffset, 195, 28);
        var wealthToggleRect = new Rectangle(rightX + 200, yOffset, 195, 28);

        var equipToggleColor = !_useStartingWealth ? Color.DarkGoldenrod : Color.DarkGray;
        var wealthToggleColor = _useStartingWealth ? Color.DarkGoldenrod : Color.DarkGray;

        spriteBatch.Draw(_pixel, equipToggleRect, equipToggleColor);
        DrawBorder(spriteBatch, equipToggleRect, 2, !_useStartingWealth ? Color.Gold : Color.Gray * 0.5f);
        spriteBatch.DrawString(_font, "Starting Equipment", new Vector2(equipToggleRect.X + 6, equipToggleRect.Y + 7), !_useStartingWealth ? Color.White : Color.LightGray, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

        spriteBatch.Draw(_pixel, wealthToggleRect, wealthToggleColor);
        DrawBorder(spriteBatch, wealthToggleRect, 2, _useStartingWealth ? Color.Gold : Color.Gray * 0.5f);
        spriteBatch.DrawString(_font, "Roll for Gold", new Vector2(wealthToggleRect.X + 6, wealthToggleRect.Y + 7), _useStartingWealth ? Color.White : Color.LightGray, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

        if (equipToggleRect.Contains(mouse.Position))
            _tooltipText = "Take your class starting equipment";
        if (wealthToggleRect.Contains(mouse.Position))
            _tooltipText = "Roll for starting gold instead of equipment";

        if (IsMouseClicked(mouse, _prevMouse, equipToggleRect))
            _useStartingWealth = false;
        if (IsMouseClicked(mouse, _prevMouse, wealthToggleRect))
            _useStartingWealth = true;

        yOffset += 36;

        if (!_useStartingWealth)
        {
            var startingEquipment = StartingEquipment.GetStartingEquipment(selectedClass.Name);
            spriteBatch.DrawString(_font, "Starting Equipment:", new Vector2(rightX, yOffset), Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 22;

            int equipCount = 0;
            foreach (var itemName in startingEquipment.EquippedItems)
            {
                if (equipCount >= 3) break;
                spriteBatch.DrawString(_font, $"  - {itemName} (equipped)", new Vector2(rightX, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                yOffset += 18;
                equipCount++;
            }
            foreach (var itemName in startingEquipment.Items)
            {
                if (equipCount >= 6) break;
                spriteBatch.DrawString(_font, $"  - {itemName}", new Vector2(rightX, yOffset), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                yOffset += 18;
                equipCount++;
            }
            if (startingEquipment.Items.Count + startingEquipment.EquippedItems.Count > 6)
                spriteBatch.DrawString(_font, "  ...and more", new Vector2(rightX, yOffset), Color.Gray, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
        else
        {
            string formula = StartingEquipment.GetWealthFormula(selectedClass.Name);
            spriteBatch.DrawString(_font, $"Starting Wealth: {formula}", new Vector2(rightX, yOffset), Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 30;

            var rollBtnRect = new Rectangle(rightX, yOffset, 120, 32);
            var rollBtnColor = rollBtnRect.Contains(mouse.Position) ? Color.DarkGoldenrod * 1.3f : Color.DarkGoldenrod;
            spriteBatch.Draw(_pixel, rollBtnRect, rollBtnColor);
            DrawBorder(spriteBatch, rollBtnRect, 2, Color.Gold);
            spriteBatch.DrawString(_font, "Roll!", new Vector2(rollBtnRect.X + 36, rollBtnRect.Y + 8), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            if (rollBtnRect.Contains(mouse.Position))
                _tooltipText = $"Roll {formula} for starting gold";
            if (IsMouseClicked(mouse, _prevMouse, rollBtnRect))
                _rolledStartingWealth = StartingEquipment.RollStartingWealth(selectedClass.Name);

            yOffset += 40;
            if (_rolledStartingWealth > 0)
            {
                spriteBatch.DrawString(_font, $"Result: {_rolledStartingWealth} gold pieces", new Vector2(rightX, yOffset), Color.Gold, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
                yOffset += 24;
                spriteBatch.DrawString(_font, "(No starting equipment)", new Vector2(rightX, yOffset), Color.LightGray, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            }
            else
            {
                spriteBatch.DrawString(_font, "Click Roll! to determine starting gold", new Vector2(rightX, yOffset), Color.LightGray, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
            }
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
            var prevText = "< " + Loc.Tr("Previous");
            var prevTextSize = _font.MeasureString(prevText);
            spriteBatch.DrawString(_font, prevText, new Vector2(prevRect.X + (prevRect.Width - prevTextSize.X * 0.6f) / 2, prevRect.Y + (prevRect.Height - prevTextSize.Y * 0.6f) / 2), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
        
        // Next button
        if (canGoNext)
        {
            var nextColor = nextRect.Contains(mouse.Position) ? Color.Green * 1.2f : Color.Green;
            spriteBatch.Draw(_pixel, nextRect, nextColor);
            DrawBorder(spriteBatch, nextRect, 2, Color.LightGreen);
            
            string nextText = _createStep == 6 ? Loc.Tr("Create!") : Loc.Tr("Next") + " >";
            var nextTextSize = _font.MeasureString(nextText);
            spriteBatch.DrawString(_font, nextText, new Vector2(nextRect.X + (nextRect.Width - nextTextSize.X * 0.7f) / 2, nextRect.Y + (nextRect.Height - nextTextSize.Y * 0.7f) / 2), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        else
        {
            // Disabled next button
            spriteBatch.Draw(_pixel, nextRect, Color.DarkGray * 0.5f);
            DrawBorder(spriteBatch, nextRect, 2, Color.Gray * 0.3f);
            var nextText = "Next >";
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
        if (_createStep == 4)
        {
            var selectedClass = ClassData.GetClass(_classes[_classIndex]);
            if (selectedClass.SkillChoicesCount <= 0)
                return true;

            return GetSelectedSkillsForClass(selectedClass).Count == selectedClass.SkillChoicesCount;
        }

        return _createStep switch
        {
            0 => !string.IsNullOrWhiteSpace(_createName),
            1 => true,
            2 => true,
            3 => true,
            4 => false,
            5 => !_isRolling,
            6 => !_useStartingWealth || _rolledStartingWealth > 0,
            _ => false
        };
    }

    private string GetNextStepTooltip()
    {
        return _createStep switch
        {
            0 => "Proceed to race selection",
            1 => "Proceed to class selection",
            2 => "Proceed to tool proficiency",
            3 => "Proceed to skill selection",
            4 => "Choose class skills then roll ability scores",
            5 => "Review your character",
            6 => "Finalize and create character",
            _ => "Next"
        };
    }

    private string FormatBonus(int bonus)
    {
        return bonus >= 0 ? $"+{bonus}" : $"{bonus}";
    }

    private List<string> GetSelectedSkillsForClass(ClassData classData)
    {
        if (!_selectedClassSkills.TryGetValue(classData.Name, out var selected))
        {
            selected = new HashSet<string>();
            int defaultCount = Math.Min(classData.SkillChoicesCount, classData.SkillChoiceOptions.Count);
            for (int i = 0; i < defaultCount; i++)
            {
                selected.Add(classData.SkillChoiceOptions[i]);
            }
            _selectedClassSkills[classData.Name] = selected;
        }

        return new List<string>(selected);
    }


    private void ToggleSkillSelection(string className, string skill, int maxChoices)
    {
        if (!_selectedClassSkills.TryGetValue(className, out var selected))
        {
            selected = new HashSet<string>();
            _selectedClassSkills[className] = selected;
        }

        if (selected.Contains(skill))
        {
            selected.Remove(skill);
            return;
        }

        if (selected.Count < maxChoices)
            selected.Add(skill);
    }

    private void ApplySkillProficiency(Character character, string skill)
    {
        switch (skill)
        {
            case "Acrobatics": character.AcrobaticsProficiency = true; break;
            case "Animal Handling": character.AnimalHandlingProficiency = true; break;
            case "Arcana": character.ArcanaProficiency = true; break;
            case "Athletics": character.AthleticsProficiency = true; break;
            case "Deception": character.DeceptionProficiency = true; break;
            case "History": character.HistoryProficiency = true; break;
            case "Insight": character.InsightProficiency = true; break;
            case "Intimidation": character.IntimidationProficiency = true; break;
            case "Investigation": character.InvestigationProficiency = true; break;
            case "Medicine": character.MedicineProficiency = true; break;
            case "Nature": character.NatureProficiency = true; break;
            case "Perception": character.PerceptionProficiency = true; break;
            case "Performance": character.PerformanceProficiency = true; break;
            case "Persuasion": character.PersuasionProficiency = true; break;
            case "Religion": character.ReligionProficiency = true; break;
            case "Sleight of Hand": character.SleightOfHandProficiency = true; break;
            case "Stealth": character.StealthProficiency = true; break;
            case "Survival": character.SurvivalProficiency = true; break;
        }
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

    private string GenerateRandomName()
    {
        var prefix = _namePrefixes[_random.Next(_namePrefixes.Length)];
        var suffix = _nameSuffixes[_random.Next(_nameSuffixes.Length)];
        return prefix + suffix;
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
        
        // Class-specific initialization
        if (c.Class == "Barbarian")
        {
            var barbarianData = ClassData.GetClass("Barbarian");
            var levelData = barbarianData.GetLevelData(c.Level);
            if (levelData != null)
                c.RagesRemaining = levelData.Rages;
        }
        else if (c.Class == "Bard")
        {
            var bardData = ClassData.GetClass("Bard");
            var levelData = bardData.GetBardLevelData(c.Level);
            if (levelData != null)
            {
                c.BardicInspirationDice = levelData.BardicInspirationDice;
                c.CantripsKnown = levelData.CantripsKnown;
                c.SpellsKnown = levelData.SpellsKnown;
                int chaMod = Math.Max(1, c.GetAbilityModifier(c.Charisma));
                c.BardicInspirationMax = chaMod;
                c.BardicInspirationUsesRemaining = chaMod;
                c.SpellSlotsMax = (int[])levelData.SpellSlots.Clone();
                c.SpellSlotsRemaining = (int[])levelData.SpellSlots.Clone();
            }
        }
        else if (c.Class == "Cleric")
        {
            var clericData = ClassData.GetClass("Cleric");
            var levelData = clericData.GetClericLevelData(c.Level);
            if (levelData != null)
            {
                c.CantripsKnown = levelData.CantripsKnown;
                c.SpellSlotsMax = (int[])levelData.SpellSlots.Clone();
                c.SpellSlotsRemaining = (int[])levelData.SpellSlots.Clone();
                c.ChannelDivinityUsesRemaining = levelData.ChannelDivinityUses;
                c.ChannelDivinityUsesMax = levelData.ChannelDivinityUses;
            }
        }
        
        return c;
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

        // Apply racial traits
        c.HasDwarvenResilience = selectedRace.HasDwarvenResilience;
        c.HasStonecunning = selectedRace.HasStonecunning;
        c.Languages = new List<string>(selectedRace.Languages);
        if (selectedRace.HasDwarvenCombatTraining)
        {
            foreach (var w in new[] { "Battleaxe", "Handaxe", "Light Hammer", "Warhammer" })
            {
                if (!c.WeaponProficiencies.Contains(w))
                    c.WeaponProficiencies.Add(w);
            }
        }
        if (selectedRace.ToolProficiencyChoices.Count > 0)
        {
            int chosenIdx = Math.Clamp(_selectedToolIndex, 0, selectedRace.ToolProficiencyChoices.Count - 1);
            c.ToolProficiencies.Add(selectedRace.ToolProficiencyChoices[chosenIdx]);
        }
        foreach (var tool in selectedClass.ToolProficiencies)
        {
            if (!c.ToolProficiencies.Contains(tool))
                c.ToolProficiencies.Add(tool);
        }

        // Apply saving throw proficiencies from class
        foreach (var save in selectedClass.SavingThrowProficiencies)
        {
            switch (save)
            {
                case "Strength":     c.StrengthSaveProficiency = true; break;
                case "Dexterity":    c.DexteritySaveProficiency = true; break;
                case "Constitution": c.ConstitutionSaveProficiency = true; break;
                case "Intelligence": c.IntelligenceSaveProficiency = true; break;
                case "Wisdom":       c.WisdomSaveProficiency = true; break;
                case "Charisma":     c.CharismaSaveProficiency = true; break;
            }
        }

        // Apply selected class skill proficiencies
        var selectedSkills = GetSelectedSkillsForClass(selectedClass);
        foreach (var skill in selectedSkills)
            ApplySkillProficiency(c, skill);

        // Add starting equipment or rolled wealth based on player choice
        if (_useStartingWealth)
        {
            c.GoldPieces = _rolledStartingWealth > 0 ? _rolledStartingWealth : StartingEquipment.RollStartingWealth(c.Class);
        }
        else
        {
            var startingEquipment = StartingEquipment.GetStartingEquipment(c.Class);
            foreach (var itemName in startingEquipment.Items)
                c.InventoryData.AddItem(itemName);
            foreach (var itemName in startingEquipment.EquippedItems)
                c.InventoryData.EquipItem(itemName);
            c.GoldPieces = startingEquipment.GoldPieces;
        }

        c.CurrentHP = c.MaxHP;

        // Calculate AC from equipment
        c.CalculateDerivedStats();

        // Class-specific initialization
        if (c.Class == "Barbarian")
        {
            var barbarianData = ClassData.GetClass("Barbarian");
            var levelData = barbarianData.GetLevelData(c.Level);
            if (levelData != null)
                c.RagesRemaining = levelData.Rages;
        }
        else if (c.Class == "Bard")
        {
            var bardData = ClassData.GetClass("Bard");
            var levelData = bardData.GetBardLevelData(c.Level);
            if (levelData != null)
            {
                c.BardicInspirationDice = levelData.BardicInspirationDice;
                c.CantripsKnown = levelData.CantripsKnown;
                c.SpellsKnown = levelData.SpellsKnown;
                int chaMod = Math.Max(1, c.GetAbilityModifier(c.Charisma));
                c.BardicInspirationMax = chaMod;
                c.BardicInspirationUsesRemaining = chaMod;
                c.SpellSlotsMax = (int[])levelData.SpellSlots.Clone();
                c.SpellSlotsRemaining = (int[])levelData.SpellSlots.Clone();
            }
        }
        else if (c.Class == "Cleric")
        {
            var clericData = ClassData.GetClass("Cleric");
            var levelData = clericData.GetClericLevelData(c.Level);
            if (levelData != null)
            {
                c.CantripsKnown = levelData.CantripsKnown;
                c.SpellSlotsMax = (int[])levelData.SpellSlots.Clone();
                c.SpellSlotsRemaining = (int[])levelData.SpellSlots.Clone();
                c.ChannelDivinityUsesRemaining = levelData.ChannelDivinityUses;
                c.ChannelDivinityUsesMax = levelData.ChannelDivinityUses;
            }
        }

        return c;
    }
}
