using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.HouseMeshSlots;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Lo singular de la fachada principal de cada casa según el catálogo: rótulos, azulejo con el número y
    /// portada de arco.
    /// </summary>
    internal static class ShopfrontBuilder
    {
        internal const string GroupName = "Rotulos";

        /// <summary>
        /// Lo singular de cada fachada principal según el catálogo: rótulos (tabla oscura con marco de hierro y letras
        /// claras) y portada de arco.
        /// </summary>
        internal static void Build(GenerationContext ctx, ShopfrontCollector shopfronts)
        {
            GameObject group = ctx.Output.CreateGroup(GroupName);
            int index = 0;
            Physics.SyncTransforms();
            foreach (Frontage front in shopfronts.Frontages)
            {
                index++;
                BuildFrontage(ctx, group, front, index);
            }
        }

        /// <summary>Rótulos, azulejo del número y portada de un frente de casa.</summary>
        private static void BuildFrontage(GenerationContext ctx, GameObject group, Frontage front, int index)
        {
            Vector2 normal = OutwardNormal(ctx, front);
            HouseStyle style = front.Style;
            bool portal = style.Portal != PortalKind.None;
            float shift = portal && front.Length > 4.2f ? Mathf.Min(1.4f, front.Length * 0.5f - 1.4f) : 0f;
            Vector2 portalAt = front.Mid - front.Along * shift;
            // A la derecha de quien mira la fachada desde la calle (mirando hacia -normal).
            float signShift = shift + style.SignShift * Vector2.Dot(new Vector2(-normal.y, normal.x), front.Along);
            Vector2 signAt = front.Mid + front.Along * signShift;
            float ground = ctx.GroundY(portalAt.x, portalAt.y);

            List<Vector3> signs = AddSigns(ctx, group, front, index, normal, signAt, signShift, portal, ground);
            AddHouseNumber(ctx, group, front, index, normal, signs);
            if (portal)
            {
                var builder = new MeshBuilder(3);
                AddPortal(builder, portalAt, normal, front.Along, ground, style.Portal);
                ctx.Results.Doorways.Add(new Vector4(portalAt.x, portalAt.y, front.Along.x * PortalRadius, front.Along.y * PortalRadius));
                EnvironmentMaterials m = ctx.Data.Materials;
                ctx.Output.AddStatic(HierarchyNames.Of("Portada", style.Address), builder, $"SM_Portada_{index}", new[] { m.WallAshlar, m.Door, m.Granite }, group, decorative: true);
            }
        }

        /// <summary>
        /// Normal exterior del frente. Todo va en la cara exterior del muro: si al mirar hacia el muro desde fuera
        /// no se encuentra la casa, el lado estaba al revés y se da la vuelta.
        /// </summary>
        private static Vector2 OutwardNormal(GenerationContext ctx, Frontage front)
        {
            Vector2 normal = front.Normal;
            var probe = new Vector3(front.Mid.x, front.SignY, front.Mid.y);
            var n3 = new Vector3(normal.x, 0f, normal.y);
            if (!ctx.Output.Raycast(probe + n3 * 0.8f, -n3, 1.2f, out _) && ctx.Output.Raycast(probe - n3 * 0.8f, n3, 1.2f, out _))
            {
                normal = -normal;
            }

            return normal;
        }

        /// <summary>Rótulos del comercio, pegados al muro.</summary>
        /// <returns>
        /// Rótulos colocados (desplazamiento a lo largo, semiancho y altura), para que el azulejo del número no
        /// quede encima de ninguno.
        /// </returns>
        private static List<Vector3> AddSigns(GenerationContext ctx, GameObject group, Frontage front, int index, Vector2 normal,
            Vector2 signAt, float shift, bool portal, float ground)
        {
            var signs = new List<Vector3>();
            HouseStyle style = front.Style;
            if (style.Sign == null)
            {
                return signs;
            }

            EnvironmentMaterials m = ctx.Data.Materials;
            float signY = portal && shift < 0.5f ? Mathf.Max(front.SignY, ground + 3.35f) : front.SignY;
            AddSign(ctx, group, m, $"{index}a", style.Sign, style.Font, new Vector3(signAt.x, signY, signAt.y), normal, front.Along);
            signs.Add(new Vector3(shift, SignWidth(style.Sign) * 0.5f, signY));
            if (style.Sign2 != null)
            {
                Vector2 second = signAt + front.Along * 1.9f;
                AddSign(ctx, group, m, $"{index}b", style.Sign2, style.Font, new Vector3(second.x, signY - 0.55f, second.y), normal, front.Along);
                signs.Add(new Vector3(shift + 1.9f, SignWidth(style.Sign2) * 0.5f, signY - 0.55f));
            }

            return signs;
        }

        /// <summary>
        /// Azulejo con el número de la casa junto a la puerta, solo si hay muro justo detrás (en los soportales el
        /// frente es la planta alta en voladizo y el azulejo quedaría flotando o metido en un pilar).
        /// </summary>
        private static void AddHouseNumber(GenerationContext ctx, GameObject group, Frontage front, int index, Vector2 normal, List<Vector3> signs)
        {
            string number = HouseNumber(front.Style.Address);
            if (number == null || !FreeTileOffset(front, signs, ctx, out float tileOffset))
            {
                return;
            }

            Vector2 tileAt = front.Mid + front.Along * tileOffset;
            var tilePos = new Vector3(tileAt.x, ctx.GroundY(tileAt.x, tileAt.y) + TileHeight, tileAt.y);
            var tn = new Vector3(normal.x, 0f, normal.y);
            if (ctx.Output.Raycast(tilePos + tn * 0.5f, -tn, 0.65f, out RaycastHit wall) && wall.distance > 0.35f)
            {
                AddNumberTile(ctx, group, ctx.Data.Materials, $"{index}n", number, wall.point, normal, front.Along);
            }
        }

        /// <summary>Altura del azulejo del número sobre la calle.</summary>
        private const float TileHeight = 2.35f;

        /// <summary>Anchura del tablero de un rótulo según su texto.</summary>
        private static float SignWidth(string text) => Mathf.Clamp(0.13f * text.Length + 0.4f, 1.4f, 3.2f);

        /// <summary>
        /// Busca a lo largo del frente un sitio para el azulejo del número que no se solape con ningún rótulo:
        /// primero junto a la puerta a un lado, luego al otro y, si no, más cerca del centro.
        /// </summary>
        private static bool FreeTileOffset(Frontage front, List<Vector3> signs, GenerationContext ctx, out float offset)
        {
            float edge = Mathf.Min(front.Length * 0.5f - 0.4f, 2.2f);
            foreach (float candidate in new[] { edge, -edge, edge * 0.5f, -edge * 0.5f })
            {
                Vector2 at = front.Mid + front.Along * candidate;
                float y = ctx.GroundY(at.x, at.y) + TileHeight;
                bool clear = true;
                foreach (Vector3 sign in signs)
                {
                    clear &= Mathf.Abs(candidate - sign.x) > sign.y + 0.3f || Mathf.Abs(y - sign.z) > 0.45f;
                }

                if (clear)
                {
                    offset = candidate;
                    return true;
                }
            }

            offset = 0f;
            return false;
        }

        /// <summary>Rótulo: tabla oscura con marco de hierro y el nombre en letras claras con su tipografía.</summary>
        private static void AddSign(GenerationContext ctx, GameObject group, EnvironmentMaterials m, string id, string text, SignFont kind,
            Vector3 position, Vector2 normal, Vector2 along)
        {
            float width = SignWidth(text);
            float yaw = PlanarGeometry.Yaw(along);
            var n3 = new Vector3(normal.x, 0f, normal.y);
            var builder = new MeshBuilder(2);
            Vector3 p = position + n3 * 0.06f;
            builder.AddOrientedBox(p, new Vector3(width, 0.5f, 0.06f), yaw, 0);
            builder.AddOrientedBox(p + n3 * 0.01f + Vector3.up * 0.27f, new Vector3(width + 0.06f, 0.04f, 0.07f), yaw, 1);
            builder.AddOrientedBox(p + n3 * 0.01f - Vector3.up * 0.27f, new Vector3(width + 0.06f, 0.04f, 0.07f), yaw, 1);
            GameObject board = ctx.Output.AddStatic(HierarchyNames.Of("Rotulo", text), builder, $"SM_Rotulo_{id}", new[] { m.Door, m.Metal }, group, decorative: true);
            SignText.AddLabel(board, text, kind, position + n3 * 0.1f, n3, width - 0.3f, 0.06f, new Color(0.95f, 0.9f, 0.75f));
        }

        /// <summary>Número de la casa a partir de su dirección («Plaza 13 (…)» → «13»).</summary>
        private static string HouseNumber(string address)
        {
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(address ?? string.Empty, @"\d+(-\d+)?");
            return match.Success ? match.Value : null;
        }

        /// <summary>Azulejo blanco con marco azul y el número de la casa en azul.</summary>
        private static void AddNumberTile(GenerationContext ctx, GameObject group, EnvironmentMaterials m, string id, string number,
            Vector3 position, Vector2 normal, Vector2 along)
        {
            float yaw = PlanarGeometry.Yaw(along);
            var n3 = new Vector3(normal.x, 0f, normal.y);
            float width = number.Length > 2 ? 0.34f : 0.24f;
            var builder = new MeshBuilder(2);
            builder.AddOrientedBox(position + n3 * 0.025f, new Vector3(width + 0.04f, 0.24f, 0.03f), yaw, 1);
            builder.AddOrientedBox(position + n3 * 0.042f, new Vector3(width - 0.02f, 0.18f, 0.01f), yaw, 0);
            GameObject tile = ctx.Output.AddStatic(HierarchyNames.Of("Azulejo", number), builder, $"SM_Azulejo_{id}", new[] { m.WallWhite, m.TileBlue }, group, decorative: true);
            SignText.AddLabel(tile, number, SignFont.Plain, position + n3 * 0.055f, n3, width - 0.06f, 0.025f, new Color(0.1f, 0.22f, 0.6f));
        }

        /// <summary>Radio del arco de las portadas (la mitad de su luz).</summary>
        private const float PortalRadius = 0.8f;

        /// <summary>
        /// Portada de arco de medio punto con puerta de madera de dos hojas. Según el tipo: rosca de piedra; tres
        /// arquivoltas labradas con columnillas; o alfiz rectangular labrado alrededor del arco.
        /// </summary>
        internal static void AddPortal(MeshBuilder builder, Vector2 at, Vector2 normal, Vector2 along, float ground, PortalKind kind)
        {
            const float radius = PortalRadius;
            float spring = ground + 2.1f;
            float yaw = PlanarGeometry.Yaw(along);
            Vector3 n3 = new Vector3(normal.x, 0f, normal.y);
            Vector3 u3 = new Vector3(along.x, 0f, along.y);
            Vector3 c = new Vector3(at.x, 0f, at.y);

            // Hojas de la puerta: rectángulo hasta el arranque y medio punto encima.
            Vector3 door = c + n3 * 0.04f;
            builder.AddOrientedBox(door + Vector3.up * ((ground + spring) * 0.5f), new Vector3(radius * 2f, spring - ground, 0.04f), yaw, 1);
            var top = new List<Vector3>();
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.PI * i / 12;
                top.Add(door + n3 * 0.021f + u3 * (Mathf.Cos(a) * radius) + Vector3.up * (spring + Mathf.Sin(a) * radius));
            }

            builder.AddConvexFace(top, n3, u3, 1);
            builder.AddOrientedBox(door + n3 * 0.03f + Vector3.up * ((ground + spring + radius) * 0.5f), new Vector3(0.05f, spring + radius - ground, 0.03f), yaw, 2);

            int rings = kind == PortalKind.Archivolts ? 3 : 1;
            for (int r = 0; r < rings; r++)
            {
                float inner = radius + r * 0.2f;
                float outer = inner + (kind == PortalKind.Archivolts ? 0.18f : 0.28f);
                float depth = 0.06f + (rings - r) * 0.05f;
                AddRing(builder, c + n3 * depth, u3, n3, spring, inner, outer, 0);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 jamb = c + n3 * depth + u3 * (side * (inner + outer) * 0.5f);
                    if (kind == PortalKind.Archivolts)
                    {
                        builder.AddLathe(new Vector3(jamb.x, 0f, jamb.z), new[]
                        {
                            new Vector2(0.09f, ground), new Vector2(0.08f, spring - 0.15f), new Vector2(0.12f, spring - 0.1f),
                            new Vector2(0.12f, spring), new Vector2(0f, spring),
                        }, 10, 0);
                    }
                    else
                    {
                        builder.AddOrientedBox(jamb + Vector3.up * ((ground + spring) * 0.5f), new Vector3(outer - inner, spring - ground, 0.12f), yaw, 0);
                    }
                }
            }

            if (kind == PortalKind.Alfiz)
            {
                // Alfiz: marco rectangular labrado que encuadra el arco, con las enjutas rehundidas.
                float half = radius + 0.55f;
                float alfizTop = spring + radius + 0.45f;
                Vector3 face = c + n3 * 0.12f;
                builder.AddOrientedBox(face + Vector3.up * (alfizTop - 0.1f), new Vector3(half * 2f + 0.2f, 0.2f, 0.14f), yaw, 0);
                foreach (float side in new[] { -1f, 1f })
                {
                    builder.AddOrientedBox(face + u3 * (side * half) + Vector3.up * ((ground + alfizTop) * 0.5f), new Vector3(0.2f, alfizTop - ground, 0.14f), yaw, 0);
                }
            }
        }

        /// <summary>Rosca de arco de medio punto entre dos radios, en un plano paralelo a la fachada.</summary>
        private static void AddRing(MeshBuilder builder, Vector3 centre, Vector3 u3, Vector3 n3, float spring, float inner, float outer, int slot)
        {
            const int steps = 14;
            for (int i = 0; i < steps; i++)
            {
                float a0 = Mathf.PI * i / steps;
                float a1 = Mathf.PI * (i + 1) / steps;
                Vector3 P(float a, float r) => centre + u3 * (Mathf.Cos(a) * r) + Vector3.up * (spring + Mathf.Sin(a) * r);
                builder.AddConvexFace(new[] { P(a0, inner), P(a1, inner), P(a1, outer), P(a0, outer) }, n3, u3, slot);
                builder.AddConvexFace(new[] { P(a0, inner), P(a1, inner), P(a1, inner) - n3 * 0.1f, P(a0, inner) - n3 * 0.1f }, -Vector3.up, u3, slot);
            }
        }
    }
}
