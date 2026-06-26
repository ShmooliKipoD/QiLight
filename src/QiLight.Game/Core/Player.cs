using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QiLight.Game.Core;

public enum PlayerMode
{
    Border,
    Drawing
}

public class Player
{
    public const float BorderSpeed = 200f;
    public const float DrawSpeedSlow = 150f;
    public const float DrawSpeedFast = 300f;
    public const float RetractSpeed = 250f;
    public const int MaxLives = 3;

    public Vector2 Position { get; set; }
    public PlayerMode Mode { get; set; } = PlayerMode.Border;
    public int Lives { get; set; } = MaxLives;
    public int Score { get; set; }
    public List<Vector2> Trail { get; } = new();
    public bool IsDrawingFast { get; set; }
    public float DrawStartTime { get; set; }
    public float InvincibleTimer { get; private set; }
    public bool IsInvincible => InvincibleTimer > 0f;

    private const float SpawnInvincibilityDuration = 1.5f;
    private const float MinDrawCompletionDistance = 20f;
    private const float BorderExitProbe = 8f; // > IsOnBorder tolerance (3f)
    private readonly PlayField _playField;

    public Player(PlayField playField)
    {
        _playField = playField;
        Position = playField.Vertices[0];
    }

    public void Update(GameTime gameTime, Input.MoveDirection direction, bool actionHeld, bool speedBoost, bool retract, Territory territory)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (InvincibleTimer > 0f)
            InvincibleTimer -= dt;

        if (Mode == PlayerMode.Border)
        {
            if (actionHeld && direction != Input.MoveDirection.None
                && DirectionLeavesBorder(direction))
            {
                StartDrawing(gameTime);
            }
            else
            {
                float speed = BorderSpeed * dt;
                Position = _playField.MoveAlongBorder(Position, direction, speed);
            }
        }
        else if (Mode == PlayerMode.Drawing)
        {
            if (retract)
            {
                RetractAlongTrail(dt);
                return;
            }

            IsDrawingFast = speedBoost;
            float speed = (IsDrawingFast ? DrawSpeedFast : DrawSpeedSlow) * dt;

            Vector2 desiredDir = DirToVector(direction);

            if (desiredDir != Vector2.Zero)
            {
                var newPos = Position + desiredDir * speed;
                newPos = ClampToPlayArea(newPos);

                if (Trail.Count > 2 && _playField.IsOnBorder(newPos, 3f))
                {
                    float distFromStart = Vector2.Distance(newPos, Trail[0]);
                    if (distFromStart > MinDrawCompletionDistance)
                    {
                        newPos = _playField.SnapToBorder(newPos);
                        Position = newPos;
                        Trail.Add(newPos);
                        CompleteDraw(territory, gameTime);
                        return;
                    }
                }

                Position = newPos;
                Trail.Add(newPos);
            }
        }
    }

    private static Vector2 DirToVector(Input.MoveDirection direction) => direction switch
    {
        Input.MoveDirection.Right => Vector2.UnitX,
        Input.MoveDirection.Left => -Vector2.UnitX,
        Input.MoveDirection.Down => Vector2.UnitY,
        Input.MoveDirection.Up => -Vector2.UnitY,
        _ => Vector2.Zero
    };

    private bool DirectionLeavesBorder(Input.MoveDirection direction)
    {
        var dir = DirToVector(direction);
        if (dir == Vector2.Zero) return false;
        var probe = ClampToPlayArea(Position + dir * BorderExitProbe);
        return !_playField.IsOnBorder(probe, 3f);
    }

    // Hold-to-retract: walk the trail head backward along the drawn polyline,
    // consuming vertices. Reaching the start cancels the draw (no capture).
    private void RetractAlongTrail(float dt)
    {
        IsDrawingFast = false;
        float remaining = RetractSpeed * dt;
        while (remaining > 0f && Trail.Count >= 2)
        {
            Vector2 head = Trail[^1];   // == Position
            Vector2 prev = Trail[^2];
            float segLen = Vector2.Distance(head, prev);
            if (segLen <= remaining)
            {
                Trail.RemoveAt(Trail.Count - 1); // drop head vertex
                Position = Trail[^1];            // head becomes prev
                remaining -= segLen;
            }
            else
            {
                Position = head + Vector2.Normalize(prev - head) * remaining;
                Trail[Trail.Count - 1] = Position; // move head vertex back
                remaining = 0f;
            }
        }

        if (Trail.Count <= 1) // reached the start on the border
        {
            if (Trail.Count == 1) Position = Trail[0];
            Trail.Clear();
            Mode = PlayerMode.Border;
        }
    }

    private void StartDrawing(GameTime gameTime)
    {
        Mode = PlayerMode.Drawing;
        Trail.Clear();
        Trail.Add(Position);
        DrawStartTime = (float)gameTime.TotalGameTime.TotalSeconds;
    }

    private void CompleteDraw(Territory territory, GameTime gameTime)
    {
        float drawDuration = (float)gameTime.TotalGameTime.TotalSeconds - DrawStartTime;
        territory.CompleteCut(Trail, this, drawDuration);
        Trail.Clear();
        Mode = PlayerMode.Border;
    }

    private Vector2 ClampToPlayArea(Vector2 pos)
    {
        var bounds = _playField.Bounds;
        pos.X = MathHelper.Clamp(pos.X, bounds.Left, bounds.Right);
        pos.Y = MathHelper.Clamp(pos.Y, bounds.Top, bounds.Bottom);
        return pos;
    }

    public void Die()
    {
        Lives--;
        Trail.Clear();
        Mode = PlayerMode.Border;
        Position = _playField.Vertices[0];
        InvincibleTimer = SpawnInvincibilityDuration;
    }

    public void Reset()
    {
        Lives = MaxLives;
        Score = 0;
        Mode = PlayerMode.Border;
        Trail.Clear();
        Position = _playField.Vertices[0];
        InvincibleTimer = SpawnInvincibilityDuration;
    }
}
