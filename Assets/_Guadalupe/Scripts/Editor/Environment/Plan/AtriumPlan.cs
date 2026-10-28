using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Medidas del atrio y del frente del monasterio. Los puntos son (u, v) en metros del marco
    /// <see cref="PlanFrame"/> y las alturas, cotas de nivel en metros.
    /// </summary>
    internal static class AtriumPlan
    {
        /// <summary>Altura del cuerpo principal del monasterio sobre el atrio.</summary>
        public const float MonasteryHeight = 17f;

        /// <summary>Altura del alero de la portería sobre el atrio.</summary>
        public const float PorteriaHeight = 6.8f;

        /// <summary>Meseta ante las portadas, arranque de la pasarela.</summary>
        public const float PortalLanding = -1.61f;

        /// <summary>Plaza al pie de la escalinata principal.</summary>
        public const float StairsFoot = -7.25f;

        /// <summary>Meseta enlosada al final de la pasarela, junto a la calle Marqués de la Romana.</summary>
        public const float EastPlatformLevel = -2.80f;

        /// <summary>Cota del abanico de acceso desde Marqués de la Romana junto a la calle.</summary>
        public const float MarquesFanEndLevel = -2.89f;

        /// <summary>Cota del atrio: baja suavemente de este (−3,00) a oeste (−3,45).</summary>
        public static float AtriumLevel(Vector2 p) => -3.27f + 0.0165f * p.x;

        /// <summary>Cota del atrio chico: −3,35 al pie de los escalones del atrio y −2,95 hacia la calle.</summary>
        public static float AtrioChicoLevel(Vector2 p) => -3.35f + 0.05f * Mathf.Clamp(p.x - 18f, 0f, 8f);

        /// <summary>
        /// Cota de la calle Marqués de la Romana junto al atrio chico, en su eje: plano ajustado a las curvas de
        /// nivel (−3,9 bajo el pretil, −3,0 donde entra el acceso en abanico y −2,2 más arriba).
        /// </summary>
        public static float MarquesStreetLevel(Vector2 p) => -8.303f + 0.0867f * p.x + 0.1001f * p.y;

        /// <summary>Punto donde el acceso en abanico entra en la calle Marqués de la Romana.</summary>
        public static readonly Vector2 MarquesEntry = new Vector2(35.5f, 24.6f);

        /// <summary>Cota de la calle en <see cref="MarquesEntry"/>.</summary>
        public const float MarquesEntryLevel = -2.95f;

        /// <summary>
        /// Tramo de la calle que sigue <see cref="MarquesStreetLevel"/>: llega hasta el ala del monasterio y más allá
        /// del límite jugable, para que no quede vaguada al pie del ala ni escalón donde la calle sale de la zona.
        /// </summary>
        public static readonly Rect MarquesBesideAtrium = Rect.MinMaxRect(20f, 6f, 56f, 46f);

        // Escalinata principal
        /// <summary>Lados del tramo superior de la escalinata (u) y borde superior (v).</summary>
        public const float StairsWest = -5.7f;
        public const float StairsEast = 8.5f;
        public const float StairsTop = 17.8f;

        /// <summary>Tramo superior: 13 peldaños de 31,5 cm de huella entre las balaustradas del atrio.</summary>
        public const int UpperSteps = 13;
        public const float UpperRun = 0.315f;

        /// <summary>
        /// Meseta intermedia, con un obelisco de granito en cada esquina: llega por detrás hasta el muro del atrio,
        /// de modo que se sube a ella también por los peldaños de los lados.
        /// </summary>
        public static readonly Rect Landing = Rect.MinMaxRect(-6.05f, 11.6f, 8.8f, StairsTop - UpperSteps * UpperRun);

        /// <summary>Tramo inferior: 6 peldaños que rodean la meseta por el frente y los dos lados, hasta el muro.</summary>
        public const int LowerSteps = 6;
        public const float LowerRun = 0.24f;

        /// <summary>V donde arranca el primer peldaño de la escalinata, al pie de la plaza.</summary>
        public static readonly float StairsFootV = Landing.yMin - LowerSteps * LowerRun;

        /// <summary>Recinto que ocupan el atrio y sus escaleras; fuera de él el terreno no se rebaja.</summary>
        public static readonly Rect AtriumExtent = Rect.MinMaxRect(-20f, StairsFootV, 46f, 45f);

        /// <summary>Franja entre la plaza y el frente del atrio, que llega llana hasta el muro de la lonja.</summary>
        public static readonly Rect Forecourt = Rect.MinMaxRect(-17f, StairsFootV - 12f, 18f, 15f);

        public static readonly Vector2[] Obelisks = { new Vector2(-5.75f, 12.1f), new Vector2(8.35f, 12f) };

        /// <summary>Cota del atrio en lo alto de la escalinata.</summary>
        public static float StairsTopLevel => AtriumLevel(new Vector2((StairsWest + StairsEast) * 0.5f, StairsTop));

        /// <summary>Tabica común de los dos tramos de la escalinata.</summary>
        public static float StairsRise => (StairsTopLevel - StairsFoot) / (UpperSteps + LowerSteps);

        /// <summary>Cota de la meseta intermedia.</summary>
        public static float LandingLevel => StairsTopLevel - UpperSteps * StairsRise;

        /// <summary>
        /// Atrio: desde la balaustrada sobre la plaza hasta las portadas y la torre del reloj, y al oeste hasta
        /// el bordillo de la escalera de la portería. Excluye el hueco de la escalinata.
        /// </summary>
        public static readonly Vector2[] Atrium =
        {
            new Vector2(-13.36f, 14.76f), new Vector2(-5.75f, 13.8f), new Vector2(-5.75f, StairsTop),
            new Vector2(8.45f, StairsTop), new Vector2(8.45f, 13.45f), new Vector2(17.7f, 14.5f),
            new Vector2(17.1f, 32.7f), new Vector2(-13.3f, 32f), new Vector2(-13.5f, 31.4f),
            new Vector2(-15.28f, PassageTop), new Vector2(-10.2f, PassageTop), new Vector2(-10.6f, 24.6f),
            new Vector2(-13.2f, VaultBack),
        };

        /// <summary>Atrio chico, entre el atrio, la pasarela y la balaustrada sobre la calle Marqués de la Romana; llega hasta bajo la pasarela.</summary>
        public static readonly Vector2[] AtrioChico =
        {
            new Vector2(17.7f, 20.35f), new Vector2(24.3f, 20.45f), new Vector2(30.9f, 24.15f),
            new Vector2(28.6f, 28.6f), new Vector2(26.55f, 28.6f), new Vector2(26.55f, 30.75f), new Vector2(17.15f, 30.75f),
        };

        /// <summary>Meseta final de la pasarela (−2,80), enlosada.</summary>
        public static readonly Vector2[] EastPlatform =
        {
            new Vector2(26.55f, 28.6f), new Vector2(29.75f, 28.6f), new Vector2(29.9f, 32.35f), new Vector2(26.55f, 32.35f),
        };

        /// <summary>
        /// Escalera del arco: entra en la plaza bajo un arco de medio punto, pasa bajo una bóveda corta coronada
        /// por balaustradas y sigue a cielo abierto, pegada al muro de la casa blanca y de la portería, hasta la
        /// explanada ante la portería. Su lado este es el muro del atrio, con pretil y después barandilla.
        /// </summary>
        public static readonly Vector2[] PassageStairs =
        {
            new Vector2(-17.94f, VaultFront), new Vector2(-14.34f, VaultFront), new Vector2(-14.34f, VaultBack),
            new Vector2(-13.2f, VaultBack), new Vector2(-10.6f, 24.6f), new Vector2(-10.2f, PassageTop),
            new Vector2(-15.28f, PassageTop), new Vector2(-15.8f, 24.7f), new Vector2(-17.92f, VaultBack),
        };

        /// <summary>Planta de la bóveda (con sus machones), entre el muro de la casa y el atrio.</summary>
        public static readonly Vector2[] Vault =
        {
            new Vector2(-18.88f, VaultFront), new Vector2(-13.36f, VaultFront), new Vector2(-13.2f, VaultBack), new Vector2(-17.92f, VaultBack),
        };

        /// <summary>Frente del arco sobre la plaza, fondo de la bóveda y final de la escalera.</summary>
        public const float VaultFront = 15.4f;
        public const float VaultBack = 18.3f;
        public const float PassageTop = 26.2f;

        /// <summary>Pretil del atrio sobre la escalera (hasta 24,6) y, después, balaustrada.</summary>
        public static readonly Vector2[] PassageParapet = { new Vector2(-13.2f, VaultBack), new Vector2(-10.6f, 24.6f), new Vector2(-10.2f, PassageTop) };

        /// <summary>Muro de la casa blanca y de la portería a lo largo de la escalera.</summary>
        public static readonly Vector2[] PassageHouseWall = { new Vector2(-17.92f, VaultBack), new Vector2(-15.8f, 24.7f), new Vector2(-15.28f, PassageTop) };

        /// <summary>Paño de mampostería del muro de la casa sobre la bóveda, entre la esquina de sillería y la balaustrada, donde se apoya el poyete.</summary>
        public static readonly Vector2[] PoyeteWall = { new Vector2(-18.27f, 16.95f), new Vector2(-17.9f, 17.97f) };

        // Escalones de la esquina, al pie del muro del atrio hacia Marqués de la Romana
        /// <summary>Bordillo de los escalones de la esquina sobre la calle (de la plaza a Marqués de la Romana).</summary>
        public static readonly Vector2[] CornerKerb = { new Vector2(11.4f, 8.7f), new Vector2(18.6f, 13.6f) };

        /// <summary>Tabicas de los escalones de la esquina (u), de la plaza hacia la calle.</summary>
        public static readonly float[] CornerRisers = { 12f, 14.1f, 16.2f, 18.3f };

        /// <summary>Gradería: 8 peldaños de unos 20 cm desde la meseta de las portadas hasta el atrio.</summary>
        public const int GraderiaSteps = 8;
        public const float GraderiaRun = 0.42f;

        /// <summary>Borde de la meseta de las portadas (v en el extremo oeste) y su pendiente en planta.</summary>
        public const float GraderiaTopEdge = 31.65f;
        public const float GraderiaSlope = 0.066f;

        /// <summary>Extremo oeste del peldaño superior; cada peldaño inferior avanza 24 cm.</summary>
        public const float GraderiaWest = -5f;
        public const float GraderiaWestStep = 0.24f;

        /// <summary>Extremo este del peldaño superior; cada peldaño inferior avanza 30 cm.</summary>
        public const float GraderiaEast = 7f;
        public const float GraderiaEastStep = 0.3f;

        // Fachada y torres
        /// <summary>Extremo oeste del frente de los contrafuertes de la fachada gótica y su giro respecto al marco.</summary>
        public static readonly Vector2 FacadeOrigin = new Vector2(-4.88f, 31.6f);
        public const float FacadeYaw = 2f;

        /// <summary>Anchura de la fachada gótica (cuatro tramos entre cinco contrafuertes).</summary>
        public const float FacadeWidth = 21.2f;

        /// <summary>
        /// Torre de la portería (oeste) en el marco de la fachada: X inicial, X final, fondo. Su esquina oeste
        /// toca la portería, de modo que la estatua queda en el rincón entre ambas.
        /// </summary>
        public static readonly Vector3 WestTower = new Vector3(-8.5f, 0f, 8.1f);

        /// <summary>Columna exenta ante la torre oeste (X en el marco de la fachada) que sostiene el arco ciego.</summary>
        public const float TowerColumnX = -3.1f;

        /// <summary>Torre del reloj (este): u inicial, u final, v del frente; es cuadrada.</summary>
        public static readonly Vector3 EastTower = new Vector3(15.8f, 25.5f, 32.66f);

        /// <summary>Convierte un punto del marco de la fachada (X a lo largo, Z hacia dentro) a (u, v).</summary>
        public static Vector2 FacadeToPlan(Vector2 local)
        {
            float a = FacadeYaw * Mathf.Deg2Rad;
            return FacadeOrigin + new Vector2(local.x * Mathf.Cos(a) - local.y * Mathf.Sin(a), local.x * Mathf.Sin(a) + local.y * Mathf.Cos(a));
        }

        /// <summary>Altura de las almenas de la torre este sobre la meseta de las portadas.</summary>
        public const float EastTowerTop = 31.6f;

        /// <summary>Anchura útil de la pasarela de madera.</summary>
        public const float WalkwayWidth = 1.55f;

        /// <summary>Rellano curvo de arranque: centro y radio del arco de su borde.</summary>
        public static readonly Vector2 WalkwayCurveCenter = new Vector2(7.1f, 33.1f);
        public const float WalkwayCurveRadius = 2.9f;

        /// <summary>Tramos de la pasarela: u inicial y cota, de oeste a este (rampas al 4-10 % y rellanos).</summary>
        public static readonly Vector2[] WalkwayProfile =
        {
            new Vector2(7.23f, -1.61f), new Vector2(13.23f, -1.87f), new Vector2(14.73f, -1.87f),
            new Vector2(20.73f, -2.35f), new Vector2(22.23f, -2.35f), new Vector2(26.55f, -2.80f),
        };

        /// <summary>Borde norte de la pasarela (contra la fachada y la torre).</summary>
        public static float WalkwayNorth(float u) => Mathf.Min(32.3f, 31.82f + 0.035f * (u - 7.14f));

        /// <summary>Rampa corta entre el atrio y el atrio chico (2,5 × 1,5 m al 10 %).</summary>
        public static readonly Rect SmallRamp = Rect.MinMaxRect(17.15f, 26.6f, 19.65f, 28.2f);

        /// <summary>Centro del arco del paso de adoquín en abanico y sus radios interior y exterior.</summary>
        public static readonly Vector2 FanCenter = new Vector2(35.7f, 32.2f);
        public const float FanInnerRadius = 5.81f;
        public const float FanOuterRadius = 8.85f;

        /// <summary>Ángulos (grados) del abanico: desde la meseta final hasta la calle.</summary>
        public const float FanStartAngle = 200f;
        public const float FanEndAngle = 278f;

        /// <summary>Balaustrada del atrio chico sobre la calle y hacia el acceso en abanico.</summary>
        public static readonly Vector2[] MarquesParapet =
        {
            new Vector2(17.7f, 20.35f), new Vector2(24.3f, 20.48f), new Vector2(27.8f, 22.45f), new Vector2(30.7f, 24.08f),
        };

        /// <summary>Balaustradas de piedra (balaustres entre pilastras con bola) sobre los bordes del atrio.</summary>
        public static readonly Vector2[][] Balustrades =
        {
            new[] { new Vector2(-5.75f, 13.8f), new Vector2(-5.75f, 17.75f) },
            new[] { new Vector2(8.45f, 13.45f), new Vector2(8.45f, 17.75f) },
            new[] { new Vector2(-13.35f, 14.75f), new Vector2(-5.75f, 13.8f) },
            new[] { new Vector2(8.45f, 13.45f), new Vector2(17.7f, 14.5f) },
            new[] { new Vector2(17.7f, 14.5f), new Vector2(17.7f, 20.35f) },
            new[] { new Vector2(17.1f, 28.4f), new Vector2(17.1f, 30.45f) },
            new[] { new Vector2(15.1f, 30.45f), new Vector2(17.1f, 30.45f) },
            new[] { new Vector2(-18.88f, VaultFront), new Vector2(-13.36f, VaultFront) },
            // Quiebro que une la balaustrada de la bóveda, más adelantada, con la del frente del atrio.
            new[] { new Vector2(-13.36f, VaultFront), new Vector2(-13.36f, 14.75f) },
            new[] { new Vector2(-17.92f, VaultBack), new Vector2(-13.2f, VaultBack) },
        };

        /// <summary>Arco de la escalera de la portería: centro (u) y luz.</summary>
        public const float ArchCenter = -16.14f;
        public const float ArchWidth = 3.6f;

        /// <summary>Banco de granito adosado al muro del atrio, junto al arco (u inicial y final).</summary>
        public static readonly Vector2 Bench = new Vector2(-12.6f, -10.4f);

        /// <summary>V del muro del atrio al este de la escalinata en la abscisa u.</summary>
        public static float CornerWallV(float u) => Mathf.Lerp(13.45f, 14.5f, Mathf.InverseLerp(8.45f, 17.7f, u));

        /// <summary>
        /// Cota más baja de lo que el atrio construye sobre un punto (u, v), o null si el punto queda fuera.
        /// El terreno se rebaja por debajo de ella para que no asome por las losas ni por el pasadizo.
        /// </summary>
        public static float? FloorLevelAt(Vector2 p)
        {
            float? level = null;
            void Take(float l) => level = level.HasValue ? Mathf.Min(level.Value, l) : l;

            if (PolygonUtils.Contains(Atrium, p))
            {
                Take(AtriumLevel(p));
            }

            if (PolygonUtils.Contains(AtrioChico, p))
            {
                Take(AtrioChicoLevel(p));
            }

            if (PolygonUtils.Contains(EastPlatform, p))
            {
                Take(-3.04f);
            }

            if (PolygonUtils.Contains(PassageStairs, p) || PolygonUtils.Contains(Vault, p))
            {
                // Bajo la escalera del arco, el terreno queda justo por debajo de su arranque, en la plaza.
                Take(StairsFoot - 0.25f);
            }

            if (p.x > CornerRisers[0] && p.x < CornerRisers[CornerRisers.Length - 1] && p.y < CornerWallV(p.x) &&
                p.y > Mathf.Lerp(CornerKerb[0].y, CornerKerb[1].y, Mathf.InverseLerp(CornerKerb[0].x, CornerKerb[1].x, p.x)))
            {
                Take(-6.9f);
            }

            float grow = LowerSteps * LowerRun;
            if (p.x > Landing.xMin - grow && p.x < Landing.xMax + grow && p.y > Landing.yMin - grow && p.y < StairsTop)
            {
                Take(StairsFoot);
            }

            if (p.x > GraderiaWest - GraderiaSteps * GraderiaWestStep - 0.3f && p.x < GraderiaEast + GraderiaSteps * GraderiaEastStep + 0.3f &&
                p.y > 27.8f && p.y < 33.6f)
            {
                Take(AtriumLevel(p));
            }

            return level;
        }
    }
}
