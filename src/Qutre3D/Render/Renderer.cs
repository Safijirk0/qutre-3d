using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Qutre3D.Core;

namespace Qutre3D.Render;

/// <summary>
/// Главный конвейер: небо -> геометрия -> пыль -> bloom -> пост.
/// Всё рисуется в HDR-таргет, поэтому туман, солнце и блики выглядят «дорого».
/// </summary>
public sealed class Renderer : IDisposable
{
    private sealed class TerrainEntry
    {
        public int Hash;
        public Mesh Mesh;
    }

    private readonly Shader _sky;
    private readonly Shader _object;
    private readonly Shader _terrain;
    private readonly Shader _water;
    private readonly Shader _checker;
    private readonly Shader _dust;
    private readonly Shader _line;
    private readonly Shader _post;
    private readonly Shader _bright;
    private readonly Shader _blur;

    private readonly Mesh _sphere;
    private readonly Mesh _box;
    private readonly Mesh _torus;
    private readonly Mesh _cylinder;
    private readonly Mesh _quad;
    private readonly Mesh _grid;
    private readonly Mesh _boxWire;
    private readonly Mesh _axisX;
    private readonly Mesh _axisY;
    private readonly Mesh _axisZ;

    private readonly Dictionary<Entity, TerrainEntry> _terrainCache = new();

    private Framebuffer _sceneFbo;
    private Framebuffer _bloomA;
    private Framebuffer _bloomB;
    private Framebuffer _postFbo;

    private int _emptyVao;
    private int _dustVao;
    private int _dustVbo;
    private int _dustCount;
    private int _width;
    private int _height;

    public int OutputTexture => _postFbo.ColorTexture;
    public int TriangleCount { get; private set; }

    public Renderer()
    {
        _sky = new Shader(Glsl.SkyVert, Glsl.Common + "\n" + Glsl.SkyFrag);
        _object = new Shader(Glsl.ObjectVert, Glsl.Common + "\n" + Glsl.ObjectFrag);
        _terrain = new Shader(Glsl.ObjectVert, Glsl.Common + "\n" + Glsl.TerrainFrag);
        _water = new Shader(Glsl.ObjectVert, Glsl.Common + "\n" + Glsl.WaterFrag);
        _checker = new Shader(Glsl.ObjectVert, Glsl.Common + "\n" + Glsl.CheckerFrag);
        _dust = new Shader(Glsl.DustVert, Glsl.DustFrag);
        _line = new Shader(Glsl.LineVert, Glsl.LineFrag);
        _post = new Shader(Glsl.FullscreenVert, Glsl.PostFrag);
        _bright = new Shader(Glsl.FullscreenVert, Glsl.BrightFrag);
        _blur = new Shader(Glsl.FullscreenVert, Glsl.BlurFrag);

        _sphere = MeshFactory.Sphere();
        _box = MeshFactory.Box();
        _torus = MeshFactory.Torus();
        _cylinder = MeshFactory.Cylinder();
        _quad = MeshFactory.Quad();
        _grid = MeshFactory.Grid(48, 3f);
        _boxWire = BuildBoxWire();
        _axisX = BuildAxis(new Vector3(1, 0, 0));
        _axisY = BuildAxis(new Vector3(0, 1, 0));
        _axisZ = BuildAxis(new Vector3(0, 0, 1));

        _emptyVao = GL.GenVertexArray();
        CreateDust();

        _width = 64;
        _height = 64;
        _sceneFbo = new Framebuffer(_width, _height, true, true);
        _bloomA = new Framebuffer(_width / 2, _height / 2, true, false);
        _bloomB = new Framebuffer(_width / 2, _height / 2, true, false);
        _postFbo = new Framebuffer(_width, _height, false, false);
    }

    private static Mesh BuildBoxWire()
    {
        var p = new List<float>();
        void Edge(Vector3 a, Vector3 b)
        {
            p.Add(a.X); p.Add(a.Y); p.Add(a.Z);
            p.Add(b.X); p.Add(b.Y); p.Add(b.Z);
        }

        Vector3[] c =
        {
            new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
            new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1),
        };

        int[,] edges = { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        for (int i = 0; i < edges.GetLength(0); i++)
            Edge(c[edges[i, 0]], c[edges[i, 1]]);

        var idx = new uint[p.Count / 3];
        for (int i = 0; i < idx.Length; i++) idx[i] = (uint)i;
        return Mesh.CreateLines(p.ToArray(), idx);
    }

    private static Mesh BuildAxis(Vector3 dir)
    {
        var p = new float[] { 0, 0, 0, dir.X, dir.Y, dir.Z };
        return Mesh.CreateLines(p, new uint[] { 0, 1 });
    }

    private void CreateDust()
    {
        var rnd = new Random(20241104);
        const int count = 1100;
        _dustCount = count;

        var data = new float[count * 4];
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(rnd.NextDouble() * Math.PI * 2.0);
            float radius = 6f + (float)rnd.NextDouble() * 110f;
            data[i * 4 + 0] = MathF.Cos(angle) * radius;
            data[i * 4 + 1] = (float)rnd.NextDouble() * 45f + 0.5f;
            data[i * 4 + 2] = MathF.Sin(angle) * radius;
            data[i * 4 + 3] = (float)rnd.NextDouble();
        }

        _dustVao = GL.GenVertexArray();
        GL.BindVertexArray(_dustVao);

        _dustVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _dustVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 4 * sizeof(float), 3 * sizeof(float));

        GL.BindVertexArray(0);
    }

    private void EnsureSize(int width, int height)
    {
        width = Math.Max(width, 16);
        height = Math.Max(height, 16);

        if (width == _width && height == _height) return;

        _width = width;
        _height = height;

        _sceneFbo.Resize(width, height);
        _bloomA.Resize(width / 2, height / 2);
        _bloomB.Resize(width / 2, height / 2);
        _postFbo.Resize(width, height);
    }

    public void Render(Scene scene, Camera camera, float time, int width, int height, int selectedIndex, bool showGrid = false)
    {
        EnsureSize(width, height);
        Rasterize(scene, camera, time, selectedIndex, showGrid);
        PostProcess(scene, camera, time);
    }

    /// <summary>Отрендерить кадр в память (RGBA, строки сверху вниз).</summary>
    public byte[] CaptureRgba(Scene scene, Camera camera, float time, int width, int height, int selectedIndex, bool showGrid = false)
    {
        EnsureSize(width, height);
        Rasterize(scene, camera, time, selectedIndex, showGrid);
        PostProcess(scene, camera, time);

        _postFbo.Bind();
        GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
        var raw = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, raw);
        Framebuffer.BindDefault();

        var flipped = new byte[raw.Length];
        int row = width * 4;
        for (int y = 0; y < height; y++)
            System.Buffer.BlockCopy(raw, y * row, flipped, (height - 1 - y) * row, row);

        return flipped;
    }

    private void Rasterize(Scene scene, Camera camera, float time, int selectedIndex, bool showGrid)
    {
        float aspect = _width / (float)_height;
        Matrix4 viewProj = camera.ViewProjection(aspect);

        _sceneFbo.Bind();
        GL.Viewport(0, 0, _width, _height);
        GL.ClearColor(0f, 0f, 0f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(true);

        TriangleCount = 0;

        // ---- небо ----
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        _sky.Use();
        SetAtmosphere(_sky, scene, camera, time);
        _sky.Set("uInvViewProj", Matrix4.Invert(viewProj));
        GL.BindVertexArray(_emptyVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);

        // ---- геометрия ----
        foreach (var entity in scene.Entities)
        {
            if (!entity.Visible) continue;

            switch (entity.Kind)
            {
                case EntityKind.Terrain:
                    DrawTerrain(entity, viewProj, scene, camera, time);
                    break;
                case EntityKind.Water:
                    DrawWater(entity, viewProj, scene, camera, time);
                    break;
                case EntityKind.Plane:
                    DrawPlane(entity, viewProj, scene, camera, time);
                    break;
                default:
                    DrawPrimitive(entity, viewProj, scene, camera, time);
                    break;
            }
        }

        DrawGrid(scene, camera, viewProj, showGrid);
        DrawSelection(scene, camera, viewProj, selectedIndex);
        DrawDust(scene, camera, viewProj, time);

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
    }

    private void DrawTerrain(Entity entity, Matrix4 viewProj, Scene scene, Camera camera, float time)
    {
        var t = entity.Terrain;
        int hash = HashCode.Combine(t.Seed, t.Size, t.Height, t.Frequency, t.Octaves, t.Warp, t.Resolution, t.Resolution);

        if (!_terrainCache.TryGetValue(entity, out var entry) || entry.Hash != hash)
        {
            entry?.Mesh.Dispose();
            entry = new TerrainEntry { Hash = hash, Mesh = MeshFactory.BuildTerrain(t) };
            _terrainCache[entity] = entry;
        }

        _terrain.Use();
        SetAtmosphere(_terrain, scene, camera, time);
        _terrain.Set("uViewProj", viewProj);
        _terrain.Set("uModel", entity.ModelMatrix());
        _terrain.Set("uLowColor", t.LowColor);
        _terrain.Set("uHighColor", t.HighColor);
        _terrain.Set("uRockColor", t.RockColor);
        _terrain.Set("uMossColor", t.MossColor);

        entry.Mesh.Draw();
        TriangleCount += entry.Mesh.IndexCount / 3;
    }

    private void DrawWater(Entity entity, Matrix4 viewProj, Scene scene, Camera camera, float time)
    {
        var scale = new Vector3(
            MathF.Max(MathF.Abs(entity.Scale.X), 0.01f) * 2600f,
            1f,
            MathF.Max(MathF.Abs(entity.Scale.Z), 0.01f) * 2600f);

        _water.Use();
        SetAtmosphere(_water, scene, camera, time);
        _water.Set("uViewProj", viewProj);
        _water.Set("uModel", Matrix4.CreateScale(scale) * Matrix4.CreateTranslation(entity.Position));
        _water.Set("uDeepColor", entity.Albedo);
        _water.Set("uWaveScale", 1f);
        _water.Set("uWaveSpeed", 1f);
        _water.Set("uWaveStrength", Math.Clamp(0.20f + (1f - entity.Roughness) * 1.1f, 0.05f, 1.5f));

        _quad.Draw();
        TriangleCount += 2;
    }

    private void DrawPlane(Entity entity, Matrix4 viewProj, Scene scene, Camera camera, float time)
    {
        _checker.Use();
        SetAtmosphere(_checker, scene, camera, time);
        _checker.Set("uViewProj", viewProj);
        _checker.Set("uModel", entity.ModelMatrix());
        _checker.Set("uAlbedo", entity.Albedo);
        _checker.Set("uTileColor", entity.TileColor);
        _checker.Set("uTileSize", MathF.Max(entity.TileSize, 0.05f));
        _checker.Set("uRoughness", Math.Clamp(entity.Roughness, 0.02f, 1f));

        // мягкие контактные тени: без них хром «висит» над полом
        var sun = scene.Sky.SunDirection;
        var sunFlat = new Vector2(sun.X, sun.Z);
        sunFlat = sunFlat.LengthSquared > 1e-5f ? Vector2.Normalize(sunFlat) : Vector2.Zero;

        int shadows = 0;
        string[] shadowNames = { "uShadow0", "uShadow1", "uShadow2", "uShadow3" };
        foreach (var e in scene.Entities)
        {
            if (shadows >= 4) break;
            if (!e.Visible) continue;
            if (e.Kind is not (EntityKind.Sphere or EntityKind.Box or EntityKind.Torus or EntityKind.Cylinder))
                continue;

            float footprint = e.Kind switch
            {
                EntityKind.Sphere => MathF.Max(e.Scale.X, e.Scale.Z),
                EntityKind.Box => MathF.Max(e.Scale.X, e.Scale.Z) * 1.5f,
                EntityKind.Cylinder => MathF.Max(e.Scale.X, e.Scale.Z) * 1.4f,
                EntityKind.Torus => MathF.Max(e.Scale.X, e.Scale.Z) * 1.45f,
                _ => 1f,
            };

            float halfHeight = e.Kind switch
            {
                EntityKind.Sphere => e.Scale.Y,
                EntityKind.Box => e.Scale.Y,
                EntityKind.Cylinder => e.Scale.Y,
                EntityKind.Torus => e.Scale.Y * 1.2f,
                _ => e.Scale.Y,
            };

            float bottom = e.Position.Y - MathF.Abs(halfHeight);
            if (bottom > 55f) continue;

            float strength = 0.72f * Math.Clamp(1f - bottom / 45f, 0.12f, 1f);
            float radius = MathF.Max(footprint * (2.4f + MathF.Max(bottom, 0f) * 0.02f), 1f);
            var center = new Vector2(e.Position.X, e.Position.Z) - sunFlat * MathF.Max(halfHeight, 0f) * 0.55f;

            _checker.Set(shadowNames[shadows], new Vector4(center.X, center.Y, radius, strength));
            shadows++;
        }

        _checker.Set("uShadowCount", shadows);

        _quad.Draw();
        TriangleCount += 2;
    }

    private void DrawPrimitive(Entity entity, Matrix4 viewProj, Scene scene, Camera camera, float time)
    {
        Mesh mesh = entity.Kind switch
        {
            EntityKind.Sphere => _sphere,
            EntityKind.Box => _box,
            EntityKind.Torus => _torus,
            EntityKind.Cylinder => _cylinder,
            _ => _sphere,
        };

        bool transparent = entity.Opacity < 0.999f;
        if (transparent)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);
        }

        _object.Use();
        SetAtmosphere(_object, scene, camera, time);
        _object.Set("uViewProj", viewProj);
        _object.Set("uModel", entity.ModelMatrix());
        _object.Set("uAlbedo", entity.Albedo);
        _object.Set("uEmissive", entity.Emissive);
        _object.Set("uRoughness", Math.Clamp(entity.Roughness, 0.02f, 1f));
        _object.Set("uMetallic", Math.Clamp(entity.Metallic, 0f, 1f));
        _object.Set("uOpacity", Math.Clamp(entity.Opacity, 0f, 1f));

        mesh.Draw();
        TriangleCount += mesh.IndexCount / 3;

        if (transparent)
        {
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
        }
    }

    private void DrawGrid(Scene scene, Camera camera, Matrix4 viewProj, bool show)
    {
        if (!show) return;

        _line.Use();
        _line.Set("uModel", Matrix4.Identity);
        _line.Set("uViewProj", viewProj);
        _line.Set("uColor", new Vector3(0.35f, 0.65f, 0.62f));
        _line.Set("uAlpha", 0.22f);
        _line.Set("uCameraPos", camera.Position);
        _line.Set("uFadeStart", 60f);
        _line.Set("uFadeEnd", 220f);
        _grid.DrawLines();
    }

    private void DrawSelection(Scene scene, Camera camera, Matrix4 viewProj, int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= scene.Entities.Count) return;
        var entity = scene.Entities[selectedIndex];
        if (!entity.Visible) return;

        float radius = MathF.Max(entity.BoundingRadius(), 0.5f);
        var center = entity.Position;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);

        var accent = new Vector3(0.42f, 0.86f, 0.78f);

        _line.Use();
        _line.Set("uViewProj", viewProj);
        _line.Set("uCameraPos", camera.Position);
        _line.Set("uFadeStart", 5000f);
        _line.Set("uFadeEnd", 9000f);

        if (entity.Kind is EntityKind.Terrain or EntityKind.Water or EntityKind.Plane)
        {
            float size = entity.IsTerrain ? entity.Terrain.Size * 0.5f : 40f;
            if (entity.Kind == EntityKind.Plane)
                size = MathF.Max(entity.TileSize * 3f, 12f);
            _line.Set("uModel", Matrix4.CreateScale(new Vector3(size, 0.02f, size)) * Matrix4.CreateTranslation(center));
            _line.Set("uColor", accent);
            _line.Set("uAlpha", 0.35f);
            _boxWire.DrawLines();
        }
        else
        {
            _line.Set("uModel", Matrix4.CreateScale(radius) * Matrix4.CreateTranslation(center));
            _line.Set("uColor", accent);
            _line.Set("uAlpha", 0.75f);
            _boxWire.DrawLines();

            float gizmo = MathF.Max(radius * 0.9f, 2f);
            var m = Matrix4.CreateScale(gizmo) * Matrix4.CreateTranslation(center);
            _line.Set("uModel", m);
            _line.Set("uAlpha", 0.9f);
            _line.Set("uColor", new Vector3(0.95f, 0.35f, 0.32f));
            _axisX.DrawLines();
            _line.Set("uColor", new Vector3(0.45f, 0.92f, 0.45f));
            _axisY.DrawLines();
            _line.Set("uColor", new Vector3(0.40f, 0.62f, 0.98f));
            _axisZ.DrawLines();
        }

        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
    }

    private void DrawDust(Scene scene, Camera camera, Matrix4 viewProj, float time)
    {
        if (!scene.Dust || scene.DustDensity <= 0.001f) return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
        GL.DepthMask(false);

        _dust.Use();
        _dust.Set("uViewProj", viewProj);
        _dust.Set("uCameraPos", camera.Position);
        _dust.Set("uTime", time);
        _dust.Set("uDensity", scene.DustDensity);
        _dust.Set("uDustColor", scene.Sky.SunColor * 0.5f + new Vector3(0.1f, 0.12f, 0.11f));

        GL.BindVertexArray(_dustVao);
        GL.DrawArrays(PrimitiveType.Points, 0, _dustCount);
        GL.BindVertexArray(0);

        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    private void PostProcess(Scene scene, Camera camera, float time)
    {
        float aspect = _width / (float)_height;
        Matrix4 viewProj = camera.ViewProjection(aspect);

        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(_emptyVao);

        // 1. яркие области
        _bloomA.Bind();
        GL.Viewport(0, 0, _bloomA.Width, _bloomA.Height);
        _bright.Use();
        _bright.Set("uSource", 0);
        _bright.Set("uThreshold", scene.Post.BloomThreshold);
        BindTexture(TextureUnit.Texture0, _sceneFbo.ColorTexture);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // 2. размытие
        // Радиус ореола держим пропорциональным кадру, иначе в маленьком окне
        // bloom «расплывается» сильнее, чем в экспорте 1920×1080.
        float bloomScale = MathF.Max(_height / 1080f, 0.35f);

        _bloomB.Bind();
        GL.Viewport(0, 0, _bloomB.Width, _bloomB.Height);
        _blur.Use();
        _blur.Set("uSource", 0);
        _blur.Set("uDirection", new Vector2(bloomScale / _bloomA.Width, 0f));
        BindTexture(TextureUnit.Texture0, _bloomA.ColorTexture);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        _bloomA.Bind();
        GL.Viewport(0, 0, _bloomA.Width, _bloomA.Height);
        _blur.Set("uDirection", new Vector2(0f, bloomScale / _bloomA.Height));
        BindTexture(TextureUnit.Texture0, _bloomB.ColorTexture);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        // 3. финальный пост
        _postFbo.Bind();
        GL.Viewport(0, 0, _width, _height);

        Vector2 sunScreen = new(0.5f, 1.2f);
        Vector3 sunWorld = camera.Position + scene.Sky.SunDirection * 2000f;
        Vector4 clip = Vector4.TransformRow(new Vector4(sunWorld, 1f), viewProj);
        if (clip.W > 0.001f)
        {
            var ndc = new Vector2(clip.X / clip.W, clip.Y / clip.W);
            sunScreen = ndc * 0.5f + new Vector2(0.5f, 0.5f);
        }

        _post.Use();
        _post.Set("uScene", 0);
        _post.Set("uBloom", 1);
        _post.Set("uResolution", new Vector2(_width, _height));
        _post.Set("uSunScreen", sunScreen);
        _post.Set("uShaftColor", scene.Sky.SunColor * scene.Sky.SunIntensity * 0.45f);
        _post.Set("uTint", scene.Post.Tint);
        _post.Set("uExposure", scene.Post.Exposure);
        _post.Set("uBloomStrength", scene.Post.Bloom);
        _post.Set("uGodRays", scene.Post.GodRays);
        _post.Set("uGrain", scene.Post.Grain);
        _post.Set("uVignette", scene.Post.Vignette);
        _post.Set("uSaturation", scene.Post.Saturation);
        _post.Set("uContrast", scene.Post.Contrast);
        _post.Set("uChromatic", scene.Post.Chromatic);
        _post.Set("uTime", time);

        BindTexture(TextureUnit.Texture0, _sceneFbo.ColorTexture);
        BindTexture(TextureUnit.Texture1, _bloomA.ColorTexture);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        GL.BindVertexArray(0);
        Framebuffer.BindDefault();
        GL.DepthMask(true);
    }

    private static void BindTexture(TextureUnit unit, int texture)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, texture);
    }

    private static void SetAtmosphere(Shader shader, Scene scene, Camera camera, float time)
    {
        var sky = scene.Sky;
        var fog = scene.Fog;

        shader.Set("uSunDir", sky.SunDirection);
        shader.Set("uSunColor", sky.SunColor);
        shader.Set("uSunIntensity", sky.SunIntensity);
        shader.Set("uZenithColor", sky.Zenith);
        shader.Set("uHorizonColor", sky.Horizon);
        shader.Set("uGroundColor", sky.Ground);
        shader.Set("uCloudCover", sky.CloudCover);
        shader.Set("uStars", sky.Stars);
        shader.Set("uTime", time);

        shader.Set("uCameraPos", camera.Position);
        shader.Set("uFogColor", fog.Color);
        shader.Set("uFogDensity", fog.Enabled ? fog.Density : 0f);
        shader.Set("uFogHeightFalloff", fog.HeightFalloff);

        Entity floor = null;
        foreach (var entity in scene.Entities)
        {
            if (entity.Visible && entity.Kind == EntityKind.Plane)
            {
                floor = entity;
                break;
            }
        }

        if (floor is not null)
        {
            shader.Set("uFloorReflect", 1f);
            shader.Set("uFloorY", floor.Position.Y);
            shader.Set("uFloorTile", MathF.Max(floor.TileSize, 0.1f));
            shader.Set("uFloorDark", floor.Albedo);
            shader.Set("uFloorLight", floor.TileColor);
        }
        else
        {
            shader.Set("uFloorReflect", 0f);
            shader.Set("uFloorY", 0f);
            shader.Set("uFloorTile", 1f);
            shader.Set("uFloorDark", Vector3.Zero);
            shader.Set("uFloorLight", Vector3.Zero);
        }
    }

    public void Dispose()
    {
        foreach (var entry in _terrainCache.Values) entry.Mesh.Dispose();
        _terrainCache.Clear();

        _sky.Dispose(); _object.Dispose(); _terrain.Dispose(); _water.Dispose();
        _checker.Dispose(); _dust.Dispose(); _line.Dispose(); _post.Dispose();
        _bright.Dispose(); _blur.Dispose();

        _sphere.Dispose(); _box.Dispose(); _torus.Dispose(); _cylinder.Dispose();
        _quad.Dispose(); _grid.Dispose(); _boxWire.Dispose();
        _axisX.Dispose(); _axisY.Dispose(); _axisZ.Dispose();

        _sceneFbo.Dispose(); _bloomA.Dispose(); _bloomB.Dispose(); _postFbo.Dispose();

        GL.DeleteVertexArray(_emptyVao);
        GL.DeleteVertexArray(_dustVao);
        GL.DeleteBuffer(_dustVbo);
    }
}

