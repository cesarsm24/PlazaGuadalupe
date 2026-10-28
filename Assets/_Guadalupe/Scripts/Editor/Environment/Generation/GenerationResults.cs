using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Resultados intermedios que un constructor deja para los que vienen después: referencia de cotas, huecos de
    /// las fachadas, faroles y balcones construidos y piezas de los soportales.
    /// </summary>
    internal sealed class GenerationResults
    {
        /// <summary>
        /// Diferencia entre las alturas del mundo y las cotas de nivel del atrio
        /// (altura = cota + PlanDatum). La fija el terreno al ajustar el pie de la escalinata a la plaza.
        /// </summary>
        public float PlanDatum { get; set; }

        /// <summary>Altura del suelo del atrio ante el monasterio (base de la portería y del ala oeste).</summary>
        public float TerraceTopY { get; set; }

        public int BuildingCount { get; set; }

        public int PorticoCount { get; set; }

        /// <summary>
        /// Puertas, escaparates y portadas de la planta baja: centro (X, Z) y semiancho a lo largo del muro (Z, W),
        /// para que los pilares de los soportales no queden delante de ellos.
        /// </summary>
        public List<Vector4> Doorways { get; } = new List<Vector4>();

        /// <summary>Posición del farol de cada farol de pared, para encenderlos de noche.</summary>
        public List<Vector3> LanternPositions { get; } = new List<Vector3>();

        /// <summary>Balcones de hierro de las casas, para colgar de ellos la iluminación navideña.</summary>
        public List<BalconyAnchor> Balconies { get; } = new List<BalconyAnchor>();

        /// <summary>Balcones del cuerpo del monasterio, cuyas cortinas de luz van en el modelo del monasterio.</summary>
        public List<BalconyAnchor> MonasteryBalconies { get; } = new List<BalconyAnchor>();

        /// <summary>Objetos de los soportales (pilares y vigas), para distinguirlos de los muros al lanzar rayos.</summary>
        public HashSet<GameObject> Colonnades { get; } = new HashSet<GameObject>();
    }
}
