using System;
using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Orienta la luz direccional como el sol según la hora del <see cref="SimulationClock"/> y ajusta su
    /// intensidad, su color y la luz ambiental. Indica además si es de noche.
    /// </summary>
    /// <remarks>
    /// La posición del sol se calcula con <see cref="SolarPosition"/> para la latitud y longitud de Guadalupe, de
    /// modo que la elevación es continua: el anochecer pasa por el crepúsculo antes de la noche.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(Light))]
    public sealed class SunController : MonoBehaviour
    {
        [SerializeField] private SimulationClock _clock;

        [Header("Lugar")]
        [SerializeField] private float _latitude = 39.45f;
        [SerializeField] private float _longitude = -5.33f;

        [Header("Luz")]
        [SerializeField] private float _maxIntensity = 1.3f;
        [Tooltip("Elevación (grados) a partir de la que el sol da la intensidad máxima.")]
        [SerializeField, Min(0.1f)] private float _fullIntensityElevation = 15f;
        [SerializeField] private Color _noonColor = new Color(1f, 0.96f, 0.9f);
        [SerializeField] private Color _lowSunColor = new Color(1f, 0.80f, 0.62f);
        [Tooltip("Elevación (grados) a partir de la que la luz ya tiene el color del mediodía.")]
        [SerializeField, Min(0.1f)] private float _noonColorElevation = 35f;
        [Tooltip("Elevación (grados) por debajo de la que es de noche: final del crepúsculo civil.")]
        [SerializeField] private float _nightBelowElevation = -6f;

        [Header("Luz ambiental")]
        [Tooltip("Colores ambientales de día (modo Trilight): cielo, horizonte y suelo.")]
        [SerializeField] private Color _skyAmbient = new Color(0.72f, 0.8f, 0.94f);
        [SerializeField] private Color _equatorAmbient = new Color(0.8f, 0.72f, 0.62f);
        [SerializeField] private Color _groundAmbient = new Color(0.5f, 0.44f, 0.38f);
        [Tooltip("Fracción de la luz ambiental y de los reflejos del cielo que queda de noche; de día es 1.")]
        [SerializeField, Range(0f, 1f)] private float _nightAmbient = 0.7f;
        [Tooltip("Grados que sube el sol desde la elevación de noche hasta que la luz ambiental es la de día.")]
        [SerializeField, Min(0.1f)] private float _ambientRampDegrees = 16f;

        /// <summary>Elevación mínima de la luz: por debajo, las sombras serían rasantes e inestables.</summary>
        private const float MinLightElevation = 2f;

        private Light _light;
        private bool? _isNight;

        public event Action Changed;

        /// <summary>Se lanza al empezar o acabar la noche; el argumento indica si es de noche.</summary>
        public event Action<bool> NightChanged;

        public SimulationClock Clock => _clock;

        /// <summary>Elevación del sol sobre el horizonte, en grados (negativa de noche).</summary>
        public float Elevation { get; private set; }

        /// <summary>Hora de la puesta del sol en Guadalupe para una fecha.</summary>
        public float SunsetOn(int dayOfYear, float utcOffset)
        {
            SolarPosition.Daylight(dayOfYear, utcOffset, _latitude, _longitude, out _, out float sunset);
            return sunset;
        }

        public bool IsNight => _isNight ?? false;

        private void OnEnable()
        {
            _light = GetComponent<Light>();
            if (_clock != null)
            {
                _clock.Changed += Apply;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (_clock != null)
            {
                _clock.Changed -= Apply;
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Durante OnValidate no conviene tocar otros objetos (activarlos o cambiar sus componentes da
            // avisos): el sol y quienes lo siguen se actualizan justo después.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled)
                {
                    _light = GetComponent<Light>();
                    Apply();
                }
            };
#endif
        }

        private void Apply()
        {
            if (_clock == null || _light == null)
            {
                return;
            }

            SolarPosition.Compute(_clock.Hour, _clock.DayOfYear, _clock.UtcOffset, _latitude, _longitude,
                out float elevation, out float azimuth);
            Elevation = elevation;

            float elevationRad = Mathf.Max(elevation, MinLightElevation) * Mathf.Deg2Rad;
            float azimuthRad = azimuth * Mathf.Deg2Rad;
            var towardSun = new Vector3(
                Mathf.Sin(azimuthRad) * Mathf.Cos(elevationRad),
                Mathf.Sin(elevationRad),
                Mathf.Cos(azimuthRad) * Mathf.Cos(elevationRad));
            transform.rotation = Quaternion.LookRotation(-towardSun);

            _light.intensity = _maxIntensity * Mathf.Clamp01(elevation / _fullIntensityElevation);
            _light.color = Color.Lerp(_lowSunColor, _noonColor, Mathf.Clamp01(elevation / _noonColorElevation));

            // De noche se reduce la luz ambiental para que destaquen los faroles. En modo Trilight la intensidad
            // ambiental no tiene efecto, así que se escalan los colores. Los reflejos del cielo se capturan de día y,
            // sin bajarlos, el agua y las superficies brillantes reflejarían un cielo claro.
            float day = Mathf.Clamp01((elevation - _nightBelowElevation) / _ambientRampDegrees);
            float ambient = Mathf.Lerp(_nightAmbient, 1f, day);
            RenderSettings.ambientSkyColor = _skyAmbient * ambient;
            RenderSettings.ambientEquatorColor = _equatorAmbient * ambient;
            RenderSettings.ambientGroundColor = _groundAmbient * ambient;
            RenderSettings.reflectionIntensity = ambient;

            Changed?.Invoke();
            bool night = elevation < _nightBelowElevation;
            if (_isNight != night)
            {
                _isNight = night;
                NightChanged?.Invoke(night);
            }
        }
    }
}
