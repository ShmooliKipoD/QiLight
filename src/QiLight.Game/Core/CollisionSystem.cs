using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace QiLight.Game.Core;

public class CollisionSystem
{
    private const float PlayerHitRadius = 8f;
    private const float SparxHitRadius = 12f;

    public bool CheckQixVsTrail(List<Vector2> qixPoints, List<Vector2> trail)
    {
        if (trail.Count < 2 || qixPoints.Count < 2) return false;

        for (int i = 0; i < qixPoints.Count - 1; i++)
        {
            for (int j = 0; j < trail.Count - 1; j++)
            {
                if (MathUtils.SegmentsIntersect(qixPoints[i], qixPoints[i + 1],
                    trail[j], trail[j + 1], out _))
                    return true;
            }
        }

        return false;
    }

    public bool CheckQixVsPlayer(List<Vector2> qixPoints, Vector2 playerPos)
    {
        if (qixPoints.Count < 2) return false;

        for (int i = 0; i < qixPoints.Count - 1; i++)
        {
            float dist = MathUtils.DistanceToSegment(playerPos, qixPoints[i], qixPoints[i + 1]);
            if (dist < PlayerHitRadius) return true;
        }

        return false;
    }

    public bool CheckSparxVsPlayer(Vector2 sparxPos, Vector2 playerPos)
    {
        return Vector2.Distance(sparxPos, playerPos) < SparxHitRadius;
    }

    public bool CheckTrailSelfIntersection(List<Vector2> trail)
    {
        if (trail.Count < 4) return false;

        for (int i = 0; i < trail.Count - 3; i++)
        {
            for (int j = i + 2; j < trail.Count - 1; j++)
            {
                if (i == 0 && j == trail.Count - 2) continue;
                if (MathUtils.SegmentsIntersect(trail[i], trail[i + 1],
                    trail[j], trail[j + 1], out _))
                    return true;
            }
        }

        return false;
    }
}
