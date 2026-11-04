namespace Qutre3D.App;

/// <summary>Простейший контейнер RGBA-пикселей (строки сверху вниз).</summary>
public sealed class ImageData
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public ImageData(int width, int height, byte[] rgba)
    {
        Width = width;
        Height = height;
        Rgba = rgba;
    }

    public int Index(int x, int y) => (y * Width + x) * 4;
}
