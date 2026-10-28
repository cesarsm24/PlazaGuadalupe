using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Iguala la altura de los aleros de casas contiguas casi iguales.</summary>
    internal static class EaveAligner
    {
        /// <summary>Altura del alero sobre la última planta.</summary>
        private const float EaveAboveTopFloor = 0.5f;

        /// <summary>Plantas de una parte: las del catálogo, si no las del soportal y si no las del Catastro.</summary>
        internal static int FloorsOf(BuildingFootprint part, HouseStyle style, ArcadePart arcade) =>
            style?.Floors ?? arcade?.Floors ?? part.Floors;

        /// <summary>Altura del alero de una casa sobre el punto más alto de su suelo.</summary>
        internal static float EaveHeight(float groundTop, int floors, float levelHeight) =>
            groundTop + floors * levelHeight + EaveAboveTopFloor;

        /// <summary>
        /// Iguala la altura del alero de las partes contiguas cuya diferencia es menor de 60 cm. En calles en
        /// cuesta, casas vecinas con el mismo número de plantas tendrían aleros casi coincidentes y dejarían ver
        /// una franja de muro sobre el tejado vecino. Las diferencias mayores (una planta más) se mantienen.
        /// </summary>
        /// <returns>Altura del alero igualada de cada parte.</returns>
        internal static Dictionary<BuildingFootprint, float> Align(GenerationContext ctx, List<BuildingFootprint> parts,
            Dictionary<BuildingFootprint, ArcadePart> arcade)
        {
            const float tolerance = 0.6f;
            const float touch = 0.6f;
            var heights = new Dictionary<BuildingFootprint, float>();
            int n = parts.Count;
            var eave = new float[n];
            var bounds = new Rect[n];
            for (int i = 0; i < n; i++)
            {
                List<Vector2> f = parts[i].Footprint;
                ctx.GroundRange(f, out _, out float maxY);
                HouseStyle style = HouseCatalog.Find(parts[i].BuildingId);
                arcade.TryGetValue(parts[i], out ArcadePart info);
                eave[i] = EaveHeight(maxY, FloorsOf(parts[i], style, info), ctx.Data.Settings.LevelHeight);
                PolygonUtils.Bounds(f, out Vector2 min, out Vector2 max);

                bounds[i] = Rect.MinMaxRect(min.x - touch, min.y - touch, max.x + touch, max.y + touch);
            }

            // Unión de partes contiguas de altura casi igual.
            var parent = new int[n];
            for (int i = 0; i < n; i++)
            {
                parent[i] = i;
            }

            int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (Mathf.Abs(eave[i] - eave[j]) > tolerance || !bounds[i].Overlaps(bounds[j]) || !Touch(parts[i].Footprint, parts[j].Footprint, touch))
                    {
                        continue;
                    }

                    parent[Find(i)] = Find(j);
                }
            }

            var groupMax = new Dictionary<int, float>();
            for (int i = 0; i < n; i++)
            {
                int root = Find(i);
                groupMax[root] = groupMax.TryGetValue(root, out float m) ? Mathf.Max(m, eave[i]) : eave[i];
            }

            for (int i = 0; i < n; i++)
            {
                heights[parts[i]] = groupMax[Find(i)];
            }

            return heights;
        }

        private static bool Touch(List<Vector2> a, List<Vector2> b, float distance)
        {
            foreach (Vector2 v in a)
            {
                if (PolygonUtils.DistanceToEdges(b, v) < distance)
                {
                    return true;
                }
            }

            foreach (Vector2 v in b)
            {
                if (PolygonUtils.DistanceToEdges(a, v) < distance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
