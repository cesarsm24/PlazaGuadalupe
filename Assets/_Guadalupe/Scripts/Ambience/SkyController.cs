using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Adapta el cielo a la elevación del sol: exposición del skybox procedural, tinte de la cúpula de nubes
    /// (blanco de día, cálido al atardecer y gris azulado de noche) y, ya de noche, el fondo estrellado de la cúpula.
    /// </summary>
    [ExecuteAlways]
    public sealed class SkyController : MonoBehaviour
    {
        [SerializeField] private SunController _sun;
        [SerializeField] private Renderer _clouds;
        [SerializeField] private Material _skybox;

        [Header("Tinte de las nubes")]
        [SerializeField] private Color _nightTint = DefaultNightTint;
        [SerializeField] private Color _noonTint = Color.white;
        [Tooltip("Tinte con el sol a unos 4° sobre el horizonte.")]
        [SerializeField] private Color _sunsetTint = DefaultSunsetTint;

        private static readonly Color DefaultNightTint = new Color(0.16f, 0.18f, 0.24f, 0.7f);
        private static readonly Color DefaultSunsetTint = new Color(1f, 0.72f, 0.55f, 1f);
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int NightId = Shader.PropertyToID("_Night");
        private MaterialPropertyBlock _block;
        private Material _runtimeSky;
        private float _lastElevation = float.NaN;
        private SunController _subscribed;

        private void OnEnable()
        {
            if (_sun != null)
            {
                _subscribed = _sun;
                _subscribed.Changed += Apply;
            }

            _lastElevation = float.NaN;
            Apply();
        }

        private void OnDisable()
        {
            if (_subscribed != null)
            {
                _subscribed.Changed -= Apply;
                _subscribed = null;
            }
        }

        private void Apply()
        {
            if (_sun == null || Mathf.Approximately(_sun.Elevation, _lastElevation))
            {
                return;
            }

            _lastElevation = _sun.Elevation;
            (Color tint, float exposure) = SkyFor(_lastElevation, _nightTint, _noonTint, _sunsetTint);
            if (_clouds != null)
            {
                _block ??= new MaterialPropertyBlock();
                _clouds.GetPropertyBlock(_block);
                _block.SetColor(TintId, tint);
                _block.SetFloat(NightId, NightFor(_lastElevation));
                _clouds.SetPropertyBlock(_block);
            }

            if (_skybox != null && Application.isPlaying)
            {
                // En juego se trabaja sobre una copia para no modificar el material guardado en el proyecto.
                if (_runtimeSky == null)
                {
                    _runtimeSky = new Material(_skybox);
                    RenderSettings.skybox = _runtimeSky;
                }

                _runtimeSky.SetFloat(ExposureId, exposure);
            }
        }

        /// <summary>
        /// Tinte de las nubes y exposición del cielo para una elevación del sol: de día pesa el blanco del mediodía,
        /// alrededor de 4° el tono cálido del atardecer y por debajo de −4° el gris azulado de la noche.
        /// </summary>
        internal static (Color Tint, float Exposure) SkyFor(float elevation, Color night, Color noon, Color sunset)
        {
            // day: 0 con el sol 4° bajo el horizonte, 1 a partir de 10°.
            // golden: máximo con el sol a 4° y nulo a más de 10° de esa altura.
            float day = Mathf.Clamp01((elevation + 4f) / 14f);
            float golden = Mathf.Clamp01(1f - Mathf.Abs(elevation - 4f) / 10f);
            return (Color.Lerp(night, Color.Lerp(noon, sunset, golden), day), Mathf.Lerp(0.08f, 1.15f, day));
        }

        /// <summary>
        /// Peso del cielo de noche (azul marino con estrellas): 0 con el sol a 2° bajo el horizonte y 1 a partir de
        /// 10° bajo él, cuando ya no queda luz del crepúsculo.
        /// </summary>
        internal static float NightFor(float elevation) => Mathf.Clamp01((-2f - elevation) / 8f);

        private void OnDestroy()
        {
            if (_runtimeSky != null)
            {
                Destroy(_runtimeSky);
            }
        }
    }
}
