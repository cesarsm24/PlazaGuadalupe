using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Recoge, mientras se construyen las casas, el mejor lado de fachada de cada una para colocar después su
    /// rótulo, portada y azulejo (uno por finca catastral).
    /// </summary>
    internal sealed class ShopfrontCollector
    {
        private readonly Dictionary<string, Frontage> _best = new Dictionary<string, Frontage>();

        /// <summary>Fachada principal elegida para cada finca.</summary>
        public IEnumerable<Frontage> Frontages => _best.Values;

        /// <summary>Propone el lado a→b como fachada principal de la casa; gana el que más mira a la plaza.</summary>
        /// <param name="y">Altura a la que iría el rótulo.</param>
        public void Propose(BuildingFootprint building, HouseStyle style, Vector2 a, Vector2 b, float y)
        {
            if (style == null || string.IsNullOrEmpty(style.Address) || Vector2.Distance(a, b) < 1.6f)
            {
                return;
            }

            // Se queda con el lado que más mira a la plaza: el de menor distancia al centro tras salir 3 m del muro.
            Vector2 mid = (a + b) * 0.5f;
            float score = (mid + Outward(a, b) * 3f).magnitude;
            string key = building.ParcelRef;
            if (!_best.TryGetValue(key, out var best) || score < best.Score)
            {
                _best[key] = new Frontage
                {
                    Mid = mid, Normal = Outward(a, b), Along = (b - a).normalized, Length = Vector2.Distance(a, b),
                    SignY = y, Score = score, Style = style,
                };
            }
        }
    }
}
