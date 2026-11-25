using UnityEngine;

namespace EcoGenesis.Audio
{
    /// <summary>
    /// Sintetizador de audio por código (sin archivos): música lo-fi, drone contaminado, viento, agua, pájaros
    /// y efectos. Sirve como audio provisional acorde al GDD; se puede reemplazar por archivos de Freesound,
    /// Pixabay o FMOD sin tocar el resto del juego.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int Rate = 22050;
        private const float TwoPi = Mathf.PI * 2f;

        // ------------------------------------------------------------------ MÚSICA

        /// <summary>Lo-fi cálido con Rhodes, bajo, batería con swing, campanitas y crujido de vinilo (8 compases).</summary>
        public static AudioClip LofiClean()
        {
            var rng = new System.Random(5);
            float beat = 60f / 76f;
            int bars = 8;
            int n = (int)(bars * 4 * beat * Rate);
            var d = new float[n];

            int[][] chords =
            {
                new[] { 48, 52, 55, 59 }, // Cmaj7
                new[] { 45, 48, 52, 55 }, // Am7
                new[] { 41, 45, 48, 52 }, // Fmaj7
                new[] { 43, 47, 50, 53 }  // G7
            };
            int[] bass = { 36, 33, 29, 31 };
            int[] scale = { 72, 74, 76, 79, 81 }; // pentatónica de Do

            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                int[] ch = chords[bar % 4];

                for (int k = 0; k < ch.Length; k++) Piano(d, t0 + k * 0.014f, Hz(ch[k] + 12), 2.6f * beat, 0.11f);
                Piano(d, t0 + 2.5f * beat, Hz(ch[1] + 12), 1.4f * beat, 0.07f);
                Piano(d, t0 + 2.5f * beat + 0.012f, Hz(ch[3] + 12), 1.4f * beat, 0.06f);

                Bass(d, t0, Hz(bass[bar % 4]), 1.9f * beat, 0.30f);
                Bass(d, t0 + 2f * beat, Hz(bass[bar % 4]), 1.5f * beat, 0.22f);

                for (int step = 0; step < 8; step++)
                {
                    float swing = (step % 2 == 1) ? 0.07f * beat : 0f;
                    float t = t0 + step * beat * 0.5f + swing;
                    Hat(d, t, step % 2 == 1 ? 0.045f : 0.03f, rng);
                    if (step == 0 || step == 5) Kick(d, t, 0.55f);
                    if (step == 2 || step == 6) Snare(d, t, 0.3f, rng);
                }

                if (bar % 2 == 1)
                {
                    for (int m = 0; m < 2; m++)
                        Bell(d, t0 + beat * (0.5f + (float)rng.NextDouble() * 3f), Hz(scale[rng.Next(scale.Length)]), 1.4f, 0.06f);
                }
            }

            Lowpass(d, 0.5f);
            Crackle(d, rng, 0.0004f, 0.10f);
            for (int i = 0; i < n; i++) d[i] = (float)System.Math.Tanh(d[i] * 1.3f);
            return Make("Lofi_Clean", d, 0.8f);
        }

        /// <summary>Drone grave y lento con notas de piano sueltas: el parque contaminado (24 s en bucle).</summary>
        public static AudioClip PollutedDrone()
        {
            var rng = new System.Random(9);
            int n = 24 * Rate;
            var d = new float[n];

            int[] notes = { 33, 40, 45, 48 };
            for (int k = 0; k < notes.Length; k++)
            {
                float f = Mathf.Round(Hz(notes[k]) * 24f) / 24f; // ciclos enteros: bucle sin clic
                float f2 = f + 3f / 24f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    float lfo = 0.6f + 0.4f * Mathf.Sin(TwoPi * (1f / 12f) * t + k);
                    d[i] += (Mathf.Sin(TwoPi * f * t) + 0.6f * Mathf.Sin(TwoPi * f2 * t)) * lfo * 0.11f;
                }
            }

            int[] minor = { 57, 60, 62, 64, 67, 69 }; // pentatónica menor de La
            for (int k = 0; k < 7; k++)
            {
                float t = 2f + k * 3.2f + (float)rng.NextDouble() * 1.2f;
                Piano(d, t, Hz(minor[rng.Next(minor.Length)]), 3f, 0.09f);
            }

            Lowpass(d, 0.16f);
            Crackle(d, rng, 0.0002f, 0.05f);
            return Make("Drone_Polluted", d, 0.7f);
        }

        // ------------------------------------------------------------------ AMBIENTE

        public static AudioClip Wind()
        {
            var rng = new System.Random(21);
            int n = 11 * Rate;
            int fade = (int)(0.5f * Rate);
            var x = new float[n + fade];
            float y = 0f, y2 = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                float noise = Noise(rng);
                y += 0.06f * (noise - y);
                y2 += 0.02f * (noise - y2);
                float t = i / (float)Rate;
                float gust = 0.6f + 0.4f * Mathf.Sin(TwoPi * (2f / 11f) * t);
                x[i] = (y * 0.7f + y2 * 1.5f) * gust;
            }
            return Make("Wind", Loop(x, n, fade), 0.6f);
        }

        public static AudioClip Water()
        {
            var rng = new System.Random(33);
            int n = 8 * Rate;
            int fade = (int)(0.5f * Rate);
            var x = new float[n + fade];
            float y1 = 0f, y2 = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                float noise = Noise(rng);
                y1 += 0.25f * (noise - y1);
                y2 += 0.02f * (noise - y2);
                x[i] = (y1 - y2) * 0.5f;
            }
            for (int b = 0; b < 60; b++)
            {
                int s = rng.Next(x.Length);
                float f0 = 500f + (float)rng.NextDouble() * 700f;
                int len = (int)(0.07f * Rate);
                float phase = 0f;
                for (int j = 0; j < len && s + j < x.Length; j++)
                {
                    float t = j / (float)Rate;
                    phase += TwoPi * f0 * (1f + t * 18f) / Rate;
                    x[s + j] += Mathf.Sin(phase) * Mathf.Exp(-t * 45f) * 0.25f;
                }
            }
            return Make("Water", Loop(x, n, fade), 0.6f);
        }

        /// <summary>Zumbido industrial grave (4 s en bucle, ciclos enteros).</summary>
        public static AudioClip Hum()
        {
            int n = 4 * Rate;
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = Mathf.Sin(TwoPi * 55f * t) * 0.5f + Mathf.Sin(TwoPi * 110f * t) * 0.25f
                          + Mathf.Sin(TwoPi * 165f * t) * 0.12f + Mathf.Sin(TwoPi * 55.5f * t) * 0.3f
                          + Mathf.Sin(TwoPi * 27.5f * t) * 0.3f;
                d[i] = v * (0.7f + 0.3f * Mathf.Sin(TwoPi * 0.25f * t));
            }
            return Make("Hum", d, 0.5f);
        }

        public static AudioClip[] Birds()
        {
            var a = new float[(int)(0.6f * Rate)];
            Chirp(a, 0f, 2800f, 3600f, 0.11f, 0.5f);
            Chirp(a, 0.16f, 3000f, 3900f, 0.11f, 0.5f);
            Chirp(a, 0.32f, 3200f, 4200f, 0.14f, 0.5f);

            var b = new float[(int)(0.7f * Rate)];
            for (int k = 0; k < 8; k++)
            {
                float f = 3300f + (k % 2) * 500f;
                Chirp(b, k * 0.07f, f, f + 150f, 0.05f, 0.45f);
            }

            var c = new float[(int)(0.6f * Rate)];
            Chirp(c, 0f, 4300f, 2600f, 0.45f, 0.5f);

            var e = new float[(int)(0.8f * Rate)];
            Chirp(e, 0f, 3000f, 3400f, 0.12f, 0.5f);
            Chirp(e, 0.2f, 3000f, 3400f, 0.12f, 0.5f);
            Chirp(e, 0.45f, 3500f, 2800f, 0.2f, 0.4f);

            return new[] { Make("BirdA", a, 0.6f), Make("BirdB", b, 0.6f), Make("BirdC", c, 0.6f), Make("BirdD", e, 0.6f) };
        }

        // ------------------------------------------------------------------ EFECTOS

        public static AudioClip Pickup()
        {
            var d = new float[(int)(0.22f * Rate)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / Rate;
                // Pop táctil de recojo (burbuja / crujido ascendente muy satisfactorio)
                float env = Mathf.Exp(-t * 26f);
                float freq = Mathf.Lerp(160f, 740f, Mathf.Sqrt(Mathf.Clamp01(t / 0.10f)));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.75f
                           + Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.25f;
                d[i] = wave * env * 0.85f;
            }
            // Campana brillante suave de confirmación
            Bell(d, 0.035f, Hz(83), 0.18f, 0.45f);
            return Make("Pickup", d, 0.95f);
        }

        public static AudioClip Correct()
        {
            var d = new float[(int)(0.9f * Rate)];
            int[] notes = { 72, 76, 79, 84 };
            for (int i = 0; i < notes.Length; i++) Bell(d, i * 0.09f, Hz(notes[i]), 0.6f, 0.5f);
            return Make("Correct", d, 0.7f);
        }

        public static AudioClip Wrong()
        {
            var d = new float[(int)(0.4f * Rate)];
            Buzz(d, 0f, 220f, 0.18f);
            Buzz(d, 0.2f, 165f, 0.2f);
            Lowpass(d, 0.3f);
            return Make("Wrong", d, 0.6f);
        }

        public static AudioClip Mission()
        {
            var d = new float[(int)(2.0f * Rate)];
            Bell(d, 0f, Hz(72), 1.2f, 0.45f);
            Bell(d, 0.12f, Hz(76), 1.2f, 0.45f);
            Bell(d, 0.24f, Hz(79), 1.2f, 0.45f);
            Bell(d, 0.36f, Hz(84), 1.4f, 0.45f);
            Bell(d, 0.6f, Hz(88), 1.3f, 0.4f);
            return Make("Mission", d, 0.75f);
        }

        public static AudioClip Footstep(int seed)
        {
            var rng = new System.Random(seed);
            var d = new float[(int)(0.09f * Rate)];
            float y = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                y += 0.25f * (Noise(rng) - y);
                d[i] = y * Mathf.Exp(-t * 45f) + Mathf.Sin(TwoPi * 85f * t) * Mathf.Exp(-t * 60f) * 0.6f;
            }
            return Make("Step" + seed, d, 0.5f);
        }

        public static AudioClip Click()
        {
            var d = new float[(int)(0.05f * Rate)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                d[i] = (Mathf.Sin(TwoPi * 1800f * t) + 0.5f * Mathf.Sin(TwoPi * 900f * t)) * Mathf.Exp(-t * 90f);
            }
            return Make("Click", d, 0.5f);
        }

        // ------------------------------------------------------------------ INSTRUMENTOS Y UTILIDADES

        private static void Piano(float[] d, float start, float freq, float dur, float amp)
        {
            int s = (int)(start * Rate);
            int len = (int)(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t * 2.2f);
                float tone = Mathf.Sin(TwoPi * freq * t)
                             + 0.35f * Mathf.Sin(TwoPi * freq * 2f * t) * Mathf.Exp(-t * 4f)
                             + 0.12f * Mathf.Sin(TwoPi * freq * 3f * t) * Mathf.Exp(-t * 6f);
                float trem = 1f + 0.08f * Mathf.Sin(TwoPi * 4.5f * t);
                Add(d, s + i, tone * env * trem * amp);
            }
        }

        private static void Bass(float[] d, float start, float freq, float dur, float amp)
        {
            int s = (int)(start * Rate);
            int len = (int)(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t * 1.4f);
                float tone = Mathf.Sin(TwoPi * freq * t) + 0.25f * Mathf.Sin(TwoPi * freq * 2f * t);
                Add(d, s + i, tone * env * amp);
            }
        }

        private static void Bell(float[] d, float start, float freq, float dur, float amp)
        {
            int s = (int)(start * Rate);
            int len = (int)(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * 3.2f);
                float tone = Mathf.Sin(TwoPi * freq * t) + 0.4f * Mathf.Sin(TwoPi * freq * 2.76f * t) * Mathf.Exp(-t * 5f);
                Add(d, s + i, tone * env * amp);
            }
        }

        private static void Kick(float[] d, float start, float amp)
        {
            int s = (int)(start * Rate);
            int len = (int)(0.25f * Rate);
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                phase += TwoPi * (45f + 90f * Mathf.Exp(-t * 22f)) / Rate;
                Add(d, s + i, Mathf.Sin(phase) * Mathf.Exp(-t * 14f) * amp);
            }
        }

        private static void Snare(float[] d, float start, float amp, System.Random rng)
        {
            int s = (int)(start * Rate);
            int len = (int)(0.2f * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float v = Noise(rng) * 0.7f + Mathf.Sin(TwoPi * 190f * t) * 0.3f;
                Add(d, s + i, v * Mathf.Exp(-t * 22f) * amp);
            }
        }

        private static void Hat(float[] d, float start, float amp, System.Random rng)
        {
            int s = (int)(start * Rate);
            int len = (int)(0.05f * Rate);
            float prev = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float n = Noise(rng);
                Add(d, s + i, (n - prev) * Mathf.Exp(-t * 70f) * amp);
                prev = n;
            }
        }

        private static void Buzz(float[] d, float start, float freq, float dur)
        {
            int s = (int)(start * Rate);
            int len = (int)(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float sine = Mathf.Sin(TwoPi * freq * t);
                float env = Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 6f);
                Add(d, s + i, (Mathf.Sign(sine) * 0.3f + sine * 0.5f) * env * 0.6f);
            }
        }

        private static void Chirp(float[] d, float start, float f0, float f1, float dur, float amp)
        {
            int s = (int)(start * Rate);
            int len = (int)(dur * Rate);
            float phase = 0f;
            for (int i = 0; i < len && s + i < d.Length; i++)
            {
                float x = i / (float)len;
                float f = Mathf.Lerp(f0, f1, x) + 120f * Mathf.Sin(TwoPi * 28f * x * dur);
                phase += TwoPi * f / Rate;
                float env = Mathf.Sin(Mathf.PI * x);
                d[s + i] += Mathf.Sin(phase) * env * env * amp;
            }
        }

        private static void Crackle(float[] d, System.Random rng, float density, float amp)
        {
            for (int i = 0; i < d.Length; i++)
            {
                if (rng.NextDouble() < density) d[i] += Noise(rng) * amp * (0.3f + 0.7f * (float)rng.NextDouble());
                d[i] += Noise(rng) * 0.002f;
            }
        }

        private static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

        private static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        /// <summary>Suma con vuelta al inicio: las colas de las notas hacen el bucle sin cortes.</summary>
        private static void Add(float[] d, int index, float value)
        {
            int n = d.Length;
            d[((index % n) + n) % n] += value;
        }

        private static void Lowpass(float[] d, float alpha)
        {
            float y = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                y += alpha * (d[i] - y);
                d[i] = y;
            }
        }

        /// <summary>Convierte un tramo largo en un bucle continuo mezclando el final con el inicio.</summary>
        private static float[] Loop(float[] x, int n, int fade)
        {
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = x[i];
                if (i < fade) v = Mathf.Lerp(x[n + i], x[i], i / (float)fade);
                o[i] = v;
            }
            return o;
        }

        private static AudioClip Make(string name, float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            if (max > 1e-5f)
            {
                float gain = peak / max;
                for (int i = 0; i < data.Length; i++) data[i] *= gain;
            }

            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
