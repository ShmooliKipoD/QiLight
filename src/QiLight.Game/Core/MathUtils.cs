using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QiLight.Game.Core;

public static class MathUtils
{
    private const float Epsilon = 0.001f;

    public static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        var d1 = a2 - a1;
        var d2 = b2 - b1;
        var cross = d1.X * d2.Y - d1.Y * d2.X;

        if (MathF.Abs(cross) < Epsilon)
            return false;

        var d = b1 - a1;
        var t = (d.X * d2.Y - d.Y * d2.X) / cross;
        var u = (d.X * d1.Y - d.Y * d1.X) / cross;

        if (t >= -Epsilon && t <= 1 + Epsilon && u >= -Epsilon && u <= 1 + Epsilon)
        {
            intersection = a1 + t * d1;
            return true;
        }

        return false;
    }

    public static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) /
                (polygon[j].Y - polygon[i].Y) + polygon[i].X)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    public static float PolygonArea(List<Vector2> vertices)
    {
        float area = 0;
        int j = vertices.Count - 1;
        for (int i = 0; i < vertices.Count; i++)
        {
            area += (vertices[j].X + vertices[i].X) * (vertices[j].Y - vertices[i].Y);
            j = i;
        }
        return MathF.Abs(area / 2f);
    }

    public static List<int> EarClipTriangulate(List<Vector2> polygon)
    {
        var indices = new List<int>();
        var verts = new List<int>();
        for (int i = 0; i < polygon.Count; i++)
            verts.Add(i);

        bool isCCW = SignedPolygonArea(polygon) > 0;

        int safety = polygon.Count * 3;
        while (verts.Count > 2 && safety-- > 0)
        {
            bool earFound = false;
            for (int i = 0; i < verts.Count; i++)
            {
                int prev = verts[(i - 1 + verts.Count) % verts.Count];
                int curr = verts[i];
                int next = verts[(i + 1) % verts.Count];

                var a = polygon[prev];
                var b = polygon[curr];
                var c = polygon[next];

                float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                bool isConvex = isCCW ? cross > 0 : cross < 0;

                if (!isConvex) continue;

                bool containsPoint = false;
                for (int j = 0; j < verts.Count; j++)
                {
                    if (j == (i - 1 + verts.Count) % verts.Count || j == i || j == (i + 1) % verts.Count)
                        continue;
                    if (PointInTriangle(polygon[verts[j]], a, b, c))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (!containsPoint)
                {
                    indices.Add(prev);
                    indices.Add(curr);
                    indices.Add(next);
                    verts.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound) break;
        }

        return indices;
    }

    public static float SignedPolygonArea(List<Vector2> vertices)
    {
        float area = 0;
        int j = vertices.Count - 1;
        for (int i = 0; i < vertices.Count; i++)
        {
            area += (vertices[j].X + vertices[i].X) * (vertices[j].Y - vertices[i].Y);
            j = i;
        }
        return area / 2f;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }

    public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var ap = point - a;
        float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
        t = MathHelper.Clamp(t, 0, 1);
        var closest = a + t * ab;
        return Vector2.Distance(point, closest);
    }

    public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        if (ab.LengthSquared() < Epsilon) return a;
        float t = Vector2.Dot(point - a, ab) / Vector2.Dot(ab, ab);
        t = MathHelper.Clamp(t, 0, 1);
        return a + t * ab;
    }
}
