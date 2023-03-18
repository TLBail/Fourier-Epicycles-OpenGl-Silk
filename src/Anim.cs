using System.Diagnostics;
using System.Numerics;
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
    private List<float> wave = new List<float>();
    private float lineOffset = 0;
    private int nbPoints = 2000;
    private float lengthWave = 4.0f;
    private Line pointToWaveLine;
    private List<Circle> circles = new List<Circle>();
    private int nbCirlces = 5;
    private float sizeCircles = 1;
    private int wantedNbCircles = 5;
    private Stopwatch stopwatchUpdate = new Stopwatch();

    public static readonly float[] signal = {
        1.0f,1.0f,1.0f,-1.0f,-1.0f,-1.0f,1.0f,1.0f,1.0f,-1.0f,-1.0f,-1.0f
    };

    public Fourier.Component[] dft;
    
    public Anim(OpenGl openGl) {
        this.openGl = openGl;
        openGl.DrawEvent += Draw;
        openGl.UpdateEvent += Update;
        openGl.LoadEvent += Load; 
        openGl.OnCloseEvent += Stop;

        
        
        dft = Fourier.dft(signal);
        
        Console.WriteLine(String.Join(",",dft) );
        nbCirlces = dft.Length;
        wantedNbCircles = nbCirlces;
        openGl.Run();
    }

   

    private  void Load(GL gl) {
        camera = new Camera(openGl.window);
        camera.Position = new Vector3(1, 0, 3);
        
        
        
        for (int i = 0; i < nbCirlces; i++) {
            circles.Add(new Circle(openGl, new Vector3D<float>(-2,0,0),
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
    }



    private  void Update(double deltatime) {
        stopwatchUpdate.Restart();
        int i = 0;
        // if (wantedNbCircles > circles.Count) {
        //     int nbCircleToAdd = wantedNbCircles - circles.Count;
        //     for (i = 0; i < nbCircleToAdd ; i++) {
        //         circles.Add(new Circle(openGl, Vector3D<float>.Zero, new Vector3D<float>(1, 0, 0), false, 0.4f));
        //     }
        // }
        // nbCirlces = wantedNbCircles;
     
        
        float x = 0;
        float y = 0;
        float freq = 0;
        float radius = 0;
        float phase = 0;
        float prevX = 0;
        float prevY = 0;
        for (i = 0; i < dft.Length; i++) {
            prevX = x;
            prevY = y;
            // freq = dft[i ].frequency;
            // radius = dft[i].amplitude;
            // phase = dft[i].phase;

            float n = 2 * i + 1;
            freq = n;
            radius = (4 / (float)((n) * MathF.PI));
            phase = 0;
            x += radius * MathF.Cos(freq * (float)(time + phase));
            y += radius * MathF.Sin(freq * (float)(time + phase));
            circles[i].position = new Vector3D<float>((float)prevX, (float)prevY, 0.0f);
            circles[i].radius = (float)radius;

        }
        
        circleFilled.position = circles[dft.Length - 1].position;
        
        wave.Insert(0, (float)y);
        if (wave.Count > nbPoints) {
            wave.RemoveRange(nbPoints, wave.Count  -nbPoints  );
        }

        Vector3D<float>[] points = new Vector3D<float>[wave.Count()];
        for (i = 0; i < wave.Count; i++) {
            points[i] = new Vector3D<float>( lengthWave*i / nbPoints + 2, wave[i], 0.0f);
        }
        waveLine.points = points.ToArray();

        pointToWaveLine.points = new[]
        {
            circleFilled.position,
            new Vector3D<float>(2, (float)y, 0)
        };
        double speed = 0.01;
        time +=speed *2 *MathF.PI * (float)signal.Length * deltatime;
        stopwatchUpdate.Stop();
    }



    static float cameraZ = 0;

    private unsafe void Draw(GL gl) {
        setUniforms(gl);

        ImGui.Begin("Fourier Series ");
        ImGui.Text( (1000.0f / ImGui.GetIO().Framerate).ToString("F") +  " ms/frame ( "+ ImGui.GetIO().Framerate.ToString("F1") + " FPS)" );
        ImGui.Text( stopwatchUpdate.ElapsedMilliseconds.ToString() + " ms/update" );

        ImGui.SliderInt("nbPoints", ref nbPoints, 10, 10000);
        ImGui.SliderFloat("lenghtWave", ref lengthWave, 0.0f, 5.0f);
        ImGui.SliderFloat("sizeCircles", ref sizeCircles, 0.0f, 5.0f);
        // ImGui.DragInt("nbCircles ", ref wantedNbCircles,1 );

        if(ImGui.DragFloat("cameraZ", ref cameraZ, 1.0f)) camera.Position = new Vector3(0, 0, cameraZ);
        ImGui.End();

        Circle.DrawAll(gl, (uint)nbCirlces);
        circleFilled.Draw(gl);
        waveLine.Draw(gl);
        pointToWaveLine.Draw(gl);

        // ImGui.ShowDemoWindow();
    }

    private unsafe void setUniforms(GL gl) {
        
        gl.BindBuffer(BufferTargetARB.UniformBuffer, openGl.uboWorld);
        System.Numerics.Matrix4x4 projectionMatrix = camera.GetProjectionMatrix();
        // projectionMatrix = Matrix4x4.Identity;
        gl.BufferSubData(BufferTargetARB.UniformBuffer, 0, (uint)sizeof(System.Numerics.Matrix4x4), projectionMatrix);
        gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);


        gl.BindBuffer(BufferTargetARB.UniformBuffer, openGl.uboWorld);
        System.Numerics.Matrix4x4 viewMatrix = camera.GetViewMatrix();
        // viewMatrix = Matrix4x4.Identity;
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
    
}