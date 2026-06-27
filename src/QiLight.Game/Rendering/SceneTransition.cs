using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace QiLight.Game.Rendering;

// State + render target for an animated scene transition. Holds the captured outgoing
// frame; GameRenderer drives the capture blit and the slide composite.
public sealed class SceneTransition
{
    private const float Duration = 0.4f;

    private readonly GraphicsDevice _device;

    // The captured outgoing scene (last fully-rendered frame of the previous phase).
    public RenderTarget2D From { get; private set; } = null!;

    public bool IsActive { get; private set; }
    private float _elapsed;

    public SceneTransition(GraphicsDevice device)
    {
        _device = device;
        CreateTarget();
    }

    // Eased [0..1] slide progress (smoothstep).
    public float Progress
    {
        get
        {
            float p = MathHelper.Clamp(_elapsed / Duration, 0f, 1f);
            return p * p * (3f - 2f * p);
        }
    }

    public void Start()
    {
        _elapsed = 0f;
        IsActive = true;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        _elapsed += dt;
        if (_elapsed >= Duration)
            IsActive = false;
    }

    public void HandleResize()
    {
        CreateTarget();
    }

    private void CreateTarget()
    {
        From?.Dispose();
        From = new RenderTarget2D(_device,
            _device.PresentationParameters.BackBufferWidth,
            _device.PresentationParameters.BackBufferHeight,
            false, SurfaceFormat.Color, DepthFormat.None);
    }
}
