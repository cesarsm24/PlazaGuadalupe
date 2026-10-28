using Guadalupe.Editor.Environment;
using UnityEngine;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>Submallas de las mallas del ala oeste del monasterio y de la portería.</summary>
    internal static class MonasteryWingSlots
    {
        // 0 mampostería, 1 tejado, 2 teja vidriada, 3 cal, 4 sillería, 5 huecos, 6 hierro,
        // 7 madera de puertas, 8 bronce, 9 cantería labrada, 10 alero de madera.
        internal const int Masonry = 0;
        internal const int Roof = 1;
        internal const int GlazedTile = 2;
        internal const int Lime = 3;
        internal const int Ashlar = 4;
        internal const int Opening = 5;
        internal const int Iron = 6;
        internal const int DoorWood = 7;
        internal const int Bronze = 8;
        internal const int Carved = 9;
        internal const int Eave = 10;
        internal const int Count = 11;

        /// <summary>Materiales de cada submalla, en el orden de las constantes.</summary>
        internal static Material[] MaterialsFor(EnvironmentMaterials m)
        {
            return new[]
            {
                m.Stone, m.Roof, m.GreenTile, m.WallWhite, m.Ashlar, m.Shadow, m.Metal, m.Door, m.Bronze, m.Facade, m.Beam,
            };
        }
    }
}
