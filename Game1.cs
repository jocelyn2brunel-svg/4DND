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

    private InfiniteGrid3D<TileType> _tacticalMap = null!;
    private Texture2D _pixel = null!;

    // 3D Camera system
    private Vector3 _cameraTarget = Vector3.Zero;
    private float _cameraYaw = MathHelper.ToRadians(45f);
    private float _cameraPitch = MathHelper.ToRadians(35f);  // Angle POSITIF maintenant
    private float _cameraDistance = 20f;
    private float _targetYaw = MathHelper.ToRadians(45f);
    private const float RotationSpeed = 5f; // Rad per second for transition

    private BasicEffect _basicEffect = null!;
    private VertexBuffer _cubeVertexBuffer = null!;
    private IndexBuffer _cubeIndexBuffer = null!;
    private VertexBuffer _capsuleVertexBuffer = null!;
    private IndexBuffer _capsuleIndexBuffer = null!;
    private int _capsulePrimitiveCount = 0;
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
    private JournalUI _journalUI = null!;
    private CampaignCreation _campaignCreation = null!;
    private CampaignMapViewer _campaignMapViewer = null!;

    private bool _isMenuOpen = false;
    private int _menuIndex = 0;
    private readonly string[] _menuItems = new[] { "Continue", "Options", "Main Menu", "Desktop" };
    
    private bool _showCampaignMap = false;

    private KeyboardState _prevKb;
    private SpriteFont _font = null!;
    private HashSet<char> _supportedChars = new();

    private bool _showCharacterSheet = false;
    private bool _showJournal = false;

    private CombatManager _combatManager = new();
    private Creature _playerCreature = null;
    private List<string> _combatLog = new();
    private const int MAX_COMBAT_LOG = 5;
    
    // Vision and lighting system
    private VisionSystem _visionSystem = new();
    private bool _showVisionOverlay = true;
    private bool _visionNeedsUpdate = false;
    private bool _wasPlayerMovingForVision = false;
    private (int X, int Y, int Z)? _lastRoundedVisualTile = null;
    
    // Combat UI state
    private enum CombatAction { None, Move, Attack, BonusAction, EndTurn }
    private CombatAction _selectedAction = CombatAction.None;
    private bool _showBonusActionMenu = false;
    private bool _showCombatUI = false;
    private const int CombatTopPanelHeight = 220;
    private MouseState _prevMouse;
    private DiceRoll3DAnimation _diceRollAnimation = new();
    private readonly Random _random = new();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        
        // Start in standard windowed mode for now
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.HardwareModeSwitch = false;
        _graphics.IsFullScreen = false;
    }

    protected override void Initialize()
    {
        // Keep a normal bordered window at startup
        Window.IsBorderless = false;
        _graphics.ApplyChanges();

        _tacticalMap = new InfiniteGrid3D<TileType>(GetProceduralTile);
        
        _prevKb = Keyboard.GetState();
        _prevMouse = Mouse.GetState();
        base.Initialize();
    }

    private TileType GetProceduralTile(int x, int y, int z)
    {
        if (z != 0) return TileType.Empty;

        // Convert tactical coordinates to hex coordinates
        (int q, int r) = Campaign.TacticalToHex(x, y);

        // Deterministic terrain selection without allocating Random object every call.
        // Simple hash of hex coordinates.
        int h = q * 37 + r * 101;
        h ^= (h >> 13);
        h *= 0x5bd1e995;
        h ^= (h >> 15);

        int dominantType = Math.Abs(h % 100);

        // Even/Odd hex color variation to make boundaries more visible
        bool isEven = (q + r) % 2 == 0;

        if (dominantType < 70) // 70% chance of primary terrain
        {
            return isEven ? TileType.Grass : TileType.Floor;
        }
        else if (dominantType < 90) // 20% chance of secondary terrain
        {
            return TileType.DifficultTerrain;
        }
        else // 10% chance of rare terrain (water)
        {
            return (q % 5 == 0 && r % 5 == 0) ? TileType.Water : TileType.Grass;
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try
        {
            _font = Content.Load<SpriteFont>("DefaultFont");
            _supportedChars = new HashSet<char>(_font.Characters);
        }
        catch (Microsoft.Xna.Framework.Content.ContentLoadException)
        {
            _font = null!;
            _supportedChars = new HashSet<char>();
            System.Console.WriteLine("Warning: DefaultFont not found. Build the Content/Content.mgcb with the MonoGame Pipeline Tool to generate DefaultFont.xnb. Menu text will be hidden.");
        }

        _characterCreation = new CharacterCreation(_font, _pixel);
        _characterSheet = new CharacterSheet(_font, _pixel);
        _journalUI = new JournalUI(_font, _pixel);
        _campaignCreation = new CampaignCreation(_font, _pixel);
        _campaignMapViewer = new CampaignMapViewer(_font, _pixel);

        Initialize3DRendering();
        _combatManager.TacticalMap = _tacticalMap;
        _visionSystem.TacticalMap = _tacticalMap;
        _visionSystem.GlobalDaylight = true; // Morning/Daylight by default

        // Procedural ground is now handled by InfiniteGrid3D factory.
        
        // Add some walls
        for (int i = -5; i <= 5; i++)
        {
            if (i == 0) continue; // Doorway
            _tacticalMap.Set(i, 3, 0, TileType.Wall);
            _tacticalMap.Set(i, 3, 1, TileType.Wall);
        }

        // Add some platforms at different heights
        for (int z = 1; z <= 3; z++)
        {
            for (int i = -3; i <= 3; i++)
            {
                _tacticalMap.Set(i, i, z, TileType.Floor);
                _tacticalMap.Set(-i, i, z, TileType.Floor);
            }
        }
        
        try
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir));
        }
        catch { }

        LoadCharacters();
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
        _selectedAction = CombatAction.Move;
        AddToCombatLog("Combat started!");
        AddToCombatLog($"Round {_combatManager.CurrentRound} begins!");
        
        // Setup lighting for combat
        SetupCombatLighting();
        UpdateVision();
    }

    private bool TryStartCombatFromEnemyDetection()
    {
        if (_combatManager.InCombat || _playerCreature == null || _currentCharacter == null)
        {
            return false;
        }

        var spottingEnemy = _combatManager.Combatants
            .FirstOrDefault(enemy => !enemy.IsPlayer && enemy.IsAlive() && _visionSystem.CanSee(enemy, _playerCreature));

        if (spottingEnemy == null)
        {
            return false;
        }

        StartCombatWithNearbyEnemies();
        AddToCombatLog($"{spottingEnemy.Name} spotted you! Combat started automatically.");
        return true;
    }
    
    private bool TrySpawnRandomCreatureNearPlayer(int targetDistance = 20)
    {
        if (_playerCreature == null)
            return false;

        const int maxAttempts = 200;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int dx = _random.Next(-targetDistance, targetDistance + 1);
            int dyMagnitude = targetDistance - Math.Abs(dx);
            int dy = dyMagnitude == 0 ? 0 : (_random.Next(0, 2) == 0 ? -dyMagnitude : dyMagnitude);

            int spawnX = _playerCreature.X + dx;
            int spawnY = _playerCreature.Y + dy;
            int spawnZ = Math.Max(0, _playerCreature.Z);

            if (_combatManager.GetCreatureAt(spawnX, spawnY, spawnZ) != null)
                continue;

            var tile = _tacticalMap.Get(spawnX, spawnY, spawnZ);
            if (tile != TileType.Floor && tile != TileType.Grass && tile != TileType.DifficultTerrain)
                continue;

            int enemyType = _random.Next(0, 7);
            Creature enemy = enemyType switch
            {
                0 => Creature.CreateGoblin(spawnX, spawnY, spawnZ),
                1 => Creature.CreateOrc(spawnX, spawnY, spawnZ),
                2 => Creature.CreateSkeleton(spawnX, spawnY, spawnZ),
                3 => Creature.CreateWolf(spawnX, spawnY, spawnZ),
                4 => Creature.CreateKobold(spawnX, spawnY, spawnZ),
                5 => Creature.CreateUmberHulk(spawnX, spawnY, spawnZ),
                _ => Creature.CreateCouatl(spawnX, spawnY, spawnZ)
            };

            if (enemy.CanFly && _random.NextDouble() < 0.35)
                enemy.IsFlying = true;

            _combatManager.Combatants.Add(enemy);
            AddToCombatLog($"Spawn: {enemy.Name} apparait en ({spawnX}, {spawnY}, {spawnZ}).");
            UpdateVision();
            return true;
        }

        AddToCombatLog("Spawn impossible: aucune case valide trouvee a 20 cases.");
        return false;
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
            int originalX = _playerCreature.X;
            int originalY = _playerCreature.Y;
            int originalZ = _playerCreature.Z;

            if (_playerCreature.IsMoving())
            {
                _playerCreature.X = (int)MathF.Round(_playerCreature.VisualX);
                _playerCreature.Y = (int)MathF.Round(_playerCreature.VisualY);
                _playerCreature.Z = (int)MathF.Round(_playerCreature.VisualZ);
            }

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

            _playerCreature.X = originalX;
            _playerCreature.Y = originalY;
            _playerCreature.Z = originalZ;
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
        _basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f), aspectRatio, 0.1f, 1000f);
        Vector3 direction = new Vector3(0, _cameraDistance, 0);
        Matrix rotation = Matrix.CreateRotationX(_cameraPitch) * Matrix.CreateRotationZ(_cameraYaw);
        _basicEffect.View = Matrix.CreateLookAt(_cameraTarget + Vector3.Transform(direction, rotation), _cameraTarget, Vector3.UnitZ);
    }

    private void Initialize3DRendering()
    {
        _basicEffect = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true, LightingEnabled = true };
        _basicEffect.EnableDefaultLighting();

        var cubeVertices = new VertexPositionNormalColor[] {
            new(new(-0.5f,-0.5f,0), Vector3.Down, Color.White), new(new(0.5f,-0.5f,0), Vector3.Down, Color.White), new(new(0.5f,0.5f,0), Vector3.Down, Color.White), new(new(-0.5f,0.5f,0), Vector3.Down, Color.White),
            new(new(-0.5f,-0.5f,1), Vector3.Up, Color.White), new(new(0.5f,-0.5f,1), Vector3.Up, Color.White), new(new(0.5f,0.5f,1), Vector3.Up, Color.White), new(new(-0.5f,0.5f,1), Vector3.Up, Color.White),
            new(new(-0.5f,-0.5f,0), Vector3.Left, Color.White), new(new(-0.5f,0.5f,0), Vector3.Left, Color.White), new(new(-0.5f,0.5f,1), Vector3.Left, Color.White), new(new(-0.5f,-0.5f,1), Vector3.Left, Color.White),
            new(new(0.5f,-0.5f,0), Vector3.Right, Color.White), new(new(0.5f,-0.5f,1), Vector3.Right, Color.White), new(new(0.5f,0.5f,1), Vector3.Right, Color.White), new(new(0.5f,0.5f,0), Vector3.Right, Color.White),
            new(new(-0.5f,-0.5f,0), Vector3.Forward, Color.White), new(new(0.5f,-0.5f,0), Vector3.Forward, Color.White), new(new(0.5f,-0.5f,1), Vector3.Forward, Color.White), new(new(-0.5f,-0.5f,1), Vector3.Forward, Color.White),
            new(new(-0.5f,0.5f,0), Vector3.Backward, Color.White), new(new(-0.5f,0.5f,1), Vector3.Backward, Color.White), new(new(0.5f,0.5f,1), Vector3.Backward, Color.White), new(new(0.5f,0.5f,0), Vector3.Backward, Color.White)
        };
        _cubeVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), cubeVertices.Length, BufferUsage.WriteOnly);
        _cubeVertexBuffer.SetData(cubeVertices);
        var cubeIndices = new short[] { 0,1,2,0,2,3, 4,6,5,4,7,6, 8,9,10,8,10,11, 12,13,14,12,14,15, 16,17,18,16,18,19, 20,21,22,20,22,23 };
        _cubeIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, cubeIndices.Length, BufferUsage.WriteOnly);
        _cubeIndexBuffer.SetData(cubeIndices);

        var tileVertices = new VertexPositionNormalColor[] {
            new(new(-0.5f,-0.5f,0), Vector3.Up, Color.White), new(new(0.5f,-0.5f,0), Vector3.Up, Color.White), new(new(0.5f,0.5f,0), Vector3.Up, Color.White), new(new(-0.5f,0.5f,0), Vector3.Up, Color.White)
        };
        _tileVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), tileVertices.Length, BufferUsage.WriteOnly);
        _tileVertexBuffer.SetData(tileVertices);
        var tileIndices = new short[] { 0, 1, 2, 0, 2, 3 };
        _tileIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, tileIndices.Length, BufferUsage.WriteOnly);
        _tileIndexBuffer.SetData(tileIndices);

        const int longitudeSegments = 16;
        const int hemisphereLatitudeSegments = 8;
        const int cylinderSegments = 1;
        const float capsuleRadius = 0.5f;
        const float capsuleBodyHeight = 1.0f;
        float hemisphereCenterBottom = capsuleRadius;
        float hemisphereCenterTop = hemisphereCenterBottom + capsuleBodyHeight;

        var capsuleVertices = new List<VertexPositionNormalColor>();
        var capsuleIndices = new List<short>();

        short AddCapsuleVertex(Vector3 position, Vector3 normal)
        {
            capsuleVertices.Add(new VertexPositionNormalColor(position, Vector3.Normalize(normal), Color.White));
            return (short)(capsuleVertices.Count - 1);
        }

        void AddTriangle(short a, short b, short c)
        {
            capsuleIndices.Add(a);
            capsuleIndices.Add(b);
            capsuleIndices.Add(c);
        }

        var cylinderRings = new List<short[]>();
        for (int ring = 0; ring <= cylinderSegments; ring++)
        {
            float t = ring / (float)cylinderSegments;
            float z = hemisphereCenterBottom + (t * capsuleBodyHeight);
            var ringIndices = new short[longitudeSegments + 1];
            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float angle = MathHelper.TwoPi * lon / longitudeSegments;
                float x = MathF.Cos(angle) * capsuleRadius;
                float y = MathF.Sin(angle) * capsuleRadius;
                ringIndices[lon] = AddCapsuleVertex(new Vector3(x, y, z), new Vector3(x, y, 0f));
            }

            cylinderRings.Add(ringIndices);
        }

        for (int ring = 0; ring < cylinderSegments; ring++)
        {
            var lower = cylinderRings[ring];
            var upper = cylinderRings[ring + 1];
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                short l0 = lower[lon];
                short l1 = lower[lon + 1];
                short u0 = upper[lon];
                short u1 = upper[lon + 1];
                AddTriangle(l0, u0, u1);
                AddTriangle(l0, u1, l1);
            }
        }

        short[] BuildHemisphereRings(float centerZ, bool isUpper)
        {
            var seamRing = new short[longitudeSegments + 1];
            for (int lat = 1; lat <= hemisphereLatitudeSegments; lat++)
            {
                float t = lat / (float)hemisphereLatitudeSegments;
                float phi = t * MathHelper.PiOver2;
                float ringRadius = capsuleRadius * MathF.Cos(phi);
                float localZ = capsuleRadius * MathF.Sin(phi);
                float z = isUpper ? centerZ + localZ : centerZ - localZ;

                var currentRing = new short[longitudeSegments + 1];
                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    float angle = MathHelper.TwoPi * lon / longitudeSegments;
                    float x = MathF.Cos(angle) * ringRadius;
                    float y = MathF.Sin(angle) * ringRadius;
                    Vector3 position = new Vector3(x, y, z);
                    Vector3 normal = position - new Vector3(0f, 0f, centerZ);
                    currentRing[lon] = AddCapsuleVertex(position, normal);
                }

                if (lat == 1)
                {
                    seamRing = currentRing;
                }
                else
                {
                    for (int lon = 0; lon < longitudeSegments; lon++)
                    {
                        short p0 = seamRing[lon];
                        short p1 = seamRing[lon + 1];
                        short c0 = currentRing[lon];
                        short c1 = currentRing[lon + 1];
                        if (isUpper)
                        {
                            AddTriangle(p0, c0, c1);
                            AddTriangle(p0, c1, p1);
                        }
                        else
                        {
                            AddTriangle(p0, c1, c0);
                            AddTriangle(p0, p1, c1);
                        }
                    }

                    seamRing = currentRing;
                }
            }

            short pole = AddCapsuleVertex(new Vector3(0f, 0f, isUpper ? centerZ + capsuleRadius : centerZ - capsuleRadius), isUpper ? Vector3.Up : Vector3.Down);
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                short a = seamRing[lon];
                short b = seamRing[lon + 1];
                if (isUpper) AddTriangle(a, pole, b);
                else AddTriangle(a, b, pole);
            }

            return seamRing;
        }

        var lowerHemisphereSeam = BuildHemisphereRings(hemisphereCenterBottom, false);
        var upperHemisphereSeam = BuildHemisphereRings(hemisphereCenterTop, true);

        var lowerCylinderRing = cylinderRings[0];
        var upperCylinderRing = cylinderRings[^1];
        for (int lon = 0; lon < longitudeSegments; lon++)
        {
            AddTriangle(lowerHemisphereSeam[lon], lowerCylinderRing[lon + 1], lowerCylinderRing[lon]);
            AddTriangle(lowerHemisphereSeam[lon], lowerHemisphereSeam[lon + 1], lowerCylinderRing[lon + 1]);

            AddTriangle(upperHemisphereSeam[lon], upperCylinderRing[lon], upperCylinderRing[lon + 1]);
            AddTriangle(upperHemisphereSeam[lon], upperCylinderRing[lon + 1], upperHemisphereSeam[lon + 1]);
        }

        _capsuleVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), capsuleVertices.Count, BufferUsage.WriteOnly);
        _capsuleVertexBuffer.SetData(capsuleVertices.ToArray());
        _capsuleIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, capsuleIndices.Count, BufferUsage.WriteOnly);
        _capsuleIndexBuffer.SetData(capsuleIndices.ToArray());
        _capsulePrimitiveCount = capsuleIndices.Count / 3;
    }

    private void Draw3DGrid(int zLevel)
    {
        _basicEffect.World = Matrix.Identity;
        _basicEffect.LightingEnabled = true;
        _basicEffect.DiffuseColor = Vector3.One;

        List<VertexPositionNormalColor> wallVertices = new();
        List<VertexPositionNormalColor> tileVertices = new();

        // 1. Draw procedural ground around camera
        int viewDist = 40; // View radius in tiles
        int minX = (int)Math.Floor(_cameraTarget.X - viewDist);
        int maxX = (int)Math.Ceiling(_cameraTarget.X + viewDist);
        int minY = (int)Math.Floor(_cameraTarget.Y - viewDist);
        int maxY = (int)Math.Ceiling(_cameraTarget.Y + viewDist);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                TileType type = _tacticalMap.Get(x, y, 0);
                if (type == TileType.Empty) continue;

                // Ground grass should stay on the base level only and not bleed into upper-floor views.
                if (type == TileType.Grass && zLevel > 0) continue;

                Color color = GetTileColor(type, x, y, 0, zLevel);
                if (color.A == 0) continue;

                if (type == TileType.Wall)
                {
                    AddThinWallVertices(wallVertices, x, y, 0, color);
                }
                else
                {
                    AddTileVertices(tileVertices, x, y, 0, type, color);
                    if (type == TileType.DifficultTerrain)
                    {
                        Draw3DLine(new Vector3(x - 0.2f, y - 0.2f, 0.01f), new Vector3(x + 0.2f, y + 0.2f, 0.01f), Color.Black * 0.5f);
                        Draw3DLine(new Vector3(x - 0.2f, y + 0.2f, 0.01f), new Vector3(x + 0.2f, y - 0.2f, 0.01f), Color.Black * 0.5f);
                    }
                }
            }
        }

        // 2. Draw non-empty cells from dictionary (for verticality and overrides)
        foreach (var cell in _tacticalMap.EnumerateNonEmpty())
        {
            int cx = cell.Key.x, cy = cell.Key.y, cz = cell.Key.z;

            // Skip cells already drawn in the view box at z=0
            if (cz == 0 && cx >= minX && cx <= maxX && cy >= minY && cy <= maxY) continue;

            if (cz > zLevel || cell.Value == TileType.Empty) continue;

            // Ground grass should stay on the base level only and not bleed into upper-floor views.
            if (cell.Value == TileType.Grass && (cz != 0 || zLevel > 0)) continue;

            Color color = GetTileColor(cell.Value, cx, cy, cz, zLevel);
            if (color.A == 0) continue;

            if (cell.Value == TileType.Wall)
            {
                AddThinWallVertices(wallVertices, cx, cy, cz, color);
            }
            else
            {
                AddTileVertices(tileVertices, cx, cy, cz, cell.Value, color);
                if (cell.Value == TileType.DifficultTerrain)
                {
                    Draw3DLine(new Vector3(cx - 0.2f, cy - 0.2f, cz + 0.01f), new Vector3(cx + 0.2f, cy + 0.2f, cz + 0.01f), Color.Black * 0.5f);
                    Draw3DLine(new Vector3(cx - 0.2f, cy + 0.2f, cz + 0.01f), new Vector3(cx + 0.2f, cy - 0.2f, cz + 0.01f), Color.Black * 0.5f);
                }
            }
        }

        // 3. Render batches
        if (wallVertices.Count > 0)
        {
            _basicEffect.World = Matrix.Identity;
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, wallVertices.ToArray(), 0, wallVertices.Count / 3);
            }
        }

        if (tileVertices.Count > 0)
        {
            _basicEffect.World = Matrix.Identity;
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, tileVertices.ToArray(), 0, tileVertices.Count / 3);
            }
        }
    }

    private Color GetTileColor(TileType type, int x, int y, int z, int zLevel)
    {
        Color baseColor = type switch
        {
            TileType.Floor => new Color(70, 145, 70),
            TileType.Grass => new Color(80, 180, 80),
            TileType.DifficultTerrain => new Color(139, 69, 19),
            TileType.Wall => new Color(100, 100, 110),
            TileType.Water => Color.CornflowerBlue,
            _ => Color.ForestGreen
        };

        // Deterministic tile-level variation to mimic texture diversity and reduce visible tiling.
        if (type == TileType.Grass || type == TileType.Floor)
        {
            float dryness = Hash01(x, y, z, 11);
            float moisture = Hash01(x, y, z, 23);
            float wear = Hash01(x, y, z, 37);

            // Blend toward worn and humid variants while staying readable for tactical overlays.
            baseColor = Color.Lerp(baseColor, new Color(118, 106, 74), MathHelper.Clamp((wear - 0.72f) * 1.7f, 0f, 0.28f));
            baseColor = Color.Lerp(baseColor, new Color(52, 128, 78), MathHelper.Clamp((moisture - 0.65f) * 1.4f, 0f, 0.22f));
            baseColor = Color.Lerp(baseColor, new Color(142, 160, 93), MathHelper.Clamp((dryness - 0.8f) * 1.3f, 0f, 0.18f));

            // Parity keeps grid rhythm readable without making a checkerboard too obvious.
            float parity = (((x + y) & 1) == 0) ? 1.02f : 0.98f;
            baseColor = ScaleColor(baseColor, parity);
        }
        else if (type == TileType.DifficultTerrain)
        {
            float mudNoise = Hash01(x, y, z, 53);
            baseColor = ScaleColor(baseColor, 0.92f + mudNoise * 0.12f);
        }

        if (z < zLevel) baseColor *= 0.3f;
        if (_showVisionOverlay && _playerCreature != null)
        {
            bool isVisible = _visionSystem.IsVisible(x, y, z);
            Color tint = _visionSystem.GetFogOfWarTint(x, y, z, isVisible, _playerCreature);
            if (tint == Color.Black) return Color.Transparent;
            baseColor = new Color((byte)(baseColor.R * tint.R / 255), (byte)(baseColor.G * tint.G / 255), (byte)(baseColor.B * tint.B / 255), (byte)(baseColor.A * tint.A / 255));
        }
        return baseColor;
    }

    private void AddTileVertices(List<VertexPositionNormalColor> vertices, int x, int y, int z, TileType type, Color baseColor)
    {
        const float half = 0.5f;
        Vector3 n = Vector3.Up;

        Color bottomLeft = baseColor;
        Color bottomRight = baseColor;
        Color topRight = baseColor;
        Color topLeft = baseColor;

        if (type == TileType.Grass || type == TileType.Floor || type == TileType.DifficultTerrain)
        {
            // Per-corner variation creates a subtle faux texture and breaks up repeated flat color blocks.
            bottomLeft = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 101) * 0.16f);
            bottomRight = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 102) * 0.16f);
            topRight = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 103) * 0.16f);
            topLeft = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 104) * 0.16f);
        }

        // Triangle 1
        vertices.Add(new VertexPositionNormalColor(new Vector3(x - half, y - half, z), n, bottomLeft));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x + half, y - half, z), n, bottomRight));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x + half, y + half, z), n, topRight));

        // Triangle 2
        vertices.Add(new VertexPositionNormalColor(new Vector3(x - half, y - half, z), n, bottomLeft));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x + half, y + half, z), n, topRight));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x - half, y + half, z), n, topLeft));
    }

    private static Color ScaleColor(Color color, float factor)
    {
        factor = MathHelper.Clamp(factor, 0f, 2f);
        return new Color(
            (byte)MathHelper.Clamp(color.R * factor, 0f, 255f),
            (byte)MathHelper.Clamp(color.G * factor, 0f, 255f),
            (byte)MathHelper.Clamp(color.B * factor, 0f, 255f),
            color.A);
    }

    private static float Hash01(int x, int y, int z, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + z * 982451653 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            uint u = (uint)h;
            return (u & 0x00FFFFFF) / 16777215f;
        }
    }

    private void AddThinWallVertices(List<VertexPositionNormalColor> vertices, int x, int y, int z, Color color)
    {
        // Check neighbors to see which edges need a vertical plane
        // A plane is drawn if the neighbor is NOT a wall
        bool north = _tacticalMap.Get(x, y + 1, z) != TileType.Wall;
        bool south = _tacticalMap.Get(x, y - 1, z) != TileType.Wall;
        bool east = _tacticalMap.Get(x + 1, y, z) != TileType.Wall;
        bool west = _tacticalMap.Get(x - 1, y, z) != TileType.Wall;

        const float halfTile = 0.5f;
        const float wallHeight = 1.0f;

        if (north) AddVerticalPlaneVertices(vertices, new Vector3(x - halfTile, y + halfTile, z), new Vector3(x + halfTile, y + halfTile, z), wallHeight, color, Vector3.Backward);
        if (south) AddVerticalPlaneVertices(vertices, new Vector3(x + halfTile, y - halfTile, z), new Vector3(x - halfTile, y - halfTile, z), wallHeight, color, Vector3.Forward);
        if (east) AddVerticalPlaneVertices(vertices, new Vector3(x + halfTile, y + halfTile, z), new Vector3(x + halfTile, y - halfTile, z), wallHeight, color, Vector3.Right);
        if (west) AddVerticalPlaneVertices(vertices, new Vector3(x - halfTile, y - halfTile, z), new Vector3(x - halfTile, y + halfTile, z), wallHeight, color, Vector3.Left);

        // Also draw a small "cap" on top of the wall edges to make them look solid from above
        // This helps the "separation" look
        AddHorizontalWallCap(vertices, x, y, z, color);
    }

    private void AddVerticalPlaneVertices(List<VertexPositionNormalColor> vertices, Vector3 start, Vector3 end, float height, Color color, Vector3 normal)
    {
        var v1 = new VertexPositionNormalColor(start, normal, color);
        var v2 = new VertexPositionNormalColor(end, normal, color);
        var v3 = new VertexPositionNormalColor(start + Vector3.UnitZ * height, normal, color);
        var v4 = new VertexPositionNormalColor(end + Vector3.UnitZ * height, normal, color);

        // Triangle 1
        vertices.Add(v1); vertices.Add(v3); vertices.Add(v2);
        // Triangle 2
        vertices.Add(v2); vertices.Add(v3); vertices.Add(v4);

        // Draw the other side of the plane too so it's visible from both directions
        var v1r = new VertexPositionNormalColor(start, -normal, color);
        var v2r = new VertexPositionNormalColor(end, -normal, color);
        var v3r = new VertexPositionNormalColor(start + Vector3.UnitZ * height, -normal, color);
        var v4r = new VertexPositionNormalColor(end + Vector3.UnitZ * height, -normal, color);

        vertices.Add(v1r); vertices.Add(v2r); vertices.Add(v3r);
        vertices.Add(v2r); vertices.Add(v4r); vertices.Add(v3r);
    }

    private void AddHorizontalWallCap(List<VertexPositionNormalColor> vertices, int x, int y, int z, Color color)
    {
        const float halfTile = 0.5f;
        const float wallHeight = 1.0f;
        const float thickness = 0.05f; // Thin cap

        float topZ = z + wallHeight;

        // We draw a thin border on top
        Vector3 tl = new Vector3(x - halfTile, y - halfTile, topZ);
        Vector3 tr = new Vector3(x + halfTile, y - halfTile, topZ);
        Vector3 br = new Vector3(x + halfTile, y + halfTile, topZ);
        Vector3 bl = new Vector3(x - halfTile, y + halfTile, topZ);

        // Check neighbors to avoid drawing caps where walls are continuous
        bool north = _tacticalMap.Get(x, y + 1, z) != TileType.Wall;
        bool south = _tacticalMap.Get(x, y - 1, z) != TileType.Wall;
        bool east = _tacticalMap.Get(x + 1, y, z) != TileType.Wall;
        bool west = _tacticalMap.Get(x - 1, y, z) != TileType.Wall;

        if (north) AddCapSegment(vertices, bl, br, thickness, color);
        if (south) AddCapSegment(vertices, tr, tl, thickness, color);
        if (east) AddCapSegment(vertices, br, tr, thickness, color);
        if (west) AddCapSegment(vertices, tl, bl, thickness, color);
    }

    private void AddCapSegment(List<VertexPositionNormalColor> vertices, Vector3 p1, Vector3 p2, float thickness, Color color)
    {
        Vector3 dir = Vector3.Normalize(p2 - p1);
        Vector3 side = Vector3.Cross(dir, Vector3.UnitZ) * thickness;

        Vector3 v1 = p1;
        Vector3 v2 = p2;
        Vector3 v3 = p1 - side;
        Vector3 v4 = p2 - side;

        vertices.Add(new VertexPositionNormalColor(v1, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v3, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));

        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v3, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v4, Vector3.Up, color));
    }

    private void Draw3DGridOutlines(int zLevel)
    {
        Color gridOutlineColor = new Color(40, 50, 60);
        List<VertexPositionColor> vertices = new();

        foreach (var cell in _tacticalMap.EnumerateNonEmpty())
        {
            int cx = cell.Key.x, cy = cell.Key.y, cz = cell.Key.z;
            if (cz > zLevel || cell.Value == TileType.Empty) continue;

            // Only draw grid for the current level and one level below
            if (cz < zLevel - 1) continue;

            Color color = gridOutlineColor;
            if (cz < zLevel) color *= 0.5f;

            if (_showVisionOverlay && _playerCreature != null)
            {
                bool isVisible = _visionSystem.IsVisible(cx, cy, cz);
                Color tint = _visionSystem.GetFogOfWarTint(cx, cy, cz, isVisible, _playerCreature);
                if (tint == Color.Black) continue;
                color = new Color((byte)(color.R * tint.R / 255), (byte)(color.G * tint.G / 255), (byte)(color.B * tint.B / 255), (byte)(color.A * tint.A / 255));
            }

            const float halfTile = 0.5f;
            const float elevation = 0.01f; // Closer to tile surface to avoid looking like it floats
            float zPos = cz + elevation;

            Vector3 tl = new Vector3(cx - halfTile, cy - halfTile, zPos);
            Vector3 tr = new Vector3(cx + halfTile, cy - halfTile, zPos);
            Vector3 br = new Vector3(cx + halfTile, cy + halfTile, zPos);
            Vector3 bl = new Vector3(cx - halfTile, cy + halfTile, zPos);

            // Add 4 lines for the square
            vertices.Add(new VertexPositionColor(tl, color)); vertices.Add(new VertexPositionColor(tr, color));
            vertices.Add(new VertexPositionColor(tr, color)); vertices.Add(new VertexPositionColor(br, color));
            vertices.Add(new VertexPositionColor(br, color)); vertices.Add(new VertexPositionColor(bl, color));
            vertices.Add(new VertexPositionColor(bl, color)); vertices.Add(new VertexPositionColor(tl, color));
        }

        if (vertices.Count > 0)
        {
            _basicEffect.World = Matrix.Identity;
            _basicEffect.LightingEnabled = false; // Grids don't need lighting
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices.ToArray(), 0, vertices.Count / 2);
            }
            _basicEffect.LightingEnabled = true;
        }
    }

    private void Draw3DLine(Vector3 start, Vector3 end, Color color)
    {
        _basicEffect.World = Matrix.Identity;
        var vertices = new[] { new VertexPositionColor(start, color), new VertexPositionColor(end, color) };
        _basicEffect.LightingEnabled = false;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1); }
        _basicEffect.LightingEnabled = true;
    }

    private void Draw3DTile(int x, int y, int z, Color color)
    {
        _basicEffect.World = Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;
        GraphicsDevice.SetVertexBuffer(_tileVertexBuffer);
        GraphicsDevice.Indices = _tileIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2); }
    }

    private void Draw3DCreatures()
    {
        if (_combatManager.InCombat) foreach (var creature in _combatManager.Combatants) { if (creature.IsAlive()) Draw3DCreature(creature); }
        else if (_playerCreature != null) { Draw3DCreature(_playerCreature); foreach (var creature in _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive())) Draw3DCreature(creature); }
    }

    private void DrawCreatureTileOutlines()
    {
        IEnumerable<Creature> creatures = _combatManager.InCombat
            ? _combatManager.Combatants.Where(c => c.IsAlive())
            : (_playerCreature == null
                ? Enumerable.Empty<Creature>()
                : new[] { _playerCreature }.Concat(_combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive())));

        foreach (var creature in creatures)
        {
            if (creature.Z > _currentViewLevel) continue;

            bool isVisible = _visionSystem.IsVisible(creature.X, creature.Y, creature.Z);
            if (_combatManager.InCombat && _showVisionOverlay && _playerCreature != null)
            {
                Color fogTint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, isVisible, _playerCreature);
                if (fogTint == Color.Black) continue;
            }

            Color outlineColor = GetCreatureFactionOutlineColor(creature);
            if (_showVisionOverlay && _playerCreature != null)
            {
                Color tint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, true, _playerCreature);
                outlineColor = new Color(
                    (byte)(outlineColor.R * tint.R / 255),
                    (byte)(outlineColor.G * tint.G / 255),
                    (byte)(outlineColor.B * tint.B / 255));
            }

            var (w, h) = SizeHelper.GetSpaceInSquares(creature.Size);
            Draw3DTileOutline(creature.X, creature.Y, creature.Z, outlineColor, w, h);
        }
    }

    private void DrawPlayerMovementPerimeter()
    {
        if (!_combatManager.InCombat)
            return;

        var currentCombatant = _combatManager.CurrentCombatant;
        if (currentCombatant == null || !currentCombatant.IsPlayer)
            return;

        var reachableTiles = _combatManager.GetReachablePositions(currentCombatant)
            .Where(t => t.z == _currentViewLevel)
            .Select(t => (t.x, t.y))
            .ToHashSet();

        if (reachableTiles.Count == 0)
            return;

        Color perimeterColor = new Color(0, 240, 255);
        const float halfTile = 0.5f;
        const float zOffset = 0.09f;

        foreach (var tile in reachableTiles)
        {
            float z = _currentViewLevel + zOffset;
            int x = tile.x;
            int y = tile.y;

            if (!reachableTiles.Contains((x, y + 1)))
            {
                Draw3DLine(new Vector3(x - halfTile, y + halfTile, z), new Vector3(x + halfTile, y + halfTile, z), perimeterColor);
            }

            if (!reachableTiles.Contains((x, y - 1)))
            {
                Draw3DLine(new Vector3(x + halfTile, y - halfTile, z), new Vector3(x - halfTile, y - halfTile, z), perimeterColor);
            }

            if (!reachableTiles.Contains((x + 1, y)))
            {
                Draw3DLine(new Vector3(x + halfTile, y + halfTile, z), new Vector3(x + halfTile, y - halfTile, z), perimeterColor);
            }

            if (!reachableTiles.Contains((x - 1, y)))
            {
                Draw3DLine(new Vector3(x - halfTile, y - halfTile, z), new Vector3(x - halfTile, y + halfTile, z), perimeterColor);
            }
        }
    }

    private void DrawHoveredMovementPath((int x, int y)? hoveredTile)
    {
        if (!hoveredTile.HasValue)
            return;

        if (_playerCreature == null || !_playerCreature.IsAlive())
            return;

        int targetX = hoveredTile.Value.x;
        int targetY = hoveredTile.Value.y;
        int targetZ = _currentViewLevel;

        // Path trace for flight is tricky, only show if on same level as target for now
        // unless the creature is currently flying.
        if (!_playerCreature.IsFlying && _playerCreature.Z != _currentViewLevel)
            return;

        const float zOffset = 0.12f;
        var offset = SizeHelper.GetCenterOffset(_playerCreature.Size);
        Vector3 previousPoint = new Vector3(_playerCreature.VisualX + offset.X, _playerCreature.VisualY + offset.Y, _playerCreature.VisualZ + zOffset);

        Color activePathColor = new Color(0, 240, 255); // Blue for current movement

        // 1. Draw through remaining waypoints (current movement in progress)
        var waypoints = _playerCreature.GetRemainingWaypoints();
        foreach (var wp in waypoints)
        {
            Vector3 nextPoint = new Vector3(wp.X + offset.X, wp.Y + offset.Y, wp.Z + zOffset);
            Draw3DLine(previousPoint, nextPoint, activePathColor);
            previousPoint = nextPoint;
        }

        // 2. Draw potential new path from logical position to hover target
        if (_combatManager.GetCreatureAt(targetX, targetY, targetZ) == null)
        {
            var path = _combatManager.GetPath(_playerCreature, targetX, targetY, targetZ);
            if (path != null && path.Count >= 2)
            {
                Color hoverPathColor = (_combatManager.InCombat && !_combatManager.CanMove(_playerCreature, targetX, targetY, targetZ))
                    ? new Color(255, 165, 0) // Orange if out of movement
                    : activePathColor;

                for (int i = 1; i < path.Count; i++)
                {
                    var to = path[i];
                    Vector3 nextPoint = new Vector3(to.x + offset.X, to.y + offset.Y, to.z + zOffset);
                    Draw3DLine(previousPoint, nextPoint, hoverPathColor);
                    previousPoint = nextPoint;
                }
            }
        }
    }

    private void DrawEnemySightLinesToPlayer()
    {
        if (_playerCreature == null || !_playerCreature.IsAlive())
            return;

        foreach (var enemy in _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive()))
        {
            if (!_visionSystem.CanSee(enemy, _playerCreature))
                continue;

            Vector3 enemyPos = new Vector3(enemy.X, enemy.Y, enemy.Z + 0.65f);
            Vector3 playerPos = new Vector3(_playerCreature.X, _playerCreature.Y, _playerCreature.Z + 0.65f);
            Draw3DLine(enemyPos, playerPos, Color.Red * 0.9f);
        }
    }

    private static Color GetCreatureFactionOutlineColor(Creature creature)
    {
        if (creature.IsPlayer)
            return Color.LimeGreen;

        bool isEnemy = creature.Alignment is Alignment.LawfulEvil or Alignment.NeutralEvil or Alignment.ChaoticEvil;
        return isEnemy ? Color.Red : Color.DeepSkyBlue;
    }

    private void Draw3DTileOutline(int x, int y, int z, Color color, int width = 1, int height = 1)
    {
        const float halfTile = 0.5f;
        const float elevation = 0.07f;
        float zPos = z + elevation;

        Vector3 topLeft = new Vector3(x - halfTile, y - halfTile, zPos);
        Vector3 topRight = new Vector3(x + width - halfTile, y - halfTile, zPos);
        Vector3 bottomRight = new Vector3(x + width - halfTile, y + height - halfTile, zPos);
        Vector3 bottomLeft = new Vector3(x - halfTile, y + height - halfTile, zPos);

        Draw3DLine(topLeft, topRight, color);
        Draw3DLine(topRight, bottomRight, color);
        Draw3DLine(bottomRight, bottomLeft, color);
        Draw3DLine(bottomLeft, topLeft, color);
    }

    private void Draw3DCreature(Creature creature)
    {
        if (creature.Z > _currentViewLevel) return;
        bool isVisible = _visionSystem.IsVisible(creature.X, creature.Y, creature.Z);
        if (_combatManager.InCombat && _showVisionOverlay && _playerCreature != null)
        {
            Color fogTint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, isVisible, _playerCreature);
            if (fogTint == Color.Black) return;
        }
        Color color = creature.DisplayColor;
        if (_showVisionOverlay && _playerCreature != null) { Color tint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, isVisible, _playerCreature); color = new Color((byte)(color.R * tint.R / 255), (byte)(color.G * tint.G / 255), (byte)(color.B * tint.B / 255)); }
        var (capsuleRadius, capsuleHeight) = GetCreatureCapsuleDimensions(creature.Size);
        var offset = SizeHelper.GetCenterOffset(creature.Size);

        // Use visual position for smooth movement
        Draw3DCapsule(creature.VisualX + offset.X, creature.VisualY + offset.Y, creature.VisualZ, capsuleRadius, capsuleHeight, color);

        if (creature.VisualZ > 0)
        {
            // Draw shadow centered under creature
            var (w, h) = SizeHelper.GetSpaceInSquares(creature.Size);
            for (int dx = 0; dx < w; dx++)
                for (int dy = 0; dy < h; dy++)
                    Draw3DTile(creature.X + dx, creature.Y + dy, 0, Color.Black * 0.3f);

            Draw3DLine(new Vector3(creature.VisualX + offset.X, creature.VisualY + offset.Y, creature.VisualZ), new Vector3(creature.VisualX + offset.X, creature.VisualY + offset.Y, 0), Color.Gray * 0.3f);
        }
    }

    private static (float Radius, float Height) GetCreatureCapsuleDimensions(CreatureSize size)
    {
        float scale = size switch
        {
            CreatureSize.Tiny => 0.4f,
            CreatureSize.Small => 0.7f,
            CreatureSize.Large => 1.5f,
            CreatureSize.Huge => 2.0f,
            CreatureSize.Gargantuan => 2.5f,
            _ => 0.9f
        };

        return (scale * 0.45f, scale * 0.9f);
    }

    private static float GetCreatureVisualTopZ(Creature creature)
    {
        var (capsuleRadius, capsuleHeight) = GetCreatureCapsuleDimensions(creature.Size);
        return creature.Z + capsuleHeight + (capsuleRadius * 2f);
    }

    private void Draw3DCapsule(float x, float y, float z, float radius, float height, Color color)
    {
        // Capsule mesh is authored with radius 0.5 and total unit height 2.0 (body=1.0 + 2*hemispheres)
        float capsuleTotalHeight = height + (radius * 2f);
        Vector3 scale = new Vector3(radius / 0.5f, radius / 0.5f, capsuleTotalHeight / 2.0f);
        _basicEffect.World = Matrix.CreateScale(scale) * Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;
        _basicEffect.LightingEnabled = true;
        GraphicsDevice.SetVertexBuffer(_capsuleVertexBuffer);
        GraphicsDevice.Indices = _capsuleIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _capsulePrimitiveCount); }
    }

    private void Draw3DCube(float x, float y, float z, float scale, Color color)
    {
        // Place la base du cube sur z
        _basicEffect.World = Matrix.CreateScale(scale) * Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = 1.0f;
        _basicEffect.LightingEnabled = true;
        GraphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
        GraphicsDevice.Indices = _cubeIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12); }
    }

    private void Draw3DCreatureUI(Creature creature)
    {
        if (creature.Z > _currentViewLevel) return;
        if (_font == null) return;

        bool isVisible = _visionSystem.IsVisible(creature.X, creature.Y, creature.Z);
        if (_combatManager.InCombat && _showVisionOverlay && _playerCreature != null)
        {
            Color fogTint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, isVisible, _playerCreature);
            if (fogTint == Color.Black) return;
        }
        var (capsuleRadius, _) = GetCreatureCapsuleDimensions(creature.Size);
        float uiAnchorZ = GetCreatureVisualTopZ(creature) + MathF.Max(0.15f, capsuleRadius * 0.4f);
        var offset = SizeHelper.GetCenterOffset(creature.Size);

        // Use visual position for UI anchoring
        Vector3 screenPos = GraphicsDevice.Viewport.Project(new Vector3(creature.VisualX + offset.X, creature.VisualY + offset.Y, uiAnchorZ), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
        if (screenPos.Z < 0 || screenPos.Z > 1) return;
        Vector2 pos = new Vector2(screenPos.X, screenPos.Y);

        // Avoid overlap between creature labels and the tactical top HUD.
        if (_showCombatUI && _combatManager.InCombat && pos.Y <= CombatTopPanelHeight + 12)
            return;

        _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 30, (int)pos.Y - 20, 60, 6), Color.DarkRed);
        _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 30, (int)pos.Y - 20, (int)(60 * (float)creature.CurrentHP / creature.MaxHP), 6), Color.Green);
        string name = $"{creature.Name} [Z{creature.Z}]" + (creature.IsFlying ? " [Vol]" : "");
        Vector2 size = _font.MeasureString(name) * 0.6f;
        _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - (int)size.X / 2 - 4, (int)pos.Y - 40, (int)size.X + 8, (int)size.Y + 4), Color.Black * 0.6f);
        _spriteBatch.DrawString(_font, name, new Vector2(pos.X - size.X / 2, pos.Y - 38), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
    }

    private (int x, int y)? GetHoveredTile()
    {
        var mouse = Mouse.GetState();
        Vector3 near = GraphicsDevice.Viewport.Unproject(new Vector3(mouse.X, mouse.Y, 0f), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
        Vector3 far = GraphicsDevice.Viewport.Unproject(new Vector3(mouse.X, mouse.Y, 1f), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
        Vector3 dir = Vector3.Normalize(far - near);
        Ray ray = new Ray(near, dir);
        Plane plane = new Plane(Vector3.UnitZ, -_currentViewLevel);
        float? d = ray.Intersects(plane);
        return d.HasValue ? ((int)Math.Round(near.X + dir.X * d.Value), (int)Math.Round(near.Y + dir.Y * d.Value)) : null;
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

        _diceRollAnimation.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

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
                        // Initialize player creature from the selected character
                        if (_currentCharacter != null)
                        {
                            _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                            _combatManager.Combatants.Clear();
                        }
                        
                        // TODO: Go to multiplayer lobby
                        _state = AppState.Playing;
                        UpdateVision();
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
                                    // Initialize player creature from the selected character
                                    if (_currentCharacter != null)
                                    {
                                        _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                                        _combatManager.Combatants.Clear();
                                    }
                                    
                                    // TODO: Go to multiplayer lobby
                                    _state = AppState.Playing;
                                    UpdateVision();
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
                    // Initialize player creature from the selected character
                    _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                    _combatManager.Combatants.Clear();
                    // TODO: Go to multiplayer lobby
                    _state = AppState.Playing;
                    UpdateVision();
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
                    
                    // Initialize player creature from the selected character
                    if (_currentCharacter != null)
                    {
                        _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                        
                        // Clear existing enemies first to avoid duplicates
                        _combatManager.Combatants.Clear();
                    }
                    
                    _state = AppState.Playing;
                    UpdateVision();
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
                                
                                // Initialize player creature from the selected character
                                if (_currentCharacter != null)
                                {
                                    _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                                    _combatManager.Combatants.Clear();
                                }
                                 
                                _state = AppState.Playing;
                                UpdateVision();
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
                
                // Initialize player creature from the selected character
                if (_currentCharacter != null)
                {
                    _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
                    _combatManager.Combatants.Clear();
                }
                
                _state = AppState.Playing;
                UpdateVision();
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

            var closeMapButtonRect = GetMapButtonRect(GraphicsDevice.Viewport);
            if (mouse.LeftButton == ButtonState.Pressed &&
                _prevMouse.LeftButton == ButtonState.Released &&
                closeMapButtonRect.Contains(mouse.Position))
            {
                _showCampaignMap = false;
            }
            
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
            bool wasCharacterSheetOpen = _showCharacterSheet;
            bool wasJournalOpen = _showJournal;

            // Update movement animation for all creatures
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_playerCreature != null)
            {
                _playerCreature.UpdateMovementAnimation(deltaTime);

                bool isPlayerMoving = _playerCreature.IsMoving();
                if (isPlayerMoving)
                {
                    int roundedVisualX = (int)MathF.Round(_playerCreature.VisualX);
                    int roundedVisualY = (int)MathF.Round(_playerCreature.VisualY);
                    int roundedVisualZ = (int)MathF.Round(_playerCreature.VisualZ);
                    var roundedVisualTile = (roundedVisualX, roundedVisualY, roundedVisualZ);

                    if (!_wasPlayerMovingForVision || _lastRoundedVisualTile != roundedVisualTile)
                    {
                        UpdateVision();
                        _lastRoundedVisualTile = roundedVisualTile;
                    }

                    _wasPlayerMovingForVision = true;
                }
                else if (_wasPlayerMovingForVision)
                {
                    // Ensure we refresh one last time when movement ends.
                    UpdateVision();
                    _wasPlayerMovingForVision = false;
                    _lastRoundedVisualTile = null;
                }
            }
            foreach (var creature in _combatManager.Combatants)
            {
                if (creature.IsAlive())
                {
                    creature.UpdateMovementAnimation(deltaTime);
                }
            }
            
            if (kb.IsKeyDown(Keys.C) && !_prevKb.IsKeyDown(Keys.C))
            {
                _showCharacterSheet = !_showCharacterSheet;
                if (_showCharacterSheet)
                {
                    _showJournal = false;
                    _characterSheet.ResetScroll();
                }
            }

            if (kb.IsKeyDown(Keys.J) && !_prevKb.IsKeyDown(Keys.J))
            {
                _showJournal = !_showJournal;
                if (_showJournal)
                {
                    _showCharacterSheet = false;
                    _journalUI.ResetScroll();
                }
            }

            if (_showCharacterSheet)
            {
                var closeSheetButtonRect = _characterSheet.GetCloseButtonRect(GraphicsDevice.Viewport);
                if (mouse.LeftButton == ButtonState.Pressed &&
                    _prevMouse.LeftButton == ButtonState.Released &&
                    closeSheetButtonRect.Contains(mouse.Position))
                {
                    _showCharacterSheet = false;
                }
            }

            if (_showJournal)
            {
                var closeJournalButtonRect = _journalUI.GetCloseButtonRect(GraphicsDevice.Viewport);
                if (mouse.LeftButton == ButtonState.Pressed &&
                    _prevMouse.LeftButton == ButtonState.Released &&
                    closeJournalButtonRect.Contains(mouse.Position))
                {
                    _showJournal = false;
                }
            }

            var inventoryButtonRect = GetInventoryButtonRect(GraphicsDevice.Viewport);
            bool mouseClickedThisFrame = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            bool clickedOnGameplayUiButton = false;

            if (!wasCharacterSheetOpen &&
                !_showCharacterSheet &&
                mouseClickedThisFrame &&
                inventoryButtonRect.Contains(mouse.Position))
            {
                _showCharacterSheet = true;
                _characterSheet.ResetScroll();
                clickedOnGameplayUiButton = true;
            }

            var mapButtonRect = GetMapButtonRect(GraphicsDevice.Viewport);
            if (!_showCharacterSheet &&
                mouseClickedThisFrame &&
                mapButtonRect.Contains(mouse.Position))
            {
                _showCampaignMap = true;
                clickedOnGameplayUiButton = true;
            }

            var spawnButtonRect = GetSpawnButtonRect(GraphicsDevice.Viewport);
            if (!_showCharacterSheet &&
                mouseClickedThisFrame &&
                spawnButtonRect.Contains(mouse.Position))
            {
                TrySpawnRandomCreatureNearPlayer();
                clickedOnGameplayUiButton = true;
            }

            var rotateLeftButtonRect = GetRotateLeftButtonRect(GraphicsDevice.Viewport);
            var rotateRightButtonRect = GetRotateRightButtonRect(GraphicsDevice.Viewport);
            var combatMoveButtonRect = GetCombatMoveButtonRect(GraphicsDevice.Viewport);
            var combatAttackButtonRect = GetCombatAttackButtonRect(GraphicsDevice.Viewport);
            var combatBonusActionButtonRect = GetCombatBonusActionButtonRect(GraphicsDevice.Viewport);
            var combatEndTurnButtonRect = GetCombatEndTurnButtonRect(GraphicsDevice.Viewport);
            var combatRageButtonRect = GetCombatRageButtonRect(GraphicsDevice.Viewport);

            if (!_showCharacterSheet &&
                mouseClickedThisFrame)
            {
                if (rotateLeftButtonRect.Contains(mouse.Position))
                {
                    _targetYaw -= MathHelper.ToRadians(45f);
                    clickedOnGameplayUiButton = true;
                }
                else if (rotateRightButtonRect.Contains(mouse.Position))
                {
                    _targetYaw += MathHelper.ToRadians(45f);
                    clickedOnGameplayUiButton = true;
                }
                else if (_combatManager.InCombat && _showCombatUI)
                {
                    var currentCombatant = _combatManager.CurrentCombatant;
                    if (currentCombatant != null && currentCombatant.IsPlayer)
                    {
                        if (combatMoveButtonRect.Contains(mouse.Position))
                        {
                            _selectedAction = CombatAction.Move;
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (combatAttackButtonRect.Contains(mouse.Position))
                        {
                            _selectedAction = CombatAction.Attack;
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (combatBonusActionButtonRect.Contains(mouse.Position))
                        {
                            _showBonusActionMenu = !_showBonusActionMenu;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (combatEndTurnButtonRect.Contains(mouse.Position))
                        {
                            EndCurrentPlayerTurn(currentCombatant);
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_showBonusActionMenu && combatRageButtonRect.Contains(mouse.Position))
                        {
                            if (currentCombatant.HasBonusAction && currentCombatant.RagesRemaining > 0 && !currentCombatant.IsRaging && _currentCharacter?.Class == "Barbarian")
                            {
                                _combatManager.StartRage(currentCombatant);
                                currentCombatant.HasBonusAction = false;
                                AddToCombatLog($"{currentCombatant.Name} enters RAGE!");
                            }
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                    }
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
                    _playerCreature.MoveTo(_playerCreature.X, _playerCreature.Y, _playerCreature.Z + 1);
                    AddToCombatLog($"Ascending to level {_playerCreature.Z}");
                    UpdateVision();
                }
            }
            if (kb.IsKeyDown(Keys.T) && !_prevKb.IsKeyDown(Keys.T) && _playerCreature != null)
            {
                if (_playerCreature.CanFly && _playerCreature.IsFlying)
                {
                    int newZ = Math.Max(0, _playerCreature.Z - 1);
                    _playerCreature.MoveTo(_playerCreature.X, _playerCreature.Y, newZ);
                    AddToCombatLog($"Descending to level {newZ}");
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
                _characterSheet.Update(mouse, _currentCharacter);
                _prevKb = kb;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            if (_showJournal)
            {
                _journalUI.Update(mouse);
                _prevKb = kb;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            if (TryStartCombatFromEnemyDetection())
            {
                _prevKb = kb;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            // Exploration movement (outside combat)
            if (!_combatManager.InCombat)
            {
                if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
                {
                    var hovered = GetHoveredTile();
                    if (hovered.HasValue && _playerCreature != null)
                    {
                        int tx = hovered.Value.x;
                        int ty = hovered.Value.y;
                        int tz = _currentViewLevel;

                        var tileType = _tacticalMap.Get(tx, ty, tz);
                        if (tileType != TileType.Wall && tileType != TileType.Empty)
                        {
                            if (_combatManager.GetCreatureAt(tx, ty, tz) == null)
                            {
                                // Determine effective start position for new movement (finish current tile first)
                                int startX = _playerCreature.X;
                                int startY = _playerCreature.Y;
                                int startZ = _playerCreature.Z;
                                if (_playerCreature.IsMoving())
                                {
                                    var waypoints = _playerCreature.GetRemainingWaypoints();
                                    if (waypoints.Count > 0)
                                    {
                                        startX = (int)waypoints[0].X;
                                        startY = (int)waypoints[0].Y;
                                        startZ = (int)waypoints[0].Z;
                                    }
                                }

                                // Temporarily update logical position to check path from next tile
                                int actualX = _playerCreature.X;
                                int actualY = _playerCreature.Y;
                                int actualZ = _playerCreature.Z;
                                _playerCreature.X = startX;
                                _playerCreature.Y = startY;
                                _playerCreature.Z = startZ;

                                var path = _combatManager.GetPath(_playerCreature, tx, ty, tz);

                                // Restore logical position
                                _playerCreature.X = actualX;
                                _playerCreature.Y = actualY;
                                _playerCreature.Z = actualZ;

                                if (path != null)
                                {
                                    _playerCreature.InterruptMovement();
                                    for (int i = 1; i < path.Count; i++)
                                    {
                                        var step = path[i];
                                        _playerCreature.MoveTo(step.x, step.y, step.z);
                                    }
                                }
                                UpdateVision();
                            }
                        }
                    }
                }
            }
            
            // Combat controls
            if (_combatManager.InCombat && _showCombatUI)
            {
                var currentCombatant = _combatManager.CurrentCombatant;
                
                if (currentCombatant != null && currentCombatant.IsPlayer)
                {
                    // Player's turn
                    if (kb.IsKeyDown(Keys.D1) && !_prevKb.IsKeyDown(Keys.D1))
                    {
                        _selectedAction = CombatAction.Move;
                        _showBonusActionMenu = false;
                    }
                    if (kb.IsKeyDown(Keys.D2) && !_prevKb.IsKeyDown(Keys.D2))
                    {
                        _selectedAction = CombatAction.Attack;
                        _showBonusActionMenu = false;
                    }
                    if (kb.IsKeyDown(Keys.D3) && !_prevKb.IsKeyDown(Keys.D3))
                    {
                        _showBonusActionMenu = !_showBonusActionMenu;
                    }
                    if (kb.IsKeyDown(Keys.D4) && !_prevKb.IsKeyDown(Keys.D4))
                    {
                        EndCurrentPlayerTurn(currentCombatant);
                    }
                    
                    // Handle attack action
                    if (_selectedAction == CombatAction.Attack)
                    {
                        // Click on grid to attack
                        if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
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
                                    _diceRollAnimation.Start(result.AttackRoll);
                                    _selectedAction = CombatAction.Move;
                                    
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
                        if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
                        {
                            var hovered = GetHoveredTile();
                            if (hovered.HasValue)
                            {
                                int tx = hovered.Value.x;
                                int ty = hovered.Value.y;
                                // Check if tile is empty and within movement range
                                if (_combatManager.GetCreatureAt(tx, ty, _currentViewLevel) == null)
                                {
                                    // Determine effective start position for new movement (finish current tile first)
                                    int startX = currentCombatant.X;
                                    int startY = currentCombatant.Y;
                                    int startZ = currentCombatant.Z;
                                    if (currentCombatant.IsMoving())
                                    {
                                        var waypoints = currentCombatant.GetRemainingWaypoints();
                                        if (waypoints.Count > 0)
                                        {
                                            startX = (int)waypoints[0].X;
                                            startY = (int)waypoints[0].Y;
                                            startZ = (int)waypoints[0].Z;
                                        }
                                    }

                                    // Temporarily update logical position to check if movement from next tile is valid
                                    int actualX = currentCombatant.X;
                                    int actualY = currentCombatant.Y;
                                    int actualZ = currentCombatant.Z;
                                    currentCombatant.X = startX;
                                    currentCombatant.Y = startY;
                                    currentCombatant.Z = startZ;

                                    bool canMove = _combatManager.CanMove(currentCombatant, tx, ty, _currentViewLevel);

                                    // Restore logical position
                                    currentCombatant.X = actualX;
                                    currentCombatant.Y = actualY;
                                    currentCombatant.Z = actualZ;

                                    if (canMove)
                                    {
                                        currentCombatant.InterruptMovement();
                                        int prevX = currentCombatant.X;
                                        int prevY = currentCombatant.Y;
                                        int prevZ = currentCombatant.Z;
                                        int prevMove = currentCombatant.MovementRemaining;

                                        _combatManager.Move(currentCombatant, tx, ty, _currentViewLevel);

                                        int distanceInFeet = prevMove - currentCombatant.MovementRemaining;
                                        AddToCombatLog($"{currentCombatant.Name} moved to ({currentCombatant.X}, {currentCombatant.Y}, {currentCombatant.Z}) [{distanceInFeet}ft, {currentCombatant.MovementRemaining}ft remaining]");
                                        _selectedAction = CombatAction.Move;
                                        
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
                    bool shouldEndTurn = true;

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
                                _diceRollAnimation.Start(result.AttackRoll);
                                shouldEndTurn = false;

                                // Nimble Escape: bonus action Disengage, then retreat
                                if (currentCombatant.HasNimbleEscape && currentCombatant.HasBonusAction && currentCombatant.MovementRemaining > 0)
                                {
                                    currentCombatant.HasBonusAction = false;
                                    AddToCombatLog($"{currentCombatant.Name} uses Nimble Escape (Disengage)");
                                    var retreatStep = _combatManager.GetNextStepAwayFrom(currentCombatant, playerCreature);
                                    if (retreatStep.HasValue && _combatManager.CanMove(currentCombatant, retreatStep.Value.x, retreatStep.Value.y, retreatStep.Value.z))
                                    {
                                        _combatManager.Move(currentCombatant, retreatStep.Value.x, retreatStep.Value.y, retreatStep.Value.z);
                                        AddToCombatLog($"{currentCombatant.Name} retreats");
                                        UpdateVision();
                                    }
                                    currentCombatant.MovementRemaining = 0;
                                }
                            }
                            else if (currentCombatant.MovementRemaining > 0)
                            {
                                var nextStep = _combatManager.GetNextStepTowards(currentCombatant, playerCreature);
                                if (nextStep.HasValue && _combatManager.CanMove(currentCombatant, nextStep.Value.x, nextStep.Value.y, nextStep.Value.z))
                                {
                                    _combatManager.Move(currentCombatant, nextStep.Value.x, nextStep.Value.y, nextStep.Value.z);
                                    AddToCombatLog($"{currentCombatant.Name} moved");
                                    UpdateVision();
                                    shouldEndTurn = false;
                                }
                            }
                        }
                    }

                    if (shouldEndTurn)
                    {
                        // No valid action/move left, end turn.
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
            }
        }

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (kb.IsKeyDown(Keys.Q) && !_prevKb.IsKeyDown(Keys.Q)) _targetYaw -= MathHelper.ToRadians(45f);
        if (kb.IsKeyDown(Keys.E) && !_prevKb.IsKeyDown(Keys.E)) _targetYaw += MathHelper.ToRadians(45f);
        _cameraYaw = MathHelper.Lerp(_cameraYaw, _targetYaw, RotationSpeed * dt);

        float moveSpeed = 10f * dt;
        Vector3 moveDir = Vector3.Zero;
        if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) moveDir.Y -= 1;
        if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)) moveDir.Y += 1;
        if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) moveDir.X += 1;
        if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) moveDir.X -= 1;
        if (moveDir != Vector3.Zero) { moveDir.Normalize(); _cameraTarget += Vector3.Transform(moveDir, Matrix.CreateRotationZ(_cameraYaw)) * moveSpeed; }

        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0) { _cameraDistance = MathHelper.Clamp(_cameraDistance - scrollDelta * 0.01f, 5f, 50f); _prevScrollValue = mouse.ScrollWheelValue; }
        
        // Update vision before potential early returns in combat AI turns
        if (_visionNeedsUpdate)
        {
            RecalculateVision();
        }
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
        const int buttonHeight = 34;
        const int startX = 10;
        // Keep action buttons below turn/initiative text in the combat top panel.
        const int y = 130;
        return new Rectangle(startX, y, buttonWidth, buttonHeight);
    }

    private Rectangle GetCombatAttackButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int spacing = 10;
        var moveRect = GetCombatMoveButtonRect(viewport);
        return new Rectangle(moveRect.Right + spacing, moveRect.Y, buttonWidth, moveRect.Height);
    }

    private Rectangle GetCombatBonusActionButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int spacing = 10;
        var attackRect = GetCombatAttackButtonRect(viewport);
        return new Rectangle(attackRect.Right + spacing, attackRect.Y, buttonWidth, attackRect.Height);
    }

    private Rectangle GetCombatEndTurnButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int spacing = 10;
        var bonusRect = GetCombatBonusActionButtonRect(viewport);
        return new Rectangle(bonusRect.Right + spacing, bonusRect.Y, buttonWidth, bonusRect.Height);
    }

    private Rectangle GetCombatRageButtonRect(Viewport viewport)
    {
        const int buttonWidth = 130;
        const int buttonHeight = 34;
        var bonusRect = GetCombatBonusActionButtonRect(viewport);
        return new Rectangle(bonusRect.X, bonusRect.Bottom + 5, buttonWidth, buttonHeight);
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

    private void EndCurrentPlayerTurn(Creature currentCombatant)
    {
        _showBonusActionMenu = false;
        int prevRound = _combatManager.CurrentRound;
        _combatManager.NextTurn();
        int newRound = _combatManager.CurrentRound;

        int xpEarned = _combatManager.CollectPendingXP();
        if (xpEarned > 0 && _currentCharacter != null)
        {
            bool leveledUp = _currentCharacter.GainXP(xpEarned);
            AddToCombatLog($"Gained {xpEarned} XP!");
            if (leveledUp)
                AddToCombatLog($"Level up! Now level {_currentCharacter.Level}!");
        }

        if (newRound > prevRound)
        {
            AddToCombatLog($"=== Round {newRound} ===");
        }

        AddToCombatLog($"{currentCombatant.Name} ended turn");
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
            string label = "Inventaire [C]";
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
            string label = isCloseButton ? "Fermer map [M]" : "Ouvrir map [M]";
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
    

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        UpdateCameraMatrices();

        int? hoveredX = null;
        int? hoveredY = null;

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
            DrawEnemySightLinesToPlayer();
            var hovered = GetHoveredTile();
            DrawHoveredMovementPath(hovered);
            if (hovered.HasValue)
            {
                hoveredX = hovered.Value.x;
                hoveredY = hovered.Value.y;
                Draw3DTile(hoveredX.Value, hoveredY.Value, _currentViewLevel, Color.Yellow * 0.5f);
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

            DrawInventoryButton(vp);
            DrawMapButton(vp, false);
            DrawSpawnButton(vp);
            DrawRotationButtons(vp);

            if (_combatManager.InCombat && _showVisionOverlay)
            {
                foreach (var source in _visionSystem._lightSources)
                    if (source.IsActive)
                    {
                        Vector3 sPos = GraphicsDevice.Viewport.Project(new Vector3(source.X, source.Y, source.Z), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
                        if (sPos.Z >= 0 && sPos.Z <= 1) _spriteBatch.Draw(_pixel, new Rectangle((int)sPos.X - 5, (int)sPos.Y - 5, 10, 10), source.LightColor * 0.8f);
                    }
                foreach (var effect in _visionSystem._areaEffects)
                {
                    Vector3 ePos = GraphicsDevice.Viewport.Project(new Vector3(effect.X, effect.Y, effect.Z), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
                    if (ePos.Z >= 0 && ePos.Z <= 1) {
                        Color col = effect.EffectType == LightType.Darkness ? Color.Purple * 0.5f : Color.White * 0.5f;
                        if (effect.BlocksVision) col = Color.Gray * 0.7f;
                        _spriteBatch.Draw(_pixel, new Rectangle((int)ePos.X - 10, (int)ePos.Y - 1, 20, 2), col);
                        _spriteBatch.Draw(_pixel, new Rectangle((int)ePos.X - 1, (int)ePos.Y - 10, 2, 20), col);
                    }
                }
            }
        }

        // CHARACTER SHEET
        if (_showCharacterSheet && _state == AppState.Playing && _currentCharacter != null)
        {
            _characterSheet.Draw(_spriteBatch, GraphicsDevice, _currentCharacter, _currentCampaign);
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

            // "Create New" option
            {
                int newIndex = _characters.Count;
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + newIndex * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var col = (newIndex == _characterIndex) ? Color.LightGray : Color.Gray;
                _spriteBatch.Draw(_pixel, itemRect, col);

                if (_font != null)
                {
                    var label = "Create New Character";
                    var m = _font.MeasureString(label);
                    var p = new Vector2(itemRect.X + 12, itemRect.Y + (itemRect.Height - m.Y) / 2);
                    var textCol = (newIndex == _characterIndex) ? Color.Black : Color.White;
                    _spriteBatch.DrawString(_font, label, p, textCol);
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
                _spriteBatch.DrawString(_font, "Adventure Summary", new Vector2(summaryRect.X + 20, sy), Color.Yellow);
                sy += 40;

                DrawSummarySection("HOOK", selCampaign.AdventureHook, summaryRect.X + 20, ref sy, summaryWidth - 40);
                sy += 20;
                DrawSummarySection("MIDDLE", selCampaign.AdventureMiddle, summaryRect.X + 20, ref sy, summaryWidth - 40);
                sy += 20;
                DrawSummarySection("ENDING", selCampaign.AdventureEnding, summaryRect.X + 20, ref sy, summaryWidth - 40);
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


        
        // Combat UI
        if (_showCombatUI && _combatManager.InCombat)
        {
            // Combat panel at top
            int panelHeight = CombatTopPanelHeight;
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
                    _spriteBatch.DrawString(_font, SafeString(actionIcon), new Vector2(80, y), actionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, "Bonus:", new Vector2(130, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, SafeString(bonusIcon), new Vector2(200, y), bonusColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
                    _spriteBatch.DrawString(_font, "Reaction:", new Vector2(250, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    _spriteBatch.DrawString(_font, SafeString(reactionIcon), new Vector2(340, y), reactionColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    
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
                        DrawCombatActionButton(
                            GetCombatMoveButtonRect(vp),
                            "Move",
                            new Color(45, 95, 145),
                            _selectedAction == CombatAction.Move);
                        DrawCombatActionButton(
                            GetCombatAttackButtonRect(vp),
                            "Attack",
                            new Color(130, 70, 50),
                            _selectedAction == CombatAction.Attack);
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
                            bool canRage = currentCombatant.HasBonusAction && currentCombatant.RagesRemaining > 0 && !currentCombatant.IsRaging && _currentCharacter?.Class == "Barbarian";
                            DrawCombatActionButton(
                                GetCombatRageButtonRect(vp),
                                $"Rage ({currentCombatant.RagesRemaining})",
                                canRage ? Color.DarkRed : Color.Gray,
                                false);
                        }

                        _spriteBatch.DrawString(_font, "Raccourcis: [1] Move  [2] Attack  [3] Bonus Act  [4] End Turn", new Vector2(10, y + 10), Color.LightGray, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
                        y += 45;

                        if (_selectedAction != CombatAction.None && _selectedAction != CombatAction.BonusAction)
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
                
                // Combat log (always below action controls)
                int combatLogY = Math.Max(y + 8, panelHeight - 100);
                _spriteBatch.DrawString(_font, "Combat Log:", new Vector2(10, combatLogY), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                combatLogY += 20;
                
                for (int i = Math.Max(0, _combatLog.Count - 4); i < _combatLog.Count; i++)
                {
                    _spriteBatch.DrawString(_font, _combatLog[i], new Vector2(10, combatLogY), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    combatLogY += 18;
                }

                _diceRollAnimation.Draw(_spriteBatch, _pixel, _font, new Rectangle(0, 0, vp.Width, vp.Height));
                
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
                    legendY += 25;

                    // Difficult terrain
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.Sienna);
                    _spriteBatch.DrawString(_font, "Difficult Terrain (2x Cost)", new Vector2(legendX + 25, legendY), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    legendY += 25;

                    // Wall
                    _spriteBatch.Draw(_pixel, new Rectangle(legendX, legendY, 20, 20), Color.DarkSlateGray);
                    _spriteBatch.DrawString(_font, "Wall (Blocks Move/Vision)", new Vector2(legendX + 25, legendY), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    legendY += 35;
                    
                    // Creature indicators (top of unit)
                    _spriteBatch.DrawString(_font, "Creature Indicators (top of unit):", new Vector2(legendX, legendY), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
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
            
            var tileType = _tacticalMap.Get(tx, ty, tz);
            var lightLevel = _visionSystem.GetLightLevel(tx, ty, tz);
            var isVisible = _visionSystem.IsVisible(tx, ty, tz);
            
            var creature = _combatManager.GetCreatureAt(tx, ty, tz);
            
            var tooltip = $"Tile ({tx}, {ty}, Z{tz}) [{tileType}] | Light: {lightLevel} | Visible: {isVisible}";
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

            if (_showCombatUI)
                tooltipPos.Y = MathF.Max(CombatTopPanelHeight + 8, tooltipPos.Y);
            
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
}

public struct VertexPositionNormalColor : IVertexType
{
    public Vector3 Position;
    public Vector3 Normal;
    public Color Color;
    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );
    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    public VertexPositionNormalColor(Vector3 position, Vector3 normal, Color color) { Position = position; Normal = normal; Color = color; }
}
