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
