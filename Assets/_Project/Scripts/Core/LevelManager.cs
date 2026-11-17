using System;
using EcoGenesis.Environment;
using EcoGenesis.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Core
{
    /// <summary>
    /// Controla el objetivo del nivel: se completa cuando no queda ningún residuo en el parque ni en la mochila.
    /// Calcula las estrellas (precisión de clasificación) y guarda el progreso.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public sealed class LevelResult
        {
            public int index;
            public string title;
            public int stars;
            public int points;
            public float accuracy;
            public float seconds;
            public bool hasNext;
        }

        public static event Action<LevelResult> Completed;

        /// <summary>Lo activa el menú cuando empieza la partida (el tiempo no corre en los menús).</summary>
        public bool Running { get; set; }
        public int Index { get; private set; }

        private EnvironmentalManager environment;
        private ScoreManager score;
        private PlayerInventory inventory;
        private float elapsed;
        private float checkTimer;
        private int startingTrash;
        private bool done;

        private void Awake()
        {
            Index = LevelCatalog.IndexOfScene(SceneManager.GetActiveScene().name);
        }

        private void Start()
        {
            environment = FindFirstObjectByType<EnvironmentalManager>();
            score = FindFirstObjectByType<ScoreManager>();
            inventory = FindFirstObjectByType<PlayerInventory>();
            startingTrash = FindObjectsByType<TrashItem>(FindObjectsSortMode.None).Length;
        }

        private void Update()
        {
            if (done || !Running) return;
            elapsed += Time.deltaTime;

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 0.5f;

            int remaining = FindObjectsByType<TrashItem>(FindObjectsSortMode.None).Length;
            bool carrying = inventory != null && inventory.Items.Count > 0;
            if (startingTrash > 0 && remaining == 0 && !carrying) Finish();
        }

        private void Finish()
        {
            done = true;
            if (environment != null) environment.SetHealth(100f);

            float accuracy = score != null ? score.Accuracy01 : 1f;
            int stars = accuracy >= 0.9f ? 3 : (accuracy >= 0.7f ? 2 : 1);
            if (Index >= 0) LevelCatalog.SaveResult(Index, stars);

            Completed?.Invoke(new LevelResult
            {
                index = Index,
                title = Index >= 0 ? LevelCatalog.Levels[Index].title : SceneManager.GetActiveScene().name,
                stars = stars,
                points = score != null ? score.Points : 0,
                accuracy = accuracy,
                seconds = elapsed,
                hasNext = Index >= 0 && Index + 1 < LevelCatalog.Levels.Length
            });
        }
    }
}
