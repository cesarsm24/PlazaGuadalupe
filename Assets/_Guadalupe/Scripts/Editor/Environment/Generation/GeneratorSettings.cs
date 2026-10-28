using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Ajustes del generador de la plaza que se editan en el Inspector: área de trabajo, edificios, relieve,
    /// soportales e iluminación. Es un recurso del proyecto (<c>Assets/_Guadalupe/Settings/Generation</c>).
    /// </summary>
    /// <remarks>
    /// Los datos del lugar (origen, referencias catastrales, trazado de la plaza) están en <see cref="PlazaLayout"/>
    /// y las medidas del monasterio, en <see cref="AtriumPlan"/>.
    /// </remarks>
    [CreateAssetMenu(menuName = "Guadalupe/Ajustes del generador", fileName = "GeneratorSettings")]
    public sealed class GeneratorSettings : ScriptableObject
    {
        [Header("Área")]
        [Tooltip("Radio en metros del entorno que se genera alrededor del origen.")]
        [SerializeField, Min(10f)] private float _workRadius = 190f;

        [Header("Edificios")]
        [Tooltip("Altura media de una planta en metros.")]
        [SerializeField, Min(2f)] private float _levelHeight = 3.1f;
        [Tooltip("Profundidad a la que se entierran las bases para no dejar huecos con el terreno.")]
        [SerializeField, Min(0f)] private float _foundationDepth = 1.5f;
        [Tooltip("Radio dentro del que se detallan las fachadas (más lejos no se aprecian).")]
        [SerializeField, Min(0f)] private float _detailRadius = 90f;
        [Tooltip("Radio dentro del que las calles tienen comercios con toldos.")]
        [SerializeField, Min(0f)] private float _shopRadius = 45f;

        [Header("Relieve")]
        [Tooltip("Factor de la pendiente del plano ajustado de la plaza: el modelo de 25 m la exagera.")]
        [SerializeField, Range(0f, 1f)] private float _plazaSlopeFactor = 0.2f;
        [Tooltip("Fracción del desnivel real que se conserva cerca de la plaza.")]
        [SerializeField, Range(0f, 1f)] private float _reliefFactor = 0.15f;
        [Tooltip("Radio hasta el que se aplica entero el factor de relieve.")]
        [SerializeField, Min(0f)] private float _reliefInnerRadius = 90f;
        [Tooltip("Radio a partir del que el relieve es el real.")]
        [SerializeField, Min(0f)] private float _reliefFullRadius = 180f;

        [Header("Calle Marqués de la Romana")]
        [Tooltip("Anchura de la calle.")]
        [SerializeField, Min(1f)] private float _marquesWidth = 7f;
        [Tooltip("Pendiente máxima: la calle sube hasta casi la altura del atrio.")]
        [SerializeField, Range(0f, 0.3f)] private float _marquesMaxSlope = 0.06f;

        [Header("Soportales")]
        [Tooltip("Altura libre del soportal sobre su suelo.")]
        [SerializeField, Min(2f)] private float _porticoHeight = 3.1f;
        [Tooltip("Fondo de la franja de soportal: en ella la planta baja de todas las casas queda abierta.")]
        [SerializeField, Min(1f)] private float _arcadeCorridorDepth = 3.2f;

        [Header("Iluminación")]
        [Tooltip("Radio alrededor de la plaza dentro del que los faroles de pared llevan luz.")]
        [SerializeField, Min(0f)] private float _streetLightRadius = 55f;
        [Tooltip("Máximo de luces de faroles, para acotar el coste de las luces adicionales.")]
        [SerializeField, Min(0)] private int _maxStreetLights = 40;
        [Tooltip("Radio alrededor de la plaza dentro del que los balcones llevan cortinas de luz en Navidad.")]
        [SerializeField, Min(0f)] private float _balconyLightsRadius = 60f;
        [Tooltip("Radio alrededor del árbol de Navidad en el que se ocultan las mesas de terraza.")]
        [SerializeField, Min(0f)] private float _treeClearance = 6f;

        public float WorkRadius => _workRadius;
        public float LevelHeight => _levelHeight;
        public float FoundationDepth => _foundationDepth;
        public float DetailRadius => _detailRadius;
        public float ShopRadius => _shopRadius;
        public float PlazaSlopeFactor => _plazaSlopeFactor;
        public float ReliefFactor => _reliefFactor;
        public float ReliefInnerRadius => _reliefInnerRadius;
        public float ReliefFullRadius => _reliefFullRadius;
        public float MarquesWidth => _marquesWidth;
        public float MarquesMaxSlope => _marquesMaxSlope;
        public float PorticoHeight => _porticoHeight;
        public float ArcadeCorridorDepth => _arcadeCorridorDepth;
        public float StreetLightRadius => _streetLightRadius;
        public int MaxStreetLights => _maxStreetLights;
        public float BalconyLightsRadius => _balconyLightsRadius;
        public float TreeClearance => _treeClearance;

        /// <summary>Carga los ajustes del proyecto.</summary>
        public static GeneratorSettings Load() => ProjectPaths.Load<GeneratorSettings>(ProjectPaths.GeneratorSettings);
    }
}
