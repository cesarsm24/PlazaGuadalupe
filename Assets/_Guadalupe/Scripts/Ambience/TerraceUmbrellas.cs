using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Abre o recoge las sombrillas de las terrazas hijas de este objeto: se recogen de noche y, cuando se
    /// fuerzan (<see cref="Folded"/>, en Navidad), también de día.
    /// </summary>
    public sealed class TerraceUmbrellas : MonoBehaviour
    {
        [SerializeField] private SunController _sun;

        private TerraceUmbrella[] _umbrellas;
        private bool _folded;
        private bool? _open;

        /// <summary>True si las sombrillas quedan recogidas también de día.</summary>
        public bool Folded
        {
            get => _folded;
            set
            {
                _folded = value;
                Apply();
            }
        }

        private void Awake() => _umbrellas = GetComponentsInChildren<TerraceUmbrella>(true);

        private void OnEnable()
        {
            if (_sun != null)
            {
                _sun.NightChanged += OnNightChanged;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (_sun != null)
            {
                _sun.NightChanged -= OnNightChanged;
            }
        }

        private void OnNightChanged(bool night) => Apply();

        private void Apply()
        {
            bool open = !_folded && (_sun == null || !_sun.IsNight);
            if (_umbrellas == null || _open == open)
            {
                return;
            }

            _open = open;
            foreach (TerraceUmbrella umbrella in _umbrellas)
            {
                umbrella.SetOpen(open);
            }
        }
    }
}
