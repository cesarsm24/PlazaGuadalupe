using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Detalles singulares de dos casas de la calle Marqués de la Romana: en el soportal de la 6-8, la puerta de
    /// arco y la placa de azulejos del pilar; en la 4, el cierre de tijera de la puerta junto a Las
    /// Villuercas. Se colocan buscando con rayos los muros ya construidos: las puertas antes que los pilares del
    /// soportal (para que estos no las tapen) y la placa después, en el primer pilar junto a la puerta.
    /// </summary>
    internal static class MarquesDetailsBuilder
    {
        /// <summary>Distancia a lo largo del soportal de la 6-8 (desde su extremo junto al atrio) de la puerta de arco.</summary>
        private const float ArchDoorStation = 4.9f;


        /// <summary>Punto de la fachada de la 4 donde está la puerta del cierre de tijera.</summary>
        private static readonly Vector2 ScissorGateAt = new Vector2(27.4f, 23.0f);

        /// <summary>Puerta de arco de la 6-8 y cierre de tijera de la 4; se apuntan como puertas.</summary>
        internal static void BuildDoors(GenerationContext ctx, GameObject group)
        {
            EnvironmentMaterials m = ctx.Data.Materials;
            if (Arcade(out Vector2 a, out Vector2 dir, out Vector2 inward))
            {
                // Puerta de arco en el fondo del soportal.
                if (FirstHit(ctx, a + dir * ArchDoorStation - inward * 3f, inward, false, out RaycastHit wall))
                {
                    var at = new Vector2(wall.point.x, wall.point.z);
                    var normal = new Vector2(wall.normal.x, wall.normal.z).normalized;
                    Vector2 along = new Vector2(-normal.y, normal.x);
                    float ground = ctx.GroundY(at.x, at.y);
                    var builder = new MeshBuilder(3);
                    ShopfrontBuilder.AddPortal(builder, at, normal, along, ground, PortalKind.RoundArch);
                    ctx.Output.AddStatic(HierarchyNames.Of("Portada", "Marqués de la Romana 6-8"), builder, "SM_PortadaMarques68", new[] { m.WallAshlar, m.Door, m.Granite }, group, decorative: true);
                    ctx.Results.Doorways.Add(new Vector4(at.x, at.y, along.x * 0.8f, along.y * 0.8f));
                }
            }

            // Cierre de tijera en la puerta de la 4.
            Vector2 street = ScissorGateAt + new Vector2(-0.7f, 0.75f) * 2f;
            if (FirstHit(ctx, street, (ScissorGateAt - street).normalized, false, out RaycastHit front))
            {
                AddScissorGate(ctx, group, m, front.point, front.normal);
                Vector2 side = new Vector2(-front.normal.z, front.normal.x).normalized * 0.65f;
                ctx.Results.Doorways.Add(new Vector4(front.point.x, front.point.z, side.x, side.y));
            }
        }

        /// <summary>Placa de azulejos en la cara a la calle del primer pilar del soportal subiendo desde la puerta de arco.</summary>
        internal static void BuildPlaque(GenerationContext ctx, GameObject group)
        {
            if (!Arcade(out Vector2 a, out Vector2 dir, out Vector2 inward))
            {
                return;
            }

            for (float s = ArchDoorStation + 0.6f; s < ArchDoorStation + 5f; s += 0.1f)
            {
                if (FirstHit(ctx, a + dir * s - inward * 3f, inward, true, out RaycastHit pillar) && pillar.distance < 4f)
                {
                    AddPlaque(ctx, group, ctx.Data.Materials, pillar.point, pillar.normal);
                    return;
                }
            }
        }

        /// <summary>Extremo del soportal de la 6-8 junto al atrio, su dirección y la normal hacia las casas.</summary>
        private static bool Arcade(out Vector2 start, out Vector2 dir, out Vector2 inward)
        {
            ArcadeSegment arcade = System.Array.Find(PlazaLayout.Arcades, s => s.Name == PlazaLayout.MarquesArcade);
            start = dir = inward = Vector2.zero;
            if (arcade == null)
            {
                return false;
            }

            start = arcade.Line[0];
            dir = (arcade.Line[arcade.Line.Length - 1] - start).normalized;
            inward = new Vector2(dir.y, -dir.x);
            return true;
        }

        /// <summary>Giro de una caja para que su eje X vaya a lo largo de un muro con esta normal.</summary>
        private static float WallYaw(Vector3 normal)
        {
            var along = new Vector2(-normal.z, normal.x);
            return PlanarGeometry.Yaw(along);
        }

        /// <summary>Primer muro (o pilar del soportal, si <paramref name="pillar"/>) que se encuentra a 1,2 m del suelo.</summary>
        private static bool FirstHit(GenerationContext ctx, Vector2 from, Vector2 dir, bool pillar, out RaycastHit result)
        {
            var origin = new Vector3(from.x, ctx.GroundY(from.x, from.y) + 1.2f, from.y);
            foreach (RaycastHit hit in ctx.Output.Raycast(origin, new Vector3(dir.x, 0f, dir.y), 10f))
            {
                bool isPillar = ctx.Results.Colonnades.Contains(hit.collider.gameObject);
                if (hit.collider is TerrainCollider || Mathf.Abs(hit.normal.y) > 0.3f || isPillar != pillar)
                {
                    continue;
                }

                result = hit;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>Placa de azulejos blancos con cenefa azul, escudo arriba y renglones de texto.</summary>
        private static void AddPlaque(GenerationContext ctx, GameObject group, EnvironmentMaterials m, Vector3 face, Vector3 normal)
        {
            var n = new Vector3(normal.x, 0f, normal.z).normalized;
            float yaw = WallYaw(n);
            Vector3 c = new Vector3(face.x, ctx.GroundY(face.x, face.z) + 2.15f, face.z) + n * 0.02f;
            var b = new MeshBuilder(3);
            b.AddOrientedBox(c, new Vector3(0.5f, 0.68f, 0.02f), yaw, 1);
            b.AddOrientedBox(c + n * 0.012f, new Vector3(0.44f, 0.62f, 0.01f), yaw, 0);
            b.AddOrientedBox(c + n * 0.02f + Vector3.up * 0.22f, new Vector3(0.14f, 0.12f, 0.01f), yaw, 1);
            for (int k = 0; k < 9; k++)
            {
                b.AddOrientedBox(c + n * 0.02f + Vector3.up * (0.1f - k * 0.045f), new Vector3(k == 8 ? 0.2f : 0.34f, 0.012f, 0.005f), yaw, 2);
            }

            ctx.Output.AddStatic(HierarchyNames.Of("PlacaAzulejos", "Marqués de la Romana 6-8"), b, "SM_PlacaMarques68", new[] { m.WallWhite, m.TileBlue, m.Door }, group, decorative: true);
        }

        /// <summary>Cierre de tijera dorado (rombos de pletina con guías arriba y abajo) delante de un hueco oscuro.</summary>
        private static void AddScissorGate(GenerationContext ctx, GameObject group, EnvironmentMaterials m, Vector3 face, Vector3 normal)
        {
            const float width = 1.3f, height = 2.25f, cell = 0.22f;
            var n = new Vector3(normal.x, 0f, normal.z).normalized;
            Vector3 u = Vector3.Cross(Vector3.up, n).normalized;
            float yaw = WallYaw(n);
            float ground = ctx.GroundY(face.x, face.z);
            Vector3 c = new Vector3(face.x, ground, face.z);
            var b = new MeshBuilder(2);

            // Hueco oscuro, con fondo suficiente para tapar lo que hubiera en el muro.
            b.AddOrientedBox(c + n * 0.06f + Vector3.up * (height * 0.5f), new Vector3(width, height, 0.14f), yaw, 0);
            Vector3 P(float x, float y) => c + n * 0.15f + u * x + Vector3.up * y;
            int cols = Mathf.RoundToInt(width / cell);
            float step = width / cols;
            for (float y = 0.05f; y < height - 0.05f; y += cell)
            {
                float y1 = Mathf.Min(y + cell, height - 0.05f);
                for (int k = 0; k < cols; k++)
                {
                    float x0 = -width * 0.5f + k * step;
                    ArchitecturalElements.AddBar(b, P(x0, y), P(x0 + step, y1), 0.018f, 1);
                    ArchitecturalElements.AddBar(b, P(x0 + step, y), P(x0, y1), 0.018f, 1);
                }
            }

            for (int k = 0; k <= cols; k++)
            {
                float x = -width * 0.5f + k * step;
                ArchitecturalElements.AddBar(b, P(x, 0.02f), P(x, height - 0.02f), 0.025f, 1);
            }

            ArchitecturalElements.AddBar(b, P(-width * 0.5f, height - 0.03f), P(width * 0.5f, height - 0.03f), 0.06f, 1);
            ArchitecturalElements.AddBar(b, P(-width * 0.5f, 0.03f), P(width * 0.5f, 0.03f), 0.05f, 1);

            ctx.Output.AddStatic(HierarchyNames.Of("CierreTijera", "Marqués de la Romana 4"), b, "SM_CierreMarques4", new[] { m.Shadow, m.Bronze }, group);
        }
    }
}
