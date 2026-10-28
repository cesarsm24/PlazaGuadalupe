using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>Piezas móviles del modelo de paloma, que anima <see cref="PigeonFlock"/>.</summary>
    public sealed class PigeonModel : MonoBehaviour
    {
        [SerializeField] private Transform _leftWing;
        [SerializeField] private Transform _rightWing;

        public Transform LeftWing => _leftWing;

        public Transform RightWing => _rightWing;
    }
}
