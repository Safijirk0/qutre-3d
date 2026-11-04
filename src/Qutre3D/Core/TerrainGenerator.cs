using OpenTK.Mathematics;

namespace Qutre3D.Core;

/// <summary>
/// Процедурный рельеф. Height01(u,v) — базовая функция высоты, её же
/// использует генератор меша. Меняй смело: это сердце «брисовских» пейзажей.
/// </summary>
public static class TerrainGenerator
{
    public static float Height01(float u, float v, TerrainSettings t)
    {
        int seed = t.Seed;

        // Доменное искривление — рельеф перестаёт быть «шумным блином».
        float warpX = (Noise.Fbm(u * 2.3f + 11.2f, v * 2.3f - 7.4f, 3, seed + 31) - 0.5f) * 2f;
        float warpZ = (Noise.Fbm(u * 2.3f - 5.1f, v * 2.3f + 3.7f, 3, seed + 57) - 0.5f) * 2f;

        float x = u * t.Frequency + warpX * t.Warp;
        float z = v * t.Frequency + warpZ * t.Warp;

        float baseH = Noise.Fbm(x, z, Math.Clamp(t.Octaves, 1, 8), seed);
        float ridge = Noise.Ridged(x * 0.62f + 4.2f, z * 0.62f - 2.8f, 4, seed + 7);

        float h = baseH * 0.38f + ridge * 0.84f;

        // Остров: края уходят под воду.
        float r = MathF.Sqrt(u * u + v * v);
        float mask = 1f - Noise.Smoothstep(0.58f, 1.05f, r);
        h *= mask * mask;

        // Берега плоские, гребни резкие.
        h = MathF.Pow(Math.Clamp(h, 0f, 1.25f) / 1.25f, 1.15f);

        // Тонкая эрозия: прожилки на склонах.
        h += (Noise.Fbm(u * 14f + 3.1f, v * 14f - 8.7f, 3, seed + 91) - 0.5f) * 0.035f * h;

        return Math.Clamp(h, 0f, 1f);
    }

    /// <summary>Нормаль рельефа в локальных координатах меша.</summary>
    public static Vector3 NormalAt(float u, float v, TerrainSettings t)
    {
        const float e = 0.004f;
        float hL = Height01(u - e, v, t) * t.Height;
        float hR = Height01(u + e, v, t) * t.Height;
        float hD = Height01(u, v - e, t) * t.Height;
        float hU = Height01(u, v + e, t) * t.Height;

        float dx = e * t.Size;
        float dz = e * t.Size;

        var n = new Vector3(-(hR - hL) / (2f * dx), 1f, -(hU - hD) / (2f * dz));
        return Vector3.Normalize(n);
    }
}
