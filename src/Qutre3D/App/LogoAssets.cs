using System.Drawing;
using OpenTK.Windowing.Common.Input;
using StbImageSharp;
using GlfwImage = OpenTK.Windowing.Common.Input.Image;

namespace Qutre3D.App;

/// <summary>
/// Брендинг приложения: поиск файла логотипа, декодирование и сборка иконки окна.
/// Всё необязательное — если файла нет, редактор просто работает без картинки.
/// </summary>
public static class LogoAssets
{
    public const string RelativePath = "assets/logo.png";

    /// <summary>Ищем логотип рядом с exe, а если запускаемся из IDE — поднимаемся к корню репозитория.</summary>
    public static string FindLogoPath()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "assets", "logo.png"),
            Path.Combine(AppContext.BaseDirectory, "logo.png"),
        };

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
            candidates.Add(Path.Combine(dir.FullName, RelativePath));

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>Декодирует PNG/JPG в RGBA через StbImageSharp (кроссплатформенно).</summary>
    public static ImageData LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        if (result is null || result.Data is null || result.Width <= 0 || result.Height <= 0)
            throw new InvalidDataException("Unsupported image: " + path);

        return new ImageData(result.Width, result.Height, result.Data);
    }

    /// <summary>Границы «полезной» картинки без чёрной рамки исходника.</summary>
    public static Rectangle FindArtBounds(ImageData image)
    {
        const int threshold = 12;
        int minX = image.Width, minY = image.Height, maxX = -1, maxY = -1;

        for (int y = 0; y < image.Height; y++)
        {
            int row = y * image.Width * 4;
            for (int x = 0; x < image.Width; x++)
            {
                int i = row + x * 4;
                if (image.Rgba[i] <= threshold && image.Rgba[i + 1] <= threshold && image.Rgba[i + 2] <= threshold)
                    continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return new Rectangle(0, 0, image.Width, image.Height);

        // пара пикселей воздуха, чтобы не обрезать сглаженный край
        const int pad = 2;
        minX = Math.Max(0, minX - pad);
        minY = Math.Max(0, minY - pad);
        maxX = Math.Min(image.Width - 1, maxX + pad);
        maxY = Math.Min(image.Height - 1, maxY + pad);

        return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    /// <summary>Квадратный кроп вокруг планеты — чтобы иконка читалась в 16×16.</summary>
    public static Rectangle IconCrop(Rectangle art)
    {
        int side = Math.Max(8, (int)(art.Height * 0.675f));
        side = Math.Min(side, Math.Min(art.Width, art.Height));

        int cx = art.Left + (int)(art.Width * 0.49f);
        int cy = art.Top + (int)(art.Height * 0.30f);

        int x = Math.Clamp(cx - side / 2, art.Left, art.Right - side);
        int y = Math.Clamp(cy - side / 2, art.Top, art.Bottom - side);
        return new Rectangle(x, y, side, side);
    }

    public static WindowIcon TryLoadWindowIcon()
    {
        try
        {
            string path = FindLogoPath();
            if (path is null) return null;

            var source = LoadImage(path);
            var crop = IconCrop(FindArtBounds(source));

            int[] sizes = { 256, 64, 48, 32, 16 };
            var images = new List<GlfwImage>(sizes.Length);

            foreach (int size in sizes)
                images.Add(new GlfwImage(size, size, Resize(source, crop, size)));

            return new WindowIcon(images.ToArray());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Усредняющее уменьшение выбранного прямоугольника до size×size.
    /// Достаточно качественно для иконок и не тянет за собой GDI+.
    /// </summary>
    public static byte[] Resize(ImageData source, Rectangle crop, int size)
    {
        var dest = new byte[size * size * 4];

        for (int dy = 0; dy < size; dy++)
        {
            int sy0 = crop.Top + dy * crop.Height / size;
            int sy1 = crop.Top + (dy + 1) * crop.Height / size;
            if (sy1 <= sy0) sy1 = Math.Min(sy0 + 1, crop.Bottom);

            for (int dx = 0; dx < size; dx++)
            {
                int sx0 = crop.Left + dx * crop.Width / size;
                int sx1 = crop.Left + (dx + 1) * crop.Width / size;
                if (sx1 <= sx0) sx1 = Math.Min(sx0 + 1, crop.Right);

                long r = 0, g = 0, b = 0, a = 0;
                int count = 0;

                for (int sy = sy0; sy < sy1; sy++)
                {
                    for (int sx = sx0; sx < sx1; sx++)
                    {
                        int i = source.Index(sx, sy);
                        r += source.Rgba[i + 0];
                        g += source.Rgba[i + 1];
                        b += source.Rgba[i + 2];
                        a += source.Rgba[i + 3];
                        count++;
                    }
                }

                int o = (dy * size + dx) * 4;
                if (count > 0)
                {
                    dest[o + 0] = (byte)(r / count);
                    dest[o + 1] = (byte)(g / count);
                    dest[o + 2] = (byte)(b / count);
                    dest[o + 3] = (byte)(a / count);
                }
            }
        }

        return dest;
    }
}
