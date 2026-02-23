#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _4DND;

public class JournalUI
{
    private SpriteFont _font;
    private Texture2D _pixel;
    private float _scrollOffset = 0f;
    private int _prevScrollValue = 0;
    private const int Margin = 50;
    private const int ScrollbarWidth = 20;
    private const int CloseButtonWidth = 120;
    private const int CloseButtonHeight = 36;

    public JournalUI(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Update(MouseState mouse)
    {
        if (_prevScrollValue == 0)
        {
            _prevScrollValue = mouse.ScrollWheelValue;
            return;
        }

        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta * 0.5f;
            _prevScrollValue = mouse.ScrollWheelValue;
        }
    }

    public void ResetScroll()
    {
        _scrollOffset = 0f;
        _prevScrollValue = 0;
    }

    public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphics, Campaign campaign)
    {
        var vp = graphics.Viewport;

        // Dark overlay background
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.7f);

        int sheetWidth = vp.Width - Margin * 2;
        int sheetHeight = vp.Height - Margin * 2;
        int sheetX = Margin;
        int sheetY = Margin;

        var sheetRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
        spriteBatch.Draw(_pixel, sheetRect, new Color(30, 35, 45));
        DrawBorder(spriteBatch, sheetRect, Color.Gold * 0.5f, 2);

        // Calculate content height
        int padding = 30;
        int currentY = padding;

        // Title
        string title = "ADVENTURE JOURNAL";
        var titleSize = _font.MeasureString(title) * 1.5f;

        // Sections calculation
        int textWidth = sheetWidth - padding * 2 - ScrollbarWidth;

        int hHook = CalculateSectionHeight("THE BEGINNING (HOOK)", campaign.AdventureHook, textWidth);
        int hMiddle = CalculateSectionHeight("THE MIDDLE (DEVELOPMENT)", campaign.AdventureMiddle, textWidth);
        int hEnding = CalculateSectionHeight("THE ENDING (CLIMAX)", campaign.AdventureEnding, textWidth);

        int totalContentHeight = padding + (int)titleSize.Y + 40 + hHook + 20 + hMiddle + 20 + hEnding + padding;

        int maxScroll = System.Math.Max(0, totalContentHeight - sheetHeight);
        _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, maxScroll);

        var scissorRect = new Rectangle(sheetX, sheetY, sheetWidth, sheetHeight);
        var previousScissor = graphics.ScissorRectangle;
        graphics.ScissorRectangle = Rectangle.Intersect(previousScissor, scissorRect);

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = true });

        int drawY = sheetY + padding - (int)_scrollOffset;

        spriteBatch.DrawString(_font, title, new Vector2(sheetX + (sheetWidth - titleSize.X) / 2, drawY), Color.Gold, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
        drawY += (int)titleSize.Y + 40;

        drawY += DrawSection(spriteBatch, "THE BEGINNING (HOOK)", campaign.AdventureHook, sheetX + padding, drawY, textWidth, Color.SkyBlue);
        drawY += 20;
        drawY += DrawSection(spriteBatch, "THE MIDDLE (DEVELOPMENT)", campaign.AdventureMiddle, sheetX + padding, drawY, textWidth, Color.LightGreen);
        drawY += 20;
        drawY += DrawSection(spriteBatch, "THE ENDING (CLIMAX)", campaign.AdventureEnding, sheetX + padding, drawY, textWidth, Color.Orange);

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        graphics.ScissorRectangle = previousScissor;

        if (totalContentHeight > sheetHeight)
        {
            DrawScrollbar(spriteBatch, sheetX + sheetWidth - ScrollbarWidth - 5, sheetY + 5, ScrollbarWidth, sheetHeight - 10, maxScroll, totalContentHeight);
        }

        DrawCloseHint(spriteBatch, vp);
    }

    private int CalculateSectionHeight(string title, string content, int width)
    {
        string wrapped = WrapText(_font, content, width - 20, 0.9f);
        var contentSize = _font.MeasureString(wrapped) * 0.9f;
        return (int)System.Math.Max(100, 40 + contentSize.Y);
    }

    private int DrawSection(SpriteBatch spriteBatch, string title, string content, int x, int y, int width, Color titleColor)
    {
        string wrapped = WrapText(_font, content, width - 20, 0.9f);
        var contentSize = _font.MeasureString(wrapped) * 0.9f;
        int height = (int)System.Math.Max(100, 40 + contentSize.Y);

        var rect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, rect, Color.White * 0.05f);
        DrawBorder(spriteBatch, rect, Color.White * 0.1f, 1);

        spriteBatch.DrawString(_font, title, new Vector2(x + 10, y + 5), titleColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        if (!string.IsNullOrEmpty(wrapped))
        {
            spriteBatch.DrawString(_font, wrapped, new Vector2(x + 15, y + 30), Color.White * 0.9f, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }
        else
        {
            spriteBatch.DrawString(_font, "No details yet for this part of the adventure.", new Vector2(x + 15, y + 30), Color.Gray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        return height;
    }

    private void DrawScrollbar(SpriteBatch spriteBatch, int x, int y, int width, int height, int maxScroll, int contentHeight)
    {
        var trackRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, trackRect, Color.Black * 0.3f);

        float contentRatio = (float)height / contentHeight;
        int thumbHeight = (int)(height * contentRatio);
        thumbHeight = System.Math.Max(thumbHeight, 30);

        float scrollRatio = maxScroll > 0 ? _scrollOffset / maxScroll : 0;
        int thumbY = y + (int)((height - thumbHeight) * scrollRatio);

        var thumbRect = new Rectangle(x + 2, thumbY, width - 4, thumbHeight);
        spriteBatch.Draw(_pixel, thumbRect, Color.Gold * 0.4f);
    }

    private void DrawCloseHint(SpriteBatch spriteBatch, Viewport viewport)
    {
        var hint = "Press J or ESC to close | Mouse wheel to scroll";
        var hintSize = _font.MeasureString(hint);
        spriteBatch.DrawString(_font, hint, new Vector2((viewport.Width - hintSize.X * 0.8f) / 2, viewport.Height - 35), Color.White * 0.7f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private string WrapText(SpriteFont font, string text, float maxLineWidth, float scale)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(' ');
        string result = "";
        string currentLine = "";

        foreach (string word in words)
        {
            if (font.MeasureString(currentLine + word).X * scale < maxLineWidth)
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
