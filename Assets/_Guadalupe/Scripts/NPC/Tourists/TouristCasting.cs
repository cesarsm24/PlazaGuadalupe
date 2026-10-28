using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Elige qué personaje sale y con qué ropa. Nadie se repite mientras quede algún personaje libre vestido para la
    /// época (<see cref="TouristCrowd.SeasonRank"/>). Si no queda ninguno, puede salir otra copia de un adulto, con otra
    /// ropa y nunca en el mismo grupo ni en el mismo destino que el otro; de cada niño hay como mucho uno.
    /// </summary>
    internal sealed class TouristCasting
    {
        private readonly GameObject[] _characters;
        private readonly int _maxCopies;
        private readonly IReadOnlyCollection<Tourist> _inPlaza;
        private readonly TouristPool _pool;
        private readonly Func<Tourist, int> _destinationOf;
        private readonly List<GameObject> _candidates = new List<GameObject>();
        private readonly List<int> _clothesInUse = new List<int>();

        /// <param name="inPlaza">Turistas que hay ahora en la plaza.</param>
        /// <param name="destinationOf">Destino al que va el grupo de un turista (-1 si ninguno).</param>
        public TouristCasting(GameObject[] characters, int maxCopies, IReadOnlyCollection<Tourist> inPlaza, TouristPool pool,
            Func<Tourist, int> destinationOf)
        {
            _characters = characters;
            _maxCopies = maxCopies;
            _inPlaza = inPlaza;
            _pool = pool;
            _destinationOf = destinationOf;
        }

        /// <summary>
        /// Personaje para el siguiente miembro de un grupo que va a <paramref name="destination"/>: uno libre vestido
        /// para la época, en el orden de <see cref="TouristCrowd.SeasonRank"/>; si no queda ninguno, otra copia del
        /// adulto con menos copias que no esté en el grupo ni en ese destino. Null si no hay ninguno posible.
        /// </summary>
        /// <param name="chosen">Personajes ya elegidos para el grupo.</param>
        public GameObject PickPrefab(bool christmas, bool childAllowed, int destination, List<GameObject> chosen)
        {
            for (int rank = 0; rank <= 1; rank++)
            {
                _candidates.Clear();
                foreach (GameObject prefab in _characters)
                {
                    var tourist = prefab.GetComponent<Tourist>();
                    if (TouristCrowd.SeasonRank(tourist.Season, christmas) == rank && (childAllowed || !tourist.IsChild) &&
                        CopiesOf(prefab) == 0 && !chosen.Contains(prefab))
                    {
                        _candidates.Add(prefab);
                    }
                }

                if (_candidates.Count > 0)
                {
                    return _candidates[Random.Range(0, _candidates.Count)];
                }
            }

            GameObject best = null;
            int fewest = _maxCopies;
            foreach (GameObject prefab in _characters)
            {
                var tourist = prefab.GetComponent<Tourist>();
                int copies = CopiesOf(prefab);
                // Una copia tiene que vestir distinto: solo personajes con alguna variante de ropa libre.
                if (!tourist.IsChild && TouristCrowd.SeasonRank(tourist.Season, christmas) >= 0 && copies < fewest &&
                    copies < tourist.ClothesCount && !chosen.Contains(prefab) && !IsAt(prefab, destination))
                {
                    best = prefab;
                    fewest = copies;
                }
            }

            return best;
        }

        /// <summary>Ropa para el personaje: si ya hay alguien de él en la plaza, una variante que no lleve; si no, -1 (al azar).</summary>
        public int ClothesFor(GameObject prefab)
        {
            _clothesInUse.Clear();
            foreach (Tourist tourist in _inPlaza)
            {
                if (_pool.PrefabOf(tourist) == prefab)
                {
                    _clothesInUse.Add(tourist.ClothesIndex);
                }
            }

            return _clothesInUse.Count == 0 ? -1 : TouristCrowd.UnusedClothes(prefab.GetComponent<Tourist>().ClothesCount, _clothesInUse);
        }

        /// <summary>Indica si algún miembro del grupo se encontraría en el destino con otra copia de sí mismo.</summary>
        public bool MeetsACopy(IReadOnlyList<Tourist> members, int destination)
        {
            foreach (Tourist member in members)
            {
                foreach (Tourist other in _inPlaza)
                {
                    if (other != member && !Contains(members, other) && _pool.PrefabOf(other) == _pool.PrefabOf(member) &&
                        _destinationOf(other) == destination)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Turistas de ese personaje que hay ahora en la plaza.</summary>
        private int CopiesOf(GameObject prefab)
        {
            int copies = 0;
            foreach (Tourist tourist in _inPlaza)
            {
                if (_pool.PrefabOf(tourist) == prefab)
                {
                    copies++;
                }
            }

            return copies;
        }

        /// <summary>Indica si ya hay alguien de ese personaje en el destino.</summary>
        private bool IsAt(GameObject prefab, int destination)
        {
            foreach (Tourist tourist in _inPlaza)
            {
                if (_pool.PrefabOf(tourist) == prefab && _destinationOf(tourist) == destination)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(IReadOnlyList<Tourist> list, Tourist tourist)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == tourist)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
