using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace _4DND;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private InfiniteGrid<bool> _grid = new();
    private Texture2D _pixel = null!; // simple 1x1 white texture
    private int _cellSize = 24;
    private Vector2 _camera = Vector2.Zero; // pixel offset applied to origin
    private float _zoom = 1f;
    private int _prevScrollValue = 0;

    // rotation around world vertical axis (radians)
    private float _rotation = 0f;
    private const float RotationSpeed = MathHelper.PiOver2; // 90 degrees/sec

    // high level state
    private enum AppState { MainMenu, CharacterSelect, CharacterCreate, Playing }
    private AppState _state = AppState.MainMenu;

    // main menu state
    private bool _inMainMenu => _state == AppState.MainMenu;
    private readonly string[] _mainMenuItems = new[] { "New Game", "Continue", "Load Game", "Options", "Desktop" };
    private int _mainMenuIndex = 0;

    // character selection
    private List<Character> _characters = new();
    private int _characterIndex = 0;
    private string _savesDir = "saves";
    private string _charsFile = "characters.json";
    private Character? _currentCharacter = null;

    // character creation
    private string _createName = string.Empty;
    private int _createStep = 0; // 0: name, 1: race, 2: class
    private readonly string[] _races = new[] { "Human", "Elf", "Dwarf" };
    private readonly string[] _classes = new[] { "Warrior", "Mage", "Rogue" };
    private int _raceIndex = 0;
    private int _classIndex = 0;

    // pause menu state
    private bool _isMenuOpen = false;
    private int _menuIndex = 0;
    private readonly string[] _menuItems = new[] { "Continue", "Options", "Main Menu", "Desktop" };

    private KeyboardState _prevKb;
    private SpriteFont _font = null!;
    // input consumption flags
    private bool _escapeHandled = false;
    // UI state
    private bool _showCharacterSheet = false;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _prevKb = Keyboard.GetState();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // create 1x1 white pixel
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // load default font (may not be present if content wasn't built)
        try
        {
            _font = Content.Load<SpriteFont>("DefaultFont");
        }
        catch (Microsoft.Xna.Framework.Content.ContentLoadException ex)
        {
            // Font not found / content not built. Fall back to null and continue running.
            _font = null!;
            System.Console.WriteLine("Warning: DefaultFont not found. Build the Content/Content.mgcb with the MonoGame Pipeline Tool to generate DefaultFont.xnb. Menu text will be hidden.");
        }

        // sample content: a small pattern and a cross
        for (int x = -10; x <= 10; x++)
            _grid.Set(x, 0, x % 2 == 0);

        for (int y = -6; y <= 6; y++)
        {
            _grid.Set(0, y, true);
            _grid.Set(1, y, (y % 3) == 0);
        }

        // ensure saves directory
        try
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir));
        }
        catch { }

        // load characters if present
        LoadCharacters();
    }

    private void LoadCharacters()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _charsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _characters = JsonSerializer.Deserialize<List<Character>>(json) ?? new List<Character>();
            }
            else
            {
                _characters = new List<Character>();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to load characters: " + ex.Message);
            _characters = new List<Character>();
        }
    }

    private void SaveCharacters()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _charsFile);
            var json = JsonSerializer.Serialize(_characters);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to save characters: " + ex.Message);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

        // reset per-frame input consumption
        _escapeHandled = false;

        // MAIN MENU
        if (_state == AppState.MainMenu)
        {
            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _mainMenuIndex = (_mainMenuIndex - 1 + _mainMenuItems.Length) % _mainMenuItems.Length;
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _mainMenuIndex = (_mainMenuIndex + 1) % _mainMenuItems.Length;
            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
                ExecuteMainMenuAction(_mainMenuIndex);

            // mouse hover / click
            var vp = GraphicsDevice.Viewport;
            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 120;
            int menuHeight = titleHeight + _mainMenuItems.Length * (itemHeight + padding) + padding;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            for (int i = 0; i < _mainMenuItems.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                if (itemRect.Contains(mouse.Position))
                {
                    _mainMenuIndex = i;
                    if (mouse.LeftButton == ButtonState.Pressed)
                        ExecuteMainMenuAction(i);
                }
            }

            _prevKb = kb;
            base.Update(gameTime);
            return;
        }

        // CHARACTER SELECT
        if (_state == AppState.CharacterSelect)
        {
            // navigate list
            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _characterIndex = Math.Max(0, _characterIndex - 1);
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _characterIndex = Math.Min(_characters.Count - 1, _characterIndex + 1);
            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
            {
                if (_characters.Count > 0 && _characterIndex < _characters.Count)
                {
                    _currentCharacter = _characters[_characterIndex];
                    _state = AppState.Playing; // start game with selected character
                }
                else
                {
                    // create new -> go to creation
                    _createName = string.Empty;
                    _createStep = 0;
                    _raceIndex = 0;
                    _classIndex = 0;
                    _state = AppState.CharacterCreate;
                }
            }

            // mouse
            var vp = GraphicsDevice.Viewport;
            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 80;
            int menuHeight = titleHeight + Math.Max(1, _characters.Count + 1) * (itemHeight + padding) + padding; // + create new
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            // item 0..count-1 are characters, last is Create New
            for (int i = 0; i < Math.Max(1, _characters.Count + 1); i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                if (itemRect.Contains(mouse.Position))
                {
                    _characterIndex = Math.Min(i, Math.Max(0, _characters.Count - 1));
                    if (mouse.LeftButton == ButtonState.Pressed)
                    {
                        if (i < _characters.Count)
                        {
                            _currentCharacter = _characters[i];
                            _state = AppState.Playing;
                        }
                        else
                        {
                            // go to creation UI
                            _createName = string.Empty;
                            _createStep = 0;
                            _raceIndex = 0;
                            _classIndex = 0;
                            _state = AppState.CharacterCreate;
                        }
                    }
                }
            }

            // if no characters, go to creation
            if (_characters.Count == 0)
            {
                _createName = string.Empty;
                _createStep = 0;
                _raceIndex = 0;
                _classIndex = 0;
                _state = AppState.CharacterCreate;
            }

            _prevKb = kb;
            base.Update(gameTime);
            return;
        }

        // CHARACTER CREATION
        if (_state == AppState.CharacterCreate)
        {
            // typing name (edge-detected keys)
            // handle backspace and simple chars
            if (_createStep == 0)
            {
                foreach (var k in kb.GetPressedKeys())
                {
                    if (!_prevKb.IsKeyDown(k))
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
                        {
                            _createName += '-';
                        }
                        else if (k == Keys.OemPeriod)
                        {
                            _createName += '.';
                        }
                        else if (k == Keys.Enter)
                        {
                            if (!string.IsNullOrWhiteSpace(_createName)) _createStep = 1; // move to race selection
                        }
                    }
                }
            }
            else if (_createStep == 1)
            {
                if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up)) _raceIndex = (_raceIndex - 1 + _races.Length) % _races.Length;
                if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down)) _raceIndex = (_raceIndex + 1) % _races.Length;
                if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter)) _createStep = 2;
            }
            else if (_createStep == 2)
            {
                if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up)) _classIndex = (_classIndex - 1 + _classes.Length) % _classes.Length;
                if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down)) _classIndex = (_classIndex + 1) % _classes.Length;
                if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
                {
                    // finish creation
                    var c = new Character { Name = _createName.Trim(), CreatedAt = DateTime.UtcNow, Race = _races[_raceIndex], Class = _classes[_classIndex], Level = 1, XP = 0 };
                    _characters.Add(c);
                    SaveCharacters();
                    _currentCharacter = c;
                    _state = AppState.Playing;
                }
            }

            // mouse interaction for race/class and confirm button
            var vpCreate = GraphicsDevice.Viewport;
            int menuWidthC = 600;
            int paddingC = 16;
            int titleH = 60;
            int rowH = 40;
            var menuRectC = new Rectangle((vpCreate.Width - menuWidthC) / 2, (vpCreate.Height - 300) / 2, menuWidthC, 300);

            // name input box area
            var nameRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + 12, menuWidthC - paddingC * 2, 40);
            if (nameRect.Contains(mouse.Position))
            {
                // click focuses name
                if (mouse.LeftButton == ButtonState.Pressed) _createStep = 0;
            }

            // races list
            var racesRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + 70, 200, 120);
            for (int i = 0; i < _races.Length; i++)
            {
                var item = new Rectangle(racesRect.X, racesRect.Y + i * (rowH), racesRect.Width, rowH - 4);
                if (item.Contains(mouse.Position))
                {
                    if (mouse.LeftButton == ButtonState.Pressed) { _raceIndex = i; _createStep = 1; }
                }
            }

            // classes list
            var classesRect = new Rectangle(menuRectC.X + menuWidthC - paddingC - 200, menuRectC.Y + 70, 200, 120);
            for (int i = 0; i < _classes.Length; i++)
            {
                var item = new Rectangle(classesRect.X, classesRect.Y + i * (rowH), classesRect.Width, rowH - 4);
                if (item.Contains(mouse.Position))
                {
                    if (mouse.LeftButton == ButtonState.Pressed) { _classIndex = i; _createStep = 2; }
                }
            }

            _prevKb = kb;
            base.Update(gameTime);
            return;
        }

        // Normal gameplay and pause menu handling (game running)
        // toggle pause menu on Escape (edge detect)
        if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
        {
            _isMenuOpen = !_isMenuOpen;
            if (_isMenuOpen) _menuIndex = 0;
            _escapeHandled = true;
        }

        // if pause menu is open, handle it and skip game controls
        if (_isMenuOpen)
        {
            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _menuIndex = (_menuIndex - 1 + _menuItems.Length) % _menuItems.Length;
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _menuIndex = (_menuIndex + 1) % _menuItems.Length;
            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
                ExecuteMenuAction(_menuIndex);

            // mouse hover/select for pause menu
            var vp2 = GraphicsDevice.Viewport;
            int menuWidth2 = 360;
            int itemHeight2 = 48;
            int padding2 = 12;
            int menuHeight2 = _menuItems.Length * (itemHeight2 + padding2) + padding2;
            var menuRect2 = new Rectangle((vp2.Width - menuWidth2) / 2, (vp2.Height - menuHeight2) / 2, menuWidth2, menuHeight2);

            for (int i = 0; i < _menuItems.Length; i++)
            {
                var itemRect = new Rectangle(menuRect2.X + padding2, menuRect2.Y + padding2 + i * (itemHeight2 + padding2), menuWidth2 - padding2 * 2, itemHeight2);
                if (itemRect.Contains(mouse.Position))
                {
                    _menuIndex = i;
                    if (mouse.LeftButton == ButtonState.Pressed)
                        ExecuteMenuAction(i);
                }
            }

            _prevKb = kb;
            base.Update(gameTime);
            return; // skip gameplay while menu open
        }

        // toggle character sheet with C (only when playing and not paused)
        if (_state == AppState.Playing)
        {
            if (kb.IsKeyDown(Keys.C) && !_prevKb.IsKeyDown(Keys.C))
            {
                _showCharacterSheet = !_showCharacterSheet;
            }

            // if the sheet is open, consume input and skip gameplay updates
            if (_showCharacterSheet)
            {
                _prevKb = kb;
                base.Update(gameTime);
                return;
            }
        }

        // Exit only via gamepad Back button; Escape reserved for pause/menu
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float speed = 400f * dt;

        if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A)) _camera.X += speed;
        if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) _camera.X -= speed;
        if (kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W)) _camera.Y += speed;
        if (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S)) _camera.Y -= speed;

        // Rotation with Q and E (around world vertical axis)
        if (kb.IsKeyDown(Keys.Q)) _rotation -= RotationSpeed * dt;
        if (kb.IsKeyDown(Keys.E)) _rotation += RotationSpeed * dt;

        // normalize rotation to -PI..PI
        if (_rotation > MathHelper.Pi) _rotation -= MathHelper.TwoPi;
        if (_rotation < -MathHelper.Pi) _rotation += MathHelper.TwoPi;

        // Handle mouse wheel zoom
        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0)
        {
            _zoom += scrollDelta * 0.001f;
            _zoom = MathHelper.Clamp(_zoom, 0.1f, 5f);
            _prevScrollValue = mouse.ScrollWheelValue;
        }

        _prevKb = kb;
        base.Update(gameTime);
    }

    private void ExecuteMainMenuAction(int index)
    {
        var sel = _mainMenuItems[index];
        if (sel == "New Game")
        {
            // go to character creation UI
            _createName = string.Empty;
            _createStep = 0;
            _raceIndex = 0;
            _classIndex = 0;
            _state = AppState.CharacterCreate;
        }
        else if (sel == "Continue")
        {
            // if there is a saved current character, start; otherwise go to character select
            if (_currentCharacter != null)
            {
                _state = AppState.Playing;
            }
            else
            {
                LoadCharacters();
                _state = AppState.CharacterSelect;
            }
        }
        else if (sel == "Load Game")
        {
            LoadCharacters();
            _state = AppState.CharacterSelect;
        }
        else if (sel == "Options")
        {
            // open pause/options menu UI or implement options
            _isMenuOpen = true;
            _menuIndex = 1; // point to Options in pause menu
        }
        else if (sel == "Desktop")
        {
            Exit();
        }
    }

    private void ExecuteMenuAction(int index)
    {
        var sel = _menuItems[index];
        if (sel == "Continue")
        {
            _isMenuOpen = false;
        }
        else if (sel == "Options")
        {
            // placeholder
        }
        else if (sel == "Main Menu")
        {
            // go back to main menu
            _state = AppState.MainMenu;
            _isMenuOpen = false;
        }
        else if (sel == "Desktop")
        {
            Exit();
        }
    }

    private void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        float distance = Vector2.Distance(start, end);
        float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        sb.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(distance, thickness), SpriteEffects.None, 0f);
    }

    private class Character
    {
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Race { get; set; } = "";
        public string Class { get; set; } = "";
        public int Level { get; set; }
        public int XP { get; set; }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var vp = GraphicsDevice.Viewport;
        var screenCenter = new Vector2(vp.Width / 2f, vp.Height / 2f);
        var origin = screenCenter + _camera;

        // If in main menu, draw it and return
        if (_state == AppState.MainMenu)
        {
            // full-screen dim background
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 120;
            int menuHeight = titleHeight + _mainMenuItems.Length * (itemHeight + padding) + padding;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            // panel
            _spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

            // Title
            if (_font != null)
            {
                string title = "4DND"; // game title
                var titleSize = _font.MeasureString(title);
                var titlePos = new Vector2(menuRect.X + (menuWidth - titleSize.X) / 2, menuRect.Y + 12);
                _spriteBatch.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            }
            else
            {
                // placeholder title bar
                var titleBar = new Rectangle(menuRect.X + 12, menuRect.Y + 12, menuWidth - 24, titleHeight - 24);
                _spriteBatch.Draw(_pixel, titleBar, Color.Gray);
            }

            for (int i = 0; i < _mainMenuItems.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (i == _mainMenuIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                // text
                if (_font != null)
                {
                    var text = _mainMenuItems[i];
                    var size = _font.MeasureString(text);
                    var pos = new Vector2(itemRect.X + (itemRect.Width - size.X) / 2, itemRect.Y + (itemRect.Height - size.Y) / 2);
                    var textCol = (i == _mainMenuIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, text, pos, textCol);
                }
                else
                {
                    // placeholder block already drawn
                }
            }

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // Character select UI
        if (_state == AppState.CharacterSelect)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 80;
            int menuHeight = titleHeight + Math.Max(1, _characters.Count + 1) * (itemHeight + padding) + padding;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            _spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

            if (_font != null)
            {
                var title = "Choose a Character";
                var size = _font.MeasureString(title);
                var pos = new Vector2(menuRect.X + (menuWidth - size.X) / 2, menuRect.Y + 12);
                _spriteBatch.DrawString(_font, title, pos, Color.White);
            }

            for (int i = 0; i < Math.Max(1, _characters.Count + 1); i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (i == _characterIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    string label = (i < _characters.Count) ? _characters[i].Name : "Create New";
                    var m = _font.MeasureString(label);
                    var p = new Vector2(itemRect.X + (itemRect.Width - m.X) / 2, itemRect.Y + (itemRect.Height - m.Y) / 2);
                    var textCol = (i == _characterIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, label, p, textCol);
                }
            }

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // Character creation UI
        if (_state == AppState.CharacterCreate)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.9f);

            int menuWidthC = 600;
            int paddingC = 16;
            int titleH = 60;
            int rowH = 40;
            var menuRectC = new Rectangle((vp.Width - menuWidthC) / 2, (vp.Height - 300) / 2, menuWidthC, 300);
            _spriteBatch.Draw(_pixel, menuRectC, Color.DarkSlateGray * 0.95f);

            if (_font != null)
            {
                var title = "Create Character";
                var tsize = _font.MeasureString(title);
                _spriteBatch.DrawString(_font, title, new Vector2(menuRectC.X + (menuWidthC - tsize.X) / 2, menuRectC.Y + 8), Color.White);

                // name input
                var nameRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + 48, menuWidthC - paddingC * 2, 40);
                _spriteBatch.Draw(_pixel, nameRect, Color.LightGray);
                var displayName = string.IsNullOrEmpty(_createName) ? "Enter name..." : _createName + ((_createStep == 0 && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2) == 0) ? "|" : "");
                _spriteBatch.DrawString(_font, displayName, new Vector2(nameRect.X + 8, nameRect.Y + 8), Color.Black);

                // races
                var racesRect = new Rectangle(menuRectC.X + paddingC, menuRectC.Y + 100, 200, 120);
                _spriteBatch.Draw(_pixel, racesRect, Color.Gray);
                for (int i = 0; i < _races.Length; i++)
                {
                    var item = new Rectangle(racesRect.X + 4, racesRect.Y + i * 40 + 4, racesRect.Width - 8, 36);
                    _spriteBatch.Draw(_pixel, item, (i == _raceIndex) ? Color.LightGray : Color.DarkGray);
                    _spriteBatch.DrawString(_font, _races[i], new Vector2(item.X + 8, item.Y + 6), (i == _raceIndex) ? Color.Black : Color.White);
                }

                // classes
                var classesRect = new Rectangle(menuRectC.X + menuWidthC - paddingC - 200, menuRectC.Y + 100, 200, 120);
                _spriteBatch.Draw(_pixel, classesRect, Color.Gray);
                for (int i = 0; i < _classes.Length; i++)
                {
                    var item = new Rectangle(classesRect.X + 4, classesRect.Y + i * 40 + 4, classesRect.Width - 8, 36);
                    _spriteBatch.Draw(_pixel, item, (i == _classIndex) ? Color.LightGray : Color.DarkGray);
                    _spriteBatch.DrawString(_font, _classes[i], new Vector2(item.X + 8, item.Y + 6), (i == _classIndex) ? Color.Black : Color.White);
                }

                // hint
                var hint = "Press Enter to advance / confirm";
                var hsize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(menuRectC.X + (menuWidthC - hsize.X) / 2, menuRectC.Y + menuRectC.Height - 28), Color.White * 0.7f);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        float tileW = _cellSize * _zoom;
        float tileH = _cellSize * 0.5f * _zoom;

        // compute hovered tile under mouse (only when not in main menu)
        if (!_inMainMenu)
        {
            var mouse = Mouse.GetState();
            var mp = mouse.Position.ToVector2();
            // origin is center + camera
            var rel = mp - origin;

            float a = tileW * 0.5f; // px per (x - y)
            float b = tileH * 0.5f; // px per (x + y)

            if (a != 0 && b != 0)
            {
                float rx = rel.X;
                float ry = rel.Y;
                float wx = ((rx / a) + (ry / b)) * 0.5f;
                float wy = ((ry / b) - (rx / a)) * 0.5f;

                // use floor to get the tile indices (tile lies between grid lines)
                int tx = (int)System.Math.Floor(wx);
                int ty = (int)System.Math.Floor(wy);

                // center of that tile in screen space at (tx+0.5, ty+0.5)
                var center = origin + new Vector2(((tx + 0.5f) - (ty + 0.5f)) * a, ((tx + 0.5f) + (ty + 0.5f)) * b);

                // diamond corners
                var top = center + new Vector2(0, -b);
                var right = center + new Vector2(a, 0);
                var bottom = center + new Vector2(0, b);
                var left = center + new Vector2(-a, 0);

                // draw outline on top of grid
                DrawLine(_spriteBatch, _pixel, top, right, Color.Yellow, 3f);
                DrawLine(_spriteBatch, _pixel, right, bottom, Color.Yellow, 3f);
                DrawLine(_spriteBatch, _pixel, bottom, left, Color.Yellow, 3f);
                DrawLine(_spriteBatch, _pixel, left, top, Color.Yellow, 3f);
            }
        }

        int range = (int)((Math.Max(vp.Width, vp.Height) / Math.Min(tileW, tileH))) + 6;
        int xmin = -range, xmax = range;
        int ymin = -range, ymax = range;

        // draw grid lines in isometric projection
        for (int y = ymin; y <= ymax; y++)
        {
            var start = origin + new Vector2((xmin - y) * tileW * 0.5f, (xmin + y) * tileH * 0.5f);
            var end = origin + new Vector2((xmax - y) * tileW * 0.5f, (xmax + y) * tileH * 0.5f);
            DrawLine(_spriteBatch, _pixel, start, end, Color.White, 1f);
        }

        for (int x = xmin; x <= xmax; x++)
        {
            var start = origin + new Vector2((x - ymin) * tileW * 0.5f, (x + ymin) * tileH * 0.5f);
            var end = origin + new Vector2((x - ymax) * tileW * 0.5f, (x + ymax) * tileH * 0.5f);
            DrawLine(_spriteBatch, _pixel, start, end, Color.White, 1f);
        }

        var originCenter = origin + new Vector2(0, 0);
        float axisLength = 100f * _zoom;
        DrawLine(_spriteBatch, _pixel, originCenter, originCenter + new Vector2(axisLength * 0.5f, axisLength * 0.25f), Color.Red, 2f);
        DrawLine(_spriteBatch, _pixel, originCenter, originCenter + new Vector2(-axisLength * 0.5f, axisLength * 0.25f), Color.Lime, 2f);
        DrawLine(_spriteBatch, _pixel, originCenter, originCenter + new Vector2(0, -axisLength), Color.Blue, 2f);
        _spriteBatch.Draw(_pixel, new Rectangle((int)originCenter.X - 2, (int)originCenter.Y - 2, 4, 4), Color.Red);

        // draw pause menu overlay if open
        if (_isMenuOpen)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.6f);

            int menuWidth2 = 360;
            int itemHeight2 = 48;
            int padding2 = 12;
            int menuHeight2 = _menuItems.Length * (itemHeight2 + padding2) + padding2;
            var menuRect2 = new Rectangle((vp.Width - menuWidth2) / 2, (vp.Height - menuHeight2) / 2, menuWidth2, menuHeight2);

            _spriteBatch.Draw(_pixel, menuRect2, Color.DarkSlateGray * 0.95f);

            for (int i = 0; i < _menuItems.Length; i++)
            {
                var itemRect = new Rectangle(menuRect2.X + padding2, menuRect2.Y + padding2 + i * (itemHeight2 + padding2), menuWidth2 - padding2 * 2, itemHeight2);
                var col = (i == _menuIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);
                var barRect = new Rectangle(itemRect.X + 6, itemRect.Y + 6, 8, itemRect.Height - 12);
                _spriteBatch.Draw(_pixel, barRect, (i == _menuIndex) ? Color.Orange : Color.DarkOrange);
                if (_font != null)
                {
                    var textPos = new Vector2(itemRect.X + 24 + 8, itemRect.Y + (itemRect.Height - _font.LineSpacing) / 2);
                    var textCol = (i == _menuIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, _menuItems[i], textPos, textCol);
                }
                else
                {
                    var placeholderRect = new Rectangle(itemRect.X + 24, itemRect.Y + 8, itemRect.Width - 24 - 16, itemRect.Height - 16);
                    _spriteBatch.Draw(_pixel, placeholderRect, (i == _menuIndex) ? Color.Black * 0.7f : Color.White * 0.2f);
                }
            }
        }

        // draw character sheet if requested
        if (_showCharacterSheet && _state == AppState.Playing && _currentCharacter != null)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.75f);
            int sheetW = 480;
            int sheetH = 260;
            var sheetRect = new Rectangle((vp.Width - sheetW) / 2, (vp.Height - sheetH) / 2, sheetW, sheetH);
            _spriteBatch.Draw(_pixel, sheetRect, Color.DarkSlateGray * 0.98f);

            if (_font != null)
            {
                int y = sheetRect.Y + 12;
                _spriteBatch.DrawString(_font, _currentCharacter.Name, new Vector2(sheetRect.X + 16, y), Color.White);
                y += 28;
                _spriteBatch.DrawString(_font, $"Race: {_currentCharacter.Race}", new Vector2(sheetRect.X + 16, y), Color.White);
                y += 20;
                _spriteBatch.DrawString(_font, $"Class: {_currentCharacter.Class}", new Vector2(sheetRect.X + 16, y), Color.White);
                y += 20;
                _spriteBatch.DrawString(_font, $"Level: {_currentCharacter.Level}", new Vector2(sheetRect.X + 16, y), Color.White);
                y += 20;
                _spriteBatch.DrawString(_font, $"XP: {_currentCharacter.XP}", new Vector2(sheetRect.X + 16, y), Color.White);
                y += 28;
                // placeholder for attributes/stats
                _spriteBatch.DrawString(_font, "STR: 10   DEX: 10   CON: 10", new Vector2(sheetRect.X + 16, y), Color.White * 0.9f);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
