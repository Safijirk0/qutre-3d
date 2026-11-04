using System.Text.Json.Serialization;
using OpenTK.Mathematics;

namespace Qutre3D.Core;

public enum EntityKind
{
    Terrain,
    Water,
    Sphere,
    Box,
    Torus,
    Cylinder,
    Plane,
}

/// <summary>
/// Параметры процедурного ландшафта. Всё, что влияет на рельеф, живёт здесь.
/// Поля (а не свойства) — чтобы в редакторе работали ref-виджеты ImGui.
/// </summary>
public sealed class TerrainSettings
{
    public int Seed = 1337;
    public float Size = 150f;
    public float Height = 20f;
    public float Frequency = 1.8f;
    public int Octaves = 6;
    public float Warp = 0.65f;
    public int Resolution = 180;

    public Vector3 LowColor = new(0.055f, 0.085f, 0.062f);
    public Vector3 HighColor = new(0.18f, 0.22f, 0.15f);
    public Vector3 RockColor = new(0.20f, 0.19f, 0.17f);
    public Vector3 MossColor = new(0.10f, 0.24f, 0.16f);

    public TerrainSettings Clone() => (TerrainSettings)MemberwiseClone();
}

/// <summary>Объект сцены: ландшафт, вода или примитив.</summary>
public sealed class Entity
{
    public string Name = Loc.T("Объект", "Object");
    public EntityKind Kind = EntityKind.Sphere;
    public bool Visible = true;

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    public Vector3 Albedo = new(0.62f, 0.66f, 0.62f);
    public Vector3 Emissive = Vector3.Zero;
    public float Roughness = 0.7f;
    public float Metallic = 0f;
    public float Opacity = 1f;

    /// <summary>Второй цвет клеток для шахматного пола (EntityKind.Plane).</summary>
    public Vector3 TileColor = new(0.93f, 0.94f, 0.91f);

    /// <summary>Размер клетки шахматного пола в юнитах.</summary>
    public float TileSize = 10f;

    public TerrainSettings Terrain = new();

    [JsonIgnore]
    public bool IsTerrain => Kind == EntityKind.Terrain;

    [JsonIgnore]
    public bool IsWater => Kind == EntityKind.Water;

    public Matrix4 ModelMatrix()
    {
        var rot = Quaternion.FromEulerAngles(
            MathHelper.DegreesToRadians(Rotation.X),
            MathHelper.DegreesToRadians(Rotation.Y),
            MathHelper.DegreesToRadians(Rotation.Z));

        return Matrix4.CreateScale(Scale)
             * Matrix4.CreateFromQuaternion(rot)
             * Matrix4.CreateTranslation(Position);
    }

    /// <summary>Грубый радиус для выбора мышкой и кадрирования.</summary>
    public float BoundingRadius()
    {
        float baseRadius = Kind switch
        {
            EntityKind.Sphere => 1f,
            EntityKind.Box => 1.74f,
            EntityKind.Torus => 1.45f,
            EntityKind.Cylinder => 1.3f,
            EntityKind.Water => 2000f,
            EntityKind.Plane => 2000f,
            EntityKind.Terrain => MathF.Max(Terrain.Size * 0.5f, Terrain.Height),
            _ => 1f,
        };

        float scale = MathF.Max(MathF.Abs(Scale.X), MathF.Max(MathF.Abs(Scale.Y), MathF.Abs(Scale.Z)));
        return baseRadius * scale;
    }

    public static string DefaultName(EntityKind kind) => kind switch
    {
        EntityKind.Terrain => Loc.T("Ландшафт", "Terrain"),
        EntityKind.Water => Loc.T("Вода", "Water"),
        EntityKind.Sphere => Loc.T("Сфера", "Sphere"),
        EntityKind.Box => Loc.T("Куб", "Box"),
        EntityKind.Torus => Loc.T("Тор", "Torus"),
        EntityKind.Cylinder => Loc.T("Цилиндр", "Cylinder"),
        EntityKind.Plane => Loc.T("Шахматный пол", "Checker floor"),
        _ => Loc.T("Объект", "Object"),
    };

    public static string KindTitle(EntityKind kind) => kind switch
    {
        EntityKind.Terrain => Loc.T("процедурный рельеф", "procedural terrain"),
        EntityKind.Water => Loc.T("водная гладь", "water plane"),
        EntityKind.Sphere => Loc.T("сфера", "sphere"),
        EntityKind.Box => Loc.T("куб", "box"),
        EntityKind.Torus => Loc.T("тор", "torus"),
        EntityKind.Cylinder => Loc.T("цилиндр", "cylinder"),
        EntityKind.Plane => Loc.T("шахматный пол", "checker floor"),
        _ => Loc.T("объект", "object"),
    };

    public Entity Clone()
    {
        var copy = (Entity)MemberwiseClone();
        copy.Terrain = Terrain.Clone();
        return copy;
    }
}

public sealed class SkySettings
{
    public Vector3 Zenith = new(0.035f, 0.06f, 0.085f);
    public Vector3 Horizon = new(0.16f, 0.24f, 0.23f);
    public Vector3 Ground = new(0.012f, 0.016f, 0.02f);

    public Vector3 SunColor = new(1.0f, 0.72f, 0.45f);
    public float SunIntensity = 1.6f;
    public float SunElevation = 7f;
    public float SunAzimuth = 55f;

    public float CloudCover = 0.55f;
    public float Stars = 0f;

    [JsonIgnore]
    public Vector3 SunDirection
    {
        get
        {
            float el = MathHelper.DegreesToRadians(SunElevation);
            float az = MathHelper.DegreesToRadians(SunAzimuth);
            return Vector3.Normalize(new Vector3(
                MathF.Cos(el) * MathF.Sin(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Cos(az)));
        }
    }
}

public sealed class FogSettings
{
    public bool Enabled = true;
    public Vector3 Color = new(0.09f, 0.145f, 0.14f);
    public float Density = 0.011f;
    public float HeightFalloff = 0.075f;
}

public sealed class PostSettings
{
    public float Exposure = 1.15f;
    public float Bloom = 0.55f;
    public float BloomThreshold = 0.85f;
    public float GodRays = 0.35f;

    public float Grain = 0.035f;
    public float Vignette = 0.45f;
    public float Saturation = 1.05f;
    public float Contrast = 1.04f;
    public float Chromatic = 0.35f;

    public Vector3 Tint = new(1f, 1f, 1f);
}

/// <summary>Вся сцена целиком. Сериализуется в .qutre (JSON).</summary>
public sealed class Scene
{
    public string Name = Loc.T("Безымянный мир", "Untitled World");
    public string Preset = "jungle";

    public List<Entity> Entities = new();

    public SkySettings Sky = new();
    public FogSettings Fog = new();
    public PostSettings Post = new();

    public bool Dust = true;
    public float DustDensity = 1f;

    public Entity FindEntity(string name) => Entities.FirstOrDefault(e => e.Name == name);
}
