using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DND;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private InfiniteGrid3D<TileType> _grid = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private HashSet<char> _supportedChars = new();

    private InputManager _input = new();
    private SaveManager _saveManager = new();
    private WorldRenderer _worldRenderer = null!;
    private MainMenuUI _mainMenuUI = null!;
    private CombatUI _combatUI = null!;

    private enum AppState { MainMenu, CharacterSelect, CharacterCreate, CampaignSelect, CampaignCreate, Playing }
    private AppState _state = AppState.MainMenu;

    private List<Character> _characters = new();
    private int _characterIndex = 0;
    private Character? _currentCharacter = null;
    private Campaign? _currentCampaign = null;
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
    private bool _showCharacterSheet = false;

    private Vector3 _cameraTarget = Vector3.Zero;
    private float _cameraYaw = MathHelper.ToRadians(45f);
    private float _cameraPitch = MathHelper.ToRadians(35f);
    private float _cameraDistance = 20f;
    private float _targetYaw = MathHelper.ToRadians(45f);
    private const float RotationSpeed = 5f;

    private int _currentViewLevel = 0;

    private CombatManager _combatManager = new();
    private Creature? _playerCreature = null;
    private List<string> _combatLog = new();
    private const int MAX_COMBAT_LOG = 5;
    
    private VisionSystem _visionSystem = new();
    private bool _showVisionOverlay = true;
    private bool _visionNeedsUpdate = false;
    
    private CombatAction _selectedAction = CombatAction.None;
    private bool _showCombatUI = false;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        try { _font = Content.Load<SpriteFont>("DefaultFont"); _supportedChars = new HashSet<char>(_font.Characters); }
        catch { System.Console.WriteLine("Warning: DefaultFont not found."); }

        _characterCreation = new CharacterCreation(_font, _pixel);
        _characterSheet = new CharacterSheet(_font, _pixel);
        _campaignCreation = new CampaignCreation(_font, _pixel);
        _campaignMapViewer = new CampaignMapViewer(_font, _pixel);

        _worldRenderer = new WorldRenderer(GraphicsDevice);
        _mainMenuUI = new MainMenuUI(_font, _pixel);
        _combatUI = new CombatUI(_font, _pixel, _supportedChars);

        _combatManager.Grid = _grid;
        _visionSystem.Grid = _grid;
        _visionSystem.GlobalDaylight = true;

        InitializeWorld();
        _characters = _saveManager.LoadCharacters();
    }

    private void InitializeWorld()
    {
        for (int x = -10; x <= 10; x++) for (int y = -10; y <= 10; y++) _grid.Set(x, y, 0, TileType.Grass);
        for (int i = -5; i <= 5; i++) if (i != 0) { _grid.Set(i, 3, 0, TileType.Wall); _grid.Set(i, 3, 1, TileType.Wall); }
        for (int z = 1; z <= 3; z++) for (int i = -3; i <= 3; i++) { _grid.Set(i, i, z, TileType.Floor); _grid.Set(-i, i, z, TileType.Floor); }
        SpawnTestEnemies();
    }
    
    private void SpawnTestEnemies()
    {
        var rand = new Random();
        for (int i = 0; i < rand.Next(3, 6); i++)
        {
            int ex, ey, ez; Creature? enemy = null; bool valid = false; int attempts = 0;
            while (!valid && attempts++ < 100)
            {
                ex = rand.Next(-10, 11); ey = rand.Next(-10, 11); ez = rand.Next(0, 4);
                if ((ex == 0 && ey == 0 && ez == 0) || _grid.Get(ex, ey, ez) == TileType.Wall || _combatManager.GetCreatureAt(ex, ey, ez) != null) continue;
                enemy = (rand.Next(0, 7)) switch { 0=>Creature.CreateGoblin(ex,ey,ez), 1=>Creature.CreateOrc(ex,ey,ez), 2=>Creature.CreateSkeleton(ex,ey,ez), 3=>Creature.CreateWolf(ex,ey,ez), 4=>Creature.CreateKobold(ex,ey,ez), 5=>Creature.CreateUmberHulk(ex,ey,ez), _=>Creature.CreateCouatl(ex,ey,ez) };
                if (enemy.CanFly) { valid = true; if (ez > 0) enemy.IsFlying = true; }
                else if (ez == 0 && _grid.Get(ex, ey, 0) != TileType.Wall) valid = true;
            }
            if (enemy != null && valid) _combatManager.Combatants.Add(enemy);
        }
    }
    
    private void StartCombat()
    {
        if (_currentCharacter == null || _playerCreature == null) return;
        var list = new List<Creature> { _playerCreature };
        list.AddRange(_combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive()));
        _combatManager.StartCombat(list);
        _showCombatUI = true; _selectedAction = CombatAction.None;
        AddToCombatLog("Combat started!"); UpdateVision();
    }
    
    private void UpdateVision() => _visionNeedsUpdate = true;
    
    private void RecalculateVision()
    {
        if (_playerCreature == null) return;
        foreach (var l in _visionSystem._lightSources) if (l.AttachedTo != null) { l.X = l.AttachedTo.X; l.Y = l.AttachedTo.Y; l.Z = l.AttachedTo.Z; }
        _visionSystem.CalculateLighting(); _visionSystem.CalculateVisibility(_playerCreature);
        _visionNeedsUpdate = false;
    }
    
    private void AddToCombatLog(string msg) { _combatLog.Add(msg); if (_combatLog.Count > MAX_COMBAT_LOG) _combatLog.RemoveAt(0); }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        if (_state == AppState.MainMenu) { var a = _mainMenuUI.Update(_input, GraphicsDevice.Viewport); if (a != null) ExecuteMainMenuAction(a); base.Update(gameTime); return; }
        if (_state == AppState.CharacterSelect) { UpdateCharacterSelect(); base.Update(gameTime); return; }
        if (_state == AppState.CharacterCreate) { UpdateCharacterCreate(gameTime); base.Update(gameTime); return; }
        if (_state == AppState.CampaignSelect) { UpdateCampaignSelect(); base.Update(gameTime); return; }
        if (_state == AppState.CampaignCreate) { UpdateCampaignCreate(gameTime); base.Update(gameTime); return; }

        if (_showCampaignMap && _state == AppState.Playing) { _campaignMapViewer.Update(_currentCampaign, _input.CurrentMouseState, _input.CurrentKeyboardState, _input.PreviousKeyboardState); if (_input.IsKeyPressed(Keys.M)) _showCampaignMap = false; base.Update(gameTime); return; }
        if (_input.IsKeyPressed(Keys.Escape)) { _isMenuOpen = !_isMenuOpen; if (_isMenuOpen) _menuIndex = 0; }
        if (_isMenuOpen) { UpdatePauseMenu(); base.Update(gameTime); return; }

        if (_state == AppState.Playing) UpdatePlaying(gameTime);
        UpdateCameraControls(gameTime);
        if (_visionNeedsUpdate) RecalculateVision();
        base.Update(gameTime);
    }

    private void ExecuteMainMenuAction(string a)
    {
        if (a == "Desktop") Exit();
        else if (a == "Options") { _isMenuOpen = true; _menuIndex = 1; }
        else { _isMultiplayerMode = (a == "Multiplayer"); _characters = _saveManager.LoadCharacters(); _state = AppState.CharacterSelect; }
    }

    private void UpdateCharacterSelect()
    {
        if (_input.IsKeyPressed(Keys.Escape)) { _state = AppState.MainMenu; return; }
        if (_input.IsKeyPressed(Keys.Up)) _characterIndex = Math.Max(0, _characterIndex - 1);
        if (_input.IsKeyPressed(Keys.Down)) _characterIndex = Math.Min(_characters.Count, _characterIndex + 1);
        if (_input.IsKeyPressed(Keys.Delete) && _characterIndex < _characters.Count) { _characters.RemoveAt(_characterIndex); _saveManager.SaveCharacters(_characters); _characterIndex = Math.Max(0, _characters.Count - 1); }
        if (_input.IsKeyPressed(Keys.Enter)) HandleCharSel(_characterIndex);
        UpdateCharSelectMouse();
    }

    private void HandleCharSel(int i)
    {
        if (i < _characters.Count) { _currentCharacter = _characters[i]; if (_isMultiplayerMode) { InitPlayer(); _state = AppState.Playing; UpdateVision(); } else { _campaigns = _saveManager.LoadCampaigns(); _campaignIndex = 0; _state = AppState.CampaignSelect; } }
        else { _characterCreation.Reset(); _state = AppState.CharacterCreate; }
    }

    private void InitPlayer() { if (_currentCharacter != null && _playerCreature == null) { _playerCreature = Creature.FromCharacter(_currentCharacter, 0, 0); _combatManager.Combatants.Clear(); SpawnTestEnemies(); } }

    private void UpdateCharacterCreate(GameTime gt)
    {
        if (!_characterCreation.Update(gt, GraphicsDevice, _input.CurrentKeyboardState, _input.PreviousKeyboardState, out Character? c)) _state = AppState.CharacterSelect;
        else if (c != null) { _characters.Add(c); _saveManager.SaveCharacters(_characters); _currentCharacter = c; HandleCharSel(_characters.Count - 1); }
    }

    private void UpdateCampaignSelect()
    {
        if (_input.IsKeyPressed(Keys.Escape)) { _state = AppState.CharacterSelect; return; }
        if (_input.IsKeyPressed(Keys.Up)) _campaignIndex = Math.Max(0, _campaignIndex - 1);
        if (_input.IsKeyPressed(Keys.Down)) _campaignIndex = Math.Min(_campaigns.Count, _campaignIndex + 1);
        if (_input.IsKeyPressed(Keys.Delete) && _campaignIndex < _campaigns.Count) { _campaigns.RemoveAt(_campaignIndex); _saveManager.SaveCampaigns(_campaigns); _campaignIndex = Math.Max(0, _campaigns.Count - 1); }
        if (_input.IsKeyPressed(Keys.Enter)) HandleCampSel(_campaignIndex);
        UpdateCampSelectMouse();
    }

    private void HandleCampSel(int i)
    {
        if (i < _campaigns.Count) { _currentCampaign = _campaigns[i]; if (_currentCharacter != null && !_currentCampaign.PartyMembers.Contains(_currentCharacter.Name)) { _currentCampaign.PartyMembers.Add(_currentCharacter.Name); _saveManager.SaveCampaigns(_campaigns); } InitPlayer(); _state = AppState.Playing; UpdateVision(); }
        else { _campaignCreation.Reset(); _state = AppState.CampaignCreate; }
    }

    private void UpdateCampaignCreate(GameTime gt)
    {
        if (!_campaignCreation.Update(gt, GraphicsDevice, _input.CurrentKeyboardState, _input.PreviousKeyboardState, out Campaign? c)) _state = AppState.CampaignSelect;
        else if (c != null) { _currentCampaign = c; if (_currentCharacter != null) c.PartyMembers.Add(_currentCharacter.Name); _saveManager.SaveCampaign(c); InitPlayer(); _state = AppState.Playing; UpdateVision(); }
    }

    private void UpdatePlaying(GameTime gt)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;
        _playerCreature?.UpdateMovementAnimation(dt);
        foreach (var c in _combatManager.Combatants) if (c.IsAlive()) c.UpdateMovementAnimation(dt);
        if (_input.IsKeyPressed(Keys.C)) { _showCharacterSheet = !_showCharacterSheet; if (_showCharacterSheet) _characterSheet.ResetScroll(); }
        if (_input.IsKeyPressed(Keys.M)) _showCampaignMap = !_showCampaignMap;
        if (_input.IsKeyPressed(Keys.PageUp)) { _currentViewLevel++; AddToCombatLog($"Viewing level {_currentViewLevel}"); }
        if (_input.IsKeyPressed(Keys.PageDown)) { _currentViewLevel = Math.Max(0, _currentViewLevel - 1); AddToCombatLog($"Viewing level {_currentViewLevel}"); }
        UpdateTestKeys();
        if (_input.IsKeyPressed(Keys.Tab)) { if (!_combatManager.InCombat) StartCombat(); else _showCombatUI = !_showCombatUI; }
        if (_showCharacterSheet) { _characterSheet.Update(_input.CurrentMouseState); return; }
        if (!_combatManager.InCombat && _input.IsLeftClick())
        {
            var h = GetHoveredTile();
            if (h.HasValue && _playerCreature != null) { var p = _combatManager.GetPath(_playerCreature, h.Value.x, h.Value.y, _currentViewLevel); if (p != null) { foreach (var s in p.Skip(1)) _playerCreature.MoveTo(s.x, s.y, s.z); UpdateVision(); } }
        }
        if (_combatManager.InCombat && _showCombatUI) UpdateCombat();
    }

    private void UpdateCombat()
    {
        var cur = _combatManager.CurrentCombatant; if (cur == null) return;
        if (cur.IsPlayer)
        {
            if (_input.IsKeyPressed(Keys.D1)) _selectedAction = CombatAction.Move;
            if (_input.IsKeyPressed(Keys.D2)) _selectedAction = CombatAction.Attack;
            if (_input.IsKeyPressed(Keys.D3)) { _combatManager.NextTurn(); _selectedAction = CombatAction.None; }
            if (_input.IsLeftClick())
            {
                var h = GetHoveredTile(); if (!h.HasValue) return;
                if (_selectedAction == CombatAction.Attack) { var t = _combatManager.GetCreatureAt(h.Value.x, h.Value.y, _currentViewLevel); if (t != null && !t.IsPlayer && cur.HasAction) { AddToCombatLog(_combatManager.MakeAttack(cur, t, _visionSystem).GetMessage()); _selectedAction = CombatAction.None; CheckCombatEnd(); } }
                else if (_selectedAction == CombatAction.Move && _combatManager.CanMove(cur, h.Value.x, h.Value.y, _currentViewLevel)) { _combatManager.Move(cur, h.Value.x, h.Value.y, _currentViewLevel); _selectedAction = CombatAction.None; UpdateVision(); }
            }
        }
        else UpdateAI(cur);
    }

    private void UpdateAI(Creature cur)
    {
        if (cur.HasAction || cur.MovementRemaining > 0)
        {
            var p = _combatManager.Combatants.FirstOrDefault(c => c.IsPlayer);
            if (p != null)
            {
                if (_combatManager.IsInMeleeRange(cur, p) && cur.HasAction) AddToCombatLog(_combatManager.MakeAttack(cur, p, _visionSystem).GetMessage());
                else if (cur.MovementRemaining > 0) { var s = _combatManager.GetNextStepTowards(cur, p); if (s.HasValue && _combatManager.CanMove(cur, s.Value.x, s.Value.y, s.Value.z)) { _combatManager.Move(cur, s.Value.x, s.Value.y, s.Value.z); UpdateVision(); } else _combatManager.NextTurn(); }
                else _combatManager.NextTurn();
            }
        }
        else _combatManager.NextTurn();
        CheckCombatEnd();
    }

    private void CheckCombatEnd() { if (!_combatManager.InCombat) { _showCombatUI = false; if (_playerCreature != null && _currentCharacter != null) { _playerCreature.UpdateCharacter(_currentCharacter); _saveManager.SaveCharacters(_characters); } } }

    private void UpdateCameraControls(GameTime gt)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;
        if (_input.IsKeyPressed(Keys.Q)) _targetYaw -= MathHelper.ToRadians(45f);
        if (_input.IsKeyPressed(Keys.E)) _targetYaw += MathHelper.ToRadians(45f);
        _cameraYaw = MathHelper.Lerp(_cameraYaw, _targetYaw, RotationSpeed * dt);
        Vector3 m = Vector3.Zero;
        if (_input.IsKeyDown(Keys.W) || _input.IsKeyDown(Keys.Up)) m.Y -= 1; if (_input.IsKeyDown(Keys.S) || _input.IsKeyDown(Keys.Down)) m.Y += 1; if (_input.IsKeyDown(Keys.A) || _input.IsKeyDown(Keys.Left)) m.X += 1; if (_input.IsKeyDown(Keys.D) || _input.IsKeyDown(Keys.Right)) m.X -= 1;
        if (m != Vector3.Zero) { m.Normalize(); _cameraTarget += Vector3.Transform(m, Matrix.CreateRotationZ(_cameraYaw)) * 10f * dt; }
        if (_input.ScrollDelta != 0) _cameraDistance = MathHelper.Clamp(_cameraDistance - _input.ScrollDelta * 0.01f, 5f, 50f);
    }

    private (int x, int y)? GetHoveredTile()
    {
        var m = _input.MousePosition;
        var n = GraphicsDevice.Viewport.Unproject(new Vector3(m.X, m.Y, 0f), _worldRenderer.Effect.Projection, _worldRenderer.Effect.View, Matrix.Identity);
        var f = GraphicsDevice.Viewport.Unproject(new Vector3(m.X, m.Y, 1f), _worldRenderer.Effect.Projection, _worldRenderer.Effect.View, Matrix.Identity);
        var d = Vector3.Normalize(f - n); var r = new Ray(n, d); var p = new Plane(Vector3.UnitZ, -_currentViewLevel);
        float? dist = r.Intersects(p); return dist.HasValue ? ((int)Math.Round(n.X + d.X * dist.Value), (int)Math.Round(n.Y + d.Y * dist.Value)) : null;
    }

    protected override void Draw(GameTime gt)
    {
        GraphicsDevice.Clear(Color.Black);
        _worldRenderer.UpdateCamera(_cameraTarget, _cameraYaw, _cameraPitch, _cameraDistance);
        int? hx = null, hy = null;
        if (_state == AppState.Playing && !_showCharacterSheet && !_showCampaignMap)
        {
            GraphicsDevice.RasterizerState = RasterizerState.CullNone; GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            _worldRenderer.Draw3DGrid(_grid, _currentViewLevel, _showVisionOverlay, _visionSystem, _playerCreature);
            DrawOutlines(); DrawCreatures();
            var h = GetHoveredTile(); if (h.HasValue) { hx = h.Value.x; hy = h.Value.y; _worldRenderer.Draw3DTile(hx.Value, hy.Value, _currentViewLevel, Color.Yellow * 0.5f); }
            _worldRenderer.Draw3DLine(Vector3.Zero, new Vector3(5,0,0), Color.Red); _worldRenderer.Draw3DLine(Vector3.Zero, new Vector3(0,5,0), Color.Lime); _worldRenderer.Draw3DLine(Vector3.Zero, new Vector3(0,0,5), Color.Blue);
        }
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_state == AppState.MainMenu) _mainMenuUI.Draw(_spriteBatch, GraphicsDevice.Viewport);
        else if (_state == AppState.CharacterSelect) DrawCharSel();
        else if (_state == AppState.CharacterCreate) _characterCreation.Draw(gt, _spriteBatch, GraphicsDevice);
        else if (_state == AppState.CampaignSelect) DrawCampSel();
        else if (_state == AppState.CampaignCreate) _campaignCreation.Draw(gt, _spriteBatch, GraphicsDevice);
        else if (_state == AppState.Playing)
        {
            if (_showCharacterSheet) _characterSheet.Draw(_spriteBatch, GraphicsDevice, _currentCharacter!, _currentCampaign);
            else if (_showCampaignMap) _campaignMapViewer.Draw(_spriteBatch, GraphicsDevice, _currentCampaign!);
            else { DrawUIs(); if (_combatManager.InCombat && _showVisionOverlay) DrawEffects(); if (_showCombatUI && _combatManager.InCombat) _combatUI.Draw(_spriteBatch, GraphicsDevice.Viewport, _combatManager, _combatLog, _showVisionOverlay, _playerCreature, _currentViewLevel, _selectedAction); if (hx.HasValue && hy.HasValue && _combatManager.InCombat && _showVisionOverlay) DrawTooltip(hx.Value, hy.Value); }
        }
        if (_isMenuOpen) DrawPause();
        _spriteBatch.End();
        base.Draw(gt);
    }

    private void DrawOutlines()
    {
        var list = _combatManager.InCombat ? _combatManager.Combatants.Where(c=>c.IsAlive()) : (_playerCreature == null ? Enumerable.Empty<Creature>() : new[]{_playerCreature}.Concat(_combatManager.Combatants.Where(c=>!c.IsPlayer && c.IsAlive())));
        foreach (var c in list)
        {
            if (c.Z > _currentViewLevel || (_combatManager.InCombat && _showVisionOverlay && !_visionSystem.IsVisible(c.X, c.Y, c.Z))) continue;
            Color col = c.IsPlayer ? Color.LimeGreen : (c.Alignment is Alignment.LawfulEvil or Alignment.NeutralEvil or Alignment.ChaoticEvil ? Color.Red : Color.DeepSkyBlue);
            if (_showVisionOverlay && _playerCreature != null) { Color t = _visionSystem.GetFogOfWarTint(c.X, c.Y, c.Z, true, _playerCreature); col = new Color((byte)(col.R*t.R/255), (byte)(col.G*t.G/255), (byte)(col.B*t.B/255)); }
            _worldRenderer.Draw3DTileOutline(c.X, c.Y, c.Z, col);
        }
    }

    private void DrawCreatures() { if (_combatManager.InCombat) foreach (var c in _combatManager.Combatants) { if (c.IsAlive()) _worldRenderer.Draw3DCreature(c, _currentViewLevel, true, _showVisionOverlay, _visionSystem, _playerCreature); } else { if (_playerCreature!=null) _worldRenderer.Draw3DCreature(_playerCreature, _currentViewLevel, false, _showVisionOverlay, _visionSystem, _playerCreature); foreach (var c in _combatManager.Combatants.Where(c=>!c.IsPlayer && c.IsAlive())) _worldRenderer.Draw3DCreature(c, _currentViewLevel, false, _showVisionOverlay, _visionSystem, _playerCreature); } }
    private void DrawUIs() { var list = _combatManager.InCombat ? _combatManager.Combatants.Where(c=>c.IsAlive()) : (_playerCreature == null ? Enumerable.Empty<Creature>() : new[]{_playerCreature}.Concat(_combatManager.Combatants.Where(c=>!c.IsPlayer && c.IsAlive()))); foreach (var c in list) DrawUnitUI(c); }
    private void DrawUnitUI(Creature c)
    {
        if (c.Z > _currentViewLevel || _font == null || (_combatManager.InCombat && _showVisionOverlay && !_visionSystem.IsVisible(c.X, c.Y, c.Z))) return;
        Vector3 s = GraphicsDevice.Viewport.Project(new Vector3(c.VisualX, c.VisualY, WorldRenderer.GetCreatureVisualTopZ(c) + 0.2f), _worldRenderer.Effect.Projection, _worldRenderer.Effect.View, Matrix.Identity);
        if (s.Z < 0 || s.Z > 1) return;
        Vector2 p = new(s.X, s.Y); _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 30, (int)p.Y - 20, 60, 6), Color.DarkRed); _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 30, (int)p.Y - 20, (int)(60 * (float)c.CurrentHP / c.MaxHP), 6), Color.Green);
        string n = SafeString($"{c.Name} [Z{c.Z}]"); Vector2 sz = _font.MeasureString(n) * 0.6f; _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - (int)sz.X / 2 - 4, (int)p.Y - 40, (int)sz.X + 8, (int)sz.Y + 4), Color.Black * 0.6f); _spriteBatch.DrawString(_font, n, new Vector2(p.X - sz.X / 2, p.Y - 38), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
    }

    private void DrawEffects()
    {
        foreach (var s in _visionSystem._lightSources) if (s.IsActive) { Vector3 p = GraphicsDevice.Viewport.Project(new Vector3(s.X, s.Y, s.Z), _worldRenderer.Effect.Projection, _worldRenderer.Effect.View, Matrix.Identity); if (p.Z >= 0 && p.Z <= 1) _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 5, (int)p.Y - 5, 10, 10), s.LightColor * 0.8f); }
        foreach (var e in _visionSystem._areaEffects) { Vector3 p = GraphicsDevice.Viewport.Project(new Vector3(e.X, e.Y, e.Z), _worldRenderer.Effect.Projection, _worldRenderer.Effect.View, Matrix.Identity); if (p.Z >= 0 && p.Z <= 1) { Color col = e.EffectType == LightType.Darkness ? Color.Purple * 0.5f : Color.White * 0.5f; _spriteBatch.Draw(_pixel, new Rectangle((int)p.X - 10, (int)p.Y - 1, 20, 2), col); } }
    }

    private void DrawTooltip(int tx, int ty)
    {
        string t = SafeString($"({tx}, {ty}, Z{_currentViewLevel}) {_grid.Get(tx, ty, _currentViewLevel)} | Light: {_visionSystem.GetLightLevel(tx, ty, _currentViewLevel)}");
        var c = _combatManager.GetCreatureAt(tx, ty, _currentViewLevel); if (c != null && _visionSystem.IsVisible(tx, ty, _currentViewLevel)) t += SafeString($" | {c.Name}");
        Vector2 sz = _font.MeasureString(t), m = _input.MousePosition.ToVector2() + new Vector2(15, 15); _spriteBatch.Draw(_pixel, new Rectangle((int)m.X-5, (int)m.Y-3, (int)sz.X+10, (int)sz.Y+6), Color.Black * 0.9f); _spriteBatch.DrawString(_font, t, m, Color.White);
    }

    private void UpdateCharSelectMouse() { var vp = GraphicsDevice.Viewport; int mw=480, ih=48, p=12, th=80; int mh = th + Math.Max(1, _characters.Count + 1) * (ih + p) + p; var mr = new Rectangle((vp.Width - mw) / 2, (vp.Height - mh) / 2, mw, mh); if (new Rectangle(mr.X + p, mr.Y + p, 80, 30).Contains(_input.MousePosition) && _input.IsLeftClick()) _state = AppState.MainMenu; for (int i = 0; i < _characters.Count; i++) { var r = new Rectangle(mr.X + p, mr.Y + th + p + i * (ih + p), mw - p * 2, ih); if (new Rectangle(r.X + r.Width - 70, r.Y + (r.Height - 30) / 2, 60, 30).Contains(_input.MousePosition) && _input.IsLeftClick()) { _characters.RemoveAt(i); _saveManager.SaveCharacters(_characters); _characterIndex = Math.Max(0, _characters.Count - 1); return; } if (r.Contains(_input.MousePosition)) { _characterIndex = i; if (_input.IsLeftClick()) HandleCharSel(i); } } var nr = new Rectangle(mr.X + p, mr.Y + th + p + _characters.Count * (ih + p), mw - p * 2, ih); if (nr.Contains(_input.MousePosition)) { _characterIndex = _characters.Count; if (_input.IsLeftClick()) HandleCharSel(_characters.Count); } }
    private void UpdateCampSelectMouse() { var vp = GraphicsDevice.Viewport; int mw=480, ih=48, p=12, th=80; int mh = th + Math.Max(1, _campaigns.Count + 1) * (ih + p) + p + 40; var mr = new Rectangle((vp.Width - mw) / 2, (vp.Height - mh) / 2, mw, mh); if (new Rectangle(mr.X + p, mr.Y + p, 80, 30).Contains(_input.MousePosition) && _input.IsLeftClick()) _state = AppState.CharacterSelect; for (int i = 0; i < _campaigns.Count; i++) { var r = new Rectangle(mr.X + p, mr.Y + th + p + i * (ih + p), mw - p * 2, ih); if (new Rectangle(r.X + r.Width - 70, r.Y + (r.Height - 30) / 2, 60, 30).Contains(_input.MousePosition) && _input.IsLeftClick()) { _campaigns.RemoveAt(i); _saveManager.SaveCampaigns(_campaigns); _campaignIndex = Math.Max(0, _campaigns.Count - 1); return; } if (r.Contains(_input.MousePosition)) { _campaignIndex = i; if (_input.IsLeftClick()) HandleCampSel(i); } } var nr = new Rectangle(mr.X + p, mr.Y + th + p + _campaigns.Count * (ih + p), mw - p * 2, ih); if (nr.Contains(_input.MousePosition)) { _campaignIndex = _campaigns.Count; if (_input.IsLeftClick()) HandleCampSel(_campaigns.Count); } }
    private void UpdatePauseMenu() { var vp = GraphicsDevice.Viewport; int mw=360, ih=48, p=12; int mh = _menuItems.Length * (ih + p) + p; var mr = new Rectangle((vp.Width - mw) / 2, (vp.Height - mh) / 2, mw, mh); for (int i = 0; i < _menuItems.Length; i++) { var r = new Rectangle(mr.X + p, mr.Y + p + i * (ih + p), mw - p * 2, ih); if (r.Contains(_input.MousePosition)) { _menuIndex = i; if (_input.IsLeftClick()) ExecuteMenuAction(i); } } }

    private void UpdateTestKeys()
    {
        if (_currentCharacter != null) { if (_input.IsKeyPressed(Keys.X)) AddToCombatLog(_currentCharacter.MakeAbilityCheck("Strength", DndMath.DifficultyClass.Medium).GetSimpleMessage()); if (_input.IsKeyPressed(Keys.Z)) AddToCombatLog(_currentCharacter.MakeSkillCheck("Stealth", DndMath.DifficultyClass.Hard).GetSimpleMessage()); if (_input.IsKeyPressed(Keys.N)) AddToCombatLog(_currentCharacter.MakeSavingThrow("Dexterity", DndMath.DifficultyClass.Hard).GetSimpleMessage()); }
        if (_playerCreature != null) {
            if (_input.IsKeyPressed(Keys.Space) && _playerCreature.CanFly) { _playerCreature.IsFlying = !_playerCreature.IsFlying; AddToCombatLog(_playerCreature.IsFlying ? "Flying" : "Landed"); }
            if (_input.IsKeyPressed(Keys.R) && _playerCreature.IsFlying) { _playerCreature.MoveTo(_playerCreature.X, _playerCreature.Y, _playerCreature.Z + 1); UpdateVision(); }
            if (_input.IsKeyPressed(Keys.T) && _playerCreature.IsFlying) { _playerCreature.MoveTo(_playerCreature.X, _playerCreature.Y, Math.Max(0, _playerCreature.Z - 1)); UpdateVision(); }
            if (_input.IsKeyPressed(Keys.B)) { if (_playerCreature.Conditions.HasCondition(Condition.Blinded)) _playerCreature.Conditions = _playerCreature.Conditions.RemoveCondition(Condition.Blinded); else _playerCreature.Conditions = _playerCreature.Conditions.AddCondition(Condition.Blinded); UpdateVision(); }
            if (_input.IsKeyPressed(Keys.F)) { _visionSystem.AddAreaEffect(AreaEffect.FogCloud(_playerCreature.X, _playerCreature.Y, _playerCreature.Z)); UpdateVision(); }
            if (_input.IsKeyPressed(Keys.K)) { _visionSystem.AddAreaEffect(AreaEffect.Darkness(_playerCreature.X, _playerCreature.Y, _playerCreature.Z)); UpdateVision(); }
        }
        if (_input.IsKeyPressed(Keys.V)) _showVisionOverlay = !_showVisionOverlay;
    }

    private void DrawCharSel()
    {
        var vp = GraphicsDevice.Viewport; _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);
        int mw = 480, ih = 48, p = 12, th = 80; int count = Math.Max(1, _characters.Count + 1); int mh = th + count * (ih + p) + p;
        var mr = new Rectangle((vp.Width - mw) / 2, (vp.Height - mh) / 2, mw, mh); _spriteBatch.Draw(_pixel, mr, Color.DarkSlateGray * 0.95f);
        if (_font != null) { _spriteBatch.DrawString(_font, _isMultiplayerMode ? "Choose Character (MP)" : "Choose Character (SP)", new Vector2(mr.X + 20, mr.Y + 12), Color.White); var br = new Rectangle(mr.X + p, mr.Y + p, 80, 30); _spriteBatch.Draw(_pixel, br, Color.Gray); _spriteBatch.DrawString(_font, "< Back", new Vector2(br.X + 5, br.Y + 5), Color.White); }
        for (int i = 0; i < count; i++) {
            var r = new Rectangle(mr.X + p, mr.Y + th + p + i * (ih + p), mw - p * 2, ih); _spriteBatch.Draw(_pixel, r, (i == _characterIndex) ? Color.LightGray : Color.Gray);
            if (_font != null) { string name = SafeString((i < _characters.Count) ? _characters[i].Name : "Create New"); _spriteBatch.DrawString(_font, name, new Vector2(r.X + 12, r.Y + 10), (i == _characterIndex) ? Color.Black : Color.White); if (i < _characters.Count) { var dr = new Rectangle(r.X + r.Width - 70, r.Y + (r.Height - 30) / 2, 60, 30); _spriteBatch.Draw(_pixel, dr, Color.DarkRed * 0.7f); _spriteBatch.DrawString(_font, "Del", new Vector2(dr.X + 5, dr.Y + 5), Color.White, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0); } }
        }
    }

    private void DrawCampSel()
    {
        var vp = GraphicsDevice.Viewport; _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);
        int mw = 480, ih = 48, p = 12, th = 80; int count = Math.Max(1, _campaigns.Count + 1); int mh = th + count * (ih + p) + p + 40;
        var mr = new Rectangle((vp.Width - mw) / 2, (vp.Height - mh) / 2, mw, mh); _spriteBatch.Draw(_pixel, mr, Color.DarkSlateGray * 0.95f);
        if (_font != null) { _spriteBatch.DrawString(_font, "Select a Campaign", new Vector2(mr.X + 20, mr.Y + 12), Color.White); var br = new Rectangle(mr.X + p, mr.Y + p, 80, 30); _spriteBatch.Draw(_pixel, br, Color.Gray); _spriteBatch.DrawString(_font, "< Back", new Vector2(br.X + 5, br.Y + 5), Color.White); }
        for (int i = 0; i < count; i++) {
            var r = new Rectangle(mr.X + p, mr.Y + th + p + i * (ih + p), mw - p * 2, ih); _spriteBatch.Draw(_pixel, r, (i == _campaignIndex) ? Color.LightGray : Color.Gray);
            if (_font != null) { string name = SafeString((i < _campaigns.Count) ? _campaigns[i].Name : "Create New"); _spriteBatch.DrawString(_font, name, new Vector2(r.X + 12, r.Y + 10), (i == _campaignIndex) ? Color.Black : Color.White); if (i < _campaigns.Count) { var dr = new Rectangle(r.X + r.Width - 70, r.Y + (r.Height - 30) / 2, 60, 30); _spriteBatch.Draw(_pixel, dr, Color.DarkRed * 0.7f); _spriteBatch.DrawString(_font, "Del", new Vector2(dr.X + 5, dr.Y + 5), Color.White, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0); } }
        }
        if (_campaignIndex < _campaigns.Count) DrawCampaignSummary(_campaigns[_campaignIndex]);
    }

    private void DrawPause()
    {
        var vp = GraphicsDevice.Viewport; _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.6f);
        int mw = 360, ih = 48, p = 12; int mh = _menuItems.Length * (ih + p) + p;
        var mr = new Rectangle((vp.Width - mw) / 2, (vp.Height - mh) / 2, mw, mh); _spriteBatch.Draw(_pixel, mr, Color.DarkSlateGray * 0.95f);
        for (int i = 0; i < _menuItems.Length; i++) {
            var r = new Rectangle(mr.X + p, mr.Y + p + i * (ih + p), mw - p * 2, ih); _spriteBatch.Draw(_pixel, r, (i == _menuIndex) ? Color.LightGray : Color.Gray);
            if (_font != null) _spriteBatch.DrawString(_font, _menuItems[i], new Vector2(r.X + 32, r.Y + 10), (i == _menuIndex) ? Color.Black : Color.White);
        }
    }

    private void DrawCampaignSummary(Campaign sel)
    {
        var vp = GraphicsDevice.Viewport; int sw = 400, sh = vp.Height - 100; var r = new Rectangle(vp.Width / 2 + 100, 50, sw, sh);
        _spriteBatch.Draw(_pixel, r, Color.DarkSlateGray * 0.9f); int sy = r.Y + 20;
        if (_font != null) _spriteBatch.DrawString(_font, "Summary", new Vector2(r.X+20, sy), Color.Yellow); sy += 40;
        DrawSummarySect("HOOK", sel.AdventureHook, r.X+20, ref sy, sw-40);
        DrawSummarySect("MIDDLE", sel.AdventureMiddle, r.X+20, ref sy, sw-40);
        DrawSummarySect("ENDING", sel.AdventureEnding, r.X+20, ref sy, sw-40);
    }
    private void DrawSummarySect(string t, string c, int x, ref int y, int w) { if (_font==null) return; _spriteBatch.DrawString(_font, t+":", new Vector2(x, y), Color.Orange, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f); y += 20; string wrapped = WrapText(SafeString(c ?? "None"), w); _spriteBatch.DrawString(_font, wrapped, new Vector2(x+10, y), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f); y += (int)(_font.MeasureString(wrapped).Y * 0.6f) + 10; }
    private string WrapText(string t, float w) { if (_font == null || string.IsNullOrEmpty(t)) return ""; string res = "", line = ""; foreach (var word in t.Split(' ')) { if (_font.MeasureString(line + word).X * 0.6f < w) line += word + " "; else { res += line + "\n"; line = word + " "; } } return res + line; }
    private string SafeString(string t) { if (_font == null || t == null) return t ?? ""; char[] chars = t.ToCharArray(); for (int i = 0; i < chars.Length; i++) if (!_supportedChars.Contains(chars[i])) chars[i] = '?'; return new string(chars); }
}
