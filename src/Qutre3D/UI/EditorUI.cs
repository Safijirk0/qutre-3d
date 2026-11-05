using ImGuiNET;
using OpenTK.Mathematics;
using Qutre3D.App;
using Qutre3D.Core;
using Qutre3D.Render;
using ImVec2 = System.Numerics.Vector2;
using ImVec3 = System.Numerics.Vector3;
using ImVec4 = System.Numerics.Vector4;

namespace Qutre3D.UI;

public enum EditorTab
{
    World,
    Object,
    Sky,
}

/// <summary>Весь интерфейс редактора. Панели — в соседнем файле (partial).</summary>
public sealed partial class EditorUI
{
    public const string Version = "0.3.0";

    public const float ToolStripWidth = 38f;
    public const float LeftWidth = 246f;
    public const float LeftTotal = ToolStripWidth + LeftWidth;
    public const float RightWidth = 344f;
    public const float StatusHeight = 34f;
    public const float TabStripHeight = 30f;
    public const float ShelfHeight = 62f;
    public const float TopBarHeight = TabStripHeight + ShelfHeight;

    public int Selected = -1;
    public ImVec2 ViewportMin;
    public ImVec2 ViewportMax;
    public bool ViewportHovered;
    public EditorTab Tab = EditorTab.World;

    /// <summary>Логотип в интерфейсе (загружается в QutreApp.OnLoad, когда есть GL-контекст).</summary>
    public LogoTexture Logo { get; } = new();

    private float _menuHeight = 24f;
    private float _fps;
    private bool _showAbout;
    private bool _showShortcuts;

    private const float IntroDuration = 1.9f;
    private float _introTime;
    private bool _introDone;

    private Scene _sceneRef;
    private string _sceneName = "";
    private string _entityName = "";

    public float ViewportAspect =>
        MathF.Max(ViewportMax.X - ViewportMin.X, 1f) / MathF.Max(ViewportMax.Y - ViewportMin.Y, 1f);

    public void Draw(QutreApp app)
    {
        SyncBuffers(app);

        DrawMenuBar(app);

        float w = app.ClientSize.X;
        float h = app.ClientSize.Y;
        float top = _menuHeight;

        DrawTopBar(app, w, top);

        float contentTop = top + TopBarHeight;
        float bottom = h - StatusHeight;

        ViewportMin = new ImVec2(LeftTotal, contentTop);
        ViewportMax = new ImVec2(MathF.Max(w - RightWidth, LeftTotal + 16f), bottom);

        _fps = MathF.Abs(app.Fps) < 400f ? app.Fps : 60f;

        DrawToolStrip(app, contentTop, bottom);
        DrawLeftPanel(app, contentTop, bottom);
        DrawRightPanel(app, contentTop, bottom);
        DrawStatusBar(app, w, h);
        app.Dialog.Draw();
        DrawModals(app);
        HandleShortcuts(app);
        DrawIntro(app);
    }

    private void SyncBuffers(QutreApp app)
    {
        if (!ReferenceEquals(_sceneRef, app.Scene))
        {
            _sceneRef = app.Scene;
            _sceneName = app.Scene.Name;
            Selected = -1;
        }

        if (Selected >= 0 && Selected < app.Scene.Entities.Count)
        {
            var e = app.Scene.Entities[Selected];
            if (!ImGui.IsAnyItemActive()) _entityName = e.Name;
        }
    }

    // ---------------------------------------------------------------- меню

    private void DrawMenuBar(QutreApp app)
    {
        if (!ImGui.BeginMainMenuBar())
            return;

        _menuHeight = ImGui.GetWindowSize().Y;

        if (ImGui.BeginMenu(Loc.T("Файл", "File")))
        {
            if (ImGui.MenuItem(Loc.T("Новая сцена", "New scene"), "Ctrl+N")) app.NewScene();
            if (ImGui.MenuItem(Loc.T("Открыть…", "Open…"), "Ctrl+O")) app.OpenSceneDialog();
            ImGui.Separator();
            if (ImGui.MenuItem(Loc.T("Сохранить", "Save"), "Ctrl+S")) app.SaveScene();
            if (ImGui.MenuItem(Loc.T("Сохранить как…", "Save as…"), "Ctrl+Shift+S")) app.SaveSceneAs();
            ImGui.Separator();
            if (ImGui.MenuItem(Loc.T("Экспорт PNG 1920×1080", "Export PNG 1920×1080"), "Ctrl+E"))
                app.ExportImage(1920, 1080);
            if (ImGui.MenuItem(Loc.T("Экспорт PNG 3840×2160", "Export PNG 3840×2160")))
                app.ExportImage(3840, 2160);
            ImGui.Separator();
            if (ImGui.MenuItem(Loc.T("Выход", "Exit"), "Alt+F4")) app.Close();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu(Loc.T("Сцена", "Scene")))
        {
            if (ImGui.BeginMenu(Loc.T("Добавить", "Add")))
            {
                foreach (EntityKind kind in Enum.GetValues<EntityKind>())
                {
                    if (ImGui.MenuItem(Entity.DefaultName(kind))) app.AddEntity(kind);
                }
                ImGui.EndMenu();
            }
            if (ImGui.MenuItem(Loc.T("Удалить выделенное", "Delete selected"), "Del",
                    Selected >= 0 && Selected < app.Scene.Entities.Count))
                app.DeleteSelected();
            if (ImGui.MenuItem(Loc.T("Кадрировать выделенное", "Frame selected"), "F", Selected >= 0))
                app.FrameSelected();
            ImGui.Separator();
            if (ImGui.MenuItem(Loc.T("Сбросить камеру", "Reset camera"), "Home")) app.ResetCamera();
            ImGui.Separator();
            if (ImGui.MenuItem(Loc.T("Пересобрать ландшафт", "Rebuild terrain"))) app.RebuildTerrain();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu(Loc.T("Пресеты", "Presets")))
        {
            foreach (var preset in Presets.All)
            {
                bool active = app.Scene.Preset == preset.Id;
                if (ImGui.MenuItem(preset.Title + "  —  " + preset.Hint, null, active))
                    app.ApplyPreset(preset.Id);
            }
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu(Loc.T("Вид", "View")))
        {
            bool showGrid = app.ShowGrid;
            ImGui.MenuItem(Loc.T("Сетка", "Grid"), "G", ref showGrid);
            app.ShowGrid = showGrid;
            ImGui.MenuItem(Loc.T("Пыль в воздухе", "Dust in the air"), null, ref app.Scene.Dust);
            ImGui.Separator();

            if (ImGui.BeginMenu(Loc.T("Язык", "Language")))
            {
                if (ImGui.MenuItem("Русский", null, Loc.Language == AppLanguage.Russian))
                    app.SetLanguage(AppLanguage.Russian);
                if (ImGui.MenuItem("English", null, Loc.Language == AppLanguage.English))
                    app.SetLanguage(AppLanguage.English);
                ImGui.EndMenu();
            }

            if (ImGui.MenuItem(Loc.T("Горячие клавиши", "Keyboard shortcuts"), "F1")) _showShortcuts = true;
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu(Loc.T("Справка", "Help")))
        {
            if (ImGui.MenuItem(Loc.T("О Qutre 3D", "About Qutre 3D"))) _showAbout = true;
            if (ImGui.MenuItem(Loc.T("Горячие клавиши", "Keyboard shortcuts"), "F1")) _showShortcuts = true;
            ImGui.EndMenu();
        }

        var dl = ImGui.GetWindowDrawList();
        string brand = "QUTRE 3D";
        var io = ImGui.GetIO();
        ImGui.PushFont(Theme.FontHeader);
        var bs = ImGui.CalcTextSize(brand);
        ImGui.PopFont();
        dl.AddText(Theme.FontHeader, Theme.HeaderSize,
            new ImVec2(io.DisplaySize.X - bs.X - 14f, (_menuHeight - bs.Y) * 0.5f),
            Theme.U32(Theme.TextFaint), brand);

        ImGui.EndMainMenuBar();
    }

    // ------------------------------------------------------------- верхняя полка

    private void DrawTopBar(QutreApp app, float w, float top)
    {
        ImGui.SetNextWindowPos(new ImVec2(0f, top));
        ImGui.SetNextWindowSize(new ImVec2(w, TopBarHeight));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new ImVec4(0f, 0f, 0f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new ImVec2(0f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.Begin("##topbar", PanelFlags);

        var dl = ImGui.GetWindowDrawList();
        var a = ImGui.GetWindowPos();
        var b = a + ImGui.GetWindowSize();
        float shelfTop = a.Y + TabStripHeight;
        float shelfBottom = b.Y - 3f;

        dl.AddRectFilledMultiColor(a, new ImVec2(b.X, shelfTop),
            Theme.U32(new ImVec4(0.430f, 0.430f, 0.418f, 1f)),
            Theme.U32(new ImVec4(0.430f, 0.430f, 0.418f, 1f)),
            Theme.U32(new ImVec4(0.305f, 0.305f, 0.296f, 1f)),
            Theme.U32(new ImVec4(0.305f, 0.305f, 0.296f, 1f)));
        dl.AddLine(new ImVec2(a.X, shelfTop - 1f), new ImVec2(b.X, shelfTop - 1f),
            Theme.U32(new ImVec4(0f, 0f, 0f, 0.35f)), 1f);

        // --- вкладки, как Create / Edit / Sky & Fog у Bryce
        float tx = 10f;
        float tabY = a.Y + 1f;

        tx = DrawTab(tx, tabY, Loc.T("Мир", "World"), EditorTab.World, Loc.T(
            "Сцена, пресеты и статистика", "Scene, presets and statistics"));
        tx = DrawTab(tx, tabY, Loc.T("Объект", "Object"), EditorTab.Object, Loc.T(
            "Инспектор выделенного объекта", "Inspector for the selected object"));
        tx = DrawTab(tx, tabY, Loc.T("Небо и туман", "Sky & Fog"), EditorTab.Sky, Loc.T(
            "Атмосфера, туман и пост-обработка", "Atmosphere, fog and post-processing"));

        // разделитель вкладок и полки
        dl.AddLine(new ImVec2(tx + 6f, a.Y + 6f), new ImVec2(tx + 6f, shelfTop - 5f),
            Theme.U32(Theme.Shadow), 1f);
        dl.AddLine(new ImVec2(tx + 7f, a.Y + 6f), new ImVec2(tx + 7f, shelfTop - 5f),
            Theme.U32(Theme.Highlight), 1f);

        // --- полка
        var shelfA = new ImVec2(8f, shelfTop + 2f);
        var shelfB = new ImVec2(b.X - 8f, shelfBottom);
        Chrome.Card(dl, shelfA, shelfB, 2f);

        float iconSize = 46f;
        float x = shelfA.X + 8f;
        float iconY = shelfTop + (ShelfHeight - iconSize) * 0.5f - 1f;

        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Terrain, Loc.T("Ландшафт", "Terrain"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Water, Loc.T("Вода", "Water"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Sphere, Loc.T("Сфера", "Sphere"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Box, Loc.T("Куб", "Box"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Torus, Loc.T("Тор", "Torus"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Cylinder, Loc.T("Цилиндр", "Cylinder"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Mug, Loc.T("Кружка", "Mug"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Teapot, Loc.T("Чайник", "Teapot"));
        x = DrawShelfAdd(app, x, iconY, iconSize, EntityKind.Plane, Loc.T("Шахматный пол", "Checker floor"));

        x += 8f;
        dl.AddLine(new ImVec2(x, shelfTop + 10f), new ImVec2(x, shelfBottom - 6f), Theme.U32(Theme.Shadow), 1f);
        dl.AddLine(new ImVec2(x + 1f, shelfTop + 10f), new ImVec2(x + 1f, shelfBottom - 6f), Theme.U32(Theme.Highlight), 1f);
        x += 10f;

        float util = 40f;
        float utilY = shelfTop + (ShelfHeight - util) * 0.5f - 1f;

        ImGui.SetCursorScreenPos(new ImVec2(x, utilY));
        if (Chrome.IconButton("shelf_rebuild", new ImVec2(util, util),
                (d, p0, p1) => PrimitiveIcons.Draw(d, EntityKind.Terrain,
                    new ImVec2((p0.X + p1.X) * 0.5f, (p0.Y + p1.Y) * 0.5f), util * 0.72f, gold: true),
                tooltip: Loc.T("Новый случайный рельеф", "New random terrain")))
            app.RebuildTerrain();
        x += util + 4f;

        ImGui.SetCursorScreenPos(new ImVec2(x, utilY));
        if (Chrome.IconButton("shelf_frame", new ImVec2(util, util),
                (d, p0, p1) => PrimitiveIcons.Tool(d, ToolGlyph.Frame,
                    new ImVec2(p0.X + 9f, p0.Y + 9f), new ImVec2(p1.X - 9f, p1.Y - 9f), Theme.TextDim),
                tooltip: Loc.T("Кадрировать выделенное (F)", "Frame selection (F)")))
            app.FrameSelected();
        x += util + 4f;

        ImGui.SetCursorScreenPos(new ImVec2(x, utilY));
        if (Chrome.IconButton("shelf_grid", new ImVec2(util, util),
                (d, p0, p1) => PrimitiveIcons.Tool(d, ToolGlyph.Grid,
                    new ImVec2(p0.X + 9f, p0.Y + 9f), new ImVec2(p1.X - 9f, p1.Y - 9f), Theme.TextDim),
                active: app.ShowGrid, tooltip: Loc.T("Сетка (G)", "Grid (G)")))
            app.ShowGrid = !app.ShowGrid;
        x += util + 4f;

        ImGui.SetCursorScreenPos(new ImVec2(x, utilY));
        if (Chrome.IconButton("shelf_render", new ImVec2(util, util),
                (d, p0, p1) => PrimitiveIcons.Tool(d, ToolGlyph.Render,
                    new ImVec2(p0.X + 9f, p0.Y + 9f), new ImVec2(p1.X - 9f, p1.Y - 9f), Theme.TextDim),
                tooltip: Loc.T("Экспорт PNG 1920×1080 (Ctrl+E)", "Export PNG 1920×1080 (Ctrl+E)")))
            app.ExportImage(1920, 1080);
        x += util + 4f;

        ImGui.SetCursorScreenPos(new ImVec2(x, utilY));
        if (Chrome.IconButton("shelf_save", new ImVec2(util, util),
                (d, p0, p1) => PrimitiveIcons.Tool(d, ToolGlyph.Save,
                    new ImVec2(p0.X + 9f, p0.Y + 9f), new ImVec2(p1.X - 9f, p1.Y - 9f), Theme.TextDim),
                tooltip: Loc.T("Сохранить сцену (Ctrl+S)", "Save scene (Ctrl+S)")))
            app.SaveScene();

        // справа — что открыто
        string info = string.IsNullOrEmpty(app.ScenePath)
            ? Loc.T("новый мир", "new world")
            : Path.GetFileName(app.ScenePath);
        ImGui.PushFont(Theme.FontMono);
        var isz = ImGui.CalcTextSize(info);
        ImGui.PopFont();
        dl.AddText(Theme.FontMono, Theme.MonoSize,
            new ImVec2(b.X - isz.X - 18f, shelfTop + 14f), Theme.U32(Theme.TextDim), info);
        string preset = Presets.Get(app.Scene.Preset).Title;
        ImGui.PushFont(Theme.FontSmall);
        var psz = ImGui.CalcTextSize(preset);
        ImGui.PopFont();
        dl.AddText(Theme.FontSmall, Theme.SmallSize,
            new ImVec2(b.X - psz.X - 18f, shelfTop + 32f), Theme.U32(Theme.Text), preset);

        ImGui.End();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    private float DrawTab(float x, float y, string label, EditorTab tab, string tooltip)
    {
        bool active = Tab == tab;
        var size = new ImVec2(Chrome.Measure(Theme.FontHeader, label).X + 46f, 27f);
        ImGui.SetCursorScreenPos(new ImVec2(x, y));
        if (Chrome.Tab("tab_" + tab, label, active, onDark: true))
            Tab = tab;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        return x + size.X + 2f;
    }

    private float DrawShelfAdd(QutreApp app, float x, float y, float size, EntityKind kind, string tooltip)
    {
        ImGui.SetCursorScreenPos(new ImVec2(x, y));
        if (Chrome.IconButton("shelf_" + kind, new ImVec2(size, size),
                (dl, a, b) => PrimitiveIcons.Draw(dl, kind,
                    new ImVec2((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f), size * 0.78f),
                tooltip: Loc.T("Добавить: ", "Add: ") + tooltip))
            app.AddEntity(kind);

        return x + size + 5f;
    }

    // -------------------------------------------------------------- левый край

    private void DrawToolStrip(QutreApp app, float top, float bottom)
    {
        ImGui.SetNextWindowPos(new ImVec2(0f, top));
        ImGui.SetNextWindowSize(new ImVec2(ToolStripWidth, bottom - top));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Theme.Panel);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new ImVec2(4f, 8f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.Begin("##toolstrip", PanelFlags);

        Chrome.PanelChrome();

        float x = (ToolStripWidth - 30f) * 0.5f;
        float y = top + 8f;

        ToolButton("tool_select", ToolGlyph.Select, ref x, ref y, Loc.T("Выбор и перетаскивание", "Select and drag"), false, () => { });
        ToolButton("tool_orbit", ToolGlyph.Orbit, ref x, ref y, Loc.T("Орбита камеры", "Orbit camera"), false,
            () => app.Camera.Orbit(14f, 0f));
        ToolButton("tool_zoom", ToolGlyph.Zoom, ref x, ref y, Loc.T("Приблизить", "Zoom in"), false,
            () => app.Camera.Zoom(0.5f));
        ToolButton("tool_frame", ToolGlyph.Frame, ref x, ref y, Loc.T("Кадрировать (F)", "Frame (F)"), false,
            app.FrameSelected);
        ToolButton("tool_home", ToolGlyph.Home, ref x, ref y, Loc.T("Сброс камеры (Home)", "Reset camera (Home)"), false,
            app.ResetCamera);
        ToolButton("tool_grid", ToolGlyph.Grid, ref x, ref y, Loc.T("Сетка (G)", "Grid (G)"), app.ShowGrid,
            () => app.ShowGrid = !app.ShowGrid);
        ToolButton("tool_render", ToolGlyph.Render, ref x, ref y, Loc.T("Экспорт PNG (Ctrl+E)", "Export PNG (Ctrl+E)"), false,
            () => app.ExportImage(1920, 1080));
        ToolButton("tool_save", ToolGlyph.Save, ref x, ref y, Loc.T("Сохранить (Ctrl+S)", "Save (Ctrl+S)"), false,
            app.SaveScene);
        ToolButton("tool_delete", ToolGlyph.Delete, ref x, ref y, Loc.T("Удалить выделенное (Del)", "Delete selected (Del)"),
            false, app.DeleteSelected, Selected >= 0 && Selected < app.Scene.Entities.Count);

        ImGui.End();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    private void ToolButton(string id, ToolGlyph glyph, ref float x, ref float y, string tooltip, bool active,
        Action action, bool enabled = true)
    {
        ImGui.SetCursorScreenPos(new ImVec2(x, y));
        if (Chrome.IconButton(id, new ImVec2(30f, 30f),
                (dl, a, b) => PrimitiveIcons.Tool(dl, glyph,
                    new ImVec2(a.X + 7f, a.Y + 7f), new ImVec2(b.X - 7f, b.Y - 7f),
                    active ? Theme.TextOnAccent : Theme.TextDim),
                active, tooltip, enabled))
            action();
        y += 34f;
    }

    // ------------------------------------------------------------------ низ

    private void DrawStatusBar(QutreApp app, float w, float h)
    {
        ImGui.SetNextWindowPos(new ImVec2(0f, h - StatusHeight));
        ImGui.SetNextWindowSize(new ImVec2(w, StatusHeight));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Theme.PanelDark);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new ImVec2(0f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.Begin("##status", PanelFlags);

        var dl = ImGui.GetWindowDrawList();
        var a = ImGui.GetWindowPos();
        var b = a + ImGui.GetWindowSize();

        dl.AddRectFilledMultiColor(a, b,
            Theme.U32(Theme.PanelLight), Theme.U32(Theme.PanelLight),
            Theme.U32(Theme.PanelDark), Theme.U32(Theme.PanelDark));
        dl.AddLine(new ImVec2(a.X, a.Y + 0.5f), new ImVec2(b.X, a.Y + 0.5f), Theme.U32(Theme.Highlight), 1f);
        dl.AddLine(new ImVec2(a.X, b.Y - 0.5f), new ImVec2(b.X, b.Y - 0.5f), Theme.U32(Theme.Shadow), 1f);

        // индикатор состояния
        dl.AddCircleFilled(new ImVec2(a.X + 14f, a.Y + StatusHeight * 0.5f), 3.4f,
            Theme.U32(Theme.AccentBright), 12);
        dl.AddText(Theme.FontHeader, Theme.HeaderSize, new ImVec2(a.X + 24f, a.Y + 9f),
            Theme.U32(Theme.TextDim), app.Status);

        // транспорт по центру
        float btnW = 34f, btnH = 24f, gap = 4f;
        int count = 5;
        float total = count * btnW + (count - 1) * gap;
        float startX = w * 0.5f - total * 0.5f;
        float by = a.Y + (StatusHeight - btnH) * 0.5f;

        var grooveA = new ImVec2(startX - 16f, a.Y + 8f);
        var grooveB = new ImVec2(startX + total + 16f, a.Y + StatusHeight - 8f);
        Chrome.Groove(dl, grooveA, grooveB);

        float bx = startX;
        if (StatusRoundButton("st_home", ToolGlyph.Home, bx, by, btnW, btnH, Loc.T("Сброс камеры", "Reset camera")))
            app.ResetCamera();
        bx += btnW + gap;
        if (StatusRoundButton("st_frame", ToolGlyph.Frame, bx, by, btnW, btnH, Loc.T("Кадрировать", "Frame")))
            app.FrameSelected();
        bx += btnW + gap;
        if (StatusRoundButton("st_grid", ToolGlyph.Grid, bx, by, btnW, btnH, Loc.T("Сетка", "Grid"), app.ShowGrid))
            app.ShowGrid = !app.ShowGrid;
        bx += btnW + gap;
        if (StatusRoundButton("st_render", ToolGlyph.Render, bx, by, btnW, btnH, Loc.T("Рендер PNG", "Render PNG")))
            app.ExportImage(1920, 1080);
        bx += btnW + gap;
        if (StatusRoundButton("st_save", ToolGlyph.Save, bx, by, btnW, btnH, Loc.T("Сохранить", "Save")))
            app.SaveScene();

        string right = Loc.F("{0} объектов  ·  {1:N0} трис  ·  {2:0} fps  ·  QUTRE 3D {3}",
            "{0} objects  ·  {1:N0} tris  ·  {2:0} fps  ·  QUTRE 3D {3}",
            app.Scene.Entities.Count, app.Renderer.TriangleCount, _fps, Version);
        ImGui.PushFont(Theme.FontMono);
        float rw = ImGui.CalcTextSize(right).X;
        ImGui.PopFont();
        float px = MathF.Max(w - rw - 16f, startX + total + 24f);
        dl.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(px, a.Y + 9f), Theme.U32(Theme.TextFaint), right);

        ImGui.End();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    private static bool StatusRoundButton(string id, ToolGlyph glyph, float x, float y, float w, float h,
        string tooltip, bool active = false)
    {
        ImGui.SetCursorScreenPos(new ImVec2(x, y));
        return Chrome.RoundButton(id, new ImVec2(w, h),
            (dl, a, b) => PrimitiveIcons.Tool(dl, glyph,
                new ImVec2(a.X + 9f, a.Y + 6f), new ImVec2(b.X - 9f, b.Y - 6f),
                active ? Theme.TextOnAccent : Theme.TextDim),
            tooltip, active);
    }

    // ------------------------------------------------------- изображение 3D

    public void DrawViewportImage(QutreApp app)
    {
        var dl = ImGui.GetBackgroundDrawList();
        float minX = ViewportMin.X, minY = ViewportMin.Y;
        float maxX = MathF.Max(ViewportMax.X, minX + 1f);
        float maxY = MathF.Max(ViewportMax.Y, minY + 1f);

        dl.PushClipRect(new ImVec2(minX, minY), new ImVec2(maxX, maxY), true);
        dl.AddRectFilled(new ImVec2(minX, minY), new ImVec2(maxX, maxY), Theme.U32(Theme.Canvas));
        dl.AddImage((IntPtr)app.Renderer.OutputTexture,
            new ImVec2(minX, minY), new ImVec2(maxX, maxY),
            new ImVec2(0f, 1f), new ImVec2(1f, 0f));

        dl.AddRectFilledMultiColor(new ImVec2(minX, minY), new ImVec2(maxX, minY + 86f),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0.52f)),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0.52f)),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0f)),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0f)));
        dl.AddRectFilledMultiColor(new ImVec2(minX, maxY - 64f), new ImVec2(maxX, maxY),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0f)),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0f)),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0.58f)),
            Theme.U32(new ImVec4(0.01f, 0.015f, 0.02f, 0.58f)));
        dl.PopClipRect();

        var fg = ImGui.GetForegroundDrawList();

        // толстая фасковая рамка вьюпорта
        fg.AddRect(new ImVec2(minX, minY), new ImVec2(maxX, maxY), Theme.U32(Theme.Border), 0f, 0, 2f);
        fg.AddLine(new ImVec2(minX + 1f, minY + 1f), new ImVec2(maxX - 1f, minY + 1f),
            Theme.U32(new ImVec4(1f, 1f, 1f, 0.35f)), 1f);
        fg.AddLine(new ImVec2(minX + 1f, minY + 1f), new ImVec2(minX + 1f, maxY - 1f),
            Theme.U32(new ImVec4(1f, 1f, 1f, 0.18f)), 1f);

        // уголки
        float corner = 20f;
        uint accent = Theme.U32(new ImVec4(Theme.AccentBright.X, Theme.AccentBright.Y, Theme.AccentBright.Z,
            ViewportHovered ? 0.95f : 0.35f));
        fg.AddLine(new ImVec2(minX, minY), new ImVec2(minX + corner, minY), accent, 2f);
        fg.AddLine(new ImVec2(minX, minY), new ImVec2(minX, minY + corner), accent, 2f);
        fg.AddLine(new ImVec2(maxX - corner, minY), new ImVec2(maxX, minY), accent, 2f);
        fg.AddLine(new ImVec2(maxX, minY), new ImVec2(maxX, minY + corner), accent, 2f);
        fg.AddLine(new ImVec2(minX, maxY - corner), new ImVec2(minX, maxY), accent, 2f);
        fg.AddLine(new ImVec2(minX, maxY), new ImVec2(minX + corner, maxY), accent, 2f);
        fg.AddLine(new ImVec2(maxX - corner, maxY), new ImVec2(maxX, maxY), accent, 2f);
        fg.AddLine(new ImVec2(maxX, maxY - corner), new ImVec2(maxX, maxY), accent, 2f);

        // подпись
        float px = minX + 22f;
        float py = minY + 16f;

        fg.AddText(Theme.FontBrand, Theme.BrandSize, new ImVec2(px + 1f, py + 1f),
            Theme.U32(new ImVec4(0f, 0f, 0f, 0.45f)), "QUTRE");
        fg.AddText(Theme.FontBrand, Theme.BrandSize, new ImVec2(px, py),
            Theme.U32(new ImVec4(0.96f, 0.98f, 0.97f, 1f)), "QUTRE");
        ImGui.PushFont(Theme.FontBrand);
        float brandWidth = ImGui.CalcTextSize("QUTRE").X;
        ImGui.PopFont();
        fg.AddText(Theme.FontBrand, Theme.BrandSize, new ImVec2(px + brandWidth + 9f, py + 6f),
            Theme.U32(new ImVec4(0.44f, 0.84f, 0.82f, 1f)), "3D");

        fg.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(px + 1f, py + 34f),
            Theme.U32(new ImVec4(0.86f, 0.90f, 0.89f, 0.85f)), app.Scene.Name.ToUpperInvariant());
        fg.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(px + 1f, py + 49f),
            Theme.U32(new ImVec4(0.76f, 0.81f, 0.79f, 0.78f)),
            Presets.Get(app.Scene.Preset).Title + " · " +
            (app.ScenePath is null ? Loc.T("новый мир", "new world") : Path.GetFileName(app.ScenePath)));

        // тех. инфо справа
        string info = $"{maxX - minX:0}×{maxY - minY:0}   {_fps:0} fps";
        ImGui.PushFont(Theme.FontMono);
        float iw = ImGui.CalcTextSize(info).X;
        ImGui.PopFont();
        fg.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(maxX - iw - 22f, minY + 20f),
            Theme.U32(new ImVec4(0.82f, 0.86f, 0.85f, 0.75f)), info);

        // подсказки снизу
        string hints = Loc.T(
            "ЛКМ — выбрать/двигать   ·   ПКМ — орбита   ·   СКМ/Shift+ПКМ — пан   ·   колесо — зум   ·   F — кадр",
            "LMB — select/move   ·   RMB — orbit   ·   MMB/Shift+RMB — pan   ·   wheel — zoom   ·   F — frame");
        fg.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(minX + 22f, maxY - 27f),
            Theme.U32(new ImVec4(0.74f, 0.79f, 0.77f, 0.62f)), hints);

        string sel = Selected >= 0 && Selected < app.Scene.Entities.Count
            ? Loc.T("объект: ", "object: ") + app.Scene.Entities[Selected].Name
            : Loc.T("ничего не выбрано", "nothing selected");
        ImGui.PushFont(Theme.FontMono);
        float sw = ImGui.CalcTextSize(sel).X;
        ImGui.PopFont();
        fg.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(maxX - sw - 22f, maxY - 27f),
            Theme.U32(new ImVec4(0.86f, 0.90f, 0.89f, 0.72f)), sel);
    }

    // ------------------------------------------------------------ заставка

    /// <summary>Короткая атмосферная заставка с логотипом при запуске. В CLI-экспорте пропускается.</summary>
    private void DrawIntro(QutreApp app)
    {
        if (_introDone) return;

        if (app.AutoExportPath is not null)
        {
            _introDone = true;
            return;
        }

        _introTime += ImGui.GetIO().DeltaTime;
        float t = _introTime;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            _introDone = true;
            return;
        }

        const float fadeIn = 0.30f;
        const float fadeOut = 0.42f;
        float alpha = 1f;

        if (t < fadeIn) alpha = t / fadeIn;
        else if (t > IntroDuration - fadeOut) alpha = (IntroDuration - t) / fadeOut;
        alpha = Math.Clamp(alpha, 0f, 1f);

        if (t >= IntroDuration)
        {
            _introDone = true;
            return;
        }

        var dl = ImGui.GetForegroundDrawList();
        var screen = ImGui.GetIO().DisplaySize;

        dl.AddRectFilled(ImVec2.Zero, screen,
            Theme.U32(new ImVec4(0.008f, 0.011f, 0.015f, 0.94f * alpha)));

        float logoH = MathF.Min(screen.Y * 0.46f, 380f);
        float logoW = logoH * (Logo.Ready ? Logo.Aspect : 1.6f);
        var center = new ImVec2(screen.X * 0.5f, screen.Y * 0.47f);
        var p0 = new ImVec2(center.X - logoW * 0.5f, center.Y - logoH * 0.5f);
        var p1 = new ImVec2(center.X + logoW * 0.5f, center.Y + logoH * 0.5f);
        var white = new ImVec4(1f, 1f, 1f, alpha);

        if (Logo.Ready)
            dl.AddImage((IntPtr)Logo.Handle, p0, p1, Logo.Uv0, Logo.Uv1, Theme.U32(white));
        else
            dl.AddText(Theme.FontBrand, 46f, new ImVec2(p0.X, center.Y - 24f),
                Theme.U32(new ImVec4(0.93f, 0.96f, 0.96f, alpha)), "QUTRE 3D");

        float lineW = MathF.Min(260f, logoW * 0.62f);
        float lineY = p1.Y + 26f;
        var l0 = new ImVec2(center.X - lineW * 0.5f, lineY);
        var l1 = new ImVec2(l0.X + lineW, lineY + 1f);

        dl.AddRectFilled(l0, l1, Theme.U32(new ImVec4(1f, 1f, 1f, 0.10f * alpha)));
        float progress = Math.Clamp(t / IntroDuration, 0f, 1f);
        dl.AddRectFilled(l0, new ImVec2(l0.X + lineW * progress, lineY + 1f),
            Theme.U32(new ImVec4(Theme.AccentBright.X, Theme.AccentBright.Y, Theme.AccentBright.Z, 0.85f * alpha)));

        var caption = new ImVec4(Theme.TextDim.X, Theme.TextDim.Y, Theme.TextDim.Z, alpha);
        dl.AddText(Theme.FontMono, 12f, new ImVec2(l0.X, lineY + 12f), Theme.U32(caption),
            Loc.T("атмосферный редактор миров", "atmospheric world editor"));
    }

    // ------------------------------------------------------------- модалки

    private void DrawModals(QutreApp app)
    {
        if (_showAbout)
        {
            ImGui.OpenPopup(Loc.T("О Qutre 3D", "About Qutre 3D") + "###about");
            _showAbout = false;
        }
        if (_showShortcuts)
        {
            ImGui.OpenPopup(Loc.T("Горячие клавиши", "Keyboard shortcuts") + "###shortcuts");
            _showShortcuts = false;
        }

        ImGui.SetNextWindowSize(new ImVec2(468f, 0f), ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal(Loc.T("О Qutre 3D", "About Qutre 3D") + "###about", ImGuiWindowFlags.NoResize))
        {
            if (Logo.Ready)
            {
                Logo.Draw(ContentWidth() / Logo.Aspect);
                ImGui.Spacing();
            }
            else
            {
                ImGui.TextColored(Theme.Accent, "QUTRE 3D");
            }

            ImGui.TextWrapped(Loc.T(
                "Атмосферный 3D-редактор миров: процедурный рельеф, вода, туман, пыль и киношный пост.",
                "An atmospheric 3D world editor: procedural terrain, water, fog, dust and a cinematic post stack."));
            ImGui.Spacing();
            ImGui.TextColored(Theme.TextDim, Loc.F("Версия {0} · C# / OpenTK / ImGui",
                "Version {0} · C# / OpenTK / ImGui", Version));
            ImGui.TextColored(Theme.TextDim, Loc.T(
                "Сцены: .qutre (JSON) — обычный текст, можно править руками.",
                "Scenes: .qutre (JSON) — plain text you can edit by hand."));
            ImGui.TextColored(Theme.TextDim, Loc.T(
                "Интерфейс: русский / English — переключается в меню «Вид → Язык».",
                "Interface: English / Русский — switch it in View → Language."));
            ImGui.Spacing();
            if (ImGui.Button(Loc.T("Закрыть", "Close"), new ImVec2(-1f, 0f))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }

        ImGui.SetNextWindowSize(new ImVec2(480f, 0f), ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal(Loc.T("Горячие клавиши", "Keyboard shortcuts") + "###shortcuts",
                ImGuiWindowFlags.NoResize))
        {
            (string, string)[] rows =
            {
                ("Ctrl+N / Ctrl+O", Loc.T("новая сцена / открыть", "new scene / open")),
                ("Ctrl+S / Ctrl+Shift+S", Loc.T("сохранить / сохранить как", "save / save as")),
                ("Ctrl+E", Loc.T("экспорт PNG 1920×1080", "export PNG 1920×1080")),
                (Loc.T("ЛКМ", "LMB"), Loc.T("выбрать объект, тянуть по земле", "select an object, drag on the ground")),
                (Loc.T("Shift + ЛКМ", "Shift + LMB"), Loc.T("тянуть в плоскости экрана", "drag in screen space")),
                (Loc.T("ПКМ / СКМ", "RMB / MMB"), Loc.T("орбита / панорама", "orbit / pan")),
                (Loc.T("Колесо", "Wheel"), Loc.T("приближение", "zoom")),
                ("F", Loc.T("кадрировать выделенное", "frame selection")),
                ("G", Loc.T("сетка", "grid")),
                ("Del", Loc.T("удалить объект", "delete object")),
                ("F1", Loc.T("эта справка", "this help")),
            };
            foreach (var (key, what) in rows)
            {
                ImGui.TextColored(Theme.Accent, key);
                ImGui.SameLine(205f);
                ImGui.TextColored(Theme.TextDim, what);
            }
            ImGui.Spacing();
            if (ImGui.Button(Loc.T("Понятно", "Got it"), new ImVec2(-1f, 0f))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    // ------------------------------------------------------------ хоткеи

    private void HandleShortcuts(QutreApp app)
    {
        if (app.Dialog.IsOpen) return;

        var io = ImGui.GetIO();
        if (io.WantTextInput) return;

        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.N)) app.NewScene();
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.O)) app.OpenSceneDialog();
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S)) { if (io.KeyShift) app.SaveSceneAs(); else app.SaveScene(); }
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.E)) app.ExportImage(1920, 1080);

        if (ImGui.IsKeyPressed(ImGuiKey.F1)) _showShortcuts = true;
        if (ImGui.IsKeyPressed(ImGuiKey.G)) app.ShowGrid = !app.ShowGrid;
        if (ImGui.IsKeyPressed(ImGuiKey.F)) app.FrameSelected();
        if (ImGui.IsKeyPressed(ImGuiKey.Home)) app.ResetCamera();
        if (ImGui.IsKeyPressed(ImGuiKey.Delete) && !io.WantTextInput) app.DeleteSelected();
    }

    // ------------------------------------------------------------ хелперы

    private static float ContentWidth() => MathF.Max(ImGui.GetContentRegionAvail().X, 16f);

    private static void Dim(string text) => ImGui.TextColored(Theme.TextFaint, text);

    private static bool DragF(string id, string label, ref float value, float speed, float min, float max, string fmt = "%.2f")
    {
        ImGui.TextColored(Theme.TextDim, label);
        ImGui.SetNextItemWidth(-1f);
        return ImGui.DragFloat("##" + id, ref value, speed, min, max, fmt);
    }

    private static bool SlideF(string id, string label, ref float value, float min, float max, string fmt = "%.3f")
    {
        ImGui.TextColored(Theme.TextDim, label);
        ImGui.SetNextItemWidth(-1f);
        return ImGui.SliderFloat("##" + id, ref value, min, max, fmt);
    }

    private static bool DragI(string id, string label, ref int value, float speed, int min, int max)
    {
        ImGui.TextColored(Theme.TextDim, label);
        ImGui.SetNextItemWidth(-1f);
        return ImGui.DragInt("##" + id, ref value, speed, min, max);
    }

    private static void DragV3(string id, string label, ref Vector3 value, float speed, float min, float max, string fmt = "%.2f")
    {
        ImGui.TextColored(Theme.TextDim, label);
        var v = new ImVec3(value.X, value.Y, value.Z);
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.DragFloat3("##" + id, ref v, speed, min, max, fmt))
            value = new Vector3(v.X, v.Y, v.Z);
    }

    private static void ColorRow(string id, string label, ref Vector3 value)
    {
        ImGui.TextColored(Theme.TextDim, label);
        ImGui.SameLine();
        var c = new ImVec3(value.X, value.Y, value.Z);
        float rightEdge = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(rightEdge - 74f);
        ImGui.SetNextItemWidth(74f);
        if (ImGui.ColorEdit3("##" + id, ref c,
                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.NoAlpha))
            value = new Vector3(c.X, c.Y, c.Z);
    }
}
