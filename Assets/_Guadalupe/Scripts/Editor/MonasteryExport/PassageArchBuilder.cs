using System.Collections.Generic;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.AtriumSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Escalera del arco de la portería: arranca en la plaza bajo un arco de medio punto con dovelas, pasa
    /// bajo una bóveda corta coronada por balaustradas y sigue a cielo abierto, pegada al muro de la casa
    /// blanca y de la portería, hasta la explanada de la portería. La separa del atrio un pretil y, arriba,
    /// una balaustrada, ambos con poyete corrido. Junto al arco, en la plaza, añade un banco de granito en el
    /// muro del atrio.
    /// </summary>
    internal static class PassageArchBuilder
    {
        private const int ArchSegments = 16;
        private const int Voussoirs = 13;

        /// <summary>Tabica máxima de la escalera.</summary>
        private const float MaxRise = 0.16f;

        /// <summary>Suelo llano bajo la bóveda antes del primer peldaño.</summary>
        private const float EntryLanding = 2f;

        /// <summary>Grueso de la losa sobre la bóveda.</summary>
        private const float RoofThickness = 0.55f;

        /// <summary>Construye la escalera, la bóveda, el pretil, la barandilla, y los bancos.</summary>
        public static void Build(GenerationContext ctx, GameObject group)
        {
            var builder = new MeshBuilder(Count);
            float bottom = AtriumBuilder.FoundationY(ctx);
            float front = AtriumPlan.VaultFront;
            float back = AtriumPlan.VaultBack;
            float x0 = AtriumPlan.ArchCenter - AtriumPlan.ArchWidth * 0.5f;
            float x1 = AtriumPlan.ArchCenter + AtriumPlan.ArchWidth * 0.5f;

            // Cota de la plaza ante el arco, medida fuera del terreno rebajado bajo la escalera.
            float groundY = PlanFrame.GroundY(ctx, new Vector2(AtriumPlan.ArchCenter, front - 2.5f));
            float topY = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(new Vector2(-12f, AtriumPlan.PassageTop)));
            float roofY = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(new Vector2(AtriumPlan.ArchCenter, front)));
            float StairY(float v) => Mathf.Lerp(groundY, topY, Mathf.InverseLerp(front + EntryLanding, AtriumPlan.PassageTop, v));

            AddStairs(builder, groundY, topY, bottom);

            // Solera de granito ante el arco, a nivel con la plaza y con el suelo bajo la bóveda.
            builder.AddSlab(new[]
            {
                new Vector2(x0 - 0.4f, front - 1.4f), new Vector2(x1 + 0.4f, front - 1.4f), new Vector2(x1 + 0.4f, front), new Vector2(x0 - 0.4f, front),
            }, _ => groundY + 0.01f, groundY - 0.8f, Granite, Granite);

            // Bóveda: machones a los lados del arco, losa encima y rosca de dovelas en el frente.
            float radius = AtriumPlan.ArchWidth * 0.5f;
            float crownY = roofY - RoofThickness;
            float springY = crownY - radius;
            Vector2[] vault = AtriumPlan.Vault;
            builder.AddSlab(new[] { vault[0], new Vector2(x0, front), new Vector2(x0, back), vault[3] }, _ => roofY, bottom, Ashlar, Floor);
            builder.AddSlab(new[] { new Vector2(x1, front), vault[1], vault[2], new Vector2(x1, back) }, _ => roofY, bottom, Ashlar, Floor);
            builder.AddBox(new Vector3(AtriumPlan.ArchCenter, (crownY + roofY) * 0.5f, (front + back) * 0.5f),
                new Vector3(AtriumPlan.ArchWidth, roofY - crownY, back - front), Floor);
            AddArch(builder, AtriumPlan.ArchCenter, springY, radius, roofY, front, back);
            AddVoussoirs(builder, AtriumPlan.ArchCenter, springY, radius, front);

            AddParapetAndBalustrade(ctx, builder);
            AddPoyete(builder, AtriumPlan.PoyeteWall[0], AtriumPlan.PoyeteWall[1], roofY);
            AddHouseHandrail(builder, StairY);
            AddBench(ctx, builder);
            AtriumBuilder.AddToScene(ctx, builder, "EscaleraPorteria", group);
        }

        /// <summary>Peldaños de granito de la plaza a la explanada de la portería; cada uno llega hasta arriba.</summary>
        private static void AddStairs(MeshBuilder builder, float groundY, float topY, float bottom)
        {
            var area = new List<Vector2>(AtriumPlan.PassageStairs);
            float start = AtriumPlan.VaultFront + EntryLanding;
            int steps = Mathf.Max(1, Mathf.CeilToInt((topY - groundY) / MaxRise));
            float run = (AtriumPlan.PassageTop - start) / steps;
            builder.AddSlab(area, _ => groundY, bottom, Ashlar, Granite);
            for (int i = 0; i < steps; i++)
            {
                List<Vector2> tread = PolygonUtils.ClipHalfPlane(area, new Vector2(0f, start + i * run), Vector2.up);
                if (tread.Count >= 3)
                {
                    float top = groundY + (i + 1) * (topY - groundY) / steps;
                    builder.AddSlab(tread, _ => top, bottom, Granite, Granite);
                }
            }
        }

        /// <summary>
        /// Pretil de sillería sobre el atrio a lo largo de la escalera y, donde la escalera llega casi a la altura
        /// del atrio, balaustrada de piedra.
        /// </summary>
        private static void AddParapetAndBalustrade(GenerationContext ctx, MeshBuilder builder)
        {
            Vector2[] line = AtriumPlan.PassageParapet;
            float bottom = AtriumBuilder.FoundationY(ctx);

            // Pretil: de la bóveda hasta el quiebro, 90 cm sobre el atrio.
            Vector2 a = line[0], b = line[1];
            Vector2 along = (b - a).normalized;
            // Hacia el atrio (a la derecha de la línea, que sube hacia el norte).
            Vector2 inward = new Vector2(along.y, -along.x) * 0.18f;
            float top = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(a)) + 0.9f;
            builder.AddSlab(new[] { a, a + inward * 2f, b + inward * 2f, b }, _ => top, bottom, Ashlar, Granite);

            // Albardilla de granito que vuela 5 cm a cada lado y remata el pretil entre las pilastras.
            Vector2 capMid = (a + b) * 0.5f + inward;
            float yaw = -Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;
            builder.AddOrientedBox(new Vector3(capMid.x, top + 0.04f, capMid.y), new Vector3(Vector2.Distance(a, b), 0.08f, 0.46f), yaw, Granite);

            // Balaustrada de piedra como la del resto del atrio, desde el quiebro hasta lo alto de la escalera.
            Vector2 c = line[2];
            float y0 = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(b));
            ArchitecturalElements.AddBalustrade(builder, b + inward, c + inward, y0);

            // Poyete corrido a lo largo del pretil y de la balaustrada, del lado del atrio: el pretil y el zócalo de
            // la balaustrada hacen de respaldo. Se interrumpe en las pilastras.
            Vector2 side = inward.normalized;
            float floor = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel((a + c) * 0.5f));
            AddPoyete(builder, a + side * 0.36f + along * 0.35f, b + side * 0.36f - along * 0.35f, floor);
            Vector2 alongTop = (c - b).normalized;
            AddPoyete(builder, b + side * 0.4f + alongTop * 0.4f, c + side * 0.4f - alongTop * 0.4f, floor);
        }

        /// <summary>
        /// Poyete adosado al muro de la casa sobre la bóveda: base de sillería y asiento de granito a 45 cm,
        /// con el muro como respaldo.
        /// </summary>
        private static void AddPoyete(MeshBuilder builder, Vector2 wallStart, Vector2 wallEnd, float floorY)
        {
            const float depth = 0.45f, seat = 0.45f, slab = 0.07f;
            Vector2 along = (wallEnd - wallStart).normalized;
            Vector2 inward = new Vector2(along.y, -along.x);
            float length = Vector2.Distance(wallStart, wallEnd);
            float yaw = -Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;
            Vector2 mid = (wallStart + wallEnd) * 0.5f;
            Vector2 baseAt = mid + inward * (depth - 0.06f) * 0.5f;
            Vector2 seatAt = mid + inward * depth * 0.5f;
            builder.AddOrientedBox(new Vector3(baseAt.x, floorY + (seat - slab) * 0.5f, baseAt.y), new Vector3(length, seat - slab, depth - 0.06f), yaw, Ashlar);
            builder.AddOrientedBox(new Vector3(seatAt.x, floorY + seat - slab * 0.5f, seatAt.y), new Vector3(length, slab, depth), yaw, Granite);
        }

        /// <summary>Pasamanos de hierro en el muro de la casa, a lo largo de la escalera.</summary>
        private static void AddHouseHandrail(MeshBuilder builder, System.Func<float, float> stairY)
        {
            Vector2[] wall = AtriumPlan.PassageHouseWall;
            for (int i = 0; i < wall.Length - 1; i++)
            {
                Vector2 along = (wall[i + 1] - wall[i]).normalized;
                Vector2 off = new Vector2(along.y, -along.x) * -0.08f;
                Vector2 a = wall[i] + off, b = wall[i + 1] + off;
                ArchitecturalElements.AddBar(builder, new Vector3(a.x, stairY(a.y) + 0.9f, a.y), new Vector3(b.x, stairY(b.y) + 0.9f, b.y), 0.05f, Metal);
                for (float t = 0.1f; t < 1f; t += 0.3f)
                {
                    Vector2 p = Vector2.Lerp(a, b, t);
                    Vector2 w = Vector2.Lerp(wall[i], wall[i + 1], t);
                    ArchitecturalElements.AddBar(builder, new Vector3(p.x, stairY(p.y) + 0.88f, p.y), new Vector3(w.x, stairY(p.y) + 0.88f, w.y), 0.03f, Metal);
                }
            }

            // Bajo la bóveda, pasamanos en los dos machones.
            float x0 = AtriumPlan.ArchCenter - AtriumPlan.ArchWidth * 0.5f + 0.06f;
            float x1 = AtriumPlan.ArchCenter + AtriumPlan.ArchWidth * 0.5f - 0.06f;
            foreach (float x in new[] { x0, x1 })
            {
                float va = AtriumPlan.VaultFront + EntryLanding, vb = AtriumPlan.VaultBack;
                ArchitecturalElements.AddBar(builder, new Vector3(x, stairY(va) + 0.9f, va), new Vector3(x, stairY(vb) + 0.9f, vb), 0.05f, Metal);
            }
        }

        /// <summary>Banco de granito de dos patas, paralelo a <paramref name="along"/>.</summary>
        private static void AddBench(MeshBuilder builder, Vector2 at, float floorY, Vector2 along)
        {
            along.Normalize();
            float yaw = -Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;
            const float length = 1.8f;
            builder.AddOrientedBox(new Vector3(at.x, floorY + 0.42f, at.y), new Vector3(length, 0.1f, 0.48f), yaw, Granite);
            foreach (float s in new[] { -0.36f, 0.36f })
            {
                Vector2 leg = at + along * (s * length);
                builder.AddOrientedBox(new Vector3(leg.x, floorY + 0.19f, leg.y), new Vector3(0.18f, 0.38f, 0.4f), yaw, Granite);
            }
        }

        /// <summary>Bóveda de medio punto y enjutas del frente y del fondo hasta lo alto de la losa.</summary>
        private static void AddArch(MeshBuilder builder, float centerX, float springY, float radius, float topY, float front, float back)
        {
            var arc = new List<Vector2>(ArchSegments + 1);
            for (int i = 0; i <= ArchSegments; i++)
            {
                float t = Mathf.PI * (1f - (float)i / ArchSegments);
                arc.Add(new Vector2(centerX + radius * Mathf.Cos(t), springY + radius * Mathf.Sin(t)));
            }

            for (int i = 0; i < ArchSegments; i++)
            {
                Vector2 p = arc[i];
                Vector2 q = arc[i + 1];
                foreach ((float z, Vector3 facing) in new[] { (front, Vector3.back), (back, Vector3.forward) })
                {
                    builder.AddConvexFace(new[]
                    {
                        new Vector3(p.x, p.y, z), new Vector3(q.x, q.y, z), new Vector3(q.x, topY, z), new Vector3(p.x, topY, z),
                    }, facing, Vector3.right, Ashlar);
                }

                Vector2 mid = (p + q) * 0.5f;
                builder.AddConvexFace(new[]
                {
                    new Vector3(p.x, p.y, front), new Vector3(q.x, q.y, front), new Vector3(q.x, q.y, back), new Vector3(p.x, p.y, back),
                }, new Vector3(centerX - mid.x, springY - mid.y, 0f), Vector3.forward, Ashlar);
            }
        }

        /// <summary>Rosca de dovelas de granito resaltada en el frente del arco.</summary>
        private static void AddVoussoirs(MeshBuilder builder, float centerX, float springY, float radius, float front)
        {
            const float thickness = 0.42f;
            for (int i = 0; i < Voussoirs; i++)
            {
                float t0 = Mathf.PI * i / Voussoirs;
                float t1 = Mathf.PI * (i + 1) / Voussoirs;
                Vector2 dir0 = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0));
                Vector2 dir1 = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1));
                Vector2 c = new Vector2(centerX, springY);
                Vector2 in0 = c + dir0 * radius, in1 = c + dir1 * radius;
                Vector2 out0 = c + dir0 * (radius + thickness), out1 = c + dir1 * (radius + thickness);
                float z = front - 0.1f;
                builder.AddConvexFace(new[]
                {
                    new Vector3(in0.x, in0.y, z), new Vector3(out0.x, out0.y, z), new Vector3(out1.x, out1.y, z), new Vector3(in1.x, in1.y, z),
                }, Vector3.back, Vector3.right, Granite);
                foreach ((Vector2 a, Vector2 b) in new[] { (in0, out0), (in1, out1), (out0, out1), (in0, in1) })
                {
                    Vector2 m = (a + b) * 0.5f - c;
                    builder.AddConvexFace(new[]
                    {
                        new Vector3(a.x, a.y, z), new Vector3(b.x, b.y, z), new Vector3(b.x, b.y, front), new Vector3(a.x, a.y, front),
                    }, new Vector3(m.x, m.y, 0f), Vector3.forward, Granite);
                }
            }
        }

        /// <summary>Banco de granito adosado al muro del atrio, a la derecha del arco.</summary>
        private static void AddBench(GenerationContext ctx, MeshBuilder builder)
        {
            Vector2[] wall = AtriumPlan.Balustrades[2];
            Vector2 along = (wall[1] - wall[0]).normalized;
            Vector2 outward = new Vector2(along.y, -along.x);
            Vector2 OnWall(float u) => wall[0] + along * ((u - wall[0].x) / along.x);

            Vector2 bench = OnWall((AtriumPlan.Bench.x + AtriumPlan.Bench.y) * 0.5f) + outward * 0.3f;
            AddBench(builder, bench, PlanFrame.GroundY(ctx, bench), along);
        }
    }
}
