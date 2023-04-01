using System.Drawing;
using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SilkCircle;

public sealed class OpenGl
{

    private static OpenGl _instance;
    public static readonly object _lock = new object();
    public static OpenGl Instance()
    {
        if (_instance == null) {
            lock (_lock) {
                if (_instance == null) {
                    _instance = new OpenGl();
                }
            }
        }
        return _instance;
    }

    
    public IWindow window { get; private set; } 
    public IInputContext input { get; private set; }
    public GL Gl { get; private set; }
    public IKeyboard primaryKeyboard { get; private set; }
    public IMouse primaryMouse { get; private set; }

    ImGuiController imGuiController = null;

    private static readonly Color CLEAR_COLOR = Color.Black;

    private Glfw glfw;
    
    private bool running = true;
    public delegate void LoadDelegate(GL gl);
    public delegate void DrawDelegate(GL gl);
    public delegate void UpdateDelegate(double deltaTime);
    public uint uboWorld;
    public Vector2D<int> screenSize;
    public LoadDelegate LoadEvent;
    public UpdateDelegate UpdateEvent;
    public DrawDelegate DrawEvent;
    public Action OnCloseEvent;

    private OpenGl() {
        screenSize = new Vector2D<int>(1920, 1080);
        
        //Create a window.
        var options = WindowOptions.Default;
        options.Size = screenSize;
        options.Title = "Silk circle";
        
        window = Window.Create(options);
    
        //Assign events.
        window.Load += OnLoad;
        window.Render += OnRender;
        window.Update += OnUpdate;
        window.Closing += OnClose;
        window.FramebufferResize += FrameBufferResize;

        glfw = Glfw.GetApi();
    }

    public void Run() {
        window.Run();
    }

    public void Stop() {
        running = false;
    }

    private unsafe void OnLoad() {
        //Set-up input context.
        input = window.CreateInput();
        Gl = window.CreateOpenGL();

        imGuiController = new ImGuiController(Gl, window, input);

        primaryKeyboard = input.Keyboards.FirstOrDefault();
        primaryMouse = input.Mice.FirstOrDefault();

        if (primaryKeyboard != null) {
            primaryKeyboard.KeyDown += KeyDown;
        }
        initUniformBuffers();
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        
        LoadEvent?.Invoke(Gl);
    }


    private void OnUpdate(double deltaTime) {
        if (!running) {
            closeWindow();
            return;
        }

        UpdateEvent?.Invoke(deltaTime);
        imGuiController.Update((float)deltaTime);
    }

    private unsafe void OnRender(double delta) {
        Gl.Enable(EnableCap.DepthTest);
        Gl.ClearColor(CLEAR_COLOR);
        Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        DrawEvent?.Invoke(Gl);
        imGuiController.Render();
    }

    private void FrameBufferResize(Vector2D<int> size) {
        screenSize = size;
        Gl.Viewport(size);
    }

    private void OnClose() {
        OnCloseEvent?.Invoke();
        imGuiController?.Dispose();
        input?.Dispose();
        Gl?.Dispose();
    }

    public unsafe void setCursorMode(CursorModeValue cursorMode) {
        glfw.SetInputMode((WindowHandle*)window.Handle, CursorStateAttribute.Cursor, cursorMode);
    }

    public bool cursorIsNotAvailable() => getCursorMode() != CursorModeValue.CursorNormal;

    public unsafe CursorModeValue getCursorMode() {
        return (CursorModeValue)glfw
            .GetInputMode((WindowHandle*)window.Handle, CursorStateAttribute.Cursor);
    }

    private void closeWindow() {
        window.Close();
    }

    private void KeyDown(IKeyboard keyboard, Key key, int arg3) {
        if (key == Key.Escape) {
            Stop();
            return;
        }

        if (key == Key.F1) {
            unsafe {
                setCursorMode((getCursorMode() == CursorModeValue.CursorNormal)
                    ? CursorModeValue.CursorDisabled
                    : CursorModeValue.CursorNormal);
            }
        }
    }
    
    private unsafe void initUniformBuffers() {
        nuint size = (nuint)(2 * sizeof(Matrix4X4<float>) + sizeof(float));
        uboWorld = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.UniformBuffer, uboWorld);
        Gl.BufferData(BufferTargetARB.UniformBuffer, size, null, GLEnum.StaticDraw);
        Gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
        Gl.BindBufferRange(BufferTargetARB.UniformBuffer, 0, uboWorld, 0, size);
    }
}