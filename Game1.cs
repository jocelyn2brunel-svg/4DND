using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace _4DND;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private InfiniteGrid<bool> _grid = new();
    private Texture2D _pixel = null!; // simple 1x1 white texture
    private int _cellSize = 24;
    private Vector2 _camera = Vector2.Zero; // pixel offset applied to origin
    private float _zoom = 1f;
    private int _prevScrollValue = 0;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // create 1x1 white pixel
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // sample content: a small pattern and a cross
        for (int x = -10; x <= 10; x++)
            _grid.Set(x, 0, x % 2 == 0);

        for (int y = -6; y <= 6; y++)
        {
            _grid.Set(0, y, true);
            _grid.Set(1, y, (y % 3) == 0);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var kb = Keyboard.GetState();
        float speed = 400f * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A)) _camera.X += speed;
        if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) _camera.X -= speed;
        if (kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W)) _camera.Y += speed;
        if (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S)) _camera.Y -= speed;

        // Handle mouse wheel zoom
        var mouse = Mouse.GetState();
        int scrollDelta = mouse.ScrollWheelValue - _prevScrollValue;
        if (scrollDelta != 0)
        {
            _zoom += scrollDelta * 0.001f;
            _zoom = MathHelper.Clamp(_zoom, 0.1f, 5f);
            _prevScrollValue = mouse.ScrollWheelValue;
        }

        base.Update(gameTime);
    }

    private void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        float distance = Vector2.Distance(start, end);
        float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        sb.Draw(pixel, start, null, color, angle, Vector2.Zero, new Vector2(distance, thickness), SpriteEffects.None, 0f);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var vp = GraphicsDevice.Viewport;
        var screenCenter = new Vector2(vp.Width / 2f, vp.Height / 2f);
        var origin = screenCenter + _camera;

        float tileW = _cellSize * _zoom;
        float tileH = _cellSize * 0.5f * _zoom;

        int range = (int)((Math.Max(vp.Width, vp.Height) / Math.Min(tileW, tileH))) + 6;
        int xmin = -range, xmax = range;
        int ymin = -range, ymax = range;

        // draw grid lines in isometric projection
        for (int y = ymin; y <= ymax; y++)
        {
            // horizontal lines (along y axis)
            var start = origin + new Vector2((xmin - y) * tileW * 0.5f, (xmin + y) * tileH * 0.5f);
            var end = origin + new Vector2((xmax - y) * tileW * 0.5f, (xmax + y) * tileH * 0.5f);
            DrawLine(_spriteBatch, _pixel, start, end, Color.White, 1f);
        }

        for (int x = xmin; x <= xmax; x++)
        {
            // vertical lines (along x axis)
            var start = origin + new Vector2((x - ymin) * tileW * 0.5f, (x + ymin) * tileH * 0.5f);
            var end = origin + new Vector2((x - ymax) * tileW * 0.5f, (x + ymax) * tileH * 0.5f);
            DrawLine(_spriteBatch, _pixel, start, end, Color.White, 1f);
        }

        var originCenter = origin + new Vector2(0, 0);
        
        // Draw world axes from origin
        float axisLength = 100f * _zoom;
        
        // X axis (red)
        DrawLine(_spriteBatch, _pixel, originCenter, 
                 originCenter + new Vector2(axisLength * 0.5f, axisLength * 0.25f), 
                 Color.Red, 2f);
        
        // Y axis (green)
        DrawLine(_spriteBatch, _pixel, originCenter, 
                 originCenter + new Vector2(-axisLength * 0.5f, axisLength * 0.25f), 
                 Color.Lime, 2f);
        
        // Z axis (blue)
        DrawLine(_spriteBatch, _pixel, originCenter, 
                 originCenter + new Vector2(0, -axisLength), 
                 Color.Blue, 2f);

        _spriteBatch.Draw(_pixel, new Rectangle((int)originCenter.X - 2, (int)originCenter.Y - 2, 4, 4), Color.Red);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
