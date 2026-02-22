using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DND
{
    /// <summary>
    /// UI for creating a new campaign following DMG guidance:
    /// 1. Choose creation mode (Random or Custom)
    /// 2. Create a home base
    /// 3. Create a local region (1 mile around home base)
    /// 4. Create a starting dungeon
    /// </summary>
    public class CampaignCreation
    {
        private SpriteFont _font;
        private Texture2D _pixel;
        
        private int _step = -1; // -1=Mode selection, 0=Name, 1=HomeBase, 2=Region, 3=Hook, 4=Middle, 5=Ending
        
        // Campaign data
        private bool _isRandomMode = false;
        private string _campaignName = "";
        private string _homeBaseName = "";
        private SettlementType _homeBaseType = SettlementType.Village;
        private string _regionTerrain = "Forest";
        private string _adventureHook = "";
        private string _adventureMiddle = "";
        private string _adventureEnding = "";
        
        private int _selectedModeIndex = 0; // 0=Random, 1=Custom
        private int _selectedTypeIndex = 2; // Default to Village
        private int _selectedTerrainIndex = 0;
        
        private readonly string[] _creationModes = new[]
        {
            "Random", "Custom"
        };
        
        private readonly string[] _settlementTypes = new[]
        {
            "Hamlet", "Village", "Town", "City", "Fort", "Castle", "Monastery"
        };
        
        private readonly string[] _terrainTypes = new[]
        {
            "Forest", "Hills", "Mountains", "Plains", "Swamp", "Desert", "Coast"
        };
        
        private readonly string[] _adventureHooks = new[]
        {
            "Monsters attack on the road",
            "An assassin makes an attempt on your lives",
            "A dragon shows up at the city gates",
            "You stumble onto something you're not meant to see",
            "Custom hook"
        };
        
        private readonly string[] _adventureMiddles = new[]
        {
            "Discover secrets that reveal new goals",
            "The rumors of treasure were a trick to lure you into a death trap",
            "The spy you're hunting is actually working for the monarch",
            "Thwart your adversaries while they try to carry out their nefarious plans",
            "Custom middle"
        };

        private readonly string[] _adventureEndings = new[]
        {
            "A climax where the fate of the world hangs in the balance",
            "A resolution that hinges entirely on your previous choices",
            "A resolution that leaves some unfinished business for the future",
            "A peak of tension where you must decide the ultimate outcome",
            "Custom ending"
        };

        private readonly string[] _extraSettlementNames = new[]
        {
            "Oakhaven", "Willow Creek", "Ironforge", "Stonehill", "Meadowere",
            "Riverrun", "Shadowglen", "Greenhill", "Stonebridge", "Raven's Watch",
            "Silverwood", "Goldmeadow", "Crossroad Inn", "Winterhaven", "Thistle-wick"
        };

        private readonly string[] _extraDungeonNames = new[]
        {
            "Shadow Crypt", "Ancient Mines", "Cursed Grotto", "Dragon's Lair",
            "Whispering Caverns", "Lost Sanctum", "The Obsidian Vault", "Gloom-rot Cave",
            "Silent Tombs", "Serpent's Burrow", "Frozen Outpost", "Bandit's Hideout",
            "Ruined Temple of the Sun"
        };

        private int _selectedHookIndex = 0;
        private int _selectedMiddleIndex = 0;
        private int _selectedEndingIndex = 0;
        
        private bool _isTyping = false;
        private string _typingField = "";
        
        private MouseState _prevMouse;
        
        public CampaignCreation(SpriteFont font, Texture2D pixel)
        {
            _font = font;
            _pixel = pixel;
            _prevMouse = Mouse.GetState();
        }
        
        public void Reset()
        {
            _step = -1;
            _isRandomMode = false;
            _campaignName = "";
            _homeBaseName = "";
            _homeBaseType = SettlementType.Village;
            _regionTerrain = "Forest";
            _adventureHook = "";
            _adventureMiddle = "";
            _adventureEnding = "";
            _selectedModeIndex = 0;
            _selectedTypeIndex = 2;
            _selectedTerrainIndex = 0;
            _selectedHookIndex = 0;
            _selectedMiddleIndex = 0;
            _selectedEndingIndex = 0;
            _isTyping = false;
        }
        
        /// <summary>
        /// Update campaign creation UI.
        /// Returns false if user cancels, true if continuing, and sets outCampaign if complete.
        /// </summary>
        public bool Update(GameTime gameTime, GraphicsDevice graphicsDevice, KeyboardState kb, KeyboardState prevKb, out Campaign outCampaign)
        {
            outCampaign = null!;
            var mouse = Mouse.GetState();
            var vp = graphicsDevice.Viewport;
            
            // Escape to cancel
            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            {
                return false;
            }
            
            // Handle typing
            if (_isTyping)
            {
                bool wasTyping = _isTyping;
                HandleTyping(kb, prevKb);
                
                // If we just finished typing the ending, we might want to complete the campaign
                if (wasTyping && !_isTyping && _step == 5 && !string.IsNullOrEmpty(_adventureEnding))
                {
                    outCampaign = CreateFinalCampaign();
                    return true;
                }

                // Handle clicking input boxes while typing
                var menuWidth = 600;
                var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - 500) / 2, menuWidth, 500);
                
                if (_step == 0)
                {
                    var inputRect = new Rectangle(menuRect.X + 20, menuRect.Y + 90, menuRect.Width - 40, 40);
                    if (inputRect.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    {
                        _isTyping = true;
                        _typingField = "campaign";
                    }
                }
                else if (_step == 1)
                {
                    var inputRect = new Rectangle(menuRect.X + 20, menuRect.Y + 80, menuRect.Width - 40, 40);
                    if (inputRect.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    {
                        _isTyping = true;
                        _typingField = "homebase";
                    }
                }
                
                _prevMouse = mouse;
                return true;
            }
            
            // MODE SELECTION
            if (_step == -1)
            {
                // Keyboard navigation
                if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                    _selectedModeIndex = Math.Max(0, _selectedModeIndex - 1);
                if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                    _selectedModeIndex = Math.Min(_creationModes.Length - 1, _selectedModeIndex + 1);
                
                // Mouse navigation
                int menuWidth = 600;
                var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - 400) / 2, menuWidth, 400);
                int y = menuRect.Y + 100;
                
                for (int i = 0; i < _creationModes.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect.X + 50, y, menuRect.Width - 100, 60);
                    
                    if (itemRect.Contains(mouse.Position))
                    {
                        _selectedModeIndex = i;
                        
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            _isRandomMode = (i == 0);
                            
                            if (_isRandomMode)
                            {
                                // Generate random campaign
                                GenerateRandomCampaign(out outCampaign);
                                _prevMouse = mouse;
                                return true;
                            }
                            else
                            {
                                _step = 0;
                                _prevMouse = mouse;
                                return true;
                            }
                        }
                    }
                    
                    y += 80;
                }
                
                // Enter to proceed
                if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
                {
                    _isRandomMode = (_selectedModeIndex == 0);
                    
                    if (_isRandomMode)
                    {
                        // Generate random campaign
                        GenerateRandomCampaign(out outCampaign);
                        _prevMouse = mouse;
                        return true;
                    }
                    else
                    {
                        _step = 0;
                    }
                }
                
                _prevMouse = mouse;
                return true;
            }
            
            // Navigation
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
            {
                if (_step == 1)
                    _selectedTypeIndex = Math.Max(0, _selectedTypeIndex - 1);
                else if (_step == 2)
                    _selectedTerrainIndex = Math.Max(0, _selectedTerrainIndex - 1);
                else if (_step == 3)
                    _selectedHookIndex = Math.Max(0, _selectedHookIndex - 1);
                else if (_step == 4)
                    _selectedMiddleIndex = Math.Max(0, _selectedMiddleIndex - 1);
                else if (_step == 5)
                    _selectedEndingIndex = Math.Max(0, _selectedEndingIndex - 1);
            }
            
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
            {
                if (_step == 1)
                    _selectedTypeIndex = Math.Min(_settlementTypes.Length - 1, _selectedTypeIndex + 1);
                else if (_step == 2)
                    _selectedTerrainIndex = Math.Min(_terrainTypes.Length - 1, _selectedTerrainIndex + 1);
                else if (_step == 3)
                    _selectedHookIndex = Math.Min(_adventureHooks.Length - 1, _selectedHookIndex + 1);
                else if (_step == 4)
                    _selectedMiddleIndex = Math.Min(_adventureMiddles.Length - 1, _selectedMiddleIndex + 1);
                else if (_step == 5)
                    _selectedEndingIndex = Math.Min(_adventureEndings.Length - 1, _selectedEndingIndex + 1);
            }
            
            // Mouse navigation for each step
            var menuWidth2 = 600;
            var menuRect2 = new Rectangle((vp.Width - menuWidth2) / 2, (vp.Height - 500) / 2, menuWidth2, 500);
            
            // Step 0: Click on input box
            if (_step == 0)
            {
                var inputRect = new Rectangle(menuRect2.X + 20, menuRect2.Y + 90, menuRect2.Width - 40, 40);
                if (inputRect.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    _isTyping = true;
                    _typingField = "campaign";
                }
            }
            
            // Step 1: Click on input box or settlement type
            if (_step == 1)
            {
                var inputRect = new Rectangle(menuRect2.X + 20, menuRect2.Y + 80, menuRect2.Width - 40, 40);
                if (inputRect.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    _isTyping = true;
                    _typingField = "homebase";
                }
                
                int y = menuRect2.Y + 200;
                for (int i = 0; i < _settlementTypes.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect2.X + 40, y, menuRect2.Width - 80, 35);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _selectedTypeIndex = i;
                        
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            if (!string.IsNullOrEmpty(_homeBaseName))
                            {
                                _homeBaseType = ParseSettlementType(_settlementTypes[_selectedTypeIndex]);
                                _step = 2;
                            }
                        }
                    }
                    y += 37;
                }
            }
            
            // Step 2: Click on terrain type
            if (_step == 2)
            {
                int y = menuRect2.Y + 110;
                for (int i = 0; i < _terrainTypes.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect2.X + 40, y, menuRect2.Width - 80, 40);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _selectedTerrainIndex = i;
                        
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            _regionTerrain = _terrainTypes[_selectedTerrainIndex];
                            _step = 3;
                        }
                    }
                    y += 42;
                }
            }
            
            // Step 3: Click on adventure hook
            if (_step == 3)
            {
                int y = menuRect2.Y + 110;
                for (int i = 0; i < _adventureHooks.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect2.X + 40, y, menuRect2.Width - 80, 40);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _selectedHookIndex = i;
                        
                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            _adventureHook = _adventureHooks[_selectedHookIndex];
                            _step = 4;
                        }
                    }
                    y += 42;
                }
            }

            // Step 4: Click on adventure middle
            if (_step == 4)
            {
                int y = menuRect2.Y + 110;
                for (int i = 0; i < _adventureMiddles.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect2.X + 40, y, menuRect2.Width - 80, 40);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _selectedMiddleIndex = i;

                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            _adventureMiddle = _adventureMiddles[_selectedMiddleIndex];
                            _step = 5;
                        }
                    }
                    y += 42;
                }
            }

            // Step 5: Click on adventure ending
            if (_step == 5)
            {
                int y = menuRect2.Y + 110;
                for (int i = 0; i < _adventureEndings.Length; i++)
                {
                    var itemRect = new Rectangle(menuRect2.X + 40, y, menuRect2.Width - 80, 40);
                    if (itemRect.Contains(mouse.Position))
                    {
                        _selectedEndingIndex = i;

                        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                        {
                            _adventureEnding = _adventureEndings[_selectedEndingIndex];
                            
                            // Create campaign!
                            outCampaign = CreateFinalCampaign();
                            
                            _prevMouse = mouse;
                            return true;
                        }
                    }
                    y += 42;
                }
            }
            
            // Enter to proceed
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            {
                if (_step == 0)
                {
                    // Start typing campaign name
                    if (string.IsNullOrEmpty(_campaignName))
                    {
                        _isTyping = true;
                        _typingField = "campaign";
                    }
                    else
                    {
                        _step = 1;
                    }
                }
                else if (_step == 1)
                {
                    // Start typing home base name if not set, else proceed
                    if (string.IsNullOrEmpty(_homeBaseName))
                    {
                        _isTyping = true;
                        _typingField = "homebase";
                    }
                    else
                    {
                        _homeBaseType = ParseSettlementType(_settlementTypes[_selectedTypeIndex]);
                        _step = 2;
                    }
                }
                else if (_step == 2)
                {
                    _regionTerrain = _terrainTypes[_selectedTerrainIndex];
                    _step = 3;
                }
                else if (_step == 3)
                {
                    if (_selectedHookIndex == _adventureHooks.Length - 1)
                    {
                        _isTyping = true;
                        _typingField = "hook";
                    }
                    else
                    {
                        _adventureHook = _adventureHooks[_selectedHookIndex];
                        _step = 4;
                    }
                }
                else if (_step == 4)
                {
                    if (_selectedMiddleIndex == _adventureMiddles.Length - 1)
                    {
                        _isTyping = true;
                        _typingField = "middle";
                    }
                    else
                    {
                        _adventureMiddle = _adventureMiddles[_selectedMiddleIndex];
                        _step = 5;
                    }
                }
                else if (_step == 5)
                {
                    if (_selectedEndingIndex == _adventureEndings.Length - 1)
                    {
                        _isTyping = true;
                        _typingField = "ending";
                    }
                    else
                    {
                        _adventureEnding = _adventureEndings[_selectedEndingIndex];
                        outCampaign = CreateFinalCampaign();
                        return true;
                    }
                }
            }
            
            _prevMouse = mouse;
            return true;
        }
        
        private Campaign CreateFinalCampaign()
        {
            var campaign = Campaign.CreateStartingCampaign(_campaignName, _homeBaseName, _homeBaseType);
            campaign.LocalRegion.Terrain = _regionTerrain;
            campaign.AdventureHook = _adventureHook;
            campaign.AdventureMiddle = _adventureMiddle;
            campaign.AdventureEnding = _adventureEnding;
            campaign.CurrentObjective = _adventureHook; // Set initial objective to the hook

            var rand = new Random();

            // Add 1-3 extra settlements and 2-4 dungeons (DMG guidance: Start Small)
            int extraSettlementsCount = rand.Next(1, 4);
            int dungeonsCount = rand.Next(2, 5);
            int totalExtra = extraSettlementsCount + dungeonsCount;

            // Homogeneous distribution around home base
            float angleStep = 360f / totalExtra;
            float currentAngle = (float)rand.NextDouble() * 360f;

            var extraLocations = new List<(string Name, SettlementType Type)>();

            // Settlements
            var availSettlementNames = new List<string>(_extraSettlementNames);
            for (int i = 0; i < extraSettlementsCount; i++)
            {
                int idx = rand.Next(availSettlementNames.Count);
                string name = availSettlementNames[idx];
                availSettlementNames.RemoveAt(idx);

                SettlementType type = rand.Next(10) switch
                {
                    0 => SettlementType.Town,
                    1 or 2 => SettlementType.Hamlet,
                    _ => SettlementType.Village
                };
                extraLocations.Add((name, type));
            }

            // Dungeons
            var availDungeonNames = new List<string>(_extraDungeonNames);
            for (int i = 0; i < dungeonsCount; i++)
            {
                int idx = rand.Next(availDungeonNames.Count);
                string name = availDungeonNames[idx];
                availDungeonNames.RemoveAt(idx);
                extraLocations.Add((name, SettlementType.Dungeon));
            }

            // Shuffle
            for (int i = extraLocations.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                var temp = extraLocations[i];
                extraLocations[i] = extraLocations[j];
                extraLocations[j] = temp;
            }

            // Place them
            for (int i = 0; i < extraLocations.Count; i++)
            {
                var locData = extraLocations[i];
                float angleRad = MathHelper.ToRadians(currentAngle + (float)rand.NextDouble() * (angleStep * 0.4f) - (angleStep * 0.2f));
                float distance = (float)rand.NextDouble() * 15f + 10f; // Between 10 and 25 miles

                int x = (int)(Math.Cos(angleRad) * distance);
                int y = (int)(Math.Sin(angleRad) * distance);

                campaign.AddLocation(new Location
                {
                    Name = locData.Name,
                    Type = locData.Type,
                    X = x,
                    Y = y,
                    Description = locData.Type == SettlementType.Dungeon ? "A mysterious location awaits exploration." : "A notable settlement in the region.",
                    IsDiscovered = true
                });

                currentAngle += angleStep;
            }

            return campaign;
        }

        private void GenerateRandomCampaign(out Campaign campaign)
        {
            var rand = new Random();
            
            // Random campaign name
            var adjectives = new[] { "Lost", "Ancient", "Forgotten", "Dark", "Sacred", "Cursed", "Hidden", "Frozen" };
            var nouns = new[] { "Dungeon", "Temple", "Mines", "Crypt", "Tower", "Vale", "Peaks", "Realm" };
            _campaignName = $"{adjectives[rand.Next(adjectives.Length)]} {nouns[rand.Next(nouns.Length)]}";
            
            // Random home base
            var homeBaseNames = new[] { "Phandalin", "Greenest", "Neverwinter", "Waterdeep", "Baldur's Gate", "Silverymoon", "Bryn Shander" };
            _homeBaseName = homeBaseNames[rand.Next(homeBaseNames.Length)];
            _homeBaseType = (SettlementType)rand.Next(0, 7);
            
            // Random terrain
            _regionTerrain = _terrainTypes[rand.Next(_terrainTypes.Length)];
            
            // Random adventure parts
            _adventureHook = _adventureHooks[rand.Next(_adventureHooks.Length - 1)]; // Exclude "Custom"
            _adventureMiddle = _adventureMiddles[rand.Next(_adventureMiddles.Length - 1)];
            _adventureEnding = _adventureEndings[rand.Next(_adventureEndings.Length - 1)];
            
            // Create campaign
            campaign = CreateFinalCampaign();
        }
        
        private void HandleTyping(KeyboardState kb, KeyboardState prevKb)
        {
            // Enter to finish typing
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            {
                _isTyping = false;
                
                if (_typingField == "campaign" && !string.IsNullOrEmpty(_campaignName))
                    _step = 1;
                else if (_typingField == "homebase" && !string.IsNullOrEmpty(_homeBaseName))
                {
                    _homeBaseType = ParseSettlementType(_settlementTypes[_selectedTypeIndex]);
                    _step = 2;
                }
                else if (_typingField == "hook" && !string.IsNullOrEmpty(_adventureHook))
                    _step = 4;
                else if (_typingField == "middle" && !string.IsNullOrEmpty(_adventureMiddle))
                    _step = 5;
                else if (_typingField == "ending" && !string.IsNullOrEmpty(_adventureEnding))
                {
                    // Created through separate logic or by calling CreateFinalCampaign later
                }
                
                return;
            }
            
            // Backspace
            if (kb.IsKeyDown(Keys.Back) && !prevKb.IsKeyDown(Keys.Back))
            {
                if (_typingField == "campaign" && _campaignName.Length > 0)
                    _campaignName = _campaignName.Substring(0, _campaignName.Length - 1);
                else if (_typingField == "homebase" && _homeBaseName.Length > 0)
                    _homeBaseName = _homeBaseName.Substring(0, _homeBaseName.Length - 1);
                else if (_typingField == "hook" && _adventureHook.Length > 0)
                    _adventureHook = _adventureHook.Substring(0, _adventureHook.Length - 1);
                else if (_typingField == "middle" && _adventureMiddle.Length > 0)
                    _adventureMiddle = _adventureMiddle.Substring(0, _adventureMiddle.Length - 1);
                else if (_typingField == "ending" && _adventureEnding.Length > 0)
                    _adventureEnding = _adventureEnding.Substring(0, _adventureEnding.Length - 1);
                return;
            }
            
            // Type characters
            var keys = kb.GetPressedKeys();
            foreach (var key in keys)
            {
                if (!prevKb.IsKeyDown(key))
                {
                    char c = GetCharFromKey(key, kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift));
                    if (c != '\0')
                    {
                        if (_typingField == "campaign" && _campaignName.Length < 30)
                            _campaignName += c;
                        else if (_typingField == "homebase" && _homeBaseName.Length < 30)
                            _homeBaseName += c;
                        else if (_typingField == "hook" && _adventureHook.Length < 100)
                            _adventureHook += c;
                        else if (_typingField == "middle" && _adventureMiddle.Length < 100)
                            _adventureMiddle += c;
                        else if (_typingField == "ending" && _adventureEnding.Length < 100)
                            _adventureEnding += c;
                    }
                }
            }
        }
        
        private char GetCharFromKey(Keys key, bool shift)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)('a' + (key - Keys.A));
                return shift ? char.ToUpper(c) : c;
            }
            
            if (key >= Keys.D0 && key <= Keys.D9)
                return (char)('0' + (key - Keys.D0));
            
            if (key == Keys.Space) return ' ';
            
            return '\0';
        }
        
        private SettlementType ParseSettlementType(string name)
        {
            return name switch
            {
                "Hamlet" => SettlementType.Hamlet,
                "Village" => SettlementType.Village,
                "Town" => SettlementType.Town,
                "City" => SettlementType.City,
                "Fort" => SettlementType.Fort,
                "Castle" => SettlementType.Castle,
                "Monastery" => SettlementType.Monastery,
                _ => SettlementType.Village
            };
        }
        
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            if (_font == null) return;
            
            var vp = graphicsDevice.Viewport;
            
            // Background
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);
            
            // MODE SELECTION
            if (_step == -1)
            {
                DrawModeSelection(spriteBatch, vp);
                return;
            }
            
            // CAMPAIGN CREATION STEPS
            int menuWidth = 600;
            int menuHeight = 500;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);
            
            spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);
            
            int y = menuRect.Y + 20;
            
            // Title
            string title = "Create Campaign - " + (_step switch
            {
                0 => "Campaign Name",
                1 => "Home Base",
                2 => "Local Region",
                3 => "Adventure Hook",
                4 => "Adventure Middle",
                5 => "Adventure Ending",
                _ => ""
            });
            
            var titleSize = _font.MeasureString(title);
            spriteBatch.DrawString(_font, title, new Vector2(menuRect.X + (menuWidth - titleSize.X) / 2, y), Color.Yellow, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
            y += 50;
            
            // Step-specific content
            if (_step == 0)
            {
                DrawStep0_CampaignName(spriteBatch, menuRect, ref y);
            }
            else if (_step == 1)
            {
                DrawStep1_HomeBase(spriteBatch, menuRect, ref y);
            }
            else if (_step == 2)
            {
                DrawStep2_Region(spriteBatch, menuRect, ref y);
            }
            else if (_step == 3)
            {
                DrawStep3_Hook(spriteBatch, menuRect, ref y);
            }
            else if (_step == 4)
            {
                DrawStep4_Middle(spriteBatch, menuRect, ref y);
            }
            else if (_step == 5)
            {
                DrawStep5_Ending(spriteBatch, menuRect, ref y);
            }
            
            // Instructions
            string instructions = _isTyping ? "Type name, then press Enter" : "Click or use Arrow keys | Enter to continue | Esc to cancel";
            var instSize = _font.MeasureString(instructions);
            spriteBatch.DrawString(_font, instructions, new Vector2(menuRect.X + (menuWidth - instSize.X) / 2, menuRect.Y + menuHeight - 30), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
        
        private void DrawModeSelection(SpriteBatch sb, Viewport vp)
        {
            int menuWidth = 600;
            int menuHeight = 400;
            var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);
            
            sb.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);
            
            // Title
            string title = "Campaign Creation";
            var titleSize = _font.MeasureString(title);
            sb.DrawString(_font, title, new Vector2(menuRect.X + (menuWidth - titleSize.X) / 2, menuRect.Y + 20), Color.Yellow, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
            
            int y = menuRect.Y + 100;
            
            // Mode options
            for (int i = 0; i < _creationModes.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + 50, y, menuWidth - 100, 60);
                var mouse = Mouse.GetState();
                bool isHovered = itemRect.Contains(mouse.Position);
                bool isSelected = i == _selectedModeIndex;
                
                var bgColor = isSelected ? Color.LightBlue * 0.6f : (isHovered ? Color.Gray * 0.4f : Color.Gray * 0.2f);
                sb.Draw(_pixel, itemRect, bgColor);
                
                // Border
                DrawBorder(sb, itemRect, isSelected ? Color.Yellow : Color.White * 0.5f, 2);
                
                // Text
                var text = _creationModes[i];
                var textSize = _font.MeasureString(text);
                var textPos = new Vector2(itemRect.X + (itemRect.Width - textSize.X) / 2, itemRect.Y + (itemRect.Height - textSize.Y) / 2);
                sb.DrawString(_font, text, textPos, Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
                
                y += 80;
            }
            
            // Descriptions
            y = menuRect.Y + 280;
            
            if (_selectedModeIndex == 0)
            {
                sb.DrawString(_font, "Random: Generate a complete campaign instantly", new Vector2(menuRect.X + 50, y), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                y += 25;
                sb.DrawString(_font, "Perfect for quick play and testing!", new Vector2(menuRect.X + 50, y), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
            else
            {
                sb.DrawString(_font, "Custom: Create your campaign step by step", new Vector2(menuRect.X + 50, y), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                y += 25;
                sb.DrawString(_font, "Full control over every detail!", new Vector2(menuRect.X + 50, y), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
            
            // Instructions
            string instructions = "Click to select | Enter to confirm | Esc to cancel";
            var instSize = _font.MeasureString(instructions);
            sb.DrawString(_font, instructions, new Vector2(menuRect.X + (menuWidth - instSize.X) / 2, menuRect.Y + menuHeight - 30), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
        
        private void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
        {
            // Top
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            sb.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
        
        private void DrawStep0_CampaignName(SpriteBatch sb, Rectangle menuRect, ref int y)
        {
            string prompt = "Enter a name for your campaign:";
            sb.DrawString(_font, prompt, new Vector2(menuRect.X + 20, y), Color.White);
            y += 40;
            
            // Input box
            var inputRect = new Rectangle(menuRect.X + 20, y, menuRect.Width - 40, 40);
            var mouse = Mouse.GetState();
            bool isHovered = inputRect.Contains(mouse.Position);
            
            sb.Draw(_pixel, inputRect, _isTyping ? Color.LightYellow * 0.3f : (isHovered ? Color.Gray * 0.4f : Color.Gray * 0.3f));
            
            // Border for input box
            if (_isTyping || isHovered)
            {
                DrawBorder(sb, inputRect, Color.Yellow, 2);
            }
            
            string displayText = string.IsNullOrEmpty(_campaignName) ? "(Click to type)" : _campaignName;
            if (_isTyping && _campaignName.Length > 0)
                displayText += "_";
            
            sb.DrawString(_font, displayText, new Vector2(inputRect.X + 10, inputRect.Y + 10), Color.White);
            y += 60;
            
            // Info
            sb.DrawString(_font, "This will be the name of your adventure.", new Vector2(menuRect.X + 20, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            y += 30;
            sb.DrawString(_font, "Examples: 'Lost Mine of Phandelver', 'Dragon's Curse'", new Vector2(menuRect.X + 20, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        
        private void DrawStep1_HomeBase(SpriteBatch sb, Rectangle menuRect, ref int y)
        {
            string prompt = "Name your home base:";
            sb.DrawString(_font, prompt, new Vector2(menuRect.X + 20, y), Color.White);
            y += 30;
            
            // Input box
            var inputRect = new Rectangle(menuRect.X + 20, y, menuRect.Width - 40, 40);
            var mouse = Mouse.GetState();
            bool isHovered = inputRect.Contains(mouse.Position);
            
            sb.Draw(_pixel, inputRect, (_isTyping && _typingField == "homebase") ? Color.LightYellow * 0.3f : (isHovered ? Color.Gray * 0.4f : Color.Gray * 0.3f));
            
            // Border for input box
            if ((_isTyping && _typingField == "homebase") || isHovered)
            {
                DrawBorder(sb, inputRect, Color.Yellow, 2);
            }
            
            string displayText = string.IsNullOrEmpty(_homeBaseName) ? "(Click to type)" : _homeBaseName;
            if (_isTyping && _typingField == "homebase" && _homeBaseName.Length > 0)
                displayText += "_";
            
            sb.DrawString(_font, displayText, new Vector2(inputRect.X + 10, inputRect.Y + 10), Color.White);
            y += 60;
            
            // Settlement type
            sb.DrawString(_font, "Choose settlement type:", new Vector2(menuRect.X + 20, y), Color.White);
            y += 30;
            
            var mouse2 = Mouse.GetState();
            for (int i = 0; i < _settlementTypes.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + 40, y, menuRect.Width - 80, 35);
                bool isItemHovered = itemRect.Contains(mouse2.Position);
                
                if (i == _selectedTypeIndex)
                    sb.Draw(_pixel, itemRect, Color.LightBlue * 0.4f);
                else if (isItemHovered)
                    sb.Draw(_pixel, itemRect, Color.LightBlue * 0.2f);
                
                sb.DrawString(_font, _settlementTypes[i], new Vector2(itemRect.X + 10, itemRect.Y + 8), i == _selectedTypeIndex ? Color.Yellow : (isItemHovered ? Color.LightYellow : Color.White));
                y += 37;
            }
        }
        
        private void DrawStep2_Region(SpriteBatch sb, Rectangle menuRect, ref int y)
        {
            string prompt = $"The {_homeBaseName} is in a region of:";
            sb.DrawString(_font, prompt, new Vector2(menuRect.X + 20, y), Color.White);
            y += 40;
            
            var mouse = Mouse.GetState();
            for (int i = 0; i < _terrainTypes.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + 40, y, menuRect.Width - 80, 40);
                bool isHovered = itemRect.Contains(mouse.Position);
                
                if (i == _selectedTerrainIndex)
                    sb.Draw(_pixel, itemRect, Color.LightGreen * 0.4f);
                else if (isHovered)
                    sb.Draw(_pixel, itemRect, Color.LightGreen * 0.2f);
                
                sb.DrawString(_font, _terrainTypes[i], new Vector2(itemRect.X + 10, itemRect.Y + 10), i == _selectedTerrainIndex ? Color.Yellow : (isHovered ? Color.LightYellow : Color.White));
                y += 42;
            }
            
            y += 20;
            sb.DrawString(_font, "This defines the local area around your home base.", new Vector2(menuRect.X + 20, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        
        private void DrawStep3_Hook(SpriteBatch sb, Rectangle menuRect, ref int y)
        {
            if (_isTyping && _typingField == "hook")
            {
                DrawCustomInput(sb, menuRect, ref y, "Enter custom adventure hook:", _adventureHook);
                return;
            }

            string prompt = "Choose your adventure hook:";
            sb.DrawString(_font, prompt, new Vector2(menuRect.X + 20, y), Color.White);
            y += 40;

            var mouse = Mouse.GetState();
            for (int i = 0; i < _adventureHooks.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + 40, y, menuRect.Width - 80, 40);
                bool isHovered = itemRect.Contains(mouse.Position);

                if (i == _selectedHookIndex)
                    sb.Draw(_pixel, itemRect, Color.Orange * 0.4f);
                else if (isHovered)
                    sb.Draw(_pixel, itemRect, Color.Orange * 0.2f);

                sb.DrawString(_font, _adventureHooks[i], new Vector2(itemRect.X + 10, itemRect.Y + 10), i == _selectedHookIndex ? Color.Yellow : (isHovered ? Color.LightYellow : Color.White), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                y += 42;
            }

            y += 20;
            sb.DrawString(_font, "Next: Choose what happens in the middle", new Vector2(menuRect.X + 20, y), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        private void DrawStep4_Middle(SpriteBatch sb, Rectangle menuRect, ref int y)
        {
            if (_isTyping && _typingField == "middle")
            {
                DrawCustomInput(sb, menuRect, ref y, "Enter custom middle events:", _adventureMiddle);
                return;
            }

            string prompt = "What happens during the adventure?";
            sb.DrawString(_font, prompt, new Vector2(menuRect.X + 20, y), Color.White);
            y += 40;

            var mouse = Mouse.GetState();
            for (int i = 0; i < _adventureMiddles.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + 40, y, menuRect.Width - 80, 40);
                bool isHovered = itemRect.Contains(mouse.Position);

                if (i == _selectedMiddleIndex)
                    sb.Draw(_pixel, itemRect, Color.Orange * 0.4f);
                else if (isHovered)
                    sb.Draw(_pixel, itemRect, Color.Orange * 0.2f);

                sb.DrawString(_font, _adventureMiddles[i], new Vector2(itemRect.X + 10, itemRect.Y + 10), i == _selectedMiddleIndex ? Color.Yellow : (isHovered ? Color.LightYellow : Color.White), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                y += 42;
            }

            y += 20;
            sb.DrawString(_font, "Next: Choose the adventure's climax", new Vector2(menuRect.X + 20, y), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        private void DrawStep5_Ending(SpriteBatch sb, Rectangle menuRect, ref int y)
        {
            if (_isTyping && _typingField == "ending")
            {
                DrawCustomInput(sb, menuRect, ref y, "Enter custom adventure ending:", _adventureEnding);
                return;
            }

            string prompt = "How does the adventure end?";
            sb.DrawString(_font, prompt, new Vector2(menuRect.X + 20, y), Color.White);
            y += 40;
            
            var mouse = Mouse.GetState();
            for (int i = 0; i < _adventureEndings.Length; i++)
            {
                var itemRect = new Rectangle(menuRect.X + 40, y, menuRect.Width - 80, 40);
                bool isHovered = itemRect.Contains(mouse.Position);
                
                if (i == _selectedEndingIndex)
                    sb.Draw(_pixel, itemRect, Color.Orange * 0.4f);
                else if (isHovered)
                    sb.Draw(_pixel, itemRect, Color.Orange * 0.2f);
                
                sb.DrawString(_font, _adventureEndings[i], new Vector2(itemRect.X + 10, itemRect.Y + 10), i == _selectedEndingIndex ? Color.Yellow : (isHovered ? Color.LightYellow : Color.White), 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
                y += 42;
            }
            
            y += 20;
            sb.DrawString(_font, "Click to create your campaign!", new Vector2(menuRect.X + 20, y), Color.LightGreen, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        private void DrawCustomInput(SpriteBatch sb, Rectangle menuRect, ref int y, string prompt, string value)
        {
            sb.DrawString(_font, prompt, new Vector2(menuRect.X + 20, y), Color.White);
            y += 40;

            var inputRect = new Rectangle(menuRect.X + 20, y, menuRect.Width - 40, 100);
            sb.Draw(_pixel, inputRect, Color.LightYellow * 0.15f);
            DrawBorder(sb, inputRect, Color.Yellow, 2);

            string wrappedText = WrapText(_font, value + "_", inputRect.Width - 20);
            sb.DrawString(_font, wrappedText, new Vector2(inputRect.X + 10, inputRect.Y + 10), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            y += 120;
            sb.DrawString(_font, "Press Enter to confirm custom text", new Vector2(menuRect.X + 20, y), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        private string WrapText(SpriteFont font, string text, float maxLineWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] words = text.Split(' ');
            string result = "";
            string currentLine = "";

            foreach (string word in words)
            {
                if (font.MeasureString(currentLine + word).X < maxLineWidth)
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
}
