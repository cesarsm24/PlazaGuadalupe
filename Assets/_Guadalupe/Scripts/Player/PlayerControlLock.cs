using Guadalupe.Core;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Guadalupe.Player
{
    /// <summary>
    /// Bloquea el control del jugador mientras el menú principal está abierto o mientras conversa con un personaje:
    /// desactiva la entrada y los controles adicionales, detiene el movimiento en curso y libera el cursor.
    /// </summary>
    [RequireComponent(typeof(StarterAssetsInputs))]
    public sealed class PlayerControlLock : MonoBehaviour
    {
        [SerializeField] private SessionState _session;

        private StarterAssetsInputs _inputs;
        private PlayerInput _playerInput;
        private Behaviour[] _extraControls;

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();
            _extraControls = new Behaviour[] { GetComponent<KeyboardCameraControls>(), GetComponent<PlayerViewModeSwitcher>() };
        }

        private void OnEnable()
        {
            if (!MissingReferences.Check(this, (_session, "el estado de la sesión")))
            {
                return;
            }

            _session.MenuChanged += OnMenuChanged;
            _session.ConversationChanged += OnConversationChanged;
            Apply();
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.MenuChanged -= OnMenuChanged;
                _session.ConversationChanged -= OnConversationChanged;
            }
        }

        private void OnMenuChanged(bool inMenu) => Apply();

        private void OnConversationChanged(Transform partner) => Apply();

        private void Apply()
        {
            bool locked = _session.InMenu || _session.InConversation;
            if (_playerInput != null)
            {
                _playerInput.enabled = !locked;
            }

            foreach (Behaviour control in _extraControls)
            {
                if (control != null)
                {
                    control.enabled = !locked;
                }
            }

            // Sin entrada activa, StarterAssetsInputs conservaría el último valor y el personaje seguiría andando.
            _inputs.MoveInput(Vector2.zero);
            _inputs.LookInput(Vector2.zero);
            _inputs.SprintInput(false);
            _inputs.JumpInput(false);

            _inputs.cursorLocked = !locked;
            _inputs.cursorInputForLook = !locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }
}
