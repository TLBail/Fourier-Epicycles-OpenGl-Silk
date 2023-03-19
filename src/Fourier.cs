using System.Numerics;

namespace SilkCircle;

public static class Fourier
{
    public struct Component
    {
        public float real;
        public float img;
        public float frequency;
        public float amplitude;
        public float phase;

        public Component(float real, float img, float frequency, float amplitude, float phase) {
            this.real = real;
            this.img = img;
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.phase = phase;
        }

        public override string ToString() {
            return "("+ real+ "|" + img + "|" + frequency + "|" + amplitude + "|" + phase + ")";
        }
    }
    
    public static Component[] dft(float[] x) {
        Component[] X = new Component[x.Length];
        const float pi2 = 2 * MathF.PI;
        float N = x.Length;
        for (int k = 0; k < N; k++) {

            float real = 0;
            float img = 0;
            for (int n = 0; n < N; n++) {
                float phi = (pi2 * k * n) / N;
                real += x[n] * MathF.Cos(phi);
                img -= x[n] * MathF.Sin(phi);
            }

            real /= (float)N;
            img /= (float)N;

            float amplitude = MathF.Sqrt((real * real) + (img * img));
            float phase = MathF.Atan2(img, real);
            
            X[k] = new Component(real, img, k, amplitude, phase);
        }
        return X;
    }
}