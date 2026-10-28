using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.HouseMeshSlots;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Arcos de calle: pasos bajo casas (Arco de Sevilla) y casas puente (Arco del Chorro Gordo).</summary>
    internal static class StreetArchBuilder
    {
        /// <summary>Tramos con que se aproximan los arcos de medio punto.</summary>
        private const int ArchSteps = 12;

        /// <summary>
        /// Bocas de un paso bajo casas: arco de medio punto con dovelas de granito en cada fachada por la que
        /// la calle entra y sale.
        /// </summary>
        internal static void BuildPassage(GenerationContext ctx, StreetPassage passage,
            Dictionary<BuildingFootprint, ArcadePart> arcade, GameObject group)
        {
            var covered = new List<(List<Vector2> Footprint, ArcadePart Part)>();
            foreach (KeyValuePair<BuildingFootprint, ArcadePart> kv in arcade)
            {
                if (kv.Value.Passage == passage)
                {
                    covered.Add((kv.Key.Footprint, kv.Value));
                }
            }

            ArcadePart Over(Vector2 p)
            {
                foreach ((List<Vector2> footprint, ArcadePart part) in covered)
                {
                    if (PolygonUtils.Contains(footprint, p))
                    {
                        return part;
                    }
                }

                return null;
            }

            Vector2[] line = passage.Line;
            float total = PolylineLength(line);

            // Submallas: 0 mampostería, 1 ladrillo, 2 hierro.
            var builder = new MeshBuilder(3);
            // Solo dos bocas: donde el paso queda cubierto por primera vez y donde deja de estarlo por última
            // (los huecos entre casas dentro del paso no llevan portada).
            float first = -1f;
            float last = -1f;
            for (float s = 0f; s <= total; s += 0.1f)
            {
                PointAt(line, s, out Vector2 p, out _);
                if (Over(p) != null)
                {
                    if (first < 0f)
                    {
                        first = s;
                    }

                    last = s;
                }
            }

            if (first >= 0f)
            {
                foreach ((float s, bool entry) in new[] { (first, true), (last, false) })
                {
                    PointAt(line, s, out Vector2 p, out Vector2 dir);
                    ArcadePart part = Over(p);
                    Vector2 facing = entry ? -dir : dir;
                    AddMouth(builder, p, facing, passage.Width, ctx.GroundY(p.x, p.y), passage.ClearHeight, part.Top);
                }
            }

            if (!builder.IsEmpty)
            {
                EnvironmentMaterials m = ctx.Data.Materials;
                ctx.Output.AddStatic(passage.Name, builder, $"SM_{passage.Name}", new[] { m.Stone, m.ZocaloBrick, m.Metal }, group);
            }
        }

        /// <summary>Grosor de la rosca de ladrillo a cada lado de la línea del intradós.</summary>
        private const float RingDepth = 0.2f;

        /// <summary>Anchura de las pilastras de la portada de un paso.</summary>
        private const float MouthPier = 0.75f;

        /// <summary>
        /// Boca de un paso bajo casas: paño de mampostería hasta el techo del paso, rosca de ladrillo, portada
        /// con balcón y cruz de hierro sobre la pilastra.
        /// </summary>
        private static void AddMouth(MeshBuilder builder, Vector2 p, Vector2 facing, float width, float groundY, float clear, float topY)
        {
            var mouth = new Mouth(p + facing * 0.04f, facing, width * 0.5f, groundY, groundY + clear - width * 0.5f, topY);
            AddMouthArch(builder, mouth);
            float frameTop = AddMouthFrame(builder, mouth);
            AddMouthBalcony(builder, mouth, frameTop);

            // Cruz de hierro sobre la pilastra izquierda (vista desde la calle).
            Vector2 cross = mouth.Plane + facing * 0.15f - mouth.Across * (mouth.Outer + MouthPier * 0.5f);
            float crossY = topY + 1.4f;
            builder.AddOrientedBox(ToWorld(cross, crossY), new Vector3(0.07f, 1.1f, 0.05f), mouth.Yaw, 2);
            builder.AddOrientedBox(ToWorld(cross, crossY + 0.2f), new Vector3(0.6f, 0.07f, 0.05f), mouth.Yaw, 2);
        }

        /// <summary>Paño sobre el intradós y rosca de ladrillo ligeramente adelantada.</summary>
        private static void AddMouthArch(MeshBuilder builder, Mouth m)
        {
            Vector3 u = ToWorld(m.Across, 0f);
            Vector3 normal = ToWorld(m.Facing, 0f);
            for (int k = 0; k < ArchSteps; k++)
            {
                float a0 = Mathf.PI * k / ArchSteps;
                float a1 = Mathf.PI * (k + 1) / ArchSteps;
                Vector2 q0 = m.Plane + m.Across * (m.Radius * Mathf.Cos(a0));
                Vector2 q1 = m.Plane + m.Across * (m.Radius * Mathf.Cos(a1));
                float y0 = m.SpringY + m.Radius * Mathf.Sin(a0);
                float y1 = m.SpringY + m.Radius * Mathf.Sin(a1);
                builder.AddConvexFace(new[] { ToWorld(q0, y0), ToWorld(q1, y1), ToWorld(q1, m.TopY), ToWorld(q0, m.TopY) }, normal, u, 0);

                float am = (a0 + a1) * 0.5f;
                Vector2 c = m.Plane + m.Facing * 0.05f + m.Across * (m.Outer * Mathf.Cos(am));
                Vector3 centre = ToWorld(c, m.SpringY + m.Outer * Mathf.Sin(am));
                Vector3 tangent = (u * -Mathf.Sin(am) + Vector3.up * Mathf.Cos(am)).normalized;
                Vector3 radial = (u * Mathf.Cos(am) + Vector3.up * Mathf.Sin(am)).normalized;
                float half = Mathf.PI * m.Outer / ArchSteps * 0.5f - 0.015f;
                builder.AddConvexFace(new[]
                {
                    centre - tangent * half - radial * RingDepth, centre + tangent * half - radial * RingDepth,
                    centre + tangent * half + radial * RingDepth, centre - tangent * half + radial * RingDepth,
                }, normal, tangent, 1);
            }
        }

        /// <summary>
        /// Portada de mampostería: pilastras desde el suelo hasta una imposta sobre la que vuela un balcón.
        /// </summary>
        /// <returns>Altura de la imposta.</returns>
        private static float AddMouthFrame(MeshBuilder builder, Mouth m)
        {
            float frameTop = m.TopY + 0.55f;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 j = m.Plane + m.Facing * 0.06f + m.Across * (side * (m.Outer + MouthPier * 0.5f));
                builder.AddOrientedBox(ToWorld(j, (m.GroundY - 0.2f + frameTop) * 0.5f),
                    new Vector3(MouthPier, frameTop - m.GroundY + 0.2f, 0.14f), m.Yaw, 0);
                Vector2 inner = m.Plane + m.Facing * 0.08f + m.Across * (side * (m.Radius + 0.1f));
                builder.AddOrientedBox(ToWorld(inner, (m.GroundY + m.SpringY) * 0.5f), new Vector3(0.22f, m.SpringY - m.GroundY, 0.12f), m.Yaw, 1);
            }

            Vector2 front = m.Plane + m.Facing * 0.06f;
            builder.AddConvexFace(new[]
            {
                ToWorld(front - m.Across * m.Outer, m.TopY), ToWorld(front + m.Across * m.Outer, m.TopY),
                ToWorld(front + m.Across * m.Outer, frameTop), ToWorld(front - m.Across * m.Outer, frameTop),
            }, ToWorld(m.Facing, 0f), ToWorld(m.Across, 0f), 0);
            return frameTop;
        }

        /// <summary>Balcón con barandilla de hierro y macetas sobre la imposta de la portada.</summary>
        private static void AddMouthBalcony(MeshBuilder builder, Mouth m, float frameTop)
        {
            float length = 2f * (m.Outer + MouthPier) * 0.7f;
            builder.AddOrientedBox(ToWorld(m.Plane + m.Facing * 0.4f, frameTop + 0.06f), new Vector3(length, 0.12f, 0.8f), m.Yaw, 0);
            Vector2 railFront = m.Plane + m.Facing * 0.78f;
            builder.AddOrientedBox(ToWorld(railFront, frameTop + 1.05f), new Vector3(length, 0.05f, 0.05f), m.Yaw, 2);
            int bars = Mathf.RoundToInt(length / 0.13f);
            for (int b = 0; b <= bars; b++)
            {
                Vector2 bp = railFront + m.Across * (-length * 0.5f + b * length / bars);
                builder.AddOrientedBox(ToWorld(bp, frameTop + 0.58f), new Vector3(0.025f, 0.95f, 0.025f), m.Yaw, 2);
            }

            foreach (float f in new[] { -0.6f, 0f, 0.55f })
            {
                Vector2 pot = m.Plane + m.Facing * 0.5f + m.Across * f;
                builder.AddOrientedBox(ToWorld(pot, frameTop + 0.3f), new Vector3(0.3f, 0.36f, 0.3f), m.Yaw, 1);
            }
        }

        /// <summary>Geometría común de la boca de un paso.</summary>
        private readonly struct Mouth
        {
            public readonly Vector2 Plane;
            public readonly Vector2 Facing;
            public readonly Vector2 Across;
            public readonly float Radius;
            public readonly float Outer;
            public readonly float GroundY;
            public readonly float SpringY;
            public readonly float TopY;
            public readonly float Yaw;

            public Mouth(Vector2 plane, Vector2 facing, float radius, float groundY, float springY, float topY)
            {
                Plane = plane;
                Facing = facing;
                Across = new Vector2(-facing.y, facing.x);
                Radius = radius;
                Outer = radius + RingDepth;
                GroundY = groundY;
                SpringY = springY;
                TopY = topY;
                Yaw = Yaw(Across);
            }
        }

        /// <summary>
        /// Casa puente sobre una calle abierta (Arco del Chorro Gordo): cuerpo encalado apoyado en las casas de
        /// ambos lados; en las dos caras, arco de medio punto de mampostería con rosca de ladrillo y jardinera
        /// bajo la ventana; bóveda de cañón y tejado.
        /// </summary>
        internal static void BuildBridge(GenerationContext ctx, StreetBridge bridge, GameObject group)
        {
            Vector2 d = bridge.StreetDirection;
            Vector2 across = new Vector2(-d.y, d.x);
            (float left, float right) = OpenSpan(ctx, bridge.Center, across);

            var arch = new BridgeArch
            {
                Centre = bridge.Center + across * ((right - left) * 0.5f),
                Along = d,
                Across = across,
                Radius = (left + right) * 0.5f,
                HalfLength = bridge.Length * 0.5f,
            };
            arch.HalfWidth = arch.Radius + 0.3f;
            arch.Crown = ctx.GroundY(arch.Centre.x, arch.Centre.y) + bridge.ClearHeight;
            float bodyTop = arch.Crown + 0.5f + bridge.Floors * ctx.Data.Settings.LevelHeight;

            var builder = new MeshBuilder(HouseMeshSlots.Count);
            List<Vector2> body = arch.Footprint();
            builder.AddWalls(body, arch.Crown, bodyTop, Wall);
            BuildingShapes.AddRoof(builder, body, bodyTop, Roof, TownBuilder.EaveOverhang, Wood);
            foreach (float side in new[] { -1f, 1f })
            {
                AddBridgeFace(builder, arch, side);
            }

            AddBarrelVault(builder, arch);

            EnvironmentMaterials m = ctx.Data.Materials;
            Material[] slots =
            {
                m.WallWhite, m.Roof, m.Zocalo, m.Window, m.Door, m.Wood, m.Metal, m.LanternGlass, m.AwningRed, m.AwningGreen,
                m.Stone, m.WindowFrame, m.Beam, m.ZocaloBrick, m.Foliage, m.Grime, m.WindowLit,
            };
            ctx.Output.AddStatic(bridge.Name, builder, $"SM_{bridge.Name}", slots, group);
        }

        /// <summary>
        /// Anchura libre de la calle bajo el puente: distancia desde el centro hasta las casas de cada lado
        /// (como mucho 6 m), buscada en pasos de 10 cm.
        /// </summary>
        private static (float Left, float Right) OpenSpan(GenerationContext ctx, Vector2 center, Vector2 across)
        {
            bool Built(Vector2 p)
            {
                foreach (BuildingFootprint b in ctx.Data.Buildings)
                {
                    if (PolygonUtils.Contains(b.Footprint, p))
                    {
                        return true;
                    }
                }

                return false;
            }

            float left = 0f;
            float right = 0f;
            while (left < 6f && !Built(center - across * left))
            {
                left += 0.1f;
            }

            while (right < 6f && !Built(center + across * right))
            {
                right += 0.1f;
            }

            return (left, right);
        }

        /// <summary>Cara del puente hacia un extremo de la calle: arco de mampostería, rosca de ladrillo y ventana.</summary>
        private static void AddBridgeFace(MeshBuilder builder, BridgeArch arch, float side)
        {
            Vector2 plane = arch.Centre + arch.Along * (side * arch.HalfLength);
            Vector2 facing = arch.Along * side;
            Vector2 across = arch.Across;
            float radius = arch.Radius;
            float springY = arch.SpringY;
            for (int k = 0; k < ArchSteps; k++)
            {
                float a0 = Mathf.PI * k / ArchSteps;
                float a1 = Mathf.PI * (k + 1) / ArchSteps;
                Vector2 q0 = plane + across * (radius * Mathf.Cos(a0));
                Vector2 q1 = plane + across * (radius * Mathf.Cos(a1));
                float y0 = springY + radius * Mathf.Sin(a0);
                float y1 = springY + radius * Mathf.Sin(a1);
                builder.AddConvexFace(new[] { ToWorld(q0, y0), ToWorld(q1, y1), ToWorld(q1, arch.Crown + 0.4f), ToWorld(q0, arch.Crown + 0.4f) },
                    ToWorld(facing, 0f), ToWorld(across, 0f), Stone);

                // Rosca de ladrillo.
                Vector2 r0 = plane + facing * 0.05f + across * ((radius + 0.18f) * Mathf.Cos(a0));
                Vector2 r1 = plane + facing * 0.05f + across * ((radius + 0.18f) * Mathf.Cos(a1));
                float z0 = springY + (radius + 0.18f) * Mathf.Sin(a0);
                float z1 = springY + (radius + 0.18f) * Mathf.Sin(a1);
                Vector2 i0 = plane + facing * 0.05f + across * (radius * Mathf.Cos(a0));
                Vector2 i1 = plane + facing * 0.05f + across * (radius * Mathf.Cos(a1));
                builder.AddConvexFace(new[] { ToWorld(i0, y0), ToWorld(i1, y1), ToWorld(r1, z1), ToWorld(r0, z0) },
                    ToWorld(facing, 0f), ToWorld(across, 0f), Brick);
            }

            // Paño de mampostería sobre el arco y jardinera de flores bajo la ventana.
            float crown = arch.Crown;
            builder.AddConvexFace(new[]
            {
                ToWorld(plane - across * arch.HalfWidth, crown), ToWorld(plane + across * arch.HalfWidth, crown),
                ToWorld(plane + across * arch.HalfWidth, crown + 0.4f), ToWorld(plane - across * arch.HalfWidth, crown + 0.4f),
            }, ToWorld(facing, 0f), ToWorld(across, 0f), Stone);
            float yaw = Yaw(across);
            builder.AddOrientedBox(ToWorld(plane + facing * 0.2f, crown + 0.95f), new Vector3(1.1f, 0.22f, 0.3f), yaw, Brick);

            // Ventana con reja en la planta del puente.
            Vector2 w = plane + facing * 0.04f;
            builder.AddOrientedBox(ToWorld(w, crown + 1.6f), new Vector3(0.8f, 1.1f, 0.04f), yaw, Window);
            builder.AddOrientedBox(ToWorld(w + facing * 0.03f, crown + 1.0f), new Vector3(1f, 0.07f, 0.12f), yaw, Stone);
        }

        /// <summary>Bóveda de cañón bajo el puente, de una boca a la otra.</summary>
        private static void AddBarrelVault(MeshBuilder builder, BridgeArch arch)
        {
            Vector2 f = arch.Along * arch.HalfLength;
            for (int k = 0; k < ArchSteps; k++)
            {
                float a0 = Mathf.PI * k / ArchSteps;
                float a1 = Mathf.PI * (k + 1) / ArchSteps;
                Vector2 q0 = arch.Centre + arch.Across * (arch.Radius * Mathf.Cos(a0));
                Vector2 q1 = arch.Centre + arch.Across * (arch.Radius * Mathf.Cos(a1));
                float y0 = arch.SpringY + arch.Radius * Mathf.Sin(a0);
                float y1 = arch.SpringY + arch.Radius * Mathf.Sin(a1);
                float am = (a0 + a1) * 0.5f;
                Vector3 inward = -(ToWorld(arch.Across, 0f) * Mathf.Cos(am) + Vector3.up * Mathf.Sin(am));
                builder.AddConvexFace(new[] { ToWorld(q0 - f, y0), ToWorld(q1 - f, y1), ToWorld(q1 + f, y1), ToWorld(q0 + f, y0) },
                    inward, ToWorld(arch.Along, 0f), Wall);
            }
        }

        /// <summary>Medidas de una casa puente: centro sobre la calle, ejes, radio del arco y altura de su clave.</summary>
        private struct BridgeArch
        {
            public Vector2 Centre;
            public Vector2 Along;
            public Vector2 Across;
            public float Radius;
            public float HalfLength;
            public float HalfWidth;
            public float Crown;

            /// <summary>Altura de los arranques del arco de medio punto.</summary>
            public float SpringY => Crown - Radius;

            /// <summary>Planta rectangular del cuerpo del puente, en sentido antihorario.</summary>
            public List<Vector2> Footprint()
            {
                var rect = new List<Vector2>
                {
                    Centre - Along * HalfLength - Across * HalfWidth, Centre + Along * HalfLength - Across * HalfWidth,
                    Centre + Along * HalfLength + Across * HalfWidth, Centre - Along * HalfLength + Across * HalfWidth,
                };
                if (PolygonUtils.SignedArea(rect) < 0f)
                {
                    rect.Reverse();
                }

                return rect;
            }
        }
    }
}
