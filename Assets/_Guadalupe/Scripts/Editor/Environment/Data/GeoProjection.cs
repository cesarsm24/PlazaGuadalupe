using System;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Proyección equirectangular local: convierte coordenadas geográficas (WGS84) a metros
    /// en el plano XZ de Unity respecto a un origen.
    /// </summary>
    /// <remarks>
    /// Para un área de pocos cientos de metros el error de esta aproximación es despreciable
    /// (milímetros), y evita depender de librerías de proyección cartográfica.
    /// </remarks>
    public readonly struct GeoProjection
    {
        private const double MetersPerDegreeLat = 110540.0;
        private const double MetersPerDegreeLonAtEquator = 111320.0;

        private readonly double _originLat;
        private readonly double _originLon;
        private readonly double _metersPerDegreeLon;

        /// <summary>Crea una proyección centrada en el origen indicado.</summary>
        /// <param name="originLat">Latitud del origen en grados.</param>
        /// <param name="originLon">Longitud del origen en grados.</param>
        public GeoProjection(double originLat, double originLon)
        {
            _originLat = originLat;
            _originLon = originLon;
            _metersPerDegreeLon = MetersPerDegreeLonAtEquator * Math.Cos(originLat * Math.PI / 180.0);
        }

        /// <summary>
        /// Convierte latitud/longitud a coordenadas locales: X hacia el este, Y (Z en Unity) hacia el norte.
        /// </summary>
        public Vector2 ToLocal(double lat, double lon)
        {
            return new Vector2(
                (float)((lon - _originLon) * _metersPerDegreeLon),
                (float)((lat - _originLat) * MetersPerDegreeLat));
        }
    }
}
