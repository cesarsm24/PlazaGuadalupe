using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.HouseMeshSlots;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Elementos que se añaden a las fachadas según el catálogo de cada casa: tejaroz, balcón corrido de madera,
    /// esquinas de sillería, cables, azotea con balaustrada y suciedad al pie de los muros.
    /// </summary>
    internal static class FacadeOrnaments
    {
        /// <summary>
        /// Suciedad y salpicaduras al pie del muro, siguiendo la pendiente de la calle: degradado transparente que
        /// se desvanece hacia arriba con borde irregular.
        /// </summary>
        internal static void AddGrime(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b)
        {
            const float height = 3.4f;
            const float tile = 4f;
            float length = Vector2.Distance(a, b);
            if (length < 1f)
            {
                return;
            }

            Vector2 n = Outward(a, b);
            Vector3 facing = ToWorld(n, 0f);
            int segments = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
            float phase = (a.x * 0.37f + a.y * 0.61f) % 1f;
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                Vector2 p = Vector2.Lerp(a, b, t0) + n * 0.04f;
                Vector2 q = Vector2.Lerp(a, b, t1) + n * 0.04f;
                float gp = ctx.GroundY(p.x, p.y) - 0.05f;
                float gq = ctx.GroundY(q.x, q.y) - 0.05f;
                float u0 = phase + t0 * length / tile;
                float u1 = phase + t1 * length / tile;
                builder.AddQuadWithUvs(
                    new[] { ToWorld(p, gp), ToWorld(q, gq), ToWorld(q, gq + height), ToWorld(p, gp + height) },
                    new[] { new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, 1f), new Vector2(u0, 1f) },
                    facing, Grime);
            }
        }

        /// <summary>Tejaroz: pequeño alero de teja sobre canes encima de la planta baja.</summary>
        internal static void AddTejaroz(MeshBuilder builder, Vector2 a, Vector2 b, float y)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 n = Outward(a, b);
            float length = Vector2.Distance(a, b) - 0.4f;
            if (length < 1.5f)
            {
                return;
            }

            Vector2 mid = (a + b) * 0.5f;
            Vector2 l0 = mid - dir * (length * 0.5f);
            Vector2 r0 = mid + dir * (length * 0.5f);
            const float projection = 0.6f;
            builder.AddConvexFace(new[] { ToWorld(l0, y + 0.3f), ToWorld(r0, y + 0.3f), ToWorld(r0 + n * projection, y), ToWorld(l0 + n * projection, y) },
                Vector3.up + ToWorld(n, 0f), ToWorld(dir, 0f), Roof);
            builder.AddConvexFace(new[] { ToWorld(l0, y + 0.3f), ToWorld(r0, y + 0.3f), ToWorld(r0 + n * projection, y), ToWorld(l0 + n * projection, y) },
                Vector3.down, ToWorld(dir, 0f), Wood);
            int corbels = Mathf.Max(2, Mathf.RoundToInt(length / 0.6f));
            float yaw = Yaw(dir);
            for (int i = 0; i <= corbels; i++)
            {
                Vector2 p = l0 + dir * (length * i / corbels) + n * (projection * 0.4f);
                builder.AddOrientedBox(ToWorld(p, y - 0.05f), new Vector3(0.1f, 0.12f, projection * 0.8f), yaw, Wood);
            }
        }

        /// <summary>
        /// Balcón corrido de madera en la primera planta: suelo de tablas sobre canes, balaustres torneados
        /// (simplificados) y pasamanos.
        /// </summary>
        /// <param name="balconies">Lista donde se anota el balcón construido (null para no anotarlo).</param>
        internal static void AddWoodenGallery(MeshBuilder builder, Vector2 a, Vector2 b, float floorY, List<BalconyAnchor> balconies = null)
        {
            const float depth = 0.85f;
            const float rail = 1f;
            Vector2 dir = (b - a).normalized;
            Vector2 n = new Vector2(dir.y, -dir.x);
            float length = Vector2.Distance(a, b) - 0.6f;
            if (length < 2f)
            {
                return;
            }

            float yaw = Yaw(dir);
            Vector2 mid = (a + b) * 0.5f;
            builder.AddOrientedBox(ToWorld(mid + n * (depth * 0.5f), floorY + 0.05f), new Vector3(length, 0.1f, depth), yaw, Wood);
            builder.AddOrientedBox(ToWorld(mid + n * (depth - 0.04f), floorY + rail), new Vector3(length, 0.07f, 0.09f), yaw, Wood);
            balconies?.Add(new BalconyAnchor(ToWorld(mid + n * depth, floorY + rail), dir, n, length));
            builder.AddOrientedBox(ToWorld(mid + n * (depth - 0.04f), floorY + 0.18f), new Vector3(length, 0.06f, 0.07f), yaw, Wood);
            int balusters = Mathf.FloorToInt(length / 0.14f);
            for (int i = 0; i <= balusters; i++)
            {
                Vector2 p = mid + dir * (-length * 0.5f + i * length / balusters) + n * (depth - 0.04f);
                builder.AddOrientedBox(ToWorld(p, floorY + 0.59f), new Vector3(0.045f, 0.78f, 0.045f), yaw, Wood);
            }

            foreach (float side in new[] { -0.5f, 0.5f })
            {
                Vector2 end = mid + dir * (length * side);
                builder.AddOrientedBox(ToWorld(end + n * (depth * 0.5f), floorY + 0.55f), new Vector3(0.07f, 0.9f, depth), yaw, Wood);
            }

            // Canes de madera bajo el suelo del balcón.
            int corbels = Mathf.Max(2, Mathf.RoundToInt(length / 1.2f));
            for (int i = 0; i <= corbels; i++)
            {
                Vector2 p = mid + dir * (-length * 0.5f + i * length / corbels) + n * (depth * 0.45f);
                builder.AddOrientedBox(ToWorld(p, floorY - 0.1f), new Vector3(0.12f, 0.18f, depth * 0.9f), yaw, Beam);
            }
        }

        /// <summary>
        /// Esquinas de sillería: sillares alternos largo/corto en el arranque del lado si la esquina es exterior
        /// (ambos lados dan a la calle).
        /// </summary>
        internal static void AddQuoins(GenerationContext ctx, MeshBuilder builder, List<Vector2> footprint, int edge,
            FacadeDetailer detailer, float eaveY, HouseStyle style)
        {
            if (style == null || !style.Quoins)
            {
                return;
            }

            int n = footprint.Count;
            Vector2 a = footprint[edge];
            Vector2 b = footprint[(edge + 1) % n];
            Vector2 prev = footprint[(edge - 1 + n) % n];
            if (detailer.IsPartyWall(a, b, footprint) && detailer.IsPartyWall(prev, a, footprint))
            {
                return;
            }

            Vector2 dirA = (b - a).normalized;
            Vector2 dirB = (prev - a).normalized;
            float bottom = ctx.GroundY(a.x, a.y) + 0.9f;
            const float course = 0.38f;
            int courses = Mathf.FloorToInt((eaveY - 0.3f - bottom) / course);
            for (int i = 0; i < courses; i++)
            {
                float y = bottom + (i + 0.5f) * course;
                bool longOnA = i % 2 == 0;
                // Normal exterior de cada cara: la del lado a→b y la del lado anterior (prev→a).
                var faces = new[]
                {
                    (dirA, new Vector2(dirA.y, -dirA.x), longOnA),
                    (dirB, new Vector2(-dirB.y, dirB.x), !longOnA),
                };
                foreach ((Vector2 d, Vector2 outward, bool isLong) in faces)
                {
                    float len = isLong ? 0.62f : 0.36f;
                    Vector2 c = a + d * (len * 0.5f) + outward * 0.02f;
                    builder.AddOrientedBox(ToWorld(c, y), new Vector3(len, course - 0.04f, 0.06f), Yaw(d), Stone);
                }
            }
        }

        /// <summary>Cable negro grapado a la fachada que cuelga ligeramente entre grapas, como los de la plaza.</summary>
        internal static void AddCable(MeshBuilder builder, Vector2 a, Vector2 b, float y)
        {
            float length = Vector2.Distance(a, b);
            if (length < 3f)
            {
                return;
            }

            Vector2 dir = (b - a) / length;
            Vector2 n = new Vector2(dir.y, -dir.x);
            int spans = Mathf.Max(1, Mathf.RoundToInt(length / 3f));
            const int steps = 4;
            for (int sIdx = 0; sIdx < spans; sIdx++)
            {
                for (int k = 0; k < steps; k++)
                {
                    float t0 = (sIdx + (float)k / steps) / spans;
                    float t1 = (sIdx + (float)(k + 1) / steps) / spans;
                    float sag0 = 0.07f * Mathf.Sin(Mathf.PI * k / steps);
                    float sag1 = 0.07f * Mathf.Sin(Mathf.PI * (k + 1) / steps);
                    Vector2 p0 = a + dir * (length * t0) + n * 0.04f;
                    Vector2 p1 = a + dir * (length * t1) + n * 0.04f;
                    Vector3 w0 = ToWorld(p0, y - sag0);
                    Vector3 w1 = ToWorld(p1, y - sag1);
                    Vector3 up = Vector3.up * 0.012f;
                    Vector3 o = ToWorld(n, 0f) * 0.012f;
                    builder.AddConvexFace(new[] { w0 - up + o, w1 - up + o, w1 + up + o, w0 + up + o }, ToWorld(n, 0f), w1 - w0, Metal);
                    builder.AddConvexFace(new[] { w0 - up - o, w1 - up - o, w1 - up + o, w0 - up + o }, Vector3.down, w1 - w0, Metal);
                }
            }
        }

        /// <summary>
        /// Azotea plana: balaustrada de piedra en los lados que dan a la calle y pretil macizo encalado con
        /// albardilla en las medianeras y los lados cortos.
        /// </summary>
        /// <param name="openEdges">Lados que dan al tejado del resto de la casa, sin balaustrada ni pretil.</param>
        internal static void AddRoofTerrace(MeshBuilder builder, List<Vector2> footprint, FacadeDetailer detailer, float eaveY,
            ICollection<int> openEdges = null)
        {
            const float ParapetHeight = 1f;
            const float ParapetThickness = 0.25f;

            builder.AddCap(footprint, eaveY, Wall, facingUp: true);
            var cornice = BuildingShapes.InsetEdges(footprint, AllEdges(footprint), -0.15f);
            if (cornice != null)
            {
                builder.AddWalls(cornice, eaveY - 0.25f, eaveY, Stone);
            }

            for (int i = 0; i < footprint.Count; i++)
            {
                if (openEdges != null && openEdges.Contains(i))
                {
                    continue;
                }

                Vector2 a = footprint[i];
                Vector2 b = footprint[(i + 1) % footprint.Count];
                float length = Vector2.Distance(a, b);
                Vector2 dir = (b - a) / length;
                Vector2 inward = new Vector2(-dir.y, dir.x);
                if (!detailer.IsPartyWall(a, b, footprint) && length > 1.5f)
                {
                    ArchitecturalElements.AddBalustrade(builder, a + inward * 0.25f, b + inward * 0.25f, eaveY);
                    continue;
                }

                // Pretil macizo pegado al borde, alargado su grosor para cerrar las esquinas con los lados vecinos.
                Vector2 mid = (a + b) * 0.5f + inward * (ParapetThickness * 0.5f);
                float yaw = Yaw(dir);
                builder.AddOrientedBox(ToWorld(mid, eaveY + ParapetHeight * 0.5f),
                    new Vector3(length + ParapetThickness, ParapetHeight, ParapetThickness), yaw, Wall);
                builder.AddOrientedBox(ToWorld(mid, eaveY + ParapetHeight + 0.04f),
                    new Vector3(length + ParapetThickness + 0.1f, 0.08f, ParapetThickness + 0.1f), yaw, Stone);
            }
        }
    }
}
