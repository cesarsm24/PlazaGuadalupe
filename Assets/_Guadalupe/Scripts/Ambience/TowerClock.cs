using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Agujas del reloj de la torre del monasterio: marcan la hora del <see cref="SimulationClock"/>, de modo que
    /// el reloj coincide con la franja elegida (mañana, tarde o noche) y avanza con ella.
    /// </summary>
    /// <remarks>
    /// Cada aguja gira sobre su pivote en el centro de la esfera alrededor del eje Z local, que apunta hacia el
    /// interior de la torre; con el ángulo negativo, el giro es en el sentido de las agujas visto desde la plaza.
    /// </remarks>
    [ExecuteAlways]
    public sealed class TowerClock : MonoBehaviour
    {
        [SerializeField] private SimulationClock _clock;
        [SerializeField] private Transform _hourHand;
        [SerializeField] private Transform _minuteHand;

        /// <summary>Avance mínimo de la hora (medio grado de la aguja de minutos) que mueve las agujas.</summary>
        private const float MinStep = 1f / 720f;

        private float _lastHour = -1f;
        private SimulationClock _subscribed;

        /// <summary>Asigna el reloj y los pivotes de las agujas al construir el modelo del monasterio.</summary>
        public void Configure(SimulationClock clock, Transform hourHand, Transform minuteHand)
        {
            _clock = clock;
            _hourHand = hourHand;
            _minuteHand = minuteHand;
            Subscribe();
            _lastHour = -1f;
            UpdateHands();
        }

        /// <summary>Asigna el reloj de la simulación (el prefab no puede referenciar objetos de la escena).</summary>
        public void SetClock(SimulationClock clock) => Configure(clock, _hourHand, _minuteHand);

        private void OnEnable()
        {
            Subscribe();
            _lastHour = -1f;
            UpdateHands();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            Unsubscribe();
            if (isActiveAndEnabled && _clock != null)
            {
                _subscribed = _clock;
                _subscribed.Changed += UpdateHands;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed != null)
            {
                _subscribed.Changed -= UpdateHands;
                _subscribed = null;
            }
        }

        private void UpdateHands()
        {
            if (_clock == null || _hourHand == null || _minuteHand == null)
            {
                return;
            }

            float hour = _clock.Hour;
            if (Mathf.Abs(hour - _lastHour) < MinStep)
            {
                return;
            }

            _lastHour = hour;
            _hourHand.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Repeat(hour, 12f) / 12f * 360f);
            _minuteHand.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Repeat(hour, 1f) * 360f);
        }
    }
}
