using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace QiLight.Game.Rendering;

public class NeonRenderer
{
    private readonly GraphicsDevice _device;
    private BasicEffect _effect = null!;

    public NeonRenderer(GraphicsDevice device)
    {
        _device = device;
    }

    public void LoadContent()
    {
        _effect = new BasicEffect(_device)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            World = Matrix.Identity,
            View = Matrix.Identity
        };
        UpdateProjection();
    }

    public void UpdateProjection()
    {
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            0, _device.Viewport.Width, _device.Viewport.Height, 0, 0, 1);
    }

    public void DrawLine(Vector2 a, Vector2 b, Color color, float thickness = 3f)
    {
        var dir = b - a;
        if (dir.LengthSquared() < 0.001f) return;
        dir = Vector2.Normalize(dir);
        var perp = new Vector2(-dir.Y, dir.X) * thickness * 0.5f;

        var vertices = new VertexPositionColor[]
        {
            new(new Vector3(a + perp, 0), color),
            new(new Vector3(a - perp, 0), color),
            new(new Vector3(b + perp, 0), color),
            new(new Vector3(b - perp, 0), color)
        };

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 2);
        }
    }

    public void DrawLines(List<Vector2> points, Color color, float thickness = 3f, bool closed = false)
    {
        if (points.Count < 2) return;

        int count = closed ? points.Count : points.Count - 1;
        for (int i = 0; i < count; i++)
        {
            DrawLine(points[i], points[(i + 1) % points.Count], color, thickness);
        }
    }

    public void DrawDot(Vector2 pos, Color color, float radius = 5f)
    {
        int segments = 8;
        var vertices = new VertexPositionColor[segments * 3];

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * MathF.Tau / segments;
            float angle2 = (i + 1) * MathF.Tau / segments;
            var p1 = pos + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
            var p2 = pos + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;

            vertices[i * 3] = new VertexPositionColor(new Vector3(pos, 0), color);
            vertices[i * 3 + 1] = new VertexPositionColor(new Vector3(p1, 0), color);
            vertices[i * 3 + 2] = new VertexPositionColor(new Vector3(p2, 0), color);
        }

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, segments);
        }
    }

    public void DrawFilledPolygon(List<Vector2> vertices, List<int> triangleIndices, Color color)
    {
        if (triangleIndices.Count < 3) return;

        var verts = new VertexPositionColor[triangleIndices.Count];
        for (int i = 0; i < triangleIndices.Count; i++)
        {
            int idx = triangleIndices[i];
            if (idx >= 0 && idx < vertices.Count)
                verts[i] = new VertexPositionColor(new Vector3(vertices[idx], 0), color);
        }

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList, verts, 0, triangleIndices.Count / 3);
        }
    }
}
