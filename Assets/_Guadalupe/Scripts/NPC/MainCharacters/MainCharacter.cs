using System.Collections;
using System.Collections.Generic;
using Guadalupe.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Personaje principal de la plaza: pasea por su zona o está sentado en su silla, saluda al jugador cuando se
    /// acerca y, al lado y de cara, se puede hablar con él (<see cref="Interact"/>).
    /// </summary>
    /// <remarks>El modelo es intercambiable: basta con un Animator humanoide con el controlador AC_MainCharacter.</remarks>
    [RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
    public sealed class MainCharacter : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int IdleParameter = Animator.StringToHash("Idle");
        private static readonly int SeatedParameter = Animator.StringToHash("Seated");
        private static readonly int WaveTrigger = Animator.StringToHash("Wave");
        private static readonly int BowTrigger = Animator.StringToHash("Bow");
        private static readonly int BeckonTrigger = Animator.StringToHash("Beckon");
        private static readonly int NodTrigger = Animator.StringToHash("Nod");
        private static readonly int NodVariantParameter = Animator.StringToHash("NodVariant");
        private static readonly int LaughTrigger = Animator.StringToHash("Laugh");
        private static readonly int DrinkTrigger = Animator.StringToHash("Drink");
        private static readonly int DrinkState = Animator.StringToHash("Drink");
        private static readonly int TalkingParameter = Animator.StringToHash("Talking");
        private static readonly int TalkVariantParameter = Animator.StringToHash("TalkVariant");
        private static readonly int PointTrigger = Animator.StringToHash("Point");

        // Capa de brazos y cabeza: los gestos de sentado y el de hablar no deben mover el resto del cuerpo (el clip
        // de hablar de pie inclina el tronco).
        private const int UpperBodyLayer = 1;
        private const int IdleVariants = 3;

        // Margen sobre el radio de la zona que pueden pisar sus caminos, para rodear algo.
        private const float ZoneSlack = 0.5f;
        private const float ArriveDistance = 0.25f;
        private const float TurnDegreesPerSecond = 240f;

        private static readonly List<MainCharacter> Characters = new List<MainCharacter>();

        [Tooltip("Quién es: nombre, saludos y lo que sabe para la conversación.")]
        [SerializeField] private NpcProfile _profile;
        [Tooltip("Gesto con el que saluda al acercarse el jugador.")]
        [SerializeField] private Greeting _greeting = Greeting.Wave;
        [Tooltip("Si da la mano al hablar con él (de pie).")]
        [SerializeField] private bool _shakesHands;
        [Tooltip("Animación del apretón de manos.")]
        [SerializeField] private AnimationClip _handshake;
        [Tooltip("Estado de la visita: escenario, jugador y lo que se sabe de él.")]
        [SerializeField] private SessionState _session;
        [Tooltip("Reloj de la simulación, para saludar según la hora.")]
        [SerializeField] private SimulationClock _clock;

        [Header("Zona")]
        [Tooltip("Extremos de la zona por la que pasea, en el mundo.")]
        [SerializeField] private Vector3 _zoneStart;
        [SerializeField] private Vector3 _zoneEnd;
        [Tooltip("Radio de la zona alrededor del segmento entre sus extremos (m).")]
        [SerializeField] private float _zoneRadius = 3f;
        [Tooltip("Puerta por la que sale al empezar (opcional).")]
        [SerializeField] private Transform _entrance;
        [Tooltip("Silla en la que está sentado (opcional): entonces no pasea y su mesa queda reservada.")]
        [SerializeField] private Transform _chair;
        [Tooltip("Silla de otra mesa a la que se cambia mientras la de la suya no está (opcional).")]
        [SerializeField] private Transform _spareChair;
        [Tooltip("Vaso de su mesa que coge al beber sentado (opcional). El grupo en el que está (vaso, botella...) " +
                 "se muda con él de mesa.")]
        [SerializeField] private Transform _drink;
        [Tooltip("Velocidad al pasear (m/s).")]
        [SerializeField] private float _walkSpeed = 0.8f;
        [Tooltip("Segundos mínimos y máximos que se para entre paseo y paseo.")]
        [SerializeField] private Vector2 _pauseSeconds = new Vector2(5f, 14f);
        [Tooltip("Segundos mínimos y máximos entre dos gestos sentado (beber, reírse).")]
        [SerializeField] private Vector2 _seatedGestureSeconds = new Vector2(12f, 30f);
        [Tooltip("Probabilidad de que el gesto sentado sea beber en lugar de reírse.")]
        [SerializeField, Range(0f, 1f)] private float _drinkChance = 0.6f;

        [Header("Saludo")]
        [Tooltip("Segundos antes de volver a saludar al mismo jugador.")]
        [SerializeField] private float _greetCooldown = 45f;
        [Tooltip("Segundos durante los que, si vuelve a verle, solo le dice algo breve.")]
        [SerializeField] private float _seenRecentlySeconds = 180f;
        [Tooltip("Distancia a la que llama al jugador si pasa cerca sin acercarse (m). Solo si tiene frases para ello.")]
        [SerializeField] private float _callOutDistance = 7f;
        [Tooltip("Segundos mínimos entre dos llamadas.")]
        [SerializeField] private float _callOutCooldown = 60f;
        [Tooltip("Altura sobre los pies del jugador a la que le mira (m).")]
        [SerializeField] private float _lookHeight = 1.55f;

        private Animator _animator;
        private NavMeshAgent _agent;
        private Transform _head;
        private MainCharacterSpeech _speech;
        private MainCharacterSeat _seating;
        private MainCharacterDrink _drinking;
        private float _pauseUntil = -1f;
        private float _idle;
        private float _greetedAt = float.NegativeInfinity;
        private float _calledOutAt = float.NegativeInfinity;
        private float _lookWeight;
        private float _nextSeatedGesture;
        private bool _busy;

        public static IReadOnlyList<MainCharacter> All => Characters;

        public NpcProfile Profile => _profile;

        public bool IsSeated => _chair != null;

        /// <summary>Hasta dónde se fija ahora en el jugador.</summary>
        public Engagement Engagement { get; private set; }

        /// <summary>Lo que está diciendo, o null si no dice nada.</summary>
        public string CurrentLine => _speech?.CurrentLine;

        /// <summary>Encima de la cabeza, para el bocadillo.</summary>
        public Vector3 HeadTop => (_head != null ? _head.position : transform.position + Vector3.up * 1.6f) + Vector3.up * 0.35f;

        /// <summary>Mientras da la mano no atiende a nada más.</summary>
        public bool IsBusy => _busy;

        public bool InConversation { get; private set; }

        /// <summary>Hueso de la cabeza (o la raíz si el modelo no lo tiene), para encuadrarle al conversar.</summary>
        public Transform Head => _head != null ? _head : transform;

        internal (Vector3 Start, Vector3 End, float Radius) Zone => (_zoneStart, _zoneEnd, _zoneRadius);

        internal Transform Entrance => _entrance;

        internal Transform Chair => _chair;

        internal Transform Seat => _seating?.Current;

        internal Transform Drink => _drink;

        private Transform Player => _session.Player;

        private void Awake()
        {
            // Registrado mientras exista, no mientras esté activo: al dar la mano se desactiva y debe seguir en la
            // interfaz con lo que dice.
            Characters.Add(this);
            if (!MissingReferences.Check(this, (_profile, "el perfil"), (_session, "el estado de la sesión"),
                    (_clock, "el reloj de la simulación")))
            {
                enabled = false;
                return;
            }

            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _head = _animator.GetBoneTransform(HumanBodyBones.Head);
            _speech = new MainCharacterSpeech(_profile, _session, _clock, _seenRecentlySeconds);
            if (_drink != null)
            {
                _drinking = new MainCharacterDrink(_animator, _drink, transform);
            }

            if (IsSeated)
            {
                _seating = new MainCharacterSeat(transform, _animator.GetBoneTransform(HumanBodyBones.Hips), _chair, _spareChair, _drinking);
            }
        }

        // Al entrar en juego sin recargar el dominio no deben quedar restos de la partida anterior.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearRegistry() => Characters.Clear();

        private void OnDestroy()
        {
            Characters.Remove(this);
            _seating?.Release();
        }

        private void Start()
        {
            if (IsSeated)
            {
                _agent.enabled = false;
                _seating.SitDown();
                _animator.SetBool(SeatedParameter, true);
                _nextSeatedGesture = Time.time + Random.Range(_seatedGestureSeconds.x, _seatedGestureSeconds.y);
                return;
            }

            _agent.speed = _walkSpeed;
            _idle = Random.Range(0, IdleVariants);
            if (_entrance != null)
            {
                _agent.Warp(_entrance.position);
                transform.rotation = _entrance.rotation;
                PickDestination();
            }
        }

        private void Update()
        {
            UpdateEngagement();
            if (IsSeated)
            {
                UpdateSeated();
            }
            else
            {
                UpdateWalking();
            }
        }

        /// <summary>
        /// Empieza a hablar con él si el jugador está al lado y de cara: si es de dar la mano, se la da (el jugador queda
        /// quieto mientras, con <paramref name="playerControls"/> en pausa); si no, asiente o se ríe. Al terminar el
        /// gesto llama a <paramref name="ready"/>.
        /// </summary>
        public bool Interact(Animator player, IReadOnlyList<Behaviour> playerControls, System.Action ready = null)
        {
            if (_busy || Engagement != Engagement.InReach)
            {
                return false;
            }

            if (_shakesHands && !IsSeated && _handshake != null && player != null)
            {
                StartCoroutine(ShakeHands(player, playerControls, ready));
            }
            else
            {
                // De pie asiente, a veces con énfasis.
                _animator.SetFloat(NodVariantParameter, Random.Range(0, 2));
                _animator.SetTrigger(IsSeated ? LaughTrigger : NodTrigger);
                ready?.Invoke();
            }

            return true;
        }

        private IEnumerator ShakeHands(Animator player, IReadOnlyList<Behaviour> playerControls, System.Action ready)
        {
            _busy = true;
            var paused = new List<Behaviour>(playerControls ?? new Behaviour[0]) { this };
            yield return Handshake.Play(player, _animator, _handshake, paused);
            _busy = false;
            ready?.Invoke();
        }

        /// <summary>Empieza a conversar: atiende al jugador hasta <see cref="EndConversation"/> sin saludarle ni llamarle.</summary>
        public void BeginConversation() => InConversation = true;

        /// <summary>Termina la conversación y se despide.</summary>
        public void EndConversation()
        {
            if (!InConversation)
            {
                return;
            }

            InConversation = false;
            SetTalking(false);
            _greetedAt = Time.time;
            _speech.SayGoodbye();
        }

        /// <summary>Frase con la que abre la conversación, distinta de las últimas que ha dicho. Null si no tiene.</summary>
        public string OpeningLine() => _speech.OpeningLine();

        public void Say(LineMoment moment) => _speech.Say(moment);

        /// <summary>Gesticula con los brazos y la cabeza como quien habla (de pie con una de dos variantes).</summary>
        public void SetTalking(bool talking)
        {
            if (talking && !_animator.GetBool(TalkingParameter))
            {
                _animator.SetFloat(TalkVariantParameter, Random.Range(0, 2));
            }

            _animator.SetBool(TalkingParameter, talking);
        }

        /// <summary>Asiente mientras escucha (solo de pie).</summary>
        public void Nod()
        {
            if (!IsSeated)
            {
                _animator.SetFloat(NodVariantParameter, 0f);
                _animator.SetTrigger(NodTrigger);
            }
        }

        /// <summary>Señala a lo lejos, al indicar un camino o un sitio (solo de pie); deja de gesticular para hacerlo.</summary>
        public void Point()
        {
            if (!IsSeated)
            {
                SetTalking(false);
                _animator.SetTrigger(PointTrigger);
            }
        }

        private void UpdateEngagement()
        {
            if (Player == null)
            {
                Engagement = Engagement.None;
                return;
            }

            Vector3 toMe = (transform.position - Player.position).Flat();
            Engagement = MainCharacterRules.EngagementFor(toMe.magnitude, Vector3.Angle(Player.forward.Flat(), toMe));
            if (InConversation)
            {
                return;
            }

            if (Engagement >= Engagement.Greeted && Time.time - _greetedAt > _greetCooldown)
            {
                _greetedAt = Time.time;
                _animator.SetTrigger(_greeting == Greeting.Bow ? BowTrigger : _greeting == Greeting.Beckon ? BeckonTrigger : WaveTrigger);
                _speech.Greet();
            }
            else if (Engagement == Engagement.Noticed && toMe.magnitude <= _callOutDistance && CanCallOut())
            {
                _calledOutAt = Time.time;
                _animator.SetTrigger(BeckonTrigger);
                _speech.Say(LineMoment.CallOut);
            }
        }

        private bool CanCallOut() =>
            _speech.HasLinesFor(LineMoment.CallOut) &&
            Time.time - _calledOutAt > _callOutCooldown && Time.time - _greetedAt > _greetCooldown;

        private void UpdateWalking()
        {
            bool attending = Player != null && (Engagement >= Engagement.Greeted || InConversation);
            _agent.isStopped = attending;
            _agent.updateRotation = !attending;
            if (attending)
            {
                Vector3 look = (Player.position - transform.position).Flat();
                if (look.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), TurnDegreesPerSecond * Time.deltaTime);
                }
            }
            else if (!_agent.pathPending && _agent.remainingDistance <= ArriveDistance)
            {
                if (_pauseUntil < 0f)
                {
                    _pauseUntil = Time.time + Random.Range(_pauseSeconds.x, _pauseSeconds.y);
                    _idle = Random.Range(0, IdleVariants);
                }
                else if (Time.time >= _pauseUntil)
                {
                    _pauseUntil = -1f;
                    PickDestination();
                }
            }

            float speed = attending ? 0f : _agent.velocity.magnitude / _walkSpeed;
            _animator.SetFloat(SpeedParameter, speed, 0.15f, Time.deltaTime);
            _animator.SetFloat(IdleParameter, _idle, 0.5f, Time.deltaTime);
        }

        private void UpdateSeated()
        {
            // El escenario puede quitar su mesa (o devolverla) con la visita ya en marcha: se cambia en cuanto no
            // está bebiendo.
            if (!(_drinking?.InUse ?? false) && !_busy)
            {
                _seating.FollowTable();
            }

            if (Engagement < Engagement.Greeted && !InConversation && Time.time >= _nextSeatedGesture)
            {
                _animator.SetTrigger(Random.value < _drinkChance ? DrinkTrigger : LaughTrigger);
                _nextSeatedGesture = Time.time + Random.Range(_seatedGestureSeconds.x, _seatedGestureSeconds.y);
            }

            AnimatorStateInfo upper = _animator.GetCurrentAnimatorStateInfo(UpperBodyLayer);
            bool entering = _animator.IsInTransition(UpperBodyLayer) && _animator.GetNextAnimatorStateInfo(UpperBodyLayer).shortNameHash == DrinkState;
            _drinking?.Update(upper.shortNameHash == DrinkState ? upper.normalizedTime : entering ? 0f : -1f);
        }

        /// <summary>
        /// Elige un punto de la zona al que se pueda llegar sin salir de ella (así Fray Paulino no baja la
        /// escalinata ni Inés se levanta: los caminos que salen de la zona se descartan).
        /// </summary>
        private void PickDestination()
        {
            var path = new NavMeshPath();
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 point = MainCharacterRules.PointInZone(_zoneStart, _zoneEnd, _zoneRadius, Random.value, Random.insideUnitCircle);
                if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 1f, NavMesh.AllAreas) ||
                    !MainCharacterRules.InZone(hit.position, _zoneStart, _zoneEnd, _zoneRadius) ||
                    !NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                bool inside = true;
                foreach (Vector3 corner in path.corners)
                {
                    inside &= MainCharacterRules.InZone(corner, _zoneStart, _zoneEnd, _zoneRadius + ZoneSlack);
                }

                if (inside)
                {
                    _agent.SetPath(path);
                    return;
                }
            }
        }

        private void LateUpdate()
        {
            if (!IsSeated)
            {
                return;
            }

            _seating.Lift();
            _drinking?.Place(_animator.IsInTransition(UpperBodyLayer));
        }

        private void OnAnimatorIK(int layerIndex)
        {
            // La mirada se pide en las dos capas: la de brazos y cabeza la taparía en la cabeza.
            if (layerIndex == 0)
            {
                bool looking = Player != null && (Engagement >= Engagement.Greeted || InConversation);
                _lookWeight = Mathf.MoveTowards(_lookWeight, looking ? 1f : 0f, 2f * Time.deltaTime);
            }

            _animator.SetLookAtWeight(_lookWeight, IsSeated ? 0f : 0.25f, 0.8f, 1f, 0.5f);
            if (Player != null)
            {
                _animator.SetLookAtPosition(Player.position + Vector3.up * _lookHeight);
            }

            if (layerIndex == UpperBodyLayer)
            {
                _drinking?.ApplyIK();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            for (int i = 0; i <= 24; i++)
            {
                float angle = i * Mathf.PI * 2f / 24f;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _zoneRadius;
                Gizmos.DrawLine(_zoneStart + offset, _zoneEnd + offset);
            }
        }
    }
}
