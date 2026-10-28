using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Enciende de forma gradual los faroles, su cristal y las ventanas iluminadas según la elevación del sol.
    /// </summary>
    /// <remarks>
    /// En juego, los renderers de <see cref="_emissiveRenderers"/> pasan a usar copias de los materiales de
    /// ventanas y cristales para variar su emisión sin modificar los recursos del proyecto.
    /// </remarks>
    public sealed class StreetLights : SunDrivenLights
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private float _maxIntensity = 18f;
        [Tooltip("Emisión del cristal de los faroles de noche.")]
        [SerializeField, ColorUsage(false, true)] private Color _lanternEmission = new Color(1f, 0.78f, 0.45f) * 2.5f;
        [Tooltip("Emisión de las ventanas iluminadas de noche.")]
        [SerializeField, ColorUsage(false, true)] private Color _windowEmission = new Color(1f, 0.72f, 0.4f) * 1.4f;
        [Tooltip("Luces de los faroles de pared y de las farolas.")]
        [SerializeField] private Light[] _lights;
        [Tooltip("Material de las ventanas iluminadas, cuya emisión sube de noche.")]
        [SerializeField] private Material _windowLit;
        [Tooltip("Material del cristal de los faroles, cuya emisión sube de noche.")]
        [SerializeField] private Material _lanternGlass;
        [Tooltip("Renderers que usan alguno de los dos materiales anteriores.")]
        [SerializeField] private Renderer[] _emissiveRenderers;

        private Material _windows;
        private Material _lanterns;

        /// <summary>
        /// Asigna el sol, las luces, los materiales que brillan de noche y los renderers que los usan. Lo llama el
        /// generador del entorno.
        /// </summary>
        public void Configure(SunController sun, Light[] lights, Material windowLit, Material lanternGlass,
            Renderer[] emissiveRenderers)
        {
            _lights = lights;
            _windowLit = windowLit;
            _lanternGlass = lanternGlass;
            _emissiveRenderers = emissiveRenderers;
            SetSun(sun);
        }

        private void Reset() => SetElevations(8f, -2f);

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _windows = _windowLit != null ? new Material(_windowLit) : null;
            _lanterns = _lanternGlass != null ? new Material(_lanternGlass) : null;
            if (_windows != null)
            {
                _windows.EnableKeyword("_EMISSION");
            }

            if (_lanterns != null)
            {
                _lanterns.EnableKeyword("_EMISSION");
            }

            foreach (Renderer r in _emissiveRenderers ?? System.Array.Empty<Renderer>())
            {
                if (r == null)
                {
                    continue;
                }

                Material[] materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == _windowLit && _windows != null)
                    {
                        materials[i] = _windows;
                    }
                    else if (materials[i] == _lanternGlass && _lanterns != null)
                    {
                        materials[i] = _lanterns;
                    }
                }

                r.sharedMaterials = materials;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_windows != null)
            {
                Destroy(_windows);
            }

            if (_lanterns != null)
            {
                Destroy(_lanterns);
            }
        }

        protected override void ApplyLevel(float level)
        {
            if (_lanterns != null)
            {
                _lanterns.SetColor(EmissionId, _lanternEmission * level);
            }

            if (_windows != null)
            {
                _windows.SetColor(EmissionId, _windowEmission * level);
            }

            foreach (Light light in _lights ?? System.Array.Empty<Light>())
            {
                if (light == null)
                {
                    continue;
                }

                light.enabled = level > OnThreshold;
                light.intensity = _maxIntensity * level;
            }
        }
    }
}
