using System.Numerics;
using ImGuiNET;
using Qutre3D.Core;

namespace Qutre3D.UI;

public enum ToolGlyph
{
    Select,
    Move,
    Orbit,
    Zoom,
    Frame,
    Home,
    Grid,
    Render,
    Save,
    Delete,
    Plus,
    Eye,
    Camera,
    Duplicate,
    Terrain,
}

/// <summary>
/// Процедурные иконки: объёмные примитивы в духе полки Bryce и линейные
/// пиктограммы инструментов. Никаких внешних файлов — всё рисуется кодом.
/// </summary>
public static class PrimitiveIcons
{
    // Бирюзовый «позитив» Qutre и золотой «утилитарный» акцент.
    private static readonly Vector4 TealHi = new(0.42f, 0.82f, 0.84f, 1f);
    private static readonly Vector4 TealMid = new(0.10f, 0.50f, 0.62f, 1f);
    private static readonly Vector4 TealLo = new(0.035f, 0.24f, 0.34f, 1f);
    private static readonly Vector4 GoldHi = new(1.00f, 0.88f, 0.48f, 1f);
    private static readonly Vector4 GoldMid = new(0.89f, 0.67f, 0.18f, 1f);
    private static readonly Vector4 GoldLo = new(0.54f, 0.37f, 0.07f, 1f);
    private static readonly Vector4 GreenHi = new(0.44f, 0.66f, 0.36f, 1f);
    private static readonly Vector4 GreenMid = new(0.20f, 0.40f, 0.24f, 1f);
    private static readonly Vector4 GreenLo = new(0.08f, 0.20f, 0.13f, 1f);
    private static readonly Vector4 WaterHi = new(0.44f, 0.78f, 0.82f, 1f);

    private static uint C(Vector4 c, float alpha = 1f) => Theme.U32(Theme.A(c, alpha));

    private static float R(float v) => MathF.Round(v);

    // -------------------------------------------------------------- примитивы

    public static void Draw(ImDrawListPtr dl, EntityKind kind, Vector2 center, float size, bool gold = false)
    {
        float r = size * 0.5f;
        var palette = gold
            ? (Hi: GoldHi, Mid: GoldMid, Lo: GoldLo)
            : (Hi: TealHi, Mid: TealMid, Lo: TealLo);

        switch (kind)
        {
            case EntityKind.Sphere: Sphere(dl, center, r, palette); break;
            case EntityKind.Box: Box(dl, center, r, palette); break;
            case EntityKind.Torus: Torus(dl, center, r, palette); break;
            case EntityKind.Cylinder: Cylinder(dl, center, r, palette); break;
            case EntityKind.Terrain: Terrain(dl, center, r); break;
            case EntityKind.Water: Water(dl, center, r); break;
            case EntityKind.Plane: Plane(dl, center, r); break;
            default: Sphere(dl, center, r, palette); break;
        }
    }

    private static void Sphere(ImDrawListPtr dl, Vector2 c, float r,
        (Vector4 Hi, Vector4 Mid, Vector4 Lo) p)
    {
        EllipseFill(dl, new Vector2(c.X, c.Y + r * 0.82f), r * 0.80f, r * 0.22f, C(Theme.Shadow, 0.30f));
        dl.AddCircleFilled(c, r * 0.78f, C(p.Mid), 32);
        dl.AddCircleFilled(new Vector2(c.X - r * 0.20f, c.Y - r * 0.22f), r * 0.56f, C(p.Hi, 0.92f), 28);
        dl.AddCircleFilled(new Vector2(c.X - r * 0.30f, c.Y - r * 0.34f), r * 0.13f, C(new Vector4(1f, 1f, 1f, 1f), 0.92f), 12);
        dl.AddCircle(c, r * 0.78f, C(p.Lo), 32, 1.2f);
    }

    private static void Box(ImDrawListPtr dl, Vector2 c, float r,
        (Vector4 Hi, Vector4 Mid, Vector4 Lo) p)
    {
        EllipseFill(dl, new Vector2(c.X, c.Y + r * 0.86f), r * 0.78f, r * 0.20f, C(Theme.Shadow, 0.28f));

        var t = new Vector2(c.X, c.Y - r * 0.92f);
        var rt = new Vector2(c.X + r * 0.86f, c.Y - r * 0.38f);
        var bt = new Vector2(c.X, c.Y + r * 0.16f);
        var lt = new Vector2(c.X - r * 0.86f, c.Y - r * 0.38f);
        var lb = new Vector2(lt.X, lt.Y + r * 0.68f);
        var bb = new Vector2(bt.X, bt.Y + r * 0.68f);
        var rb = new Vector2(rt.X, rt.Y + r * 0.68f);

        dl.AddQuadFilled(t, rt, bt, lt, C(p.Hi));
        dl.AddQuadFilled(lt, bt, bb, lb, C(p.Mid));
        dl.AddQuadFilled(bt, rt, rb, bb, C(p.Lo));

        dl.AddLine(t, rt, C(p.Lo, 0.55f), 1f);
        dl.AddLine(rt, bt, C(p.Lo, 0.55f), 1f);
        dl.AddLine(bt, lt, C(p.Lo, 0.55f), 1f);
        dl.AddLine(lt, t, C(p.Lo, 0.55f), 1f);
        dl.AddLine(lt, lb, C(p.Lo, 0.55f), 1f);
        dl.AddLine(bt, bb, C(p.Lo, 0.55f), 1f);
        dl.AddLine(rt, rb, C(p.Lo, 0.55f), 1f);
        dl.AddLine(lb, bb, C(p.Lo, 0.55f), 1f);
        dl.AddLine(bb, rb, C(p.Lo, 0.55f), 1f);
    }

    private static void Torus(ImDrawListPtr dl, Vector2 c, float r,
        (Vector4 Hi, Vector4 Mid, Vector4 Lo) p)
    {
        EllipseFill(dl, new Vector2(c.X, c.Y + r * 0.72f), r * 0.85f, r * 0.22f, C(Theme.Shadow, 0.28f));
        var center = new Vector2(c.X, c.Y - r * 0.05f);

        EllipseStroke(dl, center, r * 0.88f, r * 0.48f, C(p.Lo), r * 0.46f, 40);
        EllipseStroke(dl, center, r * 0.88f, r * 0.48f, C(p.Mid), r * 0.32f, 40);
        EllipseStroke(dl, center, r * 0.88f, r * 0.48f, C(p.Hi), r * 0.12f, 40,
            MathF.PI * 0.95f, MathF.PI * 1.95f);
    }

    private static void Cylinder(ImDrawListPtr dl, Vector2 c, float r,
        (Vector4 Hi, Vector4 Mid, Vector4 Lo) p)
    {
        EllipseFill(dl, new Vector2(c.X, c.Y + r * 0.78f), r * 0.72f, r * 0.20f, C(Theme.Shadow, 0.28f));

        float top = c.Y - r * 0.62f;
        float bottom = c.Y + r * 0.58f;
        float rx = r * 0.68f;

        dl.AddRectFilled(new Vector2(c.X - rx, top), new Vector2(c.X + rx, bottom), C(p.Mid));
        dl.AddRectFilled(new Vector2(c.X - rx, top), new Vector2(c.X - rx * 0.25f, bottom), C(p.Hi, 0.35f));
        dl.AddRectFilled(new Vector2(c.X + rx * 0.35f, top), new Vector2(c.X + rx, bottom), C(p.Lo, 0.35f));

        EllipseFill(dl, new Vector2(c.X, top), rx, r * 0.20f, C(p.Hi));
        EllipseStroke(dl, new Vector2(c.X, top), rx, r * 0.20f, C(p.Lo), 1.1f, 28);
        EllipseStroke(dl, new Vector2(c.X, bottom), rx, r * 0.20f, C(p.Lo), 1.1f, 28, 0f, MathF.PI);
        dl.AddLine(new Vector2(c.X - rx, top), new Vector2(c.X - rx, bottom), C(p.Lo, 0.8f), 1.1f);
        dl.AddLine(new Vector2(c.X + rx, top), new Vector2(c.X + rx, bottom), C(p.Lo, 0.8f), 1.1f);
    }

    private static void Terrain(ImDrawListPtr dl, Vector2 c, float r)
    {
        // водная гладь
        EllipseFill(dl, new Vector2(c.X, c.Y + r * 0.42f), r * 0.98f, r * 0.34f, C(TealMid, 0.85f));
        EllipseStroke(dl, new Vector2(c.X, c.Y + r * 0.42f), r * 0.98f, r * 0.34f, C(TealLo, 0.6f), 1f, 32);
        EllipseStroke(dl, new Vector2(c.X, c.Y + r * 0.46f), r * 0.70f, r * 0.20f, C(WaterHi, 0.65f), 1f, 28);

        float baseY = c.Y + r * 0.36f;
        Vector2[] ridge =
        {
            new(c.X - r * 0.86f, baseY),
            new(c.X - r * 0.58f, c.Y + r * 0.02f),
            new(c.X - r * 0.34f, c.Y + r * 0.18f),
            new(c.X - r * 0.08f, c.Y - r * 0.56f),
            new(c.X + r * 0.14f, c.Y + r * 0.02f),
            new(c.X + r * 0.36f, c.Y - r * 0.28f),
            new(c.X + r * 0.62f, c.Y + r * 0.14f),
            new(c.X + r * 0.86f, baseY),
        };

        var b = new Vector2(c.X, baseY);
        for (int i = 0; i < ridge.Length - 1; i++)
            dl.AddTriangleFilled(b, ridge[i], ridge[i + 1], C(GreenMid));

        // левые склоны светлее
        dl.AddTriangleFilled(b, ridge[0], ridge[3], C(GreenHi, 0.30f));
        dl.AddTriangleFilled(b, ridge[3], ridge[4], C(GreenLo, 0.28f));
        dl.AddTriangleFilled(b, ridge[5], ridge[6], C(GreenLo, 0.25f));

        // снежные шапки
        SnowCap(dl, ridge[3], r * 0.16f, r * 0.07f);
        SnowCap(dl, ridge[5], r * 0.12f, r * 0.055f);

        dl.AddLine(ridge[0], ridge[1], C(GreenLo, 0.5f), 1f);
        dl.AddLine(ridge[1], ridge[2], C(GreenLo, 0.5f), 1f);
        dl.AddLine(ridge[2], ridge[3], C(GreenLo, 0.5f), 1f);
        dl.AddLine(ridge[3], ridge[4], C(GreenLo, 0.5f), 1f);
        dl.AddLine(ridge[4], ridge[5], C(GreenLo, 0.5f), 1f);
        dl.AddLine(ridge[5], ridge[6], C(GreenLo, 0.5f), 1f);
        dl.AddLine(ridge[6], ridge[7], C(GreenLo, 0.5f), 1f);
    }

    private static void SnowCap(ImDrawListPtr dl, Vector2 peak, float w, float h)
    {
        dl.AddTriangleFilled(
            new Vector2(peak.X, peak.Y),
            new Vector2(peak.X - w, peak.Y + h * 1.9f),
            new Vector2(peak.X + w * 0.85f, peak.Y + h * 1.85f),
            C(new Vector4(0.94f, 0.97f, 0.96f, 1f), 0.92f));
    }

    private static void Water(ImDrawListPtr dl, Vector2 c, float r)
    {
        var a = new Vector2(c.X - r * 0.94f, c.Y - r * 0.52f);
        var b = new Vector2(c.X + r * 0.94f, c.Y + r * 0.52f);
        dl.AddRectFilledMultiColor(a, b,
            C(TealHi), C(TealHi), C(TealLo), C(TealLo));
        dl.AddRect(a, b, C(TealLo), 2f, ImDrawFlags.None, 1f);

        for (int i = 0; i < 3; i++)
        {
            float y = c.Y - r * 0.30f + i * r * 0.28f;
            float amp = r * 0.055f;
            float phase = i * 1.7f;
            Vector2 prev = new(a.X + r * 0.10f, y);
            for (int k = 1; k <= 16; k++)
            {
                float t = k / 16f;
                float x = a.X + r * 0.10f + t * (b.X - a.X - r * 0.20f);
                float yy = y + MathF.Sin(t * 9f + phase) * amp;
                var p = new Vector2(x, yy);
                dl.AddLine(prev, p, C(new Vector4(1f, 1f, 1f, 1f), 0.42f), 1.1f);
                prev = p;
            }
        }

        dl.AddCircleFilled(new Vector2(c.X + r * 0.34f, c.Y - r * 0.30f), r * 0.10f,
            C(new Vector4(1f, 0.95f, 0.75f, 1f), 0.85f), 10);
    }

    private static void Plane(ImDrawListPtr dl, Vector2 c, float r)
    {
        var farL = new Vector2(c.X - r * 0.62f, c.Y - r * 0.50f);
        var farR = new Vector2(c.X + r * 0.62f, c.Y - r * 0.50f);
        var nearR = new Vector2(c.X + r * 0.99f, c.Y + r * 0.54f);
        var nearL = new Vector2(c.X - r * 0.99f, c.Y + r * 0.54f);

        Vector2 P(float u, float v)
        {
            var top = Vector2.Lerp(farL, farR, u);
            var bottom = Vector2.Lerp(nearL, nearR, u);
            return Vector2.Lerp(top, bottom, v);
        }

        const int n = 4;
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                bool light = ((i + j) & 1) == 0;
                Vector4 col = light
                    ? new Vector4(0.91f, 0.92f, 0.87f, 1f)
                    : new Vector4(0.10f, 0.14f, 0.17f, 1f);

                dl.AddQuadFilled(
                    P(i / (float)n, j / (float)n), P((i + 1) / (float)n, j / (float)n),
                    P((i + 1) / (float)n, (j + 1) / (float)n), P(i / (float)n, (j + 1) / (float)n),
                    C(col));
            }
        }

        dl.AddLine(farL, farR, C(TealHi, 0.55f), 1.2f);
        dl.AddLine(farR, nearR, C(TealLo, 0.75f), 1.2f);
        dl.AddLine(nearR, nearL, C(TealLo, 0.75f), 1.2f);
        dl.AddLine(nearL, farL, C(TealLo, 0.75f), 1.2f);
    }

    // --------------------------------------------------------------- утилиты

    public static void EllipseFill(ImDrawListPtr dl, Vector2 c, float rx, float ry, uint color)
    {
        if (rx <= 0f || ry <= 0f) return;
        int steps = Math.Clamp((int)(ry * 1.5f), 8, 42);
        float thick = MathF.Max(ry * 2f / steps, 1f);
        for (int i = 0; i < steps; i++)
        {
            float t = -1f + 2f * (i + 0.5f) / steps;
            float w = rx * MathF.Sqrt(MathF.Max(0f, 1f - t * t));
            float y = c.Y + t * ry;
            dl.AddLine(new Vector2(c.X - w, y), new Vector2(c.X + w, y), color, thick);
        }
    }

    public static void EllipseStroke(ImDrawListPtr dl, Vector2 c, float rx, float ry, uint color,
        float thickness, int segments, float start = 0f, float end = MathF.PI * 2f)
    {
        if (segments < 3) segments = 3;
        Vector2 prev = new(c.X + rx * MathF.Cos(start), c.Y + ry * MathF.Sin(start));
        for (int i = 1; i <= segments; i++)
        {
            float t = start + (end - start) * i / segments;
            var p = new Vector2(c.X + rx * MathF.Cos(t), c.Y + ry * MathF.Sin(t));
            dl.AddLine(prev, p, color, thickness);
            prev = p;
        }
    }

    // ---------------------------------------------------------------- пиктограммы

    public static void Tool(ImDrawListPtr dl, ToolGlyph glyph, Vector2 a, Vector2 b, Vector4 color)
    {
        float w = b.X - a.X, h = b.Y - a.Y;
        float s = MathF.Min(w, h);
        var c = new Vector2(a.X + w * 0.5f, a.Y + h * 0.5f);
        uint col = Theme.U32(color);
        float t = MathF.Max(1.2f, s * 0.055f);

        switch (glyph)
        {
            case ToolGlyph.Select:
            {
                var p0 = new Vector2(c.X - s * 0.28f, c.Y - s * 0.36f);
                var p1 = new Vector2(c.X - s * 0.28f, c.Y + s * 0.34f);
                var p2 = new Vector2(c.X - s * 0.04f, c.Y + s * 0.12f);
                var p3 = new Vector2(c.X + s * 0.10f, c.Y + s * 0.38f);
                var p4 = new Vector2(c.X + s * 0.24f, c.Y + s * 0.32f);
                var p5 = new Vector2(c.X + s * 0.10f, c.Y + s * 0.06f);
                var p6 = new Vector2(c.X + s * 0.36f, c.Y + s * 0.02f);
                dl.AddTriangleFilled(p0, p1, p2, col);
                dl.AddQuadFilled(p2, p3, p4, p5, col);
                dl.AddTriangleFilled(p2, p5, p6, col);
                break;
            }
            case ToolGlyph.Move:
            {
                float r = s * 0.32f;
                dl.AddLine(new Vector2(c.X - r, c.Y), new Vector2(c.X + r, c.Y), col, t);
                dl.AddLine(new Vector2(c.X, c.Y - r), new Vector2(c.X, c.Y + r), col, t);
                ArrowHead(dl, c + new Vector2(r, 0f), new Vector2(1f, 0f), s, col);
                ArrowHead(dl, c + new Vector2(-r, 0f), new Vector2(-1f, 0f), s, col);
                ArrowHead(dl, c + new Vector2(0f, r), new Vector2(0f, 1f), s, col);
                ArrowHead(dl, c + new Vector2(0f, -r), new Vector2(0f, -1f), s, col);
                break;
            }
            case ToolGlyph.Orbit:
            {
                EllipseStroke(dl, c, s * 0.34f, s * 0.20f, col, t, 28);
                dl.AddCircleFilled(new Vector2(c.X + s * 0.30f, c.Y - s * 0.10f), s * 0.07f, col, 10);
                EllipseStroke(dl, c, s * 0.36f, s * 0.30f, col, t, 22, -0.4f, 2.2f);
                break;
            }
            case ToolGlyph.Zoom:
            {
                dl.AddCircle(c + new Vector2(-s * 0.05f, -s * 0.06f), s * 0.24f, col, 24, t);
                dl.AddLine(c + new Vector2(s * 0.13f, s * 0.13f), c + new Vector2(s * 0.35f, s * 0.35f), col, t + 0.6f);
                break;
            }
            case ToolGlyph.Frame:
            {
                float r = s * 0.32f, l = s * 0.16f;
                Corner(dl, new Vector2(c.X - r, c.Y - r), 1f, 1f, l, col, t);
                Corner(dl, new Vector2(c.X + r, c.Y - r), -1f, 1f, l, col, t);
                Corner(dl, new Vector2(c.X - r, c.Y + r), 1f, -1f, l, col, t);
                Corner(dl, new Vector2(c.X + r, c.Y + r), -1f, -1f, l, col, t);
                break;
            }
            case ToolGlyph.Home:
            {
                float r = s * 0.30f;
                dl.AddLine(new Vector2(c.X - r, c.Y), new Vector2(c.X, c.Y - r), col, t);
                dl.AddLine(new Vector2(c.X, c.Y - r), new Vector2(c.X + r, c.Y), col, t);
                dl.AddRect(new Vector2(c.X - r * 0.62f, c.Y), new Vector2(c.X + r * 0.62f, c.Y + r), col, 0f, ImDrawFlags.None, t);
                break;
            }
            case ToolGlyph.Grid:
            {
                float r = s * 0.32f;
                for (int i = -1; i <= 1; i++)
                {
                    float o = i * r * 0.66f;
                    dl.AddLine(new Vector2(c.X - r, c.Y + o), new Vector2(c.X + r, c.Y + o), col, t * 0.8f);
                    dl.AddLine(new Vector2(c.X + o, c.Y - r), new Vector2(c.X + o, c.Y + r), col, t * 0.8f);
                }
                dl.AddRect(new Vector2(c.X - r, c.Y - r), new Vector2(c.X + r, c.Y + r), col, 0f, ImDrawFlags.None, t);
                break;
            }
            case ToolGlyph.Render:
            {
                float r = s * 0.30f;
                dl.AddTriangleFilled(
                    new Vector2(c.X - r * 0.7f, c.Y - r),
                    new Vector2(c.X - r * 0.7f, c.Y + r),
                    new Vector2(c.X + r, c.Y), col);
                break;
            }
            case ToolGlyph.Save:
            {
                float r = s * 0.30f;
                dl.AddRect(new Vector2(c.X - r, c.Y - r), new Vector2(c.X + r, c.Y + r), col, 1f, ImDrawFlags.None, t);
                dl.AddRectFilled(new Vector2(c.X - r * 0.45f, c.Y - r), new Vector2(c.X + r * 0.15f, c.Y - r * 0.35f), col);
                dl.AddRect(new Vector2(c.X - r * 0.5f, c.Y + r * 0.1f), new Vector2(c.X + r * 0.5f, c.Y + r), col, 0f, ImDrawFlags.None, t * 0.8f);
                break;
            }
            case ToolGlyph.Delete:
            {
                float r = s * 0.26f;
                dl.AddRect(new Vector2(c.X - r, c.Y - r * 0.55f), new Vector2(c.X + r, c.Y + r), col, 1f, ImDrawFlags.None, t);
                dl.AddLine(new Vector2(c.X - r * 1.2f, c.Y - r * 0.75f), new Vector2(c.X + r * 1.2f, c.Y - r * 0.75f), col, t);
                dl.AddLine(new Vector2(c.X - r * 0.4f, c.Y - r * 0.95f), new Vector2(c.X + r * 0.4f, c.Y - r * 0.95f), col, t);
                break;
            }
            case ToolGlyph.Plus:
            {
                float r = s * 0.28f;
                dl.AddLine(new Vector2(c.X - r, c.Y), new Vector2(c.X + r, c.Y), col, t);
                dl.AddLine(new Vector2(c.X, c.Y - r), new Vector2(c.X, c.Y + r), col, t);
                break;
            }
            case ToolGlyph.Eye:
            {
                EllipseStroke(dl, c, s * 0.34f, s * 0.20f, col, t, 24, 0f, MathF.PI * 2f);
                dl.AddCircleFilled(c, s * 0.09f, col, 12);
                break;
            }
            case ToolGlyph.Camera:
            {
                float r = s * 0.30f;
                dl.AddRect(new Vector2(c.X - r, c.Y - r * 0.6f), new Vector2(c.X + r * 0.6f, c.Y + r * 0.7f), col, 1f, ImDrawFlags.None, t);
                dl.AddTriangleFilled(new Vector2(c.X + r * 0.6f, c.Y - r * 0.1f), new Vector2(c.X + r, c.Y - r * 0.45f), new Vector2(c.X + r, c.Y + r * 0.5f), col);
                dl.AddCircle(c + new Vector2(-r * 0.2f, c.Y + r * 0.05f), r * 0.22f, col, 16, t * 0.8f);
                break;
            }
            case ToolGlyph.Duplicate:
            {
                float r = s * 0.26f;
                dl.AddRect(new Vector2(c.X - r, c.Y - r), new Vector2(c.X + r * 0.6f, c.Y + r * 0.6f), col, 1f, ImDrawFlags.None, t);
                dl.AddRect(new Vector2(c.X - r * 0.3f, c.Y - r * 0.3f), new Vector2(c.X + r, c.Y + r), col, 1f, ImDrawFlags.None, t);
                break;
            }
            case ToolGlyph.Terrain:
                Draw(dl, EntityKind.Terrain, c, s * 0.9f);
                break;
        }
    }

    private static void ArrowHead(ImDrawListPtr dl, Vector2 tip, Vector2 dir, float s, uint col)
    {
        var perp = new Vector2(-dir.Y, dir.X);
        dl.AddTriangleFilled(tip, tip - dir * s * 0.14f + perp * s * 0.09f,
            tip - dir * s * 0.14f - perp * s * 0.09f, col);
    }

    private static void Corner(ImDrawListPtr dl, Vector2 p, float dx, float dy, float len, uint col, float t)
    {
        dl.AddLine(p, p + new Vector2(dx * len, 0f), col, t);
        dl.AddLine(p, p + new Vector2(0f, dy * len), col, t);
    }
}
