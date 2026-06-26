using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QiLight.Game.Core;

public struct LineSegment
{
    public Vector2 Start;
    public Vector2 End;

    public LineSegment(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    public float Length => Vector2.Distance(Start, End);

    public Vector2 Direction => Vector2.Normalize(End - Start);
}

public class PlayField
{
    public const int Margin = 40;

    public List<Vector2> Vertices { get; private set; }
    public List<LineSegment> Segments { get; private set; } = new();
    public float TotalPerimeter { get; private set; }

    public int Width { get; }
    public int Height { get; }

    public PlayField(int screenWidth, int screenHeight)
    {
        Width = screenWidth - Margin * 2;
        Height = screenHeight - Margin * 2;

        Vertices = new List<Vector2>
        {
            new(Margin, Margin),
            new(Margin + Width, Margin),
            new(Margin + Width, Margin + Height),
            new(Margin, Margin + Height)
        };

        RebuildSegments();
    }

    public void UpdateBorder(List<Vector2> newBorderVertices)
    {
        Vertices = newBorderVertices;
        RebuildSegments();
    }

    private void RebuildSegments()
    {
        Segments = new List<LineSegment>();
        TotalPerimeter = 0;
        for (int i = 0; i < Vertices.Count; i++)
        {
            var seg = new LineSegment(Vertices[i], Vertices[(i + 1) % Vertices.Count]);
            Segments.Add(seg);
            TotalPerimeter += seg.Length;
        }
    }

    public bool IsOnBorder(Vector2 point, float tolerance = 2f)
    {
        foreach (var seg in Segments)
        {
            if (MathUtils.DistanceToSegment(point, seg.Start, seg.End) < tolerance)
                return true;
        }
        return false;
    }

    public Vector2 SnapToBorder(Vector2 point)
    {
        float minDist = float.MaxValue;
        Vector2 closest = point;

        foreach (var seg in Segments)
        {
            var c = MathUtils.ClosestPointOnSegment(point, seg.Start, seg.End);
            float d = Vector2.Distance(point, c);
            if (d < minDist)
            {
                minDist = d;
                closest = c;
            }
        }

        return closest;
    }

    public (int segmentIndex, float t) GetBorderPosition(Vector2 point)
    {
        float minDist = float.MaxValue;
        int bestSeg = 0;
        float bestT = 0;

        for (int i = 0; i < Segments.Count; i++)
        {
            var seg = Segments[i];
            var ab = seg.End - seg.Start;
            float len = ab.Length();
            if (len < 0.001f) continue;
            float t = Vector2.Dot(point - seg.Start, ab) / (len * len);
            t = MathHelper.Clamp(t, 0, 1);
            var closest = seg.Start + t * ab;
            float d = Vector2.Distance(point, closest);
            if (d < minDist)
            {
                minDist = d;
                bestSeg = i;
                bestT = t;
            }
        }

        return (bestSeg, bestT);
    }

    public Vector2 MoveAlongBorder(Vector2 current, Input.MoveDirection direction, float speed)
    {
        var (segIdx, t) = GetBorderPosition(current);
        var seg = Segments[segIdx];

        Vector2 desiredDir = direction switch
        {
            Input.MoveDirection.Right => Vector2.UnitX,
            Input.MoveDirection.Left => -Vector2.UnitX,
            Input.MoveDirection.Down => Vector2.UnitY,
            Input.MoveDirection.Up => -Vector2.UnitY,
            _ => Vector2.Zero
        };

        if (desiredDir == Vector2.Zero) return current;

        float dot = Vector2.Dot(seg.Direction, desiredDir);
        float remaining = speed;

        if (dot > 0.1f)
        {
            float distToEnd = Vector2.Distance(current, seg.End);
            if (remaining <= distToEnd)
                return current + seg.Direction * remaining;

            remaining -= distToEnd;
            int nextSeg = (segIdx + 1) % Segments.Count;
            return MoveOnSegment(Segments[nextSeg].Start, Segments[nextSeg], remaining, 1);
        }
        else if (dot < -0.1f)
        {
            float distToStart = Vector2.Distance(current, seg.Start);
            if (remaining <= distToStart)
                return current - seg.Direction * remaining;

            remaining -= distToStart;
            int prevSeg = (segIdx - 1 + Segments.Count) % Segments.Count;
            return MoveOnSegment(Segments[prevSeg].End, Segments[prevSeg], remaining, -1);
        }
        else
        {
            int fwdSeg = (segIdx + 1) % Segments.Count;
            float fwdDot = Vector2.Dot(Segments[fwdSeg].Direction, desiredDir);
            int bwdSeg = (segIdx - 1 + Segments.Count) % Segments.Count;
            float bwdDot = Vector2.Dot(-Segments[bwdSeg].Direction, desiredDir);

            if (fwdDot > 0.1f && Vector2.Distance(current, seg.End) < 2f)
                return MoveOnSegment(Segments[fwdSeg].Start, Segments[fwdSeg], remaining, 1);
            if (bwdDot > 0.1f && Vector2.Distance(current, seg.Start) < 2f)
                return MoveOnSegment(Segments[bwdSeg].End, Segments[bwdSeg], remaining, -1);
        }

        return current;
    }

    private Vector2 MoveOnSegment(Vector2 pos, LineSegment seg, float dist, int dir)
    {
        if (dir > 0)
        {
            float segLen = seg.Length;
            if (dist <= segLen)
                return pos + seg.Direction * dist;
            return seg.End;
        }
        else
        {
            var revDir = -seg.Direction;
            float segLen = seg.Length;
            if (dist <= segLen)
                return pos + revDir * dist;
            return seg.Start;
        }
    }

    public Rectangle Bounds => new(Margin, Margin, Width, Height);

    public float TotalArea => Width * Height;
}
