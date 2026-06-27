using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using QiLight.Game.Config;
using QiLight.Game.Core;
using QiLight.Game.Entities;
using QiLight.Game.Input;
using QiLight.Game.Rendering;

namespace QiLight.Game;

public class QiLightGame : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private GameRenderer _renderer = null!;
    private InputManager _input = null!;
    private GameStateManager _state = null!;
    private PlayField _playField = null!;
    private Player _player = null!;
    private Territory _territory = null!;
    private CollisionSystem _collision = null!;
    private LevelManager _levelManager = null!;
    private Qix _qix = null!;
    private List<Sparx> _sparxList = new();
    private int _themeIndex;
    private float _pulseTime;

    public QiLightGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.SynchronizeWithVerticalRetrace = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (_, _) => _renderer?.HandleResize();
    }

    protected override void Initialize()
    {
        _input = new InputManager();
        _state = new GameStateManager();
        _collision = new CollisionSystem();
        _levelManager = new LevelManager();

        InitializeLevel();

        base.Initialize();
    }

    private void InitializeLevel()
    {
        _playField = new PlayField(
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight);

        _player = new Player(_playField);
        _territory = new Territory(_playField);

        _qix = new Qix(_playField);
        _qix.SpeedMultiplier = _levelManager.QixSpeedMultiplier;

        _sparxList.Clear();
        for (int i = 0; i < _levelManager.SparxCount; i++)
        {
            var sparx = new Sparx(_playField, i % 2 == 0, startSegment: 2);
            sparx.SpeedMultiplier = _levelManager.SparxSpeedMultiplier;
            _sparxList.Add(sparx);
        }
    }

    protected override void LoadContent()
    {
        _renderer = new GameRenderer(GraphicsDevice);
        _renderer.LoadContent(Content);
        _renderer.Theme = ColorTheme.AllThemes[_themeIndex];
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        _pulseTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state.CurrentPhase)
        {
            case GamePhase.Menu:
                UpdateMenu();
                break;
            case GamePhase.Playing:
                UpdatePlaying(gameTime);
                break;
            case GamePhase.Paused:
                if (_input.PausePressed)
                    _state.TogglePause();
                break;
            case GamePhase.GameOver:
                if (_input.EnterPressed)
                {
                    _levelManager.Reset();
                    _player.Reset();
                    InitializeLevel();
                    _state.TransitionTo(GamePhase.Menu);
                }
                break;
            case GamePhase.Win:
                if (_input.EnterPressed)
                {
                    _levelManager.NextLevel();
                    int savedScore = _player.Score;
                    int savedLives = _player.Lives;
                    InitializeLevel();
                    _player.Score = savedScore;
                    _player.Lives = savedLives;
                    _state.TransitionTo(GamePhase.Playing);
                }
                break;
        }

        base.Update(gameTime);
    }

    private void UpdateMenu()
    {
        if (_input.EnterPressed)
        {
            _levelManager.Reset();
            _player.Reset();
            InitializeLevel();
            _state.TransitionTo(GamePhase.Playing);
        }

        if (_input.LeftPressed)
        {
            _themeIndex = (_themeIndex - 1 + ColorTheme.AllThemes.Length) % ColorTheme.AllThemes.Length;
            _renderer.Theme = ColorTheme.AllThemes[_themeIndex];
        }
        if (_input.RightPressed)
        {
            _themeIndex = (_themeIndex + 1) % ColorTheme.AllThemes.Length;
            _renderer.Theme = ColorTheme.AllThemes[_themeIndex];
        }
    }

    private void UpdatePlaying(GameTime gameTime)
    {
        if (_input.PausePressed)
        {
            _state.TogglePause();
            return;
        }

        float prevCaptured = _territory.CapturedPercentage;

        _player.Update(gameTime, _input.Direction, _input.ActionHeld, _input.SpeedBoost, _input.RetractHeld, _territory);

        _territory.SetQixPosition(_qix.Center);
        _qix.Update(gameTime, _territory.UncapturedPolygon);

        _playField.UpdateBorder(_territory.UncapturedPolygon);
        foreach (var sparx in _sparxList)
            sparx.Update(gameTime, _playField);

        if (_territory.CapturedPercentage > prevCaptured + 0.1f)
        {
            _renderer.TriggerCaptureFlash();
            _renderer.TriggerCaptureBurst(_player.Position);
        }

        if (_player.Mode == PlayerMode.Drawing)
        {
            if (_collision.CheckTrailSelfIntersection(_player.Trail))
            {
                KillPlayer();
                return;
            }
            if (_collision.CheckQixVsTrail(_qix.Points, _player.Trail))
            {
                KillPlayer();
                return;
            }
        }

        if (_player.Mode == PlayerMode.Drawing &&
            _collision.CheckQixVsPlayer(_qix.Points, _player.Position))
        {
            KillPlayer();
            return;
        }

        if (!_player.IsInvincible)
        {
            foreach (var sparx in _sparxList)
            {
                if (_collision.CheckSparxVsPlayer(sparx.Position, _player.Position))
                {
                    KillPlayer();
                    return;
                }
            }
        }

        if (_territory.CapturedPercentage >= 75f)
        {
            _state.TransitionTo(GamePhase.Win);
        }
    }

    private void KillPlayer()
    {
        _renderer.TriggerScreenShake();
        _renderer.TriggerDeathBurst(_player.Position);
        _player.Die();
        if (_player.Lives <= 0)
        {
            _state.TransitionTo(GamePhase.GameOver);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _renderer.Draw(gameTime, null!);

        switch (_state.CurrentPhase)
        {
            case GamePhase.Menu:
                _renderer.DrawMenu(gameTime, _themeIndex, _pulseTime);
                break;
            case GamePhase.Playing:
            case GamePhase.Paused:
                _renderer.DrawGame(gameTime, _playField, _player, _territory,
                    _qix, _sparxList, _levelManager, _state.CurrentPhase, _pulseTime);
                break;
            case GamePhase.GameOver:
                _renderer.DrawGameOver(gameTime, _player.Score, _levelManager.CurrentLevel);
                break;
            case GamePhase.Win:
                _renderer.DrawWin(gameTime, _player.Score, _levelManager.CurrentLevel);
                break;
        }

        base.Draw(gameTime);
    }
}
