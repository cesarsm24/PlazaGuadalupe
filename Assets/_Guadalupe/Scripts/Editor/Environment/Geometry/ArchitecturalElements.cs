using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Piezas arquitectónicas reutilizables (bloques, balaustradas, bolas, arcos apuntados, discos y
    /// chapiteles) que comparten el monasterio, sus alas y las fachadas del pueblo.
    /// </summary>
    internal static class ArchitecturalElements
    {
        internal static void AddBlock(MeshBuilder builder, float x0, float x1, float y0, float y1, float z0, float z1, int submesh)
        {
            if (x1 - x0 < 0.01f || y1 - y0 < 0.01f || z1 - z0 < 0.01f)
            {
                return;
            }

            builder.AddBoxBetween(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), submesh);
        }

        /// <summary>Balaustrada de piedra: zócalo, balaustres torneados (simplificados), pasamanos y pilastras.</summary>
        internal static void AddBalustrade(MeshBuilder builder, Vector2 a, Vector2 b, float baseY)
        {
            float length = Vector2.Distance(a, b);
            if (length < 0.5f)
            {
                return;
            }

            Vector2 dir = (b - a) / length;
            float yaw = PlanarGeometry.Yaw(dir);
            Vector2 mid = (a + b) * 0.5f;
            builder.AddOrientedBox(new Vector3(mid.x, baseY + 0.1f, mid.y), new Vector3(length, 0.2f, 0.42f), yaw, 0);
            builder.AddOrientedBox(new Vector3(mid.x, baseY + 0.98f, mid.y), new Vector3(length, 0.14f, 0.46f), yaw, 0);

            for (float t = 0.3f; t < length - 0.1f; t += 0.42f)
            {
                Vector2 p = a + dir * t;
                builder.AddOrientedBox(new Vector3(p.x, baseY + 0.27f, p.y), new Vector3(0.22f, 0.14f, 0.22f), yaw, 0);
                builder.AddOrientedBox(new Vector3(p.x, baseY + 0.44f, p.y), new Vector3(0.17f, 0.2f, 0.17f), yaw, 0);
                builder.AddOrientedBox(new Vector3(p.x, baseY + 0.58f, p.y), new Vector3(0.24f, 0.08f, 0.24f), yaw, 0);
                builder.AddOrientedBox(new Vector3(p.x, baseY + 0.75f, p.y), new Vector3(0.14f, 0.26f, 0.14f), yaw, 0);
                builder.AddOrientedBox(new Vector3(p.x, baseY + 0.89f, p.y), new Vector3(0.2f, 0.06f, 0.2f), yaw, 0);
            }

            for (float t = 0f; t <= length + 0.01f; t += Mathf.Max(3.5f, length / Mathf.Max(1, Mathf.Round(length / 3.5f))))
            {
                Vector2 p = a + dir * Mathf.Min(t, length);
                builder.AddOrientedBox(new Vector3(p.x, baseY + 0.65f, p.y), new Vector3(0.55f, 1.3f, 0.55f), yaw, 0);
                builder.AddOrientedBox(new Vector3(p.x, baseY + 1.36f, p.y), new Vector3(0.65f, 0.12f, 0.65f), yaw, 0);
                AddBall(builder, new Vector3(p.x, baseY + 1.66f, p.y), 0.26f, 0);
            }
        }

        /// <summary>Bola de piedra aproximada con un octaedro estirado de 16 caras.</summary>
        internal static void AddBall(MeshBuilder builder, Vector3 c, float r, int submesh)
        {
            const int sides = 8;
            Vector3 top = c + Vector3.up * r;
            Vector3 bottom = c + Vector3.down * r * 0.8f;
            for (int i = 0; i < sides; i++)
            {
                float a0 = 2f * Mathf.PI * i / sides;
                float a1 = 2f * Mathf.PI * (i + 1) / sides;
                Vector3 p0 = c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * r;
                Vector3 p1 = c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * r;
                Vector3 n = ((p0 + p1) * 0.5f - c).normalized;
                builder.AddConvexFace(new[] { p0, p1, top }, n + Vector3.up, p1 - p0, submesh);
                builder.AddConvexFace(new[] { p0, p1, bottom }, n + Vector3.down, p1 - p0, submesh);
            }
        }

        /// <summary>Hueco apuntado (arco ojival equilátero) en un plano vertical orientado al sur.</summary>
        internal static void AddPointedOpening(MeshBuilder builder, float centerX, float bottomY, float width, float height, float z, int submesh)
        {
            AddPointedOpeningOriented(builder, new Vector3(centerX, 0f, z), bottomY, width, height, Vector3.back, Vector3.right, submesh);
        }

        internal static void AddPointedOpeningOriented(MeshBuilder builder, Vector3 center, float bottomY, float width, float height,
            Vector3 facing, Vector3 along, int submesh)
        {
            float rise = width * 0.866f;
            float springY = bottomY + Mathf.Max(0.2f, height - rise);
            var points = new List<Vector3>
            {
                center + along * (-width * 0.5f) + Vector3.up * bottomY,
                center + along * (width * 0.5f) + Vector3.up * bottomY,
            };

            // Lado derecho: arco con centro en el arranque izquierdo; lado izquierdo, simétrico.
            const int steps = 6;
            for (int i = 0; i <= steps; i++)
            {
                float t = Mathf.PI / 3f * i / steps;
                points.Add(center + along * (-width * 0.5f + width * Mathf.Cos(t)) + Vector3.up * (springY + width * Mathf.Sin(t)));
            }

            for (int i = steps - 1; i >= 0; i--)
            {
                float t = Mathf.PI / 3f * i / steps;
                points.Add(center + along * (width * 0.5f - width * Mathf.Cos(t)) + Vector3.up * (springY + width * Mathf.Sin(t)));
            }

            builder.AddConvexFace(points, facing, along, submesh);
        }

        /// <summary>Hueco de medio punto en un plano vertical con la orientación dada.</summary>
        internal static void AddRoundOpeningOriented(MeshBuilder builder, Vector3 center, float bottomY, float width, float height,
            Vector3 facing, Vector3 along, int submesh)
        {
            float radius = width * 0.5f;
            float springY = bottomY + Mathf.Max(0.2f, height - radius);
            var points = new List<Vector3>
            {
                center + along * -radius + Vector3.up * bottomY,
                center + along * radius + Vector3.up * bottomY,
            };

            const int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                float t = Mathf.PI * i / steps;
                points.Add(center + along * (radius * Mathf.Cos(t)) + Vector3.up * (springY + radius * Mathf.Sin(t)));
            }

            builder.AddConvexFace(points, facing, along, submesh);
        }

        /// <summary>Disco plano en un plano vertical con la orientación dada (óculo).</summary>
        internal static void AddDiscOriented(MeshBuilder builder, Vector3 center, float radius, Vector3 facing, Vector3 along, int submesh)
        {
            const int segments = 20;
            var points = new List<Vector3>(segments);
            for (int i = 0; i < segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                points.Add(center + along * (radius * Mathf.Cos(a)) + Vector3.up * (radius * Mathf.Sin(a)));
            }

            builder.AddConvexFace(points, facing, along, submesh);
        }

        /// <summary>Disco plano orientado al sur (reloj, rosetón).</summary>
        internal static void AddDisc(MeshBuilder builder, Vector3 center, float radius, int submesh)
        {
            const int segments = 20;
            var points = new List<Vector3>(segments);
            for (int i = 0; i < segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                points.Add(center + new Vector3(radius * Mathf.Cos(a), radius * Mathf.Sin(a), 0f));
            }

            builder.AddConvexFace(points, Vector3.back, Vector3.right, submesh);
        }

        /// <summary>Chapitel o pináculo: pirámide de <paramref name="sides"/> caras.</summary>
        internal static void AddSpire(MeshBuilder builder, Vector3 baseCenter, float radius, int sides, float height, int submesh)
        {
            Vector3 apex = baseCenter + Vector3.up * height;
            float offset = sides == 4 ? Mathf.PI / 4f : 0f;
            for (int i = 0; i < sides; i++)
            {
                float a0 = offset + 2f * Mathf.PI * i / sides;
                float a1 = offset + 2f * Mathf.PI * (i + 1) / sides;
                Vector3 p0 = baseCenter + new Vector3(radius * Mathf.Cos(a0), 0f, radius * Mathf.Sin(a0));
                Vector3 p1 = baseCenter + new Vector3(radius * Mathf.Cos(a1), 0f, radius * Mathf.Sin(a1));
                Vector3 outward = ((p0 + p1) * 0.5f - baseCenter).normalized + Vector3.up * 0.3f;
                builder.AddConvexFace(new[] { p0, p1, apex }, outward, p1 - p0, submesh);
            }
        }

        /// <summary>Barra recta de sección cuadrada entre dos puntos (pasamanos, perfiles, tirantes).</summary>
        internal static void AddBar(MeshBuilder builder, Vector3 a, Vector3 b, float size, int submesh)
        {
            Vector3 axis = b - a;
            if (axis.sqrMagnitude < 1e-6f)
            {
                return;
            }

            Vector3 dir = axis.normalized;
            Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized * (size * 0.5f);
            Vector3 up = Vector3.Cross(side, dir).normalized * (size * 0.5f);
            Vector3[] offsets = { side + up, -side + up, -side - up, side - up };
            for (int i = 0; i < 4; i++)
            {
                Vector3 o0 = offsets[i];
                Vector3 o1 = offsets[(i + 1) % 4];
                builder.AddConvexFace(new[] { a + o0, a + o1, b + o1, b + o0 }, o0 + o1, dir, submesh);
            }
        }
    }
}
