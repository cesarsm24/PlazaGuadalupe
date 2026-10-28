using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Turistas que se han ido de la plaza, guardados por personaje para reutilizarlos en vez de crear otros.
    /// </summary>
    internal sealed class TouristPool
    {
        private readonly Dictionary<GameObject, Stack<Tourist>> _stored = new Dictionary<GameObject, Stack<Tourist>>();
        private readonly Dictionary<Tourist, GameObject> _prefabOf = new Dictionary<Tourist, GameObject>();

        public int Count
        {
            get
            {
                int count = 0;
                foreach (Stack<Tourist> stack in _stored.Values)
                {
                    count += stack.Count;
                }

                return count;
            }
        }

        /// <summary>Un turista del personaje: uno guardado si lo hay; si no, uno nuevo dentro de <paramref name="parent"/>.</summary>
        public Tourist Take(GameObject prefab, Transform parent)
        {
            if (_stored.TryGetValue(prefab, out Stack<Tourist> stack) && stack.Count > 0)
            {
                return stack.Pop();
            }

            var tourist = Object.Instantiate(prefab, parent).GetComponent<Tourist>();
            _prefabOf[tourist] = prefab;
            return tourist;
        }

        /// <summary>Guarda un turista ya retirado de la plaza.</summary>
        public void Store(Tourist tourist)
        {
            GameObject prefab = _prefabOf[tourist];
            if (!_stored.TryGetValue(prefab, out Stack<Tourist> stack))
            {
                stack = new Stack<Tourist>();
                _stored[prefab] = stack;
            }

            stack.Push(tourist);
        }

        /// <summary>Personaje del que salió el turista.</summary>
        public GameObject PrefabOf(Tourist tourist) => _prefabOf[tourist];
    }
}
