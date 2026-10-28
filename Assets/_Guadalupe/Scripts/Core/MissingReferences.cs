using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Comprobación de las referencias que un componente necesita para funcionar: si falta alguna, lo dice en
    /// la consola con el nombre del objeto en lugar de fallar en silencio o con un error en cada fotograma.
    /// </summary>
    public static class MissingReferences
    {
        /// <summary>
        /// Devuelve true si están todas las referencias. Si falta alguna, registra un error en juego (en el Editor
        /// no, para no llenar la consola al editar prefabs o escenas a medio montar) y devuelve false.
        /// </summary>
        public static bool Check(Component owner, params (Object Value, string Name)[] references)
        {
            bool complete = true;
            foreach ((Object value, string name) in references)
            {
                if (value == null)
                {
                    complete = false;
                    if (Application.isPlaying)
                    {
                        Debug.LogError($"{owner.GetType().Name} en «{owner.name}»: falta asignar {name}.", owner);
                    }
                }
            }

            return complete;
        }
    }
}
