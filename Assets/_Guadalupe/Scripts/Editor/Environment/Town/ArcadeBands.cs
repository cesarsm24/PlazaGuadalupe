using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Franjas de soportal: la banda de terreno a la derecha de cada polilínea de soportal donde la planta
    /// baja de todas las casas queda abierta, de modo que el pasillo es continuo de un extremo a otro sin
    /// muros entre casas vecinas.
    /// </summary>
    public static class ArcadeBands
    {
        /// <summary>Margen por delante de la línea que también se considera soportal (vuelos de esquina).</summary>
        public const float FrontTolerance = 1.5f;

        private const float SampleStep = 0.25f;
        private const int BisectionSteps = 10;

        /// <summary>
        /// Distancia lateral con signo de un punto a la polilínea (positiva a la derecha, hacia las casas).
        /// </summary>
        /// <returns>Falso si el punto queda más allá de los extremos de la polilínea.</returns>
        public static bool TryLateral(IReadOnlyList<Vector2> line, Vector2 p, out float lateral, out float along)
        {
            PolylineHit hit = PlanarGeometry.ClosestOnPolyline(line, p);
            along = hit.Along;
            lateral = 0f;
            if ((hit.Segment == 0 && hit.T < 0f) || (hit.Segment == line.Count - 2 && hit.T > 1f))
            {
                return false;
            }

            Vector2 dir = (line[hit.Segment + 1] - line[hit.Segment]).normalized;
            Vector2 right = new Vector2(dir.y, -dir.x);
            lateral = Vector2.Dot(p - hit.Point, right) >= 0f ? hit.Distance : -hit.Distance;
            return true;
        }

        /// <summary>Indica si un punto está dentro de la franja de soportal de una polilínea.</summary>
        public static bool InBand(IReadOnlyList<Vector2> line, Vector2 p, float depth)
        {
            return TryLateral(line, p, out float lateral, out _) && lateral >= -FrontTolerance && lateral <= depth;
        }

        /// <summary>
        /// Recorta un polígono (antihorario) quitándole la parte que cae dentro de la región indicada.
        /// </summary>
        /// <param name="inside">Función de pertenencia a la región que se elimina.</param>
        /// <param name="cut">Verdadero si se ha eliminado algo.</param>
        /// <returns>El polígono restante, o null si no queda nada.</returns>
        public static List<Vector2> ClipOutside(IReadOnlyList<Vector2> polygon, Func<Vector2, bool> inside, out bool cut)
        {
            var dense = new List<Vector2>();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / SampleStep));
                for (int k = 0; k < steps; k++)
                {
                    dense.Add(Vector2.Lerp(a, b, (float)k / steps));
                }
            }

            var flags = new bool[dense.Count];
            int insideCount = 0;
            for (int i = 0; i < dense.Count; i++)
            {
                flags[i] = inside(dense[i]);
                insideCount += flags[i] ? 1 : 0;
            }

            cut = insideCount > 0;
            if (insideCount == 0)
            {
                return new List<Vector2>(polygon);
            }

            if (insideCount == dense.Count)
            {
                return null;
            }

            var result = new List<Vector2>();
            for (int i = 0; i < dense.Count; i++)
            {
                int j = (i + 1) % dense.Count;
                if (!flags[i])
                {
                    result.Add(dense[i]);
                }

                if (flags[i] != flags[j])
                {
                    result.Add(Boundary(dense[i], dense[j], flags[i], inside));
                }
            }

            result = Simplify(result);
            return result.Count >= 3 && Mathf.Abs(PolygonUtils.SignedArea(result)) > 0.5f ? result : null;
        }

        /// <summary>Punto de la frontera de la región entre dos puntos a distinto lado (bisección).</summary>
        private static Vector2 Boundary(Vector2 a, Vector2 b, bool aInside, Func<Vector2, bool> inside)
        {
            for (int k = 0; k < BisectionSteps; k++)
            {
                Vector2 mid = (a + b) * 0.5f;
                if (inside(mid) == aInside)
                {
                    a = mid;
                }
                else
                {
                    b = mid;
                }
            }

            return (a + b) * 0.5f;
        }

        /// <summary>Quita puntos duplicados y alineados.</summary>
        private static List<Vector2> Simplify(List<Vector2> points)
        {
            var clean = new List<Vector2>();
            foreach (Vector2 p in points)
            {
                if (clean.Count == 0 || Vector2.Distance(clean[clean.Count - 1], p) > 0.05f)
                {
                    clean.Add(p);
                }
            }

            if (clean.Count > 1 && Vector2.Distance(clean[0], clean[clean.Count - 1]) <= 0.05f)
            {
                clean.RemoveAt(clean.Count - 1);
            }

            bool changed = true;
            while (changed && clean.Count > 3)
            {
                changed = false;
                for (int i = 0; i < clean.Count; i++)
                {
                    Vector2 prev = clean[(i - 1 + clean.Count) % clean.Count];
                    Vector2 cur = clean[i];
                    Vector2 next = clean[(i + 1) % clean.Count];
                    Vector2 u = cur - prev;
                    Vector2 v = next - cur;
                    float cross = u.x * v.y - u.y * v.x;
                    if (Mathf.Abs(cross) < 0.002f * u.magnitude * v.magnitude + 1e-5f && Vector2.Dot(u, v) > 0f)
                    {
                        clean.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }

            return clean;
        }
    }
}
