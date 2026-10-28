using System.Collections.Generic;
using Guadalupe.Core;
using Guadalupe.Dialogue;
using Guadalupe.NPC;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Guadalupe.UI
{
    /// <summary>
    /// Indicaciones de los personajes principales: un bocadillo discreto sobre la cabeza de los que están a la vista,
    /// el aviso de la tecla para hablar con el que está al lado y de cara, y lo que dicen al saludar. Al pulsar la
    /// tecla empieza la conversación (tras el apretón de manos, si es de darla). Solo se ve durante la visita y
    /// fuera de las conversaciones.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class InteractionHud : MonoBehaviour
    {
        [SerializeField] private SessionState _session;
        [SerializeField] private InputActionReference _interactAction;
        [SerializeField] private ConversationDirector _conversation;
        [SerializeField] private Animator _player;
        [Tooltip("Controles del jugador que se pausan mientras da la mano.")]
        [SerializeField] private Behaviour[] _playerControls;

        private readonly Dictionary<MainCharacter, VisualElement> _bubbles = new Dictionary<MainCharacter, VisualElement>();
        private VisualElement _root;
        private VisualElement _prompt;
        private Label _promptText;
        private Label _subtitle;
        private MainCharacter _target;
        private Camera _camera;

        private bool Visiting => _session != null && _session.Started && !_session.InMenu;

        private void OnEnable()
        {
            if (!MissingReferences.Check(this, (_session, "el estado de la sesión"), (_interactAction, "la acción de hablar"),
                    (_conversation, "el director de la conversación")))
            {
                enabled = false;
                return;
            }

            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.AddToClassList("hud-root");
            _root.pickingMode = PickingMode.Ignore;
            _prompt = _root.Q("hudPrompt");
            _promptText = _root.Q<Label>("hudPromptText");
            _subtitle = _root.Q<Label>("hudSubtitle");
            _root.Q<Label>("hudKey").text = KeyName();
            _bubbles.Clear();

            _interactAction.action.performed += OnInteract;
            _interactAction.action.Enable();
        }

        private void OnDisable()
        {
            if (_interactAction != null)
            {
                _interactAction.action.performed -= OnInteract;
            }

            if (_session != null)
            {
                _session.Player = null;
            }
        }

        private void LateUpdate()
        {
            // Con el menú abierto los personajes no se fijan en el jugador (ni gastan el saludo).
            _session.Player = Visiting && _player != null ? _player.transform : null;
            // Durante la conversación todo está en el panel del chat.
            bool visiting = Visiting && !_session.InConversation;
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Camera camera = _camera;
            _target = null;
            MainCharacter speaker = null;
            float nearestTarget = float.MaxValue;
            float nearestSpeaker = float.MaxValue;

            foreach (MainCharacter character in MainCharacter.All)
            {
                float distance = _player != null ? Vector3.Distance(_player.transform.position, character.transform.position) : float.MaxValue;
                bool inReach = character.Engagement == Engagement.InReach && !character.IsBusy;
                if (visiting && inReach && distance < nearestTarget)
                {
                    nearestTarget = distance;
                    _target = character;
                }

                if (visiting && character.CurrentLine != null && distance < nearestSpeaker)
                {
                    nearestSpeaker = distance;
                    speaker = character;
                }

                bool noticed = character.Engagement == Engagement.Noticed || character.Engagement == Engagement.Greeted;
                PlaceBubble(Bubble(character), character, camera, visiting && noticed && !character.IsBusy);
            }

            Show(_prompt, _target != null);
            if (_target != null)
            {
                _promptText.text = $"Hablar con {_target.Profile.DisplayName}";
            }

            Show(_subtitle, speaker != null);
            if (speaker != null)
            {
                _subtitle.text = $"<b>{speaker.Profile.DisplayName}:</b> {speaker.CurrentLine}";
            }
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            if (Visiting && !_session.InConversation && _target != null)
            {
                MainCharacter character = _target;
                character.Interact(_player, _playerControls, () => _conversation.Begin(character));
            }
        }

        private VisualElement Bubble(MainCharacter character)
        {
            if (!_bubbles.TryGetValue(character, out VisualElement bubble))
            {
                bubble = new Label("···") { pickingMode = PickingMode.Ignore };
                bubble.AddToClassList("hud-bubble");
                bubble.AddToClassList("hud-bubble--hidden");
                _root.Insert(0, bubble);
                _bubbles.Add(character, bubble);
            }

            return bubble;
        }

        /// <summary>Coloca el bocadillo sobre la cabeza; se desvanece si no toca verlo o queda detrás de la cámara.</summary>
        private void PlaceBubble(VisualElement bubble, MainCharacter character, Camera camera, bool visible)
        {
            if (camera != null && visible)
            {
                Vector3 screen = camera.WorldToScreenPoint(character.HeadTop);
                visible = screen.z > 0f;
                if (visible)
                {
                    // La pantalla tiene el origen abajo; el panel, arriba.
                    Vector2 position = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(screen.x, Screen.height - screen.y));
                    bubble.style.left = position.x;
                    bubble.style.top = position.y;
                }
            }

            bubble.EnableInClassList("hud-bubble--hidden", !visible);
        }

        private static void Show(VisualElement element, bool visible) => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>Tecla de la acción de hablar tal como la muestra el sistema de entrada (F).</summary>
        private string KeyName()
        {
            InputAction action = _interactAction != null ? _interactAction.action : null;
            int binding = action != null ? action.GetBindingIndex(InputBinding.MaskByGroup("KeyboardMouse")) : -1;
            return binding >= 0 ? action.GetBindingDisplayString(binding) : "F";
        }
    }
}
