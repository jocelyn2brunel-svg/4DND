using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _4DND;

public class MainMenuUI
{
    private SpriteFont _font;
    private Texture2D _pixel;
    private readonly string[] _mainMenuItems = new[] { "Single Player", "Multiplayer", "Options", "Desktop" };
    private int _selectedIndex = 0;

    public MainMenuUI(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public int SelectedIndex => _selectedIndex;

    public string? Update(InputManager input, Viewport vp)
    {
        if (input.IsKeyPressed(Keys.Up))
            _selectedIndex = (_selectedIndex - 1 + _mainMenuItems.Length) % _mainMenuItems.Length;
        if (input.IsKeyPressed(Keys.Down))
            _selectedIndex = (_selectedIndex + 1) % _mainMenuItems.Length;

        int menuWidth = 480;
        int itemHeight = 48;
        int padding = 12;
        int titleHeight = 120;
        int menuHeight = titleHeight + _mainMenuItems.Length * (itemHeight + padding) + padding;
        var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

        for (int i = 0; i < _mainMenuItems.Length; i++)
        {
            var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
            if (itemRect.Contains(input.MousePosition))
            {
                _selectedIndex = i;
                if (input.IsLeftClick())
                    return _mainMenuItems[i];
            }
        }

        if (input.IsKeyPressed(Keys.Enter))
            return _mainMenuItems[_selectedIndex];

        return null;
    }

    public void Draw(SpriteBatch spriteBatch, Viewport vp)
    {
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.85f);

        int menuWidth = 480;
        int itemHeight = 48;
        int padding = 12;
        int titleHeight = 120;
        int menuHeight = titleHeight + _mainMenuItems.Length * (itemHeight + padding) + padding;
        var menuRect = new Rectangle((vp.Width - menuWidth) / 2, (vp.Height - menuHeight) / 2, menuWidth, menuHeight);

        spriteBatch.Draw(_pixel, menuRect, Color.DarkSlateGray * 0.95f);

        if (_font != null)
        {
            string title = "4DND";
            var titleSize = _font.MeasureString(title);
            var titlePos = new Vector2(menuRect.X + (menuWidth - titleSize.X * 2) / 2, menuRect.Y + 12);
            spriteBatch.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        }

        for (int i = 0; i < _mainMenuItems.Length; i++)
        {
            var itemRect = new Rectangle(menuRect.X + padding, menuRect.Y + titleHeight + padding + i * (itemHeight + padding), menuWidth - padding * 2, itemHeight);
            var col = (i == _selectedIndex) ? Color.LightGray : Color.Gray;
            spriteBatch.Draw(_pixel, itemRect, col);

            if (_font != null)
            {
                var text = _mainMenuItems[i];
                var size = _font.MeasureString(text);
                var pos = new Vector2(itemRect.X + (itemRect.Width - size.X) / 2, itemRect.Y + (itemRect.Height - size.Y) / 2);
                var textCol = (i == _selectedIndex) ? Color.Black : Color.White;
                spriteBatch.DrawString(_font, text, pos, textCol);
            }
        }
    }
}
