using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;

namespace QiLight.Game.Rendering;

// Neon particle bursts (MonoGame.Extended) for capture / death feedback. Particles are
// drawn into the scene render target with additive blend so the bloom pass makes them
// glow. Bursts are triggered manually; colour is set per-trigger from the live theme.
public sealed class ParticleSystem : IDisposable
{
    private readonly Texture2D _dot;
    private readonly ParticleEffect _capture;
    private readonly ParticleEffect _death;

    public ParticleSystem(GraphicsDevice device)
    {
        _dot = CreateSoftDot(device, 16);
        var region = new Texture2DRegion(_dot);

        _capture = BuildBurst(region, "capture",
            quantityMin: 18, quantityMax: 28, speedMin: 60f, speedMax: 190f,
            lifeSpan: 0.8f, scale: 4f);
        _death = BuildBurst(region, "death",
            quantityMin: 40, quantityMax: 60, speedMin: 120f, speedMax: 320f,
            lifeSpan: 1.0f, scale: 5f);
    }

    public void TriggerCapture(Vector2 position, Color color) => Trigger(_capture, position, color);
    public void TriggerDeath(Vector2 position, Color color) => Trigger(_death, position, color);

    public void Update(float elapsedSeconds)
    {
        _capture.Update(elapsedSeconds);
        _death.Update(elapsedSeconds);
    }

    // Caller must have an active SpriteBatch (additive blend) bound to the scene target.
    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_capture);
        spriteBatch.Draw(_death);
    }

    private static void Trigger(ParticleEffect effect, Vector2 position, Color color)
    {
        var emitter = effect.Emitters[0];
        var pars = emitter.Parameters;
        pars.Color = new Range<HslColor>(HslColor.FromRgb(color));
        emitter.Parameters = pars;
        effect.Trigger(position, 0f);
    }

    private static ParticleEffect BuildBurst(Texture2DRegion region, string name,
        int quantityMin, int quantityMax, float speedMin, float speedMax,
        float lifeSpan, float scale)
    {
        var emitter = new ParticleEmitter(region, quantityMax * 2,
            TimeSpan.FromSeconds(lifeSpan), Profile.Circle(6f, Profile.CircleRadiation.Out))
        {
            AutoTrigger = false,
            Parameters = new ParticleReleaseParameters
            {
                Quantity = new Range<int>(quantityMin, quantityMax),
                Speed = new Range<float>(speedMin, speedMax),
                Scale = new Range<float>(scale),
                Opacity = new Range<float>(1f),
                Rotation = new Range<float>(0f),
                Mass = new Range<float>(1f),
            },
        };

        var age = new AgeModifier();
        age.Interpolators.Add(new ScaleInterpolator
        {
            StartValue = new Vector2(scale),
            EndValue = Vector2.Zero,
        });
        emitter.Modifiers.Add(age);
        emitter.Modifiers.Add(new OpacityFastFadeModifier());
        emitter.Modifiers.Add(new DragModifier { DragCoefficient = 0.5f, Density = 0.6f });

        var effect = new ParticleEffect(name);
        effect.Emitters.Add(emitter);
        return effect;
    }

    // A small radial-alpha white dot so additive particles read as soft glowing points.
    private static Texture2D CreateSoftDot(GraphicsDevice device, int size)
    {
        var texture = new Texture2D(device, size, size);
        var data = new Color[size * size];
        float r = size / 2f;
        var center = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / r;
                float a = MathHelper.Clamp(1f - d, 0f, 1f);
                a *= a; // softer falloff toward the rim
                data[y * size + x] = new Color(a, a, a, a);
            }
        }
        texture.SetData(data);
        return texture;
    }

    public void Dispose()
    {
        _capture.Dispose();
        _death.Dispose();
        _dot.Dispose();
    }
}
