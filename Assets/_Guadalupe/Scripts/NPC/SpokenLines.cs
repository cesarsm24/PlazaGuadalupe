using System;
using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>Momento en el que un personaje dice una frase corta sin que el jugador le hable.</summary>
    public enum LineMoment
    {
        /// <summary>La primera vez que ve al jugador: se presenta.</summary>
        Introduction,

        /// <summary>Saludo de cualquier momento.</summary>
        General,

        Morning,
        Afternoon,
        Night,

        Christmas,

        /// <summary>En la gran peregrinación del 8 de septiembre.</summary>
        Pilgrimage,

        Crowded,

        /// <summary>Cuando ya sabe su nombre; <c>{nombre}</c> se sustituye por él.</summary>
        Named,

        /// <summary>Al volver a verle poco después: algo breve.</summary>
        Again,

        /// <summary>Le llama para que se acerque cuando pasa cerca.</summary>
        CallOut,

        Farewell,

        /// <summary>Cuando no puede conversar (sin conexión con el modelo de lenguaje).</summary>
        Unavailable,
    }

    /// <summary>Frases de un personaje para un momento.</summary>
    [Serializable]
    public struct SpokenLines
    {
        [SerializeField] private LineMoment _moment;
        [SerializeField, TextArea(1, 3)] private List<string> _lines;

        public SpokenLines(LineMoment moment, List<string> lines)
        {
            _moment = moment;
            _lines = lines;
        }

        public LineMoment Moment => _moment;

        public IReadOnlyList<string> Lines => _lines ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
}
