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
        
        private Vector2 _cameraOffset = Vector2.Zero;
        private float _zoom = 1.0f;
        private int _tileSize = 30;
        
        private Location _selectedLocation = null;
        private (int q, int r)? _selectedHex = null;
        private MouseState _prevMouse;
        private int _prevScrollValue = 0;
        private bool _showAdventureDetails = false;
        public bool TravelOccurred { get; set; } = false;

        private bool _isTraveling = false;
        private float _targetPartyX;
        private float _targetPartyY;
        private const float TravelSpeed = 20f; // Miles per second

        // Caches for procedural map data
        private System.Collections.Generic.Dictionary<(int q, int r, int seed, MapScale scale), BiomeType> _biomeCache = new();
        private System.Collections.Generic.Dictionary<(int q, int r, int seed), System.Collections.Generic.List<Vector2>> _foliageCache = new();
        
        public CampaignMapViewer(SpriteFont font, Texture2D pixel)
        {
            _font = font;
            _pixel = pixel;
        }
        
        public void Update(Campaign campaign, MouseState mouse, KeyboardState kb, KeyboardState prevKb, GameTime gameTime)
        {
            if (campaign == null) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Handle travel movement
            if (_isTraveling)
            {
                Vector2 currentPos = new Vector2(campaign.PartyX, campaign.PartyY);
                Vector2 targetPos = new Vector2(_targetPartyX, _targetPartyY);
                float dist = Vector2.Distance(currentPos, targetPos);
                float moveAmount = TravelSpeed * dt;

                if (dist <= moveAmount)
                {
                    campaign.PartyX = _targetPartyX;
                    campaign.PartyY = _targetPartyY;
                    _isTraveling = false;
                    TravelOccurred = true;
                    System.Console.WriteLine("Arrived at destination.");
                }
                else
                {
                    Vector2 dir = Vector2.Normalize(targetPos - currentPos);
                    campaign.PartyX += dir.X * moveAmount;
                    campaign.PartyY += dir.Y * moveAmount;
                    TravelOccurred = true; // Refresh tactical map if we stop traveling
                }
            }

            var vp = new Rectangle(0, 0, 1280, 720); // Default if device not accessible here, but we should use passed one if possible
            // We'll use a simplified check or pass the viewport if needed
            
            // Pan camera with WASD
            float panSpeed = 5f;
            if (kb.IsKeyDown(Keys.W)) _cameraOffset.Y += panSpeed;
            if (kb.IsKeyDown(Keys.S)) _cameraOffset.Y -= panSpeed;
            if (kb.IsKeyDown(Keys.A)) _cameraOffset.X += panSpeed;
            if (kb.IsKeyDown(Keys.D)) _cameraOffset.X -= panSpeed;
            
            // Zoom with +/-
            if (kb.IsKeyDown(Keys.OemPlus) && !prevKb.IsKeyDown(Keys.OemPlus))
                _zoom = Math.Min(2.0f, _zoom + 0.1f);
            if (kb.IsKeyDown(Keys.OemMinus) && !prevKb.IsKeyDown(Keys.OemMinus))
                _zoom = Math.Max(0.5f, _zoom - 0.1f);
            
            // Zoom with mouse wheel
            int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
            if (scrollDelta != 0)
            {
                _zoom += scrollDelta * 0.001f;
                _zoom = MathHelper.Clamp(_zoom, 0.3f, 3.0f);
                _prevScrollValue = mouse.ScrollWheelValue;
            }
            
            // Switch map scale with 1, 2, 3 keys
            if (kb.IsKeyDown(Keys.D1) && !prevKb.IsKeyDown(Keys.D1))
            {
                campaign.CurrentScale = MapScale.Province;
                System.Console.WriteLine($"Switched to Province scale (1 hex = {Campaign.GetHexSize(MapScale.Province)} mile)");
            }
            if (kb.IsKeyDown(Keys.D2) && !prevKb.IsKeyDown(Keys.D2))
            {
                campaign.CurrentScale = MapScale.Kingdom;
                System.Console.WriteLine($"Switched to Kingdom scale (1 hex = {Campaign.GetHexSize(MapScale.Kingdom)} miles)");
            }
            if (kb.IsKeyDown(Keys.D3) && !prevKb.IsKeyDown(Keys.D3))
            {
                campaign.CurrentScale = MapScale.Continent;
                System.Console.WriteLine($"Switched to Continent scale (1 hex = {Campaign.GetHexSize(MapScale.Continent)} miles)");
            }
            
            // Click to select location or hex
            if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            {
                var center = new Vector2(1280 / 2f, 720 / 2f); // Assuming default 720p for logic

                // Handle UI button clicks first
                var panelRect = new Rectangle(10, 10, 350, 250); // Match DrawInfoPanel
                var travelBtn = new Rectangle(panelRect.X + 20, panelRect.Y + 200, 180, 30);

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
                            var miles = Campaign.AxialToMiles(_selectedHex.Value.q * hexSizeInMiles, _selectedHex.Value.r * hexSizeInMiles);
                            tx = miles.x;
                            ty = miles.y;
                        }

                        _targetPartyX = tx;
                        _targetPartyY = ty;
                        _isTraveling = true;
                        System.Console.WriteLine($"Traveling to ({tx:F1}, {ty:F1}) miles");
                    }
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
                        float size = _tileSize * _zoom;
                        Vector2 mouseRel = mouse.Position.ToVector2() - center - _cameraOffset;

                        float r_click = mouseRel.Y / (1.5f * size);
                        float q_click = (mouseRel.X / (size * (float)Math.Sqrt(3))) - r_click / 2f;

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
        
        public void Draw(SpriteBatch sb, GraphicsDevice device, Campaign campaign)
        {
            if (campaign == null || _font == null) return;
            
            var vp = device.Viewport;
            var center = new Vector2(vp.Width / 2f, vp.Height / 2f);
            
            // Background
            sb.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.9f);
            
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
            
            if (_showAdventureDetails)
            {
                DrawAdventureDetails(sb, vp, campaign);
            }

            // Instructions
            if (_font != null)
            {
                sb.DrawString(_font, "WASD: Pan | Zoom: Wheel | [1][2][3]: Scale | J: Journal | M: Close", new Vector2(10, vp.Height - 30), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
        
        private void DrawScaleIndicator(SpriteBatch sb, Viewport vp, Campaign campaign)
        {
            var panelRect = new Rectangle(vp.Width - 320, 10, 310, 220);
            sb.Draw(_pixel, panelRect, Color.Black * 0.8f);
            DrawBorder(sb, panelRect, Color.Gold, 2);
            
            if (_font == null) return;
            
            int y = panelRect.Y + 10;
            
            sb.DrawString(_font, "Map Scale", new Vector2(panelRect.X + 10, y), Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            y += 25;
            
            // Draw all three scales with current one highlighted
            var scales = new[] { MapScale.Province, MapScale.Kingdom, MapScale.Continent };
            foreach (var scale in scales)
            {
                bool isCurrent = scale == campaign.CurrentScale;
                Color color = isCurrent ? Color.Yellow : Color.Gray;
                
                string scaleName = scale switch
                {
                    MapScale.Province => "[1] Province",
                    MapScale.Kingdom => "[2] Kingdom",
                    MapScale.Continent => "[3] Continent",
                    _ => "Unknown"
                };
                
                int hexSize = Campaign.GetHexSize(scale);
                string scaleText = $"{scaleName}: 1 hex = {hexSize} mile{(hexSize > 1 ? "s" : "")}";
                
                sb.DrawString(_font, scaleText, new Vector2(panelRect.X + 15, y), color, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                y += 22;
            }
            
            y += 10;
            
            // Scale description
            string description = campaign.CurrentScale switch
            {
                MapScale.Province => "Detailed local exploration",
                MapScale.Kingdom => "Regional travel overview",
                MapScale.Continent => "Continental geography",
                _ => ""
            };
            
            sb.DrawString(_font, description, new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            y += 25;
            
            // Show what's visible at current scale
            sb.DrawString(_font, "Visible at this scale:", new Vector2(panelRect.X + 10, y), Color.Cyan, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
            y += 18;
            
            var visibleTypes = GetVisibleSettlementTypes(campaign.CurrentScale);
            foreach (var type in visibleTypes)
            {
                string typeName = type switch
                {
                    SettlementType.Hamlet => "Hamlets",
                    SettlementType.Village => "Villages",
                    SettlementType.Town => "Towns",
                    SettlementType.City => "Cities",
                    SettlementType.Metropolis => "Metropolises",
                    SettlementType.Fort => "Forts",
                    SettlementType.Castle => "Castles",
                    SettlementType.Monastery => "Monasteries",
                    SettlementType.Dungeon => "Dungeons",
                    SettlementType.Wilderness => "Wilderness",
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
            // Calculate which hexes are visible based on camera offset
            float size = _tileSize * _zoom;
            float focusX = -_cameraOffset.X;
            float focusY = -_cameraOffset.Y;
            
            // Reverse of axial to cartesian conversion
            float r_focus = focusY / (1.5f * size);
            float q_focus = (focusX / (size * (float)Math.Sqrt(3))) - r_focus / 2f;

            int iq_focus = (int)Math.Round(q_focus);
            int ir_focus = (int)Math.Round(r_focus);

            // Grid range depends on zoom and screen size
            int gridRangeQ = (int)(center.X / (size * (float)Math.Sqrt(3))) + 2;
            int gridRangeR = (int)(center.Y / (size * 1.5f)) + 2;

            int hexSizeInMiles = Campaign.GetHexSize(campaign.CurrentScale);

            for (int q = iq_focus - gridRangeQ; q <= iq_focus + gridRangeQ; q++)
            {
                for (int r = ir_focus - gridRangeR; r <= ir_focus + gridRangeR; r++)
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
                    Color biomeColor = WorldGenerator.GetBiomeColor(biome);

                    // Fill hexagon with biome color
                    DrawHexagonFill(sb, pos, _tileSize * _zoom, biomeColor * 0.6f);

                    // Draw procedural foliage if biome supports it
                    DrawProceduralFoliage(sb, pos, biome, q, r, campaign.Seed);

                    // Draw outline
                    DrawHexagon(sb, pos, _tileSize * _zoom, Color.Black * 0.2f);
                }
            }
        }

        private void DrawProceduralFoliage(SpriteBatch sb, Vector2 center, BiomeType biome, int q, int r, int seed)
        {
            // Only draw foliage for certain biomes and if zoomed in enough
            if (_zoom < 0.4f) return;

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
            Color foliageColor = new Color(30, 60, 20) * 0.5f;
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
            // Approximate hexagon fill with three rectangles to minimize overlap artifacts with alpha
            // and better fit the hexagonal shape.

            // Central rectangle
            int cw = (int)(size * 1.732f);
            int ch = (int)(size);
            sb.Draw(_pixel, new Rectangle((int)center.X - cw / 2, (int)center.Y - ch / 2, cw, ch), color);

            // Top rectangle
            int tw = (int)(size * 0.866f);
            int th = (int)(size * 0.5f);
            sb.Draw(_pixel, new Rectangle((int)center.X - tw / 2, (int)center.Y - (int)(size), tw, th), color);

            // Bottom rectangle
            sb.Draw(_pixel, new Rectangle((int)center.X - tw / 2, (int)center.Y + (int)(size * 0.5f), tw, th), color);
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
                sb.Draw(_pixel, new Rectangle((int)pos.X - size/2, (int)pos.Y - size/2, size, size), locationColor);
                
                // Draw border for cities and metropolises
                if (location.Type == SettlementType.City || location.Type == SettlementType.Metropolis)
                {
                    DrawBorder(sb, new Rectangle((int)pos.X - size/2, (int)pos.Y - size/2, size, size), Color.White * 0.7f, 1);
                }
            }
            
            // Location name (only show if zoomed in enough or if it's an important location)
            if (_font != null)
            {
                bool showName = _zoom > 0.7f || location.Type == SettlementType.City || location.Type == SettlementType.Metropolis || location.IsHomeBase;
                
                if (showName)
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
                    sb.DrawString(_font, location.Name, namePos + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
                    // Draw text
                    sb.DrawString(_font, location.Name, namePos, Color.White, 0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
                }
            }
        }
        
        private void DrawInfoPanel(SpriteBatch sb, Viewport vp, Campaign campaign)
        {
            var panelRect = new Rectangle(10, 10, 350, 250);
            sb.Draw(_pixel, panelRect, Color.Black * 0.8f);
            DrawBorder(sb, panelRect, Color.Gold, 2);
            
            if (_font == null) return;
            
            int y = panelRect.Y + 10;
            
            sb.DrawString(_font, campaign.Name, new Vector2(panelRect.X + 10, y), Color.Yellow, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            y += 25;
            
            sb.DrawString(_font, $"Home Base: {campaign.HomeBase.Name}", new Vector2(panelRect.X + 10, y), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 20;
            
            // Show location count for current scale
            var visibleLocations = campaign.GetLocationsAtScale(campaign.CurrentScale);
            sb.DrawString(_font, $"Locations (visible): {visibleLocations.Count} / {campaign.AllLocations.Count}", new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 20;
            
            sb.DrawString(_font, $"Session: {campaign.SessionCount}", new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 20;
            
            // Current scale info
            string scaleName = campaign.CurrentScale switch
            {
                MapScale.Province => "Province",
                MapScale.Kingdom => "Kingdom", 
                MapScale.Continent => "Continent",
                _ => "Unknown"
            };
            int hexSize = Campaign.GetHexSize(campaign.CurrentScale);
            sb.DrawString(_font, $"Scale: {scaleName} ({hexSize}mi/hex)", new Vector2(panelRect.X + 10, y), Color.Cyan, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 25;
            
            if (!string.IsNullOrEmpty(campaign.CurrentObjective))
            {
                sb.DrawString(_font, "Objective:", new Vector2(panelRect.X + 10, y), Color.Orange, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                y += 18;
                
                // Wrap objective text
                string wrapped = WrapText(campaign.CurrentObjective, 330);
                sb.DrawString(_font, wrapped, new Vector2(panelRect.X + 10, y), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }

            if (_selectedLocation != null || _selectedHex != null)
            {
                y += 18;
                string selectedName = _selectedLocation != null ? _selectedLocation.Name : $"Hex ({_selectedHex.Value.q}, {_selectedHex.Value.r})";
                sb.DrawString(_font, $"Selected: {selectedName}", new Vector2(panelRect.X + 10, y), Color.Yellow, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

                if (_selectedLocation == null && _selectedHex.HasValue)
                {
                    y += 18;
                    int hexSize = Campaign.GetHexSize(campaign.CurrentScale);
                    var (xm, ym) = Campaign.AxialToMiles(_selectedHex.Value.q * hexSize, _selectedHex.Value.r * hexSize);
                    var biome = WorldGenerator.GetBiome(xm, ym, campaign.Seed);
                    sb.DrawString(_font, $"Terrain: {biome}", new Vector2(panelRect.X + 10, y), Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }

                y += 25;

                // Travel / Cancel Button
                var travelBtn = new Rectangle(panelRect.X + 20, panelRect.Y + 200, 180, 30);
                var mouse = Mouse.GetState();
                bool isHovered = travelBtn.Contains(mouse.Position);

                Color btnColor = _isTraveling ? Color.DarkRed : Color.Green;
                if (isHovered) btnColor = _isTraveling ? Color.Red : Color.DarkGreen;

                sb.Draw(_pixel, travelBtn, btnColor);
                string btnLabel = _isTraveling ? "Annuler le voyage" : "Voyager ici";
                sb.DrawString(_font, btnLabel, new Vector2(travelBtn.X + 10, travelBtn.Y + 5), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
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
                sb.DrawString(_font, "GROUPE", pos + new Vector2(-20, -30 * _zoom), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
        
        private Vector2 HexToScreen(float q, float r, Vector2 center)
        {
            // Convert axial hex coordinates (q, r) to screen position (pointy-top hexagons)
            float size = _tileSize * _zoom;
            
            float screenX = size * ((float)Math.Sqrt(3) * q + (float)Math.Sqrt(3) / 2f * r);
            float screenY = size * (1.5f * r);
            
            return center + _cameraOffset + new Vector2(screenX, screenY);
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
