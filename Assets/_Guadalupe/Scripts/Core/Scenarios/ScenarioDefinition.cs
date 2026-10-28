using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Escenario que se elige en el menú: nombre, descripción, época (fecha del sol e iluminación navideña) y
    /// afluencia de turistas en la plaza a lo largo del día.
    /// </summary>
    [CreateAssetMenu(menuName = "Guadalupe/Escenario", fileName = "SC_Scenario")]
    public sealed class ScenarioDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Escenario";
        [SerializeField, TextArea(2, 4)] private string _description = "";
        [Tooltip("Texto corto de afluencia para la interfaz (p. ej. «Afluencia baja»).")]
        [SerializeField] private string _crowdLabel = "";

        [Header("Afluencia")]
        [Tooltip("Afluencia en la hora punta: 0 = plaza vacía, 1 = máxima capacidad simulada.")]
        [SerializeField, Range(0f, 1f)] private float _crowdLevel = 0.3f;
        [Tooltip("Reparto de la afluencia a lo largo del día: hora (0-24) y fracción de la hora punta (0-1).")]
        [SerializeField] private AnimationCurve _crowdByHour = new AnimationCurve(
            new Keyframe(0f, 0.05f), new Keyframe(9f, 0.3f), new Keyframe(12.5f, 1f), new Keyframe(17f, 0.8f),
            new Keyframe(21f, 0.4f), new Keyframe(24f, 0.05f));

        [Header("Época")]
        [Tooltip("Qué día es; en Navidad se muestra la iluminación navideña.")]
        [SerializeField] private ScenarioOccasion _occasion;
        [Tooltip("Día del año para el sol (salida y puesta); 251 = 8 de septiembre, 355 = 21 de diciembre.")]
        [SerializeField, Range(1, 365)] private int _dayOfYear = 251;
        [Tooltip("Diferencia con UTC en horas: 2 en horario de verano, 1 en invierno.")]
        [SerializeField] private float _utcOffset = 2f;

        public string DisplayName => _displayName;

        public string Description => _description;

        public string CrowdLabel => _crowdLabel;

        /// <summary>Afluencia (0-1) a una hora del día según la curva y el nivel de la hora punta.</summary>
        public float CrowdAt(float hour) => _crowdLevel * Mathf.Clamp01(_crowdByHour.Evaluate(Mathf.Repeat(hour, 24f)));

        public ScenarioOccasion Occasion => _occasion;

        /// <summary>True si el escenario muestra la iluminación navideña.</summary>
        public bool Christmas => _occasion == ScenarioOccasion.Christmas;

        public int DayOfYear => _dayOfYear;

        public float UtcOffset => _utcOffset;
    }
}
