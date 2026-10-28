using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Lee la respuesta JSON de la API Overpass de OpenStreetMap y extrae los elementos
    /// que necesita el generador del entorno: contorno de la plaza, recinto del monasterio y fuentes.
    /// </summary>
    public sealed class OsmDataset
    {
        /// <summary>Contorno de la plaza principal (vacío si no se encuentra).</summary>
        public List<Vector2> PlazaOutline { get; private set; } = new List<Vector2>();

        /// <summary>Contorno del recinto del monasterio (vacío si no se encuentra).</summary>
        public List<Vector2> MonasteryOutline { get; private set; } = new List<Vector2>();

        /// <summary>Posiciones de las fuentes.</summary>
        public List<Vector2> Fountains { get; } = new List<Vector2>();

        /// <summary>Analiza el JSON de Overpass (<c>out geom</c>) y proyecta las geometrías.</summary>
        /// <param name="json">Contenido del fichero.</param>
        /// <param name="projection">Proyección al sistema local.</param>
        /// <param name="plazaWayId">Identificador OSM de la vía que delimita la plaza.</param>
        public static OsmDataset Parse(string json, GeoProjection projection, long plazaWayId)
        {
            var dataset = new OsmDataset();
            var root = JObject.Parse(json);

            foreach (JToken element in root["elements"] ?? new JArray())
            {
                string type = (string)element["type"];
                long id = (long)element["id"];
                JObject tags = element["tags"] as JObject ?? new JObject();

                if (type == "node")
                {
                    if ((string)tags["amenity"] == "fountain")
                    {
                        dataset.Fountains.Add(projection.ToLocal((double)element["lat"], (double)element["lon"]));
                    }

                    continue;
                }

                if (type == "way")
                {
                    List<Vector2> ring = PolygonUtils.NormalizeRing(ReadGeometry(element["geometry"], projection));
                    if (ring.Count == 0)
                    {
                        continue;
                    }

                    if (id == plazaWayId)
                    {
                        dataset.PlazaOutline = ring;
                    }
                    else if ((string)tags["amenity"] == "monastery")
                    {
                        dataset.MonasteryOutline = ring;
                    }
                }
            }

            return dataset;
        }

        private static List<Vector2> ReadGeometry(JToken geometry, GeoProjection projection)
        {
            var points = new List<Vector2>();
            foreach (JToken point in geometry ?? new JArray())
            {
                points.Add(projection.ToLocal((double)point["lat"], (double)point["lon"]));
            }

            return points;
        }
    }
}
