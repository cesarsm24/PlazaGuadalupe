using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Guadalupe.Dialogue
{
    /// <summary>Quién dice un mensaje de la conversación.</summary>
    public enum ChatRole
    {
        Player,
        Character,
    }

    /// <summary>Un mensaje de la conversación.</summary>
    public readonly struct ChatMessage
    {
        public ChatMessage(ChatRole role, string text)
        {
            Role = role;
            Text = text;
        }

        public ChatRole Role { get; }

        public string Text { get; }
    }

    /// <summary>Lo que se le pide al modelo: instrucciones, conversación hasta ahora y formato de la respuesta.</summary>
    public sealed class LanguageModelRequest
    {
        public LanguageModelRequest(string instructions, IReadOnlyList<ChatMessage> messages, bool jsonResponse = false)
        {
            Instructions = instructions;
            Messages = messages;
            JsonResponse = jsonResponse;
        }

        /// <summary>Quién es el personaje y cómo debe responder.</summary>
        public string Instructions { get; }

        /// <summary>Mensajes en orden; el último es al que responde.</summary>
        public IReadOnlyList<ChatMessage> Messages { get; }

        /// <summary>Si la respuesta debe ser un objeto JSON en lugar de texto libre.</summary>
        public bool JsonResponse { get; }
    }

    /// <summary>Modelo de lenguaje que genera las respuestas de los personajes.</summary>
    public interface ILanguageModel
    {
        /// <summary>
        /// Genera la respuesta a <paramref name="request"/> pasando a <paramref name="onText"/> cada trozo según llega,
        /// y devuelve el texto completo.
        /// </summary>
        /// <exception cref="LanguageModelException">Si no se ha podido obtener respuesta.</exception>
        /// <exception cref="OperationCanceledException">Si se cancela antes de terminar.</exception>
        Awaitable<string> GenerateAsync(LanguageModelRequest request, Action<string> onText, CancellationToken cancellation);
    }
}
