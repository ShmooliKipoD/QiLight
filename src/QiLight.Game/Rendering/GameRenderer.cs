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
        _shader.FlashBloom(3f, 0.15f);
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
        _shader.Begin();

        DrawCapturedTerritory(territory);
        DrawPlayfield(playField, territory);
        DrawPlayerTrail(player, pulseFactor);
        DrawQix(qix);
        foreach (var sparx in sparxList)
            DrawSparx(sparx);
        DrawPlayer(player);
        DrawHUD(player, territory, levelManager);

        if (phase == Core.GamePhase.Paused)
            DrawPauseOverlay();

        _shader.End(gameTime);
    }

    public void DrawMenu(GameTime gameTime, int themeIndex, float pulseFactor)
    {
        _shader.Begin();

        var titleColor = Theme.Border;
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 3f);

        string title = "QiLight";
        var titleSize = _font.MeasureString(title);
        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, title, center - titleSize / 2f, titleColor);

        string start = "Press ENTER to Start";
        var startSize = _font.MeasureString(start);
        float alpha = 0.5f + 0.5f * MathF.Sin(pulseFactor * 3f);
        _spriteBatch.DrawString(_font, start,
            new Vector2(center.X - startSize.X / 2f, center.Y + 80),
            Theme.HUD * alpha);

        string themeName = $"< {ColorTheme.AllThemes[themeIndex].Name} >";
        var themeSize = _font.MeasureString(themeName);
        _spriteBatch.DrawString(_font, themeName,
            new Vector2(center.X - themeSize.X / 2f, center.Y + 140),
            Theme.Trail);

        _spriteBatch.End();

        _shader.End(gameTime);
    }

    public void DrawGameOver(GameTime gameTime, int score, int level)
    {
        _shader.Begin();

        _spriteBatch.Begin();
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 3f);

        string go = "GAME OVER";
        var goSize = _font.MeasureString(go);
        _spriteBatch.DrawString(_font, go, center - goSize / 2f, Theme.Sparx);

        string scoreText = $"Score: {score}  Level: {level}";
        var scoreSize = _font.MeasureString(scoreText);
        _spriteBatch.DrawString(_font, scoreText,
            new Vector2(center.X - scoreSize.X / 2f, center.Y + 60), Theme.HUD);

        string restart = "Press ENTER to Restart";
        var restartSize = _font.MeasureString(restart);
        _spriteBatch.DrawString(_font, restart,
            new Vector2(center.X - restartSize.X / 2f, center.Y + 120), Theme.HUD * 0.7f);

        _spriteBatch.End();
        _shader.End(gameTime);
    }

    public void DrawWin(GameTime gameTime, int score, int level)
    {
        _shader.Begin();

        _spriteBatch.Begin();
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 3f);

        string win = "LEVEL COMPLETE!";
        var winSize = _font.MeasureString(win);
        _spriteBatch.DrawString(_font, win, center - winSize / 2f, Theme.Trail);

        string scoreText = $"Score: {score}  Level: {level}";
        var scoreSize = _font.MeasureString(scoreText);
        _spriteBatch.DrawString(_font, scoreText,
            new Vector2(center.X - scoreSize.X / 2f, center.Y + 60), Theme.HUD);

        string next = "Press ENTER to Continue";
        var nextSize = _font.MeasureString(next);
        _spriteBatch.DrawString(_font, next,
            new Vector2(center.X - nextSize.X / 2f, center.Y + 120), Theme.HUD * 0.7f);

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

    private void DrawPlayer(Player player)
    {
        if (player.IsInvincible && ((int)(player.InvincibleTimer * 10) % 2 == 0))
            return; // blink effect during invincibility

        var color = player.Mode == PlayerMode.Drawing ? Theme.Trail : Theme.Border;
        _neon.DrawDot(player.Position, color, 6f);
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
        _spriteBatch.Begin();

        string hud = $"Score: {player.Score}  Lives: {player.Lives}  " +
                      $"Level: {levelManager.CurrentLevel}  " +
                      $"Captured: {territory.CapturedPercentage:F1}%";

        _spriteBatch.DrawString(_font, hud, new Vector2(10 + _shakeOffset.X, 10 + _shakeOffset.Y), Theme.HUD);

        _spriteBatch.End();
    }

    private void DrawPauseOverlay()
    {
        _spriteBatch.Begin();
        string paused = "PAUSED";
        var size = _font.MeasureString(paused);
        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 2f);
        _spriteBatch.DrawString(_font, paused, center - size / 2f, Theme.HUD);
        _spriteBatch.End();
    }
}
