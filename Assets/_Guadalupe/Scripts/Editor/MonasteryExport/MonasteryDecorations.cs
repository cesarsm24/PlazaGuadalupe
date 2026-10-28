using System.Collections.Generic;
using System.Linq;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Adornos de Navidad del modelo del monasterio: columnas de luz en los contrafuertes, copos y luz en las
    /// torres, tapiz bajo el balcón, silueta de la Virgen, cortinas del atrio y de los balcones del cuerpo.
    /// </summary>
    internal static class MonasteryDecorations
    {
        /// <summary>Grosor del tubo luminoso de la silueta de la Virgen.</summary>
        private const float OutlineTube = 0.07f;

        private static readonly Color WindowGlow = new Color(1f, 0.74f, 0.4f) * 1.3f;

        private static readonly Color SnowflakeWhite = new Color(0.85f, 0.9f, 1f) * 2.2f;

        /// <summary>
        /// Adornos del monasterio (fachada, torres, atrio y balcones del cuerpo) para su modelo. Devuelve el grupo
        /// y, por cada luz, su color pleno.
        /// </summary>
        public static GameObject Build(GenerationContext ctx, Dictionary<Renderer, Color> bulbs)
        {
            GameObject decorations = ctx.Output.CreateGroup(MonasteryPrefabNodes.DecorationsName);
            void Register(Renderer r, Color c) => bulbs[r] = c;

            BuildFacade(ctx, decorations, ctx.Data.Materials.LightsNet, ctx.Data.Materials.LightsSolid, Register);
            BuildAtrium(ctx, decorations, ctx.Data.Materials.LightsCurtain, Register);
            ChristmasBuilder.BuildBalconies(ctx, ctx.Results.MonasteryBalconies, decorations, ctx.Data.Materials.LightsCurtain, "SM_CortinasBalconesMonasterio", Register);
            return decorations;
        }

        /// <summary>
        /// Fachada y torres: columnas de luz en los cinco contrafuertes (frente y costados), copos en las dos
        /// ventanas de la torre de la portería y luz cálida en los vanos de su campanario; en la torre del reloj,
        /// tiras en los balcones, luz en las tres ventanas altas y el tapiz rojo bajo el balcón inferior.
        /// </summary>
        private static void BuildFacade(GenerationContext ctx, GameObject parent, Material net, Material solid, System.Action<Renderer, Color> register)
        {
            float portalLanding = PlanFrame.Y(ctx, AtriumPlan.PortalLanding);
            GameObject front = PlanFrame.CreateFrame(parent, "FrenteNavidad");
            GameObject facadeFrame = PlanFrame.CreateChildFrame(front, "FachadaNavidad", AtriumPlan.FacadeOrigin, AtriumPlan.FacadeYaw);
            AddButtressColumns(ctx, facadeFrame, portalLanding, net, register);
            AddWestTowerLights(ctx, facadeFrame, portalLanding, solid, register);
            AddClockTowerLights(ctx, front, portalLanding, net, solid, register);
        }

        /// <summary>Columnas de luz en el frente y los costados de los cinco contrafuertes de la fachada gótica.</summary>
        private static void AddButtressColumns(GenerationContext ctx, GameObject facadeFrame, float portalLanding, Material net, System.Action<Renderer, Color> register)
        {
            float H(float h) => portalLanding + h;
            var columns = new MeshBuilder(1);
            for (int i = 0; i < MonasteryBuilder.Buttresses.Length; i++)
            {
                float x = MonasteryBuilder.Buttresses[i];
                float top = H(MonasteryBuilder.ButtressTop(i));
                float bottom = H(-0.2f);
                Quad(columns, new Vector3(x - 0.5f, bottom, -0.06f), new Vector3(x + 0.5f, bottom, -0.06f), top, Vector3.back);
                foreach (float side in new[] { -0.5f, 0.5f })
                {
                    Quad(columns, new Vector3(x + side, bottom, -0.06f), new Vector3(x + side, bottom, 0.9f), top, side < 0f ? Vector3.left : Vector3.right);
                }
            }

            register(ChristmasBuilder.AddRenderer(ctx, facadeFrame, "ColumnasDeLuz", "LightColumns", columns, net), ChristmasBuilder.WarmGold);
        }

        /// <summary>
        /// Torre de la portería (oeste): copos en sus dos ventanas, silueta luminosa de la Virgen bajo la ventana
        /// inferior y vanos del campanario encendidos.
        /// </summary>
        private static void AddWestTowerLights(GenerationContext ctx, GameObject facadeFrame, float portalLanding, Material solid, System.Action<Renderer, Color> register)
        {
            float H(float h) => portalLanding + h;
            Vector3 tower = AtriumPlan.WestTower;
            float cx = (tower.x + tower.y) * 0.5f;
            var snow = new MeshBuilder(1);
            foreach (float h in new[] { 13.4f, 16.3f })
            {
                AddSnowflake(snow, new Vector3(cx - 0.1f, H(h + 1.1f), 0.3f), 0.42f);
            }

            var belfry = new MeshBuilder(1);
            foreach ((float offset, float w, float h, float sill) in MonasteryBuilder.WestBelfryFront)
            {
                belfry.AddBox(new Vector3(cx + offset, H(MonasteryBuilder.WestBelfryBottom + sill) + h * 0.4f, 0.1f), new Vector3(w * 0.9f, h * 0.8f, 0.02f), 0);
            }

            var virgin = new MeshBuilder(1);
            AddVirginOutline(virgin, new Vector3(cx - 0.1f, H(9.6f), 0.2f), 3.4f);
            register(ChristmasBuilder.AddRenderer(ctx, facadeFrame, "CoposTorreOeste", "WestTowerSnowflakes", snow, solid), SnowflakeWhite);
            register(ChristmasBuilder.AddRenderer(ctx, facadeFrame, "VirgenIluminada", "VirginOutline", virgin, solid), ChristmasBuilder.WarmGold);
            register(ChristmasBuilder.AddRenderer(ctx, facadeFrame, "CampanarioTorreOeste", "WestBelfryGlow", belfry, solid), WindowGlow);
        }

        /// <summary>
        /// Torre del reloj (este), en el marco (u, v): tiras en los balcones y bajo las ventanas del remate,
        /// ventanas iluminadas, un copo y el tapiz rojo con cenefa dorada colgado del balcón inferior.
        /// </summary>
        private static void AddClockTowerLights(GenerationContext ctx, GameObject front, float portalLanding, Material net, Material solid,
            System.Action<Renderer, Color> register)
        {
            float H(float h) => portalLanding + h;
            Vector3 t = AtriumPlan.EastTower;
            float v0 = t.z;
            const float wu = 20.65f;
            var strips = new MeshBuilder(1);
            foreach (float slab in new[] { 11.5f, 16.6f })
            {
                Quad(strips, new Vector3(wu - 2f, H(slab + 0.35f), v0 - 0.74f), new Vector3(wu + 2f, H(slab + 0.35f), v0 - 0.74f), H(slab + 1.05f), Vector3.back);
            }

            Quad(strips, new Vector3(t.x - 0.5f, H(26.2f), v0 - 0.72f), new Vector3(t.y + 0.5f, H(26.2f), v0 - 0.72f), H(26.75f), Vector3.back);

            var windows = new MeshBuilder(1);
            foreach (float u in new[] { 18.8f, 20.7f, 22.9f })
            {
                windows.AddBox(new Vector3(u, H(27.6f), v0 - 0.9f), new Vector3(0.65f, 1.5f, 0.02f), 0);
            }

            var snow = new MeshBuilder(1);
            AddSnowflake(snow, new Vector3(wu, H(17.2f), v0 - 0.8f), 0.38f);
            register(ChristmasBuilder.AddRenderer(ctx, front, "TirasTorreReloj", "ClockTowerStrips", strips, net), ChristmasBuilder.WarmGold);
            register(ChristmasBuilder.AddRenderer(ctx, front, "VentanasTorreReloj", "ClockTowerWindows", windows, solid), WindowGlow);
            register(ChristmasBuilder.AddRenderer(ctx, front, "CopoTorreReloj", "ClockTowerSnowflake", snow, solid), SnowflakeWhite);

            var banner = new MeshBuilder(2);
            banner.AddBox(new Vector3(wu, H(10.3f), v0 - 0.74f), new Vector3(1.5f, 2.3f, 0.02f), 0);
            banner.AddBox(new Vector3(wu, H(10.3f), v0 - 0.76f), new Vector3(1.2f, 1.95f, 0.02f), 1);
            banner.AddBox(new Vector3(wu, H(10.3f), v0 - 0.77f), new Vector3(0.5f, 0.9f, 0.02f), 0);
            ctx.Output.AddStatic("TapizNavidad", banner, "SM_TapizNavidad", new[] { ctx.Data.Materials.Gold, ctx.Data.Materials.AwningRed }, front, decorative: true);
        }

        /// <summary>Cortinas de luz que cubren las balaustradas del atrio a ambos lados de la escalinata.</summary>
        private static void BuildAtrium(GenerationContext ctx, GameObject parent, Material curtain, System.Action<Renderer, Color> register)
        {
            GameObject frame = PlanFrame.CreateFrame(parent, "AtrioNavidad");
            var curtains = new ChristmasBuilder.CurtainMesh();
            foreach (int index in new[] { 2, 3 })
            {
                Vector2 a = AtriumPlan.Balustrades[index][0];
                Vector2 b = AtriumPlan.Balustrades[index][1];
                Vector2 along = (b - a).normalized;
                Vector2 outward = new Vector2(along.y, -along.x) * 0.28f;
                float top = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel((a + b) * 0.5f)) + 1.08f;
                curtains.Add(ToWorld(a + outward, 0f), ToWorld(b + outward, 0f), top, top - 2.1f);
            }

            register(ChristmasBuilder.AddRenderer(ctx, frame, "CortinasAtrio", curtains.Build("SM_CortinasAtrio"), curtain), ChristmasBuilder.WarmGold);
        }

        /// <summary>Rectángulo vertical entre dos puntos de su base y una altura superior, mirando hacia <paramref name="facing"/>.</summary>
        private static void Quad(MeshBuilder builder, Vector3 a, Vector3 b, float topY, Vector3 facing)
        {
            builder.AddConvexFace(new[] { a, b, new Vector3(b.x, topY, b.z), new Vector3(a.x, topY, a.z) }, facing, b - a, 0);
        }

        /// <summary>Copo de nieve plano: tres barras cruzadas con ramitas en los extremos, en el plano XY.</summary>
        private static void AddSnowflake(MeshBuilder builder, Vector3 center, float radius)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI / 3f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                ArchitecturalElements.AddBar(builder, center - d * radius, center + d * radius, 0.05f, 0);
                foreach (float sign in new[] { -1f, 1f })
                {
                    Vector3 tip = center + d * radius * 0.65f * sign;
                    foreach (float twist in new[] { -0.6f, 0.6f })
                    {
                        Vector3 branch = Quaternion.AngleAxis(twist * Mathf.Rad2Deg, Vector3.forward) * d * sign;
                        ArchitecturalElements.AddBar(builder, tip, tip + branch * radius * 0.3f, 0.035f, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Silueta de la Virgen de Guadalupe en tubos de luz, en el plano XY mirando a -Z: manto acampanado con
        /// el borde inferior curvo, cabeza, corona de tres puntas, ráfaga de rayos cortos y una línea central que
        /// marca la abertura del manto. <paramref name="bottom"/> es el centro del borde inferior.
        /// </summary>
        private static void AddVirginOutline(MeshBuilder builder, Vector3 bottom, float height)
        {
            AddMantleOutline(builder, bottom, height);
            AddHeadOutline(builder, bottom + new Vector3(0f, height * 0.8f, 0f), height * 0.075f);
        }

        private static void OutlineBar(MeshBuilder builder, Vector3 a, Vector3 b) =>
            ArchitecturalElements.AddBar(builder, a, b, OutlineTube, 0);

        /// <summary>Manto: costados abiertos de los hombros a los pies, borde inferior en arco y abertura central.</summary>
        private static void AddMantleOutline(MeshBuilder builder, Vector3 bottom, float height)
        {
            float w = height * 0.62f;
            float shoulderY = height * 0.7f;
            Vector3 P(float x, float y) => bottom + new Vector3(x, y, 0f);
            void Line(Vector3 a, Vector3 b) => OutlineBar(builder, a, b);

            var left = new List<Vector3>();
            var right = new List<Vector3>();
            const int steps = 8;
            for (int i = 0; i <= steps; i++)
            {
                float s = (float)i / steps;
                float half = Mathf.Lerp(height * 0.12f, w * 0.5f, Mathf.Pow(s, 0.8f));
                float y = Mathf.Lerp(shoulderY, 0.08f * height, s);
                left.Add(P(-half, y));
                right.Add(P(half, y));
            }

            for (int i = 0; i < steps; i++)
            {
                Line(left[i], left[i + 1]);
                Line(right[i], right[i + 1]);
            }

            // Borde inferior en arco y abertura central del manto.
            Vector3 prev = left[steps];
            for (int i = 1; i <= 8; i++)
            {
                float s = (float)i / 8;
                float x = Mathf.Lerp(-w * 0.5f, w * 0.5f, s);
                Vector3 q = P(x, 0.08f * height - Mathf.Sin(s * Mathf.PI) * 0.06f * height);
                Line(prev, q);
                prev = q;
            }

            Line(P(0f, shoulderY - 0.1f * height), P(0f, 0.02f * height));
            Line(left[0], right[0]);
        }

        /// <summary>Cabeza, corona y ráfaga de la silueta.</summary>
        private static void AddHeadOutline(MeshBuilder builder, Vector3 head, float headR)
        {
            void Line(Vector3 a, Vector3 b) => OutlineBar(builder, a, b);
            const int ring = 12;
            for (int i = 0; i < ring; i++)
            {
                float a0 = i * 2f * Mathf.PI / ring;
                float a1 = (i + 1) * 2f * Mathf.PI / ring;
                Line(head + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * headR, head + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * headR);
            }

            foreach (float x in new[] { -0.6f, 0f, 0.6f })
            {
                Line(head + new Vector3(x * headR, headR, 0f), head + new Vector3(x * headR * 0.6f, headR * (x == 0f ? 2.4f : 2f), 0f));
            }

            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.PI * (0.05f + 0.9f * i / 10f);
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Line(head + d * headR * 1.9f, head + d * headR * (i % 2 == 0 ? 3f : 2.5f));
            }
        }
    }
}
