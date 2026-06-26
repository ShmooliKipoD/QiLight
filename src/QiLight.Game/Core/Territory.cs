using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace QiLight.Game.Core;

public class CapturedRegion
{
    public List<Vector2> Polygon { get; }
    public List<int> TriangleIndices { get; }
    public float Area { get; }

    public CapturedRegion(List<Vector2> polygon)
    {
        Polygon = polygon;
        Area = MathUtils.PolygonArea(polygon);
        TriangleIndices = MathUtils.EarClipTriangulate(polygon);
    }
}

public class Territory
{
    public List<Vector2> UncapturedPolygon { get; private set; }
    public List<CapturedRegion> CapturedRegions { get; } = new();
    public float TotalArea { get; }
    public float CapturedArea { get; private set; }
    public float CapturedPercentage => CapturedArea / TotalArea * 100f;

    private Vector2 _qixPosition;

    public Territory(PlayField playField)
    {
        UncapturedPolygon = new List<Vector2>(playField.Vertices);
        TotalArea = MathUtils.PolygonArea(UncapturedPolygon);
    }

    public void SetQixPosition(Vector2 pos)
    {
        _qixPosition = pos;
    }

    public void CompleteCut(List<Vector2> trail, Player player, float drawDuration)
    {
        if (trail.Count < 2) return;

        var startPoint = trail[0];
        var endPoint = trail[^1];

        int startEdge = FindNearestEdge(startPoint);
        int endEdge = FindNearestEdge(endPoint);

        if (startEdge < 0 || endEdge < 0) return;

        var poly1 = BuildSubPolygon(startPoint, endPoint, startEdge, endEdge, trail, true);
        var poly2 = BuildSubPolygon(startPoint, endPoint, startEdge, endEdge, trail, false);

        if (poly1.Count < 3 || poly2.Count < 3) return;

        bool qixInPoly1 = MathUtils.PointInPolygon(_qixPosition, poly1);
        bool qixInPoly2 = MathUtils.PointInPolygon(_qixPosition, poly2);

        List<Vector2> uncaptured;
        List<Vector2> captured;

        if (qixInPoly1 && !qixInPoly2)
        {
            uncaptured = poly1;
            captured = poly2;
        }
        else if (qixInPoly2 && !qixInPoly1)
        {
            uncaptured = poly2;
            captured = poly1;
        }
        else
        {
            float area1 = MathUtils.PolygonArea(poly1);
            float area2 = MathUtils.PolygonArea(poly2);
            if (area1 > area2)
            {
                uncaptured = poly1;
                captured = poly2;
            }
            else
            {
                uncaptured = poly2;
                captured = poly1;
            }
        }

        var region = new CapturedRegion(captured);
        CapturedRegions.Add(region);
        CapturedArea += region.Area;
        UncapturedPolygon = uncaptured;

        int baseScore = (int)(region.Area / 10f);
        if (drawDuration < 3f) baseScore *= 2;
        player.Score += baseScore;
    }

    private int FindNearestEdge(Vector2 point)
    {
        float minDist = float.MaxValue;
        int bestEdge = -1;

        for (int i = 0; i < UncapturedPolygon.Count; i++)
        {
            var a = UncapturedPolygon[i];
            var b = UncapturedPolygon[(i + 1) % UncapturedPolygon.Count];
            float d = MathUtils.DistanceToSegment(point, a, b);
            if (d < minDist)
            {
                minDist = d;
                bestEdge = i;
            }
        }

        return minDist < 5f ? bestEdge : -1;
    }

    private List<Vector2> BuildSubPolygon(Vector2 start, Vector2 end, int startEdge, int endEdge,
        List<Vector2> trail, bool clockwise)
    {
        var poly = new List<Vector2>();

        if (clockwise)
        {
            poly.Add(start);

            for (int i = 1; i < trail.Count; i++)
                poly.Add(trail[i]);

            int idx = (endEdge + 1) % UncapturedPolygon.Count;
            int safety = UncapturedPolygon.Count + 1;
            while (idx != (startEdge + 1) % UncapturedPolygon.Count && safety-- > 0)
            {
                poly.Add(UncapturedPolygon[idx]);
                idx = (idx + 1) % UncapturedPolygon.Count;
            }
        }
        else
        {
            poly.Add(end);

            for (int i = trail.Count - 2; i >= 0; i--)
                poly.Add(trail[i]);

            int idx = startEdge;
            if (idx < 0) idx += UncapturedPolygon.Count;
            int safety = UncapturedPolygon.Count + 1;
            int target = endEdge;
            while (idx != target && safety-- > 0)
            {
                poly.Add(UncapturedPolygon[idx]);
                idx = (idx - 1 + UncapturedPolygon.Count) % UncapturedPolygon.Count;
            }
            if (safety > 0)
                poly.Add(UncapturedPolygon[target]);
        }

        return RemoveDuplicateVertices(poly);
    }

    private static List<Vector2> RemoveDuplicateVertices(List<Vector2> vertices)
    {
        if (vertices.Count == 0) return vertices;
        var result = new List<Vector2> { vertices[0] };
        for (int i = 1; i < vertices.Count; i++)
        {
            if (Vector2.Distance(vertices[i], result[^1]) > 1f)
                result.Add(vertices[i]);
        }
        if (result.Count > 1 && Vector2.Distance(result[^1], result[0]) < 1f)
            result.RemoveAt(result.Count - 1);
        return result;
    }

    public void Reset(PlayField playField)
    {
        UncapturedPolygon = new List<Vector2>(playField.Vertices);
        CapturedRegions.Clear();
        CapturedArea = 0;
    }
}
