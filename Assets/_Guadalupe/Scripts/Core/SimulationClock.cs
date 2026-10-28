using System;
using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Reloj de la simulación: hora, día del año y diferencia con UTC. Es la única fuente de la hora de la escena;
    /// el sol, el reloj de la torre y el sonido la leen de aquí.
    /// </summary>
    /// <remarks>
    /// En juego avanza a tiempo real y pasa al día siguiente a medianoche. En el Editor no avanza,
    /// pero cambiar sus valores en el Inspector actualiza la escena para previsualizar la hora.
    /// </remarks>
    [ExecuteAlways]
    public sealed class SimulationClock : MonoBehaviour
    {
        [SerializeField, Range(0f, 24f)] private float _hour = 17.5f;
        [Tooltip("Día del año (251 = 8 de septiembre).")]
        [SerializeField, Range(1, 365)] private int _dayOfYear = 251;
        [Tooltip("Diferencia con UTC en horas (2 = horario de verano peninsular).")]
        [SerializeField] private float _utcOffset = 2f;

        public event Action Changed;

        public float Hour
        {
            get => _hour;
            set
            {
                _hour = Mathf.Repeat(value, 24f);
                Changed?.Invoke();
            }
        }

        public int DayOfYear => _dayOfYear;

        public float UtcOffset => _utcOffset;

        public void SetDate(int dayOfYear, float utcOffset)
        {
            _dayOfYear = Mathf.Clamp(dayOfYear, 1, 365);
            _utcOffset = utcOffset;
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            (float hour, int dayOfYear) = Advance(_hour, _dayOfYear, Time.deltaTime / 3600f);
            _dayOfYear = dayOfYear;
            Hour = hour;
        }

        /// <summary>
        /// Avanza la hora y pasa al día siguiente al cruzar la medianoche; después del día 365 vuelve el 1.
        /// </summary>
        internal static (float Hour, int DayOfYear) Advance(float hour, int dayOfYear, float hours)
        {
            float next = hour + hours;
            if (next >= 24f)
            {
                dayOfYear = dayOfYear % 365 + 1;
            }

            return (Mathf.Repeat(next, 24f), dayOfYear);
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Los suscriptores mueven el sol y las luces, algo que no conviene hacer durante OnValidate.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    Changed?.Invoke();
                }
            };
#endif
        }
    }
}
