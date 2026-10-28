using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Utilidades geométricas en el plano del suelo (X, Z), compartidas por los constructores del entorno.
    /// Los puntos 2D usan <c>x</c> para la X del mundo e <c>y</c> para la Z.
    /// </summary>
    internal static class PlanarGeometry
    {
        /// <summary>Convierte un punto del plano a un punto 3D a la altura indicada.</summary>
        internal static Vector3 ToWorld(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        /// <summary>Normal exterior del lado a→b de un polígono en sentido antihorario.</summary>
        internal static Vector2 Outward(Vector2 a, Vector2 b)
        {
            Vector2 d = (b - a).normalized;
            return new Vector2(d.y, -d.x);
        }

        /// <summary>
        /// Giro en grados alrededor del eje vertical que alinea el eje X local con <paramref name="direction"/>, como
        /// lo espera <see cref="MeshBuilder.AddOrientedBox"/>.
        /// </summary>
        internal static float Yaw(Vector2 direction) => Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg;

        /// <summary>
        /// Punto de una polilínea más cercano a <paramref name="p"/>: tramo, posición en él sin recortar (fuera de
        /// [0, 1] si queda más allá de sus extremos), punto, distancia y longitud recorrida hasta él.
        /// </summary>
        internal static PolylineHit ClosestOnPolyline(IReadOnlyList<Vector2> line, Vector2 p)
        {
            var hit = new PolylineHit(0, 0f, line.Count > 0 ? line[0] : Vector2.zero, float.MaxValue, 0f);
            float walked = 0f;
            for (int i = 0; i < line.Count - 1; i++)
            {
                Vector2 a = line[i];
                Vector2 ab = line[i + 1] - a;
                float length = ab.magnitude;
                float t = Vector2.Dot(p - a, ab) / (length * length);
                Vector2 closest = a + ab * Mathf.Clamp01(t);
                float distance = Vector2.Distance(p, closest);
                if (distance < hit.Distance)
                {
                    hit = new PolylineHit(i, t, closest, distance, walked + Mathf.Clamp01(t) * length);
                }

                walked += length;
            }

            return hit;
        }

        /// <summary>Longitud total de una polilínea.</summary>
        internal static float PolylineLength(IReadOnlyList<Vector2> line)
        {
            float total = 0f;
            for (int i = 0; i < line.Count - 1; i++)
            {
                total += Vector2.Distance(line[i], line[i + 1]);
            }

            return total;
        }

        /// <summary>Índices de todos los lados de un polígono.</summary>
        internal static List<int> AllEdges(IReadOnlyList<Vector2> polygon)
        {
            var edges = new List<int>(polygon.Count);
            for (int i = 0; i < polygon.Count; i++)
            {
                edges.Add(i);
            }

            return edges;
        }

        /// <summary>Indica si un punto está dentro de un polígono (falso si el polígono está vacío).</summary>
        internal static bool IsInside(List<Vector2> polygon, Vector2 point)
        {
            return polygon.Count > 0 && PolygonUtils.Contains(polygon, point);
        }

        /// <summary>Punto y dirección de la polilínea a una distancia <paramref name="s"/> de su inicio.</summary>
        internal static void PointAt(Vector2[] line, float s, out Vector2 point, out Vector2 direction)
        {
            for (int i = 0; i < line.Length - 1; i++)
            {
                float length = Vector2.Distance(line[i], line[i + 1]);
                if (s <= length || i == line.Length - 2)
                {
                    direction = (line[i + 1] - line[i]) / length;
                    point = line[i] + direction * Mathf.Min(s, length);
                    return;
                }

                s -= length;
            }

            point = line[0];
            direction = Vector2.right;
        }

        /// <summary>Puntos del contorno de un polígono cada <paramref name="step"/> metros.</summary>
        internal static IEnumerable<Vector2> Densify(List<Vector2> polygon, float step)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / step));
                for (int k = 0; k < n; k++)
                {
                    yield return Vector2.Lerp(a, b, (float)k / n);
                }
            }
        }
    }

    /// <summary>Resultado de <see cref="PlanarGeometry.ClosestOnPolyline"/>.</summary>
    internal readonly struct PolylineHit
    {
        public readonly int Segment;
        public readonly float T;
        public readonly Vector2 Point;
        public readonly float Distance;
        public readonly float Along;

        public PolylineHit(int segment, float t, Vector2 point, float distance, float along)
        {
            Segment = segment;
            T = t;
            Point = point;
            Distance = distance;
            Along = along;
        }
    }
}
