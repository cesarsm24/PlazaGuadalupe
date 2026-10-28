using System.Collections.Generic;
using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Perfil de un personaje: nombre y lo que necesita el modelo de lenguaje para hablar
    /// como él (papel, dónde está, personalidad, temas, datos contrastados, prudencias y a quién remite).
    /// </summary>
    [CreateAssetMenu(menuName = "Guadalupe/Perfil de personaje", fileName = "SO_NpcProfile")]
    public sealed class NpcProfile : ScriptableObject
    {
        [SerializeField] private string _displayName = "Personaje";

        [Header("Conversación")]
        [Tooltip("Quién es, en una línea (p. ej. «franciscano del monasterio»).")]
        [SerializeField] private string _role = "";
        [Tooltip("Dónde está en la plaza, tal como lo diría él.")]
        [SerializeField, TextArea(2, 4)] private string _whereabouts = "";
        [Tooltip("Personalidad y forma de hablar.")]
        [SerializeField, TextArea(3, 8)] private string _personality = "";
        [Tooltip("De qué suele hablar.")]
        [SerializeField, TextArea(2, 4)] private string _topics = "";
        [Tooltip("Datos que puede dar por ciertos, uno por elemento.")]
        [SerializeField, TextArea(2, 6)] private List<string> _facts = new List<string>();
        [Tooltip("Lo que no debe afirmar y cómo responder cuando no lo sabe, uno por elemento.")]
        [SerializeField, TextArea(2, 6)] private List<string> _cautions = new List<string>();
        [Tooltip("A quién remite lo que no es de su tema.")]
        [SerializeField, TextArea(2, 4)] private string _referrals = "";

        [Header("Frases")]
        [Tooltip("Frases cortas que dice sin que el jugador le hable, agrupadas por momento.")]
        [SerializeField] private List<SpokenLines> _lines = new List<SpokenLines>();

        public string DisplayName => _displayName;
        public string Role => _role;
        public string Whereabouts => _whereabouts;
        public string Personality => _personality;
        public string Topics => _topics;
        public IReadOnlyList<string> Facts => _facts;
        public IReadOnlyList<string> Cautions => _cautions;
        public string Referrals => _referrals;

        /// <summary>Frases del momento indicado; vacío si no tiene.</summary>
        public IReadOnlyList<string> LinesFor(LineMoment moment)
        {
            foreach (SpokenLines lines in _lines)
            {
                if (lines.Moment == moment)
                {
                    return lines.Lines;
                }
            }

            return System.Array.Empty<string>();
        }
    }
}
