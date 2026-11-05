using ImGuiNET;
using OpenTK.Mathematics;
using Qutre3D.App;
using Qutre3D.Core;
using ImVec2 = System.Numerics.Vector2;
using ImVec4 = System.Numerics.Vector4;

namespace Qutre3D.UI;

public sealed partial class EditorUI
{
    private static ImVec4 DotColor(EntityKind kind) => kind switch
    {
        EntityKind.Terrain => new ImVec4(0.30f, 0.56f, 0.34f, 1f),
        EntityKind.Water => new ImVec4(0.16f, 0.48f, 0.62f, 1f),
        EntityKind.Sphere => new ImVec4(0.74f, 0.55f, 0.20f, 1f),
        EntityKind.Box => new ImVec4(0.48f, 0.48f, 0.50f, 1f),
        EntityKind.Torus => new ImVec4(0.70f, 0.42f, 0.24f, 1f),
        EntityKind.Cylinder => new ImVec4(0.34f, 0.52f, 0.40f, 1f),
        EntityKind.Mug => new ImVec4(0.86f, 0.82f, 0.70f, 1f),
        EntityKind.Teapot => new ImVec4(0.16f, 0.46f, 0.50f, 1f),
        EntityKind.Plane => new ImVec4(0.84f, 0.86f, 0.82f, 1f),
        _ => new ImVec4(0.5f, 0.5f, 0.5f, 1f),
    };

    private const ImGuiWindowFlags PanelFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus;

    // ------------------------------------------------------------------ слева

    private void DrawBrandHeader()
    {
        if (!Logo.Ready)
        {
            ImGui.PushFont(Theme.FontHeader);
            ImGui.TextColored(Theme.Text, "QUTRE 3D");
            ImGui.PopFont();
            ImGui.TextColored(Theme.TextFaint, Loc.T("атмосферный редактор миров", "atmospheric world editor"));
            return;
        }

        float h = 40f;
        float w = h * Logo.Aspect;
        var origin = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(new ImVec2(origin.X - 2f, origin.Y - 2f),
            new ImVec2(origin.X + w + 2f, origin.Y + h + 2f),
            Theme.U32(new ImVec4(0.06f, 0.09f, 0.10f, 1f)), 2f);

        ImGui.Image((IntPtr)Logo.Handle, new ImVec2(w, h), Logo.Uv0, Logo.Uv1);
        ImGui.SameLine();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + h * 0.12f);
        ImGui.PushFont(Theme.FontHeader);
        ImGui.TextColored(Theme.Text, "QUTRE 3D");
        ImGui.PopFont();
        ImGui.PushFont(Theme.FontSmall);
        ImGui.TextColored(Theme.TextFaint, Loc.T("редактор миров", "world editor"));
        ImGui.PopFont();
    }

    private void DrawLeftPanel(QutreApp app, float top, float bottom)
    {
        ImGui.SetNextWindowPos(new ImVec2(ToolStripWidth, top));
        ImGui.SetNextWindowSize(new ImVec2(LeftWidth, bottom - top));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Theme.Panel);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new ImVec2(11f, 9f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new ImVec2(6f, 5f));

        ImGui.Begin("##leftpanel", PanelFlags);

        Chrome.PanelChrome();

        DrawBrandHeader();
        DrawSkyCard(app);

        Chrome.SectionTitle(Loc.T("Слои", "Layers"));
        DrawLayerList(app);

        Chrome.SectionTitle(Loc.T("Вид", "View"));
        DrawViewButtons(app);

        ImGui.Spacing();
        DrawDeleteButton(app);

        if (ImGui.GetContentRegionAvail().Y > 206f)
            DrawNavPad(app);

        DrawLeftStats(app, bottom - top);

        ImGui.End();
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
    }

    private void DrawSkyCard(QutreApp app)
    {
        ImGui.Spacing();
        float w = MathF.Max(ImGui.GetContentRegionAvail().X, 16f);
        float h = 86f;
        var a = ImGui.GetCursorScreenPos();
        var b = new ImVec2(a.X + w, a.Y + h);

        ImGui.InvisibleButton("##skypreview", new ImVec2(w, h));
        bool hovered = ImGui.IsItemHovered();
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            Tab = EditorTab.Sky;

        var dl = ImGui.GetWindowDrawList();
        var sky = app.Scene.Sky;

        dl.PushClipRect(a, b, true);
        dl.AddRectFilledMultiColor(a, b,
            Theme.U32(Sky(sky.Zenith)), Theme.U32(Sky(sky.Zenith)),
            Theme.U32(Sky(sky.Horizon)), Theme.U32(Sky(sky.Horizon)));

        float groundY = b.Y - h * 0.24f;
        dl.AddRectFilledMultiColor(new ImVec2(a.X, groundY), b,
            Theme.U32(Sky(sky.Ground, 0.75f)), Theme.U32(Sky(sky.Ground, 0.75f)),
            Theme.U32(Sky(sky.Ground)), Theme.U32(Sky(sky.Ground)));
        dl.AddLine(new ImVec2(a.X, groundY), new ImVec2(b.X, groundY),
            Theme.U32(Theme.A(Sky(sky.Horizon), 0.55f)), 1f);

        if (sky.CloudCover > 0.02f)
        {
            uint cloud = Theme.U32(new ImVec4(0.92f, 0.94f, 0.93f, 0.10f + 0.22f * sky.CloudCover));
            PrimitiveIcons.EllipseFill(dl, new ImVec2(a.X + w * 0.30f, a.Y + h * 0.30f), w * 0.18f, h * 0.06f, cloud);
            PrimitiveIcons.EllipseFill(dl, new ImVec2(a.X + w * 0.52f, a.Y + h * 0.22f), w * 0.22f, h * 0.05f, cloud);
            PrimitiveIcons.EllipseFill(dl, new ImVec2(a.X + w * 0.74f, a.Y + h * 0.34f), w * 0.16f, h * 0.045f, cloud);
        }

        float az = MathHelper.DegreesToRadians(sky.SunAzimuth);
        float el = Math.Clamp(sky.SunElevation, 0f, 90f) / 90f;
        float sunX = a.X + w * (0.5f + MathF.Sin(az) * 0.32f);
        float sunY = groundY - (groundY - a.Y) * (0.08f + el * 0.72f);

        var sunColor = Sky(sky.SunColor);
        for (int i = 5; i >= 1; i--)
            dl.AddCircleFilled(new ImVec2(sunX, sunY), 2.2f + i * 2.6f,
                Theme.U32(Theme.A(sunColor, 0.05f + 0.035f * (5 - i))), 24);
        dl.AddCircleFilled(new ImVec2(sunX, sunY), 3.2f, Theme.U32(sunColor), 14);

        if (hovered)
            dl.AddRectFilled(a, b, Theme.U32(new ImVec4(1f, 1f, 1f, 0.06f)));
        dl.PopClipRect();

        dl.AddRect(a, b, Theme.U32(Theme.Border), 2f, ImDrawFlags.None, 1f);
        Chrome.Bevel(dl, a, b, false, 2f, 1f);

        dl.AddText(Theme.FontTiny, Theme.TinySize, new ImVec2(a.X + 8f, a.Y + 6f),
            Theme.U32(new ImVec4(1f, 1f, 1f, 0.72f)), "SKY");
        string sun = Loc.F("{0:0}°", "{0:0}°", sky.SunElevation);
        ImGui.PushFont(Theme.FontTiny);
        float sw = ImGui.CalcTextSize(sun).X;
        ImGui.PopFont();
        float sx = b.X - sw - 17f;
        dl.AddCircleFilled(new ImVec2(sx, a.Y + 12f), 3f,
            Theme.U32(new ImVec4(1f, 0.92f, 0.62f, 0.9f)), 10);
        dl.AddText(Theme.FontTiny, Theme.TinySize, new ImVec2(b.X - sw - 7f, a.Y + 6f),
            Theme.U32(new ImVec4(1f, 1f, 1f, 0.72f)), sun);
        dl.AddText(Theme.FontTiny, Theme.TinySize, new ImVec2(a.X + 8f, b.Y - 15f),
            Theme.U32(new ImVec4(1f, 1f, 1f, 0.80f)), Presets.Get(app.Scene.Preset).Title);
    }

    private static ImVec4 Sky(Vector3 c, float alpha = 1f) => new(c.X, c.Y, c.Z, alpha);

    private void DrawLayerList(QutreApp app)
    {
        float h = 168f;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Theme.Field);
        ImGui.BeginChild("##layers", new ImVec2(-1f, h), ImGuiChildFlags.Borders);

        if (app.Scene.Entities.Count == 0)
        {
            ImGui.Spacing();
            Dim(Loc.T("  пусто — добавьте ландшафт", "  empty — add some terrain"));
        }

        var dl = ImGui.GetWindowDrawList();
        float width = MathF.Max(ImGui.GetContentRegionAvail().X, 16f);

        for (int i = 0; i < app.Scene.Entities.Count; i++)
        {
            var entity = app.Scene.Entities[i];
            ImGui.PushID(i);

            var rowStart = ImGui.GetCursorScreenPos();
            bool isSelected = Selected == i;

            ImGui.InvisibleButton("##row", new ImVec2(width, 25f));
            bool clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
            bool hovered = ImGui.IsItemHovered();
            var rowEnd = new ImVec2(rowStart.X + width, rowStart.Y + 25f);

            if (clicked)
            {
                Selected = i;
                _entityName = entity.Name;
            }
            else if (hovered && !isSelected)
            {
                dl.AddRectFilled(rowStart, rowEnd, Theme.U32(new ImVec4(1f, 1f, 1f, 0.35f)), 2f);
            }

            if (isSelected)
            {
                dl.AddRectFilled(rowStart, rowEnd, Theme.U32(Theme.A(Theme.AccentBright, 0.20f)), 2f);
                dl.AddRect(rowStart, rowEnd, Theme.U32(Theme.AccentDim), 2f, ImDrawFlags.None, 1f);
                dl.AddRectFilled(rowStart, new ImVec2(rowStart.X + 2f, rowEnd.Y), Theme.U32(Theme.Accent), 1f);
            }

            dl.AddCircleFilled(new ImVec2(rowStart.X + 10f, rowStart.Y + 12.5f), 3.3f,
                Theme.U32(DotColor(entity.Kind)), 10);

            string shown = entity.Name.Length > 19 ? entity.Name[..18] + "…" : entity.Name;
            uint textColor = Theme.U32(entity.Visible
                ? (isSelected ? Theme.Text : Theme.TextDim)
                : Theme.TextFaint);
            dl.AddText(Theme.FontBody, Theme.BodySize, new ImVec2(rowStart.X + 21f, rowStart.Y + 4f),
                textColor, shown);

            string kind = Entity.KindTitle(entity.Kind);
            ImGui.PushFont(Theme.FontTiny);
            float kw = ImGui.CalcTextSize(kind).X;
            ImGui.PopFont();
            dl.AddText(Theme.FontTiny, Theme.TinySize,
                new ImVec2(rowEnd.X - kw - 7f, rowStart.Y + 7f), Theme.U32(Theme.TextFaint), kind);

            ImGui.PopID();
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private void DrawViewButtons(QutreApp app)
    {
        float bw = (ImGui.GetContentRegionAvail().X - 5f) * 0.5f;

        if (Chrome.Button("view_frame", Loc.T("Кадр  F", "Frame  F"), new ImVec2(bw, 26f), bold: true))
            app.FrameSelected();
        ImGui.SameLine();
        if (Chrome.Button("view_reset", Loc.T("Сброс  Home", "Reset  Home"), new ImVec2(bw, 26f), bold: true))
            app.ResetCamera();

        if (Chrome.Button("view_grid", Loc.T("Сетка  G", "Grid  G"), new ImVec2(bw, 26f), app.ShowGrid, bold: true))
            app.ShowGrid = !app.ShowGrid;
        ImGui.SameLine();
        if (Chrome.Button("view_render", Loc.T("Рендер  PNG", "Render  PNG"), new ImVec2(bw, 26f), bold: true))
            app.ExportImage(1920, 1080);
    }

    private void DrawDeleteButton(QutreApp app)
    {
        bool enabled = Selected >= 0 && Selected < app.Scene.Entities.Count;
        ImGui.BeginDisabled(!enabled);
        if (Chrome.Button("delete_selected", Loc.T("Удалить выделенное", "Delete selected"),
                new ImVec2(-1f, 28f), bold: true))
            app.DeleteSelected();
        ImGui.EndDisabled();
    }

    /// <summary>
    /// Навигационный крест в духе Bryce: клик по четверти — орбита/наклон,
    /// клик по центру — сброс камеры. Справа — зум и сброс.
    /// </summary>
    private void DrawNavPad(QutreApp app)
    {
        Chrome.SectionTitle(Loc.T("Навигация", "Navigation"));

        float w = MathF.Max(ImGui.GetContentRegionAvail().X, 16f);
        float h = 108f;
        var a = ImGui.GetCursorScreenPos();
        var b = new ImVec2(a.X + w, a.Y + h);
        var dl = ImGui.GetWindowDrawList();
        Chrome.Card(dl, a, b, 2f);

        float pad = 92f;
        var pa = new ImVec2(a.X + 8f, a.Y + (h - pad) * 0.5f);
        var pb = new ImVec2(pa.X + pad, pa.Y + pad);
        var center = new ImVec2((pa.X + pb.X) * 0.5f, (pa.Y + pb.Y) * 0.5f);

        Chrome.Inset(dl, pa, pb, 2f, new ImVec4(0.585f, 0.585f, 0.570f, 1f));

        ImGui.SetCursorScreenPos(pa);
        ImGui.InvisibleButton("##navpad", new ImVec2(pad, pad));
        bool hovered = ImGui.IsItemHovered();
        bool clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        var mouse = ImGui.GetIO().MousePos;
        float dx = mouse.X - center.X;
        float dy = mouse.Y - center.Y;
        bool overCenter = hovered && MathF.Abs(dx) < pad * 0.17f && MathF.Abs(dy) < pad * 0.17f;

        bool horizontal = MathF.Abs(dx) > MathF.Abs(dy);
        int dirX = horizontal ? Math.Sign(dx) : 0;
        int dirY = horizontal ? 0 : Math.Sign(dy);

        if (clicked)
        {
            if (overCenter)
            {
                app.ResetCamera();
            }
            else if (horizontal)
            {
                app.Camera.Orbit(dirX > 0 ? -16f : 16f, 0f);
            }
            else
            {
                app.Camera.Orbit(0f, dirY > 0 ? -12f : 12f);
            }
        }

        // подсветка выбранной четверти
        if (hovered && !overCenter)
        {
            uint hi = Theme.U32(Theme.A(Theme.AccentBright, 0.30f));
            if (horizontal)
            {
                float x0 = dirX > 0 ? center.X + 6f : pa.X + 4f;
                float x1 = dirX > 0 ? pb.X - 4f : center.X - 6f;
                dl.AddRectFilled(new ImVec2(x0, center.Y - 22f), new ImVec2(x1, center.Y + 22f), hi, 2f);
            }
            else
            {
                float y0 = dirY > 0 ? center.Y + 6f : pa.Y + 4f;
                float y1 = dirY > 0 ? pb.Y - 4f : center.Y - 6f;
                dl.AddRectFilled(new ImVec2(center.X - 22f, y0), new ImVec2(center.X + 22f, y1), hi, 2f);
            }
        }

        // крест
        uint groove = Theme.U32(new ImVec4(0f, 0f, 0f, 0.16f));
        uint lift = Theme.U32(new ImVec4(1f, 1f, 1f, 0.35f));
        dl.AddLine(new ImVec2(pa.X + 8f, center.Y + 1f), new ImVec2(pb.X - 8f, center.Y + 1f), lift, 1f);
        dl.AddLine(new ImVec2(center.X + 1f, pa.Y + 8f), new ImVec2(center.X + 1f, pb.Y - 8f), lift, 1f);
        dl.AddLine(new ImVec2(pa.X + 8f, center.Y), new ImVec2(pb.X - 8f, center.Y), groove, 1f);
        dl.AddLine(new ImVec2(center.X, pa.Y + 8f), new ImVec2(center.X, pb.Y - 8f), groove, 1f);

        uint arrow = Theme.U32(overCenter || !hovered
            ? Theme.TextDim
            : new ImVec4(Theme.Accent.X, Theme.Accent.Y, Theme.Accent.Z, 1f));
        float asz = 5.5f;
        dl.AddTriangleFilled(new ImVec2(center.X, pa.Y + 6f), new ImVec2(center.X - asz, pa.Y + 13f),
            new ImVec2(center.X + asz, pa.Y + 13f), arrow);
        dl.AddTriangleFilled(new ImVec2(center.X, pb.Y - 6f), new ImVec2(center.X - asz, pb.Y - 13f),
            new ImVec2(center.X + asz, pb.Y - 13f), arrow);
        dl.AddTriangleFilled(new ImVec2(pa.X + 6f, center.Y), new ImVec2(pa.X + 13f, center.Y - asz),
            new ImVec2(pa.X + 13f, center.Y + asz), arrow);
        dl.AddTriangleFilled(new ImVec2(pb.X - 6f, center.Y), new ImVec2(pb.X - 13f, center.Y - asz),
            new ImVec2(pb.X - 13f, center.Y + asz), arrow);

        dl.AddCircleFilled(center, 9f, Theme.U32(new ImVec4(0.86f, 0.86f, 0.845f, 1f)), 20);
        dl.AddCircle(center, 9f, Theme.U32(Theme.Border), 20, 1f);
        Chrome.Diamond(dl, center, overCenter ? 5.2f : 4.2f,
            Theme.U32(overCenter ? Theme.AccentBright : Theme.Accent));

        // правая колонка: зум и сброс
        float rx = pa.X + pad + 9f;
        float rw = b.X - rx - 8f;
        float bw = MathF.Max((rw - 5f) * 0.5f, 16f);

        ImGui.SetCursorScreenPos(new ImVec2(rx, a.Y + 14f));
        if (Chrome.Button("nav_zoom_out", "-", new ImVec2(bw, 30f), bold: true))
            app.Camera.Zoom(-1.6f);
        ImGui.SameLine(0f, 5f);
        if (Chrome.Button("nav_zoom_in", "+", new ImVec2(bw, 30f), bold: true))
            app.Camera.Zoom(1.6f);

        ImGui.SetCursorScreenPos(new ImVec2(rx, a.Y + 49f));
        if (Chrome.Button("nav_reset", Loc.T("Сброс", "Reset"), new ImVec2(rw, 30f), bold: true))
            app.ResetCamera();

        dl.AddText(Theme.FontTiny, Theme.TinySize, new ImVec2(rx, a.Y + 86f),
            Theme.U32(Theme.TextFaint), Loc.T("орбита · наклон", "orbit · tilt"));
    }

    private void DrawLeftStats(QutreApp app, float panelHeight)
    {
        var wp = ImGui.GetWindowPos();
        var ws = ImGui.GetWindowSize();
        var dl = ImGui.GetWindowDrawList();
        float y = wp.Y + ws.Y - 46f;

        dl.AddLine(new ImVec2(wp.X + 10f, y), new ImVec2(wp.X + ws.X - 10f, y),
            Theme.U32(new ImVec4(0f, 0f, 0f, 0.18f)), 1f);
        dl.AddLine(new ImVec2(wp.X + 10f, y + 1f), new ImVec2(wp.X + ws.X - 10f, y + 1f),
            Theme.U32(new ImVec4(1f, 1f, 1f, 0.45f)), 1f);

        dl.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(wp.X + 12f, y + 8f),
            Theme.U32(Theme.TextDim), Loc.F("{0} объектов", "{0} objects", app.Scene.Entities.Count));
        dl.AddText(Theme.FontMono, Theme.MonoSize, new ImVec2(wp.X + 12f, y + 25f),
            Theme.U32(Theme.TextFaint), Loc.F("{0:N0} полигонов", "{0:N0} polygons", app.Renderer.TriangleCount));
    }

    // ------------------------------------------------------------------ справа

    private void DrawRightPanel(QutreApp app, float top, float bottom)
    {
        float x = MathF.Max(app.ClientSize.X - RightWidth, 0f);
        ImGui.SetNextWindowPos(new ImVec2(x, top));
        ImGui.SetNextWindowSize(new ImVec2(RightWidth, bottom - top));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Theme.Panel);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new ImVec2(13f, 10f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new ImVec2(6f, 5f));

        ImGui.Begin("##rightpanel", PanelFlags);

        Chrome.PanelChrome();

        switch (Tab)
        {
            case EditorTab.World:
                DrawWorldPanel(app);
                break;
            case EditorTab.Object:
                DrawObjectPanel(app);
                break;
            case EditorTab.Sky:
                DrawSkyPanel(app);
                break;
        }

        ImGui.End();
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
    }

    private void DrawWorldPanel(QutreApp app)
    {
        Chrome.SectionTitle(Loc.T("Мир", "World"));

        ImGui.TextColored(Theme.TextDim, Loc.T("Название", "Name"));
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText("##sceneName", ref _sceneName, 96))
            app.Scene.Name = _sceneName;

        ImGui.Spacing();
        ImGui.TextColored(Theme.TextDim, Loc.T("Пресет", "Preset"));

        float bw = (ImGui.GetContentRegionAvail().X - 6f) * 0.5f;
        for (int i = 0; i < Presets.All.Count; i++)
        {
            var preset = Presets.All[i];
            if (i % 2 == 1) ImGui.SameLine();

            bool active = app.Scene.Preset == preset.Id;
            if (Chrome.Button("preset_" + preset.Id, preset.Title, new ImVec2(bw, 28f), active, bold: true))
                app.ApplyPreset(preset.Id);
        }

        ImGui.Spacing();
        ImGui.Checkbox(Loc.T("Пыль в воздухе", "Dust in the air"), ref app.Scene.Dust);
        SlideF("dustdensity", Loc.T("Плотность пыли", "Dust density"), ref app.Scene.DustDensity, 0f, 3f);

        Chrome.SectionTitle(Loc.T("Статистика", "Statistics"));
        StatRow(Loc.T("Объектов", "Objects"), app.Scene.Entities.Count.ToString());
        StatRow(Loc.T("Полигонов", "Polygons"), $"{app.Renderer.TriangleCount:N0}");
        StatRow(Loc.T("Вьюпорт", "Viewport"),
            $"{ViewportMax.X - ViewportMin.X:0}×{ViewportMax.Y - ViewportMin.Y:0}");
        StatRow(Loc.T("Кадров в секунду", "Frames per second"), $"{_fps:0}");

        ImGui.Spacing();
        ImGui.Spacing();
        if (Chrome.Button("world_reset_cam", Loc.T("Сбросить камеру", "Reset camera"),
                new ImVec2(-1f, 28f), bold: true))
            app.ResetCamera();
    }

    private static void StatRow(string label, string value)
    {
        ImGui.TextColored(Theme.TextDim, label);
        ImGui.SameLine();
        var ts = ImGui.CalcTextSize(value);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f,
            ImGui.GetContentRegionAvail().X - ts.X - 2f));
        ImGui.TextColored(Theme.TextFaint, value);
    }

    private void DrawObjectPanel(QutreApp app)
    {
        Chrome.SectionTitle(Loc.T("Объект", "Object"));

        bool hasSelection = Selected >= 0 && Selected < app.Scene.Entities.Count;
        if (!hasSelection)
        {
            Dim(Loc.T("Ничего не выбрано.", "Nothing is selected."));
            ImGui.Spacing();
            Dim(Loc.T("Щёлкните объект во вьюпорте или строку в «Слоях».",
                "Click an object in the viewport or a row in Layers."));
            ImGui.Spacing();
            ImGui.Spacing();
            if (Chrome.Button("obj_pick_terrain", Loc.T("Выбрать ландшафт", "Select terrain"),
                    new ImVec2(-1f, 28f), bold: true))
            {
                for (int i = 0; i < app.Scene.Entities.Count; i++)
                {
                    if (app.Scene.Entities[i].Kind == EntityKind.Terrain)
                    {
                        Selected = i;
                        break;
                    }
                }
            }
            return;
        }

        DrawEntityInspector(app, app.Scene.Entities[Selected]);
    }

    private void DrawSkyPanel(QutreApp app)
    {
        Chrome.SectionTitle(Loc.T("Атмосфера", "Atmosphere"));
        DrawAtmosphere(app);

        Chrome.SectionTitle(Loc.T("Туман и воздух", "Fog & air"));
        DrawFog(app);

        Chrome.SectionTitle(Loc.T("Пост-обработка", "Post-processing"));
        DrawPost(app);

        ImGui.Spacing();
        ImGui.Spacing();
    }

    private void DrawEntityInspector(QutreApp app, Entity entity)
    {
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText("##entityName", ref _entityName, 96))
            entity.Name = _entityName;

        ImGui.TextColored(Theme.TextFaint, Entity.KindTitle(entity.Kind));
        ImGui.Checkbox(Loc.T("видимый", "visible"), ref entity.Visible);

        Chrome.SectionTitle(Loc.T("Трансформация", "Transform"));
        DragV3("pos", Loc.T("Позиция", "Position"), ref entity.Position, 0.1f, -2000f, 2000f);
        DragV3("rot", Loc.T("Поворот", "Rotation"), ref entity.Rotation, 0.4f, -360f, 360f, "%.1f");
        DragV3("scl", Loc.T("Масштаб", "Scale"), ref entity.Scale, 0.05f, 0.01f, 800f);

        if (entity.Kind == EntityKind.Terrain)
        {
            DrawTerrainSection(entity.Terrain);
        }
        else if (entity.Kind == EntityKind.Water)
        {
            Chrome.SectionTitle(Loc.T("Вода", "Water"));
            ColorRow("waterDeep", Loc.T("Цвет глубины", "Deep colour"), ref entity.Albedo);
            SlideF("waterRough", Loc.T("Волнение", "Ripple"), ref entity.Roughness, 0f, 1f);
            Dim(Loc.T("0 — стекло, 1 — рябь и барашки.", "0 — glass, 1 — ripples and whitecaps."));
        }
        else if (entity.Kind == EntityKind.Plane)
        {
            Chrome.SectionTitle(Loc.T("Шахматный пол", "Checker floor"));
            ColorRow("planeDark", Loc.T("Тёмные клетки", "Dark tiles"), ref entity.Albedo);
            ColorRow("planeLight", Loc.T("Светлые клетки", "Light tiles"), ref entity.TileColor);
            DragF("planeTile", Loc.T("Размер клетки", "Tile size"), ref entity.TileSize, 0.2f, 0.5f, 400f, "%.1f");
            SlideF("planeGloss", Loc.T("Шероховатость", "Roughness"), ref entity.Roughness, 0.02f, 1f);
            Dim(Loc.T("Меньше — зеркальнее: пол ловит небо у горизонта.",
                "Lower — more mirror-like: the floor catches the sky at the horizon."));
        }
        else
        {
            Chrome.SectionTitle(Loc.T("Материал", "Material"));
            ColorRow("matAlbedo", Loc.T("Цвет", "Colour"), ref entity.Albedo);
            ColorRow("matEmissive", Loc.T("Свечение", "Emission"), ref entity.Emissive);
            SlideF("matRough", Loc.T("Шероховатость", "Roughness"), ref entity.Roughness, 0.02f, 1f);
            SlideF("matMetal", Loc.T("Металличность", "Metallic"), ref entity.Metallic, 0f, 1f);
            SlideF("matOpacity", Loc.T("Непрозрачность", "Opacity"), ref entity.Opacity, 0f, 1f);
        }

        ImGui.Spacing();
        ImGui.BeginDisabled(entity.Kind is EntityKind.Terrain or EntityKind.Water);
        if (Chrome.Button("entity_duplicate", Loc.T("Дублировать", "Duplicate"),
                new ImVec2((ImGui.GetContentRegionAvail().X - 6f) * 0.5f, 27f), bold: true))
            app.DuplicateSelected();
        ImGui.SameLine();
        if (Chrome.Button("entity_frame", Loc.T("Кадрировать", "Frame"), new ImVec2(-1f, 27f), bold: true))
            app.FrameSelected();
        ImGui.EndDisabled();
    }

    private void DrawTerrainSection(TerrainSettings t)
    {
        Chrome.SectionTitle(Loc.T("Ландшафт", "Terrain"));

        ImGui.TextColored(Theme.TextDim, Loc.T("Семя", "Seed"));
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 96f);
        ImGui.DragInt("##terrainSeed", ref t.Seed, 1f, 0, 999999);
        ImGui.SameLine();
        if (Chrome.Button("terrain_new_seed", Loc.T("новый", "new"), new ImVec2(-1f, 24f)))
            t.Seed = Random.Shared.Next(0, 999999);

        DragF("terrainSize", Loc.T("Размер", "Size"), ref t.Size, 1f, 20f, 600f, "%.0f");
        DragF("terrainHeight", Loc.T("Высота", "Height"), ref t.Height, 0.2f, 0f, 140f, "%.1f");
        SlideF("terrainFreq", Loc.T("Частота", "Frequency"), ref t.Frequency, 0.2f, 6f, "%.2f");
        SlideF("terrainWarp", Loc.T("Искривление", "Warp"), ref t.Warp, 0f, 2f, "%.2f");

        ImGui.TextColored(Theme.TextDim, Loc.T("Октавы", "Octaves"));
        ImGui.SetNextItemWidth(-1f);
        ImGui.SliderInt("##terrainOct", ref t.Octaves, 1, 8);

        ImGui.TextColored(Theme.TextDim, Loc.T("Детализация (пересборка)", "Detail (rebuild)"));
        ImGui.SetNextItemWidth(-1f);
        ImGui.SliderInt("##terrainRes", ref t.Resolution, 32, 320);

        ImGui.Spacing();
        ColorRow("terrainLow", Loc.T("Низины", "Lowlands"), ref t.LowColor);
        ColorRow("terrainHigh", Loc.T("Вершины", "Peaks"), ref t.HighColor);
        ColorRow("terrainRock", Loc.T("Скалы", "Rock"), ref t.RockColor);
        ColorRow("terrainMoss", Loc.T("Мох", "Moss"), ref t.MossColor);
    }

    private void DrawAtmosphere(QutreApp app)
    {
        var sky = app.Scene.Sky;

        SlideF("sunElevation", Loc.T("Высота солнца", "Sun elevation"), ref sky.SunElevation, -6f, 90f, "%.1f°");
        SlideF("sunAzimuth", Loc.T("Азимут", "Azimuth"), ref sky.SunAzimuth, 0f, 360f, "%.0f°");
        SlideF("sunIntensity", Loc.T("Интенсивность", "Intensity"), ref sky.SunIntensity, 0f, 6f, "%.2f");
        ColorRow("sunColor", Loc.T("Цвет солнца", "Sun colour"), ref sky.SunColor);

        ImGui.Spacing();
        ColorRow("skyZenith", Loc.T("Зенит", "Zenith"), ref sky.Zenith);
        ColorRow("skyHorizon", Loc.T("Горизонт", "Horizon"), ref sky.Horizon);
        ColorRow("skyGround", Loc.T("Подсветка снизу", "Bounce light"), ref sky.Ground);

        SlideF("skyClouds", Loc.T("Облака", "Clouds"), ref sky.CloudCover, 0f, 1f);
        SlideF("skyStars", Loc.T("Звёзды", "Stars"), ref sky.Stars, 0f, 1f);
    }

    private void DrawFog(QutreApp app)
    {
        var fog = app.Scene.Fog;
        ImGui.Checkbox(Loc.T("Туман включён", "Fog enabled"), ref fog.Enabled);
        SlideF("fogDensity", Loc.T("Плотность", "Density"), ref fog.Density, 0f, 0.06f, "%.4f");
        SlideF("fogHeight", Loc.T("Высота слоя", "Layer height"), ref fog.HeightFalloff, 0f, 0.4f, "%.3f");
        ColorRow("fogColor", Loc.T("Цвет мглы", "Fog colour"), ref fog.Color);
    }

    private void DrawPost(QutreApp app)
    {
        var post = app.Scene.Post;
        SlideF("postExposure", Loc.T("Экспозиция", "Exposure"), ref post.Exposure, 0.1f, 3f, "%.2f");
        SlideF("postBloom", Loc.T("Свечение", "Bloom"), ref post.Bloom, 0f, 2f, "%.2f");
        SlideF("postBloomThreshold", Loc.T("Порог свечения", "Bloom threshold"), ref post.BloomThreshold, 0f, 3f, "%.2f");
        SlideF("postGodRays", Loc.T("Лучи солнца", "God rays"), ref post.GodRays, 0f, 2f, "%.2f");
        SlideF("postGrain", Loc.T("Зерно", "Grain"), ref post.Grain, 0f, 0.2f, "%.3f");
        SlideF("postVignette", Loc.T("Виньетка", "Vignette"), ref post.Vignette, 0f, 1f, "%.2f");
        SlideF("postSaturation", Loc.T("Насыщенность", "Saturation"), ref post.Saturation, 0f, 2f, "%.2f");
        SlideF("postContrast", Loc.T("Контраст", "Contrast"), ref post.Contrast, 0.5f, 1.8f, "%.2f");
        SlideF("postChromatic", Loc.T("Аберрация", "Chromatic aberration"), ref post.Chromatic, 0f, 3f, "%.2f");
        ColorRow("postTint", Loc.T("Тонирование", "Tint"), ref post.Tint);
    }
}
