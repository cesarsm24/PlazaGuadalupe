using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Lee las partes de edificio del Catastro (servicio INSPIRE), ya convertidas a coordenadas
    /// geográficas por <c>Tools/GeoData/descargar_datos.py</c>.
    /// </summary>
    /// <remarks>
    /// Se usan las partes y no los edificios completos porque cada parte trae su propio número
    /// de plantas, lo que da los escalonamientos reales de altura.
    /// </remarks>
    public static class CatastroDataset
    {
        /// <summary>Analiza el JSON y proyecta las plantas al sistema local.</summary>
        public static List<BuildingFootprint> Parse(string json, GeoProjection projection)
        {
            var result = new List<BuildingFootprint>();
            CatastroFile file = JsonConvert.DeserializeObject<CatastroFile>(json);

            foreach (CatastroPart part in file.Parts ?? new List<CatastroPart>())
            {
                var ring = new List<Vector2>(part.Ring.Count);
                foreach (double[] coordinate in part.Ring)
                {
                    ring.Add(projection.ToLocal(coordinate[0], coordinate[1]));
                }

                List<Vector2> footprint = PolygonUtils.NormalizeRing(ring);
                if (footprint.Count == 0 || part.Floors <= 0)
                {
                    continue;
                }

                result.Add(new BuildingFootprint
                {
                    Id = part.Id,
                    BuildingId = part.Building,
                    Floors = part.Floors,
                    Footprint = footprint,
                });
            }

            return result;
        }

        /// <summary>Estructura del fichero JSON.</summary>
        private sealed class CatastroFile
        {
            [JsonProperty("parts")] public List<CatastroPart> Parts { get; set; }
        }

        /// <summary>Parte de edificio en el JSON: anillo de pares [latitud, longitud].</summary>
        private sealed class CatastroPart
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("building")] public string Building { get; set; }
            [JsonProperty("floors")] public int Floors { get; set; }
            [JsonProperty("ring")] public List<double[]> Ring { get; set; }
        }
    }
}
