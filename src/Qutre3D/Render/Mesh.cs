using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Qutre3D.Render;

/// <summary>
/// Меш: позиция(3) + нормаль(3) + uv(2) + высота(1) = 9 float на вершину.
/// Для линий — только позиция(3).
/// </summary>
public sealed class Mesh : IDisposable
{
    private bool _disposed;

    public int Vao { get; }
    public int Vbo { get; }
    public int Ebo { get; }
    public int IndexCount { get; }

    private Mesh(int vao, int vbo, int ebo, int indexCount)
    {
        Vao = vao;
        Vbo = vbo;
        Ebo = ebo;
        IndexCount = indexCount;
    }

    public static Mesh Create(float[] vertices, uint[] indices)
    {
        const int stride = 9 * sizeof(float);

        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        int ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, 8 * sizeof(float));

        GL.BindVertexArray(0);

        return new Mesh(vao, vbo, ebo, indices.Length);
    }

    public static Mesh CreateLines(float[] positions, uint[] indices)
    {
        int vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, positions.Length * sizeof(float), positions, BufferUsageHint.StaticDraw);

        int ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        GL.BindVertexArray(0);
        return new Mesh(vao, vbo, ebo, indices.Length);
    }

    public void Draw()
    {
        GL.BindVertexArray(Vao);
        GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0);
    }

    public void DrawLines()
    {
        GL.BindVertexArray(Vao);
        GL.DrawElements(PrimitiveType.Lines, IndexCount, DrawElementsType.UnsignedInt, 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GL.DeleteVertexArray(Vao);
        GL.DeleteBuffer(Vbo);
        GL.DeleteBuffer(Ebo);
    }
}
