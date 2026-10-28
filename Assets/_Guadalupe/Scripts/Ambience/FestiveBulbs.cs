using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Marca un renderer de bombillas navideñas dentro de un prefab y el color con que lo enciende
    /// <see cref="FestiveLights"/>. Opcionalmente las bombillas se ven también apagadas y se acompañan de un resplandor
    /// sobre el suelo.
    /// </summary>
    /// <remarks>
    /// El resplandor es un renderer aditivo y no una luz: el suelo es un terreno grande que ya recibe muchas luces y
    /// URP solo aplica unas pocas por objeto, así que una luz puntual junto al árbol no llegaría a verse.
    /// </remarks>
    [RequireComponent(typeof(Renderer))]
    public sealed class FestiveBulbs : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField, ColorUsage(false, true)]
        [Tooltip("Color e intensidad (HDR) de las bombillas encendidas.")]
        private Color _color = Color.white;

        [SerializeField, ColorUsage(false)]
        [Tooltip("Color de las bombillas apagadas, de día; en negro no se ven.")]
        private Color _unlitColor = Color.black;

        [SerializeField]
        [Tooltip("Resplandor sobre el suelo que se enciende con las bombillas (opcional).")]
        private Renderer _glow;

        [SerializeField, ColorUsage(false, true)] private Color _glowColor = new Color(2f, 1.45f, 0.75f);

        private MaterialPropertyBlock _block;

        public Color Color => _color;

        public Color UnlitColor => _unlitColor;

        /// <summary>Enciende el resplandor con un nivel entre 0 (apagado) y 1 (intensidad plena).</summary>
        public void SetGlow(float level)
        {
            if (_glow == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            _glow.GetPropertyBlock(_block);
            _block.SetColor(ColorId, _glowColor * level);
            _glow.SetPropertyBlock(_block);
            _glow.enabled = level > 0f;
        }
    }
}
