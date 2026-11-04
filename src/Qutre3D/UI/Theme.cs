using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Qutre3D.UI;

/// <summary>
/// Палитра и типографика редактора.
/// Светлый «пластик» в духе Bryce, но с бирюзовым акцентом Qutre.
/// </summary>
public static class Theme
{
    public static ImFontPtr FontBody;
    public static ImFontPtr FontSmall;
    public static ImFontPtr FontHeader;
    public static ImFontPtr FontMono;
    public static ImFontPtr FontBrand;
    public static ImFontPtr FontTiny;

    public const float BodySize = 14.0f;
    public const float SmallSize = 11.5f;
    public const float HeaderSize = 12.0f;
    public const float MonoSize = 12.0f;
    public const float BrandSize = 25.0f;
    public const float TinySize = 10.0f;

    // ---------------------------------------------------------------- акценты

    /// <summary>Глубокий бирюзовый Qutre — замена королевскому синему Bryce.</summary>
    public static readonly Vector4 Accent = new(0.055f, 0.435f, 0.435f, 1f);
    public static readonly Vector4 AccentBright = new(0.13f, 0.60f, 0.58f, 1f);
    public static readonly Vector4 AccentDim = new(0.36f, 0.56f, 0.55f, 1f);
    public static readonly Vector4 Warm = new(0.84f, 0.60f, 0.16f, 1f);
    public static readonly Vector4 WarmDim = new(0.66f, 0.48f, 0.16f, 1f);
    public static readonly Vector4 Danger = new(0.62f, 0.20f, 0.16f, 1f);

    // ------------------------------------------------------------------- текст

    public static readonly Vector4 Text = new(0.10f, 0.11f, 0.115f, 1f);
    public static readonly Vector4 TextDim = new(0.27f, 0.29f, 0.30f, 1f);
    public static readonly Vector4 TextFaint = new(0.47f, 0.49f, 0.50f, 1f);
    public static readonly Vector4 TextOnAccent = new(0.97f, 0.99f, 0.98f, 1f);

    // -------------------------------------------------------------- поверхность

    public static readonly Vector4 Panel = new(0.792f, 0.792f, 0.776f, 1f);
    public static readonly Vector4 PanelLight = new(0.878f, 0.878f, 0.862f, 1f);
    public static readonly Vector4 PanelDark = new(0.678f, 0.678f, 0.662f, 1f);
    public static readonly Vector4 Border = new(0.44f, 0.44f, 0.42f, 1f);
    public static readonly Vector4 BorderSoft = new(0.60f, 0.60f, 0.58f, 1f);

    public static readonly Vector4 Field = new(0.935f, 0.935f, 0.918f, 1f);
    public static readonly Vector4 FieldHover = new(0.968f, 0.968f, 0.952f, 1f);
    public static readonly Vector4 FieldActive = new(1f, 1f, 0.988f, 1f);

    public static readonly Vector4 Canvas = new(0.145f, 0.155f, 0.155f, 1f);

    // ------------------------------------------------------------------- фаски

    public static readonly Vector4 Highlight = new(1f, 1f, 1f, 0.92f);
    public static readonly Vector4 Shadow = new(0f, 0f, 0f, 0.30f);
    public static readonly Vector4 ShadowSoft = new(0f, 0f, 0f, 0.13f);

    public static uint U32(Vector4 c) => ImGui.ColorConvertFloat4ToU32(c);

    /// <summary>Тот же цвет с другой альфой.</summary>
    public static Vector4 A(Vector4 c, float alpha) => new(c.X, c.Y, c.Z, alpha);

    public static unsafe void LoadFonts(ImGuiIOPtr io)
    {
        string body = FindFont("DejaVuSans.ttf",
            "segoeui.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/TTF/DejaVuSans.ttf",
            "/usr/share/fonts/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
            "/usr/share/fonts/noto/NotoSans-Regular.ttf");

        string bold = FindFont("DejaVuSans-Bold.ttf",
            "segoeuib.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
            "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf",
            "/usr/share/fonts/dejavu/DejaVuSans-Bold.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
            "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
            "/usr/share/fonts/noto/NotoSans-Bold.ttf");

        string mono = FindFont("DejaVuSansMono.ttf",
            "consola.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
            "/usr/share/fonts/TTF/DejaVuSansMono.ttf",
            "/usr/share/fonts/dejavu/DejaVuSansMono.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf");

        IntPtr ranges = BuildGlyphRanges();

        FontBody = TryFont(io, body, BodySize, ranges);
        FontSmall = TryFont(io, body, SmallSize, ranges);
        FontHeader = TryFont(io, bold, HeaderSize, ranges);
        FontMono = TryFont(io, mono, MonoSize, ranges);
        FontBrand = TryFont(io, bold, BrandSize, ranges);
        FontTiny = TryFont(io, body, TinySize, ranges);

        if (FontBody.NativePtr == null)
            FontBody = io.Fonts.AddFontDefault();
        if (FontSmall.NativePtr == null) FontSmall = FontBody;
        if (FontHeader.NativePtr == null) FontHeader = FontBody;
        if (FontMono.NativePtr == null) FontMono = FontBody;
        if (FontBrand.NativePtr == null) FontBrand = FontHeader;
        if (FontTiny.NativePtr == null) FontTiny = FontSmall;
    }

    /// <summary>
    /// Ищем шрифт сначала среди встроенных (assets/fonts), затем в системе.
    /// Такой порядок даёт одинаковый вид на всех платформах и гарантирует кириллицу.
    /// </summary>
    private static string FindFont(string bundledName, params string[] systemCandidates)
    {
        var candidates = new List<string>();

        foreach (var root in AssetRoots())
            candidates.Add(Path.Combine(root, "fonts", bundledName));

        bool windows = OperatingSystem.IsWindows();
        string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

        for (int i = 0; i < systemCandidates.Length; i++)
        {
            string candidate = systemCandidates[i];
            if (Path.IsPathRooted(candidate))
                candidates.Add(candidate);
            else if (!string.IsNullOrEmpty(fontsDir))
                candidates.Add(Path.Combine(fontsDir, candidate));

            // На Windows предпочитаем системный Segoe UI, на Linux — DejaVu/Noto.
            if (windows && i == 0)
            {
                var preferred = Path.Combine(fontsDir, candidate);
                if (File.Exists(preferred))
                    return preferred;
            }
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Кириллица + Latin + типографика (тире, кавычки, многоточие) + стрелки и
    /// символы. Так интерфейс не показывает «?» вместо «—».
    /// </summary>
    private static unsafe IntPtr BuildGlyphRanges()
    {
        ushort[] ranges =
        {
            0x0020, 0x00FF, // Basic Latin + Latin-1 Supplement
            0x0400, 0x052F, // Cyrillic + Cyrillic Supplement
            0x2000, 0x206F, // General Punctuation
            0x2190, 0x21FF, // Arrows
            0x25A0, 0x25FF, // Geometric Shapes
            0x2600, 0x26FF, // Miscellaneous Symbols
            0x0000,
        };

        IntPtr ptr = Marshal.AllocHGlobal(ranges.Length * sizeof(ushort));
        fixed (ushort* src = ranges)
            Buffer.MemoryCopy(src, (void*)ptr, ranges.Length * sizeof(ushort), ranges.Length * sizeof(ushort));
        return ptr;
    }

    private static IEnumerable<string> AssetRoots()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "assets");

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
            yield return Path.Combine(dir.FullName, "assets");
    }

    private static ImFontPtr TryFont(ImGuiIOPtr io, string path, float size, IntPtr ranges)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return default;
        try
        {
            return io.Fonts.AddFontFromFileTTF(path, size, null, ranges);
        }
        catch
        {
            return default;
        }
    }

    public static void ApplyStyle()
    {
        var style = ImGui.GetStyle();

        style.WindowRounding = 0f;
        style.ChildRounding = 2f;
        style.FrameRounding = 2f;
        style.PopupRounding = 3f;
        style.ScrollbarRounding = 6f;
        style.GrabRounding = 2f;
        style.TabRounding = 2f;

        style.WindowBorderSize = 1f;
        style.ChildBorderSize = 1f;
        style.PopupBorderSize = 1f;
        style.FrameBorderSize = 1f;

        style.WindowPadding = new Vector2(10f, 9f);
        style.FramePadding = new Vector2(7f, 4f);
        style.ItemSpacing = new Vector2(7f, 6f);
        style.ItemInnerSpacing = new Vector2(5f, 4f);
        style.IndentSpacing = 12f;
        style.ScrollbarSize = 13f;
        style.GrabMinSize = 10f;

        style.Colors[(int)ImGuiCol.Text] = Text;
        style.Colors[(int)ImGuiCol.TextDisabled] = TextFaint;
        style.Colors[(int)ImGuiCol.WindowBg] = Panel;
        style.Colors[(int)ImGuiCol.ChildBg] = Field;
        style.Colors[(int)ImGuiCol.PopupBg] = PanelLight;
        style.Colors[(int)ImGuiCol.Border] = BorderSoft;
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f);
        style.Colors[(int)ImGuiCol.FrameBg] = Field;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = FieldHover;
        style.Colors[(int)ImGuiCol.FrameBgActive] = FieldActive;
        style.Colors[(int)ImGuiCol.TitleBg] = PanelDark;
        style.Colors[(int)ImGuiCol.TitleBgActive] = PanelDark;
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = PanelDark;
        style.Colors[(int)ImGuiCol.MenuBarBg] = PanelLight;
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.70f, 0.70f, 0.685f, 1f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = BorderSoft;
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = TextFaint;
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = AccentDim;
        style.Colors[(int)ImGuiCol.CheckMark] = Accent;
        style.Colors[(int)ImGuiCol.SliderGrab] = AccentDim;
        style.Colors[(int)ImGuiCol.SliderGrabActive] = Accent;
        style.Colors[(int)ImGuiCol.Button] = PanelLight;
        style.Colors[(int)ImGuiCol.ButtonHovered] = FieldHover;
        style.Colors[(int)ImGuiCol.ButtonActive] = AccentDim;
        style.Colors[(int)ImGuiCol.Header] = A(AccentBright, 0.22f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = A(AccentBright, 0.34f);
        style.Colors[(int)ImGuiCol.HeaderActive] = A(Accent, 0.42f);
        style.Colors[(int)ImGuiCol.Separator] = BorderSoft;
        style.Colors[(int)ImGuiCol.SeparatorHovered] = AccentDim;
        style.Colors[(int)ImGuiCol.SeparatorActive] = Accent;
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.55f, 0.55f, 0.53f, 0.5f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = AccentDim;
        style.Colors[(int)ImGuiCol.ResizeGripActive] = Accent;
        style.Colors[(int)ImGuiCol.Tab] = PanelDark;
        style.Colors[(int)ImGuiCol.TabHovered] = PanelLight;
        style.Colors[(int)ImGuiCol.PlotLines] = Accent;
        style.Colors[(int)ImGuiCol.PlotHistogram] = Warm;
        style.Colors[(int)ImGuiCol.TextSelectedBg] = A(AccentBright, 0.35f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = Warm;
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = Accent;
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0f, 0f, 0f, 0.35f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.05f, 0.06f, 0.06f, 0.62f);
    }
}
