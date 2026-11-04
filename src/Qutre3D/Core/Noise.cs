namespace Qutre3D.Core;

/// <summary>
/// Крошечная библиотека процедурного шума.
/// Без зависимостей: целочисленный хеш + сглаженный value-noise + fBm.
/// </summary>
public static class Noise
{
    public static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)x * 0x9E3779B1u;
            h ^= (uint)y * 0x85EBCA77u;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    public static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Гладкий 2D value-noise, значения 0..1.</summary>
    public static float Value(float x, float y, int seed)
    {
        int xi = (int)MathF.Floor(x);
        int yi = (int)MathF.Floor(y);
        float fx = x - xi;
        float fy = y - yi;

        float ux = fx * fx * (3f - 2f * fx);
        float uy = fy * fy * (3f - 2f * fy);

        float a = Hash(xi, yi, seed);
        float b = Hash(xi + 1, yi, seed);
        float c = Hash(xi, yi + 1, seed);
        float d = Hash(xi + 1, yi + 1, seed);

        return Lerp(Lerp(a, b, ux), Lerp(c, d, ux), uy);
    }

    /// <summary>Фрактальная сумма октав, примерно 0..1.</summary>
    public static float Fbm(float x, float y, int octaves, int seed, float lacunarity = 2.03f, float gain = 0.5f)
    {
        float sum = 0f;
        float amp = 0.5f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            sum += amp * Value(x, y, seed + i * 101);
            norm += amp;
            x = x * lacunarity + 17.3f;
            y = y * lacunarity - 9.1f;
            amp *= gain;
        }

        return norm > 0f ? sum / norm : 0f;
    }

    /// <summary>Хребтовый шум — даёт горные гребни.</summary>
    public static float Ridged(float x, float y, int octaves, int seed)
    {
        float sum = 0f;
        float amp = 0.5f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float n = Value(x, y, seed + i * 71);
            n = 1f - MathF.Abs(n * 2f - 1f);
            sum += amp * n * n;
            norm += amp;
            x = x * 2.07f + 5.2f;
            y = y * 2.07f - 3.4f;
            amp *= 0.5f;
        }

        return norm > 0f ? sum / norm : 0f;
    }
}
