using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Análisis de soportales y pasos de calle: qué parte de la planta baja de cada casa queda abierta, a qué
    /// altura va el techo y consultas sobre los lados de fachada (si dan al pasillo, al frente, etc.).
    /// </summary>
    internal static class ArcadeAnalysis
    {
        /// <summary>Fracción de planta baja que debe quedar para no considerar la parte una franja de soportal.</summary>
        private const float StripAreaRatio = 0.35f;

        /// <summary>
        /// Recorta la planta baja de cada parte con las franjas de soportal y calcula la altura del techo y
        /// las plantas que vuelan sobre él.
        /// </summary>
        internal static Dictionary<BuildingFootprint, ArcadePart> Analyse(GenerationContext ctx, List<BuildingFootprint> parts)
        {
            var result = new Dictionary<BuildingFootprint, ArcadePart>();
            foreach (BuildingFootprint building in parts)
            {
                ArcadePart part = ArcadeStrip(ctx, building, parts);
                part = OpenPassages(ctx, building, part);
                if (part != null)
                {
                    result[building] = part;
                }
            }

            return result;
        }

        /// <summary>
        /// Recorta la planta baja de una parte con la franja del primer soportal que la atraviesa. El techo del
        /// soportal va a la altura libre sobre el punto más alto de la franja de esta casa.
        /// </summary>
        /// <returns>La parte afectada, o null si ningún soportal la cruza.</returns>
        private static ArcadePart ArcadeStrip(GenerationContext ctx, BuildingFootprint building, List<BuildingFootprint> parts)
        {
            List<Vector2> footprint = building.Footprint;
            float depth = ctx.Data.Settings.ArcadeCorridorDepth;
            foreach (ArcadeSegment segment in PlazaLayout.Arcades)
            {
                bool InBand(Vector2 p) => ArcadeBands.InBand(segment.Line, p, depth);
                List<Vector2> ground = ArcadeBands.ClipOutside(footprint, InBand, out bool cut);
                if (!cut)
                {
                    continue;
                }

                float groundArea = ground == null ? 0f : Mathf.Abs(PolygonUtils.SignedArea(ground));
                bool strip = groundArea < StripAreaRatio * Mathf.Abs(PolygonUtils.SignedArea(footprint));
                HouseStyle style = HouseCatalog.Find(building.BuildingId);
                return new ArcadePart
                {
                    Ground = ground,
                    Segment = segment,
                    Top = HighestGround(ctx, footprint, InBand) + ctx.Data.Settings.PorticoHeight,
                    Floors = strip ? Mathf.Max(building.Floors, TallestNeighbourBehind(building, parts, segment)) : building.Floors,
                    Arched = style != null && style.ArchedArcade,
                };
            }

            return null;
        }

        /// <summary>Pasos de calle bajo la casa: abre también esa franja de su planta baja.</summary>
        /// <param name="part">Parte ya recortada por un soportal, o null.</param>
        private static ArcadePart OpenPassages(GenerationContext ctx, BuildingFootprint building, ArcadePart part)
        {
            foreach (StreetPassage passage in PlazaLayout.Passages)
            {
                List<Vector2> source = part != null ? part.Ground : building.Footprint;
                if (source == null)
                {
                    continue;
                }

                List<Vector2> ground = ArcadeBands.ClipOutside(source, passage.Contains, out bool cut);
                if (!cut)
                {
                    continue;
                }

                float top = HighestGround(ctx, building.Footprint, passage.Contains) + passage.ClearHeight;
                if (part != null)
                {
                    part.Ground = ground;
                    part.Top = Mathf.Max(part.Top, top);
                }
                else
                {
                    part = new ArcadePart { Ground = ground, Passage = passage, Top = top, Floors = building.Floors };
                }
            }

            return part;
        }

        /// <summary>Cota más alta del terreno en los puntos del contorno (cada 0,5 m) que caen dentro de una región.</summary>
        private static float HighestGround(GenerationContext ctx, List<Vector2> footprint, System.Func<Vector2, bool> inside)
        {
            float highest = float.MinValue;
            foreach (Vector2 v in Densify(footprint, 0.5f))
            {
                if (inside(v))
                {
                    highest = Mathf.Max(highest, ctx.GroundY(v.x, v.y));
                }
            }

            return highest;
        }

        /// <summary>
        /// Indica si delante de un lado pasa un soportal: ahí no se ponen toldos, que cortarían el paso.
        /// </summary>
        internal static bool NearArcade(Vector2 a, Vector2 b)
        {
            Vector2 probe = (a + b) * 0.5f + Outward(a, b) * 1.5f;
            foreach (ArcadeSegment segment in PlazaLayout.Arcades)
            {
                if (segment.DistanceTo(probe) < 5f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Indica si un lado da al interior de un paso bajo casas.</summary>
        internal static bool FacesPassage(Vector2 a, Vector2 b)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 probe = (a + b) * 0.5f + new Vector2(dir.y, -dir.x) * 0.5f;
            foreach (StreetPassage passage in PlazaLayout.Passages)
            {
                if (passage.Contains(probe))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Indica si un lado da al pasillo de algún soportal (está en el fondo de su franja).</summary>
        internal static bool FacesCorridor(Vector2 a, Vector2 b, float corridorDepth)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 probe = (a + b) * 0.5f + new Vector2(dir.y, -dir.x) * 0.6f;
            foreach (ArcadeSegment segment in PlazaLayout.Arcades)
            {
                if (ArcadeBands.InBand(segment.Line, probe, corridorDepth))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Indica si un lado forma el frente de un soportal (mira hacia la línea y está cerca de ella).</summary>
        internal static bool IsArcadeFront(ArcadeSegment segment, Vector2 a, Vector2 b)
        {
            Vector2 mid = (a + b) * 0.5f;
            return ArcadeBands.TryLateral(segment.Line, mid, out float lateral, out _) && lateral < 2.5f &&
                   ArcadeBands.TryLateral(segment.Line, mid + Outward(a, b) * 1f, out float ahead, out _) && ahead < lateral;
        }

        /// <summary>
        /// Mayor número de plantas entre las partes pegadas a esta y más alejadas de la línea del soportal
        /// (la casa de la que la franja del soportal forma parte).
        /// </summary>
        private static int TallestNeighbourBehind(BuildingFootprint building, List<BuildingFootprint> allParts, ArcadeSegment segment)
        {
            const float touchDistance = 0.6f;
            List<Vector2> footprint = building.Footprint;
            Vector2 centroid = PolygonUtils.Centroid(footprint);
            float ownDistance = segment.DistanceTo(centroid);
            int tallest = 0;

            foreach (BuildingFootprint other in allParts)
            {
                if (other == building || other.Floors <= tallest)
                {
                    continue;
                }

                Vector2 otherCentroid = PolygonUtils.Centroid(other.Footprint);
                if (segment.DistanceTo(otherCentroid) <= ownDistance || Vector2.Distance(otherCentroid, centroid) > 40f)
                {
                    continue;
                }

                foreach (Vector2 v in other.Footprint)
                {
                    if (PolygonUtils.DistanceToEdges(footprint, v) < touchDistance)
                    {
                        tallest = other.Floors;
                        break;
                    }
                }
            }

            return tallest;
        }
    }
}
