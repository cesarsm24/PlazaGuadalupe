using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Datos del lugar que no vienen en los datos abiertos: origen local, vía de la plaza en OpenStreetMap,
    /// referencias catastrales singulares y trazado de calle, soportales, pasos y casas puente, que se lee de
    /// <c>Data/Design/plaza_layout.json</c>. Los ajustes de la generación están en <see cref="GeneratorSettings"/>
    /// y las medidas del atrio y del monasterio, en <see cref="AtriumPlan"/>.
    /// </summary>
    /// <remarks>
    /// Origen: centro de la plaza según OpenStreetMap. X hacia el este, Z hacia el norte.
    /// Los valores son aproximados, no medidas topográficas.
    /// </remarks>
    public static class PlazaLayout
    {
        /// <summary>Latitud del origen local (nodo de la plaza en OSM).</summary>
        public const double OriginLat = 39.4519182;

        /// <summary>Longitud del origen local.</summary>
        public const double OriginLon = -5.3274418;

        /// <summary>Identificador OSM de la vía que delimita la plaza.</summary>
        public const long PlazaWayId = 204998217;

        /// <summary>Nombre, en <c>plaza_layout.json</c>, del tramo de soportal de la calle Marqués de la Romana.</summary>
        public const string MarquesArcade = "SoportalMarques";

        /// <summary>Referencia catastral del monasterio (sus partes al oeste del atrio se construyen aparte).</summary>
        public const string MonasteryCadastralRef = "9898205TJ9699N";

        /// <summary>Parte del Catastro de la casa blanca de dos plantas que hay sobre el atrio, junto al arco.</summary>
        public const string AtriumHousePart = "9898205TJ9699N_part3";

        private const string FileName = ProjectPaths.LayoutFile;

        private static LayoutFile _layout;

        private static LayoutFile Layout => _layout ??= Load();

        /// <summary>Eje de la calle Marqués de la Romana, desde la esquina noreste de la plaza.</summary>
        public static Vector2[] MarquesRomana => Layout.MarquesRomana;

        /// <summary>Tramos de soportal; el resto de fachadas a la plaza no tienen.</summary>
        public static ArcadeSegment[] Arcades => Layout.BuiltArcades;

        /// <summary>Pasos bajo casas: calles que el Catastro registra cubiertas por la casa de encima.</summary>
        public static StreetPassage[] Passages => Layout.BuiltPassages;

        /// <summary>Casas puente sobre una calle abierta (arcos que el Catastro no registra).</summary>
        public static StreetBridge[] Bridges => Layout.BuiltBridges;

        /// <summary>Extremos de la fachada de la portería al atrio.</summary>
        public static Vector2[] PorteriaFront => Layout.PorteriaFront;

        /// <summary>Vuelve a leer el trazado del disco.</summary>
        public static void Reload() => _layout = Load();

        private static LayoutFile Load()
        {
            LayoutFile file = DesignData.Read<LayoutFile>(FileName);
            // Los tramos que falten en el fichero equivalen a ninguno, igual que en la validación.
            file.BuiltArcades = (file.Arcades ?? new ArcadeEntry[0]).Select(a => new ArcadeSegment(a.Name, a.Raised, a.WrapCorners, a.Line)).ToArray();
            file.BuiltPassages = (file.Passages ?? new PassageEntry[0]).Select(p => new StreetPassage(p.Name, p.Width, p.ClearHeight, p.Line)).ToArray();
            file.BuiltBridges = (file.Bridges ?? new BridgeEntry[0])
                .Select(b => new StreetBridge(b.Name, b.Center, b.StreetDirection, b.Length, b.Floors, b.ClearHeight))
                .ToArray();
            return file;
        }

        // Estructura del fichero JSON.

        internal sealed class LayoutFile
        {
            public Vector2[] MarquesRomana;
            public ArcadeEntry[] Arcades;
            public PassageEntry[] Passages;
            public BridgeEntry[] Bridges;
            public Vector2[] PorteriaFront;

            // Objetos construidos a partir de las entradas al cargar el fichero.
            [JsonIgnore] public ArcadeSegment[] BuiltArcades;
            [JsonIgnore] public StreetPassage[] BuiltPassages;
            [JsonIgnore] public StreetBridge[] BuiltBridges;
        }

        internal sealed class ArcadeEntry
        {
            public string Name;
            public bool Raised;
            public bool WrapCorners = true;
            public Vector2[] Line;
        }

        internal sealed class PassageEntry
        {
            public string Name;
            public float Width;
            public float ClearHeight;
            public Vector2[] Line;
        }

        internal sealed class BridgeEntry
        {
            public string Name;
            public Vector2 Center;
            public Vector2 StreetDirection;
            public float Length;
            public int Floors;
            public float ClearHeight;
        }
    }
}
