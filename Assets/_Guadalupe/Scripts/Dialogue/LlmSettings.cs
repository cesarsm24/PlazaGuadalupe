using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Guadalupe.Dialogue
{
    /// <summary>
    /// Configuración de Gemini: clave, modelo y límites. Se lee de <c>config.local.json</c>, que no forma parte del
    /// proyecto ni de la aplicación porque lleva la clave (plantilla en StreamingAssets/config.example.json).
    /// </summary>
    /// <remarks>
    /// En la aplicación el fichero va en <see cref="Application.persistentDataPath"/>; en el Editor también vale
    /// la carpeta UserSettings del proyecto, que no se sube al repositorio.
    /// </remarks>
    [Serializable]
    public sealed class LlmSettings
    {
        public const string FileName = "config.local.json";

        /// <summary>Valor de la clave en la plantilla, que hay que sustituir.</summary>
        public const string KeyPlaceholder = "PEGA_AQUI_TU_API_KEY";

        private const int DefaultTimeoutSeconds = 15;
        private const int DefaultMaxOutputTokens = 256;

        // Los nombres de los campos son los del JSON.
        [SerializeField] private string apiKey = "";
        [SerializeField] private string model = "";
        [SerializeField] private int timeoutSeconds = DefaultTimeoutSeconds;
        [SerializeField] private int maxOutputTokens = DefaultMaxOutputTokens;

        public string ApiKey => apiKey.Trim();

        public string Model => model.Trim();

        /// <summary>Segundos máximos de una petición completa.</summary>
        public int TimeoutSeconds => timeoutSeconds > 0 ? timeoutSeconds : DefaultTimeoutSeconds;

        /// <summary>Longitud máxima de una respuesta, en tokens.</summary>
        public int MaxOutputTokens => maxOutputTokens > 0 ? maxOutputTokens : DefaultMaxOutputTokens;

        /// <summary>Rutas en las que se busca el fichero, por orden.</summary>
        public static IEnumerable<string> SearchPaths
        {
            get
            {
                yield return Path.Combine(Application.persistentDataPath, FileName);
#if UNITY_EDITOR
                yield return Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "UserSettings", FileName);
#endif
            }
        }

        /// <summary>Lee la configuración de la primera ruta en la que exista el fichero.</summary>
        /// <param name="problem">Qué falla, para mostrárselo al jugador, si devuelve false.</param>
        public static bool TryLoad(out LlmSettings settings, out string problem)
        {
            foreach (string path in SearchPaths)
            {
                if (File.Exists(path))
                {
                    return TryParse(File.ReadAllText(path), out settings, out problem);
                }
            }

            settings = null;
            problem = $"No se encuentra {FileName}. Cópialo en {Path.Combine(Application.persistentDataPath, FileName)}.";
            return false;
        }

        /// <summary>Lee la configuración del contenido del fichero y comprueba que se puede usar.</summary>
        public static bool TryParse(string json, out LlmSettings settings, out string problem)
        {
            settings = null;
            ConfigFile file;
            try
            {
                file = JsonUtility.FromJson<ConfigFile>(json);
            }
            catch (ArgumentException)
            {
                problem = $"{FileName} no es un JSON válido.";
                return false;
            }

            // JsonUtility crea la sección vacía si falta: entonces lo que se echa en falta es la clave.
            LlmSettings read = file?.llm ?? new LlmSettings();
            if (string.IsNullOrWhiteSpace(read.apiKey) || read.apiKey == KeyPlaceholder)
            {
                problem = $"Falta la clave de la API en {FileName}.";
            }
            else if (string.IsNullOrWhiteSpace(read.model))
            {
                problem = $"Falta el modelo en {FileName}.";
            }
            else
            {
                settings = read;
                problem = null;
                return true;
            }

            return false;
        }

        [Serializable]
        private sealed class ConfigFile
        {
            public LlmSettings llm;
        }
    }
}
