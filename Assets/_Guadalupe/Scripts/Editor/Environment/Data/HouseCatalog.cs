using System.Collections.Generic;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Catálogo de casas de la plaza (<c>Data/Design/houses.json</c>); las que no están en él reciben un aspecto
    /// genérico.
    /// </summary>
    public static class HouseCatalog
    {
        private const string FileName = ProjectPaths.HousesFile;

        private static Dictionary<string, HouseStyle> _styles;

        private static Dictionary<string, HouseStyle> Styles => _styles ??= Load();

        /// <summary>Vuelve a leer el catálogo del disco.</summary>
        public static void Reload() => _styles = Load();

        private static Dictionary<string, HouseStyle> Load()
        {
            var styles = new Dictionary<string, HouseStyle>();
            foreach (HouseStyle style in DesignData.Read<List<HouseStyle>>(FileName))
            {
                styles.Add(style.CadastralRef, style);
            }

            return styles;
        }

        /// <summary>
        /// Aspecto de una casa no catalogada (calles de salida): variado pero estable, con la paleta del pueblo
        /// (encalado casi siempre, a veces crema u ocre; zócalo gris, marrón o almagre; carpintería de madera o
        /// verde; balcones de hierro y, alguna vez, galería de madera).
        /// </summary>
        public static HouseStyle Generic(string buildingId)
        {
            uint h = StableHash.Fnv1a(buildingId);

            float Roll(int k)
            {
                uint x = StableHash.Mix(h, (uint)(k * 2654435761u));
                x ^= x >> 15;
                return (x % 1000) / 1000f;
            }

            float wall = Roll(1);
            float zocalo = Roll(2);
            return new HouseStyle
            {
                Wall = wall < 0.78f ? WallFinish.White : wall < 0.92f ? WallFinish.Cream : WallFinish.Ochre,
                Zocalo = zocalo < 0.55f ? ZocaloFinish.Grey : zocalo < 0.8f ? ZocaloFinish.Brown : ZocaloFinish.Red,
                GreenShutters = Roll(3) < 0.3f,
                ShutterChance = 0.35f + 0.4f * Roll(4),
                BalconyChance = 0.25f + 0.4f * Roll(5),
                WoodenGallery = Roll(6) < 0.08f,
            };
        }

        /// <summary>Busca el aspecto de la casa con esa referencia catastral; null si no está catalogada.</summary>
        /// <param name="buildingId">Referencia catastral de la finca (14 caracteres o más).</param>
        public static HouseStyle Find(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId) || buildingId.Length < 7)
            {
                return null;
            }

            return Styles.TryGetValue(BuildingFootprint.ParcelOf(buildingId), out HouseStyle style) ? style : null;
        }
    }
}
