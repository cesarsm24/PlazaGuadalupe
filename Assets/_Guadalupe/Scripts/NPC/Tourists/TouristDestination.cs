using System;
using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Tipo de visita que hacen los turistas a un punto de interés: qué hacen allí, cuánto tiempo, con quién y si
    /// se sientan. Lo usa <see cref="TouristSpawner"/> para repartirlos por la plaza.
    /// </summary>
    /// <remarks>
    /// Los sitios del punto de interés se agrupan en bloques consecutivos de <see cref="BlockSize"/> (las sillas de
    /// una mesa, las dos personas de una charla) y un grupo ocupa sitios de un mismo bloque libre. Con bloques de un
    /// solo sitio (miradores), el grupo entero se para en ese sitio, uno al lado del otro.
    /// </remarks>
    [Serializable]
    public sealed class TouristDestination
    {
        [SerializeField] private string _name = "Destino";
        [SerializeField] private PointOfInterest _place;
        [Tooltip("Punto al que miran desde su sitio; vacío = hacia donde mira el sitio.")]
        [SerializeField] private Transform _lookAt;
        [Tooltip("Actividades posibles; se elige una al azar (repetir una la hace más probable).")]
        [SerializeField] private TouristActivity[] _activities = { TouristActivity.LookAround };
        [Tooltip("Probabilidad relativa de elegir este destino.")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Segundos mínimos y máximos que pasan en el sitio.")]
        [SerializeField] private Vector2 _stay = new Vector2(10f, 25f);
        [Tooltip("Sitios consecutivos que forman un bloque (mesa, pareja).")]
        [SerializeField, Min(1)] private int _blockSize = 1;
        [Tooltip("Personas mínimas y máximas de un grupo (en un bloque, o en fila en un mismo sitio).")]
        [SerializeField] private Vector2Int _groupSize = Vector2Int.one;
        [Tooltip("Los sitios son sillas: se colocan delante, de espaldas al asiento, y se sientan.")]
        [SerializeField] private bool _seated;

        public string Name => _name;

        public PointOfInterest Place => _place;

        public Transform LookAt => _lookAt;

        public TouristActivity[] Activities => _activities;

        public float Weight => _weight;

        public Vector2 Stay => _stay;

        public int BlockSize => Mathf.Max(1, _blockSize);

        public int MinGroup => Mathf.Clamp(_groupSize.x, 1, BlockSize);

        public int MaxGroup => Mathf.Clamp(_groupSize.y, MinGroup, BlockSize);

        public bool Seated => _seated;

        /// <summary>El grupo entero se para en un único sitio, uno al lado del otro.</summary>
        public bool SideBySide => BlockSize == 1 && !_seated;

        /// <summary>Indica si un grupo de <paramref name="size"/> personas puede hacer esta visita.</summary>
        public bool Accepts(int size) => _weight > 0f && size >= MinGroup && size <= (SideBySide ? Mathf.Max(1, _groupSize.y) : MaxGroup);

        /// <summary>Sitios del punto de interés.</summary>
        public Transform[] Spots => _place != null ? _place.Spots : Array.Empty<Transform>();
    }
}
