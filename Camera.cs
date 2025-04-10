using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

public class Camera
{
    private float zoom;
    private float rotation;
    private Vector2 position;
    private Matrix transform;
    private bool transformDirty;

    public Camera()
    {
        zoom = 1f;
        rotation = 0f;
        position = Vector2.Zero;
        transformDirty = true;
    }

    public float Zoom
    {
        get => zoom;
        set
        {
            zoom = MathHelper.Max(value, 0.1f);
            transformDirty = true;
        }
    }

    public float Rotation
    {
        get => rotation;
        set
        {
            rotation = value;
            transformDirty = true;
        }
    }

    public Vector2 Position
    {
        get => position;
        set
        {
            position = value;
            transformDirty = true;
        }
    }

    public Matrix GetViewMatrix(GraphicsDevice graphicsDevice)
    {
        if (transformDirty)
        {
            var viewport = graphicsDevice.Viewport;
            var screenCenter = new Vector2(viewport.Width, viewport.Height) / 2f;

            transform = Matrix.CreateTranslation(new Vector3(-position, 0))
                * Matrix.CreateRotationZ(rotation)
                * Matrix.CreateScale(zoom, zoom, 1)
                * Matrix.CreateTranslation(new Vector3(screenCenter, 0));

            transformDirty = false;
        }
        return transform;
    }

    public void Move(Vector2 amount)
    {
        Position += amount;
    }

    public void LookAt(Vector2 target)
    {
        Position = target;
    }
}