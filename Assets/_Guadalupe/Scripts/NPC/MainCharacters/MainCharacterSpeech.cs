using System.Collections.Generic;
using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Lo que dice un personaje principal fuera del chat: el saludo según el momento, frases sueltas y la frase con la
    /// que abre la conversación, sin repetir las últimas que ha dicho.
    /// </summary>
    internal sealed class MainCharacterSpeech
    {
        private readonly NpcProfile _profile;
        private readonly SessionState _session;
        private readonly SimulationClock _clock;
        private readonly float _seenRecentlySeconds;
        private readonly Queue<string> _recent = new Queue<string>();
        private string _line;
        private float _lineUntil;
        private float _seenAt = float.NegativeInfinity;

        public MainCharacterSpeech(NpcProfile profile, SessionState session, SimulationClock clock, float seenRecentlySeconds)
        {
            _profile = profile;
            _session = session;
            _clock = clock;
            _seenRecentlySeconds = seenRecentlySeconds;
        }

        public string CurrentLine => Time.time < _lineUntil ? _line : null;

        public bool HasLinesFor(LineMoment moment) => _profile.LinesFor(moment).Count > 0;

        /// <summary>Le saluda con una frase que encaje con el momento, o solo con el gesto si no tiene ninguna.</summary>
        public void Greet()
        {
            bool firstTime = !_session.HasMet(_profile.DisplayName);
            bool seenRecently = Time.time - _seenAt < _seenRecentlySeconds;
            Show(GreetingRules.Pick(Context(firstTime, seenRecently), _profile.LinesFor, _recent, () => Random.value));
            _seenAt = Time.time;
            _session.MarkMet(_profile.DisplayName);
        }

        /// <summary>
        /// Frase con la que empieza a conversar, como un saludo de quien ya le conoce. Null si no tiene frases.
        /// </summary>
        public string OpeningLine()
        {
            string line = GreetingRules.Pick(Context(false, false), _profile.LinesFor, _recent, () => Random.value);
            Remember(line);
            return line;
        }

        public void Say(LineMoment moment) =>
            Show(GreetingRules.PickLine(_profile.LinesFor(moment), _session.PlayerName, _recent, () => Random.value));

        public void SayGoodbye()
        {
            // Recién despedido, si vuelve a verle solo le dice algo breve.
            _seenAt = Time.time;
            Say(LineMoment.Farewell);
        }

        private GreetingContext Context(bool firstTime, bool seenRecently)
        {
            ScenarioDefinition scenario = _session.Scenario;
            float hour = _clock.Hour;
            return new GreetingContext(firstTime, seenRecently, _session.PlayerName, hour,
                scenario != null ? scenario.Occasion : ScenarioOccasion.Normal, scenario != null ? scenario.CrowdAt(hour) : 0f);
        }

        private void Show(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            _line = line;
            _lineUntil = Time.time + GreetingRules.Duration(line);
            Remember(line);
        }

        private void Remember(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            _recent.Enqueue(line);
            while (_recent.Count > GreetingRules.RecentLines)
            {
                _recent.Dequeue();
            }
        }
    }
}
