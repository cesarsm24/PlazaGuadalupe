using System.Collections.Generic;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.Environment.ArchitecturalElements;
using static Guadalupe.Editor.MonasteryExport.AtriumSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Pasarela accesible a lo largo de la fachada y de la torre del reloj: rellano curvo a la cota de las
    /// portadas, rampas y rellanos de tarima de madera tecnológica sobre una estructura de acero galvanizado
    /// apoyada en pilares, y barandillas de acero inoxidable con doble pasamanos a ambos lados. Incluye la
    /// rampa corta entre el atrio y el atrio chico.
    /// </summary>
    internal static class WalkwayBuilder
    {
        /// <summary>Canto de la tarima con sus rastreles y el perfil de borde.</summary>
        private const float DeckDepth = 0.18f;

        /// <summary>Separación de pilares y de montantes de barandilla.</summary>
        private const float PostSpacing = 1.5f;

        /// <summary>Alturas de los pasamanos sobre la tarima.</summary>
        private const float UpperRail = 0.9f;
        private const float LowerRail = 0.66f;

        /// <summary>Construye la pasarela y la rampa corta.</summary>
        public static void Build(GenerationContext ctx, GameObject group)
        {
            var builder = new MeshBuilder(Count);
            AddCurvedLanding(ctx, builder);

            Vector2[] profile = AtriumPlan.WalkwayProfile;
            for (int i = 0; i < profile.Length - 1; i++)
            {
                AddSegment(ctx, builder, profile[i], profile[i + 1], i == 0, i == profile.Length - 2);
            }

            AddSmallRamp(ctx, builder);
            AtriumBuilder.AddToScene(ctx, builder, "Pasarela", group);
        }

        /// <summary>Rellano curvo de arranque, macizo, a la cota de la meseta de las portadas.</summary>
        private static void AddCurvedLanding(GenerationContext ctx, MeshBuilder builder)
        {
            Vector2 c = AtriumPlan.WalkwayCurveCenter;
            float r = AtriumPlan.WalkwayCurveRadius;
            var footprint = new List<Vector2> { c };
            const int segments = 12;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.PI * (1f + 0.5f * i / segments);
                footprint.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }

            float top = PlanFrame.Y(ctx, AtriumPlan.PortalLanding);
            builder.AddSlab(footprint, _ => top, AtriumBuilder.FoundationY(ctx), Ashlar, Wood);
        }

        /// <summary>Tramo de pasarela (rampa o rellano) entre dos puntos del perfil.</summary>
        private static void AddSegment(GenerationContext ctx, MeshBuilder builder, Vector2 start, Vector2 end, bool first, bool last)
        {
            float width = AtriumPlan.WalkwayWidth;
            Vector3 Edge(float u, float level, bool north) =>
                new Vector3(u, PlanFrame.Y(ctx, level), AtriumPlan.WalkwayNorth(u) - (north ? 0f : width));

            Vector3 s0 = Edge(start.x, start.y, false), s1 = Edge(end.x, end.y, false);
            Vector3 n0 = Edge(start.x, start.y, true), n1 = Edge(end.x, end.y, true);
            Vector3 down = Vector3.down * DeckDepth;

            // Tarima, fondo y perfiles laterales de borde.
            builder.AddConvexFace(new[] { s0, s1, n1, n0 }, Vector3.up, Vector3.right, Wood);
            builder.AddConvexFace(new[] { s0 + down, s1 + down, n1 + down, n0 + down }, Vector3.down, Vector3.right, Metal);
            builder.AddConvexFace(new[] { s0, s1, s1 + down, s0 + down }, Vector3.back, Vector3.right, Metal);
            builder.AddConvexFace(new[] { n0, n1, n1 + down, n0 + down }, Vector3.forward, Vector3.right, Metal);
            if (last)
            {
                builder.AddConvexFace(new[] { s1, n1, n1 + down, s1 + down }, Vector3.right, Vector3.forward, Metal);
            }

            // Pilares con su travesaño bajo la tarima y barandillas en los dos bordes.
            float length = end.x - start.x;
            int bays = Mathf.Max(1, Mathf.RoundToInt(length / PostSpacing));
            for (int i = first ? 0 : 1; i <= bays; i++)
            {
                float t = (float)i / bays;
                Vector3 south = Vector3.Lerp(s0, s1, t);
                Vector3 north = Vector3.Lerp(n0, n1, t);
                foreach (Vector3 top in new[] { south, north })
                {
                    float ground = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(new Vector2(top.x, top.z))) - 0.1f;
                    float postTop = top.y - DeckDepth;
                    if (postTop > ground)
                    {
                        builder.AddBox(new Vector3(top.x, (postTop + ground) * 0.5f, top.z), new Vector3(0.1f, postTop - ground, 0.1f), Metal);
                    }

                    builder.AddBox(top + Vector3.up * (UpperRail * 0.5f), new Vector3(0.04f, UpperRail, 0.02f), Metal);
                }

                AddBar(builder, south + down * 1.3f, north + down * 1.3f, 0.08f, Metal);
            }

            foreach (bool north in new[] { false, true })
            {
                Vector3 a = north ? n0 : s0;
                Vector3 b = north ? n1 : s1;
                Vector3 inward = (north ? Vector3.back : Vector3.forward) * 0.07f;
                AddBar(builder, a + inward + Vector3.up * UpperRail, b + inward + Vector3.up * UpperRail, 0.045f, Metal);
                AddBar(builder, a + inward + Vector3.up * LowerRail, b + inward + Vector3.up * LowerRail, 0.045f, Metal);
                AddBar(builder, a + Vector3.up * 0.05f, b + Vector3.up * 0.05f, 0.1f, Metal);
            }
        }

        /// <summary>Rampa corta de tarima entre el atrio y el atrio chico.</summary>
        private static void AddSmallRamp(GenerationContext ctx, MeshBuilder builder)
        {
            Rect r = AtriumPlan.SmallRamp;
            float high = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(new Vector2(r.xMin, r.center.y)));
            float low = PlanFrame.Y(ctx, AtriumPlan.AtrioChicoLevel(new Vector2(r.xMax, r.center.y)));
            Vector2[] footprint = { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
            builder.AddSlab(footprint, p => Mathf.Lerp(high, low, (p.x - r.xMin) / r.width) + 0.02f, low - 0.4f, Granite, Wood);

            // Barandillas a los dos lados, con pasamanos que siguen la pendiente.
            foreach (float v in new[] { r.yMin + 0.05f, r.yMax - 0.05f })
            {
                var a = new Vector3(r.xMin + 0.1f, high, v);
                var b = new Vector3(r.xMax - 0.05f, low, v);
                foreach (float t in new[] { 0f, 0.5f, 1f })
                {
                    Vector3 p = Vector3.Lerp(a, b, t);
                    builder.AddBox(p + Vector3.up * (UpperRail * 0.5f), new Vector3(0.04f, UpperRail, 0.04f), Metal);
                }

                AddBar(builder, a + Vector3.up * UpperRail, b + Vector3.up * UpperRail, 0.045f, Metal);
                AddBar(builder, a + Vector3.up * LowerRail, b + Vector3.up * LowerRail, 0.045f, Metal);
            }
        }
    }
}
