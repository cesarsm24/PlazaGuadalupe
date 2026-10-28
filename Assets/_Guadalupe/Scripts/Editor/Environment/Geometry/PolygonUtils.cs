using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Operaciones geométricas sobre polígonos simples en 2D (plano XZ).</summary>
    public static class PolygonUtils
    {
        private const float Epsilon = 1e-5f;

        /// <summary>Área con signo (positiva si los vértices van en sentido antihorario).</summary>
        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        /// <summary>Centroide aproximado (media de los vértices).</summary>
        public static Vector2 Centroid(IReadOnlyList<Vector2> polygon)
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2 p in polygon)
            {
                sum += p;
            }

            return sum / Mathf.Max(1, polygon.Count);
        }

        /// <summary>
        /// Limpia un anillo de OSM: quita el vértice de cierre, los duplicados y deja el orden antihorario.
        /// </summary>
        /// <returns>El anillo limpio, o una lista vacía si no forma un polígono válido.</returns>
        public static List<Vector2> NormalizeRing(IReadOnlyList<Vector2> ring)
        {
            var result = new List<Vector2>(ring.Count);
            foreach (Vector2 p in ring)
            {
                if (result.Count == 0 || (p - result[result.Count - 1]).sqrMagnitude > 0.0025f)
                {
                    result.Add(p);
                }
            }

            if (result.Count > 1 && (result[0] - result[result.Count - 1]).sqrMagnitude <= 0.0025f)
            {
                result.RemoveAt(result.Count - 1);
            }

            if (result.Count < 3 || Mathf.Abs(SignedArea(result)) < 1f)
            {
                return new List<Vector2>();
            }

            if (SignedArea(result) < 0f)
            {
                result.Reverse();
            }

            return result;
        }

        /// <summary>Indica si un punto está dentro del polígono (regla par-impar).</summary>
        public static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>Distancia mínima de un punto al contorno del polígono.</summary>
        public static float DistanceToEdges(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            float best = float.MaxValue;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, Epsilon));
                best = Mathf.Min(best, Vector2.Distance(point, a + ab * t));
            }

            return best;
        }

        /// <summary>
        /// Triangula un polígono simple en sentido antihorario mediante recorte de orejas (ear clipping).
        /// </summary>
        /// <returns>Índices de los triángulos (en sentido antihorario visto desde arriba).</returns>
        public static List<int> Triangulate(IReadOnlyList<Vector2> polygon)
        {
            var triangles = new List<int>();
            var remaining = new List<int>(polygon.Count);
            for (int i = 0; i < polygon.Count; i++)
            {
                remaining.Add(i);
            }

            // Límite de iteraciones para no bloquear el Editor con polígonos degenerados.
            int guard = polygon.Count * polygon.Count;
            while (remaining.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int curr = remaining[i];
                    int next = remaining[(i + 1) % remaining.Count];

                    if (!IsEar(polygon, remaining, prev, curr, next))
                    {
                        continue;
                    }

                    triangles.Add(prev);
                    triangles.Add(curr);
                    triangles.Add(next);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    break;
                }
            }

            if (remaining.Count == 3)
            {
                triangles.AddRange(remaining);
            }

            return triangles;
        }

        private static bool IsEar(IReadOnlyList<Vector2> polygon, List<int> remaining, int prev, int curr, int next)
        {
            Vector2 a = polygon[prev];
            Vector2 b = polygon[curr];
            Vector2 c = polygon[next];

            // El vértice debe ser convexo.
            if (Cross(b - a, c - b) <= Epsilon)
            {
                return false;
            }

            foreach (int index in remaining)
            {
                if (index == prev || index == curr || index == next)
                {
                    continue;
                }

                if (PointInTriangle(polygon[index], a, b, c))
                {
                    return false;
                }
            }

            return true;
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a);
            float d2 = Cross(c - b, p - b);
            float d3 = Cross(a - c, p - c);
            return d1 >= -Epsilon && d2 >= -Epsilon && d3 >= -Epsilon;
        }

        /// <summary>Esquinas mínima y máxima del rectángulo que contiene un polígono.</summary>
        public static void Bounds(IReadOnlyList<Vector2> polygon, out Vector2 min, out Vector2 max)
        {
            min = polygon[0];
            max = polygon[0];
            foreach (Vector2 v in polygon)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
        }

        /// <summary>Recorta un polígono por un semiplano: se queda con la parte donde (p − point)·normal ≥ 0.</summary>
        public static List<Vector2> ClipHalfPlane(IReadOnlyList<Vector2> polygon, Vector2 point, Vector2 normal)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                float da = Vector2.Dot(a - point, normal);
                float db = Vector2.Dot(b - point, normal);
                if (da >= 0f)
                {
                    result.Add(a);
                }

                if ((da >= 0f) != (db >= 0f))
                {
                    result.Add(Vector2.Lerp(a, b, da / (da - db)));
                }
            }

            return result;
        }

        /// <summary>Envolvente convexa en sentido antihorario (cadena monótona de Andrew).</summary>
        public static List<Vector2> ConvexHull(IReadOnlyList<Vector2> points)
        {
            var sorted = new List<Vector2>(points);
            sorted.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
            var hull = new List<Vector2>();
            float Cross(Vector2 o, Vector2 p, Vector2 q) => (p.x - o.x) * (q.y - o.y) - (p.y - o.y) * (q.x - o.x);

            for (int pass = 0; pass < 2; pass++)
            {
                int start = hull.Count;
                foreach (Vector2 p in sorted)
                {
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0f)
                    {
                        hull.RemoveAt(hull.Count - 1);
                    }

                    hull.Add(p);
                }

                hull.RemoveAt(hull.Count - 1);
                sorted.Reverse();
            }

            return hull;
        }
    }
}
