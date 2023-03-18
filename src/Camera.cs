using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace SilkCircle;

public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Front { get; set; }

        public Vector3 Up { get; private set; }
        public float AspectRatio { get; set; }

        public float Yaw { get; set; } = -90f;
        public float Pitch { get; set; }

        private float Zoom = 45f;
        private Vector2 LastMousePosition;
        private bool isZoomActive = false;
        private IMouse mouse;
        private IKeyboard primaryKeyboard;

        public Camera(IWindow window) : this(new Vector3(0,0,3.0f), Vector3.UnitZ * -1, Vector3.UnitY, window.Size.X / (float)window.Size.Y) {
            window.FramebufferResize += FrameBufferResize;
        }
        public Camera(IWindow window, IMouse mouse, IKeyboard keyboard) : this(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, 800 / 600) {
            this.primaryKeyboard = keyboard;
            Vector2D<int> size = window.GetFullSize();
            AspectRatio = (float)size.X / (float)size.Y;
            window.FramebufferResize += FrameBufferResize;
            this.mouse = mouse;
            mouse.Cursor.CursorMode = CursorMode.Normal;
            mouse.MouseMove += OnMouseMove;
        }

        
        
        public void setZoomActive(bool active) {
            if(isZoomActive == active) return;
            if (active) {
                mouse.Scroll += OnMouseWheel;
            } else {
                mouse.Scroll -= OnMouseWheel;
            }
        }
        
        private Camera(Vector3 position, Vector3 front, Vector3 up, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
            Front = front;
            Up = up;
        }

        public void ModifyZoom(float zoomAmount)
        {
            //We don't want to be able to zoom in too close or too far away so clamp to these values
            Zoom = Math.Clamp(Zoom - zoomAmount, 1.0f, 45f);
        }

        private void ModifyDirection(float xOffset, float yOffset)
        {
            Yaw += xOffset;
            Pitch -= yOffset;

            //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
            Pitch = Math.Clamp(Pitch, -89f, 89f);

            var cameraDirection = Vector3.Zero;
            cameraDirection.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            cameraDirection.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            cameraDirection.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));

            Front = Vector3.Normalize(cameraDirection);
        }

        public Matrix4x4 GetViewMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, Position + Front, Up);
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Zoom), AspectRatio, 0.1f, Single.PositiveInfinity);
        }

        private unsafe void OnMouseMove(IMouse mouse, Vector2 position)
        {
            var lookSensitivity = 0.1f;
            if (LastMousePosition == default) { LastMousePosition = position; }
            else
            {
                var xOffset = (position.X - LastMousePosition.X) * lookSensitivity;
                var yOffset = (position.Y - LastMousePosition.Y) * lookSensitivity;
                LastMousePosition = position;

                ModifyDirection(xOffset, yOffset);
            }
        }

        private unsafe void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            ModifyZoom(scrollWheel.Y);
        }

        private void FrameBufferResize(Vector2D<int> size)
        {
            AspectRatio = (float)size.X / (float)size.Y;
        }

        
        public void movePlayer(double deltaTime)
        {
            var speed = 1 * (float)deltaTime;


            if (primaryKeyboard.IsKeyPressed(Key.ShiftLeft)) speed = 10 * (float)deltaTime;

                
            if (primaryKeyboard.IsKeyPressed(Key.W))
            {
                //Move forwards
                Position += speed * Front;
            }
            if (primaryKeyboard.IsKeyPressed(Key.S))
            {
                //Move backwards
                Position -= speed * Front;
            }
            if (primaryKeyboard.IsKeyPressed(Key.A))
            {
                //Move left
                Position -= Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.D))
            {
                //Move right
                Position += Vector3.Normalize(Vector3.Cross(Front, Up)) * speed;
            }

            if (primaryKeyboard.IsKeyPressed(Key.Space))
            {
                //move up
                Position += Up * speed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.Q))
            {
                //move up
                Position += -Up * speed;
            }
        }

        public void reset() {
            
        }
    }

public class MathHelper
{
    public static float DegreesToRadians(float degrees)
    {
        return MathF.PI / 180f * degrees;
    }
}