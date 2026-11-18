using System.Collections.Generic;
using System.Globalization;
using System.IO;
using EcoGenesis.Environment;
using EcoGenesis.Interaction;
using EcoGenesis.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EcoGenesis.EditorTools
{
    /// <summary>
    /// EcoGenesis > 3. Mejorar visuales: reconstruye Park_Prototype con un parque low-poly generado por código
    /// (paleta del GDD), personajes con chaleco, residuos con forma real y transición gris -> vivo.
    /// </summary>
    public static class EcoGenesisParkUpgrade
    {
        private const string Root = "Assets/_Project";
        private const string MeshFolder = Root + "/Models/Generated";
        private const string ScenePath = Root + "/Scenes/Park_Prototype.unity";

        // Paleta del GDD
        internal static readonly Color Green = Hex("#4F8B57");
        internal static readonly Color Blue = Hex("#4A91B8");
        internal static readonly Color Earth = Hex("#94734E");
        internal static readonly Color Cream = Hex("#E9D9B4");
        internal static readonly Color Grey = Hex("#555D61");

        internal static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        internal static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        internal static System.Random rng;
        internal static int Seed = 11;
        internal static float? BlockZ;
        internal static bool BlockMarket;
        internal static Vector3 MarketCenter = new Vector3(0f, 0f, 28f);

        internal sealed class Style
        {
            public Color shirt, vest, pants, boots, gloves, skin, hair;
            public bool hasVest = true, longHair, backpack, strawHat, clipboard, shovel;
        }

        // ------------------------------------------------------------------ ENTRADA

        [MenuItem("EcoGenesis/3. Mejorar visuales (parque low-poly)")]
        public static void Build()
        {
            meshes.Clear();
            materials.Clear();
            if (AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.DeleteAsset(MeshFolder);
            EnsureFolder(MeshFolder);

            EcoGenesisSetupTool.BuildPrototypeScene(); // escena base limpia con sistemas, HUD y misiones
            rng = new System.Random(Seed);

            Scene scene = SceneManager.GetActiveScene();
            GameObject ground = FindRoot(scene, "Ground");
            GameObject player = FindRoot(scene, "Player");
            GameObject bins = FindRoot(scene, "Bins");
            GameObject trash = FindRoot(scene, "Trash");
            GameObject pond = FindRoot(scene, "Pond");
            GameObject systems = FindRoot(scene, "_Systems");
            GameObject elena = FindRoot(scene, "NPC_Elena");
            GameObject sunGo = FindRoot(scene, "Directional Light");

            for (int i = 1; i <= 3; i++)
            {
                GameObject old = FindRoot(scene, "Vegetation_Stage" + i);
                if (old != null) Object.DestroyImmediate(old);
            }

            Transform park = new GameObject("Park").transform;

            UpgradeGround(ground);
            BuildPaths(Group(park, "Paths"));
            BuildFountain(Group(park, "Fountain"));
            BuildHedges(Group(park, "Hedges"));
            BuildTrees(Group(park, "Trees"), 34);
            BuildBushes(Group(park, "Bushes"), 26);
            BuildFlowers(Group(park, "Flowers"), 55);
            BuildTufts(Group(park, "Grass"), 80);
            BuildRocks(Group(park, "Rocks"), 14);
            BuildBenches(Group(park, "Benches"));
            BuildLamps(Group(park, "Lamps"));
            BuildPollutionPiles(Group(park, "Pollution"), 18);
            BuildClouds(Group(park, "Clouds"), 8);
            UpgradePond(pond, Group(park, "PondDecor"));

            UpgradePlayer(player);
            UpgradeElena(elena);
            BuildMateo();
            UpgradeBins(bins);
            UpgradeTrash(trash);
            SetupLooks(sunGo, systems);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[EcoGenesis] Parque low-poly listo. Pulsa Play: al clasificar bien, el parque pasa de gris a verde.");
        }

        // ------------------------------------------------------------------ ESCENARIO

        internal static void UpgradeGround(GameObject ground)
        {
            ground.transform.localScale = new Vector3(80f, 1f, 80f);
            var rend = ground.GetComponent<MeshRenderer>();
            rend.sharedMaterial = Mat(Hex("#7C8685"));
            var rp = ground.AddComponent<RestorationProp>();
            rp.Configure(0f, 0.9f, 1f, 1f);
            rp.AddPart(rend, Hex("#7C8685"), Hex("#7DB07A"));
        }

        internal static void BuildPaths(Transform parent)
        {
            Color dead = Hex("#828789");
            Color clean = Hex("#D9C59A");
            MakeFlatSlab(parent, "Path_Z", new Vector3(3f, 0.05f, 76f), new Vector3(0f, 0f, 0f), dead, clean);
            MakeFlatSlab(parent, "Path_X", new Vector3(76f, 0.05f, 3f), new Vector3(0f, 0f, 0f), dead, clean);
        }

        internal static void MakeFlatSlab(Transform parent, string name, Vector3 size, Vector3 pos, Color dead, Color clean)
        {
            GameObject slab = Add(parent, name, Box(size.x, size.y, size.z), dead, pos);
            var rp = slab.AddComponent<RestorationProp>();
            rp.Configure(0.05f, 0.7f, 1f, 1f);
            rp.AddPart(slab.GetComponent<MeshRenderer>(), dead, clean);
        }

        internal static void BuildFountain(Transform parent)
        {
            parent.position = new Vector3(0f, 0f, 28f);
            Color stone = Hex("#8A8F91");

            GameObject plaza = Add(parent, "Plaza", Frustum(5f, 5f, 0.07f, 24), Hex("#828789"), Vector3.zero);
            var plazaRp = plaza.AddComponent<RestorationProp>();
            plazaRp.Configure(0.05f, 0.7f, 1f, 1f);
            plazaRp.AddPart(plaza.GetComponent<MeshRenderer>(), Hex("#828789"), Cream);

            Add(parent, "Basin", Frustum(2.4f, 2.2f, 0.6f, 16), stone, new Vector3(0f, 0.07f, 0f));
            GameObject water = Add(parent, "Water", Frustum(2.15f, 2.15f, 0.04f, 16, false, true), Hex("#6B5A3C"), new Vector3(0f, 0.6f, 0f));
            var waterRp = water.AddComponent<RestorationProp>();
            waterRp.Configure(0.1f, 0.6f, 1f, 1f);
            waterRp.AddPart(water.GetComponent<MeshRenderer>(), Hex("#6B5A3C"), Blue);

            Add(parent, "Pillar", Frustum(0.35f, 0.25f, 1.4f, 8), stone, new Vector3(0f, 0.07f, 0f));
            Add(parent, "Bowl", Frustum(0.9f, 0.5f, 0.25f, 12), stone, new Vector3(0f, 1.45f, 0f));

            GameObject jet = Add(parent, "Jet", Frustum(0.12f, 0f, 0.9f, 6), Hex("#9CCBE3"), new Vector3(0f, 1.7f, 0f));
            var jetRp = jet.AddComponent<RestorationProp>();
            jetRp.Configure(0.45f, 0.8f, 0f, 1f);
            jetRp.AddPart(jet.GetComponent<MeshRenderer>(), Hex("#9CCBE3"), Hex("#BEE3F2"));

            var col = parent.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.radius = 2.4f;
            col.height = 1f;
        }

        internal static void BuildHedges(Transform parent)
        {
            Color dead = Hex("#7B7F73");
            Color clean = Hex("#3F7A4A");
            MakeHedge(parent, "Hedge_N", new Vector3(80f, 1.6f, 1.2f), new Vector3(0f, 0f, 38f), dead, clean);
            MakeHedge(parent, "Hedge_S", new Vector3(80f, 1.6f, 1.2f), new Vector3(0f, 0f, -38f), dead, clean);
            MakeHedge(parent, "Hedge_E", new Vector3(1.2f, 1.6f, 80f), new Vector3(38f, 0f, 0f), dead, clean);
            MakeHedge(parent, "Hedge_W", new Vector3(1.2f, 1.6f, 80f), new Vector3(-38f, 0f, 0f), dead, clean);
        }

        internal static void MakeHedge(Transform parent, string name, Vector3 size, Vector3 pos, Color dead, Color clean)
        {
            GameObject hedge = Add(parent, name, Box(size.x, size.y, size.z), dead, pos);
            var col = hedge.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, size.y * 0.5f, 0f);
            col.size = size;
            var rp = hedge.AddComponent<RestorationProp>();
            rp.Configure(0f, 0.6f, 1f, 1f);
            rp.AddPart(hedge.GetComponent<MeshRenderer>(), dead, clean);
        }

        internal static void BuildTrees(Transform parent, int count)
        {
            Color[] greens = { Green, Hex("#5E9C64"), Hex("#3F7A4A"), Hex("#6FAE6B") };
            Color deadCrown = Hex("#8F8C7C");
            Color deadTrunk = Hex("#76695A");

            for (int i = 0; i < count; i++)
            {
                Vector3 p = Spot(34f, true);
                bool pine = rng.NextDouble() < 0.35;
                float s = Range(0.9f, 1.4f);
                float full = Range(0.25f, 0.95f);

                var tree = new GameObject((pine ? "Pine_" : "Tree_") + i);
                tree.transform.SetParent(parent, false);
                tree.transform.position = p;
                tree.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * s;

                var rp = tree.AddComponent<RestorationProp>();
                rp.Configure(Mathf.Max(0f, full - 0.35f), full, 0.55f, 1f);

                GameObject trunk = Add(tree.transform, "Trunk", Frustum(0.26f, 0.16f, 1.8f, 6), deadTrunk, Vector3.zero);
                rp.AddPart(trunk.GetComponent<MeshRenderer>(), deadTrunk, Earth);

                Color leaf = greens[rng.Next(greens.Length)];
                if (pine)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        GameObject cone = Add(tree.transform, "Cone" + k, Frustum(1.5f - k * 0.4f, 0f, 1.5f, 7), deadCrown, new Vector3(0f, 1.1f + k * 0.85f, 0f));
                        rp.AddPart(cone.GetComponent<MeshRenderer>(), deadCrown, leaf);
                    }
                }
                else
                {
                    GameObject c1 = Add(tree.transform, "Crown1", Ico(1.25f, 1, 0.18f, 1 + rng.Next(4)), deadCrown, new Vector3(0f, 2.5f, 0f));
                    GameObject c2 = Add(tree.transform, "Crown2", Ico(0.9f, 1, 0.18f, 5 + rng.Next(3)), deadCrown, new Vector3(0.55f, 3.15f, 0.2f));
                    GameObject c3 = Add(tree.transform, "Crown3", Ico(0.8f, 1, 0.2f, 9 + rng.Next(2)), deadCrown, new Vector3(-0.5f, 2.9f, -0.3f));
                    rp.AddPart(c1.GetComponent<MeshRenderer>(), deadCrown, leaf);
                    rp.AddPart(c2.GetComponent<MeshRenderer>(), deadCrown, Color.Lerp(leaf, Color.white, 0.08f));
                    rp.AddPart(c3.GetComponent<MeshRenderer>(), deadCrown, Color.Lerp(leaf, Color.black, 0.08f));
                }

                var col = tree.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1f, 0f);
                col.radius = 0.35f;
                col.height = 2f;
            }
        }

        internal static void BuildBushes(Transform parent, int count)
        {
            Color[] greens = { Green, Hex("#5E9C64"), Hex("#3F7A4A") };
            Color dead = Hex("#858879");
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Spot(35f, true);
                float full = Range(0.2f, 0.9f);
                var bush = new GameObject("Bush_" + i);
                bush.transform.SetParent(parent, false);
                bush.transform.position = p;
                bush.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                float s = Range(0.8f, 1.3f);
                bush.transform.localScale = Vector3.one * s;

                var rp = bush.AddComponent<RestorationProp>();
                rp.Configure(Mathf.Max(0f, full - 0.3f), full, 0.7f, 1f);
                GameObject body = Add(bush.transform, "Body", Ico(0.8f, 1, 0.15f, 20 + rng.Next(3)), dead, new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.75f, 1f));
                rp.AddPart(body.GetComponent<MeshRenderer>(), dead, greens[rng.Next(greens.Length)]);
            }
        }

        internal static void BuildFlowers(Transform parent, int count)
        {
            Color[] blooms = { Cream, Blue, Hex("#E8A0A0"), Hex("#F2C14E") };
            Color dead = Hex("#8A8E78");
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Spot(35f, true);
                float start = Range(0.4f, 0.7f);
                var cluster = new GameObject("Flowers_" + i);
                cluster.transform.SetParent(parent, false);
                cluster.transform.position = p;
                var rp = cluster.AddComponent<RestorationProp>();
                rp.Configure(start, start + 0.25f, 0f, 1f);

                Color bloom = blooms[rng.Next(blooms.Length)];
                for (int k = 0; k < 3; k++)
                {
                    Vector3 off = new Vector3(Range(-0.35f, 0.35f), 0f, Range(-0.35f, 0.35f));
                    GameObject stem = Add(cluster.transform, "Stem", Frustum(0.03f, 0.02f, 0.4f, 4), dead, off);
                    GameObject head = Add(cluster.transform, "Bloom", Ico(0.13f, 0, 0.1f, 30 + k), dead, off + new Vector3(0f, 0.45f, 0f));
                    rp.AddPart(stem.GetComponent<MeshRenderer>(), dead, Green);
                    rp.AddPart(head.GetComponent<MeshRenderer>(), dead, bloom);
                }
            }
        }

        internal static void BuildTufts(Transform parent, int count)
        {
            Color dead = Hex("#7B745A");
            Color clean = Hex("#6FAE6B");
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Spot(36f, true);
                float start = Range(0f, 0.6f);
                var tuft = new GameObject("Tuft_" + i);
                tuft.transform.SetParent(parent, false);
                tuft.transform.position = p;
                var rp = tuft.AddComponent<RestorationProp>();
                rp.Configure(start, start + 0.3f, 0.5f, 1f);
                for (int k = 0; k < 3; k++)
                {
                    GameObject blade = Add(tuft.transform, "Blade", Frustum(0.12f, 0f, 0.5f, 4), dead,
                        new Vector3(Range(-0.15f, 0.15f), 0f, Range(-0.15f, 0.15f)), null,
                        new Vector3(Range(-12f, 12f), Range(0f, 360f), Range(-12f, 12f)));
                    rp.AddPart(blade.GetComponent<MeshRenderer>(), dead, clean);
                }
            }
        }

        internal static void BuildRocks(Transform parent, int count)
        {
            Color[] greys = { Hex("#7A7F82"), Hex("#8A8F91"), Hex("#6E7376") };
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Spot(35f, false);
                float s = Range(0.6f, 1.6f);
                GameObject rock = Add(parent, "Rock_" + i, Ico(0.6f, 0, 0.3f, 40 + rng.Next(4)), greys[rng.Next(greys.Length)],
                    p + new Vector3(0f, 0.2f * s, 0f), new Vector3(s, s * 0.7f, s), new Vector3(0f, Range(0f, 360f), 0f));
                var col = rock.AddComponent<SphereCollider>();
                col.radius = 0.5f;
            }
        }

        internal static void BuildBenches(Transform parent)
        {
            // (x, z, rotación Y): la banca mira hacia el sendero.
            float[][] spots =
            {
                new[] { 3.2f, -8f, 270f }, new[] { -3.2f, -16f, 90f }, new[] { 3.2f, 17f, 270f }, new[] { -3.2f, 22f, 90f },
                new[] { 10f, 3.2f, 180f }, new[] { -12f, -3.2f, 0f }, new[] { 22f, -3.2f, 0f }, new[] { -22f, 3.2f, 180f }
            };
            Color dead = Hex("#7D786C");
            for (int i = 0; i < spots.Length; i++)
            {
                var bench = new GameObject("Bench_" + i);
                bench.transform.SetParent(parent, false);
                bench.transform.position = new Vector3(spots[i][0], 0f, spots[i][1]);
                bench.transform.rotation = Quaternion.Euler(0f, spots[i][2], 0f);

                var rp = bench.AddComponent<RestorationProp>();
                rp.Configure(0.1f, 0.6f, 1f, 1f);
                GameObject seat = Add(bench.transform, "Seat", Box(1.5f, 0.1f, 0.5f), dead, new Vector3(0f, 0.45f, 0f));
                GameObject back = Add(bench.transform, "Back", Box(1.5f, 0.5f, 0.08f), dead, new Vector3(0f, 0.55f, -0.24f));
                rp.AddPart(seat.GetComponent<MeshRenderer>(), dead, Earth);
                rp.AddPart(back.GetComponent<MeshRenderer>(), dead, Earth);
                Add(bench.transform, "LegL", Box(0.1f, 0.45f, 0.45f), Grey, new Vector3(-0.6f, 0f, 0f));
                Add(bench.transform, "LegR", Box(0.1f, 0.45f, 0.45f), Grey, new Vector3(0.6f, 0f, 0f));

                var col = bench.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.45f, 0f);
                col.size = new Vector3(1.5f, 0.9f, 0.55f);
            }
        }

        internal static void BuildLamps(Transform parent)
        {
            float[][] spots =
            {
                new[] { 2.4f, -12f }, new[] { -2.4f, -24f }, new[] { 2.4f, 14f }, new[] { -2.4f, 24f },
                new[] { 12f, 2.4f }, new[] { -14f, -2.4f }, new[] { 24f, -2.4f }, new[] { -24f, 2.4f }
            };
            Color deadHead = Hex("#6E7072");
            for (int i = 0; i < spots.Length; i++)
            {
                var lamp = new GameObject("Lamp_" + i);
                lamp.transform.SetParent(parent, false);
                lamp.transform.position = new Vector3(spots[i][0], 0f, spots[i][1]);

                var rp = lamp.AddComponent<RestorationProp>();
                rp.Configure(0.2f, 0.6f, 1f, 1f);
                Add(lamp.transform, "Pole", Frustum(0.07f, 0.05f, 3.2f, 6), Hex("#4A4F52"), Vector3.zero);
                GameObject head = Add(lamp.transform, "Head", Frustum(0.25f, 0.18f, 0.3f, 6), deadHead, new Vector3(0f, 3.2f, 0f));
                rp.AddPart(head.GetComponent<MeshRenderer>(), deadHead, Cream);

                var lightGo = new GameObject("Light");
                lightGo.transform.SetParent(lamp.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 3.4f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 8f;
                light.color = new Color(1f, 0.9f, 0.7f);
                light.shadows = LightShadows.None;
                light.intensity = 0f;
                rp.AddLight(light, 0f, 1.8f);

                var col = lamp.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1.6f, 0f);
                col.radius = 0.15f;
                col.height = 3.2f;
            }
        }

        internal static void BuildPollutionPiles(Transform parent, int count)
        {
            Color[] dull = { Hex("#6E7276"), Hex("#7A6F5F"), Hex("#5A6468"), Hex("#85796A") };
            for (int i = 0; i < count; i++)
            {
                Vector3 p;
                if (i < 4)
                {
                    float angle = (i * 90f + 40f) * Mathf.Deg2Rad;
                    p = new Vector3(Mathf.Cos(angle) * 8f, 0f, Mathf.Sin(angle) * 8f);
                    if (IsBlocked(p, false)) p = Spot(34f, false);
                }
                else
                {
                    p = Spot(34f, false);
                }

                float start = Range(0.1f, 0.55f);
                var pile = new GameObject("Pile_" + i);
                pile.transform.SetParent(parent, false);
                pile.transform.position = p;
                var rp = pile.AddComponent<RestorationProp>();
                rp.Configure(start, start + 0.3f, 1f, 0f);

                for (int k = 0; k < 5; k++)
                {
                    float s = Range(0.5f, 1.1f);
                    Add(pile.transform, "Junk" + k, Ico(0.3f, 0, 0.3f, 50 + rng.Next(5)), dull[rng.Next(dull.Length)],
                        new Vector3(Range(-0.7f, 0.7f), 0.15f * s, Range(-0.7f, 0.7f)), new Vector3(s, s * 0.8f, s),
                        new Vector3(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f)));
                }
                Add(pile.transform, "Tire", Frustum(0.4f, 0.4f, 0.18f, 10), Hex("#2F3235"), new Vector3(0.5f, 0.05f, -0.4f), null,
                    new Vector3(Range(-20f, 20f), Range(0f, 360f), 0f));
            }
        }

        internal static void BuildClouds(Transform parent, int count)
        {
            Color dead = Hex("#9AA0A3");
            for (int i = 0; i < count; i++)
            {
                var cloud = new GameObject("Cloud_" + i);
                cloud.transform.SetParent(parent, false);
                cloud.transform.position = new Vector3(Range(-55f, 55f), Range(26f, 36f), Range(-55f, 55f));
                var rp = cloud.AddComponent<RestorationProp>();
                rp.Configure(0f, 1f, 1f, 1f);
                for (int k = 0; k < 3; k++)
                {
                    GameObject puff = Add(cloud.transform, "Puff" + k, Ico(3f, 1, 0.12f, 60 + k), dead,
                        new Vector3((k - 1) * 3.2f, k == 1 ? 0.8f : 0f, Range(-1f, 1f)), new Vector3(1.5f, 0.7f, 1.1f));
                    rp.AddPart(puff.GetComponent<MeshRenderer>(), dead, Color.white);
                }
            }
        }

        internal static void UpgradePond(GameObject pond, Transform decor)
        {
            Vector3 center = new Vector3(16f, 0f, -12f);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                float s = Range(0.7f, 1.2f);
                Add(decor, "Stone" + i, Ico(0.45f, 0, 0.3f, 70 + (i % 4)), Hex("#8A8F91"),
                    center + new Vector3(Mathf.Cos(a) * 3.8f, 0.12f * s, Mathf.Sin(a) * 3.8f), new Vector3(s, s * 0.7f, s));
            }

            Color deadReed = Hex("#7B745A");
            for (int i = 0; i < 10; i++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                var reed = new GameObject("Reed" + i);
                reed.transform.SetParent(decor, false);
                reed.transform.position = center + new Vector3(Mathf.Cos(a) * 3.4f, 0f, Mathf.Sin(a) * 3.4f);
                var rp = reed.AddComponent<RestorationProp>();
                rp.Configure(0.1f, 0.6f, 0.5f, 1f);
                GameObject blade = Add(reed.transform, "Blade", Frustum(0.04f, 0f, 1.2f, 4), deadReed, Vector3.zero);
                rp.AddPart(blade.GetComponent<MeshRenderer>(), deadReed, Green);
            }

            Color deadPad = Hex("#5E5A4A");
            for (int i = 0; i < 7; i++)
            {
                float a = Range(0f, Mathf.PI * 2f);
                float r = Range(0.5f, 2.6f);
                var pad = new GameObject("Lily" + i);
                pad.transform.SetParent(decor, false);
                pad.transform.position = center + new Vector3(Mathf.Cos(a) * r, 0.08f, Mathf.Sin(a) * r);
                var rp = pad.AddComponent<RestorationProp>();
                float start = Range(0.25f, 0.6f);
                rp.Configure(start, start + 0.25f, 0f, 1f);
                GameObject disc = Add(pad.transform, "Pad", Frustum(0.35f, 0.35f, 0.03f, 8), deadPad, Vector3.zero);
                rp.AddPart(disc.GetComponent<MeshRenderer>(), deadPad, Green);
            }
        }

        // ------------------------------------------------------------------ PERSONAJES

        internal static void UpgradePlayer(GameObject player)
        {
            player.GetComponent<MeshRenderer>().enabled = false;
            Transform nose = player.transform.Find("Nose");
            if (nose != null) Object.DestroyImmediate(nose.gameObject);

            BuildHumanoid(player.transform, new Style
            {
                shirt = Blue, vest = Green, pants = Earth, boots = Hex("#3A3A3A"), gloves = Cream,
                skin = Hex("#E8B796"), hair = Hex("#4B3425"), backpack = true
            }, -1f);
        }

        internal static void UpgradeElena(GameObject elena)
        {
            elena.GetComponent<MeshRenderer>().enabled = false;
            elena.transform.rotation = Quaternion.Euler(0f, 53f, 0f);
            BuildHumanoid(elena.transform, new Style
            {
                shirt = Cream, vest = Blue, pants = Grey, boots = Hex("#3A3A3A"), gloves = Cream,
                skin = Hex("#D9A27E"), hair = Hex("#2F2323"), longHair = true, clipboard = true
            }, -1f);
        }

        internal static void BuildMateo()
        {
            var mateo = new GameObject("NPC_Mateo");
            mateo.transform.position = new Vector3(7f, 1f, -6f);
            mateo.transform.rotation = Quaternion.Euler(0f, 311f, 0f);
            var col = mateo.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;

            var talker = mateo.AddComponent<NpcTalker>();
            talker.Configure("Mateo", new[]
            {
                "¡Hola! Soy Mateo, el jardinero del parque.",
                "Cuando la zona esté libre de residuos peligrosos, plantaremos árboles y flores juntos.",
                "Las pilas contaminan el suelo: nunca van a la basura común."
            });

            BuildHumanoid(mateo.transform, new Style
            {
                shirt = Cream, vest = Earth, pants = Green, boots = Hex("#3A3A3A"), gloves = Earth,
                skin = Hex("#C98F6B"), hair = Hex("#3B2B22"), strawHat = true, shovel = true
            }, -1f);
        }

        internal static void BuildHumanoid(Transform parent, Style s, float yOffset)
        {
            var root = new GameObject("Model").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, yOffset, 0f);

            Transform leftLeg = Limb(root, "LegL", new Vector3(-0.14f, 0.62f, 0f), Box(0.2f, 0.62f, 0.22f), s.pants, 0.62f);
            Transform rightLeg = Limb(root, "LegR", new Vector3(0.14f, 0.62f, 0f), Box(0.2f, 0.62f, 0.22f), s.pants, 0.62f);
            Add(leftLeg, "Boot", Box(0.22f, 0.14f, 0.3f), s.boots, new Vector3(0f, -0.62f, 0.03f));
            Add(rightLeg, "Boot", Box(0.22f, 0.14f, 0.3f), s.boots, new Vector3(0f, -0.62f, 0.03f));

            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            body.localPosition = new Vector3(0f, 0.62f, 0f);

            Add(body, "Torso", Box(0.5f, 0.6f, 0.28f), s.shirt, Vector3.zero);
            if (s.hasVest)
            {
                Add(body, "Vest", Box(0.54f, 0.5f, 0.32f), s.vest, new Vector3(0f, 0.04f, 0f));
                Add(body, "StripeL", Box(0.07f, 0.5f, 0.335f), Cream, new Vector3(-0.15f, 0.04f, 0f));
                Add(body, "StripeR", Box(0.07f, 0.5f, 0.335f), Cream, new Vector3(0.15f, 0.04f, 0f));
            }

            Add(body, "Head", Ico(0.23f, 1, 0.04f, 3), s.skin, new Vector3(0f, 0.84f, 0f));
            Add(body, "Hair", Ico(0.25f, 1, 0.05f, 4), s.hair, new Vector3(0f, 0.92f, -0.02f), new Vector3(1f, 0.7f, 1f));
            Add(body, "EyeL", Box(0.045f, 0.07f, 0.03f), Hex("#2B2B2B"), new Vector3(-0.08f, 0.82f, 0.21f));
            Add(body, "EyeR", Box(0.045f, 0.07f, 0.03f), Hex("#2B2B2B"), new Vector3(0.08f, 0.82f, 0.21f));

            if (s.longHair) Add(body, "HairBack", Box(0.4f, 0.55f, 0.14f), s.hair, new Vector3(0f, 0.45f, -0.17f));
            if (s.backpack) Add(body, "Backpack", Box(0.36f, 0.4f, 0.16f), Earth, new Vector3(0f, 0.1f, -0.23f));
            if (s.strawHat)
            {
                Add(body, "HatBrim", Frustum(0.42f, 0.4f, 0.03f, 10), Cream, new Vector3(0f, 0.98f, 0f));
                Add(body, "HatCrown", Frustum(0.25f, 0.2f, 0.2f, 10), Cream, new Vector3(0f, 0.99f, 0f));
                Add(body, "HatBand", Frustum(0.255f, 0.255f, 0.05f, 10), Green, new Vector3(0f, 1.0f, 0f));
            }
            if (s.shovel)
            {
                Add(body, "ShovelPole", Frustum(0.025f, 0.025f, 1.3f, 5), Earth, new Vector3(0.45f, -0.25f, 0.12f), null, new Vector3(8f, 0f, -6f));
                Add(body, "ShovelBlade", Box(0.2f, 0.26f, 0.03f), Hex("#8A8F91"), new Vector3(0.45f, -0.25f, 0.12f), null, new Vector3(8f, 0f, -6f));
            }

            Transform leftArm = Limb(body, "ArmL", new Vector3(-0.33f, 0.56f, 0f), Box(0.14f, 0.5f, 0.14f), s.shirt, 0.5f);
            Transform rightArm = Limb(body, "ArmR", new Vector3(0.33f, 0.56f, 0f), Box(0.14f, 0.5f, 0.14f), s.shirt, 0.5f);
            Add(leftArm, "Glove", Box(0.17f, 0.16f, 0.17f), s.gloves, new Vector3(0f, -0.56f, 0f));
            Add(rightArm, "Glove", Box(0.17f, 0.16f, 0.17f), s.gloves, new Vector3(0f, -0.56f, 0f));
            if (s.clipboard) Add(leftArm, "Clipboard", Box(0.22f, 0.3f, 0.03f), Cream, new Vector3(0f, -0.55f, 0.13f));

            var anim = root.gameObject.AddComponent<ProceduralCharacterAnimator>();
            anim.Configure(body, leftArm, rightArm, leftLeg, rightLeg);
        }

        /// <summary>Pivote en la articulación; la malla cuelga hacia abajo para poder rotarla.</summary>
        internal static Transform Limb(Transform parent, string name, Vector3 pos, Mesh mesh, Color color, float length)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = pos;
            Add(pivot, "Mesh", mesh, color, new Vector3(0f, -length, 0f));
            return pivot;
        }

        // ------------------------------------------------------------------ CONTENEDORES Y RESIDUOS

        internal static void UpgradeBins(GameObject bins)
        {
            foreach (Transform bin in bins.transform)
            {
                var rend = bin.GetComponent<MeshRenderer>();
                Color c = rend.sharedMaterial.color;
                rend.enabled = false;

                GameObject body = Add(bin, "Body", Box(0.8f, 0.85f, 0.6f), c, new Vector3(0f, -0.5f, 0f));
                Add(bin, "Lid", Box(0.88f, 0.1f, 0.68f), Darken(c, 0.8f), new Vector3(0f, 0.35f, 0f));
                Add(bin, "Plate", Box(0.42f, 0.42f, 0.02f), Cream, new Vector3(0f, -0.2f, -0.31f));
                Add(bin, "Symbol", Box(0.24f, 0.24f, 0.02f), c, new Vector3(0f, -0.11f, -0.32f));

                var controller = bin.GetComponent<BinController>();
                if (controller != null) SetObjArray(controller, "tintRenderers", new Object[] { body.GetComponent<MeshRenderer>() });
            }
        }

        internal static void UpgradeTrash(GameObject trash)
        {
            foreach (Transform item in trash.transform)
            {
                string kind = item.name.Split('_')[1];
                Vector3 pos = item.position;
                item.position = new Vector3(pos.x, 0f, pos.z);
                item.localScale = Vector3.one;
                item.GetComponent<MeshRenderer>().enabled = false;

                var box = item.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.size = new Vector3(0.6f, 0.6f, 0.6f);
                    box.center = new Vector3(0f, 0.3f, 0f);
                }

                var visual = new GameObject("Visual").transform;
                visual.SetParent(item, false);
                visual.localPosition = new Vector3(0f, 0.3f, 0f);
                visual.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                visual.gameObject.AddComponent<PickupFloat>();

                switch (kind)
                {
                    case "Plastic": BuildBottle(visual, Hex("#CFE3EC"), Blue); break;
                    case "Glass": BuildBottle(visual, Hex("#3E9B55"), Hex("#D9B84A")); break;
                    case "Organic": BuildBanana(visual); break;
                    case "Paper": BuildNewspaper(visual); break;
                    default: BuildBattery(visual); break;
                }
            }
        }

        internal static void BuildBottle(Transform p, Color glass, Color cap)
        {
            Add(p, "Body", Frustum(0.09f, 0.09f, 0.26f, 8), glass, new Vector3(0f, -0.15f, 0f));
            Add(p, "Neck", Frustum(0.09f, 0.035f, 0.1f, 8), glass, new Vector3(0f, 0.11f, 0f));
            Add(p, "Cap", Frustum(0.04f, 0.04f, 0.04f, 6), cap, new Vector3(0f, 0.21f, 0f));
        }

        internal static void BuildBanana(Transform p)
        {
            Color yellow = Hex("#E4C14F");
            Add(p, "PeelA", Frustum(0.06f, 0.04f, 0.28f, 6), yellow, new Vector3(-0.07f, -0.15f, 0f), null, new Vector3(0f, 0f, 22f));
            Add(p, "PeelB", Frustum(0.04f, 0.06f, 0.28f, 6), yellow, new Vector3(0.07f, -0.15f, 0f), null, new Vector3(0f, 0f, -22f));
            Add(p, "Tip", Frustum(0.03f, 0.03f, 0.05f, 5), Hex("#5A4632"), new Vector3(0.18f, 0.1f, 0f), null, new Vector3(0f, 0f, -22f));
        }

        internal static void BuildNewspaper(Transform p)
        {
            Add(p, "SheetA", Box(0.34f, 0.04f, 0.26f), Cream, new Vector3(0f, -0.05f, 0f), null, new Vector3(0f, 10f, 0f));
            Add(p, "SheetB", Box(0.3f, 0.045f, 0.22f), Hex("#D8D2C0"), new Vector3(0.01f, -0.02f, 0f), null, new Vector3(0f, -15f, 0f));
            Add(p, "Title", Box(0.2f, 0.05f, 0.04f), Hex("#3A3A3A"), new Vector3(0f, 0f, 0.07f), null, new Vector3(0f, -15f, 0f));
        }

        internal static void BuildBattery(Transform p)
        {
            Add(p, "Body", Frustum(0.08f, 0.08f, 0.24f, 8), Hex("#C0392B"), new Vector3(0f, -0.14f, 0f));
            Add(p, "Band", Frustum(0.085f, 0.085f, 0.06f, 8), Hex("#2F3235"), new Vector3(0f, -0.06f, 0f));
            Add(p, "Top", Frustum(0.035f, 0.035f, 0.04f, 6), Hex("#B8BDC0"), new Vector3(0f, 0.1f, 0f));
        }

        // ------------------------------------------------------------------ LUZ Y POST-PROCESO

        internal static void SetupLooks(GameObject sunGo, GameObject systems)
        {
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sunGo.GetComponent<Light>().shadows = LightShadows.Soft;

            Camera cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            Volume polluted = MakeVolume("Volume_Polluted", MakeProfile("Profile_Polluted", true), 0, 1f);
            Volume clean = MakeVolume("Volume_Clean", MakeProfile("Profile_Clean", false), 1, 0f);

            var restoration = systems.GetComponent<EnvironmentalRestoration>();
            SetObj(restoration, "pollutedVolume", polluted);
            SetObj(restoration, "cleanVolume", clean);
        }

        internal static Volume MakeVolume(string name, VolumeProfile profile, float priority, float weight)
        {
            var go = new GameObject(name);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            volume.priority = priority;
            volume.weight = weight;
            return volume;
        }

        internal static VolumeProfile MakeProfile(string name, bool polluted)
        {
            string path = Root + "/Materials/" + name + ".asset";
            AssetDatabase.DeleteAsset(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var adjust = profile.Add<ColorAdjustments>(true);
            adjust.saturation.Override(polluted ? -22f : 18f);
            adjust.contrast.Override(polluted ? 0f : 10f);
            adjust.postExposure.Override(polluted ? 0.35f : 0.3f);
            adjust.colorFilter.Override(polluted ? Hex("#EEF0EA") : Hex("#FFF4DF"));
            AssetDatabase.AddObjectToAsset(adjust, profile);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(polluted ? 0.12f : 0.1f);
            vignette.smoothness.Override(0.5f);
            AssetDatabase.AddObjectToAsset(vignette, profile);

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.Neutral);
            AssetDatabase.AddObjectToAsset(tonemap, profile);

            if (!polluted)
            {
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(0.5f);
                bloom.threshold.Override(0.95f);
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // ------------------------------------------------------------------ AYUDAS

        internal static GameObject Add(Transform parent, string name, Mesh mesh, Color color, Vector3 pos,
            Vector3? scale = null, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            if (scale.HasValue) go.transform.localScale = scale.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(color);
            return go;
        }

        internal static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        internal static Mesh Frustum(float bottom, float top, float height, int sides, bool capBottom = true, bool capTop = true)
        {
            string key = Key("fr", bottom, top, height, sides, capBottom ? 1 : 0, capTop ? 1 : 0);
            return GetMesh(key, () => LowPolyMeshes.ToMesh(LowPolyMeshes.FrustumTris(bottom, top, height, sides, capBottom, capTop), key));
        }

        internal static Mesh Box(float x, float y, float z)
        {
            string key = Key("box", x, y, z);
            return GetMesh(key, () => LowPolyMeshes.ToMesh(LowPolyMeshes.BoxTris(new Vector3(x, y, z), true), key));
        }

        internal static Mesh Ico(float radius, int subdivisions, float jitter, int seed)
        {
            string key = Key("ico", radius, subdivisions, jitter, seed);
            return GetMesh(key, () => LowPolyMeshes.ToMesh(LowPolyMeshes.IcosphereTris(radius, subdivisions, jitter, seed), key));
        }

        internal static string Key(string prefix, params float[] values)
        {
            var sb = new System.Text.StringBuilder(prefix);
            foreach (float v in values)
                sb.Append('_').Append(v.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', 'p').Replace('-', 'm'));
            return sb.ToString();
        }

        internal static Mesh GetMesh(string key, System.Func<Mesh> make)
        {
            if (meshes.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            Mesh mesh = make();
            mesh.name = key;
            AssetDatabase.CreateAsset(mesh, MeshFolder + "/" + key + ".asset");
            meshes[key] = mesh;
            return mesh;
        }

        internal static Material Mat(Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            if (materials.TryGetValue(hex, out Material cached) && cached != null) return cached;

            string path = Root + "/Materials/LP_" + hex + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.08f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            materials[hex] = material;
            return material;
        }

        internal static Vector3 Spot(float limit, bool avoidPaths)
        {
            for (int i = 0; i < 80; i++)
            {
                var p = new Vector3(Range(-limit, limit), 0f, Range(-limit, limit));
                if (!IsBlocked(p, avoidPaths)) return p;
            }
            return new Vector3(limit, 0f, limit);
        }

        internal static bool IsBlocked(Vector3 p, bool avoidPaths)
        {
            if (p.magnitude < 4f) return true;
            if (Mathf.Abs(p.x) < 9f && Mathf.Abs(p.z - 9f) < 3.5f) return true;           // contenedores
            if (Vector3.Distance(p, new Vector3(16f, 0f, -12f)) < 6f) return true;        // estanque
            if (Vector3.Distance(p, new Vector3(0f, 0f, 28f)) < 7.5f) return true;        // fuente
            if (Vector3.Distance(p, new Vector3(-4f, 0f, -3f)) < 2.5f) return true;       // Elena
            if (Vector3.Distance(p, new Vector3(7f, 0f, -6f)) < 2.5f) return true;        // Mateo
            if (BlockZ.HasValue && Mathf.Abs(p.z - BlockZ.Value) < 7f) return true;
            if (BlockMarket && Vector3.Distance(p, MarketCenter) < 16f) return true;
            if (avoidPaths && (Mathf.Abs(p.x) < 2.2f || Mathf.Abs(p.z) < 2.2f)) return true;
            return false;
        }

        internal static float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        internal static Color Darken(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, 1f);

        internal static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }

        internal static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == name) return go;
            }
            return null;
        }

        internal static void SetObj(Object target, string field, Object value)
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

        internal static void SetObjArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
