using Guadalupe.Ambience;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Prefabs que el generador busca en la escena, donde se colocan a mano, para conectar sus luces (farolas,
    /// fuente, árbol de luces y monasterio), apoyar las casas en el atrio u ocultar las mesas junto al árbol en
    /// Navidad. Es un recurso del proyecto
    /// (<c>Settings/Generation/SO_PropPrefabs.asset</c>) que se edita en el Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Guadalupe/Prefabs del mobiliario", fileName = "PropPrefabs")]
    public sealed class PropPrefabs : ScriptableObject
    {

        [SerializeField] private GameObject _terraceSet;
        [SerializeField] private GameObject _terraceTable;
        [SerializeField] private GameObject _streetLamp;
        [SerializeField] private GameObject _fountain;
        [SerializeField] private GameObject _christmasTree;
        [SerializeField] private GameObject _monastery;

        /// <summary>Mesa de terraza con cuatro sillas y sombrilla.</summary>
        public GameObject TerraceSet => _terraceSet;

        /// <summary>Mesa de terraza con cuatro sillas, sin sombrilla.</summary>
        public GameObject TerraceTable => _terraceTable;

        /// <summary>Farola de pie, con su luz.</summary>
        public GameObject StreetLamp => _streetLamp;

        /// <summary>Fuente de la plaza, con sus luces sumergidas.</summary>
        public GameObject Fountain => _fountain;

        /// <summary>Árbol de luces navideño: cono de luces, armazón y estrella (con <see cref="Core.FestiveBulbs"/>).</summary>
        public GameObject ChristmasTree => _christmasTree;

        /// <summary>Monasterio y atrio, con sus focos y adornos de Navidad.</summary>
        public GameObject Monastery => _monastery;

        /// <summary>Carga el recurso de prefabs del proyecto.</summary>
        public static PropPrefabs Load() => ProjectPaths.Load<PropPrefabs>(ProjectPaths.PropPrefabs);
    }
}
