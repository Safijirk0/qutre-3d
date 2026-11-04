using OpenTK.Mathematics;

namespace Qutre3D.Core;

public sealed class WorldPreset
{
    public string Id { get; init; } = "";
    public string TitleRu { get; init; } = "";
    public string TitleEn { get; init; } = "";
    public string HintRu { get; init; } = "";
    public string HintEn { get; init; } = "";
    public Action<Scene> Apply { get; init; } = _ => { };

    public string Title => Loc.T(TitleRu, TitleEn);
    public string Hint => Loc.T(HintRu, HintEn);
}

/// <summary>
/// Готовые «миры» — настроение в один клик.
/// Проще всего добавить свой: скопируй блок и поменяй числа/цвета.
/// </summary>
public static class Presets
{
    public static readonly List<WorldPreset> All = new()
    {
        new WorldPreset
        {
            Id = "bryce",
            TitleRu = "Bryce",
            TitleEn = "Bryce",
            HintRu = "электрическое небо, хром, стекло и шахматный пол",
            HintEn = "electric sky, chrome, glass and a checker floor",
            Apply = s =>
            {
                s.Sky.Zenith = new(0.015f, 0.070f, 0.420f);
                s.Sky.Horizon = new(0.100f, 0.520f, 0.960f);
                s.Sky.Ground = new(0.006f, 0.014f, 0.034f);
                s.Sky.SunColor = new(1.00f, 0.97f, 0.88f);
                s.Sky.SunIntensity = 2.80f;
                s.Sky.SunElevation = 34f;
                s.Sky.SunAzimuth = 25f;
                s.Sky.CloudCover = 0.26f;
                s.Sky.Stars = 0f;

                // Воздух в Bryce прозрачный: даль утопает в синеве, а не в мгле.
                s.Fog.Enabled = false;
                s.Fog.Color = new(0.28f, 0.60f, 0.95f);
                s.Fog.Density = 0f;
                s.Fog.HeightFalloff = 0.015f;

                s.Post.Exposure = 0.90f;
                s.Post.Bloom = 0.12f;
                s.Post.BloomThreshold = 1.35f;
                s.Post.GodRays = 0.00f;
                s.Post.Grain = 0.010f;
                s.Post.Vignette = 0.30f;
                s.Post.Saturation = 1.55f;
                s.Post.Contrast = 1.18f;
                s.Post.Chromatic = 0.14f;
                s.Post.Tint = new(1.00f, 1.00f, 1.02f);

                s.Dust = false;
                s.DustDensity = 0f;

                foreach (var e in s.Entities)
                {
                    if (e.Kind is EntityKind.Terrain or EntityKind.Water) e.Visible = false;

                    if (e.IsTerrain)
                    {
                        e.Terrain.LowColor = new(0.030f, 0.150f, 0.190f);
                        e.Terrain.HighColor = new(0.260f, 0.560f, 0.600f);
                        e.Terrain.RockColor = new(0.420f, 0.540f, 0.620f);
                        e.Terrain.MossColor = new(0.120f, 0.420f, 0.500f);
                    }
                }

                if (!s.Entities.Any(e => e.Kind == EntityKind.Plane))
                {
                    s.Entities.Insert(0, new Entity
                    {
                        Name = Entity.DefaultName(EntityKind.Plane),
                        Kind = EntityKind.Plane,
                        Position = new Vector3(0f, -0.06f, 0f),
                        Scale = new Vector3(6000f, 1f, 6000f),
                        Albedo = new Vector3(0.008f, 0.008f, 0.011f),
                        TileColor = new Vector3(0.660f, 0.650f, 0.600f),
                        TileSize = 9f,
                        Roughness = 0.16f,
                    });
                }
            },
        },
        new WorldPreset
        {
            Id = "jungle",
            TitleRu = "Джунгли",
            TitleEn = "Jungle",
            HintRu = "тёплый низкий свет, тяжёлая мгла",
            HintEn = "warm low light, heavy haze",
            Apply = s =>
            {
                s.Sky.Zenith = new(0.028f, 0.048f, 0.072f);
                s.Sky.Horizon = new(0.215f, 0.315f, 0.285f);
                s.Sky.Ground = new(0.020f, 0.032f, 0.028f);
                s.Sky.SunColor = new(1.00f, 0.80f, 0.58f);
                s.Sky.SunIntensity = 1.55f;
                s.Sky.SunElevation = 12f;
                s.Sky.SunAzimuth = 232f;
                s.Sky.CloudCover = 0.72f;
                s.Sky.Stars = 0f;

                s.Fog.Color = new(0.085f, 0.140f, 0.135f);
                s.Fog.Density = 0.0058f;
                s.Fog.HeightFalloff = 0.045f;

                s.Post.Exposure = 1.04f;
                s.Post.Bloom = 0.55f;
                s.Post.BloomThreshold = 0.95f;
                s.Post.GodRays = 0.40f;
                s.Post.Grain = 0.035f;
                s.Post.Vignette = 0.55f;
                s.Post.Saturation = 1.02f;
                s.Post.Contrast = 1.08f;
                s.Post.Chromatic = 0.30f;
                s.Post.Tint = new(1.02f, 1.00f, 0.98f);

                s.Dust = true;
                s.DustDensity = 1.0f;

                foreach (var e in s.Entities.Where(e => e.IsTerrain))
                {
                    e.Terrain.LowColor = new(0.030f, 0.062f, 0.045f);
                    e.Terrain.HighColor = new(0.120f, 0.180f, 0.110f);
                    e.Terrain.RockColor = new(0.130f, 0.130f, 0.115f);
                    e.Terrain.MossColor = new(0.055f, 0.200f, 0.105f);
                }
            },
        },
        new WorldPreset
        {
            Id = "breakcore",
            TitleRu = "Breakcore",
            TitleEn = "Breakcore",
            HintRu = "кислотный закат, зерно, аберрация",
            HintEn = "acid sunset, grain, aberration",
            Apply = s =>
            {
                s.Sky.Zenith = new(0.060f, 0.020f, 0.090f);
                s.Sky.Horizon = new(0.550f, 0.180f, 0.220f);
                s.Sky.Ground = new(0.020f, 0.010f, 0.030f);
                s.Sky.SunColor = new(1.00f, 0.55f, 0.35f);
                s.Sky.SunIntensity = 2.40f;
                s.Sky.SunElevation = 3f;
                s.Sky.SunAzimuth = 120f;
                s.Sky.CloudCover = 0.70f;
                s.Sky.Stars = 0.10f;

                s.Fog.Color = new(0.280f, 0.090f, 0.160f);
                s.Fog.Density = 0.0140f;
                s.Fog.HeightFalloff = 0.050f;

                s.Post.Exposure = 1.04f;
                s.Post.Bloom = 0.90f;
                s.Post.BloomThreshold = 0.70f;
                s.Post.GodRays = 0.60f;
                s.Post.Grain = 0.060f;
                s.Post.Vignette = 0.55f;
                s.Post.Saturation = 1.15f;
                s.Post.Contrast = 1.10f;
                s.Post.Chromatic = 0.70f;
                s.Post.Tint = new(1.05f, 0.97f, 1.02f);

                s.Dust = true;
                s.DustDensity = 1.2f;

                foreach (var e in s.Entities.Where(e => e.IsTerrain))
                {
                    e.Terrain.LowColor = new(0.060f, 0.030f, 0.070f);
                    e.Terrain.HighColor = new(0.220f, 0.120f, 0.200f);
                    e.Terrain.RockColor = new(0.160f, 0.130f, 0.180f);
                    e.Terrain.MossColor = new(0.350f, 0.100f, 0.300f);
                }
            },
        },
        new WorldPreset
        {
            Id = "noir",
            TitleRu = "Нуар",
            TitleEn = "Noir",
            HintRu = "холод, туман, минимум цвета",
            HintEn = "cold, fog, almost no colour",
            Apply = s =>
            {
                s.Sky.Zenith = new(0.020f, 0.030f, 0.045f);
                s.Sky.Horizon = new(0.220f, 0.270f, 0.300f);
                s.Sky.Ground = new(0.008f, 0.010f, 0.012f);
                s.Sky.SunColor = new(0.75f, 0.82f, 0.90f);
                s.Sky.SunIntensity = 0.95f;
                s.Sky.SunElevation = 14f;
                s.Sky.SunAzimuth = 210f;
                s.Sky.CloudCover = 0.85f;
                s.Sky.Stars = 0.15f;

                s.Fog.Color = new(0.130f, 0.160f, 0.180f);
                s.Fog.Density = 0.0160f;
                s.Fog.HeightFalloff = 0.060f;

                s.Post.Exposure = 0.95f;
                s.Post.Bloom = 0.35f;
                s.Post.BloomThreshold = 0.95f;
                s.Post.GodRays = 0.20f;
                s.Post.Grain = 0.080f;
                s.Post.Vignette = 0.60f;
                s.Post.Saturation = 0.55f;
                s.Post.Contrast = 1.12f;
                s.Post.Chromatic = 0.25f;
                s.Post.Tint = new(0.95f, 1.00f, 1.05f);

                s.Dust = true;
                s.DustDensity = 0.6f;

                foreach (var e in s.Entities.Where(e => e.IsTerrain))
                {
                    e.Terrain.LowColor = new(0.050f, 0.060f, 0.070f);
                    e.Terrain.HighColor = new(0.170f, 0.190f, 0.205f);
                    e.Terrain.RockColor = new(0.200f, 0.210f, 0.220f);
                    e.Terrain.MossColor = new(0.090f, 0.130f, 0.120f);
                }
            },
        },
        new WorldPreset
        {
            Id = "ember",
            TitleRu = "Угли",
            TitleEn = "Ember",
            HintRu = "чёрный базальт и рыжий свет",
            HintEn = "black basalt and ember light",
            Apply = s =>
            {
                s.Sky.Zenith = new(0.030f, 0.012f, 0.008f);
                s.Sky.Horizon = new(0.300f, 0.080f, 0.030f);
                s.Sky.Ground = new(0.020f, 0.006f, 0.004f);
                s.Sky.SunColor = new(1.00f, 0.42f, 0.16f);
                s.Sky.SunIntensity = 2.20f;
                s.Sky.SunElevation = 2f;
                s.Sky.SunAzimuth = 300f;
                s.Sky.CloudCover = 0.40f;
                s.Sky.Stars = 0.05f;

                s.Fog.Color = new(0.160f, 0.045f, 0.020f);
                s.Fog.Density = 0.0130f;
                s.Fog.HeightFalloff = 0.090f;

                s.Post.Exposure = 1.04f;
                s.Post.Bloom = 0.95f;
                s.Post.BloomThreshold = 0.65f;
                s.Post.GodRays = 0.80f;
                s.Post.Grain = 0.050f;
                s.Post.Vignette = 0.60f;
                s.Post.Saturation = 1.10f;
                s.Post.Contrast = 1.08f;
                s.Post.Chromatic = 0.50f;
                s.Post.Tint = new(1.05f, 0.98f, 0.95f);

                s.Dust = true;
                s.DustDensity = 1.4f;

                foreach (var e in s.Entities.Where(e => e.IsTerrain))
                {
                    e.Terrain.LowColor = new(0.035f, 0.028f, 0.026f);
                    e.Terrain.HighColor = new(0.120f, 0.085f, 0.070f);
                    e.Terrain.RockColor = new(0.100f, 0.090f, 0.085f);
                    e.Terrain.MossColor = new(0.220f, 0.090f, 0.030f);
                }
            },
        },
        new WorldPreset
        {
            Id = "void",
            TitleRu = "Пустота",
            TitleEn = "Void",
            HintRu = "почти ночь, звёзды, тишина",
            HintEn = "almost night, stars, silence",
            Apply = s =>
            {
                s.Sky.Zenith = new(0.008f, 0.010f, 0.016f);
                s.Sky.Horizon = new(0.030f, 0.040f, 0.050f);
                s.Sky.Ground = new(0.002f, 0.003f, 0.004f);
                s.Sky.SunColor = new(0.60f, 0.70f, 0.80f);
                s.Sky.SunIntensity = 0.55f;
                s.Sky.SunElevation = 22f;
                s.Sky.SunAzimuth = 40f;
                s.Sky.CloudCover = 0.05f;
                s.Sky.Stars = 1.0f;

                s.Fog.Color = new(0.020f, 0.028f, 0.035f);
                s.Fog.Density = 0.0060f;
                s.Fog.HeightFalloff = 0.030f;

                s.Post.Exposure = 1.20f;
                s.Post.Bloom = 0.30f;
                s.Post.BloomThreshold = 1.00f;
                s.Post.GodRays = 0.15f;
                s.Post.Grain = 0.040f;
                s.Post.Vignette = 0.60f;
                s.Post.Saturation = 0.90f;
                s.Post.Contrast = 1.00f;
                s.Post.Chromatic = 0.20f;
                s.Post.Tint = new(1f, 1f, 1f);

                s.Dust = true;
                s.DustDensity = 0.5f;

                foreach (var e in s.Entities.Where(e => e.IsTerrain))
                {
                    e.Terrain.LowColor = new(0.020f, 0.025f, 0.030f);
                    e.Terrain.HighColor = new(0.080f, 0.090f, 0.100f);
                    e.Terrain.RockColor = new(0.100f, 0.105f, 0.110f);
                    e.Terrain.MossColor = new(0.030f, 0.060f, 0.070f);
                }
            },
        },
    };

    public static WorldPreset Get(string id) => All.FirstOrDefault(p => p.Id == id) ?? All[0];

    public static void Apply(Scene scene, string id)
    {
        var preset = Get(id);
        bool bryce = preset.Id == "bryce";

        // пресет полностью отвечает за «сценографию»: рельеф и вода
        // возвращаются в кадр, если другой пресет их прятал; шахматный
        // пол живёт только в мире Bryce и не должен перекрывать рельеф
        foreach (var entity in scene.Entities)
        {
            if (entity.Kind is EntityKind.Terrain or EntityKind.Water)
                entity.Visible = true;

            if (entity.Kind == EntityKind.Plane)
                entity.Visible = bryce;
        }

        preset.Apply(scene);
        scene.Preset = preset.Id;
    }
}

/// <summary>Сборка стартовой сцены.</summary>
public static class SceneFactory
{
    public static Scene CreateDefault(string presetId = "bryce")
    {
        var scene = new Scene();

        if (presetId == "bryce")
        {
            AddBryceDemo(scene);
            Presets.Apply(scene, presetId);
            return scene;
        }

        scene.Entities.Add(new Entity
        {
            Name = Entity.DefaultName(EntityKind.Terrain),
            Kind = EntityKind.Terrain,
            Terrain = new TerrainSettings
            {
                Seed = 1337,
                Size = 170f,
                Height = 26f,
                Frequency = 2.1f,
                Octaves = 7,
                Warp = 0.85f,
                Resolution = 260,
            },
        });

        scene.Entities.Add(new Entity
        {
            Name = Entity.DefaultName(EntityKind.Water),
            Kind = EntityKind.Water,
            Position = new Vector3(0f, 0.0f, 0f),
            Albedo = new Vector3(0.025f, 0.095f, 0.105f),
            Roughness = 0.08f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Loc.T("Монолит", "Monolith"),
            Kind = EntityKind.Box,
            Position = new Vector3(-16f, 11f, -10f),
            Rotation = new Vector3(0f, 18f, 4f),
            Scale = new Vector3(2.6f, 22f, 2.6f),
            Albedo = new Vector3(0.13f, 0.15f, 0.14f),
            Roughness = 0.85f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Entity.DefaultName(EntityKind.Sphere),
            Kind = EntityKind.Sphere,
            Position = new Vector3(22f, 7.5f, -18f),
            Scale = new Vector3(7.5f),
            Albedo = new Vector3(0.10f, 0.16f, 0.14f),
            Roughness = 0.55f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Entity.DefaultName(EntityKind.Torus),
            Kind = EntityKind.Torus,
            Position = new Vector3(6f, 16f, 12f),
            Rotation = new Vector3(28f, 0f, 20f),
            Scale = new Vector3(5.5f),
            Albedo = new Vector3(0.20f, 0.17f, 0.13f),
            Roughness = 0.45f,
            Metallic = 0.3f,
        });

        Presets.Apply(scene, presetId);
        return scene;
    }

    /// <summary>
    /// Стартовая сцена в духе Bryce: бесконечный шахматный пол, хром и стекло
    /// на фоне электрического неба. Рельеф и вода здесь есть, но пресет
    /// прячет их — переключись на «Джунгли», и остров вернётся.
    /// </summary>
    private static void AddBryceDemo(Scene scene)
    {
        scene.Name = Loc.T("Электрический мир", "Electric World");

        scene.Entities.Add(new Entity
        {
            Name = Entity.DefaultName(EntityKind.Terrain),
            Kind = EntityKind.Terrain,
            Position = new Vector3(0f, -1.5f, -540f),
            Terrain = new TerrainSettings
            {
                Seed = 8123,
                Size = 620f,
                Height = 34f,
                Frequency = 1.7f,
                Octaves = 6,
                Warp = 0.7f,
                Resolution = 200,
            },
        });

        scene.Entities.Add(new Entity
        {
            Name = Entity.DefaultName(EntityKind.Water),
            Kind = EntityKind.Water,
            Position = new Vector3(0f, -1.2f, 0f),
            Albedo = new Vector3(0.030f, 0.180f, 0.230f),
            Roughness = 0.10f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Entity.DefaultName(EntityKind.Plane),
            Kind = EntityKind.Plane,
            Position = new Vector3(0f, -0.06f, 0f),
            Scale = new Vector3(6000f, 1f, 6000f),
            Albedo = new Vector3(0.008f, 0.008f, 0.011f),
            TileColor = new Vector3(0.660f, 0.650f, 0.600f),
            TileSize = 9f,
            Roughness = 0.16f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Loc.T("Хромовая сфера", "Chrome sphere"),
            Kind = EntityKind.Sphere,
            Position = new Vector3(-11f, 9f, -2f),
            Scale = new Vector3(9f),
            Albedo = new Vector3(0.90f, 0.92f, 0.95f),
            Roughness = 0.06f,
            Metallic = 1f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Loc.T("Стеклянный тор", "Glass torus"),
            Kind = EntityKind.Torus,
            Position = new Vector3(12f, 16f, 4f),
            Rotation = new Vector3(64f, 0f, 20f),
            Scale = new Vector3(7.5f),
            Albedo = new Vector3(0.35f, 0.82f, 0.94f),
            Roughness = 0.05f,
            Metallic = 0.12f,
            Opacity = 0.30f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Loc.T("Золотой обелиск", "Golden obelisk"),
            Kind = EntityKind.Box,
            Position = new Vector3(-33f, 11f, -4f),
            Rotation = new Vector3(0f, 26f, 3f),
            Scale = new Vector3(2.8f, 22f, 2.8f),
            Albedo = new Vector3(1.00f, 0.62f, 0.10f),
            Roughness = 0.15f,
            Metallic = 1f,
        });

        scene.Entities.Add(new Entity
        {
            Name = Loc.T("Алая сфера", "Crimson sphere"),
            Kind = EntityKind.Sphere,
            Position = new Vector3(19f, 5f, -15f),
            Scale = new Vector3(5f),
            Albedo = new Vector3(0.78f, 0.055f, 0.075f),
            Roughness = 0.16f,
            Metallic = 0.25f,
        });
    }
}




