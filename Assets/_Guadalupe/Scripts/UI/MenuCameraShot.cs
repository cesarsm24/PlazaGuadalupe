using UnityEngine;

namespace Guadalupe.UI
{
    /// <summary>
    /// Encuadre de la cámara del menú con un balanceo horizontal lento: la cámara está donde se coloca este objeto
    /// en la escena y mira hacia <see cref="_lookAt"/>. La cámara de Cinemachine del mismo objeto toma el control
    /// mientras está activa y, al desactivarla, el Brain vuelve a la del jugador con una transición suave.
    /// </summary>
    public sealed class MenuCameraShot : MonoBehaviour
    {
        [Tooltip("Punto al que mira la cámara.")]
        [SerializeField] private Transform _lookAt;
        [Tooltip("Amplitud del balanceo a cada lado (grados).")]
        [SerializeField] private float _swayDegrees = 5f;
        [Tooltip("Duración de un balanceo completo (s).")]
        [SerializeField, Min(1f)] private float _swayPeriod = 45f;

        private float _startTime;

        private void OnEnable()
        {
            _startTime = Time.unscaledTime;
            Aim();
        }

        private void Update() => Aim();

        private void Aim()
        {
            if (_lookAt == null)
            {
                return;
            }

            float phase = (Time.unscaledTime - _startTime) / _swayPeriod * 2f * Mathf.PI;
            Quaternion look = Quaternion.LookRotation(_lookAt.position - transform.position);
            transform.rotation = Quaternion.Euler(0f, Mathf.Sin(phase) * _swayDegrees, 0f) * look;
        }
    }
}
