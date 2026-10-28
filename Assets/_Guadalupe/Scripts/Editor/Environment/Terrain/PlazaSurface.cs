using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Superficie de la plaza: plano ajustado por mínimos cuadrados a las elevaciones dentro
    /// del contorno. El modelo de elevaciones (25 m) tiene ondulaciones que una plaza pavimentada no tiene.
    /// </summary>
    internal readonly struct PlazaSurface
    {
        private const float BlendDistance = 8f;

        private readonly List<Vector2> _outline;
        private readonly Vector3 _plane;
        private readonly GeneratorSettings _settings;

        private PlazaSurface(List<Vector2> outline, Vector3 plane, GeneratorSettings settings)
        {
            _outline = outline;
            _plane = plane;
            _settings = settings;
        }

        /// <summary>Ajusta el plano h = a·x + b·z + c a las muestras interiores de la plaza.</summary>
        public static PlazaSurface Fit(GenerationContext ctx)
        {
            List<Vector2> outline = ctx.Data.Osm.PlazaOutline;
            if (outline.Count == 0)
            {
                return new PlazaSurface(outline, Vector3.zero, ctx.Data.Settings);
            }

            var samples = new List<Vector3>();
            for (float z = -60f; z <= 60f; z += 2f)
            {
                for (float x = -60f; x <= 60f; x += 2f)
                {
                    if (PolygonUtils.Contains(outline, new Vector2(x, z)))
                    {
                        samples.Add(new Vector3(x, ctx.Data.Dem.Sample(x, z), z));
                    }
                }
            }

            // Se suaviza la pendiente manteniendo la altura en el centroide de la plaza.
            FittedPlane fit = PlaneFit.LeastSquares(samples);
            float a = fit.SlopeX * ctx.Data.Settings.PlazaSlopeFactor;
            float b = fit.SlopeZ * ctx.Data.Settings.PlazaSlopeFactor;
            float c = fit.CentroidHeight - a * fit.Centroid.x - b * fit.Centroid.y;
            return new PlazaSurface(outline, new Vector3(a, b, c), ctx.Data.Settings);
        }

        /// <summary>
        /// Suaviza el relieve: acerca la elevación al plano de la plaza según <see cref="GeneratorSettings.ReliefFactor"/>
        /// cerca de ella y la deja intacta lejos, para que las cuestas del entorno no sean tan pronunciadas.
        /// </summary>
        public float Soften(Vector2 p, float elevation)
        {
            float planeHeight = _plane.x * p.x + _plane.y * p.y + _plane.z;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_settings.ReliefInnerRadius, _settings.ReliefFullRadius, p.magnitude));
            float factor = Mathf.Lerp(_settings.ReliefFactor, 1f, t);
            return planeHeight + (elevation - planeHeight) * factor;
        }

        /// <summary>Devuelve la elevación corregida: el plano dentro de la plaza y una transición suave fuera.</summary>
        public float Apply(Vector2 p, float elevation)
        {
            if (_outline == null || _outline.Count == 0 || p.sqrMagnitude > 120f * 120f)
            {
                return elevation;
            }

            float planeHeight = _plane.x * p.x + _plane.y * p.y + _plane.z;
            if (PolygonUtils.Contains(_outline, p))
            {
                return planeHeight;
            }

            // El suelo de la plaza llega llano hasta el muro de la lonja en todo su frente.
            float distance = Mathf.Min(PolygonUtils.DistanceToEdges(_outline, p), DistanceToForecourt(p));
            float weight = Mathf.SmoothStep(1f, 0f, distance / BlendDistance);
            return Mathf.Lerp(elevation, planeHeight, weight);
        }

        /// <summary>Distancia a la franja entre la plaza y el frente del atrio (0 dentro).</summary>
        private static float DistanceToForecourt(Vector2 p)
        {
            Vector2 q = PlanFrame.ToPlan(p);
            Rect forecourt = AtriumPlan.Forecourt;
            float du = Mathf.Max(forecourt.xMin - q.x, q.x - forecourt.xMax, 0f);
            float dv = Mathf.Max(forecourt.yMin - q.y, q.y - forecourt.yMax, 0f);
            return Mathf.Sqrt(du * du + dv * dv);
        }
    }
}
