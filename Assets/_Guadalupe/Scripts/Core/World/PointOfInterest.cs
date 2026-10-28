using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Punto de interés colocado en la escena: lugar al que van los turistas. Los puntos de espera son hijos de este
    /// objeto.
    /// </summary>
    public sealed class PointOfInterest : MonoBehaviour
    {
        [Tooltip("Posiciones en las que esperan o se detienen las personas.")]
        [SerializeField] private Transform[] _spots;

        public Transform[] Spots => _spots;
    }
}
