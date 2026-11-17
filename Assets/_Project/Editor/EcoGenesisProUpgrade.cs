using EcoGenesis.Audio;
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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EcoGenesis.EditorTools.EcoGenesisParkUpgrade;

namespace EcoGenesis.EditorTools
{
    /// <summary>
    /// EcoGenesis > 4. Versión completa: parque low-poly + ciudad y montañas andinas, cielo vivo, partículas,
    /// audio procedural, guardado, HUD nuevo y menús (principal y pausa).
    /// </summary>
    public static class EcoGenesisProUpgrade
    {
        private const string ScenePath = "Assets/_Project/Scenes/Park_Prototype.unity";
        private static readonly Color PanelDark = new Color(0.05f, 0.11f, 0.08f, 0.78f);
        private static readonly Color Soft = new Color(0.78f, 0.9f, 0.8f, 1f);

        [MenuItem("EcoGenesis/4. Versión completa (menú, audio, cielo, ciudad)")]
        public static void BuildAll()
        {
            EcoGenesisParkUpgrade.Build();

            Scene scene = SceneManager.GetActiveScene();
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "_Systems");
            GameObject pond = FindRoot(scene, "Pond");

            Transform world = new GameObject("World").transform;
            BuildOutskirts(Group(world, "Outskirts"));
            BuildCity(Group(world, "City"), 34);
            BuildMountains(Group(world, "Mountains"));
            BuildDetails(Group(world, "Details"));

            SetupSky(systems);
            SetupParticles(player);
            SaveSystem save = SetupSaveAndAudio(systems, player, pond);
            EcoGenesisLevelBuilder.BuildNewUi();
            TuneCamera();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[EcoGenesis] Versión completa lista. Pulsa Play: verás el menú principal.");
        }

        // ------------------------------------------------------------------ ALREDEDORES

        private static void BuildOutskirts(Transform parent)
        {
            Color dead = Hex("#838A86");
            GameObject outer = Add(parent, "OuterGround", Box(420f, 0.5f, 420f), dead, new Vector3(0f, -0.56f, 0f));
            var rp = outer.AddComponent<RestorationProp>();
            rp.Configure(0f, 1f, 1f, 1f);
            rp.AddPart(outer.GetComponent<MeshRenderer>(), dead, Hex("#86A97A"));

            Color asphalt = Hex("#3F4447");
            Add(parent, "Road_N", Box(110f, 0.04f, 7f), asphalt, new Vector3(0f, -0.06f, 47f));
            Add(parent, "Road_S", Box(110f, 0.04f, 7f), asphalt, new Vector3(0f, -0.06f, -47f));
            Add(parent, "Road_E", Box(7f, 0.04f, 110f), asphalt, new Vector3(47f, -0.06f, 0f));
            Add(parent, "Road_W", Box(7f, 0.04f, 110f), asphalt, new Vector3(-47f, -0.06f, 0f));

            for (int i = -6; i <= 6; i++)
            {
                float p = i * 8f;
                Add(parent, "Dash", Box(3f, 0.05f, 0.25f), Cream, new Vector3(p, -0.05f, 47f));
                Add(parent, "Dash", Box(3f, 0.05f, 0.25f), Cream, new Vector3(p, -0.05f, -47f));
                Add(parent, "Dash", Box(0.25f, 0.05f, 3f), Cream, new Vector3(47f, -0.05f, p));
                Add(parent, "Dash", Box(0.25f, 0.05f, 3f), Cream, new Vector3(-47f, -0.05f, p));
            }
        }

        private static void BuildCity(Transform parent, int count)
        {
            Color[] pastel = { Hex("#C9B79A"), Hex("#B7C4B0"), Hex("#D3A68C"), Hex("#9FB4C0"), Hex("#D9CBA5") };
            Color deadWall = Hex("#7E8284");
            Color roofColor = Hex("#A3513B");
            Color window = Hex("#E3CE9A");

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Range(-0.05f, 0.05f);
                float radius = Range(60f, 78f);
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                float w = Mathf.Round(Range(7f, 13f));
                float d = Mathf.Round(Range(7f, 11f));
                float h = Mathf.Round(Range(8f, 22f));

                var building = new GameObject("Building_" + i);
                building.transform.SetParent(parent, false);
                building.transform.position = pos;
                building.transform.rotation = Quaternion.LookRotation(-pos.normalized);

                var rp = building.AddComponent<RestorationProp>();
                rp.Configure(0f, 1f, 1f, 1f);
                GameObject body = Add(building.transform, "Body", Box(w, h, d), deadWall, Vector3.zero);
                rp.AddPart(body.GetComponent<MeshRenderer>(), deadWall, pastel[rng.Next(pastel.Length)]);

                Add(building.transform, "Roof", Frustum(Mathf.Max(w, d) * 0.74f, 0f, 2.5f, 4), roofColor, new Vector3(0f, h, 0f), null, new Vector3(0f, 45f, 0f));

                int rows = Mathf.Min(3, Mathf.FloorToInt(h / 4f));
                int cols = Mathf.Min(3, Mathf.FloorToInt(w / 3f));
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        float x = (c - (cols - 1) * 0.5f) * 2.8f;
                        Add(building.transform, "Window", Box(0.9f, 1.4f, 0.1f), window, new Vector3(x, 2f + r * 3.5f, d * 0.5f + 0.01f));
                    }
                }
            }
        }

        private static void BuildMountains(Transform parent)
        {
            Color snow = Hex("#F1F3F2");
            int count = 18;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Range(-0.08f, 0.08f);
                float radius = Range(140f, 175f);
                float baseR = Mathf.Round(Range(35f, 55f));
                float h = Mathf.Round(Range(34f, 62f));

                var mountain = new GameObject("Mountain_" + i);
                mountain.transform.SetParent(parent, false);
                mountain.transform.position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                mountain.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);

                Color dead = Hex("#7E8078");
                var rp = mountain.AddComponent<RestorationProp>();
                rp.Configure(0f, 1f, 1f, 1f);
                GameObject body = Add(mountain.transform, "Body", Frustum(baseR, 0f, h, 6), dead, Vector3.zero);
                rp.AddPart(body.GetComponent<MeshRenderer>(), dead, Hex("#6F8A5E"));
                Add(mountain.transform, "Snow", Frustum(baseR * 0.333f, 0f, h * 0.32f, 6), snow, new Vector3(0f, h * 0.68f, 0f));
            }

            for (int i = 0; i < 10; i++)
            {
                float angle = (i / 10f) * Mathf.PI * 2f + Range(-0.1f, 0.1f);
                float radius = Range(98f, 112f);
                var hill = new GameObject("Hill_" + i);
                hill.transform.SetParent(parent, false);
                hill.transform.position = new Vector3(Mathf.Cos(angle) * radius, -2f, Mathf.Sin(angle) * radius);
                var rp = hill.AddComponent<RestorationProp>();
                rp.Configure(0f, 1f, 1f, 1f);
                Color dead = Hex("#7F8A78");
                GameObject body = Add(hill.transform, "Body", Ico(30f, 1, 0.12f, 80 + (i % 4)), dead, Vector3.zero, new Vector3(1.2f, 0.3f, 1f));
                rp.AddPart(body.GetComponent<MeshRenderer>(), dead, Hex("#7FA36E"));
            }
        }

        private static void BuildDetails(Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = Spot(30f, true);
                var sign = new GameObject("Signpost_" + i);
                sign.transform.SetParent(parent, false);
                sign.transform.position = p;
                sign.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                Add(sign.transform, "Pole", Frustum(0.06f, 0.05f, 2.2f, 6), Earth, Vector3.zero);
                Add(sign.transform, "Board", Box(1.1f, 0.35f, 0.06f), Cream, new Vector3(0f, 1.7f, 0f));
                var col = sign.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1.1f, 0f);
                col.radius = 0.15f;
                col.height = 2.2f;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector3 p = Spot(30f, true);
                var table = new GameObject("PicnicTable_" + i);
                table.transform.SetParent(parent, false);
                table.transform.position = p;
                table.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                Add(table.transform, "Top", Box(1.8f, 0.07f, 0.8f), Earth, new Vector3(0f, 0.75f, 0f));
                Add(table.transform, "BenchA", Box(1.8f, 0.06f, 0.3f), Earth, new Vector3(0f, 0.45f, 0.65f));
                Add(table.transform, "BenchB", Box(1.8f, 0.06f, 0.3f), Earth, new Vector3(0f, 0.45f, -0.65f));
                Add(table.transform, "LegA", Box(0.1f, 0.75f, 1.4f), Hex("#7A5E3E"), new Vector3(-0.7f, 0f, 0f));
                Add(table.transform, "LegB", Box(0.1f, 0.75f, 1.4f), Hex("#7A5E3E"), new Vector3(0.7f, 0f, 0f));
                var col = table.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.45f, 0f);
                col.size = new Vector3(1.8f, 0.9f, 1.6f);
            }

            Color deadStem = Hex("#8A8E78");
            Color deadCap = Hex("#7A6F5F");
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = Spot(33f, true);
                var mushroom = new GameObject("Mushroom_" + i);
                mushroom.transform.SetParent(parent, false);
                mushroom.transform.position = p;
                float start = Range(0.45f, 0.75f);
                var rp = mushroom.AddComponent<RestorationProp>();
                rp.Configure(start, start + 0.2f, 0f, 1f);
                GameObject stem = Add(mushroom.transform, "Stem", Frustum(0.05f, 0.04f, 0.12f, 6), deadStem, Vector3.zero);
                GameObject cap = Add(mushroom.transform, "Cap", Ico(0.11f, 1, 0.05f, 90 + (i % 3)), deadCap, new Vector3(0f, 0.14f, 0f), new Vector3(1f, 0.6f, 1f));
                rp.AddPart(stem.GetComponent<MeshRenderer>(), deadStem, Cream);
                rp.AddPart(cap.GetComponent<MeshRenderer>(), deadCap, Hex("#C0392B"));
            }

            for (int i = 0; i < 5; i++)
            {
                Vector3 p = Spot(33f, true);
                GameObject log = Add(parent, "Log_" + i, Frustum(0.25f, 0.25f, 2.2f, 7), Hex("#6B5642"),
                    p + new Vector3(0f, 0.25f, 0f), null, new Vector3(0f, Range(0f, 360f), 90f));
                var col = log.AddComponent<CapsuleCollider>();
                col.radius = 0.25f;
                col.height = 2.2f;
                col.direction = 1;
                col.center = new Vector3(0f, 1.1f, 0f);
            }

            float[][] canSpots = { new[] { 2.6f, -18f }, new[] { -2.6f, 6f }, new[] { 2.6f, 20f }, new[] { 9f, -2.6f } };
            for (int i = 0; i < canSpots.Length; i++)
            {
                var can = new GameObject("ParkBin_" + i);
                can.transform.SetParent(parent, false);
                can.transform.position = new Vector3(canSpots[i][0], 0f, canSpots[i][1]);
                Add(can.transform, "Body", Frustum(0.35f, 0.3f, 0.8f, 8), Hex("#3F7A4A"), Vector3.zero);
                Add(can.transform, "Lid", Frustum(0.37f, 0.37f, 0.08f, 8), Hex("#8A8F91"), new Vector3(0f, 0.8f, 0f));
                var col = can.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.45f, 0f);
                col.radius = 0.35f;
                col.height = 0.9f;
            }
        }

        // ------------------------------------------------------------------ CIELO, PARTÍCULAS, AUDIO, GUARDADO

        private static void SetupSky(GameObject systems)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("[EcoGenesis] No se encontró Skybox/Procedural; se mantiene el cielo por defecto.");
                return;
            }

            string path = "Assets/_Project/Materials/M_Sky.mat";
            AssetDatabase.DeleteAsset(path);
            var sky = new Material(shader);
            sky.SetFloat("_SunSize", 0.05f);
            AssetDatabase.CreateAsset(sky, path);
            RenderSettings.skybox = sky;

            var restoration = systems.AddComponent<SkyRestoration>();
            SetObj(restoration, "skySource", sky);
        }

        private static void SetupParticles(GameObject player)
        {
            var pollen = new GameObject("Pollen");
            var particles = pollen.AddComponent<PollenParticles>();
            SetObj(particles, "target", player.transform);
        }

        private static SaveSystem SetupSaveAndAudio(GameObject systems, GameObject player, GameObject pond)
        {
            var env = systems.GetComponent<EnvironmentalManager>();
            var score = systems.GetComponent<ScoreManager>();
            var missions = systems.GetComponent<MissionManager>();

            var audioGo = new GameObject("_Audio");
            var director = audioGo.AddComponent<AudioDirector>();
            SetObj(director, "environment", env);
            SetObj(director, "player", player.GetComponent<PlayerController>());
            SetObj(director, "waterPoint", pond.transform);

            var save = systems.AddComponent<SaveSystem>();
            SetObj(save, "environment", env);
            SetObj(save, "score", score);
            SetObj(save, "missions", missions);
            SetObj(save, "inventory", player.GetComponent<PlayerInventory>());
            SetObj(save, "player", player.GetComponent<PlayerController>());
            SetObjArray(save, "knownTrash", LoadAll<TrashData>());
            SetObjArray(save, "knownMissions", LoadAll<MissionData>());
            return save;
        }

        private static Object[] LoadAll<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var result = new Object[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return result;
        }

        private static void TuneCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.farClipPlane = 700f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        // ------------------------------------------------------------------ INTERFAZ

        private static void BuildUi(GameObject player, SaveSystem save)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject oldHud = FindRoot(scene, "HUD");
            if (oldHud != null) Object.DestroyImmediate(oldHud);

            if (Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 0)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            CanvasGroup hudGroup = BuildHud();
            MenuController menu = BuildMenus(player, save, hudGroup);
            menu.name = "MenuUI";
        }

        private static CanvasGroup BuildHud()
        {
            GameObject canvasGo = UiCanvas("HUD", 0);
            var group = canvasGo.AddComponent<CanvasGroup>();
            var hud = canvasGo.AddComponent<HUDController>();
            Transform root = canvasGo.transform;

            // Puntos y salud del parque
            RectTransform topLeft = UiPanel(root, "StatusPanel", new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(540f, 150f), PanelDark);
            TextMeshProUGUI points = UiText(topLeft, "PointsText", "", 34f, TextAlignmentOptions.TopLeft, Cream, new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(490f, 44f));
            TextMeshProUGUI health = UiText(topLeft, "HealthText", "", 25f, TextAlignmentOptions.TopLeft, Soft, new Vector2(0f, 1f), new Vector2(24f, -64f), new Vector2(490f, 34f));
            Slider bar = UiSlider(topLeft, "HealthBar", new Vector2(0f, 1f), new Vector2(24f, -106f), new Vector2(490f, 24f));
            bar.interactable = false;
            if (bar.handleRect != null) bar.handleRect.gameObject.SetActive(false);

            // Misiones
            RectTransform topRight = UiPanel(root, "MissionPanel", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(660f, 330f), PanelDark);
            UiText(topRight, "MissionTitle", "MISIONES", 22f, TextAlignmentOptions.TopLeft, Hex("#8FC79A"), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(400f, 30f));
            TextMeshProUGUI missions = UiText(topRight, "MissionsText", "", 24f, TextAlignmentOptions.TopLeft, Cream, new Vector2(0f, 1f), new Vector2(24f, -50f), new Vector2(612f, 270f));

            // Mochila
            RectTransform bottomLeft = UiPanel(root, "InventoryPanel", new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(600f, 250f), PanelDark);
            TextMeshProUGUI inventory = UiText(bottomLeft, "InventoryText", "", 24f, TextAlignmentOptions.TopLeft, Cream, new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(552f, 220f));

            // Indicación de interacción y avisos
            RectTransform promptPanel = UiPanel(root, "PromptPanel", new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(780f, 84f), PanelDark);
            TextMeshProUGUI prompt = UiText(promptPanel, "PromptText", "", 36f, TextAlignmentOptions.Center, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740f, 70f));
            RectTransform messagePanel = UiPanel(root, "MessagePanel", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1250f, 112f), PanelDark);
            TextMeshProUGUI message = UiText(messagePanel, "MessageText", "", 30f, TextAlignmentOptions.Center, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 100f));

            var promptHide = canvasGo.AddComponent<HudPanelAutoHide>();
            promptHide.watched = prompt;
            promptHide.panel = promptPanel.gameObject;
            var messageHide = canvasGo.AddComponent<HudPanelAutoHide>();
            messageHide.watched = message;
            messageHide.panel = messagePanel.gameObject;

            // Mira
            RectTransform dot = UiPanel(root, "Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f), new Color(0.91f, 0.85f, 0.71f, 0.75f));
            dot.GetComponent<Image>().raycastTarget = false;

            SetObj(hud, "promptText", prompt);
            SetObj(hud, "pointsText", points);
            SetObj(hud, "healthText", health);
            SetObj(hud, "healthBar", bar);
            SetObj(hud, "inventoryText", inventory);
            SetObj(hud, "missionsText", missions);
            SetObj(hud, "messageText", message);

            return group;
        }

        private static MenuController BuildMenus(GameObject player, SaveSystem save, CanvasGroup hudGroup)
        {
            GameObject canvasGo = UiCanvas("MenuCanvas", 10);
            var menu = canvasGo.AddComponent<MenuController>();
            Transform root = canvasGo.transform;

            // ---- Menú principal
            GameObject main = UiGroup(root, "MainPanel");
            RectTransform side = UiPanel(main.transform, "SidePanel", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(820f, 1080f), new Color(0.04f, 0.1f, 0.07f, 0.62f));
            side.anchorMin = new Vector2(0f, 0f);
            side.anchorMax = new Vector2(0f, 1f);
            side.pivot = new Vector2(0f, 0.5f);
            side.sizeDelta = new Vector2(820f, 0f);
            side.GetComponent<Image>().raycastTarget = false;

            UiText(main.transform, "Title", "ECOGENESIS", 128f, TextAlignmentOptions.TopLeft, Cream, new Vector2(0f, 1f), new Vector2(110f, -150f), new Vector2(760f, 150f)).fontStyle = FontStyles.Bold;
            UiText(main.transform, "Subtitle", "Restaura el parque. Cuida el planeta.", 34f, TextAlignmentOptions.TopLeft, Hex("#8FC79A"), new Vector2(0f, 1f), new Vector2(116f, -300f), new Vector2(700f, 50f));

            RectTransform buttons = UiRect(main.transform, "Buttons", new Vector2(0f, 1f), new Vector2(116f, -400f), new Vector2(460f, 520f));
            var layout = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            menu.continueButton = UiButton(buttons, "Continuar");
            menu.newGameButton = UiButton(buttons, "Nueva partida");
            menu.optionsButton = UiButton(buttons, "Opciones");
            menu.creditsButton = UiButton(buttons, "Créditos");
            menu.quitButton = UiButton(buttons, "Salir");

            UiText(main.transform, "Footer", "ODS 11 · 12 · 15   |   Huancayo, Perú", 24f, TextAlignmentOptions.BottomLeft, Soft, new Vector2(0f, 0f), new Vector2(116f, 40f), new Vector2(700f, 40f));

            // ---- Opciones
            GameObject options = UiGroup(root, "OptionsPanel");
            RectTransform optionsCard = UiPanel(options.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f), PanelDark);
            UiText(optionsCard, "Title", "OPCIONES", 56f, TextAlignmentOptions.Center, Cream, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 80f)).fontStyle = FontStyles.Bold;
            menu.masterSlider = UiOptionRow(optionsCard, "Volumen general", -170f);
            menu.musicSlider = UiOptionRow(optionsCard, "Música", -270f);
            menu.effectsSlider = UiOptionRow(optionsCard, "Efectos y ambiente", -370f);
            menu.optionsBackButton = UiButton(optionsCard, "Volver");
            PlaceBottom(menu.optionsBackButton, 40f);

            // ---- Créditos
            GameObject credits = UiGroup(root, "CreditsPanel");
            RectTransform creditsCard = UiPanel(credits.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 700f), PanelDark);
            UiText(creditsCard, "Title", "CRÉDITOS", 56f, TextAlignmentOptions.Center, Cream, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 80f)).fontStyle = FontStyles.Bold;
            UiText(creditsCard, "Body",
                "EcoGenesis 3D\nVideojuego de aventura, exploración y restauración ambiental.\n\n" +
                "Basado en la problemática real de los residuos sólidos.\nObjetivos de Desarrollo Sostenible 11, 12 y 15.\n\n" +
                "Motor: Unity 6 (URP)   ·   Código: C#\nModelado: Blender   ·   Control de versiones: Git y GitHub\n\n" +
                "Universidad Continental — Huancayo, Perú",
                28f, TextAlignmentOptions.Top, Cream, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(880f, 420f));
            menu.creditsBackButton = UiButton(creditsCard, "Volver");
            PlaceBottom(menu.creditsBackButton, 40f);

            // ---- Pausa
            GameObject pause = UiGroup(root, "PausePanel");
            RectTransform pauseCard = UiPanel(pause.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 560f), PanelDark);
            UiText(pauseCard, "Title", "PAUSA", 56f, TextAlignmentOptions.Center, Cream, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(500f, 80f)).fontStyle = FontStyles.Bold;
            RectTransform pauseButtons = UiRect(pauseCard, "Buttons", new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(460f, 380f));
            var pauseLayout = pauseButtons.gameObject.AddComponent<VerticalLayoutGroup>();
            pauseLayout.spacing = 16f;
            pauseLayout.childAlignment = TextAnchor.UpperCenter;
            pauseLayout.childControlWidth = true;
            pauseLayout.childControlHeight = true;
            pauseLayout.childForceExpandWidth = false;
            pauseLayout.childForceExpandHeight = false;
            menu.resumeButton = UiButton(pauseButtons, "Continuar");
            menu.saveButton = UiButton(pauseButtons, "Guardar partida");
            menu.pauseOptionsButton = UiButton(pauseButtons, "Opciones");
            menu.pauseMenuButton = UiButton(pauseButtons, "Menú principal");

            // ---- Fundido a negro (por encima de todo)
            GameObject fadeCanvas = UiCanvas("FadeCanvas", 20);
            RectTransform fadeImage = UiRect(fadeCanvas.transform, "Fade", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3000f, 3000f));
            fadeImage.gameObject.AddComponent<Image>().color = Color.black;
            var fadeGroup = fadeCanvas.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;

            menu.mainPanel = main;
            menu.optionsPanel = options;
            menu.creditsPanel = credits;
            menu.pausePanel = pause;
            menu.hud = hudGroup;
            menu.fade = fadeGroup;
            menu.orbitCenter = player.transform;
            menu.playerInput = player.GetComponent<PlayerInputReader>();
            menu.gameCamera = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
            menu.saveSystem = save;

            options.SetActive(false);
            credits.SetActive(false);
            pause.SetActive(false);
            return menu;
        }

        // ------------------------------------------------------------------ AYUDAS DE UI

        private static Sprite RoundSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        private static GameObject UiCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        private static GameObject UiGroup(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static RectTransform UiRect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform UiPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rt = UiRect(parent, name, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = RoundSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rt;
        }

        private static TextMeshProUGUI UiText(Transform parent, string name, string text, float fontSize,
            TextAlignmentOptions alignment, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Button UiButton(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = RoundSprite;
            image.type = Image.Type.Sliced;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Green;
            colors.highlightedColor = Hex("#69A872");
            colors.pressedColor = Hex("#3C6E45");
            colors.selectedColor = Hex("#5E9C64");
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 460f;
            element.preferredHeight = 64f;

            TextMeshProUGUI text = UiText(go.transform, "Label", label, 32f, TextAlignmentOptions.Center, Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 56f));
            text.raycastTarget = false;
            return button;
        }

        private static void PlaceBottom(Button button, float margin)
        {
            var rt = (RectTransform)button.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, margin);
            rt.sizeDelta = new Vector2(460f, 64f);
            button.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        private static Slider UiSlider(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            Transform background = go.transform.Find("Background");
            if (background != null) background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = Green;
            if (slider.handleRect != null) slider.handleRect.GetComponent<Image>().color = Cream;
            return slider;
        }

        private static Slider UiOptionRow(Transform parent, string label, float y)
        {
            UiText(parent, label + "_Label", label, 32f, TextAlignmentOptions.Left, Cream, new Vector2(0f, 1f), new Vector2(70f, y), new Vector2(360f, 44f));
            return UiSlider(parent, label + "_Slider", new Vector2(0f, 1f), new Vector2(450f, y - 10f), new Vector2(380f, 26f));
        }
    }
}
