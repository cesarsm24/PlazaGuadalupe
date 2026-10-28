using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.HouseMeshSlots;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Pilares, vigas y arcos de los soportales.</summary>
    internal static class ColonnadeBuilder
    {
        private const float PillarSize = 0.5f;
        private const float PillarZocalo = 1.1f;
        private const float PillarSetback = 0.3f;
        private const float PillarSpacing = 3.6f;
        private const float JoistSpacing = 0.6f;

        /// <summary>
        /// Pilares, vigas, viguetas y arcos de un tramo de soportal. Cada pilar se coloca justo detrás del frente
        /// de la casa que tiene encima y toma la altura de su techo; donde no hay casa (bocacalles), no hay pilar.
        /// </summary>
        internal static void Build(GenerationContext ctx, ArcadeSegment segment,
            Dictionary<BuildingFootprint, ArcadePart> arcade, GameObject group)
        {
            var row = new PillarRow(ctx, segment, arcade);
            List<Pillar> pillars = row.Place();

            // Submallas: 0 cal, 1 zócalo, 2 madera, 3 granito.
            var builder = new MeshBuilder(4);
            foreach (Pillar pillar in pillars)
            {
                AddPillar(builder, ctx.GroundY(pillar.Position.x, pillar.Position.y), pillar);
            }

            for (int i = 0; i < pillars.Count - 1; i++)
            {
                AddBay(builder, row, pillars[i], pillars[i + 1], ctx.Data.Settings.ArcadeCorridorDepth);
            }

            if (builder.IsEmpty)
            {
                return;
            }

            EnvironmentMaterials m = ctx.Data.Materials;
            Mesh mesh = builder.BuildCompact($"SM_{segment.Name}", new[] { m.WallWhite, m.Zocalo, m.Beam, m.Granite }, out Material[] used);
            ctx.Results.Colonnades.Add(ctx.Output.AddStatic(segment.Name, mesh, used, group));
        }

        /// <summary>Pilar de sillería (soportal de arcos) o encalado sobre zócalo (adintelado).</summary>
        private static void AddPillar(MeshBuilder builder, float floor, Pillar pillar)
        {
            Vector2 p = pillar.Position;
            float top = pillar.Part.Top;
            if (pillar.Part.Arched)
            {
                builder.AddOrientedBox(new Vector3(p.x, floor + 0.15f, p.y), new Vector3(PillarSize + 0.16f, 0.5f, PillarSize + 0.16f), pillar.Yaw, 3);
                builder.AddOrientedBox(new Vector3(p.x, (floor + top) * 0.5f, p.y), new Vector3(PillarSize, top - floor, PillarSize), pillar.Yaw, 3);
            }
            else
            {
                float zocaloTop = floor + PillarZocalo;
                builder.AddOrientedBox(new Vector3(p.x, (floor - 0.3f + zocaloTop) * 0.5f, p.y),
                    new Vector3(PillarSize + 0.02f, PillarZocalo + 0.3f, PillarSize + 0.02f), pillar.Yaw, 1);
                builder.AddOrientedBox(new Vector3(p.x, (zocaloTop + top - 0.4f) * 0.5f, p.y),
                    new Vector3(PillarSize, top - 0.4f - zocaloTop, PillarSize), pillar.Yaw, 0);
            }
        }

        /// <summary>
        /// Tramo entre dos pilares: viga, arco si ambos son de arcos y viguetas hasta el fondo del pasillo, solo
        /// bajo la casa. Los huecos muy anchos (bocacalles) se dejan abiertos.
        /// </summary>
        private static void AddBay(MeshBuilder builder, PillarRow row, Pillar a, Pillar b, float corridorDepth)
        {
            float span = Vector2.Distance(a.Position, b.Position);
            if (span > PillarSpacing * 2.5f)
            {
                return;
            }

            Vector2 dir = (b.Position - a.Position) / span;
            Vector2 right = ((a.Right + b.Right) * 0.5f).normalized;
            float yaw = Yaw(dir);
            float top = Mathf.Min(a.Part.Top, b.Part.Top);
            Vector2 mid = (a.Position + b.Position) * 0.5f;
            builder.AddOrientedBox(ToWorld(mid, top - 0.2f), new Vector3(span + PillarSize, 0.4f, 0.36f), yaw, 2);

            if (a.Part.Arched && b.Part.Arched)
            {
                AddArch(builder, a.Position, b.Position, right, top - 0.4f);
            }

            int joists = Mathf.Max(1, Mathf.FloorToInt(span / JoistSpacing));
            float length = corridorDepth - PillarSetback;
            for (int j = 0; j < joists; j++)
            {
                Vector2 c = a.Position + dir * ((j + 0.5f) * span / joists);
                Vector2 centre = c + right * (length * 0.5f);
                if (row.OwnerAt(centre) != null)
                {
                    builder.AddOrientedBox(ToWorld(centre, top - 0.08f), new Vector3(0.13f, 0.16f, length), yaw, 2);
                }
            }
        }

        /// <summary>
        /// Indica si un pilar queda delante de una puerta, un escaparate o una portada: a menos de 4,5 m de su
        /// muro y dentro de su ancho más el del pilar y un margen para pasar.
        /// </summary>
        private static bool BlocksDoorway(GenerationContext ctx, Vector2 pillar)
        {
            foreach (Vector4 d in ctx.Results.Doorways)
            {
                var center = new Vector2(d.x, d.y);
                var half = new Vector2(d.z, d.w);
                float halfWidth = half.magnitude;
                Vector2 along = half / halfWidth;
                Vector2 offset = pillar - center;
                float sideways = Mathf.Abs(Vector2.Dot(offset, along));
                float ahead = Mathf.Abs(Vector2.Dot(offset, new Vector2(along.y, -along.x)));
                if (ahead < 4.5f && sideways < halfWidth + PillarSize * 0.5f + 0.15f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Arco de medio punto de sillería entre dos pilares, con enjutas hasta la viga e intradós.</summary>
        private static void AddArch(MeshBuilder builder, Vector2 p0, Vector2 p1, Vector2 right, float topY)
        {
            const int steps = 10;
            const float thickness = 0.42f;
            Vector2 dir = (p1 - p0).normalized;
            float clear = Vector2.Distance(p0, p1) - PillarSize;
            float radius = clear * 0.5f;
            float rise = Mathf.Min(radius, 1.6f);
            float springY = topY - rise;
            Vector2 centre = (p0 + p1) * 0.5f;
            Vector3 u = ToWorld(dir, 0f);
            for (int k = 0; k < steps; k++)
            {
                float a0 = Mathf.PI * k / steps;
                float a1 = Mathf.PI * (k + 1) / steps;
                Vector2 q0 = centre - dir * (radius * Mathf.Cos(a0));
                Vector2 q1 = centre - dir * (radius * Mathf.Cos(a1));
                float y0 = springY + rise * Mathf.Sin(a0);
                float y1 = springY + rise * Mathf.Sin(a1);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 o = right * (side * thickness * 0.5f);
                    builder.AddConvexFace(new[] { ToWorld(q0 + o, y0), ToWorld(q1 + o, y1), ToWorld(q1 + o, topY), ToWorld(q0 + o, topY) },
                        ToWorld(right * side, 0f), u, 3);
                }

                Vector2 f = -right * (thickness * 0.5f);
                Vector2 bk = right * (thickness * 0.5f);
                builder.AddConvexFace(new[] { ToWorld(q0 + f, y0), ToWorld(q1 + f, y1), ToWorld(q1 + bk, y1), ToWorld(q0 + bk, y0) },
                    Vector3.down, ToWorld(right, 0f), 3);
            }
        }

        /// <summary>Pilar colocado: posición, dirección hacia las casas y parte de casa que sostiene.</summary>
        private readonly struct Pillar
        {
            public readonly Vector2 Position;
            public readonly Vector2 Right;
            public readonly ArcadePart Part;

            public Pillar(Vector2 position, Vector2 right, ArcadePart part)
            {
                Position = position;
                Right = right;
                Part = part;
            }

            public float Yaw => Mathf.Atan2(Right.x, Right.y) * Mathf.Rad2Deg;
        }

        /// <summary>Estaciones equiespaciadas a lo largo de un tramo y casas que cubren su pasillo.</summary>
        private sealed class PillarRow
        {
            /// <summary>Distancia máxima que se corre un pilar para no tapar una puerta.</summary>
            private const float MaxShift = 2.4f;

            /// <summary>Margen en los extremos de la polilínea en el que no se ponen pilares.</summary>
            private const float EndMargin = 0.3f;

            private readonly GenerationContext _ctx;
            private readonly Vector2[] _line;
            private readonly float _length;
            private readonly List<(List<Vector2> Footprint, ArcadePart Part)> _owners = new List<(List<Vector2>, ArcadePart)>();

            public PillarRow(GenerationContext ctx, ArcadeSegment segment, Dictionary<BuildingFootprint, ArcadePart> arcade)
            {
                _ctx = ctx;
                _line = segment.Line;
                _length = PolylineLength(_line);
                foreach (KeyValuePair<BuildingFootprint, ArcadePart> kv in arcade)
                {
                    if (kv.Value.Segment == segment)
                    {
                        _owners.Add((kv.Key.Footprint, kv.Value));
                    }
                }
            }

            /// <summary>Parte de casa del tramo que cubre el punto, o null.</summary>
            public ArcadePart OwnerAt(Vector2 p)
            {
                foreach ((List<Vector2> footprint, ArcadePart part) in _owners)
                {
                    if (PolygonUtils.Contains(footprint, p))
                    {
                        return part;
                    }
                }

                return null;
            }

            /// <summary>
            /// Pilares del tramo. Si uno queda delante de una puerta o un escaparate, se corre al hueco libre más
            /// cercano; si no lo hay, se quita y la viga salva el hueco.
            /// </summary>
            public List<Pillar> Place()
            {
                int count = Mathf.Max(2, Mathf.RoundToInt(_length / PillarSpacing) + 1);
                var pillars = new List<Pillar>();
                for (int k = 0; k < count; k++)
                {
                    float s = Mathf.Lerp(EndMargin, _length - EndMargin, (float)k / (count - 1));
                    if (!PillarAt(s, out Pillar pillar))
                    {
                        continue;
                    }

                    if (BlocksDoorway(_ctx, pillar.Position))
                    {
                        for (float shift = 0.2f; shift <= MaxShift; shift += 0.1f)
                        {
                            if (TryShift(s + shift, out Pillar moved) || TryShift(s - shift, out moved))
                            {
                                pillar = moved;
                                break;
                            }
                        }

                        if (BlocksDoorway(_ctx, pillar.Position))
                        {
                            continue;
                        }
                    }

                    pillars.Add(pillar);
                }

                return pillars;
            }

            /// <summary>Pilar en la estación s: justo detrás del frente de la casa que tiene encima, o ninguno.</summary>
            private bool PillarAt(float s, out Pillar pillar)
            {
                PointAt(_line, s, out Vector2 p, out Vector2 dir);
                Vector2 right = new Vector2(dir.y, -dir.x);
                for (float off = -ArcadeBands.FrontTolerance; off < 2.5f; off += 0.1f)
                {
                    ArcadePart owner = OwnerAt(p + right * (off + PillarSetback + 0.2f));
                    if (owner != null && OwnerAt(p + right * off) != null)
                    {
                        pillar = new Pillar(p + right * (off + PillarSetback), right, owner);
                        return true;
                    }
                }

                pillar = default;
                return false;
            }

            private bool TryShift(float s, out Pillar moved)
            {
                moved = default;
                return s > EndMargin && s < _length - EndMargin && PillarAt(s, out moved) && !BlocksDoorway(_ctx, moved.Position);
            }
        }
    }
}
