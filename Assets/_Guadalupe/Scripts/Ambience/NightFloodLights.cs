using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Iluminación monumental nocturna (fachada del monasterio y fuente). Sus luces se encienden de forma
    /// gradual al ponerse el sol, cada una hasta su intensidad plena.
    /// </summary>
    public sealed class NightFloodLights : SunDrivenLights
    {
        [SerializeField] private Light[] _lights;
        [SerializeField] private float[] _intensities;

        private float _boost = 1f;
        private Color? _tint;
        private Color[] _colors;

        /// <summary>
        /// Multiplicador de la intensidad (1 = la generada). La iluminación navideña lo sube para bañar el
        /// monasterio de luz dorada.
        /// </summary>
        public float Boost
        {
            get => _boost;
            set
            {
                _boost = Mathf.Max(0f, value);
                Refresh();
            }
        }

        /// <summary>
        /// Color de los proyectores mientras tiene valor; sin valor, el suyo. La iluminación navideña los dora. No
        /// cambia las luces que no son proyectores (las de la fuente).
        /// </summary>
        public Color? Tint
        {
            get => _tint;
            set
            {
                _tint = value;
                Refresh();
            }
        }

        /// <summary>Asigna el sol y las luces con su intensidad plena. Lo llama el generador del entorno.</summary>
        public void Configure(SunController sun, Light[] lights, float[] intensities)
        {
            _lights = lights;
            _intensities = intensities;
            SetSun(sun);
        }

        private void Reset() => SetElevations(4f, -4f);

        protected override void ApplyLevel(float level)
        {
            if (_lights == null || _intensities == null)
            {
                return;
            }

            if (_colors == null || _colors.Length != _lights.Length)
            {
                _colors = new Color[_lights.Length];
                for (int i = 0; i < _lights.Length; i++)
                {
                    _colors[i] = _lights[i] != null ? _lights[i].color : Color.white;
                }
            }

            for (int i = 0; i < _lights.Length && i < _intensities.Length; i++)
            {
                if (_lights[i] == null)
                {
                    continue;
                }

                _lights[i].enabled = level > OnThreshold;
                _lights[i].intensity = _intensities[i] * level * _boost;
                _lights[i].color = _tint.HasValue && _lights[i].type == LightType.Spot ? _tint.Value : _colors[i];
            }
        }
    }
}
