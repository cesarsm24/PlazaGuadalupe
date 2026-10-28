using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Parte de edificio afectada por una franja de soportal o por un paso de calle bajo ella.</summary>
    internal sealed class ArcadePart
    {
        /// <summary>Planta baja que queda tras recortar la franja (null si queda abierta entera).</summary>
        public List<Vector2> Ground;

        /// <summary>Tramo de soportal al que pertenece (null si solo la cruza un paso de calle).</summary>
        public ArcadeSegment Segment;

        /// <summary>Altura del techo del soportal o del paso.</summary>
        public float Top;

        public int Floors;

        /// <summary>Soportal de arcos (no adintelado).</summary>
        public bool Arched;

        /// <summary>Paso bajo la casa (arco de calle), si lo hay.</summary>
        public StreetPassage Passage;
    }
}
