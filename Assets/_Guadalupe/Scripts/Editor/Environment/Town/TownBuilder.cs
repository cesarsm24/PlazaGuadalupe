using System.Collections.Generic;
using UnityEngine;
using static Guadalupe.Editor.Environment.HouseMeshSlots;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Construye las casas del pueblo a partir de las partes de edificio del Catastro: muros encalados
    /// con zócalo, cubiertas con alero, huecos de fachada y soportales.
    /// </summary>
    /// <remarks>
    /// Coordina el proceso; cada tarea concreta está en su propia clase:
    /// <list type="bullet">
    /// <item><see cref="ArcadeAnalysis"/>: qué planta baja queda abierta por soportales y pasos de calle.</item>
    /// <item><see cref="EaveAligner"/>: aleros igualados entre casas contiguas.</item>
    /// <item><see cref="FacadeDetailer"/> y <see cref="FacadeOrnaments"/>: huecos y elementos de fachada.</item>
    /// <item><see cref="ColonnadeBuilder"/> y <see cref="StreetArchBuilder"/>: soportales y arcos de calle.</item>
    /// <item><see cref="ShopfrontBuilder"/>: rótulos, portadas y género de los comercios.</item>
    /// </list>
    /// </remarks>
    internal static class TownBuilder
    {
        internal const string GroupName = "Edificios";

        /// <summary>Vuelo del alero sobre los muros que dan a la calle.</summary>
        internal const float EaveOverhang = 0.45f;

        /// <summary>Genera todas las partes de edificio dentro del radio de trabajo.</summary>
        public static void Build(GenerationContext ctx)
        {
            GameObject group = ctx.Output.CreateGroup(GroupName);
            List<BuildingFootprint> parts = SelectTownParts(ctx);
            Dictionary<BuildingFootprint, ArcadePart> arcade = ArcadeAnalysis.Analyse(ctx, parts);
            Dictionary<BuildingFootprint, float> eaves = EaveAligner.Align(ctx, parts, arcade);
            var shopfronts = new ShopfrontCollector();

            // Dos detalladores: el de las plantas altas ve las plantas completas; el de la planta baja ve las
            // plantas ya recortadas, de modo que los muros que dan al pasillo del soportal reciben puertas.
            var slots = new FacadeSlots(Zocalo, Window, Door, Metal, Glass, AwningRed, AwningGreen, Stone, Frame,
                pot: Brick, foliage: Leaves, windowLit: WindowLit);
            var detailer = new FacadeDetailer(parts.ConvertAll(p => p.Footprint), slots, ctx.Data.Settings.LevelHeight) { Balconies = ctx.Results.Balconies };
            var groundDetailer = new FacadeDetailer(GroundPolygons(parts, arcade), slots, ctx.Data.Settings.LevelHeight);
            var buildingNodes = new Dictionary<string, GameObject>();
            foreach (BuildingFootprint building in parts)
            {
                arcade.TryGetValue(building, out ArcadePart info);
                List<BuildingFootprint> siblings = parts.FindAll(p => p != building && p.BuildingId == building.BuildingId);
                AddBuilding(ctx, group, buildingNodes, building, siblings, info, detailer, groundDetailer, eaves, shopfronts);
            }

            // Las portadas y rótulos van antes que los pilares para que estos no queden delante de ninguna puerta.
            ShopfrontBuilder.Build(ctx, shopfronts);
            MarquesDetailsBuilder.BuildDoors(ctx, group);
            foreach (ArcadeSegment segment in PlazaLayout.Arcades)
            {
                ColonnadeBuilder.Build(ctx, segment, arcade, group);
            }

            foreach (StreetPassage passage in PlazaLayout.Passages)
            {
                StreetArchBuilder.BuildPassage(ctx, passage, arcade, group);
            }

            foreach (StreetBridge bridge in PlazaLayout.Bridges)
            {
                StreetArchBuilder.BuildBridge(ctx, bridge, group);
            }

            Physics.SyncTransforms();
            MarquesDetailsBuilder.BuildPlaque(ctx, group);
        }

        /// <summary>
        /// Partes de edificio del pueblo dentro del radio de trabajo. Lo que queda dentro del recinto del
        /// monasterio lo representa su modelo, y lo que cae dentro de la plaza son arcos o voladizos que no deben
        /// cerrar el paso.
        /// </summary>
        private static List<BuildingFootprint> SelectTownParts(GenerationContext ctx)
        {
            var parts = new List<BuildingFootprint>();
            foreach (BuildingFootprint building in ctx.Data.Buildings)
            {
                Vector2 centroid = PolygonUtils.Centroid(building.Footprint);
                bool inLonja = AtriumPlan.FloorLevelAt(PlanFrame.ToPlan(centroid)).HasValue;
                bool monastery = building.BuildingId == PlazaLayout.MonasteryCadastralRef;
                if (centroid.magnitude <= ctx.Data.Settings.WorkRadius && !inLonja && !monastery &&
                    !IsInside(ctx.Data.Osm.MonasteryOutline, centroid) && !ctx.Data.InPlaza(centroid))
                {
                    parts.Add(building);
                }
            }

            return parts;
        }

        /// <summary>Plantas bajas de las partes: recortadas por el soportal o completas si no lo tienen.</summary>
        private static List<List<Vector2>> GroundPolygons(List<BuildingFootprint> parts, Dictionary<BuildingFootprint, ArcadePart> arcade)
        {
            var polygons = new List<List<Vector2>>();
            foreach (BuildingFootprint part in parts)
            {
                if (!arcade.TryGetValue(part, out ArcadePart info))
                {
                    polygons.Add(part.Footprint);
                }
                else if (info.Ground != null)
                {
                    polygons.Add(info.Ground);
                }
            }

            return polygons;
        }

        /// <summary>
        /// Construye una parte de edificio como un objeto con una sola malla. Las casas de la plaza toman colores y
        /// elementos de su entrada en <see cref="HouseCatalog"/>; las demás, un aspecto variado y estable.
        /// </summary>
        /// <param name="siblings">Las demás partes del mismo edificio.</param>
        private static void AddBuilding(GenerationContext ctx, GameObject group, Dictionary<string, GameObject> nodes,
            BuildingFootprint building, List<BuildingFootprint> siblings, ArcadePart info, FacadeDetailer detailer, FacadeDetailer groundDetailer,
            Dictionary<BuildingFootprint, float> eaves, ShopfrontCollector shopfronts)
        {
            HouseStyle style = HouseCatalog.Find(building.BuildingId) ?? HouseCatalog.Generic(building.BuildingId);
            detailer.Style = style;
            groundDetailer.Style = style;
            var builder = new MeshBuilder(HouseMeshSlots.Count);
            BuildPart(ctx, builder, building, siblings, detailer, groundDetailer, info, style, eaves, shopfronts);

            Material[] slotMaterials = HouseMeshSlots.MaterialsFor(ctx.Data.Materials, style, building.BuildingId);
            ctx.Output.AddStatic(PartName(building), builder, $"SM_{building.Id}", slotMaterials, BuildingNode(group, nodes, building, style));
            ctx.Results.BuildingCount++;
            if (info != null)
            {
                ctx.Results.PorticoCount++;
            }
        }

        /// <summary>
        /// Nodo de la jerarquía que agrupa las partes catastrales de un edificio, nombrado por su referencia
        /// catastral y, si la tiene en el catálogo, su dirección.
        /// </summary>
        private static GameObject BuildingNode(GameObject group, Dictionary<string, GameObject> nodes, BuildingFootprint building, HouseStyle style)
        {
            if (!nodes.TryGetValue(building.BuildingId, out GameObject node))
            {
                node = new GameObject(HierarchyNames.Of($"Edificio_{building.BuildingId}", style.Address));
                node.transform.SetParent(group.transform, false);
                nodes.Add(building.BuildingId, node);
            }

            return node;
        }

        /// <summary>Nombre legible de una parte catastral: "9898206TJ9699N_part2" pasa a "Parte_02".</summary>
        private static string PartName(BuildingFootprint building)
        {
            int index = building.Id.LastIndexOf("_part", System.StringComparison.Ordinal);
            return index >= 0 && int.TryParse(building.Id.Substring(index + "_part".Length), out int number)
                ? HierarchyNames.Numbered("Parte", number)
                : building.Id;
        }

        /// <summary>Construye una parte: muros, cubierta y fachadas, con su planta baja recortada si tiene soportal.</summary>
        private static void BuildPart(GenerationContext ctx, MeshBuilder builder, BuildingFootprint building,
            List<BuildingFootprint> siblings, FacadeDetailer detailer, FacadeDetailer groundDetailer, ArcadePart arcade, HouseStyle style,
            Dictionary<BuildingFootprint, float> eaves, ShopfrontCollector shopfronts)
        {
            List<Vector2> footprint = building.Footprint;
            ctx.GroundRange(footprint, out float minY, out float maxY);
            float baseY = minY - ctx.Data.Settings.FoundationDepth;
            bool detailed = PolygonUtils.Centroid(footprint).magnitude < ctx.Data.Settings.DetailRadius;
            int floors = EaveAligner.FloorsOf(building, style, arcade);
            float eaveY = EaveAligner.EaveHeight(maxY, floors, ctx.Data.Settings.LevelHeight);
            if (eaves.TryGetValue(building, out float aligned))
            {
                eaveY = Mathf.Max(eaveY, aligned);
            }

            float upperStart = maxY + ctx.Data.Settings.LevelHeight;

            if (arcade != null)
            {
                // Planta baja recortada hasta el techo del soportal; encima, la casa completa en voladizo.
                upperStart = arcade.Top;
                eaveY = Mathf.Max(eaveY, arcade.Top + 1.2f);
                if (arcade.Ground != null)
                {
                    builder.AddWalls(arcade.Ground, baseY, arcade.Top, Wall);
                }

                builder.AddWalls(footprint, arcade.Top, eaveY, Wall);
                builder.AddCap(footprint, arcade.Top, Beam, facingUp: false);
            }
            else
            {
                builder.AddWalls(footprint, baseY, eaveY, Wall);
            }

            // La azotea es solo una franja a lo largo de la fachada de la plaza; detrás, la casa lleva tejado.
            List<int> terraceFront = style != null && style.Parapet
                ? BuildingShapes.FindFrontingEdges(footprint, ctx.Data.Osm.PlazaOutline, 2.5f)
                : null;
            if (terraceFront != null && terraceFront.Count > 0)
            {
                List<Vector2> roofed = BuildingShapes.SplitFrontStrip(footprint, terraceFront, TerraceDepth,
                    out List<Vector2> strip, out List<int> innerEdges);
                // Abiertos el lado que da al tejado de detrás y los que dan a otra parte de la misma casa, que
                // continúa la azotea o el tejado sin pretil en medio.
                List<Vector2> terrace = strip ?? footprint;
                var openEdges = innerEdges ?? new List<int>();
                openEdges.AddRange(EdgesFacing(terrace, siblings));
                FacadeOrnaments.AddRoofTerrace(builder, terrace, detailer, eaveY, openEdges);
                if (roofed != null)
                {
                    // Detrás de la azotea la casa sube una planta más, sin ventanas. La única puerta a la azotea va
                    // en la parte con azotea más grande de la casa, en su lado más largo que da a ella.
                    float topY = eaveY + ctx.Data.Settings.LevelHeight;
                    builder.AddWalls(roofed, eaveY, topY, Wall);
                    int door = -1;
                    for (int i = 0; i < roofed.Count; i++)
                    {
                        Vector2 a = roofed[i];
                        Vector2 b = roofed[(i + 1) % roofed.Count];
                        bool facesTerrace = PolygonUtils.Contains(terrace, (a + b) * 0.5f + Outward(a, b) * 0.4f);
                        if (facesTerrace && (door < 0 || Vector2.Distance(a, b) > Vector2.Distance(roofed[door], roofed[(door + 1) % roofed.Count])))
                        {
                            door = i;
                        }
                    }

                    float area = PolygonUtils.SignedArea(footprint);
                    bool largest = siblings.TrueForAll(p => PolygonUtils.SignedArea(p.Footprint) <= area
                        || BuildingShapes.FindFrontingEdges(p.Footprint, ctx.Data.Osm.PlazaOutline, 2.5f).Count == 0);
                    if (door >= 0 && largest)
                    {
                        detailer.AddTerraceDoor(builder, roofed[door], roofed[(door + 1) % roofed.Count], eaveY);
                    }

                    AddPitchedRoof(builder, roofed, footprint, detailer, topY);
                }
            }
            else
            {
                AddPitchedRoof(builder, footprint, footprint, detailer, eaveY);
            }

            if (detailed)
            {
                AddUpperFacades(ctx, builder, building, floors, detailer, arcade, upperStart, eaveY, style);
                AddGroundFacades(ctx, builder, building, groundDetailer, arcade, upperStart, style, shopfronts);
            }
        }

        /// <summary>Lados del polígono que dan a alguna de las partes dadas.</summary>
        private static IEnumerable<int> EdgesFacing(List<Vector2> polygon, List<BuildingFootprint> parts)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                Vector2 probe = (a + b) * 0.5f + Outward(a, b) * 0.4f;
                if (parts.Exists(p => PolygonUtils.Contains(p.Footprint, probe)))
                {
                    yield return i;
                }
            }
        }

        /// <summary>Fondo de la azotea desde la fachada de la plaza.</summary>
        private const float TerraceDepth = 3f;

        /// <summary>
        /// Tejado con alero sobre los lados que dan a la calle (no sobre las medianeras) y, en algunas casas, chimenea.
        /// </summary>
        /// <param name="outline">Polígono que se cubre: la planta entera o la parte de detrás de una azotea.</param>
        /// <param name="footprint">Planta de la casa, para no tomar por medianera el lado que da a su propia azotea.</param>
        private static void AddPitchedRoof(MeshBuilder builder, List<Vector2> outline, List<Vector2> footprint, FacadeDetailer detailer, float eaveY)
        {
            var streetEdges = new List<int>();
            for (int i = 0; i < outline.Count; i++)
            {
                if (!detailer.IsPartyWall(outline[i], outline[(i + 1) % outline.Count], footprint))
                {
                    streetEdges.Add(i);
                }
            }

            float rise = BuildingShapes.AddRoof(builder, outline, eaveY, Roof, EaveOverhang, Wood, streetEdges);
            AddChimney(builder, outline, eaveY, rise);
        }

        /// <summary>
        /// Chimenea encalada con losa y sombrerete de teja en una de cada tres partes con tejado de más de 40 m²,
        /// elegidas de forma estable por la posición de su centroide. Sale de la cubierta junto al centroide,
        /// donde la altura del tejado es la que devuelve <see cref="BuildingShapes.AddRoof"/>.
        /// </summary>
        private static void AddChimney(MeshBuilder builder, List<Vector2> footprint, float eaveY, float roofRise)
        {
            Vector2 c = PolygonUtils.Centroid(footprint);
            int hash = (Mathf.RoundToInt(c.x * 7f) * 73856093 ^ Mathf.RoundToInt(c.y * 7f) * 19349663) & int.MaxValue;
            if (roofRise < 0.8f || PolygonUtils.SignedArea(footprint) < 40f || hash % 3 != 0)
            {
                return;
            }

            // Algo apartada del centroide hacia un vértice para no salir justo por el vértice de un tejado piramidal.
            Vector2 p = c + (footprint[hash % footprint.Count] - c) * 0.18f;
            if (!PolygonUtils.Contains(footprint, p))
            {
                return;
            }

            float top = eaveY + roofRise + 0.9f;
            float shaft = top - eaveY;
            builder.AddBox(ToWorld(p, eaveY + shaft * 0.5f), new Vector3(0.6f, shaft, 0.6f), Wall);
            builder.AddBox(ToWorld(p, top + 0.03f), new Vector3(0.8f, 0.06f, 0.8f), Stone);
            foreach (float dx in new[] { -0.28f, 0.28f })
            {
                builder.AddBox(ToWorld(p + new Vector2(dx, 0f), top + 0.16f), new Vector3(0.08f, 0.2f, 0.5f), Wall);
            }

            builder.AddBox(ToWorld(p, top + 0.3f), new Vector3(0.75f, 0.1f, 0.7f), Roof);
        }

        /// <summary>Ventanas, balcones y galerías de las plantas altas en los lados que no son medianera.</summary>
        private static void AddUpperFacades(GenerationContext ctx, MeshBuilder builder, BuildingFootprint building, int floors,
            FacadeDetailer detailer, ArcadePart arcade, float upperStart, float eaveY, HouseStyle style)
        {
            List<Vector2> footprint = building.Footprint;
            List<int> plazaFacing = BuildingShapes.FindFrontingEdges(footprint, ctx.Data.Osm.PlazaOutline, 2.5f);
            bool nearPlaza = PolygonUtils.Centroid(footprint).magnitude < ctx.Data.Settings.ShopRadius;
            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 a = footprint[i];
                Vector2 b = footprint[(i + 1) % footprint.Count];
                if (detailer.IsPartyWall(a, b, footprint))
                {
                    continue;
                }

                // Lado de fachada principal: el que da a la plaza o al pasillo del soportal.
                bool front = plazaFacing.Contains(i) || (arcade?.Segment != null && ArcadeAnalysis.IsArcadeFront(arcade.Segment, a, b));
                float balconies = front ? style?.BalconyChance ?? 0.5f : style != null ? style.BalconyChance * 0.4f : 0.15f;
                detailer.AddWindows(builder, a, b, upperStart - ctx.Data.Settings.LevelHeight, 1, floors, eaveY, balconies, $"{building.Id}#{i}");
                FacadeOrnaments.AddQuoins(ctx, builder, footprint, i, detailer, eaveY, style);
                if (style != null && style.WoodenGallery && (front || nearPlaza) && Vector2.Distance(a, b) > 3f)
                {
                    FacadeOrnaments.AddWoodenGallery(builder, a, b, upperStart, ctx.Results.Balconies);
                }

                if (front || nearPlaza)
                {
                    // No sobre los frentes de soportal: el farol chocaría con la viga y los pilares.
                    if (arcade?.Segment == null || !ArcadeAnalysis.IsArcadeFront(arcade.Segment, a, b))
                    {
                        detailer.AddWallLanterns(ctx, builder, a, b, upperStart + 0.2f, front ? 8f : 14f);
                    }

                    FacadeOrnaments.AddCable(builder, a, b, upperStart + 2.45f);
                }
            }
        }

        /// <summary>
        /// Planta baja: en el fondo del soportal, escaparates y puertas; hacia la calle, puertas, ventanas con
        /// reja y toldos; tejaroz si el catálogo lo indica.
        /// </summary>
        private static void AddGroundFacades(GenerationContext ctx, MeshBuilder builder, BuildingFootprint building,
            FacadeDetailer groundDetailer, ArcadePart arcade, float ceilingY, HouseStyle style, ShopfrontCollector shopfronts)
        {
            List<Vector2> polygon = arcade != null ? arcade.Ground : building.Footprint;
            if (polygon == null)
            {
                return;
            }

            List<int> plazaFacing = BuildingShapes.FindFrontingEdges(polygon, ctx.Data.Osm.PlazaOutline, 2.5f);
            bool nearPlaza = PolygonUtils.Centroid(polygon).magnitude < ctx.Data.Settings.ShopRadius;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                if (Vector2.Distance(a, b) < 1.2f || groundDetailer.IsPartyWall(a, b, polygon))
                {
                    continue;
                }

                string seed = $"{building.Id}#g{i}";
                if (ArcadeAnalysis.FacesPassage(a, b))
                {
                    // Paredes interiores de un arco de calle: lisas, sin huecos.
                    continue;
                }

                groundDetailer.AddZocalo(ctx, builder, a, b);
                FacadeOrnaments.AddGrime(ctx, builder, a, b);
                if (ArcadeAnalysis.FacesCorridor(a, b, ctx.Data.Settings.ArcadeCorridorDepth))
                {
                    // Fondo del soportal: escaparates de los comercios y su rótulo encima.
                    float top = arcade != null ? arcade.Top : ceilingY;
                    if (style != null && style.ArcadeDoors)
                    {
                        groundDetailer.AddGroundFloor(ctx, builder, a, b, top, 0f, seed);
                    }
                    else
                    {
                        groundDetailer.AddShopFronts(ctx, builder, a, b, top);
                    }

                    shopfronts.Propose(building, style, a, b, top - 0.45f);
                    continue;
                }

                bool facesPlaza = plazaFacing.Contains(i);
                float awnings = style != null && style.Tejaroz || ArcadeAnalysis.NearArcade(a, b) ? 0f : facesPlaza ? 0.6f : nearPlaza ? 0.25f : 0f;
                float groundTop = Mathf.Max(ctx.GroundY(a.x, a.y), ctx.GroundY(b.x, b.y));
                shopfronts.Propose(building, style, a, b, groundTop + 2.75f);

                groundDetailer.AddGroundFloor(ctx, builder, a, b, groundTop + ctx.Data.Settings.LevelHeight, awnings, seed);
                if (style != null && style.Tejaroz && (facesPlaza || nearPlaza))
                {
                    FacadeOrnaments.AddTejaroz(builder, a, b, groundTop + 2.75f);
                }
            }
        }
    }
}
