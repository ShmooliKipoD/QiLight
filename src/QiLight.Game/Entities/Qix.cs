using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using QiLight.Game.Core;

namespace QiLight.Game.Entities;

public class Qix
{
    private const int PointCount = 5;
    private const float BaseSpeed = 80f;

    public List<Vector2> Points { get; } = new();
    private readonly List<Vector2> _velocities = new();
    private readonly Random _rng = new();

    public float SpeedMultiplier { get; set; } = 1f;

    public Qix(PlayField playField)
    {
        var bounds = playField.Bounds;
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);

        for (int i = 0; i < PointCount; i++)
        {
            float offset = 30f;
            Points.Add(center + new Vector2(
                (float)(_rng.NextDouble() * offset * 2 - offset),
                (float)(_rng.NextDouble() * offset * 2 - offset)));

            float angle = (float)(_rng.NextDouble() * MathF.Tau);
            float speed = BaseSpeed * (0.5f + (float)_rng.NextDouble());
            _velocities.Add(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed);
        }
    }

    public Vector2 Center
    {
        get
        {
            var sum = Vector2.Zero;
            foreach (var p in Points) sum += p;
            return sum / Points.Count;
        }
    }

    public void Update(GameTime gameTime, List<Vector2> boundary)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = 0; i < Points.Count; i++)
        {
            var newPos = Points[i] + _velocities[i] * SpeedMultiplier * dt;

            bool bounced = false;
            for (int e = 0; e < boundary.Count; e++)
            {
                var a = boundary[e];
                var b = boundary[(e + 1) % boundary.Count];

                if (MathUtils.SegmentsIntersect(Points[i], newPos, a, b, out var hit))
                {
                    var edge = b - a;
                    var normal = new Vector2(-edge.Y, edge.X);
                    if (normal.LengthSquared() > 0)
                        normal = Vector2.Normalize(normal);

                    var v = _velocities[i];
                    var reflected = v - 2f * Vector2.Dot(v, normal) * normal;
                    _velocities[i] = reflected;
                    newPos = hit + Vector2.Normalize(reflected) * 2f;
                    bounced = true;
                    break;
                }
            }

            if (!bounced && !MathUtils.PointInPolygon(newPos, boundary))
            {
                _velocities[i] = -_velocities[i];
                newPos = Points[i] + _velocities[i] * SpeedMultiplier * dt;
            }

            Points[i] = newPos;
        }
    }

    public void Reset(PlayField playField)
    {
        Points.Clear();
        _velocities.Clear();
        var bounds = playField.Bounds;
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);

        for (int i = 0; i < PointCount; i++)
        {
            float offset = 30f;
            Points.Add(center + new Vector2(
                (float)(_rng.NextDouble() * offset * 2 - offset),
                (float)(_rng.NextDouble() * offset * 2 - offset)));

            float angle = (float)(_rng.NextDouble() * MathF.Tau);
            float speed = BaseSpeed * (0.5f + (float)_rng.NextDouble());
            _velocities.Add(new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed);
        }
    }
}
