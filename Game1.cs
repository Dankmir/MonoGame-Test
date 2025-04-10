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

    private RenderTarget2D renderTarget;

    private int windowSizeWidth = 1280;
    private int windowSizeHeight = 720;

    private const int nativeResolutionWidth = 640;
    private const int nativeResolutionHeight = 360;

    private Rectangle renderDestination;

    private Camera camera;

    private bool isResizing;


    bool _isFullscreen = false;
    bool _isBorderless = false;
    int _width = 0;
    int _height = 0;
    bool switchedCurrentFrame = false;

    public Game1()
    {
        _graphics = new(this)
        {
            PreferredBackBufferWidth = windowSizeWidth,
            PreferredBackBufferHeight = windowSizeHeight
        };

        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        camera = new Camera();
        renderTarget = new RenderTarget2D(GraphicsDevice, nativeResolutionWidth, nativeResolutionHeight);

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        CalculateRenderDestination();


        base.Initialize();
    }

    private void OnClientSizeChanged(object sender, EventArgs e)
    {
        if (!isResizing && Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
        {
            isResizing = true;
            CalculateRenderDestination();
            isResizing = false;
        }
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

        if (kState.IsKeyDown(Keys.Q)) camera.Zoom -= 1f / nativeResolutionWidth;
        if (kState.IsKeyDown(Keys.E)) camera.Zoom += 1f / nativeResolutionHeight;

        if (kState.IsKeyDown(Keys.F) && !switchedCurrentFrame)
        {
            switchedCurrentFrame = true;
            ToggleFullscreen();
        }

        if (kState.IsKeyUp(Keys.F) && switchedCurrentFrame)
        {
            switchedCurrentFrame = false;
        }

        base.Update(gameTime);
    }

    private void CalculateRenderDestination()
    {
        Point size = GraphicsDevice.Viewport.Bounds.Size;

        float scaleX = (float)size.X / renderTarget.Width;
        float scaleY = (float)size.Y / renderTarget.Height;
        float scale = Math.Min(scaleX, scaleY);

        renderDestination.Width = (int)(renderTarget.Width * scale);
        renderDestination.Height = (int)(renderTarget.Height * scale);

        renderDestination.X = (size.X - renderDestination.Width) / 2;
        renderDestination.Y = (size.Y - renderDestination.Height) / 2;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GraphicsDevice.SetRenderTarget(renderTarget);

        // TODO: Add your drawing code here
        _spriteBatch.Begin(transformMatrix: camera.GetViewMatrix(GraphicsDevice), samplerState: SamplerState.PointClamp);
        DrawSmoothAdaptiveGrid(_spriteBatch, pixel, camera, GraphicsDevice);

        _spriteBatch.Draw(pixelGuy, Vector2.Zero, null, Color.White, 0f, new Vector2(pixelGuy.Width / 2, pixelGuy.Height / 2), Vector2.One, SpriteEffects.None, 0f);
        // _spriteBatch.Draw(ballTexture, Vector2.Zero, null, Color.White, 0f, new Vector2(ballTexture.Width / 2, ballTexture.Height / 2), Vector2.One, SpriteEffects.None, 0f);

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(renderTarget, renderDestination, Color.White);
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



    public void ToggleFullscreen()
    {
        bool oldIsFullscreen = _isFullscreen;

        if (_isBorderless)
        {
            _isBorderless = false;
        }
        else
        {
            _isFullscreen = !_isFullscreen;
        }

        ApplyFullscreenChange(oldIsFullscreen);
    }
    public void ToggleBorderless()
    {
        bool oldIsFullscreen = _isFullscreen;

        _isBorderless = !_isBorderless;
        _isFullscreen = _isBorderless;

        ApplyFullscreenChange(oldIsFullscreen);
    }

    private void ApplyFullscreenChange(bool oldIsFullscreen)
    {
        if (_isFullscreen)
        {
            if (oldIsFullscreen)
            {
                ApplyHardwareMode();
            }
            else
            {
                SetFullscreen();
            }
        }
        else
        {
            UnsetFullscreen();
        }
    }
    private void ApplyHardwareMode()
    {
        _graphics.HardwareModeSwitch = !_isBorderless;
        _graphics.ApplyChanges();
    }
    private void SetFullscreen()
    {
        _width = Window.ClientBounds.Width;
        _height = Window.ClientBounds.Height;

        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        _graphics.HardwareModeSwitch = !_isBorderless;

        _graphics.IsFullScreen = true;
        _graphics.ApplyChanges();
    }
    private void UnsetFullscreen()
    {
        _graphics.PreferredBackBufferWidth = _width;
        _graphics.PreferredBackBufferHeight = _height;
        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();
    }

}
