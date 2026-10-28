using System.Collections.Generic;
using Guadalupe.Ambience;
using Guadalupe.Editor.Environment;
using UnityEngine;
using static Guadalupe.Editor.Environment.ArchitecturalElements;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Construye el monasterio ante la plaza: la fachada gótica de cuatro tramos (dos portadas y dos ventanales)
    /// con la galería y el paño del rosetón encima, el frente de la iglesia detrás y, al fondo, el cimborrio de
    /// chapitel piramidal; la torre de la portería al oeste, la torre del reloj al este y, detrás, el cuerpo del
    /// monasterio a partir de su contorno de OpenStreetMap.
    /// </summary>
    /// <remarks>
    /// Las alturas de la fachada y de las torres van en metros sobre la meseta de las portadas
    /// (<see cref="AtriumPlan.PortalLanding"/>). La fachada y la torre oeste se construyen en el marco de la
    /// fachada (X a lo largo del frente de los contrafuertes, Z hacia dentro) y la torre este en (u, v).
    /// La parte alta (galería, paño del rosetón, frente de la iglesia y cimborrio) se escalona en profundidad;
    /// la altura de cada cuerpo es la de su frente.
    /// </remarks>
    internal static class MonasteryBuilder
    {
        internal const string GroupName = "Monasterio";
        internal const string ClockNode = "Reloj";
        internal const string HourHandNode = "AgujaHoras";
        internal const string MinuteHandNode = "AgujaMinutos";

        // Submallas de la fachada: 0 sillería, 1 huecos oscuros, 2 bronce verde, 3 tejado, 4 relieve de tracería,
        // 5 mampostería (frente de la iglesia y cimborrio) y 6 la textura de la cara de la fachada.
        private const int Stone = 0;
        private const int Dark = 1;
        private const int Bronze = 2;
        private const int RoofSlot = 3;
        private const int Relief = 4;
        private const int Masonry = 5;
        private const int Face = 6;

        // Submallas de las torres: 0 piedra, 1 huecos, 2 esfera del reloj, 3 hierro, 4 sillería,
        // 5 cantería clara de recercados y losas de balcón.
        private const int ClockFace = 2;
        private const int Iron = 3;
        private const int Ashlar = 4;
        private const int Trim = 5;
        private const int TowerSlots = 6;

        private const float Wall = 1f; // Paño de la fachada, detrás del frente de los contrafuertes.

        // Cotas de la fachada, en metros sobre la meseta de las portadas.
        private const float FootLevel = -3.6f;          // Cota de nivel absoluta del pie de fachada y torres.
        private const float FriezeBottom = 9.9f;        // Cornisa bajo el friso de celosía.
        private const float LowerBodyTop = 13.2f;       // Cornisa del cuerpo bajo.
        private const float GalleryTop = 15.5f;         // Cornisa de la galería.
        private const float RosePanelTop = 17.6f;       // Cornisa del paño del rosetón.
        private const float ChurchFrontTop = 19.6f;     // Crestería del frente de la iglesia, tras la galería y el rosetón.
        private const float UpperBodyTop = 28f;         // Arranque del chapitel del cimborrio.
        private const float WestShaftTop = 19.7f;       // Fuste de la torre oeste, bajo la imposta del campanario.
        internal const float WestBelfryBottom = 20.3f;
        private const float WestBelfryTop = 23.2f;
        private const float WestCorniceTop = 23.6f;
        private const float EastShaftTop = 24.8f;
        private const float EastCrownBottom = 25.3f;
        private const float EastCrownTop = 30.3f;
        private const float EastBattlementBase = 30.6f;

        // Posiciones a lo largo del frente de la fachada.
        private const float GalleryEnd = 9.6f;          // Límite entre la galería (oeste) y el paño del rosetón.
        private const float RosePanelEnd = 17f;         // Límite este del paño del rosetón, junto a la torre del reloj.
        private const float UpperBodyAxis = 14.5f;      // Eje del cimborrio y del chapitel.
        private const float EastTowerAxis = 20.65f;     // Eje (u) de los huecos de la torre del reloj.

        // Profundidad de los cuerpos de la parte alta (Z hacia dentro).
        private const float ChurchFront = Wall + 3f;    // Frente de la iglesia, retranqueado sobre el cuerpo bajo.
        private const float CrossingFront = Wall + 12f; // Frente del cimborrio, sobre el crucero.

        // Cuadrado que cubre la textura de la cara de la fachada (T_GothicFacade): X a lo largo del frente desde 0 y
        // altura desde FaceBottom, de FaceSize m de lado.
        private const float FaceBottom = -2f;
        private const float FaceSize = 21.2f;

        // Punto de la textura con el color liso de la piedra: costados de las piezas y sillería fuera de la textura.
        private static readonly Vector2 FaceSideUv = new Vector2(20f / FaceSize, (19f - FaceBottom) / FaceSize);

        /// <summary>
        /// Vanos del frente del campanario de la torre oeste: desplazamiento desde el eje de la torre, ancho, alto y
        /// arranque sobre el suelo del campanario. Los de los lados son vanos de campana; el central, más pequeño, es
        /// una ventana con balconcillo.
        /// </summary>
        internal static readonly (float Offset, float Width, float Height, float Sill)[] WestBelfryFront =
        {
            (-1.85f, 0.9f, 2.4f, 0.15f), (0f, 0.8f, 1.6f, 0.75f), (2f, 0.9f, 2.4f, 0.15f),
        };

        /// <summary>Contrafuertes de la fachada (centro a lo largo del frente).</summary>
        internal static readonly float[] Buttresses = { 0.4f, 6f, 11.55f, 16.25f, 20.75f };

        /// <summary>
        /// Altura del contrafuerte <paramref name="index"/> sobre la meseta de las portadas, sin su pináculo: el que
        /// separa las portadas sube hasta la galería y los demás llegan a la cornisa del cuerpo bajo.
        /// </summary>
        internal static float ButtressTop(int index) => index == 1 ? GalleryTop : LowerBodyTop;

        public static void BuildMonastery(GenerationContext ctx)
        {
            GameObject group = ctx.Output.CreateGroup(GroupName);
            if (ctx.Data.Osm.MonasteryOutline.Count > 0)
            {
                BuildBody(ctx, group);
            }

            GameObject front = PlanFrame.CreateFrame(group, "Frente");
            GameObject facadeFrame = PlanFrame.CreateChildFrame(front, "Fachada", AtriumPlan.FacadeOrigin, AtriumPlan.FacadeYaw);
            BuildFacade(ctx, facadeFrame);
            BuildWestTower(ctx, facadeFrame);
            BuildEastTower(ctx, front);
        }

        private const float BodyLevelHeight = 4.6f; // Más que la planta de las casas.
        private const float EastWingLength = 16f;
        private const float OldBuildingHeight = 8.5f; // Alero del edificio que sigue al ala este, sobre el atrio.
        private const float OldBuildingLength = 12f;  // Frente hasta la esquina donde la calle gira.
        private const float OldBuildingDepth = 10f;

        /// <summary>
        /// Cuerpo del monasterio detrás de la fachada, de la misma mampostería que las torres. Junto a la torre del
        /// reloj, el ala este con sus balcones; al este de ella, un edificio más bajo y corto, con tejado de teja a
        /// cuatro aguas y una torrecilla, que hace esquina con la calle; pasada la esquina, el resto del monasterio
        /// queda retranqueado. Al oeste de las torres, ventanas con balcones de hierro.
        /// </summary>
        private static void BuildBody(GenerationContext ctx, GameObject group)
        {
            // El contorno de OSM asoma por delante de la fachada: el cuerpo empieza tras el paño de la fachada.
            float yaw = (PlanFrame.RotationDegrees + AtriumPlan.FacadeYaw) * Mathf.Deg2Rad;
            var inward = new Vector2(-Mathf.Sin(yaw), Mathf.Cos(yaw));
            Vector2 facadeLine = PlanFrame.ToWorld(AtriumPlan.FacadeOrigin) + inward * (Wall + 1.5f);
            List<Vector2> outline = PolygonUtils.ClipHalfPlane(ctx.Data.Osm.MonasteryOutline, facadeLine, inward);
            float baseY = ctx.Results.TerraceTopY;
            float top = baseY + AtriumPlan.MonasteryHeight;
            float lowTop = baseY + OldBuildingHeight;

            float westLimit = AtriumPlan.FacadeToPlan(new Vector2(AtriumPlan.WestTower.x, 0f)).x;
            float eastLimit = AtriumPlan.EastTower.y;
            float wingEnd = eastLimit + EastWingLength;
            Vector2 cut = PlanFrame.ToWorld(wingEnd, 0f);
            Vector2 east = PlanFrame.ToWorld(1f, 0f) - PlanFrame.ToWorld(0f, 0f);
            List<Vector2> high = PolygonUtils.ClipHalfPlane(outline, cut, -east);
            List<Vector2> low = PolygonUtils.ClipHalfPlane(outline, cut, east);

            // Frente del ala a la plaza (v del marco), del que salen el bloque viejo y el retranqueo.
            float frontV = float.NaN;
            foreach (Edge edge in SouthEdges(high))
            {
                if (edge.EastU > eastLimit + 1f)
                {
                    frontV = PlanFrame.ToPlan(Stretch(edge.A, edge.B, eastLimit, wingEnd).B).y;
                }
            }

            List<Vector2> oldBlock = new List<Vector2>();
            List<Vector2> rest = low;
            if (!float.IsNaN(frontV))
            {
                Vector2 north = PlanFrame.ToWorld(0f, 1f) - PlanFrame.ToWorld(0f, 0f);
                Vector2 back = PlanFrame.ToWorld(0f, frontV + OldBuildingDepth);
                oldBlock = PolygonUtils.ClipHalfPlane(PolygonUtils.ClipHalfPlane(low, back, -north),
                    PlanFrame.ToWorld(wingEnd + OldBuildingLength, 0f), -east);
                rest = PolygonUtils.ClipHalfPlane(low, back, north);
            }

            const int bodyStone = 0, bodyRoof = 1, bodyGlass = 2, bodyFrame = 3, bodyIron = 4, bodyTrim = 5;
            var body = new MeshBuilder(6);
            foreach ((List<Vector2> part, float partTop) in new[] { (high, top), (rest, lowTop) })
            {
                if (part.Count >= 3)
                {
                    ctx.GroundRange(part, out float minY, out _);
                    body.AddWalls(part, minY - ctx.Data.Settings.FoundationDepth, partTop, bodyStone);
                    BuildingShapes.AddRoof(body, part, partTop, bodyRoof);
                }
            }

            if (oldBlock.Count >= 3)
            {
                ctx.GroundRange(oldBlock, out float minY, out _);
                body.AddWalls(oldBlock, minY - ctx.Data.Settings.FoundationDepth, lowTop, bodyStone);
                BuildingShapes.AddRoof(body, oldBlock, lowTop, bodyRoof, 0.5f, bodyTrim);
            }

            // El cuerpo no tiene zócalo ni toldos: esos índices usan la piedra.
            var slots = new FacadeSlots(zocalo: bodyStone, window: bodyGlass, door: bodyFrame, metal: bodyIron,
                glass: bodyGlass, awningRed: bodyStone, awningGreen: bodyStone, stone: bodyTrim, frame: bodyFrame);
            // Muros de mampostería con pocos huecos: ventanas bien separadas, con recercado claro de cantería, sin
            // frontón ni contraventanas, y los balcones en la planta alta, bajo la cornisa.
            var detailer = new FacadeDetailer(new List<List<Vector2>>(), slots, BodyLevelHeight)
            {
                Balconies = ctx.Results.MonasteryBalconies,
                WindowSpacing = 6.5f,
                TopFloorBalconies = true,
                Pediments = false,
                Style = new HouseStyle { StoneSurrounds = true, ShutterChance = 0f },
            };

            foreach (Edge edge in SouthEdges(high))
            {
                if (edge.EastU > eastLimit + 1f)
                {
                    // El lado arranca detrás de la fachada: las ventanas empiezan en la torre del reloj.
                    (Vector2 a, Vector2 b) = Stretch(edge.A, edge.B, eastLimit, wingEnd);
                    AddEastWingWindows(detailer, body, a, b, baseY);
                    AddCornice(body, a, b, top, bodyTrim);
                }
                else if (edge.U < westLimit)
                {
                    // Como al este, el lado sigue detrás de la torre: la fachada empieza en su cara oeste.
                    (Vector2 a, Vector2 b) = Stretch(edge.A, edge.B, float.MinValue, westLimit);
                    AddWestSide(body, a, b, baseY, bodyStone, bodyRoof, bodyGlass, bodyTrim);
                    AddCornice(body, edge.A, edge.B, top, bodyTrim);
                }
            }

            foreach (Edge edge in SouthEdges(oldBlock))
            {
                AddOldBuildingWindows(detailer, body, edge.A, edge.B, baseY);
            }

            foreach (Edge edge in SouthEdges(rest))
            {
                // Detrás del bloque viejo el muro no se ve: las ventanas empiezan pasada la esquina.
                (Vector2 a, Vector2 b) = Stretch(edge.A, edge.B, wingEnd + OldBuildingLength, float.MaxValue);
                AddOldBuildingWindows(detailer, body, a, b, baseY);
                AddCornice(body, edge.A, edge.B, lowTop, bodyTrim);
            }

            if (oldBlock.Count >= 3)
            {
                AddTurret(body, PlanFrame.ToWorld(wingEnd + OldBuildingLength * 0.8f, frontV + OldBuildingDepth * 0.35f),
                    PlanarGeometry.Yaw(east), lowTop, bodyStone, bodyRoof, bodyTrim, bodyIron);
            }

            Material[] slotMaterials =
            {
                ctx.Data.Materials.Stone, ctx.Data.Materials.Roof, ctx.Data.Materials.Window, ctx.Data.Materials.WindowFrame,
                ctx.Data.Materials.Metal, ctx.Data.Materials.WallCream,
            };
            ctx.Output.AddStatic("Cuerpo", body, "SM_CuerpoMonasterio", slotMaterials, group);
        }

        private readonly struct Edge
        {
            public Edge(int index, Vector2 a, Vector2 b)
            {
                Index = index;
                A = a;
                B = b;
                U = PlanFrame.ToPlan((a + b) * 0.5f).x;
                EastU = Mathf.Max(PlanFrame.ToPlan(a).x, PlanFrame.ToPlan(b).x);
            }

            public int Index { get; }
            public Vector2 A { get; }
            public Vector2 B { get; }
            public float U { get; }
            public float EastU { get; }
        }

        private static IEnumerable<Edge> SouthEdges(List<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                // Normal exterior (dy, -dx) con componente sur.
                if ((b - a).normalized.x > 0.6f)
                {
                    yield return new Edge(i, a, b);
                }
            }
        }

        /// <summary>
        /// Frente del ala este: la planta baja ciega; encima, dos ventanas grandes enrejadas; luego cinco ventanas
        /// casi cuadradas; y arriba tres balconeras, la primera con su balcón y las otras dos con uno corrido.
        /// Alturas sobre el atrio.
        /// </summary>
        private static void AddEastWingWindows(FacadeDetailer detailer, MeshBuilder body, Vector2 a, Vector2 b, float atriumY)
        {
            const float bay = 3f; // Separación mínima: en los 16 m del ala caben cinco ejes.
            const float balconyFloor = 12.1f;

            detailer.AddWindowRow(body, a, b, atriumY, 3.2f, 1.6f, 2.6f, bay, every: 2, first: 1, grille: true);
            detailer.AddWindowRow(body, a, b, atriumY, 7.35f, 1.2f, 1.5f, bay, every: 1, first: 0);
            detailer.AddWindowRow(body, a, b, atriumY + balconyFloor, 0f, 1.2f, 2.1f, bay, every: 2, first: 0);
            detailer.AddBalconyRun(body, a, b, atriumY + balconyFloor, bay, 0, 0);
            detailer.AddBalconyRun(body, a, b, atriumY + balconyFloor, bay, 2, 4);
        }

        /// <summary>
        /// Lo que asoma al oeste de la torre, de ella hacia fuera: un cuerpo con tres ventanas estrechas de medio
        /// punto y un óculo debajo; un bloque más alto, con tejado, una ventana de medio punto y un pináculo esbelto en
        /// su esquina; y después un cuerpo más bajo con un ventanuco y otro pináculo. Alturas sobre el atrio.
        /// </summary>
        private static void AddWestSide(MeshBuilder body, Vector2 a, Vector2 b, float atriumY, int stone, int roof, int glass, int trim)
        {
            const float blockFrom = 3.2f, blockTo = 9.5f, blockHeight = 19.6f, blockDepth = 8f, blockFront = 0.3f;

            // Las medidas van desde el extremo pegado a la torre (el de mayor u) hacia el oeste.
            bool fromA = PlanFrame.ToPlan(a).x > PlanFrame.ToPlan(b).x;
            Vector2 start = fromA ? a : b;
            Vector2 west = ((fromA ? b : a) - start).normalized;
            Vector2 outward2 = new Vector2(b.y - a.y, a.x - b.x).normalized;
            float length = Vector2.Distance(a, b);
            var facing = new Vector3(outward2.x, 0f, outward2.y);
            var along = new Vector3(west.x, 0f, west.y);
            Vector3 At(float s, float depth) => new Vector3(start.x + west.x * s, 0f, start.y + west.y * s) + facing * depth;

            void Arched(float s, float bottom, float width, float height, float wall = 0f)
            {
                ArchitecturalElements.AddRoundOpeningOriented(body, At(s, wall + 0.03f), atriumY + bottom - 0.15f, width + 0.3f, height + 0.3f, facing, along, trim);
                ArchitecturalElements.AddRoundOpeningOriented(body, At(s, wall + 0.05f), atriumY + bottom, width, height, facing, along, glass);
            }

            if (length < blockTo + 2f)
            {
                return;
            }

            foreach (float s in new[] { 0.6f, 1.7f, 2.8f })
            {
                Arched(s, 13f, 0.7f, 1.6f);
            }

            ArchitecturalElements.AddDiscOriented(body, At(1.7f, 0.03f) + Vector3.up * (atriumY + 10.9f), 1.15f, facing, along, trim);
            ArchitecturalElements.AddDiscOriented(body, At(1.7f, 0.05f) + Vector3.up * (atriumY + 10.9f), 0.85f, facing, along, glass);

            // El bloque alto sale un poco del muro y lleva su propio tejado.
            var block = new List<Vector2>();
            foreach ((float s, float depth) in new[] { (blockFrom, blockFront), (blockTo, blockFront), (blockTo, -blockDepth), (blockFrom, -blockDepth) })
            {
                Vector3 p = At(s, depth);
                block.Add(new Vector2(p.x, p.z));
            }

            if (PolygonUtils.SignedArea(block) < 0f)
            {
                block.Reverse();
            }

            body.AddWalls(block, atriumY + 10f, atriumY + blockHeight, stone);
            BuildingShapes.AddRoof(body, block, atriumY + blockHeight, roof, 0.4f, trim);
            Arched(6.5f, 16.3f, 0.8f, 1.8f, blockFront);
            Arched(11.5f, 14.5f, 0.6f, 1.4f);

            AddPinnacle(body, At(blockFrom + 0.2f, blockFront + 0.1f), atriumY + blockHeight, 2.2f, 2.6f, trim);
            AddPinnacle(body, At(blockTo + 0.3f, 0.4f), atriumY + AtriumPlan.MonasteryHeight, 1.6f, 2f, trim);
        }

        /// <summary>Pináculo octogonal esbelto de piedra clara: caña, imposta, aguja y bola.</summary>
        private static void AddPinnacle(MeshBuilder body, Vector3 foot, float baseY, float shaft, float spire, int stone)
        {
            var center = new Vector3(foot.x, 0f, foot.z);
            float shaftTop = baseY + shaft;
            body.AddLathe(center, new[]
            {
                new Vector2(0.45f, baseY - 1f), new Vector2(0.45f, shaftTop - 0.3f), new Vector2(0.58f, shaftTop - 0.2f),
                new Vector2(0.58f, shaftTop), new Vector2(0.4f, shaftTop),
            }, 8, stone);
            ArchitecturalElements.AddSpire(body, center + Vector3.up * shaftTop, 0.4f, 8, spire, stone);
            ArchitecturalElements.AddBall(body, center + Vector3.up * (shaftTop + spire + 0.1f), 0.13f, stone);
        }

        /// <summary>Edificio viejo al este del ala: ventanas enrejadas bien separadas y, entre ellas, algún ventanuco.</summary>
        private static void AddOldBuildingWindows(FacadeDetailer detailer, MeshBuilder body, Vector2 a, Vector2 b, float atriumY)
        {
            const float bay = 4.5f;
            detailer.AddWindowRow(body, a, b, atriumY, 3.2f, 1.4f, 2.1f, bay, every: 1, first: 0, grille: true);
            detailer.AddWindowRow(body, a, b, atriumY, 6f, 0.9f, 1f, bay, every: 2, first: 1);
        }

        /// <summary>
        /// Torrecilla cuadrada sobre el tejado del edificio viejo: caña de mampostería con cornisa, chapitel de teja,
        /// bola y cruz.
        /// </summary>
        private static void AddTurret(MeshBuilder body, Vector2 center, float yaw, float eave, int stone, int roof, int trim, int iron)
        {
            const float side = 2.4f;
            const float shaft = 6f; // Sale del tejado a cuatro aguas.
            float top = eave + shaft;
            body.AddOrientedBox(new Vector3(center.x, eave - 1f + (shaft + 1f) * 0.5f, center.y), new Vector3(side, shaft + 1f, side), yaw, stone);
            body.AddOrientedBox(new Vector3(center.x, top + 0.12f, center.y), new Vector3(side + 0.4f, 0.24f, side + 0.4f), yaw, trim);
            var cap = new Vector3(center.x, top + 0.24f, center.y);
            ArchitecturalElements.AddSpire(body, cap, side * 0.75f, 4, 1.8f, roof);
            body.AddBox(cap + Vector3.up * 1.95f, new Vector3(0.3f, 0.3f, 0.3f), trim);
            body.AddBox(cap + Vector3.up * 2.55f, new Vector3(0.05f, 0.9f, 0.05f), iron);
            body.AddBox(cap + Vector3.up * 2.75f, new Vector3(0.4f, 0.05f, 0.05f), iron);
        }

        /// <summary>Tramo del lado a→b entre las abscisas u del marco dadas, con el sentido del lado.</summary>
        private static (Vector2 A, Vector2 B) Stretch(Vector2 a, Vector2 b, float fromU, float toU)
        {
            float ua = PlanFrame.ToPlan(a).x;
            float ub = PlanFrame.ToPlan(b).x;
            float t0 = Mathf.InverseLerp(ua, ub, Mathf.Max(fromU, Mathf.Min(ua, ub)));
            float t1 = Mathf.InverseLerp(ua, ub, Mathf.Min(toU, Mathf.Max(ua, ub)));
            return (Vector2.Lerp(a, b, Mathf.Min(t0, t1)), Vector2.Lerp(a, b, Mathf.Max(t0, t1)));
        }

        /// <summary>
        /// Fachada gótica: cinco contrafuertes con pináculos; dos portadas abocinadas con puertas de bronce y
        /// tímpano calado y dos ventanales de cuatro luces; friso de celosía; encima, al oeste, la galería con el
        /// tejado de la capilla y, al este, el paño del rosetón; detrás, el frente de la iglesia con su crestería y,
        /// al fondo, el cimborrio con ventanal, pináculos y chapitel piramidal de teja con linterna; y la torrecilla
        /// de la escalera entre la galería y el rosetón. Su cara lleva la textura de la fachada.
        /// </summary>
        private static void BuildFacade(GenerationContext ctx, GameObject frame)
        {
            float y0 = PlanFrame.Y(ctx, AtriumPlan.PortalLanding);
            float foot = PlanFrame.Y(ctx, FootLevel);
            float width = AtriumPlan.FacadeWidth;
            float H(float h) => y0 + h;
            var f = new MeshBuilder(7);

            // Volúmenes: paño bajo de lado a lado; encima, la galería al oeste y el paño del rosetón (al lado de la
            // torre del reloj, mampostería); detrás, el frente de la iglesia y, al fondo, el cimborrio.
            AddBlock(f, 0f, width, foot, H(LowerBodyTop), Wall, Wall + 2.5f, Stone);
            AddBlock(f, 0.8f, GalleryEnd, H(LowerBodyTop), H(GalleryTop), Wall + 0.3f, ChurchFront, Stone);
            AddBlock(f, GalleryEnd, RosePanelEnd, H(LowerBodyTop), H(RosePanelTop), Wall, ChurchFront, Stone);
            AddBlock(f, RosePanelEnd, width, H(LowerBodyTop), H(RosePanelTop), Wall, ChurchFront, Masonry);
            AddBlock(f, 6.9f, RosePanelEnd, H(LowerBodyTop), H(ChurchFrontTop), ChurchFront, CrossingFront, Masonry);
            AddBlock(f, 9.7f, 19.3f, H(ChurchFrontTop), H(UpperBodyTop), CrossingFront, CrossingFront + 9.8f, Masonry);
            AddLeanToRoof(f, 0.8f, GalleryEnd, H(GalleryTop + 0.2f), H(17f), Wall + 0.3f, ChurchFront);

            // Chapitel con linterna y cruz.
            const float spireZ = CrossingFront + 4.9f;
            AddSpire(f, new Vector3(UpperBodyAxis, H(UpperBodyTop), spireZ), 6.9f, 4, 7f, RoofSlot);
            f.AddBox(new Vector3(UpperBodyAxis, H(35.5f), spireZ), new Vector3(0.8f, 1.2f, 0.8f), Stone);
            AddSpire(f, new Vector3(UpperBodyAxis, H(36.1f), spireZ), 0.5f, 8, 1.4f, RoofSlot);
            f.AddBox(new Vector3(UpperBodyAxis, H(38.1f), spireZ), new Vector3(0.05f, 1.2f, 0.05f), Stone);
            f.AddBox(new Vector3(UpperBodyAxis, H(38.3f), spireZ), new Vector3(0.5f, 0.05f, 0.05f), Stone);

            AddButtresses(f, H);
            AddPortal(f, 3.2f, H);
            AddPortal(f, 8.78f, H);
            AddTraceryWindow(f, 13.9f, H);
            AddTraceryWindow(f, 18.5f, H);

            // Cornisas, friso de celosía y crestería sobre el cuerpo bajo.
            foreach (float h in new[] { FriezeBottom, LowerBodyTop })
            {
                f.AddBox(new Vector3(width * 0.5f, H(h), Wall - 0.15f), new Vector3(width, 0.3f, 0.3f), Stone);
            }

            AddLattice(f, 0.9f, width - 0.6f, H(FriezeBottom + 0.45f), Wall - 0.05f);
            AddCresting(f, 0.9f, width - 0.5f, H(LowerBodyTop + 0.15f), Wall - 0.12f);

            AddGallery(f, H);
            AddRosePanel(f, H);
            AddChurchFront(f, H);
            AddUpperBody(f, H);

            // Torrecilla octogonal de la escalera entre la galería y el cuerpo alto.
            var turret = new List<Vector2>();
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) * Mathf.PI / 4f;
                turret.Add(new Vector2(9.25f + 0.85f * Mathf.Cos(a), Wall + 0.9f + 0.85f * Mathf.Sin(a)));
            }

            f.AddExtrusion(turret, foot, H(20.3f), Stone, Stone);
            AddSpire(f, new Vector3(9.25f, H(20.3f), Wall + 0.9f), 0.9f, 8, 1.5f, Stone);

            // La textura de la fachada cubre el cuerpo bajo, la galería y el paño del rosetón: su cara y, con el color de
            // la piedra, los costados de contrafuertes, cornisas y molduras.
            var faceArea = new Rect(0f, H(FaceBottom), FaceSize, FaceSize);
            Rect[] faceZones =
            {
                Rect.MinMaxRect(0f, foot, width, H(LowerBodyTop + 0.6f)),
                Rect.MinMaxRect(0.8f, H(LowerBodyTop), GalleryEnd, H(GalleryTop + 0.2f)),
                Rect.MinMaxRect(GalleryEnd, H(LowerBodyTop), RosePanelEnd, H(RosePanelTop + 0.3f)),
            };
            f.ProjectFacing(Vector3.back, faceArea, faceZones, FaceSideUv, new[] { Stone, Dark, Bronze, Relief }, Face);

            // El resto de la sillería (pináculos, crestería, torrecilla, cornisas altas, cimborrio) es la misma piedra: va
            // en el mismo material, con su color liso.
            f.MoveToSubmesh(new[] { Stone, Relief }, Face, FaceSideUv);

            // La sillería y el relieve acaban en la textura de la fachada: sus submallas quedan vacías y no llevan material.
            Material[] slotMaterials =
            {
                null, ctx.Data.Materials.Shadow, ctx.Data.Materials.GreenBronze, ctx.Data.Materials.Roof,
                null, ctx.Data.Materials.Stone, ctx.Data.Materials.GothicFacade,
            };
            ctx.Output.AddStatic("FachadaGotica", f, "SM_FachadaGotica", slotMaterials, frame);
        }

        /// <summary>Contrafuertes con columnillas, doseletes y pináculos; el que separa las portadas sube hasta la galería.</summary>
        private static void AddButtresses(MeshBuilder f, System.Func<float, float> H)
        {
            for (int i = 0; i < Buttresses.Length; i++)
            {
                float x = Buttresses[i];
                float top = ButtressTop(i);
                bool tall = top > LowerBodyTop;
                AddBlock(f, x - 0.45f, x + 0.45f, H(-2f), H(top), 0f, Wall, Stone);
                foreach (float side in new[] { -0.3f, 0.3f })
                {
                    f.AddBox(new Vector3(x + side, H(top * 0.5f), -0.03f), new Vector3(0.09f, top, 0.09f), Relief);
                }

                foreach (float h in new[] { 4.6f, 10.2f, 15.6f })
                {
                    if (h > top - 1f)
                    {
                        continue;
                    }

                    f.AddBox(new Vector3(x, H(h), -0.06f), new Vector3(0.62f, 0.14f, 0.16f), Relief);
                    AddPointedOpening(f, x, H(h - 1.1f), 0.42f, 1.05f, -0.01f, Relief);
                    AddSpire(f, new Vector3(x, H(h + 0.07f), -0.06f), 0.12f, 4, 0.55f, Relief);
                }

                // Pináculo, más alto en el que sube hasta la galería.
                float pinnacleHeight = tall ? 2.2f : 1.8f;
                f.AddBox(new Vector3(x, H(top + pinnacleHeight * 0.5f), 0.45f), new Vector3(0.55f, pinnacleHeight, 0.55f), Stone);
                AddSpire(f, new Vector3(x, H(top + pinnacleHeight), 0.45f), 0.4f, 4, tall ? 1.6f : 1.5f, Stone);
            }
        }

        /// <summary>Portada abocinada: arquivoltas, jambas con columnillas, puerta de bronce con parteluz y tímpano calado.</summary>
        private static void AddPortal(MeshBuilder f, float cx, System.Func<float, float> H)
        {
            AddPointedOpening(f, cx, H(0f), 4.5f, 9.9f, Wall - 0.02f, Relief);
            AddPointedOpening(f, cx, H(0f), 4.1f, 9.6f, Wall - 0.03f, Stone);
            AddPointedOpening(f, cx, H(0f), 3.6f, 9.2f, Wall - 0.04f, Dark);
            AddPointedOpening(f, cx, H(0f), 2.6f, 5.6f, Wall - 0.08f, Bronze);
            f.AddBox(new Vector3(cx, H(2.8f), Wall - 0.11f), new Vector3(0.2f, 5.6f, 0.08f), Stone);
            f.AddBox(new Vector3(cx, H(5.7f), Wall - 0.1f), new Vector3(3.4f, 0.25f, 0.1f), Stone);

            // Tímpano: tres rosetas entrelazadas.
            foreach (Vector2 o in new[] { new Vector2(-0.78f, 6.9f), new Vector2(0.78f, 6.9f), new Vector2(0f, 8.25f) })
            {
                AddDisc(f, new Vector3(cx + o.x, H(o.y), Wall - 0.1f), 0.66f, Relief);
                AddDisc(f, new Vector3(cx + o.x, H(o.y), Wall - 0.11f), 0.48f, Dark);
                AddDisc(f, new Vector3(cx + o.x, H(o.y), Wall - 0.12f), 0.2f, Relief);
            }

            float shaftTop = 9.6f - 4.1f * 0.866f;
            for (int k = 0; k < 4; k++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    float sx = cx + side * (1.35f + k * 0.18f);
                    f.AddBox(new Vector3(sx, H(shaftTop * 0.5f), Wall - 0.12f - k * 0.02f), new Vector3(0.13f, shaftTop, 0.13f), Stone);
                    f.AddBox(new Vector3(sx, H(0.15f), Wall - 0.14f - k * 0.02f), new Vector3(0.2f, 0.3f, 0.2f), Relief);
                }
            }
        }

        /// <summary>Ventanal de cuatro luces con parteluces y rosa en la cabeza, sobre una arquería ciega.</summary>
        private static void AddTraceryWindow(MeshBuilder f, float cx, System.Func<float, float> H)
        {
            AddPointedOpening(f, cx, H(0f), 3.8f, 9.7f, Wall - 0.02f, Relief);
            AddPointedOpening(f, cx, H(0.2f), 3.4f, 9.3f, Wall - 0.03f, Stone);
            AddPointedOpening(f, cx, H(2.8f), 2.9f, 6.2f, Wall - 0.04f, Dark);
            foreach (float m in new[] { -0.725f, 0f, 0.725f })
            {
                f.AddBox(new Vector3(cx + m, H(4.7f), Wall - 0.07f), new Vector3(0.09f, 3.8f, 0.06f), Stone);
            }

            for (int l = 0; l < 4; l++)
            {
                AddPointedOpening(f, cx - 1.09f + l * 0.725f, H(6f), 0.55f, 0.85f, Wall - 0.08f, Stone);
            }

            AddDisc(f, new Vector3(cx, H(7.9f), Wall - 0.08f), 0.65f, Stone);
            AddDisc(f, new Vector3(cx, H(7.9f), Wall - 0.09f), 0.45f, Dark);
            AddDisc(f, new Vector3(cx, H(7.9f), Wall - 0.1f), 0.15f, Stone);
            for (int l = 0; l < 5; l++)
            {
                AddPointedOpening(f, cx - 1.36f + l * 0.68f, H(0.4f), 0.5f, 2.2f, Wall - 0.06f, Relief);
            }
        }

        // Los arquillos de la galería están en la textura.
        private static void AddGallery(MeshBuilder f, System.Func<float, float> H)
        {
            float z = Wall + 0.3f;
            f.AddBox(new Vector3(5.2f, H(GalleryTop), z - 0.15f), new Vector3(8.8f, 0.3f, 0.3f), Stone);
            AddCresting(f, 1.2f, 9.3f, H(GalleryTop + 0.15f), z - 0.12f);
        }

        // El arco y la tracería del rosetón ya están en la textura: aquí solo la cornisa y el fuste del lado este.
        private static void AddRosePanel(MeshBuilder f, System.Func<float, float> H)
        {
            f.AddBox(new Vector3((GalleryEnd + RosePanelEnd) * 0.5f, H(RosePanelTop), Wall - 0.15f), new Vector3(RosePanelEnd - GalleryEnd, 0.3f, 0.3f), Stone);
            AddBlock(f, RosePanelEnd - 0.3f, RosePanelEnd + 0.3f, H(LowerBodyTop), H(ChurchFrontTop), Wall + 0.1f, Wall + 0.7f, Stone);
            AddSpire(f, new Vector3(RosePanelEnd, H(ChurchFrontTop), Wall + 0.4f), 0.35f, 4, 1f, Stone);
        }

        /// <summary>Crestería del frente de la iglesia, que asoma tras la galería y el rosetón, con dos pináculos mayores.</summary>
        private static void AddChurchFront(MeshBuilder f, System.Func<float, float> H)
        {
            f.AddBox(new Vector3((6.9f + RosePanelEnd) * 0.5f, H(ChurchFrontTop), ChurchFront - 0.15f), new Vector3(RosePanelEnd - 6.9f, 0.3f, 0.3f), Stone);
            AddCresting(f, 7.2f, RosePanelEnd - 0.4f, H(ChurchFrontTop + 0.15f), ChurchFront - 0.12f);
            foreach (float x in new[] { 12f, 14.3f })
            {
                f.AddBox(new Vector3(x, H(ChurchFrontTop + 0.6f), ChurchFront + 0.25f), new Vector3(0.5f, 1.2f, 0.5f), Stone);
                AddSpire(f, new Vector3(x, H(ChurchFrontTop + 1.2f), ChurchFront + 0.25f), 0.35f, 4, 0.8f, Stone);
            }
        }

        /// <summary>Cimborrio: ventanal apuntado entre arquillos ciegos, crestería y pináculos en las esquinas.</summary>
        private static void AddUpperBody(MeshBuilder f, System.Func<float, float> H)
        {
            const float z = CrossingFront;
            AddPointedOpening(f, UpperBodyAxis, H(21.8f), 1.9f, 4.1f, z - 0.02f, Relief);
            AddPointedOpening(f, UpperBodyAxis, H(21.95f), 1.4f, 3.7f, z - 0.03f, Dark);
            foreach (float x in new[] { 11f, 12.4f, 16.6f, 18f })
            {
                AddPointedOpening(f, x, H(22.2f), 0.8f, 3.2f, z - 0.02f, Relief);
            }

            f.AddBox(new Vector3(UpperBodyAxis, H(26.3f), z - 0.15f), new Vector3(9.6f, 0.3f, 0.3f), Stone);
            AddCresting(f, 10f, 19f, H(26.45f), z - 0.12f);
            foreach (float x in new[] { 9.9f, 13.1f, 16.9f, 19.1f })
            {
                f.AddBox(new Vector3(x, H(27.8f), z + 0.25f), new Vector3(0.6f, 3f, 0.6f), Stone);
                AddSpire(f, new Vector3(x, H(29.3f), z + 0.25f), 0.42f, 4, 1.8f, Stone);
            }
        }

        /// <summary>
        /// Torre de la portería (oeste): fuste de mampostería con ventanas enrejadas, matacán corrido, campanario con
        /// dos vanos y una ventana con balconcillo al frente y dos vanos en cada lado, y cornisa volada sobre la terraza.
        /// </summary>
        private static void BuildWestTower(GenerationContext ctx, GameObject frame)
        {
            Vector3 t = AtriumPlan.WestTower;
            float y0 = PlanFrame.Y(ctx, AtriumPlan.PortalLanding);
            float H(float h) => y0 + h;
            float x0 = t.x, x1 = t.y, z0 = 0.4f, z1 = 0.4f + t.z;
            float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f, side = x1 - x0;
            var b = new MeshBuilder(TowerSlots);

            AddBlock(b, x0, x1, PlanFrame.Y(ctx, FootLevel), H(WestShaftTop), z0, z1, Stone);
            AddBlock(b, x0 - 0.25f, x1 + 0.25f, H(WestShaftTop), H(WestBelfryBottom), z0 - 0.25f, z1 + 0.25f, Ashlar);
            AddBlock(b, x0, x1, H(WestBelfryBottom), H(WestBelfryTop), z0, z1, Stone);
            AddBlock(b, x0 - 0.4f, x1 + 0.4f, H(WestBelfryTop), H(WestCorniceTop), z0 - 0.4f, z1 + 0.4f, Ashlar);

            // Campanario: al frente, dos vanos y la ventana con balconcillo entre ellos; dos vanos en cada otra cara.
            foreach ((float offset, float width, float height, float sill) in WestBelfryFront)
            {
                AddPointedOpening(b, cx + offset, H(WestBelfryBottom + sill), width, height, z0 - 0.02f, Dark);
            }

            b.AddBox(new Vector3(cx, H(WestBelfryBottom + 0.68f), z0 - 0.25f), new Vector3(1.1f, 0.1f, 0.5f), Ashlar);
            for (int i = 0; i <= 5; i++)
            {
                b.AddBox(new Vector3(cx - 0.5f + i * 0.2f, H(WestBelfryBottom + 1.1f), z0 - 0.48f), new Vector3(0.025f, 0.8f, 0.025f), Iron);
            }

            b.AddBox(new Vector3(cx, H(WestBelfryBottom + 1.5f), z0 - 0.48f), new Vector3(1.05f, 0.04f, 0.04f), Iron);

            foreach (Vector3 face in new[] { Vector3.left, Vector3.right, Vector3.forward })
            {
                Vector3 along = Vector3.Cross(Vector3.up, face).normalized;
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 c = new Vector3(cx, 0f, cz) + face * (side * 0.5f + 0.02f) + along * (i * side * 0.22f);
                    AddPointedOpeningOriented(b, c, H(WestBelfryBottom + 0.15f), 0.9f, 2.4f, face, along, Dark);
                }
            }

            // Ventanas enrejadas con balconcillo en el frente.
            foreach (float h in new[] { 13.4f, 16.3f })
            {
                // El hueco va por delante de su cerco de sillería, que asoma alrededor.
                b.AddBox(new Vector3(cx - 0.1f, H(h + 1.1f), z0 - 0.04f), new Vector3(0.95f, 2.2f, 0.02f), Dark);
                b.AddBox(new Vector3(cx - 0.1f, H(h + 1.1f), z0 - 0.015f), new Vector3(1.25f, 2.5f, 0.03f), Trim);
                b.AddBox(new Vector3(cx - 0.1f, H(h - 0.05f), z0 - 0.25f), new Vector3(1.3f, 0.12f, 0.5f), Trim);
                for (int i = 0; i <= 5; i++)
                {
                    b.AddBox(new Vector3(cx - 0.55f + i * 0.18f, H(h + 0.45f), z0 - 0.48f), new Vector3(0.035f, 0.9f, 0.035f), Iron);
                }

                b.AddBox(new Vector3(cx - 0.1f, H(h + 0.92f), z0 - 0.48f), new Vector3(1.15f, 0.05f, 0.05f), Iron);
                b.AddBox(new Vector3(cx - 0.1f, H(h + 0.1f), z0 - 0.48f), new Vector3(1.15f, 0.04f, 0.04f), Iron);
            }

            AddColumnAndBlindArch(ctx, b, x0, z0);

            AddToScene(ctx, b, "TorreOeste", frame);
        }

        /// <summary>
        /// Columna estriada exenta ante la cara de la torre oeste y arco ciego rebajado que vuela desde ella hasta
        /// la esquina con la portería; bajo él queda la estatua.
        /// </summary>
        private static void AddColumnAndBlindArch(GenerationContext ctx, MeshBuilder b, float towerWest, float face)
        {
            float colX = AtriumPlan.TowerColumnX;
            float colZ = face - 0.35f;
            float floor = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(AtriumPlan.FacadeToPlan(new Vector2(colX, colZ))));
            b.AddBox(new Vector3(colX, floor + 0.25f, colZ), new Vector3(0.6f, 0.5f, 0.6f), Ashlar);
            b.AddLathe(new Vector3(colX, 0f, colZ), new[]
            {
                new Vector2(0.22f, floor + 0.5f), new Vector2(0.2f, floor + 4.6f), new Vector2(0f, floor + 4.6f),
            }, 16, Ashlar, flutes: 8, fluteDepth: 0.12f);
            b.AddBox(new Vector3(colX, floor + 4.75f, colZ), new Vector3(0.65f, 0.3f, 0.6f), Ashlar);

            float west = towerWest + 0.4f;
            float centre = (colX + west) * 0.5f;
            float radius = (colX - west) * 0.5f;
            float spring = floor + 4.9f;
            for (int i = 0; i < 16; i++)
            {
                float t0 = Mathf.PI * i / 16;
                float t1 = Mathf.PI * (i + 1) / 16;
                Vector3 Ring(float k, float r) => new Vector3(centre + Mathf.Cos(k) * r, spring + Mathf.Sin(k) * r * 0.75f, face - 0.07f);
                b.AddConvexFace(new[] { Ring(t0, radius - 0.12f), Ring(t1, radius - 0.12f), Ring(t1, radius + 0.12f), Ring(t0, radius + 0.12f) },
                    Vector3.back, Vector3.right, Ashlar);
            }
        }

        /// <summary>
        /// Torre del reloj (este): fuste de mampostería con ventana gótica enrejada, dos balcones con sus
        /// puertas y el reloj; remate volado sobre canecillos con tres ventanas de medio punto, cornisa y almenas.
        /// </summary>
        private static void BuildEastTower(GenerationContext ctx, GameObject frame)
        {
            Vector3 t = AtriumPlan.EastTower;
            float y0 = PlanFrame.Y(ctx, AtriumPlan.PortalLanding);
            float H(float h) => y0 + h;
            float u0 = t.x, u1 = t.y, v0 = t.z, v1 = t.z + (t.y - t.x);
            var b = new MeshBuilder(TowerSlots);

            AddBlock(b, u0, u1, PlanFrame.Y(ctx, FootLevel), H(EastShaftTop), v0, v1, Stone);
            AddBlock(b, u0 - 0.3f, u1 + 0.3f, H(EastShaftTop), H(EastCrownBottom), v0 - 0.3f, v1 + 0.3f, Ashlar);
            AddBlock(b, u0 - 0.6f, u1 + 0.6f, H(EastCrownBottom), H(EastCrownTop), v0 - 0.6f, v1 + 0.6f, Stone);
            AddBlock(b, u0 - 0.75f, u1 + 0.75f, H(EastCrownTop), H(EastBattlementBase), v0 - 0.75f, v1 + 0.75f, Ashlar);

            AddEastCrown(b, u0, u1, v0, v1, H);

            // Reloj: la esfera va en la torre; las agujas, aparte, porque giran con la hora del juego.
            var clock = new Vector3(20.7f, H(23.6f), v0 - 0.03f);
            AddDisc(b, clock, 0.98f, Ashlar);
            AddDisc(b, clock + new Vector3(0f, 0f, -0.01f), 0.85f, ClockFace);
            AddClockHands(ctx, frame, clock);

            AddEastFrontOpenings(b, v0, H);
            AddToScene(ctx, b, "TorreEste", frame);
        }

        /// <summary>Almenas en los cuatro lados del remate y ventanas de medio punto al frente.</summary>
        private static void AddEastCrown(MeshBuilder b, float u0, float u1, float v0, float v1, System.Func<float, float> H)
        {
            float top = H(AtriumPlan.EastTowerTop);
            for (float x = u0 - 0.25f; x <= u1 + 0.3f; x += 1.4f)
            {
                AddBlock(b, x - 0.35f, x + 0.35f, H(EastBattlementBase), top, v0 - 0.6f, v0 - 0.1f, Stone);
                AddBlock(b, x - 0.35f, x + 0.35f, H(EastBattlementBase), top, v1 + 0.1f, v1 + 0.6f, Stone);
            }

            for (float z = v0 + 1.15f; z <= v1 - 0.4f; z += 1.4f)
            {
                AddBlock(b, u0 - 0.6f, u0 - 0.1f, H(EastBattlementBase), top, z - 0.35f, z + 0.35f, Stone);
                AddBlock(b, u1 + 0.1f, u1 + 0.6f, H(EastBattlementBase), top, z - 0.35f, z + 0.35f, Stone);
            }

            float upperFront = v0 - 0.62f;
            foreach (float u in new[] { 18.8f, 20.7f, 22.9f })
            {
                b.AddBox(new Vector3(u, H(27.7f), upperFront), new Vector3(0.75f, 1.6f, 0.04f), Dark);
                AddDisc(b, new Vector3(u, H(28.5f), upperFront), 0.375f, Dark);
            }
        }

        /// <summary>Ventana gótica enrejada y dos balcones de hierro con sus puertas en el frente de la torre.</summary>
        private static void AddEastFrontOpenings(MeshBuilder b, float v0, System.Func<float, float> H)
        {
            const float wu = EastTowerAxis;
            AddPointedOpening(b, wu, H(6.1f), 2.4f, 4.6f, v0 - 0.02f, Ashlar);
            AddPointedOpening(b, wu, H(6.3f), 2f, 4.1f, v0 - 0.03f, Dark);
            for (int i = 0; i <= 8; i++)
            {
                b.AddBox(new Vector3(wu - 0.9f + i * 0.225f, H(7.9f), v0 - 0.06f), new Vector3(0.03f, 3.2f, 0.03f), Iron);
            }

            foreach ((float slab, float door, float doorHeight) in new[] { (11.5f, 11.6f, 4.6f), (16.6f, 16.7f, 2.5f) })
            {
                b.AddBox(new Vector3(wu, H(door + doorHeight * 0.5f), v0 - 0.04f), new Vector3(1.3f, doorHeight, 0.02f), Dark);
                b.AddBox(new Vector3(wu, H(door + doorHeight * 0.5f), v0 - 0.015f), new Vector3(1.7f, doorHeight + 0.3f, 0.03f), Trim);
                b.AddBox(new Vector3(wu, H(slab - 0.07f), v0 - 0.35f), new Vector3(3.9f, 0.14f, 0.7f), Trim);
                b.AddBox(new Vector3(wu, H(slab + 1f), v0 - 0.68f), new Vector3(3.9f, 0.06f, 0.06f), Iron);
                b.AddBox(new Vector3(wu, H(slab + 0.12f), v0 - 0.68f), new Vector3(3.9f, 0.04f, 0.04f), Iron);
                for (int i = 0; i <= 16; i++)
                {
                    b.AddBox(new Vector3(wu - 1.92f + i * 0.24f, H(slab + 0.5f), v0 - 0.68f), new Vector3(0.035f, 1f, 0.035f), Iron);
                }
            }
        }

        /// <summary>
        /// Agujas del reloj de la torre sobre pivotes en el centro de la esfera, movidas por <see cref="TowerClock"/>:
        /// la de las horas, corta y ancha; la de los minutos, larga y fina, algo más adelantada.
        /// </summary>
        private static void AddClockHands(GenerationContext ctx, GameObject frame, Vector3 center)
        {
            var clock = new GameObject(ClockNode);
            clock.transform.SetParent(frame.transform, false);
            clock.transform.localPosition = center;
            Material[] iron = { ctx.Data.Materials.Metal };

            Transform Hand(string name, float length, float width, float depth)
            {
                var pivot = new GameObject(name);
                pivot.transform.SetParent(clock.transform, false);
                pivot.transform.localPosition = new Vector3(0f, 0f, -depth);
                var hand = new MeshBuilder(1);
                hand.AddBox(new Vector3(0f, length * 0.5f - 0.08f, 0f), new Vector3(width, length, 0.02f), 0);
                ctx.Output.AddMoving(HierarchyNames.Of("Malla", name), hand.Build($"SM_{name}"), iron, pivot);
                return pivot.transform;
            }

            Transform hours = Hand(HourHandNode, 0.5f, 0.08f, 0.03f);
            Transform minutes = Hand(MinuteHandNode, 0.74f, 0.05f, 0.05f);
            clock.AddComponent<TowerClock>().Configure(ctx.Scene.Clock, hours, minutes);
        }

        private static void AddToScene(GenerationContext ctx, MeshBuilder builder, string name, GameObject parent)
        {
            Material[] slotMaterials =
            {
                ctx.Data.Materials.Stone, ctx.Data.Materials.Shadow, ctx.Data.Materials.Canvas, ctx.Data.Materials.Metal,
                ctx.Data.Materials.Ashlar, ctx.Data.Materials.WallCream,
            };
            ctx.Output.AddStatic(name, builder, $"SM_{name}", slotMaterials, parent);
        }

        /// <summary>Cornisa de remate a lo largo del muro de planta <paramref name="a"/>-<paramref name="b"/>, bajo <paramref name="top"/>.</summary>
        private static void AddCornice(MeshBuilder builder, Vector2 a, Vector2 b, float top, int submesh)
        {
            Vector2 along = (b - a).normalized;
            Vector2 outward = new Vector2(along.y, -along.x);
            Vector2 mid = (a + b) * 0.5f + outward * 0.12f;
            float yaw = Mathf.Atan2(-along.y, along.x) * Mathf.Rad2Deg;
            builder.AddOrientedBox(new Vector3(mid.x, top - 0.2f, mid.y), new Vector3(Vector2.Distance(a, b), 0.35f, 0.3f), yaw, submesh);
        }

        private static void AddBlock(MeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1, int submesh)
        {
            b.AddBoxBetween(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), submesh);
        }

        /// <summary>Tejado a un agua que sube desde la cornisa de la galería hacia el interior.</summary>
        private static void AddLeanToRoof(MeshBuilder f, float x0, float x1, float lowY, float highY, float lowZ, float highZ)
        {
            Vector3[] slope = { new Vector3(x0, lowY, lowZ), new Vector3(x1, lowY, lowZ), new Vector3(x1, highY, highZ), new Vector3(x0, highY, highZ) };
            f.AddConvexFace(slope, Vector3.up + Vector3.back, Vector3.right, RoofSlot);
            foreach (float x in new[] { x0, x1 })
            {
                f.AddConvexFace(new[] { new Vector3(x, lowY, lowZ), new Vector3(x, highY, highZ), new Vector3(x, lowY, highZ) },
                    x == x0 ? Vector3.left : Vector3.right, Vector3.forward, Stone);
            }
        }

        // Friso de rombos entrelazados.
        private static void AddLattice(MeshBuilder f, float xStart, float xEnd, float bottomY, float z)
        {
            const float cell = 0.55f;
            for (float x = xStart; x < xEnd; x += cell)
            {
                for (int row = 0; row < 5; row++)
                {
                    var c = new Vector3(x + (row % 2) * cell * 0.5f, bottomY + 0.25f + row * 0.5f, z);
                    f.AddConvexFace(new[]
                    {
                        c + new Vector3(0f, -0.22f, 0f), c + new Vector3(0.22f, 0f, 0f), c + new Vector3(0f, 0.22f, 0f), c + new Vector3(-0.22f, 0f, 0f),
                    }, Vector3.back, Vector3.right, Relief);
                }
            }
        }

        private static void AddCresting(MeshBuilder f, float xStart, float xEnd, float y, float z)
        {
            for (float x = xStart; x <= xEnd; x += 0.9f)
            {
                f.AddBox(new Vector3(x, y + 0.3f, z), new Vector3(0.16f, 0.6f, 0.16f), Stone);
                AddSpire(f, new Vector3(x, y + 0.6f, z), 0.13f, 4, 0.5f, Stone);
            }
        }
    }
}
