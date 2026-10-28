using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Pinta el terreno con cuatro capas: tierra, empedrado de calles, losas de granito y una variante de las losas
    /// que rompe la repetición de la textura.
    /// </summary>
    internal static class TerrainPainter
    {
        internal const int AlphamapResolution = 512;

        private const float UrbanRadius = 160f;

        /// <summary>Lo que se pavimenta más allá del fondo de los soportales, junto al borde de la plaza.</summary>
        private const float PavedBeyondArcades = 1.4f;

        internal static void Paint(GenerationContext ctx, TerrainData data)
        {
            var alpha = new float[AlphamapResolution, AlphamapResolution, 4];
            float size = ctx.Data.Dem.SizeMeters;
            float half = size * 0.5f;
            List<Vector2> outline = ctx.Data.Osm.PlazaOutline;

            for (int iz = 0; iz < AlphamapResolution; iz++)
            {
                for (int ix = 0; ix < AlphamapResolution; ix++)
                {
                    var p = new Vector2(-half + (ix + 0.5f) * size / AlphamapResolution, -half + (iz + 0.5f) * size / AlphamapResolution);
                    if (IsPaved(ctx, outline, p))
                    {
                        // Enlosado con zonas del mismo enlosado a otra escala y alguna mancha de tierra y desgaste.
                        float alt = Mathf.Clamp01((Mathf.PerlinNoise(p.x * 0.06f + 3.1f, p.y * 0.06f + 7.7f) - 0.35f) * 1.6f) * 0.6f;
                        float stain = Mathf.Clamp01((Mathf.PerlinNoise(p.x * 0.18f + 11f, p.y * 0.18f + 5f) - 0.62f) * 2.5f) * 0.22f;
                        alpha[iz, ix, 3] = alt;
                        alpha[iz, ix, 0] = stain;
                        alpha[iz, ix, 2] = 1f - alt - stain;
                    }
                    else
                    {
                        alpha[iz, ix, p.magnitude < UrbanRadius ? 1 : 0] = 1f;
                    }
                }
            }

            data.SetAlphamaps(0, 0, alpha);
        }

        private static bool IsPaved(GenerationContext ctx, List<Vector2> outline, Vector2 p)
        {
            // La calle de la rampa es empedrado, no losa de plaza.
            if (RampSurface.InCorridor(p, ctx.Data.Settings.MarquesWidth, 0f))
            {
                return false;
            }

            if (AtriumPlan.FloorLevelAt(PlanFrame.ToPlan(p)).HasValue || ctx.Data.InPlaza(p))
            {
                return true;
            }

            // Se descarta rápido lo lejano antes de calcular la distancia al contorno.
            return outline.Count > 0 && p.sqrMagnitude < 90f * 90f && PolygonUtils.DistanceToEdges(outline, p) < ctx.Data.Settings.ArcadeCorridorDepth + PavedBeyondArcades;
        }
    }
}
