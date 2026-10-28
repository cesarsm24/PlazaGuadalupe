using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Posición del sol para una hora, un día del año y un lugar: elevación y acimut por la fórmula del ángulo
    /// horario, y horas de salida y puesta.
    /// </summary>
    /// <remarks>
    /// Usa la declinación aproximada de Cooper y el tiempo solar medio (sin ecuación del tiempo), con un error
    /// de pocos minutos, suficiente para la iluminación de la escena.
    /// </remarks>
    internal static class SolarPosition
    {
        /// <summary>Inclinación del eje terrestre en grados.</summary>
        private const float AxialTilt = 23.44f;

        /// <summary>Declinación solar en radianes para un día del año (1-365).</summary>
        public static float Declination(int dayOfYear)
        {
            return AxialTilt * Mathf.Sin(2f * Mathf.PI * (284 + dayOfYear) / 365f) * Mathf.Deg2Rad;
        }

        /// <summary>Hora oficial del mediodía solar.</summary>
        /// <param name="longitude">Longitud en grados (positiva al este).</param>
        /// <param name="utcOffset">Diferencia con UTC en horas.</param>
        public static float SolarNoon(float longitude, float utcOffset) => 12f - longitude / 15f + utcOffset;

        /// <summary>
        /// Elevación (grados sobre el horizonte, negativa de noche) y acimut (grados desde el norte hacia el este)
        /// del sol a una hora oficial.
        /// </summary>
        public static void Compute(float hour, int dayOfYear, float utcOffset, float latitude, float longitude,
            out float elevation, out float azimuth)
        {
            float declination = Declination(dayOfYear);
            float phi = latitude * Mathf.Deg2Rad;
            float hourAngle = (hour - SolarNoon(longitude, utcOffset)) * 15f * Mathf.Deg2Rad;

            float sinElevation = Mathf.Sin(phi) * Mathf.Sin(declination) +
                                 Mathf.Cos(phi) * Mathf.Cos(declination) * Mathf.Cos(hourAngle);
            elevation = Mathf.Asin(Mathf.Clamp(sinElevation, -1f, 1f)) * Mathf.Rad2Deg;

            // Acimut medido desde el sur hacia el oeste; se pasa a la referencia norte-este.
            float fromSouth = Mathf.Atan2(Mathf.Sin(hourAngle),
                Mathf.Cos(hourAngle) * Mathf.Sin(phi) - Mathf.Tan(declination) * Mathf.Cos(phi));
            azimuth = Mathf.Repeat(fromSouth * Mathf.Rad2Deg + 180f, 360f);
        }

        /// <summary>Horas oficiales de salida y puesta del sol (centro del disco en el horizonte).</summary>
        public static void Daylight(int dayOfYear, float utcOffset, float latitude, float longitude,
            out float sunrise, out float sunset)
        {
            float cosH = Mathf.Clamp(-Mathf.Tan(latitude * Mathf.Deg2Rad) * Mathf.Tan(Declination(dayOfYear)), -1f, 1f);
            float halfDay = Mathf.Acos(cosH) * Mathf.Rad2Deg / 15f;
            float noon = SolarNoon(longitude, utcOffset);
            sunrise = noon - halfDay;
            sunset = noon + halfDay;
        }
    }
}
