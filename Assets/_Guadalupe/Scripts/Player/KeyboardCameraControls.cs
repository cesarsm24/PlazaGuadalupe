using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Guadalupe.Player
{
    /// <summary>
    /// Giro de la cámara con el teclado (Q/E), útil con trackpad, añadido a los controles de Starter Assets.
    /// </summary>
    /// <remarks>
    /// Recibe la acción <c>TurnCamera</c> del <see cref="PlayerInput"/> (comportamiento Send Messages) y escribe
    /// en <see cref="StarterAssetsInputs"/> en lugar de modificar el paquete de terceros. El controlador no escala
    /// la entrada de mirar por el tiempo de fotograma, así que aquí se multiplica por <see cref="Time.deltaTime"/>
    /// para que el giro no dependa de los FPS.
    /// </remarks>
    [RequireComponent(typeof(StarterAssetsInputs))]
    public sealed class KeyboardCameraControls : MonoBehaviour
    {
        [SerializeField, Tooltip("Grados por segundo al girar con el teclado.")]
        private float _turnSpeed = 120f;

        private StarterAssetsInputs _inputs;
        private float _turn;
        private bool _wasTurning;

        private void Awake() => _inputs = GetComponent<StarterAssetsInputs>();

        private void OnDisable() => _turn = 0f;

        /// <summary>Mensaje del <see cref="PlayerInput"/> para la acción <c>TurnCamera</c>.</summary>
        private void OnTurnCamera(InputValue value) => _turn = value.Get<float>();

        private void Update()
        {
            if (Mathf.Abs(_turn) > 0.01f)
            {
                _inputs.LookInput(new Vector2(_turn * _turnSpeed * Time.deltaTime, 0f));
                _wasTurning = true;
            }
            else if (_wasTurning)
            {
                // Se detiene el giro al soltar la tecla; el ratón vuelve a mandar su propia entrada.
                _inputs.LookInput(Vector2.zero);
                _wasTurning = false;
            }
        }
    }
}
