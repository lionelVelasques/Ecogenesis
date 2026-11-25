using System;
using System.Collections.Generic;
using System.IO;
using EcoGenesis.Data;
using EcoGenesis.Environment;
using EcoGenesis.Interaction;
using EcoGenesis.Missions;
using EcoGenesis.Player;
using UnityEngine;

namespace EcoGenesis.Core
{
    /// <summary>
    /// Guardado y carga en JSON (Application.persistentDataPath): salud del parque, puntos, misiones,
    /// mochila, residuos ya recogidos y posición del jugador. Autoguardado al completar misiones y cada 90 s.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [Serializable]
        public class MissionState
        {
            public string id;
            public int[] progress;
        }

        [Serializable]
        public class SaveData
        {
            public int version = 1;
            public string scene;
            public float health;
            public int points;
            public Vector3 playerPosition;
            public float playerYaw;
            public List<string> collected = new List<string>();
            public List<string> inventory = new List<string>();
            public List<string> completed = new List<string>();
            public List<MissionState> active = new List<MissionState>();
        }

        [SerializeField] private EnvironmentalManager environment;
        [SerializeField] private ScoreManager score;
        [SerializeField] private MissionManager missions;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerController player;
        [SerializeField] private TrashData[] knownTrash;
        [SerializeField] private MissionData[] knownMissions;
        [SerializeField, Min(10f)] private float autoSaveSeconds = 90f;

        private float timer;

        /// <summary>Lo activa el menú cuando empieza la partida.</summary>
        public bool AutoSave { get; set; }

        private static string FilePath => Path.Combine(Application.persistentDataPath, "ecogenesis_save.json");
        public bool HasSave => File.Exists(FilePath);

        /// <summary>Nombre de la escena (nivel) de la partida guardada, o null si no hay.</summary>
        public static string SavedSceneName()
        {
            if (!File.Exists(FilePath)) return null;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                return data != null ? data.scene : null;
            }
            catch (Exception) { return null; }
        }

        private void OnEnable() => GameEvents.MissionCompleted += OnMissionCompleted;
        private void OnDisable() => GameEvents.MissionCompleted -= OnMissionCompleted;

        private void Update()
        {
            if (!AutoSave || Time.timeScale <= 0f) return;
            timer += Time.deltaTime;
            if (timer >= autoSaveSeconds)
            {
                timer = 0f;
                Save(false);
            }
        }

        private void OnMissionCompleted(MissionData mission)
        {
            if (AutoSave) Save(false);
        }

        public void Delete()
        {
            if (HasSave) File.Delete(FilePath);
        }

        public void Save(bool announce = true)
        {
            var data = new SaveData
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                health = environment.Health,
                points = score.Points,
                playerPosition = player.transform.position,
                playerYaw = player.transform.eulerAngles.y
            };

            foreach (TrashItem item in FindObjectsByType<TrashItem>(FindObjectsInactive.Include))
            {
                if (!item.gameObject.activeSelf) data.collected.Add(item.name);
            }
            foreach (TrashData held in inventory.Items) data.inventory.Add(held.name);
            foreach (string id in missions.CompletedIds) data.completed.Add(id);
            foreach (MissionRuntime m in missions.Active)
                data.active.Add(new MissionState { id = m.Data.missionId, progress = (int[])m.Progress.Clone() });

            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            if (announce) GameEvents.RaiseMessage("Partida guardada.");
        }

        public bool Load()
        {
            if (!HasSave) return false;

            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
            catch (Exception) { return false; }
            if (data == null) return false;

            var collected = new HashSet<string>(data.collected);
            foreach (TrashItem item in FindObjectsByType<TrashItem>(FindObjectsInactive.Include))
            {
                if (collected.Contains(item.name)) item.gameObject.SetActive(false);
            }

            foreach (string trashName in data.inventory)
            {
                TrashData trash = FindTrash(trashName);
                if (trash != null) inventory.TryAdd(trash);
            }

            var restored = new List<MissionRuntime>();
            foreach (MissionState state in data.active)
            {
                MissionData mission = FindMission(state.id);
                if (mission == null) continue;

                var runtime = new MissionRuntime(mission);
                for (int i = 0; i < Mathf.Min(runtime.Progress.Length, state.progress.Length); i++)
                    runtime.Progress[i] = state.progress[i];
                restored.Add(runtime);
            }
            missions.RestoreState(data.completed, restored);

            score.SetPoints(data.points);
            environment.SetHealth(data.health);
            player.Teleport(data.playerPosition);
            player.transform.rotation = Quaternion.Euler(0f, data.playerYaw, 0f);
            GameEvents.RaiseMessage("Partida cargada.");
            return true;
        }

        private TrashData FindTrash(string assetName)
        {
            foreach (TrashData t in knownTrash)
            {
                if (t != null && t.name == assetName) return t;
            }
            return null;
        }

        private MissionData FindMission(string id)
        {
            foreach (MissionData m in knownMissions)
            {
                if (m != null && m.missionId == id) return m;
            }
            return null;
        }
    }
}
