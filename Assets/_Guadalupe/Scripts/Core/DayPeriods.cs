namespace Guadalupe.Core
{
    /// <summary>Franja horaria en la que empieza la visita.</summary>
    public enum DayPeriod
    {
        Morning,
        Afternoon,
        Night,
    }

    /// <summary>Hora de inicio y nombre visible de cada franja horaria.</summary>
    public static class DayPeriods
    {
        /// <summary>Horas antes de la puesta de sol a las que empieza la tarde (luz cálida, todavía de día).</summary>
        public const float AfternoonBeforeSunset = 1.25f;

        /// <summary>
        /// Hora (0-24) a la que empieza la franja: la mañana a las 10:30 y la noche a las 22:30; la tarde, un rato
        /// antes de la puesta de sol, de modo que es de día tanto en verano como en invierno.
        /// </summary>
        /// <param name="sunset">Hora de la puesta de sol del día de la visita.</param>
        public static float StartHour(DayPeriod period, float sunset)
        {
            switch (period)
            {
                case DayPeriod.Morning: return 10.5f;
                case DayPeriod.Night: return 22.5f;
                default: return sunset - AfternoonBeforeSunset;
            }
        }

        public static string DisplayName(DayPeriod period)
        {
            switch (period)
            {
                case DayPeriod.Morning: return "Mañana";
                case DayPeriod.Night: return "Noche";
                default: return "Tarde";
            }
        }
    }
}
