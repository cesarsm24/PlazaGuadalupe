using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Operaciones de forma específicas de edificios: detección de fachadas a la plaza,
    /// retranqueo de soportales y cubiertas.
    /// </summary>
    public static class BuildingShapes
    {
        /// <summary>Pendiente de las cubiertas (tangente de unos 25°, habitual en teja árabe).</summary>
        public const float RoofPitch = 0.466f;

        private const float MinRoofArea = 8f;

        /// <summary>Distancia máxima entre una fachada y el contorno de la plaza para considerarla frente a ella.</summary>
        private const float MaxFacadeGap = 6f;

        /// <summary>
        /// Devuelve los índices de los lados del edificio que dan a la plaza: lados de al menos
        /// <paramref name="minLength"/> metros, junto al contorno y cuya normal exterior apunta hacia dentro de ella.
        /// </summary>
        /// <param name="footprint">Planta del edificio en sentido antihorario.</param>
        /// <param name="plaza">Contorno de la plaza en sentido antihorario.</param>
        public static List<int> FindFrontingEdges(IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> plaza, float minLength = 3f)
        {
            var result = new List<int>();
            if (plaza == null || plaza.Count < 3)
            {
                return result;
            }

            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 a = footprint[i];
                Vector2 b = footprint[(i + 1) % footprint.Count];
                if (Vector2.Distance(a, b) < minLength)
                {
                    continue;
                }

                // El contorno de OSM no siempre llega hasta las fachadas (aceras, terrazas): se admite
                // un hueco de unos metros siempre que delante del lado esté la plaza.
                Vector2 mid = (a + b) * 0.5f;
                Vector2 outward = Outward(a, b);
                float distance = PolygonUtils.DistanceToEdges(plaza, mid);
                bool isClose = distance < MaxFacadeGap;
                bool facesPlaza = PolygonUtils.Contains(plaza, mid + outward * (distance + 1.5f));
                if (facesPlaza && isClose)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// Desplaza hacia dentro los lados indicados una distancia <paramref name="depth"/> y recalcula
        /// las esquinas como intersección de los lados adyacentes.
        /// </summary>
        /// <returns>La planta retranqueada, o <c>null</c> si el resultado no es un polígono válido.</returns>
        public static List<Vector2> InsetEdges(IReadOnlyList<Vector2> footprint, ICollection<int> edges, float depth)
        {
            int n = footprint.Count;
            var offsets = new float[n];
            foreach (int e in edges)
            {
                offsets[e] = depth;
            }

            var result = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                Vector2 vertex = footprint[i];

                // Lado anterior (prev → i) y siguiente (i → i+1), desplazados hacia dentro.
                Vector2 pa = footprint[prev];
                Vector2 nb = footprint[(i + 1) % n];
                Vector2 inPrev = -Outward(pa, vertex);
                Vector2 inNext = -Outward(vertex, nb);
                Vector2 p1 = vertex + inPrev * offsets[prev];
                Vector2 d1 = vertex - pa;
                Vector2 p2 = vertex + inNext * offsets[i];
                Vector2 d2 = nb - vertex;

                // Si los lados son (casi) paralelos ambos desplazamientos van en la misma dirección;
                // se descarta una intersección muy lejana para no generar picos.
                bool valid = IntersectLines(p1, d1, p2, d2, out Vector2 hit) && Vector2.Distance(hit, vertex) < Mathf.Abs(depth) * 3f + 0.5f;
                result.Add(valid ? hit : vertex + inNext * Mathf.Max(offsets[prev], offsets[i]));
            }

            float originalArea = PolygonUtils.SignedArea(footprint);
            float newArea = PolygonUtils.SignedArea(result);
            return newArea > originalArea * 0.3f ? result : null;
        }

        /// <summary>
        /// Separa de la planta una franja de fondo constante a lo largo de una fachada (lados consecutivos).
        /// </summary>
        /// <param name="strip">Franja junto a la fachada, antihoraria (null si no se puede separar).</param>
        /// <param name="innerEdges">Lados de la franja que dan al resto de la planta.</param>
        /// <returns>Resto de la planta detrás de la franja, o null si la fachada no es continua o la franja no cabe.</returns>
        public static List<Vector2> SplitFrontStrip(IReadOnlyList<Vector2> footprint, ICollection<int> front, float depth,
            out List<Vector2> strip, out List<int> innerEdges)
        {
            strip = null;
            innerEdges = null;
            int n = footprint.Count;
            int start = -1;
            foreach (int e in front)
            {
                if (!front.Contains((e - 1 + n) % n))
                {
                    if (start >= 0)
                    {
                        return null;
                    }

                    start = e;
                }
            }

            List<Vector2> rest = start < 0 || front.Count >= n ? null : InsetEdges(footprint, front, depth);
            if (rest == null)
            {
                return null;
            }

            // Fachada de start a start+edges, y vuelta por los mismos vértices desplazados hacia dentro.
            int edges = front.Count;
            strip = new List<Vector2>(2 * edges + 2);
            for (int k = 0; k <= edges; k++)
            {
                strip.Add(footprint[(start + k) % n]);
            }

            for (int k = edges; k >= 0; k--)
            {
                strip.Add(rest[(start + k) % n]);
            }

            innerEdges = new List<int>();
            for (int k = edges + 1; k <= 2 * edges; k++)
            {
                innerEdges.Add(k);
            }

            return rest;
        }

        /// <summary>Indica si el polígono (antihorario) es convexo.</summary>
        public static bool IsConvex(IReadOnlyList<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                Vector2 c = polygon[(i + 2) % polygon.Count];
                if (Cross(b - a, c - b) < -1e-3f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Añade la cubierta sobre la planta: a cuatro aguas si es casi rectangular, piramidal si es convexa y,
        /// si es cóncava, faldones hasta un retranqueo interior plano. Queda plana si la planta es muy pequeña.
        /// </summary>
        /// <returns>Altura de la cubierta sobre el alero en el centroide de la planta (0 si es plana).</returns>
        /// <param name="overhang">Vuelo del alero en metros (0 = sin alero).</param>
        /// <param name="soffitSubmesh">Submalla de la cara inferior del alero (si hay vuelo).</param>
        /// <param name="overhangEdges">Lados con alero (los que dan a la calle); null para todos. En las medianeras no
        /// vuela el alero, que atravesaría el tejado de la casa vecina.</param>
        public static float AddRoof(MeshBuilder builder, IReadOnlyList<Vector2> footprint, float eaveY, int submesh,
            float overhang = 0f, int soffitSubmesh = -1, IReadOnlyList<int> overhangEdges = null)
        {
            float area = PolygonUtils.SignedArea(footprint);
            if (area < MinRoofArea)
            {
                builder.AddCap(footprint, eaveY, submesh, facingUp: true);
                return 0f;
            }

            if (!IsConvex(footprint))
            {
                return TryAddInsetRoof(builder, footprint, eaveY, submesh, out float insetRise) ? insetRise : CapFlat(builder, footprint, eaveY, submesh);
            }

            if (overhang > 0f && soffitSubmesh >= 0)
            {
                var all = new List<int>(footprint.Count);
                for (int i = 0; i < footprint.Count; i++)
                {
                    if (overhangEdges == null || ContainsIndex(overhangEdges, i))
                    {
                        all.Add(i);
                    }
                }

                // Un retranqueo negativo de todos los lados es un desplazamiento hacia fuera: el alero.
                List<Vector2> eaves = InsetEdges(footprint, all, -overhang);
                if (eaves != null && IsConvex(eaves))
                {
                    AddSoffit(builder, footprint, eaves, eaveY, soffitSubmesh);
                    footprint = eaves;
                }
            }

            if (footprint.Count == 4 && IsNearlyRectangular(footprint) && TryAddHipRoof(builder, footprint, eaveY, submesh, out float hipRise))
            {
                return hipRise;
            }

            float rise = Mathf.Clamp(0.2f * Mathf.Sqrt(area), 0.6f, 2.5f);
            AddPyramidRoof(builder, footprint, eaveY, rise, submesh);
            return rise;
        }

        private static float CapFlat(MeshBuilder builder, IReadOnlyList<Vector2> footprint, float eaveY, int submesh)
        {
            builder.AddCap(footprint, eaveY, submesh, facingUp: true);
            return 0f;
        }

        /// <summary>
        /// Cubierta de una planta cóncava: un faldón con la pendiente <see cref="RoofPitch"/> desde cada lado hasta
        /// la planta retranqueada, que se cierra en plano. Prueba retranqueos de 3 m a 0,8 m y usa el mayor que
        /// deja un polígono válido dentro de la planta.
        /// </summary>
        private static bool TryAddInsetRoof(MeshBuilder builder, IReadOnlyList<Vector2> footprint, float eaveY, int submesh, out float rise)
        {
            var all = new List<int>(footprint.Count);
            for (int i = 0; i < footprint.Count; i++)
            {
                all.Add(i);
            }

            var outer = new List<Vector2>(footprint);
            for (float inset = 3f; inset >= 0.8f; inset -= 0.4f)
            {
                List<Vector2> inner = InsetEdges(outer, all, inset);
                if (inner == null || inner.Count != outer.Count || PolygonUtils.SignedArea(inner) <= 0.5f || !inner.TrueForAll(p => PolygonUtils.Contains(outer, p)))
                {
                    continue;
                }

                rise = inset * RoofPitch;
                float top = eaveY + rise;
                for (int i = 0; i < outer.Count; i++)
                {
                    int j = (i + 1) % outer.Count;
                    builder.AddConvexFace(new[] { ToWorld(outer[i], eaveY), ToWorld(outer[j], eaveY), ToWorld(inner[j], top), ToWorld(inner[i], top) },
                        Facing(outer[i], outer[j]), ToWorld(outer[j], 0f) - ToWorld(outer[i], 0f), submesh);
                }

                builder.AddCap(inner, top, submesh, facingUp: true);
                return true;
            }

            rise = 0f;
            return false;
        }

        private static bool ContainsIndex(IReadOnlyList<int> list, int value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Cara inferior del alero: banda entre el muro y el borde del tejado, mirando abajo.</summary>
        private static void AddSoffit(MeshBuilder builder, IReadOnlyList<Vector2> wall, IReadOnlyList<Vector2> eaves, float y, int submesh)
        {
            for (int i = 0; i < wall.Count; i++)
            {
                int j = (i + 1) % wall.Count;
                builder.AddConvexFace(
                    new[] { ToWorld(wall[i], y), ToWorld(wall[j], y), ToWorld(eaves[j], y), ToWorld(eaves[i], y) },
                    Vector3.down, ToWorld(wall[j] - wall[i], 0f), submesh);
            }
        }

        private static bool TryAddHipRoof(MeshBuilder builder, IReadOnlyList<Vector2> footprint, float eaveY, int submesh, out float rise)
        {
            rise = 0f;
            // Se rota la lista para que los lados largos sean p0→p1 y p2→p3.
            int shift = Vector2.Distance(footprint[0], footprint[1]) >= Vector2.Distance(footprint[1], footprint[2]) ? 0 : 1;
            Vector2 p0 = footprint[shift];
            Vector2 p1 = footprint[(shift + 1) % 4];
            Vector2 p2 = footprint[(shift + 2) % 4];
            Vector2 p3 = footprint[(shift + 3) % 4];

            float length = (Vector2.Distance(p0, p1) + Vector2.Distance(p2, p3)) * 0.5f;
            float width = (Vector2.Distance(p1, p2) + Vector2.Distance(p3, p0)) * 0.5f;
            float halfRidge = (length - width) * 0.5f;
            if (halfRidge < 0.1f)
            {
                return false;
            }

            rise = width * 0.5f * RoofPitch;
            Vector2 c = (p0 + p1 + p2 + p3) * 0.25f;
            Vector2 dir = (p1 - p0).normalized;
            Vector3 r1 = ToWorld(c - dir * halfRidge, eaveY + rise);
            Vector3 r2 = ToWorld(c + dir * halfRidge, eaveY + rise);
            Vector3 w0 = ToWorld(p0, eaveY);
            Vector3 w1 = ToWorld(p1, eaveY);
            Vector3 w2 = ToWorld(p2, eaveY);
            Vector3 w3 = ToWorld(p3, eaveY);

            builder.AddConvexFace(new[] { w0, w1, r2, r1 }, Facing(p0, p1), w1 - w0, submesh);
            builder.AddConvexFace(new[] { w1, w2, r2 }, Facing(p1, p2), w2 - w1, submesh);
            builder.AddConvexFace(new[] { w2, w3, r1, r2 }, Facing(p2, p3), w3 - w2, submesh);
            builder.AddConvexFace(new[] { w3, w0, r1 }, Facing(p3, p0), w0 - w3, submesh);
            return true;
        }

        private static void AddPyramidRoof(MeshBuilder builder, IReadOnlyList<Vector2> footprint, float eaveY, float rise, int submesh)
        {
            Vector3 apex = ToWorld(PolygonUtils.Centroid(footprint), eaveY + rise);
            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 a = footprint[i];
                Vector2 b = footprint[(i + 1) % footprint.Count];
                Vector3 wa = ToWorld(a, eaveY);
                Vector3 wb = ToWorld(b, eaveY);
                builder.AddConvexFace(new[] { wa, wb, apex }, Facing(a, b), wb - wa, submesh);
            }
        }

        private static bool IsNearlyRectangular(IReadOnlyList<Vector2> quad)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = quad[i];
                Vector2 b = quad[(i + 1) % 4];
                Vector2 c = quad[(i + 2) % 4];
                float angle = Vector2.Angle(a - b, c - b);
                if (Mathf.Abs(angle - 90f) > 15f)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 Facing(Vector2 a, Vector2 b)
        {
            Vector2 n = Outward(a, b);
            return new Vector3(n.x, 1f, n.y);
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        private static bool IntersectLines(Vector2 p1, Vector2 d1, Vector2 p2, Vector2 d2, out Vector2 hit)
        {
            float denominator = Cross(d1, d2);
            if (Mathf.Abs(denominator) < 1e-4f)
            {
                hit = Vector2.zero;
                return false;
            }

            float t = Cross(p2 - p1, d2) / denominator;
            hit = p1 + d1 * t;
            return true;
        }
    }
}
