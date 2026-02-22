using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace _4DND;

public class CharacterCreation
{
    private string _createName = string.Empty;
    private int _createStep = 0; // 0: name, 1: race, 2: class, 3: abilities
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
    private const int MAX_VISIBLE_RACES = 6;
    
    // Scrolling for class list
    private int _classScrollOffset = 0;
    private const int MAX_VISIBLE_CLASSES = 6;

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
    }

    public bool Update(GameTime gameTime, GraphicsDevice graphics, KeyboardState kb, KeyboardState prevKb, out Character? createdCharacter)
    {
        createdCharacter = null;
        var mouse = Mouse.GetState();
        var vp = graphics.Viewport;

        int menuWidthC = 700;
        int paddingC = 16;
        int titleH = 60;
        int rowH = 40;
        var menuRectC = new Rectangle((vp.Width - menuWidthC) / 2, (vp.Height - 400) / 2, menuWidthC, 400);

        // Back button to cancel
        var backRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + paddingC, 80, 30);
        if (backRect.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed)
        {
            return false; // signal to go back
        }

        // Escape to cancel
        if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
        {
            return false;
        }

        // Step 0: Name input
        if (_createStep == 0)
        {
            foreach (var k in kb.GetPressedKeys())
            {
                if (!prevKb.IsKeyDown(k))
                {
                    if (k == Keys.Back && _createName.Length > 0)
                        _createName = _createName.Substring(0, _createName.Length - 1);
                    else if (k == Keys.Space)
                        _createName += ' ';
                    else if (k >= Keys.A && k <= Keys.Z)
                    {
                        bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift) || Console.CapsLock;
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
                    else if (k == Keys.Enter)
                    {
                        if (!string.IsNullOrWhiteSpace(_createName)) 
                            _createStep = 1;
                    }
                }
            }
        }
        // Step 1: Race selection
        else if (_createStep == 1)
        {
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
            {
                _raceIndex = (_raceIndex - 1 + _races.Count) % _races.Count;
                // Adjust scroll
                if (_raceIndex < _raceScrollOffset)
                    _raceScrollOffset = _raceIndex;
            }
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
            {
                _raceIndex = (_raceIndex + 1) % _races.Count;
                // Adjust scroll
                if (_raceIndex >= _raceScrollOffset + MAX_VISIBLE_RACES)
                    _raceScrollOffset = _raceIndex - MAX_VISIBLE_RACES + 1;
            }
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter)) 
                _createStep = 2;

            // Mouse selection for visible races
            var racesRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH + 52, 320, MAX_VISIBLE_RACES * 36 + 10);
            int visibleCount = Math.Min(MAX_VISIBLE_RACES, _races.Count - _raceScrollOffset);
            
            for (int i = 0; i < visibleCount; i++)
            {
                int raceIdx = _raceScrollOffset + i;
                var item = new Rectangle(racesRect.X + 4, racesRect.Y + i * 36 + 4, 312, 32);
                if (item.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed)
                {
                    _raceIndex = raceIdx;
                    _createStep = 1;
                }
            }
        }
        // Step 2: Class selection
        else if (_createStep == 2)
        {
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up)) 
                _classIndex = (_classIndex - 1 + _classes.Length) % _classes.Length;
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down)) 
                _classIndex = (_classIndex + 1) % _classes.Length;
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            {
                RollAbilities();
                _createStep = 3;
            }

            // Mouse selection for visible classes
            var classesRect = new Rectangle(menuRectC.X + menuWidthC - paddingC - 320, menuRectC.Y + titleH + 52, 320, MAX_VISIBLE_CLASSES * 36 + 10);
            int visibleClassCount = Math.Min(MAX_VISIBLE_CLASSES, _classes.Length - _classScrollOffset);
            
            for (int i = 0; i < visibleClassCount; i++)
            {
                int classIdx = _classScrollOffset + i;
                var item = new Rectangle(classesRect.X + 4, classesRect.Y + i * 36 + 4, 312, 32);
                if (item.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed)
                {
                    _classIndex = classIdx;
                    _createStep = 2;
                }
            }
        }
        // Step 3: Ability roll confirmation
        else if (_createStep == 3)
        {
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            {
                createdCharacter = CreateCharacterFromData();
                return true; // signal character creation complete
            }
            if (kb.IsKeyDown(Keys.R) && !prevKb.IsKeyDown(Keys.R))
            {
                RollAbilities();
            }
        }

        return true; // stay in creation mode
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics)
    {
        var vp = graphics.Viewport;
        
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.9f);

        int menuWidthC = 700;
        int paddingC = 16;
        int titleH = 60;
        var menuRectC = new Rectangle((vp.Width - menuWidthC) / 2, (vp.Height - 400) / 2, menuWidthC, 400);
        spriteBatch.Draw(_pixel, menuRectC, Color.DarkSlateGray * 0.95f);

        if (_font != null)
        {
            var title = "Create Character";
            var tsize = _font.MeasureString(title);
            spriteBatch.DrawString(_font, title, new Vector2(menuRectC.X + (menuWidthC - tsize.X) / 2, menuRectC.Y + 8), Color.White);

            // Back button
            var backRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + paddingC, 80, 30);
            var mouse = Mouse.GetState();
            var backColor = backRect.Contains(mouse.Position) ? Color.LightGray : Color.Gray;
            spriteBatch.Draw(_pixel, backRect, backColor);
            var backText = "< Back";
            var backTextSize = _font.MeasureString(backText);
            spriteBatch.DrawString(_font, backText, new Vector2(backRect.X + (backRect.Width - backTextSize.X) / 2, backRect.Y + (backRect.Height - backTextSize.Y) / 2), Color.White);

            if (_createStep < 3)
            {
                // Name input
                var nameRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH, menuWidthC - paddingC * 2, 40);
                spriteBatch.Draw(_pixel, nameRect, Color.LightGray);
                var displayName = string.IsNullOrEmpty(_createName) ? "Enter name..." : _createName + ((_createStep == 0 && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2) == 0) ? "|" : "");
                spriteBatch.DrawString(_font, displayName, new Vector2(nameRect.X + 8, nameRect.Y + 8), Color.Black);

                // Races list (scrollable)
                var racesRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + titleH + 52, 320, MAX_VISIBLE_RACES * 36 + 10);
                spriteBatch.Draw(_pixel, racesRect, Color.Gray);
                
                int visibleCount = Math.Min(MAX_VISIBLE_RACES, _races.Count - _raceScrollOffset);
                for (int i = 0; i < visibleCount; i++)
                {
                    int raceIdx = _raceScrollOffset + i;
                    var item = new Rectangle(racesRect.X + 4, racesRect.Y + i * 36 + 4, 312, 32);
                    spriteBatch.Draw(_pixel, item, (raceIdx == _raceIndex) ? Color.LightGray : Color.DarkGray);
                    
                    var race = Race.GetRace(_races[raceIdx]);
                    spriteBatch.DrawString(_font, race.DisplayName, new Vector2(item.X + 8, item.Y + 4), (raceIdx == _raceIndex) ? Color.Black : Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                }
                
                // Show race description
                if (_raceIndex >= 0 && _raceIndex < _races.Count)
                {
                    var selectedRace = Race.GetRace(_races[_raceIndex]);
                    var descRect = new Rectangle(racesRect.X, racesRect.Y + racesRect.Height + 8, 320, 60);
                    spriteBatch.Draw(_pixel, descRect, Color.Gray * 0.7f);
                    spriteBatch.DrawString(_font, selectedRace.Description, new Vector2(descRect.X + 8, descRect.Y + 8), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                }

                // Classes list
                var classesRect = new Rectangle(menuRectC.X + menuWidthC - paddingC - 320, menuRectC.Y + titleH + 52, 320, MAX_VISIBLE_CLASSES * 36 + 10);
                spriteBatch.Draw(_pixel, classesRect, Color.Gray);
                
                int visibleClassCount = Math.Min(MAX_VISIBLE_CLASSES, _classes.Length - _classScrollOffset);
                for (int i = 0; i < visibleClassCount; i++)
                {
                    int classIdx = _classScrollOffset + i;
                    var item = new Rectangle(classesRect.X + 4, classesRect.Y + i * 36 + 4, 312, 32);
                    spriteBatch.Draw(_pixel, item, (classIdx == _classIndex) ? Color.LightGray : Color.DarkGray);
                    spriteBatch.DrawString(_font, _classes[classIdx], new Vector2(item.X + 8, item.Y + 4), (classIdx == _classIndex) ? Color.Black : Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                }

                // Hint
                var hint = "Use arrow keys and Enter | Esc to cancel";
                var hsize = _font.MeasureString(hint);
                spriteBatch.DrawString(_font, hint, new Vector2(menuRectC.X + (menuWidthC - hsize.X) / 2, menuRectC.Y + menuRectC.Height - 28), Color.White * 0.7f);
            }
            else if (_createStep == 3)
            {
                // Show rolled abilities
                spriteBatch.DrawString(_font, "Rolled Abilities:", new Vector2(menuRectC.X + paddingC, menuRectC.Y + titleH), Color.White);
                
                for (int i = 0; i < 6; i++)
                {
                    int col = i % 3;
                    int row = i / 3;
                    int x = menuRectC.X + paddingC + col * 220;
                    int y = menuRectC.Y + titleH + 32 + row * 60;
                    
                    var abilityBox = new Rectangle(x, y, 210, 50);
                    spriteBatch.Draw(_pixel, abilityBox, Color.Gray * 0.7f);
                    
                    string abilityText = $"{_abilityNames[i]}: {_rolledAbilities[i]}";
                    int modifier = (_rolledAbilities[i] - 10) / 2;
                    string modText = modifier >= 0 ? $"+{modifier}" : $"{modifier}";
                    
                    spriteBatch.DrawString(_font, abilityText, new Vector2(x + 8, y + 8), Color.White);
                    spriteBatch.DrawString(_font, $"({modText})", new Vector2(x + 8, y + 26), Color.LightGray);
                }
                
                // Show race bonuses
                var selectedRace = Race.GetRace(_races[_raceIndex]);
                spriteBatch.DrawString(_font, $"Racial Bonuses: {selectedRace.DisplayName}", new Vector2(menuRectC.X + paddingC, menuRectC.Y + titleH + 160), Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                
                var hint = "Press R to reroll, Enter to confirm, Esc to cancel";
                var hsize = _font.MeasureString(hint);
                spriteBatch.DrawString(_font, hint, new Vector2(menuRectC.X + (menuWidthC - hsize.X) / 2, menuRectC.Y + menuRectC.Height - 28), Color.White * 0.7f);
            }
        }
    }

    private void RollAbilities()
    {
        var rand = new Random();
        for (int i = 0; i < 6; i++)
        {
            // Roll 4d6, drop lowest (standard method)
            int[] rolls = new int[4];
            for (int j = 0; j < 4; j++)
                rolls[j] = rand.Next(1, 7);
            Array.Sort(rolls);
            _rolledAbilities[i] = rolls[1] + rolls[2] + rolls[3]; // sum top 3
        }
    }

    private Character CreateCharacterFromData()
    {
        var selectedRace = Race.GetRace(_races[_raceIndex]);
        
        var c = new Character 
        { 
            Name = _createName.Trim(), 
            CreatedAt = DateTime.UtcNow, 
            Race = selectedRace.Name, 
            Class = _classes[_classIndex], 
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
        
        // Set hit points based on class and CON modifier
        int conMod = c.GetAbilityModifier(c.Constitution);
        switch (c.Class)
        {
            case "Barbarian":
                c.MaxHP = 12 + conMod;
                c.HitDiceTotal = 1;
                c.StrengthSaveProficiency = true;
                c.ConstitutionSaveProficiency = true;
                c.AthleticsProficiency = true;
                c.IntimidationProficiency = true;
                break;
            case "Bard":
                c.MaxHP = 8 + conMod;
                c.HitDiceTotal = 1;
                c.DexteritySaveProficiency = true;
                c.CharismaSaveProficiency = true;
                c.PerceptionProficiency = true;
                c.PerformanceProficiency = true;
                c.PersuasionProficiency = true;
                break;
            case "Cleric":
                c.MaxHP = 8 + conMod;
                c.HitDiceTotal = 1;
                c.WisdomSaveProficiency = true;
                c.CharismaSaveProficiency = true;
                c.MedicineProficiency = true;
                c.ReligionProficiency = true;
                break;
            case "Druid":
                c.MaxHP = 8 + conMod;
                c.HitDiceTotal = 1;
                c.IntelligenceSaveProficiency = true;
                c.WisdomSaveProficiency = true;
                c.NatureProficiency = true;
                c.SurvivalProficiency = true;
                break;
            case "Fighter":
                c.MaxHP = 10 + conMod;
                c.HitDiceTotal = 1;
                c.StrengthSaveProficiency = true;
                c.ConstitutionSaveProficiency = true;
                c.AthleticsProficiency = true;
                c.IntimidationProficiency = true;
                break;
            case "Monk":
                c.MaxHP = 8 + conMod;
                c.HitDiceTotal = 1;
                c.StrengthSaveProficiency = true;
                c.DexteritySaveProficiency = true;
                c.AcrobaticsProficiency = true;
                c.StealthProficiency = true;
                break;
            case "Paladin":
                c.MaxHP = 10 + conMod;
                c.HitDiceTotal = 1;
                c.WisdomSaveProficiency = true;
                c.CharismaSaveProficiency = true;
                c.AthleticsProficiency = true;
                c.ReligionProficiency = true;
                break;
            case "Ranger":
                c.MaxHP = 10 + conMod;
                c.HitDiceTotal = 1;
                c.StrengthSaveProficiency = true;
                c.DexteritySaveProficiency = true;
                c.SurvivalProficiency = true;
                c.NatureProficiency = true;
                break;
            case "Rogue":
                c.MaxHP = 8 + conMod;
                c.HitDiceTotal = 1;
                c.DexteritySaveProficiency = true;
                c.IntelligenceSaveProficiency = true;
                c.StealthProficiency = true;
                c.SleightOfHandProficiency = true;
                c.AcrobaticsProficiency = true;
                c.PerceptionProficiency = true;
                break;
            case "Sorcerer":
                c.MaxHP = 6 + conMod;
                c.HitDiceTotal = 1;
                c.ConstitutionSaveProficiency = true;
                c.CharismaSaveProficiency = true;
                c.ArcanaProficiency = true;
                c.PersuasionProficiency = true;
                break;
            case "Warlock":
                c.MaxHP = 8 + conMod;
                c.HitDiceTotal = 1;
                c.WisdomSaveProficiency = true;
                c.CharismaSaveProficiency = true;
                c.ArcanaProficiency = true;
                c.DeceptionProficiency = true;
                break;
            case "Wizard":
                c.MaxHP = 6 + conMod;
                c.HitDiceTotal = 1;
                c.IntelligenceSaveProficiency = true;
                c.WisdomSaveProficiency = true;
                c.ArcanaProficiency = true;
                c.InvestigationProficiency = true;
                break;
            case "Warrior":
                c.MaxHP = 10 + conMod;
                c.HitDiceTotal = 1;
                c.StrengthSaveProficiency = true;
                c.ConstitutionSaveProficiency = true;
                c.AthleticsProficiency = true;
                c.IntimidationProficiency = true;
                break;
            case "Mage":
                c.MaxHP = 6 + conMod;
                c.HitDiceTotal = 1;
                c.IntelligenceSaveProficiency = true;
                c.WisdomSaveProficiency = true;
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
        c.HitDiceRemaining = c.HitDiceTotal;
        
        // Calculate AC from equipment
        c.CalculateDerivedStats();
        
        return c;
    }
}
