using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Qutre3D.Core;
using Qutre3D.Export;
using Qutre3D.Render;
using Qutre3D.UI;

namespace Qutre3D.App;

/// <summary>
/// Окно редактора и вся «клейкая» логика: камера, выбор объектов, файлы, экспорт.
/// </summary>
public sealed class QutreApp : GameWindow
{
    private enum DragMode
    {
        None,
        Orbit,
        Pan,
        Move,
    }

    public Scene Scene { get; private set; }
    public Camera Camera { get; } = new();
    public Renderer Renderer { get; private set; }
    public EditorUI UI { get; } = new();
    public ImGuiController ImGuiCtl { get; private set; }
    public FileDialog Dialog { get; } = new();
    public AppSettings Settings { get; }

    public string ScenePath { get; private set; }
    public string Status { get; set; } = Loc.T("готов", "ready");
    public float Fps { get; private set; } = 60f;
    public bool ShowGrid { get; set; }
    public bool ViewportHovered { get; private set; }

    /// <summary>Если задано — приложение отрендерит кадр в PNG и закроется (CLI-экспорт).</summary>
    public string AutoExportPath { get; set; }

    /// <summary>Сцена, которую нужно открыть при старте (CLI).</summary>
    public string InitialScenePath { get; set; }

    private bool _autoExported;

    private float _time;
    private DragMode _drag;
    private Vector3 _dragOffset;
    private Vector3 _dragPlanePoint;
    private Vector3 _dragPlaneNormal;

    public QutreApp(AppSettings settings = null) : base(
        new GameWindowSettings { UpdateFrequency = 60.0 },
        CreateWindowSettings())
    {
        Settings = settings ?? new AppSettings();
        Scene = SceneFactory.CreateDefault();
        Dialog.LastDirectory = Settings.LastDirectory;
    }

    private static NativeWindowSettings CreateWindowSettings() => new()
    {
        ClientSize = new Vector2i(1500, 880),
        Title = "Qutre 3D",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        Vsync = VSyncMode.On,
        StartVisible = true,
        NumberOfSamples = 0,
        Icon = LogoAssets.TryLoadWindowIcon(),
    };

    protected override void OnLoad()
    {
        base.OnLoad();

        Renderer = new Renderer();
        ImGuiCtl = new ImGuiController(this);
        UI.Logo.Load();

        if (!string.IsNullOrEmpty(InitialScenePath) && File.Exists(InitialScenePath))
        {
            try
            {
                Scene = SceneIO.Load(InitialScenePath);
                ScenePath = InitialScenePath;
            }
            catch (Exception ex)
            {
                Status = Loc.T("не удалось открыть сцену", "could not open scene");
                Dialog.ShowError(Loc.T("Не удалось открыть сцену", "Could not open scene"), ex.Message);
            }
        }

        ResetCamera();
        UpdateTitle();
        Status = Loc.T("мир создан · выберите объект или настройте атмосферу",
            "world created · pick an object or tune the atmosphere");
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        ImGuiCtl?.AddInputCharacter((char)e.Unicode);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        float dt = (float)Math.Min(args.Time, 0.1);
        _time += dt;
        Fps = Fps * 0.9f + (1f / MathF.Max(dt, 0.0001f)) * 0.1f;

        ImGuiCtl.Update(dt);
        UI.Draw(this);
        HandleViewportInput();

        float vw = MathF.Max(UI.ViewportMax.X - UI.ViewportMin.X, 16f);
        float vh = MathF.Max(UI.ViewportMax.Y - UI.ViewportMin.Y, 16f);
        Renderer.Render(Scene, Camera, _time, (int)vw, (int)vh, UI.Selected, ShowGrid);

        if (!_autoExported && !string.IsNullOrEmpty(AutoExportPath))
        {
            _autoExported = true;
            try
            {
                byte[] rgba = Renderer.CaptureRgba(Scene, Camera, _time, 1920, 1080, UI.Selected, ShowGrid);
                ImageExporter.SavePng(rgba, 1920, 1080, AutoExportPath);
                Status = Loc.T("экспорт: ", "export: ") + AutoExportPath;
            }
            catch (Exception ex)
            {
                Status = Loc.T("ошибка экспорта", "export failed");
                Dialog.ShowError(Loc.T("Не удалось сохранить PNG", "Could not save PNG"), ex.Message);
            }

            Close();
            return;
        }

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        GL.ClearColor(0.792f, 0.792f, 0.776f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        UI.DrawViewportImage(this);

        ImGuiCtl.Render();
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        Settings.LastDirectory = Dialog.LastDirectory;
        Settings.Save();

        UI.Logo.Dispose();
        Renderer?.Dispose();
        ImGuiCtl?.Dispose();
        base.OnUnload();
    }

    // ------------------------------------------------------------- ввод

    private void HandleViewportInput()
    {
        if (Dialog.IsOpen)
        {
            ViewportHovered = false;
            UI.ViewportHovered = false;
            return;
        }

        var io = ImGui.GetIO();
        var mouse = io.MousePos;

        bool inside =
            mouse.X >= UI.ViewportMin.X && mouse.X <= UI.ViewportMax.X &&
            mouse.Y >= UI.ViewportMin.Y && mouse.Y <= UI.ViewportMax.Y;

        ViewportHovered = inside && !io.WantCaptureMouse;
        UI.ViewportHovered = ViewportHovered;

        if (ViewportHovered && io.MouseWheel != 0f)
            Camera.Zoom(Math.Clamp(io.MouseWheel, -4f, 4f));

        if (ViewportHovered)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                _drag = io.KeyShift ? DragMode.Pan : DragMode.Orbit;
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                _drag = DragMode.Pan;
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                BeginLeftDrag(mouse);
        }

        if (_drag == DragMode.Orbit && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            Camera.Orbit(io.MouseDelta.X, io.MouseDelta.Y);
        else if (_drag == DragMode.Pan &&
                 (ImGui.IsMouseDown(ImGuiMouseButton.Middle) || ImGui.IsMouseDown(ImGuiMouseButton.Right)))
            Camera.Pan(io.MouseDelta.X, io.MouseDelta.Y);
        else if (_drag == DragMode.Move && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            UpdateLeftDrag(io);
        else
            _drag = DragMode.None;
    }

    private void BeginLeftDrag(System.Numerics.Vector2 mouse)
    {
        if (!TryPickEntity(mouse, out int picked))
        {
            UI.Selected = -1;
            _drag = DragMode.None;
            return;
        }

        UI.Selected = picked;
        var entity = Scene.Entities[picked];

        if (entity.Kind is EntityKind.Terrain or EntityKind.Water)
        {
            _drag = DragMode.None;
            return;
        }

        bool screenPlane = ImGui.GetIO().KeyShift;
        _dragPlaneNormal = screenPlane ? -Camera.Forward : Vector3.UnitY;
        _dragPlanePoint = entity.Position;

        if (!TryRayPlane(mouse, out Vector3 hit))
        {
            _drag = DragMode.None;
            return;
        }

        _dragOffset = entity.Position - hit;
        _drag = DragMode.Move;
    }

    private void UpdateLeftDrag(ImGuiIOPtr io)
    {
        if (UI.Selected < 0 || UI.Selected >= Scene.Entities.Count)
            return;

        if (!TryRayPlane(io.MousePos, out Vector3 hit))
            return;

        var entity = Scene.Entities[UI.Selected];
        entity.Position = hit + _dragOffset;
        Status = Loc.F("перемещение: {0}", "moving: {0}", entity.Name);
    }

    private bool TryRayPlane(System.Numerics.Vector2 mouse, out Vector3 hit)
    {
        var ray = ScreenRay(mouse);
        float denom = Vector3.Dot(ray.Direction, _dragPlaneNormal);
        hit = default;

        if (MathF.Abs(denom) < 1e-5f) return false;

        float t = Vector3.Dot(_dragPlanePoint - ray.Origin, _dragPlaneNormal) / denom;
        if (t < 0f) return false;

        hit = ray.Origin + ray.Direction * t;
        return true;
    }

    private (Vector3 Origin, Vector3 Direction) ScreenRay(System.Numerics.Vector2 mouse)
    {
        float vw = MathF.Max(UI.ViewportMax.X - UI.ViewportMin.X, 1f);
        float vh = MathF.Max(UI.ViewportMax.Y - UI.ViewportMin.Y, 1f);

        float nx = (mouse.X - UI.ViewportMin.X) / vw * 2f - 1f;
        float ny = 1f - (mouse.Y - UI.ViewportMin.Y) / vh * 2f;

        return Camera.Ray(new Vector2(nx, ny), vw / vh);
    }

    private bool TryPickEntity(System.Numerics.Vector2 mouse, out int index)
    {
        index = -1;
        var ray = ScreenRay(mouse);
        float best = float.MaxValue;

        for (int i = 0; i < Scene.Entities.Count; i++)
        {
            var entity = Scene.Entities[i];
            if (!entity.Visible) continue;
            if (entity.Kind is EntityKind.Terrain or EntityKind.Water) continue;

            if (entity.Kind == EntityKind.Plane)
            {
                if (MathF.Abs(ray.Direction.Y) < 0.0001f) continue;
                float tp = (entity.Position.Y - ray.Origin.Y) / ray.Direction.Y;
                if (tp > 0f && tp < best)
                {
                    best = tp;
                    index = i;
                }

                continue;
            }

            float radius = MathF.Max(entity.BoundingRadius(), 0.5f);
            if (RaySphere(ray.Origin, ray.Direction, entity.Position, radius, out float t) && t < best)
            {
                best = t;
                index = i;
            }
        }

        return index >= 0;
    }

    private static bool RaySphere(Vector3 origin, Vector3 dir, Vector3 center, float radius, out float t)
    {
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, dir);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float disc = b * b - c;
        t = 0f;

        if (disc < 0f) return false;

        float sq = MathF.Sqrt(disc);
        t = -b - sq;
        if (t < 0f) t = -b + sq;
        return t >= 0f;
    }

    // ------------------------------------------------------------ команды

    public void NewScene()
    {
        Scene = SceneFactory.CreateDefault(Scene.Preset);
        ScenePath = null;
        ResetCamera();
        Status = Loc.T("новая сцена", "new scene");
        UpdateTitle();
    }

    public void OpenSceneDialog()
    {
        Dialog.Open(
            Loc.T("Открыть сцену Qutre 3D", "Open a Qutre 3D scene"),
            new[] { "qutre", "json" },
            path =>
            {
                try
                {
                    Scene = SceneIO.Load(path);
                    ScenePath = path;
                    ResetCamera();
                    Status = Loc.T("открыто: ", "opened: ") + Path.GetFileName(path);
                    UpdateTitle();
                    RememberDirectory(path);
                }
                catch (Exception ex)
                {
                    Status = Loc.T("не удалось открыть сцену", "could not open scene");
                    Dialog.ShowError(Loc.T("Не удалось открыть сцену", "Could not open scene"), ex.Message);
                }
            });
    }

    public void SaveScene()
    {
        if (string.IsNullOrEmpty(ScenePath))
        {
            SaveSceneAs();
            return;
        }

        SaveSceneTo(ScenePath);
    }

    public void SaveSceneAs()
    {
        string initial = string.IsNullOrEmpty(ScenePath)
            ? SafeFileName(Scene.Name) + ".qutre"
            : Path.GetFileName(ScenePath);

        Dialog.Save(
            Loc.T("Сохранить сцену Qutre 3D", "Save a Qutre 3D scene"),
            new[] { "qutre", "json" },
            SaveSceneTo,
            initial);
    }

    private void SaveSceneTo(string path)
    {
        try
        {
            SceneIO.Save(Scene, path);
            ScenePath = path;
            Status = Loc.T("сохранено: ", "saved: ") + Path.GetFileName(path);
            UpdateTitle();
            RememberDirectory(path);
        }
        catch (Exception ex)
        {
            Status = Loc.T("не удалось сохранить сцену", "could not save scene");
            Dialog.ShowError(Loc.T("Не удалось сохранить сцену", "Could not save scene"), ex.Message);
        }
    }

    public void ExportImage(int width, int height)
    {
        Dialog.Save(
            Loc.F("Экспорт PNG {0}×{1}", "Export PNG {0}×{1}", width, height),
            new[] { "png" },
            path =>
            {
                try
                {
                    Status = Loc.F("рендер {0}×{1}…", "rendering {0}×{1}…", width, height);
                    byte[] rgba = Renderer.CaptureRgba(Scene, Camera, _time, width, height, UI.Selected);
                    ImageExporter.SavePng(rgba, width, height, path);
                    Status = Loc.F("экспорт: {0} ({1}×{2})", "exported: {0} ({1}×{2})",
                        Path.GetFileName(path), width, height);
                    RememberDirectory(path);
                }
                catch (Exception ex)
                {
                    Status = Loc.T("ошибка экспорта", "export failed");
                    Dialog.ShowError(Loc.T("Не удалось сохранить PNG", "Could not save PNG"), ex.Message);
                }
            },
            $"qutre-{width}x{height}.png");
    }

    private void RememberDirectory(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory)) return;

        Dialog.LastDirectory = directory;
        Settings.LastDirectory = directory;
        Settings.Save();
    }

    private static string SafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "scene";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '-');

        return name.Trim();
    }

    public void SetLanguage(AppLanguage language)
    {
        if (Loc.Language == language) return;

        Loc.SetLanguage(language);
        Settings.SetLanguage(language);
        Settings.Save();
        Status = Loc.T("язык: Русский", "language: English");
        UpdateTitle();
    }

    public void AddEntity(EntityKind kind)
    {
        var entity = new Entity
        {
            Kind = kind,
            Name = UniqueName(Entity.DefaultName(kind)),
        };

        switch (kind)
        {
            case EntityKind.Terrain:
            {
                var template = Scene.Entities.FirstOrDefault(e => e.IsTerrain);
                entity.Terrain = template?.Terrain.Clone() ?? new TerrainSettings();
                entity.Terrain.Seed = Random.Shared.Next(0, 999999);
                break;
            }
            case EntityKind.Water:
                entity.Position = new Vector3(0f, 0f, 0f);
                entity.Albedo = new Vector3(0.025f, 0.095f, 0.105f);
                entity.Roughness = 0.15f;
                break;
            case EntityKind.Sphere:
                entity.Position = Camera.Target + new Vector3(0f, 3f, 0f);
                entity.Scale = new Vector3(4f);
                break;
            case EntityKind.Box:
                entity.Position = Camera.Target + new Vector3(0f, 2f, 0f);
                entity.Scale = new Vector3(3f, 4f, 3f);
                break;
            case EntityKind.Torus:
                entity.Position = Camera.Target + new Vector3(0f, 5f, 0f);
                entity.Scale = new Vector3(5f);
                entity.Rotation = new Vector3(65f, 0f, 0f);
                break;
            case EntityKind.Cylinder:
                entity.Position = Camera.Target + new Vector3(0f, 3f, 0f);
                entity.Scale = new Vector3(2.5f, 4f, 2.5f);
                break;
            case EntityKind.Plane:
                entity.Position = new Vector3(0f, -0.05f, 0f);
                entity.Scale = new Vector3(900f, 1f, 900f);
                entity.Albedo = new Vector3(0.07f, 0.08f, 0.10f);
                entity.TileColor = new Vector3(0.66f, 0.65f, 0.60f);
                entity.TileSize = 12f;
                entity.Roughness = 0.22f;
                break;
        }

        Scene.Entities.Add(entity);
        UI.Selected = Scene.Entities.Count - 1;
        Status = Loc.T("добавлено: ", "added: ") + entity.Name;
    }

    public void DeleteSelected()
    {
        if (UI.Selected < 0 || UI.Selected >= Scene.Entities.Count) return;

        string name = Scene.Entities[UI.Selected].Name;
        Scene.Entities.RemoveAt(UI.Selected);
        UI.Selected = Math.Min(UI.Selected, Scene.Entities.Count - 1);
        Status = Loc.T("удалено: ", "deleted: ") + name;
    }

    public void DuplicateSelected()
    {
        if (UI.Selected < 0 || UI.Selected >= Scene.Entities.Count) return;

        var source = Scene.Entities[UI.Selected];
        var copy = source.Clone();
        copy.Name = UniqueName(source.Name);
        copy.Position += new Vector3(2.5f, 0f, 2.5f);

        Scene.Entities.Insert(UI.Selected + 1, copy);
        UI.Selected++;
        Status = Loc.T("дубликат: ", "duplicate: ") + copy.Name;
    }

    public void ApplyPreset(string id)
    {
        Presets.Apply(Scene, id);
        Status = Loc.T("пресет: ", "preset: ") + Presets.Get(id).Title;
    }

    public void FrameSelected()
    {
        if (UI.Selected < 0 || UI.Selected >= Scene.Entities.Count) return;

        var entity = Scene.Entities[UI.Selected];
        float radius = entity.Kind == EntityKind.Terrain
            ? entity.Terrain.Size * 0.28f
            : entity.Kind == EntityKind.Plane
                ? MathF.Max(entity.TileSize * 3f, 8f)
                : MathF.Max(entity.BoundingRadius(), 1f);

        Camera.Frame(entity.Position, radius);
        Status = Loc.T("кадр: ", "framed: ") + entity.Name;
    }

    public void ResetCamera()
    {
        Camera.Target = new Vector3(0f, 8f, 0f);
        Camera.Distance = 122f;
        Camera.Yaw = 35f;
        Camera.Pitch = 16f;

        if (Scene.Preset == "bryce")
        {
            // почти с уровня глаз: пол уходит в перспективу, небо занимает кадр
            Camera.Target = new Vector3(0f, 9f, -2f);
            Camera.Distance = 88f;
            Camera.Yaw = 26f;
            Camera.Pitch = 8f;
        }
    }

    public void RebuildTerrain()
    {
        int count = 0;
        foreach (var entity in Scene.Entities.Where(e => e.IsTerrain))
        {
            entity.Terrain.Seed = Random.Shared.Next(0, 999999);
            count++;
        }

        Status = count > 0
            ? Loc.F("новый рельеф ({0})", "new terrain ({0})", count)
            : Loc.T("в сцене нет ландшафта", "the scene has no terrain");
    }

    private string UniqueName(string baseName)
    {
        if (Scene.FindEntity(baseName) is null) return baseName;

        for (int i = 2; i < 1000; i++)
        {
            string candidate = $"{baseName} {i}";
            if (Scene.FindEntity(candidate) is null) return candidate;
        }

        return baseName + Loc.T(" копия", " copy");
    }

    private void UpdateTitle()
    {
        Title = string.IsNullOrEmpty(ScenePath)
            ? $"Qutre 3D — {Scene.Name}"
            : $"Qutre 3D — {Scene.Name}  ·  {Path.GetFileName(ScenePath)}";
    }
}
