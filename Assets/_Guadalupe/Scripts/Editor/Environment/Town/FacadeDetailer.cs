using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Añade los elementos de fachada típicos de Guadalupe: zócalo, ventanas, puertas, escaparates, balcones
    /// de hierro, toldos y faroles de pared.
    /// </summary>
    /// <remarks>
    /// Las decisiones "aleatorias" (qué ventana lleva balcón, color del toldo...) salen de un hash
    /// de la referencia catastral: el resultado es variado pero idéntico en cada generación.
    /// Las medianeras (lados pegados a otro edificio) no reciben huecos.
    /// </remarks>
    internal sealed class FacadeDetailer
    {
        private const float WindowWidth = 1f;
        private const float WindowHeight = 1.3f;
        private const float SillHeight = 0.9f;
        private const float BalconyDoorHeight = 2.1f;
        private const float DoorWidth = 1.1f;
        private const float DoorHeight = 2.2f;
        private const float ShopWidth = 2f;

        /// <summary>Distancia mínima de un escaparate a la esquina de su muro.</summary>
        private const float CornerClearance = 0.5f;
        private const float ShopHeight = 2.3f;
        private const float ZocaloHeight = 0.9f;
        private const float Spacing = 2.6f;
        private const float WallOffset = 0.03f;
        private const float MinEdgeLength = 2.2f;

        private readonly List<(Rect Bounds, List<Vector2> Footprint)> _parts = new List<(Rect, List<Vector2>)>();
        private readonly FacadeSlots _slots;
        private readonly float _levelHeight;

        /// <summary>Aspecto de la casa que se está detallando (null si no está catalogada).</summary>
        public HouseStyle Style { get; set; }

        /// <summary>Lista donde se anotan los balcones construidos (null para no anotarlos).</summary>
        public List<BalconyAnchor> Balconies { get; set; }

        /// <summary>Separación mínima entre ejes de ventanas (m); los muros de mampostería las llevan más separadas.</summary>
        public float WindowSpacing { get; set; } = Spacing;

        /// <summary>Los balcones van en la planta más alta en lugar de en la primera.</summary>
        public bool TopFloorBalconies { get; set; }

        /// <summary>Frontón sobre los recercados de las ventanas altas.</summary>
        public bool Pediments { get; set; } = true;

        /// <summary>
        /// Crea el detallador con todas las plantas que pueden formar medianeras y la altura de cada planta.
        /// </summary>
        public FacadeDetailer(IEnumerable<List<Vector2>> footprints, FacadeSlots slots, float levelHeight)
        {
            _slots = slots;
            _levelHeight = levelHeight;
            foreach (List<Vector2> footprint in footprints)
            {
                _parts.Add((BoundsOf(footprint), footprint));
            }
        }

        /// <summary>Indica si el lado a→b está pegado a otro edificio.</summary>
        public bool IsPartyWall(Vector2 a, Vector2 b, List<Vector2> self)
        {
            Vector2 probe = (a + b) * 0.5f + Outward(a, b) * 0.4f;
            foreach ((Rect bounds, List<Vector2> footprint) in _parts)
            {
                if (footprint != self && bounds.Contains(probe) && PolygonUtils.Contains(footprint, probe))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Banda de zócalo a lo largo del lado, siguiendo la pendiente de la calle o sobre un suelo dado.</summary>
        public void AddZocalo(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b, float? floorY = null)
        {
            float length = Vector2.Distance(a, b);
            int segments = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
            Vector2 offset = Outward(a, b) * (WallOffset * 0.5f);
            Vector3 facing = ToWorld(Outward(a, b), 0f);
            Vector3 u = ToWorld((b - a).normalized, 0f);

            for (int i = 0; i < segments; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / segments) + offset;
                Vector2 q = Vector2.Lerp(a, b, (float)(i + 1) / segments) + offset;
                float gp = floorY ?? ctx.GroundY(p.x, p.y);
                float gq = floorY ?? ctx.GroundY(q.x, q.y);
                float height = Style?.ZocaloHeight ?? ZocaloHeight;
                builder.AddConvexFace(
                    new[] { ToWorld(p, gp - 0.2f), ToWorld(q, gq - 0.2f), ToWorld(q, gq + height), ToWorld(p, gp + height) },
                    facing, u, _slots.Zocalo);
            }
        }

        /// <summary>
        /// Ventanas de las plantas <paramref name="firstFloor"/> a <paramref name="floors"/> − 1. En la primera planta
        /// (o en la última, con <see cref="TopFloorBalconies"/>), según <paramref name="balconyChance"/>, la ventana se
        /// convierte en balconera con balcón de hierro.
        /// </summary>
        /// <param name="floorZeroY">Altura del suelo de la planta 0 en este lado.</param>
        /// <param name="ceilingY">Altura máxima permitida (alero).</param>
        public void AddWindows(MeshBuilder builder, Vector2 a, Vector2 b, float floorZeroY, int firstFloor, int floors,
            float ceilingY, float balconyChance, string seed)
        {
            int balconyFloor = TopFloorBalconies ? LastFloor(floorZeroY, firstFloor, floors, ceilingY) : firstFloor;
            int slot = 0;
            foreach (float t in Slots(a, b, WindowWidth, WindowSpacing))
            {
                for (int floor = firstFloor; floor < floors; floor++)
                {
                    float floorY = floorZeroY + floor * _levelHeight;
                    bool balcony = floor == balconyFloor && Chance(seed, slot, floor) < balconyChance;
                    float bottom = balcony ? floorY + 0.05f : floorY + SillHeight;
                    float height = balcony ? BalconyDoorHeight : WindowHeight;
                    if (bottom + height > ceilingY - 0.3f)
                    {
                        break;
                    }

                    bool shutters = !balcony && Chance(seed, slot, floor + 50) < (Style?.ShutterChance ?? 0.45f);
                    // Una de cada cinco o seis ventanas tiene luz dentro por la noche.
                    bool lit = _slots.WindowLit >= 0 && Chance(seed, slot, floor + 77) < 0.18f;
                    AddWindow(builder, a, b, t, WindowWidth, bottom, height, grille: false, shutters, sill: !balcony,
                        glassSlot: lit ? _slots.WindowLit : -1);
                    if (Style != null && Style.StoneSurrounds)
                    {
                        AddSurround(builder, a, b, t, WindowWidth, bottom, height, Pediments);
                    }

                    if (balcony)
                    {
                        AddBalcony(builder, a, b, t, floorY);
                    }
                }

                slot++;
            }
        }

        /// <summary>
        /// Fila de huecos de una sola planta con medidas propias. Los ejes son los de <paramref name="spacing"/>, y
        /// solo uno de cada <paramref name="every"/> a partir de <paramref name="first"/> lleva hueco: así varias filas
        /// con la misma separación alternan sus huecos sin perder la alineación vertical.
        /// </summary>
        /// <param name="sill">Altura del alféizar sobre el suelo de la planta; 0 para balconeras.</param>
        public void AddWindowRow(MeshBuilder builder, Vector2 a, Vector2 b, float floorY, float sill, float width, float height,
            float spacing, int every, int first, bool grille = false)
        {
            int slot = 0;
            foreach (float t in Slots(a, b, width, spacing))
            {
                if (slot >= first && (slot - first) % every == 0)
                {
                    float bottom = floorY + Mathf.Max(sill, 0.05f);
                    AddWindow(builder, a, b, t, width, bottom, height, grille, shutters: false, sill: sill > 0f);
                    if (Style != null && Style.StoneSurrounds)
                    {
                        AddSurround(builder, a, b, t, width, bottom, height, Pediments);
                    }
                }

                slot++;
            }
        }

        /// <summary>
        /// Balcón corrido sobre ménsulas de piedra delante de los ejes <paramref name="firstSlot"/> a
        /// <paramref name="lastSlot"/> de una fila de <see cref="AddWindowRow"/> con la misma separación.
        /// </summary>
        public void AddBalconyRun(MeshBuilder builder, Vector2 a, Vector2 b, float floorY, float spacing, int firstSlot, int lastSlot)
        {
            const float overhang = 1.1f;
            int slot = 0;
            float from = 0f, to = 0f;
            foreach (float t in Slots(a, b, 1f, spacing))
            {
                if (slot == firstSlot)
                {
                    from = t - overhang;
                }

                if (slot == lastSlot)
                {
                    to = t + overhang;
                }

                slot++;
            }

            if (to <= from)
            {
                return;
            }

            AddBalcony(builder, a, b, (from + to) * 0.5f, floorY, to - from);
            Vector2 dir = (b - a).normalized;
            Vector2 n = Outward(a, b);
            float yaw = Yaw(dir);
            for (float along = from + 0.25f; along <= to - 0.2f; along += 0.7f)
            {
                builder.AddOrientedBox(ToWorld(a + dir * along + n * 0.25f, floorY - 0.32f), new Vector3(0.18f, 0.4f, 0.5f), yaw, _slots.Stone);
            }
        }

        /// <summary>Última planta cuya balconera cabe bajo <paramref name="ceilingY"/>.</summary>
        private int LastFloor(float floorZeroY, int firstFloor, int floors, float ceilingY)
        {
            int last = firstFloor;
            for (int floor = firstFloor; floor < floors; floor++)
            {
                if (floorZeroY + floor * _levelHeight + 0.05f + BalconyDoorHeight <= ceilingY - 0.3f)
                {
                    last = floor;
                }
            }

            return last;
        }

        /// <summary>Puerta acristalada a ras de suelo, en medio del lado, para salir a una azotea.</summary>
        public void AddTerraceDoor(MeshBuilder builder, Vector2 a, Vector2 b, float floorY)
        {
            AddWindow(builder, a, b, Vector2.Distance(a, b) * 0.5f, WindowWidth, floorY + 0.05f, BalconyDoorHeight,
                grille: false, shutters: true, sill: false);
        }

        /// <summary>
        /// Planta baja a la calle: una puerta de cada tres huecos y ventanas en el resto; con
        /// <paramref name="awningChance"/> cada hueco lleva un toldo rojo o verde.
        /// </summary>
        /// <param name="floorY">Altura del suelo si no es el terreno (suelo elevado de un soportal).</param>
        public void AddGroundFloor(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b, float ceilingY,
            float awningChance, string seed, float? floorY = null)
        {
            int index = 0;
            foreach (float t in Slots(a, b, DoorWidth))
            {
                Vector2 p = Vector2.Lerp(a, b, t / Vector2.Distance(a, b));
                float ground = floorY ?? ctx.GroundY(p.x, p.y);
                bool isDoor = index % 3 == 1;
                float width = isDoor ? DoorWidth : WindowWidth;
                float bottom = isDoor ? ground : ground + SillHeight;
                float height = isDoor ? DoorHeight : WindowHeight;
                if (bottom + height < ceilingY - 0.3f)
                {
                    if (isDoor)
                    {
                        Vector2 half = (b - a).normalized * (width * 0.5f);
                        ctx.Results.Doorways.Add(new Vector4(p.x, p.y, half.x, half.y));
                        AddOpening(builder, a, b, t, width, bottom, height, _slots.Door);
                        AddFrame(builder, a, b, t, width, bottom, height);
                    }
                    else
                    {
                        AddWindow(builder, a, b, t, width, bottom, height, grille: true, shutters: false, sill: true);
                    }

                    if (Style != null && Style.StoneSurrounds)
                    {
                        AddSurround(builder, a, b, t, width, bottom, height, pediment: false);
                    }

                    float roll = Chance(seed, index, 99);
                    if (roll < awningChance)
                    {
                        int awning = roll < awningChance * 0.5f ? _slots.AwningRed : _slots.AwningGreen;
                        AddAwning(builder, a, b, t, width + 0.5f, bottom + height + 0.35f, awning);
                    }
                }

                index++;
            }
        }

        /// <summary>Escaparates en el muro retranqueado de un soportal.</summary>
        public void AddShopFronts(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b, float ceilingY, float? floorY = null)
        {
            float length = Vector2.Distance(a, b);
            foreach (float t in Slots(a, b, ShopWidth + 0.6f))
            {
                if (t - ShopWidth * 0.5f < CornerClearance || t + ShopWidth * 0.5f > length - CornerClearance)
                {
                    continue;
                }

                Vector2 p = Vector2.Lerp(a, b, t / Vector2.Distance(a, b));
                float bottom = (floorY ?? ctx.GroundY(p.x, p.y)) + 0.05f;
                float height = Mathf.Min(ShopHeight, ceilingY - bottom - 0.3f);
                if (height > 1f)
                {
                    // La entrada es el metro central del escaparate: es lo que no debe tapar un pilar.
                    Vector2 half = (b - a).normalized * 0.5f;
                    ctx.Results.Doorways.Add(new Vector4(p.x, p.y, half.x, half.y));
                    AddOpening(builder, a, b, t, ShopWidth, bottom, height, _slots.Window);
                    AddFrame(builder, a, b, t, ShopWidth, bottom, height);
                }
            }
        }

        /// <summary>
        /// Faroles de pared de forja: palomilla con tornapunta, farol de cuatro caras que se abre hacia arriba,
        /// sombrerete en pirámide y remate.
        /// </summary>
        public void AddWallLanterns(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b, float y, float spacing)
        {
            float length = Vector2.Distance(a, b);
            if (length < spacing * 0.6f)
            {
                return;
            }

            Vector2 dir = (b - a) / length;
            Vector2 n = Outward(a, b);
            float yaw = Yaw(dir);
            int count = Mathf.Max(1, Mathf.RoundToInt(length / spacing));
            for (int i = 0; i < count; i++)
            {
                Vector2 p = a + dir * (length * (i + 0.5f) / count);
                Vector2 lamp = p + n * 0.6f;

                // Placa, brazo horizontal y tornapunta inclinada.
                builder.AddOrientedBox(ToWorld(p + n * 0.02f, y + 0.4f), new Vector3(0.12f, 0.5f, 0.03f), yaw, _slots.Metal);
                builder.AddOrientedBox(ToWorld(p + n * 0.32f, y + 0.6f), new Vector3(0.04f, 0.04f, 0.6f), yaw, _slots.Metal);
                Vector3 s0 = ToWorld(p + n * 0.04f, y + 0.22f);
                Vector3 s1 = ToWorld(p + n * 0.4f, y + 0.58f);
                Vector3 mid = (s0 + s1) * 0.5f;
                Vector3 axis = (s1 - s0).normalized;
                Vector3 side = ToWorld(dir, 0f) * 0.02f;
                Vector3 up = Vector3.Cross(axis, side).normalized * 0.02f;
                builder.AddConvexFace(new[] { s0 - side - up, s1 - side - up, s1 + side - up, s0 + side - up }, -up, axis, _slots.Metal);
                builder.AddConvexFace(new[] { s0 - side + up, s1 - side + up, s1 + side + up, s0 + side + up }, up, axis, _slots.Metal);

                // Farol: cuerpo troncopiramidal de cristal con aristas de hierro.
                Vector3 c = ToWorld(lamp, 0f);
                Vector3 u = ToWorld(dir, 0f);
                Vector3 v = ToWorld(n, 0f);
                float bottom = y + 0.05f;
                float top = y + 0.5f;
                Vector3[] lower = { c + (-u - v) * 0.1f, c + (u - v) * 0.1f, c + (u + v) * 0.1f, c + (-u + v) * 0.1f };
                Vector3[] upper = { c + (-u - v) * 0.16f, c + (u - v) * 0.16f, c + (u + v) * 0.16f, c + (-u + v) * 0.16f };
                for (int k = 0; k < 4; k++)
                {
                    Vector3 l0 = lower[k] + Vector3.up * bottom;
                    Vector3 l1 = lower[(k + 1) % 4] + Vector3.up * bottom;
                    Vector3 u1 = upper[(k + 1) % 4] + Vector3.up * top;
                    Vector3 u0 = upper[k] + Vector3.up * top;
                    Vector3 normal = Vector3.Cross(l1 - l0, u0 - l0).normalized;
                    builder.AddConvexFace(new[] { l0, l1, u1, u0 }, -normal, l1 - l0, _slots.Glass);
                    builder.AddOrientedBox((l0 + u0) * 0.5f, new Vector3(0.025f, top - bottom, 0.025f), yaw, _slots.Metal);
                }

                builder.AddOrientedBox(ToWorld(lamp, bottom - 0.03f), new Vector3(0.24f, 0.06f, 0.24f), yaw, _slots.Metal);
                builder.AddOrientedBox(ToWorld(lamp, top + 0.02f), new Vector3(0.38f, 0.04f, 0.38f), yaw, _slots.Metal);
                ArchitecturalElements.AddSpire(builder, ToWorld(lamp, top + 0.04f), 0.25f, 4, 0.2f, _slots.Metal);
                builder.AddOrientedBox(ToWorld(lamp, top + 0.3f), new Vector3(0.05f, 0.1f, 0.05f), yaw, _slots.Metal);
                ctx.Results.LanternPositions.Add(ToWorld(lamp, (bottom + top) * 0.5f));
            }
        }

        /// <summary>
        /// Ventana completa: cristal, marco de madera, alféizar de piedra y, según el caso, reja de
        /// hierro (planta baja) o contraventanas abiertas a los lados.
        /// </summary>
        private void AddWindow(MeshBuilder builder, Vector2 a, Vector2 b, float along, float width, float bottom, float height,
            bool grille, bool shutters, bool sill, int glassSlot = -1)
        {
            AddOpening(builder, a, b, along, width, bottom, height, glassSlot >= 0 ? glassSlot : _slots.Window);
            AddFrame(builder, a, b, along, width, bottom, height);

            Vector2 dir = (b - a).normalized;
            Vector2 n = Outward(a, b);
            float yaw = Yaw(dir);
            Vector2 center = a + dir * along;

            if (sill)
            {
                builder.AddOrientedBox(ToWorld(center + n * 0.08f, bottom - 0.04f), new Vector3(width + 0.2f, 0.07f, 0.16f), yaw, _slots.Stone);
            }

            if (grille)
            {
                // Reja: barrotes verticales y dos travesaños, adelantados respecto al cristal.
                const int bars = 5;
                Vector2 plane = center + n * 0.1f;
                for (int i = 0; i < bars; i++)
                {
                    float offset = -width * 0.5f + width * (i + 0.5f) / bars;
                    builder.AddOrientedBox(ToWorld(plane + dir * offset, bottom + height * 0.5f), new Vector3(0.025f, height, 0.025f), yaw, _slots.Metal);
                }

                foreach (float y in new[] { bottom + 0.15f, bottom + height - 0.15f })
                {
                    builder.AddOrientedBox(ToWorld(plane, y), new Vector3(width, 0.03f, 0.03f), yaw, _slots.Metal);
                }
            }

            if (shutters)
            {
                // Contraventanas abiertas, pegadas al muro a ambos lados del hueco.
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 p = center + dir * side * (width * 0.75f) + n * 0.04f;
                    builder.AddOrientedBox(ToWorld(p, bottom + height * 0.5f), new Vector3(width * 0.5f, height, 0.03f), yaw, _slots.Frame);
                }
            }
        }

        /// <summary>Marco de madera alrededor de un hueco (cuatro listones adelantados).</summary>
        private void AddFrame(MeshBuilder builder, Vector2 a, Vector2 b, float along, float width, float bottom, float height)
        {
            const float bar = 0.07f;
            Vector2 dir = (b - a).normalized;
            Vector2 n = Outward(a, b);
            float yaw = Yaw(dir);
            Vector2 center = a + dir * along + n * 0.045f;

            builder.AddOrientedBox(ToWorld(center, bottom + height + bar * 0.5f), new Vector3(width + bar * 2f, bar, 0.06f), yaw, _slots.Frame);
            foreach (float side in new[] { -1f, 1f })
            {
                builder.AddOrientedBox(ToWorld(center + dir * side * (width + bar) * 0.5f, bottom + height * 0.5f),
                    new Vector3(bar, height, 0.06f), yaw, _slots.Frame);
            }
        }

        /// <summary>
        /// Recercado de cantería alrededor de un hueco (jambas y dintel en resalto) y, en las ventanas altas,
        /// cornisa con frontón triangular, como en la casa ocre de Marqués de la Romana.
        /// </summary>
        private void AddSurround(MeshBuilder builder, Vector2 a, Vector2 b, float along, float width, float bottom, float height, bool pediment)
        {
            const float band = 0.2f;
            const float projection = 0.07f;
            Vector2 dir = (b - a).normalized;
            Vector2 n = Outward(a, b);
            float yaw = Yaw(dir);
            Vector2 center = a + dir * along + n * (projection * 0.5f + 0.01f);
            foreach (float side in new[] { -1f, 1f })
            {
                builder.AddOrientedBox(ToWorld(center + dir * side * (width * 0.5f + band * 0.5f + 0.06f), bottom + height * 0.5f),
                    new Vector3(band, height + band, projection), yaw, _slots.Stone);
            }

            float top = bottom + height + 0.06f;
            builder.AddOrientedBox(ToWorld(center, top + band * 0.5f), new Vector3(width + band * 2f + 0.12f, band, projection), yaw, _slots.Stone);
            if (!pediment)
            {
                return;
            }

            // Cornisa volada y frontón triangular encima.
            float corniceY = top + band + 0.06f;
            float span = width + band * 2f + 0.4f;
            builder.AddOrientedBox(ToWorld(center + n * 0.04f, corniceY), new Vector3(span, 0.1f, projection + 0.08f), yaw, _slots.Stone);
            Vector2 front = a + dir * along + n * (projection + 0.06f);
            Vector3 u = ToWorld(dir, 0f);
            builder.AddConvexFace(new[]
            {
                ToWorld(front - dir * span * 0.5f, corniceY + 0.05f), ToWorld(front + dir * span * 0.5f, corniceY + 0.05f),
                ToWorld(front, corniceY + 0.45f),
            }, ToWorld(n, 0f), u, _slots.Stone);
        }

        /// <summary>Balcón de hierro: losa de piedra, pasamanos y barrotes.</summary>
        private void AddBalcony(MeshBuilder builder, Vector2 a, Vector2 b, float along, float floorY, float width = 1.7f)
        {
            const float depth = 0.6f;
            const float railHeight = 1f;
            Vector2 dir = (b - a).normalized;
            Vector2 n = Outward(a, b);
            float yaw = Yaw(dir);
            Vector2 center = a + dir * along + n * (depth * 0.5f);
            Vector2 front = a + dir * along + n * (depth - 0.03f);

            builder.AddOrientedBox(ToWorld(center, floorY - 0.06f), new Vector3(width, 0.12f, depth), yaw, _slots.Stone);
            builder.AddOrientedBox(ToWorld(front, floorY + railHeight), new Vector3(width, 0.05f, 0.05f), yaw, _slots.Metal);
            Balconies?.Add(new BalconyAnchor(ToWorld(front, floorY + railHeight), dir, n, width));
            foreach (float side in new[] { -width * 0.5f + 0.02f, width * 0.5f - 0.02f })
            {
                Vector2 s = a + dir * (along + side) + n * (depth * 0.5f);
                builder.AddOrientedBox(ToWorld(s, floorY + railHeight), new Vector3(0.05f, 0.05f, depth), yaw, _slots.Metal);
            }

            int bars = Mathf.Max(8, Mathf.RoundToInt(width / 0.2f));
            for (int i = 0; i <= bars; i++)
            {
                float offset = -width * 0.5f + 0.04f + i * (width - 0.08f) / bars;
                Vector2 p = front + dir * offset;
                builder.AddOrientedBox(ToWorld(p, floorY + railHeight * 0.5f), new Vector3(0.025f, railHeight, 0.025f), yaw, _slots.Metal);
            }

            // Macetas de geranios en algunos balcones, colgadas del pasamanos y en el suelo.
            if (_slots.Pot >= 0 && Chance(Style?.Address ?? string.Empty, Mathf.RoundToInt(along * 10f), Mathf.RoundToInt(floorY * 10f)) < 0.55f)
            {
                foreach (float offset in new[] { -0.55f, 0f, 0.55f })
                {
                    Vector2 hang = front + dir * offset + n * 0.1f;
                    builder.AddOrientedBox(ToWorld(hang, floorY + railHeight - 0.12f), new Vector3(0.24f, 0.2f, 0.2f), yaw, _slots.Pot);
                    if (_slots.Foliage >= 0)
                    {
                        builder.AddOrientedBox(ToWorld(hang + n * 0.02f, floorY + railHeight + 0.05f), new Vector3(0.34f, 0.2f, 0.3f), yaw, _slots.Foliage);
                    }
                }
            }
        }

        /// <summary>Toldo inclinado con faldón, sobre un hueco de planta baja.</summary>
        private void AddAwning(MeshBuilder builder, Vector2 a, Vector2 b, float along, float width, float topY, int submesh)
        {
            const float projection = 1.1f;
            const float drop = 0.45f;
            const float valance = 0.25f;
            Vector2 dir = (b - a).normalized;
            Vector2 n = Outward(a, b);
            Vector2 l0 = a + dir * (along - width * 0.5f) + n * 0.05f;
            Vector2 r0 = a + dir * (along + width * 0.5f) + n * 0.05f;
            Vector2 l1 = l0 + n * projection;
            Vector2 r1 = r0 + n * projection;
            float lowY = topY - drop;
            Vector3 u = ToWorld(dir, 0f);

            var top = new[] { ToWorld(l0, topY), ToWorld(r0, topY), ToWorld(r1, lowY), ToWorld(l1, lowY) };
            builder.AddConvexFace(top, Vector3.up + ToWorld(n, 0f), u, submesh);
            builder.AddConvexFace(top, Vector3.down - ToWorld(n, 0f), u, submesh);
            builder.AddConvexFace(
                new[] { ToWorld(l1, lowY), ToWorld(r1, lowY), ToWorld(r1, lowY - valance), ToWorld(l1, lowY - valance) },
                ToWorld(n, 0f), u, submesh);
        }

        /// <summary>Distancias a lo largo del lado donde caben huecos, centradas y a paso fijo.</summary>
        private static IEnumerable<float> Slots(Vector2 a, Vector2 b, float openingWidth, float spacing = Spacing)
        {
            float length = Vector2.Distance(a, b);
            if (length < MinEdgeLength)
            {
                yield break;
            }

            int count = Mathf.Max(1, Mathf.FloorToInt((length - 0.8f) / Mathf.Max(spacing, openingWidth + 0.8f)));
            float step = length / count;
            for (int i = 0; i < count; i++)
            {
                yield return step * (i + 0.5f);
            }
        }

        private void AddOpening(MeshBuilder builder, Vector2 a, Vector2 b, float along, float width, float bottom,
            float height, int submesh)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 offset = Outward(a, b) * WallOffset;
            Vector2 left = a + dir * (along - width * 0.5f) + offset;
            Vector2 right = a + dir * (along + width * 0.5f) + offset;

            builder.AddConvexFace(
                new[] { ToWorld(left, bottom), ToWorld(right, bottom), ToWorld(right, bottom + height), ToWorld(left, bottom + height) },
                ToWorld(offset, 0f), ToWorld(dir, 0f), submesh);
        }

        /// <summary>Número pseudoaleatorio estable en [0, 1) a partir de una semilla y dos índices.</summary>
        private static float Chance(string seed, int a, int b)
        {
            uint hash = StableHash.Mix(StableHash.Mix(StableHash.Fnv1a(seed), (uint)a), (uint)b);
            hash ^= hash >> 13;
            return (hash % 10000) / 10000f;
        }

        private static Rect BoundsOf(List<Vector2> footprint)
        {
            PolygonUtils.Bounds(footprint, out Vector2 min, out Vector2 max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
