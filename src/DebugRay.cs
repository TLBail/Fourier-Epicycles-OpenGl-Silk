using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SilkCircle;


public class DebugRay
{

    private static readonly Vector3D<float> DEFAULT_COLOR = new Vector3D<float>(1.0f, 0, 0);

    private BufferObject<DebugRayVertex> Vbo;
    private VertexArrayObject<DebugRayVertex, uint> Vao;

    
    private Vector3D<float> start;
    private Vector3D<float> end;

    private static Shader rayShader;
    private GL Gl;
    
    public DebugRay(OpenGl openGl, Vector3D<float> start, Vector3D<float> end, Vector3D<float> color)
    : this(openGl, start, end, color, color) {    }

    public DebugRay(OpenGl openGl, Vector3D<float> start, Vector3D<float> end, Vector3D<float> startColor, Vector3D<float> endColor)
    {
        this.start = start;
        this.end = end;
        Gl = openGl.Gl;
        openGl.OnCloseEvent += remove;
        DebugRayVertex[] vertices = new[]
        {
            new DebugRayVertex(start, startColor),
            new DebugRayVertex(end, endColor),
        };

        Vbo = new BufferObject<DebugRayVertex>(Gl, vertices, BufferTargetARB.ArrayBuffer);
        Vao = new VertexArrayObject<DebugRayVertex, uint>(Gl, Vbo);
        
        Vao.Bind();
        Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, "position");
        Vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, "color");

        if (rayShader == null) {
            rayShader = new Shader(Gl, "./Assets/3dPosOneColorUni/VertexShader.hlsl",
                "./Assets/3dPosOneColorUni/FragmentShader.hlsl");
        }

    }
    
    
    public DebugRay(OpenGl openGl, Vector3D<float> start, Vector3D<float> end) : this(openGl, start,  end, DEFAULT_COLOR){}

    public void remove()
    {
        Vao.Dispose();
        Vbo.Dispose();
    }
    
    public void Drawables(GL gl)
    {
        Vao.Bind();
        rayShader.Use();
        
        
        var model = Matrix4x4.Identity;
        model = Matrix4x4.CreateTranslation(new Vector3(0,0,0));
        rayShader.SetUniform("model", model);
        
        
        Gl.DrawArrays(PrimitiveType.Lines, 0, 2);
    }
}