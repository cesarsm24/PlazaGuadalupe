using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Aplica a la escena el escenario y la franja de la <see cref="SessionState"/>: fecha y hora del reloj y,
    /// en Navidad, los adornos, los focos reforzados y las sombrillas recogidas. Así la plaza muestra cómo
    /// empezará la visita en cuanto se elige en el menú.
    /// </summary>
    public sealed class ScenarioApplier : MonoBehaviour
    {
        [SerializeField] private SessionState _session;
        [SerializeField] private SimulationClock _clock;
        [SerializeField] private SunController _sun;
        [SerializeField] private FestiveLights _festiveLights;
        [Tooltip("Iluminación monumental, que se refuerza en Navidad.")]
        [SerializeField] private NightFloodLights _floodLights;
        [Tooltip("Sombrillas de las terrazas, que se recogen en Navidad.")]
        [SerializeField] private TerraceUmbrellas _umbrellas;
        [Tooltip("Multiplicador de la iluminación monumental en Navidad.")]
        [SerializeField, Min(1f)] private float _christmasFloodBoost = 2.4f;
        [Tooltip("Color de los proyectores del monasterio en Navidad, que lo bañan de luz dorada.")]
        [SerializeField] private Color _christmasFloodColor = new Color(1f, 0.72f, 0.4f);

        private bool _subscribed;

        private void OnEnable()
        {
            if (!MissingReferences.Check(this, (_session, "el estado de la sesión"), (_clock, "el reloj"), (_sun, "el sol")))
            {
                return;
            }

            _session.ScenarioChanged += OnScenarioChanged;
            _session.PeriodChanged += OnPeriodChanged;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _session.ScenarioChanged -= OnScenarioChanged;
                _session.PeriodChanged -= OnPeriodChanged;
                _subscribed = false;
            }
        }

        private void OnScenarioChanged(ScenarioDefinition scenario)
        {
            if (scenario == null)
            {
                return;
            }

            // La fecha cambia la hora de la puesta, así que la hora de la franja se recalcula con la fecha nueva.
            _clock.SetDate(scenario.DayOfYear, scenario.UtcOffset);
            OnPeriodChanged(_session.Period);
            ApplyChristmas(scenario.Christmas);
        }

        private void OnPeriodChanged(DayPeriod period) =>
            _clock.Hour = DayPeriods.StartHour(period, _sun.SunsetOn(_clock.DayOfYear, _clock.UtcOffset));

        private void ApplyChristmas(bool christmas)
        {
            if (_floodLights != null)
            {
                _floodLights.Boost = christmas ? _christmasFloodBoost : 1f;
                _floodLights.Tint = christmas ? _christmasFloodColor : (Color?)null;
            }

            if (_umbrellas != null)
            {
                _umbrellas.Folded = christmas;
            }

            if (_festiveLights != null)
            {
                _festiveLights.Show(christmas);
            }
        }
    }
}
