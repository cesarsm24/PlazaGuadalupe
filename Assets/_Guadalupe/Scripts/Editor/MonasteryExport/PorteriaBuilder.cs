using System.Collections.Generic;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.MonasteryWingSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>Portería del monasterio: cuerpo de mampostería con la portada labrada.</summary>
    internal static class PorteriaBuilder
    {
        /// <summary>
        /// Portería: cuerpo de mampostería con cornisa de ladrillo y tejado, con la fachada al este (en el plano
        /// de la casa blanca, no en el de la iglesia) y la portada de entrada al monasterio: jambas y dintel
        /// labrados con rosetas, pilastrillas con pináculos, arco mixtilíneo con escudo y cartel. La columna con su
        /// arco ciego se construye con la torre oeste.
        /// </summary>
        internal static void Build(MeshBuilder builder, float terraceY)
        {
            var front = new Front(builder, PlazaLayout.PorteriaFront[0], PlazaLayout.PorteriaFront[1], terraceY);
            float top = terraceY + AtriumPlan.PorteriaHeight;
            const float bodyDepth = 6f;
            var body = new List<Vector2> { front.A, front.B, front.B - front.N * bodyDepth, front.A - front.N * bodyDepth };
            builder.AddWalls(body, terraceY - 0.5f, top, Masonry);
            BuildingShapes.AddRoof(builder, body, top, Roof, 0.5f, Eave);

            AddCornice(front);
            float doorT = front.Length * 0.5f;
            float lintel = AddDoorway(front, doorT);
            AddCrest(front, doorT, lintel);

            // Cartel «Real Monasterio» y farol a la izquierda de la portada.
            front.Box(doorT - DoorWidth * 0.5f - Band - 1f, 2.3f, 0.04f, 1.3f, 0.5f, 0.04f, Ashlar);
            front.Box(doorT - DoorWidth * 0.5f - Band - 2.1f, 2.9f, 0.25f, 0.25f, 0.4f, 0.25f, Iron);
        }

        private const float DoorWidth = 1.8f;
        private const float DoorHeight = 2.9f;

        /// <summary>Anchura de las jambas y del dintel labrados.</summary>
        private const float Band = 0.4f;

        /// <summary>Cornisa de ladrillo en dientes de sierra y moldura de piedra bajo el alero.</summary>
        private static void AddCornice(Front front)
        {
            for (float t = 0.15f; t < front.Length; t += 0.3f)
            {
                front.Box(t, AtriumPlan.PorteriaHeight - 0.35f, 0.08f, 0.18f, 0.12f, 0.16f, Carved);
            }

            front.Box(front.Length * 0.5f, AtriumPlan.PorteriaHeight - 0.18f, 0.1f, front.Length, 0.16f, 0.2f, Ashlar);
        }

        /// <summary>
        /// Puerta de dos hojas con cuarterones, jambas y dintel labrados con rosetas y pilastrillas con pináculo.
        /// </summary>
        /// <returns>Altura del centro del dintel sobre el atrio.</returns>
        private static float AddDoorway(Front front, float doorT)
        {
            front.Box(doorT, DoorHeight * 0.5f, 0.02f, DoorWidth, DoorHeight, 0.04f, Opening);
            foreach (float side in new[] { -1f, 1f })
            {
                float leaf = doorT + side * DoorWidth * 0.25f;
                front.Box(leaf, DoorHeight * 0.5f, 0.05f, DoorWidth * 0.47f, DoorHeight - 0.05f, 0.05f, DoorWood);
                for (int r = 0; r < 4; r++)
                {
                    front.Box(leaf, 1.25f + r * 0.4f, 0.08f, DoorWidth * 0.4f, 0.03f, 0.02f, Eave);
                }

                for (int c = -1; c <= 1; c++)
                {
                    front.Box(leaf + c * 0.2f, 1.85f, 0.08f, 0.03f, 1.3f, 0.02f, Eave);
                }

                float jamb = doorT + side * (DoorWidth * 0.5f + Band * 0.5f);
                front.Box(jamb, (DoorHeight + Band) * 0.5f, 0.08f, Band, DoorHeight + Band, 0.12f, Carved);
                for (float ry = 0.35f; ry < DoorHeight; ry += 0.42f)
                {
                    AddRosette(front.Builder, front.At(jamb, ry, 0.15f), 0.15f, front.U, front.N);
                }

                float pilaster = doorT + side * (DoorWidth * 0.5f + Band + 0.13f);
                front.Box(pilaster, 1.85f, 0.12f, 0.22f, 3.7f, 0.2f, Carved);
                front.Box(pilaster, 0.2f, 0.15f, 0.32f, 0.4f, 0.26f, Carved);
                ArchitecturalElements.AddSpire(front.Builder, front.At(pilaster, 3.7f, 0.12f), 0.16f, 4, 0.7f, Carved);
            }

            float lintel = DoorHeight + Band * 0.5f;
            front.Box(doorT, lintel, 0.08f, DoorWidth + Band * 2f, Band, 0.12f, Carved);
            for (float rt = -DoorWidth * 0.5f + 0.2f; rt < DoorWidth * 0.5f; rt += 0.42f)
            {
                AddRosette(front.Builder, front.At(doorT + rt, lintel, 0.15f), 0.15f, front.U, front.N);
            }

            front.Box(doorT, lintel + 0.3f, 0.12f, DoorWidth + Band * 2f + 0.4f, 0.18f, 0.22f, Carved);
            return lintel;
        }

        /// <summary>Arco mixtilíneo rehundido sobre el dintel, recercado de ladrillo, con el escudo en el centro.</summary>
        private static void AddCrest(Front front, float doorT, float lintel)
        {
            var arch = new List<Vector3>();
            var inner = new List<Vector3>();
            const int steps = 14;
            float archBase = lintel + 0.45f;
            for (int i = 0; i <= steps; i++)
            {
                float k = Mathf.PI * i / steps;
                float lift = 0.25f * Mathf.Sin(k * 3f) * Mathf.Sin(k);
                arch.Add(front.At(doorT + Mathf.Cos(k) * 1.45f, archBase + Mathf.Sin(k) * 1.3f + lift, 0.05f));
                inner.Add(front.At(doorT + Mathf.Cos(k) * 1.15f, archBase + Mathf.Sin(k) * 1.05f + lift * 0.8f, 0.06f));
            }

            var facing = new Vector3(front.N.x, 0f, front.N.y);
            var along = new Vector3(front.U.x, 0f, front.U.y);
            front.Builder.AddConvexFace(arch, facing, along, Carved);
            front.Builder.AddConvexFace(inner, facing, along, Masonry);
            front.Box(doorT, archBase + 0.55f, 0.09f, 0.5f, 0.6f, 0.06f, Ashlar);
            front.Box(doorT, archBase + 0.55f, 0.1f, 0.36f, 0.44f, 0.05f, Carved);
        }

        /// <summary>Roseta labrada: disco con pétalos en relieve en el plano de una fachada.</summary>
        private static void AddRosette(MeshBuilder builder, Vector3 c, float r, Vector2 along, Vector2 normal)
        {
            var u = new Vector3(along.x, 0f, along.y);
            var facing = new Vector3(normal.x, 0f, normal.y);
            var disc = new List<Vector3>();
            for (int i = 0; i < 12; i++)
            {
                float k = i * Mathf.PI / 6f;
                float rr = i % 2 == 0 ? r : r * 0.75f;
                disc.Add(c + u * (Mathf.Cos(k) * rr) + Vector3.up * (Mathf.Sin(k) * rr));
            }

            builder.AddConvexFace(disc, facing, u, Ashlar);
            var centre = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                float k = i * Mathf.PI / 3f;
                centre.Add(c + facing * 0.02f + u * (Mathf.Cos(k) * r * 0.35f) + Vector3.up * (Mathf.Sin(k) * r * 0.35f));
            }

            builder.AddConvexFace(centre, facing, u, Carved);
        }

        /// <summary>
        /// Fachada de la portería: piezas colocadas por distancia a lo largo del frente (t), altura sobre el atrio
        /// (h) y salida hacia fuera (d).
        /// </summary>
        private sealed class Front
        {
            public Front(MeshBuilder builder, Vector2 a, Vector2 b, float baseY)
            {
                Builder = builder;
                A = a;
                B = b;
                Length = Vector2.Distance(a, b);
                U = (b - a) / Length;
                N = new Vector2(U.y, -U.x);
                Yaw = PlanarGeometry.Yaw(U);
                BaseY = baseY;
            }

            public MeshBuilder Builder { get; }
            public Vector2 A { get; }
            public Vector2 B { get; }
            public float Length { get; }

            /// <summary>Dirección a lo largo de la fachada.</summary>
            public Vector2 U { get; }

            /// <summary>Normal exterior de la fachada.</summary>
            public Vector2 N { get; }

            public float Yaw { get; }
            public float BaseY { get; }

            public Vector3 At(float t, float h, float d) => new Vector3(A.x + U.x * t + N.x * d, BaseY + h, A.y + U.y * t + N.y * d);

            public void Box(float t, float h, float d, float width, float height, float depth, int slot) =>
                Builder.AddOrientedBox(At(t, h, d), new Vector3(width, height, depth), Yaw, slot);
        }
    }
}
