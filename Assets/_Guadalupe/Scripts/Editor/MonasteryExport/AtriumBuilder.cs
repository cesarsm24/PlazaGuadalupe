using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.AtriumSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Construye el atrio del monasterio: plataformas enlosadas (atrio, atrio chico y meseta final) y
    /// balaustradas, y coordina la escalinata (<see cref="MainStairsBuilder"/>), la gradería
    /// (<see cref="GraderiaBuilder"/>), la pasarela (<see cref="WalkwayBuilder"/>), el acceso desde la calle (<see cref="MarquesAccessBuilder"/>) y el
    /// pasadizo de la portería (<see cref="PassageArchBuilder"/>).
    /// </summary>
    /// <remarks>Todo se construye en el marco (u, v) de <see cref="PlanFrame"/>.</remarks>
    internal static class AtriumBuilder
    {
        internal const string GroupName = "AccesoMonasterio";

        public static void Build(GenerationContext ctx)
        {
            // Cota de referencia de la portería y del ala oeste: la del atrio ante la portería.
            ctx.Results.TerraceTopY = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(new Vector2(-13f, 30f)));
            GameObject group = PlanFrame.CreateGroup(ctx, GroupName);

            BuildPlatforms(ctx, group);
            MainStairsBuilder.Build(ctx, group);
            GraderiaBuilder.Build(ctx, group);
            WalkwayBuilder.Build(ctx, group);
            MarquesAccessBuilder.Build(ctx, group);
            CornerStepsBuilder.Build(ctx, group);
            PassageArchBuilder.Build(ctx, group);
            BuildBalustrades(ctx, group);
        }

        /// <summary>Altura de la base de los macizos del atrio, por debajo de la plaza.</summary>
        internal static float FoundationY(GenerationContext ctx) => PlanFrame.Y(ctx, AtriumPlan.StairsFoot) - ctx.Data.Settings.FoundationDepth;

        internal static void AddToScene(GenerationContext ctx, MeshBuilder builder, string name, GameObject group)
        {
            ctx.Output.AddStatic(name, builder, $"SM_{name}", MaterialsFor(ctx.Data.Materials), group);
        }

        /// <summary>Atrio, atrio chico y meseta final de la pasarela.</summary>
        private static void BuildPlatforms(GenerationContext ctx, GameObject group)
        {
            float bottom = FoundationY(ctx);
            var builder = new MeshBuilder(Count);
            builder.AddSlab(AtriumPlan.Atrium, p => PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(p)), bottom, Ashlar, Floor);
            builder.AddSlab(AtriumPlan.AtrioChico, p => PlanFrame.Y(ctx, AtriumPlan.AtrioChicoLevel(p)), bottom, Ashlar, Floor);
            builder.AddSlab(AtriumPlan.EastPlatform, _ => PlanFrame.Y(ctx, AtriumPlan.EastPlatformLevel), bottom, Ashlar, Granite);
            AddStepsToAtrioChico(ctx, builder, bottom);

            AddToScene(ctx, builder, "Atrio", group);
        }

        /// <summary>
        /// Escalón intermedio corrido entre el atrio y el atrio chico, que queda unos 25 cm más bajo: dos
        /// escalones cortos a lo largo del borde, desde la balaustrada hasta la rampa.
        /// </summary>
        private static void AddStepsToAtrioChico(GenerationContext ctx, MeshBuilder builder, float bottom)
        {
            const float v0 = 20.45f, v1 = 26.5f, tread = 0.4f;
            float Edge(float v) => Mathf.Lerp(17.7f, 17.1f, Mathf.InverseLerp(14.5f, 32.7f, v));
            Vector2[] step =
            {
                new Vector2(Edge(v0), v0), new Vector2(Edge(v0) + tread, v0), new Vector2(Edge(v1) + tread, v1), new Vector2(Edge(v1), v1),
            };
            builder.AddSlab(step, p =>
            {
                float high = AtriumPlan.AtriumLevel(new Vector2(Edge(p.y), p.y));
                float low = AtriumPlan.AtrioChicoLevel(new Vector2(Edge(p.y) + tread, p.y));
                return PlanFrame.Y(ctx, (high + low) * 0.5f);
            }, bottom, Granite, Granite);
        }

        /// <summary>Balaustradas de piedra sobre los bordes del atrio y del pasadizo.</summary>
        private static void BuildBalustrades(GenerationContext ctx, GameObject group)
        {
            var builder = new MeshBuilder(Count);
            foreach (Vector2[] line in AtriumPlan.Balustrades)
            {
                Vector2 mid = (line[0] + line[1]) * 0.5f;
                ArchitecturalElements.AddBalustrade(builder, line[0], line[1], PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(mid)));
            }

            AddToScene(ctx, builder, "Balaustradas", group);
        }
    }
}
