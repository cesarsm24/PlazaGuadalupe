using System.Collections.Generic;
using Guadalupe.Ambience;
using Guadalupe.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Lo colocado a mano en la escena que necesita lo generado: el sol y el reloj, los controladores de
    /// iluminación a los que se asignan las luces y el monasterio.
    /// </summary>
    internal sealed class SceneServices
    {
        private Scene _scene;

        /// <summary>Sol de la escena, del que dependen las luces generadas.</summary>
        public SunController Sun { get; private set; }

        /// <summary>Reloj de la simulación de la escena, que mueve las agujas del reloj de la torre.</summary>
        public SimulationClock Clock { get; private set; }

        public StreetLights StreetLights { get; private set; }

        public NightFloodLights FloodLights { get; private set; }

        public FestiveLights FestiveLights { get; private set; }

        /// <summary>Monasterio colocado en la escena (instancia de <see cref="PropPrefabs.Monastery"/>).</summary>
        public GameObject PlacedMonastery { get; private set; }

        /// <summary>Atrio del monasterio colocado, en el que se apoyan algunas casas.</summary>
        public Transform PlacedAtrium { get; private set; }

        /// <summary>
        /// Localiza en la escena activa, fuera del entorno generado, el sol, el reloj y los controladores de
        /// iluminación.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Si falta alguno.</exception>
        public static SceneServices Find()
        {
            return new SceneServices
            {
                _scene = SceneManager.GetActiveScene(),
                Sun = FindPlaced<SunController>(),
                Clock = FindPlaced<SimulationClock>(),
                StreetLights = FindPlaced<StreetLights>(),
                FloodLights = FindPlaced<NightFloodLights>(),
                FestiveLights = FindPlaced<FestiveLights>(),
            };
        }

        /// <summary>Localiza el monasterio colocado en la escena y su atrio.</summary>
        /// <exception cref="System.InvalidOperationException">Si no está o hay más de uno.</exception>
        public void FindPlacedMonastery()
        {
            GameObject prefab = PropPrefabs.Load().Monastery;
            List<GameObject> found = InstancesOf(prefab);
            if (found.Count != 1)
            {
                throw new System.InvalidOperationException($"La escena debe tener un monasterio ({prefab.name}); tiene {found.Count}.");
            }

            PlacedMonastery = found[0];
            PlacedAtrium = PlacedMonastery.transform.Find(MonasteryPrefabNodes.Atrium);
        }

        /// <summary>Copias de un prefab en la escena, generadas o colocadas a mano, en orden de jerarquía.</summary>
        public List<GameObject> InstancesOf(GameObject prefab)
        {
            var found = new List<GameObject>();
            foreach (GameObject sceneRoot in _scene.GetRootGameObjects())
            {
                foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) &&
                        PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) == prefab)
                    {
                        found.Add(t.gameObject);
                    }
                }
            }

            return found;
        }

        private static T FindPlaced<T>() where T : Component
        {
            foreach (T found in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (found.GetComponentInParent<GeneratedEnvironment>(true) == null)
                {
                    return found;
                }
            }

            throw new System.InvalidOperationException($"La escena no tiene ningún {typeof(T).Name} fuera del entorno generado.");
        }
    }
}
