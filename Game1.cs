using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;

namespace MonoGameTest;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Texture2D ballTexture;
    private Texture2D pixel;
    private Texture2D pixelGuy;


    private Camera camera;


    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 400;
        _graphics.PreferredBackBufferHeight = 200;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        camera = new Camera();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
        ballTexture = Content.Load<Texture2D>("ball");
        pixelGuy = Content.Load<Texture2D>("PixelGuy");

        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here
        var kState = Keyboard.GetState();

        var moveSpeed = 300f * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (kState.IsKeyDown(Keys.W)) camera.Move(new Vector2(0, -moveSpeed));
        if (kState.IsKeyDown(Keys.S)) camera.Move(new Vector2(0, moveSpeed));
        if (kState.IsKeyDown(Keys.A)) camera.Move(new Vector2(-moveSpeed, 0));
        if (kState.IsKeyDown(Keys.D)) camera.Move(new Vector2(moveSpeed, 0));

        if (kState.IsKeyDown(Keys.Q)) camera.Zoom -= 0.01f;
        if (kState.IsKeyDown(Keys.E)) camera.Zoom += 0.01f;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        _spriteBatch.Begin(transformMatrix: camera.GetViewMatrix(GraphicsDevice), samplerState: SamplerState.PointClamp);
        DrawSmoothAdaptiveGrid(_spriteBatch, pixel, camera, GraphicsDevice);

        _spriteBatch.Draw(pixelGuy, Vector2.Zero, null, Color.White, 0f, new Vector2(pixelGuy.Width / 2, pixelGuy.Height / 2), Vector2.One, SpriteEffects.None, 0f);
        // _spriteBatch.Draw(ballTexture, Vector2.Zero, null, Color.White, 0f, new Vector2(ballTexture.Width / 2, ballTexture.Height / 2), Vector2.One, SpriteEffects.None, 0f);


        _spriteBatch.End();

        base.Draw(gameTime);
    }

    struct GridLevel(int size, float threshold)
    {
        public int Size = size;
        public float ZoomThreshold = threshold;
    }

    GridLevel[] gridLevels =
    [
        new GridLevel(1, 32.0f),
        new GridLevel(2, 16.0f),
        new GridLevel(4, 8.0f),
        new GridLevel(8, 4.0f),
        new GridLevel(16, 2.0f),
        new GridLevel(32, 1.0f),
        new GridLevel(64, 0.5f),
        new GridLevel(128, 0.25f),
        new GridLevel(256, 0.125f)
    ];

    void DrawSmoothAdaptiveGrid(SpriteBatch spriteBatch, Texture2D pixel, Camera camera, GraphicsDevice graphicsDevice)
    {
        float zoom = camera.Zoom;

        for (int i = 0; i < gridLevels.Length - 1; i++)
        {
            var current = gridLevels[i];
            var next = gridLevels[i + 1];

            // Check if zoom is between these two levels
            if (zoom <= current.ZoomThreshold && zoom > next.ZoomThreshold)
            {
                // Calculate blend factor (0 to 1)
                float range = current.ZoomThreshold - next.ZoomThreshold;
                float t = (zoom - next.ZoomThreshold) / range;

                // Fade current out, next in
                Color colorCurrent = new Color(200, 200, 200) * t;
                Color colorNext = new Color(200, 200, 200) * (1f - t);

                DrawGridLayer(spriteBatch, pixel, camera, graphicsDevice, current.Size, colorCurrent);
                DrawGridLayer(spriteBatch, pixel, camera, graphicsDevice, next.Size, colorNext);

                return;
            }
        }

        // If no blend (zoom beyond all ranges), draw closest match
        var fallback = gridLevels[^1];
        DrawGridLayer(spriteBatch, pixel, camera, graphicsDevice, fallback.Size, new Color(200, 200, 200) * 0.4f);
    }

    void DrawGridLayer(SpriteBatch spriteBatch, Texture2D pixel, Camera camera, GraphicsDevice graphicsDevice, int gridSize, Color lineColor)
    {
        int screenWidth = graphicsDevice.Viewport.Width;
        int screenHeight = graphicsDevice.Viewport.Height;

        Matrix inverseView = Matrix.Invert(camera.GetViewMatrix(graphicsDevice));
        Vector2 topLeft = Vector2.Transform(Vector2.Zero, inverseView);
        Vector2 bottomRight = Vector2.Transform(new Vector2(screenWidth, screenHeight), inverseView);

        int startX = (int)Math.Floor(topLeft.X / gridSize) * gridSize;
        int endX = (int)Math.Ceiling(bottomRight.X / gridSize) * gridSize;
        int startY = (int)Math.Floor(topLeft.Y / gridSize) * gridSize;
        int endY = (int)Math.Ceiling(bottomRight.Y / gridSize) * gridSize;

        for (int x = startX; x <= endX; x += gridSize)
        {
            spriteBatch.Draw(pixel, new Rectangle(x, startY, 1, endY - startY), lineColor);
        }

        for (int y = startY; y <= endY; y += gridSize)
        {
            spriteBatch.Draw(pixel, new Rectangle(startX, y, endX - startX, 1), lineColor);
        }
    }

}
