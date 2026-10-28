using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Lee los ficheros de diseño de la plaza (catálogo de casas y trazado), que completan los datos abiertos.
    /// </summary>
    /// <remarks>Los puntos se escriben como pares [x, z] en metros locales.</remarks>
    internal static class DesignData
    {
        private const string Folder = ProjectPaths.DesignData;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter(), new Vector2Converter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        /// <summary>Deserializa el fichero indicado de la carpeta de diseño.</summary>
        public static T Read<T>(string fileName)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText($"{Folder}/{fileName}"), Settings);
        }

        /// <summary>Lee y escribe un <see cref="Vector2"/> como el par [x, z].</summary>
        private sealed class Vector2Converter : JsonConverter<Vector2>
        {
            public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                float[] pair = serializer.Deserialize<float[]>(reader);
                if (pair == null || pair.Length != 2)
                {
                    throw new JsonSerializationException($"Se esperaba un par [x, z] en {reader.Path}.");
                }

                return new Vector2(pair[0], pair[1]);
            }

            public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, new[] { value.x, value.y });
            }
        }
    }
}
