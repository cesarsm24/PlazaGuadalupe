using System;

namespace Guadalupe.Dialogue
{
    /// <summary>Por qué no se ha obtenido respuesta del modelo.</summary>
    public enum LanguageModelError
    {
        /// <summary>Sin conexión o no se llega al servidor.</summary>
        Network,
        Timeout,

        /// <summary>El servidor rechaza la petición: clave no válida o modelo que no existe.</summary>
        Rejected,

        /// <summary>Se ha superado el límite de peticiones de la clave.</summary>
        Quota,
        Server,

        /// <summary>La respuesta se ha bloqueado por los filtros de contenido.</summary>
        Blocked,
        Empty,
    }

    /// <summary>Fallo al pedir una respuesta al modelo.</summary>
    public sealed class LanguageModelException : Exception
    {
        public LanguageModelException(LanguageModelError error, string detail)
            : base($"{error}: {detail}")
        {
            Error = error;
        }

        public LanguageModelError Error { get; }
    }
}
