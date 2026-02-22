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

    private InfiniteGrid3D<bool> _grid = new();
    private Texture2D _pixel = null!;
    private int _cellSize = 24;

    // 3D Camera system
    private Vector3 _cameraTarget = Vector3.Zero;
    private float _cameraYaw = MathHelper.ToRadians(45f);
    private float _cameraPitch = MathHelper.ToRadians(-35f);
    private float _cameraDistance = 20f;
    private float _targetYaw = MathHelper.ToRadians(45f);
    private const float RotationSpeed = 5f; // Rad per second for transition

    private BasicEffect _basicEffect = null!;
    private VertexBuffer _cubeVertexBuffer = null!;
    private IndexBuffer _cubeIndexBuffer = null!;
    private VertexBuffer _tileVertexBuffer = null!;
    private IndexBuffer _tileIndexBuffer = null!;

    private int _prevScrollValue = 0;
    private int _currentViewLevel = 0; // Current Z-level being viewed

    private enum AppState { MainMenu, CharacterSelect, CharacterCreate, CampaignSelect, CampaignCreate, Playing }
    private AppState _state = AppState.MainMenu;

    private bool _inMainMenu => _state == AppState.MainMenu;
    private readonly string[] _mainMenuItems = new[] { "Single Player", "Multiplayer", "Options", "Desktop" };
    private int _mainMenuIndex = 0;

    private List<Character> _characters = new();
    private int _characterIndex = 0;
    private string _savesDir = "saves";
    private string _charsFile = "characters.json";
    private string _campaignsFile = "campaigns.json";
    private Character _currentCharacter = null;
    private Campaign _currentCampaign = null;
    private bool _isMultiplayerMode = false;
    
    private List<Campaign> _campaigns = new();
    private int _campaignIndex = 0;

    private CharacterCreation _characterCreation = null!;
    private CharacterSheet _characterSheet = null!;
    private CampaignCreation _campaignCreation = null!;
    private CampaignMapViewer _campaignMapViewer = null!;

    private bool _isMenuOpen = false;
    private int _menuIndex = 0;
    private readonly string[] _menuItems = new[] { "Continue", "Options", "Main Menu", "Desktop" };
    
    private bool _showCampaignMap = false;

    private KeyboardState _prevKb;
    private SpriteFont _font = null!;

    private bool _showCharacterSheet = false;

    private CombatManager _combatManager = new();
    private Creature _playerCreature = null;
    private List<string> _combatLog = new();
    private const int MAX_COMBAT_LOG = 5;
    
    // Vision and lighting system
    private VisionSystem _visionSystem = new();
    private bool _showVisionOverlay = true;
    private bool _visionNeedsUpdate = false;
    
    // Combat UI state
    private enum CombatAction { None, Move, Attack, EndTurn }
    private CombatAction _selectedAction = CombatAction.None;
    private bool _showCombatUI = false;
    private MouseState _prevMouse;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        
        // Configure borderless fullscreen window
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        _graphics.HardwareModeSwitch = false;
        _graphics.IsFullScreen = false;
    }

    protected override void Initialize()
    {
        // Apply borderless fullscreen after initialization
        Window.IsBorderless = true;
        Window.Position = new Point(0, 0);
        _graphics.ApplyChanges();
        
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
        catch (Microsoft.Xna.Framework.Content.ContentLoadException)
        {
            _font = null!;
            System.Console.WriteLine("Warning: DefaultFont not found. Build the Content/Content.mgcb with the MonoGame Pipeline Tool to generate DefaultFont.xnb. Menu text will be hidden.");
        }

        _characterCreation = new CharacterCreation(_font, _pixel);
        _characterSheet = new CharacterSheet(_font, _pixel);
        _campaignCreation = new CampaignCreation(_font, _pixel);
        _campaignMapViewer = new CampaignMapViewer(_font, _pixel);

        Initialize3DRendering();

        // Create a 3D test structure
        for (int x = -10; x <= 10; x++)
            _grid.Set(x, 0, 0, x % 2 == 0);

        for (int y = -6; y <= 6; y++)
        {
            _grid.Set(0, y, 0, true);
            _grid.Set(1, y, 0, (y % 3) == 0);
        }
        
        // Add some platforms at different heights
        for (int z = 1; z <= 3; z++)
        {
            for (int i = -3; i <= 3; i++)
            {
                _grid.Set(i, i, z, true);
                _grid.Set(-i, i, z, true);
            }
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
        // Spawn some initial enemies for exploration mode
        var rand = new Random();
        var enemies = new List<Creature>();
        
        // Create 3-5 enemies scattered around at various heights
        int numEnemies = rand.Next(3, 6);
        
        for (int i = 0; i < numEnemies; i++)
        {
            int enemyX = rand.Next(-8, 9);
            int enemyY = rand.Next(-8, 9);
            int enemyZ = rand.Next(0, 4); // Random height from 0 to 3
            
            // Don't spawn at origin (player spawn)
            if (enemyX == 0 && enemyY == 0 && enemyZ == 0)
            {
                enemyX = 5;
                enemyY = 5;
                enemyZ = 1;
            }
            
            int enemyType = rand.Next(0, 7);
            Creature enemy = enemyType switch
            {
                0 => Creature.CreateGoblin(enemyX, enemyY, enemyZ),
                1 => Creature.CreateOrc(enemyX, enemyY, enemyZ),
                2 => Creature.CreateSkeleton(enemyX, enemyY, enemyZ),
                3 => Creature.CreateWolf(enemyX, enemyY, enemyZ),
                4 => Creature.CreateKobold(enemyX, enemyY, enemyZ),
                5 => Creature.CreateUmberHulk(enemyX, enemyY, enemyZ),
                _ => Creature.CreateCouatl(enemyX, enemyY, enemyZ)
            };
            
            // Flying creatures start flying at elevated positions
            if (enemy.CanFly && enemyZ > 0)
            {
                enemy.IsFlying = true;
            }
            
            enemies.Add(enemy);
        }
        
        // Add enemies to combat manager for tracking (but don't start combat yet)
        foreach (var enemy in enemies)
        {
            _combatManager.Combatants.Add(enemy);
        }
    }
    
    private void StartCombatWithNearbyEnemies()
    {
        if (_currentCharacter == null || _playerCreature == null) return;
        
        var combatants = new List<Creature>();
        combatants.Add(_playerCreature);
        
        // Add existing enemies to combat
        var existingEnemies = _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive()).ToList();
        combatants.AddRange(existingEnemies);
        
        // Clear and restart with proper initiative
        _combatManager.Combatants.Clear();
        _combatManager.StartCombat(combatants);
        _showCombatUI = true;
        _selectedAction = CombatAction.None;
        AddToCombatLog("Combat started!");
        AddToCombatLog($"Round {_combatManager.CurrentRound} begins!");
        
        // Setup lighting for combat
        SetupCombatLighting();
        UpdateVision();
    }
    
    private void SetupCombatLighting()
    {
        _visionSystem.ClearLightSources();
        
        // Add torches at strategic locations
        if (_playerCreature != null)
        {
            // Player carries a torch
            var torch = LightSource.Torch(_playerCreature.X, _playerCreature.Y, _playerCreature.Z);
            torch.AttachedTo = _playerCreature;
            _visionSystem.AddLightSource(torch);
        }
        
        // Add some ambient light sources at different heights
        var rand = new Random();
        for (int i = 0; i < 2; i++)
        {
            int lx = rand.Next(-10, 11);
            int ly = rand.Next(-10, 11);
            int lz = rand.Next(0, 4);
            _visionSystem.AddLightSource(LightSource.Lantern(lx, ly, lz));
        }
    }
    
    private void UpdateVision()
    {
        _visionNeedsUpdate = true;
    }
    
    private void RecalculateVision()
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
                    light.Z = light.AttachedTo.Z;
                }
            }
            
            _visionSystem.CalculateLighting();
            _visionSystem.CalculateVisibility(_playerCreature);
        }
        _visionNeedsUpdate = false;
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
    
    private void LoadCampaigns()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _campaigns = JsonSerializer.Deserialize<List<Campaign>>(json) ?? new List<Campaign>();
            }
            else
            {
                _campaigns = new List<Campaign>();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to load campaigns: " + ex.Message);
            _campaigns = new List<Campaign>();
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
    
    private void SaveCampaigns()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            var json = JsonSerializer.Serialize(_campaigns);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to save campaigns: " + ex.Message);
        }
    }
    
    private void UpdateCameraMatrices()
    {
        float aspectRatio = GraphicsDevice.Viewport.AspectRatio;

        // Projection Matrix
        _basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45f),
            aspectRatio,
            0.1f,
            1000f);

        // Calculate Camera Position
        // Rotate a vector based on Yaw and Pitch, then add it to Target
        Vector3 direction = new Vector3(0, _cameraDistance, 0);
        Matrix rotation = Matrix.CreateRotationX(_cameraPitch) * Matrix.CreateRotationZ(_cameraYaw);
        Vector3 cameraPosition = _cameraTarget + Vector3.Transform(direction, rotation);

        // View Matrix
        _basicEffect.View = Matrix.CreateLookAt(
            cameraPosition,
            _cameraTarget,
            Vector3.UnitZ // Up is Z
        );
    }

    private void Initialize3DRendering()
    {
        _basicEffect = new BasicEffect(GraphicsDevice);
        _basicEffect.VertexColorEnabled = true;
        _basicEffect.LightingEnabled = true;
        _basicEffect.EnableDefaultLighting();

        // Create Cube (for creatures)
        // Vertices for a unit cube (from -0.5 to 0.5 in X/Y, 0 to 1 in Z)
        var cubeVertices = new VertexPositionNormalColor[]
        {
            // Bottom face (Z=0)
            new(new(-0.5f, -0.5f, 0), Vector3.Down, Color.White),
            new(new(0.5f, -0.5f, 0), Vector3.Down, Color.White),
            new(new(0.5f, 0.5f, 0), Vector3.Down, Color.White),
            new(new(-0.5f, 0.5f, 0), Vector3.Down, Color.White),
            // Top face (Z=1)
            new(new(-0.5f, -0.5f, 1), Vector3.Up, Color.White),
            new(new(0.5f, -0.5f, 1), Vector3.Up, Color.White),
            new(new(0.5f, 0.5f, 1), Vector3.Up, Color.White),
            new(new(-0.5f, 0.5f, 1), Vector3.Up, Color.White),
            // Left face (X=-0.5)
            new(new(-0.5f, -0.5f, 0), Vector3.Left, Color.White),
            new(new(-0.5f, 0.5f, 0), Vector3.Left, Color.White),
            new(new(-0.5f, 0.5f, 1), Vector3.Left, Color.White),
            new(new(-0.5f, -0.5f, 1), Vector3.Left, Color.White),
            // Right face (X=0.5)
            new(new(0.5f, -0.5f, 0), Vector3.Right, Color.White),
            new(new(0.5f, -0.5f, 1), Vector3.Right, Color.White),
            new(new(0.5f, 0.5f, 1), Vector3.Right, Color.White),
            new(new(0.5f, 0.5f, 0), Vector3.Right, Color.White),
            // Front face (Y=-0.5)
            new(new(-0.5f, -0.5f, 0), Vector3.Forward, Color.White),
            new(new(0.5f, -0.5f, 0), Vector3.Forward, Color.White),
            new(new(0.5f, -0.5f, 1), Vector3.Forward, Color.White),
            new(new(-0.5f, -0.5f, 1), Vector3.Forward, Color.White),
            // Back face (Y=0.5)
            new(new(-0.5f, 0.5f, 0), Vector3.Backward, Color.White),
            new(new(-0.5f, 0.5f, 1), Vector3.Backward, Color.White),
            new(new(0.5f, 0.5f, 1), Vector3.Backward, Color.White),
            new(new(0.5f, 0.5f, 0), Vector3.Backward, Color.White),
        };

        _cubeVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), cubeVertices.Length, BufferUsage.WriteOnly);
        _cubeVertexBuffer.SetData(cubeVertices);

        var cubeIndices = new short[]
        {
            0, 1, 2, 0, 2, 3, // Bottom
            4, 6, 5, 4, 7, 6, // Top
            8, 9, 10, 8, 10, 11, // Left
            12, 13, 14, 12, 14, 15, // Right
            16, 17, 18, 16, 18, 19, // Front
            20, 21, 22, 20, 22, 23 // Back
        };

        _cubeIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, cubeIndices.Length, BufferUsage.WriteOnly);
        _cubeIndexBuffer.SetData(cubeIndices);

        // Create Tile (Quad)
        var tileVertices = new VertexPositionNormalColor[]
        {
            new(new(-0.5f, -0.5f, 0), Vector3.Up, Color.White),
            new(new(0.5f, -0.5f, 0), Vector3.Up, Color.White),
            new(new(0.5f, 0.5f, 0), Vector3.Up, Color.White),
            new(new(-0.5f, 0.5f, 0), Vector3.Up, Color.White),
        };

        _tileVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), tileVertices.Length, BufferUsage.WriteOnly);
        _tileVertexBuffer.SetData(tileVertices);

        var tileIndices = new short[] { 0, 1, 2, 0, 2, 3 };
        _tileIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, tileIndices.Length, BufferUsage.WriteOnly);
        _tileIndexBuffer.SetData(tileIndices);
    }

    private void SaveCampaign()
    {
        try
        {
            if (_currentCampaign == null) return;
            
            // Load existing campaigns
            LoadCampaigns();
            
            // Update or add current campaign
            var existing = _campaigns.FindIndex(c => c.Name == _currentCampaign.Name);
            if (existing >= 0)
                _campaigns[existing] = _currentCampaign;
            else
                _campaigns.Add(_currentCampaign);
            
            // Save
            SaveCampaigns();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to save campaign: " + ex.Message);
        }
    }
    
    private void LoadCampaign(string campaignName)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir, _campaignsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var campaigns = JsonSerializer.Deserialize<List<Campaign>>(json) ?? new List<Campaign>();
                _currentCampaign = campaigns.FirstOrDefault(c => c.Name == campaignName);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Failed to load campaign: " + ex.Message);
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
    
    private void ExecuteMainMenuAction(int index)
    {
        var sel = _mainMenuItems[index];
        if (sel == "Single Player")
        {
            _isMultiplayerMode = false;
            LoadCharacters();
            _state = AppState.CharacterSelect;
        }
        else if (sel == "Multiplayer")
        {
            _isMultiplayerMode = true;
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
    
    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

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
                    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        ExecuteMainMenuAction(i);
                }
            }

            _prevKb = kb;
            _prevMouse = mouse;
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
                _prevMouse = mouse;
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
                    if (_isMultiplayerMode)
                    {
                        // Initialize player creature when entering game (only if not already created)
                        if (_currentCharacter != null && _playerCreature == null)
                        {
                            _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                            _combatManager.Combatants.Clear();
                            SpawnTestEnemies();
                        }
                        
                        // TODO: Go to multiplayer lobby
                        _state = AppState.Playing;
                    }
                    else
                    {
                        LoadCampaigns();
                        _campaignIndex = 0;
                        _state = AppState.CampaignSelect;
                    }
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
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    _state = AppState.MainMenu;
                    _prevKb = kb;
                    _prevMouse = mouse;
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
                    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
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
                        
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            if (IsExistingCharacterIndex(i))
                            {
                                _currentCharacter = _characters[i];
                                if (_isMultiplayerMode)
                                {
                                    // Initialize player creature when entering game (only if not already created)
                                    if (_currentCharacter != null && _playerCreature == null)
                                    {
                                        _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                                        _combatManager.Combatants.Clear();
                                        SpawnTestEnemies();
                                    }
                                    
                                    // TODO: Go to multiplayer lobby
                                    _state = AppState.Playing;
                                }
                                else
                                {
                                    LoadCampaigns();
                                    _campaignIndex = 0;
                                    _state = AppState.CampaignSelect;
                                }
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
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // CHARACTER CREATION
        if (_state == AppState.CharacterCreate)
        {
            bool continueCreation = _characterCreation.Update(gameTime, GraphicsDevice, kb, _prevKb, out Character newCharacter);
            
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
                if (_isMultiplayerMode)
                {
                    // Initialize player creature only if not already created
                    if (_playerCreature == null)
                    {
                        _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                        _combatManager.Combatants.Clear();
                        SpawnTestEnemies();
                    }
                    // TODO: Go to multiplayer lobby
                    _state = AppState.Playing;
                }
                else
                {
                    LoadCampaigns();
                    _campaignIndex = 0;
                    _state = AppState.CampaignSelect;
                }
            }

            _prevKb = kb;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }
        
        // CAMPAIGN SELECT
        if (_state == AppState.CampaignSelect)
        {
            if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
            {
                _state = AppState.CharacterSelect;
                _prevKb = kb;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _campaignIndex = Math.Max(0, _campaignIndex - 1);
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _campaignIndex = Math.Min(_campaigns.Count, _campaignIndex + 1);
            
            if (kb.IsKeyDown(Keys.Delete) && !_prevKb.IsKeyDown(Keys.Delete))
            {
                if (_campaignIndex >= 0 && _campaignIndex < _campaigns.Count)
                {
                    _campaigns.RemoveAt(_campaignIndex);
                    SaveCampaigns();
                    if (_campaignIndex >= _campaigns.Count)
                        _campaignIndex = Math.Max(0, _campaigns.Count - 1);
                }
            }

            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
            {
                if (_campaignIndex >= 0 && _campaignIndex < _campaigns.Count)
                {
                    _currentCampaign = _campaigns[_campaignIndex];
                    if (_currentCharacter != null && !_currentCampaign.PartyMembers.Contains(_currentCharacter.Name))
                    {
                        _currentCampaign.PartyMembers.Add(_currentCharacter.Name);
                        _currentCampaign.LastPlayedDate = DateTime.Now;
                        SaveCampaigns();
                    }
                    
                    // Initialize player creature when entering game (only once!)
                    if (_currentCharacter != null && _playerCreature == null)
                    {
                        _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                        
                        // Clear existing enemies first to avoid duplicates
                        _combatManager.Combatants.Clear();
                        SpawnTestEnemies();
                    }
                    
                    _state = AppState.Playing;
                }
                else
                {
                    _campaignCreation.Reset();
                    _state = AppState.CampaignCreate;
                }
            }

            var vp = GraphicsDevice.Viewport;
            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 80;
            int menuHeight = titleHeight + Math.Max(1, _campaigns.Count + 1) * (itemHeight + padding) + padding + 40;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            var backRect = new Rectangle(menuRect.X + padding, menuRect.Y + padding, 80, 30);
            if (backRect.Contains(mouse.Position))
            {
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    _state = AppState.CharacterSelect;
                    _prevKb = kb;
                    _prevMouse = mouse;
                    base.Update(gameTime);
                    return;
                }
            }

            bool clickedDelete = false;

            for (int i = 0; i < _campaigns.Count; i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var deleteRect = new Rectangle(itemRect.X + itemRect.Width - 70, itemRect.Y + (itemRect.Height - 30) / 2, 60, 30);
                
                if (deleteRect.Contains(mouse.Position))
                {
                    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    {
                        _campaigns.RemoveAt(i);
                        SaveCampaigns();
                        if (_campaignIndex >= _campaigns.Count)
                            _campaignIndex = Math.Max(0, _campaigns.Count - 1);
                        clickedDelete = true;
                        break;
                    }
                }
            }

            if (!clickedDelete)
            {
                for (int i = 0; i <= _campaigns.Count; i++)
                {
                    var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _campaignIndex = i;
                        
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            if (i < _campaigns.Count)
                            {
                                _currentCampaign = _campaigns[i];
                                if (_currentCharacter != null && !_currentCampaign.PartyMembers.Contains(_currentCharacter.Name))
                                {
                                    _currentCampaign.PartyMembers.Add(_currentCharacter.Name);
                                    _currentCampaign.LastPlayedDate = DateTime.Now;
                                    SaveCampaigns();
                                }
                                
                                // Initialize player creature when entering game (only if not already created)
                                if (_currentCharacter != null && _playerCreature == null)
                                {
                                    _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                                    _combatManager.Combatants.Clear();
                                    SpawnTestEnemies();
                                }
                                 
                                _state = AppState.Playing;
                            }
                            else
                            {
                                _campaignCreation.Reset();
                                _state = AppState.CampaignCreate;
                            }
                        }
                        break;
                    }
                }
            }

            _prevKb = kb;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // CAMPAIGN CREATION
        if (_state == AppState.CampaignCreate)
        {
            bool continueCampaign = _campaignCreation.Update(gameTime, GraphicsDevice, kb, _prevKb, out Campaign newCampaign);
            
            if (!continueCampaign)
            {
                LoadCampaigns();
                _state = AppState.CampaignSelect;
            }
            else if (newCampaign != null)
            {
                _currentCampaign = newCampaign;
                if (_currentCharacter != null)
                {
                    _currentCampaign.PartyMembers.Add(_currentCharacter.Name);
                }
                SaveCampaign();
                
                // Initialize player creature when entering game (only if not already created)
                if (_currentCharacter != null && _playerCreature == null)
                {
                    _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                    _combatManager.Combatants.Clear();
                    SpawnTestEnemies();
                }
                
                _state = AppState.Playing;
            }

            _prevKb = kb;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // CAMPAIGN MAP VIEWER
        if (_showCampaignMap && _state == AppState.Playing)
        {
            _campaignMapViewer.Update(_currentCampaign, mouse, kb, _prevKb);
            
            // Close map with M
            if (kb.IsKeyDown(Keys.M) && !_prevKb.IsKeyDown(Keys.M))
            {
                _showCampaignMap = false;
            }
            
            _prevKb = kb;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // Normal gameplay and pause menu handling
        if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
        {
            _isMenuOpen = !_isMenuOpen;
            if (_isMenuOpen) _menuIndex = 0;
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
                    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        ExecuteMenuAction(i);
                }
            }

            _prevKb = kb;
            _prevMouse = mouse;
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
            
            // Toggle campaign map with M
            if (kb.IsKeyDown(Keys.M) && !_prevKb.IsKeyDown(Keys.M))
            {
                _showCampaignMap = !_showCampaignMap;
            }
            
            // Change view level with PageUp/PageDown
            if (kb.IsKeyDown(Keys.PageUp) && !_prevKb.IsKeyDown(Keys.PageUp))
            {
                _currentViewLevel++;
                AddToCombatLog($"Viewing level {_currentViewLevel}");
            }
            if (kb.IsKeyDown(Keys.PageDown) && !_prevKb.IsKeyDown(Keys.PageDown))
            {
                _currentViewLevel = Math.Max(0, _currentViewLevel - 1);
                AddToCombatLog($"Viewing level {_currentViewLevel}");
            }
            
            // Test: Make a strength check with X
            if (kb.IsKeyDown(Keys.X) && !_prevKb.IsKeyDown(Keys.X) && _currentCharacter != null)
            {
                var check = _currentCharacter.MakeAbilityCheck("Strength", DndMath.DifficultyClass.Medium);
                AddToCombatLog(check.GetSimpleMessage());
                System.Console.WriteLine(check.GetDetailedMessage());
            }
            
            // Test: Make a stealth check with Z
            if (kb.IsKeyDown(Keys.Z) && !_prevKb.IsKeyDown(Keys.Z) && _currentCharacter != null)
            {
                var check = _currentCharacter.MakeSkillCheck("Stealth", DndMath.DifficultyClass.Hard);
                AddToCombatLog(check.GetSimpleMessage());
                System.Console.WriteLine(check.GetDetailedMessage());
            }
            
            // Test: Make a saving throw with N
            if (kb.IsKeyDown(Keys.N) && !_prevKb.IsKeyDown(Keys.N) && _currentCharacter != null)
            {
                var check = _currentCharacter.MakeSavingThrow("Dexterity", DndMath.DifficultyClass.Hard);
                AddToCombatLog(check.GetSimpleMessage());
                System.Console.WriteLine(check.GetDetailedMessage());
            }
            
            // Toggle flying mode with Space (for flying creatures)
            if (kb.IsKeyDown(Keys.Space) && !_prevKb.IsKeyDown(Keys.Space) && _playerCreature != null)
            {
                if (_playerCreature.CanFly)
                {
                    _playerCreature.IsFlying = !_playerCreature.IsFlying;
                    AddToCombatLog(_playerCreature.IsFlying ? "Now flying!" : "Landing...");
                }
            }
            
            // Ascend/Descend with R/T (for flying creatures)
            if (kb.IsKeyDown(Keys.R) && !_prevKb.IsKeyDown(Keys.R) && _playerCreature != null)
            {
                if (_playerCreature.CanFly && _playerCreature.IsFlying)
                {
                    _playerCreature.Z++;
                    AddToCombatLog($"Ascended to level {_playerCreature.Z}");
                    UpdateVision();
                }
            }
            if (kb.IsKeyDown(Keys.T) && !_prevKb.IsKeyDown(Keys.T) && _playerCreature != null)
            {
                if (_playerCreature.CanFly && _playerCreature.IsFlying)
                {
                    _playerCreature.Z = Math.Max(0, _playerCreature.Z - 1);
                    AddToCombatLog($"Descended to level {_playerCreature.Z}");
                    UpdateVision();
                }
            }
            
            // Toggle vision overlay with V
            if (kb.IsKeyDown(Keys.V) && !_prevKb.IsKeyDown(Keys.V))
            {
                _showVisionOverlay = !_showVisionOverlay;
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
                var fogCloud = AreaEffect.FogCloud(_playerCreature.X, _playerCreature.Y, _playerCreature.Z);
                _visionSystem.AddAreaEffect(fogCloud);
                AddToCombatLog("Fog Cloud created!");
                UpdateVision();
            }
            
            // Test: Create Darkness with K
            if (kb.IsKeyDown(Keys.K) && !_prevKb.IsKeyDown(Keys.K) && _playerCreature != null)
            {
                var darkness = AreaEffect.Darkness(_playerCreature.X, _playerCreature.Y, _playerCreature.Z);
                _visionSystem.AddAreaEffect(darkness);
                AddToCombatLog("Darkness spell cast!");
                UpdateVision();
            }
            
            // Toggle combat UI with Tab
            if (kb.IsKeyDown(Keys.Tab) && !_prevKb.IsKeyDown(Keys.Tab))
            {
                if (!_combatManager.InCombat && _currentCharacter != null && _playerCreature != null)
                {
                    // Start combat with existing creatures
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
                _prevMouse = mouse;
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
                        int prevRound = _combatManager.CurrentRound;
                        _combatManager.NextTurn();
                        int newRound = _combatManager.CurrentRound;
                        
                        if (newRound > prevRound)
                        {
                            AddToCombatLog($"=== Round {newRound} ===");
                        }
                        
                        AddToCombatLog($"{currentCombatant.Name} ended turn");
                        _selectedAction = CombatAction.None;
                    }
                    
                    // Handle attack action
                    if (_selectedAction == CombatAction.Attack)
                    {
                        // Click on grid to attack
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            var hovered = GetHoveredTile();
                            if (hovered.HasValue)
                            {
                                int tx = hovered.Value.x;
                                int ty = hovered.Value.y;
                                
                                var target = _combatManager.GetCreatureAt(tx, ty, _currentViewLevel);
                                if (target != null && !target.IsPlayer && currentCombatant.HasAction)
                                {
                                    var result = _combatManager.MakeAttack(currentCombatant, target, _visionSystem);
                                    AddToCombatLog(result.GetMessage());
                                    _selectedAction = CombatAction.None;
                                    
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
                                else if (target != null && !currentCombatant.HasAction)
                                {
                                    AddToCombatLog("No action available!");
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
                            var hovered = GetHoveredTile();
                            if (hovered.HasValue)
                            {
                                int tx = hovered.Value.x;
                                int ty = hovered.Value.y;
                                
                                // Check if tile is empty and within movement range
                                if (_combatManager.GetCreatureAt(tx, ty, _currentViewLevel) == null)
                                {
                                    if (_combatManager.CanMove(currentCombatant, tx, ty, _currentViewLevel))
                                    {
                                        int distanceMoved = (Math.Abs(tx - currentCombatant.X) + Math.Abs(ty - currentCombatant.Y) + Math.Abs(_currentViewLevel - currentCombatant.Z)) * 5;
                                        _combatManager.Move(currentCombatant, tx, ty, _currentViewLevel);
                                        AddToCombatLog($"{currentCombatant.Name} moved to ({tx}, {ty}, {_currentViewLevel}) [{distanceMoved}ft, {currentCombatant.MovementRemaining}ft remaining]");
                                        _selectedAction = CombatAction.None;
                                        
                                        // Update vision after movement
                                        UpdateVision();
                                    }
                                    else
                                    {
                                        AddToCombatLog("Out of movement range!");
                                    }
                                }
                            }
                        }
                    }
                }
                else if (currentCombatant != null && !currentCombatant.IsPlayer)
                {
                    // AI turn - check if they have action
                    if (currentCombatant.HasAction || currentCombatant.MovementRemaining > 0)
                    {
                        var playerCreature = _combatManager.Combatants.FirstOrDefault(c => c.IsPlayer);
                        
                        if (playerCreature != null)
                        {
                            if (_combatManager.IsInMeleeRange(currentCombatant, playerCreature) && currentCombatant.HasAction)
                            {
                                // Attack
                                var result = _combatManager.MakeAttack(currentCombatant, playerCreature, _visionSystem);
                                AddToCombatLog(result.GetMessage());
                            }
                            else if (currentCombatant.MovementRemaining > 0)
                            {
                                // Move towards player (3D pathfinding)
                                int dx = Math.Sign(playerCreature.X - currentCombatant.X);
                                int dy = Math.Sign(playerCreature.Y - currentCombatant.Y);
                                int dz = Math.Sign(playerCreature.Z - currentCombatant.Z);
                                
                                // Prioritize horizontal movement, then vertical if can fly
                                int newX = currentCombatant.X;
                                int newY = currentCombatant.Y;
                                int newZ = currentCombatant.Z;
                                
                                if (dx != 0 || dy != 0)
                                {
                                    newX = currentCombatant.X + dx;
                                    newY = currentCombatant.Y + dy;
                                }
                                else if (dz != 0 && currentCombatant.CanFly)
                                {
                                    newZ = currentCombatant.Z + dz;
                                    currentCombatant.IsFlying = true;
                                }
                                
                                if (_combatManager.GetCreatureAt(newX, newY, newZ) == null && _combatManager.CanMove(currentCombatant, newX, newY, newZ))
                                {
                                    _combatManager.Move(currentCombatant, newX, newY, newZ);
                                    AddToCombatLog($"{currentCombatant.Name} moved");
                                    UpdateVision();
                                }
                            }
                            else
                            {
                                // Out of actions and movement, end turn
                                int prevRound = _combatManager.CurrentRound;
                                _combatManager.NextTurn();
                                int newRound = _combatManager.CurrentRound;
                        
                                if (newRound > prevRound)
                                {
                                    AddToCombatLog($"=== Round {newRound} ===");
                                }
                            }
                        }
                    }
                    else
                    {
                        // No actions left, end turn
                        int prevRound = _combatManager.CurrentRound;
                        _combatManager.NextTurn();
                        int newRound = _combatManager.CurrentRound;
                        
                        if (newRound > prevRound)
                        {
                            AddToCombatLog($"=== Round {newRound} ===");
                        }
                    }
                    
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
                
                _prevMouse = mouse;
                _prevKb = kb;
                base.Update(gameTime);
                return;
            }
        }

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Rotation logic (45-degree increments with Q/E)
        if (kb.IsKeyDown(Keys.Q) && !_prevKb.IsKeyDown(Keys.Q))
            _targetYaw -= MathHelper.ToRadians(45f);
        if (kb.IsKeyDown(Keys.E) && !_prevKb.IsKeyDown(Keys.E))
            _targetYaw += MathHelper.ToRadians(45f);

        // Smooth yaw interpolation
        _cameraYaw = MathHelper.Lerp(_cameraYaw, _targetYaw, RotationSpeed * dt);

        // Movement logic
        float moveSpeed = 10f * dt;
        Vector3 moveDir = Vector3.Zero;

        if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) moveDir.Y -= 1;
        if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)) moveDir.Y += 1;
        if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) moveDir.X -= 1;
        if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) moveDir.X += 1;

        if (moveDir != Vector3.Zero)
        {
            moveDir.Normalize();
            // Rotate move direction by camera yaw
            var rotMatrix = Matrix.CreateRotationZ(_cameraYaw);
            moveDir = Vector3.Transform(moveDir, rotMatrix);
            _cameraTarget += moveDir * moveSpeed;
        }

        // Zoom logic
        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0)
        {
            _cameraDistance -= scrollDelta * 0.01f;
            _cameraDistance = MathHelper.Clamp(_cameraDistance, 5f, 50f);
            _prevScrollValue = mouse.ScrollWheelValue;
        }
        
        // Update vision if in combat
        if (_combatManager.InCombat)
        {
            // Only recalculate if needed
            if (_visionNeedsUpdate)
            {
                RecalculateVision();
            }
        }

        UpdateCameraMatrices();

        _prevKb = kb;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        float distance = Vector2.Distance(start, end);
        float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        sb.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(distance, thickness), SpriteEffects.None, 0f);
    }
    
    private void DrawTileWithLighting(SpriteBatch sb, int x, int y, int z, Vector2 origin, float tileW, float tileH)
    {
        if (!_showVisionOverlay || _playerCreature == null)
            return;
        
        bool isVisible = _visionSystem.IsVisible(x, y, z);
        var tint = _visionSystem.GetFogOfWarTint(x, y, z, isVisible, _playerCreature);
        
        // If tile is completely black, draw black overlay
        if (tint == Color.Black)
        {
            float a = tileW * 0.5f;
            float b = tileH * 0.5f;
            // Add Z offset for height
            float zOffset = -z * (tileH * 0.5f);
            var center = origin + new Vector2(((x + 0.5f) - (y + 0.5f)) * a, ((x + 0.5f) + (y + 0.5f)) * b + zOffset);
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
            float zOffset = -z * (tileH * 0.5f);
            var center = origin + new Vector2(((x + 0.5f) - (y + 0.5f)) * a, ((x + 0.5f) + (y + 0.5f)) * b + zOffset);
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
        // Only apply vision checks when in combat with vision overlay enabled
        if (_combatManager.InCombat && _showVisionOverlay && !_visionSystem.IsVisible(creature.X, creature.Y, creature.Z))
        {
            return;
        }
        
        float a = tileW * 0.5f;
        float b = tileH * 0.5f;
        
        // Scale cube size based on creature size
        float sizeMultiplier = creature.Size switch
        {
            CreatureSize.Tiny => 0.4f,
            CreatureSize.Small => 0.7f,
            CreatureSize.Medium => 1.0f,
            CreatureSize.Large => 1.5f,
            CreatureSize.Huge => 2.0f,
            CreatureSize.Gargantuan => 2.5f,
            _ => 1.0f
        };
        
        // Calculate cube dimensions
        float cubeWidth = tileW * 0.4f * sizeMultiplier;
        float cubeHeight = tileH * 2.0f * sizeMultiplier;
        
        // Add Z offset for height (creatures appear higher on screen when at higher Z)
        float zOffset = -creature.Z * (tileH * 0.5f);
        var baseCenter = origin + new Vector2(((creature.X + 0.5f) - (creature.Y + 0.5f)) * a, ((creature.X + 0.5f) + (creature.Y + 0.5f)) * b + zOffset);
        
        Color creatureColor = creature.DisplayColor;
        
        // Apply lighting tint if vision overlay is enabled
        if (_showVisionOverlay && _playerCreature != null)
        {
            var tint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, true, _playerCreature);
            creatureColor = new Color(
                (creature.DisplayColor.R * tint.R) / 255,
                (creature.DisplayColor.G * tint.G) / 255,
                (creature.DisplayColor.B * tint.B) / 255
            );
        }
        
        // Draw isometric cube
        DrawIsometricCube(sb, baseCenter, cubeWidth, cubeHeight, creatureColor);
        
        // Draw shadow on ground level if elevated (Z > 0)
        if (creature.Z > 0)
        {
            float groundZOffset = 0;
            var shadowCenter = origin + new Vector2(((creature.X + 0.5f) - (creature.Y + 0.5f)) * a, ((creature.X + 0.5f) + (creature.Y + 0.5f)) * b + groundZOffset);
            
            // Draw shadow as flat diamond
            float shadowSize = cubeWidth * 0.6f;
            var shadowTop = shadowCenter + new Vector2(0, -shadowSize * 0.5f);
            var shadowRight = shadowCenter + new Vector2(shadowSize * 0.5f, 0);
            var shadowBottom = shadowCenter + new Vector2(0, shadowSize * 0.5f);
            var shadowLeft = shadowCenter + new Vector2(-shadowSize * 0.5f, 0);
            
            DrawLine(sb, _pixel, shadowTop, shadowRight, Color.Black * 0.3f, 2f);
            DrawLine(sb, _pixel, shadowRight, shadowBottom, Color.Black * 0.3f, 2f);
            DrawLine(sb, _pixel, shadowBottom, shadowLeft, Color.Black * 0.3f, 2f);
            DrawLine(sb, _pixel, shadowLeft, shadowTop, Color.Black * 0.3f, 2f);
            
            // Draw vertical line connecting creature to shadow
            DrawLine(sb, _pixel, baseCenter, shadowCenter, Color.Gray * 0.3f, 1f);
        }
        
        // Draw vision type indicators (top of cube)
        var topOfCube = baseCenter - new Vector2(0, cubeHeight);
        int indicatorX = (int)topOfCube.X + (int)(cubeWidth * 0.25f);
        int indicatorY = (int)topOfCube.Y - 8;
        
        if (creature.HasTrueSight)
        {
            sb.Draw(_pixel, new Rectangle(indicatorX, indicatorY, 6, 6), Color.Gold);
        }
        else if (creature.HasBlindSight)
        {
            sb.Draw(_pixel, new Rectangle(indicatorX, indicatorY, 6, 6), Color.Cyan);
        }
        else if (creature.HasTremorsense)
        {
            sb.Draw(_pixel, new Rectangle(indicatorX, indicatorY, 6, 6), Color.Orange);
        }
        else if (creature.DarkvisionRange >= 120)
        {
            sb.Draw(_pixel, new Rectangle(indicatorX, indicatorY, 5, 5), Color.Purple);
        }
        else if (creature.DarkvisionRange > 0)
        {
            sb.Draw(_pixel, new Rectangle(indicatorX, indicatorY, 5, 5), Color.Yellow);
        }
        
        // Draw sunlight sensitivity indicator (top-left)
        if (creature.HasSunlightSensitivity && (_visionSystem.GlobalDaylight || _visionSystem.GetLightLevel(creature.X, creature.Y) == LightType.Bright))
        {
            sb.Draw(_pixel, new Rectangle(indicatorX - (int)(cubeWidth * 0.5f), indicatorY, 5, 5), Color.Orange);
        }
        
        // Draw condition indicator (on front face of cube)
        if (creature.Conditions != Condition.None)
        {
            sb.Draw(_pixel, new Rectangle((int)baseCenter.X - 3, (int)baseCenter.Y - (int)(cubeHeight * 0.5f) - 3, 6, 6), Color.Red);
        }
        
        // Draw health bar above cube
        if (_font != null)
        {
            int barWidth = (int)(cubeWidth * 1.2f);
            int barHeight = 5;
            int barX = (int)topOfCube.X - barWidth / 2;
            int barY = (int)topOfCube.Y - 15;
            
            // Background
            sb.Draw(_pixel, new Rectangle(barX, barY, barWidth, barHeight), Color.DarkRed);
            // Health
            float healthPercent = (float)creature.CurrentHP / creature.MaxHP;
            sb.Draw(_pixel, new Rectangle(barX, barY, (int)(barWidth * healthPercent), barHeight), Color.Green);
            
            // Name (with size indicator and Z-level)
            var safeName = SafeString(creature.Name);
            var sizePrefix = creature.Size switch
            {
                CreatureSize.Tiny => "[T]",
                CreatureSize.Small => "[S]",
                CreatureSize.Medium => "[M]",
                CreatureSize.Large => "[L]",
                CreatureSize.Huge => "[H]",
                CreatureSize.Gargantuan => "[G]",
                _ => ""
            };
            var flyingIndicator = creature.IsFlying ? "✈" : "";
            var displayName = $"{sizePrefix} {safeName} [Z{creature.Z}]{flyingIndicator}";
            var nameSize = _font.MeasureString(displayName);
            sb.DrawString(_font, displayName, new Vector2(topOfCube.X - nameSize.X * 0.25f, barY - 18), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }
    
    private void Draw3DGrid(int zLevel)
    {
        _basicEffect.World = Matrix.Identity;
        _basicEffect.DiffuseColor = Color.White.ToVector3();
        _basicEffect.Alpha = 1.0f;
        _basicEffect.LightingEnabled = false; // Grid lines shouldn't be affected by light

        int range = 20;

        // Draw grid lines at zLevel
        for (int x = -range; x <= range; x++)
        {
            Draw3DLine(new Vector3(x, -range, zLevel), new Vector3(x, range, zLevel), Color.Gray * 0.5f);
        }
        for (int y = -range; y <= range; y++)
        {
            Draw3DLine(new Vector3(-range, y, zLevel), new Vector3(range, y, zLevel), Color.Gray * 0.5f);
        }

        // Draw filled tiles for non-empty cells in the grid at this level
        _basicEffect.LightingEnabled = true;
        foreach (var cell in _grid.EnumerateNonEmpty())
        {
            if (cell.Key.z == zLevel && cell.Value)
            {
                Draw3DTile(cell.Key.x, cell.Key.y, cell.Key.z, Color.SlateGray);
            }
            else if (cell.Key.z < zLevel && cell.Value)
            {
                // Draw lower levels with transparency
                Draw3DTile(cell.Key.x, cell.Key.y, cell.Key.z, Color.SlateGray * 0.3f);
            }
        }
    }

    private void Draw3DLine(Vector3 start, Vector3 end, Color color)
    {
        var vertices = new[]
        {
            new VertexPositionColor(start, color),
            new VertexPositionColor(end, color)
        };

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1);
        }
    }

    private void Draw3DTile(int x, int y, int z, Color color)
    {
        _basicEffect.World = Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;

        GraphicsDevice.SetVertexBuffer(_tileVertexBuffer);
        GraphicsDevice.Indices = _tileIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
        }
    }

    private void Draw3DCreatures()
    {
        if (_combatManager.InCombat)
        {
            foreach (var creature in _combatManager.Combatants)
            {
                if (creature.IsAlive())
                {
                    Draw3DCreature(creature);
                }
            }
        }
        else if (_playerCreature != null)
        {
            Draw3DCreature(_playerCreature);
            foreach (var creature in _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive()))
            {
                Draw3DCreature(creature);
            }
        }
    }

    private void Draw3DCreature(Creature creature)
    {
        if (_combatManager.InCombat && _showVisionOverlay && !_visionSystem.IsVisible(creature.X, creature.Y, creature.Z))
            return;

        Color color = creature.DisplayColor;

        float sizeMultiplier = creature.Size switch
        {
            CreatureSize.Tiny => 0.4f,
            CreatureSize.Small => 0.7f,
            CreatureSize.Medium => 0.9f,
            CreatureSize.Large => 1.5f,
            CreatureSize.Huge => 2.0f,
            CreatureSize.Gargantuan => 2.5f,
            _ => 0.9f
        };

        Draw3DCube(creature.X, creature.Y, creature.Z, sizeMultiplier, color);

        // Draw shadow on ground if elevated
        if (creature.Z > 0)
        {
            Draw3DTile(creature.X, creature.Y, 0, Color.Black * 0.3f);
            Draw3DLine(new Vector3(creature.X, creature.Y, creature.Z), new Vector3(creature.X, creature.Y, 0), Color.Gray * 0.3f);
        }
    }

    private void Draw3DCreatureUI(Creature creature)
    {
        if (_combatManager.InCombat && _showVisionOverlay && !_visionSystem.IsVisible(creature.X, creature.Y, creature.Z))
            return;

        if (_font == null) return;

        // Project creature top position to screen
        Vector3 worldPos = new Vector3(creature.X, creature.Y, creature.Z + 1.2f); // Slightly above the cube
        Vector3 screenPos = GraphicsDevice.Viewport.Project(worldPos, _basicEffect.Projection, _basicEffect.View, Matrix.Identity);

        if (screenPos.Z < 0 || screenPos.Z > 1) return; // Behind camera or too far

        Vector2 pos = new Vector2(screenPos.X, screenPos.Y);

        // Draw health bar
        int barWidth = 60;
        int barHeight = 6;
        Rectangle bgRect = new Rectangle((int)pos.X - barWidth / 2, (int)pos.Y - 20, barWidth, barHeight);
        _spriteBatch.Draw(_pixel, bgRect, Color.DarkRed);

        float hpPercent = (float)creature.CurrentHP / creature.MaxHP;
        Rectangle hpRect = new Rectangle(bgRect.X, bgRect.Y, (int)(barWidth * hpPercent), barHeight);
        _spriteBatch.Draw(_pixel, hpRect, Color.Green);

        // Draw name label in a "bubble"
        string displayName = $"{creature.Name} [Z{creature.Z}]";
        if (creature.IsFlying) displayName += " ✈";

        Vector2 nameSize = _font.MeasureString(displayName) * 0.6f;
        Rectangle nameBg = new Rectangle((int)pos.X - (int)nameSize.X / 2 - 4, (int)pos.Y - 40, (int)nameSize.X + 8, (int)nameSize.Y + 4);

        _spriteBatch.Draw(_pixel, nameBg, Color.Black * 0.6f);
        _spriteBatch.DrawString(_font, displayName, new Vector2(nameBg.X + 4, nameBg.Y + 2), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
    }

    private (int x, int y)? GetHoveredTile()
    {
        var mouse = Mouse.GetState();
        var vp = GraphicsDevice.Viewport;

        Vector3 nearSource = new Vector3(mouse.X, mouse.Y, 0f);
        Vector3 farSource = new Vector3(mouse.X, mouse.Y, 1f);

        Vector3 nearPoint = vp.Unproject(nearSource, _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
        Vector3 farPoint = vp.Unproject(farSource, _basicEffect.Projection, _basicEffect.View, Matrix.Identity);

        Vector3 direction = farPoint - nearPoint;
        direction.Normalize();

        Ray ray = new Ray(nearPoint, direction);

        // Plane at Z = _currentViewLevel
        Plane plane = new Plane(Vector3.UnitZ, -_currentViewLevel);

        float? d = ray.Intersects(plane);
        if (d.HasValue)
        {
            Vector3 hit = nearPoint + direction * d.Value;
            return ((int)Math.Round(hit.X), (int)Math.Round(hit.Y));
        }

        return null;
    }

    private void Draw3DCube(float x, float y, float z, float scale, Color color)
    {
        _basicEffect.World = Matrix.CreateScale(scale) * Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = 1.0f;
        _basicEffect.LightingEnabled = true;

        GraphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
        GraphicsDevice.Indices = _cubeIndexBuffer;

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }
    }

    private void DrawIsometricCube(SpriteBatch sb, Vector2 baseCenter, float width, float height, Color color)
    {
        float halfWidth = width * 0.5f;
        float quarterWidth = width * 0.25f;
        
        // Define cube corners in isometric space
        // Base (bottom) corners
        var bottomFrontLeft = baseCenter + new Vector2(-halfWidth, 0);
        var bottomFrontRight = baseCenter + new Vector2(0, quarterWidth);
        var bottomBackRight = baseCenter + new Vector2(halfWidth, 0);
        var bottomBackLeft = baseCenter + new Vector2(0, -quarterWidth);
        
        // Top corners
        var topFrontLeft = bottomFrontLeft - new Vector2(0, height);
        var topFrontRight = bottomFrontRight - new Vector2(0, height);
        var topBackRight = bottomBackRight - new Vector2(0, height);
        var topBackLeft = bottomBackLeft - new Vector2(0, height);
        
        // Draw filled faces with different shades
        Color leftFaceColor = new Color(
            (int)(color.R * 0.6f),
            (int)(color.G * 0.6f),
            (int)(color.B * 0.6f)
        );
        Color rightFaceColor = new Color(
            (int)(color.R * 0.8f),
            (int)(color.G * 0.8f),
            (int)(color.B * 0.8f)
        );
        Color topFaceColor = color;
        
        // Fill left face
        DrawFilledQuad(sb, bottomFrontLeft, topFrontLeft, topBackLeft, bottomBackLeft, leftFaceColor);
        
        // Fill right face
        DrawFilledQuad(sb, bottomFrontRight, topFrontRight, topBackRight, bottomBackRight, rightFaceColor);
        
        // Fill top face
        DrawFilledQuad(sb, topFrontLeft, topFrontRight, topBackRight, topBackLeft, topFaceColor);
        
        // Draw cube edges (outline)
        Color edgeColor = Color.Black * 0.5f;
        float edgeThickness = 2f;
        
        // Bottom edges
        DrawLine(sb, _pixel, bottomFrontLeft, bottomFrontRight, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, bottomFrontRight, bottomBackRight, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, bottomBackRight, bottomBackLeft, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, bottomBackLeft, bottomFrontLeft, edgeColor, edgeThickness);
        
        // Top edges
        DrawLine(sb, _pixel, topFrontLeft, topFrontRight, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, topFrontRight, topBackRight, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, topBackRight, topBackLeft, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, topBackLeft, topFrontLeft, edgeColor, edgeThickness);
        
        // Vertical edges
        DrawLine(sb, _pixel, bottomFrontLeft, topFrontLeft, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, bottomFrontRight, topFrontRight, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, bottomBackRight, topBackRight, edgeColor, edgeThickness);
        DrawLine(sb, _pixel, bottomBackLeft, topBackLeft, edgeColor, edgeThickness);
    }
    
    private void DrawFilledQuad(SpriteBatch sb, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Color color)
    {
        // Draw quad as filled area using horizontal lines
        // Find min and max Y to determine scanning range
        float minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        float maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
        
        int steps = (int)(maxY - minY) + 1;
        
        for (int i = 0; i < steps; i++)
        {
            float y = minY + i;
            var intersections = new List<float>();
            
            // Check intersection with each edge
            AddLineIntersection(intersections, p1, p2, y);
            AddLineIntersection(intersections, p2, p3, y);
            AddLineIntersection(intersections, p3, p4, y);
            AddLineIntersection(intersections, p4, p1, y);
            
            if (intersections.Count >= 2)
            {
                intersections.Sort();
                for (int j = 0; j < intersections.Count - 1; j += 2)
                {
                    var start = new Vector2(intersections[j], y);
                    var end = new Vector2(intersections[j + 1], y);
                    if (Vector2.Distance(start, end) > 0.5f)
                    {
                        DrawLine(sb, _pixel, start, end, color, 1f);
                    }
                }
            }
        }
    }
    
    private void AddLineIntersection(List<float> intersections, Vector2 p1, Vector2 p2, float y)
    {
        // Find X coordinate where line segment intersects horizontal line at y
        if ((p1.Y <= y && p2.Y >= y) || (p2.Y <= y && p1.Y >= y))
        {
            if (Math.Abs(p2.Y - p1.Y) > 0.001f)
            {
                float t = (y - p1.Y) / (p2.Y - p1.Y);
                if (t >= 0 && t <= 1)
                {
                    float x = p1.X + t * (p2.X - p1.X);
                    intersections.Add(x);
                }
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        UpdateCameraMatrices();

        var vp = GraphicsDevice.Viewport;

        // Draw 3D elements if playing
        if (_state == AppState.Playing && !_showCharacterSheet && !_showCampaignMap)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Draw3DGrid(_currentViewLevel);
            Draw3DCreatures();

            // Draw highlight for hovered tile
            var hovered = GetHoveredTile();
            if (hovered.HasValue)
            {
                Draw3DTile(hovered.Value.x, hovered.Value.y, _currentViewLevel, Color.Yellow * 0.5f);
            }

            // Draw origin axis (in 3D)
            float axisLength = 5f;
            Draw3DLine(Vector3.Zero, new Vector3(axisLength, 0, 0), Color.Red);
            Draw3DLine(Vector3.Zero, new Vector3(0, axisLength, 0), Color.Lime);
            Draw3DLine(Vector3.Zero, new Vector3(0, 0, axisLength), Color.Blue);
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw 3D elements UI
        if (_state == AppState.Playing && !_showCharacterSheet && !_showCampaignMap)
        {
            if (_combatManager.InCombat)
            {
                foreach (var creature in _combatManager.Combatants)
                    if (creature.IsAlive()) Draw3DCreatureUI(creature);
            }
            else if (_playerCreature != null)
            {
                Draw3DCreatureUI(_playerCreature);
                foreach (var creature in _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive()))
                    Draw3DCreatureUI(creature);
            }
        }

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
                var title = _isMultiplayerMode ? "Choose a Character (Multiplayer)" : "Choose a Character (Single Player)";
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
        
        // CAMPAIGN SELECT
        if (_state == AppState.CampaignSelect)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 80;
            int menuHeight = titleHeight + Math.Max(1, _campaigns.Count + 1) * (itemHeight + padding) + padding + 40;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            _spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

            if (_font != null)
            {
                var title = "Select a Campaign";
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
                var hint = "Press Delete to remove campaign | Esc to go back";
                var hintSize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(menuRect.X + (menuWidth - hintSize.X) / 2, menuRect.Y + menuHeight - 28), Color.White * 0.7f);
            }

            for (int i = 0; i < _campaigns.Count; i++)
            {
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
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
                    
                    var deleteText = "Delete";
                    var deleteSize = _font.MeasureString(deleteText);
                    _spriteBatch.DrawString(_font, deleteText, new Vector2(deleteRect.X + (deleteRect.Width - deleteSize.X) / 2, deleteRect.Y + (deleteRect.Height - deleteSize.Y) / 2), Color.White);
                }
            }

            // "Create New" option
            {
                int newIndex = _campaigns.Count;
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + newIndex * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (newIndex == _campaignIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    var label = "Create New Campaign";
                    var m = _font.MeasureString(label);
                    var p = new Vector2(itemRect.X + 12, itemRect.Y + (itemRect.Height - m.Y) / 2);
                    var textCol = (newIndex == _campaignIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, label, p, textCol);
                }
            }

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

        // CHARACTER SHEET
        if (_showCharacterSheet && _state == AppState.Playing && _currentCharacter != null)
        {
            _characterSheet.Draw(_spriteBatch, GraphicsDevice, _currentCharacter);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }
        
        // CAMPAIGN MAP
        if (_showCampaignMap && _state == AppState.Playing && _currentCampaign != null)
        {
            _campaignMapViewer.Draw(_spriteBatch, GraphicsDevice, _currentCampaign);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        // GAMEPLAY RENDERING (NOW IN 3D)
        int? hoveredX = null;
        int? hoveredY = null;

        if (_state == AppState.Playing && !_showCharacterSheet && !_showCampaignMap)
        {
            var hovered = GetHoveredTile();
            if (hovered.HasValue)
            {
                hoveredX = hovered.Value.x;
                hoveredY = hovered.Value.y;
            }
        }
        
        // Draw fog of war / lighting overlay
        if (_combatManager.InCombat && _showVisionOverlay)
        {
            for (int z = 0; z <= Math.Max(_currentViewLevel, _playerCreature?.Z ?? 0); z++)
            {
                for (int y = ymin; y <= ymax; y++)
                {
                    for (int x = xmin; x <= xmax; x++)
                    {
                        DrawTileWithLighting(_spriteBatch, x, y, z, origin, tileW, tileH);
                    }
                }
            }
            
            // Draw light sources
            foreach (var source in _visionSystem._lightSources)
            {
                if (source.IsActive)
                {
                    float a = tileW * 0.5f;
                    float b = tileH * 0.5f;
                    float zOffset = -source.Z * (b * 0.5f);
                    var center = origin + new Vector2(((source.X + 0.5f) - (source.Y + 0.5f)) * a, ((source.X + 0.5f) + (source.Y + 0.5f)) * b + zOffset);
                    
                    int lightRadius = (int)(b * 0.8f);
                    _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - lightRadius, (int)center.Y - lightRadius, lightRadius * 2, lightRadius * 2), null, source.LightColor * 0.8f, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                }
            }
            
            // Draw area effects (fog clouds, darkness spells)
            foreach (var effect in _visionSystem._areaEffects)
            {
                float a = tileW * 0.5f;
                float b = tileH * 0.5f;
                float zOffset = -effect.Z * (b * 0.5f);
                var center = origin + new Vector2(((effect.X + 0.5f) - (effect.Y + 0.5f)) * a, ((effect.X + 0.5f) + (effect.Y + 0.5f)) * b + zOffset);
                
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
                float zOffset = -_playerCreature.Z * (b * 0.5f);
                var center = origin + new Vector2(((_playerCreature.X + 0.5f) - (_playerCreature.Y + 0.5f)) * a, ((_playerCreature.X + 0.5f) + (_playerCreature.Y + 0.5f)) * b + zOffset);
                
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
            int panelHeight = 220;
            var combatPanel = new Rectangle(0, 0, vp.Width, panelHeight);
            _spriteBatch.Draw(_pixel, combatPanel, Color.Black * 0.8f);
            
            if (_font != null)
            {
                int y = 10;
                
                // Round counter
                var roundText = $"=== ROUND {_combatManager.CurrentRound} ===";
                var roundSize = _font.MeasureString(roundText);
                _spriteBatch.DrawString(_font, roundText, new Vector2((vp.Width - roundSize.X) / 2, y), Color.Gold);
                y += 30;
                
                // Current turn
                var currentCombatant = _combatManager.CurrentCombatant;
                if (currentCombatant != null)
                {
                    var turnText = $"Turn: {SafeString(currentCombatant.Name)} (HP: {currentCombatant.CurrentHP}/{currentCombatant.MaxHP})";
                    _spriteBatch.DrawString(_font, turnText, new Vector2(10, y), Color.Yellow);
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
                    
                    _spriteBatch.DrawString(_font, "Action:", new Vector2(10, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, actionIcon, new Vector2(80, y), actionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, "Bonus:", new Vector2(130, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, bonusIcon, new Vector2(200, y), bonusColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, "Reaction:", new Vector2(250, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, reactionIcon, new Vector2(340, y), reactionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, "Move:", new Vector2(390, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, movementText, new Vector2(450, y), movementColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
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
                    if (_playerCreature != null && _playerCreature.DarkvisionRange > 0)
                    {
                        _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), new Color(96, 96, 96));
                        _spriteBatch.DrawString(_font, "Darkness (Darkvision, Grayscale)", new Vector2(legendX + 25, legendY), new Color(150, 150, 180), 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                        legendY += 25;
                    }
                    
                    // Complete darkness
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.Black);
                    _spriteBatch.DrawString(_font, "Darkness (Heavily Obscured)", new Vector2(legendX + 25, legendY), Color.DarkGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    legendY += 35;
                    
                    // Creature indicators (top of cube)
                    _spriteBatch.DrawString(_font, "Creature Indicators:", new Vector2(legendX, legendY), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                    legendY += 20;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 5, 5), Color.Gold);
                    _spriteBatch.DrawString(_font, "Truesight (See All)", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 5, 5), Color.Cyan);
                    _spriteBatch.DrawString(_font, "Blindsight", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 5, 5), Color.Orange);
                    _spriteBatch.DrawString(_font, "Tremorsense", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Purple);
                    _spriteBatch.DrawString(_font, "Superior Darkvision 120ft", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Yellow);
                    _spriteBatch.DrawString(_font, "Darkvision 60ft", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 20;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Orange);
                    _spriteBatch.DrawString(_font, "Sunlight Sensitivity", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                    
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 4, 4), Color.Red);
                    _spriteBatch.DrawString(_font, "Has Condition", new Vector2(legendX + 10, legendY - 3), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    legendY += 18;
                }
                
                // Instructions
                var hint = "Press Tab to toggle combat UI | ESC for menu | PageUp/Down: Change level";
                var hintSize = _font.MeasureString(hint);
                _spriteBatch.DrawString(_font, hint, new Vector2(vp.Width - hintSize.X - 10, panelHeight - 25), Color.White * 0.7f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                
                // Test keybinding
                var testHint = "Test: [B]linded [F]og [K]Darkness | [Space]Fly [R]Up [T]Down | [X]STR [Z]Stealth [N]Save";
                var testHintSize = _font.MeasureString(testHint);
                _spriteBatch.DrawString(_font, testHint, new Vector2(vp.Width - testHintSize.X - 10, panelHeight - 50), Color.Yellow * 0.6f, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                
                // Display current view level
                var levelHint = $"View Level: Z{_currentViewLevel}";
                if (_playerCreature != null && _playerCreature.CanFly)
                {
                    levelHint += $" | Player: Z{_playerCreature.Z} {(_playerCreature.IsFlying ? "[FLYING]" : "[GROUND]")}";
                }
                var levelHintSize = _font.MeasureString(levelHint);
                _spriteBatch.DrawString(_font, levelHint, new Vector2(10, panelHeight - 25), Color.Cyan * 0.8f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
        
        // Tile tooltip (outside combat panel)
        if (_font != null && hoveredX.HasValue && hoveredY.HasValue && _combatManager.InCombat && _showVisionOverlay)
        {
            int tx = hoveredX.Value;
            int ty = hoveredY.Value;
            int tz = _currentViewLevel;
            
            var lightLevel = _visionSystem.GetLightLevel(tx, ty, tz);
            var isVisible = _visionSystem.IsVisible(tx, ty, tz);
            
            var creature = _combatManager.GetCreatureAt(tx, ty, tz);
            
            var tooltip = $"Tile ({tx}, {ty}, Z{tz}) | Light: {lightLevel} | Visible: {isVisible}";
            if (creature != null && isVisible)
            {
                var sizeDesc = SizeHelper.GetSpaceDescription(creature.Size);
                var flyingStatus = creature.IsFlying ? " [FLYING]" : "";
                tooltip += $" | {creature.Name} ({creature.Size}, {sizeDesc}) HP: {creature.CurrentHP}/{creature.MaxHP}{flyingStatus}";
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

public struct VertexPositionNormalColor : IVertexType
{
    public Vector3 Position;
    public Vector3 Normal;
    public Color Color;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
    (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VertexPositionNormalColor(Vector3 position, Vector3 normal, Color color)
    {
        Position = position;
        Normal = normal;
        Color = color;
    }
}
