using System.Collections.Generic;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.AtriumSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Esquina del atrio hacia la calle Marqués de la Romana: al pie del muro con el relieve de bronce, una
    /// franja de losas de granito que sube en escalones cortos desde la plaza hasta la calle, separada de la
    /// calzada por un bordillo.
    /// </summary>
    internal static class CornerStepsBuilder
    {
        // El relieve va en la misma malla que los escalones.
        public static void Build(GenerationContext ctx, GameObject group)
        {
            var builder = new MeshBuilder(Count);
            float bottom = AtriumBuilder.FoundationY(ctx);
            float[] risers = AtriumPlan.CornerRisers;

            // Cotas de la plaza junto a la escalinata y de la calle pasada la esquina: los escalones las reparten.
            float plazaY = PlanFrame.GroundY(ctx, new Vector2(risers[0] - 0.8f, 11.4f));
            float streetY = PlanFrame.GroundY(ctx, new Vector2(risers[risers.Length - 1] + 0.8f, 14.2f));
            float rise = Mathf.Max(0.08f, (streetY - plazaY) / risers.Length);

            List<Vector2> platform = Platform();
            for (int i = 0; i < risers.Length - 1; i++)
            {
                List<Vector2> tread = PolygonUtils.ClipHalfPlane(platform, new Vector2(risers[i], 0f), Vector2.right);
                tread = PolygonUtils.ClipHalfPlane(tread, new Vector2(risers[i + 1], 0f), Vector2.left);
                if (tread.Count >= 3)
                {
                    float top = plazaY + (i + 1) * rise;
                    builder.AddSlab(tread, _ => top, bottom, Granite, Paving);
                }
            }

            AddRelief(ctx, builder);
            AtriumBuilder.AddToScene(ctx, builder, "EscalonesEsquina", group);
        }

        /// <summary>Franja entre el bordillo de la calle y el muro del atrio.</summary>
        private static List<Vector2> Platform()
        {
            float[] risers = AtriumPlan.CornerRisers;
            var area = new List<Vector2>
            {
                new Vector2(risers[0], 6f), new Vector2(risers[risers.Length - 1], 6f),
                new Vector2(risers[risers.Length - 1], 16f), new Vector2(risers[0], 16f),
            };

            var wallA = new Vector2(8.45f, AtriumPlan.CornerWallV(8.45f));
            var wallB = new Vector2(17.7f, AtriumPlan.CornerWallV(17.7f));
            Vector2 wallDir = (wallB - wallA).normalized;
            area = PolygonUtils.ClipHalfPlane(area, wallA, new Vector2(wallDir.y, -wallDir.x));

            Vector2[] kerb = AtriumPlan.CornerKerb;
            Vector2 kerbDir = (kerb[1] - kerb[0]).normalized;
            return PolygonUtils.ClipHalfPlane(area, kerb[0], new Vector2(-kerbDir.y, kerbDir.x));
        }

        /// <summary>Relieve de bronce en un marco de granito en el muro, junto a la escalinata.</summary>
        private static void AddRelief(GenerationContext ctx, MeshBuilder builder)
        {
            const float u = 11.4f;
            var onWall = new Vector2(u, AtriumPlan.CornerWallV(u));
            Vector2 along = new Vector2(17.7f - 8.45f, AtriumPlan.CornerWallV(17.7f) - AtriumPlan.CornerWallV(8.45f)).normalized;
            Vector2 outward = new Vector2(along.y, -along.x);
            float yaw = -Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;
            float ground = PlanFrame.GroundY(ctx, onWall + outward * 0.6f);
            Vector2 p = onWall + outward * 0.04f;
            builder.AddOrientedBox(new Vector3(p.x, ground + 2f, p.y), new Vector3(1.55f, 2.35f, 0.06f), yaw, Granite);
            Vector2 q = onWall + outward * 0.07f;
            builder.AddOrientedBox(new Vector3(q.x, ground + 2f, q.y), new Vector3(1.35f, 2.15f, 0.05f), yaw, Bronze);
        }
    }
}
