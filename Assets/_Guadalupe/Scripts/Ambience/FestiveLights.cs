using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Iluminación navideña de la plaza. Muestra u oculta los adornos según el escenario y, mientras están
    /// visibles, enciende de forma gradual las luces al ponerse el sol.
    /// </summary>
    /// <remarks>
    /// Las luces usan un shader aditivo, así que con intensidad 0 desaparecen, salvo las que tienen un color de
    /// bombillas apagadas en su <see cref="FestiveBulbs"/> (el árbol de día). La intensidad se aplica con un
    /// <see cref="MaterialPropertyBlock"/> para no modificar los materiales compartidos. Con los adornos visibles
    /// también se ocultan las mesas junto al árbol; el resto de cambios de la Navidad (focos y sombrillas) los
    /// aplica <see cref="ScenarioApplier"/>.
    /// </remarks>
    public sealed class FestiveLights : SunDrivenLights
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private GameObject _decorations;
        [SerializeField] private Renderer[] _lights;
        [SerializeField] private Color[] _colors;
        [Tooltip("Objetos de la escena que se ocultan mientras están los adornos (p. ej. mesas junto al árbol).")]
        [SerializeField] private GameObject[] _hiddenWhenShown;
        [Tooltip("Objetos de la escena que solo se muestran con los adornos (p. ej. el árbol de luces).")]
        [SerializeField] private GameObject[] _shownWithDecorations;

        private MaterialPropertyBlock _block;

        public bool Shown => _decorations != null && _decorations.activeSelf;

        protected override bool IsActive => Shown;

        /// <summary>
        /// Asigna el sol, el grupo de adornos, las luces con su color pleno y los objetos de la escena que se
        /// ocultan o se muestran con ellos. Lo llama el generador del entorno.
        /// </summary>
        public void Configure(SunController sun, GameObject decorations, Renderer[] lights, Color[] colors,
            GameObject[] hiddenWhenShown, GameObject[] shownWithDecorations)
        {
            _decorations = decorations;
            _lights = lights;
            _colors = colors;
            _hiddenWhenShown = hiddenWhenShown;
            _shownWithDecorations = shownWithDecorations;
            SetSun(sun);
        }

        /// <summary>Muestra u oculta los adornos y los objetos que dependen de ellos.</summary>
        public void Show(bool show)
        {
            if (_decorations != null)
            {
                _decorations.SetActive(show);
            }

            foreach (GameObject hidden in _hiddenWhenShown ?? System.Array.Empty<GameObject>())
            {
                if (hidden != null)
                {
                    hidden.SetActive(!show);
                }
            }

            foreach (GameObject shown in _shownWithDecorations ?? System.Array.Empty<GameObject>())
            {
                if (shown != null)
                {
                    shown.SetActive(show);
                }
            }

            Refresh();
        }

        private void Reset() => SetElevations(6f, -3f);

        protected override void ApplyLevel(float level)
        {
            if (_lights == null || _colors == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            for (int i = 0; i < _lights.Length && i < _colors.Length; i++)
            {
                if (_lights[i] == null)
                {
                    continue;
                }

                Color unlit = Color.black;
                if (_lights[i].TryGetComponent(out FestiveBulbs bulbs))
                {
                    unlit = bulbs.UnlitColor;
                    bulbs.SetGlow(level > OnThreshold ? level : 0f);
                }

                _lights[i].GetPropertyBlock(_block);
                _block.SetColor(ColorId, Color.Lerp(unlit, _colors[i], level));
                _lights[i].SetPropertyBlock(_block);
                _lights[i].enabled = level > OnThreshold || unlit.maxColorComponent > 0f;
            }
        }
    }
}
