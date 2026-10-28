using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Balcón de hierro construido: centro del pasamanos, dirección de la fachada, normal exterior y ancho.</summary>
    internal readonly struct BalconyAnchor
    {
        public BalconyAnchor(Vector3 railCenter, Vector2 along, Vector2 outward, float width)
        {
            RailCenter = railCenter;
            Along = along;
            Outward = outward;
            Width = width;
        }

        /// <summary>Centro del pasamanos delantero (mundo).</summary>
        public Vector3 RailCenter { get; }

        /// <summary>Dirección horizontal de la fachada (X, Z).</summary>
        public Vector2 Along { get; }

        /// <summary>Normal exterior de la fachada (X, Z).</summary>
        public Vector2 Outward { get; }

        /// <summary>Ancho del balcón (m).</summary>
        public float Width { get; }
    }
}
