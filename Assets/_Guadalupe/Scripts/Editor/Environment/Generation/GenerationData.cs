using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Datos de entrada de una generación: ajustes, materiales y los datos geográficos ya leídos (modelo de
    /// elevaciones, OpenStreetMap y Catastro). No cambian mientras se genera.
    /// </summary>
    internal sealed class GenerationData
    {
        public GeneratorSettings Settings { get; private set; }

        public EnvironmentMaterials Materials { get; private set; }

        public DemGrid Dem { get; private set; }

        /// <summary>Contornos y puntos de OpenStreetMap.</summary>
        public OsmDataset Osm { get; private set; }

        /// <summary>Partes de edificio del Catastro.</summary>
        public List<BuildingFootprint> Buildings { get; private set; }

        /// <summary>Elevación del origen local sobre el nivel del mar.</summary>
        public float OriginElevation { get; private set; }

        /// <summary>Lee los ajustes, los materiales y los ficheros de datos geográficos y de diseño.</summary>
        public static GenerationData Load()
        {
            PlazaLayout.Reload();
            HouseCatalog.Reload();
            var projection = new GeoProjection(PlazaLayout.OriginLat, PlazaLayout.OriginLon);
            var data = new GenerationData
            {
                Settings = GeneratorSettings.Load(),
                Materials = EnvironmentMaterials.Load(),
                Dem = DemGrid.FromJson(File.ReadAllText(ProjectPaths.DemFile)),
                Osm = OsmDataset.Parse(File.ReadAllText(ProjectPaths.OsmFile), projection, PlazaLayout.PlazaWayId),
                Buildings = CatastroDataset.Parse(File.ReadAllText(ProjectPaths.CadastreFile), projection),
            };
            data.OriginElevation = data.Dem.Sample(0f, 0f);
            return data;
        }

        /// <summary>Indica si un punto está dentro de la plaza.</summary>
        public bool InPlaza(Vector2 p) => Osm.PlazaOutline.Count > 0 && PolygonUtils.Contains(Osm.PlazaOutline, p);
    }
}
