using OpenTK.Mathematics;
using Qutre3D.Core;

namespace Qutre3D.Render;

/// <summary>Генераторы геометрии. Всё в локальном пространстве, единичный размер.</summary>
public static class MeshFactory
{
    private static void Add(List<float> v, Vector3 pos, Vector3 normal, Vector2 uv, float height)
    {
        v.Add(pos.X); v.Add(pos.Y); v.Add(pos.Z);
        v.Add(normal.X); v.Add(normal.Y); v.Add(normal.Z);
        v.Add(uv.X); v.Add(uv.Y);
        v.Add(height);
    }

    public static Mesh Sphere(int segments = 64, int rings = 40)
    {
        var verts = new List<float>();
        var idx = new List<uint>();

        for (int y = 0; y <= rings; y++)
        {
            float v = y / (float)rings;
            float phi = v * MathF.PI;
            float sinPhi = MathF.Sin(phi);
            float cosPhi = MathF.Cos(phi);

            for (int x = 0; x <= segments; x++)
            {
                float u = x / (float)segments;
                float theta = u * MathF.Tau;
                var n = new Vector3(sinPhi * MathF.Cos(theta), cosPhi, sinPhi * MathF.Sin(theta));
                Add(verts, n, n, new Vector2(u, 1f - v), 0f);
            }
        }

        int stride = segments + 1;
        for (int y = 0; y < rings; y++)
        {
            for (int x = 0; x < segments; x++)
            {
                uint a = (uint)(y * stride + x);
                uint b = (uint)(a + 1);
                uint c = (uint)(a + stride);
                uint d = (uint)(c + 1);
                idx.Add(a); idx.Add(c); idx.Add(b);
                idx.Add(b); idx.Add(c); idx.Add(d);
            }
        }

        return Mesh.Create(verts.ToArray(), idx.ToArray());
    }

    public static Mesh Box()
    {
        var verts = new List<float>();
        var idx = new List<uint>();

        void Face(Vector3 origin, Vector3 right, Vector3 up, Vector3 normal)
        {
            uint start = (uint)(verts.Count / 9);
            Add(verts, origin, normal, new Vector2(0, 0), 0);
            Add(verts, origin + right, normal, new Vector2(1, 0), 0);
            Add(verts, origin + right + up, normal, new Vector2(1, 1), 0);
            Add(verts, origin + up, normal, new Vector2(0, 1), 0);

            idx.Add(start); idx.Add(start + 1); idx.Add(start + 2);
            idx.Add(start); idx.Add(start + 2); idx.Add(start + 3);
        }

        Face(new Vector3(-1, -1, 1), new Vector3(2, 0, 0), new Vector3(0, 2, 0), new Vector3(0, 0, 1));
        Face(new Vector3(1, -1, -1), new Vector3(-2, 0, 0), new Vector3(0, 2, 0), new Vector3(0, 0, -1));
        Face(new Vector3(-1, 1, 1), new Vector3(2, 0, 0), new Vector3(0, 0, -2), new Vector3(0, 1, 0));
        Face(new Vector3(-1, -1, -1), new Vector3(2, 0, 0), new Vector3(0, 0, 2), new Vector3(0, -1, 0));
        Face(new Vector3(1, -1, 1), new Vector3(0, 0, -2), new Vector3(0, 2, 0), new Vector3(1, 0, 0));
        Face(new Vector3(-1, -1, -1), new Vector3(0, 0, 2), new Vector3(0, 2, 0), new Vector3(-1, 0, 0));

        return Mesh.Create(verts.ToArray(), idx.ToArray());
    }

    public static Mesh Torus(float ringRadius = 1f, float tubeRadius = 0.38f, int major = 72, int minor = 28)
    {
        var verts = new List<float>();
        var idx = new List<uint>();

        for (int i = 0; i <= major; i++)
        {
            float u = i / (float)major;
            float theta = u * MathF.Tau;
            var center = new Vector3(MathF.Cos(theta) * ringRadius, 0f, MathF.Sin(theta) * ringRadius);
            var radial = new Vector3(MathF.Cos(theta), 0f, MathF.Sin(theta));

            for (int j = 0; j <= minor; j++)
            {
                float v = j / (float)minor;
                float phi = v * MathF.Tau;
                var n = radial * MathF.Cos(phi) + Vector3.UnitY * MathF.Sin(phi);
                Add(verts, center + n * tubeRadius, n, new Vector2(u, v), 0);
            }
        }

        int stride = minor + 1;
        for (int i = 0; i < major; i++)
        {
            for (int j = 0; j < minor; j++)
            {
                uint a = (uint)(i * stride + j);
                uint b = (uint)(a + 1);
                uint c = (uint)((i + 1) * stride + j);
                uint d = (uint)(c + 1);
                idx.Add(a); idx.Add(c); idx.Add(b);
                idx.Add(b); idx.Add(c); idx.Add(d);
            }
        }

        return Mesh.Create(verts.ToArray(), idx.ToArray());
    }

    public static Mesh Cylinder(int segments = 64)
    {
        var verts = new List<float>();
        var idx = new List<uint>();

        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            float theta = u * MathF.Tau;
            var n = new Vector3(MathF.Cos(theta), 0f, MathF.Sin(theta));
            Add(verts, new Vector3(n.X, -1f, n.Z), n, new Vector2(u, 0), 0);
            Add(verts, new Vector3(n.X, 1f, n.Z), n, new Vector2(u, 1), 0);
        }

        for (int i = 0; i < segments; i++)
        {
            uint a = (uint)(i * 2);
            idx.Add(a); idx.Add(a + 1); idx.Add(a + 2);
            idx.Add(a + 1); idx.Add(a + 3); idx.Add(a + 2);
        }

        // крышки
        uint topCenter = (uint)(verts.Count / 9);
        Add(verts, new Vector3(0, 1, 0), Vector3.UnitY, new Vector2(0.5f, 0.5f), 0);
        for (int i = 0; i <= segments; i++)
        {
            float theta = i / (float)segments * MathF.Tau;
            Add(verts, new Vector3(MathF.Cos(theta), 1f, MathF.Sin(theta)), Vector3.UnitY, new Vector2(0, 0), 0);
        }
        for (int i = 0; i < segments; i++)
        {
            idx.Add(topCenter); idx.Add(topCenter + 1 + (uint)i); idx.Add(topCenter + 2 + (uint)i);
        }

        uint bottomCenter = (uint)(verts.Count / 9);
        Add(verts, new Vector3(0, -1, 0), -Vector3.UnitY, new Vector2(0.5f, 0.5f), 0);
        for (int i = 0; i <= segments; i++)
        {
            float theta = i / (float)segments * MathF.Tau;
            Add(verts, new Vector3(MathF.Cos(theta), -1f, MathF.Sin(theta)), -Vector3.UnitY, new Vector2(0, 0), 0);
        }
        for (int i = 0; i < segments; i++)
        {
            idx.Add(bottomCenter); idx.Add(bottomCenter + 2 + (uint)i); idx.Add(bottomCenter + 1 + (uint)i);
        }

        return Mesh.Create(verts.ToArray(), idx.ToArray());
    }

    // ------------------------------------------------------------- фигуры вращения

    /// <summary>Тело вращения по профилю (r, y) — кружки, чайники, вазы.</summary>
    private static void AddLathe(List<float> verts, List<uint> idx, IReadOnlyList<Vector2> profile, int segments)
    {
        int rows = profile.Count;
        int stride = segments + 1;

        var normals = new Vector2[rows];
        for (int i = 0; i < rows; i++)
        {
            Vector2 prev = profile[Math.Max(i - 1, 0)];
            Vector2 next = profile[Math.Min(i + 1, rows - 1)];
            Vector2 t = next - prev;
            if (t.LengthSquared < 1e-8f) t = Vector2.UnitY;
            t = Vector2.Normalize(t);
            normals[i] = new Vector2(t.Y, -t.X);
        }

        uint start = (uint)(verts.Count / 9);
        for (int i = 0; i < rows; i++)
        {
            float r = profile[i].X;
            float y = profile[i].Y;
            Vector2 n2 = normals[i];
            for (int j = 0; j <= segments; j++)
            {
                float u = j / (float)segments;
                float theta = u * MathF.Tau;
                float ct = MathF.Cos(theta);
                float st = MathF.Sin(theta);
                var pos = new Vector3(r * ct, y, r * st);
                Vector3 n = r < 1e-4f
                    ? new Vector3(0f, n2.Y >= 0f ? 1f : -1f, 0f)
                    : new Vector3(n2.X * ct, n2.Y, n2.X * st);
                Add(verts, pos, n, new Vector2(u, i / (float)(rows - 1)), 0f);
            }
        }

        for (int i = 0; i < rows - 1; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                uint a = start + (uint)(i * stride + j);
                uint b = a + 1;
                uint c = a + (uint)stride;
                uint d = c + 1;
                idx.Add(a); idx.Add(c); idx.Add(b);
                idx.Add(b); idx.Add(c); idx.Add(d);
            }
        }
    }

    /// <summary>Труба по опорным точкам — ручки и носики.</summary>
    private static void AddTube(List<float> verts, List<uint> idx, IReadOnlyList<Vector3> points,
        float r0, float r1, int ring)
    {
        int n = points.Count;
        if (n < 2) return;

        var tangents = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 a = points[Math.Max(i - 1, 0)];
            Vector3 b = points[Math.Min(i + 1, n - 1)];
            Vector3 t = b - a;
            tangents[i] = t.LengthSquared > 1e-8f ? Vector3.Normalize(t) : Vector3.UnitY;
        }

        uint start = (uint)(verts.Count / 9);
        for (int i = 0; i < n; i++)
        {
            float f = i / (float)(n - 1);
            float radius = r0 + (r1 - r0) * f;
            Vector3 t = tangents[i];
            Vector3 n1 = Vector3.Cross(t, Vector3.UnitZ);
            if (n1.LengthSquared < 1e-6f) n1 = Vector3.Cross(t, Vector3.UnitX);
            n1 = Vector3.Normalize(n1);
            Vector3 n2 = Vector3.Normalize(Vector3.Cross(t, n1));

            for (int j = 0; j <= ring; j++)
            {
                float phi = j / (float)ring * MathF.Tau;
                Vector3 dir = n1 * MathF.Cos(phi) + n2 * MathF.Sin(phi);
                Add(verts, points[i] + dir * radius, dir, new Vector2(j / (float)ring, f), 0f);
            }
        }

        int stride = ring + 1;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < ring; j++)
            {
                uint a = start + (uint)(i * stride + j);
                uint b = a + 1;
                uint c = a + (uint)stride;
                uint d = c + 1;
                idx.Add(a); idx.Add(c); idx.Add(b);
                idx.Add(b); idx.Add(c); idx.Add(d);
            }
        }

        AddTubeCap(verts, idx, points[0], -tangents[0], r0, ring);
        AddTubeCap(verts, idx, points[n - 1], tangents[n - 1], r1, ring);
    }

    private static void AddTubeCap(List<float> verts, List<uint> idx, Vector3 center, Vector3 normal, float radius, int ring)
    {
        Vector3 n1 = Vector3.Cross(normal, Vector3.UnitZ);
        if (n1.LengthSquared < 1e-6f) n1 = Vector3.Cross(normal, Vector3.UnitX);
        n1 = Vector3.Normalize(n1);
        Vector3 n2 = Vector3.Normalize(Vector3.Cross(normal, n1));

        uint c = (uint)(verts.Count / 9);
        Add(verts, center, normal, new Vector2(0.5f, 0.5f), 0f);
        for (int j = 0; j <= ring; j++)
        {
            float phi = j / (float)ring * MathF.Tau;
            Vector3 dir = n1 * MathF.Cos(phi) + n2 * MathF.Sin(phi);
            Add(verts, center + dir * radius, normal,
                new Vector2(0.5f + 0.5f * MathF.Cos(phi), 0.5f + 0.5f * MathF.Sin(phi)), 0f);
        }

        for (int j = 0; j < ring; j++)
        {
            idx.Add(c); idx.Add(c + 1 + (uint)j); idx.Add(c + 2 + (uint)j);
        }
    }

    private static List<Vector3> CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps)
    {
        var points = new List<Vector3>(steps + 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float u = 1f - t;
            points.Add(u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3);
        }
        return points;
    }

    /// <summary>Кружка: тело вращения с полой внутренностью и C-образная ручка.</summary>
    public static Mesh Mug()
    {
        var verts = new List<float>();
        var idx = new List<uint>();

        Vector2[] profile =
        {
            new(0.00f, -0.98f),
            new(0.54f, -0.98f),
            new(0.72f, -0.92f),
            new(0.79f, -0.78f),
            new(0.80f,  0.70f),
            new(0.83f,  0.90f),
            new(0.80f,  1.00f),
            new(0.72f,  1.00f),
            new(0.70f,  0.84f),
            new(0.68f, -0.70f),
            new(0.57f, -0.84f),
            new(0.00f, -0.84f),
        };
        AddLathe(verts, idx, profile, 48);

        var handle = CubicBezier(
            new Vector3(0.76f, 0.60f, 0f),
            new Vector3(1.52f, 0.52f, 0f),
            new Vector3(1.52f, -0.26f, 0f),
            new Vector3(0.74f, -0.38f, 0f), 16);
        AddTube(verts, idx, handle, 0.085f, 0.085f, 12);

        return Mesh.Create(verts.ToArray(), idx.ToArray());
    }

    /// <summary>Чайник: пузатое тело с крышкой, носик и ручка.</summary>
    public static Mesh Teapot()
    {
        var verts = new List<float>();
        var idx = new List<uint>();

        Vector2[] profile =
        {
            new(0.00f, -0.98f),
            new(0.46f, -0.98f),
            new(0.76f, -0.90f),
            new(0.92f, -0.62f),
            new(0.98f, -0.10f),
            new(0.94f,  0.34f),
            new(0.76f,  0.64f),
            new(0.54f,  0.78f),
            new(0.50f,  0.86f),
            new(0.58f,  0.90f),
            new(0.56f,  0.98f),
            new(0.36f,  1.02f),
            new(0.18f,  1.05f),
            new(0.10f,  1.09f),
            new(0.13f,  1.13f),
            new(0.10f,  1.18f),
            new(0.00f,  1.20f),
        };
        AddLathe(verts, idx, profile, 48);

        var spout = CubicBezier(
            new Vector3(0.84f, -0.10f, 0f),
            new Vector3(1.30f, -0.02f, 0f),
            new Vector3(1.38f,  0.42f, 0f),
            new Vector3(1.52f,  0.62f, 0f), 16);
        AddTube(verts, idx, spout, 0.15f, 0.075f, 12);

        var handle = CubicBezier(
            new Vector3(-0.72f, 0.62f, 0f),
            new Vector3(-1.55f, 0.55f, 0f),
            new Vector3(-1.55f, -0.30f, 0f),
            new Vector3(-0.92f, -0.40f, 0f), 18);
        AddTube(verts, idx, handle, 0.09f, 0.09f, 12);

        return Mesh.Create(verts.ToArray(), idx.ToArray());
    }

    /// <summary>Плоскость воды: единичный квадрат в плоскости XZ.</summary>
    public static Mesh Quad()
    {
        var verts = new List<float>();
        Add(verts, new Vector3(-1, 0, -1), Vector3.UnitY, new Vector2(0, 0), 0);
        Add(verts, new Vector3(1, 0, -1), Vector3.UnitY, new Vector2(1, 0), 0);
        Add(verts, new Vector3(1, 0, 1), Vector3.UnitY, new Vector2(1, 1), 0);
        Add(verts, new Vector3(-1, 0, 1), Vector3.UnitY, new Vector2(0, 1), 0);

        return Mesh.Create(verts.ToArray(), new uint[] { 0, 2, 1, 0, 3, 2 });
    }

    /// <summary>Процедурный ландшафт по TerrainSettings.</summary>
    public static Mesh BuildTerrain(TerrainSettings t)
    {
        int res = Math.Clamp(t.Resolution, 32, 320);
        int side = res + 1;
        float half = t.Size * 0.5f;
        float step = t.Size / res;

        var heights = new float[side * side];
        for (int z = 0; z < side; z++)
        {
            float v = z / (float)res * 2f - 1f;
            for (int x = 0; x < side; x++)
            {
                float u = x / (float)res * 2f - 1f;
                heights[z * side + x] = TerrainGenerator.Height01(u, v, t);
            }
        }

        var verts = new List<float>(side * side * 9);
        var idx = new List<uint>(res * res * 6);

        for (int z = 0; z < side; z++)
        {
            float pz = -half + z * step;
            for (int x = 0; x < side; x++)
            {
                float px = -half + x * step;
                float h = heights[z * side + x];
                float hl = heights[z * side + Math.Max(x - 1, 0)];
                float hr = heights[z * side + Math.Min(x + 1, res)];
                float hd = heights[Math.Max(z - 1, 0) * side + x];
                float hu = heights[Math.Min(z + 1, res) * side + x];
                float dx = (x == 0 || x == res) ? step : step * 2f;
                float dz = (z == 0 || z == res) ? step : step * 2f;

                var n = Vector3.Normalize(new Vector3(
                    -(hr - hl) * t.Height / dx,
                    1f,
                    -(hu - hd) * t.Height / dz));

                // Края острова чуть топим под воду, иначе виден шов «плоскость/вода».
                float sink = 2.2f * (1f - Noise.Smoothstep(0f, 0.05f, h));

                Add(verts, new Vector3(px, h * t.Height - sink, pz), n,
                    new Vector2(x / (float)res, z / (float)res), h);
            }
        }

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                uint a = (uint)(z * side + x);
                uint b = a + 1;
                uint c = (uint)((z + 1) * side + x);
                uint d = c + 1;
                idx.Add(a); idx.Add(c); idx.Add(b);
                idx.Add(b); idx.Add(c); idx.Add(d);
            }
        }

        return Mesh.Create(verts.ToArray(), idx.ToArray());
    }

    /// <summary>Сетка-помощник на плоскости XZ.</summary>
    public static Mesh Grid(int halfLines = 60, float spacing = 2f)
    {
        var positions = new List<float>();
        var indices = new List<uint>();
        float extent = halfLines * spacing;

        for (int i = -halfLines; i <= halfLines; i++)
        {
            float p = i * spacing;
            uint a = (uint)(positions.Count / 3);
            positions.Add(-extent); positions.Add(0); positions.Add(p);
            positions.Add(extent); positions.Add(0); positions.Add(p);
            indices.Add(a); indices.Add(a + 1);

            a = (uint)(positions.Count / 3);
            positions.Add(p); positions.Add(0); positions.Add(-extent);
            positions.Add(p); positions.Add(0); positions.Add(extent);
            indices.Add(a); indices.Add(a + 1);
        }

        return Mesh.CreateLines(positions.ToArray(), indices.ToArray());
    }
}
