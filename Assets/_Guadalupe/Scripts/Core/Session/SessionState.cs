using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Estado de la visita en curso que comparten los módulos: escenario y franja horaria elegidos, si el menú está
    /// abierto, con quién está hablando el jugador y lo que los personajes saben de él (su nombre, a quién ha
    /// conocido y lo que ha contado). Es un recurso del proyecto que se asigna en el Inspector a quien lo necesita.
    /// </summary>
    /// <remarks>
    /// Los valores son de ejecución: no se guardan en el recurso y se reinician al cargarlo y al entrar en juego.
    /// Cada evento se lanza solo cuando el valor cambia.
    /// </remarks>
    [CreateAssetMenu(menuName = "Guadalupe/Estado de la sesión", fileName = "SO_Session")]
    public sealed class SessionState : ScriptableObject
    {
        [NonSerialized] private ScenarioDefinition _scenario;
        [NonSerialized] private DayPeriod _period = DayPeriod.Afternoon;
        [NonSerialized] private bool _inMenu;
        [NonSerialized] private bool _started;
        [NonSerialized] private Transform _conversationPartner;
        [NonSerialized] private readonly HashSet<string> _met = new HashSet<string>();
        [NonSerialized] private readonly HashSet<string> _talkedTo = new HashSet<string>();
        [NonSerialized] private readonly List<string> _notes = new List<string>();

        public event Action<ScenarioDefinition> ScenarioChanged;

        public event Action<DayPeriod> PeriodChanged;

        /// <summary>El argumento indica si el menú queda abierto.</summary>
        public event Action<bool> MenuChanged;

        /// <summary>Se lanza al empezar o terminar una conversación; el argumento es la cabeza del personaje o null.</summary>
        public event Action<Transform> ConversationChanged;

        /// <summary>Null hasta que se elige uno.</summary>
        public ScenarioDefinition Scenario
        {
            get => _scenario;
            set
            {
                if (_scenario == value)
                {
                    return;
                }

                _scenario = value;
                ScenarioChanged?.Invoke(value);
            }
        }

        public DayPeriod Period
        {
            get => _period;
            set
            {
                if (_period == value)
                {
                    return;
                }

                _period = value;
                PeriodChanged?.Invoke(value);
            }
        }

        /// <summary>True mientras el menú principal está abierto: el jugador no se controla.</summary>
        public bool InMenu
        {
            get => _inMenu;
            set
            {
                if (_inMenu == value)
                {
                    return;
                }

                _inMenu = value;
                MenuChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// Cabeza del personaje con el que habla el jugador (para encuadrarle), o null si no está hablando con nadie.
        /// </summary>
        public Transform ConversationPartner
        {
            get => _conversationPartner;
            set
            {
                if (_conversationPartner == value)
                {
                    return;
                }

                _conversationPartner = value;
                ConversationChanged?.Invoke(value);
            }
        }

        public bool InConversation => _conversationPartner != null;

        /// <summary>True desde que se empieza la primera visita de la sesión.</summary>
        public bool Started => _started;

        /// <summary>
        /// Jugador en el que se pueden fijar los personajes: null con el menú abierto. Lo asigna la interfaz.
        /// </summary>
        public Transform Player { get; set; }

        /// <summary>Nombre con el que se ha presentado el jugador, o null si no lo ha dicho.</summary>
        public string PlayerName { get; set; }

        public bool HasMet(string who) => _met.Contains(who);

        public void MarkMet(string who) => _met.Add(who);

        public bool HasTalkedTo(string who) => _talkedTo.Contains(who);

        public void MarkTalkedTo(string who) => _talkedTo.Add(who);

        /// <summary>Lo que el jugador ha contado o preguntado en las conversaciones, una nota por conversación.</summary>
        public IReadOnlyList<string> Notes => _notes;

        /// <summary>Apunta algo que ha contado el jugador para que lo sepan los demás personajes.</summary>
        public void AddNote(string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
            {
                _notes.Add(note.Trim());
            }
        }

        /// <summary>Empieza la visita con el escenario y la franja elegidos y cierra el menú.</summary>
        public void StartVisit()
        {
            _started = true;
            InMenu = false;
        }

        /// <summary>Vuelve a los valores iniciales sin lanzar eventos.</summary>
        public void ResetState()
        {
            _scenario = null;
            _period = DayPeriod.Afternoon;
            _inMenu = false;
            _started = false;
            _conversationPartner = null;
            Player = null;
            PlayerName = null;
            _met.Clear();
            _talkedTo.Clear();
            _notes.Clear();
        }

        private void OnEnable() => ResetState();
    }
}
