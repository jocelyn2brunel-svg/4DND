using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace _4DND
{
    /// <summary>
    /// Displays the campaign world map showing discovered locations.
    /// Players can see their home base and explore outward.
    /// Supports multiple map scales (Province, Kingdom, Continent).
    /// </summary>
    public class CampaignMapViewer
    {
        private SpriteFont _font;
        private Texture2D _pixel;
        public VisionSystem VisionSystem { get; set; } = null!;
        
        private Vector2 _cameraOffset = Vector2.Zero;
        private float _zoom = 1.0f;
        private int _tileSize = 30;
        
        private Location? _selectedLocation = null;
        private (int q, int r)? _selectedHex = null;
        private MouseState _prevMouse;
        private int _prevScrollValue = 0;
        private bool _showAdventureDetails = false;
        public bool TravelOccurred { get; private set; } = false;
        public bool PartyPositionChanged { get; private set; } = false;

        private bool _isTraveling = false;
        private float _targetPartyX;
        private float _targetPartyY;
        private Vector2 _startPartyPos;
        private TravelPace _travelPace = TravelPace.Normal;
        private System.Random _random = new System.Random();
        private float _minutesSinceLastEncounterCheck = 0;
        private string _travelMessage = "";
        private float _travelMessageTimer = 0f;

        private const float TimeScale = 20f; // 1 seconde rÃ©elle = 20 minutes de jeu

        // Miles par heure selon l'allure (PHB p.182)
        private static float GetTravelSpeedMPH(TravelPace pace) => pace switch
        {
            TravelPace.Fast   => 4f,
            TravelPace.Normal => 3f,
            TravelPace.Slow   => 2f,
            _ => 3f
        };

        // Caches for procedural map data
        private System.Collections.Generic.Dictionary<(int q, int r, int seed, MapScale scale), BiomeType> _biomeCache = new();
        private System.Collections.Generic.Dictionary<(int q, int r, int seed), System.Collections.Generic.List<Vector2>> _foliageCache = new();
        
        public CampaignMapViewer(SpriteFont font, Texture2D pixel)
        {
            _font = font;
            _pixel = pixel;
        }
        
        public void Update(Campaign campaign, System.Collections.Generic.List<Character> characters, MouseState mouse, KeyboardState kb, KeyboardState prevKb, GameTime gameTime, Viewport viewport)
        {
            if (campaign == null) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle travel movement
            if (_isTraveling)
            {
                float gameMinutesPassed = dt * TimeScale;
                campaign.TotalGameMinutes += gameMinutesPassed;

                ProcessSurvival(campaign, characters, gameMinutesPassed);
                CheckRandomEncounters(campaign, gameMinutesPassed);

                if (_isTraveling) // Might have been stopped by encounter or fatigue
                {
                    Vector2 startPos = new Vector2(campaign.PartyX, campaign.PartyY);
                    Vector2 targetPos = new Vector2(_targetPartyX, _targetPartyY);
                    float dist = Vector2.Distance(startPos, targetPos);

                    float mph = GetTravelSpeedMPH(_travelPace);
                    float milesPerMinute = mph / 60f;
                    float moveAmount = milesPerMinute * gameMinutesPassed;

                    if (dist <= moveAmount)
                    {
                        campaign.PartyX = _targetPartyX;
                        campaign.PartyY = _targetPartyY;
                        PartyPositionChanged = true;
                        _isTraveling = false;
                        TravelOccurred = true;
                    }
                    else if (dist > 0f)
                    {
                        Vector2 direction = Vector2.Normalize(targetPos - startPos);
                        Vector2 newPos = startPos + direction * moveAmount;
                        campaign.PartyX = newPos.X;
                        campaign.PartyY = newPos.Y;
                        PartyPositionChanged = true;
                    }

                    Vector2 endPos = new Vector2(campaign.PartyX, campaign.PartyY);

                    // Calculate reveal radius based on light level and party capabilities
                    float revealRadiusFeet = VisionSystem.GetRevealRadius(campaign, characters);

                    VisionSystem?.RevealPath(startPos, endPos, revealRadiusFeet);

                    // Auto-discover locations revealed by the party's vision
                    campaign.DiscoverLocations(revealRadiusFeet, msg => SetTravelMessage(msg));
                }
            }

            if (_travelMessageTimer > 0)
            {
                _travelMessageTimer -= dt;
                if (_travelMessageTimer <= 0) _travelMessage = "";
            }

            var center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
            
            // Pan camera with WASD
            float panSpeed = 5f;
            if (kb.IsKeyDown(Keys.W)) _cameraOffset.Y += panSpeed;
            if (kb.IsKeyDown(Keys.S)) _cameraOffset.Y -= panSpeed;
            if (kb.IsKeyDown(Keys.A)) _cameraOffset.X += panSpeed;
            if (kb.IsKeyDown(Keys.D)) _cameraOffset.X -= panSpeed;
            
            // Zoom with +/-
            if (kb.IsKeyDown(Keys.OemPlus) && !prevKb.IsKeyDown(Keys.OemPlus))
                ZoomAtScreenPosition(mouse.Position.ToVector2(), center, _zoom + 0.1f, campaign);
            if (kb.IsKeyDown(Keys.OemMinus) && !prevKb.IsKeyDown(Keys.OemMinus))
                ZoomAtScreenPosition(mouse.Position.ToVector2(), center, _zoom - 0.1f, campaign);
            
            // Zoom with mouse wheel
            int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
            if (scrollDelta != 0)
            {
                ZoomAtScreenPosition(mouse.Position.ToVector2(), center, _zoom + scrollDelta * 0.001f, campaign);
                _prevScrollValue = mouse.ScrollWheelValue;
            }
            
            // Switch travel pace with F4/F5/F6 keys
            if (kb.IsKeyDown(Keys.F4) && !prevKb.IsKeyDown(Keys.F4))
            {
                _travelPace = TravelPace.Fast;
                System.Console.WriteLine("Travel pace: Fast (4 mi/h, 30 mi/day, -5 passive Perception)");
            }
            if (kb.IsKeyDown(Keys.F5) && !prevKb.IsKeyDown(Keys.F5))
            {
                _travelPace = TravelPace.Normal;
                System.Console.WriteLine("Travel pace: Normal (3 mi/h, 24 mi/day)");
            }
            if (kb.IsKeyDown(Keys.F6) && !prevKb.IsKeyDown(Keys.F6))
            {
                _travelPace = TravelPace.Slow;
                System.Console.WriteLine("Travel pace: Slow (2 mi/h, 18 mi/day, stealth available)");
            }

            // Click to select location or hex
            if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                // Handle UI button clicks first
                var panelRect = new Rectangle(10, 10, 350, 300); // Match DrawInfoPanel
                var travelBtn = new Rectangle(panelRect.X + 20, panelRect.Y + 200, 180, 30);
                var restBtn = new Rectangle(panelRect.X + 20, panelRect.Y + 250, 180, 30);

                if (travelBtn.Contains(mouse.Position))
                {
                    if (_isTraveling)
                    {
                        // Cancel travel
                        _isTraveling = false;
                        System.Console.WriteLine("Travel cancelled.");
                    }
                    else if (_selectedLocation != null || _selectedHex != null)
                    {
                        // Start Travel
                        float tx, ty;
                        if (_selectedLocation != null)
                        {
                            var miles = Campaign.AxialToMiles(_selectedLocation.X, _selectedLocation.Y);
                            tx = miles.x;
                            ty = miles.y;
                        }
                        else
                        {
                            int hexSizeInMiles = Campaign.GetHexSize(campaign.CurrentScale);
                            var miles = Campaign.AxialToMiles(_selectedHex!.Value.q * hexSizeInMiles, _selectedHex.Value.r * hexSizeInMiles);
                            tx = miles.x;
                            ty = miles.y;
                        }

                        _targetPartyX = tx;
                        _targetPartyY = ty;
                        _startPartyPos = new Vector2(campaign.PartyX, campaign.PartyY);
                        _isTraveling = true;
                        System.Console.WriteLine($"Traveling to ({tx:F1}, {ty:F1}) miles");
                    }
                }
                else if (restBtn.Contains(mouse.Position) && !_isTraveling)
                {
                    // Long Rest
                    campaign.TotalGameMinutes += 8 * 60;
                    foreach (var c in characters)
                    {
                        RestManager.LongRest(c);
                    }
                    SetTravelMessage(Loc.Tr("The party took a long rest (8h)."));
                }
                else
                {
                    // Map Selection logic
                    _selectedLocation = null;
                    _selectedHex = null;

                    // 1. Try selecting a location first
                    foreach (var loc in campaign.AllLocations)
                    {
                        if (!loc.IsDiscovered) continue;

                        float scaleDivisor = Campaign.GetHexSize(campaign.CurrentScale);
                        var pos = HexToScreen(loc.X / scaleDivisor, loc.Y / scaleDivisor, center);

                        if (Vector2.Distance(mouse.Position.ToVector2(), pos) < 30 * _zoom)
                        {
                            _selectedLocation = loc;
                            _selectedHex = Campaign.RoundToHex(loc.X / scaleDivisor, loc.Y / scaleDivisor); // Store hex for highlighting
                            break;
                        }
                    }

                    // 2. If no location selected, select the hex under mouse
                    if (_selectedLocation == null)
                    {
                        Vector2 mouseWorld = ScreenToHexWorld(mouse.Position.ToVector2(), center);
                        float q_click = mouseWorld.X;
                        float r_click = mouseWorld.Y;

                        var (qh, rh) = Campaign.RoundToHex(q_click, r_click);
                        _selectedHex = (qh, rh);
                    }
                }
            }

            // Toggle adventure details with J
            if (kb.IsKeyDown(Keys.J) && !prevKb.IsKeyDown(Keys.J))
            {
                _showAdventureDetails = !_showAdventureDetails;
            }
            
            _prevMouse = mouse;
        }

        public void ResetTravelFlags()
        {
            TravelOccurred = false;
            PartyPositionChanged = false;
        }

        public void SyncScrollBaseline(int scrollWheelValue)
        {
            _prevScrollValue = scrollWheelValue;
        }

        public void ResetInputBaseline(MouseState mouse)
        {
            _prevMouse = mouse;
            _prevScrollValue = mouse.ScrollWheelValue;
        }
        
        public void Draw(SpriteBatch sb, GraphicsDevice device, Campaign campaign)
        {
            if (campaign == null || _font == null) return;
            
            var vp = device.Viewport;
            var center = new Vector2(vp.Width / 2f, vp.Height / 2f);
            
            // Background (Day/Night)
            Color bgColor = Color.Black * 0.9f;
            if (campaign.GameHour < 6 || campaign.GameHour >= 20) bgColor = new Color(10, 10, 30) * 0.95f; // Night
            else if (campaign.GameHour < 8 || campaign.GameHour >= 18) bgColor = new Color(40, 20, 10) * 0.9f; // Twilight/Dawn

            sb.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), bgColor);
            
            // Draw grid
            DrawGrid(sb, center, campaign);
            
            // Draw Selected Hex Highlight
            if (_selectedHex.HasValue)
            {
                var pos = HexToScreen(_selectedHex.Value.q, _selectedHex.Value.r, center);
                DrawHexagon(sb, pos, _tileSize * _zoom, Color.Yellow, 4f);
            }

            // Draw regions at current scale
            var regionsAtScale = campaign.GetRegionsAtScale(campaign.CurrentScale);
            foreach (var region in regionsAtScale)
            {
                DrawRegion(sb, center, region, campaign.CurrentScale);
            }
            
            // Draw locations at current scale
            var locationsAtScale = campaign.GetLocationsAtScale(campaign.CurrentScale);
            foreach (var location in locationsAtScale)
            {
                DrawLocation(sb, center, location, campaign.CurrentScale);
            }

            // Draw Party Marker
            DrawPartyMarker(sb, center, campaign);
            
            // Draw info panel
            DrawInfoPanel(sb, vp, campaign);
            
            // Draw scale indicator
            DrawScaleIndicator(sb, vp, campaign);

            DrawTravelProgress(sb, vp, campaign);
            DrawTravelMessage(sb, vp);
            
            if (_showAdventureDetails)
            {
                DrawAdventureDetails(sb, vp, campaign);
            }

            // Draw travel pace panel
            DrawTravelPacePanel(sb, vp);

            // Instructions
            if (_font != null)
            {
                sb.DrawString(_font, Loc.Tr("Map Legend"), new Vector2(10, vp.Height - 30), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
        
        private void DrawTravelPacePanel(SpriteBatch sb, Viewport vp)
        {
            if (_font == null) return;

            // Position below the scale indicator panel (top-right area)
            var panelRect = new Rectangle(vp.Width - 320, 240, 310, 130);
            sb.Draw(_pixel, panelRect, Color.Black * 0.8f);
            DrawBorder(sb, panelRect, Color.SaddleBrown, 2);

            int y = panelRect.Y + 10;
            sb.DrawString(_font, Loc.Tr("Travel Pace"), new Vector2(panelRect.X + 10, y), new Color(205, 133, 63), 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            y += 22;

            // Header row
            sb.DrawString(_font, $"{Loc.Tr("Pace")}       ft/min  mi/hr  mi/day  {Loc.Tr("Effect")}", new Vector2(panelRect.X + 10, y), Color.Gray, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            y += 14;

            var paces = new[]
            {
                (TravelPace.Fast,   $"[F4] {Loc.Tr("Fast")}  ", 400, 4, 30, Loc.Tr("-5 passive Percep.")),
                (TravelPace.Normal, $"[F5] {Loc.Tr("Normal")}", 300, 3, 24, string.Empty),
                (TravelPace.Slow,   $"[F6] {Loc.Tr("Slow")}  ", 200, 2, 18, Loc.Tr("Stealth available")),
            };

            foreach (var (pace, label, ftMin, miHr, miDay, effect) in paces)
            {
                bool isCurrent = pace == _travelPace;
                Color color = isCurrent ? Color.Yellow : Color.LightGray;
                string row = $"{label}  {ftMin,3}     {miHr}      {miDay,2}     {effect}";
                sb.DrawString(_font, row, new Vector2(panelRect.X + 10, y), color, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                y += 14;
            }
        }

        private void DrawScaleIndicator(SpriteBatch sb, Viewport vp, Campaign campaign)
        {
            var panelRect = new Rectangle(vp.Width - 320, 10, 310, 220);
            sb.Draw(_pixel, panelRect, Color.Black * 0.8f);
            DrawBorder(sb, panelRect, Color.Gold, 2);
            
            if (_font == null) return;
            
            int y = panelRect.Y + 10;
            
            sb.DrawString(_font, Loc.Tr("Map Scale"), new Vector2(panelRect.X + 10, y), Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            y += 25;
            
            // Draw all three scales with current one highlighted
            var scales = new[] { MapScale.Province, MapScale.Kingdom, MapScale.Continent };
            foreach (var scale in scales)
            {
                bool isCurrent = scale == campaign.CurrentScale;
                Color color = isCurrent ? Color.Yellow : Color.Gray;
                
                string scaleName = scale switch
                {
                    MapScale.Province => Loc.Tr("Province"),
                    MapScale.Kingdom => Loc.Tr("Kingdom"),
                    MapScale.Continent => Loc.Tr("Continent"),
                    _ => "Unknown"
                };
                
                int hexSize = Campaign.GetHexSize(scale);
                string scaleText = $"{scaleName}: {Loc.Tr("1 hex = {0} mile{1}", hexSize, hexSize > 1 ? "s" : "")}";
                
                sb.DrawString(_font, scaleText, new Vector2(panelRect.X + 15, y), color, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                y += 22;
            }
            
            y += 10;
            
            // Scale description
            string description = campaign.CurrentScale switch
            {
                MapScale.Province => Loc.Tr("Detailed local exploration"),
                MapScale.Kingdom => Loc.Tr("Regional travel overview"),
                MapScale.Continent => Loc.Tr("Continental geography"),
                _ => ""
            };
            
            sb.DrawString(_font, description, new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            y += 25;
            
            // Show what's visible at current scale
            sb.DrawString(_font, Loc.Tr("Visible at this scale:"), new Vector2(panelRect.X + 10, y), Color.Cyan, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
            y += 18;
            
            var visibleTypes = GetVisibleSettlementTypes(campaign.CurrentScale);
            foreach (var type in visibleTypes)
            {
                string typeName = type switch
                {
                    SettlementType.Hamlet => Loc.Tr("Hamlets"),
                    SettlementType.Village => Loc.Tr("Villages"),
                    SettlementType.Town => Loc.Tr("Towns"),
                    SettlementType.City => Loc.Tr("Cities"),
                    SettlementType.Metropolis => Loc.Tr("Metropolises"),
                    SettlementType.Fort => Loc.Tr("Forts"),
                    SettlementType.Castle => Loc.Tr("Castles"),
                    SettlementType.Monastery => Loc.Tr("Monasteries"),
                    SettlementType.Dungeon => Loc.Tr("Dungeons"),
                    SettlementType.Wilderness => Loc.Tr("Wilderness"),
                    _ => "Unknown"
                };
                
                sb.DrawString(_font, $"- {typeName}", new Vector2(panelRect.X + 20, y), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
                y += 16;
            }
        }
        
        private SettlementType[] GetVisibleSettlementTypes(MapScale scale)
        {
            return scale switch
            {
                MapScale.Province => new[] 
                { 
                    SettlementType.Hamlet, 
                    SettlementType.Village, 
                    SettlementType.Town,
                    SettlementType.Fort,
                    SettlementType.Castle,
                    SettlementType.City,
                    SettlementType.Monastery,
                    SettlementType.Dungeon,
                    SettlementType.Wilderness
                },
                MapScale.Kingdom => new[] 
                { 
                    SettlementType.Town,
                    SettlementType.Fort,
                    SettlementType.Castle,
                    SettlementType.City,
                    SettlementType.Metropolis
                },
                MapScale.Continent => new[] 
                { 
                    SettlementType.City,
                    SettlementType.Metropolis,
                    SettlementType.Castle
                },
                _ => Array.Empty<SettlementType>()
            };
        }
        
        private void DrawGrid(SpriteBatch sb, Vector2 center, Campaign campaign)
        {
            float size = _tileSize * _zoom;
            float horizontalSpacing = size * (float)Math.Sqrt(3);
            float verticalSpacing = size * 1.5f;

            // Determine visible range of r
            float minY = -center.Y - _cameraOffset.Y;
            float maxY = center.Y - _cameraOffset.Y;

            int minR = (int)Math.Floor(minY / verticalSpacing) - 1;
            int maxR = (int)Math.Ceiling(maxY / verticalSpacing) + 1;

            int hexSizeInMiles = Campaign.GetHexSize(campaign.CurrentScale);

            for (int r = minR; r <= maxR; r++)
            {
                // For a given r, screenX = horizontalSpacing * (q + r/2.0f)
                // We want screenX + _cameraOffset.X between -center.X and +center.X
                float minX = -center.X - _cameraOffset.X;
                float maxX = center.X - _cameraOffset.X;

                // q = screenX / horizontalSpacing - r/2.0f
                int minQ = (int)Math.Floor(minX / horizontalSpacing - r / 2f) - 1;
                int maxQ = (int)Math.Ceiling(maxX / horizontalSpacing - r / 2f) + 1;

                for (int q = minQ; q <= maxQ; q++)
                {
                    var pos = HexToScreen(q, r, center);

                    // Determine biome at this hex (with cache)
                    var cacheKey = (q, r, campaign.Seed, campaign.CurrentScale);
                    if (!_biomeCache.TryGetValue(cacheKey, out BiomeType biome))
                    {
                        var (x_miles, y_miles) = Campaign.AxialToMiles((float)q * hexSizeInMiles, (float)r * hexSizeInMiles);
                        biome = WorldGenerator.GetBiome(x_miles, y_miles, campaign.Seed);
                        _biomeCache[cacheKey] = biome;
                    }

                    // Check if hex is explored
                    int worldQ = q * hexSizeInMiles;
                    int worldR = r * hexSizeInMiles;
                    bool isExplored = VisionSystem?.IsHexExplored(worldQ, worldR) ?? true;

                    if (!isExplored)
                    {
                        // Always show hexes very close to the party
                        var (pq, pr) = Campaign.MilesToAxial(campaign.PartyX, campaign.PartyY);
                        if (Campaign.GetHexDistance(worldQ, worldR, (int)Math.Round(pq), (int)Math.Round(pr)) <= 1)
                            isExplored = true;
                    }

                    Color biomeColor = isExplored ? WorldGenerator.GetBiomeColor(biome) : Color.Black * 0.9f;

                    // Fill hexagon with biome color
                    DrawHexagonFill(sb, pos, _tileSize * _zoom, isExplored ? biomeColor * 0.6f : biomeColor);

                    if (isExplored)
                    {
                        // Draw procedural foliage if biome supports it
                        DrawProceduralFoliage(sb, pos, biome, q, r, campaign.Seed);
                    }

                    // Draw outline
                    DrawHexagon(sb, pos, _tileSize * _zoom, isExplored ? Color.Black * 0.2f : Color.White * 0.05f);
                }
            }
        }

        private void DrawProceduralFoliage(SpriteBatch sb, Vector2 center, BiomeType biome, int q, int r, int seed)
        {
            // Only draw foliage for certain biomes and if zoomed in enough
            float foliageAlpha = MathHelper.Clamp((_zoom - 0.25f) / 0.15f, 0f, 1f);
            if (foliageAlpha <= 0) return;

            int foliageCount = biome switch
            {
                BiomeType.Forest => 5,
                BiomeType.Swamp => 3,
                BiomeType.Plains => 1,
                BiomeType.Mountain => 1,
                _ => 0
            };

            if (foliageCount == 0) return;

            var cacheKey = (q, r, seed);
            if (!_foliageCache.TryGetValue(cacheKey, out var offsets))
            {
                offsets = new System.Collections.Generic.List<Vector2>();
                for (int i = 0; i < foliageCount; i++)
                {
                    float angle = Hash(q, r, seed + i) * MathHelper.TwoPi;
                    float dist = Hash(q, r, seed + i + 100) * 0.6f; // Normalized dist
                    offsets.Add(new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist));
                }
                _foliageCache[cacheKey] = offsets;
            }

            float size = _tileSize * _zoom;
            Color foliageColor = new Color(30, 60, 20) * 0.5f * foliageAlpha;
            int dotSize = (int)Math.Max(2, 4 * _zoom);

            foreach (var offset in offsets)
            {
                Vector2 foliagePos = center + offset * size;
                sb.Draw(_pixel, new Rectangle((int)foliagePos.X - dotSize / 2, (int)foliagePos.Y - dotSize / 2, dotSize, dotSize), foliageColor);
            }
        }

        private float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                uint u = (uint)h;
                return (u & 0x00FFFFFF) / 16777215f;
            }
        }

        private void DrawHexagonFill(SpriteBatch sb, Vector2 center, float size, Color color)
        {
            // Fill a proper pointy-top hex using horizontal scanlines.
            // This avoids visible diagonal seam artifacts that appear when approximating
            // the fill with a few rectangles.
            int minY = (int)Math.Floor(center.Y - size);
            int maxY = (int)Math.Ceiling(center.Y + size);
            float halfWidthMax = (float)Math.Sqrt(3) * size * 0.5f;

            for (int y = minY; y <= maxY; y++)
            {
                float dy = Math.Abs(y - center.Y);
                float halfWidth;

                if (dy <= size * 0.5f)
                {
                    halfWidth = halfWidthMax;
                }
                else
                {
                    float taper = (size - dy) / (size * 0.5f);
                    if (taper <= 0f) continue;
                    halfWidth = halfWidthMax * taper;
                }

                int x = (int)Math.Floor(center.X - halfWidth);
                int width = Math.Max(1, (int)Math.Ceiling(halfWidth * 2f));
                sb.Draw(_pixel, new Rectangle(x, y, width, 1), color);
            }
        }
        
        private void DrawRegion(SpriteBatch sb, Vector2 center, Region region, MapScale currentScale)
        {
            float scaleDivisor = Campaign.GetHexSize(currentScale);
            var pos = HexToScreen((float)region.CenterX / scaleDivisor, (float)region.CenterY / scaleDivisor, center);
            
            // Draw region circle
            // Radius in pixels: region.Radius (miles) / scaleDivisor * (distance between hex centers)
            float hexDistance = (float)Math.Sqrt(3) * _tileSize * _zoom;
            int radius = (int)((float)region.Radius * hexDistance / scaleDivisor);
            
            for (int angle = 0; angle < 360; angle += 10)
            {
                float rad1 = MathHelper.ToRadians(angle);
                float rad2 = MathHelper.ToRadians(angle + 10);
                
                var p1 = pos + new Vector2((float)Math.Cos(rad1) * radius, (float)Math.Sin(rad1) * radius);
                var p2 = pos + new Vector2((float)Math.Cos(rad2) * radius, (float)Math.Sin(rad2) * radius);
                
                DrawLine(sb, p1, p2, Color.Yellow * 0.4f, 2f);
            }
            
            // Region name
            if (_font != null)
            {
                var nameSize = _font.MeasureString(region.Name);
                sb.DrawString(_font, region.Name, pos - nameSize * 0.5f, Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
        
        private void DrawLocation(SpriteBatch sb, Vector2 center, Location location, MapScale currentScale)
        {
            float scaleDivisor = Campaign.GetHexSize(currentScale);
            var pos = HexToScreen((float)location.X / scaleDivisor, (float)location.Y / scaleDivisor, center);
            
            // Smoothly fade important locations at far zoom, but keep cities/metropolises visible longer
            float baseAlpha = 1.0f;
            if (currentScale == MapScale.Continent && _zoom < 0.6f && !location.IsHomeBase)
            {
                float threshold = location.Type == SettlementType.Metropolis ? 0.3f : 0.45f;
                baseAlpha = MathHelper.Clamp((_zoom - threshold) / 0.15f, 0f, 1f);
            }
            if (baseAlpha <= 0) return;

            // Location color based on type
            Color locationColor = location.Type switch
            {
                SettlementType.Village => Color.LightGreen,
                SettlementType.Town => Color.Green,
                SettlementType.City => Color.DarkGreen,
                SettlementType.Metropolis => Color.Gold,
                SettlementType.Fort => Color.Red,
                SettlementType.Castle => Color.DarkRed,
                SettlementType.Dungeon => Color.Purple,
                SettlementType.Monastery => Color.LightBlue,
                SettlementType.Hamlet => Color.YellowGreen,
                SettlementType.Wilderness => Color.Brown,
                _ => Color.White
            };
            
            // Adjust size based on settlement importance
            float sizeMultiplier = location.Type switch
            {
                SettlementType.Hamlet => 0.3f,
                SettlementType.Village => 0.4f,
                SettlementType.Monastery => 0.4f,
                SettlementType.Dungeon => 0.45f,
                SettlementType.Fort => 0.5f,
                SettlementType.Town => 0.6f,
                SettlementType.Castle => 0.65f,
                SettlementType.City => 0.8f,
                SettlementType.Metropolis => 1.0f,
                _ => 0.5f
            };
            
            // Home base gets special marker
            if (location.IsHomeBase)
            {
                DrawStar(sb, pos, _tileSize * _zoom * 0.7f, Color.Gold);
                // Add glow effect
                DrawStar(sb, pos, _tileSize * _zoom * 0.9f, Color.Gold * 0.3f);
            }
            else
            {
                // Draw location marker with size based on importance
                int size = (int)(_tileSize * _zoom * sizeMultiplier);
                
                // Draw shadow for depth
                sb.Draw(_pixel, new Rectangle((int)pos.X - size/2 + 2, (int)pos.Y - size/2 + 2, size, size), Color.Black * 0.5f);
                
                // Draw main marker
                sb.Draw(_pixel, new Rectangle((int)pos.X - size/2, (int)pos.Y - size/2, size, size), locationColor * baseAlpha);
                
                // Draw border for cities and metropolises
                if (location.Type == SettlementType.City || location.Type == SettlementType.Metropolis)
                {
                    DrawBorder(sb, new Rectangle((int)pos.X - size/2, (int)pos.Y - size/2, size, size), Color.White * 0.7f * baseAlpha, 1);
                }
            }
            
            // Location name (only show if zoomed in enough or if it's an important location)
            if (_font != null)
            {
                float nameAlpha = baseAlpha;
                if (_zoom < 0.8f && location.Type != SettlementType.City && location.Type != SettlementType.Metropolis && !location.IsHomeBase)
                {
                    nameAlpha *= MathHelper.Clamp((_zoom - 0.5f) / 0.3f, 0f, 1f);
                }

                if (nameAlpha > 0.05f)
                {
                    float nameScale = location.Type switch
                    {
                        SettlementType.Metropolis => 0.7f,
                        SettlementType.City => 0.6f,
                        _ => 0.5f
                    };
                    
                    var nameSize = _font.MeasureString(location.Name) * nameScale;
                    var namePos = pos + new Vector2(-nameSize.X * 0.5f, _tileSize * _zoom * 0.6f);
                    
                    // Draw text shadow
                    sb.DrawString(_font, location.Name, namePos + new Vector2(1, 1), Color.Black * 0.8f * nameAlpha, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
                    // Draw text
                    sb.DrawString(_font, location.Name, namePos, Color.White * nameAlpha, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
                }
            }
        }
        
        private void DrawInfoPanel(SpriteBatch sb, Viewport vp, Campaign campaign)
        {
            var panelRect = new Rectangle(10, 10, 350, 300);
            sb.Draw(_pixel, panelRect, Color.Black * 0.8f);
            DrawBorder(sb, panelRect, Color.Gold, 2);

            if (_font == null) return;

            int y = panelRect.Y + 10;

            sb.DrawString(_font, campaign.Name, new Vector2(panelRect.X + 10, y), Color.Yellow, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            y += 25;

            sb.DrawString(_font, campaign.GameTimeDisplay, new Vector2(panelRect.X + 10, y), Color.LimeGreen, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            y += 25;

            sb.DrawString(_font, Loc.Tr("Home Base: {0}", campaign.HomeBase.Name), new Vector2(panelRect.X + 10, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 20;

            // Show location count for current scale
            var visibleLocations = campaign.GetLocationsAtScale(campaign.CurrentScale);
            sb.DrawString(_font, Loc.Tr("Locations (visible): {0} / {1}", visibleLocations.Count, campaign.AllLocations.Count), new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 20;

            sb.DrawString(_font, Loc.Tr("Session: {0}", campaign.SessionCount), new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 20;

            // Current scale info
            string scaleName = campaign.CurrentScale switch
            {
                MapScale.Province => Loc.Tr("Province"),
                MapScale.Kingdom => Loc.Tr("Kingdom"),
                MapScale.Continent => Loc.Tr("Continent"),
                _ => "Unknown"
            };
            int currentHexSize = Campaign.GetHexSize(campaign.CurrentScale);
            sb.DrawString(_font, $"{Loc.Tr("Scale")}: {scaleName} ({Loc.Tr("{0}mi/hex", currentHexSize)})", new Vector2(panelRect.X + 10, y), Color.Cyan, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 25;

            if (!string.IsNullOrEmpty(campaign.CurrentObjective))
            {
                sb.DrawString(_font, Loc.Tr("Objective:"), new Vector2(panelRect.X + 10, y), Color.Orange, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                y += 18;

                // Wrap objective text
                string wrapped = WrapText(campaign.CurrentObjective, 330);
                sb.DrawString(_font, wrapped, new Vector2(panelRect.X + 10, y), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }

            if (_selectedLocation != null || _selectedHex != null)
            {
                y += 18;
                string selectedName = _selectedLocation != null ? _selectedLocation.Name : $"Hex ({_selectedHex!.Value.q}, {_selectedHex.Value.r})";
                sb.DrawString(_font, Loc.Tr("Selected: {0}", selectedName), new Vector2(panelRect.X + 10, y), Color.Yellow, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

                if (_selectedLocation == null && _selectedHex.HasValue)
                {
                    y += 18;
                    int selectedHexSize = Campaign.GetHexSize(campaign.CurrentScale);
                    var (xm, ym) = Campaign.AxialToMiles(_selectedHex.Value.q * selectedHexSize, _selectedHex.Value.r * selectedHexSize);
                    var biome = WorldGenerator.GetBiome(xm, ym, campaign.Seed);
                    sb.DrawString(_font, Loc.Tr("Terrain: {0}", biome), new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }

                y += 25;

                // Travel / Cancel Button
                var travelBtn = new Rectangle(panelRect.X + 20, panelRect.Y + 200, 180, 30);
                var mouse = Mouse.GetState();
                bool isHovered = travelBtn.Contains(mouse.Position);

                Color btnColor = _isTraveling ? Color.DarkRed : Color.Green;
                if (isHovered) btnColor = _isTraveling ? Color.Red : Color.DarkGreen;

                sb.Draw(_pixel, travelBtn, btnColor);
                string btnLabel = _isTraveling ? Loc.Tr("Cancel Travel") : Loc.Tr("Travel Here");
                sb.DrawString(_font, btnLabel, new Vector2(travelBtn.X + 10, travelBtn.Y + 5), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

            // Long Rest Button
            if (!_isTraveling)
            {
                var restBtn = new Rectangle(panelRect.X + 20, panelRect.Y + 250, 180, 30);
                var mouse = Mouse.GetState();
                bool isHovered = restBtn.Contains(mouse.Position);
                sb.Draw(_pixel, restBtn, isHovered ? Color.DarkBlue : Color.Blue);
                sb.DrawString(_font, Loc.Tr("Long Rest (8h)"), new Vector2(restBtn.X + 10, restBtn.Y + 5), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

        }

        private void DrawPartyMarker(SpriteBatch sb, Vector2 center, Campaign campaign)
        {
            // Convert cartesian miles back to axial for HexToScreen
            var (q_miles, r_miles) = Campaign.MilesToAxial(campaign.PartyX, campaign.PartyY);

            float scaleDivisor = Campaign.GetHexSize(campaign.CurrentScale);
            var pos = HexToScreen(q_miles / scaleDivisor, r_miles / scaleDivisor, center);

            // Draw a party icon (circle with a cross)
            int size = (int)(24 * _zoom);
            sb.Draw(_pixel, new Rectangle((int)pos.X - size/2, (int)pos.Y - size/2, size, size), Color.White);
            sb.Draw(_pixel, new Rectangle((int)pos.X - size/2 + 2, (int)pos.Y - size/2 + 2, size - 4, size - 4), Color.Red);

            if (_font != null)
            {
                sb.DrawString(_font, Loc.Tr("PARTY"), pos + new Vector2(-20, -30 * _zoom), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
        
        private void SetTravelMessage(string msg)
        {
            _travelMessage = msg;
            _travelMessageTimer = 5f;
            System.Console.WriteLine(msg);
        }

        private void ProcessSurvival(Campaign campaign, System.Collections.Generic.List<Character> characters, float gameMinutesPassed)
        {
            if (campaign == null || characters == null || characters.Count == 0) return;

            // Track hours traveled for exhaustion (Forced March PHB p.181)
            float prevHours = campaign.HoursTraveledToday;
            campaign.HoursTraveledToday += gameMinutesPassed / 60f;

            if (campaign.HoursTraveledToday > 8.0f)
            {
                int currentHourInt = (int)Math.Floor(campaign.HoursTraveledToday);
                int prevHourInt = (int)Math.Floor(prevHours);

                if (currentHourInt > prevHourInt && currentHourInt > 8)
                {
                    // Chaque heure au-dela de 8h, jet de sauvegarde de CON DC 10 + 1 par heure supp.
                    int extraHours = currentHourInt - 8;
                    int dc = 10 + extraHours;

                    foreach (var c in characters)
                    {
                        var save = c.MakeSavingThrow("CON", dc, false, false);
                        if (!save.Success)
                        {
                            if (c.ExhaustionLevel < 6)
                            {
                                c.ExhaustionLevel++;
                                SetTravelMessage(Loc.Tr("{0} is exhausted from forced march (CON save DC {1} failed)!", c.Name, dc));
                            }
                        }
                    }
                }
            }

            // Daily survival check (midnight)
            int currentDay = campaign.GameDay;
            int prevDay = (int)((campaign.TotalGameMinutes - gameMinutesPassed) / (24 * 60)) + 1;

            if (currentDay > prevDay)
            {
                foreach (var character in characters)
                {
                    // Food consumption
                    if (character.FoodConsumedToday < 1f)
                    {
                        if (character.InventoryData.RemoveItem("Rations (1 day)"))
                        {
                            character.FoodConsumedToday = 1f;
                        }
                        else
                        {
                            character.DaysWithoutFood += 1f;
                            if (character.DaysWithoutFood > 3 + DndMath.GetAbilityModifier(character.Constitution))
                            {
                                character.ExhaustionLevel++;
                                SetTravelMessage(Loc.Tr("{0} suffers from hunger!", character.Name));
                            }
                        }
                    }

                    character.FoodConsumedToday = 0;
                    character.WaterConsumedToday = 0;
                    campaign.HoursTraveledToday = 0;

                    RestManager.ResetLongRestTracker(character);
                }
                SetTravelMessage(Loc.Tr("A new day begins. Day {0}.", currentDay));
            }
        }

        private void CheckRandomEncounters(Campaign campaign, float gameMinutesPassed)
        {
            _minutesSinceLastEncounterCheck += gameMinutesPassed;
            if (_minutesSinceLastEncounterCheck >= 4 * 60) // Every 4 hours
            {
                _minutesSinceLastEncounterCheck = 0;
                if (_random.Next(1, 21) >= 19) // 10% chance
                {
                    SetTravelMessage(Loc.Tr("Random encounter! Travel stops."));
                    _isTraveling = false;
                }
            }
        }

        private void DrawTravelProgress(SpriteBatch sb, Viewport vp, Campaign campaign)
        {
            if (!_isTraveling) return;

            var currentPos = new Vector2(campaign.PartyX, campaign.PartyY);
            var targetPos = new Vector2(_targetPartyX, _targetPartyY);
            float totalDist = Vector2.Distance(_startPartyPos, targetPos);
            float currentDist = Vector2.Distance(currentPos, targetPos);
            float progress = totalDist > 0.1f ? 1f - (currentDist / totalDist) : 1f;
            progress = MathHelper.Clamp(progress, 0f, 1f);

            var barRect = new Rectangle(vp.Width / 2 - 200, vp.Height - 100, 400, 20);
            sb.Draw(_pixel, barRect, Color.Black * 0.5f);
            sb.Draw(_pixel, new Rectangle(barRect.X, barRect.Y, (int)(barRect.Width * progress), barRect.Height), Color.LimeGreen);
            DrawBorder(sb, barRect, Color.White, 1);

            string msg = Loc.Tr("Traveling... {0:P0}", progress);
            if (_font != null)
            {
                var size = _font.MeasureString(msg) * 0.8f;
                sb.DrawString(_font, msg, new Vector2(vp.Width / 2 - size.X / 2, barRect.Y - 25), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }

        private void DrawTravelMessage(SpriteBatch sb, Viewport vp)
        {
            if (string.IsNullOrEmpty(_travelMessage) || _font == null) return;

            var size = _font.MeasureString(_travelMessage) * 0.9f;
            var pos = new Vector2(vp.Width / 2 - size.X / 2, 100);

            sb.Draw(_pixel, new Rectangle((int)pos.X - 10, (int)pos.Y - 5, (int)size.X + 20, (int)size.Y + 10), Color.Black * 0.7f);
            DrawBorder(sb, new Rectangle((int)pos.X - 10, (int)pos.Y - 5, (int)size.X + 20, (int)size.Y + 10), Color.Orange, 1);
            sb.DrawString(_font, _travelMessage, pos, Color.Yellow, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        private Vector2 HexToScreen(float q, float r, Vector2 center)
        {
            return HexWorldToScreen(new Vector2(q, r), center);
        }

        private void ZoomAtScreenPosition(Vector2 screenPosition, Vector2 center, float targetZoom, Campaign campaign)
        {
            // 1. Get world position under cursor in miles before scale change
            Vector2 worldUnderCursorHex = ScreenToHexWorld(screenPosition, center);
            float hexSizeOld = Campaign.GetHexSize(campaign.CurrentScale);
            var miles = Campaign.AxialToMiles(worldUnderCursorHex.X * hexSizeOld, worldUnderCursorHex.Y * hexSizeOld);

            // 2. Handle Scale Transitions with hysteresis
            bool scaleChanged = false;

            if (campaign.CurrentScale == MapScale.Province && targetZoom < 0.24f)
            {
                campaign.CurrentScale = MapScale.Kingdom;
                targetZoom *= 6.0f;
                scaleChanged = true;
            }
            else if (campaign.CurrentScale == MapScale.Kingdom)
            {
                if (targetZoom > 1.55f)
                {
                    campaign.CurrentScale = MapScale.Province;
                    targetZoom /= 6.0f;
                    scaleChanged = true;
                }
                else if (targetZoom < 0.24f)
                {
                    campaign.CurrentScale = MapScale.Continent;
                    targetZoom *= 10.0f;
                    scaleChanged = true;
                }
            }
            else if (campaign.CurrentScale == MapScale.Continent && targetZoom > 2.55f)
            {
                campaign.CurrentScale = MapScale.Kingdom;
                targetZoom /= 10.0f;
                scaleChanged = true;
            }

            // Absolute clamping for each scale
            if (campaign.CurrentScale == MapScale.Province) _zoom = MathHelper.Clamp(targetZoom, 0.2f, 4.0f);
            else if (campaign.CurrentScale == MapScale.Continent) _zoom = MathHelper.Clamp(targetZoom, 0.1f, 3.0f);
            else _zoom = MathHelper.Clamp(targetZoom, 0.2f, 2.0f); // Kingdom

            if (scaleChanged)
            {
                _selectedHex = null; // Clear hex selection as grid changed
                System.Console.WriteLine($"Scale changed to {campaign.CurrentScale}. Zoom: {_zoom}");
            }

            // 3. Recalculate camera offset to keep the SAME mile position under screenPosition
            float hexSizeNew = Campaign.GetHexSize(campaign.CurrentScale);
            var (qh_miles, rh_miles) = Campaign.MilesToAxial(miles.x, miles.y);
            Vector2 worldUnderCursorNew = new Vector2(qh_miles / hexSizeNew, rh_miles / hexSizeNew);

            Vector2 withoutCamera = HexWorldToScreen(worldUnderCursorNew, center, includeCameraOffset: false);
            _cameraOffset = screenPosition - withoutCamera;
        }

        private Vector2 ScreenToHexWorld(Vector2 screenPosition, Vector2 center)
        {
            float size = _tileSize * _zoom;
            Vector2 mouseRelative = screenPosition - center - _cameraOffset;

            float r = mouseRelative.Y / (1.5f * size);
            float q = (mouseRelative.X / (size * (float)Math.Sqrt(3))) - r / 2f;
            return new Vector2(q, r);
        }

        private Vector2 HexWorldToScreen(Vector2 worldPosition, Vector2 center, bool includeCameraOffset = true)
        {
            float size = _tileSize * _zoom;
            float screenX = size * ((float)Math.Sqrt(3) * worldPosition.X + (float)Math.Sqrt(3) / 2f * worldPosition.Y);
            float screenY = size * (1.5f * worldPosition.Y);

            Vector2 camera = includeCameraOffset ? _cameraOffset : Vector2.Zero;
            return center + camera + new Vector2(screenX, screenY);
        }
        
        private void DrawHexagon(SpriteBatch sb, Vector2 center, float size, Color color, float thickness = 1f)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle1 = MathHelper.ToRadians(60 * i - 30);
                float angle2 = MathHelper.ToRadians(60 * (i + 1) - 30);
                
                var p1 = center + new Vector2((float)Math.Cos(angle1) * size, (float)Math.Sin(angle1) * size);
                var p2 = center + new Vector2((float)Math.Cos(angle2) * size, (float)Math.Sin(angle2) * size);
                
                DrawLine(sb, p1, p2, color, thickness);
            }
        }
        
        private void DrawStar(SpriteBatch sb, Vector2 center, float size, Color color)
        {
            // 5-pointed star for home base
            for (int i = 0; i < 5; i++)
            {
                float angle1 = MathHelper.ToRadians(72 * i - 90);
                float angle2 = MathHelper.ToRadians(72 * (i + 2) - 90);
                
                var p1 = center + new Vector2((float)Math.Cos(angle1) * size, (float)Math.Sin(angle1) * size);
                var p2 = center + new Vector2((float)Math.Cos(angle2) * size, (float)Math.Sin(angle2) * size);
                
                DrawLine(sb, p1, p2, color, 3f);
            }
        }
        
        private void DrawAdventureDetails(SpriteBatch sb, Viewport vp, Campaign campaign)
        {
            int width = 600;
            int height = 500;
            var rect = new Rectangle((vp.Width - width) / 2, (vp.Height - height) / 2, width, height);

            sb.Draw(_pixel, rect, Color.Black * 0.95f);
            DrawBorder(sb, rect, Color.Gold, 2);

            int y = rect.Y + 20;
            sb.DrawString(_font, "Adventure Journal", new Vector2(rect.X + 20, y), Color.Gold, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
            y += 50;

            DrawAdventureSection(sb, "HOOK", campaign.AdventureHook, rect.X + 20, ref y, width - 40);
            y += 20;
            DrawAdventureSection(sb, "MIDDLE", campaign.AdventureMiddle, rect.X + 20, ref y, width - 40);
            y += 20;
            DrawAdventureSection(sb, "ENDING", campaign.AdventureEnding, rect.X + 20, ref y, width - 40);

            sb.DrawString(_font, "Press J to close", new Vector2(rect.X + width - 150, rect.Y + height - 30), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        private void DrawAdventureSection(SpriteBatch sb, string title, string content, int x, ref int y, int width)
        {
            sb.DrawString(_font, title + ":", new Vector2(x, y), Color.Orange, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            y += 25;

            string text = string.IsNullOrEmpty(content) ? "No data available." : content;
            string wrapped = WrapText(text, width);
            sb.DrawString(_font, wrapped, new Vector2(x + 10, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            var textSize = _font.MeasureString(wrapped) * 0.7f;
            y += (int)textSize.Y + 5;
        }

        private string WrapText(string text, float maxLineWidth)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return "";
            string[] words = text.Split(' ');
            string result = "";
            string currentLine = "";

            foreach (string word in words)
            {
                if (_font.MeasureString(currentLine + word).X * 0.7f < maxLineWidth)
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

        private void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
        {
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, float thickness)
        {
            float distance = Vector2.Distance(start, end);
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            
            sb.Draw(_pixel, start, null, color, angle, Vector2.Zero, new Vector2(distance, thickness), SpriteEffects.None, 0f);
        }
    }
}
