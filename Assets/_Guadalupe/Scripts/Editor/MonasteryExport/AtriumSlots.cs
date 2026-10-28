using Guadalupe.Editor.Environment;
using UnityEngine;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>Submallas de las mallas del atrio y sus materiales.</summary>
    internal static class AtriumSlots
    {
        public const int Ashlar = 0;   // Muros y balaustradas.
        public const int Paving = 1;
        public const int Granite = 2;  // Peldaños, bordillos, bancos y adoquinado.
        public const int Dark = 3;
        public const int Bronze = 4;
        public const int Metal = 5;
        public const int Wood = 6;
        public const int Floor = 7;    // Losas grandes del atrio, a matajunta y paralelas a la fachada.
        public const int Count = 8;

        /// <summary>Materiales de cada submalla, en el orden de las constantes.</summary>
        public static Material[] MaterialsFor(EnvironmentMaterials m)
        {
            return new[] { m.Ashlar, m.PavingSlab, m.Granite, m.Shadow, m.Bronze, m.Metal, m.Wood, m.AtriumFloor };
        }
    }
}
