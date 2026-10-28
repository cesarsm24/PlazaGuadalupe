using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Tramo de soportal definido por una polilínea que deja los edificios a su derecha.</summary>
    public sealed class ArcadeSegment
    {
        public ArcadeSegment(string name, bool raised, bool wrapCorners, Vector2[] line)
        {
            Name = name;
            Raised = raised;
            WrapCorners = wrapCorners;
            Line = line;
        }

        public string Name { get; }

        /// <summary>Indica si el suelo del soportal está elevado y lleva escalones.</summary>
        public bool Raised { get; }

        /// <summary>Indica si en las casas de esquina el soportal da la vuelta al lateral.</summary>
        public bool WrapCorners { get; }

        /// <summary>Polilínea del frente del soportal.</summary>
        public Vector2[] Line { get; }

        /// <summary>Distancia de un punto a la polilínea.</summary>
        public float DistanceTo(Vector2 p) => PlanarGeometry.ClosestOnPolyline(Line, p).Distance;
    }
}
