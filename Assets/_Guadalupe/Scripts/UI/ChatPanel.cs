using System.Text;
using Guadalupe.Core;
using Guadalupe.Dialogue;
using Guadalupe.NPC;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Guadalupe.UI
{
    /// <summary>
    /// Panel del chat con un personaje principal: su nombre y papel, la conversación y el campo para escribir.
    /// La respuesta aparece poco a poco según llega del <see cref="ConversationDirector"/>; mientras no llega nada,
    /// unos puntos indican que está pensando. Enter envía y la acción de cerrar (Esc, la misma que abre el menú)
    /// termina la conversación aunque el campo no tenga el foco.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ChatPanel : MonoBehaviour
    {
        private const string HiddenClass = "chat-panel--hidden";
        private const string Placeholder = "Escribe lo que quieras decirle…";

        /// <summary>Respuesta pendiente de mostrar a partir de la que se acelera, para no quedarse atrás.</summary>
        private const int CatchUpCharacters = 120;

        /// <summary>Milisegundos entre los pulsos de los puntos de «pensando».</summary>
        private const long TypingPulseMs = 450;

        [SerializeField] private ConversationDirector _director;
        [Tooltip("Acción con la que se termina la conversación (la de abrir el menú: Esc o Start).")]
        [SerializeField] private InputActionReference _closeAction;

        private VisualElement _root;
        private VisualElement _panel;
        private Label _name;
        private Label _role;
        private ScrollView _log;
        private ScrollView _inputBox;
        private TextField _input;
        private TextElement _inputText;
        private Button _send;
        private Label _typing;
        private IVisualElementScheduledItem _typingPulse;
        private Label _reply;
        private readonly StringBuilder _pending = new StringBuilder();
        private float _revealed;
        private bool _closeRequested;
        private bool _followEnd = true;

        public bool IsOpen => _panel != null && !_panel.ClassListContains(HiddenClass);

        private void OnEnable()
        {
            if (!MissingReferences.Check(this, (_director, "el director de la conversación"), (_closeAction, "la acción de cerrar")))
            {
                enabled = false;
                return;
            }

            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.AddToClassList("chat-root");
            _root.pickingMode = PickingMode.Ignore;
            _panel = _root.Q("chatPanel");
            _name = _root.Q<Label>("chatName");
            _role = _root.Q<Label>("chatRole");
            _log = _root.Q<ScrollView>("chatLog");
            _inputBox = _root.Q<ScrollView>("chatInputBox");
            _input = _root.Q<TextField>("chatInput");
            _send = _root.Q<Button>("chatSend");
            _input.maxLength = ConversationDirector.MaxPlayerText;
            _input.textEdition.placeholder = Placeholder;
            // El campo crece con el texto dentro de chatInputBox, que tiene una altura máxima y se desplaza: el
            // desplazamiento propio del campo recorta el texto en lugar de desplazarlo.
            _input.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _inputText = _input.Q<TextElement>();
            _input.RegisterValueChangedCallback(_ => FitInput());
            _inputText.RegisterCallback<GeometryChangedEvent>(OnInputResized);
            _input.RegisterCallback<FocusInEvent>(_ => _inputBox.AddToClassList("chat-input-box--focused"));
            _input.RegisterCallback<FocusOutEvent>(_ => _inputBox.RemoveFromClassList("chat-input-box--focused"));

            _send.clicked += Send;
            _input.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            _log.contentContainer.RegisterCallback<GeometryChangedEvent>(ScrollToEnd);
            _log.verticalScroller.valueChanged += OnLogScrolled;

            _director.Started += OnStarted;
            _director.ReplyStarted += OnReplyStarted;
            _director.ReplyText += OnReplyText;
            _director.ReplyFinished += OnReplyFinished;
            _director.ReplyFailed += OnReplyFailed;
            _director.Ended += OnEnded;
            _closeAction.action.performed += OnClose;
            _closeAction.action.Enable();
            _panel.AddToClassList(HiddenClass);
            _panel.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.Started -= OnStarted;
                _director.ReplyStarted -= OnReplyStarted;
                _director.ReplyText -= OnReplyText;
                _director.ReplyFinished -= OnReplyFinished;
                _director.ReplyFailed -= OnReplyFailed;
                _director.Ended -= OnEnded;
            }

            if (_send != null)
            {
                _send.clicked -= Send;
            }

            if (_closeAction != null)
            {
                _closeAction.action.performed -= OnClose;
            }
        }

        private void Update()
        {
            // Se cierra aquí y no al pulsar: el menú escucha la misma tecla y debe ver aún la conversación abierta.
            if (_closeRequested)
            {
                _closeRequested = false;
                _director.End();
            }

            if (_reply == null || _pending.Length == 0)
            {
                return;
            }

            // Se muestra al ritmo al que habla el personaje, más deprisa si llega mucho texto de golpe.
            float speed = ConversationDirector.CharactersPerSecond * (1f + _pending.Length / (float)CatchUpCharacters);
            _revealed += speed * Time.unscaledDeltaTime;
            int count = Mathf.Min((int)_revealed, _pending.Length);
            if (count > 0)
            {
                _reply.text += _pending.ToString(0, count);
                _pending.Remove(0, count);
                _revealed -= count;
            }
        }

        private void OnStarted(MainCharacter character)
        {
            _log.Clear();
            _followEnd = true;
            _pending.Clear();
            _reply = null;
            _name.text = character.Profile.DisplayName;
            _role.text = character.Profile.Role;
            _input.value = "";
            // Es del mapa del jugador, que se desactiva mientras se conversa.
            _closeAction.action.Enable();
            _panel.style.display = DisplayStyle.Flex;
            // Un fotograma después, para que la transición de entrada se vea y la tecla que abre el chat no se
            // escriba en el campo.
            _panel.schedule.Execute(() =>
            {
                _panel.RemoveFromClassList(HiddenClass);
                _input.Focus();
            }).ExecuteLater(50);
        }

        private void OnEnded()
        {
            HideTyping();
            _input.Blur();
            _panel.AddToClassList(HiddenClass);
            _panel.schedule.Execute(() =>
            {
                if (!IsOpen)
                {
                    _panel.style.display = DisplayStyle.None;
                }
            }).ExecuteLater(300);
        }

        private void OnReplyStarted()
        {
            FlushReply();
            _send.SetEnabled(false);
            ShowTyping();
        }

        private void OnReplyText(string text)
        {
            if (_reply == null)
            {
                HideTyping();
                _reply = AddMessage("", "chat-message", "chat-message--character");
            }

            _pending.Append(text);
        }

        private void OnReplyFinished(string reply)
        {
            if (_reply == null)
            {
                HideTyping();
                _reply = AddMessage("", "chat-message", "chat-message--character");
                _pending.Append(reply);
            }

            _send.SetEnabled(true);
        }

        private void OnReplyFailed(string line, string notice)
        {
            FlushReply();
            HideTyping();
            if (!string.IsNullOrEmpty(line))
            {
                AddMessage(line, "chat-message", "chat-message--character");
            }

            AddMessage(notice, "chat-notice");
            _send.SetEnabled(true);
        }

        private void Send()
        {
            string text = _input.value.Trim();
            if (text.Length > 0 && _director.Partner != null && !_director.Waiting)
            {
                // Antes de enviarlo, para que los puntos de «pensando» queden debajo.
                FlushReply();
                _followEnd = true;
                Label message = AddMessage(text, "chat-message", "chat-message--player");
                if (_director.Send(text))
                {
                    _input.value = "";
                }
                else
                {
                    message.RemoveFromHierarchy();
                }
            }

            _input.Focus();
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            // Enter envía (con Mayúsculas, salto de línea). El campo recibe la tecla y el carácter por separado.
            bool enter = evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == '\n' || evt.character == '\r';
            if (!enter || evt.shiftKey)
            {
                return;
            }

            if (evt.keyCode != KeyCode.None)
            {
                Send();
            }

            evt.StopImmediatePropagation();
            _input.focusController?.IgnoreEvent(evt);
        }

        private void OnClose(InputAction.CallbackContext context) => _closeRequested = _director.Partner != null;

        /// <summary>Muestra de golpe lo que quede de la respuesta anterior.</summary>
        private void FlushReply()
        {
            if (_reply != null && _pending.Length > 0)
            {
                _reply.text += _pending.ToString();
            }

            _pending.Clear();
            _revealed = 0f;
            _reply = null;
        }

        private Label AddMessage(string text, params string[] classNames)
        {
            var message = new Label(text) { enableRichText = false };
            foreach (string className in classNames)
            {
                message.AddToClassList(className);
            }

            _log.Add(message);
            return message;
        }

        private void ShowTyping()
        {
            if (_typing != null)
            {
                return;
            }

            _typing = new Label("···");
            _typing.AddToClassList("chat-typing");
            _log.Add(_typing);
            _typingPulse = _typing.schedule.Execute(() => _typing?.ToggleInClassList("chat-typing--dim")).Every(TypingPulseMs);
        }

        private void HideTyping()
        {
            _typingPulse?.Pause();
            _typingPulse = null;
            _typing?.RemoveFromHierarchy();
            _typing = null;
        }

        /// <summary>
        /// Ajusta la altura del campo a su texto con el ancho que tiene (la medida propia del campo se queda corta con
        /// varias líneas) y, si se escribe al final, baja el recuadro para que se vea lo que se escribe.
        /// </summary>
        private void FitInput()
        {
            float width = _inputText.layout.width;
            if (float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            string text = string.IsNullOrEmpty(_input.value) ? " " : _input.value;
            Vector2 size = _inputText.MeasureTextSize(text, width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
            _inputText.style.minHeight = size.y;
            if (_input.cursorIndex >= _input.value.Length - 1)
            {
                _inputBox.schedule.Execute(() => _inputBox.scrollOffset = new Vector2(0f, _inputBox.verticalScroller.highValue));
            }
        }

        private void OnInputResized(GeometryChangedEvent evt)
        {
            if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width))
            {
                FitInput();
            }
        }

        // Si el jugador sube para releer, la conversación deja de bajar sola hasta que vuelva abajo o envíe algo.
        private void OnLogScrolled(float value) => _followEnd = value >= _log.verticalScroller.highValue - 4f;

        private void ScrollToEnd(GeometryChangedEvent evt)
        {
            if (_followEnd)
            {
                _log.scrollOffset = new Vector2(0f, Mathf.Max(0f, _log.contentContainer.layout.height - _log.contentViewport.layout.height));
            }
        }
    }
}
