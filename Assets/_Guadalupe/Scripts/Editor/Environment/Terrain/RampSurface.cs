using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Calle Marqués de la Romana: cuesta de pendiente constante a lo largo de su eje real, desde el nivel
    /// de la plaza hasta casi la altura de la lonja junto a su esquina; más allá se funde con el terreno.
    /// </summary>
    internal readonly struct RampSurface
    {
        private const float Blend = 8f;
        private const float FadeStart = 35f;
        private const float FadeEnd = 70f;


        private readonly float _start;
        private readonly float _slope;
        private readonly float _halfWidth;

        private RampSurface(float start, float slope, float width)
        {
            _start = start;
            _slope = slope;
            _halfWidth = width * 0.5f;
        }

        public static RampSurface Create(GenerationContext ctx, PlazaSurface plaza)
        {
            Vector2[] line = PlazaLayout.MarquesRomana;
            float start = plaza.Apply(line[0], plaza.Soften(line[0], ctx.Data.Dem.Sample(line[0].x, line[0].y)));
            // La calle sube hasta la cota del abanico de adoquín por el que se entra al atrio chico.
            Vector2 entry = PlanFrame.ToWorld(AtriumPlan.MarquesEntry);
            Project(entry, out float sEntry, out _);
            float slope = Mathf.Clamp((PlanFrame.Y(ctx, AtriumPlan.MarquesEntryLevel) + ctx.Data.OriginElevation - start) / Mathf.Max(sEntry, 1f), 0f, ctx.Data.Settings.MarquesMaxSlope);
            return new RampSurface(start, slope, ctx.Data.Settings.MarquesWidth);
        }

        /// <summary>Indica si un punto está en la calle de anchura <paramref name="width"/>, ampliada <paramref name="margin"/> metros.</summary>
        public static bool InCorridor(Vector2 p, float width, float margin)
        {
            Project(p, out _, out float d);
            return d <= width * 0.5f + margin;
        }

        public float Apply(Vector2 p, float elevation)
        {
            Project(p, out float s, out float d);
            float half = _halfWidth;
            if (d > half + Blend)
            {
                return elevation;
            }

            float target = _start + _slope * s;
            float across = 1f - Mathf.SmoothStep(0f, 1f, (d - half) / Blend);
            float along = 1f - Mathf.SmoothStep(0f, 1f, (s - FadeStart) / (FadeEnd - FadeStart));
            return Mathf.Lerp(elevation, target, Mathf.Clamp01(across) * Mathf.Clamp01(along));
        }

        /// <summary>Punto del eje de la calle más cercano a <paramref name="p"/>.</summary>
        public static Vector2 AxisPoint(Vector2 p) => PlanarGeometry.ClosestOnPolyline(PlazaLayout.MarquesRomana, p).Point;

        /// <summary>Distancia de un punto al eje de la calle.</summary>
        public static void Distance(Vector2 p, out float distance) => Project(p, out _, out distance);

        /// <summary>Indica si un punto queda a la izquierda del eje al subir la calle (el lado del atrio).</summary>
        public static bool OnAtriumSide(Vector2 p)
        {
            Vector2[] line = PlazaLayout.MarquesRomana;
            int i = PlanarGeometry.ClosestOnPolyline(line, p).Segment;
            Vector2 ab = line[i + 1] - line[i];
            return ab.x * (p.y - line[i].y) - ab.y * (p.x - line[i].x) > 0f;
        }

        /// <summary>Longitud recorrida sobre el eje hasta el punto más cercano y distancia a él.</summary>
        private static void Project(Vector2 p, out float along, out float distance)
        {
            PolylineHit hit = PlanarGeometry.ClosestOnPolyline(PlazaLayout.MarquesRomana, p);
            along = hit.Along;
            distance = hit.Distance;
        }
    }
}
