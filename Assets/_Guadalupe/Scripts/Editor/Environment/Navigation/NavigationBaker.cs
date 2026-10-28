using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Hornea la malla de navegación de la escena con el <see cref="NavMeshSurface"/> colocado en ella. Incluye
    /// el entorno generado y el mobiliario (capas Environment y Props); el jugador y los NPC quedan fuera.
    /// </summary>
    /// <remarks>
    /// El agente es el tipo por defecto de la ventana Navigation, ajustado al tamaño del jugador (radio 0,3 m,
    /// altura 1,8 m, escalón 0,3 m, pendiente 40°). Antes de hornear se comprueba que sigue así.
    /// </remarks>
    public static class NavigationBaker
    {
        private const float AgentRadius = 0.3f;
        private const float AgentHeight = 1.8f;
        private const float AgentClimb = 0.3f;
        private const float AgentSlope = 40f;

        /// <summary>Hornea la NavMesh desde el menú del Editor.</summary>
        [MenuItem("Guadalupe/Entorno/Hornear NavMesh", priority = 21)]
        public static void BakeFromMenu()
        {
            Bake();
            EditorSceneManager.SaveOpenScenes();
        }

        /// <summary>Hornea la NavMesh y la guarda junto a la escena.</summary>
        /// <exception cref="System.InvalidOperationException">
        /// Si la escena no tiene <see cref="NavMeshSurface"/> o el agente no tiene las medidas del jugador.
        /// </exception>
        public static void Bake() => Save(Build());

        /// <summary>
        /// Comprueba que se puede hornear (agente con las medidas del jugador y un <see cref="NavMeshSurface"/> en
        /// la escena) y devuelve la superficie.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Si no se cumple alguna de las dos condiciones.</exception>
        internal static NavMeshSurface FindSurface()
        {
            CheckAgent();
            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);
            if (surface == null)
            {
                throw new System.InvalidOperationException("La escena no tiene ningún NavMeshSurface.");
            }

            return surface;
        }

        /// <summary>Hornea la NavMesh en memoria con lo que está activo en la escena.</summary>
        internal static NavMeshSurface Build()
        {
            NavMeshSurface surface = FindSurface();
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            return surface;
        }

        /// <summary>Guarda la NavMesh horneada junto a la escena.</summary>
        internal static void Save(NavMeshSurface surface)
        {
            // Mismo nombre y carpeta que usa el Inspector del NavMeshSurface, para que la escena conserve la malla.
            Scene scene = surface.gameObject.scene;
            string parent = Path.GetDirectoryName(scene.path);
            string folder = $"{parent}/{scene.name}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parent, scene.name);
            }

            string path = $"{folder}/NavMesh-{surface.name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void CheckAgent()
        {
            NavMeshBuildSettings agent = NavMesh.GetSettingsByIndex(0);
            if (!Mathf.Approximately(agent.agentRadius, AgentRadius) || !Mathf.Approximately(agent.agentHeight, AgentHeight) ||
                !Mathf.Approximately(agent.agentClimb, AgentClimb) || !Mathf.Approximately(agent.agentSlope, AgentSlope))
            {
                throw new System.InvalidOperationException(
                    $"El agente de la ventana Navigation debe tener radio {AgentRadius}, altura {AgentHeight}, " +
                    $"escalón {AgentClimb} y pendiente {AgentSlope}.");
            }
        }
    }
}
