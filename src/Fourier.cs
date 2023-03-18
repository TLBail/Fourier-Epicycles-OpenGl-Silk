using System.Numerics;

namespace SilkCircle;

public static class Fourier
{
    public struct Component
    {
        public float frequency;
        public float amplitude;
        public float phase;

        public Component(float frequency, float amplitude, float phase) {
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.phase = phase;
        }

        public override string ToString() {
            return "(" + frequency + "|" + amplitude + "|" + phase + ")";
        }
    }
    
    public static Component[] dft(float[] x) {
        Component[] X = new Component[x.Length];
        const float pi2 = 2 * MathF.PI;
        int N = x.Length;
        for (int k = 0; k < N; k++) {

            float real = 0;
            float img = 0;
            for (int n = 0; n < N; n++) {
                float phi = (pi2 * k * n) / N;
                real += x[n] * MathF.Cos(phi);
                img -= x[n] * MathF.Sin(phi);
            }

            real = real / N;
            img = img / N;

            float magnitude = MathF.Sqrt(real * real + img * img);
            float phase = MathF.Atan2(img, real);
            
            X[k] = new Component(k, magnitude, phase);
        }
        return X;
    }
}