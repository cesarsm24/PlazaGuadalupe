using System.Collections.Generic;
using Guadalupe.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Puebla la plaza de turistas según la afluencia del escenario a cada hora. Llegan solos o en grupo por las
    /// bocacalles que el jugador no ve de cerca, hacen de una a tres paradas juntos y se marchan.
    /// </summary>
    public sealed class TouristSpawner : MonoBehaviour
    {
        /// <summary>Un grupo de turistas que visitan la plaza juntos.</summary>
        private sealed class Group
        {
            public readonly List<Tourist> Members = new List<Tourist>();
            public int StopsLeft;
            public int Destination = -1;
            public int Finished;
        }

        private const float SideBySideSpacing = 0.7f; // m entre los que se paran en fila

        [SerializeField] private SessionState _session;
        [SerializeField] private SimulationClock _clock;
        [Tooltip("Lugares a los que van y qué hacen en cada uno.")]
        [SerializeField] private TouristDestination[] _destinations = new TouristDestination[0];
        [Tooltip("Bocacalles por las que entran y salen, sobre la NavMesh.")]
        [SerializeField] private Transform[] _entrances = new Transform[0];

        [Header("Personajes")]
        [Tooltip("Personajes posibles; cada uno indica la época para la que va vestido.")]
        [SerializeField] private GameObject[] _characters = new GameObject[0];
        [Tooltip("Copias que puede haber a la vez de un mismo adulto cuando no queda ninguno libre.")]
        [SerializeField, Min(1)] private int _maxCopies = 2;
        [Tooltip("Escala mínima y máxima de cada turista, para variar la estatura.")]
        [SerializeField] private Vector2 _scale = new Vector2(0.92f, 1f);
        [Tooltip("Variación de la velocidad de andar sobre la del prefab (fracción).")]
        [SerializeField, Range(0f, 0.3f)] private float _speedVariation = 0.12f;

        [Tooltip("Móvil que sacan para hacer fotos.")]
        [SerializeField] private GameObject _phone;

        [Header("Afluencia")]
        [Tooltip("Turistas a la vez con afluencia máxima.")]
        [SerializeField, Min(0)] private int _maxTourists = 40;
        [Tooltip("Segundos entre la llegada de dos grupos.")]
        [SerializeField, Min(0.1f)] private float _spawnInterval = 2.5f;
        [Tooltip("Distancia al jugador a partir de la que puede aparecer alguien aunque esté a la vista (m).")]
        [SerializeField, Min(0f)] private float _hiddenDistance = 35f;

        private readonly HashSet<Tourist> _tourists = new HashSet<Tourist>();
        private readonly Dictionary<Tourist, Group> _groupOf = new Dictionary<Tourist, Group>();
        private readonly TouristPool _pool = new TouristPool();
        private readonly SeatPlanner _seats = new SeatPlanner();
        private readonly List<int> _freeSpots = new List<int>();
        private readonly List<TouristVisit> _plans = new List<TouristVisit>();
        private float[] _weights;
        private readonly List<GameObject> _chosen = new List<GameObject>();
        private SpotOccupancy _spots;
        private TouristCasting _casting;
        private Transform _root;
        private Camera _camera;
        private float _nextSpawn;
        private bool _active;

        /// <summary>Turistas que hay ahora en la plaza.</summary>
        public int Count => _tourists.Count;

        /// <summary>Veces que un grupo ha pasado de una parada a otra sin marcharse.</summary>
        internal int ChainedStops { get; private set; }

        /// <summary>Turistas guardados para reutilizarlos.</summary>
        public int Pooled => _pool.Count;

        internal SeatPlanner Seats => _seats;

        /// <summary>Turistas que debería haber ahora según el escenario y la hora.</summary>
        public int Target => _session.Scenario == null
            ? 0
            : TouristCrowd.TargetCount(_session.Scenario.CrowdAt(_clock.Hour), _maxTourists);

        private void OnEnable()
        {
            if (!MissingReferences.Check(this, (_session, "el estado de la sesión"), (_clock, "el reloj de la simulación")))
            {
                enabled = false;
                return;
            }

            _spots ??= new SpotOccupancy(_destinations);
            _weights ??= new float[_destinations.Length];
            _casting ??= new TouristCasting(_characters, _maxCopies, _tourists, _pool,
                tourist => _groupOf.TryGetValue(tourist, out Group group) ? group.Destination : -1);
            _session.MenuChanged += OnMenuChanged;
            _session.ScenarioChanged += OnScenarioChanged;
            if (!_session.InMenu)
            {
                Begin();
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.MenuChanged -= OnMenuChanged;
                _session.ScenarioChanged -= OnScenarioChanged;
            }

            Clear();
        }

        private void OnMenuChanged(bool inMenu)
        {
            if (!inMenu && !_active)
            {
                Begin();
            }
        }

        private void OnScenarioChanged(ScenarioDefinition scenario)
        {
            Clear();
            _active = false;
        }

        /// <summary>Puebla la plaza al empezar la visita, con todos los turistas ya en su sitio.</summary>
        private void Begin()
        {
            _active = true;
            _camera = Camera.main;
            if (_root == null)
            {
                _root = new GameObject("Turistas_Activos").transform;
                _root.SetParent(transform, false);
            }

            // Cada intento coloca un grupo; cuando ya no caben más, se para.
            int target = Target;
            for (int attempt = 0; Count < target && attempt < target * 2; attempt++)
            {
                SpawnGroup(true, target - Count);
            }

            _nextSpawn = Time.time + _spawnInterval;
        }

        private void Update()
        {
            if (!_active || Time.time < _nextSpawn)
            {
                return;
            }

            _nextSpawn = Time.time + _spawnInterval * Random.Range(0.6f, 1.4f);
            int missing = Target - Count;
            if (missing > 0)
            {
                SpawnGroup(false, missing);
            }
        }

        /// <summary>Hace llegar un grupo de hasta <paramref name="maxPeople"/> personas a un destino con sitio.</summary>
        /// <param name="atSpot">Aparecen ya en su sitio.</param>
        private void SpawnGroup(bool atSpot, int maxPeople)
        {
            int size = Mathf.Min(TouristCrowd.GroupSize(Random.value), maxPeople);
            int destination = -1;
            while (size >= 1 && (destination = PickDestination(size, -1)) < 0)
            {
                size--;
            }

            int entrance = atSpot ? Random.Range(0, _entrances.Length) : HiddenEntrance();
            if (destination < 0 || entrance < 0 || !PlanStop(destination, size, atSpot))
            {
                return;
            }

            // Primero se eligen los personajes: si no quedan suficientes sin repetir, el grupo es más pequeño.
            _chosen.Clear();
            for (int i = 0; i < size; i++)
            {
                // Ningún niño visita la plaza solo: el primero de cada grupo es un adulto.
                GameObject prefab = _casting.PickPrefab(_session.Scenario.Christmas, i > 0, destination, _chosen);
                if (prefab == null)
                {
                    break;
                }

                _chosen.Add(prefab);
            }

            if (_chosen.Count == 0 || (_chosen.Count < size && !PlanStop(destination, _chosen.Count, atSpot)))
            {
                return;
            }

            var group = new Group { StopsLeft = TouristCrowd.Stops(Random.value) - 1 };
            float speed = float.MaxValue;
            foreach (GameObject prefab in _chosen)
            {
                speed = Mathf.Min(speed, prefab.GetComponent<NavMeshAgent>().speed * (1f + Random.Range(-_speedVariation, _speedVariation)));
            }

            for (int i = 0; i < _chosen.Count; i++)
            {
                int clothes = _casting.ClothesFor(_chosen[i]);
                Tourist tourist = _pool.Take(_chosen[i], _root);
                group.Members.Add(tourist);
                _tourists.Add(tourist);
                _groupOf[tourist] = group;
                Vector3 start = atSpot ? _plans[i].Approach : NearEntrance(_entrances[entrance].position);
                tourist.Prepare(start, Random.Range(_scale.x, _scale.y), speed, Random.Range(30, 70), _phone, clothes);
            }

            StartStop(group, destination, atSpot);
        }

        /// <summary>
        /// Prepara en <see cref="_plans"/> la parada de un grupo de <paramref name="size"/> personas en el destino y
        /// deja en <see cref="_freeSpots"/> los sitios que ocupará; false si no hay sitio.
        /// </summary>
        private bool PlanStop(int destinationIndex, int size, bool atSpot)
        {
            TouristDestination destination = _destinations[destinationIndex];
            if (!_spots.FreeBlock(destinationIndex, destination.SideBySide ? 1 : size, _freeSpots))
            {
                return false;
            }

            Shuffle(_freeSpots);
            float stay = Random.Range(destination.Stay.x, destination.Stay.y);
            if (atSpot)
            {
                // Los que ya están al empezar llevan un rato: se irán escalonados, no todos a la vez.
                stay *= Random.Range(0.1f, 1f);
            }

            _plans.Clear();
            for (int i = 0; i < size; i++)
            {
                int spot = destination.SideBySide ? _freeSpots[0] : _freeSpots[i];
                float side = destination.SideBySide ? TouristCrowd.SideBySideOffset(i, size, SideBySideSpacing) : 0f;
                if (!TryPlan(destination, spot, side, size, stay, out TouristVisit visit))
                {
                    return false;
                }

                _plans.Add(visit);
            }

            return true;
        }

        /// <summary>Reserva los sitios planeados en <see cref="PlanStop"/> y manda a cada miembro a su sitio.</summary>
        private void StartStop(Group group, int destinationIndex, bool atSpot)
        {
            group.Destination = destinationIndex;
            group.Finished = 0;
            bool sideBySide = _destinations[destinationIndex].SideBySide;
            for (int i = 0; i < group.Members.Count; i++)
            {
                Tourist tourist = group.Members[i];
                _spots.Occupy(tourist, destinationIndex, sideBySide ? _freeSpots[0] : _freeSpots[i]);
                tourist.StopFinished -= OnStopFinished;
                tourist.StopFinished += OnStopFinished;
                tourist.Left -= OnLeft;
                tourist.Left += OnLeft;
                tourist.Visit(_plans[i], atSpot);
            }
        }

        /// <summary>Prepara la parada de un turista en un sitio; false si el sitio no está al alcance de la NavMesh.</summary>
        /// <param name="side">Desplazamiento lateral sobre el sitio, para los que se paran en fila.</param>
        private bool TryPlan(TouristDestination destination, int spot, float side, int groupSize, float stay, out TouristVisit visit)
        {
            Transform place = destination.Spots[spot];
            TouristActivity activity = destination.Seated
                ? TouristCrowd.SeatedActivity(groupSize, Random.value)
                : destination.Activities[Random.Range(0, destination.Activities.Length)];

            if (destination.Seated)
            {
                return _seats.TryPlan(place, activity, stay, out visit);
            }

            Vector3 position = place.position;
            if (side != 0f)
            {
                // Si al lado no hay suelo libre, se queda detrás del sitio.
                position = NavMesh.SamplePosition(place.position + place.right * side, out NavMeshHit sideHit, 0.25f, NavMesh.AllAreas)
                    ? sideHit.position
                    : place.position - place.forward * Mathf.Abs(side);
            }

            Quaternion rotation = place.rotation;
            Vector3 focus = destination.LookAt != null ? destination.LookAt.position : place.position + place.forward * 10f + Vector3.up * 1.5f;
            if (destination.LookAt != null)
            {
                Vector3 facing = (destination.LookAt.position - position).Flat();
                if (facing.sqrMagnitude > 1e-4f)
                {
                    rotation = Quaternion.LookRotation(facing);
                }
            }

            visit = new TouristVisit(position, position, rotation, activity, stay, focus);
            return true;
        }

        /// <summary>
        /// Un miembro ha terminado su parada: deja su sitio y, cuando han terminado todos, el grupo pasa a otra
        /// parada o se marcha.
        /// </summary>
        private void OnStopFinished(Tourist tourist)
        {
            _spots.Free(tourist);
            Group group = _groupOf[tourist];
            if (++group.Finished < group.Members.Count)
            {
                return;
            }

            if (group.StopsLeft > 0)
            {
                int destination = PickDestination(group.Members.Count, group.Destination);
                if (destination >= 0 && !_casting.MeetsACopy(group.Members, destination) && PlanStop(destination, group.Members.Count, false))
                {
                    group.StopsLeft--;
                    ChainedStops++;
                    StartStop(group, destination, false);
                    return;
                }
            }

            int exit = HiddenEntrance();
            if (exit < 0)
            {
                exit = Random.Range(0, _entrances.Length);
            }

            foreach (Tourist member in group.Members)
            {
                member.Leave(NearEntrance(_entrances[exit].position));
            }
        }

        private void OnLeft(Tourist tourist)
        {
            Release(tourist);
            tourist.Retire();
            _pool.Store(tourist);
        }

        private void Release(Tourist tourist)
        {
            tourist.StopFinished -= OnStopFinished;
            tourist.Left -= OnLeft;
            _spots.Free(tourist);
            _tourists.Remove(tourist);
            if (_groupOf.TryGetValue(tourist, out Group group))
            {
                group.Members.Remove(tourist);
                _groupOf.Remove(tourist);
            }
        }

        private void Clear()
        {
            foreach (Tourist tourist in new List<Tourist>(_tourists))
            {
                Release(tourist);
                if (tourist != null)
                {
                    tourist.Retire();
                    _pool.Store(tourist);
                }
            }
        }

        /// <summary>
        /// Destino al azar según su peso, entre los que admiten un grupo de <paramref name="size"/> y tienen sitio,
        /// sin repetir <paramref name="except"/>; -1 si no hay.
        /// </summary>
        private int PickDestination(int size, int except)
        {
            int count = _destinations.Length;
            for (int i = 0; i < count; i++)
            {
                TouristDestination destination = _destinations[i];
                bool fits = i != except && destination.Accepts(size) &&
                    _spots.FreeBlock(i, destination.SideBySide ? 1 : size, _freeSpots);
                _weights[i] = fits ? destination.Weight : 0f;
            }

            return TouristCrowd.PickWeighted(_weights, count, Random.value);
        }

        /// <summary>Bocacalle al azar que el jugador no ve de cerca; -1 si no hay.</summary>
        private int HiddenEntrance()
        {
            int count = _entrances.Length;
            int start = Random.Range(0, Mathf.Max(1, count));
            for (int i = 0; i < count; i++)
            {
                int entrance = (start + i) % count;
                if (IsHidden(_entrances[entrance].position))
                {
                    return entrance;
                }
            }

            return -1;
        }

        private bool IsHidden(Vector3 position)
        {
            if (_camera == null)
            {
                return true;
            }

            Vector3 viewport = _camera.WorldToViewportPoint(position);
            bool inView = viewport.z > 0f && viewport.x is > -0.1f and < 1.1f && viewport.y is > -0.1f and < 1.1f;
            return TouristCrowd.IsHiddenFromPlayer(Vector3.Distance(_camera.transform.position, position), inView, _hiddenDistance);
        }

        /// <summary>
        /// Punto al azar junto a la entrada para que un grupo no aparezca amontonado. Se busca por la NavMesh desde
        /// la entrada, así que nunca queda al otro lado de una tapia ni de una pared.
        /// </summary>
        private static Vector3 NearEntrance(Vector3 entrance)
        {
            Vector3 target = entrance + Random.insideUnitSphere.Flat() * 1.2f;
            return NavMesh.Raycast(entrance, target, out NavMeshHit hit, NavMesh.AllAreas) ? hit.position : target;
        }

        private static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
