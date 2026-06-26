using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QiLight.Game.Config;
using QiLight.Game.Core;
using QiLight.Game.Entities;

namespace QiLight.Game.Rendering;

public class GameRenderer
{
    private readonly GraphicsDevice _device;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private NeonRenderer _neon = null!;
    private ShaderPipeline _shader = null!;

    private const float LightRadius = 240f;
    private const float LightIntensity = 0.45f;
    private const float SparxOccluderRadius = 7f;
    // Constant faint floor glow so player-relative shadows are visible screen-wide.
    private static readonly Color AmbientColor = new(26, 24, 36);

    // Pixel font (base size 10) is scaled up per context; titles/headings big, HUD 1:1.
    private const float TitleScale = 4f;
    private const float HeadingScale = 2.5f;
    private const float PromptScale = 1.5f;

    private float _shakeTimer;
    private Vector2 _shakeOffset;
    private readonly Random _rng = new();

    public ColorTheme Theme { get; set; } = ColorTheme.Synthwave;

    public GameRenderer(GraphicsDevice device)
    {
        _device = device;
    }

    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        _spriteBatch = new SpriteBatch(_device);
        _font = content.Load<SpriteFont>("Fonts/NeonFont");
        _neon = new NeonRenderer(_device);
        _neon.LoadContent();
        _shader = new ShaderPipeline(_device);
        _shader.LoadContent(content);
    }

    public void HandleResize()
    {
        _neon.UpdateProjection();
        _shader.HandleResize();
    }

    public void TriggerScreenShake(float duration = 0.3f)
    {
        _shakeTimer = duration;
    }

    public void TriggerCaptureFlash()
    {
        _shader.FlashBloom(2f, 0.15f);
    }

    public void Draw(GameTime gameTime, Core.GameStateManager? drawState)
    {
        // Update screen shake
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_shakeTimer > 0)
        {
            _shakeTimer -= dt;
            _shakeOffset = new Vector2(
                (float)(_rng.NextDouble() * 8 - 4),
                (float)(_rng.NextDouble() * 8 - 4));
        }
        else
        {
            _shakeOffset = Vector2.Zero;
        }
    }

    public void DrawGame(GameTime gameTime, PlayField playField, Player player,
        Territory territory, Qix qix, List<Sparx> sparxList, LevelManager levelManager,
        GamePhase phase, float pulseFactor)
    {
        RenderLightBuffer(player, qix, sparxList, pulseFactor);

        _shader.Begin();
        _shader.CompositeLight();

        DrawCapturedTerritory(territory);
        DrawPlayfield(playField, territory);
        DrawPlayerTrail(player, pulseFactor);
        DrawQix(qix);
        foreach (var sparx in sparxList)
            DrawSparx(sparx);
        DrawPlayer(player, pulseFactor);
        DrawHUD(player, territory, levelManager);

        if (phase == Core.GamePhase.Paused)
            DrawPauseOverlay();

        _shader.End(gameTime);
    }

    // Draws text with a very light glow: a few low-alpha offset copies behind the
    // crisp text. Must be called inside an active _spriteBatch.Begin/End.
    private void DrawTextGlow(string text, Vector2 pos, Color color, float scale = 1f)
    {
        var glow = color * 0.25f;
        float o = 1.5f * scale;
        foreach (var off in new[] { new Vector2(o, 0), new Vector2(-o, 0), new Vector2(0, o), new Vector2(0, -o) })
            _spriteBatch.DrawString(_font, text, pos + off, glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private Vector2 CenteredX(string text, float centerX, float y, float scale) =>
        new(centerX - _font.MeasureString(text).X * scale / 2f, y);

    public void DrawMenu(GameTime gameTime, int themeIndex, float pulseFactor)
    {
        _shader.Begin();

        var titleColor = Theme.Border;
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 3f);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        string title = "QiLight";
        DrawTextGlow(title, CenteredX(title, center.X, center.Y - _font.MeasureString(title).Y * TitleScale / 2f, TitleScale),
            titleColor, TitleScale);

        string start = "Press ENTER to Start";
        float alpha = 0.5f + 0.5f * MathF.Sin(pulseFactor * 3f);
        DrawTextGlow(start, CenteredX(start, center.X, center.Y + 80, PromptScale), Theme.HUD * alpha, PromptScale);

        string themeName = $"< {ColorTheme.AllThemes[themeIndex].Name} >";
        DrawTextGlow(themeName, CenteredX(themeName, center.X, center.Y + 140, PromptScale), Theme.Trail, PromptScale);

        _spriteBatch.End();

        _shader.End(gameTime);
    }

    public void DrawGameOver(GameTime gameTime, int score, int level)
    {
        _shader.Begin();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 3f);

        string go = "GAME OVER";
        DrawTextGlow(go, CenteredX(go, center.X, center.Y - _font.MeasureString(go).Y * HeadingScale / 2f, HeadingScale),
            Theme.Sparx, HeadingScale);

        string scoreText = $"Score: {score}  Level: {level}";
        DrawTextGlow(scoreText, CenteredX(scoreText, center.X, center.Y + 60, PromptScale), Theme.HUD, PromptScale);

        string restart = "Press ENTER to Restart";
        DrawTextGlow(restart, CenteredX(restart, center.X, center.Y + 120, PromptScale), Theme.HUD * 0.7f, PromptScale);

        _spriteBatch.End();
        _shader.End(gameTime);
    }

    public void DrawWin(GameTime gameTime, int score, int level)
    {
        _shader.Begin();

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 3f);

        string win = "LEVEL COMPLETE!";
        DrawTextGlow(win, CenteredX(win, center.X, center.Y - _font.MeasureString(win).Y * HeadingScale / 2f, HeadingScale),
            Theme.Trail, HeadingScale);

        string scoreText = $"Score: {score}  Level: {level}";
        DrawTextGlow(scoreText, CenteredX(scoreText, center.X, center.Y + 60, PromptScale), Theme.HUD, PromptScale);

        string next = "Press ENTER to Continue";
        DrawTextGlow(next, CenteredX(next, center.X, center.Y + 120, PromptScale), Theme.HUD * 0.7f, PromptScale);

        _spriteBatch.End();
        _shader.End(gameTime);
    }

    private void DrawPlayfield(PlayField playField, Territory territory)
    {
        _neon.DrawLines(territory.UncapturedPolygon, Theme.Border, 3f, true);
    }

    private void DrawCapturedTerritory(Territory territory)
    {
        foreach (var region in territory.CapturedRegions)
        {
            _neon.DrawFilledPolygon(region.Polygon, region.TriangleIndices, Theme.CapturedFill);
            _neon.DrawLines(region.Polygon, Theme.Border * 0.4f, 1.5f, true);
        }
    }

    private void DrawPlayerTrail(Player player, float pulseFactor)
    {
        if (player.Trail.Count < 2) return;
        float brightness = 0.7f + 0.3f * MathF.Sin(pulseFactor * 5f);
        var color = Theme.Trail * brightness;
        _neon.DrawLines(player.Trail, color, 3f);
    }

    // Treat the player as a light source: build a radial pool, then carve shadows
    // cast by the other objects (Qix + Sparx) away from the player.
    private void RenderLightBuffer(Player player, Qix qix, List<Sparx> sparxList, float pulseFactor)
    {
        var occluders = new List<(Vector2 a, Vector2 b)>();

        var qp = qix.Points;
        for (int i = 0; i < qp.Count; i++)
            occluders.Add((qp[i], qp[(i + 1) % qp.Count]));

        foreach (var s in sparxList)
        {
            var d = s.Position - player.Position;
            if (d.LengthSquared() < 0.01f) continue;
            d.Normalize();
            var perp = new Vector2(-d.Y, d.X) * SparxOccluderRadius;
            occluders.Add((s.Position + perp, s.Position - perp));
        }

        float pulse = 0.95f + 0.05f * MathF.Sin(pulseFactor * 4f);
        float radius = LightRadius * pulse;
        var lightColor = (player.Mode == PlayerMode.Drawing ? Theme.Trail : Theme.Border) * LightIntensity;

        // Project shadows across the whole screen so they show over the ambient floor.
        float screenDiag = MathF.Sqrt(
            _device.Viewport.Width * _device.Viewport.Width +
            _device.Viewport.Height * _device.Viewport.Height);

        _shader.BeginLight(AmbientColor);
        _neon.DrawRadialLight(player.Position, radius, lightColor);
        _neon.DrawShadowVolumes(player.Position, occluders, screenDiag);
        _shader.EndLight();
    }

    private void DrawPlayer(Player player, float pulseFactor)
    {
        if (player.IsInvincible && ((int)(player.InvincibleTimer * 10) % 2 == 0))
            return; // blink effect during invincibility

        var color = player.Mode == PlayerMode.Drawing ? Theme.Trail : Theme.Border;
        float pulse = 0.8f + 0.2f * MathF.Sin(pulseFactor * 4f); // matches trail pulse style
        _neon.DrawGlowDot(player.Position, color, 3f, 5f * pulse, 2);
    }

    private void DrawQix(Qix qix)
    {
        _neon.DrawLines(qix.Points, Theme.Qix, 2.5f, true);
    }

    private void DrawSparx(Sparx sparx)
    {
        for (int i = sparx.TrailPositions.Count - 1; i >= 0; i--)
        {
            float alpha = 1f - (float)i / sparx.TrailPositions.Count;
            _neon.DrawDot(sparx.TrailPositions[i], Theme.Sparx * (alpha * 0.5f), 3f);
        }
        _neon.DrawDot(sparx.Position, Theme.Sparx, 5f);
    }

    private void DrawHUD(Player player, Territory territory, LevelManager levelManager)
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        string hud = $"Score: {player.Score}  Lives: {player.Lives}  " +
                      $"Level: {levelManager.CurrentLevel}  " +
                      $"Captured: {territory.CapturedPercentage:F1}%";

        DrawTextGlow(hud, new Vector2(10 + _shakeOffset.X, 10 + _shakeOffset.Y), Theme.HUD);

        _spriteBatch.End();
    }

    private void DrawPauseOverlay()
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        string paused = "PAUSED";
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 2f);
        DrawTextGlow(paused, center - _font.MeasureString(paused) * HeadingScale / 2f, Theme.HUD, HeadingScale);
        _spriteBatch.End();
    }
}
