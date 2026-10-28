using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.AtriumSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Gradería de granito ante las portadas de la basílica: una meseta a la cota de las puertas y ocho
    /// peldaños hasta el atrio, que doblan por el extremo oeste y terminan escalonados por el este, donde
    /// arranca la pasarela.
    /// </summary>
    internal static class GraderiaBuilder
    {
        private const float BackV = 33.6f; // Fondo de la meseta, ya bajo la fachada.

        public static void Build(GenerationContext ctx, GameObject group)
        {
            float bottom = AtriumBuilder.FoundationY(ctx);
            int steps = AtriumPlan.GraderiaSteps;
            float footV = Edge(0f) - steps * AtriumPlan.GraderiaRun;
            float footLevel = AtriumPlan.AtriumLevel(new Vector2(0f, footV));
            float rise = (AtriumPlan.PortalLanding - footLevel) / steps;
            var builder = new MeshBuilder(Count);

            // Los peldaños inferiores, más anchos, se construyen primero; cada uno llega por detrás hasta la fachada.
            for (int k = steps; k >= 0; k--)
            {
                float west = AtriumPlan.GraderiaWest - k * AtriumPlan.GraderiaWestStep;
                float east = AtriumPlan.GraderiaEast + k * AtriumPlan.GraderiaEastStep;
                float nose = -k * AtriumPlan.GraderiaRun;
                Vector2[] footprint =
                {
                    new Vector2(west, Edge(west) + nose), new Vector2(east, Edge(east) + nose),
                    new Vector2(east, BackV), new Vector2(west, BackV),
                };
                float top = PlanFrame.Y(ctx, AtriumPlan.PortalLanding - k * rise);
                builder.AddSlab(footprint, _ => top, bottom, Granite, k == 0 ? Paving : Granite);
            }

            AtriumBuilder.AddToScene(ctx, builder, "Graderia", group);
        }

        /// <summary>Borde de la meseta de las portadas (v) en la abscisa u.</summary>
        private static float Edge(float u) => AtriumPlan.GraderiaTopEdge + (u - AtriumPlan.GraderiaWest) * AtriumPlan.GraderiaSlope;
    }
}
