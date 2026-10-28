using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>Una parada de un turista: adónde va, dónde se coloca y qué hace allí.</summary>
    public readonly struct TouristVisit
    {
        public TouristVisit(Vector3 approach, Vector3 position, Quaternion rotation, TouristActivity activity, float stay,
            Vector3 focus)
            : this(approach, position, rotation, activity, stay, false, position, 0f, focus, position, null, default, default)
        {
        }

        public TouristVisit(Vector3 approach, Vector3 position, Quaternion rotation, TouristActivity activity, float stay,
            bool seated, Vector3 seat, float seatHeight, Vector3 focus, Vector3 side, Transform chair, Vector3 chairHome,
            Vector3 chairPulled)
        {
            Side = side;
            Chair = chair;
            ChairHome = chairHome;
            ChairPulled = chairPulled;
            Approach = approach;
            Position = position;
            Rotation = rotation;
            Activity = activity;
            Stay = stay;
            Seated = seated;
            Seat = seat;
            SeatHeight = seatHeight;
            Focus = focus;
        }

        /// <summary>Punto de la NavMesh al que llega andando.</summary>
        public Vector3 Approach { get; }

        /// <summary>
        /// Posición de pie en su sitio: delante del asiento de la silla ya separada de la mesa, si se sienta; si no, la
        /// de <see cref="Approach"/>.
        /// </summary>
        public Vector3 Position { get; }

        public Quaternion Rotation { get; }
        public TouristActivity Activity { get; }

        /// <summary>Segundos que pasa en su sitio.</summary>
        public float Stay { get; }

        /// <summary>Se sienta: deja la NavMesh para colocarse en la silla.</summary>
        public bool Seated { get; }

        public Vector3 Seat { get; }

        /// <summary>Lo que mira o fotografía desde su sitio.</summary>
        public Vector3 Focus { get; }

        /// <summary>
        /// Punto a un lado de la silla por el que entra y sale de su sitio (sentado), fuera de la mesa; sin silla, el
        /// mismo que <see cref="Position"/>.
        /// </summary>
        public Vector3 Side { get; }

        public Transform Chair { get; }

        // Posición de la silla junto a la mesa y separada de ella para sentarse.
        public Vector3 ChairHome { get; }
        public Vector3 ChairPulled { get; }

        public float SeatHeight { get; }
    }
}
