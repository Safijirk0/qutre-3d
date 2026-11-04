using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Qutre3D.UI;

namespace Qutre3D.App;

/// <summary>
/// Минимальный бэкенд ImGui для OpenTK: шрифты, ввод, отрисовка.
/// Здесь обычно ничего трогать не нужно.
/// </summary>
public sealed class ImGuiController : IDisposable
{
    private readonly GameWindow _window;

    private int _vertexArray;
    private int _vertexBuffer;
    private int _indexBuffer;
    private int _shader;
    private int _shaderTexture;
    private int _shaderProjection;
    private int _fontTexture;

    private bool _frameBegun;
    private bool _disposed;

    private Vector2 _scrollDelta;
    private readonly HashSet<ImGuiKey> _keysDown = new();

    private static readonly Dictionary<Keys, ImGuiKey> KeyMap = BuildKeyMap();

    public ImGuiController(GameWindow window)
    {
        _window = window;

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        Theme.ApplyStyle();
        Theme.LoadFonts(io);

        // Колесо ловим событием, а не опросом состояния: опрос возвращает накопленный
        // скролл и применял бы его каждый кадр (камера/панели «ехали» без остановки).
        window.MouseWheel += OnMouseWheel;

        CreateDeviceResources();
        CreateFontsTexture();

        io.DisplaySize = new System.Numerics.Vector2(window.ClientSize.X, window.ClientSize.Y);
    }

    private void OnMouseWheel(MouseWheelEventArgs e)
    {
        _scrollDelta += new Vector2(e.OffsetX, e.OffsetY);
    }

    private static Dictionary<Keys, ImGuiKey> BuildKeyMap()
    {
        var map = new Dictionary<Keys, ImGuiKey>
        {
            [Keys.Tab] = ImGuiKey.Tab,
            [Keys.Left] = ImGuiKey.LeftArrow,
            [Keys.Right] = ImGuiKey.RightArrow,
            [Keys.Up] = ImGuiKey.UpArrow,
            [Keys.Down] = ImGuiKey.DownArrow,
            [Keys.PageUp] = ImGuiKey.PageUp,
            [Keys.PageDown] = ImGuiKey.PageDown,
            [Keys.Home] = ImGuiKey.Home,
            [Keys.End] = ImGuiKey.End,
            [Keys.Insert] = ImGuiKey.Insert,
            [Keys.Delete] = ImGuiKey.Delete,
            [Keys.Backspace] = ImGuiKey.Backspace,
            [Keys.Space] = ImGuiKey.Space,
            [Keys.Enter] = ImGuiKey.Enter,
            [Keys.KeyPadEnter] = ImGuiKey.KeypadEnter,
            [Keys.Escape] = ImGuiKey.Escape,
            [Keys.Menu] = ImGuiKey.Menu,
            [Keys.Minus] = ImGuiKey.Minus,
            [Keys.Equal] = ImGuiKey.Equal,
            [Keys.LeftBracket] = ImGuiKey.LeftBracket,
            [Keys.RightBracket] = ImGuiKey.RightBracket,
            [Keys.Semicolon] = ImGuiKey.Semicolon,
            [Keys.Apostrophe] = ImGuiKey.Apostrophe,
            [Keys.Comma] = ImGuiKey.Comma,
            [Keys.Period] = ImGuiKey.Period,
            [Keys.Slash] = ImGuiKey.Slash,
            [Keys.Backslash] = ImGuiKey.Backslash,
            [Keys.GraveAccent] = ImGuiKey.GraveAccent,
            [Keys.LeftShift] = ImGuiKey.LeftShift,
            [Keys.RightShift] = ImGuiKey.RightShift,
            [Keys.LeftControl] = ImGuiKey.LeftCtrl,
            [Keys.RightControl] = ImGuiKey.RightCtrl,
            [Keys.LeftAlt] = ImGuiKey.LeftAlt,
            [Keys.RightAlt] = ImGuiKey.RightAlt,
            [Keys.LeftSuper] = ImGuiKey.LeftSuper,
            [Keys.RightSuper] = ImGuiKey.RightSuper,
            [Keys.A] = ImGuiKey.A, [Keys.B] = ImGuiKey.B, [Keys.C] = ImGuiKey.C, [Keys.D] = ImGuiKey.D,
            [Keys.E] = ImGuiKey.E, [Keys.F] = ImGuiKey.F, [Keys.G] = ImGuiKey.G, [Keys.H] = ImGuiKey.H,
            [Keys.I] = ImGuiKey.I, [Keys.J] = ImGuiKey.J, [Keys.K] = ImGuiKey.K, [Keys.L] = ImGuiKey.L,
            [Keys.M] = ImGuiKey.M, [Keys.N] = ImGuiKey.N, [Keys.O] = ImGuiKey.O, [Keys.P] = ImGuiKey.P,
            [Keys.Q] = ImGuiKey.Q, [Keys.R] = ImGuiKey.R, [Keys.S] = ImGuiKey.S, [Keys.T] = ImGuiKey.T,
            [Keys.U] = ImGuiKey.U, [Keys.V] = ImGuiKey.V, [Keys.W] = ImGuiKey.W, [Keys.X] = ImGuiKey.X,
            [Keys.Y] = ImGuiKey.Y, [Keys.Z] = ImGuiKey.Z,
            [Keys.D0] = ImGuiKey._0, [Keys.D1] = ImGuiKey._1, [Keys.D2] = ImGuiKey._2,
            [Keys.D3] = ImGuiKey._3, [Keys.D4] = ImGuiKey._4, [Keys.D5] = ImGuiKey._5,
            [Keys.D6] = ImGuiKey._6, [Keys.D7] = ImGuiKey._7, [Keys.D8] = ImGuiKey._8,
            [Keys.D9] = ImGuiKey._9,
            [Keys.F1] = ImGuiKey.F1, [Keys.F2] = ImGuiKey.F2, [Keys.F3] = ImGuiKey.F3, [Keys.F4] = ImGuiKey.F4,
            [Keys.F5] = ImGuiKey.F5, [Keys.F6] = ImGuiKey.F6, [Keys.F7] = ImGuiKey.F7, [Keys.F8] = ImGuiKey.F8,
            [Keys.F9] = ImGuiKey.F9, [Keys.F10] = ImGuiKey.F10, [Keys.F11] = ImGuiKey.F11, [Keys.F12] = ImGuiKey.F12,
        };
        return map;
    }

    private void CreateDeviceResources()
    {
        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, 10000, IntPtr.Zero, BufferUsageHint.StreamDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, 20000, IntPtr.Zero, BufferUsageHint.StreamDraw);

        const int stride = 20;
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        string vertexSource = """
            #version 330 core
            layout(location = 0) in vec2 Position;
            layout(location = 1) in vec2 UV;
            layout(location = 2) in vec4 Color;
            uniform mat4 ProjMtx;
            out vec2 Frag_UV;
            out vec4 Frag_Color;
            void main()
            {
                Frag_UV = UV;
                Frag_Color = Color;
                gl_Position = ProjMtx * vec4(Position, 0, 1);
            }
            """;

        string fragmentSource = """
            #version 330 core
            in vec2 Frag_UV;
            in vec4 Frag_Color;
            uniform sampler2D Texture;
            out vec4 Out_Color;
            void main()
            {
                Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
            }
            """;

        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vertexSource);
        GL.CompileShader(vs);

        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, fragmentSource);
        GL.CompileShader(fs);

        _shader = GL.CreateProgram();
        GL.AttachShader(_shader, vs);
        GL.AttachShader(_shader, fs);
        GL.LinkProgram(_shader);
        GL.DetachShader(_shader, vs);
        GL.DetachShader(_shader, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        _shaderTexture = GL.GetUniformLocation(_shader, "Texture");
        _shaderProjection = GL.GetUniformLocation(_shader, "ProjMtx");
    }

    private void CreateFontsTexture()
    {
        var io = ImGui.GetIO();

        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();
    }

    public void Update(float deltaSeconds)
    {
        if (_frameBegun) ImGui.Render();

        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_window.ClientSize.X, _window.ClientSize.Y);
        io.DeltaTime = deltaSeconds > 0.0001f ? deltaSeconds : 1f / 60f;

        var mouse = _window.MouseState;
        io.AddMousePosEvent(mouse.X, mouse.Y);

        io.AddMouseWheelEvent(_scrollDelta.X, _scrollDelta.Y);
        _scrollDelta = Vector2.Zero;

        var buttons = _window.MouseState;
        io.AddMouseButtonEvent(0, buttons[MouseButton.Left]);
        io.AddMouseButtonEvent(1, buttons[MouseButton.Right]);
        io.AddMouseButtonEvent(2, buttons[MouseButton.Middle]);
        io.AddMouseButtonEvent(3, buttons[MouseButton.Button1]);
        io.AddMouseButtonEvent(4, buttons[MouseButton.Button2]);

        var keyboard = _window.KeyboardState;
        foreach (var pair in KeyMap)
        {
            bool down = keyboard.IsKeyDown(pair.Key);
            bool was = _keysDown.Contains(pair.Value);
            if (down == was) continue;

            if (down) _keysDown.Add(pair.Value);
            else _keysDown.Remove(pair.Value);
            io.AddKeyEvent(pair.Value, down);
        }

        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        bool alt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        io.AddKeyEvent(ImGuiKey.ModCtrl, ctrl);
        io.AddKeyEvent(ImGuiKey.ModShift, shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, alt);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void AddInputCharacter(char c) => ImGui.GetIO().AddInputCharacter(c);

    public void Render()
    {
        if (!_frameBegun) return;

        ImGui.Render();
        var data = ImGui.GetDrawData();

        int width = _window.ClientSize.X;
        int height = _window.ClientSize.Y;
        GL.Viewport(0, 0, width, height);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);

        GL.UseProgram(_shader);
        var projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
        GL.UniformMatrix4(_shaderProjection, false, ref projection);
        GL.Uniform1(_shaderTexture, 0);

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.ActiveTexture(TextureUnit.Texture0);

        for (int n = 0; n < data.CmdListsCount; n++)
        {
            var list = data.CmdLists[n];

            GL.BufferData(BufferTarget.ArrayBuffer, list.VtxBuffer.Size * 20, list.VtxBuffer.Data, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, list.IdxBuffer.Size * sizeof(ushort), list.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            for (int i = 0; i < list.CmdBuffer.Size; i++)
            {
                var cmd = list.CmdBuffer[i];
                if (cmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException("User callbacks в этом бэкенде не поддерживаются.");
                }

                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);

                var clip = cmd.ClipRect;
                GL.Scissor(
                    (int)clip.X,
                    (int)(height - clip.W),
                    (int)(clip.Z - clip.X),
                    (int)(clip.W - clip.Y));

                GL.DrawElementsBaseVertex(
                    PrimitiveType.Triangles,
                    (int)cmd.ElemCount,
                    DrawElementsType.UnsignedShort,
                    (IntPtr)(cmd.IdxOffset * sizeof(ushort)),
                    (int)cmd.VtxOffset);
            }
        }

        GL.Disable(EnableCap.ScissorTest);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        _frameBegun = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _window.MouseWheel -= OnMouseWheel;

        GL.DeleteTexture(_fontTexture);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shader);
    }
}



