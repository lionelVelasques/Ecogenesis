using System.Collections.Generic;
using System.IO;
using EcoGenesis.Core;
using EcoGenesis.Data;
using EcoGenesis.Environment;
using EcoGenesis.Interaction;
using EcoGenesis.Missions;
using EcoGenesis.Player;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EcoGenesis.EditorTools
{
    /// <summary>
    /// Menú EcoGenesis: crea datos de ejemplo (ScriptableObjects) y construye la escena Park_Prototype
    /// con jugador, cámara, contenedores, residuos, NPC, sistemas, vegetación por etapas y HUD.
    /// </summary>
    public static class EcoGenesisSetupTool
    {
        private const string Root = "Assets/_Project";
        private const string DataPath = Root + "/Data";
        private const string ScenePath = Root + "/Scenes/Park_Prototype.unity";

        // ------------------------------------------------------------------ DATOS

        [MenuItem("EcoGenesis/1. Crear datos de ejemplo")]
        public static void CreateSampleData()
        {
            EnsureFolder(DataPath + "/Trash");
            EnsureFolder(DataPath + "/Missions");

            // Colores de contenedor según la NTP 900.058 de Perú (vidrio verde, papel azul, plástico blanco,
            // orgánico marrón, peligroso rojo). Se pueden cambiar en cada TrashData.
            CreateTrash("TrashData_BotellaPlastico", "Botella de plástico", WasteType.Plastic, 10, 1f, Hex("#F2F2F2"),
                "Los plásticos van al contenedor blanco, limpios y sin líquido.");
            CreateTrash("TrashData_CascaraPlatano", "Cáscara de plátano", WasteType.Organic, 10, 1f, Hex("#8B5E3C"),
                "Los restos de comida van al contenedor marrón: se pueden convertir en compost.");
            CreateTrash("TrashData_Periodico", "Periódico", WasteType.Paper, 10, 1f, Hex("#3F7FBF"),
                "El papel y el cartón secos van al contenedor azul.");
            CreateTrash("TrashData_BotellaVidrio", "Botella de vidrio", WasteType.Glass, 15, 1.2f, Hex("#3E9B55"),
                "El vidrio va al contenedor verde. ¡Cuidado al cargarlo!");
            CreateTrash("TrashData_PilaUsada", "Pila usada", WasteType.Hazardous, 20, 1.5f, Hex("#C0392B"),
                "Las pilas son peligrosas: van al contenedor rojo, nunca a la basura común.");

            TrashData plastic = LoadTrash("TrashData_BotellaPlastico");
            TrashData organic = LoadTrash("TrashData_CascaraPlatano");
            TrashData paper = LoadTrash("TrashData_Periodico");
            TrashData glass = LoadTrash("TrashData_BotellaVidrio");
            TrashData hazard = LoadTrash("TrashData_PilaUsada");

            MissionData m4 = CreateMission("Mission_04_Peligrosos", "m04", "Residuos peligrosos",
                "Las pilas contaminan el suelo. Recógelas con cuidado y deposítalas en el contenedor rojo.",
                "¡Gran trabajo! Ahora el parque está libre de residuos peligrosos.",
                new[] { Obj(ObjectiveType.CollectTrash, hazard.wasteType, 3), Obj(ObjectiveType.SortCorrectly, hazard.wasteType, 3) },
                100, 4f, null);
            MissionData m3 = CreateMission("Mission_03_PapelVidrio", "m03", "Papel y vidrio",
                "Ahora toca el papel y el vidrio. Cada uno tiene su contenedor.",
                "Casi listo. El parque empieza a respirar.",
                new[] { Obj(ObjectiveType.SortCorrectly, paper.wasteType, 3), Obj(ObjectiveType.SortCorrectly, glass.wasteType, 3) },
                80, 3f, m4);
            MissionData m2 = CreateMission("Mission_02_Organicos", "m02", "Restos orgánicos",
                "Los restos de comida van al contenedor marrón. Recoge tres y deposítalos.",
                "¡Con eso haremos compost!",
                new[] { Obj(ObjectiveType.CollectTrash, organic.wasteType, 3), Obj(ObjectiveType.SortCorrectly, organic.wasteType, 3) },
                60, 2f, m3);
            CreateMission("Mission_01_Plasticos", "m01", "Primer día en el parque",
                "Hola, soy Elena. Empecemos por los plásticos: recoge tres botellas y deposítalas en el contenedor blanco.",
                "¡Excelente! Así se empieza.",
                new[] { Obj(ObjectiveType.CollectTrash, plastic.wasteType, 3), Obj(ObjectiveType.SortCorrectly, plastic.wasteType, 3) },
                50, 2f, m2);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EcoGenesis] Datos de ejemplo creados en " + DataPath);
        }

        // ------------------------------------------------------------------ ESCENA

        [MenuItem("EcoGenesis/2. Construir escena Park_Prototype")]
        public static void BuildPrototypeScene()
        {
            CreateSampleData();
            foreach (string folder in new[] { "Scenes", "Materials", "Prefabs", "UI" }) EnsureFolder(Root + "/" + folder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var rng = new System.Random(7);

            TrashData[] trashTypes =
            {
                LoadTrash("TrashData_BotellaPlastico"), LoadTrash("TrashData_CascaraPlatano"),
                LoadTrash("TrashData_Periodico"), LoadTrash("TrashData_BotellaVidrio"), LoadTrash("TrashData_PilaUsada")
            };

            // ---- Suelo
            Material groundMat = MakeMaterial("M_GroundPolluted", Hex("#555D61"));
            GameObject ground = Prim(PrimitiveType.Cube, "Ground", new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60), groundMat, null);

            // ---- Jugador
            Material playerMat = MakeMaterial("M_Player", Hex("#4F8B57"));
            GameObject player = Prim(PrimitiveType.Capsule, "Player", new Vector3(0, 1.05f, 0), Vector3.one, playerMat, null);
            player.tag = "Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;
            GameObject nose = Prim(PrimitiveType.Cube, "Nose", new Vector3(0, 0.4f, 0.5f), new Vector3(0.3f, 0.3f, 0.3f), playerMat, player.transform);
            nose.transform.localPosition = new Vector3(0, 0.4f, 0.5f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerInventory>();
            player.AddComponent<InteractionDetector>();

            // ---- Cámara
            Camera cam = Camera.main;
            var camCtrl = cam.gameObject.AddComponent<CameraController>();
            SetObj(camCtrl, "target", player.transform);
            SetObj(camCtrl, "input", player.GetComponent<PlayerInputReader>());

            // ---- Contenedores (uno por tipo)
            Transform bins = new GameObject("Bins").transform;
            for (int i = 0; i < trashTypes.Length; i++)
            {
                TrashData t = trashTypes[i];
                Material mat = MakeMaterial("M_Bin_" + t.wasteType, t.tint);
                GameObject bin = Prim(PrimitiveType.Cube, "Bin_" + t.wasteType, new Vector3(-6 + i * 3f, 0.6f, 9f), new Vector3(1.2f, 1.2f, 1.2f), mat, bins);
                var binCtrl = bin.AddComponent<BinController>();
                binCtrl.Configure(t.wasteType, "contenedor de " + t.wasteType.DisplayName().ToLower(), t.tint);
            }

            // ---- Residuos (4 por tipo, esparcidos)
            Transform trashRoot = new GameObject("Trash").transform;
            foreach (TrashData t in trashTypes)
            {
                Material mat = MakeMaterial("M_Trash_" + t.wasteType, t.tint);
                for (int n = 0; n < 4; n++)
                {
                    Vector3 pos = RandomSpot(rng, 5f, 22f);
                    GameObject item = Prim(PrimitiveType.Cube, "Trash_" + t.wasteType + "_" + n, new Vector3(pos.x, 0.2f, pos.z), new Vector3(0.35f, 0.35f, 0.35f), mat, trashRoot);
                    item.GetComponent<Collider>().isTrigger = true;
                    item.AddComponent<TrashItem>().Configure(t);
                }
            }

            // ---- Vegetación por etapas (se activa al restaurar)
            GameObject[] stageRoots = new GameObject[3];
            Material trunkMat = MakeMaterial("M_Trunk", Hex("#94734E"));
            Material leafMat = MakeMaterial("M_Leaf", Hex("#4F8B57"));
            Material flowerMat = MakeMaterial("M_Flower", Hex("#E9D9B4"));
            for (int s = 0; s < 3; s++)
            {
                stageRoots[s] = new GameObject("Vegetation_Stage" + (s + 1));
                for (int k = 0; k < 8; k++)
                {
                    Vector3 p = RandomSpot(rng, 8f, 26f);
                    if (s < 2)
                    {
                        float h = 1.5f + (float)rng.NextDouble();
                        Prim(PrimitiveType.Cylinder, "Trunk", new Vector3(p.x, h * 0.5f, p.z), new Vector3(0.4f, h * 0.5f, 0.4f), trunkMat, stageRoots[s].transform);
                        GameObject crown = Prim(PrimitiveType.Sphere, "Crown", new Vector3(p.x, h + 0.8f, p.z), new Vector3(2.4f, 2.2f, 2.4f), leafMat, stageRoots[s].transform);
                        Object.DestroyImmediate(crown.GetComponent<Collider>());
                    }
                    else
                    {
                        GameObject flower = Prim(PrimitiveType.Sphere, "Flower", new Vector3(p.x, 0.2f, p.z), new Vector3(0.5f, 0.5f, 0.5f), flowerMat, stageRoots[s].transform);
                        Object.DestroyImmediate(flower.GetComponent<Collider>());
                    }
                }
                stageRoots[s].SetActive(false);
            }

            // ---- Agua
            Material waterMat = MakeMaterial("M_Water", Hex("#6B5A3C"));
            GameObject pond = Prim(PrimitiveType.Cylinder, "Pond", new Vector3(16, 0.03f, -12), new Vector3(7, 0.03f, 7), waterMat, null);
            Object.DestroyImmediate(pond.GetComponent<Collider>());

            // ---- Sistemas
            GameObject systems = new GameObject("_Systems");
            systems.AddComponent<ScoreManager>();
            var env = systems.AddComponent<EnvironmentalManager>();
            var missions = systems.AddComponent<MissionManager>();
            var restoration = systems.AddComponent<EnvironmentalRestoration>();
            restoration.ConfigureDefaults();
            restoration.SetReferences(env, GameObject.Find("Directional Light").GetComponent<Light>(), pond.GetComponent<Renderer>());
            restoration.AddStage("Brotes", 0.2f, new[] { stageRoots[0] }, null);
            restoration.AddStage("Bosque", 0.5f, new[] { stageRoots[1] }, null);
            restoration.AddStage("Flores", 0.8f, new[] { stageRoots[2] }, null);
            EditorUtility.SetDirty(restoration);

            // ---- NPC Elena
            Material npcMat = MakeMaterial("M_Elena", Hex("#4A91B8"));
            GameObject elena = Prim(PrimitiveType.Capsule, "NPC_Elena", new Vector3(-4, 1f, -3), Vector3.one, npcMat, null);
            var giver = elena.AddComponent<MissionGiver>();
            SetObj(giver, "missions", missions);
            SetObjArray(giver, "offered", new Object[] { AssetDatabase.LoadAssetAtPath<MissionData>(DataPath + "/Missions/Mission_01_Plasticos.asset") });

            BuildHud();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[EcoGenesis] Escena creada: " + ScenePath + ". Pulsa Play, habla con Elena (E) y recoge residuos.");
        }

        // ------------------------------------------------------------------ HUD

        private static void BuildHud()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var hud = canvasGo.AddComponent<HUDController>();
            Transform root = canvasGo.transform;

            TextMeshProUGUI points = MakeText(root, "PointsText", new Vector2(0, 1), new Vector2(20, -20), new Vector2(500, 40), TextAlignmentOptions.TopLeft, 30);
            TextMeshProUGUI health = MakeText(root, "HealthText", new Vector2(0, 1), new Vector2(20, -60), new Vector2(500, 40), TextAlignmentOptions.TopLeft, 26);
            TextMeshProUGUI inventory = MakeText(root, "InventoryText", new Vector2(0, 0), new Vector2(20, 20), new Vector2(600, 260), TextAlignmentOptions.BottomLeft, 24);
            TextMeshProUGUI missions = MakeText(root, "MissionsText", new Vector2(1, 1), new Vector2(-20, -20), new Vector2(700, 400), TextAlignmentOptions.TopRight, 24);
            TextMeshProUGUI prompt = MakeText(root, "PromptText", new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(900, 60), TextAlignmentOptions.Center, 34);
            TextMeshProUGUI message = MakeText(root, "MessageText", new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1200, 100), TextAlignmentOptions.Center, 30);

            GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.name = "HealthBar";
            sliderGo.transform.SetParent(root, false);
            var rt = (RectTransform)sliderGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -100);
            rt.sizeDelta = new Vector2(360, 24);
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.interactable = false;
            if (slider.handleRect != null) slider.handleRect.gameObject.SetActive(false);
            Transform background = sliderGo.transform.Find("Background");
            if (background != null) background.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = Hex("#4F8B57");

            SetObj(hud, "promptText", prompt);
            SetObj(hud, "pointsText", points);
            SetObj(hud, "healthText", health);
            SetObj(hud, "healthBar", slider);
            SetObj(hud, "inventoryText", inventory);
            SetObj(hud, "missionsText", missions);
            SetObj(hud, "messageText", message);
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        // ------------------------------------------------------------------ HELPERS

        private static Vector3 RandomSpot(System.Random rng, float minRadius, float maxRadius)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float radius = minRadius + (float)rng.NextDouble() * (maxRadius - minRadius);
                var p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                bool nearBins = Mathf.Abs(p.x) < 9f && Mathf.Abs(p.z - 9f) < 3f;
                bool nearPond = Vector3.Distance(p, new Vector3(16, 0, -12)) < 6f;
                if (!nearBins && !nearPond) return p;
            }
            return new Vector3(minRadius, 0f, minRadius);
        }

        private static GameObject Prim(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetObj(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning("[EcoGenesis] Campo '" + field + "' no encontrado en " + target.GetType().Name);
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static TrashData LoadTrash(string file) =>
            AssetDatabase.LoadAssetAtPath<TrashData>(DataPath + "/Trash/" + file + ".asset");

        private static void CreateTrash(string file, string displayName, WasteType type, int points, float weight, Color tint, string tip)
        {
            string path = DataPath + "/Trash/" + file + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TrashData>(path) != null) return;

            var data = ScriptableObject.CreateInstance<TrashData>();
            data.displayName = displayName;
            data.wasteType = type;
            data.points = points;
            data.restorationWeight = weight;
            data.tint = tint;
            data.recyclingTip = tip;
            AssetDatabase.CreateAsset(data, path);
        }

        private static MissionObjective Obj(ObjectiveType type, WasteType waste, int amount)
        {
            return new MissionObjective { type = type, wasteType = waste, requiredAmount = amount };
        }

        private static MissionData CreateMission(string file, string id, string title, string start, string complete,
            MissionObjective[] objectives, int rewardPoints, float bonus, MissionData next)
        {
            string path = DataPath + "/Missions/" + file + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (existing != null) return existing;

            var mission = ScriptableObject.CreateInstance<MissionData>();
            mission.missionId = id;
            mission.title = title;
            mission.startDialogue = start;
            mission.completeDialogue = complete;
            mission.objectives = new List<MissionObjective>(objectives);
            mission.rewardPoints = rewardPoints;
            mission.restorationBonus = bonus;
            mission.nextMission = next;
            AssetDatabase.CreateAsset(mission, path);
            return mission;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }
    }
}
