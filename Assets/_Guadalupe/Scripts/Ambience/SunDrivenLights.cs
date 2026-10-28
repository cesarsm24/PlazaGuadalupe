using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Base de los grupos de luces que se encienden de forma gradual al ponerse el sol: calcula un nivel entre 0
    /// (apagadas) y 1 (intensidad plena) a partir de la elevación del <see cref="SunController"/> y lo aplica
    /// cuando cambia.
    /// </summary>
    [ExecuteAlways]
    public abstract class SunDrivenLights : MonoBehaviour
    {
        [SerializeField] private SunController _sun;
        [Tooltip("Elevación del sol (grados) por debajo de la que empiezan a encenderse.")]
        [SerializeField] private float _onBelowElevation = 8f;
        [Tooltip("Elevación del sol (grados) a la que alcanzan la intensidad plena.")]
        [SerializeField] private float _fullBelowElevation = -2f;

        /// <summary>Diferencia mínima de nivel que se aplica, para no reasignar las luces sin necesidad.</summary>
        private const float LevelStep = 0.01f;

        /// <summary>Nivel por debajo del cual una luz se apaga en lugar de quedarse con intensidad casi nula.</summary>
        protected const float OnThreshold = 0.01f;

        private float _lastLevel = -1f;
        private SunController _subscribed;

        protected SunController Sun => _sun;

        /// <summary>Asigna el sol y vuelve a aplicar el nivel.</summary>
        protected void SetSun(SunController sun)
        {
            _sun = sun;
            Subscribe();
            Refresh();
        }

        /// <summary>Fija las elevaciones de inicio y de intensidad plena del encendido.</summary>
        protected void SetElevations(float onBelow, float fullBelow)
        {
            _onBelowElevation = onBelow;
            _fullBelowElevation = fullBelow;
        }

        /// <summary>Fuerza a aplicar el nivel actual aunque no haya cambiado.</summary>
        protected void Refresh()
        {
            _lastLevel = -1f;
            UpdateLevel();
        }

        /// <summary>Indica si hay que aplicar el nivel (por ejemplo, solo con los adornos visibles).</summary>
        protected virtual bool IsActive => true;

        /// <summary>Aplica a las luces un nivel entre 0 y 1.</summary>
        protected abstract void ApplyLevel(float level);

        protected virtual void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        protected virtual void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            Unsubscribe();
            if (isActiveAndEnabled && _sun != null)
            {
                _subscribed = _sun;
                _subscribed.Changed += UpdateLevel;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed != null)
            {
                _subscribed.Changed -= UpdateLevel;
                _subscribed = null;
            }
        }

        private void UpdateLevel()
        {
            if (_sun == null || !IsActive)
            {
                return;
            }

            float level = LevelFor(_sun.Elevation, _onBelowElevation, _fullBelowElevation);
            if (Mathf.Abs(level - _lastLevel) < LevelStep)
            {
                return;
            }

            _lastLevel = level;
            ApplyLevel(level);
        }

        /// <summary>
        /// Nivel de encendido para una elevación del sol: 0 por encima de <paramref name="onBelow"/>, 1 por debajo de
        /// <paramref name="fullBelow"/> y una rampa lineal entre las dos.
        /// </summary>
        internal static float LevelFor(float elevation, float onBelow, float fullBelow) =>
            Mathf.InverseLerp(onBelow, fullBelow, elevation);
    }
}
