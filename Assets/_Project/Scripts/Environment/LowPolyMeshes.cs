using System.Collections.Generic;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Generador de mallas low-poly con sombreado plano (cada triángulo tiene sus propios vértices).
    /// Sirve como reemplazo temporal de modelos de Blender: árboles, rocas, bancas, botellas, etc.
    /// </summary>
    public static class LowPolyMeshes
    {
        public static Mesh ToMesh(List<Vector3> triangles, string meshName)
        {
            var mesh = new Mesh { name = meshName };
            int count = triangles.Count;
            if (count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            var indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;

            mesh.SetVertices(triangles);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Tronco de cono (o cono si topRadius = 0). Base en y = 0.</summary>
        public static List<Vector3> FrustumTris(float bottomRadius, float topRadius, float height, int sides,
            bool capBottom = true, bool capTop = true)
        {
            var t = new List<Vector3>();
            Vector3 bc = Vector3.zero;
            Vector3 tc = new Vector3(0f, height, 0f);
            bool hasTop = topRadius > 0.0001f;

            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * bottomRadius, 0f, Mathf.Sin(a0) * bottomRadius);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * bottomRadius, 0f, Mathf.Sin(a1) * bottomRadius);
                Vector3 t0 = new Vector3(Mathf.Cos(a0) * topRadius, height, Mathf.Sin(a0) * topRadius);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * topRadius, height, Mathf.Sin(a1) * topRadius);

                if (hasTop) Tri(t, b0, t0, t1);
                Tri(t, b0, t1, b1);
                if (capBottom) Tri(t, bc, b0, b1);
                if (capTop && hasTop) Tri(t, tc, t1, t0);
            }
            return t;
        }

        /// <summary>Caja. Con pivotBottom el origen queda en el centro de la cara inferior.</summary>
        public static List<Vector3> BoxTris(Vector3 size, bool pivotBottom = true)
        {
            var t = new List<Vector3>();
            float x = size.x * 0.5f;
            float z = size.z * 0.5f;
            float y0 = pivotBottom ? 0f : -size.y * 0.5f;
            float y1 = y0 + size.y;

            Quad(t, new Vector3(x, y0, z), new Vector3(x, y1, z), new Vector3(-x, y1, z), new Vector3(-x, y0, z));       // +z
            Quad(t, new Vector3(-x, y0, -z), new Vector3(-x, y1, -z), new Vector3(x, y1, -z), new Vector3(x, y0, -z));   // -z
            Quad(t, new Vector3(x, y0, -z), new Vector3(x, y1, -z), new Vector3(x, y1, z), new Vector3(x, y0, z));       // +x
            Quad(t, new Vector3(-x, y0, z), new Vector3(-x, y1, z), new Vector3(-x, y1, -z), new Vector3(-x, y0, -z));   // -x
            Quad(t, new Vector3(-x, y1, z), new Vector3(x, y1, z), new Vector3(x, y1, -z), new Vector3(-x, y1, -z));     // +y
            Quad(t, new Vector3(-x, y0, -z), new Vector3(x, y0, -z), new Vector3(x, y0, z), new Vector3(-x, y0, z));     // -y
            return t;
        }

        /// <summary>Esfera facetada (icosfera) con vértices deformados: sirve para copas, rocas y arbustos.</summary>
        public static List<Vector3> IcosphereTris(float radius, int subdivisions, float jitter, int seed)
        {
            float g = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0),
                new Vector3(0, -1, g), new Vector3(0, 1, g), new Vector3(0, -1, -g), new Vector3(0, 1, -g),
                new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;

            var faces = new List<int[]>
            {
                new[] { 0, 11, 5 }, new[] { 0, 5, 1 }, new[] { 0, 1, 7 }, new[] { 0, 7, 10 }, new[] { 0, 10, 11 },
                new[] { 1, 5, 9 }, new[] { 5, 11, 4 }, new[] { 11, 10, 2 }, new[] { 10, 7, 6 }, new[] { 7, 1, 8 },
                new[] { 3, 9, 4 }, new[] { 3, 4, 2 }, new[] { 3, 2, 6 }, new[] { 3, 6, 8 }, new[] { 3, 8, 9 },
                new[] { 4, 9, 5 }, new[] { 2, 4, 11 }, new[] { 6, 2, 10 }, new[] { 8, 6, 7 }, new[] { 9, 8, 1 }
            };

            var cache = new Dictionary<long, int>();
            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int[]>();
                cache.Clear();
                foreach (int[] f in faces)
                {
                    int a = Midpoint(verts, cache, f[0], f[1]);
                    int b = Midpoint(verts, cache, f[1], f[2]);
                    int c = Midpoint(verts, cache, f[2], f[0]);
                    next.Add(new[] { f[0], a, c });
                    next.Add(new[] { f[1], b, a });
                    next.Add(new[] { f[2], c, b });
                    next.Add(new[] { a, b, c });
                }
                faces = next;
            }

            var rng = new System.Random(seed);
            for (int i = 0; i < verts.Count; i++)
            {
                float k = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
                verts[i] = verts[i] * (radius * k);
            }

            var t = new List<Vector3>();
            foreach (int[] f in faces) TriOutward(t, verts[f[0]], verts[f[1]], verts[f[2]]);
            return t;
        }

        private static int Midpoint(List<Vector3> verts, Dictionary<long, int> cache, int i, int j)
        {
            long key = ((long)Mathf.Min(i, j) << 32) + Mathf.Max(i, j);
            if (cache.TryGetValue(key, out int found)) return found;
            Vector3 m = ((verts[i] + verts[j]) * 0.5f).normalized;
            verts.Add(m);
            cache[key] = verts.Count - 1;
            return verts.Count - 1;
        }

        private static void Tri(List<Vector3> list, Vector3 a, Vector3 b, Vector3 c)
        {
            list.Add(a); list.Add(b); list.Add(c);
        }

        private static void Quad(List<Vector3> list, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(list, a, b, c);
            Tri(list, a, c, d);
        }

        /// <summary>Añade el triángulo con la orientación que mira hacia afuera del origen (formas convexas).</summary>
        private static void TriOutward(List<Vector3> list, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            Vector3 centroid = (a + b + c) / 3f;
            if (Vector3.Dot(normal, centroid) < 0f) Tri(list, a, c, b);
            else Tri(list, a, b, c);
        }
    }
}
