using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Comportamiento de una paloma sin depender de la escena: cuándo ha aterrizado, cómo planea, el ciclo de
    /// pasitos y picoteo en el suelo, el aleteo y la huida. <see cref="PigeonFlock"/> lo aplica a cada paloma.
    /// </summary>
    internal static class PigeonBehaviour
    {
        public const float TouchdownSpeed = 1.2f;
        public const float GlideDistance = 12f;

        public const float GroundCycle = 3f; // s
        // Segundos del ciclo que pasa caminando; el resto cabecea y picotea.
        public const float WalkingPart = 1.2f;
        // Desde aquí hasta el final del ciclo tiene la cabeza agachada.
        public const float PeckFrom = 2.2f;
        public const float WalkSpeed = 0.35f;

        public const float FleeDistance = 10f;
        public const float TakeOffSpeed = 3.5f;
        public const float ScareSpreadDistance = 3f;

        /// <summary>Velocidad del planeo de aterrizaje: frena al acercarse al punto.</summary>
        public static float LandingSpeed(float distance, float flySpeed) =>
            Mathf.Lerp(TouchdownSpeed, flySpeed, Mathf.Clamp01(distance / GlideDistance));

        /// <summary>Indica si ha llegado al punto: casi encima, o cerca y ya a su altura.</summary>
        public static bool HasLanded(float distance, float heightAboveTarget) =>
            distance < 0.35f || (distance < 1.2f && heightAboveTarget < 0.3f);

        /// <summary>Indica si en este momento del ciclo en el suelo está dando pasitos.</summary>
        public static bool IsWalking(float phase) => Mathf.Repeat(phase, GroundCycle) < WalkingPart;

        /// <summary>Inclinación de la cabeza en el suelo: cabeceo al andar y picotazo al final del ciclo.</summary>
        public static float HeadPitch(float phase) =>
            Mathf.Repeat(phase, GroundCycle) > PeckFrom ? 25f : Mathf.Sin(phase * 9f) * 6f;

        /// <summary>
        /// Ángulo del ala en el aire: aleteo rápido al despegar y al subir, y planeo a ratos en el vuelo en círculo.
        /// </summary>
        public static float WingAngle(bool fleeing, bool circling, float time, float phase)
        {
            bool gliding = circling && Mathf.Sin(time * 0.6f + phase) > 0.35f;
            float frequency = fleeing ? 14f : 9f;
            return gliding ? 8f : Mathf.Sin((time + phase) * frequency) * 55f;
        }

        /// <summary>
        /// Despegue al asustarse: un punto alejado del jugador y por encima, y una velocidad inicial casi vertical.
        /// </summary>
        /// <param name="away">Dirección horizontal hacia la que huye.</param>
        /// <param name="climb">Altura que gana (m).</param>
        public static (Vector3 Target, Vector3 Velocity) TakeOff(Vector3 position, Vector3 away, float climb)
        {
            away.y = 0f;
            away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
            return (position + away * FleeDistance + Vector3.up * climb, (away + Vector3.up * 2.2f).normalized * TakeOffSpeed);
        }
    }
}
