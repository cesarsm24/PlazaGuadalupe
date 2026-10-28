using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Sitios que los turistas no pueden usar porque son de un personaje principal (la mesa de Inés María). Los
    /// turistas no comparten mesa con desconocidos, así que reservar una silla deja libre la mesa entera.
    /// </summary>
    public static class ReservedSpots
    {
        private static readonly HashSet<Transform> Spots = new HashSet<Transform>();

        public static void Reserve(Transform spot) => Spots.Add(spot);

        public static void Release(Transform spot) => Spots.Remove(spot);

        public static bool Contains(Transform spot) => Spots.Contains(spot);

        // Sin recarga de dominio seguirían las reservas de la partida anterior.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear() => Spots.Clear();
    }
}
