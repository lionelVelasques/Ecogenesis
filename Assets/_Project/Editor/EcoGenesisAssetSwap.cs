using System.Collections.Generic;
using System.IO;
using EcoGenesis.Core;
using EcoGenesis.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static EcoGenesis.EditorTools.EcoGenesisParkUpgrade;

namespace EcoGenesis.EditorTools
{
    /// <summary>
    /// Reemplaza los árboles, arbustos, flores, pasto, rocas y hongos generados por código con los modelos
    /// del pack Stylized Nature MegaKit de Quaternius (Assets/_Project/Art/Nature). Busca los modelos por nombre.
    /// Los árboles tienen versión seca y viva, y se transforman con la salud del parque.
    /// </summary>
    public static class EcoGenesisAssetSwap
    {
        private const string NatureFolder = "Assets/_Project/Art/Nature";
        private static System.Random random;

        private sealed class Pools
        {
            public readonly List<GameObject> alive = new List<GameObject>();
            public readonly List<GameObject> pine = new List<GameObject>();
            public readonly List<GameObject> dead = new List<GameObject>();
            public readonly List<GameObject> bush = new List<GameObject>();
            public readonly List<GameObject> bushFlowers = new List<GameObject>();
            public readonly List<GameObject> flower = new List<GameObject>();
            public readonly List<GameObject> grass = new List<GameObject>();
            public readonly List<GameObject> rock = new List<GameObject>();
            public readonly List<GameObject> mushroom = new List<GameObject>();
        }

        [MenuItem("EcoGenesis/6. Usar modelos de Quaternius (escena abierta)")]
        public static void ApplyToOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            int count = Apply(scene);
            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        [MenuItem("EcoGenesis/7. Usar modelos de Quaternius (los 3 niveles)")]
        public static void ApplyToAllLevels()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (LevelCatalog.Info info in LevelCatalog.Levels)
            {
                string path = "Assets/_Project/Scenes/" + info.scene + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (Apply(scene) > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }

        private static int Apply(Scene scene)
        {
            Pools pools = Gather();
            if (pools == null) return 0;

            random = new System.Random(5);
            GameObject park = FindRoot(scene, "Park");
            GameObject world = FindRoot(scene, "World");
            if (park == null)
            {
                Debug.LogWarning("[EcoGenesis] Esta escena no tiene el objeto 'Park'. Construye primero los niveles (menú 5).");
                return 0;
            }

            int total = 0;
            total += ReplaceTrees(park.transform.Find("Trees"), pools);
            total += ReplaceBushes(park.transform.Find("Bushes"), pools);
            total += ReplaceFlowers(park.transform.Find("Flowers"), pools);
            total += ReplaceGrass(park.transform.Find("Grass"), pools);
            total += ReplaceRocks(park.transform.Find("Rocks"), pools);
            if (world != null) total += ReplaceMushrooms(world.transform.Find("Details"), pools);

            Debug.Log("[EcoGenesis] Modelos de Quaternius aplicados: " + total + " objetos reemplazados en " + scene.name + ".");
            return total;
        }

        // ------------------------------------------------------------------ BÚSQUEDA DE MODELOS

        private static Pools Gather()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { NatureFolder });
            var pools = new Pools();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

                string name = Path.GetFileNameWithoutExtension(path);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) continue;

                if (name.StartsWith("DeadTree")) pools.dead.Add(model);
                else if (name.StartsWith("Pine")) pools.pine.Add(model);
                else if (name.StartsWith("CommonTree") || name.StartsWith("TwistedTree") || name.StartsWith("Birch")) pools.alive.Add(model);
                else if (name.StartsWith("Bush") && name.Contains("Flowers")) pools.bushFlowers.Add(model);
                else if (name.StartsWith("Bush")) pools.bush.Add(model);
                else if (name.Contains("Mushroom")) pools.mushroom.Add(model);
                else if (name.StartsWith("Rock")) pools.rock.Add(model);
                else if (name.StartsWith("Clover") || name.StartsWith("Grass") || name.StartsWith("Fern")) pools.grass.Add(model);
                else if (name.Contains("Flower")) pools.flower.Add(model);
            }

            Debug.Log("[EcoGenesis] Modelos encontrados en Art/Nature — árboles vivos: " + pools.alive.Count + ", pinos: " + pools.pine.Count +
                      ", secos: " + pools.dead.Count + ", arbustos: " + pools.bush.Count + " (+" + pools.bushFlowers.Count + " con flores), flores: " +
                      pools.flower.Count + ", pasto/tréboles: " + pools.grass.Count + ", rocas: " + pools.rock.Count + ", hongos: " + pools.mushroom.Count);

            int any = pools.alive.Count + pools.pine.Count + pools.bush.Count + pools.flower.Count + pools.grass.Count + pools.rock.Count + pools.mushroom.Count;
            if (any == 0)
            {
                Debug.LogWarning("[EcoGenesis] No encontré modelos .fbx en " + NatureFolder + ". Revisa que copiaste 'FBX (Unity)' dentro de Art/Nature.");
                return null;
            }
            return pools;
        }

        // ------------------------------------------------------------------ REEMPLAZOS

        private static int ReplaceTrees(Transform root, Pools pools)
        {
            if (root == null || (pools.alive.Count == 0 && pools.pine.Count == 0)) return 0;

            int made = 0;
            foreach (Transform old in Snapshot(root))
            {
                bool isPine = old.name.StartsWith("Pine");
                Vector3 pos = old.position;
                Object.DestroyImmediate(old.gameObject);

                var holder = new GameObject((isPine ? "NaturePine_" : "NatureTree_") + made);
                holder.transform.SetParent(root, false);
                holder.transform.position = pos;

                float full = Rand(0.25f, 0.95f);
                float start = Mathf.Max(0f, full - 0.35f);
                List<GameObject> aliveList = (isPine && pools.pine.Count > 0) ? pools.pine : (pools.alive.Count > 0 ? pools.alive : pools.pine);
                float height = Rand(5f, 7.5f);

                GameObject alive = Spawn(Pick(aliveList), holder.transform, pos, height);
                alive.AddComponent<RestorationProp>().Configure(start, full, 0f, 1f);
                if (pools.dead.Count > 0)
                {
                    GameObject dead = Spawn(Pick(pools.dead), holder.transform, pos, height * 0.85f);
                    dead.AddComponent<RestorationProp>().Configure(start, full, 1f, 0f);
                }

                var col = holder.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1.5f, 0f);
                col.radius = 0.4f;
                col.height = 3f;
                made++;
            }
            return made;
        }

        private static int ReplaceBushes(Transform root, Pools pools)
        {
            if (root == null || pools.bush.Count == 0) return 0;

            int made = 0;
            foreach (Transform old in Snapshot(root))
            {
                Vector3 pos = old.position;
                Object.DestroyImmediate(old.gameObject);

                var holder = new GameObject("NatureBush_" + made);
                holder.transform.SetParent(root, false);
                holder.transform.position = pos;

                float full = Rand(0.3f, 0.9f);
                GameObject bush = Spawn(Pick(pools.bush), holder.transform, pos, 1.3f);
                bush.AddComponent<RestorationProp>().Configure(Mathf.Max(0f, full - 0.3f), full, 0.7f, 1f);
                if (pools.bushFlowers.Count > 0)
                {
                    GameObject flowers = Spawn(Pick(pools.bushFlowers), holder.transform, pos, 1.35f);
                    flowers.AddComponent<RestorationProp>().Configure(0.55f, 0.85f, 0f, 1f);
                }
                made++;
            }
            return made;
        }

        private static int ReplaceFlowers(Transform root, Pools pools)
        {
            List<GameObject> pool = pools.flower.Count > 0 ? pools.flower : pools.grass;
            if (root == null || pool.Count == 0) return 0;

            int made = 0;
            foreach (Transform old in Snapshot(root))
            {
                Vector3 pos = old.position;
                Object.DestroyImmediate(old.gameObject);
                float start = Rand(0.4f, 0.7f);
                GameObject flower = Spawn(Pick(pool), root, pos, 0.55f);
                flower.name = "NatureFlower_" + made;
                flower.AddComponent<RestorationProp>().Configure(start, start + 0.25f, 0f, 1f);
                made++;
            }
            return made;
        }

        private static int ReplaceGrass(Transform root, Pools pools)
        {
            if (root == null || pools.grass.Count == 0) return 0;

            int made = 0;
            foreach (Transform old in Snapshot(root))
            {
                Vector3 pos = old.position;
                Object.DestroyImmediate(old.gameObject);
                float start = Rand(0f, 0.6f);
                GameObject tuft = Spawn(Pick(pools.grass), root, pos, 0.5f);
                tuft.name = "NatureGrass_" + made;
                tuft.AddComponent<RestorationProp>().Configure(start, start + 0.3f, 0.5f, 1f);
                made++;
            }
            return made;
        }

        private static int ReplaceRocks(Transform root, Pools pools)
        {
            if (root == null || pools.rock.Count == 0) return 0;

            int made = 0;
            foreach (Transform old in Snapshot(root))
            {
                Vector3 pos = old.position;
                Object.DestroyImmediate(old.gameObject);

                var holder = new GameObject("NatureRock_" + made);
                holder.transform.SetParent(root, false);
                holder.transform.position = pos;
                GameObject rock = Spawn(Pick(pools.rock), holder.transform, pos, Rand(0.7f, 1.6f));

                Bounds b = Measure(rock);
                var col = holder.AddComponent<BoxCollider>();
                col.center = b.center - pos;
                col.size = b.size;
                made++;
            }
            return made;
        }

        private static int ReplaceMushrooms(Transform root, Pools pools)
        {
            if (root == null || pools.mushroom.Count == 0) return 0;

            int made = 0;
            foreach (Transform old in Snapshot(root))
            {
                if (!old.name.StartsWith("Mushroom_")) continue;
                Vector3 pos = old.position;
                Object.DestroyImmediate(old.gameObject);
                float start = Rand(0.45f, 0.75f);
                GameObject mushroom = Spawn(Pick(pools.mushroom), root, pos, 0.35f);
                mushroom.name = "NatureMushroom_" + made;
                mushroom.AddComponent<RestorationProp>().Configure(start, start + 0.2f, 0f, 1f);
                made++;
            }
            return made;
        }

        // ------------------------------------------------------------------ AYUDAS

        private static List<Transform> Snapshot(Transform root)
        {
            var list = new List<Transform>();
            foreach (Transform child in root) list.Add(child);
            return list;
        }

        /// <summary>Instancia el modelo, lo escala a la altura pedida y apoya su base en el suelo.</summary>
        private static GameObject Spawn(GameObject model, Transform parent, Vector3 worldPos, float targetHeight)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.transform.position = worldPos;
            instance.transform.rotation = Quaternion.Euler(0f, Rand(0f, 360f), 0f);
            instance.transform.localScale = Vector3.one;

            Bounds b = Measure(instance);
            if (b.size.y > 0.001f)
            {
                float s = Mathf.Clamp(targetHeight / b.size.y, 0.05f, 20f);
                instance.transform.localScale = Vector3.one * s;
                b = Measure(instance);
                instance.transform.position += Vector3.up * (worldPos.y - b.min.y);
            }
            return instance;
        }

        private static Bounds Measure(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject Pick(List<GameObject> list) => list[random.Next(list.Count)];
        private static float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);
    }
}
