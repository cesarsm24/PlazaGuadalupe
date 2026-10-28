using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Guadalupe.Dialogue
{
    /// <summary>
    /// Formato JSON de la API de Gemini: el cuerpo de la petición <c>generateContent</c> y la lectura de cada trozo
    /// de la respuesta y de los errores.
    /// </summary>
    public static class GeminiJson
    {
        /// <summary>Un trozo de la respuesta: texto nuevo, motivo de fin (si acaba) y motivo de bloqueo (si se bloquea).</summary>
        public readonly struct Chunk
        {
            public Chunk(string text, string finishReason, string blockReason)
            {
                Text = text;
                FinishReason = finishReason;
                BlockReason = blockReason;
            }

            public string Text { get; }

            public string FinishReason { get; }

            public string BlockReason { get; }
        }

        /// <summary>Cuerpo de la petición.</summary>
        public static string Request(LanguageModelRequest request, int maxOutputTokens, float temperature)
        {
            var json = new StringBuilder(1024);
            json.Append("{\"systemInstruction\":{\"parts\":[{\"text\":");
            AppendString(json, request.Instructions);
            json.Append("}]},\"contents\":[");
            for (int i = 0; i < request.Messages.Count; i++)
            {
                ChatMessage message = request.Messages[i];
                json.Append(i > 0 ? ",{\"role\":" : "{\"role\":");
                AppendString(json, message.Role == ChatRole.Player ? "user" : "model");
                json.Append(",\"parts\":[{\"text\":");
                AppendString(json, message.Text);
                json.Append("}]}");
            }

            json.Append("],\"generationConfig\":{\"maxOutputTokens\":").Append(maxOutputTokens.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"temperature\":").Append(temperature.ToString("0.##", CultureInfo.InvariantCulture));
            if (request.JsonResponse)
            {
                json.Append(",\"responseMimeType\":\"application/json\"");
            }

            json.Append("}}");
            return json.ToString();
        }

        /// <summary>Lee un evento de la respuesta en streaming.</summary>
        public static Chunk ReadChunk(string json)
        {
            ChunkData data;
            try
            {
                data = JsonUtility.FromJson<ChunkData>(json);
            }
            catch (ArgumentException)
            {
                return default;
            }

            string block = data?.promptFeedback != null && !string.IsNullOrEmpty(data.promptFeedback.blockReason) ? data.promptFeedback.blockReason : null;
            Candidate candidate = data?.candidates != null && data.candidates.Length > 0 ? data.candidates[0] : null;
            if (candidate == null)
            {
                return new Chunk("", null, block);
            }

            var text = new StringBuilder();
            if (candidate.content?.parts != null)
            {
                foreach (Part part in candidate.content.parts)
                {
                    // Los razonamientos internos del modelo no forman parte de lo que dice.
                    if (!part.thought && part.text != null)
                    {
                        text.Append(part.text);
                    }
                }
            }

            return new Chunk(text.ToString(), string.IsNullOrEmpty(candidate.finishReason) ? null : candidate.finishReason, block);
        }

        /// <summary>Mensaje de un error de la API, o el texto tal cual si no tiene ese formato.</summary>
        public static string ReadError(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "";
            }

            try
            {
                ErrorData data = JsonUtility.FromJson<ErrorData>(json);
                if (!string.IsNullOrEmpty(data?.error?.message))
                {
                    return data.error.message;
                }
            }
            catch (ArgumentException)
            {
                // No es JSON: se devuelve tal cual.
            }

            return json.Trim();
        }

        /// <summary>Escribe <paramref name="value"/> como cadena JSON.</summary>
        public static void AppendString(StringBuilder json, string value)
        {
            json.Append('"');
            foreach (char c in value ?? "")
            {
                switch (c)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            json.Append(c);
                        }

                        break;
                }
            }

            json.Append('"');
        }

        // Clases para JsonUtility: los nombres de los campos son los de la API.
#pragma warning disable 0649
        [Serializable]
        private sealed class ChunkData
        {
            public Candidate[] candidates;
            public PromptFeedback promptFeedback;
        }

        [Serializable]
        private sealed class Candidate
        {
            public Content content;
            public string finishReason;
        }

        [Serializable]
        private sealed class Content
        {
            public Part[] parts;
        }

        [Serializable]
        private sealed class Part
        {
            public string text;
            public bool thought;
        }

        [Serializable]
        private sealed class PromptFeedback
        {
            public string blockReason;
        }

        [Serializable]
        private sealed class ErrorData
        {
            public ErrorDetail error;
        }

        [Serializable]
        private sealed class ErrorDetail
        {
            public string message;
        }
#pragma warning restore 0649
    }
}
