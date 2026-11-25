using EcoGenesis.Core;
using EcoGenesis.Data;
using EcoGenesis.Environment;
using EcoGenesis.Player;
using UnityEngine;

namespace EcoGenesis.Audio
{
    /// <summary>
    /// Mezcla viva del audio según la salud del parque: contaminado = drone grave y zumbido industrial;
    /// restaurado = lo-fi cálido, pájaros y agua. También reproduce efectos de recoger, acertar, errar,
    /// misiones, pasos y clics de interfaz. Todo el audio se genera por código (ProceduralAudio).
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        [SerializeField] private EnvironmentalManager environment;
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform waterPoint;

        private AudioSource musicClean, musicPolluted, wind, hum, water, sfx, steps, birdSource;
        private AudioClip[] birdClips, stepClips;
        private AudioClip pickup, correct, wrong, mission, click;
        private float health, shownHealth, birdTimer = 6f, stepTimer;

        public static void PlayUiClick()
        {
            if (Instance != null) Instance.PlaySfx(Instance.click, 0.8f);
        }

        private void Awake()
        {
            Instance = this;

            pickup = ProceduralAudio.Pickup();
            correct = ProceduralAudio.Correct();
            wrong = ProceduralAudio.Wrong();
            mission = ProceduralAudio.Mission();
            click = ProceduralAudio.Click();
            birdClips = ProceduralAudio.Birds();
            stepClips = new[] { ProceduralAudio.Footstep(1), ProceduralAudio.Footstep(2) };

            musicClean = CreateSource("MusicClean", ProceduralAudio.LofiClean(), true, 0f);
            musicPolluted = CreateSource("MusicPolluted", ProceduralAudio.PollutedDrone(), true, 0f);
            wind = CreateSource("Wind", ProceduralAudio.Wind(), true, 0f);
            hum = CreateSource("Hum", ProceduralAudio.Hum(), true, 0f);
            water = CreateSource("Water", ProceduralAudio.Water(), true, 1f);
            sfx = CreateSource("Sfx", null, false, 0f);
            steps = CreateSource("Steps", null, false, 0f);
            birdSource = CreateSource("Birds", null, false, 1f);

            water.minDistance = 3f;
            water.maxDistance = 30f;
            water.rolloffMode = AudioRolloffMode.Linear;
            birdSource.minDistance = 4f;
            birdSource.maxDistance = 40f;
            birdSource.rolloffMode = AudioRolloffMode.Linear;
            if (waterPoint != null) water.transform.position = waterPoint.position;
        }

        private void OnEnable()
        {
            GameEvents.TrashCollected += OnTrashCollected;
            GameEvents.TrashSorted += OnTrashSorted;
            GameEvents.MissionCompleted += OnMissionCompleted;
            GameEvents.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.TrashCollected -= OnTrashCollected;
            GameEvents.TrashSorted -= OnTrashSorted;
            GameEvents.MissionCompleted -= OnMissionCompleted;
            GameEvents.HealthChanged -= OnHealthChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            health = environment != null ? environment.Health01 : 0f;
            shownHealth = health;
            foreach (AudioSource s in new[] { musicClean, musicPolluted, wind, hum, water })
            {
                if (s.clip != null) s.Play();
            }
        }

        private void Update()
        {
            AudioListener.volume = GameSettings.Master;
            shownHealth = Mathf.MoveTowards(shownHealth, health, 0.25f * Time.unscaledDeltaTime);
            ApplyMix(shownHealth);
            UpdateBirds();
            UpdateFootsteps();
        }

        private void ApplyMix(float h)
        {
            float cleanMix = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.6f, h));
            float music = GameSettings.Music;
            float fx = GameSettings.Effects;

            musicClean.volume = cleanMix * 0.55f * music;
            musicPolluted.volume = (1f - cleanMix) * 0.7f * music;
            hum.volume = (1f - h) * 0.22f * fx;
            wind.volume = Mathf.Lerp(0.30f, 0.12f, h) * fx;
            water.volume = Mathf.Lerp(0.05f, 0.5f, h) * fx;
        }

        private void UpdateBirds()
        {
            if (shownHealth < 0.2f || Time.timeScale <= 0f) return;

            birdTimer -= Time.deltaTime;
            if (birdTimer > 0f) return;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.5f + 0.3f;
                birdSource.transform.position = cam.transform.position + dir.normalized * Random.Range(6f, 18f);
            }
            birdSource.PlayOneShot(birdClips[Random.Range(0, birdClips.Length)], 0.7f * GameSettings.Effects);
            birdTimer = Random.Range(Mathf.Lerp(9f, 2.5f, shownHealth), Mathf.Lerp(16f, 5f, shownHealth));
        }

        private void UpdateFootsteps()
        {
            if (player == null || Time.timeScale <= 0f) return;

            float speed = player.Speed01;
            if (speed < 0.12f)
            {
                stepTimer = 0.05f;
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f) return;

            steps.pitch = Random.Range(0.9f, 1.1f);
            steps.PlayOneShot(stepClips[Random.Range(0, stepClips.Length)], 0.45f * GameSettings.Effects);
            stepTimer = Mathf.Lerp(0.55f, 0.33f, speed);
        }

        private void OnTrashCollected(TrashData data)
        {
            if (sfx != null) sfx.pitch = Random.Range(0.96f, 1.06f);
            PlaySfx(pickup, 1.0f);
        }
        private void OnTrashSorted(TrashData data, bool isCorrect) => PlaySfx(isCorrect ? correct : wrong, isCorrect ? 0.7f : 0.8f);
        private void OnMissionCompleted(MissionData data) => PlaySfx(mission, 0.9f);
        private void OnHealthChanged(float health01) => health = health01;

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (clip != null && sfx != null) sfx.PlayOneShot(clip, volume * GameSettings.Effects);
        }

        private AudioSource CreateSource(string sourceName, AudioClip clip, bool loop, float spatialBlend)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.volume = 0f;
            if (clip == null) source.volume = 1f;
            return source;
        }
    }
}
