using System;
using System.Linq;
using UnityEngine;

namespace Guadalupe.Dialogue
{
    /// <summary>Lo que queda de una conversación para las siguientes: el nombre del jugador y una nota.</summary>
    [Serializable]
    public sealed class VisitNote
    {
        private const int MaxNameLength = 30;

        // Los nombres de los campos son los del JSON que devuelve el modelo.
        [SerializeField] private string nombre = "";
        [SerializeField] private string nota = "";

        /// <summary>Nombre del jugador, o null si no lo ha dicho o no parece un nombre.</summary>
        public string PlayerName
        {
            get
            {
                string name = nombre?.Trim();
                bool looksLikeName = !string.IsNullOrEmpty(name) && name.Length <= MaxNameLength &&
                                     name.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\'');
                return looksLikeName ? char.ToUpper(name[0]) + name.Substring(1) : null;
            }
        }

        /// <summary>Lo que ha contado o preguntado el jugador, o null si nada.</summary>
        public string Note => string.IsNullOrWhiteSpace(nota) ? null : nota.Trim();

        /// <summary>Lee la respuesta del modelo; null si no es el JSON esperado.</summary>
        public static VisitNote Parse(string json)
        {
            try
            {
                return JsonUtility.FromJson<VisitNote>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
