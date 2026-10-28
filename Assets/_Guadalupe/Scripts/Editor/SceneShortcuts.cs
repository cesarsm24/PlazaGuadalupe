using UnityEditor;
using UnityEditor.SceneManagement;

namespace Guadalupe.Editor
{
    /// <summary>Atajo de menú para abrir la escena de la plaza.</summary>
    internal static class SceneShortcuts
    {
        [MenuItem("Guadalupe/Abrir la plaza", priority = 0)]
        private static void OpenPlaza()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ProjectPaths.MainScene);
            }
        }
    }
}
