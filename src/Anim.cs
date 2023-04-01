using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace SilkCircle;

public class Anim
{

    private OpenGl openGl;

    private CircleFilled circleFilledX;
    private CircleFilled circleFilledY;
    private double time;
    private Camera camera;
    private Line waveLine;
    private List<Vector2D<float>> wave = new List<Vector2D<float>>();
    private Line pointToWaveLine;
    private List<CircleUnfilled> circles = new List<CircleUnfilled>();
    private List<CircleUnfilled> circles2 = new List<CircleUnfilled>();
    private Stopwatch stopwatchUpdate = new Stopwatch();
    private float speed = 1;
    private Line pointToWaveLine2;
    private Line lineCircleX;
    private Line lineCircleY;

    private class JsonPath
    {
        public float[]? path { get; set; }
    }
    
    private readonly Fourier.Component[] fourierX;
    private readonly Fourier.Component[] fourierY;
    
    public Anim() {
        this.openGl = OpenGl.Instance();
        openGl.DrawEvent += Draw;
        openGl.UpdateEvent += Update;
        openGl.LoadEvent += Load; 
        openGl.OnCloseEvent += Stop;
        
        string json = File.ReadAllText("Assets/path2.json");
        JsonPath jsonPath = JsonSerializer.Deserialize<JsonPath>(json)!;
        
        float min = jsonPath.path!.Min();
        float max = jsonPath.path!.Max();
        float[] signal2 = new float[jsonPath.path!.Length / 2];
        float[] signal3 = new float[jsonPath.path!.Length / 2];

        
        int j = 0;
        for (int i = 0; i < jsonPath.path.Length / 2; i++) { 
            signal2[i] = Mapper(jsonPath.path[j], min, max, -4, 2);
            j++;
            signal3[i] = -Mapper(jsonPath.path[j], min, max, -0.0f, 6.0f);
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
            circles.Add(new CircleUnfilled( new Vector3D<float>(0,0,0),
                new Vector3D<float>(1, 0, 0)));   
        }
        for (int i = 0; i < fourierY.Length; i++) {
            circles2.Add(new CircleUnfilled( new Vector3D<float>(0,0,0),
                new Vector3D<float>(1, 0, 0)));   
        }

        
        
        circleFilledX = new CircleFilled(
             new Vector3D<float>(1),
             new Vector3D<float>(0.9f, 0.1f, 0.5f), 0.05f);
        
        circleFilledY = new CircleFilled(
            new Vector3D<float>(1),
            new Vector3D<float>(0.9f, 0.1f, 0.5f), 0.05f);

        
        waveLine = new Line(
            new Vector3D<float>(1, -1, 0),
            new Vector3D<float>(1, 1, 0),
            new Vector3D<float>(1, 1, 0));
        
        
        pointToWaveLine = new Line( Vector3D<float>.Zero, Vector3D<float>.Zero, new Vector3D<float>(200 / 256f, 100 / 256f, 240 / 256f));
        pointToWaveLine2 = new Line( Vector3D<float>.Zero, Vector3D<float>.Zero, new Vector3D<float>(200 / 256f, 100 / 256f, 200 / 256f));

        lineCircleX = new Line( Vector3D<float>.Zero, Vector3D<float>.Zero, new Vector3D<float>(12 / 256f, 1 / 256f, 200 / 256f));
        lineCircleY = new Line( Vector3D<float>.Zero, Vector3D<float>.Zero, new Vector3D<float>(12 / 256f, 1 / 256f, 200 / 256f));
        
        stopwatchUpdate.Start();
    }


    private Vector2D<float> Epicycle(List<CircleUnfilled> epiCircle, Line line, float x, float y, float rotation, Fourier.Component[] fourier) {
        Vector3D<float>[] linePoints = new Vector3D<float>[fourier.Length + 1];
        linePoints[0] = new Vector3D<float>(x, y, 0);
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
            linePoints[i + 1] = new Vector3D<float>(x, y, 0);
        }

        line.points = linePoints.ToArray();
        return new(x,y);
    }

    private  void Update(double deltatime) {
        if (stopwatchUpdate.ElapsedMilliseconds > 100 / speed) {
            UpdateCircle(); 
            stopwatchUpdate.Restart();            
        }
    }

    private void UpdateCircle() {
        var pos = Epicycle(circles,lineCircleX, 1, 1, 0, fourierX);
        var pos2 = Epicycle(circles2, lineCircleY, -2, -1, Single.Pi / 2, fourierY);
        Vector2D<float> vector = new Vector2D<float>(pos.X, pos2.Y); 
        circleFilledX.position = new Vector3D<float>(pos.X, pos.Y, 0.0f);
        circleFilledY.position = new Vector3D<float>(pos2.X, pos2.Y, 0.0f); 
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
            circleFilledX.position,
            new Vector3D<float>(vector.X, vector.Y, 0)
        };
        
        pointToWaveLine2.points = new[]
        {
            new Vector3D<float>(pos2.X, pos2.Y, 0),
            new Vector3D<float>(vector.X, vector.Y, 0)
        };

        double dt = Math.PI * 2 / fourierX.Length;
        
        time += dt;
    }



    static float cameraZ;

    private void Draw(GL gl) {
        SetUniforms(gl);

        ImGui.Begin("Fourier Series ");
        ImGui.Text( (1000.0f / ImGui.GetIO().Framerate).ToString("F") +  " ms/frame ( "+ ImGui.GetIO().Framerate.ToString("F1") + " FPS)" );

        ImGui.DragFloat("speed", ref speed, 0.1f, 0.1f, 10.0f);

        if(ImGui.DragFloat("cameraZ", ref cameraZ, 1.0f)) camera.Position = new Vector3(0, 0, cameraZ);
        ImGui.End();

        
        CircleFilled.DrawAll(gl);
        CircleUnfilled.DrawAll(gl);

        circleFilledX.Draw(gl);
        circleFilledY.Draw(gl);
        waveLine.Draw(gl);
        pointToWaveLine.Draw(gl);
        pointToWaveLine2.Draw(gl);

        lineCircleX.Draw(gl);
        lineCircleY.Draw(gl);
        // ImGui.ShowDemoWindow();
    }

    private unsafe void SetUniforms(GL gl) {
        
        gl.BindBuffer(BufferTargetARB.UniformBuffer, openGl.uboWorld);
        Matrix4x4 projectionMatrix = camera.GetProjectionMatrix();
        gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (uint)sizeof(Matrix4x4), projectionMatrix);
        gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);


        gl.BindBuffer(BufferTargetARB.UniformBuffer, openGl.uboWorld);
        Matrix4x4 viewMatrix = camera.GetViewMatrix();
        gl.BufferSubData(BufferTargetARB.UniformBuffer, sizeof(Matrix4x4), (uint)sizeof(Matrix4x4), viewMatrix);
        gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        
        gl.BindBuffer(GLEnum.UniformBuffer, openGl.uboWorld);
        float aspectRation = openGl.screenSize.X / (float)openGl.screenSize.Y;
        gl.BufferSubData(GLEnum.UniformBuffer, 2 * sizeof(Matrix4x4), sizeof(float), aspectRation);
        gl.BindBuffer(GLEnum.UniformBuffer, 0);

        
    }

    private void Stop() {
        circleFilledX.Dispose();
        for (int i = 0; i < circles.Count; i++) {
            circles[0].Dispose();
        }
        waveLine.Dispose();
    }
    public static float Mapper(float valeur, float debut1, float fin1, float debut2, float fin2)
    {
        return debut2 + (valeur - debut1) / (fin1 - debut1) * (fin2 - debut2);
    }
    
}