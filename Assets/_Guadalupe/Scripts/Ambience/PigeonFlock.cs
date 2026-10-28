using System.Collections.Generic;
using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Bandada de palomas que vuela en círculos sobre las torres del monasterio, se posa en la plaza, camina
    /// picoteando y levanta el vuelo cuando el jugador se acerca, contagiando el susto a las palomas cercanas.
    /// </summary>
    /// <remarks>
    /// Cada paloma es una instancia del prefab (con <see cref="PigeonModel"/>, que indica sus alas) gobernada por
    /// una máquina de estados: volando, aterrizando, en el suelo y huyendo. Las reglas de cada estado están en
    /// <see cref="PigeonBehaviour"/>; aquí se aplican a los objetos y se busca el suelo con raycasts. De noche
    /// (<see cref="SunController.IsNight"/>) la bandada se oculta.
    /// </remarks>
    public sealed class PigeonFlock : MonoBehaviour
    {
        private enum State
        {
            Flying,
            Landing,
            Ground,
            Fleeing,
        }

        private sealed class Pigeon
        {
            public Transform Body;
            public Transform LeftWing;
            public Transform RightWing;
            public State State;
            public Vector3 Velocity;
            public Vector3 Target;
            public float Timer;
            public float Phase;
            public float OrbitRadius;
            public float OrbitHeight;
            public float OrbitAngle;
            public float OrbitSpeed;
        }

        [SerializeField] private SunController _sun;
        [Tooltip("Jugador: las palomas levantan el vuelo cuando se acerca.")]
        [SerializeField] private Transform _player;
        [Tooltip("Prefab de la paloma, con un PigeonModel que indica sus alas.")]
        [SerializeField] private GameObject _template;
        [SerializeField, Min(1)] private int _count = 18;

        [Header("Vuelo")]
        [Tooltip("Centro del círculo de vuelo (sobre las torres del monasterio).")]
        [SerializeField] private Transform _orbitCenter;
        [Tooltip("Radio mínimo y máximo de la órbita (m).")]
        [SerializeField] private Vector2 _orbitRadius = new Vector2(14f, 26f);
        [Tooltip("Altura mínima y máxima de la órbita (m).")]
        [SerializeField] private Vector2 _orbitHeight = new Vector2(30f, 42f);
        [SerializeField] private float _flySpeed = 8f;

        [Header("Suelo")]
        [Tooltip("Centro de la zona de la plaza donde se posan.")]
        [SerializeField] private Transform _landingCenter;
        [SerializeField] private float _landingRadius = 16f;
        [Tooltip("Centro de la zona que evitan al caminar (la fuente).")]
        [SerializeField] private Transform _avoidCenter;
        [SerializeField] private float _avoidRadius = 4.2f;
        [Tooltip("Distancia al jugador a la que levantan el vuelo (m).")]
        [SerializeField] private float _scareDistance = 3.5f;
        [Tooltip("Capas en las que se busca el suelo; solo se posan si el primer impacto es el terreno.")]
        [SerializeField] private LayerMask _groundMask = ~0;

        // Tiempos (s) que pasa cada paloma en un estado antes de cambiar, elegidos al azar en el intervalo para que
        // la bandada no se mueva a la vez.
        private static readonly Vector2 FirstGroundTime = new Vector2(10f, 40f);
        private static readonly Vector2 FirstFlightTime = new Vector2(8f, 30f);
        private static readonly Vector2 GroundTime = new Vector2(15f, 50f);
        private static readonly Vector2 FlightTime = new Vector2(12f, 35f);
        private const float RetryLandingTime = 5f;
        private const float FleeTime = 3.5f;

        // Agilidad del giro hacia el destino en cada estado: más alta, giros más cerrados.
        private const float OrbitAgility = 2.5f;
        private const float LandingAgility = 4f;
        private const float FleeAgility = 6f;

        /// <summary>Altura desde la que se lanza el rayo que busca el suelo, por encima de cualquier tejado.</summary>
        private const float GroundRayHeight = 60f;

        private readonly List<Pigeon> _birds = new List<Pigeon>();

        private bool _hidden;

        private void OnEnable()
        {
            if (_sun != null)
            {
                _sun.NightChanged += OnNightChanged;
            }
        }

        private void OnDisable()
        {
            if (_sun != null)
            {
                _sun.NightChanged -= OnNightChanged;
            }
        }

        private void Start()
        {
            if (!MissingReferences.Check(this, (_template, "el prefab de la paloma"), (_orbitCenter, "el centro del vuelo"),
                    (_landingCenter, "el centro de la zona donde se posan"), (_avoidCenter, "el centro de la zona que evitan")))
            {
                enabled = false;
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                GameObject go = Instantiate(_template, transform);
                var model = go.GetComponent<PigeonModel>();
                go.name = $"Paloma_{i + 1:00}";
                var bird = new Pigeon
                {
                    Body = go.transform,
                    LeftWing = model != null ? model.LeftWing : null,
                    RightWing = model != null ? model.RightWing : null,
                    Phase = Random.value * 10f,
                    OrbitRadius = Random.Range(_orbitRadius.x, _orbitRadius.y),
                    OrbitHeight = Random.Range(_orbitHeight.x, _orbitHeight.y),
                    OrbitAngle = Random.value * Mathf.PI * 2f,
                    OrbitSpeed = Random.Range(0.25f, 0.4f) * (Random.value < 0.5f ? 1f : -1f),
                };

                // La mitad empieza en el suelo de la plaza y la otra mitad volando.
                if (i % 2 == 0 && TryLandingSpot(out Vector3 spot))
                {
                    bird.State = State.Ground;
                    bird.Body.position = spot;
                    bird.Timer = Random.Range(FirstGroundTime.x, FirstGroundTime.y);
                }
                else
                {
                    bird.State = State.Flying;
                    bird.Body.position = OrbitPoint(bird);
                    bird.Timer = Random.Range(FirstFlightTime.x, FirstFlightTime.y);
                }

                _birds.Add(bird);
            }

            // El sol ya puede haber avisado de la noche antes de que existieran las palomas.
            OnNightChanged(_sun != null && _sun.IsNight);
        }

        /// <summary>De noche se ocultan; por la mañana reaparecen en el mismo estado.</summary>
        private void OnNightChanged(bool night)
        {
            _hidden = night;
            foreach (Pigeon bird in _birds)
            {
                bird.Body.gameObject.SetActive(!night);
            }
        }

        private void Update()
        {
            if (_hidden)
            {
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            foreach (Pigeon bird in _birds)
            {
                switch (bird.State)
                {
                    case State.Flying:
                        UpdateFlying(bird, dt);
                        break;
                    case State.Landing:
                        UpdateLanding(bird, dt);
                        break;
                    case State.Ground:
                        UpdateGround(bird, dt);
                        break;
                    case State.Fleeing:
                        UpdateFleeing(bird, dt);
                        break;
                }

                AnimateWings(bird, dt);
            }
        }

        private Vector3 OrbitPoint(Pigeon bird)
        {
            return _orbitCenter.position + new Vector3(Mathf.Cos(bird.OrbitAngle) * bird.OrbitRadius, bird.OrbitHeight,
                Mathf.Sin(bird.OrbitAngle) * bird.OrbitRadius);
        }

        private void UpdateFlying(Pigeon bird, float dt)
        {
            bird.OrbitAngle += bird.OrbitSpeed * dt;
            bird.OrbitHeight += Mathf.Sin(Time.time * 0.3f + bird.Phase) * dt * 0.8f;
            Steer(bird, OrbitPoint(bird), _flySpeed, OrbitAgility, dt);

            bird.Timer -= dt;
            if (bird.Timer <= 0f)
            {
                if (TryLandingSpot(out Vector3 spot))
                {
                    bird.Target = spot;
                    bird.State = State.Landing;
                }
                else
                {
                    bird.Timer = RetryLandingTime;
                }
            }
        }

        private void UpdateLanding(Pigeon bird, float dt)
        {
            Vector3 toTarget = bird.Target - bird.Body.position;
            float distance = toTarget.magnitude;
            float speed = PigeonBehaviour.LandingSpeed(distance, _flySpeed);
            Steer(bird, bird.Target + Vector3.up * Mathf.Min(distance * 0.3f, 3f), speed, LandingAgility, dt);
            if (PigeonBehaviour.HasLanded(distance, bird.Body.position.y - bird.Target.y))
            {
                bird.Body.position = bird.Target;
                bird.Velocity = Vector3.zero;
                bird.State = State.Ground;
                bird.Timer = Random.Range(GroundTime.x, GroundTime.y);
                bird.Body.rotation = Quaternion.Euler(0f, bird.Body.eulerAngles.y, 0f);
            }

            if (PlayerNear(bird, _scareDistance * 1.5f))
            {
                Flee(bird);
            }
        }

        private void UpdateGround(Pigeon bird, float dt)
        {
            bird.Phase += dt;
            if (PigeonBehaviour.IsWalking(bird.Phase))
            {
                Vector3 forward = bird.Body.forward;
                Vector3 next = bird.Body.position + forward * (PigeonBehaviour.WalkSpeed * dt);
                if (GroundAt(next, out Vector3 ground) && (ground - _avoidCenter.position).sqrMagnitude > _avoidRadius * _avoidRadius)
                {
                    bird.Body.position = ground;
                }
                else
                {
                    bird.Body.Rotate(0f, 120f * dt, 0f);
                }

                bird.Body.Rotate(0f, Mathf.Sin(bird.Phase * 1.7f) * 40f * dt, 0f);
            }

            bird.Body.rotation = Quaternion.Euler(PigeonBehaviour.HeadPitch(bird.Phase), bird.Body.eulerAngles.y, 0f);

            bird.Timer -= dt;
            if (bird.Timer <= 0f || PlayerNear(bird, _scareDistance))
            {
                Flee(bird);
            }
        }

        private void UpdateFleeing(Pigeon bird, float dt)
        {
            // Despegue casi vertical alejándose del jugador; después vuelve a la órbita.
            Steer(bird, bird.Target, _flySpeed * 1.2f, FleeAgility, dt);
            bird.Timer -= dt;
            if (bird.Timer <= 0f || (bird.Target - bird.Body.position).sqrMagnitude < 4f)
            {
                bird.State = State.Flying;
                bird.Timer = Random.Range(FlightTime.x, FlightTime.y);
            }
        }

        private void Flee(Pigeon bird)
        {
            if (bird.State == State.Fleeing)
            {
                return;
            }

            Vector3 away = _player != null ? bird.Body.position - _player.position : Random.insideUnitSphere;
            (bird.Target, bird.Velocity) = PigeonBehaviour.TakeOff(bird.Body.position, away, Random.Range(9f, 14f));
            bird.State = State.Fleeing;
            bird.Timer = FleeTime;

            // Las que están cerca en el suelo se asustan también.
            foreach (Pigeon other in _birds)
            {
                if (other != bird && other.State == State.Ground &&
                    (other.Body.position - bird.Body.position).sqrMagnitude < PigeonBehaviour.ScareSpreadDistance * PigeonBehaviour.ScareSpreadDistance)
                {
                    other.Timer = Mathf.Min(other.Timer, Random.Range(0.05f, 0.4f));
                }
            }
        }

        private bool PlayerNear(Pigeon bird, float distance)
        {
            return _player != null && (_player.position - bird.Body.position).sqrMagnitude < distance * distance;
        }

        private void Steer(Pigeon bird, Vector3 target, float speed, float agility, float dt)
        {
            Vector3 desired = (target - bird.Body.position).normalized * speed;
            bird.Velocity = Vector3.Lerp(bird.Velocity, desired, Mathf.Clamp01(agility * dt));
            bird.Body.position += bird.Velocity * dt;
            if (bird.Velocity.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(bird.Velocity);
                // Se inclina en los giros.
                float bank = Vector3.SignedAngle(bird.Body.forward, bird.Velocity, Vector3.up);
                bird.Body.rotation = Quaternion.Slerp(bird.Body.rotation, look * Quaternion.Euler(0f, 0f, -bank * 1.5f), 5f * dt);
            }
        }

        private void AnimateWings(Pigeon bird, float dt)
        {
            if (bird.LeftWing == null || bird.RightWing == null)
            {
                return;
            }

            if (bird.State == State.Ground)
            {
                // En el suelo las alas van plegadas sobre el lomo, hacia atrás.
                bird.LeftWing.localRotation = Quaternion.Euler(-8f, -85f, 0f);
                bird.RightWing.localRotation = Quaternion.Euler(-8f, 85f, 0f);
                return;
            }

            float angle = PigeonBehaviour.WingAngle(bird.State == State.Fleeing, bird.State == State.Flying, Time.time, bird.Phase);

            bird.LeftWing.localRotation = Quaternion.Euler(0f, 0f, angle);
            bird.RightWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private bool TryLandingSpot(out Vector3 spot)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 r = Random.insideUnitCircle * _landingRadius;
                Vector3 center = _landingCenter.position;
                var p = new Vector3(center.x + r.x, 0f, center.z + r.y);
                Vector3 avoid = _avoidCenter.position;
                if ((p - new Vector3(avoid.x, 0f, avoid.z)).sqrMagnitude < _avoidRadius * _avoidRadius)
                {
                    continue;
                }

                if (GroundAt(p, out spot))
                {
                    return true;
                }
            }

            spot = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Busca el suelo bajo un punto. Devuelve false si el primer impacto no es suelo transitable (una mesa, un
        /// toldo...), para que no se posen sobre el mobiliario.
        /// </summary>
        private bool GroundAt(Vector3 p, out Vector3 ground)
        {
            if (Physics.Raycast(new Vector3(p.x, GroundRayHeight, p.z), Vector3.down, out RaycastHit hit, 2f * GroundRayHeight, _groundMask, QueryTriggerInteraction.Ignore) &&
                hit.collider is TerrainCollider)
            {
                ground = hit.point;
                return true;
            }

            ground = Vector3.zero;
            return false;
        }
    }
}
