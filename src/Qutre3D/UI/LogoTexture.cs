using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using Qutre3D.App;

namespace Qutre3D.UI;

/// <summary>
/// Логотип как GL-текстура для интерфейса. Хранит UV-обрезку чёрной рамки исходника.
/// </summary>
public sealed class LogoTexture : IDisposable
{
    public int Handle { get; private set; }
    public Vector2 Uv0 { get; private set; } = new(0f, 1f);
    public Vector2 Uv1 { get; private set; } = new(1f, 0f);
    public float Aspect { get; private set; } = 1.27f;
    public bool Ready => Handle != 0;

    public void Load()
    {
        if (Handle != 0) return;

        try
        {
            string path = LogoAssets.FindLogoPath();
            if (path is null) return;

            var image = LogoAssets.LoadImage(path);
            var art = LogoAssets.FindArtBounds(image);

            int x0 = Math.Clamp(art.Left, 0, image.Width - 1);
            int y0 = Math.Clamp(art.Top, 0, image.Height - 1);
            int x1 = Math.Clamp(art.Right, x0 + 1, image.Width);
            int y1 = Math.Clamp(art.Bottom, y0 + 1, image.Height);

            Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Rgba);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            float u0 = x0 / (float)image.Width;
            float u1 = x1 / (float)image.Width;
            float v0 = 1f - y1 / (float)image.Height;
            float v1 = 1f - y0 / (float)image.Height;

            Uv0 = new Vector2(u0, v0);
            Uv1 = new Vector2(u1, v1);
            Aspect = (x1 - x0) / (float)(y1 - y0);
        }
        catch
        {
            if (Handle != 0)
            {
                GL.DeleteTexture(Handle);
                Handle = 0;
            }
        }
    }

    /// <summary>Рисует логотип заданной высоты, сохраняя пропорции.</summary>
    public void Draw(float height, Vector4 tint)
    {
        if (!Ready) return;

        var size = new Vector2(height * Aspect, height);
        ImGui.Image((IntPtr)Handle, size, Uv0, Uv1, tint);
    }

    public void Draw(float height) => Draw(height, Vector4.One);

    public void Dispose()
    {
        if (Handle == 0) return;

        GL.DeleteTexture(Handle);
        Handle = 0;
    }
}
