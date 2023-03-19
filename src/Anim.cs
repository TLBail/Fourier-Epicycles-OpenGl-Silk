using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace SilkCircle;

public class Anim
{

    private OpenGl openGl;

    private Circle circleFilled;
    private double time = 0;
    private Camera camera;
    private DebugRay debugRay;
    private Line waveLine;
    private List<Vector2D<float>> wave = new List<Vector2D<float>>();
    private float lineOffset = 0;
    private Line pointToWaveLine;
    private List<Circle> circles = new List<Circle>();
    private List<Circle> circles2 = new List<Circle>();
    private Stopwatch stopwatchUpdate = new Stopwatch();
    private float speed = 1;
    private Line pointToWaveLine2;

    private class JsonPath
    {
        public float[] path { get; set; }
    }
    
    public Fourier.Component[] fourierX;
    public Fourier.Component[] fourierY;
    
    public Anim(OpenGl openGl) {
        this.openGl = openGl;
        openGl.DrawEvent += Draw;
        openGl.UpdateEvent += Update;
        openGl.LoadEvent += Load; 
        openGl.OnCloseEvent += Stop;
        
        string json = File.ReadAllText("Assets/path.json");
        JsonPath jsonPath = JsonSerializer.Deserialize<JsonPath>(json);
        
        float min = jsonPath.path.Min();
        float max = jsonPath.path.Max();
        float[] signal2 = new float[jsonPath.path.Length / 2];
        float[] signal3 = new float[jsonPath.path.Length / 2];

        
        int j = 0;
        for (int i = 0; i < jsonPath.path.Length / 2; i++) { 
            signal2[i] = Mapper(jsonPath.path[j], min, max, -2, 2);
            j++;
            signal3[i] = -Mapper(jsonPath.path[j], min, max, -2, 2);
            j++;
        }
        
        
        fourierX = Fourier.dft(signal2);
        fourierY = Fourier.dft(signal3);
        openGl.Run();
    }

   

    private  void Load(GL gl) {
        camera = new Camera(openGl.window);
        camera.Position = new Vector3(0, 0, 8);
        
        
        
        for (int i = 0; i < fourierX.Length; i++) {
            circles.Add(new Circle(openGl, new Vector3D<float>(0,0,0),
                new Vector3D<float>(1, 0, 0),
                false,
                0.4f));   
        }
        for (int i = 0; i < fourierY.Length; i++) {
            circles2.Add(new Circle(openGl, new Vector3D<float>(0,0,0),
                new Vector3D<float>(1, 0, 0),
                false,
                0.4f));   
        }

        
        
        circleFilled = new Circle(openGl,
            new Vector3D<float>(0),
            new Vector3D<float>(1, 0, 0),
            true,
            0.05f);
        
        waveLine = new Line(openGl,
            new Vector3D<float>(1, -1, 0),
            new Vector3D<float>(1, 1, 0),
            new Vector3D<float>(1, 1, 0));
        
        
        pointToWaveLine = new Line(openGl, Vector3D<float>.Zero, Vector3D<float>.Zero, new Vector3D<float>(12, 1, 42));
        pointToWaveLine2 = new Line(openGl, Vector3D<float>.Zero, Vector3D<float>.Zero, new Vector3D<float>(12, 1, 42));

    }


    private Vector2D<float> epicycle(List<Circle> epiCircle, float x, float y, float rotation, Fourier.Component[] fourier) {
        for (int i = 0; i < fourier.Length; i++) {
            float prevX = x;
            float prevY = y;
            float freq = fourier[i].frequency;
            float radius = fourier[i].amplitude;
            float phase = fourier[i].phase;
            x += radius * MathF.Cos((float)(freq * time + phase + rotation));
            y += radius * MathF.Sin((float)(freq * time + phase + rotation));
            epiCircle[i].position = new Vector3D<float>(prevX, prevY, 0);
            epiCircle[i].radius = radius;
        }

        return new(x,y);
    }

    private  void Update(double deltatime) {
        stopwatchUpdate.Restart();
        var pos = epicycle(circles, 1, 1, 0, fourierX);
        var pos2 = epicycle(circles2, -2, -1, Single.Pi / 2, fourierY);
        Vector2D<float> vector = new Vector2D<float>(pos.X, pos2.Y); 
        circleFilled.position = new Vector3D<float>(pos.X, pos.Y, 0.0f);
        
        wave.Insert(0, vector);
        if (wave.Count > fourierX.Length) {
            wave.Clear();
        }

        Vector3D<float>[] points = new Vector3D<float>[wave.Count()];
        for (int i = 0; i < wave.Count; i++) {
            points[i] = new Vector3D<float>( wave[i].X, wave[i].Y, 0.0f);
        }
        waveLine.points = points.ToArray();

        pointToWaveLine.points = new[]
        {
            circleFilled.position,
            new Vector3D<float>(vector.X, vector.Y, 0)
        };
        
        pointToWaveLine2.points = new[]
        {
            new Vector3D<float>(pos2.X, pos2.Y, 0),
            new Vector3D<float>(vector.X, vector.Y, 0)
        };

        double dt = Math.PI * 2 / fourierX.Length;
        
        time += dt * speed;
        stopwatchUpdate.Stop();
    }



    static float cameraZ = 0;

    private unsafe void Draw(GL gl) {
        setUniforms(gl);

        ImGui.Begin("Fourier Series ");
        ImGui.Text( (1000.0f / ImGui.GetIO().Framerate).ToString("F") +  " ms/frame ( "+ ImGui.GetIO().Framerate.ToString("F1") + " FPS)" );
        ImGui.Text( stopwatchUpdate.ElapsedMilliseconds.ToString() + " ms/update" );

        ImGui.DragFloat("speed", ref speed, 1.0f);

        if(ImGui.DragFloat("cameraZ", ref cameraZ, 1.0f)) camera.Position = new Vector3(0, 0, cameraZ);
        ImGui.End();

        for (int i = 0; i < fourierX.Length; i++) {
            circles[i].Draw(gl);            
        }

        for (int i = 0; i < fourierX.Length; i++) {
            circles2[i].Draw(gl);
        }
        
        
        circleFilled.Draw(gl);
        waveLine.Draw(gl);
        pointToWaveLine.Draw(gl);
        pointToWaveLine2.Draw(gl);

        // ImGui.ShowDemoWindow();
    }

    private unsafe void setUniforms(GL gl) {
        
        gl.BindBuffer(BufferTargetARB.UniformBuffer, openGl.uboWorld);
        System.Numerics.Matrix4x4 projectionMatrix = camera.GetProjectionMatrix();
        gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (uint)sizeof(System.Numerics.Matrix4x4), projectionMatrix);
        gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);


        gl.BindBuffer(BufferTargetARB.UniformBuffer, openGl.uboWorld);
        System.Numerics.Matrix4x4 viewMatrix = camera.GetViewMatrix();
        gl.BufferSubData(BufferTargetARB.UniformBuffer, sizeof(System.Numerics.Matrix4x4), (uint)sizeof(System.Numerics.Matrix4x4), viewMatrix);
        gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        
        gl.BindBuffer(GLEnum.UniformBuffer, openGl.uboWorld);
        float aspectRation = (float)openGl.screenSize.X / (float)openGl.screenSize.Y;
        gl.BufferSubData(GLEnum.UniformBuffer, 2 * sizeof(System.Numerics.Matrix4x4), sizeof(float), aspectRation);
        gl.BindBuffer(GLEnum.UniformBuffer, 0);

        
    }

    private void Stop() {
        circleFilled.Dispose();
        for (int i = 0; i < circles.Count; i++) {
            circles[0].Dispose();
        }
        waveLine.Dispose();
    }
    public static float Mapper(float valeur, float debut1, float fin1, float debut2, float fin2)
    {
        return debut2 + (valeur - debut1) * (fin2 - debut2) / (fin1 - debut1);
    }
    
}