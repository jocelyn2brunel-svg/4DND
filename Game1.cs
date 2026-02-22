using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace _4DND;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private InfiniteGrid<bool> _grid = new();
    private Texture2D _pixel = null!;
    private int _cellSize = 24;
    private Vector2 _camera = Vector2.Zero;
    private float _zoom = 1f;
    private int _prevScrollValue = 0;

    private float _rotation = 0f;
    private const float RotationSpeed = MathHelper.PiOver2;

    private enum AppState { MainMenu, CharacterSelect, CharacterCreate, Playing }
    private AppState _state = AppState.MainMenu;

    private bool _inMainMenu => _state == AppState.MainMenu;
    private readonly string[] _mainMenuItems = new[] { "New Game", "Continue", "Load Game", "Options", "Desktop" };
    private int _mainMenuIndex = 0;

    private List<Character> _characters = new();
    private int _characterIndex = 0;
    private string _savesDir = "saves";
    private string _charsFile = "characters.json";
    private Character? _currentCharacter = null;

    private CharacterCreation _characterCreation = null!;
    private CharacterSheet _characterSheet = null!;

    private bool _isMenuOpen = false;
    private int _menuIndex = 0;
    private readonly string[] _menuItems = new[] { "Continue", "Options", "Main Menu", "Desktop" };

    private KeyboardState _prevKb;
    private SpriteFont _font = null!;

    private bool _escapeHandled = false;
    private bool _showCharacterSheet = false;

    private CombatManager _combatManager = new();
    private Creature? _playerCreature = null;
    private List<string> _combatLog = new();
    private const int MAX_COMBAT_LOG = 5;
    
    // Vision and lighting system
    private VisionSystem _visionSystem = new();
    private bool _showVisionOverlay = true;
    
    // Combat UI state
    private enum CombatAction { None, Move, Attack, EndTurn }
    private CombatAction _selectedAction = CombatAction.None;
    private bool _showCombatUI = false;
    private bool _turnActionExecuted = false;
    private MouseState _prevMouse;

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
        _prevMouse = Mouse.GetState();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            _font = Content.Load<SpriteFont>("DefaultFont");
        }
        catch (Microsoft.Xna.Framework.Content.ContentLoadException ex)
        {
            _font = null!;
            System.Console.WriteLine("Warning: DefaultFont not found. Build the Content/Content.mgcb with the MonoGame Pipeline Tool to generate DefaultFont.xnb. Menu text will be hidden.");
        }

        _characterCreation = new CharacterCreation(_font, _pixel);
        _characterSheet = new CharacterSheet(_font, _pixel);

        for (int x = -10; x <= 10; x++)
            _grid.Set(x, 0, x % 2 == 0);

        for (int y = -6; y <= 6; y++)
        {
            _grid.Set(0, y, true);
            _grid.Set(1, y, (y % 3) == 0);
        }
        
        // Spawn some test enemies
        SpawnTestEnemies();

        try
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir));
        }
        catch { }

        LoadCharacters();
    }
    
    private void SpawnTestEnemies()
    {
        // Don't spawn enemies yet - wait for player to enter game
    }
    
    private void StartCombatWithNearbyEnemies()
    {
        if (_currentCharacter == null || _playerCreature == null) return;
        
        var combatants = new List<Creature>();
        combatants.Add(_playerCreature);
        
        // Add 2-3 random enemies nearby
        var rand = new Random();
        int numEnemies = rand.Next(2, 4);
        
        for (int i = 0; i < numEnemies; i++)
        {
            int enemyX = _playerCreature.X + rand.Next(-3, 4);
            int enemyY = _playerCreature.Y + rand.Next(-3, 4);
            
            int enemyType = rand.Next(0, 5);
            Creature enemy = enemyType switch
            {
                0 => Creature.CreateGoblin(enemyX, enemyY),
                1 => Creature.CreateOrc(enemyX, enemyY),
                2 => Creature.CreateSkeleton(enemyX, enemyY),
                3 => Creature.CreateWolf(enemyX, enemyY),
                _ => Creature.CreateKobold(enemyX, enemyY)
            };
            
            combatants.Add(enemy);
        }
        
        _combatManager.StartCombat(combatants);
        _showCombatUI = true;
        _turnActionExecuted = false;
        AddToCombatLog("Combat started!");
        
        // Setup lighting for combat
        SetupCombatLighting();
    }
    
    private void SetupCombatLighting()
    {
        _visionSystem.ClearLightSources();
        
        // Add torches at strategic locations
        if (_playerCreature != null)
        {
            // Player carries a torch
            var torch = LightSource.Torch(_playerCreature.X, _playerCreature.Y);
            torch.AttachedTo = _playerCreature;
            _visionSystem.AddLightSource(torch);
        }
        
        // Add some ambient light sources
        var rand = new Random();
        for (int i = 0; i < 2; i++)
        {
            int lx = rand.Next(-10, 11);
            int ly = rand.Next(-10, 11);
            _visionSystem.AddLightSource(LightSource.Lantern(lx, ly));
        }
        
        UpdateVision();
    }
    
    private void UpdateVision()
    {
        if (_playerCreature != null)
        {
            // Update positions of attached light sources
            foreach (var light in _visionSystem._lightSources)
            {
                if (light.AttachedTo != null)
                {
                    light.X = light.AttachedTo.X;
                    light.Y = light.AttachedTo.Y;
                }
            }
            
            _visionSystem.CalculateLighting();
            _visionSystem.CalculateVisibility(_playerCreature);
        }
    }
    
    private void AddToCombatLog(string message)
    {
        _combatLog.Add(message);
        if (_combatLog.Count > MAX_COMBAT_LOG)
            _combatLog.RemoveAt(0);
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
    
    private bool IsExistingCharacterIndex(int index)
    {
        return _characters != null && index >= 0 && index < _characters.Count;
    }
    
    private int GetCharacterMenuItemCount()
    {
        return Math.Max(1, (_characters?.Count ?? 0) + 1);
    }
    
    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

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
            if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
            {
                _state = AppState.MainMenu;
                _prevKb = kb;
                base.Update(gameTime);
                return;
            }

            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _characterIndex = Math.Max(0, _characterIndex - 1);
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _characterIndex = Math.Min(_characters.Count, _characterIndex + 1);
            
            if (kb.IsKeyDown(Keys.Delete) && !_prevKb.IsKeyDown(Keys.Delete))
            {
                if (IsExistingCharacterIndex(_characterIndex))
                {
                    _characters.RemoveAt(_characterIndex);
                    SaveCharacters();
                    if (_characterIndex >= _characters.Count)
                        _characterIndex = Math.Max(0, _characters.Count - 1);
                }
            }

            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
            {
                if (IsExistingCharacterIndex(_characterIndex))
                {
                    _currentCharacter = _characters[_characterIndex];
                    _state = AppState.Playing;
                }
                else
                {
                    _characterCreation.Reset();
                    _state = AppState.CharacterCreate;
                }
            }

            var vp = GraphicsDevice.Viewport;
            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 80;
            int menuHeight = titleHeight + GetCharacterMenuItemCount() * (itemHeight + padding) + padding;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            var backRect = new Rectangle(menuRect.X + padding, menuRect.Y + padding, 80, 30);
            if (backRect.Contains(mouse.Position))
            {
                if (mouse.LeftButton == ButtonState.Pressed)
                {
                    _state = AppState.MainMenu;
                    _prevKb = kb;
                    base.Update(gameTime);
                    return;
                }
            }

            bool clickedDelete = false;

            for (int i = 0; i < _characters.Count; i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var deleteRect = new Rectangle(itemRect.X + itemRect.Width - 70, itemRect.Y + (itemRect.Height - 30) / 2, 60, 30);
                
                if (deleteRect.Contains(mouse.Position))
                {
                    if (mouse.LeftButton == ButtonState.Pressed)
                    {
                        _characters.RemoveAt(i);
                        SaveCharacters();
                        if (_characterIndex >= _characters.Count)
                            _characterIndex = Math.Max(0, _characters.Count - 1);
                        clickedDelete = true;
                        break;
                    }
                }
            }

            if (!clickedDelete)
            {
                for (int i = 0; i < GetCharacterMenuItemCount(); i++)
                {
                    var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _characterIndex = i;
                        
                        if (mouse.LeftButton == ButtonState.Pressed)
                        {
                            if (IsExistingCharacterIndex(i))
                            {
                                _currentCharacter = _characters[i];
                                _state = AppState.Playing;
                            }
                            else
                            {
                                _characterCreation.Reset();
                                _state = AppState.CharacterCreate;
                            }
                        }
                        break;
                    }
                }
            }

            _prevKb = kb;
            base.Update(gameTime);
            return;
        }

        // CHARACTER CREATION
        if (_state == AppState.CharacterCreate)
        {
            bool continueCreation = _characterCreation.Update(gameTime, GraphicsDevice, kb, _prevKb, out Character? newCharacter);
            
            if (!continueCreation)
            {
                LoadCharacters();
                _state = AppState.CharacterSelect;
            }
            else if (newCharacter != null)
            {
                _characters.Add(newCharacter);
                SaveCharacters();
                _currentCharacter = newCharacter;
                _state = AppState.Playing;
            }

            _prevKb = kb;
            base.Update(gameTime);
            return;
        }

        // Normal gameplay and pause menu handling
        if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
        {
            _isMenuOpen = !_isMenuOpen;
            if (_isMenuOpen) _menuIndex = 0;
            _escapeHandled = true;
        }

        if (_isMenuOpen)
        {
            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _menuIndex = (_menuIndex - 1 + _menuItems.Length) % _menuItems.Length;
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _menuIndex = (_menuIndex + 1) % _menuItems.Length;
            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
                ExecuteMenuAction(_menuIndex);

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
            return;
        }

        if (_state == AppState.Playing)
        {
            if (kb.IsKeyDown(Keys.C) && !_prevKb.IsKeyDown(Keys.C))
            {
                _showCharacterSheet = !_showCharacterSheet;
                if (_showCharacterSheet)
                {
                    _characterSheet.ResetScroll();
                }
            }
            
            // Toggle vision overlay with V
            if (kb.IsKeyDown(Keys.V) && !_prevKb.IsKeyDown(Keys.V))
            {
                _showVisionOverlay = !_showVisionOverlay;
            }
            
            // Toggle daylight with L
            if (kb.IsKeyDown(Keys.L) && !_prevKb.IsKeyDown(Keys.L))
            {
                _visionSystem.GlobalDaylight = !_visionSystem.GlobalDaylight;
                UpdateVision();
            }
            
            // Test: Toggle Blinded condition with B (for testing)
            if (kb.IsKeyDown(Keys.B) && !_prevKb.IsKeyDown(Keys.B) && _playerCreature != null)
            {
                if (_playerCreature.Conditions.HasCondition(Condition.Blinded))
                {
                    _playerCreature.Conditions = _playerCreature.Conditions.RemoveCondition(Condition.Blinded);
                    AddToCombatLog("Blindness removed!");
                }
                else
                {
                    _playerCreature.Conditions = _playerCreature.Conditions.AddCondition(Condition.Blinded);
                    AddToCombatLog("You are blinded!");
                }
                UpdateVision();
            }
            
            // Test: Create Fog Cloud with F
            if (kb.IsKeyDown(Keys.F) && !_prevKb.IsKeyDown(Keys.F) && _playerCreature != null)
            {
                var fogCloud = AreaEffect.FogCloud(_playerCreature.X, _playerCreature.Y);
                _visionSystem.AddAreaEffect(fogCloud);
                AddToCombatLog("Fog Cloud created!");
                UpdateVision();
            }
            
            // Test: Create Darkness with K
            if (kb.IsKeyDown(Keys.K) && !_prevKb.IsKeyDown(Keys.K) && _playerCreature != null)
            {
                var darkness = AreaEffect.Darkness(_playerCreature.X, _playerCreature.Y);
                _visionSystem.AddAreaEffect(darkness);
                AddToCombatLog("Darkness spell cast!");
                UpdateVision();
            }
            
            // Toggle combat UI with Tab
            if (kb.IsKeyDown(Keys.Tab) && !_prevKb.IsKeyDown(Keys.Tab))
            {
                if (!_combatManager.InCombat && _currentCharacter != null)
                {
                    // Create player creature and start combat
                    _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                    StartCombatWithNearbyEnemies();
                }
                else
                {
                    _showCombatUI = !_showCombatUI;
                }
            }

            if (_showCharacterSheet)
            {
                _characterSheet.Update(mouse);
                _prevKb = kb;
                base.Update(gameTime);
                return;
            }
            
            // Combat controls
            if (_combatManager.InCombat && _showCombatUI)
            {
                var currentCombatant = _combatManager.CurrentCombatant;
                
                if (currentCombatant != null && currentCombatant.IsPlayer)
                {
                    // Player's turn
                    if (kb.IsKeyDown(Keys.D1) && !_prevKb.IsKeyDown(Keys.D1))
                        _selectedAction = CombatAction.Move;
                    if (kb.IsKeyDown(Keys.D2) && !_prevKb.IsKeyDown(Keys.D2))
                        _selectedAction = CombatAction.Attack;
                    if (kb.IsKeyDown(Keys.D3) && !_prevKb.IsKeyDown(Keys.D3))
                    {
                        // End turn
                        _combatManager.NextTurn();
                        AddToCombatLog($"{currentCombatant.Name} ended turn");
                        _selectedAction = CombatAction.None;
                        _turnActionExecuted = false;
                    }
                    
                    // Handle attack action
                    if (_selectedAction == CombatAction.Attack)
                    {
                        // Click on grid to attack
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            var vp = GraphicsDevice.Viewport;
                            var screenCenter = new Vector2(vp.Width / 2f, vp.Height / 2f);
                            var origin = screenCenter + _camera;
                            
                            var mp = mouse.Position.ToVector2();
                            var rel = mp - origin;
                            
                            float tileW = _cellSize * _zoom;
                            float tileH = _cellSize * 0.5f * _zoom;
                            float a = tileW * 0.5f;
                            float b = tileH * 0.5f;
                            
                            if (a != 0 && b != 0)
                            {
                                float rx = rel.X;
                                float ry = rel.Y;
                                float wx = ((rx / a) + (ry / b)) * 0.5f;
                                float wy = ((ry / b) - (rx / a)) * 0.5f;
                                
                                int tx = (int)Math.Floor(wx);
                                int ty = (int)Math.Floor(wy);
                                
                                var target = _combatManager.GetCreatureAt(tx, ty);
                                if (target != null && !target.IsPlayer)
                                {
                                    var result = _combatManager.MakeAttack(currentCombatant, target, _visionSystem);
                                    AddToCombatLog(result.GetMessage());
                                    _selectedAction = CombatAction.None;
                                    
                                    // Auto end turn after attack
                                    _combatManager.NextTurn();
                                    _turnActionExecuted = false;
                                    
                                    // Check if combat ended
                                    if (!_combatManager.InCombat)
                                    {
                                        AddToCombatLog("Combat ended!");
                                        _showCombatUI = false;
                                        if (_playerCreature != null && _currentCharacter != null)
                                        {
                                            _playerCreature.UpdateCharacter(_currentCharacter);
                                            SaveCharacters();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    // Handle move action
                    if (_selectedAction == CombatAction.Move)
                    {
                        // Simple: click to move
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            var vp = GraphicsDevice.Viewport;
                            var screenCenter = new Vector2(vp.Width / 2f, vp.Height / 2f);
                            var origin = screenCenter + _camera;
                            
                            var mp = mouse.Position.ToVector2();
                            var rel = mp - origin;
                            
                            float tileW = _cellSize * _zoom;
                            float tileH = _cellSize * 0.5f * _zoom;
                            float a = tileW * 0.5f;
                            float b = tileH * 0.5f;
                            
                            if (a != 0 && b != 0)
                            {
                                float rx = rel.X;
                                float ry = rel.Y;
                                float wx = ((rx / a) + (ry / b)) * 0.5f;
                                float wy = ((ry / b) - (rx / a)) * 0.5f;
                                
                                int tx = (int)Math.Floor(wx);
                                int ty = (int)Math.Floor(wy);
                                
                                // Check if tile is empty and within movement range
                                if (_combatManager.GetCreatureAt(tx, ty) == null)
                                {
                                    int dist = Math.Abs(tx - currentCombatant.X) + Math.Abs(ty - currentCombatant.Y);
                                    int maxMove = currentCombatant.Speed / 5; // 5 feet per tile
                        
                                    if (dist <= maxMove)
                                    {
                                        currentCombatant.X = tx;
                                        currentCombatant.Y = ty;
                                        AddToCombatLog($"{currentCombatant.Name} moved to ({tx}, {ty})");
                                        _selectedAction = CombatAction.None;
                                        
                                        // Update vision after movement
                                        UpdateVision();
                                    }
                                }
                            }
                        }
                    }
                }
                else if (currentCombatant != null && !currentCombatant.IsPlayer && !_turnActionExecuted)
                {
                    // AI turn - execute once per turn
                    _turnActionExecuted = true;
                    var playerCreature = _combatManager.Combatants.FirstOrDefault(c => c.IsPlayer);
                    
                    if (playerCreature != null)
                    {
                        if (_combatManager.IsInMeleeRange(currentCombatant, playerCreature))
                        {
                            // Attack
                            var result = _combatManager.MakeAttack(currentCombatant, playerCreature, _visionSystem);
                            AddToCombatLog(result.GetMessage());
                        }
                        else
                        {
                            // Move towards player
                            int dx = Math.Sign(playerCreature.X - currentCombatant.X);
                            int dy = Math.Sign(playerCreature.Y - currentCombatant.Y);
                            
                            int newX = currentCombatant.X + dx;
                            int newY = currentCombatant.Y + dy;
                            
                            if (_combatManager.GetCreatureAt(newX, newY) == null)
                            {
                                currentCombatant.X = newX;
                                currentCombatant.Y = newY;
                                AddToCombatLog($"{currentCombatant.Name} moved");
                                UpdateVision();
                            }
                        }
                        
                        // End AI turn
                        _combatManager.NextTurn();
                        _turnActionExecuted = false;
                        
                        // Check if combat ended
                        if (!_combatManager.InCombat)
                        {
                            AddToCombatLog("Combat ended!");
                            _showCombatUI = false;
                            if (_playerCreature != null && _currentCharacter != null)
                            {
                                _playerCreature.UpdateCharacter(_currentCharacter);
                                SaveCharacters();
                            }
                        }
                    }
                }
                
                _prevMouse = mouse;
                _prevKb = kb;
                base.Update(gameTime);
                return;
            }
        }

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float speed = 400f * dt;

        if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A)) _camera.X += speed;
        if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) _camera.X -= speed;
        if (kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W)) _camera.Y += speed;
        if (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S)) _camera.Y -= speed;

        if (kb.IsKeyDown(Keys.Q)) _rotation -= RotationSpeed * dt;
        if (kb.IsKeyDown(Keys.E)) _rotation += RotationSpeed * dt;

        if (_rotation > MathHelper.Pi) _rotation -= MathHelper.TwoPi;
        if (_rotation < -MathHelper.Pi) _rotation += MathHelper.TwoPi;

        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0)
        {
            _zoom += scrollDelta * 0.001f;
            _zoom = MathHelper.Clamp(_zoom, 0.1f, 5f);
            _prevScrollValue = mouse.ScrollWheelValue;
        }
        
        // Update vision if in combat
        if (_combatManager.InCombat)
        {
            UpdateVision();
        }

        _prevKb = kb;
        base.Update(gameTime);
    }

    private void ExecuteMainMenuAction(int index)
    {
        var sel = _mainMenuItems[index];
        if (sel == "New Game")
        {
            _characterCreation.Reset();
            _state = AppState.CharacterCreate;
        }
        else if (sel == "Continue")
        {
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
            _isMenuOpen = true;
            _menuIndex = 1;
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
    
    private void DrawTileWithLighting(SpriteBatch sb, int x, int y, Vector2 origin, float tileW, float tileH)
    {
        if (!_showVisionOverlay)
            return;
        
        bool isVisible = _visionSystem.IsVisible(x, y);
        var tint = _visionSystem.GetFogOfWarTint(x, y, isVisible);
        
        // If tile is completely black, draw black overlay
        if (tint == Color.Black)
        {
            float a = tileW * 0.5f;
            float b = tileH * 0.5f;
            var center = origin + new Vector2(((x + 0.5f) - (y + 0.5f)) * a, ((x + 0.5f) + (y + 0.5f)) * b);
            var top = center + new Vector2(0, -b);
            var right = center + new Vector2(a, 0);
            var bottom = center + new Vector2(0, b);
            var left = center + new Vector2(-a, 0);
            
            // Draw filled diamond
            DrawFilledDiamond(sb, top, right, bottom, left, Color.Black * 0.9f);
        }
        else if (tint != Color.White)
        {
            // Draw dimmed overlay for dim light or darkvision
            float a = tileW * 0.5f;
            float b = tileH * 0.5f;
            var center = origin + new Vector2(((x + 0.5f) - (y + 0.5f)) * a, ((x + 0.5f) + (y + 0.5f)) * b);
            var top = center + new Vector2(0, -b);
            var right = center + new Vector2(a, 0);
            var bottom = center + new Vector2(0, b);
            var left = center + new Vector2(-a, 0);
            
            DrawFilledDiamond(sb, top, right, bottom, left, tint * 0.6f);
        }
    }
    
    private void DrawFilledDiamond(SpriteBatch sb, Vector2 top, Vector2 right, Vector2 bottom, Vector2 left, Color color)
    {
        // Draw as two triangles
        int steps = (int)Math.Max(Vector2.Distance(top, right), 10);
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)steps;
            var p1 = Vector2.Lerp(top, right, t);
            var p2 = Vector2.Lerp(left, bottom, t);
            DrawLine(sb, _pixel, p1, p2, color, 2f);
        }
    }
    
    private string SafeString(string text)
    {
        if (_font == null) return text;
        
        var result = "";
        foreach (char c in text)
        {
            try
            {
                _font.MeasureString(c.ToString());
                result += c;
            }
            catch
            {
                result += '?';
            }
        }
        return result;
    }
    
    private void DrawCreature(SpriteBatch sb, Creature creature, Vector2 origin, float tileW, float tileH)
    {
        // Only draw if visible
        if (_combatManager.InCombat && _showVisionOverlay && !_visionSystem.IsVisible(creature.X, creature.Y))
        {
            return;
        }
        
        float a = tileW * 0.5f;
        float b = tileH * 0.5f;
        
        var center = origin + new Vector2(((creature.X + 0.5f) - (creature.Y + 0.5f)) * a, ((creature.X + 0.5f) + (creature.Y + 0.5f)) * b);
        
        // Draw creature as colored circle
        int radius = (int)(b * 1.5f);
        Color creatureColor = creature.DisplayColor;
        
        // Apply lighting tint if vision overlay is enabled
        if (_showVisionOverlay)
        {
            var tint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, true);
            creatureColor = new Color(
                (creature.DisplayColor.R * tint.R) / 255,
                (creature.DisplayColor.G * tint.G) / 255,
                (creature.DisplayColor.B * tint.B) / 255
            );
        }
        
        sb.Draw(_pixel, new Rectangle((int)center.X - radius, (int)center.Y - radius, radius * 2, radius * 2), null, creatureColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        
        // Draw vision type indicator
        if (creature.HasBlindSight)
        {
            // Small blue dot for blindsight
            sb.Draw(_pixel, new Rectangle((int)center.X + radius - 4, (int)center.Y - radius, 4, 4), Color.Cyan);
        }
        else if (creature.DarkvisionRange >= 120)
        {
            // Small purple dot for superior darkvision
            sb.Draw(_pixel, new Rectangle((int)center.X + radius - 4, (int)center.Y - radius, 4, 4), Color.Purple);
        }
        else if (creature.DarkvisionRange > 0)
        {
            // Small yellow dot for normal darkvision
            sb.Draw(_pixel, new Rectangle((int)center.X + radius - 4, (int)center.Y - radius, 4, 4), Color.Yellow);
        }
        
        // Draw sunlight sensitivity indicator
        if (creature.HasSunlightSensitivity && (_visionSystem.GlobalDaylight || _visionSystem.GetLightLevel(creature.X, creature.Y) == LightType.Bright))
        {
            // Orange triangle for sunlight sensitivity
            sb.Draw(_pixel, new Rectangle((int)center.X - radius, (int)center.Y - radius, 4, 4), Color.Orange);
        }
        
        // Draw condition indicator
        if (creature.Conditions != Condition.None)
        {
            sb.Draw(_pixel, new Rectangle((int)center.X - radius, (int)center.Y + radius - 4, 4, 4), Color.Red);
        }
        
        // Draw health bar
        if (_font != null)
        {
            int barWidth = radius * 2;
            int barHeight = 4;
            int barX = (int)center.X - radius;
            int barY = (int)center.Y + radius + 4;
            
            // Background
            sb.Draw(_pixel, new Rectangle(barX, barY, barWidth, barHeight), Color.DarkRed);
            // Health
            float healthPercent = (float)creature.CurrentHP / creature.MaxHP;
            sb.Draw(_pixel, new Rectangle(barX, barY, (int)(barWidth * healthPercent), barHeight), Color.Green);
            
            // Name
            var safeName = SafeString(creature.Name);
            var nameSize = _font.MeasureString(safeName);
            sb.DrawString(_font, safeName, new Vector2(center.X - nameSize.X * 0.25f, center.Y - radius - 20), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var vp = GraphicsDevice.Viewport;
        var screenCenter = new Vector2(vp.Width / 2f, vp.Height / 2f);
        var origin = screenCenter + _camera;

        // MAIN MENU
        if (_state == AppState.MainMenu)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 120;
            int menuHeight = titleHeight + _mainMenuItems.Length * (itemHeight + padding) + padding;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            _spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

            if (_font != null)
            {
                string title = "4DND";
                var titleSize = _font.MeasureString(title);
                var titlePos = new Vector2(menuRect.X + (menuWidth - titleSize.X) / 2, menuRect.Y + 12);
                _spriteBatch.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            }
            else
            {
                var titleBar = new Rectangle(menuRect.X + 12, menuRect.Y + 12, menuWidth - 24, titleHeight - 24);
                _spriteBatch.Draw(_pixel, titleBar, Color.Gray);
            }

            for (int i = 0; i < _mainMenuItems.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (i == _mainMenuIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    var text = _mainMenuItems[i];
                    var size = _font.MeasureString(text);
                    var pos = new Vector2(itemRect.X + (itemRect.Width - size.X) / 2, itemRect.Y + (itemRect.Height - size.Y) / 2);
                    var textCol = (i == _mainMenuIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, text, pos, textCol);
                }
            }

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // CHARACTER SELECT
        if (_state == AppState.CharacterSelect)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 80;
            int menuHeight = titleHeight + GetCharacterMenuItemCount() * (itemHeight + padding) + padding;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            _spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

            if (_font != null)
            {
                var title = "Choose a Character";
                var size = _font.MeasureString(title);
                var pos = new Vector2(menuRect.X + (menuWidth - size.X) / 2, menuRect.Y + 12);
                _spriteBatch.DrawString(_font, title, pos, Color.White);

                // Back button
                var backRect = new Rectangle(menuRect.X + padding, menuRect.Y + padding, 80, 30);
                var mouse = Mouse.GetState();
                var backColor = backRect.Contains(mouse.Position) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, backRect, backColor);
                var backText = "< Back";
                var backTextSize = _font.MeasureString(backText);
                _spriteBatch.DrawString(_font, backText, new Vector2(backRect.X + (backRect.Width - backTextSize.X) / 2, backRect.Y + (backRect.Height - backTextSize.Y) / 2), Color.White);

                // Hint at bottom
                var hint = "Press Delete to remove character | Esc to go back";
                var hintSize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(menuRect.X + (menuWidth - hintSize.X) / 2, menuRect.Y + menuHeight - 28), Color.White * 0.7f);
            }

            for (int i = 0; i < GetCharacterMenuItemCount(); i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (i == _characterIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    string label = IsExistingCharacterIndex(i) ? _characters[i].Name : "Create New";
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
                        
                        var deleteText = "Delete";
                        var deleteSize = _font.MeasureString(deleteText);
                        _spriteBatch.DrawString(_font, deleteText, new Vector2(deleteRect.X + (deleteRect.Width - deleteSize.X) / 2, deleteRect.Y + (deleteRect.Height - deleteSize.Y) / 2), Color.White);
                    }
                }
            }

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

        // CHARACTER SHEET
        if (_showCharacterSheet && _state == AppState.Playing && _currentCharacter != null)
        {
            _characterSheet.Draw(_spriteBatch, GraphicsDevice, _currentCharacter);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // GAMEPLAY RENDERING
        float tileW = _cellSize * _zoom;
        float tileH = _cellSize * 0.5f * _zoom;
        
        int? hoveredX = null;
        int? hoveredY = null;

        if (!_inMainMenu)
        {
            var mouse = Mouse.GetState();
            var mp = mouse.Position.ToVector2();
            var rel = mp - origin;

            float a = tileW * 0.5f;
            float b = tileH * 0.5f;

            if (a != 0 && b != 0)
            {
                float rx = rel.X;
                float ry = rel.Y;
                float wx = ((rx / a) + (ry / b)) * 0.5f;
                float wy = ((ry / b) - (rx / a)) * 0.5f;

                int tx = (int)System.Math.Floor(wx);
                int ty = (int)System.Math.Floor(wy);
                
                hoveredX = tx;
                hoveredY = ty;

                var center = origin + new Vector2(((tx + 0.5f) - (ty + 0.5f)) * a, ((tx + 0.5f) + (ty + 0.5f)) * b);

                var top = center + new Vector2(0, -b);
                var right = center + new Vector2(a, 0);
                var bottom = center + new Vector2(0, b);
                var left = center + new Vector2(-a, 0);

                DrawLine(_spriteBatch, _pixel, top, right, Color.Yellow, 3f);
                DrawLine(_spriteBatch, _pixel, right, bottom, Color.Yellow, 3f);
                DrawLine(_spriteBatch, _pixel, bottom, left, Color.Yellow, 3f);
                DrawLine(_spriteBatch, _pixel, left, top, Color.Yellow, 3f);
            }
        }

        int range = (int)((Math.Max(vp.Width, vp.Height) / Math.Min(tileW, tileH))) + 6;
        int xmin = -range, xmax = range;
        int ymin = -range, ymax = range;

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
        
        // Draw creatures in combat
        if (_combatManager.InCombat)
        {
            foreach (var creature in _combatManager.Combatants)
            {
                if (creature.IsAlive())
                {
                    DrawCreature(_spriteBatch, creature, origin, tileW, tileH);
                }
            }
        }
        
        // Draw fog of war / lighting overlay
        if (_combatManager.InCombat && _showVisionOverlay)
        {
            for (int y = ymin; y <= ymax; y++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    DrawTileWithLighting(_spriteBatch, x, y, origin, tileW, tileH);
                }
            }
            
            // Draw light sources
            foreach (var source in _visionSystem._lightSources)
            {
                if (source.IsActive)
                {
                    float a = tileW * 0.5f;
                    float b = tileH * 0.5f;
                    var center = origin + new Vector2(((source.X + 0.5f) - (source.Y + 0.5f)) * a, ((source.X + 0.5f) + (source.Y + 0.5f)) * b);
                    
                    int lightRadius = (int)(b * 0.8f);
                    _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - lightRadius, (int)center.Y - lightRadius, lightRadius * 2, lightRadius * 2), null, source.LightColor * 0.8f, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                }
            }
            
            // Draw area effects (fog clouds, darkness spells)
            foreach (var effect in _visionSystem._areaEffects)
            {
                float a = tileW * 0.5f;
                float b = tileH * 0.5f;
                var center = origin + new Vector2(((effect.X + 0.5f) - (effect.Y + 0.5f)) * a, ((effect.X + 0.5f) + (effect.Y + 0.5f)) * b);
                
                int effectRadius = (int)((effect.Radius / 5.0f) * tileW * 0.5f);
                
                Color effectColor = effect.EffectType switch
                {
                    LightType.Darkness => Color.Purple * 0.5f,
                    _ => Color.White * 0.5f
                };
                
                if (effect.BlocksVision)
                {
                    effectColor = Color.Gray * 0.7f; // Fog
                }
                
                // Draw circle
                for (int angle = 0; angle < 360; angle += 10)
                {
                    float rad = MathHelper.ToRadians(angle);
                    var p1 = center + new Vector2((float)Math.Cos(rad) * effectRadius, (float)Math.Sin(rad) * effectRadius);
                    float rad2 = MathHelper.ToRadians(angle + 10);
                    var p2 = center + new Vector2((float)Math.Cos(rad2) * effectRadius, (float)Math.Sin(rad2) * effectRadius);
                    
                    DrawLine(_spriteBatch, _pixel, p1, p2, effectColor, 2f);
                }
            }
            
            // Draw darkvision range indicator if player has darkvision
            if (_playerCreature != null && _playerCreature.DarkvisionRange > 0 && !_visionSystem.GlobalDaylight)
            {
                float a = tileW * 0.5f;
                float b = tileH * 0.5f;
                var center = origin + new Vector2(((_playerCreature.X + 0.5f) - (_playerCreature.Y + 0.5f)) * a, ((_playerCreature.X + 0.5f) + (_playerCreature.Y + 0.5f)) * b);
                
                int darkvisionRadius = (int)((_playerCreature.DarkvisionRange / 5.0f) * tileW * 0.5f);
                
                // Draw darkvision circle (subtle)
                for (int angle = 0; angle < 360; angle += 15)
                {
                    float rad = MathHelper.ToRadians(angle);
                    var p1 = center + new Vector2((float)Math.Cos(rad) * darkvisionRadius, (float)Math.Sin(rad) * darkvisionRadius);
                    float rad2 = MathHelper.ToRadians(angle + 15);
                    var p2 = center + new Vector2((float)Math.Cos(rad2) * darkvisionRadius, (float)Math.Sin(rad2) * darkvisionRadius);
                    
                    DrawLine(_spriteBatch, _pixel, p1, p2, Color.Purple * 0.3f, 1f);
                }
            }
        }
        
        // Combat UI
        if (_showCombatUI && _combatManager.InCombat)
        {
            // Combat panel at top
            int panelHeight = 200;
            var combatPanel = new Rectangle(0, 0, vp.Width, panelHeight);
            _spriteBatch.Draw(_pixel, combatPanel, Color.Black * 0.8f);
            
            if (_font != null)
            {
                int y = 10;
                
                // Current turn
                var currentCombatant = _combatManager.CurrentCombatant;
                if (currentCombatant != null)
                {
                    var turnText = $"Turn: {SafeString(currentCombatant.Name)} (HP: {currentCombatant.CurrentHP}/{currentCombatant.MaxHP})";
                    _spriteBatch.DrawString(_font, turnText, new Vector2(10, y), Color.Yellow);
                    y += 25;
                    
                    // Initiative order
                    var initText = "Initiative: ";
                    for (int i = 0; i < _combatManager.Combatants.Count && i < 5; i++)
                    {
                        var c = _combatManager.Combatants[i];
                        initText += $"{SafeString(c.Name)}({c.Initiative}) ";
                    }
                    _spriteBatch.DrawString(_font, initText, new Vector2(10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    y += 25;
                    
                    // Player actions
                    if (currentCombatant.IsPlayer)
                    {
                        _spriteBatch.DrawString(_font, "Actions: [1] Move  [2] Attack  [3] End Turn", new Vector2(10, y), Color.White);
                        y += 25;
                        
                        if (_selectedAction != CombatAction.None)
                        {
                            var actionText = _selectedAction switch
                            {
                                CombatAction.Move => "Click on an empty tile to move",
                                CombatAction.Attack => "Click on an enemy to attack",
                                _ => ""
                            };
                            _spriteBatch.DrawString(_font, actionText, new Vector2(10, y), Color.Yellow);
                            y += 25;
                        }
                    }
                    else
                    {
                        _spriteBatch.DrawString(_font, "Enemy turn...", new Vector2(10, y), Color.Red);
                        y += 25;
                    }
                }
                
                // Combat log
                y = panelHeight - 100;
                _spriteBatch.DrawString(_font, "Combat Log:", new Vector2(10, y), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                y += 20;
                
                for (int i = Math.Max(0, _combatLog.Count - 4); i < _combatLog.Count; i++)
                {
                    _spriteBatch.DrawString(_font, _combatLog[i], new Vector2(10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    y += 18;
                }
                
                // Vision legend (right side)
                if (_showVisionOverlay)
                {
                    int legendX = vp.Width - 280;
                    int legendY = 70;
                    
                    _spriteBatch.DrawString(_font, "Vision Legend:", new Vector2(legendX, legendY), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                    legendY += 25;
                    
                    // Bright light
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.White);
                    _spriteBatch.DrawString(_font, "Bright Light", new Vector2(legendX + 25, legendY), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    legendY += 25;
                    
                    // Dim light
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), new Color(128, 128, 128));
                    _spriteBatch.DrawString(_font, "Dim Light (Lightly Obscured)", new Vector2(legendX + 25, legendY), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    legendY += 25;
                    
                    // Darkness with darkvision
                    if (_playerCreature.DarkvisionRange > 0)
                    {
                        _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), new Color(64, 64, 96));
                        _spriteBatch.DrawString(_font, "Darkness (Darkvision)", new Vector2(legendX + 25, legendY), new Color(150, 150, 180), 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                        legendY += 25;
                    }
                    
                    // Complete darkness
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.Black);
                    _spriteBatch.DrawString(_font, "Darkness (Heavily Obscured)", new Vector2(legendX + 25, legendY), Color.DarkGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    legendY += 35;
                    
                    // Creature indicators
                    _spriteBatch.DrawString(_font, "Creature Indicators:", new Vector2(legendX, legendY), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                    legendY += 20;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Yellow);
                    _spriteBatch.DrawString(_font, "Darkvision 60ft", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Purple);
                    _spriteBatch.DrawString(_font, "Superior Darkvision 120ft", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Cyan);
                    _spriteBatch.DrawString(_font, "Blindsight", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Orange);
                    _spriteBatch.DrawString(_font, "Sunlight Sensitivity", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Red);
                    _spriteBatch.DrawString(_font, "Has Condition", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                }
                
                // Instructions
                var hint = "Press Tab to toggle combat UI | ESC for menu";
                var hintSize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(vp.Width - hintSize.X - 10, panelHeight - 25), Color.White * 0.7f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                
                // Test keybindings
                var testHint = "Test: [B]linded [F]og Cloud [K]Darkness";
                var testHintSize = _font.MeasureString(testHint);
                _spriteBatch.DrawString(_font, testHint, new Vector2(vp.Width - testHintSize.X - 10, panelHeight - 50), Color.Yellow * 0.6f, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
        
        // Tile tooltip (outside combat panel)
        if (_font != null && hoveredX.HasValue && hoveredY.HasValue && _combatManager.InCombat && _showVisionOverlay)
        {
            int tx = hoveredX.Value;
            int ty = hoveredY.Value;
            
            var lightLevel = _visionSystem.GetLightLevel(tx, ty);
            var isVisible = _visionSystem.IsVisible(tx, ty);
            
            var creature = _combatManager.GetCreatureAt(tx, ty);
            
            var tooltip = $"Tile ({tx}, {ty}) | Light: {lightLevel} | Visible: {isVisible}";
            if (creature != null && isVisible)
            {
                tooltip += $" | {creature.Name} (HP: {creature.CurrentHP}/{creature.MaxHP})";
            }
            
            var tooltipSize = _font.MeasureString(tooltip);
            var mouse = Mouse.GetState();
            var tooltipPos = new Vector2(mouse.X + 15, mouse.Y + 15);
            
            // Make sure tooltip stays on screen
            if (tooltipPos.X + tooltipSize.X > vp.Width)
                tooltipPos.X = mouse.X - tooltipSize.X - 15;
            if (tooltipPos.Y + tooltipSize.Y > vp.Height)
                tooltipPos.Y = mouse.Y - tooltipSize.Y - 15;
            
            // Draw background
            _spriteBatch.Draw(_pixel, new Rectangle((int)tooltipPos.X - 5, (int)tooltipPos.Y - 3, (int)tooltipSize.X + 10, (int)tooltipSize.Y + 6), Color.Black * 0.9f);
            _spriteBatch.DrawString(_font, tooltip, tooltipPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        // PAUSE MENU
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

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
