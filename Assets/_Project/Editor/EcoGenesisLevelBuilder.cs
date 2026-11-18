using System.Collections.Generic;
using EcoGenesis.Audio;
using EcoGenesis.Core;
using EcoGenesis.Data;
using EcoGenesis.Environment;
using EcoGenesis.Missions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static EcoGenesis.EditorTools.EcoGenesisParkUpgrade;

namespace EcoGenesis.EditorTools
{
    /// <summary>
    /// EcoGenesis > 5: construye los 3 niveles (Parque Central, Ribera del Mantaro, Plaza y Mercado)
    /// con la interfaz nueva, el menú, la progresión y misiones propias de cada nivel.
    /// </summary>
    public static class EcoGenesisLevelBuilder
    {
        private const string Root = "Assets/_Project";
        private const string MissionFolder = Root + "/Data/Missions";
        private static float RiverZ = 16f;

        private sealed class Preset
        {
            public int index;
            public string path;
            public int seed;
            public bool river;
            public bool market;
            public float riverZ = 16f;
            public Vector3 marketCenter = new Vector3(0f, 0f, 20f);
            public bool removeFountain, removePond, removeCity;
            public int keepTreesEvery = 1;
            public Vector3 sunEuler = new Vector3(48f, -35f, 0f);
            public bool customLook;
            public Color groundDead = Hex("#7C8685"), groundClean = Hex("#7DB07A");
            public Color sunDead, sunClean, ambDead, ambClean, fogDead, fogClean;
            public Vector2 sunIntensity = new Vector2(1.2f, 1.55f), fogDensity = new Vector2(0.007f, 0.0018f);
            public Color skyPolTint, skyCleanTint;
            public float skyPolThick = 2.4f, skyCleanThick = 1f, skyCleanExp = 1.25f;
            public Dictionary<string, int> extraTrash = new Dictionary<string, int>();
        }

        [MenuItem("EcoGenesis/5. Construir los 3 niveles (HUD nuevo, menú y progresión)")]
        public static void BuildAllLevels()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var level1 = new Preset { index = 0, path = Root + "/Scenes/Park_Prototype.unity", seed = 11 };
            var level2 = new Preset { index = 1, path = Root + "/Scenes/Level2_Rio.unity", seed = 23, river = true };
            level2.extraTrash["Plastic"] = 4; level2.extraTrash["Glass"] = 2; level2.extraTrash["Organic"] = 2;
            level2.extraTrash["Paper"] = 2; level2.extraTrash["Hazardous"] = 2;
            var level3 = new Preset { index = 2, path = Root + "/Scenes/Level3_Mercado.unity", seed = 37, market = true };
            level3.extraTrash["Organic"] = 4; level3.extraTrash["Paper"] = 3; level3.extraTrash["Glass"] = 2;
            level3.extraTrash["Hazardous"] = 2; level3.extraTrash["Plastic"] = 2;

            // El nivel 1 va al final: Park_Prototype.unity se sobrescribe en cada construcción base.
            // Nivel 2: mañana con neblina junto al río, sin ciudad, fuente ni estanque.
            level2.riverZ = 16f;
            level2.removeFountain = true; level2.removePond = true; level2.removeCity = true;
            level2.sunEuler = new Vector3(28f, -70f, 0f);
            level2.customLook = true;
            level2.sunDead = Hex("#D5DCE6"); level2.sunClean = Hex("#FFF0D2");
            level2.ambDead = Hex("#A9B6C4"); level2.ambClean = Hex("#CFE3EA");
            level2.fogDead = Hex("#B4C2CE"); level2.fogClean = Hex("#D9ECF2");
            level2.fogDensity = new Vector2(0.011f, 0.003f); level2.sunIntensity = new Vector2(1.1f, 1.5f);
            level2.skyPolTint = new Color(0.58f, 0.62f, 0.70f); level2.skyCleanTint = new Color(0.5f, 0.5f, 0.5f);
            level2.skyPolThick = 1.8f; level2.skyCleanThick = 0.9f; level2.skyCleanExp = 1.25f;
            level2.groundDead = Hex("#9A8F78"); level2.groundClean = Hex("#79A86B");

            // Nivel 3: atardecer en la plaza, con pocos árboles y un círculo de puestos.
            level3.marketCenter = new Vector3(0f, 0f, 20f);
            level3.removePond = true; level3.keepTreesEvery = 3;
            level3.sunEuler = new Vector3(12f, -40f, 0f);
            level3.customLook = true;
            level3.sunDead = Hex("#E8C9A0"); level3.sunClean = Hex("#FFC98A");
            level3.ambDead = Hex("#B9A88F"); level3.ambClean = Hex("#E3C9A4");
            level3.fogDead = Hex("#CDB79B"); level3.fogClean = Hex("#F0CDA3");
            level3.fogDensity = new Vector2(0.010f, 0.0035f); level3.sunIntensity = new Vector2(1.15f, 1.5f);
            level3.skyPolTint = new Color(0.78f, 0.62f, 0.45f); level3.skyCleanTint = new Color(0.85f, 0.55f, 0.40f);
            level3.skyPolThick = 2.6f; level3.skyCleanThick = 1.6f; level3.skyCleanExp = 1.1f;
            level3.groundDead = Hex("#968C7C"); level3.groundClean = Hex("#C9B48F");

            Preset[] order = { level3, level2, level1 };
            for (int i = 0; i < order.Length; i++)
            {
                EditorUtility.DisplayProgressBar("EcoGenesis", "Construyendo nivel " + (order[i].index + 1) + " de 3…", i / 3f);
                BuildLevel(order[i]);
            }
            EditorUtility.ClearProgressBar();

            Seed = 11;
            BlockZ = null;
            BlockMarket = false;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(level1.path, true),
                new EditorBuildSettingsScene(level2.path, true),
                new EditorBuildSettingsScene(level3.path, true)
            };
            EditorSceneManager.OpenScene(level1.path, OpenSceneMode.Single);
            Debug.Log("[EcoGenesis] Niveles listos: Park_Prototype, Level2_Rio y Level3_Mercado. Pulsa Play.");
        }

        /// <summary>Crea el objeto "UI" (HUD + menú). Todo se construye por código al jugar.</summary>
        internal static void BuildNewUi()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject old = FindRoot(scene, "HUD");
            if (old != null) Object.DestroyImmediate(old);

            var ui = new GameObject("UI");
            ui.AddComponent<GameHud>();
            ui.AddComponent<GameMenu>();
        }

        // ------------------------------------------------------------------ CONSTRUCCIÓN

        private static void BuildLevel(Preset p)
        {
            Seed = p.seed;
            RiverZ = p.riverZ;
            BlockZ = p.river ? RiverZ : (float?)null;
            MarketCenter = p.marketCenter;
            BlockMarket = p.market;

            EcoGenesisProUpgrade.BuildAll();

            Scene scene = SceneManager.GetActiveScene();
            GameObject systems = FindRoot(scene, "_Systems");
            GameObject trash = FindRoot(scene, "Trash");
            GameObject elena = FindRoot(scene, "NPC_Elena");
            Transform extras = new GameObject("LevelExtras").transform;

            SetFloat(systems.GetComponent<EnvironmentalManager>(), "startingHealth", 15f);
            systems.AddComponent<LevelManager>();
            ApplyLook(scene, systems, p);
            ApplyLayout(scene, p);

            BuildGroundPatches(Group(extras, "GroundPatches"), 70);
            if (p.river) BuildRiver(Group(extras, "River"), trash);
            if (p.market) { BuildMarket(Group(extras, "Market")); MoveFountain(scene); }
            AddExtraTrash(trash, p.extraTrash);

            if (p.index == 1) AssignMissions(elena, MakeRiverMissions());
            if (p.index == 2) AssignMissions(elena, MakeMarketMissions());

            var save = systems.GetComponent<SaveSystem>();
            if (save != null) SetObjArray(save, "knownMissions", LoadAllMissions());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, p.path);
        }

        // ------------------------------------------------------------------ APARIENCIA Y DISEÑO POR NIVEL

        private static void ApplyLook(Scene scene, GameObject systems, Preset p)
        {
            GameObject sun = FindRoot(scene, "Directional Light");
            if (sun != null) sun.transform.rotation = Quaternion.Euler(p.sunEuler);
            if (!p.customLook) return;

            var restoration = systems.GetComponent<EnvironmentalRestoration>();
            if (restoration != null)
                restoration.SetLook(p.sunDead, p.sunClean, p.ambDead, p.ambClean, p.fogDead, p.fogClean, p.sunIntensity, p.fogDensity);

            var sky = systems.GetComponent<SkyRestoration>();
            if (sky != null) sky.SetLook(p.skyPolTint, p.skyCleanTint, p.skyPolThick, p.skyCleanThick, p.skyCleanExp);

            GameObject ground = FindRoot(scene, "Ground");
            if (ground != null)
            {
                var oldProp = ground.GetComponent<RestorationProp>();
                if (oldProp != null) Object.DestroyImmediate(oldProp);
                var rend = ground.GetComponent<MeshRenderer>();
                rend.sharedMaterial = Mat(p.groundDead);
                var rp = ground.AddComponent<RestorationProp>();
                rp.Configure(0f, 0.9f, 1f, 1f);
                rp.AddPart(rend, p.groundDead, p.groundClean);
            }
        }

        private static void ApplyLayout(Scene scene, Preset p)
        {
            GameObject park = FindRoot(scene, "Park");
            GameObject world = FindRoot(scene, "World");

            if (p.removeFountain && park != null) DestroyChild(park.transform, "Fountain");
            if (p.removePond)
            {
                GameObject pond = FindRoot(scene, "Pond");
                if (pond != null) Object.DestroyImmediate(pond);
                if (park != null) DestroyChild(park.transform, "PondDecor");
            }
            if (p.removeCity && world != null) DestroyChild(world.transform, "City");

            if (p.keepTreesEvery > 1 && park != null)
            {
                Transform trees = park.transform.Find("Trees");
                if (trees != null)
                {
                    var all = new List<Transform>();
                    foreach (Transform t in trees) all.Add(t);
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (i % p.keepTreesEvery != 0) Object.DestroyImmediate(all[i].gameObject);
                    }
                }
            }
        }

        private static void MoveFountain(Scene scene)
        {
            GameObject park = FindRoot(scene, "Park");
            Transform fountain = park != null ? park.transform.Find("Fountain") : null;
            if (fountain == null) return;

            fountain.position = MarketCenter;
            var audio = Object.FindFirstObjectByType<AudioDirector>();
            if (audio != null) SetObj(audio, "waterPoint", fountain);
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void BuildGroundPatches(Transform parent, int count)
        {
            Color[] dead = { Hex("#858A80"), Hex("#8A8570"), Hex("#7A8278") };
            Color[] clean = { Hex("#6FAE6B"), Hex("#7DB07A"), Hex("#8FBF73"), Hex("#A8A06B") };
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Spot(36f, false);
                float radius = Mathf.Round(Range(2f, 5f) * 2f) / 2f;
                Color deadColor = dead[rng.Next(dead.Length)];
                GameObject patch = Add(parent, "Patch" + i, Frustum(radius, radius, 0.02f, 9), deadColor,
                    p + new Vector3(0f, 0.006f, 0f), null, new Vector3(0f, Range(0f, 360f), 0f));
                float start = Range(0f, 0.5f);
                var rp = patch.AddComponent<RestorationProp>();
                rp.Configure(start, start + 0.4f, 1f, 1f);
                rp.AddPart(patch.GetComponent<MeshRenderer>(), deadColor, clean[rng.Next(clean.Length)]);
            }
        }

        // ------------------------------------------------------------------ NIVEL 2: RÍO

        private static void BuildRiver(Transform parent, GameObject trash)
        {
            Color deadWater = Hex("#6B5A3C");
            GameObject water = Add(parent, "River", Box(84f, 0.05f, 8f), deadWater, new Vector3(0f, -0.01f, RiverZ));
            var waterRp = water.AddComponent<RestorationProp>();
            waterRp.Configure(0.1f, 0.7f, 1f, 1f);
            waterRp.AddPart(water.GetComponent<MeshRenderer>(), deadWater, Blue);
            var riverAudio = Object.FindFirstObjectByType<AudioDirector>();
            if (riverAudio != null) SetObj(riverAudio, "waterPoint", water.transform);

            Color deadBank = Hex("#8A7A5F");
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject bank = Add(parent, "Bank", Box(84f, 0.1f, 2.4f), deadBank, new Vector3(0f, 0f, RiverZ + side * 5.2f));
                var rp = bank.AddComponent<RestorationProp>();
                rp.Configure(0.1f, 0.7f, 1f, 1f);
                rp.AddPart(bank.GetComponent<MeshRenderer>(), deadBank, Hex("#7DB07A"));
            }

            MakeBlocker(parent, new Vector3(-22.5f, 0.7f, RiverZ), new Vector3(39f, 1.4f, 8f));
            MakeBlocker(parent, new Vector3(22.5f, 0.7f, RiverZ), new Vector3(39f, 1.4f, 8f));

            var bridge = new GameObject("Bridge").transform;
            bridge.SetParent(parent, false);
            Add(bridge, "Deck", Box(5f, 0.2f, 9.5f), Earth, new Vector3(0f, 0f, RiverZ));
            foreach (float side in new[] { -2.4f, 2.4f })
            {
                GameObject rail = Add(bridge, "Rail", Box(0.15f, 0.9f, 9.5f), Hex("#7A5E3E"), new Vector3(side, 0.2f, RiverZ));
                var col = rail.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.45f, 0f);
                col.size = new Vector3(0.15f, 0.9f, 9.5f);
            }

            Color deadReed = Hex("#7B745A");
            for (int i = 0; i < 26; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                var reed = new GameObject("Reed" + i);
                reed.transform.SetParent(parent, false);
                reed.transform.position = new Vector3(Range(-36f, 36f), 0f, RiverZ + side * Range(4.4f, 5.6f));
                float start = Range(0.1f, 0.6f);
                var rp = reed.AddComponent<RestorationProp>();
                rp.Configure(start, start + 0.3f, 0.5f, 1f);
                GameObject blade = Add(reed.transform, "Blade", Frustum(0.04f, 0f, 1.3f, 4), deadReed, Vector3.zero);
                rp.AddPart(blade.GetComponent<MeshRenderer>(), deadReed, Green);
            }

            for (int i = 0; i < 14; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float s = Range(0.8f, 1.5f);
                Add(parent, "Stone" + i, Ico(0.5f, 0, 0.3f, 100 + (i % 4)), Hex("#8A8F91"),
                    new Vector3(Range(-36f, 36f), 0.15f * s, RiverZ + side * Range(3.8f, 4.6f)), new Vector3(s, s * 0.7f, s));
            }

            foreach (Transform item in trash.transform)
            {
                if (Mathf.Abs(item.position.z - RiverZ) < 6f)
                {
                    Vector3 n = Spot(24f, false);
                    item.position = new Vector3(n.x, 0f, n.z);
                }
            }
        }

        private static void MakeBlocker(Transform parent, Vector3 pos, Vector3 size)
        {
            var go = new GameObject("Blocker");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = size;
        }

        // ------------------------------------------------------------------ NIVEL 3: MERCADO

        private static void BuildMarket(Transform parent)
        {
            Color[] awnings = { Hex("#C0392B"), Blue, Hex("#E0A93B"), Green };
            Color[] fruit = { Hex("#D9553B"), Hex("#E8B93C"), Hex("#6FAE6B"), Hex("#C97A3A") };
            Color deadAwning = Hex("#8D9193");
            Color deadGoods = Hex("#7B745A");
            Vector3 center = MarketCenter;
            int count = 12;

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + 0.2f;
                Vector3 pos = new Vector3(center.x + Mathf.Sin(angle) * 11f, 0f, center.z + Mathf.Cos(angle) * 11f);
                if (Mathf.Abs(pos.x - center.x) < 3f || Vector3.Angle(pos - center, Vector3.back) < 40f) continue;

                var stall = new GameObject("Stall_" + i);
                stall.transform.SetParent(parent, false);
                stall.transform.position = pos;
                stall.transform.rotation = Quaternion.LookRotation(center - pos);

                float start = Range(0.1f, 0.4f);
                var rp = stall.AddComponent<RestorationProp>();
                rp.Configure(start, start + 0.35f, 1f, 1f);

                Add(stall.transform, "Counter", Box(2.6f, 0.9f, 1f), Earth, new Vector3(0f, 0f, 0.4f));
                Add(stall.transform, "BackWall", Box(2.6f, 2.3f, 0.1f), Hex("#7A5E3E"), new Vector3(0f, 0f, -0.6f));
                Add(stall.transform, "PostL", Frustum(0.05f, 0.05f, 2.3f, 6), Grey, new Vector3(-1.35f, 0f, 0.75f));
                Add(stall.transform, "PostR", Frustum(0.05f, 0.05f, 2.3f, 6), Grey, new Vector3(1.35f, 0f, 0.75f));

                GameObject awning = Add(stall.transform, "Awning", Box(3.1f, 0.1f, 2f), deadAwning, new Vector3(0f, 2.3f, 0.1f), null, new Vector3(12f, 0f, 0f));
                rp.AddPart(awning.GetComponent<MeshRenderer>(), deadAwning, awnings[i % awnings.Length]);

                for (int k = 0; k < 5; k++)
                {
                    GameObject goods = Add(stall.transform, "Goods" + k, Ico(0.16f, 0, 0.15f, 110 + k), deadGoods, new Vector3(-1f + k * 0.5f, 0.98f, 0.4f));
                    rp.AddPart(goods.GetComponent<MeshRenderer>(), deadGoods, fruit[(i + k) % fruit.Length]);
                }

                Add(stall.transform, "Crate", Box(0.8f, 0.5f, 0.8f), Hex("#8A6B45"), new Vector3(2.0f, 0f, 0.5f));

                var col = stall.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 1.1f, 0.3f);
                col.size = new Vector3(3f, 2.2f, 1.8f);
            }
        }

        // ------------------------------------------------------------------ RESIDUOS Y MISIONES

        private static void AddExtraTrash(GameObject trashRoot, Dictionary<string, int> extra)
        {
            foreach (KeyValuePair<string, int> pair in extra)
            {
                Transform template = trashRoot.transform.Find("Trash_" + pair.Key + "_0");
                if (template == null) continue;

                for (int n = 0; n < pair.Value; n++)
                {
                    Vector3 p = Spot(24f, false);
                    GameObject clone = Object.Instantiate(template.gameObject, trashRoot.transform);
                    clone.name = "Trash_" + pair.Key + "_" + (4 + n);
                    clone.transform.position = new Vector3(p.x, 0f, p.z);
                    clone.SetActive(true);
                }
            }
        }

        private static MissionData[] MakeRiverMissions()
        {
            MissionData m3 = MakeMission("L2_M3", "l2_m3", "Pilas junto al río",
                "Las pilas filtran veneno al agua. Recoge cuatro y deposítalas en el contenedor rojo.",
                "El río respira mejor gracias a ti.",
                new[] { Obj(ObjectiveType.CollectTrash, WasteType.Hazardous, 4), Obj(ObjectiveType.SortCorrectly, WasteType.Hazardous, 4) }, 120, 3f, null);
            MissionData m2 = MakeMission("L2_M2", "l2_m2", "Vidrio entre los juncos",
                "Hay botellas de vidrio escondidas entre los juncos. Ve con cuidado.",
                "Vidrio fuera de la orilla.",
                new[] { Obj(ObjectiveType.CollectTrash, WasteType.Glass, 4), Obj(ObjectiveType.SortCorrectly, WasteType.Glass, 4) }, 100, 3f, m3);
            MissionData m1 = MakeMission("L2_M1", "l2_m1", "Botellas en la orilla",
                "Bienvenido al Mantaro. Empieza por las botellas de plástico que arrastra la corriente.",
                "¡Cada botella menos es un pez menos en peligro!",
                new[] { Obj(ObjectiveType.CollectTrash, WasteType.Plastic, 6), Obj(ObjectiveType.SortCorrectly, WasteType.Plastic, 6) }, 80, 2f, m2);
            return new[] { m1 };
        }

        private static MissionData[] MakeMarketMissions()
        {
            MissionData m3 = MakeMission("L3_M3", "l3_m3", "Cartón y vidrio",
                "Ordena el papel y el vidrio que quedaron entre los puestos.",
                "La plaza luce como nueva.",
                new[] { Obj(ObjectiveType.SortCorrectly, WasteType.Paper, 6), Obj(ObjectiveType.SortCorrectly, WasteType.Glass, 4) }, 120, 3f, null);
            MissionData m2 = MakeMission("L3_M2", "l3_m2", "Sin pilas en el mercado",
                "Alguien tiró pilas junto a los puestos. No pueden ir a la basura común.",
                "Mercado más seguro para todos.",
                new[] { Obj(ObjectiveType.CollectTrash, WasteType.Hazardous, 4), Obj(ObjectiveType.SortCorrectly, WasteType.Hazardous, 4) }, 100, 3f, m3);
            MissionData m1 = MakeMission("L3_M1", "l3_m1", "Restos orgánicos",
                "Los restos de fruta y verdura se pueden convertir en compost. Recoge los orgánicos primero.",
                "¡Con eso haremos compost para el parque!",
                new[] { Obj(ObjectiveType.CollectTrash, WasteType.Organic, 6), Obj(ObjectiveType.SortCorrectly, WasteType.Organic, 6) }, 80, 2f, m2);
            return new[] { m1 };
        }

        private static void AssignMissions(GameObject elena, MissionData[] offered)
        {
            var giver = elena.GetComponent<MissionGiver>();
            if (giver != null) SetObjArray(giver, "offered", offered);
        }

        private static MissionData MakeMission(string file, string id, string title, string start, string complete,
            MissionObjective[] objectives, int points, float bonus, MissionData next)
        {
            EnsureFolder(MissionFolder);
            string path = MissionFolder + "/" + file + ".asset";
            AssetDatabase.DeleteAsset(path);

            var mission = ScriptableObject.CreateInstance<MissionData>();
            mission.missionId = id;
            mission.title = title;
            mission.startDialogue = start;
            mission.completeDialogue = complete;
            mission.objectives = new List<MissionObjective>(objectives);
            mission.rewardPoints = points;
            mission.restorationBonus = bonus;
            mission.nextMission = next;
            AssetDatabase.CreateAsset(mission, path);
            return mission;
        }

        private static MissionObjective Obj(ObjectiveType type, WasteType waste, int amount)
        {
            return new MissionObjective { type = type, wasteType = waste, requiredAmount = amount };
        }

        private static Object[] LoadAllMissions()
        {
            string[] guids = AssetDatabase.FindAssets("t:MissionData");
            var result = new Object[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return result;
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning("[EcoGenesis] Campo '" + field + "' no encontrado en " + target.GetType().Name);
                return;
            }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
