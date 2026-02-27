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

public partial class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

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

    // Reusable rendering buffers to avoid per-frame allocations
    private List<VertexPositionNormalColor> _reusableWallVertices = new();
    private List<VertexPositionNormalColor> _reusableTileVertices = new();
    private List<VertexPositionColor> _reusableLineVertices = new();
    private List<(int x, int y, int z, TileType type)> _reusableVegetation = new();

    private int _prevScrollValue = 0;
    private int _currentViewLevel = 0; // Current Z-level being viewed

    private enum AppState { MainMenu, CharacterSelect, CharacterCreate, CampaignSelect, CampaignCreate, Playing }
    private AppState _state = AppState.MainMenu;

    private enum MenuView { Main, Options, Language }
    private MenuView _currentMenuView = MenuView.Main;

    private bool _inMainMenu => _state == AppState.MainMenu;
    private string[] _cachedMainMenuItems = null!;
    private string[] _cachedOptionsMenuItems = null!;
    private string[] _cachedLanguageMenuItems = null!;
    private string[] _cachedPauseMenuItems = null!;
    private Loc.Language _lastLanguage;

    private void UpdateMenuCache()
    {
        _cachedMainMenuItems = new[] { Loc.Tr("Single Player"), Loc.Tr("Multiplayer"), Loc.Tr("Options"), Loc.Tr("Desktop") };
        _cachedOptionsMenuItems = new[] { Loc.Tr("Language"), Loc.Tr("Back") };
        _cachedLanguageMenuItems = new[] { Loc.Tr("English"), Loc.Tr("French") };
        _cachedPauseMenuItems = new[] { Loc.Tr("Continue"), Loc.Tr("Options"), Loc.Tr("Main Menu"), Loc.Tr("Desktop") };
        _lastLanguage = Loc.CurrentLanguage;
    }

    private int _mainMenuIndex = 0;

    private List<Character> _characters = new();
    private int _characterIndex = 0;
    private string _savesDir = "saves";
    private string _charsFile = "characters.json";
    private string _campaignsFile = "campaigns.json";
    private Character _currentCharacter = null!;
    private Campaign _currentCampaign = null!;
    private bool _isMultiplayerMode = false;
    private int _lastIntPartyX;
    private int _lastIntPartyY;
    private Vector3 _scrollOffset = Vector3.Zero;
    private float _lastTotalTacticalX;
    private float _lastTotalTacticalY;
    private Dictionary<(int x, int y, int z), DungeonDoorState> _urbanDoorStates = new();
    
    private List<Campaign> _campaigns = new();
    private int _campaignIndex = 0;

    private enum PendingDeleteType { None, Character, Campaign }
    private PendingDeleteType _pendingDeleteType = PendingDeleteType.None;
    private int _pendingDeleteIndex = -1;

    private CharacterCreation _characterCreation = null!;
    private CharacterSheet _characterSheet = null!;
    private JournalUI _journalUI = null!;
    private CampaignCreation _campaignCreation = null!;
    private CampaignMapViewer _campaignMapViewer = null!;

    private bool _isMenuOpen = false;
    private int _menuIndex = 0;
    
    private bool _showCampaignMap = false;

    private KeyboardState _prevKb;
    private SpriteFont _font = null!;
    private HashSet<char> _supportedChars = new();

    private bool _showCharacterSheet = false;
    private bool _showJournal = false;

    private CombatManager _combatManager = new();
    private Creature _playerCreature = null!;
    private List<string> _combatLog = new();
    private const int MAX_COMBAT_LOG = 5;
    
    // Vision and lighting system
    private VisionSystem _visionSystem = new();
    private bool _showVisionOverlay = true;
    private bool _visionNeedsUpdate = false;
    private bool _wasPlayerMovingForVision = false;
    private (int X, int Y, int Z)? _lastRoundedVisualTile = null;
    
    // Combat UI state
    private enum CombatAction { None, Move, Attack, Dash, CastSpell, BonusAction, EndTurn, Help, Grapple, ThrowAcid }
    private CombatAction _selectedAction = CombatAction.None;
    private bool _showBonusActionMenu = false;
    private bool _showCombatUI = true;
    private bool _hudAlwaysVisible = true;
    private int _combatTopPanelHeight = 125; // Reduced from 220
    private MouseState _prevMouse;

    // AI Turn management
    private float _aiDelayTimer = 0f;
    private float _aiAnimationStuckTimer = 0f;
    private bool _aiDidSomethingThisTurn = false;
    private enum AiPhase { Start, Move, Action, BonusAction, End }
    private AiPhase _currentAiPhase = AiPhase.Start;

    // Auto-save timer
    private double _autoSaveTimer = 0;
    private const double AutoSaveInterval = 5 * 60; // 5 minutes in seconds
    private double _lastTotalGameMinutes = 0;
    private double _torchTimerAccumulator = 0;

    private class GroundItem
    {
        public int X, Y, Z;
        public ItemInstance Item = null!;
        public LightSource? Light = null;
    }
    private List<GroundItem> _groundItems = new();

    // Draggable Combat Log
    private Rectangle _combatLogWindowRect = new Rectangle(10, 120, 350, 120);
    private bool _isDraggingCombatLog = false;
    private Point _dragOffset;
    private bool _showEnemyContextMenu = false;
    private Rectangle _enemyContextMenuRect;
    private Rectangle _enemyExamineOptionRect;
    private Creature? _contextTargetEnemy = null;
    private string _enemyExamineText = "";
    private Rectangle _enemyExaminePopupRect;
    private Spell? _activeSpell = null;
    private bool _showDoorContextMenu = false;
    private Rectangle _doorContextMenuRect;
    private (int x, int y, int z) _contextTargetDoor;
    private Dictionary<string, Rectangle> _doorMenuOptionRects = new();
    private DiceRoll3DAnimation _diceRollAnimation = new();
    private readonly Random _random = new();

    private class FloatingTooltip
    {
        public Creature Owner = null!;
        public string Text = "";
        public Color Color = Color.White;
        public float RemainingTime;
        public float MaxTime = 2.0f;
        public float StackIndex = 0;

        public float GetAlpha()
        {
            if (RemainingTime > 0.5f) return 1.0f;
            return MathHelper.Clamp(RemainingTime / 0.5f, 0f, 1f);
        }
    }
    private List<FloatingTooltip> _activeTooltips = new();
    private Dictionary<Creature, int> _creatureHPTracker = new();

    // Ammo tracking for post-battle recovery (PHB "Ammunition")
    private readonly Dictionary<string, int> _ammoFiredThisCombat = new();

    private void AddTooltip(Creature creature, string text, Color color)
    {
        // Find the highest stack index currently active for this creature to stack above it
        float maxIndex = -1;
        foreach (var t in _activeTooltips)
        {
            if (t.Owner == creature && t.StackIndex > maxIndex)
                maxIndex = t.StackIndex;
        }

        _activeTooltips.Add(new FloatingTooltip
        {
            Owner = creature,
            Text = SanitizeTextForFont(text),
            Color = color,
            RemainingTime = 2.0f,
            MaxTime = 2.0f,
            StackIndex = maxIndex + 1
        });
    }

    private LuteSynthesizer _luteSynth = null!;
    private LuteProceduralMusic _luteMusic = null!;
    private PaperMapSfxSynth _mapSfxSynth = null!;
    private TorchIgnitionSfxSynth _torchIgnitionSfx = null!;
    private FootstepSfxSynth _footstepSfx = null!;
    private readonly Dictionary<Creature, (int X, int Y, int Z)> _lastStepTiles = new();

    private bool HasPendingDeleteConfirmation => _pendingDeleteType != PendingDeleteType.None;

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
        UpdateMenuCache();
        // Keep a normal bordered window at startup
        Window.IsBorderless = false;
        _graphics.ApplyChanges();

        _tacticalMap = new InfiniteGrid3D<TileType>(GetProceduralTile) { AutoCache = true };
        
        _prevKb = Keyboard.GetState();
        _prevMouse = Mouse.GetState();
        base.Initialize();
    }

    private TileType GetProceduralTile(int x, int y, int z)
    {
        // 1. Check for dungeon tiles in the current campaign
        if (_currentCampaign != null)
        {
            var partyHex = Campaign.CartesianToAxial(_currentCampaign.PartyX, _currentCampaign.PartyY);

            // Search all generated dungeons in this campaign
            foreach (var dungeon in _currentCampaign.Dungeons)
            {
                // Only consider dungeons close to the party's current hex
                if (Campaign.GetHexDistance(partyHex.q, partyHex.r, dungeon.WorldX, dungeon.WorldY) <= 1)
                {
                    // Convert world coordinates to relative dungeon coordinates.
                    var (worldX_miles, worldY_miles) = Campaign.AxialToMiles(dungeon.WorldX, dungeon.WorldY);

                    // For smooth scrolling, we use the integer part of the party position as origin for the tactical grid
                    float originX_miles = (float)Math.Floor(_currentCampaign.PartyX * Campaign.TacticalUnitsPerMile) / Campaign.TacticalUnitsPerMile;
                    float originY_miles = (float)Math.Floor(_currentCampaign.PartyY * Campaign.TacticalUnitsPerMile) / Campaign.TacticalUnitsPerMile;

                    int dungeonOriginX = (int)Math.Round((worldX_miles - originX_miles) * Campaign.TacticalUnitsPerMile);
                    int dungeonOriginY = (int)Math.Round((worldY_miles - originY_miles) * Campaign.TacticalUnitsPerMile);

                    int rx = x - dungeonOriginX;
                    int ry = y - dungeonOriginY;

                    if (dungeon.TryGetTile(rx, ry, z, out var dungeonTile))
                    {
                        return dungeonTile;
                    }
                }
            }
        }

        // 2. Standard procedural world tiles
        int seed = _currentCampaign?.Seed ?? 0;
        // Use integer party position to keep tactical grid stable during smooth scroll
        float offsetX = _currentCampaign != null ? (float)Math.Floor(_currentCampaign.PartyX * Campaign.TacticalUnitsPerMile) / Campaign.TacticalUnitsPerMile : 0;
        float offsetY = _currentCampaign != null ? (float)Math.Floor(_currentCampaign.PartyY * Campaign.TacticalUnitsPerMile) / Campaign.TacticalUnitsPerMile : 0;
        int floor = _currentCampaign?.CurrentFloor ?? 0;
        return WorldGenerator.GetTileType(x, y, z, seed, offsetX, offsetY, floor, _currentCampaign);
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
        catch (Exception ex)
        {
            _font = null!;
            _supportedChars = new HashSet<char>();
            System.Console.WriteLine("Warning: DefaultFont not found or failed to load. Build the Content/Content.mgcb with the MonoGame Pipeline Tool to generate DefaultFont.xnb. Error: " + ex.Message);
        }

        _characterCreation = new CharacterCreation(_font, _pixel);
        _characterSheet = new CharacterSheet(_font, _pixel);
        _journalUI = new JournalUI(_font, _pixel);

        _luteSynth = new LuteSynthesizer();
        _luteMusic = new LuteProceduralMusic(_luteSynth);
        InitializeCampaignMapAudio();
        _torchIgnitionSfx = new TorchIgnitionSfxSynth();
        _footstepSfx = new FootstepSfxSynth();
        _campaignCreation = new CampaignCreation(_font, _pixel);
        _campaignMapViewer = new CampaignMapViewer(_font, _pixel);
        _campaignMapViewer.VisionSystem = _visionSystem;

        Initialize3DRendering();
        _combatManager.TacticalMap = _tacticalMap;
        _combatManager.DoorStateProvider = GetDoorState;
        _visionSystem.TacticalMap = _tacticalMap;
        _visionSystem.DoorStateProvider = GetDoorState;
        _visionSystem.AmbientLight = _currentCampaign?.GetCurrentLightLevel() ?? LightType.Bright;

        // Procedural ground is now handled by InfiniteGrid3D factory.
        
        // Add some walls
        for (int i = -5; i <= 5; i++)
        {
            if (i == 0) continue; // Doorway
            _tacticalMap.Set(i, 3, 0, TileType.Wall);
            _tacticalMap.Set(i, 3, 1, TileType.Wall);
        }

        // No debug/test upper-floor platforms.
        
        try
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _savesDir));
        }
        catch { }

        LoadCharacters();
    }
    
    private void StartCombatWithNearbyEnemies()
    {
        _ammoFiredThisCombat.Clear();
        _luteMusic.Stop();
        if (_currentCharacter == null || _playerCreature == null) return;
        
        var combatants = new List<Creature>();

        // Add all existing player characters to combat
        var existingPlayers = _combatManager.Combatants.Where(c => c.IsPlayer && c.IsAlive()).ToList();
        if (existingPlayers.Count == 0)
        {
            combatants.Add(_playerCreature);
        }
        else
        {
            combatants.AddRange(existingPlayers);
        }
        
        // Add existing enemies to combat
        var existingEnemies = _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive()).ToList();
        if (existingEnemies.Count == 0) return;

        combatants.AddRange(existingEnemies);
        
        // Clear and restart with proper initiative
        _combatManager.Combatants.Clear();
        _combatManager.StartCombat(combatants);

        if (_currentCampaign != null)
        {
            var players = combatants.Where(c => c.IsPlayer);
            var enemies = combatants.Where(c => !c.IsPlayer);
            _currentCampaign.CurrentEncounterDifficulty = CombatManager.GetEncounterDifficulty(players, enemies).Difficulty;
        }

        _showCombatUI = _hudAlwaysVisible;
        _selectedAction = CombatAction.Move;
        _currentAiPhase = AiPhase.Start;
        _aiDelayTimer = 1.0f;
        AddToCombatLog(Loc.Tr("Combat started!"));
        AddToCombatLog(Loc.Tr("Round {0} begins!", _combatManager.CurrentRound));
        
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
        AddToCombatLog(Loc.Tr("{0} spotted you! Combat started automatically.", spottingEnemy.Name));
        return true;
    }

    private static bool IsValidPlayerSpawnTile(TileType tile)
    {
        return tile != TileType.Water && tile != TileType.Wall && tile != TileType.DungeonWall && tile != TileType.Empty;
    }

    private void PlacePlayerAtNearestValidTile(int preferredX = 0, int preferredY = 0, int preferredZ = 0)
    {
        if (_playerCreature == null)
            return;

        const int maxSearchTiles = 3000;
        foreach (var (x, y, z) in _tacticalMap.SpiralCoords(preferredX, preferredY, preferredZ).Take(maxSearchTiles))
        {
            if (!IsValidPlayerSpawnTile(_tacticalMap.Get(x, y, z)))
                continue;

            _playerCreature.TeleportTo(x, y, z);
            return;
        }

        _playerCreature.TeleportTo(preferredX, preferredY, preferredZ);
        AddToCombatLog(Loc.Tr("No dry tile found for spawn; default position used."));
    }

    private void CreatePlayerCreatureAtSafeSpawn()
    {
        if (_currentCharacter == null)
            return;

        _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0);
        PlacePlayerAtNearestValidTile();
        _combatManager.EndCombat();
    }
    
    private void TriggerEncounterSpawn(int targetDistance = 20)
    {
        if (_playerCreature == null || _currentCampaign == null)
            return;

        // 1. Determine current biome
        float milesX = ((float)_playerCreature.X / Campaign.TacticalUnitsPerMile) + _currentCampaign.PartyX;
        float milesY = ((float)_playerCreature.Y / Campaign.TacticalUnitsPerMile) + _currentCampaign.PartyY;
        BiomeType biome = WorldGenerator.GetBiome(milesX, milesY, _currentCampaign.Seed, _currentCampaign.CurrentFloor, _currentCampaign);

        // 2. Determine party levels (all player creatures present)
        var playerCreatures = _combatManager.Combatants.Where(c => c.IsPlayer && c.IsAlive()).ToList();
        if (playerCreatures.Count == 0) playerCreatures.Add(_playerCreature);

        var partyLevels = playerCreatures.Select(c => c.Level);

        // 3. Generate encounter
        var enemiesToSpawn = CombatManager.GenerateEncounter(_currentCampaign.Difficulty, biome, partyLevels, _random);

        if (enemiesToSpawn.Count == 0)
        {
            AddToCombatLog(Loc.Tr("No enemies fit the budget for this area."));
            return;
        }

        // 4. Spawn them
        int spawnedCount = 0;
        foreach (var type in enemiesToSpawn)
        {
            if (TrySpawnSpecificCreatureNearPlayer(type, targetDistance))
                spawnedCount++;
        }

        if (spawnedCount > 0)
        {
            UpdateVision();
        }
        else
        {
            AddToCombatLog(Loc.Tr("Spawn failed: no valid tile found nearby."));
        }
    }

    private bool TrySpawnSpecificCreatureNearPlayer(CreatureType type, int targetDistance = 20)
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
            if (tile == TileType.Wall || tile == TileType.Water || tile == TileType.Empty)
                continue;

            Creature enemy = Creature.Create(type, spawnX, spawnY, spawnZ);

            if (enemy.CanFly && _random.NextDouble() < 0.35)
                enemy.IsFlying = true;

            _combatManager.Combatants.Add(enemy);
            AddToCombatLog(Loc.Tr("Spawn: {0} appears at ({1}, {2}, {3}).", enemy.Name, spawnX, spawnY, spawnZ));
            return true;
        }
        return false;
    }

    private void SetupCombatLighting()
    {
        _visionSystem.ClearLightSources();
    }
    
    private void UpdateVision()
    {
        _visionNeedsUpdate = true;
    }

    private void RefreshLightSources()
    {
        _visionSystem.ClearLightSources();

        // Player's equipped torches
        if (_currentCharacter != null && _playerCreature != null)
        {
            var inv = _currentCharacter.InventoryData;
            if (inv.EquippedWeapon != null && inv.EquippedWeapon.Name == "Torch" && inv.EquippedWeapon.IsLit)
            {
                var light = LightSource.Torch(_playerCreature.X, _playerCreature.Y, _playerCreature.Z);
                light.AttachedTo = _playerCreature;
                _visionSystem.AddLightSource(light);
            }
            if (inv.OffhandWeapon != null && inv.OffhandWeapon.Name == "Torch" && inv.OffhandWeapon.IsLit)
            {
                var light = LightSource.Torch(_playerCreature.X, _playerCreature.Y, _playerCreature.Z);
                light.AttachedTo = _playerCreature;
                _visionSystem.AddLightSource(light);
            }
        }

        // Ground items (lit torches)
        foreach (var gi in _groundItems)
        {
            if (gi.Item.Name == "Torch" && gi.Item.IsLit)
            {
                if (gi.Light == null)
                {
                    gi.Light = LightSource.Torch(gi.X, gi.Y, gi.Z);
                }
                else
                {
                    gi.Light.X = gi.X;
                    gi.Light.Y = gi.Y;
                    gi.Light.Z = gi.Z;
                }
                _visionSystem.AddLightSource(gi.Light);
            }
        }
    }

    private void Draw3DGroundItems()
    {
        foreach (var gi in _groundItems)
        {
            if (!_visionSystem.IsVisible(gi.X, gi.Y, gi.Z)) continue;

            Color itemColor = Color.Brown;
            if (gi.Item.Name == "Torch" && gi.Item.IsLit)
            {
                itemColor = Color.Orange;
            }

            // Draw a small cube for the item
            Draw3DCube(gi.X + 0.5f, gi.Y + 0.5f, gi.Z + 0.1f, 0.2f, itemColor);
        }
    }

    private void UpdateTorchDurations(int minutes)
    {
        if (minutes <= 0) return;

        bool changed = false;
        foreach (var character in _characters)
        {
            for (int i = character.InventoryData.Items.Count - 1; i >= 0; i--)
            {
                var item = character.InventoryData.Items[i];
                if (item.Name == "Torch" && item.IsLit)
                {
                    item.RemainingMinutes -= minutes;
                    if (item.RemainingMinutes <= 0)
                    {
                        character.InventoryData.RemoveItemInstance(item);
                        string msg = Loc.Tr("{0}'s torch has burned out!", character.Name);
                        AddToCombatLog(msg);
                        _currentCampaign?.SetTravelMessage(msg);
                        changed = true;
                    }
                }
            }
        }

        for (int i = _groundItems.Count - 1; i >= 0; i--)
        {
            var gi = _groundItems[i];
            if (gi.Item.Name == "Torch" && gi.Item.IsLit)
            {
                gi.Item.RemainingMinutes -= minutes;
                if (gi.Item.RemainingMinutes <= 0)
                {
                    _groundItems.RemoveAt(i);
                    string msg = Loc.Tr("A torch on the ground has burned out!");
                    AddToCombatLog(msg);
                    _currentCampaign?.SetTravelMessage(msg);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            UpdateVision();
        }
    }
    
    private void RecalculateVision()
    {
        if (_playerCreature != null)
        {
            if (_currentCampaign != null)
            {
                _visionSystem.WorldOffset = new Vector2(_currentCampaign.PartyX, _currentCampaign.PartyY);
            }

            int originalX = _playerCreature.X;
            int originalY = _playerCreature.Y;
            int originalZ = _playerCreature.Z;

            if (_playerCreature.IsMoving())
            {
                _playerCreature.X = (int)MathF.Round(_playerCreature.VisualX);
                _playerCreature.Y = (int)MathF.Round(_playerCreature.VisualY);
                _playerCreature.Z = (int)MathF.Round(_playerCreature.VisualZ);
            }

            RefreshLightSources();
            
            _visionSystem.CalculateLighting();
            _visionSystem.CalculateVisibility(_playerCreature);

            // Reveal surroundings in the global fog of war
            // Filter to only active party members for this campaign
            var activeParty = _characters.Where(c => _currentCampaign!.PartyMembers.Contains(c.Name)).ToList();
            if (activeParty.Count == 0 && _currentCharacter != null) activeParty.Add(_currentCharacter);

            float revealRadius = VisionSystem.GetRevealRadius(_currentCampaign!, activeParty);
            Vector2 partyPos = new Vector2(_currentCampaign!.PartyX, _currentCampaign!.PartyY);
            _visionSystem.RevealPath(partyPos, partyPos, revealRadius);
            _currentCampaign!.DiscoverLocations(revealRadius, msg => AddToCombatLog(msg));

            _playerCreature.X = originalX;
            _playerCreature.Y = originalY;
            _playerCreature.Z = originalZ;
        }
        _visionNeedsUpdate = false;
    }
    
    private void AddToCombatLog(string message)
    {
        _combatLog.Add(SanitizeTextForFont(message));
        if (_combatLog.Count > MAX_COMBAT_LOG)
            _combatLog.RemoveAt(0);
    }

    private string SanitizeTextForFont(string text)
    {
        if (string.IsNullOrEmpty(text) || _font == null)
            return text;

        var supportedChars = _font.Characters;
        var sanitized = new StringBuilder(text.Length);

        foreach (char c in text)
        {
            if (c == '\n' || c == '\r')
            {
                sanitized.Append(c);
                continue;
            }

            if (supportedChars.Contains(c))
            {
                sanitized.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                sanitized.Append(' ');
                continue;
            }

            sanitized.Append('?');
        }

        return sanitized.ToString();
    }

    private void RequestDeleteCharacter(int index)
    {
        if (!IsExistingCharacterIndex(index))
            return;

        _pendingDeleteType = PendingDeleteType.Character;
        _pendingDeleteIndex = index;
    }

    private void RequestDeleteCampaign(int index)
    {
        if (index < 0 || index >= _campaigns.Count)
            return;

        _pendingDeleteType = PendingDeleteType.Campaign;
        _pendingDeleteIndex = index;
    }

    private void ConfirmPendingDelete()
    {
        if (_pendingDeleteType == PendingDeleteType.Character)
        {
            if (IsExistingCharacterIndex(_pendingDeleteIndex))
            {
                _characters.RemoveAt(_pendingDeleteIndex);
                SaveCharacters();
                if (_characterIndex >= _characters.Count)
                    _characterIndex = Math.Max(0, _characters.Count - 1);
            }
        }
        else if (_pendingDeleteType == PendingDeleteType.Campaign)
        {
            if (_pendingDeleteIndex >= 0 && _pendingDeleteIndex < _campaigns.Count)
            {
                _campaigns.RemoveAt(_pendingDeleteIndex);
                SaveCampaigns();
                if (_campaignIndex >= _campaigns.Count)
                    _campaignIndex = Math.Max(0, _campaigns.Count - 1);
            }
        }

        CancelPendingDelete();
    }

    private void CancelPendingDelete()
    {
        _pendingDeleteType = PendingDeleteType.None;
        _pendingDeleteIndex = -1;
    }

    private string GetPendingDeleteEntityName()
    {
        return _pendingDeleteType switch
        {
            PendingDeleteType.Character when IsExistingCharacterIndex(_pendingDeleteIndex) => _characters[_pendingDeleteIndex].Name,
            PendingDeleteType.Campaign when _pendingDeleteIndex >= 0 && _pendingDeleteIndex < _campaigns.Count => _campaigns[_pendingDeleteIndex].Name,
            _ => "this item"
        };
    }
    private void GetDeleteConfirmationRects(Viewport vp, out Rectangle dialogRect, out Rectangle confirmRect, out Rectangle cancelRect)
    {
        int dialogWidth = 460;
        int dialogHeight = 230;
        const int buttonWidth = 170;
        const int buttonHeight = 42;
        const int horizontalPadding = 40;
        const int bottomPadding = 24;
        dialogRect = new Rectangle((vp.Width - dialogWidth) / 2, (vp.Height - dialogHeight) / 2, dialogWidth, dialogHeight);
        confirmRect = new Rectangle(dialogRect.X + horizontalPadding, dialogRect.Bottom - bottomPadding - buttonHeight, buttonWidth, buttonHeight);
        cancelRect = new Rectangle(dialogRect.Right - horizontalPadding - buttonWidth, dialogRect.Bottom - bottomPadding - buttonHeight, buttonWidth, buttonHeight);
    }

    private void HandlePendingDeleteMouseInput(MouseState mouse)
    {
        var vp = GraphicsDevice.Viewport;
        GetDeleteConfirmationRects(vp, out _, out var confirmRect, out var cancelRect);

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            if (confirmRect.Contains(mouse.Position))
                ConfirmPendingDelete();
            else if (cancelRect.Contains(mouse.Position))
                CancelPendingDelete();
        }
    }

    
    private void UpdateCameraMatrices()
    {
        float aspectRatio = GraphicsDevice.Viewport.AspectRatio;
        _basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f), aspectRatio, 0.1f, 1000f);
        Vector3 direction = new Vector3(0, _cameraDistance, 0);
        Matrix rotation = Matrix.CreateRotationX(_cameraPitch) * Matrix.CreateRotationZ(_cameraYaw);
        Vector3 target = _cameraTarget - _scrollOffset;
        _basicEffect.View = Matrix.CreateLookAt(target + Vector3.Transform(direction, rotation), target, Vector3.UnitZ);
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
        if (_currentMenuView == MenuView.Main)
        {
            if (index == 0) // Single Player
            {
                _isMultiplayerMode = false;
                LoadCharacters();
                _state = AppState.CharacterSelect;
            }
            else if (index == 1) // Multiplayer
            {
                _isMultiplayerMode = true;
                LoadCharacters();
                _state = AppState.CharacterSelect;
            }
            else if (index == 2) // Options
            {
                _currentMenuView = MenuView.Options;
                _mainMenuIndex = 0;
            }
            else if (index == 3) // Desktop
            {
                Exit();
            }
        }
        else if (_currentMenuView == MenuView.Options)
        {
            if (index == 0) // Language
            {
                _currentMenuView = MenuView.Language;
                // No need to reset index, we want to stay on the language option maybe?
                // Actually the language list will be to the right.
            }
            else if (index == 1) // Back
            {
                _currentMenuView = MenuView.Main;
                _mainMenuIndex = 2; // Back to Options
            }
        }
        else if (_currentMenuView == MenuView.Language)
        {
            if (index == 0) Loc.SetLanguage(Loc.Language.English);
            else if (index == 1) Loc.SetLanguage(Loc.Language.French);
            _currentMenuView = MenuView.Options;
            _mainMenuIndex = 0;
        }
    }

    private void ExecuteMenuAction(int index)
    {
        if (_currentMenuView == MenuView.Main)
        {
            if (index == 0) // Continue
            {
                _isMenuOpen = false;
            }
            else if (index == 1) // Options
            {
                _currentMenuView = MenuView.Options;
                _menuIndex = 0;
            }
            else if (index == 2) // Main Menu
            {
                SaveCurrentProgress();
                _combatManager.EndCombat();
                _luteMusic.Stop();
                _state = AppState.MainMenu;
                _isMenuOpen = false;
                _currentMenuView = MenuView.Main;
            }
            else if (index == 3) // Desktop
            {
                // OnExiting handles the save
                Exit();
            }
        }
        else if (_currentMenuView == MenuView.Options)
        {
            if (index == 0) // Language
            {
                _currentMenuView = MenuView.Language;
            }
            else if (index == 1) // Back
            {
                _currentMenuView = MenuView.Main;
                _menuIndex = 1;
            }
        }
        else if (_currentMenuView == MenuView.Language)
        {
            if (index == 0) Loc.SetLanguage(Loc.Language.English);
            else if (index == 1) Loc.SetLanguage(Loc.Language.French);
            _currentMenuView = MenuView.Options;
            _menuIndex = 0;
        }
    }
    
    protected override void OnExiting(object sender, Microsoft.Xna.Framework.ExitingEventArgs args)
    {
        SaveCurrentProgress();
        _luteSynth?.Dispose();
        _mapSfxSynth?.Dispose();
        _torchIgnitionSfx?.Dispose();
        _footstepSfx?.Dispose();
        base.OnExiting(sender, args);
    }

    private void UpdateFootstepAudio(Creature creature)
    {
        int tileX = (int)MathF.Round(creature.VisualX);
        int tileY = (int)MathF.Round(creature.VisualY);
        int tileZ = (int)MathF.Round(creature.VisualZ);
        var visualTile = (tileX, tileY, tileZ);

        if (!_lastStepTiles.TryGetValue(creature, out var previousTile))
        {
            _lastStepTiles[creature] = visualTile;
            return;
        }

        if (previousTile == visualTile)
            return;

        _lastStepTiles[creature] = visualTile;
        TileType terrain = _tacticalMap.Get(tileX, tileY, tileZ);
        _footstepSfx.PlayStep(terrain);
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

        float totalSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_aiDelayTimer > 0)
            _aiDelayTimer -= totalSeconds;

        _diceRollAnimation.Update(totalSeconds);

        for (int i = _activeTooltips.Count - 1; i >= 0; i--)
        {
            _activeTooltips[i].RemainingTime -= totalSeconds;
            if (_activeTooltips[i].RemainingTime <= 0)
                _activeTooltips.RemoveAt(i);
        }

        // MAIN MENU
        if (_state == AppState.MainMenu)
        {
            if (Loc.CurrentLanguage != _lastLanguage) UpdateMenuCache();
            var currentList = _currentMenuView switch
            {
                MenuView.Options => _cachedOptionsMenuItems,
                MenuView.Language => _cachedLanguageMenuItems,
                _ => _cachedMainMenuItems
            };

            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _mainMenuIndex = (_mainMenuIndex - 1 + currentList.Length) % currentList.Length;
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _mainMenuIndex = (_mainMenuIndex + 1) % currentList.Length;
            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
                ExecuteMainMenuAction(_mainMenuIndex);

            var vp = GraphicsDevice.Viewport;
            int menuWidth = 480;
            int itemHeight = 48;
            int padding = 12;
            int titleHeight = 120;
            int menuHeight = titleHeight + currentList.Length * (itemHeight + padding) + padding;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            if (_currentMenuView == MenuView.Language)
            {
                // Move menu to the right if language list
                menuRect.X += 260;
                // And we also want to be able to click back on the main menu?
                // The user said "a list that appears to the right", suggesting the previous menu is still visible.
            }

            for (int i = 0; i < currentList.Length; i++)
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
            if (HasPendingDeleteConfirmation)
            {
                if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
                {
                    CancelPendingDelete();
                }
                HandlePendingDeleteMouseInput(mouse);

                _prevKb = kb;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

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
                            CreatePlayerCreatureAtSafeSpawn();
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

            var backRect = new Rectangle(menuRect.X + padding, menuRect.Y + 48, 110, 30);
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
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + headerHeight + padding + i * (itemHeight + padding), listWidth - padding * 2, itemHeight);
                var deleteRect = new Rectangle(itemRect.X + itemRect.Width - 70, itemRect.Y + (itemRect.Height - 30) / 2, 60, 30);
                
                if (deleteRect.Contains(mouse.Position))
                {
                    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    {
                        RequestDeleteCharacter(i);
                        clickedDelete = true;
                        break;
                    }
                }
            }

            if (!clickedDelete)
            {
                for (int i = 0; i < GetCharacterMenuItemCount(); i++)
                {
                    var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + headerHeight + padding + i * (itemHeight + padding), listWidth - padding * 2, itemHeight);
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
                                        CreatePlayerCreatureAtSafeSpawn();
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
                if (_isMultiplayerMode)
                {
                    // Initialize player creature from the selected character
                    CreatePlayerCreatureAtSafeSpawn();
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
            if (HasPendingDeleteConfirmation)
            {
                if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
                {
                    CancelPendingDelete();
                }
                HandlePendingDeleteMouseInput(mouse);

                _prevKb = kb;
                _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

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
            
            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
            {
                if (_campaignIndex >= 0 && _campaignIndex < _campaigns.Count)
                {
                    _currentCampaign = _campaigns[_campaignIndex];
                    _lastTotalGameMinutes = _currentCampaign.TotalGameMinutes;
                    _torchTimerAccumulator = 0;
                    RestoreVisionExplorationFromCampaign();
                    RestoreGroundItemsFromCampaign();
                    if (_currentCharacter != null && !_currentCampaign.PartyMembers.Contains(_currentCharacter.Name))
                    {
                        _currentCampaign.PartyMembers.Add(_currentCharacter.Name);
                        _currentCampaign.LastPlayedDate = DateTime.Now;
                        SaveCampaigns();
                    }
                    
                    // Initialize player creature from the selected character
                    if (_currentCharacter != null)
                    {
                        CreatePlayerCreatureAtSafeSpawn();
                        RestoreTacticalStateFromCampaign();
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
            int headerHeight = 110;
            int footerHeight = 40;
            int menuHeight = headerHeight + Math.Max(1, _campaigns.Count + 1) * (itemHeight + padding) + padding + footerHeight;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

            var backRect = new Rectangle(menuRect.X + padding, menuRect.Y + 48, 110, 30);
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
                var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + headerHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                var deleteRect = new Rectangle(itemRect.X + itemRect.Width - 70, itemRect.Y + (itemRect.Height - 30) / 2, 60, 30);
                
                if (deleteRect.Contains(mouse.Position))
                {
                    if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    {
                        RequestDeleteCampaign(i);
                        clickedDelete = true;
                        break;
                    }
                }
            }

            if (!clickedDelete)
            {
                for (int i = 0; i <= _campaigns.Count; i++)
                {
                    var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + headerHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _campaignIndex = i;
                        
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            if (i < _campaigns.Count)
                            {
                                _currentCampaign = _campaigns[i];
                                RestoreVisionExplorationFromCampaign();
                                RestoreGroundItemsFromCampaign();
                                if (_currentCharacter != null && !_currentCampaign.PartyMembers.Contains(_currentCharacter.Name))
                                {
                                    _currentCampaign.PartyMembers.Add(_currentCharacter.Name);
                                    _currentCampaign.LastPlayedDate = DateTime.Now;
                                    SaveCampaigns();
                                }
                                
                                // Initialize player creature from the selected character
                                if (_currentCharacter != null)
                                {
                                    CreatePlayerCreatureAtSafeSpawn();
                                    RestoreTacticalStateFromCampaign();
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
                _lastTotalGameMinutes = _currentCampaign.TotalGameMinutes;
                _torchTimerAccumulator = 0;
                RestoreVisionExplorationFromCampaign();
                if (_currentCharacter != null)
                {
                    _currentCampaign.PartyMembers.Add(_currentCharacter.Name);
                }
                SaveCampaign();
                
                // Initialize player creature from the selected character
                if (_currentCharacter != null)
                {
                    CreatePlayerCreatureAtSafeSpawn();
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
            _campaignMapViewer.Update(_currentCampaign, _characters, mouse, kb, _prevKb, gameTime, GraphicsDevice.Viewport);

            if (_currentCampaign.EncounterRequested)
            {
                _currentCampaign.EncounterRequested = false;
                CloseCampaignMap();
                TriggerEncounterSpawn();
                StartCombatWithNearbyEnemies();
            }
        }

        // Handle Combat Log dragging
        if (_combatManager.InCombat && _showCombatUI)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (!_isDraggingCombatLog && _combatLogWindowRect.Contains(mouse.Position) && _prevMouse.LeftButton == ButtonState.Released)
                {
                    _isDraggingCombatLog = true;
                    _dragOffset = new Point(mouse.X - _combatLogWindowRect.X, mouse.Y - _combatLogWindowRect.Y);
                }

                if (_isDraggingCombatLog)
                {
                    _combatLogWindowRect.X = mouse.X - _dragOffset.X;
                    _combatLogWindowRect.Y = mouse.Y - _dragOffset.Y;

                    // Keep on screen
                    var vp = GraphicsDevice.Viewport;
                    _combatLogWindowRect.X = Math.Clamp(_combatLogWindowRect.X, 0, vp.Width - _combatLogWindowRect.Width);
                    _combatLogWindowRect.Y = Math.Clamp(_combatLogWindowRect.Y, 0, vp.Height - _combatLogWindowRect.Height);
                }
            }
            else
            {
                _isDraggingCombatLog = false;
            }
        }

        // Normal gameplay and pause menu handling
        // Let gameplay overlays (character sheet/journal) consume Escape first.
        bool isGameplayOverlayOpen = _state == AppState.Playing && (_showCharacterSheet || _showJournal);
        if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape) && !isGameplayOverlayOpen)
        {
            _isMenuOpen = !_isMenuOpen;
            if (_isMenuOpen) _menuIndex = 0;
        }

        if (_isMenuOpen)
        {
            if (Loc.CurrentLanguage != _lastLanguage) UpdateMenuCache();
            var currentList = _currentMenuView switch
            {
                MenuView.Options => _cachedOptionsMenuItems,
                MenuView.Language => _cachedLanguageMenuItems,
                _ => _cachedPauseMenuItems
            };

            if (kb.IsKeyDown(Keys.Up) && !_prevKb.IsKeyDown(Keys.Up))
                _menuIndex = (_menuIndex - 1 + currentList.Length) % currentList.Length;
            if (kb.IsKeyDown(Keys.Down) && !_prevKb.IsKeyDown(Keys.Down))
                _menuIndex = (_menuIndex + 1) % currentList.Length;
            if (kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
                ExecuteMenuAction(_menuIndex);

            var vp2 = GraphicsDevice.Viewport;
            int menuWidth2 = 360;
            int itemHeight2 = 48;
            int padding2 = 12;
            int menuHeight2 = currentList.Length * (itemHeight2 + padding2) + padding2;
            var menuRect2 = new Rectangle((vp2.Width - menuWidth2) / 2, (vp2.Height - menuHeight2) / 2, menuWidth2, menuHeight2);

            if (_currentMenuView == MenuView.Language)
                menuRect2.X += 200;

            for (int i = 0; i < currentList.Length; i++)
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
            float dtPlaying = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle travel even when map is closed
            if (!_showCampaignMap && !_combatManager.InCombat)
            {
                _currentCampaign.UpdateTravel(dtPlaying, CampaignMapViewer.TimeScale, _characters, _visionSystem);

                float totalTacticalX = _currentCampaign.PartyX * Campaign.TacticalUnitsPerMile;
                float totalTacticalY = _currentCampaign.PartyY * Campaign.TacticalUnitsPerMile;
                int intX = (int)Math.Floor(totalTacticalX);
                int intY = (int)Math.Floor(totalTacticalY);

                if (_currentCampaign.IsTraveling)
                {
                    _scrollOffset = new Vector3(totalTacticalX - intX, totalTacticalY - intY, 0);

                    // Simulate walking bobbing for player
                    float walkSpeed = 10f;
                    float bob = MathF.Abs(MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * walkSpeed)) * 0.2f;
                    _playerCreature.VisualZ = _playerCreature.Z + bob;

                    // Trigger footsteps based on distance
                    if (Math.Abs(totalTacticalX - _lastTotalTacticalX) > 0.5f || Math.Abs(totalTacticalY - _lastTotalTacticalY) > 0.5f)
                    {
                         _footstepSfx.PlayStep(_tacticalMap.Get(_playerCreature.X, _playerCreature.Y, _playerCreature.Z));
                         _lastTotalTacticalX = totalTacticalX;
                         _lastTotalTacticalY = totalTacticalY;
                    }
                }
                else
                {
                    _scrollOffset = Vector3.Zero;
                    _playerCreature.VisualZ = _playerCreature.Z;
                }

                if (intX != _lastIntPartyX || intY != _lastIntPartyY)
                {
                    _tacticalMap.Clear();
                    _urbanDoorStates.Clear(); // Clear transient urban door states when moving far
                    _lastIntPartyX = intX;
                    _lastIntPartyY = intY;
                    PlacePlayerAtNearestValidTile();
                    _playerCreature.InterruptMovement();
                    _cameraTarget = Vector3.Zero;
                    UpdateVision();
                    _currentCampaign.PartyPositionChanged = false;
                }

                if (_currentCampaign.EncounterRequested)
                {
                    _currentCampaign.EncounterRequested = false;
                    TriggerEncounterSpawn();
                    StartCombatWithNearbyEnemies();
                }

                if (_currentCampaign.TravelMessageTimer > 0)
                {
                    _currentCampaign.TravelMessageTimer -= dtPlaying;
                    if (_currentCampaign.TravelMessageTimer <= 0) _currentCampaign.LastTravelMessage = "";
                }
            }
            else
            {
                _scrollOffset = Vector3.Zero;
            }

            // Increment and check auto-save timer
            _autoSaveTimer += dtPlaying;
            if (_autoSaveTimer >= AutoSaveInterval)
            {
                SaveCurrentProgress();
                System.Console.WriteLine("Auto-save triggered (interval reached).");
            }

            // Handle armor donning/doffing time passage
            if (_currentCampaign != null)
            {
                var activeProcesses = _characters
                    .Where(c => _currentCampaign.PartyMembers.Contains(c.Name) && c.CurrentDonDoffProcess is { IsActive: true })
                    .ToList();

                if (activeProcesses.Count > 0)
                {
                    if (_showCampaignMap)
                    {
                        // In Campaign Map, advance time faster while "waiting"
                        double minutesToAdvance = dtPlaying * 10.0; // 10 minutes per real second while busy?

                        _currentCampaign.TotalGameMinutes += minutesToAdvance;

                        foreach (var c in activeProcesses)
                        {
                            c.CurrentDonDoffProcess!.MinutesRemaining -= minutesToAdvance;
                            if (c.CurrentDonDoffProcess.MinutesRemaining <= 0)
                            {
                                CompleteDonDoff(c);
                            }
                        }
                    }
                    else if (!_combatManager.InCombat)
                    {
                        // In Tactical mode but not in combat
                        // 1 game minute per 60 real seconds
                        double minutesToAdvance = dtPlaying / 60.0;

                        foreach (var c in activeProcesses)
                        {
                            c.CurrentDonDoffProcess!.MinutesRemaining -= minutesToAdvance;
                            if (c.CurrentDonDoffProcess.MinutesRemaining <= 0)
                            {
                                CompleteDonDoff(c);
                            }
                        }
                    }
                }
            }

            // Check for completed processes in combat
            if (_combatManager.InCombat)
            {
                foreach (var c in _characters)
                {
                    if (c.CurrentDonDoffProcess is { IsActive: false, Item: not null })
                    {
                        CompleteDonDoff(c);
                    }
                }
            }


            _luteMusic.Update(dtPlaying);
            _luteSynth.Update();

            // Synchronize vision system with campaign time
            if (_currentCampaign != null)
            {
                var campaignLight = _currentCampaign.GetCurrentLightLevel();
                if (_visionSystem.AmbientLight != campaignLight)
                {
                    _visionSystem.AmbientLight = campaignLight;
                    _visionSystem.MarkLightingDirty();
                    UpdateVision();
                }
            }

            bool wasCharacterSheetOpen = _showCharacterSheet;
            bool wasJournalOpen = _showJournal;
            bool mouseClickedThisFrame = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            bool rightClickedThisFrame = mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;
            bool clickedOnGameplayUiButton = false;

        if (_showDoorContextMenu && mouseClickedThisFrame)
        {
            if (HandleDoorContextMenu(mouse.Position))
                clickedOnGameplayUiButton = true;
            else
                _showDoorContextMenu = false;
        }

            // Update movement animation for all creatures
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_playerCreature != null)
            {
                _playerCreature.UpdateMovementAnimation(deltaTime);
                UpdateFootstepAudio(_playerCreature);

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
                    if (creature != _playerCreature)
                        UpdateFootstepAudio(creature);
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
                if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
                {
                    _showCharacterSheet = false;
                }

                var closeSheetButtonRect = _characterSheet.GetCloseButtonRect(GraphicsDevice.Viewport);
                if (mouseClickedThisFrame &&
                    closeSheetButtonRect.Contains(mouse.Position))
                {
                    _showCharacterSheet = false;
                    clickedOnGameplayUiButton = true;
                }
            }

            if (_showJournal)
            {
                if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape))
                {
                    _showJournal = false;
                }

                var closeJournalButtonRect = JournalUI.GetCloseButtonRect(GraphicsDevice.Viewport);
                if (mouseClickedThisFrame &&
                    closeJournalButtonRect.Contains(mouse.Position))
                {
                    _showJournal = false;
                    clickedOnGameplayUiButton = true;
                }
            }

            var inventoryButtonRect = GetInventoryButtonRect(GraphicsDevice.Viewport);

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
                if (_showCampaignMap)
                    CloseCampaignMap();
                else
                    OpenCampaignMap();
                clickedOnGameplayUiButton = true;
            }

            var spawnButtonRect = GetSpawnButtonRect(GraphicsDevice.Viewport);
            if (!_showCharacterSheet &&
                mouseClickedThisFrame &&
                spawnButtonRect.Contains(mouse.Position))
            {
                TriggerEncounterSpawn();
                clickedOnGameplayUiButton = true;
            }

            var rotateLeftButtonRect = GetRotateLeftButtonRect(GraphicsDevice.Viewport);
            var rotateRightButtonRect = GetRotateRightButtonRect(GraphicsDevice.Viewport);
            var combatMoveButtonRect = GetCombatMoveButtonRect(GraphicsDevice.Viewport);
            var combatAttackButtonRect = GetCombatAttackButtonRect(GraphicsDevice.Viewport);
            var combatCastSpellButtonRect = GetCombatCastSpellButtonRect(GraphicsDevice.Viewport);
            var combatBonusActionButtonRect = GetCombatBonusActionButtonRect(GraphicsDevice.Viewport);
            var combatEndTurnButtonRect = GetCombatEndTurnButtonRect(GraphicsDevice.Viewport);
            var combatRageButtonRect = GetCombatRageButtonRect(GraphicsDevice.Viewport);
            var combatDashButtonRect = GetCombatDashButtonRect(GraphicsDevice.Viewport);
            var combatDisengageButtonRect = GetCombatDisengageButtonRect(GraphicsDevice.Viewport);
            var combatDodgeButtonRect = GetCombatDodgeButtonRect(GraphicsDevice.Viewport);

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
                else if (_showCombatUI)
                {
                    var currentCombatant = _combatManager.InCombat ? _combatManager.CurrentCombatant : _playerCreature;
                    if (currentCombatant != null && currentCombatant.IsPlayer)
                    {
                        if (combatMoveButtonRect.Contains(mouse.Position))
                        {
                            if (_combatManager.InCombat && currentCombatant.MovementRemaining == 0 && currentCombatant.HasAction)
                            {
                                if (_combatManager.Dash(currentCombatant))
                                    AddTooltip(currentCombatant, Loc.Tr("Action: Dash"), Color.Cyan);
                                AddToCombatLog(Loc.Tr("{0} uses DASH via Move button.", currentCombatant.Name));
                            }
                            _selectedAction = CombatAction.Move;
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_selectedAction == CombatAction.Move && (currentCombatant.HasAction || !_combatManager.InCombat) && combatDashButtonRect.Contains(mouse.Position))
                        {
                            if (_combatManager.Dash(currentCombatant))
                                AddTooltip(currentCombatant, Loc.Tr("Action: Dash"), Color.Cyan);
                            FlushTurnMessages();
                            AddToCombatLog(Loc.Tr("{0} uses DASH.", currentCombatant.Name));
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_selectedAction == CombatAction.Move && (currentCombatant.HasAction || !_combatManager.InCombat) && combatDisengageButtonRect.Contains(mouse.Position))
                        {
                            if (_combatManager.Disengage(currentCombatant))
                                AddTooltip(currentCombatant, Loc.Tr("Action: Disengage"), Color.Cyan);
                            FlushTurnMessages();
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_selectedAction == CombatAction.Move && (currentCombatant.HasAction || !_combatManager.InCombat) && combatDodgeButtonRect.Contains(mouse.Position))
                        {
                            if (_combatManager.Dodge(currentCombatant))
                                AddTooltip(currentCombatant, Loc.Tr("Action: Dodge"), Color.Cyan);
                            FlushTurnMessages();
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_selectedAction == CombatAction.Move && (currentCombatant.HasAction || !_combatManager.InCombat) && GetCombatHideButtonRect(GraphicsDevice.Viewport).Contains(mouse.Position))
                        {
                            if (_combatManager.Hide(currentCombatant, visionSystem: _visionSystem))
                            {
                                if (currentCombatant.IsHidden)
                                    AddTooltip(currentCombatant, Loc.Tr("Hidden!"), Color.LimeGreen);
                                else
                                    AddTooltip(currentCombatant, Loc.Tr("Missed!"), Color.LightGray);
                            }
                            FlushTurnMessages();
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_selectedAction == CombatAction.Move && (currentCombatant.HasAction || !_combatManager.InCombat) && GetCombatHelpActionButtonRect(GraphicsDevice.Viewport).Contains(mouse.Position))
                        {
                            _selectedAction = CombatAction.Help;
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (combatAttackButtonRect.Contains(mouse.Position))
                        {
                            if (_selectedAction == CombatAction.Attack)
                            {
                                // Reset stats from current equipped if they were changed
                                _playerCreature!.SetupAttackFromWeapon(_currentCharacter.InventoryData.EquippedWeapon?.Name ?? "Unarmed Strike", _currentCharacter);
                            }
                            _selectedAction = CombatAction.Attack;
                            _activeSpell = null;
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_selectedAction == CombatAction.Attack && (currentCombatant.HasAction || !_combatManager.InCombat) && GetCombatGrappleButtonRect(GraphicsDevice.Viewport).Contains(mouse.Position))
                        {
                            _selectedAction = CombatAction.Grapple;
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_selectedAction == CombatAction.Attack && (currentCombatant.HasAction || !_combatManager.InCombat) && GetCombatThrowAcidButtonRect(GraphicsDevice.Viewport).Contains(mouse.Position) && _currentCharacter?.InventoryData.HasItem("Acid (vial)") == true)
                        {
                            _selectedAction = CombatAction.ThrowAcid;
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (combatCastSpellButtonRect.Contains(mouse.Position) && IsSpellcasterClass(_currentCharacter.Class))
                        {
                            if (_selectedAction == CombatAction.CastSpell)
                            {
                                // Just reset to first known/prepared if not set
                                var relevantSpells = _currentCharacter.UsesSpellPreparation ? _currentCharacter.PreparedSpells : _currentCharacter.KnownSpells;
                                if (relevantSpells.Count > 0)
                                {
                                    _activeSpell = relevantSpells[0];
                                    _playerCreature!.SetupAttackFromSpell(_activeSpell, _currentCharacter);
                                }
                            }
                            else if (_activeSpell == null)
                            {
                                var relevantSpells = _currentCharacter.UsesSpellPreparation ? _currentCharacter.PreparedSpells : _currentCharacter.KnownSpells;
                                if (relevantSpells.Count > 0)
                                {
                                    _activeSpell = relevantSpells[0];
                                    _playerCreature!.SetupAttackFromSpell(_activeSpell, _currentCharacter);
                                }
                            }
                            _selectedAction = CombatAction.CastSpell;
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
                        else if (_showBonusActionMenu && _currentCharacter?.Class == "Barbarian" && combatRageButtonRect.Contains(mouse.Position))
                        {
                            if ((currentCombatant.HasBonusAction || !_combatManager.InCombat) && currentCombatant.RagesRemaining > 0 && !currentCombatant.IsRaging && _currentCharacter?.Class == "Barbarian")
                            {
                                _combatManager.StartRage(currentCombatant);
                                if (_combatManager.InCombat)
                                    currentCombatant.HasBonusAction = false;
                                AddTooltip(currentCombatant, Loc.Tr("RAGE!"), Color.Gold);
                                AddToCombatLog(Loc.Tr("{0} enters RAGE!", currentCombatant.Name));
                            }
                            _showBonusActionMenu = false;
                            clickedOnGameplayUiButton = true;
                        }
                        else if (_showBonusActionMenu && GetCombatBonusHideButtonRect(GraphicsDevice.Viewport).Contains(mouse.Position))
                        {
                            if ((currentCombatant.HasBonusAction || !_combatManager.InCombat) && currentCombatant.HasNimbleEscape)
                            {
                                if (_combatManager.Hide(currentCombatant, isBonusAction: true, visionSystem: _visionSystem))
                                {
                                    if (currentCombatant.IsHidden)
                                    AddTooltip(currentCombatant, Loc.Tr("Hidden!"), Color.LimeGreen);
                                    else
                                    AddTooltip(currentCombatant, Loc.Tr("Missed!"), Color.LightGray);
                                }
                                FlushTurnMessages();
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
                if (_showCampaignMap)
                    CloseCampaignMap();
                else
                    OpenCampaignMap();
            }
            
            // Change view level with PageUp/PageDown
            if (kb.IsKeyDown(Keys.PageUp) && !_prevKb.IsKeyDown(Keys.PageUp))
            {
                _currentViewLevel++;
                AddToCombatLog(Loc.Tr("Viewing level {0}", _currentViewLevel));
            }
            if (kb.IsKeyDown(Keys.PageDown) && !_prevKb.IsKeyDown(Keys.PageDown))
            {
                _currentViewLevel = Math.Max(0, _currentViewLevel - 1);
                AddToCombatLog(Loc.Tr("Viewing level {0}", _currentViewLevel));
            }
            
            
            // Toggle vision overlay with V
            if (kb.IsKeyDown(Keys.V) && !_prevKb.IsKeyDown(Keys.V))
            {
                _showVisionOverlay = !_showVisionOverlay;
            }
            
            
            // Toggle HUD visibility preference with Tab
            if (kb.IsKeyDown(Keys.Tab) && !_prevKb.IsKeyDown(Keys.Tab))
            {
                _hudAlwaysVisible = !_hudAlwaysVisible;
                _showCombatUI = _hudAlwaysVisible;
                if (_showCombatUI && _selectedAction == CombatAction.None)
                {
                    _selectedAction = CombatAction.Move;
                }
            }

            if (_showCharacterSheet)
            {
                if (_characterSheet.Update(mouse, _currentCharacter, _currentCampaign))
                {
                    // Refresh creature stats from character (AC, Speed, etc.)
                    if (_playerCreature != null && _currentCharacter != null)
                    {
                        _playerCreature.ArmorClass = _currentCharacter.ArmorClass;
                        _playerCreature.Speed = _currentCharacter.Speed;
                        _playerCreature.DarkvisionRange = _currentCharacter.DarkvisionRange;

                        // If current attack is no longer valid (e.g. weapon unequipped), refresh it
                        var eq = _currentCharacter.InventoryData.EquippedWeapon;
                        var oh = _currentCharacter.InventoryData.OffhandWeapon;
                        bool isMainEquipped = eq != null && eq.Name == _playerCreature.AttackName;
                        bool isOffhandEquipped = oh != null && oh.Name == _playerCreature.AttackName;
                        bool isSpell = _activeSpell != null && _activeSpell.Name == _playerCreature.AttackName;
                        bool isUnarmed = _playerCreature.AttackName == "Unarmed Strike";

                        if (!isMainEquipped && !isOffhandEquipped && !isSpell && !isUnarmed)
                        {
                            if (eq != null)
                                _playerCreature.SetupAttackFromWeapon(eq.Name, _currentCharacter, false);
                            else if (oh != null)
                                _playerCreature.SetupAttackFromWeapon(oh.Name, _currentCharacter, true);
                            else
                                _playerCreature.SetupAttackFromWeapon("Unarmed Strike", _currentCharacter, false);
                        }
                    }

                    SaveCharacters();
                    UpdateVision();
                }

                if (_characterSheet.CloseRequested)
                {
                    _showCharacterSheet = false;
                }

                if (_characterSheet.AttackRequestedWithItem != null)
                {
                    _playerCreature!.SetupAttackFromWeapon(_characterSheet.AttackRequestedWithItem.Name, _currentCharacter!, _characterSheet.AttackRequestedIsOffhand, _characterSheet.AttackRequestedIsThrown, _characterSheet.AttackRequestedIsTwoHanded, _characterSheet.AttackRequestedIsImprovisedMelee);
                    _selectedAction = CombatAction.Attack;
                    _activeSpell = null;
                    _characterSheet.AttackRequestedWithItem = null;
                    _characterSheet.AttackRequestedIsOffhand = false;
                    _characterSheet.AttackRequestedIsThrown = false;
                    _characterSheet.AttackRequestedIsTwoHanded = false;
                    _characterSheet.AttackRequestedIsImprovisedMelee = false;
                }

                if (_characterSheet.AttackRequestedWithSpell != null)
                {
                    _activeSpell = _characterSheet.AttackRequestedWithSpell;
                    _playerCreature!.SetupAttackFromSpell(_activeSpell, _currentCharacter!);
                    _selectedAction = CombatAction.CastSpell;
                    _characterSheet.AttackRequestedWithSpell = null;
                }

                if (_characterSheet.AttackRequestedWithUnarmed)
                {
                    _playerCreature!.SetupAttackFromWeapon("Unarmed Strike", _currentCharacter!);
                    _selectedAction = CombatAction.Attack;
                    _activeSpell = null;
                    _characterSheet.AttackRequestedWithUnarmed = false;
                }

                if (_characterSheet.DroppedItem != null)
                {
                    if (_playerCreature != null)
                    {
                        _groundItems.Add(new GroundItem
                        {
                            X = _playerCreature.X,
                            Y = _playerCreature.Y,
                            Z = _playerCreature.Z,
                            Item = _characterSheet.DroppedItem
                        });
                        AddToCombatLog(Loc.Tr("You dropped {0}.", _characterSheet.DroppedItem.Name));
                        UpdateVision();
                    }
                    _characterSheet.DroppedItem = null;
                }

                if (_characterSheet.PlayLuteRequested)
                {
                    _luteMusic.PlayRandomTune();
                    _characterSheet.PlayLuteRequested = false;
                }

                if (_characterSheet.TorchIgniteRequested)
                {
                    _torchIgnitionSfx.Play();
                    _characterSheet.TorchIgniteRequested = false;
                }

                if (_characterSheet.GrappleRequested && _currentCharacter != null)
                {
                    int roll = Dice.Roll(20);
                    int bonus = _currentCharacter.GetSkillBonus("Athletics", out _);
                    int total = roll + bonus;
                    string sign = bonus >= 0 ? $"+{bonus}" : $"{bonus}";
                    AddToCombatLog(Loc.Tr("Grapple: d20({0}){1} = {2} (Athletics)", roll, sign, total));
                    _characterSheet.GrappleRequested = false;
                }

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

            if (rightClickedThisFrame)
            {
                var hovered = GetHoveredTile();
                Creature? target = null;
                if (hovered.HasValue)
                {
                    target = _combatManager.GetCreatureAt(hovered.Value.x, hovered.Value.y, hovered.Value.z);
                }

                if (hovered.HasValue && IsDungeonDoor(_tacticalMap.Get(hovered.Value.x, hovered.Value.y, hovered.Value.z)))
                {
                    ShowDoorContextMenu(hovered.Value.x, hovered.Value.y, hovered.Value.z, mouse.Position);
                    clickedOnGameplayUiButton = true;
                }
                else if (target != null && !target.IsPlayer && target.IsAlive())
                {
                    _contextTargetEnemy = target;
                    _showEnemyContextMenu = true;
                    _enemyExamineText = "";

                    const int menuWidth = 140;
                    const int menuHeight = 40;
                    int x = Math.Clamp(mouse.X, 8, GraphicsDevice.Viewport.Width - menuWidth - 8);
                    int y = Math.Clamp(mouse.Y, 8, GraphicsDevice.Viewport.Height - menuHeight - 8);
                    _enemyContextMenuRect = new Rectangle(x, y, menuWidth, menuHeight);
                    _enemyExamineOptionRect = new Rectangle(x + 6, y + 6, menuWidth - 12, menuHeight - 12);
                    clickedOnGameplayUiButton = true;
                }
                else
                {
                    _showEnemyContextMenu = false;
                    _contextTargetEnemy = null;
                }
            }

            if (_showEnemyContextMenu && mouseClickedThisFrame)
            {
                if (_enemyExamineOptionRect.Contains(mouse.Position) && _contextTargetEnemy != null)
                {
                    _enemyExamineText = BuildEnemyExamineText(_contextTargetEnemy);
                    AddToCombatLog(Loc.Tr("Examine: {0}", _contextTargetEnemy.Name));
                    _showEnemyContextMenu = false;
                    clickedOnGameplayUiButton = true;
                }
                else if (!_enemyContextMenuRect.Contains(mouse.Position))
                {
                    _showEnemyContextMenu = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_enemyExamineText) && mouseClickedThisFrame && !_enemyExaminePopupRect.Contains(mouse.Position))
            {
                _enemyExamineText = "";
            }

            // Exploration movement (outside combat)
            if (!_combatManager.InCombat && !_showCampaignMap)
            {
                if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
                {
                    // Cancel travel if player clicks on tactical map
                    if (_currentCampaign.IsTraveling)
                    {
                        _currentCampaign.IsTraveling = false;
                        _currentCampaign.SetTravelMessage(Loc.Tr("Travel cancelled."));
                        clickedOnGameplayUiButton = true;
                    }

                    var hovered = GetHoveredTile();
                    if (hovered.HasValue && IsDungeonDoor(_tacticalMap.Get(hovered.Value.x, hovered.Value.y, hovered.Value.z)))
                    {
                        ToggleDoor(hovered.Value.x, hovered.Value.y, hovered.Value.z);
                        clickedOnGameplayUiButton = true;
                    }
                    if (hovered.HasValue && _playerCreature != null && !clickedOnGameplayUiButton)
                    {
                        int tx = hovered.Value.x;
                        int ty = hovered.Value.y;
                        int tz = hovered.Value.z;

                        var tileType = _tacticalMap.Get(tx, ty, tz);
                        if (tileType != TileType.Wall && tileType != TileType.Tree && tileType != TileType.Shrub && tileType != TileType.Empty)
                        {
                            if (_combatManager.GetCreatureAt(tx, ty, tz) == null)
                            {
                                var path = _combatManager.GetPath(_playerCreature, tx, ty, tz);

                                // Check if destination is stairs
                                if (tileType == TileType.DungeonStairsUp || tileType == TileType.DungeonStairsDown)
                                {
                                    // Use stairs immediately
                                    var stairs = GetStairsState(tx, ty, tz);
                                    if (stairs != null)
                                    {
                                        _playerCreature.TeleportTo(tx, ty, stairs.TargetZ);
                                        _currentViewLevel = stairs.TargetZ;
                                        AddToCombatLog(Loc.Tr("You used the stairs."));
                                        UpdateVision();
                                        clickedOnGameplayUiButton = true;
                                        return;
                                    }
                                }

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
                                int tz = hovered.Value.z;
                                var target = _combatManager.GetCreatureAt(tx, ty, tz);
                                bool isOffhandAttack = currentCombatant.IsOffhandAttack;
                                bool canAttack = isOffhandAttack
                                    ? (currentCombatant.HasBonusAction || !_combatManager.InCombat)
                                    : (currentCombatant.HasAction || !_combatManager.InCombat);
                                if (target != null && !target.IsPlayer && canAttack)
                                {
                                    bool wasInCombat = _combatManager.InCombat;
                                    AttackResult result = isOffhandAttack
                                        ? _combatManager.MakeBonusActionAttack(currentCombatant, target, _visionSystem)
                                        : _combatManager.MakeAttack(currentCombatant, target, _visionSystem);
                                    AddToCombatLog(result.GetMessage());
                                    AddTooltip(currentCombatant, Loc.Tr("Attack: {0}", currentCombatant.AttackName), Color.Orange);
                                    if (result.IsHit)
                                    {
                                        string dmgText = result.IsCritical ? Loc.Tr("CRIT! -{0} HP", result.Damage) : Loc.Tr("-{0} HP", result.Damage);
                                        AddTooltip(target, dmgText, Color.Red);
                                    }
                                    else
                                        AddTooltip(target, Loc.Tr("Missed!"), Color.LightGray);
                                    _diceRollAnimation.Start(result.AttackRoll);
                                    _selectedAction = CombatAction.Move;

                                    if (!wasInCombat && target.IsAlive())
                                    {
                                        StartCombatWithNearbyEnemies();
                                    }
                                    else if (wasInCombat && !_combatManager.InCombat)
                                    {
                                        AddToCombatLog(Loc.Tr("Combat ended!"));
                                        if (_playerCreature != null && _currentCharacter != null)
                                        {
                                            _playerCreature.UpdateCharacter(_currentCharacter);
                                            SaveCharacters();
                                        }
                                    }
                                }
                                else if (target != null && !canAttack && _combatManager.InCombat)
                                {
                                    AddToCombatLog(isOffhandAttack ? Loc.Tr("No bonus action available!") : Loc.Tr("No action available!"));
                                }
                            }
                        }
                    }

                    // Handle grapple action
                    if (_selectedAction == CombatAction.Grapple)
                    {
                        if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
                        {
                            var hovered = GetHoveredTile();
                            if (hovered.HasValue)
                            {
                                int tx = hovered.Value.x;
                                int ty = hovered.Value.y;
                                int tz = hovered.Value.z;
                                var target = _combatManager.GetCreatureAt(tx, ty, tz);
                                if (target != null && !target.IsPlayer && (currentCombatant.HasAction || !_combatManager.InCombat) && _currentCharacter != null)
                                {
                                    bool wasInCombat = _combatManager.InCombat;
                                    int attackerRoll = Dice.Roll(20);
                                    int attackerBonus = _currentCharacter.GetSkillBonus("Athletics", out _);
                                    int attackerTotal = attackerRoll + attackerBonus;

                                    int defenderRoll = Dice.Roll(20);
                                    int defStrMod = target.GetAbilityModifier(target.Strength);
                                    int defDexMod = target.GetAbilityModifier(target.Dexterity);
                                    int defenderBonus = Math.Max(defStrMod, defDexMod);
                                    int defenderTotal = defenderRoll + defenderBonus;

                                    string atkSign = attackerBonus >= 0 ? $"+{attackerBonus}" : $"{attackerBonus}";
                                    string defSign = defenderBonus >= 0 ? $"+{defenderBonus}" : $"{defenderBonus}";
                                    AddToCombatLog(Loc.Tr("Grapple: {0} {1}{2}={3} vs {4} {5}{6}={7}", currentCombatant.Name, attackerRoll, atkSign, attackerTotal, target.Name, defenderRoll, defSign, defenderTotal));

                                    AddTooltip(currentCombatant, Loc.Tr("Action: Grapple"), Color.Cyan);
                                    if (attackerTotal >= defenderTotal)
                                    {
                                        target.Conditions = target.Conditions.AddCondition(Condition.Grappled);
                                        AddTooltip(target, Loc.Tr("Grappled!"), Color.Orange);
                                        AddToCombatLog(Loc.Tr("{0} is GRAPPLED! (Speed=0)", target.Name));
                                    }
                                    else
                                    {
                                        AddTooltip(target, Loc.Tr("Resists!"), Color.LightGray);
                                        AddToCombatLog(Loc.Tr("{0} resists the grapple!", target.Name));
                                    }

                                    if (_combatManager.InCombat)
                                        currentCombatant.HasAction = false;
                                    _diceRollAnimation.Start(attackerRoll);
                                    _selectedAction = CombatAction.Move;

                                    if (!wasInCombat && target.IsAlive())
                                    {
                                        StartCombatWithNearbyEnemies();
                                    }
                                    else if (wasInCombat && !_combatManager.InCombat)
                                    {
                                        AddToCombatLog(Loc.Tr("Combat ended!"));
                                        if (_playerCreature != null && _currentCharacter != null)
                                        {
                                            _playerCreature.UpdateCharacter(_currentCharacter);
                                            SaveCharacters();
                                        }
                                    }
                                }
                                else if (target != null && !currentCombatant.HasAction && _combatManager.InCombat)
                                {
                                    AddToCombatLog(Loc.Tr("No action available!"));
                                }
                            }
                        }
                    }

                    // Handle cast spell action
                    if (_selectedAction == CombatAction.CastSpell)
                    {
                        if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
                        {
                            var hovered = GetHoveredTile();
                            if (hovered.HasValue)
                            {
                                int tx = hovered.Value.x;
                                int ty = hovered.Value.y;
                                int tz = hovered.Value.z;

                                if (_activeSpell != null && _activeSpell.AreaRadiusFeet > 0)
                                {
                                    bool wasInCombat = _combatManager.InCombat;
                                    var result = _combatManager.CastAreaSpell(currentCombatant, _activeSpell, tx, ty, tz);
                                    // CastAreaSpell adds its own messages to TurnMessages, which we flush later
                                    AddTooltip(currentCombatant, Loc.Tr("Spell: {0}", _activeSpell.Name), Color.DeepSkyBlue);
                                    _selectedAction = CombatAction.Move;
                                    FlushTurnMessages();

                                    if (!wasInCombat)
                                        StartCombatWithNearbyEnemies();
                                    else if (wasInCombat && !_combatManager.InCombat)
                                    {
                                        AddToCombatLog(Loc.Tr("Combat ended!"));
                                        if (_playerCreature != null && _currentCharacter != null)
                                        {
                                            _playerCreature.UpdateCharacter(_currentCharacter);
                                            SaveCharacters();
                                        }
                                    }
                                }
                                else
                                {
                                    var target = _combatManager.GetCreatureAt(tx, ty, tz);
                                    if (target != null && !target.IsPlayer && (currentCombatant.HasAction || !_combatManager.InCombat) && _currentCharacter != null)
                                    {
                                        bool wasInCombat = _combatManager.InCombat;
                                        int spellAttackBonus = currentCombatant.AttackBonus;
                                        string damageDice = currentCombatant.DamageDice;
                                        DamageType damageType = currentCombatant.CurrentDamageType;
                                        var result = _combatManager.MakeSpellAttack(currentCombatant, target, spellAttackBonus, damageDice, damageType, _visionSystem);
                                        AddToCombatLog(result.GetMessage());
                                        AddTooltip(currentCombatant, Loc.Tr("Spell: {0}", _activeSpell?.Name ?? "Spell"), Color.DeepSkyBlue);
                                        if (result.IsHit)
                                        {
                                            string dmgText = result.IsCritical ? Loc.Tr("CRIT! -{0} HP", result.Damage) : Loc.Tr("-{0} HP", result.Damage);
                                            AddTooltip(target, dmgText, Color.Red);
                                        }
                                        else
                                            AddTooltip(target, Loc.Tr("Missed!"), Color.LightGray);
                                        _diceRollAnimation.Start(result.AttackRoll);
                                        _selectedAction = CombatAction.Move;

                                        if (!wasInCombat && target.IsAlive())
                                            StartCombatWithNearbyEnemies();
                                        else if (wasInCombat && !_combatManager.InCombat)
                                        {
                                            AddToCombatLog(Loc.Tr("Combat ended!"));
                                            if (_playerCreature != null && _currentCharacter != null)
                                            {
                                                _playerCreature.UpdateCharacter(_currentCharacter);
                                                SaveCharacters();
                                            }
                                        }
                                    }
                                    else if (target != null && !currentCombatant.HasAction && _combatManager.InCombat)
                                    {
                                        AddToCombatLog(Loc.Tr("No action available!"));
                                    }
                                }
                            }
                        }
                    }

                    // Handle throw acid action
                    if (_selectedAction == CombatAction.ThrowAcid)
                    {
                        if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
                        {
                            var hovered = GetHoveredTile();
                            if (hovered.HasValue)
                            {
                                int tx = hovered.Value.x;
                                int ty = hovered.Value.y;
                                int tz = hovered.Value.z;
                                var target = _combatManager.GetCreatureAt(tx, ty, tz);
                                if (target != null && !target.IsPlayer && (currentCombatant.HasAction || !_combatManager.InCombat) && _currentCharacter != null)
                                {
                                    bool wasInCombat = _combatManager.InCombat;
                                    var result = _combatManager.ThrowAcid(currentCombatant, target, _currentCharacter, _visionSystem);
                                    AddToCombatLog(result.GetMessage());
                                    AddTooltip(currentCombatant, Loc.Tr("Throw Acid"), new Color(60, 120, 80));
                                    if (result.IsHit)
                                    {
                                        string dmgText = result.IsCritical ? Loc.Tr("CRIT! -{0} HP", result.Damage) : Loc.Tr("-{0} HP", result.Damage);
                                        AddTooltip(target, dmgText, Color.Lime);
                                    }
                                    else
                                        AddTooltip(target, Loc.Tr("Missed!"), Color.LightGray);
                                    _diceRollAnimation.Start(result.AttackRoll);
                                    FlushTurnMessages();
                                    _selectedAction = CombatAction.Move;

                                    if (!wasInCombat && target.IsAlive())
                                        StartCombatWithNearbyEnemies();
                                    else if (wasInCombat && !_combatManager.InCombat)
                                    {
                                        AddToCombatLog(Loc.Tr("Combat ended!"));
                                        if (_playerCreature != null && _currentCharacter != null)
                                        {
                                            _playerCreature.UpdateCharacter(_currentCharacter);
                                            SaveCharacters();
                                        }
                                    }
                                }
                                else if (target != null && !currentCombatant.HasAction && _combatManager.InCombat)
                                {
                                    AddToCombatLog(Loc.Tr("No action available!"));
                                }
                            }
                        }
                    }

                    // Handle Help action target selection
                    if (_selectedAction == CombatAction.Help)
                    {
                        if (mouseClickedThisFrame && !clickedOnGameplayUiButton)
                        {
                            var hovered = GetHoveredTile();
                            if (hovered.HasValue)
                            {
                                int tx = hovered.Value.x;
                                int ty = hovered.Value.y;
                                int tz = hovered.Value.z;
                                var target = _combatManager.GetCreatureAt(tx, ty, tz);
                                if (target != null && target.IsAlive() && (currentCombatant.HasAction || !_combatManager.InCombat))
                                {
                                    bool wasInCombat = _combatManager.InCombat;
                                    if (_combatManager.Help(currentCombatant, target))
                                    {
                                        AddTooltip(currentCombatant, Loc.Tr("Action: Help"), Color.Cyan);
                                        if (target.IsPlayer != currentCombatant.IsPlayer)
                                            AddTooltip(target, Loc.Tr("Distracted!"), Color.Gold);
                                        else
                                            AddTooltip(target, Loc.Tr("Helped!"), Color.LimeGreen);
                                        FlushTurnMessages();
                                        _selectedAction = CombatAction.Move;

                                        if (!wasInCombat)
                                        {
                                            StartCombatWithNearbyEnemies();
                                        }
                                        else if (wasInCombat && !_combatManager.InCombat)
                                        {
                                        AddToCombatLog(Loc.Tr("Combat ended!"));
                                            if (_playerCreature != null && _currentCharacter != null)
                                            {
                                                _playerCreature.UpdateCharacter(_currentCharacter);
                                                SaveCharacters();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        FlushTurnMessages();
                                    }
                                }
                                else if (!currentCombatant.HasAction && _combatManager.InCombat)
                                {
                                    AddToCombatLog(Loc.Tr("No action available!"));
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
                                int tz = hovered.Value.z;
                                // Check if tile is empty and within movement range
                                if (_combatManager.GetCreatureAt(tx, ty, tz) == null)
                                {
                                    bool canMove = _combatManager.CanMove(currentCombatant, tx, ty, tz);

                                    if (canMove)
                                    {
                                        int prevX = currentCombatant.X;
                                        int prevY = currentCombatant.Y;
                                        int prevZ = currentCombatant.Z;
                                        int prevMove = currentCombatant.MovementRemaining;

                                        _combatManager.Move(currentCombatant, tx, ty, tz, _visionSystem);
                                        FlushTurnMessages();

                                        int distanceInFeet = prevMove - currentCombatant.MovementRemaining;
                                        AddToCombatLog(Loc.Tr("{0} moved to ({1}, {2}, {3}) [{4}ft, {5}ft remaining]", currentCombatant.Name, currentCombatant.X, currentCombatant.Y, currentCombatant.Z, distanceInFeet, currentCombatant.MovementRemaining));
                                        _selectedAction = CombatAction.Move;
                                        
                                        // Update vision after movement
                                        UpdateVision();
                                    }
                                    else
                                    {
                                        AddToCombatLog(Loc.Tr("Out of movement range!"));
                                    }
                                }
                            }
                        }
                    }
                }
                else if (currentCombatant != null && !currentCombatant.IsPlayer)
                {
                    // Delay logic for AI turns to improve readability
                    bool isAnimating = currentCombatant.IsMoving();

                    // Safety timeout: force-snap visual position if animation is stuck for too long
                    if (isAnimating)
                    {
                        _aiAnimationStuckTimer += totalSeconds;
                        if (_aiAnimationStuckTimer > 3.0f)
                        {
                            currentCombatant.TeleportTo(currentCombatant.X, currentCombatant.Y, currentCombatant.Z);
                            isAnimating = false;
                        }
                    }
                    else
                    {
                        _aiAnimationStuckTimer = 0f;
                    }

                    if (!isAnimating && _aiDelayTimer <= 0)
                    {
                        var playerCreature = _combatManager.Combatants.FirstOrDefault(c => c.IsPlayer);
                        bool canAct = playerCreature != null;

                        if (canAct && playerCreature != null)
                        {
                            bool inMeleeRange = _combatManager.IsInMeleeRange(currentCombatant, playerCreature);
                            int distanceFeet = _combatManager.CalculateDistance(currentCombatant.X, currentCombatant.Y, currentCombatant.Z,
                                                                                  playerCreature.X, playerCreature.Y, playerCreature.Z) * 5;

                            switch (_currentAiPhase)
                            {
                                case AiPhase.Start:
                                    _aiDidSomethingThisTurn = false;
                                    _currentAiPhase = AiPhase.Move;
                                    break;

                                case AiPhase.Move:
                                    if (currentCombatant.MovementRemaining > 0 && !inMeleeRange)
                                    {
                                        var nextStep = _combatManager.GetNextStepTowards(currentCombatant, playerCreature);
                                        if (nextStep.HasValue && _combatManager.GetCreatureAt(nextStep.Value.x, nextStep.Value.y, nextStep.Value.z) == null && _combatManager.CanMove(currentCombatant, nextStep.Value.x, nextStep.Value.y, nextStep.Value.z))
                                        {
                                            _combatManager.Move(currentCombatant, nextStep.Value.x, nextStep.Value.y, nextStep.Value.z, _visionSystem);
                                            FlushTurnMessages();
                                            AddToCombatLog(Loc.Tr("{0} moved", currentCombatant.Name));
                                            UpdateVision();
                                            _aiDelayTimer = 0.8f; // Pause after moving
                                            _aiDidSomethingThisTurn = true;
                                        }
                                    }
                                    _currentAiPhase = AiPhase.Action;
                                    break;

                                case AiPhase.Action:
                                    if (currentCombatant.HasAction)
                                    {
                                        MonsterAction? chosen = null;
                                        if (currentCombatant.Actions.Count > 0)
                                        {
                                            if (inMeleeRange)
                                                chosen = currentCombatant.Actions.FirstOrDefault(a => a.ActionType == MonsterActionType.MeleeAttack);

                                            if (chosen == null)
                                                chosen = currentCombatant.Actions.FirstOrDefault(a => a.ActionType == MonsterActionType.RangedAttack && distanceFeet <= (a.LongRange > 0 ? a.LongRange : a.NormalRange));

                                            if (chosen != null && chosen.ActionType != MonsterActionType.Special)
                                                chosen.ApplyTo(currentCombatant);
                                        }

                                        // Only proceed if we have an action or if we are using the legacy path (no actions list but in range)
                                        if (chosen != null || (inMeleeRange && currentCombatant.Actions.Count == 0))
                                        {
                                            var result = _combatManager.MakeAttack(currentCombatant, playerCreature, _visionSystem);
                                            AddToCombatLog(result.GetMessage());
                                            AddTooltip(currentCombatant, Loc.Tr("Attack: {0}", currentCombatant.AttackName), Color.Orange);
                                            if (result.IsHit)
                                            {
                                                string dmgText = result.IsCritical ? Loc.Tr("CRIT! -{0} HP", result.Damage) : Loc.Tr("-{0} HP", result.Damage);
                                                AddTooltip(playerCreature, dmgText, Color.Red);
                                            }
                                            else
                                                AddTooltip(playerCreature, Loc.Tr("Missed!"), Color.LightGray);

                                            _diceRollAnimation.Start(result.AttackRoll);
                                            _aiDelayTimer = 1.2f; // Pause after attacking
                                            _aiDidSomethingThisTurn = true;
                                        }
                                    }
                                    _currentAiPhase = AiPhase.BonusAction;
                                    break;

                                case AiPhase.BonusAction:
                                    if (currentCombatant.HasNimbleEscape && currentCombatant.HasBonusAction && currentCombatant.MovementRemaining > 0)
                                    {
                                        if (_combatManager.Disengage(currentCombatant, isBonusAction: true))
                                            AddTooltip(currentCombatant, Loc.Tr("Action: Disengage"), Color.Cyan);
                                        FlushTurnMessages();
                                        var retreatStep = _combatManager.GetNextStepAwayFrom(currentCombatant, playerCreature);
                                        if (retreatStep.HasValue && _combatManager.GetCreatureAt(retreatStep.Value.x, retreatStep.Value.y, retreatStep.Value.z) == null && _combatManager.CanMove(currentCombatant, retreatStep.Value.x, retreatStep.Value.y, retreatStep.Value.z))
                                        {
                                            _combatManager.Move(currentCombatant, retreatStep.Value.x, retreatStep.Value.y, retreatStep.Value.z, _visionSystem);
                                            FlushTurnMessages();
                                            AddToCombatLog(Loc.Tr("{0} retreats", currentCombatant.Name));
                                            UpdateVision();
                                            _aiDelayTimer = 0.8f; // Pause after retreat
                                            _aiDidSomethingThisTurn = true;
                                        }
                                        currentCombatant.MovementRemaining = 0;
                                    }
                                    _currentAiPhase = AiPhase.End;
                                    break;

                                case AiPhase.End:
                                    int prevRound = _combatManager.CurrentRound;
                                    _combatManager.NextTurn();
                                    FlushTurnMessages();
                                    int newRound = _combatManager.CurrentRound;

                                    if (newRound > prevRound)
                                        AddToCombatLog(Loc.Tr("=== Round {0} ===", newRound));

                                    _currentAiPhase = AiPhase.Start;
                                    _aiDelayTimer = _aiDidSomethingThisTurn ? 0.5f : 0f;
                                    break;
                            }
                        }
                        else
                        {
                            // No player or cannot act, just end turn
                            _combatManager.NextTurn();
                            _currentAiPhase = AiPhase.Start;
                        }
                    }
                }
                    
                    // Check if combat ended
                    if (!_combatManager.InCombat)
                    {
                        AddToCombatLog(Loc.Tr("Combat ended!"));
                        _selectedAction = CombatAction.Move;
                        if (_playerCreature != null && _currentCharacter != null)
                        {
                            _playerCreature.UpdateCharacter(_currentCharacter);
                            SaveCharacters();
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
        // Keep the character sheet data in sync with the in-game creature state.
        // Persistence still happens at combat end, but this prevents temporary HP mismatches in UI.
        if (_playerCreature != null && _currentCharacter != null)
        {
            _playerCreature.UpdateCharacter(_currentCharacter);
        }

        UpdateHPTooltips();

        // Advance game time and update torches (AppState.Playing)
        if (_state == AppState.Playing && _currentCampaign != null)
        {
            // 1. Advance game time if nothing else is advancing it
            // (Travel and Map-Waiting advance it themselves)
            bool isTraveling = _currentCampaign.IsTraveling;
            bool isAnyMemberBusy = _characters.Exists(c => _currentCampaign.PartyMembers.Contains(c.Name) && c.CurrentDonDoffProcess is { IsActive: true });
            bool isWaitingOnMap = _showCampaignMap && isAnyMemberBusy;

            if (!isTraveling && !isWaitingOnMap)
            {
                _currentCampaign.TotalGameMinutes += totalSeconds / 60.0;
            }

            // 2. Unified torch duration update based on TotalGameMinutes delta
            double deltaMinutes = _currentCampaign.TotalGameMinutes - _lastTotalGameMinutes;
            if (deltaMinutes > 0)
            {
                _torchTimerAccumulator += deltaMinutes;
                if (_torchTimerAccumulator >= 1.0)
                {
                    int minutesToConsume = (int)Math.Floor(_torchTimerAccumulator);
                    UpdateTorchDurations(minutesToConsume);
                    _torchTimerAccumulator -= minutesToConsume;
                }
            }
            _lastTotalGameMinutes = _currentCampaign.TotalGameMinutes;
        }

        _prevKb = kb;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void CloseCampaignMap()
    {
        if (!_showCampaignMap)
            return;

        SyncZoomInputStateHandoff(Mouse.GetState());
        _showCampaignMap = false;
        PlayCampaignMapCloseSfx();

        if (_currentCampaign.TravelOccurred || _currentCampaign.PartyPositionChanged || _campaignMapViewer.FloorChanged)
        {
            _tacticalMap.Clear();
            _urbanDoorStates.Clear();
            PlacePlayerAtNearestValidTile();
            _playerCreature.InterruptMovement();
            _cameraTarget = Vector3.Zero;

            // Pre-warm the tile cache for the visible area so the first render frame
            // does not stall generating thousands of procedural tiles on-demand.
            int warmCenterX = _playerCreature?.X ?? 0;
            int warmCenterY = _playerCreature?.Y ?? 0;
            _tacticalMap.PreWarm(warmCenterX, warmCenterY, 0, 40);

            UpdateVision();
            _campaignMapViewer.ResetTravelFlags(_currentCampaign);
        }
    }

    private void OpenCampaignMap()
    {
        if (_showCampaignMap)
            return;

        _showCampaignMap = true;
        SyncZoomInputStateHandoff(Mouse.GetState());
        PlayCampaignMapOpenSfx();
    }

    private void SyncZoomInputStateHandoff(MouseState mouse)
    {
        _prevScrollValue = mouse.ScrollWheelValue;
        _campaignMapViewer.ResetInputBaseline(mouse);
    }

    private void InitializeCampaignMapAudio()
    {
        _mapSfxSynth = new PaperMapSfxSynth();
    }

    private void PlayCampaignMapOpenSfx()
    {
        _mapSfxSynth.PlayOpen();
    }

    private void PlayCampaignMapCloseSfx()
    {
        _mapSfxSynth.PlayClose();
    }


}
