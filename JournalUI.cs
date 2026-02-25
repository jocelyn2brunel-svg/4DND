#nullable enable
using System;
using System.Collections.Generic;
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
    private const int Margin = 40;
    private const int Padding = 20;
    private const int ScrollbarWidth = 20;
    private const int CloseButtonWidth = 120;
    private const int CloseButtonHeight = 36;
    private Point _mousePosition;
    private HashSet<char> _supportedChars;

    public JournalUI(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
        _supportedChars = font != null ? new HashSet<char>(font.Characters) : new HashSet<char>();
    }

    public void Update(MouseState mouse)
    {
        _mousePosition = mouse.Position;

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

        // Background overlay
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(15, 15, 15, 230));

        int journalWidth = Math.Min(1000, vp.Width - Margin * 2);
        int journalHeight = vp.Height - Margin * 2;
        int journalX = (vp.Width - journalWidth) / 2;
        int journalY = Margin;

        // Paper background
        var journalRect = new Rectangle(journalX, journalY, journalWidth, journalHeight);
        spriteBatch.Draw(_pixel, journalRect, new Color(245, 240, 225));
        DrawBorder(spriteBatch, journalRect, new Color(60, 40, 20), 3);

        // Header
        int headerHeight = 80;
        var headerRect = new Rectangle(journalX, journalY, journalWidth, headerHeight);
        spriteBatch.Draw(_pixel, headerRect, new Color(80, 60, 40));

        string title = Loc.Tr("ADVENTURE JOURNAL: {0}", campaign.Name.ToUpper());
        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title, new Vector2(journalX + (journalWidth - titleSize.X) / 2, journalY + (headerHeight - titleSize.Y) / 2), Color.Gold);

        // Content area with scissor
        var contentRect = new Rectangle(journalX + Padding, journalY + headerHeight + Padding, journalWidth - Padding * 2, journalHeight - headerHeight - Padding * 2);
        var previousScissor = graphics.ScissorRectangle;

        // Calculate total content height first
        int currentY = 0;
        int wrapWidth = contentRect.Width - 40;

        // Measure sections
        int hookHeight = MeasureSection(Loc.Tr("THE BEGINNING (HOOK)"), campaign.AdventureHook, wrapWidth);
        int middleHeight = MeasureSection(Loc.Tr("THE DEVELOPMENT"), campaign.AdventureMiddle, wrapWidth);
        int endingHeight = MeasureSection(Loc.Tr("THE CONCLUSION (CLIMAX)"), campaign.AdventureEnding, wrapWidth);

        int totalContentHeight = hookHeight + middleHeight + endingHeight + 60; // Extra spacing
        int maxScroll = Math.Max(0, totalContentHeight - contentRect.Height);
        _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, maxScroll);

        graphics.ScissorRectangle = Rectangle.Intersect(new Rectangle(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height), graphics.Viewport.Bounds);

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: new RasterizerState { ScissorTestEnable = true });

        int drawY = contentRect.Y - (int)_scrollOffset;

        drawY += DrawSection(spriteBatch, Loc.Tr("THE BEGINNING (HOOK)"), campaign.AdventureHook, contentRect.X, drawY, wrapWidth, Color.DarkBlue);
        drawY += 30;
        drawY += DrawSection(spriteBatch, Loc.Tr("THE DEVELOPMENT"), campaign.AdventureMiddle, contentRect.X, drawY, wrapWidth, Color.DarkGreen);
        drawY += 30;
        drawY += DrawSection(spriteBatch, Loc.Tr("THE CONCLUSION (CLIMAX)"), campaign.AdventureEnding, contentRect.X, drawY, wrapWidth, Color.DarkRed);

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        graphics.ScissorRectangle = previousScissor;

        // Scrollbar
        if (maxScroll > 0)
        {
            DrawScrollbar(spriteBatch, journalX + journalWidth - 15, journalY + headerHeight + 5, 10, journalHeight - headerHeight - 10, maxScroll, totalContentHeight);
        }

        // Hint
        string hint = Loc.Tr("Press 'J' to close | Mouse wheel to scroll");
        var hintSize = _font.MeasureString(hint) * 0.7f;
        spriteBatch.DrawString(_font, hint, new Vector2((vp.Width - hintSize.X) / 2, vp.Height - 30), Color.White * 0.8f, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        DrawCloseButton(spriteBatch, vp);
    }

    private int MeasureSection(string title, string content, int width)
    {
        int height = 40; // Title + Padding
        string wrapped = WrapText(_font, SafeString(content), width);
        var size = _font.MeasureString(wrapped) * 0.8f;
        height += (int)size.Y + 20;
        return Math.Max(120, height);
    }

    private int DrawSection(SpriteBatch spriteBatch, string title, string content, int x, int y, int width, Color titleColor)
    {
        string wrapped = WrapText(_font, SafeString(content), width);
        var size = _font.MeasureString(wrapped) * 0.8f;
        int sectionHeight = Math.Max(120, 40 + (int)size.Y + 20);

        var rect = new Rectangle(x, y, width, sectionHeight);
        spriteBatch.Draw(_pixel, rect, Color.Black * 0.05f);
        DrawBorder(spriteBatch, rect, Color.Black * 0.1f, 1);

        spriteBatch.DrawString(_font, title, new Vector2(x + 10, y + 5), titleColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        if (!string.IsNullOrEmpty(content))
        {
            spriteBatch.DrawString(_font, wrapped, new Vector2(x + 15, y + 35), Color.Black, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
        else
        {
            spriteBatch.DrawString(_font, Loc.Tr("No detail available."), new Vector2(x + 15, y + 35), Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        return sectionHeight;
    }

    public Rectangle GetCloseButtonRect(Viewport viewport)
    {
        return new Rectangle(viewport.Width - Margin - CloseButtonWidth, Margin + 20, CloseButtonWidth, CloseButtonHeight);
    }

    private void DrawCloseButton(SpriteBatch spriteBatch, Viewport viewport)
    {
        var rect = GetCloseButtonRect(viewport);
        spriteBatch.Draw(_pixel, rect, new Color(140, 40, 40));
        DrawBorder(spriteBatch, rect, Color.Black, 2);

        string text = Loc.Tr("Close");
        var size = _font.MeasureString(text) * 0.8f;
        spriteBatch.DrawString(_font, text, new Vector2(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawScrollbar(SpriteBatch spriteBatch, int x, int y, int width, int height, int maxScroll, int contentHeight)
    {
        var trackRect = new Rectangle(x, y, width, height);
        spriteBatch.Draw(_pixel, trackRect, Color.Black * 0.2f);

        float ratio = (float)height / contentHeight;
        int thumbHeight = Math.Max(40, (int)(height * ratio));
        int thumbY = y + (int)((height - thumbHeight) * (_scrollOffset / maxScroll));

        spriteBatch.Draw(_pixel, new Rectangle(x, thumbY, width, thumbHeight), Color.Gray);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private string WrapText(SpriteFont font, string text, float maxLineWidth)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] words = text.Split(' ');
        string result = "";
        string currentLine = "";

        foreach (string word in words)
        {
            if (font.MeasureString(currentLine + word).X * 0.8f < maxLineWidth)
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

    private string SafeString(string? text)
    {
        if (_font == null || text == null) return text ?? "";
        var chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (!_supportedChars.Contains(chars[i]))
                chars[i] = '?';
        return new string(chars);
    }
}
