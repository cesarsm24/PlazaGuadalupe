using System;
using System.Collections.Generic;
using System.Text;
using Guadalupe.Core;
using Guadalupe.NPC;
using UnityEngine;

namespace Guadalupe.Dialogue
{
    /// <summary>Momento de la visita en el que empieza una conversación, tal como se le cuenta al personaje.</summary>
    public readonly struct ConversationContext
    {
        public ConversationContext(float hour, ScenarioOccasion occasion, float crowd, bool firstMeeting, string playerName,
            IReadOnlyList<string> notes)
        {
            Hour = hour;
            Occasion = occasion;
            Crowd = crowd;
            FirstMeeting = firstMeeting;
            PlayerName = playerName;
            Notes = notes ?? Array.Empty<string>();
        }

        /// <summary>Hora del día (0-24).</summary>
        public float Hour { get; }

        public ScenarioOccasion Occasion { get; }

        /// <summary>Afluencia de la plaza (0-1).</summary>
        public float Crowd { get; }

        /// <summary>Si es la primera vez que hablan.</summary>
        public bool FirstMeeting { get; }

        /// <summary>Nombre del jugador, o null si no lo ha dicho.</summary>
        public string PlayerName { get; }

        /// <summary>Lo que el jugador ha contado a otros personajes.</summary>
        public IReadOnlyList<string> Notes { get; }
    }

    /// <summary>
    /// Texto que recibe el modelo de lenguaje: quién es el personaje (su perfil), las reglas comunes, el momento de
    /// la visita y lo que ya se sabe del jugador. También la petición para resumir una conversación al terminarla.
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>Mensajes de la conversación que se envían como mucho; los anteriores se olvidan.</summary>
        public const int MaxMessages = 16;

        /// <summary>Lo que se le dice al personaje cuando el jugador se acerca a hablar, para que empiece él.</summary>
        public const string Opening = "(El visitante se acerca a hablar contigo. Salúdale y empieza tú, en una o dos frases.)";

        private const float BusyFrom = 0.25f; // afluencia con bastante gente

        /// <summary>Instrucciones del personaje para toda la conversación.</summary>
        public static string Instructions(ConversationRules rules, NpcProfile profile, ConversationContext context)
        {
            var text = new StringBuilder(4096);
            text.Append("Eres ").Append(profile.DisplayName).Append(". ").Append(profile.Role).AppendLine();
            text.AppendLine("Hablas con un visitante en la plaza de Santa María de Guadalupe. Escribe solo lo que dices en voz alta: " +
                            "sin acotaciones, asteriscos, emojis ni formato.");

            Section(text, "Cómo eres", profile.Personality);
            Section(text, "Dónde estás", profile.Whereabouts);
            Section(text, "De qué sueles hablar", profile.Topics);
            List(text, "Lo que sabes y puedes dar por cierto", profile.Facts);
            List(text, "Con cuidado", profile.Cautions);
            Section(text, "A quién remites", profile.Referrals);
            if (rules != null)
            {
                Section(text, "Ámbito", rules.Scope);
                List(text, "Reglas", rules.Rules);
            }

            List(text, "Ahora mismo", Situation(context));
            if (context.Notes.Count > 0)
            {
                text.AppendLine().AppendLine("## Lo que el visitante ha contado hoy a otros vecinos");
                text.AppendLine("En un pueblo todo se sabe: puedes sacarlo si viene a cuento, sin repetirlo todo.");
                foreach (string note in context.Notes)
                {
                    text.Append("- ").AppendLine(note);
                }
            }

            return text.ToString();
        }

        /// <summary>Frases sobre el momento de la visita: hora, día, gente, si se conocen y el nombre del jugador.</summary>
        public static List<string> Situation(ConversationContext context)
        {
            int minutes = Mathf.RoundToInt(Mathf.Repeat(context.Hour, 24f) * 60f) % (24 * 60);
            string part = GreetingRules.TimeOfDay(context.Hour) switch
            {
                LineMoment.Morning => "por la mañana",
                LineMoment.Afternoon => "por la tarde",
                _ => "por la noche",
            };

            return new List<string>
            {
                $"Son las {minutes / 60}:{minutes % 60:00}, {part}.",
                Describe(context.Occasion),
                context.Crowd >= GreetingRules.CrowdedFrom ? "La plaza está llena de gente."
                    : context.Crowd >= BusyFrom ? "Hay bastante gente en la plaza." : "La plaza está tranquila.",
                context.FirstMeeting ? "Es la primera vez que habláis." : "Ya habéis hablado antes hoy.",
                "No sabes si el visitante es hombre o mujer: no uses palabras con género para referirte a él (di «qué alegría " +
                "verle por aquí», no «bienvenido» ni «bienvenida»).",
                string.IsNullOrEmpty(context.PlayerName)
                    ? "No sabes cómo se llama el visitante; si te lo dice, llámale por su nombre de vez en cuando."
                    : $"El visitante se llama {context.PlayerName}; llámale por su nombre de vez en cuando.",
            };
        }

        /// <summary>Qué día es, tal como lo sabe el personaje.</summary>
        public static string Describe(ScenarioOccasion occasion) => occasion switch
        {
            ScenarioOccasion.Weekend => "Es fin de semana y viene más gente de fuera.",
            ScenarioOccasion.Pilgrimage => "Es 8 de septiembre, día de la Virgen de Guadalupe: llegan peregrinos de todas partes.",
            ScenarioOccasion.Christmas => "Es Navidad y la plaza tiene la iluminación navideña y el árbol.",
            _ => "Es un día normal entre semana.",
        };

        /// <summary>Los últimos mensajes de la conversación, empezando siempre por uno del jugador.</summary>
        public static List<ChatMessage> Trim(IReadOnlyList<ChatMessage> history, int max = MaxMessages)
        {
            int start = Mathf.Max(0, history.Count - max);
            while (start < history.Count && history[start].Role != ChatRole.Player)
            {
                start++;
            }

            var trimmed = new List<ChatMessage>(history.Count - start);
            for (int i = start; i < history.Count; i++)
            {
                trimmed.Add(history[i]);
            }

            return trimmed;
        }

        /// <summary>
        /// Petición para resumir la conversación con <paramref name="characterName"/>: el nombre del jugador si lo ha
        /// dicho y una nota con lo que ha contado o preguntado, en JSON (<see cref="VisitNote"/>).
        /// </summary>
        public static LanguageModelRequest Summary(string characterName, IReadOnlyList<ChatMessage> history)
        {
            var transcript = new StringBuilder();
            foreach (ChatMessage message in history)
            {
                if (message.Text != Opening)
                {
                    transcript.Append(message.Role == ChatRole.Player ? "Visitante: " : $"{characterName}: ").AppendLine(message.Text);
                }
            }

            string instructions =
                "Lees una conversación entre un visitante y un vecino de Guadalupe. Responde solo con un objeto JSON con dos campos:\n" +
                "- \"nombre\": el nombre de pila del visitante si lo ha dicho; si no, \"\".\n" +
                $"- \"nota\": una frase corta en tercera persona que empiece por «Con {characterName}» y diga lo que el visitante ha " +
                "contado de sí mismo o lo que ha preguntado (de dónde es, con quién viene, qué busca). Si no hay nada que contar, \"\".";
            return new LanguageModelRequest(instructions, new[] { new ChatMessage(ChatRole.Player, transcript.ToString()) }, true);
        }

        private static void Section(StringBuilder text, string title, string body)
        {
            if (!string.IsNullOrWhiteSpace(body))
            {
                text.AppendLine().Append("## ").AppendLine(title).AppendLine(body.Trim());
            }
        }

        private static void List(StringBuilder text, string title, IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            text.AppendLine().Append("## ").AppendLine(title);
            foreach (string item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    text.Append("- ").AppendLine(item.Trim());
                }
            }
        }
    }
}
