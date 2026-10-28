using System.Collections.Generic;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.AtriumSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Acceso al atrio desde la calle Marqués de la Romana: paso de adoquín de granito en abanico que rodea
    /// el arriate curvo de la esquina desde la meseta final de la pasarela hasta la calle, con su bordillo,
    /// y pretil con pilastras sobre la calle en el borde del atrio chico.
    /// </summary>
    internal static class MarquesAccessBuilder
    {
        private const int ArcSegments = 20;

        /// <summary>Longitud y cota final del tramo recto que entra en la calle.</summary>
        private const float StreetLink = 3.25f;

        public static void Build(GenerationContext ctx, GameObject group)
        {
            var builder = new MeshBuilder(Count);
            AddFan(ctx, builder);
            AddParapet(ctx, builder);
            AtriumBuilder.AddToScene(ctx, builder, "AccesoMarques", group);
        }

        private static void AddFan(GenerationContext ctx, MeshBuilder builder)
        {
            Vector2 c = AtriumPlan.FanCenter;
            float r0 = AtriumPlan.FanInnerRadius;
            float r1 = AtriumPlan.FanOuterRadius;
            float a0 = AtriumPlan.FanStartAngle * Mathf.Deg2Rad;
            float a1 = AtriumPlan.FanEndAngle * Mathf.Deg2Rad;
            float bottom = AtriumBuilder.FoundationY(ctx);
            float Level(Vector2 p)
            {
                Vector2 d = p - c;
                float a = Mathf.Atan2(d.y, d.x);
                if (a < 0f)
                {
                    a += 2f * Mathf.PI;
                }

                return PlanFrame.Y(ctx, Mathf.Lerp(AtriumPlan.EastPlatformLevel, AtriumPlan.MarquesFanEndLevel, Mathf.InverseLerp(a0, a1, a)));
            }

            // Cada tramo del abanico es un trapecio convexo entre los dos arcos.
            for (int i = 0; i < ArcSegments; i++)
            {
                float t0 = Mathf.Lerp(a0, a1, (float)i / ArcSegments);
                float t1 = Mathf.Lerp(a0, a1, (float)(i + 1) / ArcSegments);
                Vector2 d0 = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0));
                Vector2 d1 = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1));
                Vector2[] cell = { c + d0 * r1, c + d1 * r1, c + d1 * r0, c + d0 * r0 };
                builder.AddSlab(cell, Level, bottom, Ashlar, Granite);

                // Bordillo del arriate por el borde interior.
                Vector2 k0 = c + d0 * (r0 - 0.12f);
                Vector2 k1 = c + d1 * (r0 - 0.12f);
                float y = Level(c + (d0 + d1).normalized * r0) + 0.12f;
                ArchitecturalElements.AddBar(builder, new Vector3(k0.x, y - 0.06f, k0.y), new Vector3(k1.x, y - 0.06f, k1.y), 0.24f, Granite);
            }

            // Tramo recto que baja al 12 % hasta la calzada, tangente al final del abanico.
            Vector2 dirEnd = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            Vector2 tangent = new Vector2(-dirEnd.y, dirEnd.x);
            Vector2 inner = c + dirEnd * r0;
            Vector2 outer = c + dirEnd * r1;
            Vector2 innerEnd = inner + tangent * StreetLink;
            Vector2 outerEnd = outer + tangent * StreetLink;
            float startY = PlanFrame.Y(ctx, AtriumPlan.MarquesFanEndLevel);
            float endY = Mathf.Min(startY, PlanFrame.GroundY(ctx, (innerEnd + outerEnd) * 0.5f) + 0.02f);
            Vector2[] link = { outer, outerEnd, innerEnd, inner };
            builder.AddSlab(link, p => Mathf.Lerp(startY, endY, Vector2.Dot(p - inner, tangent) / StreetLink), bottom, Ashlar, Granite);
        }

        /// <summary>
        /// Muro del atrio chico sobre la calle, rematado con balaustrada de piedra como la del resto del atrio,
        /// que sigue por el borde hacia el acceso en abanico. El penúltimo tramo hacia Marqués de la Romana es
        /// un pretil bajo de sillería con un poyete del lado de la calle; el último, una escalerilla de granito
        /// que sube de la calle al atrio chico entre las pilastras.
        /// </summary>
        private static void AddParapet(GenerationContext ctx, MeshBuilder builder)
        {
            Vector2[] line = AtriumPlan.MarquesParapet;
            float foot = AtriumBuilder.FoundationY(ctx);
            for (int i = 0; i < line.Length - 1; i++)
            {
                Vector2 a = line[i];
                Vector2 b = line[i + 1];
                Vector2 mid = (a + b) * 0.5f;
                float level = PlanFrame.Y(ctx, AtriumPlan.AtrioChicoLevel(mid));
                float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                Vector2 dir = (b - a).normalized;
                var street = new Vector2(dir.y, -dir.x);
                if (PolygonUtils.Contains(AtriumPlan.AtrioChico, mid + street * 0.6f))
                {
                    street = -street;
                }

                builder.AddOrientedBox(new Vector3(mid.x, (level + foot) * 0.5f, mid.y), new Vector3(Vector2.Distance(a, b), level - foot, 0.45f), yaw, Ashlar);
                if (i == StairsSection)
                {
                    // Escalerilla entre dos pilastras como las del atrio y, al final, un machón bajo hacia la calle.
                    AddStreetSteps(ctx, builder, a, b, street, level, foot, yaw);
                    AddBallPost(builder, a, level, yaw);
                    AddBallPost(builder, b, level, yaw);
                    Vector2 block = b + street * 0.55f;
                    builder.AddOrientedBox(new Vector3(block.x, (level + 0.25f + foot) * 0.5f, block.y), new Vector3(0.4f, level + 0.25f - foot, 0.75f), yaw, Ashlar);
                    continue;
                }

                if (i == BenchSection)
                {
                    // Pretil bajo de sillería (sin balaustres), respaldo del poyete de la calle.
                    builder.AddOrientedBox(new Vector3(mid.x, level + LowParapet * 0.5f, mid.y), new Vector3(Vector2.Distance(a, b), LowParapet, 0.42f), yaw, Ashlar);
                    AddStreetBench(ctx, builder, a, b, street, foot, yaw);
                    continue;
                }

                ArchitecturalElements.AddBalustrade(builder, a, b, level);
            }
        }

        private const float LowParapet = 0.45f; // Sobre el suelo del atrio chico.

        /// <summary>Pilastra con remate como las de las balaustradas del atrio, a los lados de la escalerilla.</summary>
        private static void AddBallPost(MeshBuilder builder, Vector2 at, float level, float yaw)
        {
            builder.AddOrientedBox(new Vector3(at.x, level + 0.65f, at.y), new Vector3(0.55f, 1.3f, 0.55f), yaw, Ashlar);
            builder.AddOrientedBox(new Vector3(at.x, level + 1.36f, at.y), new Vector3(0.65f, 0.12f, 0.65f), yaw, Ashlar);
            ArchitecturalElements.AddBall(builder, new Vector3(at.x, level + 1.66f, at.y), 0.26f, Ashlar);
        }

        // Tramos del pretil: el del poyete y el de la escalerilla.
        private const int BenchSection = 1;
        private const int StairsSection = 2;

        /// <summary>
        /// Poyete de granito adosado al muro del lado de la calle, en dos tramos a distinta altura que siguen la
        /// cuesta, con el muro como respaldo.
        /// </summary>
        private static void AddStreetBench(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b, Vector2 street, float foot, float yaw)
        {
            const float depth = 0.45f, seat = 0.45f, slab = 0.07f, margin = 0.45f;
            Vector2 dir = (b - a).normalized;
            float length = Vector2.Distance(a, b) - margin * 2f;
            for (int k = 0; k < 2; k++)
            {
                Vector2 c = a + dir * (margin + length * (k + 0.5f) * 0.5f) + street * (0.225f + depth * 0.5f);
                float ground = PlanFrame.GroundY(ctx, c);
                float top = ground + seat;
                builder.AddOrientedBox(new Vector3(c.x, (top - slab + foot) * 0.5f, c.y), new Vector3(length * 0.5f - 0.04f, top - slab - foot, depth - 0.05f), yaw, Ashlar);
                builder.AddOrientedBox(new Vector3(c.x, top - slab * 0.5f, c.y) + new Vector3(street.x, 0f, street.y) * 0.02f,
                    new Vector3(length * 0.5f, slab, depth), yaw, Granite);
            }
        }

        /// <summary>Escalerilla de granito delante del muro: de la calle a la cota del atrio chico, peldaños de unos 16 cm.</summary>
        private static void AddStreetSteps(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b, Vector2 street, float level, float foot, float yaw)
        {
            const float run = 0.32f, inset = 0.3f;
            Vector2 dir = (b - a).normalized;
            float width = Vector2.Distance(a, b) - inset * 2f;
            Vector2 mid = (a + b) * 0.5f;
            Vector2 face = mid + street * 0.225f;
            float ground = PlanFrame.GroundY(ctx, face + street * 1.2f);
            int steps = Mathf.Clamp(Mathf.RoundToInt((level - ground) / 0.16f), 1, 8);
            float rise = (level - ground) / steps;
            for (int k = 1; k < steps; k++)
            {
                // Peldaño k: huella de (k − 1)·run a k·run desde el muro hacia la calle.
                float top = level - k * rise;
                Vector2 c = face + street * (run * (k - 0.5f));
                builder.AddOrientedBox(new Vector3(c.x, (top + foot) * 0.5f, c.y), new Vector3(width, top - foot, run), yaw, Granite);
            }

            // Rellano a la cota del atrio chico sobre el muro, donde falta la balaustrada.
            builder.AddOrientedBox(new Vector3(mid.x, level - 0.04f, mid.y), new Vector3(width + inset * 2f, 0.08f, 0.46f), yaw, Granite);
        }
    }
}
