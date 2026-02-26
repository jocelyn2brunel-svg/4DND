#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _4DND;

public class CharacterSheet
{
    private SpriteFont _font;
    private Texture2D _pixel;
    private float _scrollOffset = 0f;
    private int _prevScrollValue = 0;
    private bool _isScrollInitialized;
    private const int Margin = 20;
    private const int ScrollbarWidth = 20;
    private const int CloseButtonWidth = 120;
    private const int CloseButtonHeight = 36;
    private Point _mousePosition;
    private string? _hoverTooltip;
    private HashSet<char> _supportedChars;
    private MouseState _prevMouseState;
    private readonly List<(Rectangle Rect, ItemInstance Item, bool IsEquipped, bool IsEquippable)> _inventoryItemRects = new();
    private readonly List<(Rectangle Rect, ItemInstance? Item, Spell? Spell, bool IsOffhand)> _attackEntryRects = new();
    private bool _showItemContextMenu;
    private Rectangle _itemContextMenuRect;
    private ItemInstance? _contextItem;
    private bool _contextItemIsEquipped;
    private bool _contextItemIsEquippable;
    private bool _contextItemIsLight;
    private string? _inspectWeaponText;
    private Rectangle _inspectPopupRect;
    private Rectangle _grappleActionRect;
    private Rectangle _shoveActionRect;
    private bool _showTwoHandedConfirmation;
    private ItemInstance? _pendingEquipItem;
    private bool _isPendingOffhand;
    private Rectangle _confirmButtonRect;
    private Rectangle _cancelButtonRect;
    private Rectangle _twoHandedConfirmDialogRect;

    public bool PlayLuteRequested { get; set; }
    public bool TorchIgniteRequested { get; set; }
    public bool GrappleRequested { get; set; }
    public bool ShoveRequested { get; set; }
    public ItemInstance? DroppedItem { get; set; }
    public ItemInstance? AttackRequestedWithItem { get; set; }
    public bool AttackRequestedIsOffhand { get; set; }
    public Spell? AttackRequestedWithSpell { get; set; }
    public bool AttackRequestedWithUnarmed { get; set; }
    public bool CloseRequested { get; set; }

    public CharacterSheet(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
        _supportedChars = font != null ? new HashSet<char>(font.Characters) : new HashSet<char>();
    }

    public bool Update(MouseState mouse, Character? character = null)
    {
        bool hasCharacterChanges = false;
        _mousePosition = mouse.Position;
        CloseRequested = false;

        if (!_isScrollInitialized)
        {
            _prevScrollValue = mouse.ScrollWheelValue;
            _isScrollInitialized = true;
            return false;
        }
        
        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta * 0.5f;
            _prevScrollValue = mouse.ScrollWheelValue;
        }

        if (character == null)
        {
            _prevMouseState = mouse;
            return false;
        }

        bool rightClick = mouse.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released;
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released;

        if (rightClick)
        {
            var clickedItem = _inventoryItemRects.FirstOrDefault(w => w.Rect.Contains(_mousePosition));
            if (clickedItem.Item == null)
            {
                var clickedAttack = _attackEntryRects.FirstOrDefault(a => a.Rect.Contains(_mousePosition));
                if (clickedAttack.Item != null)
                {
                    clickedItem = (clickedAttack.Rect, clickedAttack.Item, true, true);
                }
            }

            if (clickedItem.Item != null)
            {
                _contextItem = clickedItem.Item;
                _contextItemIsEquipped = clickedItem.IsEquipped;
                _contextItemIsEquippable = clickedItem.IsEquippable;
                var contextItemData = ItemDatabase.GetItem(clickedItem.Item.Name);
                _contextItemIsLight = contextItemData != null && contextItemData.IsLight;
                _showItemContextMenu = true;
                _inspectWeaponText = null;
                _itemContextMenuRect = BuildContextMenuRect(_mousePosition, character);
            }
            else
            {
                _showItemContextMenu = false;
            }
        }

        if (leftClick)
        {
            if (!string.IsNullOrEmpty(_inspectWeaponText) && !_inspectPopupRect.Contains(_mousePosition))
            {
                _inspectWeaponText = null;
            }

            if (!_showItemContextMenu && _grappleActionRect.Contains(_mousePosition))
            {
                GrappleRequested = true;
                CloseRequested = true;
            }

            if (!_showItemContextMenu && _shoveActionRect.Contains(_mousePosition))
            {
                ShoveRequested = true;
                CloseRequested = true;
            }

            var clickedAttack = _attackEntryRects.FirstOrDefault(a => a.Rect.Contains(_mousePosition));
            if (!_showItemContextMenu && clickedAttack.Rect != Rectangle.Empty)
            {
                if (clickedAttack.Item != null)
                {
                    AttackRequestedWithItem = clickedAttack.Item;
                    AttackRequestedIsOffhand = clickedAttack.IsOffhand;
                }
                else if (clickedAttack.Spell != null)
                {
                    AttackRequestedWithSpell = clickedAttack.Spell;
                }
                else
                {
                    AttackRequestedWithUnarmed = true;
                }
                CloseRequested = true;
            }

            if (_showTwoHandedConfirmation)
            {
                if (_confirmButtonRect.Contains(_mousePosition))
                {
                    if (_pendingEquipItem != null)
                    {
                        var pendingData = ItemDatabase.GetItem(_pendingEquipItem.Name);
                        if (pendingData.IsTwoHanded)
                        {
                            if (character.InventoryData.OffhandWeapon != null)
                                character.InventoryData.UnequipItemInstance(character.InventoryData.OffhandWeapon);
                            if (character.InventoryData.EquippedShield != null)
                                character.InventoryData.UnequipItemInstance(character.InventoryData.EquippedShield);

                            if (character.InventoryData.EquipItemInstance(_pendingEquipItem))
                            {
                                character.CalculateDerivedStats();
                                hasCharacterChanges = true;
                                AttackRequestedWithItem = _pendingEquipItem;
                            }
                        }
                        else // Equipping offhand or shield while 2H is equipped
                        {
                            if (character.InventoryData.EquippedWeapon != null)
                                character.InventoryData.UnequipItemInstance(character.InventoryData.EquippedWeapon);

                            bool success = false;
                            if (_isPendingOffhand)
                            {
                                if (character.InventoryData.EquipOffhandItemInstance(_pendingEquipItem))
                                {
                                    AttackRequestedWithItem = _pendingEquipItem;
                                    AttackRequestedIsOffhand = true;
                                    success = true;
                                }
                            }
                            else
                            {
                                if (character.InventoryData.EquipItemInstance(_pendingEquipItem))
                                {
                                    success = true;
                                }
                            }

                            if (success)
                            {
                                character.CalculateDerivedStats();
                                hasCharacterChanges = true;
                            }
                        }
                    }
                    _showTwoHandedConfirmation = false;
                    _pendingEquipItem = null;
                    _isPendingOffhand = false;
                }
                else if (_cancelButtonRect.Contains(_mousePosition) || !_twoHandedConfirmDialogRect.Contains(_mousePosition))
                {
                    _showTwoHandedConfirmation = false;
                    _pendingEquipItem = null;
                }
            }

            if (_showItemContextMenu)
            {
                var option = GetContextMenuOptionAt(_mousePosition, character);
                switch (option)
                {
                    case "Equip":
                        if (_contextItem != null)
                        {
                            var itemData = ItemDatabase.GetItem(_contextItem.Name);
                            if (itemData.IsTwoHanded && (character.InventoryData.OffhandWeapon != null || character.InventoryData.EquippedShield != null))
                            {
                                _showTwoHandedConfirmation = true;
                                _pendingEquipItem = _contextItem;
                                _isPendingOffhand = false;
                            }
                            else if (itemData.Type == ItemType.Shield && character.InventoryData.EquippedWeapon != null && ItemDatabase.GetItem(character.InventoryData.EquippedWeapon.Name).IsTwoHanded)
                            {
                                _showTwoHandedConfirmation = true;
                                _pendingEquipItem = _contextItem;
                                _isPendingOffhand = false;
                            }
                            else
                            {
                                if (character.InventoryData.EquipItemInstance(_contextItem))
                                {
                                    character.CalculateDerivedStats();
                                    hasCharacterChanges = true;

                                    if (itemData.Type == ItemType.Weapon)
                                    {
                                        AttackRequestedWithItem = _contextItem;
                                    }
                                }
                            }
                        }
                        _showItemContextMenu = false;
                        break;
                    case "Equip (Offhand)":
                        if (_contextItem != null)
                        {
                            if (character.InventoryData.EquippedWeapon != null && ItemDatabase.GetItem(character.InventoryData.EquippedWeapon.Name).IsTwoHanded)
                            {
                                _showTwoHandedConfirmation = true;
                                _pendingEquipItem = _contextItem;
                                _isPendingOffhand = true;
                            }
                            else
                            {
                                if (character.InventoryData.EquipOffhandItemInstance(_contextItem))
                                {
                                    character.CalculateDerivedStats();
                                    hasCharacterChanges = true;
                                    AttackRequestedWithItem = _contextItem;
                                    AttackRequestedIsOffhand = true;
                                }
                            }
                        }
                        _showItemContextMenu = false;
                        break;
                    case "Unequip":
                        if (_contextItem != null)
                        {
                            character.InventoryData.UnequipItemInstance(_contextItem);
                            character.CalculateDerivedStats();
                            hasCharacterChanges = true;
                        }
                        _showItemContextMenu = false;
                        break;
                    case "Light":
                        if (_contextItem != null && _contextItem.Name == "Torch")
                        {
                            // Re-verify conditions
                            bool hasTinderbox = character.InventoryData.HasItem("Tinderbox");
                            bool hasOtherLitTorch = character.InventoryData.Items.Any(it => it.Name == "Torch" && it.IsLit && it != _contextItem);
                            bool hasFireSpell = character.KnownSpells.Any(s => s.DamageType == DamageType.Fire) ||
                                                character.PreparedSpells.Any(s => s.DamageType == DamageType.Fire);

                            if (hasTinderbox || hasOtherLitTorch || hasFireSpell)
                            {
                                _contextItem.IsLit = true;
                                _contextItem.RemainingMinutes = 60;
                                TorchIgniteRequested = true;

                                // Prefer main hand if empty, otherwise off-hand
                                if (character.InventoryData.EquippedWeapon == null)
                                {
                                    character.InventoryData.EquipItemInstance(_contextItem);
                                }
                                else if (character.InventoryData.OffhandWeapon == null)
                                {
                                    character.InventoryData.EquipOffhandItemInstance(_contextItem);
                                }
                                else
                                {
                                    // Both hands full, default to main hand
                                    character.InventoryData.EquipItemInstance(_contextItem);
                                }

                                character.CalculateDerivedStats();
                                hasCharacterChanges = true;
                            }
                        }
                        _showItemContextMenu = false;
                        break;
                    case "Throw":
                        if (_contextItem != null)
                        {
                            character.CalculateDerivedStats();
                            hasCharacterChanges = true;
                        }
                        _showItemContextMenu = false;
                        break;
                    case "Drop":
                        if (_contextItem != null)
                        {
                            DroppedItem = _contextItem;
                            character.InventoryData.RemoveItemInstance(_contextItem);
                            character.CalculateDerivedStats();
                            hasCharacterChanges = true;
                        }
                        _showItemContextMenu = false;
                        break;
                    case "Play":
                        PlayLuteRequested = true;
                        _showItemContextMenu = false;
                        break;
                    case "Inspect":
                        if (_contextItem != null)
                        {
                            _inspectWeaponText = BuildItemTooltip(_contextItem, _contextItemIsEquipped);
                        }
                        _showItemContextMenu = false;
                        break;
                    default:
                        if (!_itemContextMenuRect.Contains(_mousePosition))
                        {
                            _showItemContextMenu = false;
                        }
                        break;
                }
            }
        }

        _prevMouseState = mouse;
        return hasCharacterChanges;
    }

    public void ResetScroll()
    {
        _scrollOffset = 0f;
        _prevScrollValue = 0;
        _isScrollInitialized = false;
    }

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphics, Character character, Campaign? campaign = null, Creature? creature = null)
    {
        var vp = graphics.Viewport;
        _hoverTooltip = null;

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(20, 20, 20));

        if (_font != null && character != null)
        {
            _inventoryItemRects.Clear();
            _attackEntryRects.Clear();
            var c = character;
            int margin = Margin;
            int padding = 10;
            int scrollbarWidth = ScrollbarWidth;
            
            int sheetWidth = vp.Width - margin * 2 - scrollbarWidth - 10;
            int sheetHeight = vp.Height - margin * 2;
            int sheetX = margin;
            int sheetY = margin;
            
            // Layout calculations
            int col1Width = (int)(sheetWidth * 0.25f);
            int col2Width = (int)(sheetWidth * 0.35f);
            int col3Width = sheetWidth - col1Width - col2Width - padding * 2;

            // 1. Calculate heights without drawing (spriteBatch = null)
            int headerHeight = DrawHeader(null, c, sheetX, 0, sheetWidth);
            int col1Height = DrawLeftColumn(null, c, 0, 0, col1Width);
            int col2Height = DrawMiddleColumn(null, c, 0, 0, col2Width, creature);
            int col3Height = DrawRightColumn(null, c, 0, 0, col3Width);

            int maxColHeight = Math.Max(col1Height, Math.Max(col2Height, col3Height));
            int totalContentHeight = headerHeight + maxColHeight + 40;

            int maxScroll = Math.Max(0, totalContentHeight - sheetHeight);
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, maxScroll);
            
            var scissorRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
            var previousScissor = graphics.ScissorRectangle;
            graphics.ScissorRectangle = Rectangle.Intersect(scissorRect, graphics.Viewport.Bounds);
            
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = true });
            
            var sheetRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
            spriteBatch.Draw(_pixel, sheetRect, new Color(240, 235, 225));
            
            int scrollY = sheetY - (int)_scrollOffset;
            
            // 2. Actual Drawing
            DrawHeader(spriteBatch, c, sheetX, scrollY, sheetWidth);
            
            int contentY = scrollY + headerHeight + padding;
            int col1X = sheetX + padding;
            int col2X = col1X + col1Width + padding;
            int col3X = col2X + col2Width + padding;
            
            DrawLeftColumn(spriteBatch, c, col1X, contentY, col1Width);
            DrawMiddleColumn(spriteBatch, c, col2X, contentY, col2Width, creature);
            DrawRightColumn(spriteBatch, c, col3X, contentY, col3Width);
            
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            graphics.ScissorRectangle = previousScissor;
            
            if (totalContentHeight > sheetHeight)
            {
                DrawScrollbar(spriteBatch, sheetX + sheetWidth + 5, sheetY, scrollbarWidth, sheetHeight, maxScroll, totalContentHeight);
            }
            
            var hint = Loc.Tr("Press 'C' to close | Mouse wheel to scroll");
            var hintSize = _font.MeasureString(hint) * 0.8f;
            spriteBatch.DrawString(_font, hint, new Vector2((vp.Width - hintSize.X) / 2, vp.Height - 30), Color.White * 0.8f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            DrawCloseButton(spriteBatch, vp);
            DrawTooltip(spriteBatch, vp);
            DrawWeaponContextMenu(spriteBatch, vp, character);
            DrawInspectPopup(spriteBatch, vp);
            DrawTwoHandedConfirmation(spriteBatch, vp, character);
        }
    }

    private void DrawTwoHandedConfirmation(SpriteBatch spriteBatch, Viewport viewport, Character character)
    {
        if (!_showTwoHandedConfirmation || _pendingEquipItem == null) return;

        int width = 440;
        int height = 180;
        int x = (viewport.Width - width) / 2;
        int y = (viewport.Height - height) / 2;
        var rect = new Rectangle(x, y, width, height);
        _twoHandedConfirmDialogRect = rect;

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.5f);
        spriteBatch.Draw(_pixel, rect, new Color(30, 30, 30));
        DrawBorder(spriteBatch, rect, Color.White, 2);

        var pendingData = ItemDatabase.GetItem(_pendingEquipItem.Name);
        string msg;
        if (pendingData.IsTwoHanded)
        {
            string offhandName = character.InventoryData.OffhandWeapon?.Name ?? character.InventoryData.EquippedShield?.Name ?? "offhand item";
            msg = Loc.Tr("You're about to equip a 2 handed weapon which will unequip your currently equipped offhand item ({0}). Do you want to continue?", offhandName);
        }
        else
        {
            string mainHandName = character.InventoryData.EquippedWeapon?.Name ?? "main hand item";
            msg = Loc.Tr("You're about to equip {0} which will unequip your currently equipped 2 handed weapon ({1}). Do you want to continue?", _pendingEquipItem.Name, mainHandName);
        }

        string wrappedMsg = WrapText(_font, msg, width - 40, 0.7f);
        spriteBatch.DrawString(_font, wrappedMsg, new Vector2(x + 20, y + 20), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        int btnWidth = 100;
        int btnHeight = 40;
        _confirmButtonRect = new Rectangle(x + width / 2 - btnWidth - 10, y + height - 60, btnWidth, btnHeight);
        _cancelButtonRect = new Rectangle(x + width / 2 + 10, y + height - 60, btnWidth, btnHeight);

        spriteBatch.Draw(_pixel, _confirmButtonRect, _confirmButtonRect.Contains(_mousePosition) ? Color.DarkGreen : Color.Green);
        DrawBorder(spriteBatch, _confirmButtonRect, Color.White, 1);
        var yesText = Loc.Tr("Yes");
        var yesSize = _font.MeasureString(yesText) * 0.7f;
        spriteBatch.DrawString(_font, yesText, new Vector2(_confirmButtonRect.X + (btnWidth - yesSize.X) / 2, _confirmButtonRect.Y + (btnHeight - yesSize.Y) / 2), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        spriteBatch.Draw(_pixel, _cancelButtonRect, _cancelButtonRect.Contains(_mousePosition) ? Color.DarkRed : Color.Red);
        DrawBorder(spriteBatch, _cancelButtonRect, Color.White, 1);
        var noText = Loc.Tr("No");
        var noSize = _font.MeasureString(noText) * 0.7f;
        spriteBatch.DrawString(_font, noText, new Vector2(_cancelButtonRect.X + (btnWidth - noSize.X) / 2, _cancelButtonRect.Y + (btnHeight - noSize.Y) / 2), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    public Rectangle GetCloseButtonRect(Viewport viewport)
    {
        int x = viewport.Width - Margin - CloseButtonWidth;
        int y = Margin + 4;
        return new Rectangle(x, y, CloseButtonWidth, CloseButtonHeight);
    }

    private void DrawCloseButton(SpriteBatch spriteBatch, Viewport viewport)
    {
        var closeButtonRect = GetCloseButtonRect(viewport);
        spriteBatch.Draw(_pixel, closeButtonRect, new Color(140, 40, 40));
        DrawBorder(spriteBatch, closeButtonRect, Color.Black, 2);
        RegisterTooltip(closeButtonRect, Loc.Tr("Close the character sheet (shortcut: C)."));

        var text = Loc.Tr("Exit");
        var textSize = _font.MeasureString(text);
        var textPos = new Vector2(
            closeButtonRect.X + (closeButtonRect.Width - textSize.X * 0.7f) * 0.5f,
            closeButtonRect.Y + (closeButtonRect.Height - textSize.Y * 0.7f) * 0.5f
        );

        spriteBatch.DrawString(_font, text, textPos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private void DrawScrollbar(SpriteBatch spriteBatch, int x, int y, int width, int height, int maxScroll, int contentHeight)
    {
        var trackRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, trackRect, new Color(60, 60, 60));
        DrawBorder(spriteBatch, trackRect, Color.Black, 1);
        
        float contentRatio = (float)height / contentHeight;
        int thumbHeight = (int)(height * contentRatio);
        thumbHeight = Math.Max(thumbHeight, 30);
        
        float scrollRatio = maxScroll > 0 ? _scrollOffset / maxScroll : 0;
        int thumbY = y + (int)((height - thumbHeight) * scrollRatio);
        
        var thumbRect = new Rectangle(x + 2, thumbY, width - 4, thumbHeight);
        spriteBatch.Draw(_pixel, thumbRect, new Color(180, 180, 180));
        DrawBorder(spriteBatch, thumbRect, new Color(100, 100, 100), 1);
    }

    private int DrawHeader(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int padding = 10;
        
        if (spriteBatch != null)
        {
            var logoRect = new Rectangle(x + padding, y + padding, 120, 40);
            spriteBatch.Draw(_pixel, logoRect, Color.Black * 0.2f);
            spriteBatch.DrawString(_font, "D&D 5E", new Vector2(logoRect.X + 10, logoRect.Y + 10), Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            int nameX = x + padding + 130;
            int nameY = y + padding + 5;
            spriteBatch.DrawString(_font, SafeString(c.Name), new Vector2(nameX, nameY), Color.Black, 0f, Vector2.Zero, 1.8f, SpriteEffects.None, 0f);
        }
        
        int infoStartY = y + padding + 50;
        int fieldWidth = (width - padding * 2) / 6;
        int maxFieldHeight = 0;
        
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "CLASS & LEVEL", $"{c.Class} {c.Level}", x + padding, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "BACKGROUND", c.Background, x + padding + fieldWidth, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "PLAYER NAME", "", x + padding + fieldWidth * 2, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "RACE", c.Race, x + padding + fieldWidth * 3, infoStartY, fieldWidth - 5));
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "ALIGNMENT", c.Alignment, x + padding + fieldWidth * 4, infoStartY, fieldWidth - 5));
        int nextLevelXP = DndMath.GetNextLevelXP(c.Level);
        string xpDisplay = nextLevelXP >= 0 ? $"{c.XP} / {nextLevelXP}" : $"{c.XP} (MAX)";
        maxFieldHeight = Math.Max(maxFieldHeight, DrawHeaderField(spriteBatch, "EXPERIENCE POINTS", xpDisplay, x + padding + fieldWidth * 5, infoStartY, fieldWidth - 5));
        
        return padding + 50 + maxFieldHeight + 10;
    }

    private int DrawHeaderField(SpriteBatch? spriteBatch, string label, string value, int x, int y, int width)
    {
        string safeValue = SafeString(value);
        string wrappedValue = WrapText(_font, safeValue, width, 0.7f);
        var valueSize = _font.MeasureString(wrappedValue) * 0.7f;
        int height = (int)valueSize.Y + 15;
        height = Math.Max(height, 30);

        if (spriteBatch != null)
        {
            var fieldRect = new Rectangle(x, y, width, height);
            spriteBatch.DrawString(_font, label, new Vector2(x, y), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, wrappedValue, new Vector2(x + 2, y + 10), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            RegisterTooltip(fieldRect, string.IsNullOrWhiteSpace(value)
                ? Loc.Tr("{0}: field to fill in.", label)
                : $"{label}: {value}.");

            var lineRect = new Rectangle(x, y + height - 2, width, 1);
            spriteBatch.Draw(_pixel, lineRect, Color.Black * 0.3f);
        }
        
        return height;
    }

    private int DrawLeftColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int currentY = y;
        int boxSize = 70;
        int boxSpacing = 8;
        
        var abilities = new[] 
        {
            ("Strength", c.Strength, c.StrengthSaveProficiency),
            ("Dexterity", c.Dexterity, c.DexteritySaveProficiency),
            ("Constitution", c.Constitution, c.ConstitutionSaveProficiency),
            ("Intelligence", c.Intelligence, c.IntelligenceSaveProficiency),
            ("Wisdom", c.Wisdom, c.WisdomSaveProficiency),
            ("Charisma", c.Charisma, c.CharismaSaveProficiency)
        };
        
        foreach (var (key, score, saveProficiency) in abilities)
        {
            if (spriteBatch != null) DrawAbilityBox(spriteBatch, c, key, score, saveProficiency, x, currentY, width, boxSize);
            currentY += boxSize + boxSpacing;
        }
        
        currentY += 10;
        if (spriteBatch != null)
        {
            DrawSmallBox(spriteBatch, "INSPIRATION", "", x, currentY, width / 2 - 5, 50, "Inspiration: avantage sur un jet important quand le MJ l'accorde.");
            DrawSmallBox(spriteBatch, "PROFICIENCY BONUS", FormatModifier(c.ProficiencyBonus), x + width / 2 + 5, currentY, width / 2 - 5, 50, Loc.Tr("Current proficiency bonus: {0}.", FormatModifier(c.ProficiencyBonus)));
        }
        currentY += 60;
        
        int passivePerception = 10 + c.GetAbilityModifier(c.Wisdom) + (c.PerceptionProficiency ? c.ProficiencyBonus : 0);
        if (spriteBatch != null) DrawSmallBox(spriteBatch, "PASSIVE WISDOM (PERCEPTION)", passivePerception.ToString(), x, currentY, width, 40, Loc.Tr("Passive Perception = 10 + Wisdom mod + proficiency if proficient = {0}.", passivePerception));
        currentY += 50;

        currentY += DrawProficienciesBox(spriteBatch, c, x, currentY, width);

        return currentY - y;
    }
    
    private int DrawProficienciesBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int contentY = 25;
        
        // Armor proficiencies
        if (c.ArmorProficiencies != null && c.ArmorProficiencies.Count > 0)
        {
            contentY += 15;
            foreach (var armor in c.ArmorProficiencies)
            {
                contentY += 14;
            }
            contentY += 5;
        }
        
        // Weapon proficiencies
        if (c.WeaponProficiencies != null && c.WeaponProficiencies.Count > 0)
        {
            contentY += 15;
            foreach (var weapon in c.WeaponProficiencies)
            {
                contentY += 14;
            }
            contentY += 5;
        }
        
        // Class info
        contentY += 15 + 14 + 14;

        // Barbarian-specific level features
        if (c.Class == "Barbarian")
        {
            var classData = ClassData.GetClass(c.Class);
            var levelData = classData.GetLevelData(c.Level);
            if (levelData != null)
            {
                contentY += 5 + 15 + 14 + 14 + 14;
            }
        }

        // Bard-specific level features
        if (c.Class == "Bard")
        {
            var classData = ClassData.GetClass(c.Class);
            var levelData = classData.GetBardLevelData(c.Level);
            if (levelData != null)
            {
                // Section header + Bardic Inspiration + Spell Slots header + up to 5 slot rows + Features
                contentY += 5 + 15 + 14 + 14 + 15;
                for (int i = 0; i < levelData.SpellSlots.Length; i++)
                    if (levelData.SpellSlots[i] > 0) contentY += 14;
                contentY += 14; // Features line
            }
        }

        // Cleric-specific level features
        if (c.Class == "Cleric")
        {
            var classData = ClassData.GetClass(c.Class);
            var levelData = classData.GetClericLevelData(c.Level);
            if (levelData != null)
            {
                contentY += 5 + 15 + 14 + 14 + 15;
                for (int i = 0; i < levelData.SpellSlots.Length; i++)
                    if (levelData.SpellSlots[i] > 0) contentY += 14;
                contentY += 14; // Features line
            }
        }

        int height = contentY + 10;

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);

            spriteBatch.DrawString(_font, Loc.Tr("PROFICIENCIES & LANGUAGES"), new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            int drawY = y + 25;
            if (c.ArmorProficiencies != null && c.ArmorProficiencies.Count > 0)
            {
                spriteBatch.DrawString(_font, Loc.Tr("Armor:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                drawY += 15;
                foreach (var armor in c.ArmorProficiencies)
                {
                    spriteBatch.DrawString(_font, SafeString($"• {armor}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Armor proficiency: {0}.", armor));
                    drawY += 14;
                }
                drawY += 5;
            }

            if (c.WeaponProficiencies != null && c.WeaponProficiencies.Count > 0)
            {
                spriteBatch.DrawString(_font, Loc.Tr("Weapons:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                drawY += 15;
                foreach (var weapon in c.WeaponProficiencies)
                {
                    spriteBatch.DrawString(_font, SafeString($"• {weapon}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Weapon proficiency: {0}.", weapon));
                    drawY += 14;
                }
                drawY += 5;
            }

            var classData = ClassData.GetClass(c.Class);
            spriteBatch.DrawString(_font, Loc.Tr("Class Info:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            drawY += 15;
            spriteBatch.DrawString(_font, SafeString($"• Hit Die: d{c.HitDiceType}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Class hit die: d{0}.", c.HitDiceType));
            drawY += 14;
            spriteBatch.DrawString(_font, SafeString($"• Primary Ability: {classData.PrimaryAbility}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Class primary ability: {0}.", classData.PrimaryAbility));
            drawY += 14;

            if (c.Class == "Barbarian")
            {
                var levelData = classData.GetLevelData(c.Level);
                if (levelData != null)
                {
                    drawY += 5;
                    spriteBatch.DrawString(_font, Loc.Tr("Barbarian:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    drawY += 15;
                    string ragesMax = levelData.Rages == -1 ? Loc.Tr("Unlimited") : levelData.Rages.ToString();
                    spriteBatch.DrawString(_font, SafeString($"• Rages: {c.RagesRemaining}/{ragesMax}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Rages per day: {0}. Remaining: {1}.", ragesMax, c.RagesRemaining));
                    drawY += 14;
                    spriteBatch.DrawString(_font, SafeString($"• Rage Damage: +{levelData.RageDamage}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Rage damage bonus: +{0}.", levelData.RageDamage));
                    drawY += 14;
                    spriteBatch.DrawString(_font, SafeString($"• {levelData.Features}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Level {0} features: {1}.", c.Level, levelData.Features));
                }
            }
            else if (c.Class == "Bard")
            {
                var bardLevelData = classData.GetBardLevelData(c.Level);
                if (bardLevelData != null)
                {
                    drawY += 5;
                    spriteBatch.DrawString(_font, Loc.Tr("Bard:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    drawY += 15;

                    // Bardic Inspiration
                    spriteBatch.DrawString(_font, SafeString($"• Bardic Inspiration: {c.BardicInspirationUsesRemaining}/{c.BardicInspirationMax} (d{c.BardicInspirationDice})"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Bardic Inspiration: {0} remaining out of {1}. Die: d{2}.", c.BardicInspirationUsesRemaining, c.BardicInspirationMax, c.BardicInspirationDice));
                    drawY += 14;

                    // Cantrips and Spells Known
                    spriteBatch.DrawString(_font, SafeString($"• Cantrips: {c.CantripsKnown}  |  Spells Known: {c.SpellsKnown}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    drawY += 14;

                    // Spell Slots
                    spriteBatch.DrawString(_font, Loc.Tr("Spell Slots:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
                    drawY += 15;
                    for (int i = 0; i < bardLevelData.SpellSlots.Length; i++)
                    {
                        if (bardLevelData.SpellSlots[i] <= 0) continue;
                        int remaining = i < c.SpellSlotsRemaining.Length ? c.SpellSlotsRemaining[i] : 0;
                        spriteBatch.DrawString(_font, SafeString($"• Level {i + 1}: {remaining}/{bardLevelData.SpellSlots[i]}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                        RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Level {0} spell slots: {1} remaining out of {2}.", i + 1, remaining, bardLevelData.SpellSlots[i]));
                        drawY += 14;
                    }

                    // Features
                    spriteBatch.DrawString(_font, SafeString($"• {bardLevelData.Features}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Level {0} features: {1}.", c.Level, bardLevelData.Features));
                }
            }
            else if (c.Class == "Cleric")
            {
                var levelData = classData.GetClericLevelData(c.Level);
                if (levelData != null)
                {
                    drawY += 5;
                    spriteBatch.DrawString(_font, Loc.Tr("Cleric:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    drawY += 15;

                    // Channel Divinity
                    if (levelData.ChannelDivinityUses > 0)
                    {
                        spriteBatch.DrawString(_font, SafeString($"• Channel Divinity: {c.ChannelDivinityUsesRemaining}/{c.ChannelDivinityUsesMax}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                        RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Channel Divinity: {0} out of {1} uses.", c.ChannelDivinityUsesRemaining, c.ChannelDivinityUsesMax));
                        drawY += 14;
                    }

                    // Cantrips
                    spriteBatch.DrawString(_font, SafeString($"• Cantrips Known: {levelData.CantripsKnown}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    drawY += 14;

                    // Spell Slots
                    spriteBatch.DrawString(_font, Loc.Tr("Spell Slots:"), new Vector2(x + 10, drawY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
                    drawY += 15;
                    for (int i = 0; i < levelData.SpellSlots.Length; i++)
                    {
                        if (levelData.SpellSlots[i] <= 0) continue;
                        int remaining = i < c.SpellSlotsRemaining.Length ? c.SpellSlotsRemaining[i] : 0;
                        spriteBatch.DrawString(_font, SafeString($"• Level {i + 1}: {remaining}/{levelData.SpellSlots[i]}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                        RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Level {0} spell slots: {1} remaining out of {2}.", i + 1, remaining, levelData.SpellSlots[i]));
                        drawY += 14;
                    }

                    // Features
                    spriteBatch.DrawString(_font, SafeString($"• {levelData.Features}"), new Vector2(x + 15, drawY), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    RegisterTooltip(new Rectangle(x + 12, drawY - 2, width - 24, 14), Loc.Tr("Level {0} features: {1}.", c.Level, levelData.Features));
                }
            }
        }

        return height;
    }

    private void DrawAbilityBox(SpriteBatch spriteBatch, Character c, string key, int score, bool saveProficiency, int x, int y, int width, int height)
    {
        var outerRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, outerRect, Color.White);
        DrawBorder(spriteBatch, outerRect, Color.Black, 2);
        
        string name = Loc.Tr(key).ToUpper();
        var nameSize = _font.MeasureString(name);
        spriteBatch.DrawString(_font, name, new Vector2(x + (width - nameSize.X * 0.5f) / 2, y + 2), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        
        int circleSize = 40;
        int circleX = x + (width - circleSize) / 2;
        int circleY = y + 15;
        DrawCircle(spriteBatch, circleX, circleY, circleSize, Color.Black, 2);
        
        int modifier = c.GetAbilityModifier(score);
        string modText = FormatModifier(modifier);
        var modSize = _font.MeasureString(modText);
        spriteBatch.DrawString(_font, modText, new Vector2(circleX + (circleSize - modSize.X) / 2, circleY + (circleSize - modSize.Y) / 2), Color.Black);
        
        string scoreText = score.ToString();
        var scoreSize = _font.MeasureString(scoreText);
        spriteBatch.DrawString(_font, scoreText, new Vector2(x + (width - scoreSize.X * 0.7f) / 2, y + height - 18), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        int checkboxSize = 12;
        int checkboxX = x + 5;
        int checkboxY = y + height / 2 - checkboxSize / 2;
        DrawCheckbox(spriteBatch, checkboxX, checkboxY, checkboxSize, saveProficiency);

        RegisterTooltip(outerRect, Loc.Tr("{0}: score {1} ({2}). Saving throw {3}.", name, score, modText, saveProficiency ? Loc.Tr("proficient") : Loc.Tr("not proficient")));
        
        spriteBatch.DrawString(_font, Loc.Tr("SAVING THROWS"), new Vector2(x + 2, y + height - 10), Color.Black * 0.4f, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
    }

    private int DrawMiddleColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width, Creature? creature = null)
    {
        int currentY = y;
        int smallBoxSize = 60;
        int topBoxWidth = (width - 20) / 3;
        
        if (spriteBatch != null)
        {
            DrawHexBox(spriteBatch, "ARMOR CLASS", c.ArmorClass.ToString(), x, currentY, topBoxWidth, smallBoxSize, Loc.Tr("Armor Class: {0}.", c.ArmorClass));
            DrawCircleBox(spriteBatch, "INITIATIVE", FormatModifier(c.GetAbilityModifier(c.Dexterity)), x + topBoxWidth + 10, currentY, topBoxWidth, smallBoxSize, Loc.Tr("Initiative: {0}.", FormatModifier(c.GetAbilityModifier(c.Dexterity))));
            DrawCircleBox(spriteBatch, "SPEED", $"{c.Speed}", x + topBoxWidth * 2 + 20, currentY, topBoxWidth, smallBoxSize, Loc.Tr("Speed: {0} ft.", c.Speed));
        }
        currentY += smallBoxSize + 10;
        
        int hpHeight = 80;
        if (spriteBatch != null) DrawHPBox(spriteBatch, c, x, currentY, width, hpHeight);
        currentY += hpHeight + 10;

        int tempHpHeight = 80;
        if (spriteBatch != null) DrawTempHPBox(spriteBatch, c, x, currentY, width, tempHpHeight);
        currentY += tempHpHeight + 10;

        int hdWidth = width / 2 - 5;
        if (spriteBatch != null)
        {
            DrawHitDiceBox(spriteBatch, c, x, currentY, hdWidth, 80);
            DrawDeathSavesBox(spriteBatch, c, x + hdWidth + 10, currentY, hdWidth, 80);
        }
        currentY += 90;
        
        currentY += DrawAttacksBox(spriteBatch, c, x, currentY, width, creature);
        currentY += 10;

        currentY += DrawEquipmentBox(spriteBatch, c, x, currentY, width, creature);

        return currentY - y;
    }

    private int DrawRightColumn(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int currentY = y;
        
        currentY += DrawSkillsBox(spriteBatch, c, x, currentY, width);
        currentY += 10;
        
        currentY += DrawTextBox(spriteBatch, Loc.Tr("PERSONALITY TRAITS"), "", x, currentY, width);
        currentY += 10;
        
        currentY += DrawTextBox(spriteBatch, Loc.Tr("IDEALS"), "", x, currentY, width);
        currentY += 10;

        currentY += DrawTextBox(spriteBatch, Loc.Tr("BONDS"), "", x, currentY, width);
        currentY += 10;
        
        currentY += DrawTextBox(spriteBatch, Loc.Tr("FLAWS"), "", x, currentY, width);
        
        return currentY - y;
    }
    
    private int DrawAttacksBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width, Creature? creature = null)
    {
        int entryHeight = 22;
        int headerHeight = 45;
        int offhandCount = c.InventoryData.OffhandWeapon != null ? 1 : 0;

        var relevantSpells = c.UsesSpellPreparation ? c.PreparedSpells : c.KnownSpells;
        int spellCount = relevantSpells.Count;

        int entryCount = Math.Max(5, (c.InventoryData.EquippedWeapon != null ? 1 : 0) + offhandCount + 1 + spellCount + 2);
        int height = headerHeight + (entryCount * entryHeight) + 10;

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, Loc.Tr("ATTACKS & SPELLCASTING"), new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            
            int headerY = y + 30;
            int nameCol = x + 10;
            int atkBonusCol = x + width / 2;
            int damageCol = x + width / 2 + 80;

            spriteBatch.DrawString(_font, Loc.Tr("NAME"), new Vector2(nameCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "BONUS ATK", new Vector2(atkBonusCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, Loc.Tr("DAMAGE/TYPE"), new Vector2(damageCol, headerY), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

            spriteBatch.Draw(_pixel, new Rectangle(x + 5, headerY + 15, width - 10, 1), Color.Black * 0.3f);

            int entryY = headerY + 20;

            void DrawEntry(string name, string bonus, string damage, Rectangle clickRect, Color color, bool isActive)
            {
                if (clickRect.Contains(_mousePosition) && !_showItemContextMenu)
                {
                    spriteBatch.Draw(_pixel, clickRect, Color.Gold * 0.2f);
                }

                if (isActive)
                {
                    DrawMemeText(spriteBatch, SafeString(name), new Vector2(nameCol, entryY), Color.White);
                    DrawMemeText(spriteBatch, SafeString(bonus), new Vector2(atkBonusCol, entryY), Color.White);
                    DrawMemeText(spriteBatch, SafeString(damage), new Vector2(damageCol, entryY), Color.White);
                }
                else
                {
                    spriteBatch.DrawString(_font, SafeString(name), new Vector2(nameCol, entryY), color, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(_font, FormatModifier(bonus), new Vector2(atkBonusCol, entryY), color, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(_font, SafeString(damage), new Vector2(damageCol, entryY), color, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }
            }

            if (c.InventoryData.EquippedWeapon != null)
            {
                var item = c.InventoryData.EquippedWeapon;
                string weapon = item.Name;
                var weaponItem = ItemDatabase.GetItem(weapon);
                int abilityMod = weaponItem != null && weaponItem.IsFinesse
                    ? Math.Max(c.GetAbilityModifier(c.Strength), c.GetAbilityModifier(c.Dexterity))
                    : weaponItem != null && weaponItem.IsRanged
                        ? c.GetAbilityModifier(c.Dexterity)
                        : c.GetAbilityModifier(c.Strength);
                int profBonus = c.IsProficientWithWeapon(weapon) ? c.ProficiencyBonus : 0;
                int atkBonus = abilityMod + profBonus;

                bool isVersatileTwoHanded = weaponItem != null && weaponItem.IsVersatile && c.InventoryData.OffhandWeapon == null && c.InventoryData.EquippedShield == null;
                string damage = isVersatileTwoHanded && weaponItem != null ? $"{weaponItem.VersatileDamageDice} {weaponItem.DamageType.ToDisplayString()}" : GetWeaponDamage(weapon);

                var clickRect = new Rectangle(nameCol, entryY, width - 20, entryHeight);
                bool isActive = creature != null && creature.AttackName == weapon && !creature.IsOffhandAttack && creature.IsMeleeAttack == !(weaponItem?.IsRanged ?? false);

                DrawEntry(weapon, FormatModifier(atkBonus), damage, clickRect, Color.Black, isActive);
                RegisterTooltip(clickRect, BuildWeaponTooltip(weapon, atkBonus, damage));
                _attackEntryRects.Add((clickRect, item, null, false));
                entryY += entryHeight;
            }

            // Two-Weapon Fighting offhand bonus attack
            if (c.InventoryData.OffhandWeapon != null)
            {
                var item = c.InventoryData.OffhandWeapon;
                string offhand = item.Name;
                var offhandItem = ItemDatabase.GetItem(offhand);
                int offhandAbilityMod = offhandItem != null && offhandItem.IsFinesse
                    ? Math.Max(c.GetAbilityModifier(c.Strength), c.GetAbilityModifier(c.Dexterity))
                    : c.GetAbilityModifier(c.Strength);
                int offhandProfBonus = c.IsProficientWithWeapon(offhand) ? c.ProficiencyBonus : 0;
                int offhandAtkBonus = offhandAbilityMod + offhandProfBonus;
                string offhandDice = GetWeaponDamage(offhand);
                // TWF: don't add ability modifier to damage (unless negative)
                int offhandDmgMod = Math.Min(0, offhandAbilityMod);
                string offhandDamage = offhandDmgMod < 0 ? $"{offhandDice} {FormatModifier(offhandDmgMod)}" : offhandDice;

                var clickRect = new Rectangle(nameCol, entryY, width - 20, entryHeight);
                bool isActive = creature != null && creature.AttackName == offhand && creature.IsOffhandAttack;

                DrawEntry($"(BA) {offhand}", FormatModifier(offhandAtkBonus), offhandDamage, clickRect, new Color(80, 80, 180), isActive);
                RegisterTooltip(clickRect, Loc.Tr("Two-weapon fighting (bonus action): {0}\nBonus: {1}\nDamage: {2} (positive modifier not added)\n{3}", offhand, FormatModifier(offhandAtkBonus), offhandDamage, BuildWeaponTooltip(offhand, offhandAtkBonus, offhandDamage)));
                _attackEntryRects.Add((clickRect, item, null, true));
                entryY += entryHeight;
            }

            // Unarmed strike is always available
            {
                int strMod = c.GetAbilityModifier(c.Strength);
                int unarmedBonus = strMod + c.ProficiencyBonus;
                string unarmedDamage = $"1{FormatModifier(strMod)} Bludgeoning";
                var clickRect = new Rectangle(nameCol, entryY, width - 20, entryHeight);
                bool isActive = creature != null && creature.AttackName == "Unarmed Strike";

                DrawEntry("Unarmed Strike", FormatModifier(unarmedBonus), unarmedDamage, clickRect, Color.Black * 0.7f, isActive);
                RegisterTooltip(clickRect, Loc.Tr("Unarmed strike: bonus {0}, damage 1{1} bludgeoning, range 5 ft.", FormatModifier(unarmedBonus), FormatModifier(strMod)));
                _attackEntryRects.Add((clickRect, null, null, false));
                entryY += entryHeight;
            }

            // Spells
            foreach (var spell in relevantSpells)
            {
                int mod = c.GetPrimaryAbilityModifier();
                int spellAtk = mod + c.ProficiencyBonus;
                string damage = !string.IsNullOrEmpty(spell.DamageDice) ? $"{spell.DamageDice} {spell.DamageType.ToDisplayString()}" : "Effect";

                var clickRect = new Rectangle(nameCol, entryY, width - 20, entryHeight);
                bool isActive = creature != null && creature.AttackName == spell.Name;

                DrawEntry(spell.Name, FormatModifier(spellAtk), damage, clickRect, new Color(100, 50, 150), isActive);
                RegisterTooltip(clickRect, Loc.Tr("{0} (Level {1} {2})\nRange: {3} ft\n{4}", spell.Name, spell.Level, spell.School, spell.Range, spell.Description));
                _attackEntryRects.Add((clickRect, null, spell, false));
                entryY += entryHeight;
            }

            int grappleBonus = c.GetSkillBonus("Athletics", out _);
            _grappleActionRect = new Rectangle(nameCol, entryY, width - 20, entryHeight);
            if (_grappleActionRect.Contains(_mousePosition))
                spriteBatch.Draw(_pixel, _grappleActionRect, new Color(180, 210, 255, 120));
            spriteBatch.DrawString(_font, "Grapple", new Vector2(nameCol, entryY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, FormatModifier(grappleBonus), new Vector2(atkBonusCol, entryY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, SafeString("Contested (Ath/Acr)"), new Vector2(damageCol, entryY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            RegisterTooltip(_grappleActionRect, Loc.Tr("Grapple: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nRequires one free hand. Success: target grappled (speed 0).\n[Left click to use]", FormatModifier(grappleBonus)));
            entryY += entryHeight;

            int shoveBonus = c.GetSkillBonus("Athletics", out _);
            _shoveActionRect = new Rectangle(nameCol, entryY, width - 20, entryHeight);
            if (_shoveActionRect.Contains(_mousePosition))
                spriteBatch.Draw(_pixel, _shoveActionRect, new Color(180, 210, 255, 120));
            spriteBatch.DrawString(_font, "Shove", new Vector2(nameCol, entryY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, FormatModifier(shoveBonus), new Vector2(atkBonusCol, entryY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, SafeString("Prone or Push 5ft"), new Vector2(damageCol, entryY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            RegisterTooltip(_shoveActionRect, Loc.Tr("Shove: Strength (Athletics) check {0} contested by target Strength (Athletics) or Dexterity (Acrobatics).\nTarget in melee range, max size: one category larger than you.\nSuccess: target knocked prone OR pushed 5 ft.\n[Left click to use]", FormatModifier(shoveBonus)));
        }
        
        return height;
    }
    
    private int DrawEquipmentBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width, Creature? creature = null)
    {
        int lineHeight = 18;

        var displayedItems = new List<(ItemInstance Item, int Count, bool IsEquipped)>();
        var itemsProcessed = new HashSet<ItemInstance>();

        foreach (var item in c.InventoryData.Items)
        {
            if (itemsProcessed.Contains(item)) continue;

            bool isEquipped = item == c.InventoryData.EquippedWeapon || item == c.InventoryData.OffhandWeapon ||
                              item == c.InventoryData.EquippedArmor || item == c.InventoryData.EquippedShield;

            if (isEquipped)
            {
                displayedItems.Add((item, 1, true));
                itemsProcessed.Add(item);
            }
            else
            {
                int count = 1;
                itemsProcessed.Add(item);
                for (int i = c.InventoryData.Items.IndexOf(item) + 1; i < c.InventoryData.Items.Count; i++)
                {
                    var other = c.InventoryData.Items[i];
                    if (itemsProcessed.Contains(other)) continue;

                    bool canGroup = false;
                    if (other.Name == item.Name)
                    {
                        bool otherIsEquipped = other == c.InventoryData.EquippedWeapon || other == c.InventoryData.OffhandWeapon ||
                                               other == c.InventoryData.EquippedArmor || other == c.InventoryData.EquippedShield;
                        if (!otherIsEquipped)
                        {
                            if (item.Name == "Torch")
                            {
                                if (other.IsLit == item.IsLit && other.RemainingMinutes == item.RemainingMinutes)
                                    canGroup = true;
                            }
                            else
                            {
                                canGroup = true;
                            }
                        }
                    }
                    if (canGroup)
                    {
                        count++;
                        itemsProcessed.Add(other);
                    }
                }
                displayedItems.Add((item, count, false));
            }
        }

        int inventoryDisplayCount = displayedItems.Count;
        int height = 60 + ((inventoryDisplayCount + 1) / 2 * lineHeight) + 40;
        height = Math.Max(150, height);

        if (spriteBatch != null)
        {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, Loc.Tr("EQUIPMENT"), new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            
            int itemY = y + 30;
            int col1 = x + 10;
            int col2 = x + width / 2 + 5;
            int currentItemY = itemY;
            bool left = true;

            foreach (var (item, count, isEquipped) in displayedItems)
            {
                int curX = left ? col1 : col2;
                var itemRect = new Rectangle(curX, currentItemY, width / 2 - 15, lineHeight);

                var itemData = ItemDatabase.GetItem(item.Name);

                int checkboxSize = 12;
                int checkboxX = curX;
                int checkboxY = currentItemY + (lineHeight - checkboxSize) / 2;
                DrawCheckbox(spriteBatch, checkboxX, checkboxY, checkboxSize, isEquipped);

                string display = item.Name;
                if (isEquipped) display += $" {Loc.Tr("(equipped)")}";
                if (item.IsLit && item.Name == "Torch") {
                    display += $" ({item.RemainingMinutes}m)";
                }
                if (count > 1) display += $" x{count}";

                Color textColor = isEquipped ? Color.DodgerBlue : (item.IsLit ? Color.OrangeRed : Color.Black * 0.8f);

                if (isEquipped)
                {
                    DrawMemeText(spriteBatch, SafeString(display), new Vector2(curX + checkboxSize + 5, currentItemY), Color.White, 0.5f);
                }
                else
                {
                    spriteBatch.DrawString(_font, SafeString(display), new Vector2(curX + checkboxSize + 5, currentItemY), textColor, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                }

                RegisterTooltip(itemRect, BuildItemTooltip(item, isEquipped));

                _inventoryItemRects.Add((itemRect, item, isEquipped, itemData?.IsEquippable ?? false));

                if (!left) currentItemY += lineHeight;
                left = !left;
            }

            int totalWeight = c.InventoryData.GetTotalWeight();
            spriteBatch.DrawString(_font, Loc.Tr("Total Weight: {0} lbs", totalWeight), new Vector2(x + 10, y + height - 35), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, Loc.Tr("Gold: {0} gp", c.GoldPieces), new Vector2(x + 10, y + height - 20), Color.DarkGoldenrod, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
        
        return height;
    }

    private void DrawHexBox(SpriteBatch spriteBatch, string label, string value, int x, int y, int width, int height, string? tooltipText = null)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        var valueSize = _font.MeasureString(value);
        spriteBatch.DrawString(_font, value, new Vector2(x + (width - valueSize.X * 1.5f) / 2, y + 10), Color.Black, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
        var labelSize = _font.MeasureString(label);
        spriteBatch.DrawString(_font, label, new Vector2(x + (width - labelSize.X * 0.5f) / 2, y + height - 15), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        if (tooltipText != null) RegisterTooltip(rect, tooltipText);
    }

    private void DrawCircleBox(SpriteBatch spriteBatch, string label, string value, int x, int y, int width, int height, string? tooltipText = null)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        var valueSize = _font.MeasureString(value);
        spriteBatch.DrawString(_font, value, new Vector2(x + (width - valueSize.X) / 2, y + 15), Color.Black);
        var labelSize = _font.MeasureString(label);
        spriteBatch.DrawString(_font, label, new Vector2(x + (width - labelSize.X * 0.5f) / 2, y + height - 15), Color.Black, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        if (tooltipText != null) RegisterTooltip(rect, tooltipText);
    }

    private void DrawHPBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        spriteBatch.DrawString(_font, Loc.Tr("Max HP"), new Vector2(x + 5, y + 5), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, c.MaxHP.ToString(), new Vector2(x + width - 40, y + 5), Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        var hpRect = new Rectangle(x + 5, y + 25, width - 10, 30);
        spriteBatch.Draw(_pixel, hpRect, new Color(220, 220, 220));
        DrawBorder(spriteBatch, hpRect, Color.Black, 1);
        string currentHpText = c.CurrentHP.ToString();
        var hpSize = _font.MeasureString(currentHpText);
        spriteBatch.DrawString(_font, currentHpText, new Vector2(x + (width - hpSize.X * 1.2f) / 2, y + 28), Color.Black, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, "POINTS DE VIE ACTUELS", new Vector2(x + 10, y + 58), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        RegisterTooltip(rect, Loc.Tr("HP: {0}/{1}", c.CurrentHP, c.MaxHP));
    }

    private void DrawTempHPBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        var tempHpRect = new Rectangle(x + 5, y + 25, width - 10, 30);
        spriteBatch.Draw(_pixel, tempHpRect, new Color(220, 220, 220));
        DrawBorder(spriteBatch, tempHpRect, Color.Black, 1);
        string tempHpText = c.TempHP > 0 ? c.TempHP.ToString() : "";
        if (tempHpText.Length > 0)
        {
            var tempHpSize = _font.MeasureString(tempHpText);
            spriteBatch.DrawString(_font, tempHpText, new Vector2(x + (width - tempHpSize.X * 1.2f) / 2, y + 28), new Color(0, 80, 200), 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);
        }
        spriteBatch.DrawString(_font, Loc.Tr("TEMPORARY HP"), new Vector2(x + 10, y + height - 15), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        string tooltip = c.TempHP > 0
            ? Loc.Tr("Temporary HP: {0}. Absorb damage before normal HP. Expire after a long rest.", c.TempHP)
            : Loc.Tr("Temporary HP: none. Granted by some spells or abilities; absorb damage before normal HP.");
        RegisterTooltip(rect, tooltip);
    }

    private void DrawHitDiceBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        spriteBatch.DrawString(_font, "Total", new Vector2(x + 5, y + 5), Color.Black * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, $"d{c.HitDiceType}", new Vector2(x + 40, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        var diceRect = new Rectangle(x + 5, y + 25, width - 10, 30);
        spriteBatch.Draw(_pixel, diceRect, new Color(220, 220, 220));
        DrawBorder(spriteBatch, diceRect, Color.Black, 1);
        string diceText = $"{c.HitDiceRemaining}/{c.HitDiceTotal}";
        var diceSize = _font.MeasureString(diceText);
        spriteBatch.DrawString(_font, diceText, new Vector2(x + (width - diceSize.X) / 2, y + 30), Color.Black);
        spriteBatch.DrawString(_font, Loc.Tr("HIT DICE"), new Vector2(x + 10, y + height - 15), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        RegisterTooltip(rect, Loc.Tr("Hit dice: {0} (d{1}). Used during short rests.", diceText, c.HitDiceType));
    }

    private void DrawDeathSavesBox(SpriteBatch spriteBatch, Character c, int x, int y, int width, int height)
    {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        spriteBatch.DrawString(_font, Loc.Tr("DEATH SAVES"), new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        int circleSize = 16;
        int startY = y + 30;
        spriteBatch.DrawString(_font, Loc.Tr("Successes"), new Vector2(x + 10, startY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        for (int i = 0; i < 3; i++) {
            int cx = x + width - 70 + i * 22;
            DrawCircle(spriteBatch, cx, startY, circleSize, Color.Black, 1);
            if (i < c.DeathSaveSuccesses) spriteBatch.Draw(_pixel, new Rectangle(cx + 4, startY + 4, circleSize - 8, circleSize - 8), Color.Black);
        }
        startY += 25;
        spriteBatch.DrawString(_font, Loc.Tr("Failures"), new Vector2(x + 10, startY), Color.Black * 0.7f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        for (int i = 0; i < 3; i++) {
            int cx = x + width - 70 + i * 22;
            DrawCircle(spriteBatch, cx, startY, circleSize, Color.Black, 1);
            if (i < c.DeathSaveFailures) spriteBatch.Draw(_pixel, new Rectangle(cx + 4, startY + 4, circleSize - 8, circleSize - 8), Color.Black);
        }
        RegisterTooltip(rect, Loc.Tr("Death saves: {0} successes, {1} failures.", c.DeathSaveSuccesses, c.DeathSaveFailures));
    }

    private int DrawSkillsBox(SpriteBatch? spriteBatch, Character c, int x, int y, int width)
    {
        int lineHeight = 18;
        var skills = new[] {
            ("Acrobatics", "DEX"), ("Animal Handling", "WIS"), ("Arcana", "INT"), ("Athletics", "STR"),
            ("Deception", "CHA"), ("History", "INT"), ("Insight", "WIS"), ("Intimidation", "CHA"),
            ("Investigation", "INT"), ("Medicine", "WIS"), ("Nature", "INT"), ("Perception", "WIS"),
            ("Performance", "CHA"), ("Persuasion", "CHA"), ("Religion", "INT"), ("Sleight of Hand", "DEX"),
            ("Stealth", "DEX"), ("Survival", "WIS")
        };
        int height = 25 + (skills.Length * lineHeight) + 10;

        if (spriteBatch != null) {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, Loc.Tr("SKILLS"), new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            int skillY = y + 25;
            foreach (var (skillKey, ability) in skills) {
                int bonus = c.GetSkillBonus(skillKey, out _);
                bool proficient = GetSkillProficiency(c, skillKey);
                DrawCheckbox(spriteBatch, x + 8, skillY, 10, proficient);
                string bonusText = FormatModifier(bonus);
                string localizedSkill = Loc.Tr(skillKey);
                spriteBatch.DrawString(_font, bonusText, new Vector2(x + 25, skillY - 2), Color.Black, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, localizedSkill, new Vector2(x + 60, skillY - 2), proficient ? Color.Black : Color.Black * 0.6f, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, $"({Loc.Tr(ability)})", new Vector2(x + width - 45, skillY - 2), Color.Black * 0.5f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                RegisterTooltip(new Rectangle(x + 4, skillY - 2, width - 8, lineHeight), Loc.Tr("{0} ({1}): {2}", localizedSkill, Loc.Tr(ability), bonusText));
                skillY += lineHeight;
            }
        }
        return height;
    }

    private int DrawTextBox(SpriteBatch? spriteBatch, string label, string content, int x, int y, int width)
    {
        string wrappedContent = WrapText(_font, content, width - 20, 0.6f);
        var contentSize = _font.MeasureString(wrappedContent) * 0.6f;
        int height = (int)Math.Max(60, 35 + contentSize.Y);

        if (spriteBatch != null) {
            var rect = new Rectangle(x, y, width, height);
            spriteBatch.Draw(_pixel, rect, Color.White);
            DrawBorder(spriteBatch, rect, Color.Black, 2);
            spriteBatch.DrawString(_font, label, new Vector2(x + 5, y + 5), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            if (!string.IsNullOrEmpty(wrappedContent))
                spriteBatch.DrawString(_font, wrappedContent, new Vector2(x + 10, y + 25), Color.Black, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            RegisterTooltip(rect, string.IsNullOrWhiteSpace(content)
                ? Loc.Tr("{0}: no note provided.", label)
                : $"{label}: {content}");
        }
        return height;
    }

    private void RegisterTooltip(Rectangle area, string text) {
        if (_showItemContextMenu) return;
        if (_hoverTooltip == null && area.Contains(_mousePosition)) _hoverTooltip = text;
    }

    private void DrawTooltip(SpriteBatch spriteBatch, Viewport viewport) {
        if (_showItemContextMenu) return;
        if (string.IsNullOrWhiteSpace(_hoverTooltip)) return;
        const int padding = 8;
        var safeTooltip = SafeString(_hoverTooltip);
        var textSize = _font.MeasureString(safeTooltip) * 0.8f;
        int width = (int)textSize.X + padding * 2;
        int height = (int)textSize.Y + padding * 2;
        int x = _mousePosition.X + 16, y = _mousePosition.Y + 18;
        if (x + width > viewport.Width - 4) x = viewport.Width - width - 4;
        if (y + height > viewport.Height - 4) y = _mousePosition.Y - height - 8;
        x = Math.Max(4, x); y = Math.Max(4, y);
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, new Color(30, 30, 30, 240));
        DrawBorder(spriteBatch, rect, new Color(220, 220, 220), 1);
        spriteBatch.DrawString(_font, safeTooltip, new Vector2(x + padding, y + padding), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private Rectangle BuildContextMenuRect(Point mousePosition, Character character)
    {
        const int width = 180;
        const int optionHeight = 30;
        int optionCount = GetContextMenuOptions(character).Length;
        return new Rectangle(mousePosition.X, mousePosition.Y, width, optionHeight * optionCount);
    }

    private string? GetContextMenuOptionAt(Point position, Character character)
    {
        if (!_itemContextMenuRect.Contains(position)) return null;

        int relativeY = position.Y - _itemContextMenuRect.Y;
        int optionIndex = relativeY / 30;
        var options = GetContextMenuOptions(character);
        return optionIndex >= 0 && optionIndex < options.Length ? options[optionIndex] : null;
    }

    private string[] GetContextMenuOptions(Character character)
    {
        var options = new List<string>();

        if (_contextItem?.Name == "Lute")
        {
            options.Add("Play");
        }

        if (_contextItem != null && _contextItem.Name == "Torch" && !_contextItem.IsLit)
        {
            options.Add("Light");
        }

        if (_contextItemIsEquipped)
        {
            options.Add("Unequip");
        }
        else if (_contextItemIsEquippable)
        {
            options.Add("Equip");
            if (_contextItemIsLight)
            {
                options.Add("Equip (Offhand)");
            }
        }

        options.Add("Drop");
        options.Add("Throw");
        options.Add("Inspect");

        return options.ToArray();
    }

    private void DrawWeaponContextMenu(SpriteBatch spriteBatch, Viewport viewport, Character character)
    {
        if (!_showItemContextMenu || _contextItem == null) return;

        int menuX = Math.Clamp(_itemContextMenuRect.X, 8, viewport.Width - _itemContextMenuRect.Width - 8);
        int menuY = Math.Clamp(_itemContextMenuRect.Y, 8, viewport.Height - _itemContextMenuRect.Height - 8);
        _itemContextMenuRect = new Rectangle(menuX, menuY, _itemContextMenuRect.Width, _itemContextMenuRect.Height);

        spriteBatch.Draw(_pixel, _itemContextMenuRect, new Color(35, 35, 35, 245));
        DrawBorder(spriteBatch, _itemContextMenuRect, new Color(220, 220, 220), 1);

        var options = GetContextMenuOptions(character);
        for (int i = 0; i < options.Length; i++)
        {
            var optionRect = new Rectangle(_itemContextMenuRect.X, _itemContextMenuRect.Y + i * 30, _itemContextMenuRect.Width, 30);
            bool hovered = optionRect.Contains(_mousePosition);
            bool enabled = true;

            if (options[i] == "Light")
            {
                bool hasTinderbox = character.InventoryData.HasItem("Tinderbox");
                bool hasOtherLitTorch = character.InventoryData.Items.Any(it => it.Name == "Torch" && it.IsLit && it != _contextItem);
                bool hasFireSpell = character.KnownSpells.Any(s => s.DamageType == DamageType.Fire) ||
                                    character.PreparedSpells.Any(s => s.DamageType == DamageType.Fire);

                if (!hasTinderbox && !hasOtherLitTorch && !hasFireSpell)
                {
                    enabled = false;
                }
            }

            if (hovered && enabled)
            {
                spriteBatch.Draw(_pixel, optionRect, new Color(90, 90, 90, 230));
            }

            spriteBatch.DrawString(_font, SafeString(Loc.Tr(options[i])), new Vector2(optionRect.X + 10, optionRect.Y + 7), enabled ? Color.White : Color.Gray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
    }

    private void DrawInspectPopup(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (string.IsNullOrWhiteSpace(_inspectWeaponText)) return;

        string title = Loc.Tr("Inspect Weapon");
        string content = WrapText(_font, SafeString(_inspectWeaponText), 320, 0.65f);
        int width = 360;
        int height = 160;
        int x = (viewport.Width - width) / 2;
        int y = (viewport.Height - height) / 2;
        _inspectPopupRect = new Rectangle(x, y, width, height);

        spriteBatch.Draw(_pixel, _inspectPopupRect, new Color(20, 20, 20, 245));
        DrawBorder(spriteBatch, _inspectPopupRect, Color.White, 2);
        spriteBatch.DrawString(_font, SafeString(title), new Vector2(x + 12, y + 10), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, content, new Vector2(x + 12, y + 40), Color.White * 0.95f, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, Loc.Tr("(Click outside to close)"), new Vector2(x + 12, y + height - 24), Color.White * 0.6f, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }

    private void DrawSmallBox(SpriteBatch spriteBatch, string label, string value, int x, int y, int width, int height, string? tooltipText = null) {
        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White);
        DrawBorder(spriteBatch, rect, Color.Black, 2);
        if (!string.IsNullOrEmpty(value)) {
            var vs = _font.MeasureString(value);
            spriteBatch.DrawString(_font, value, new Vector2(x + (width - vs.X) / 2, y + 8), Color.Black);
        }
        var ls = _font.MeasureString(label);
        spriteBatch.DrawString(_font, label, new Vector2(x + (width - ls.X * 0.45f) / 2, y + height - 12), Color.Black, 0f, Vector2.Zero, 0.45f, SpriteEffects.None, 0f);
        if (tooltipText != null) RegisterTooltip(rect, tooltipText);
    }

    private void DrawCheckbox(SpriteBatch spriteBatch, int x, int y, int size, bool checked_) {
        var box = new Rectangle(x, y, size, size);
        spriteBatch.Draw(_pixel, box, Color.White);
        DrawBorder(spriteBatch, box, Color.Black, 1);
        if (checked_) spriteBatch.Draw(_pixel, new Rectangle(x + 2, y + 2, size - 4, size - 4), Color.Black);
    }

    private void DrawCircle(SpriteBatch spriteBatch, int x, int y, int size, Color color, int thickness) {
        int r = size / 2;
        for (int dy = 0; dy < size; dy++) {
            for (int dx = 0; dx < size; dx++) {
                float dist = (float)Math.Sqrt((dx - r) * (dx - r) + (dy - r) * (dy - r));
                if (dist >= r - thickness && dist <= r) spriteBatch.Draw(_pixel, new Rectangle(x + dx, y + dy, 1, 1), color);
            }
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness) {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private string FormatModifier(int value) => value >= 0 ? $"+{value}" : $"{value}";

    private string FormatModifier(string value) => value.StartsWith("+") || value.StartsWith("-") ? value : "+" + value;

    private void DrawMemeText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 0.6f)
    {
        Vector2[] offsets = {
            new(-1, -1), new(0, -1), new(1, -1),
            new(-1, 0),             new(1, 0),
            new(-1, 1),  new(0, 1),  new(1, 1)
        };
        foreach (var offset in offsets)
        {
            spriteBatch.DrawString(_font, text, position + offset, Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private string SafeString(string? text) {
        if (_font == null || text == null) return text ?? "";
        var chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++) if (!_supportedChars.Contains(chars[i])) chars[i] = '?';
        return new string(chars);
    }
    
    private string GetWeaponDamage(string weapon) {
        var item = ItemDatabase.GetItem(weapon);
        return (item != null && !string.IsNullOrEmpty(item.DamageDice)) ? $"{item.DamageDice} {item.DamageType.ToDisplayString()}" : "1d6";
    }

    private string BuildWeaponTooltip(string weaponName, int attackBonus, string damage) {
        var item = ItemDatabase.GetItem(weaponName);
        if (item == null) return weaponName;
        string rangeInfo = item.IsRanged ? Loc.Tr("Range: {0}/{1} ft.", item.Range, (item.LongRange > 0 ? item.LongRange : item.Range * 3)) : Loc.Tr("Melee Range: 5 ft.");
        return Loc.Tr("{0} {1}\nBonus: {2}\nDamage: {3}\n{4}", weaponName, Loc.Tr("(equipped)"), FormatModifier(attackBonus), damage, rangeInfo);
    }

    private string BuildItemTooltip(ItemInstance itemInstance, bool isEquipped = false) {
        var itemData = ItemDatabase.GetItem(itemInstance.Name);
        if (itemData == null) return itemInstance.Name;
        string eq = isEquipped ? $" {Loc.Tr("(equipped)")}" : "";
        string details = Loc.Tr("{0}{1}\nWeight: {2} lbs | Value: {3} gp", itemData.Name, eq, itemData.Weight, itemData.Value);
        if (itemInstance.Name == "Torch" && itemInstance.IsLit)
        {
            details += $"\n{Loc.Tr("Lit (Remaining: {0}m)", itemInstance.RemainingMinutes)}";
        }
        if (!string.IsNullOrWhiteSpace(itemData.Description)) details += $"\n{itemData.Description}";
        return details;
    }

    private string WrapText(SpriteFont font, string text, float maxLineWidth, float scale = 0.7f) {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(' ');
        string result = "", currentLine = "";
        foreach (string word in words) {
            if (font.MeasureString(currentLine + word).X * scale < maxLineWidth) currentLine += word + " ";
            else { result += currentLine + "\n"; currentLine = word + " "; }
        }
        return result + currentLine;
    }

    private bool GetSkillProficiency(Character c, string skill) => skill switch {
        "Acrobatics" => c.AcrobaticsProficiency, "Animal Handling" => c.AnimalHandlingProficiency,
        "Arcana" => c.ArcanaProficiency, "Athletics" => c.AthleticsProficiency,
        "Deception" => c.DeceptionProficiency, "History" => c.HistoryProficiency,
        "Insight" => c.InsightProficiency, "Intimidation" => c.IntimidationProficiency,
        "Investigation" => c.InvestigationProficiency, "Medicine" => c.MedicineProficiency,
        "Nature" => c.NatureProficiency, "Perception" => c.PerceptionProficiency,
        "Performance" => c.PerformanceProficiency, "Persuasion" => c.PersuasionProficiency,
        "Religion" => c.ReligionProficiency, "Sleight of Hand" => c.SleightOfHandProficiency,
        "Stealth" => c.StealthProficiency, "Survival" => c.SurvivalProficiency,
        _ => false
    };
}
