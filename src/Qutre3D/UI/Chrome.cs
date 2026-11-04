using System.Numerics;
using ImGuiNET;

namespace Qutre3D.UI;

/// <summary>
/// Рисованные виджеты в стиле Bryce: светлый пластик, фаски, гравировка.
/// Всё, что не помещается в стандартный ImGui, живёт здесь.
/// </summary>
public static class Chrome
{
    public static readonly Vector2 Pad = new(10f, 8f);

    // ------------------------------------------------------------- измерение

    public static Vector2 Measure(ImFontPtr font, string text)
    {
        ImGui.PushFont(font);
        var size = ImGui.CalcTextSize(text);
        ImGui.PopFont();
        return size;
    }

    public static void Centered(ImDrawListPtr dl, ImFontPtr font, float size, Vector2 a, Vector2 b,
        string text, Vector4 color)
    {
        if (string.IsNullOrEmpty(text)) return;
        var ts = Measure(font, text);
        var p = new Vector2(
            MathF.Round(a.X + (b.X - a.X - ts.X) * 0.5f),
            MathF.Round(a.Y + (b.Y - a.Y - ts.Y) * 0.5f));
        dl.AddText(font, size, p, Theme.U32(color), text);
    }

    public static void CenteredLeft(ImDrawListPtr dl, ImFontPtr font, float size, Vector2 a, Vector2 b,
        string text, Vector4 color, float inset)
    {
        if (string.IsNullOrEmpty(text)) return;
        var ts = Measure(font, text);
        var p = new Vector2(MathF.Round(a.X + inset), MathF.Round(a.Y + (b.Y - a.Y - ts.Y) * 0.5f));
        dl.AddText(font, size, p, Theme.U32(color), text);
    }

    /// <summary>Гравированная подпись: тёмная тень сверху, светлая подсветка снизу.</summary>
    public static void Engraved(ImDrawListPtr dl, Vector2 p, string text, Vector4 color)
    {
        if (string.IsNullOrEmpty(text)) return;
        dl.AddText(Theme.FontSmall, Theme.SmallSize, p + new Vector2(0f, 1f),
            Theme.U32(new Vector4(1f, 1f, 1f, 0.55f)), text);
        dl.AddText(Theme.FontSmall, Theme.SmallSize, p, Theme.U32(color), text);
    }

    // ------------------------------------------------------------------ фаски

    public static void Bevel(ImDrawListPtr dl, Vector2 a, Vector2 b, bool raised, float rounding = 2f,
        float inset = 0f)
    {
        var hi = Theme.U32(raised ? Theme.Highlight : Theme.Shadow);
        var lo = Theme.U32(raised ? Theme.Shadow : Theme.Highlight);
        float x0 = a.X + inset, y0 = a.Y + inset, x1 = b.X - inset, y1 = b.Y - inset;

        dl.AddLine(new Vector2(x0 + rounding, y0), new Vector2(x1 - rounding, y0), hi, 1f);
        dl.AddLine(new Vector2(x0, y0 + rounding), new Vector2(x0, y1 - rounding), hi, 1f);
        dl.AddLine(new Vector2(x0 + rounding, y1), new Vector2(x1 - rounding, y1), lo, 1f);
        dl.AddLine(new Vector2(x1, y0 + rounding), new Vector2(x1, y1 - rounding), lo, 1f);
    }

    /// <summary>Фон окна: вертикальный градиент светлого металла + едва заметные штрихи.</summary>
    public static void PanelChrome()
    {
        var dl = ImGui.GetWindowDrawList();
        var a = ImGui.GetWindowPos();
        var b = a + ImGui.GetWindowSize();

        dl.AddRectFilledMultiColor(a, b,
            Theme.U32(new Vector4(0.905f, 0.905f, 0.890f, 1f)),
            Theme.U32(new Vector4(0.905f, 0.905f, 0.890f, 1f)),
            Theme.U32(new Vector4(0.745f, 0.745f, 0.730f, 1f)),
            Theme.U32(new Vector4(0.745f, 0.745f, 0.730f, 1f)));

        for (float y = a.Y + 3f; y < b.Y - 1f; y += 4f)
            dl.AddLine(new Vector2(a.X + 1f, y), new Vector2(b.X - 1f, y),
                Theme.U32(new Vector4(1f, 1f, 1f, 0.030f)), 1f);

        dl.AddLine(new Vector2(a.X + 0.5f, a.Y + 0.5f), new Vector2(b.X - 0.5f, a.Y + 0.5f),
            Theme.U32(Theme.Highlight), 1f);
        dl.AddLine(new Vector2(a.X + 0.5f, a.Y + 0.5f), new Vector2(a.X + 0.5f, b.Y - 0.5f),
            Theme.U32(Theme.Highlight), 1f);
        dl.AddLine(new Vector2(a.X + 0.5f, b.Y - 0.5f), new Vector2(b.X - 0.5f, b.Y - 0.5f),
            Theme.U32(Theme.Shadow), 1f);
        dl.AddLine(new Vector2(b.X - 0.5f, a.Y + 0.5f), new Vector2(b.X - 0.5f, b.Y - 0.5f),
            Theme.U32(Theme.Shadow), 1f);
    }

    /// <summary>Вдавленная область (список, поле, вьюпорт).</summary>
    public static void Inset(ImDrawListPtr dl, Vector2 a, Vector2 b, float rounding = 2f, Vector4? fill = null)
    {
        var color = fill ?? Theme.Field;
        dl.AddRectFilled(a, b, Theme.U32(color), rounding);
        dl.AddRect(a, b, Theme.U32(Theme.Border), rounding, ImDrawFlags.None, 1f);
        Bevel(dl, a, b, false, rounding, 1f);
    }

    /// <summary>Выпуклая карточка-панель (как палитры Bryce).</summary>
    public static void Card(ImDrawListPtr dl, Vector2 a, Vector2 b, float rounding = 2f, bool raised = true)
    {
        dl.AddRectFilledMultiColor(a, b,
            Theme.U32(Theme.PanelLight), Theme.U32(Theme.PanelLight),
            Theme.U32(Theme.Panel), Theme.U32(Theme.Panel));
        dl.AddRect(a, b, Theme.U32(Theme.Border), rounding, ImDrawFlags.None, 1f);
        Bevel(dl, a, b, raised, rounding, 1f);
    }

    // ----------------------------------------------------------------- кнопки

    private static void ButtonFace(ImDrawListPtr dl, Vector2 a, Vector2 b, bool hovered, bool pressed,
        bool active, bool enabled, float rounding)
    {
        Vector4 top, bottom;

        if (!enabled)
        {
            top = new Vector4(0.815f, 0.815f, 0.800f, 1f);
            bottom = new Vector4(0.755f, 0.755f, 0.740f, 1f);
        }
        else if (active)
        {
            top = Theme.AccentBright;
            bottom = Theme.Accent;
        }
        else if (pressed)
        {
            top = new Vector4(0.715f, 0.715f, 0.700f, 1f);
            bottom = new Vector4(0.790f, 0.790f, 0.775f, 1f);
        }
        else if (hovered)
        {
            top = new Vector4(0.985f, 0.985f, 0.970f, 1f);
            bottom = new Vector4(0.860f, 0.860f, 0.845f, 1f);
        }
        else
        {
            top = new Vector4(0.930f, 0.930f, 0.915f, 1f);
            bottom = new Vector4(0.800f, 0.800f, 0.785f, 1f);
        }

        dl.AddRectFilledMultiColor(a, b, Theme.U32(top), Theme.U32(top), Theme.U32(bottom), Theme.U32(bottom));
        dl.AddRect(a, b, Theme.U32(enabled ? Theme.Border : Theme.BorderSoft), rounding, ImDrawFlags.None, 1f);
        Bevel(dl, a, b, !pressed, rounding, 1f);
    }

    public static bool Button(string id, string label, Vector2 size, bool active = false, bool enabled = true,
        bool bold = false)
    {
        size = SafeSize(size);
        if (!enabled) ImGui.BeginDisabled();
        bool clicked = ImGui.InvisibleButton(id, size);
        bool hovered = enabled && ImGui.IsItemHovered();
        bool pressed = enabled && ImGui.IsItemActive();
        if (!enabled) ImGui.EndDisabled();

        var dl = ImGui.GetWindowDrawList();
        var a = ImGui.GetItemRectMin();
        var b = ImGui.GetItemRectMax();
        ButtonFace(dl, a, b, hovered, pressed, active, enabled, 2f);

        var color = !enabled ? Theme.TextFaint : active ? Theme.TextOnAccent : Theme.Text;
        var font = bold ? Theme.FontHeader : Theme.FontBody;
        float fs = bold ? Theme.HeaderSize : Theme.BodySize;
        var offset = pushedTextOffset(pressed);
        Centered(dl, font, fs, a + offset, b + offset, label, color);
        return clicked;
    }

    private static Vector2 pushedTextOffset(bool pressed) => pressed ? new Vector2(1f, 1f) : Vector2.Zero;

    /// <summary>InvisibleButton не принимает нулевой размер — подстраховываемся.</summary>
    private static Vector2 SafeSize(Vector2 size) => new(
        size.X == 0f ? 1f : size.X,
        size.Y == 0f ? ImGui.GetFrameHeight() : size.Y);

    /// <summary>Кнопка с произвольной иконкой: колбэк рисует внутри прямоугольника.</summary>
    public static bool IconButton(string id, Vector2 size, Action<ImDrawListPtr, Vector2, Vector2> icon,
        bool active = false, string tooltip = null, bool enabled = true, bool small = false)
    {
        size = SafeSize(size);
        if (!enabled) ImGui.BeginDisabled();
        bool clicked = ImGui.InvisibleButton(id, size);
        bool hovered = enabled && ImGui.IsItemHovered();
        bool pressed = enabled && ImGui.IsItemActive();
        if (!enabled) ImGui.EndDisabled();

        var dl = ImGui.GetWindowDrawList();
        var a = ImGui.GetItemRectMin();
        var b = ImGui.GetItemRectMax();
        ButtonFace(dl, a, b, hovered, pressed, active, enabled, 2f);

        if (icon is not null)
        {
            var offset = pushedTextOffset(pressed);
            dl.PushClipRect(a + new Vector2(1f, 1f), b - new Vector2(1f, 1f), true);
            icon(dl, a + offset, b + offset);
            dl.PopClipRect();
        }

        if (enabled && hovered && !string.IsNullOrEmpty(tooltip))
            ImGui.SetTooltip(tooltip);

        return clicked;
    }

    /// <summary>Маленькая квадратная кнопка-иконка без подписи (транспорт, утилиты).</summary>
    public static bool Mini(string id, string label, Vector2 size, bool active = false, string tooltip = null,
        bool enabled = true)
    {
        return IconButton(id, size, (dl, a, b) =>
        {
            var color = active ? Theme.TextOnAccent : Theme.TextDim;
            Centered(dl, Theme.FontHeader, Theme.HeaderSize, a, b, label, color);
        }, active, tooltip, enabled, true);
    }

    /// <summary>Круглая «таблетка» — транспортная панель в духе Bryce.</summary>
    public static bool RoundButton(string id, Vector2 size, Action<ImDrawListPtr, Vector2, Vector2> icon,
        string tooltip = null, bool active = false, bool enabled = true)
    {
        size = SafeSize(size);
        if (!enabled) ImGui.BeginDisabled();
        bool clicked = ImGui.InvisibleButton(id, size);
        bool hovered = enabled && ImGui.IsItemHovered();
        bool pressed = enabled && ImGui.IsItemActive();
        if (!enabled) ImGui.EndDisabled();

        var dl = ImGui.GetWindowDrawList();
        var a = ImGui.GetItemRectMin();
        var b = ImGui.GetItemRectMax();
        float r = size.Y * 0.5f;

        Vector4 fill = !enabled
            ? new Vector4(0.800f, 0.800f, 0.785f, 1f)
            : active
                ? Theme.Accent
                : pressed
                    ? new Vector4(0.705f, 0.705f, 0.690f, 1f)
                    : hovered
                        ? new Vector4(0.965f, 0.965f, 0.950f, 1f)
                        : new Vector4(0.885f, 0.885f, 0.870f, 1f);

        dl.AddRectFilled(a + new Vector2(0f, 1.5f), b + new Vector2(0f, 1.5f),
            Theme.U32(new Vector4(0f, 0f, 0f, 0.12f)), r);
        dl.AddRectFilled(a, b, Theme.U32(fill), r);

        uint hi = Theme.U32(active
            ? new Vector4(0.45f, 0.86f, 0.84f, 0.60f)
            : new Vector4(1f, 1f, 1f, 0.85f));
        uint lo = Theme.U32(new Vector4(0f, 0f, 0f, 0.20f));
        dl.AddLine(new Vector2(a.X + r * 0.55f, a.Y + 1.2f), new Vector2(b.X - r * 0.55f, a.Y + 1.2f), hi, 1f);
        dl.AddLine(new Vector2(a.X + r * 0.55f, b.Y - 1.2f), new Vector2(b.X - r * 0.55f, b.Y - 1.2f), lo, 1f);
        dl.AddRect(a, b, Theme.U32(active ? Theme.Accent : Theme.Border), r, ImDrawFlags.None, 1f);

        if (icon is not null)
        {
            var offset = pushedTextOffset(pressed);
            dl.PushClipRect(a + new Vector2(2f, 2f), b - new Vector2(2f, 2f), true);
            icon(dl, a + offset, b + offset);
            dl.PopClipRect();
        }

        if (enabled && hovered && !string.IsNullOrEmpty(tooltip))
            ImGui.SetTooltip(tooltip);

        return clicked;
    }

    // ------------------------------------------------------------------ вкладки

    public static bool Tab(string id, string label, bool active, float height = 27f, bool onDark = false)
    {
        float width = Measure(Theme.FontHeader, label).X + 46f;
        var size = new Vector2(width, height);

        bool clicked = ImGui.InvisibleButton(id, size);
        bool hovered = ImGui.IsItemHovered();
        var dl = ImGui.GetWindowDrawList();
        var a = ImGui.GetItemRectMin();
        var b = ImGui.GetItemRectMax();

        if (active)
        {
            dl.AddRectFilledMultiColor(a, b,
                Theme.U32(new Vector4(0.94f, 0.94f, 0.925f, 1f)),
                Theme.U32(new Vector4(0.94f, 0.94f, 0.925f, 1f)),
                Theme.U32(new Vector4(0.80f, 0.80f, 0.785f, 1f)),
                Theme.U32(new Vector4(0.80f, 0.80f, 0.785f, 1f)));
            Bevel(dl, a, b, true, 2f, 1f);
            dl.AddRectFilled(new Vector2(a.X + 8f, b.Y - 3f), new Vector2(b.X - 8f, b.Y - 1f),
                Theme.U32(Theme.Accent), 1f);
        }
        else if (hovered)
        {
            dl.AddRectFilled(a, b, Theme.U32(new Vector4(1f, 1f, 1f, onDark ? 0.10f : 0.16f)), 2f);
        }

        var color = active ? Theme.Text : (onDark ? new Vector4(0.80f, 0.82f, 0.81f, 1f) : Theme.TextFaint);
        var ts = Measure(Theme.FontHeader, label);
        var p = new Vector2(a.X + 20f, MathF.Round(a.Y + (height - ts.Y) * 0.5f));
        if (onDark && !active)
            dl.AddText(Theme.FontHeader, Theme.HeaderSize, p + new Vector2(1f, 1f),
                Theme.U32(new Vector4(0f, 0f, 0f, 0.45f)), label);
        dl.AddText(Theme.FontHeader, Theme.HeaderSize, p, Theme.U32(color), label);

        // треугольник-маркер, как у Bryce
        float cx = a.X + 9f, cy = a.Y + height * 0.5f;
        uint tri = Theme.U32(active ? Theme.Accent
            : onDark ? new Vector4(0.70f, 0.72f, 0.71f, 1f) : Theme.TextFaint);
        dl.AddTriangleFilled(new Vector2(cx - 2f, cy - 4f), new Vector2(cx + 3f, cy),
            new Vector2(cx - 2f, cy + 4f), tri);

        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        return clicked;
    }

    // ---------------------------------------------------------------- заголовки

    public static void SectionTitle(string text)
    {
        ImGui.Spacing();
        var dl = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();
        float w = MathF.Max(ImGui.GetContentRegionAvail().X, 1f);

        dl.AddRectFilled(new Vector2(p.X, p.Y + 1f), new Vector2(p.X + w, p.Y + 19f),
            Theme.U32(new Vector4(0f, 0f, 0f, 0.055f)));
        dl.AddLine(new Vector2(p.X, p.Y + 1f), new Vector2(p.X + w, p.Y + 1f),
            Theme.U32(new Vector4(1f, 1f, 1f, 0.40f)), 1f);
        dl.AddLine(new Vector2(p.X, p.Y + 19f), new Vector2(p.X + w, p.Y + 19f),
            Theme.U32(new Vector4(0f, 0f, 0f, 0.10f)), 1f);
        Diamond(dl, new Vector2(p.X + 7f, p.Y + 10f), 3.4f, Theme.U32(Theme.Accent));

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 17f);
        ImGui.PushFont(Theme.FontHeader);
        ImGui.TextColored(Theme.TextDim, text.ToUpperInvariant());
        ImGui.PopFont();
        ImGui.Dummy(new Vector2(0f, 1f));
    }

    public static void Label(string text) => ImGui.TextColored(Theme.TextDim, text);
    public static void Hint(string text) => ImGui.TextColored(Theme.TextFaint, text);

    public static void Divider()
    {
        var dl = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();
        float w = MathF.Max(ImGui.GetContentRegionAvail().X, 1f);
        dl.AddLine(new Vector2(p.X, p.Y + 1f), new Vector2(p.X + w, p.Y + 1f),
            Theme.U32(new Vector4(0f, 0f, 0f, 0.13f)), 1f);
        dl.AddLine(new Vector2(p.X, p.Y + 2f), new Vector2(p.X + w, p.Y + 2f),
            Theme.U32(new Vector4(1f, 1f, 1f, 0.45f)), 1f);
        ImGui.Dummy(new Vector2(0f, 4f));
    }

    public static void Diamond(ImDrawListPtr dl, Vector2 center, float radius, uint color)
    {
        dl.AddQuadFilled(
            new Vector2(center.X, center.Y - radius),
            new Vector2(center.X + radius, center.Y),
            new Vector2(center.X, center.Y + radius),
            new Vector2(center.X - radius, center.Y),
            color);
    }

    /// <summary>Вертикальный «транспортный» жёлоб с ромбиком — декоративная деталь в духе Bryce.</summary>
    public static void Groove(ImDrawListPtr dl, Vector2 a, Vector2 b)
    {
        dl.AddRectFilled(a, b, Theme.U32(new Vector4(0.60f, 0.60f, 0.585f, 1f)), 2f);
        dl.AddRect(a, b, Theme.U32(Theme.Border), 2f, ImDrawFlags.None, 1f);
        Bevel(dl, a, b, false, 2f, 1f);
        float mid = (a.Y + b.Y) * 0.5f;
        Diamond(dl, new Vector2(a.X + 6f, mid), 3f, Theme.U32(Theme.WarmDim));
    }
}
