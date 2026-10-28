using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.AtriumSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Escalinata de granito entre la plaza y el atrio: un tramo superior de 13 peldaños entre las
    /// balaustradas del atrio, una meseta con dos obeliscos y un tramo inferior de 6 peldaños que la rodea
    /// por tres lados.
    /// </summary>
    internal static class MainStairsBuilder
    {
        public static void Build(GenerationContext ctx, GameObject group)
        {
            float bottom = AtriumBuilder.FoundationY(ctx);
            float topLevel = AtriumPlan.StairsTopLevel;
            float rise = AtriumPlan.StairsRise;
            var builder = new MeshBuilder(Count);

            // Tramo superior: cada peldaño es un bloque macizo desde su huella hasta el atrio.
            for (int k = 1; k <= AtriumPlan.UpperSteps; k++)
            {
                float nose = AtriumPlan.StairsTop - k * AtriumPlan.UpperRun;
                AddBlock(builder, AtriumPlan.StairsWest, AtriumPlan.StairsEast, nose, AtriumPlan.StairsTop,
                    bottom, PlanFrame.Y(ctx, topLevel - k * rise));
            }

            // Meseta y tramo inferior, cuyos peldaños la rodean por los lados y el frente.
            Rect landing = AtriumPlan.Landing;
            float landingLevel = AtriumPlan.LandingLevel;
            AddBlock(builder, landing.xMin, landing.xMax, landing.yMin, landing.yMax, bottom, PlanFrame.Y(ctx, landingLevel));
            for (int k = 1; k <= AtriumPlan.LowerSteps; k++)
            {
                float grow = k * AtriumPlan.LowerRun;
                AddBlock(builder, landing.xMin - grow, landing.xMax + grow, landing.yMin - grow, landing.yMax,
                    bottom, PlanFrame.Y(ctx, landingLevel - k * rise));
            }

            foreach (Vector2 p in AtriumPlan.Obelisks)
            {
                AddObelisk(builder, new Vector3(p.x, PlanFrame.Y(ctx, landingLevel), p.y));
            }

            AtriumBuilder.AddToScene(ctx, builder, "Escalinata", group);
        }

        private static void AddBlock(MeshBuilder builder, float u0, float u1, float v0, float v1, float y0, float y1)
        {
            builder.AddBoxBetween(new Vector3(u0, y0, v0), new Vector3(u1, y1, v1), Granite);
        }

        /// <summary>Obelisco de granito: pedestal con cornisa, aguja, piramidión y bola.</summary>
        private static void AddObelisk(MeshBuilder builder, Vector3 foot)
        {
            builder.AddBox(foot + Vector3.up * 0.2f, new Vector3(1.05f, 0.4f, 1.05f), Granite);
            builder.AddBox(foot + Vector3.up * 0.95f, new Vector3(0.85f, 1.1f, 0.85f), Ashlar);
            builder.AddBox(foot + Vector3.up * 1.57f, new Vector3(1f, 0.14f, 1f), Granite);
            builder.AddBox(foot + Vector3.up * 3.05f, new Vector3(0.42f, 2.8f, 0.42f), Ashlar);
            ArchitecturalElements.AddSpire(builder, foot + Vector3.up * 4.45f, 0.3f, 4, 0.45f, Ashlar);
            ArchitecturalElements.AddBall(builder, foot + Vector3.up * 5.05f, 0.17f, Granite);
        }
    }
}
