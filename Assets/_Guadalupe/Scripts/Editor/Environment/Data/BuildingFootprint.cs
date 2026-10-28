using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Parte de un edificio con su planta y número de plantas sobre rasante.</summary>
    public sealed class BuildingFootprint
    {
        /// <summary>Identificador de la parte (referencia catastral + sufijo de parte).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Referencia catastral del edificio al que pertenece la parte.</summary>
        public string BuildingId { get; set; } = string.Empty;

        /// <summary>Referencia de la finca: los siete primeros caracteres, comunes a todos sus edificios.</summary>
        public string ParcelRef => ParcelOf(BuildingId);

        /// <summary>Plantas sobre rasante.</summary>
        public int Floors { get; set; }

        /// <summary>Planta en sentido antihorario, en metros locales.</summary>
        public List<Vector2> Footprint { get; set; } = new List<Vector2>();

        /// <summary>Referencia de la finca de un identificador catastral (de edificio o de parte).</summary>
        public static string ParcelOf(string cadastralId) => cadastralId.Substring(0, System.Math.Min(7, cadastralId.Length));
    }
}
