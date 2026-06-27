using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace QiLight.Game.Rendering;

public class ShaderPipeline
{
    private readonly GraphicsDevice _device;
    private SpriteBatch _spriteBatch = null!;

    private RenderTarget2D _sceneTarget = null!;
    private RenderTarget2D _lightTarget = null!;
    private RenderTarget2D _lightBlur = null!;
    private RenderTarget2D _bloomExtract = null!;
    private RenderTarget2D _bloomBlurH = null!;
    private RenderTarget2D _bloomBlurV = null!;

    private Effect? _bloomEffect;
    private bool _useShaderBloom;

    public float BloomThreshold { get; set; } = 0.45f;
    public float BloomIntensity { get; set; } = 1.0f;
    public float BloomSaturation { get; set; } = 1.0f;
    public float BaseIntensity { get; set; } = 1.0f;

    private float _bloomIntensityOverride;
    private float _overrideDuration;

    public ShaderPipeline(GraphicsDevice device)
    {
        _device = device;
    }

    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        _spriteBatch = new SpriteBatch(_device);

        try
        {
            _bloomEffect = content.Load<Effect>("Shaders/Bloom");
            _useShaderBloom = true;
        }
        catch
        {
            _useShaderBloom = false;
        }

        CreateRenderTargets();
    }

    private void CreateRenderTargets()
    {
        int w = _device.PresentationParameters.BackBufferWidth;
        int h = _device.PresentationParameters.BackBufferHeight;
        int halfW = w / 2;
        int halfH = h / 2;

        _sceneTarget?.Dispose();
        _lightTarget?.Dispose();
        _lightBlur?.Dispose();
        _bloomExtract?.Dispose();
        _bloomBlurH?.Dispose();
        _bloomBlurV?.Dispose();

        _sceneTarget = new RenderTarget2D(_device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
        _lightTarget = new RenderTarget2D(_device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
        _lightBlur = new RenderTarget2D(_device, halfW, halfH, false, SurfaceFormat.Color, DepthFormat.None);
        _bloomExtract = new RenderTarget2D(_device, halfW, halfH, false, SurfaceFormat.Color, DepthFormat.None);
        _bloomBlurH = new RenderTarget2D(_device, halfW, halfH, false, SurfaceFormat.Color, DepthFormat.None);
        _bloomBlurV = new RenderTarget2D(_device, halfW, halfH, false, SurfaceFormat.Color, DepthFormat.None);
    }

    public void HandleResize()
    {
        CreateRenderTargets();
    }

    public void FlashBloom(float intensity = 2f, float duration = 0.1f)
    {
        _bloomIntensityOverride = intensity;
        _overrideDuration = duration;
    }

    public void BeginLight(Color ambient)
    {
        _device.SetRenderTarget(_lightTarget);
        _device.Clear(ambient);
    }

    public void EndLight()
    {
        _device.SetRenderTarget(null);
    }

    // Soften the light buffer: downsample to half res, so the upscaled composite
    // reads through bilinear filtering and the hard shadow edges feather out.
    public void BlurLight()
    {
        _device.SetRenderTarget(_lightBlur);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
        _spriteBatch.Draw(_lightTarget, _lightBlur.Bounds, Color.White);
        _spriteBatch.End();
        _device.SetRenderTarget(null);
    }

    // Additively blend the (blurred) light buffer onto the currently-bound scene target.
    public void CompositeLight()
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, NeonRenderer.AddRGB, SamplerState.LinearClamp);
        _spriteBatch.Draw(_lightBlur, _sceneTarget.Bounds, Color.White);
        _spriteBatch.End();
    }

    public void Begin()
    {
        _device.SetRenderTarget(_sceneTarget);
        _device.Clear(Color.Black);
    }

    // Applies bloom and composites the scene. With output == null the result goes to the
    // backbuffer; pass a render target to capture the composited frame instead.
    public void End(GameTime gameTime, RenderTarget2D? output = null)
    {
        _device.SetRenderTarget(null);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float effectiveIntensity = BloomIntensity;
        if (_overrideDuration > 0)
        {
            effectiveIntensity = _bloomIntensityOverride;
            _overrideDuration -= dt;
        }

        if (_useShaderBloom && _bloomEffect != null)
        {
            DrawWithShaderBloom(effectiveIntensity, output);
        }
        else
        {
            DrawWithFallbackBloom(effectiveIntensity, output);
        }
    }

    private void DrawWithShaderBloom(float intensity, RenderTarget2D? output)
    {
        _bloomEffect!.Parameters["BloomThreshold"]?.SetValue(BloomThreshold);

        _device.SetRenderTarget(_bloomExtract);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp,
            null, null, _bloomEffect);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["BrightExtract"];
        _spriteBatch.Draw(_sceneTarget, _bloomExtract.Bounds, Color.White);
        _spriteBatch.End();

        _bloomEffect.Parameters["TexelSize"]?.SetValue(new Vector2(1f / _bloomBlurH.Width, 0));
        _device.SetRenderTarget(_bloomBlurH);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp,
            null, null, _bloomEffect);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["GaussianBlur"];
        _spriteBatch.Draw(_bloomExtract, _bloomBlurH.Bounds, Color.White);
        _spriteBatch.End();

        _bloomEffect.Parameters["TexelSize"]?.SetValue(new Vector2(0, 1f / _bloomBlurV.Height));
        _device.SetRenderTarget(_bloomBlurV);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp,
            null, null, _bloomEffect);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["GaussianBlur"];
        _spriteBatch.Draw(_bloomBlurH, _bloomBlurV.Bounds, Color.White);
        _spriteBatch.End();

        _bloomEffect.Parameters["BloomIntensity"]?.SetValue(intensity);
        _bloomEffect.Parameters["BloomSaturation"]?.SetValue(BloomSaturation);
        _bloomEffect.Parameters["BaseIntensity"]?.SetValue(BaseIntensity);
        _device.SetRenderTarget(output);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp,
            null, null, _bloomEffect);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["BloomCombine"];
        _bloomEffect.Parameters["BloomTexture"]?.SetValue(_bloomBlurV);
        _spriteBatch.Draw(_sceneTarget, _device.Viewport.Bounds, Color.White);
        _spriteBatch.End();
    }

    private void DrawWithFallbackBloom(float intensity, RenderTarget2D? output)
    {
        _device.SetRenderTarget(output);
        _device.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
        _spriteBatch.Draw(_sceneTarget, _device.Viewport.Bounds, Color.White);
        _spriteBatch.End();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        float alpha = MathHelper.Clamp(intensity * 0.2f, 0, 1);
        _spriteBatch.Draw(_sceneTarget, _device.Viewport.Bounds, new Color(alpha, alpha, alpha, alpha));
        _spriteBatch.End();
    }
}
