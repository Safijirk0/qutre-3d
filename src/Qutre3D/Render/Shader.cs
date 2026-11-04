using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Qutre3D.Render;

/// <summary>Тонкая обёртка над GL-программой с кешем uniform-локаций.</summary>
public sealed class Shader : IDisposable
{
    private readonly Dictionary<string, int> _uniforms = new();
    private bool _disposed;

    public int Handle { get; }

    public Shader(string vertexSource, string fragmentSource)
    {
        int vs = Compile(ShaderType.VertexShader, vertexSource, "vertex");
        int fs = Compile(ShaderType.FragmentShader, fragmentSource, "fragment");

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vs);
        GL.AttachShader(Handle, fs);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int ok);
        if (ok == 0)
        {
            string log = GL.GetProgramInfoLog(Handle);
            GL.DeleteProgram(Handle);
            throw new InvalidOperationException("Ошибка линковки шейдера: " + log);
        }

        GL.DetachShader(Handle, vs);
        GL.DetachShader(Handle, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
    }

    private static int Compile(ShaderType type, string source, string label)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source.TrimStart());
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0)
        {
            string log = GL.GetShaderInfoLog(shader);
            string dump = Path.Combine(Path.GetTempPath(), $"qutre_{label}_fail.glsl");
            try { File.WriteAllText(dump, source.TrimStart()); } catch { }
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Ошибка компиляции {label}-шейдера ({dump}):\n{log}");
        }
        return shader;
    }

    public void Use() => GL.UseProgram(Handle);

    private int Location(string name)
    {
        if (_uniforms.TryGetValue(name, out int loc)) return loc;
        loc = GL.GetUniformLocation(Handle, name);
        _uniforms[name] = loc;
        return loc;
    }

    public void Set(string name, float value) => GL.Uniform1(Location(name), value);
    public void Set(string name, int value) => GL.Uniform1(Location(name), value);
    public void Set(string name, bool value) => GL.Uniform1(Location(name), value ? 1 : 0);

    public void Set(string name, Vector2 value) => GL.Uniform2(Location(name), value);
    public void Set(string name, Vector3 value) => GL.Uniform3(Location(name), value);

    public void Set(string name, Vector4 value) => GL.Uniform4(Location(name), value);

    public void Set(string name, Matrix4 value)
    {
        int loc = Location(name);
        GL.UniformMatrix4(loc, false, ref value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GL.DeleteProgram(Handle);
    }
}
