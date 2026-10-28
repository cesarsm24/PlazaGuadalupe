using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Reglas comunes a todos los personajes con los que se puede hablar: idioma, extensión, no salir del papel,
    /// no inventar datos y a quién remitir cada tema.
    /// </summary>
    [CreateAssetMenu(menuName = "Guadalupe/Reglas de conversación", fileName = "SO_ConversationRules")]
    public sealed class ConversationRules : ScriptableObject
    {
        [Tooltip("Ámbito de lo que se habla: el pueblo, el monasterio y la comarca.")]
        [SerializeField, TextArea(2, 4)] private string _scope = "";
        [Tooltip("Reglas que cumplen todos los personajes, una por elemento.")]
        [SerializeField, TextArea(1, 4)] private List<string> _rules = new List<string>();

        public string Scope => _scope;
        public IReadOnlyList<string> Rules => _rules;
    }
}
