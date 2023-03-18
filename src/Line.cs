using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace SilkCircle;



public class Line
{

    private struct VertexLine
    {
        public Vector3D<float> position;
        public Vector3D<float> color;

        public VertexLine(Vector3D<float> position, Vector3D<float> color) {
            this.position = position;
            this.color = color;
        }
    }
    private static bool shaderSetup = false;
    private static Shader shader;
    private VertexArrayObject<VertexLine, uint> vao;
    private BufferObject<VertexLine> vbo;
    private OpenGl openGl;

    private int nbVertex = 0;
    private Vector3D<float>[] _points;

    public Vector3D<float>[] points
    {
        get
        {
            return _points;
        }
        set
        {
            _points = value;
            updateData();
        }
    }
    public Vector3D<float> color;


    public Line(OpenGl openGl,Vector3D<float> start, Vector3D<float> end, Vector3D<float> color) : this(openGl, new []{start, end}, color){
    }
    
    public Line(OpenGl gl, Vector3D<float>[] points, Vector3D<float> color) {
        this.openGl = gl;
        this._points = points;
        this.color = color;
        if (shaderSetup == false) {
            setUpShader(this.openGl);
        }

        updateData();
    }

    
    private unsafe void updateData() {
        VertexLine[] data = new VertexLine[points.Length];
        for (int i = 0; i < points.Length; i++) {
            data[i].position = points[i];
            data[i].color = color;
        }

        if (points.Length > nbVertex) {
            nbVertex = points.Length;
            
            if (vbo != null)vbo.Dispose();
            if (vao != null)vao.Dispose();
            vbo = new BufferObject<VertexLine>(openGl.Gl, nbVertex, BufferTargetARB.ArrayBuffer);
            vao = new VertexArrayObject<VertexLine, uint>(openGl.Gl , vbo);
            vao.Bind();
            vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, "position");
            vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, "color");

        }

        vbo.Bind();
        vbo.sendData(data, 0);
    }

    public static void setUpShader(OpenGl openGl) {
        shaderSetup = true;
     
        shader = new Shader(openGl.Gl, "Assets/line.vertex","Assets/line.fragment");
        openGl.OnCloseEvent += () =>
        {
            shader.Dispose();
        };

    }

    public void Draw(GL gl) {
        vao.Bind();
        shader.Use();
        gl.DrawArrays(PrimitiveType.LineStrip, 0, (uint)points.Length);
    }
    

    public void Dispose() {
        vbo.Dispose();
        vao.Dispose();
    }
}