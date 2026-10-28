using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Malla regular de elevaciones (modelo digital de elevaciones) centrada en el origen local.</summary>
    /// <remarks>
    /// Las muestras se almacenan por filas de sur a norte y, dentro de cada fila, de oeste a este.
    /// </remarks>
    public sealed class DemGrid
    {
        private readonly float[] _heights;

        /// <summary>Latitud del origen (centro de la malla).</summary>
        public double OriginLat { get; }

        /// <summary>Longitud del origen (centro de la malla).</summary>
        public double OriginLon { get; }

        public float SizeMeters { get; }

        public float StepMeters { get; }

        public int Samples { get; }

        /// <summary>Elevación mínima de la malla, en metros sobre el nivel del mar.</summary>
        public float MinHeight { get; }

        /// <summary>Elevación máxima de la malla, en metros sobre el nivel del mar.</summary>
        public float MaxHeight { get; }

        public DemGrid(double originLat, double originLon, float sizeMeters, float stepMeters, int samples, float[] heights)
        {
            if (heights == null || heights.Length != samples * samples)
            {
                throw new ArgumentException("El número de muestras no coincide con el tamaño de la malla.", nameof(heights));
            }

            OriginLat = originLat;
            OriginLon = originLon;
            SizeMeters = sizeMeters;
            StepMeters = stepMeters;
            Samples = samples;
            _heights = heights;

            MinHeight = float.MaxValue;
            MaxHeight = float.MinValue;
            foreach (float h in heights)
            {
                MinHeight = Mathf.Min(MinHeight, h);
                MaxHeight = Mathf.Max(MaxHeight, h);
            }
        }

        /// <summary>Carga la malla desde el JSON generado por la descarga de datos.</summary>
        public static DemGrid FromJson(string json)
        {
            DemFile file = JsonConvert.DeserializeObject<DemFile>(json);
            return new DemGrid(file.OriginLat, file.OriginLon, file.SizeMeters, file.StepMeters, file.Samples, file.Heights);
        }

        /// <summary>
        /// Devuelve la elevación interpolada (bilineal) en un punto local. Fuera de la malla se usa el borde.
        /// </summary>
        /// <param name="x">Metros hacia el este desde el origen.</param>
        /// <param name="z">Metros hacia el norte desde el origen.</param>
        public float Sample(float x, float z)
        {
            float half = SizeMeters * 0.5f;
            float gx = Mathf.Clamp((x + half) / StepMeters, 0f, Samples - 1);
            float gz = Mathf.Clamp((z + half) / StepMeters, 0f, Samples - 1);

            int x0 = Mathf.Min((int)gx, Samples - 2);
            int z0 = Mathf.Min((int)gz, Samples - 2);
            float tx = gx - x0;
            float tz = gz - z0;

            float h00 = At(x0, z0);
            float h10 = At(x0 + 1, z0);
            float h01 = At(x0, z0 + 1);
            float h11 = At(x0 + 1, z0 + 1);

            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
        }

        private float At(int ix, int iz) => _heights[iz * Samples + ix];

        /// <summary>Estructura del fichero JSON de elevaciones.</summary>
        private sealed class DemFile
        {
            [JsonProperty("originLat")] public double OriginLat { get; set; }
            [JsonProperty("originLon")] public double OriginLon { get; set; }
            [JsonProperty("sizeMeters")] public float SizeMeters { get; set; }
            [JsonProperty("stepMeters")] public float StepMeters { get; set; }
            [JsonProperty("samples")] public int Samples { get; set; }
            [JsonProperty("heights")] public float[] Heights { get; set; }
        }
    }
}
