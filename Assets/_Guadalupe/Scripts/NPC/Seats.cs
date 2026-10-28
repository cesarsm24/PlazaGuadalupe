using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Medidas de las sillas de las terrazas, compartidas por los turistas y los personajes principales que se
    /// sientan en ellas.
    /// </summary>
    internal static class Seats
    {
        public const string SeatChild = "Asiento";

        /// <summary>Altura del asiento sobre la base de la silla si no tiene uno con malla (m).</summary>
        public const float DefaultHeight = 0.48f;

        /// <summary>Altura de la cadera sobre el asiento cuando está sentado (m).</summary>
        public const float HipClearance = 0.08f;

        /// <summary>Elevación máxima para que la cadera no quede por debajo del asiento (m).</summary>
        public const float MaxLift = 0.3f;

        /// <summary>Altura en el mundo de la parte de arriba del asiento de <paramref name="chair"/>.</summary>
        /// <param name="baseHeight">Altura de la base de la silla, por si no tiene asiento con malla.</param>
        public static float HeightOf(Transform chair, float baseHeight)
        {
            Transform seat = chair.Find(SeatChild);
            Renderer renderer = seat != null ? seat.GetComponent<Renderer>() : null;
            return renderer != null ? renderer.bounds.max.y : baseHeight + DefaultHeight;
        }

        /// <summary>Cuánto hay que elevar a alguien sentado para que la cadera llegue a <paramref name="wantedHips"/>.</summary>
        public static float Lift(float hips, float wantedHips, float max) => Mathf.Clamp(wantedHips - hips, 0f, max);
    }
}
