using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Ajuste por mínimos cuadrados de un plano a un conjunto de alturas.</summary>
    public static class PlaneFit
    {
        /// <summary>
        /// Resuelve las ecuaciones normales del ajuste h = a·x + b·z + c. Cada muestra es (x, h, z).
        /// Se resta la altura media para no perder precisión en float (elevaciones de cientos de metros).
        /// </summary>
        public static FittedPlane LeastSquares(IReadOnlyList<Vector3> samples)
        {
            double sxx = 0, sxz = 0, szz = 0, sx = 0, sz = 0, n = 0, sxh = 0, szh = 0, sh = 0;
            foreach (Vector3 s in samples)
            {
                float x = s.x;
                float z = s.z;
                double h = s.y;
                sxx += x * x; sxz += x * z; szz += z * z; sx += x; sz += z; n++;
                sxh += x * h; szh += z * h; sh += h;
            }

            if (n == 0)
            {
                return default;
            }

            var m = new Matrix4x4(
                new Vector4((float)sxx, (float)sxz, (float)sx, 0f),
                new Vector4((float)sxz, (float)szz, (float)sz, 0f),
                new Vector4((float)sx, (float)sz, (float)n, 0f),
                new Vector4(0f, 0f, 0f, 1f));

            double mean = sh / n;
            var rhs = new Vector4((float)(sxh - mean * sx), (float)(szh - mean * sz), (float)(sh - mean * n), 0f);
            Vector4 solution = m.inverse * rhs;

            var centroid = new Vector2((float)(sx / n), (float)(sz / n));
            float centroidHeight = solution.x * centroid.x + solution.y * centroid.y + solution.z + (float)mean;
            return new FittedPlane(solution.x, solution.y, centroid, centroidHeight);
        }
    }

    /// <summary>
    /// Plano h = a·x + b·z + c expresado por su pendiente y por la altura en el centroide de las muestras.
    /// </summary>
    public readonly struct FittedPlane
    {
        public readonly float SlopeX;
        public readonly float SlopeZ;
        public readonly Vector2 Centroid;
        public readonly float CentroidHeight;

        public FittedPlane(float slopeX, float slopeZ, Vector2 centroid, float centroidHeight)
        {
            SlopeX = slopeX;
            SlopeZ = slopeZ;
            Centroid = centroid;
            CentroidHeight = centroidHeight;
        }
    }
}
