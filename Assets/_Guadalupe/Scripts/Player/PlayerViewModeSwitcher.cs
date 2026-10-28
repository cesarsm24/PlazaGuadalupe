using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Guadalupe.Player
{
    /// <summary>Alterna la cámara del jugador entre tercera y primera persona con una transición suave.</summary>
    /// <remarks>
    /// Reutiliza la misma cámara de seguimiento (<see cref="CinemachineThirdPersonFollow"/>) y solo
    /// interpola su distancia y desplazamiento: así el control del personaje no cambia entre modos.
    /// En primera persona el cuerpo deja de dibujarse pero sigue proyectando sombra. Recibe la acción
    /// <c>ToggleView</c> del <see cref="PlayerInput"/> (comportamiento Send Messages).
    /// </remarks>
    public sealed class PlayerViewModeSwitcher : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private CinemachineThirdPersonFollow _follow;
        [SerializeField] private Renderer[] _bodyRenderers = Array.Empty<Renderer>();

        [Header("Tercera persona")]
        [SerializeField] private float _thirdPersonDistance = 4f;
        [SerializeField] private Vector3 _thirdPersonShoulder = new Vector3(1f, 0f, 0f);

        [Header("Primera persona")]
        [Tooltip("Desplazamiento desde la raíz de cámara (1,375 m) hasta los ojos.")]
        [SerializeField] private Vector3 _firstPersonShoulder = new Vector3(0f, 0.25f, 0.2f);

        [Header("Transición")]
        [SerializeField, Min(0.01f)] private float _transitionSeconds = 0.3f;

        private float _blend;

        /// <summary>Indica si la cámara está (o está pasando a) primera persona.</summary>
        public bool IsFirstPerson { get; private set; }

        public void Toggle() => IsFirstPerson = !IsFirstPerson;

        /// <summary>Mensaje del <see cref="PlayerInput"/> para la acción <c>ToggleView</c>.</summary>
        private void OnToggleView(InputValue value)
        {
            if (value.isPressed)
            {
                Toggle();
            }
        }

        private void Update()
        {
            float target = IsFirstPerson ? 1f : 0f;
            if (Mathf.Approximately(_blend, target) || _follow == null)
            {
                return;
            }

            _blend = Mathf.MoveTowards(_blend, target, Time.deltaTime / _transitionSeconds);
            float t = Mathf.SmoothStep(0f, 1f, _blend);
            _follow.CameraDistance = Mathf.Lerp(_thirdPersonDistance, 0f, t);
            _follow.ShoulderOffset = Vector3.Lerp(_thirdPersonShoulder, _firstPersonShoulder, t);

            // El cuerpo se oculta al final de la transición para no verlo desde dentro.
            ShadowCastingMode mode = _blend > 0.8f ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            foreach (Renderer bodyRenderer in _bodyRenderers)
            {
                if (bodyRenderer != null)
                {
                    bodyRenderer.shadowCastingMode = mode;
                }
            }
        }
    }
}
