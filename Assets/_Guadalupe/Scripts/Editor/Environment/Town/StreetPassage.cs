using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Paso de una calle bajo una casa (arco), abierto en la planta baja a lo largo de una polilínea.</summary>
    public sealed class StreetPassage
    {
        public StreetPassage(string name, float width, float clearHeight, params Vector2[] line)
        {
            Name = name;
            Width = width;
            ClearHeight = clearHeight;
            Line = line;
        }

        public string Name { get; }

        public float Width { get; }

        /// <summary>Altura libre en la clave del arco.</summary>
        public float ClearHeight { get; }

        /// <summary>Eje del paso.</summary>
        public Vector2[] Line { get; }

        /// <summary>
        /// Margen a cada lado del arco en el que también se abre la planta baja: ahí van las pilastras de la
        /// portada, de modo que ninguna casa vecina tapa medio arco donde la calle se quiebra.
        /// </summary>
        public const float PierMargin = 0.95f;

        /// <summary>Indica si un punto está dentro del paso (incluido el sitio de las pilastras).</summary>
        public bool Contains(Vector2 p)
        {
            return ArcadeBands.TryLateral(Line, p, out float lateral, out _) && Mathf.Abs(lateral) <= Width * 0.5f + PierMargin;
        }
    }
}
