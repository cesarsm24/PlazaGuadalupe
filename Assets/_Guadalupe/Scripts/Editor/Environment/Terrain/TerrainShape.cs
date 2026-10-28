using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Forma del suelo en cada punto: la plaza ajustada a un plano, la explanada al pie de la escalinata, la calle
    /// Marqués de la Romana junto al atrio y el terreno rebajado bajo las losas del atrio y sus escaleras.
    /// </summary>
    internal static class TerrainShape
    {
        /// <summary>V donde arranca el tramo inferior de la escalinata (su peldaño más bajo).</summary>
        /// <summary>
        /// Elevación del pie de la escalinata: el punto más bajo de su frente, que fija la relación entre las cotas
        /// de nivel del atrio y las alturas del mundo.
        /// </summary>
        internal static float StairsFootElevation(DemGrid dem, PlazaSurface plaza)
        {
            float stairsFoot = float.MaxValue;
            foreach (float u in new[] { -7f, 1.4f, 10f })
            {
                Vector2 foot = PlanFrame.ToWorld(u, AtriumPlan.StairsFootV);
                stairsFoot = Mathf.Min(stairsFoot, plaza.Apply(foot, plaza.Soften(foot, dem.Sample(foot.x, foot.y))));
            }

            return stairsFoot;
        }

        /// <summary>
        /// Elevación del suelo en cada punto: relieve suavizado con la plaza en su plano, explanada al pie de la
        /// escalinata, calle Marqués de la Romana en cuesta, cuesta junto a los escalones de la esquina y terreno
        /// rebajado bajo el atrio.
        /// </summary>
        internal static System.Func<Vector2, float> GroundShape(GenerationContext ctx, PlazaSurface plaza, float stairsFoot)
        {
            DemGrid dem = ctx.Data.Dem;
            RampSurface ramp = RampSurface.Create(ctx, plaza);
            float Street(Vector2 p)
            {
                float e = plaza.Apply(p, plaza.Soften(p, dem.Sample(p.x, p.y)));
                e = AroundStairs(p, e, stairsFoot);
                e = ramp.Apply(p, e);
                return MarquesBesideAtrium(ctx, p, e);
            }

            float[] risers = AtriumPlan.CornerRisers;
            float cornerLow = Street(PlanFrame.ToWorld(new Vector2(risers[0] - 0.8f, 11.4f)));
            float cornerHigh = Street(PlanFrame.ToWorld(new Vector2(risers[risers.Length - 1] + 0.8f, 14.2f)));
            return p => UnderAtrium(ctx, p, BesideCornerSteps(p, Street(p), cornerLow, cornerHigh));
        }

        /// <summary>
        /// Explanada a nivel del pie de la escalinata delante de su tramo inferior, que se funde con la pendiente
        /// natural de la plaza a lo largo de 14 m: todos los peldaños miden lo mismo y no queda escalón.
        /// </summary>
        private static float AroundStairs(Vector2 p, float elevation, float stairsFoot)
        {
            const float apronDepth = 2.5f;
            const float blend = 14f;
            Vector2 q = PlanFrame.ToPlan(p);
            Rect landing = AtriumPlan.Landing;
            float grow = AtriumPlan.LowerSteps * AtriumPlan.LowerRun;
            float du = Mathf.Max(landing.xMin - grow - q.x, q.x - (landing.xMax + grow), 0f);
            float dv = Mathf.Max(AtriumPlan.StairsFootV - apronDepth - q.y, q.y - AtriumPlan.StairsTop, 0f);
            float d = Mathf.Sqrt(du * du + dv * dv);
            return d >= blend ? elevation : Mathf.Lerp(stairsFoot, elevation, Mathf.SmoothStep(0f, 1f, d / blend));
        }

        /// <summary>
        /// Tramo de la calle Marqués de la Romana junto al atrio chico: sigue sus curvas de nivel
        /// (−3,9 bajo el pretil, −3,0 donde entra el acceso en abanico y −2,2 más arriba), ajustadas a un plano
        /// inclinado. Todo el lado del atrio y del monasterio queda en ese mismo plano, sin vaguada junto al muro;
        /// del lado de las casas se funde con el terreno.
        /// </summary>
        private static float MarquesBesideAtrium(GenerationContext ctx, Vector2 p, float elevation)
        {
            const float blend = 6f;
            Vector2 q = PlanFrame.ToPlan(p);
            Rect stretch = AtriumPlan.MarquesBesideAtrium;
            float edge = Mathf.Min(q.x - stretch.xMin, stretch.xMax - q.x, q.y - stretch.yMin, stretch.yMax - q.y);
            if (edge <= 0f)
            {
                return elevation;
            }

            // La calle y el arriate que rodea el acceso en abanico, que están a su misma cota.
            RampSurface.Distance(p, out float distance);
            float half = ctx.Data.Settings.MarquesWidth * 0.5f;
            float street = 1f - Mathf.SmoothStep(0f, 1f, (distance - half - 4f) / blend);
            float garden = 1f - Mathf.SmoothStep(0f, 1f, (Vector2.Distance(q, AtriumPlan.FanCenter) - AtriumPlan.FanInnerRadius) / 3f);
            float atriumSide = RampSurface.OnAtriumSide(p) ? 1f : 0f;
            float weight = Mathf.Max(Mathf.Max(street, garden), atriumSide) * Mathf.SmoothStep(0f, 1f, edge / 4f);
            if (weight <= 0f)
            {
                return elevation;
            }

            // La cota se toma en el eje de la calle: así la calzada queda a nivel de lado a lado, de las casas al
            // atrio, y solo sube a lo largo.
            Vector2 axis = PlanFrame.ToPlan(RampSurface.AxisPoint(p));
            float level = AtriumPlan.MarquesStreetLevel(axis);
            level = Mathf.Lerp(level, AroundFan(q, out float fanWeight), fanWeight);
            float target = PlanFrame.Y(ctx, level) + ctx.Data.OriginElevation;
            return Mathf.Lerp(elevation, target, Mathf.Clamp01(weight));
        }

        /// <summary>
        /// Cota del terreno junto al acceso en abanico: unos centímetros por debajo de sus losas y, pasado su
        /// bordillo, 10 cm por encima (el arriate); a 3 m del abanico deja de influir.
        /// </summary>
        private static float AroundFan(Vector2 q, out float weight)
        {
            const float blend = 3f;
            Vector2 d = q - AtriumPlan.FanCenter;
            float r = d.magnitude;
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            a = a < 0f ? a + 360f : a;
            float a0 = AtriumPlan.FanStartAngle, a1 = AtriumPlan.FanEndAngle;
            float overflow = Mathf.Max(a0 - a, a - a1, 0f) * Mathf.Deg2Rad * r;
            float radial = Mathf.Max(r - AtriumPlan.FanOuterRadius, AtriumPlan.FanInnerRadius - r, 0f);
            weight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(overflow * overflow + radial * radial) / blend);
            float fan = Mathf.Lerp(AtriumPlan.EastPlatformLevel, AtriumPlan.MarquesFanEndLevel, Mathf.InverseLerp(a0, a1, a));
            return r < AtriumPlan.FanInnerRadius - 0.8f ? fan + 0.1f : fan - 0.08f;
        }

        /// <summary>
        /// Calle junto a los escalones de la esquina hacia Marqués de la Romana: cuesta uniforme que corta cada
        /// peldaño por su mitad (de la cota de la plaza al primero a la de la calle tras el último), de modo que el
        /// costado de los peldaños apenas asoma; a 5 m del bordillo se funde con el terreno.
        /// </summary>
        private static float BesideCornerSteps(Vector2 p, float elevation, float low, float high)
        {
            const float band = 1.5f, blend = 5f;
            Vector2 q = PlanFrame.ToPlan(p);
            float[] r = AtriumPlan.CornerRisers;
            Vector2 k0 = AtriumPlan.CornerKerb[0], k1 = AtriumPlan.CornerKerb[1];
            float kerbV = Mathf.Lerp(k0.y, k1.y, Mathf.InverseLerp(k0.x, k1.x, q.x));
            float du = Mathf.Max(r[0] - band - q.x, q.x - (r[r.Length - 1] + band), 0f);
            float dv = Mathf.Max(kerbV - band - q.y, q.y - AtriumPlan.CornerWallV(q.x) - band, 0f);
            float d = Mathf.Sqrt(du * du + dv * dv);
            if (d >= blend)
            {
                return elevation;
            }

            float rise = (high - low) / r.Length;
            float run = (r[r.Length - 1] - r[0]) / (r.Length - 1);
            float target = Mathf.Clamp(low + rise * (0.5f + (q.x - r[0]) / run), Mathf.Min(low, high), Mathf.Max(low, high));
            return Mathf.Lerp(target, elevation, Mathf.SmoothStep(0f, 1f, d / blend));
        }

        /// <summary>
        /// Rebaja el terreno que queda bajo el atrio y sus escaleras para que no asome por sus losas: dentro de
        /// cada recinto queda 30 cm por debajo de su cota y, en la franja de su borde, 5 cm.
        /// </summary>
        private static float UnderAtrium(GenerationContext ctx, Vector2 p, float elevation)
        {
            Vector2 q = PlanFrame.ToPlan(p);
            Rect extent = AtriumPlan.AtriumExtent;
            if (q.x < extent.xMin || q.x > extent.xMax || q.y < extent.yMin || q.y > extent.yMax)
            {
                return elevation;
            }

            float? level = AtriumPlan.FloorLevelAt(q);
            if (!level.HasValue)
            {
                return elevation;
            }

            // Junto al borde de cada recinto el terreno solo se enrasa por debajo de la losa: si se rebajara del
            // todo, la interpolación del terreno abriría una zanja por fuera del borde.
            const float edge = 0.9f;
            bool inner = AtriumPlan.FloorLevelAt(q + new Vector2(edge, 0f)).HasValue && AtriumPlan.FloorLevelAt(q - new Vector2(edge, 0f)).HasValue &&
                         AtriumPlan.FloorLevelAt(q + new Vector2(0f, edge)).HasValue && AtriumPlan.FloorLevelAt(q - new Vector2(0f, edge)).HasValue;
            float floor = PlanFrame.Y(ctx, level.Value) + ctx.Data.OriginElevation;
            return Mathf.Min(elevation, floor - (inner ? 0.3f : 0.05f));
        }
    }
}
