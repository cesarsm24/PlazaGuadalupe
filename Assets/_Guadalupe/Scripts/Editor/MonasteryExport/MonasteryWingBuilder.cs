using System.Collections.Generic;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.MonasteryExport.MonasteryWingSlots;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Construye lo que el monasterio tiene al oeste del atrio, a partir de sus partes del Catastro:
    /// la casa blanca de dos plantas sobre la lonja, el cuerpo de mampostería parda con sus dos torres
    /// redondas de chapitel vidriado verde, y la portería (<see cref="PorteriaBuilder"/>).
    /// </summary>
    internal static class MonasteryWingBuilder
    {
        internal const string GroupName = "MonasterioOeste";

        /// <summary>Altura del alero del cuerpo de mampostería sobre la lonja.</summary>
        private const float WingEaveAboveTerrace = 13f;

        /// <summary>Lo que las torres redondas sobresalen por encima del alero del cuerpo.</summary>
        private const float RoundTowerExtra = 3.5f;

        /// <summary>Superficie máxima (m²) de una parte que se trata como torre redonda.</summary>
        private const float RoundTowerMaxArea = 40f;

        /// <summary>Partes mayores que esta superficie son el recinto completo, ya representado por su volumen.</summary>
        private const float PrecinctMinArea = 2000f;

        /// <summary>Genera el ala oeste del monasterio, y la portería.</summary>
        public static void Build(GenerationContext ctx)
        {
            GameObject group = ctx.Output.CreateGroup(GroupName);
            float terraceY = ctx.Results.TerraceTopY;
            float wingEave = terraceY + WingEaveAboveTerrace;

            foreach (BuildingFootprint part in ctx.Data.Buildings)
            {
                if (part.BuildingId != PlazaLayout.MonasteryCadastralRef)
                {
                    continue;
                }

                List<Vector2> footprint = part.Footprint;
                Vector2 centroid = PolygonUtils.Centroid(footprint);
                float area = PolygonUtils.SignedArea(footprint);
                if (centroid.magnitude > ctx.Data.Settings.WorkRadius || area > PrecinctMinArea)
                {
                    continue;
                }

                var builder = new MeshBuilder(MonasteryWingSlots.Count);
                if (part.Id == PlazaLayout.AtriumHousePart)
                {
                    BuildAtriumHouse(ctx, builder, footprint, terraceY);
                }
                else if (area < RoundTowerMaxArea)
                {
                    BuildRoundTower(ctx, builder, footprint, wingEave + RoundTowerExtra);
                }
                else if (!PolygonUtils.Contains(ctx.Data.Osm.MonasteryOutline, centroid))
                {
                    BuildMasonryBlock(ctx, builder, footprint, wingEave);
                }

                if (!builder.IsEmpty)
                {
                    AddToScene(ctx, builder, part.Id, group);
                }
            }

            var porteria = new MeshBuilder(MonasteryWingSlots.Count);
            PorteriaBuilder.Build(porteria, terraceY);
            AddToScene(ctx, porteria, "Porteria", group);
        }

        private static void AddToScene(GenerationContext ctx, MeshBuilder builder, string name, GameObject group)
        {
            Material[] slots = MonasteryWingSlots.MaterialsFor(ctx.Data.Materials);
            ctx.Output.AddStatic(name, builder, $"SM_{name}", slots, group);
        }

        /// <summary>
        /// Casa blanca junto al arco de la lonja, a pie de la avenida de Alfonso Onceno: tres plantas encaladas
        /// sobre zócalo de sillería, esquinas de cantería, ventanas con rejas voladas en las dos plantas bajas,
        /// ventanucos bajo el alero y tejado de teja con alero de madera.
        /// </summary>
        private static void BuildAtriumHouse(GenerationContext ctx, MeshBuilder builder, List<Vector2> footprint, float terraceY)
        {
            ctx.GroundRange(footprint, out float minGround, out _);
            float streetY = AvenueLevel(ctx, footprint, minGround);
            const int floors = 3;
            float plinthTop = streetY + 0.8f;
            float eave = streetY + floors * ctx.Data.Settings.LevelHeight + 0.3f;
            builder.AddWalls(footprint, minGround - ctx.Data.Settings.FoundationDepth, plinthTop, Ashlar);
            builder.AddWalls(footprint, plinthTop, eave, Lime);
            BuildingShapes.AddRoof(builder, footprint, eave, Roof, 0.6f, Eave);

            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 a = footprint[i];
                Vector2 b = footprint[(i + 1) % footprint.Count];
                float length = Vector2.Distance(a, b);
                if (length < 1.5f)
                {
                    continue;
                }

                Vector2 dir = (b - a) / length;
                var outward = new Vector2(dir.y, -dir.x);
                float yaw = PlanarGeometry.Yaw(dir);

                // Esquinas de sillería y cornisa bajo el alero.
                Vector2 corner = a + outward * 0.03f;
                builder.AddOrientedBox(new Vector3(corner.x, (plinthTop + eave) * 0.5f, corner.y), new Vector3(0.55f, eave - plinthTop, 0.55f), yaw, Ashlar);
                Vector2 mid = (a + b) * 0.5f + outward * 0.08f;
                builder.AddOrientedBox(new Vector3(mid.x, eave - 0.12f, mid.y), new Vector3(length, 0.24f, 0.16f), yaw, Ashlar);

                if (outward.x > 0.7f && length > 4f)
                {
                    AddAtriumSide(builder, a, dir, outward, length, yaw, terraceY);
                }
                else if (outward.y < -0.6f || outward.x < -0.7f)
                {
                    AddStreetSide(ctx, builder, a, b, dir, outward, length);
                }
            }
        }

        /// <summary>La casa arranca de la avenida: su altura de referencia es la del suelo ante la fachada sur.</summary>
        private static float AvenueLevel(GenerationContext ctx, List<Vector2> footprint, float minGround)
        {
            float streetY = minGround;
            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 m = (footprint[i] + footprint[(i + 1) % footprint.Count]) * 0.5f;
                Vector2 d = (footprint[(i + 1) % footprint.Count] - footprint[i]).normalized;
                if (d.x > 0.6f)
                {
                    streetY = Mathf.Max(streetY, ctx.GroundY(m.x, m.y));
                }
            }

            return streetY;
        }

        /// <summary>
        /// Fachada al atrio (este): sobre la lonja, planta de mampostería con ventana enrejada y encima el paño
        /// encalado con reja volada.
        /// </summary>
        private static void AddAtriumSide(MeshBuilder builder, Vector2 a, Vector2 dir, Vector2 outward, float length, float yaw, float atrium)
        {
            Vector2 c = a + dir * (length * 0.5f) + outward * 0.03f;
            builder.AddOrientedBox(new Vector3(c.x, atrium + 1.35f, c.y), new Vector3(length - 0.6f, 2.9f, 0.04f), yaw, Masonry);
            AddGrilledWindow(builder, a, dir, outward, length * 0.55f, 1.15f, atrium + 3.6f, 1.55f, 0.45f);
            AddGrilledWindow(builder, a, dir, outward, length * 0.55f, 0.7f, atrium + 1.1f, 0.9f, 0.06f);
        }

        /// <summary>
        /// Fachadas a la avenida (sur) y a la calle (oeste): ventanas enrejadas en tres plantas y la puerta en el
        /// último hueco de la fachada sur.
        /// </summary>
        private static void AddStreetSide(GenerationContext ctx, MeshBuilder builder, Vector2 a, Vector2 b, Vector2 dir, Vector2 outward, float length)
        {
            float edgeGround = Mathf.Max(ctx.GroundY(a.x, a.y), ctx.GroundY(b.x, b.y));
            int count = Mathf.Max(1, Mathf.FloorToInt(length / 3.6f));
            for (int w = 0; w < count; w++)
            {
                float t = length * (w + 0.5f) / count;
                if (w == count - 1 && outward.y < -0.6f)
                {
                    AddDoor(builder, a, dir, outward, t, 1.2f, edgeGround, 2.3f);
                }
                else
                {
                    AddGrilledWindow(builder, a, dir, outward, t, 0.9f, edgeGround + 1.3f, 1.2f, 0.32f);
                }

                AddGrilledWindow(builder, a, dir, outward, t, 1.15f, edgeGround + ctx.Data.Settings.LevelHeight + 0.6f, 1.7f, 0.42f);
                AddGrilledWindow(builder, a, dir, outward, t, 0.7f, edgeGround + 2f * ctx.Data.Settings.LevelHeight + 0.9f, 0.7f, 0.05f);
            }
        }

        /// <summary>
        /// Torre redonda de mampostería con imposta y chapitel cónico de teja vidriada verde, rematado
        /// en bola y cruz de hierro, como las dos del lado oeste del monasterio.
        /// </summary>
        private static void BuildRoundTower(GenerationContext ctx, MeshBuilder builder, List<Vector2> footprint, float eave)
        {
            ctx.GroundRange(footprint, out float minGround, out _);
            Vector2 c2 = PolygonUtils.Centroid(footprint);
            float radius = Mathf.Sqrt(PolygonUtils.SignedArea(footprint) / Mathf.PI);
            var center = new Vector3(c2.x, 0f, c2.y);
            const int segments = 24;

            builder.AddLathe(center, new[]
            {
                new Vector2(radius, minGround - ctx.Data.Settings.FoundationDepth), new Vector2(radius, eave - 1.6f),
                new Vector2(radius + 0.25f, eave - 1.4f), new Vector2(radius + 0.25f, eave - 1.1f), new Vector2(radius, eave - 1.1f),
                new Vector2(radius, eave),
            }, segments, Masonry);
            builder.AddLathe(center, new[] { new Vector2(radius + 0.3f, eave), new Vector2(radius + 0.3f, eave + 0.25f), new Vector2(radius - 0.1f, eave + 0.25f) }, segments, Ashlar);

            // Chapitel esbelto con ligero vuelo acampanado en la base.
            float spire = radius * 3.2f;
            builder.AddLathe(center, new[]
            {
                new Vector2(radius + 0.35f, eave + 0.25f), new Vector2(radius + 0.05f, eave + 0.8f),
                new Vector2(radius * 0.55f, eave + 0.25f + spire * 0.5f), new Vector2(0.1f, eave + 0.25f + spire), new Vector2(0f, eave + 0.3f + spire),
            }, segments, GlazedTile);
            float top = eave + 0.3f + spire;
            builder.AddLathe(center, new[]
            {
                new Vector2(0f, top - 0.02f), new Vector2(0.14f, top + 0.06f), new Vector2(0.2f, top + 0.2f), new Vector2(0.14f, top + 0.34f), new Vector2(0f, top + 0.4f),
            }, 10, Ashlar);
            builder.AddBox(center + Vector3.up * (top + 0.85f), new Vector3(0.05f, 0.9f, 0.05f), Iron);
            builder.AddBox(center + Vector3.up * (top + 1.05f), new Vector3(0.45f, 0.05f, 0.05f), Iron);

            // Saetera alta mirando a la plaza.
            Vector3 slit = center + new Vector3(0f, eave - 3.5f, -radius - 0.02f);
            builder.AddBox(slit, new Vector3(0.25f, 1.2f, 0.04f), Opening);
        }

        /// <summary>
        /// Cuerpo de mampostería parda, alto y casi ciego, con contrafuertes y ventanales apuntados en la
        /// fachada a la plaza y tejado de teja sobre su envolvente.
        /// </summary>
        private static void BuildMasonryBlock(GenerationContext ctx, MeshBuilder builder, List<Vector2> footprint, float eave)
        {
            ctx.GroundRange(footprint, out float minGround, out _);
            builder.AddWalls(footprint, minGround - ctx.Data.Settings.FoundationDepth, eave, Masonry);

            // Las torres redondas están embebidas en el contorno; el tejado va sobre la envolvente convexa.
            List<Vector2> hull = PolygonUtils.ConvexHull(footprint);
            var cornice = BuildingShapes.InsetEdges(hull, PlanarGeometry.AllEdges(hull), -0.25f);
            if (cornice != null)
            {
                builder.AddWalls(cornice, eave - 0.35f, eave, Ashlar);
                builder.AddCap(cornice, eave - 0.35f, Ashlar, facingUp: false);
            }

            BuildingShapes.AddRoof(builder, hull, eave, Roof, 0.3f, Ashlar);

            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 a = footprint[i];
                Vector2 b = footprint[(i + 1) % footprint.Count];
                float length = Vector2.Distance(a, b);
                Vector2 dir = (b - a) / length;
                var outward = new Vector2(dir.y, -dir.x);
                if (length < 8f || outward.y > -0.6f)
                {
                    continue;
                }

                // Fachada sur: dos contrafuertes y dos ventanales apuntados altos entre ellos.
                float yaw = PlanarGeometry.Yaw(dir);
                float groundY = Mathf.Max(ctx.GroundY(a.x, a.y), ctx.GroundY(b.x, b.y));
                foreach (float f in new[] { 0.33f, 0.67f })
                {
                    Vector2 p = a + dir * (length * f) + outward * 0.45f;
                    builder.AddOrientedBox(new Vector3(p.x, (minGround - 1f + eave - 1f) * 0.5f, p.y), new Vector3(0.9f, eave - minGround, 0.9f), yaw, Masonry);
                    ArchitecturalElements.AddSpire(builder, new Vector3(p.x, eave - 1f, p.y), 0.5f, 4, 2.2f, Ashlar);
                }

                foreach (float f in new[] { 0.17f, 0.5f, 0.83f })
                {
                    Vector2 p = a + dir * (length * f) + outward * 0.03f;
                    var along = new Vector3(dir.x, 0f, dir.y);
                    var facing = new Vector3(outward.x, 0f, outward.y);
                    ArchitecturalElements.AddPointedOpeningOriented(builder, new Vector3(p.x, 0f, p.y), Mathf.Max(groundY + 7f, eave - 6.5f), 1.3f, 3.4f, facing, along, Ashlar);
                    ArchitecturalElements.AddPointedOpeningOriented(builder, new Vector3(p.x, 0f, p.y) + facing * 0.02f, Mathf.Max(groundY + 7.15f, eave - 6.35f), 0.95f, 3.1f, facing, along, Opening);
                }
            }
        }

        /// <summary>Ventana con marco de piedra y reja saliente de barrotes.</summary>
        /// <param name="projection">Vuelo de la reja: las rejas voladas forman una jaula delante del hueco.</param>
        private static void AddGrilledWindow(MeshBuilder builder, Vector2 a, Vector2 dir, Vector2 outward, float along,
            float width, float bottom, float height, float projection = 0.16f)
        {
            float yaw = PlanarGeometry.Yaw(dir);
            Vector2 c = a + dir * along;
            Vector3 At(float depth, float y) => new Vector3(c.x + outward.x * depth, y, c.y + outward.y * depth);

            builder.AddOrientedBox(At(0.02f, bottom + height * 0.5f), new Vector3(width, height, 0.04f), yaw, Opening);
            builder.AddOrientedBox(At(0.06f, bottom - 0.08f), new Vector3(width + 0.5f, 0.16f, 0.12f), yaw, Ashlar);
            builder.AddOrientedBox(At(0.06f, bottom + height + 0.1f), new Vector3(width + 0.5f, 0.2f, 0.12f), yaw, Ashlar);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 jamb = c + dir * (side * (width * 0.5f + 0.12f));
                builder.AddOrientedBox(new Vector3(jamb.x + outward.x * 0.06f, bottom + height * 0.5f, jamb.y + outward.y * 0.06f),
                    new Vector3(0.24f, height, 0.12f), yaw, Ashlar);
            }

            int bars = Mathf.Max(3, Mathf.RoundToInt(width / 0.14f));
            for (int i = 1; i < bars; i++)
            {
                Vector2 bar = c + dir * (-width * 0.5f + width * i / bars);
                builder.AddOrientedBox(new Vector3(bar.x + outward.x * projection, bottom + height * 0.5f, bar.y + outward.y * projection),
                    new Vector3(0.025f, height + 0.1f, 0.025f), yaw, Iron);
            }

            foreach (float f in new[] { 0.08f, 0.5f, 0.92f })
            {
                builder.AddOrientedBox(At(projection, bottom + height * f), new Vector3(width + 0.1f, 0.03f, 0.03f), yaw, Iron);
            }

            if (projection > 0.2f)
            {
                // Jaula de la reja volada: laterales, peana y remate.
                foreach (float side in new[] { -0.5f, 0.5f })
                {
                    Vector2 s = c + dir * (side * (width + 0.1f));
                    for (float k = 0.25f; k <= 1.01f; k += 0.25f)
                    {
                        builder.AddOrientedBox(new Vector3(s.x + outward.x * projection * k, bottom + height * 0.5f, s.y + outward.y * projection * k),
                            new Vector3(0.025f, height + 0.1f, 0.025f), yaw, Iron);
                    }
                }

                builder.AddOrientedBox(At(projection * 0.5f, bottom - 0.04f), new Vector3(width + 0.3f, 0.08f, projection + 0.1f), yaw, Ashlar);
                builder.AddOrientedBox(At(projection * 0.5f, bottom + height + 0.08f), new Vector3(width + 0.2f, 0.04f, projection), yaw, Iron);
            }
        }

        private static void AddDoor(MeshBuilder builder, Vector2 a, Vector2 dir, Vector2 outward, float along, float width, float bottom, float height)
        {
            float yaw = PlanarGeometry.Yaw(dir);
            Vector2 c = a + dir * along;
            builder.AddOrientedBox(new Vector3(c.x + outward.x * 0.03f, bottom + height * 0.5f, c.y + outward.y * 0.03f), new Vector3(width, height, 0.06f), yaw, DoorWood);
            builder.AddOrientedBox(new Vector3(c.x + outward.x * 0.06f, bottom + height + 0.12f, c.y + outward.y * 0.06f), new Vector3(width + 0.5f, 0.24f, 0.12f), yaw, Ashlar);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 jamb = c + dir * (side * (width * 0.5f + 0.13f));
                builder.AddOrientedBox(new Vector3(jamb.x + outward.x * 0.06f, bottom + height * 0.5f, jamb.y + outward.y * 0.06f),
                    new Vector3(0.26f, height, 0.12f), yaw, Ashlar);
            }
        }
    }
}
