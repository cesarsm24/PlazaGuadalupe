using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>Hasta dónde se fija un personaje principal en el jugador.</summary>
    public enum Engagement
    {
        /// <summary>Lejos: sigue con lo suyo.</summary>
        None,

        /// <summary>A la vista: se le ve el bocadillo de que se puede hablar con él.</summary>
        Noticed,

        /// <summary>Cerca: se para, le mira y le saluda.</summary>
        Greeted,

        /// <summary>Al lado y de cara: se puede hablar con él.</summary>
        InReach,
    }

    /// <summary>Cómo saluda un personaje principal cuando el jugador se le acerca.</summary>
    public enum Greeting
    {
        /// <summary>Con la mano.</summary>
        Wave,

        /// <summary>Con una inclinación (Fray Paulino).</summary>
        Bow,

        /// <summary>Llamándole con la mano (Inés María, desde su silla).</summary>
        Beckon,
    }

    /// <summary>
    /// Reglas de los personajes principales sin estado: distancias a las que se fijan en el jugador y su zona, una
    /// cápsula (segmento con radio) en el plano del suelo.
    /// </summary>
    public static class MainCharacterRules
    {
        /// <summary>Distancia a la que aparece el bocadillo.</summary>
        public const float NoticeDistance = 10f;

        /// <summary>Distancia a la que se para y saluda.</summary>
        public const float GreetDistance = 4f;

        /// <summary>Distancia a la que se puede hablar con él.</summary>
        public const float TalkDistance = 2.5f;

        /// <summary>Ángulo máximo entre hacia dónde mira el jugador y el personaje para poder hablarle.</summary>
        public const float TalkAngle = 60f;

        /// <summary>Hasta dónde se fija en el jugador según la distancia y si el jugador le mira.</summary>
        public static Engagement EngagementFor(float distance, float facingAngle)
        {
            if (distance <= TalkDistance && facingAngle <= TalkAngle)
            {
                return Engagement.InReach;
            }

            if (distance <= GreetDistance)
            {
                return Engagement.Greeted;
            }

            return distance <= NoticeDistance ? Engagement.Noticed : Engagement.None;
        }

        /// <summary>Indica si el punto está dentro de la zona, mirando solo el plano del suelo.</summary>
        public static bool InZone(Vector3 point, Vector3 start, Vector3 end, float radius) =>
            GroundPlane.DistanceToSegment(point.Flat(), start.Flat(), end.Flat()) <= radius;

        /// <summary>
        /// Punto de la zona a partir de dos valores al azar: <paramref name="along"/> en [0, 1] a lo largo del eje y
        /// <paramref name="disc"/> dentro del círculo unidad. La altura es la del eje.
        /// </summary>
        public static Vector3 PointInZone(Vector3 start, Vector3 end, float radius, float along, Vector2 disc) =>
            Vector3.Lerp(start, end, along) + new Vector3(disc.x, 0f, disc.y) * radius;

        // Momentos del gesto de beber (tiempo normalizado del clip): alarga la mano hasta el vaso, lo coge, lo sube a
        // la boca con el gesto tal cual, lo baja, lo deja en su sitio y retira la mano.
        private const float ReachStart = 0.05f, Grab = 0.12f, LiftStart = 0.18f, LiftEnd = 0.28f;
        private const float LowerStart = 0.6f, LowerEnd = 0.68f, Release = 0.72f, WithdrawEnd = 0.82f;

        /// <summary>
        /// Cuánto lleva la mano al vaso en la mesa en el momento <paramref name="t"/> del gesto de beber: 1 mientras lo
        /// coge o lo deja, 0 mientras bebe (la mano sigue al gesto) y antes y después.
        /// </summary>
        public static float DrinkReach(float t)
        {
            if (t < LiftStart)
            {
                return Mathf.InverseLerp(ReachStart, Grab, t);
            }

            if (t < LowerStart)
            {
                return 1f - Mathf.InverseLerp(LiftStart, LiftEnd, t);
            }

            return t < Release ? Mathf.InverseLerp(LowerStart, LowerEnd, t) : 1f - Mathf.InverseLerp(Release, WithdrawEnd, t);
        }

        // Trago: el gesto sube la mano hasta la barbilla; en este tramo se lleva el borde del vaso a los labios.
        private const float SipStart = 0.3f, SipFull = 0.38f, SipEnd = 0.5f, SipDone = 0.58f;

        /// <summary>Cuánto lleva el borde del vaso a la boca en el momento <paramref name="t"/> del gesto de beber.</summary>
        public static float DrinkSip(float t) =>
            t < SipEnd ? Mathf.InverseLerp(SipStart, SipFull, t) : 1f - Mathf.InverseLerp(SipEnd, SipDone, t);

        /// <summary>Si en el momento <paramref name="t"/> del gesto de beber tiene el vaso en la mano.</summary>
        public static bool HoldsDrink(float t) => t >= Grab && t < Release;
    }
}
