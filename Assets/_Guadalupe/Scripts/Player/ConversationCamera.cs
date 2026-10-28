using Guadalupe.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Guadalupe.Player
{
    /// <summary>
    /// Encuadre de las conversaciones: mientras el jugador habla con un personaje, una cámara por encima de su
    /// hombro mira al personaje y el jugador se vuelve hacia él. Al terminar, el Cinemachine Brain vuelve con una
    /// transición suave a la cámara de seguimiento.
    /// </summary>
    /// <remarks>
    /// La cámara (<see cref="_camera"/>) no lleva componentes de posición ni de mira: este componente la coloca
    /// cada fotograma y la activa solo durante la conversación. Si algo tapa el hombro derecho (una columna de los
    /// soportales, una pared) usa el izquierdo, y si también está tapado se acerca hasta el obstáculo.
    /// </remarks>
    public sealed class ConversationCamera : MonoBehaviour
    {
        [SerializeField] private SessionState _session;
        [Tooltip("Cámara de la conversación, desactivada hasta que empieza.")]
        [SerializeField] private CinemachineCamera _camera;

        [Header("Encuadre")]
        [Tooltip("Altura de los ojos del jugador sobre sus pies (m).")]
        [SerializeField] private float _eyeHeight = 1.6f;
        [Tooltip("Distancia detrás del jugador, hacia el lado y hacia arriba (m).")]
        [SerializeField] private Vector3 _shoulderOffset = new Vector3(0.55f, 0.15f, -1.2f);
        [Tooltip("Hacia dónde mira, entre los ojos del jugador (0) y la cabeza del personaje (1).")]
        [SerializeField, Range(0f, 1f)] private float _lookBias = 0.8f;
        [Tooltip("Grados por segundo con los que el jugador se vuelve hacia el personaje.")]
        [SerializeField] private float _turnDegreesPerSecond = 360f;
        [Tooltip("Capas que tapan la cámara (edificios y mobiliario).")]
        [SerializeField] private LayerMask _obstacles = 1;
        [Tooltip("Radio de la cámara al comprobar si choca (m).")]
        [SerializeField] private float _cameraRadius = 0.2f;

        private Transform _partner;

        private void OnEnable()
        {
            if (!MissingReferences.Check(this, (_session, "el estado de la sesión"), (_camera, "la cámara de la conversación")))
            {
                enabled = false;
                return;
            }

            _session.ConversationChanged += OnConversationChanged;
            OnConversationChanged(_session.ConversationPartner);
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.ConversationChanged -= OnConversationChanged;
            }

            OnConversationChanged(null);
        }

        private void OnConversationChanged(Transform partner)
        {
            _partner = partner;
            if (_camera == null)
            {
                return;
            }

            if (partner != null)
            {
                // Colocada antes de activarla, para que la transición parta del sitio correcto.
                Place();
                _camera.PreviousStateIsValid = false;
            }

            _camera.gameObject.SetActive(partner != null);
        }

        private void LateUpdate()
        {
            if (_partner == null)
            {
                return;
            }

            Vector3 toPartner = _partner.position - transform.position;
            toPartner.y = 0f;
            if (toPartner.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toPartner),
                    _turnDegreesPerSecond * Time.deltaTime);
            }

            Place();
        }

        /// <summary>Coloca la cámara por encima del hombro del jugador, mirando al personaje.</summary>
        private void Place()
        {
            Vector3 eyes = transform.position + Vector3.up * _eyeHeight;
            Vector3 forward = _partner.position - eyes;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.01f ? forward.normalized : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 back = Vector3.up * _shoulderOffset.y + forward * _shoulderOffset.z;
            Vector3 position = Clear(eyes, eyes + right * _shoulderOffset.x + back, out float rightGap);
            if (rightGap < 1f)
            {
                Vector3 left = Clear(eyes, eyes - right * _shoulderOffset.x + back, out float leftGap);
                if (leftGap > rightGap)
                {
                    position = left;
                }
            }

            Vector3 target = Vector3.Lerp(eyes, _partner.position, _lookBias);
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        }

        /// <summary>
        /// Lo más cerca de <paramref name="wanted"/> que se puede poner la cámara sin atravesar nada desde
        /// <paramref name="from"/>; <paramref name="gap"/> es la fracción del recorrido que queda libre (1 = todo).
        /// </summary>
        private Vector3 Clear(Vector3 from, Vector3 wanted, out float gap)
        {
            Vector3 path = wanted - from;
            float length = path.magnitude;
            if (Physics.SphereCast(from, _cameraRadius, path / length, out RaycastHit hit, length, _obstacles, QueryTriggerInteraction.Ignore))
            {
                gap = hit.distance / length;
                return from + path / length * hit.distance;
            }

            gap = 1f;
            return wanted;
        }
    }
}
