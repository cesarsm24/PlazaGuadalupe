using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>Cálculos sobre el plano del suelo, compartidos por los turistas y los personajes principales.</summary>
    internal static class GroundPlane
    {
        /// <summary>El vector sin componente vertical.</summary>
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Distancia de un punto a un segmento.</summary>
        public static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector3.Distance(point, a + ab * t);
        }
    }
}
