using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Planea cómo se sienta un turista en una silla de terraza, como lo haría una persona: llega a un lado de la silla,
    /// por fuera de la mesa (la silla queda dentro del hueco que la mesa deja en la NavMesh), la separa de la mesa, se
    /// pone delante del asiento y se sienta. Recuerda dónde estaba cada silla junto a la mesa, porque al usarla se mueve.
    /// </summary>
    internal sealed class SeatPlanner
    {
        // Medidas en metros.
        private const float ChairPull = 0.3f;
        private const float StandOffset = 0.32f; // del asiento, ya con la silla separada
        private const float SideOffset = 0.55f; // lado de la silla por el que entra y sale
        private const float Reach = 1.5f; // de ese lado a la NavMesh
        private const float ChairClearance = 0.4f;
        private const float BodyRadius = 0.2f;

        private const string TableTop = "Tablero";

        private readonly Dictionary<Transform, Vector3> _chairHome = new Dictionary<Transform, Vector3>();

        /// <summary>
        /// Prepara la parada sentado en <paramref name="chair"/>, eligiendo el lado por el que no tiene que atravesar
        /// otra silla ni la mesa; false si ningún lado está al alcance de la NavMesh.
        /// </summary>
        public bool TryPlan(Transform chair, TouristActivity activity, float stay, out TouristVisit visit)
        {
            if (!_chairHome.TryGetValue(chair, out Vector3 home))
            {
                home = chair.position;
                _chairHome[chair] = home;
            }

            Vector3 pulled = home - chair.forward * ChairPull;
            Vector3 front = pulled + chair.forward * StandOffset;
            float seatHeight = Seats.HeightOf(chair, home.y);

            visit = default;
            bool found = false;
            float sign = Random.value < 0.5f ? 1f : -1f;
            for (int attempt = 0; attempt < 2 && !found; attempt++, sign = -sign)
            {
                Vector3 side = front + chair.right * (sign * SideOffset);
                if (!NavMesh.SamplePosition(side, out NavMeshHit hit, Reach, NavMesh.AllAreas))
                {
                    continue;
                }

                bool clear = IsClear(hit.position, side, chair) && IsClear(side, front, chair);
                if (clear || attempt == 1)
                {
                    visit = new TouristVisit(hit.position, front, chair.rotation, activity, stay, true, pulled, seatHeight,
                        home + chair.forward * 2f, side, chair, home, pulled);
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Indica si se puede andar de <paramref name="from"/> a <paramref name="to"/> sin atravesar las otras sillas de la
        /// mesa ni el tablero. El conjunto de mesa y sillas tiene un único collider que lo envuelve, así que se mira con
        /// la geometría de sus piezas.
        /// </summary>
        public static bool IsClear(Vector3 from, Vector3 to, Transform chair)
        {
            foreach (Transform piece in chair.parent)
            {
                float clearance;
                if (piece == chair)
                {
                    continue;
                }

                if (piece.name == chair.name)
                {
                    clearance = ChairClearance;
                }
                else if (piece.name == TableTop && piece.TryGetComponent(out MeshFilter top) && top.sharedMesh != null)
                {
                    // Medio lado del tablero con su propio tamaño: la caja en el mundo crece si la mesa está girada.
                    Vector3 half = Vector3.Scale(top.sharedMesh.bounds.extents, piece.lossyScale);
                    clearance = Mathf.Max(half.x, half.z) + BodyRadius;
                }
                else
                {
                    continue;
                }

                if (GroundPlane.DistanceToSegment(piece.position.Flat(), from.Flat(), to.Flat()) < clearance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
