using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Guadalupe.Dialogue
{
    /// <summary>
    /// Una conversación con un personaje: guarda los mensajes y pide al modelo cada respuesta. Si una respuesta
    /// falla, el mensaje del jugador se descarta para poder repetirlo.
    /// </summary>
    public sealed class Conversation
    {
        private readonly ILanguageModel _model;
        private readonly string _instructions;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();

        public Conversation(ILanguageModel model, string instructions)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _instructions = instructions;
        }

        /// <summary>True mientras se espera una respuesta.</summary>
        public bool Waiting { get; private set; }

        /// <summary>Si el jugador ha dicho algo (más allá de acercarse).</summary>
        public bool PlayerSpoke
        {
            get
            {
                foreach (ChatMessage message in _history)
                {
                    if (message.Role == ChatRole.Player && message.Text != PromptBuilder.Opening)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>El personaje empieza la conversación con lo que genera el modelo.</summary>
        public Awaitable<string> OpenAsync(Action<string> onText, CancellationToken cancellation) =>
            ReplyAsync(PromptBuilder.Opening, onText, cancellation);

        /// <summary>El personaje empieza la conversación con <paramref name="line"/>, sin pedir nada al modelo.</summary>
        public void OpenWith(string line)
        {
            if (_history.Count > 0)
            {
                throw new InvalidOperationException("La conversación ya ha empezado.");
            }

            _history.Add(new ChatMessage(ChatRole.Player, PromptBuilder.Opening));
            _history.Add(new ChatMessage(ChatRole.Character, line));
        }

        /// <summary>El jugador dice <paramref name="text"/> y el personaje responde.</summary>
        public Awaitable<string> SayAsync(string text, Action<string> onText, CancellationToken cancellation) =>
            ReplyAsync(text, onText, cancellation);

        /// <summary>Resume la conversación para las siguientes (nombre del jugador y nota), o null si falla.</summary>
        public async Awaitable<VisitNote> SummarizeAsync(string characterName, CancellationToken cancellation)
        {
            string json = await _model.GenerateAsync(PromptBuilder.Summary(characterName, _history), null, cancellation);
            return VisitNote.Parse(json);
        }

        private async Awaitable<string> ReplyAsync(string playerText, Action<string> onText, CancellationToken cancellation)
        {
            if (Waiting)
            {
                throw new InvalidOperationException("Ya se está esperando una respuesta.");
            }

            Waiting = true;
            _history.Add(new ChatMessage(ChatRole.Player, playerText));
            try
            {
                string reply = await _model.GenerateAsync(new LanguageModelRequest(_instructions, PromptBuilder.Trim(_history)), onText, cancellation);
                reply = reply.Trim();
                _history.Add(new ChatMessage(ChatRole.Character, reply));
                return reply;
            }
            catch
            {
                _history.RemoveAt(_history.Count - 1);
                throw;
            }
            finally
            {
                Waiting = false;
            }
        }
    }
}
