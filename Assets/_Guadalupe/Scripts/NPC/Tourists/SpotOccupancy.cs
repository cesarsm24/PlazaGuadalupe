using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Qué sitios de cada destino están ocupados y por quién. Los sitios van por bloques (una mesa, un corrillo): un
    /// bloque con alguien no se comparte con desconocidos.
    /// </summary>
    internal sealed class SpotOccupancy
    {
        // Si algo recorta la NavMesh junto al sitio, deja de valer.
        private const float SpotOnNavMesh = 0.15f;

        private readonly TouristDestination[] _destinations;
        private readonly Dictionary<Tourist, (int Destination, int Spot)> _spotOf = new Dictionary<Tourist, (int, int)>();
        private readonly Dictionary<(int Destination, int Spot), int> _occupants = new Dictionary<(int, int), int>();

        public SpotOccupancy(TouristDestination[] destinations) => _destinations = destinations;

        /// <summary>Apunta que el turista ocupa ese sitio del destino.</summary>
        public void Occupy(Tourist tourist, int destination, int spot)
        {
            var key = (destination, spot);
            _spotOf[tourist] = key;
            _occupants[key] = _occupants.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        /// <summary>El turista deja el sitio que ocupaba, si ocupaba alguno.</summary>
        public void Free(Tourist tourist)
        {
            if (!_spotOf.TryGetValue(tourist, out (int, int) spot))
            {
                return;
            }

            _spotOf.Remove(tourist);
            if (--_occupants[spot] <= 0)
            {
                _occupants.Remove(spot);
            }
        }

        /// <summary>
        /// Busca al azar un bloque del destino sin nadie y con al menos <paramref name="minFree"/> sitios que se
        /// puedan usar, y deja esos sitios en <paramref name="free"/>.
        /// </summary>
        public bool FreeBlock(int destinationIndex, int minFree, List<int> free)
        {
            TouristDestination destination = _destinations[destinationIndex];
            int blockSize = destination.BlockSize;
            int blocks = destination.Spots.Length / blockSize;
            int start = Random.Range(0, Mathf.Max(1, blocks));
            for (int b = 0; b < blocks; b++)
            {
                int block = (start + b) % blocks;
                bool empty = true;
                free.Clear();
                for (int s = block * blockSize; s < (block + 1) * blockSize; s++)
                {
                    if (_occupants.ContainsKey((destinationIndex, s)))
                    {
                        empty = false;
                    }
                    else if (IsUsable(destination, destination.Spots[s]))
                    {
                        free.Add(s);
                    }
                }

                if (empty && free.Count >= minFree)
                {
                    return true;
                }
            }

            free.Clear();
            return false;
        }

        /// <summary>
        /// Indica si se puede usar un sitio ahora: su objeto está visible (en Navidad se retiran mesas para el árbol),
        /// no lo tiene reservado un personaje principal y, si no es una silla, sigue sobre la NavMesh (el árbol
        /// recorta la NavMesh a su alrededor).
        /// </summary>
        private static bool IsUsable(TouristDestination destination, Transform spot) =>
            spot.gameObject.activeInHierarchy && !ReservedSpots.Contains(spot) &&
            (destination.Seated || NavMesh.SamplePosition(spot.position, out _, SpotOnNavMesh, NavMesh.AllAreas));
    }
}
