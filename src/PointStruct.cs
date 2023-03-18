using Silk.NET.Maths;

namespace SilkCircle;

public struct PointStruct
{
    private Vector3D<float> position;
    private Vector3D<float> color;
    private float radius;

    public PointStruct(float x, float y,float z, float r, float g, float b, float radius) {
        position.X = x;
        position.Y = y;
        color.X = r;
        color.Y = g;
        color.Z = b;
        this.radius = radius;
    }

    public PointStruct(Vector3D<float> pos, Vector3D<float> color, float radius) {
        this.position = pos;
        this.color = color;
        this.radius = radius;
    }
}