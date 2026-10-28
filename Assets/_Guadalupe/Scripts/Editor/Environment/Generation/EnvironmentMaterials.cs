using Guadalupe.Ambience;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Materiales y capas de terreno que usa el generador del entorno. Es un recurso del proyecto
    /// (<c>Settings/Generation/SO_EnvironmentMaterials.asset</c>) que se edita en el Inspector: cada
    /// campo apunta a un material o capa de <c>Art/Materials/Environment</c> y <c>Art/Terrain</c>.
    /// </summary>
    /// <remarks>
    /// Los materiales comparten pocas texturas de ambientCG con distinto tinte (variantes del encalado, tonos
    /// de teja), lo que ahorra memoria de vídeo.
    /// </remarks>
    [CreateAssetMenu(menuName = "Guadalupe/Materiales del entorno", fileName = "EnvironmentMaterials")]
    public sealed class EnvironmentMaterials : ScriptableObject
    {
        [SerializeField] private Material _wallWhite;
        [SerializeField] private Material _wallCream;
        [SerializeField] private Material _wallOchre;
        [SerializeField] private Material _roof;
        [SerializeField] private Material _stone;
        [SerializeField] private Material _stoneLight;
        [SerializeField] private Material _facade;
        [SerializeField] private Material _gothicFacade;
        [SerializeField] private Material _granite;
        [SerializeField] private Material _wood;
        [SerializeField] private Material _metal;
        [SerializeField] private Material _canvas;
        [SerializeField] private Material _shadow;
        [SerializeField] private Material _window;
        [SerializeField] private Material _door;
        [SerializeField] private Material _zocalo;
        [SerializeField] private Material _ashlar;
        [SerializeField] private Material _bronze;
        [SerializeField] private Material _awningRed;
        [SerializeField] private Material _awningGreen;
        [SerializeField] private Material _lanternGlass;
        [SerializeField] private Material _greenTile;
        [SerializeField] private Material _beam;
        [SerializeField] private Material _pavingSlab;
        [SerializeField] private Material _atriumFloor;
        [SerializeField] private Material _greenBronze;
        [SerializeField] private Material _gold;
        [SerializeField] private Material _terracotta;
        [SerializeField] private Material _foliage;
        [SerializeField] private Material _windowFrame;
        [SerializeField] private Material _wallSalmon;
        [SerializeField] private Material _wallAshlar;
        [SerializeField] private Material _zocaloBrown;
        [SerializeField] private Material _zocaloRed;
        [SerializeField] private Material _zocaloOchre;
        [SerializeField] private Material _zocaloBrick;
        [SerializeField] private Material _zocaloAshlar;
        [SerializeField] private Material _carpentryGreen;
        [SerializeField] private Material _wallWhiteWarm;
        [SerializeField] private Material _wallWhiteGrey;
        [SerializeField] private Material _roofDark;
        [SerializeField] private Material _roofLight;
        [SerializeField] private Material _windowLit;
        [SerializeField] private Material _grime;
        [SerializeField] private Material _tileBlue;
        [SerializeField] private TerrainLayer _groundLayer;
        [SerializeField] private TerrainLayer _streetLayer;
        [SerializeField] private TerrainLayer _pavingLayer;
        [SerializeField] private TerrainLayer _pavingAltLayer;

        [Header("Rótulos")]
        [SerializeField] private Font _signFontGothic;
        [SerializeField] private Font _signFontHand;
        [SerializeField] private Font _signFontRoman;
        [SerializeField] private Material _signPlain;
        [SerializeField] private Material _signGothic;
        [SerializeField] private Material _signHand;
        [SerializeField] private Material _signRoman;

        [Header("Iluminación navideña")]
        [SerializeField] private Material _lightsNet;
        [SerializeField] private Material _lightsCurtain;
        [SerializeField] private Material _lightsSolid;

        public Material WallWhite => _wallWhite;

        public Material WallCream => _wallCream;

        public Material WallOchre => _wallOchre;

        public Material Roof => _roof;

        /// <summary>Mampostería de piedra (monasterio, torres, lonja).</summary>
        public Material Stone => _stone;

        /// <summary>Mampostería clara, con más mortero, de los muros del cuerpo del monasterio.</summary>
        public Material StoneLight => _stoneLight;

        /// <summary>Sillería clara de la fachada gótica.</summary>
        public Material Facade => _facade;

        /// <summary>Cara de la fachada gótica, con su textura propia proyectada de frente.</summary>
        public Material GothicFacade => _gothicFacade;

        /// <summary>Granito de escalinata, pretiles y pilares.</summary>
        public Material Granite => _granite;

        public Material Wood => _wood;

        /// <summary>Metal oscuro (farolas, patas).</summary>
        public Material Metal => _metal;

        public Material Canvas => _canvas;

        /// <summary>Fondo oscuro de huecos y pasadizos.</summary>
        public Material Shadow => _shadow;

        public Material Window => _window;

        public Material Door => _door;

        /// <summary>Zócalo gris azulado de la base de las fachadas encaladas.</summary>
        public Material Zocalo => _zocalo;

        /// <summary>Sillería de granito del muro de la lonja y del arco.</summary>
        public Material Ashlar => _ashlar;

        /// <summary>Bronce oscuro de placas y relieves.</summary>
        public Material Bronze => _bronze;

        public Material AwningRed => _awningRed;

        public Material AwningGreen => _awningGreen;

        /// <summary>Cristal iluminado de faroles.</summary>
        public Material LanternGlass => _lanternGlass;

        /// <summary>Teja vidriada verde y blanca de los chapiteles de las torres redondas.</summary>
        public Material GreenTile => _greenTile;

        /// <summary>Madera envejecida de vigas y techos de soportal.</summary>
        public Material Beam => _beam;

        /// <summary>Losas de granito para suelos construidos (soportales).</summary>
        public Material PavingSlab => _pavingSlab;

        /// <summary>Losas grandes de granito claro del suelo del atrio.</summary>
        public Material AtriumFloor => _atriumFloor;

        /// <summary>Bronce verdoso de las puertas del monasterio.</summary>
        public Material GreenBronze => _greenBronze;

        /// <summary>Oro de los bordados del tapiz de Navidad.</summary>
        public Material Gold => _gold;

        public Material Terracotta => _terracotta;

        public Material Foliage => _foliage;

        public Material WindowFrame => _windowFrame;

        /// <summary>Pintura salmón de algunas fachadas de la plaza.</summary>
        public Material WallSalmon => _wallSalmon;

        public Material WallAshlar => _wallAshlar;

        public Material ZocaloBrown => _zocaloBrown;

        public Material ZocaloRed => _zocaloRed;

        public Material ZocaloOchre => _zocaloOchre;

        public Material ZocaloBrick => _zocaloBrick;

        public Material ZocaloAshlar => _zocaloAshlar;

        public Material CarpentryGreen => _carpentryGreen;

        /// <summary>Variantes del encalado (más cálido y más gris) para que no todas las casas sean iguales.</summary>
        public Material WallWhiteWarm => _wallWhiteWarm;

        public Material WallWhiteGrey => _wallWhiteGrey;

        /// <summary>Teja más oscura y parda (tejados más viejos).</summary>
        public Material RoofDark => _roofDark;

        /// <summary>Teja más clara y anaranjada (tejados retejados).</summary>
        public Material RoofLight => _roofLight;

        /// <summary>Cristal de ventana con luz dentro de noche (la emisión la controla StreetLights en juego).</summary>
        public Material WindowLit => _windowLit;

        /// <summary>Suciedad y salpicaduras al pie de los muros (transparente, degradado hacia arriba).</summary>
        public Material Grime => _grime;

        /// <summary>Azul cobalto de los azulejos con el número de las casas.</summary>
        public Material TileBlue => _tileBlue;

        /// <summary>Capa de terreno: tierra fuera del casco.</summary>
        public TerrainLayer GroundLayer => _groundLayer;

        /// <summary>Capa de terreno: empedrado de las calles.</summary>
        public TerrainLayer StreetLayer => _streetLayer;

        /// <summary>Capa de terreno: losas de granito de la plaza.</summary>
        public TerrainLayer PavingLayer => _pavingLayer;

        /// <summary>
        /// El mismo enlosado a otra escala y desplazado: se mezcla a manchas con el principal para que no se note la
        /// repetición de la textura en las vistas largas.
        /// </summary>
        public TerrainLayer PavingAltLayer => _pavingAltLayer;

        /// <summary>Luces navideñas en red (columnas de la fachada, árbol y tiras).</summary>
        public Material LightsNet => _lightsNet;

        /// <summary>Luces navideñas en cortina de hilos verticales (balcones y balaustradas).</summary>
        public Material LightsCurtain => _lightsCurtain;

        /// <summary>Luz continua sin bombillas (estrella, copos, ventanas iluminadas).</summary>
        public Material LightsSolid => _lightsSolid;

        /// <summary>
        /// Fuente y material de letras de una tipografía de rótulo. Los materiales usan el shader
        /// <c>Guadalupe/SignText</c> con el atlas de su fuente; la de palo seco es la fuente integrada de Unity.
        /// </summary>
        public (Font Font, Material Material) FontFor(SignFont kind)
        {
            switch (kind)
            {
                case Environment.SignFont.Gothic: return (_signFontGothic, _signGothic);
                case Environment.SignFont.Hand: return (_signFontHand, _signHand);
                case Environment.SignFont.Roman: return (_signFontRoman, _signRoman);
                default: return (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), _signPlain);
            }
        }

        /// <summary>Carga el recurso de materiales del proyecto.</summary>
        public static EnvironmentMaterials Load() => ProjectPaths.Load<EnvironmentMaterials>(ProjectPaths.EnvironmentMaterials);

        /// <summary>Material del muro de una casa: el del catálogo si la casa está catalogada y, si no, uno estable al azar.</summary>
        public Material WallFor(HouseStyle style, string buildingId)
        {
            if (style == null)
            {
                return PickWall(buildingId);
            }

            switch (style.Wall)
            {
                case WallFinish.White: return WhiteVariant(buildingId);
                case WallFinish.Cream: return WallCream;
                case WallFinish.Ochre: return WallOchre;
                case WallFinish.Salmon: return WallSalmon;
                case WallFinish.Ashlar: return WallAshlar;
                default: return WallWhite;
            }
        }

        /// <summary>Material del zócalo de una casa (gris azulado si no está catalogada).</summary>
        public Material ZocaloFor(HouseStyle style)
        {
            switch (style?.Zocalo ?? ZocaloFinish.Grey)
            {
                case ZocaloFinish.Brown: return ZocaloBrown;
                case ZocaloFinish.Red: return ZocaloRed;
                case ZocaloFinish.Ochre: return ZocaloOchre;
                case ZocaloFinish.Brick: return ZocaloBrick;
                case ZocaloFinish.PaintedAshlar: return ZocaloAshlar;
                default: return Zocalo;
            }
        }

        /// <summary>Material de la carpintería de una casa.</summary>
        public Material CarpentryFor(HouseStyle style) => style != null && style.GreenShutters ? CarpentryGreen : WindowFrame;

        /// <summary>Elige un material de pared estable para un edificio a partir de su referencia.</summary>
        /// <remarks>
        /// Se usa un hash FNV-1a propio porque <see cref="string.GetHashCode"/> no garantiza el mismo
        /// valor entre ejecuciones: el mismo edificio debe tener siempre el mismo color al regenerar.
        /// </remarks>
        public Material PickWall(string buildingId)
        {
            uint bucket = StableHash.Fnv1a(buildingId, 0) % 10;
            if (bucket < 7)
            {
                return WhiteVariant(buildingId);
            }

            return bucket < 9 ? WallCream : WallOchre;
        }

        /// <summary>Una de las tres variantes del encalado, estable para cada casa.</summary>
        public Material WhiteVariant(string buildingId)
        {
            switch (Hash(buildingId, 7) % 5)
            {
                case 0: return WallWhiteWarm;
                case 1: return WallWhiteGrey;
                default: return WallWhite;
            }
        }

        /// <summary>Teja de la casa: tres tonos repartidos de forma estable.</summary>
        public Material RoofFor(string buildingId)
        {
            switch (Hash(buildingId, 13) % 4)
            {
                case 0: return RoofDark;
                case 1: return RoofLight;
                default: return Roof;
            }
        }

        /// <summary>FNV-1a con una mezcla final de bits, para repartir mejor los módulos pequeños.</summary>
        private static uint Hash(string text, uint salt)
        {
            uint hash = StableHash.Fnv1a(text, salt);
            return hash ^ (hash >> 13);
        }
    }
}
