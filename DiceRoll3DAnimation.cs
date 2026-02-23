using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace _4DND;

/// <summary>
/// Lightweight pseudo-3D dice animation rendered in the HUD.
/// It uses a rotating cube wireframe so the player gets visual feedback when a d20 roll happens.
/// </summary>
public class DiceRoll3DAnimation
{
    private readonly List<(int A, int B)> _edges = new()
    {
        (0,1), (1,2), (2,3), (3,0),
        (4,5), (5,6), (6,7), (7,4),
        (0,4), (1,5), (2,6), (3,7)
    };

    private readonly Vector3[] _cubeVertices =
    {
        new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
        new(-1, -1,  1), new(1, -1,  1), new(1, 1,  1), new(-1, 1,  1)
    };

    private float _remainingTime;
    private float _elapsed;
    private int _lastRoll;

    public bool IsActive => _remainingTime > 0f;

    public void Start(int roll, float duration = 1.25f)
    {
        _lastRoll = roll;
        _elapsed = 0f;
        _remainingTime = duration;
    }

    public void Update(float deltaSeconds)
    {
        if (!IsActive)
            return;

        _elapsed += deltaSeconds;
        _remainingTime -= deltaSeconds;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont? font, Rectangle viewport)
    {
        if (!IsActive)
            return;

        var center = new Vector2(viewport.Width - 180, viewport.Height - 150);
        float pulse = 1f + 0.1f * MathF.Sin(_elapsed * 8f);
        float scale = 28f * pulse;

        Matrix rotation =
            Matrix.CreateRotationY(_elapsed * 4.4f) *
            Matrix.CreateRotationX(_elapsed * 3.1f) *
            Matrix.CreateRotationZ(_elapsed * 2.3f);

        var projected = new Vector2[_cubeVertices.Length];
        var depth = new float[_cubeVertices.Length];

        for (int i = 0; i < _cubeVertices.Length; i++)
        {
            Vector3 v = Vector3.Transform(_cubeVertices[i], rotation);
            float z = v.Z + 3.5f;
            float perspective = 1.4f / z;
            projected[i] = center + new Vector2(v.X, v.Y) * scale * perspective;
            depth[i] = z;
        }

        foreach (var (a, b) in _edges)
        {
            float edgeDepth = (depth[a] + depth[b]) * 0.5f;
            float alpha = MathHelper.Clamp(1.6f - edgeDepth * 0.35f, 0.25f, 1f);
            DrawLine(spriteBatch, pixel, projected[a], projected[b], Color.Gold * alpha, 2f);
        }

        if (font != null)
        {
            string text = $"d20: {_lastRoll}";
            Vector2 size = font.MeasureString(text);
            Vector2 textPos = center + new Vector2(-size.X / 2f, 44f);
            spriteBatch.DrawString(font, text, textPos, Color.Gold);
        }
    }

    private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= float.Epsilon)
            return;

        float angle = MathF.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }
}
