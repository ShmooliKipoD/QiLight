using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using QiLight.Game.Core;

namespace QiLight.Game.Entities;

public class Sparx
{
    private const float BaseSpeed = 220f;
    private const int MaxTrailPositions = 8;

    public Vector2 Position { get; private set; }
    public List<Vector2> TrailPositions { get; } = new();
    public float SpeedMultiplier { get; set; } = 1f;

    private int _currentSegment;
    private float _t;
    private int _direction;

    public Sparx(PlayField playField, bool clockwise = true, int startSegment = 0)
    {
        _direction = clockwise ? 1 : -1;
        _currentSegment = startSegment % playField.Vertices.Count;
        _t = 0;
        Position = playField.Vertices[_currentSegment];
    }

    public void Update(GameTime gameTime, PlayField playField)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float moveDistance = BaseSpeed * SpeedMultiplier * dt;

        TrailPositions.Insert(0, Position);
        if (TrailPositions.Count > MaxTrailPositions)
            TrailPositions.RemoveAt(TrailPositions.Count - 1);

        var segments = playField.Segments;
        if (segments.Count == 0) return;
        if (_currentSegment >= segments.Count)
            _currentSegment %= segments.Count;

        while (moveDistance > 0.1f)
        {
            var seg = segments[_currentSegment];
            float segLen = seg.Length;
            if (segLen < 0.001f)
            {
                AdvanceSegment(segments.Count);
                continue;
            }

            if (_direction > 0)
            {
                float remaining = (1f - _t) * segLen;
                if (moveDistance < remaining)
                {
                    _t += moveDistance / segLen;
                    moveDistance = 0;
                }
                else
                {
                    moveDistance -= remaining;
                    AdvanceSegment(segments.Count);
                }
            }
            else
            {
                float remaining = _t * segLen;
                if (moveDistance < remaining)
                {
                    _t -= moveDistance / segLen;
                    moveDistance = 0;
                }
                else
                {
                    moveDistance -= remaining;
                    RetreatSegment(segments.Count);
                }
            }
        }

        var currentSeg = segments[_currentSegment];
        Position = Vector2.Lerp(currentSeg.Start, currentSeg.End, _t);
    }

    private void AdvanceSegment(int count)
    {
        _currentSegment = (_currentSegment + 1) % count;
        _t = 0;
    }

    private void RetreatSegment(int count)
    {
        _currentSegment = (_currentSegment - 1 + count) % count;
        _t = 1;
    }

    public void Reset(PlayField playField, bool clockwise = true, int startSegment = 0)
    {
        _direction = clockwise ? 1 : -1;
        _currentSegment = startSegment % playField.Vertices.Count;
        _t = 0;
        Position = playField.Vertices[_currentSegment];
        TrailPositions.Clear();
    }
}
