using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace SilkCircle;

public class Circle : IDisposable
{
    private bool filled;
    private static bool shaderSetup = false;
    private static Shader shaderFilled;
    private static Shader shaderUnfilled;
    private static VertexArrayObject<PointStruct, uint> vao;
    private static BufferObject<PointStruct> vbo;
    private static uint nextVertexIndex = 0;
    private uint vertexIndex;
    private OpenGl openGl;

    private float _radius;
    public float radius
    {
        get
        {
            return _radius;
        }
        set
        {
            _radius = value;
            updateData();
        }
    }

    private Vector3D<float> _position;
    public Vector3D<float> position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            updateData();
        }
    }

    private Vector3D<float> _color;

    public Vector3D<float> color
    {
        get
        {
            return _color;
        }
        set
        {
            _color = value;
            updateData();
        }
    }

    public Circle(OpenGl openGl,Vector3D<float> position, Vector3D<float> color, bool filled, float radius = 0.1f) {
        this.filled = filled;
        this.openGl = openGl;
        this._radius = radius;
        this._position = position;
        this._color = color;
        if (shaderSetup == false) {
            setUpShader(this.openGl);
        }

        this.vertexIndex = nextVertexIndex;
        nextVertexIndex++;
        updateData();
    }

    private const int NBVERTEX = 10000;
    
    
    private unsafe void updateData() {
        PointStruct[] data = {
            new (position, color, _radius)
        };
        vbo.Bind();
        vbo.sendData(data, (nint)(sizeof(PointStruct) * vertexIndex));
    }

    public static void setUpShader(OpenGl openGl) {
        shaderSetup = true;
        vbo = new BufferObject<PointStruct>(openGl.Gl, NBVERTEX, BufferTargetARB.ArrayBuffer);
        vao = new VertexArrayObject<PointStruct, uint>(openGl.Gl , vbo);
        vao.Bind();
        vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, "position");
        vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, "color");
        vao.VertexAttributePointer(2, 1, VertexAttribPointerType.Float, "radius");

        
        shaderUnfilled = new Shader(openGl.Gl, "Assets/circleUnfilled.vertexShader","Assets/circleUnfilled.geometryShader", "Assets/circleUnfilled.fragmentShader");
        shaderFilled = new Shader(openGl.Gl, "Assets/circleFilled.vertexShader","Assets/circleFilled.geometryShader", "Assets/circleFilled.fragmentShader");
        openGl.OnCloseEvent += () =>
        {
            shaderFilled.Dispose();
            shaderUnfilled.Dispose();
        };

    }

    public void Draw(GL gl) {
        vao.Bind();
        if (filled) {
            shaderFilled.Use();
        } else {
            shaderUnfilled.Use();
        }
        gl.DrawArrays(PrimitiveType.Points, (int)vertexIndex, 1);
    }
    
    public static void DrawAll(GL gl, uint nbCircle) {
        vao.Bind();
        shaderUnfilled.Use();
        gl.DrawArrays(PrimitiveType.Points, 0, nbCircle);
    }
    

    public void Dispose() {
        vao.Dispose();
    }
}