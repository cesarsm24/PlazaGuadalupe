using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Guadalupe.Dialogue
{
    /// <summary>
    /// Respuestas de Gemini en streaming (<c>streamGenerateContent</c> con Server-Sent Events): el texto se pasa
    /// según llega, sin esperar a la respuesta completa.
    /// </summary>
    /// <remarks>La clave va en una cabecera, nunca en la URL ni en los mensajes de error.</remarks>
    public sealed class GeminiClient : ILanguageModel
    {
        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const float Temperature = 0.8f;

        private readonly LlmSettings _settings;

        public GeminiClient(LlmSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Awaitable<string> GenerateAsync(LanguageModelRequest request, Action<string> onText, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var reply = new StringBuilder();
            string finishReason = null;
            string blockReason = null;
            var stream = new StreamHandler(data =>
            {
                GeminiJson.Chunk chunk = GeminiJson.ReadChunk(data);
                finishReason = chunk.FinishReason ?? finishReason;
                blockReason = chunk.BlockReason ?? blockReason;
                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    reply.Append(chunk.Text);
                    onText?.Invoke(chunk.Text);
                }
            });

            string url = $"{Endpoint}{Uri.EscapeDataString(_settings.Model)}:streamGenerateContent?alt=sse";
            byte[] body = Encoding.UTF8.GetBytes(GeminiJson.Request(request, _settings.MaxOutputTokens, Temperature));
            using (var web = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                web.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
                web.downloadHandler = stream;
                web.SetRequestHeader("x-goog-api-key", _settings.ApiKey);
                web.timeout = _settings.TimeoutSeconds;
                using (cancellation.Register(web.Abort))
                {
                    await web.SendWebRequest();
                }

                cancellation.ThrowIfCancellationRequested();
                if (web.result != UnityWebRequest.Result.Success)
                {
                    throw Failure(web, stream.Body);
                }
            }

            if (reply.Length > 0)
            {
                return reply.ToString();
            }

            bool blocked = blockReason != null || finishReason == "SAFETY" || finishReason == "PROHIBITED_CONTENT";
            throw new LanguageModelException(blocked ? LanguageModelError.Blocked : LanguageModelError.Empty,
                blockReason ?? finishReason ?? "sin texto");
        }

        private static LanguageModelException Failure(UnityWebRequest web, string body)
        {
            string detail = GeminiJson.ReadError(body);
            if (string.IsNullOrEmpty(detail))
            {
                detail = web.error;
            }

            if (web.result != UnityWebRequest.Result.ProtocolError)
            {
                bool timedOut = web.error != null && web.error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
                return new LanguageModelException(timedOut ? LanguageModelError.Timeout : LanguageModelError.Network, detail);
            }

            long code = web.responseCode;
            LanguageModelError error = code == 429 ? LanguageModelError.Quota
                : code >= 500 ? LanguageModelError.Server
                : LanguageModelError.Rejected;
            return new LanguageModelException(error, $"HTTP {code}: {detail}");
        }

        /// <summary>
        /// Pasa cada evento de la respuesta según llega. Guarda también los primeros bytes tal cual, que son el
        /// mensaje cuando la API responde con un error.
        /// </summary>
        private sealed class StreamHandler : DownloadHandlerScript
        {
            private const int MaxBodyBytes = 8192;

            private readonly SseReader _reader = new SseReader();
            private readonly MemoryStream _body = new MemoryStream();
            private readonly Action<string> _onEvent;

            public StreamHandler(Action<string> onEvent)
                : base(new byte[4096])
            {
                _onEvent = onEvent;
            }

            public string Body => Encoding.UTF8.GetString(_body.GetBuffer(), 0, (int)_body.Length);

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (_body.Length < MaxBodyBytes)
                {
                    _body.Write(data, 0, Mathf.Min(dataLength, MaxBodyBytes - (int)_body.Length));
                }

                foreach (string received in _reader.Push(data, dataLength))
                {
                    _onEvent(received);
                }

                return true;
            }

            protected override void CompleteContent()
            {
                string last = _reader.Flush();
                if (last != null)
                {
                    _onEvent(last);
                }
            }
        }
    }
}
