using OpenTK.Graphics.OpenGL4;

namespace Qutre3D.Render;

/// <summary>Оффскрин-таргет: цветовая текстура (+ буфер глубины).</summary>
public sealed class Framebuffer : IDisposable
{
    private bool _disposed;
    private readonly bool _hdr;
    private readonly bool _withDepth;

    public int Handle { get; private set; }
    public int ColorTexture { get; private set; }
    public int DepthBuffer { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Framebuffer(int width, int height, bool hdr, bool withDepth)
    {
        _hdr = hdr;
        _withDepth = withDepth;
        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);
        Create();
    }

    private void Create()
    {
        Handle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

        ColorTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, _hdr ? PixelInternalFormat.Rgba16f : PixelInternalFormat.Rgba8,
            Width, Height, 0, PixelFormat.Rgba, _hdr ? PixelType.Float : PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, ColorTexture, 0);

        if (_withDepth)
        {
            DepthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, Width, Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, DepthBuffer);
        }

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException("Framebuffer не собран: " + status);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Resize(int width, int height)
    {
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);
        if (width == Width && height == Height) return;

        Destroy();
        Width = width;
        Height = height;
        Create();
    }

    public void Bind() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

    public static void BindDefault() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

    private void Destroy()
    {
        if (Handle != 0) GL.DeleteFramebuffer(Handle);
        if (ColorTexture != 0) GL.DeleteTexture(ColorTexture);
        if (DepthBuffer != 0) GL.DeleteRenderbuffer(DepthBuffer);
        Handle = ColorTexture = DepthBuffer = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Destroy();
    }
}
