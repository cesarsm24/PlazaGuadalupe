namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Nombres de las piezas del prefab del monasterio (<c>PF_Monastery</c>). Los usan la herramienta que lo
    /// exporta, el generador, que asigna el reloj, los focos y los adornos del monasterio colocado, y la validación.
    /// </summary>
    public static class MonasteryPrefabNodes
    {
        /// <summary>Hijo con el modelo del edificio.</summary>
        public const string Monastery = "Edificio";

        /// <summary>Hijo con el modelo del atrio y los accesos.</summary>
        public const string Atrium = "Accesos";

        /// <summary>Grupo de los focos nocturnos.</summary>
        public const string FloodLights = "Focos";

        /// <summary>Nombre del grupo de los adornos de Navidad dentro del modelo del edificio.</summary>
        public const string DecorationsName = "AdornosNavidad";

        /// <summary>Ruta del grupo de los adornos de Navidad desde la raíz del prefab.</summary>
        public const string Decorations = Monastery + "/" + DecorationsName;
    }
}
