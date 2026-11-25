using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;

namespace EcoGenesis.Interaction
{
    /// <summary>
    /// Controlador dinámico de tráfico y ambiente urbano:
    /// - Circulación continua de vehículos en las pistas perimétricas.
    /// - Sonido ambiental de ciudad y motor audible desde los límites y senderos exteriores del parque (radio amplio de 75m).
    /// - Toques ocasionales de claxon urbano distante para dar vida a la ciudad.
    /// </summary>
    public class AutoEnMovimiento : MonoBehaviour
    {
        [Header("Configuración del recorrido")]
        public Transform puntoInicio;
        public Transform puntoFin;
        public float velocidad = 6.5f;

        [Header("Modelos de autos distintos")]
        public GameObject[] modelosAutos;

        [Header("Audio Ambiental")]
        [Tooltip("Volumen del motor en 3D")]
        [SerializeField, Range(0.1f, 1f)] private float volumenMotor = 0.5f;
        [Tooltip("Distancia máxima en la que se escucha el auto desde el parque")]
        [SerializeField] private float radioAudibleMax = 80f;

        private AudioSource audioMotor;
        private AudioSource audioBocina;
        private static AudioClip clipMotor;
        private static AudioClip clipBocina;

        private float timerBocinaAleatoria;
        private PlayerController jugador;

        private void Awake()
        {
            InicializarAudio();
            timerBocinaAleatoria = Random.Range(8f, 22f);
        }

        private void Start()
        {
            jugador = FindFirstObjectByType<PlayerController>();
            ReiniciarAuto();
        }

        private void Update()
        {
            if (puntoInicio == null || puntoFin == null) return;

            float dt = Time.deltaTime;

            // Desplazamiento continuo del vehículo
            transform.position = Vector3.MoveTowards(transform.position, puntoFin.position, velocidad * dt);

            // Variación sutil del tono del motor para realismo
            if (audioMotor != null && audioMotor.isPlaying)
            {
                audioMotor.pitch = 0.95f + Mathf.PingPong(Time.time * 0.2f, 0.1f);
            }

            // Ocasionalmente un auto toca el claxon de fondo (ambiente de ciudad)
            timerBocinaAleatoria -= dt;
            if (timerBocinaAleatoria <= 0f)
            {
                timerBocinaAleatoria = Random.Range(14f, 32f);
                TocarBocinaAmbiente();
            }

            if (Vector3.Distance(transform.position, puntoFin.position) < 0.25f)
            {
                ReiniciarAuto();
            }
        }

        private void TocarBocinaAmbiente()
        {
            if (audioBocina == null || clipBocina == null) return;

            // Solo toca si está a una distancia prudente del jugador para enriquecer el ambiente
            if (jugador != null)
            {
                float dist = Vector3.Distance(transform.position, jugador.transform.position);
                if (dist > radioAudibleMax) return;
            }

            audioBocina.pitch = Random.Range(0.92f, 1.08f);
            audioBocina.PlayOneShot(clipBocina, 0.75f);
        }

        public void ReiniciarAuto()
        {
            if (puntoInicio != null)
            {
                transform.position = puntoInicio.position;
                if (puntoFin != null) transform.LookAt(puntoFin);
            }

            if (modelosAutos != null && modelosAutos.Length > 0)
            {
                int indiceAleatorio = Random.Range(0, modelosAutos.Length);
                for (int i = 0; i < modelosAutos.Length; i++)
                {
                    if (modelosAutos[i] != null)
                    {
                        modelosAutos[i].SetActive(i == indiceAleatorio);
                    }
                }
            }
        }

        private void InicializarAudio()
        {
            if (clipMotor == null) clipMotor = CrearClipMotor();
            if (clipBocina == null) clipBocina = CrearClipBocina();

            // Fuente de audio para el motor: Radio amplio para escucharse desde los bordes del parque
            audioMotor = gameObject.AddComponent<AudioSource>();
            audioMotor.clip = clipMotor;
            audioMotor.loop = true;
            audioMotor.playOnAwake = false;
            audioMotor.spatialBlend = 1f; // 3D espacial
            audioMotor.minDistance = 10f; // Máximo volumen hasta 10m
            audioMotor.maxDistance = radioAudibleMax; // Se escucha hasta 80m (cubre todo el borde del parque)
            audioMotor.rolloffMode = AudioRolloffMode.Logarithmic;
            audioMotor.volume = volumenMotor;
            audioMotor.dopplerLevel = 0.8f;
            audioMotor.Play();

            // Fuente de audio para el claxon ambiental
            audioBocina = gameObject.AddComponent<AudioSource>();
            audioBocina.spatialBlend = 1f;
            audioBocina.minDistance = 12f;
            audioBocina.maxDistance = radioAudibleMax + 10f;
            audioBocina.rolloffMode = AudioRolloffMode.Logarithmic;
            audioBocina.volume = 0.8f;
        }

        private static AudioClip CrearClipMotor()
        {
            const int sampleRate = 22050;
            const float duration = 1.0f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Combinación armónica de motor diésel/gasolina suave
                float tone = Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.45f
                           + Mathf.Sin(2f * Mathf.PI * 144f * t) * 0.28f
                           + (Mathf.PerlinNoise(t * 110f, 0.5f) - 0.5f) * 0.22f;
                data[i] = tone * 0.45f;
            }

            var clip = AudioClip.Create("CarEngineAmbient", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CrearClipBocina()
        {
            const int sampleRate = 22050;
            const float duration = 0.38f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Claxon clásico con envolvente suave
                bool sonando = (t >= 0f && t <= 0.12f) || (t >= 0.18f && t <= 0.32f);
                if (sonando)
                {
                    float envelope = Mathf.Sin(Mathf.Clamp01((t % 0.16f) / 0.12f) * Mathf.PI);
                    float tone = (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 554f * t)) * 0.5f;
                    data[i] = tone * envelope * 0.55f;
                }
                else
                {
                    data[i] = 0f;
                }
            }

            var clip = AudioClip.Create("CarHornAmbient", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
