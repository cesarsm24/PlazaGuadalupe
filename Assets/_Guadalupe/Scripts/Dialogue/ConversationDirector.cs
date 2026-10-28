using System;
using System.Threading;
using Guadalupe.Core;
using Guadalupe.NPC;
using UnityEngine;

namespace Guadalupe.Dialogue
{
    /// <summary>
    /// Lleva la conversación del jugador con un personaje principal: pide las respuestas al modelo, las pasa a la
    /// interfaz según llegan y, al terminar, resume lo que ha contado el jugador para los demás personajes.
    /// </summary>
    public sealed class ConversationDirector : MonoBehaviour
    {
        /// <summary>Velocidad a la que se muestra (y se pronuncia) la respuesta, en caracteres por segundo.</summary>
        public const float CharactersPerSecond = 45f;

        /// <summary>Longitud máxima de lo que escribe el jugador.</summary>
        public const int MaxPlayerText = 300;

        [SerializeField] private SessionState _session;
        [Tooltip("Reloj de la simulación, para decirle al personaje la hora.")]
        [SerializeField] private SimulationClock _clock;
        [SerializeField] private ConversationRules _rules;

        private Conversation _conversation;
        private CancellationTokenSource _cancellation;
        private float _talkingUntil;

        /// <summary>Al empezar una conversación, con el personaje.</summary>
        public event Action<MainCharacter> Started;

        /// <summary>Al pedir una respuesta: el personaje está pensando.</summary>
        public event Action ReplyStarted;

        /// <summary>Cada trozo de la respuesta según llega.</summary>
        public event Action<string> ReplyText;

        /// <summary>Al terminar la respuesta, con el texto completo.</summary>
        public event Action<string> ReplyFinished;

        /// <summary>Si no hay respuesta: frase del personaje (o null) y aviso para el jugador.</summary>
        public event Action<string, string> ReplyFailed;

        /// <summary>Al terminar la conversación.</summary>
        public event Action Ended;

        /// <summary>Personaje con el que se habla, o null.</summary>
        public MainCharacter Partner { get; private set; }

        /// <summary>True mientras se espera una respuesta.</summary>
        public bool Waiting => _conversation != null && _conversation.Waiting;

        private void OnEnable()
        {
            if (!MissingReferences.Check(this, (_session, "el estado de la sesión"), (_rules, "las reglas de conversación")))
            {
                enabled = false;
            }
        }

        private void OnDisable() => End();

        private void Update()
        {
            if (Partner != null && _talkingUntil > 0f && Time.time >= _talkingUntil)
            {
                _talkingUntil = 0f;
                Partner.SetTalking(false);
            }
        }

        /// <summary>
        /// Empieza a hablar con <paramref name="character"/>. Abre al momento con una de sus frases, elegida según la
        /// hora, el día, la gente y el nombre del jugador (<see cref="MainCharacter.OpeningLine"/>); desde ahí responde
        /// el modelo. Si no tiene frases, la primera también la genera el modelo.
        /// </summary>
        public void Begin(MainCharacter character)
        {
            if (!isActiveAndEnabled || Partner != null || character == null || character.Profile == null)
            {
                return;
            }

            Partner = character;
            character.BeginConversation();
            _session.ConversationPartner = character.Head;
            _cancellation = new CancellationTokenSource();
            Started?.Invoke(character);

            if (!LlmSettings.TryLoad(out LlmSettings settings, out string problem))
            {
                Debug.LogWarning($"No se puede conversar: {problem}", this);
                ReplyFailed?.Invoke(UnavailableLine(character), problem);
                return;
            }

            ILanguageModel model = new GeminiClient(settings);

            string name = character.Profile.DisplayName;
            _conversation = new Conversation(model, PromptBuilder.Instructions(_rules, character.Profile, Context(name)));
            _session.MarkTalkedTo(name);
            string opening = character.OpeningLine();
            if (opening != null)
            {
                _conversation.OpenWith(opening);
                Speak(opening);
                ReplyFinished?.Invoke(opening);
                return;
            }

            Conversation conversation = _conversation;
            _ = ReplyAsync(token => conversation.OpenAsync(OnText, token), true);
        }

        /// <summary>
        /// El jugador dice <paramref name="text"/>. Devuelve false si no se puede enviar (vacío, sin conversación o
        /// esperando otra respuesta).
        /// </summary>
        public bool Send(string text)
        {
            text = text?.Trim();
            if (Partner == null || _conversation == null || _conversation.Waiting || string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (text.Length > MaxPlayerText)
            {
                text = text.Substring(0, MaxPlayerText);
            }

            Partner.Nod();
            Conversation conversation = _conversation;
            _ = ReplyAsync(token => conversation.SayAsync(text, OnText, token), false);
            return true;
        }

        /// <summary>Termina la conversación: se cancela la respuesta pendiente y el personaje se despide.</summary>
        public void End()
        {
            if (Partner == null)
            {
                return;
            }

            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = null;
            _talkingUntil = 0f;

            MainCharacter partner = Partner;
            Conversation conversation = _conversation;
            Partner = null;
            _conversation = null;
            partner.EndConversation();
            _session.ConversationPartner = null;
            if (conversation != null && conversation.PlayerSpoke)
            {
                _ = RememberAsync(conversation, partner.Profile.DisplayName);
            }

            Ended?.Invoke();
        }

        private async Awaitable ReplyAsync(Func<CancellationToken, Awaitable<string>> generate, bool opening)
        {
            MainCharacter partner = Partner;
            ReplyStarted?.Invoke();
            try
            {
                string reply = await generate(_cancellation.Token);
                if (Partner != partner)
                {
                    return;
                }

                ReplyFinished?.Invoke(reply);
                if (ReplyGestures.GivesDirections(reply))
                {
                    partner.Point();
                }
            }
            catch (OperationCanceledException)
            {
                // Se ha terminado la conversación mientras respondía.
            }
            catch (LanguageModelException exception)
            {
                Debug.LogWarning($"Sin respuesta de {partner.Profile.DisplayName}: {exception.Message}", this);
                if (Partner == partner)
                {
                    _talkingUntil = Time.time;
                    ReplyFailed?.Invoke(opening ? UnavailableLine(partner) : null, Notice(exception.Error));
                }
            }
        }

        private void OnText(string text)
        {
            if (Partner != null)
            {
                Speak(text);
            }
        }

        /// <summary>Pasa <paramref name="text"/> a la interfaz y gesticula mientras se va mostrando.</summary>
        private void Speak(string text)
        {
            if (_talkingUntil <= 0f)
            {
                Partner.SetTalking(true);
            }

            _talkingUntil = Mathf.Max(_talkingUntil, Time.time) + text.Length / CharactersPerSecond;
            ReplyText?.Invoke(text);
        }

        private async Awaitable RememberAsync(Conversation conversation, string characterName)
        {
            try
            {
                VisitNote note = await conversation.SummarizeAsync(characterName, destroyCancellationToken);
                if (note == null)
                {
                    return;
                }

                if (note.PlayerName != null)
                {
                    _session.PlayerName = note.PlayerName;
                }

                _session.AddNote(note.Note);
            }
            catch (OperationCanceledException)
            {
                // Se ha cerrado la escena antes de terminar.
            }
            catch (LanguageModelException exception)
            {
                Debug.LogWarning($"No se ha podido resumir la conversación con {characterName}: {exception.Message}", this);
            }
        }

        private ConversationContext Context(string characterName)
        {
            ScenarioDefinition scenario = _session.Scenario;
            float hour = _clock != null ? _clock.Hour : 12f;
            return new ConversationContext(hour,
                scenario != null ? scenario.Occasion : ScenarioOccasion.Normal,
                scenario != null ? scenario.CrowdAt(hour) : 0f,
                !_session.HasTalkedTo(characterName), _session.PlayerName, _session.Notes);
        }

        private string UnavailableLine(MainCharacter character) =>
            GreetingRules.PickLine(character.Profile.LinesFor(LineMoment.Unavailable), _session.PlayerName, Array.Empty<string>(),
                () => UnityEngine.Random.value);

        /// <summary>Aviso para el jugador cuando no hay respuesta.</summary>
        internal static string Notice(LanguageModelError error) => error switch
        {
            LanguageModelError.Network => "No hay conexión a internet. Prueba otra vez.",
            LanguageModelError.Timeout => "El servicio tarda demasiado en responder. Prueba otra vez.",
            LanguageModelError.Rejected => $"La clave de la API o el modelo no son válidos: revisa {LlmSettings.FileName}.",
            LanguageModelError.Quota => "Se ha llegado al límite de peticiones de la clave. Espera un poco.",
            LanguageModelError.Server => "El servicio no está disponible ahora mismo. Prueba otra vez.",
            LanguageModelError.Blocked => "Mejor hablad de otra cosa.",
            _ => "No ha llegado respuesta. Prueba otra vez.",
        };
    }
}
