using UnityEngine;

namespace Guadalupe.Core
{
    /// <summary>
    /// Marca el objeto raíz del entorno que crea el generador del Editor. Al regenerar, el generador sustituye
    /// este objeto y todo lo que cuelga de él; lo que está fuera se coloca a mano y no se toca.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedEnvironment : MonoBehaviour
    {
    }
}
