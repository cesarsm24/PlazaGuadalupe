using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Turista de paso: anda hasta una parada, hace algo allí un rato y avisa al terminar para que
    /// <see cref="TouristSpawner"/> le dé otra parada o lo mande a una salida.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public sealed class Tourist : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            Arriving,
            Settling,
            SittingDown,
            Staying,
            StandingUp,
            Returning,
            Waiting,
            Leaving,
        }

        // Speed va como fracción de la velocidad de andar; WalkScale ajusta la animación para que no resbalen los pies.
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int ActivityParameter = Animator.StringToHash("Activity");
        private static readonly int WalkScaleParameter = Animator.StringToHash("WalkScale");
        private static readonly int TalkVariantParameter = Animator.StringToHash("TalkVariant");
        private static readonly int SittingState = Animator.StringToHash("Sitting");
        private static readonly int SittingTalkState = Animator.StringToHash("SittingTalk");

        private const float TurnSpeed = 180f; // grados/s

        // Pasos cortos fuera de la NavMesh, hasta la silla y de vuelta: más lentos que su paso normal.
        private const float SettleWalk = 0.6f;
        private const float FaceTheWayFrom = 0.2f;

        // La animación de señalar no se repite; al acabar mira alrededor en vez de quedarse con el brazo extendido.
        private const float PointSeconds = 4.5f;

        private const float ChairMoveSeconds = 0.6f;
        private const float FacingTolerance = 4f;
        private const float SitSeconds = 2.2f;

        // Tramo de sentarse y levantarse en el que el cuerpo entra en el asiento o sale de él.
        private static readonly Vector2 SlideWindow = new Vector2(0.2f, 0.7f);

        private const float SpeedSmoothing = 0.15f;

        // Solo quien usa una silla la separa o la arrima.
        private static readonly Dictionary<Transform, Tourist> ChairUser = new Dictionary<Transform, Tourist>();

        [Tooltip("Es un niño: no visita la plaza solo.")]
        [SerializeField] private bool _child;
        [Tooltip("Época del año para la que va vestido.")]
        [SerializeField] private TouristSeason _season = TouristSeason.AllYear;
        [Tooltip("Velocidad (m/s) a la que avanza la animación de andar con escala 1.")]
        [SerializeField, Min(0f)] private float _naturalWalkSpeed = 1.2f;

        [Tooltip("Materiales del cuerpo con la ropa de distintos colores; al aparecer se elige uno.")]
        [SerializeField] private Material[] _clothes = new Material[0];
        [Tooltip("Hueco del material del cuerpo en el SkinnedMeshRenderer.")]
        [SerializeField, Min(0)] private int _clothesSlot;

        private TouristPhone _phone;
        private FootGrounding _feet;
        private int _waypoint;
        private bool _stepping;
        private float _chairOut;
        private float _chairTarget;

        private NavMeshAgent _agent;
        private Animator _animator;
        private Transform _hips;
        private Phase _phase;
        private TouristVisit _visit;
        private Vector3 _exit;
        private Vector3 _basePosition;
        private float _phaseStart;
        private float _timer;
        private float _seatLift;
        private float _animatedSpeed;
        private float _speedVelocity;

        /// <summary>Ha terminado lo que hacía en su sitio (y, si estaba sentado, ya se ha levantado).</summary>
        public event Action<Tourist> StopFinished;

        /// <summary>Ha llegado a la salida y ya se puede retirar.</summary>
        public event Action<Tourist> Left;

        public bool IsChild => _child;
        public TouristSeason Season => _season;

        /// <summary>Variantes de ropa que tiene (al menos la original).</summary>
        public int ClothesCount => Mathf.Max(1, _clothes.Length);

        public int ClothesIndex { get; private set; }

        /// <summary>Lo que está haciendo en su sitio, o <see cref="TouristActivity.None"/> si no está en él.</summary>
        public TouristActivity CurrentActivity => _phase == Phase.Staying ? _visit.Activity : TouristActivity.None;

        /// <summary>True mientras anda por la NavMesh hacia su sitio o hacia la salida.</summary>
        public bool IsWalking => _phase is Phase.Arriving or Phase.Leaving;

        public bool IsLeaving => _phase == Phase.Leaving;

        /// <summary>True mientras está sentado o sentándose.</summary>
        public bool IsSeated => _visit.Seated && _phase is Phase.SittingDown or Phase.Staying;

        public bool IsStandingUp => _visit.Seated && _phase == Phase.StandingUp;
        public Transform Chair => _visit.Chair;
        public float WalkSpeed => _agent.speed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _phone = new TouristPhone(_animator);
            _feet = new FootGrounding(_animator, transform);
        }

        // Sin recarga de dominio quedarían las sillas de la partida anterior.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearChairs() => ChairUser.Clear();

        /// <summary>Lo deja listo para una nueva visita en <paramref name="position"/> (al crearlo o al reutilizarlo).</summary>
        /// <param name="phone">Prefab del móvil que saca para hacer fotos; solo se crea la primera vez.</param>
        /// <param name="clothes">Variante de ropa; negativa, una al azar.</param>
        public void Prepare(Vector3 position, float scale, float walkSpeed, int avoidancePriority, GameObject phone = null, int clothes = -1)
        {
            _phone.Attach(phone, transform);
            gameObject.SetActive(true);
            transform.localScale = Vector3.one * scale;
            _agent.enabled = true;
            _agent.Warp(position);
            _agent.speed = walkSpeed;
            _agent.avoidancePriority = avoidancePriority;
            Wear(clothes);
            _animator.Rebind();
            _animator.SetFloat(WalkScaleParameter, TouristCrowd.WalkAnimationScale(walkSpeed, _naturalWalkSpeed, scale));
            _phase = Phase.Idle;
            _seatLift = 0f;
            _animatedSpeed = 0f;
            _phone.Reset();
            _feet.Reset();
            _stepping = false;
        }

        private void Wear(int clothes)
        {
            ClothesIndex = clothes >= 0 ? clothes % ClothesCount : UnityEngine.Random.Range(0, ClothesCount);
            if (_clothes.Length == 0)
            {
                return;
            }

            var body = GetComponentInChildren<SkinnedMeshRenderer>();
            Material[] materials = body.sharedMaterials;
            materials[_clothesSlot] = _clothes[ClothesIndex];
            body.sharedMaterials = materials;
        }

        /// <summary>Empieza una parada.</summary>
        /// <param name="alreadyThere">Empieza ya en su sitio (al poblar la plaza al empezar la visita).</param>
        public void Visit(TouristVisit visit, bool alreadyThere = false)
        {
            ReleaseChair();
            _visit = visit;
            _waypoint = 0;
            if (visit.Seated)
            {
                ChairUser[visit.Chair] = this;
            }

            _chairOut = 0f;
            _chairTarget = 0f;
            if (!alreadyThere)
            {
                EnableAgent();
                SetPhase(Phase.Arriving);
                _agent.SetDestination(visit.Approach);
                return;
            }

            _agent.Warp(visit.Approach);
            if (visit.Seated)
            {
                _chairOut = _chairTarget = 1f;
                MoveChair();
                DisableAgent(visit.Seat);
                transform.rotation = visit.Rotation;
                Stay();
                _animator.Play(visit.Activity == TouristActivity.SitAndTalk ? SittingTalkState : SittingState, 0, UnityEngine.Random.value);
                return;
            }

            transform.rotation = visit.Rotation;
            Stay();
        }

        /// <summary>Se marcha andando hacia <paramref name="exit"/>; al llegar lanza <see cref="Left"/>.</summary>
        public void Leave(Vector3 exit)
        {
            _exit = exit;
            _animator.SetInteger(ActivityParameter, (int)TouristActivity.None);
            EnableAgent();
            SetPhase(Phase.Leaving);
            _agent.SetDestination(exit);
        }

        /// <summary>Lo retira de la plaza para reutilizarlo después; si deja una silla separada, la arrima.</summary>
        public void Retire()
        {
            if (_visit.Chair != null && _chairOut > 0f)
            {
                _chairOut = _chairTarget = 0f;
                MoveChair();
            }

            ReleaseChair();
            _phase = Phase.Idle;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            float elapsed = Time.time - _phaseStart;
            switch (_phase)
            {
                case Phase.Arriving when TouristCrowd.HasArrived(_agent.pathPending, _agent.remainingDistance, _agent.stoppingDistance):
                    _agent.ResetPath();
                    if (_visit.Seated)
                    {
                        DisableAgent(transform.position);
                    }

                    SetPhase(Phase.Settling);
                    break;
                case Phase.Settling when _visit.Seated && _waypoint == 0:
                    // Primero al lado de la silla, separándola de la mesa mientras llega; luego, delante del asiento.
                    _chairTarget = 1f;
                    if (StepTowards(_visit.Side, _visit.Rotation))
                    {
                        _waypoint = 1;
                    }

                    break;
                case Phase.Settling when StepTowards(_visit.Position, _visit.Rotation):
                    if (_visit.Seated)
                    {
                        _animator.SetInteger(ActivityParameter, (int)_visit.Activity);
                        SetPhase(Phase.SittingDown);
                    }
                    else
                    {
                        Stay();
                    }

                    break;
                case Phase.SittingDown:
                    _basePosition = Vector3.Lerp(_visit.Position, _visit.Seat, TouristCrowd.SlideProgress(elapsed / SitSeconds, SlideWindow));
                    if (elapsed >= SitSeconds)
                    {
                        Stay();
                    }

                    break;
                case Phase.Staying when _visit.Activity == TouristActivity.Point && elapsed >= PointSeconds &&
                                        _animator.GetInteger(ActivityParameter) == (int)TouristActivity.Point:
                    _animator.SetInteger(ActivityParameter, (int)TouristActivity.LookAround);
                    break;
                // Si retiran la silla (en Navidad, las mesas junto al árbol), no se queda sentado en el aire.
                case Phase.Staying when Time.time >= _timer || (_visit.Seated && !_visit.Chair.gameObject.activeInHierarchy):
                    _animator.SetInteger(ActivityParameter, (int)TouristActivity.None);
                    SetPhase(Phase.StandingUp);
                    break;
                case Phase.StandingUp:
                    if (_visit.Seated)
                    {
                        _basePosition = Vector3.Lerp(_visit.Seat, _visit.Position, TouristCrowd.SlideProgress(elapsed / SitSeconds, SlideWindow));
                    }

                    if (!_visit.Seated || elapsed >= SitSeconds)
                    {
                        _waypoint = 1;
                        SetPhase(Phase.Returning);
                    }

                    break;
                case Phase.Returning when _visit.Seated && _waypoint == 1:
                    if (StepTowards(_visit.Side, transform.rotation))
                    {
                        // Ya fuera de la mesa, vuelve a arrimar la silla.
                        _chairTarget = 0f;
                        _waypoint = 2;
                    }

                    break;
                case Phase.Returning when StepTowards(_visit.Approach, transform.rotation):
                    EnableAgent();
                    SetPhase(Phase.Waiting);
                    StopFinished?.Invoke(this);
                    break;
                case Phase.Leaving when TouristCrowd.HasArrived(_agent.pathPending, _agent.remainingDistance, _agent.stoppingDistance):
                    SetPhase(Phase.Idle);
                    Left?.Invoke(this);
                    break;
            }

            _phone.Update(CurrentActivity == TouristActivity.Photo);
            if (_visit.Chair != null && !Mathf.Approximately(_chairOut, _chairTarget))
            {
                _chairOut = Mathf.MoveTowards(_chairOut, _chairTarget, Time.deltaTime / ChairMoveSeconds);
                MoveChair();
            }

            float walking = _agent.enabled && _agent.speed > 0f
                ? _agent.velocity.magnitude / _agent.speed
                : _stepping ? SettleWalk : 0f;
            _animatedSpeed = Mathf.SmoothDamp(_animatedSpeed, walking, ref _speedVelocity, SpeedSmoothing);
            _animator.SetFloat(SpeedParameter, _animatedSpeed);
        }

        private void LateUpdate()
        {
            if (_agent.enabled)
            {
                return;
            }

            // Sentado, eleva el cuerpo lo justo para que la cadera quede sobre el asiento.
            float target = 0f;
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (_phase == Phase.Staying && _visit.Seated && (state.shortNameHash == SittingState || state.shortNameHash == SittingTalkState))
            {
                target = Seats.Lift(_hips.position.y - _seatLift, _visit.SeatHeight + Seats.HipClearance, Seats.MaxLift);
            }

            _seatLift = Mathf.MoveTowards(_seatLift, target, 0.3f * Time.deltaTime);
            transform.position = _basePosition + Vector3.up * _seatLift;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            _feet.ApplyIK(_agent.enabled && _phase != Phase.StandingUp);
            _phone.ApplyIK(_visit.Focus, transform.forward);
        }

        private void Stay()
        {
            SetPhase(Phase.Staying);
            _timer = Time.time + _visit.Stay;
            // Dos formas de gesticular al charlar, para que un corrillo no se mueva al unísono.
            _animator.SetFloat(TalkVariantParameter, UnityEngine.Random.Range(0, 2));
            _animator.SetInteger(ActivityParameter, (int)_visit.Activity);
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            _phaseStart = Time.time;
        }

        private void EnableAgent()
        {
            if (_agent.enabled)
            {
                return;
            }

            _seatLift = 0f;
            _agent.enabled = true;
            _agent.Warp(_basePosition);
        }

        private void DisableAgent(Vector3 position)
        {
            _agent.enabled = false;
            _basePosition = position;
            transform.position = position;
        }

        /// <summary>
        /// Da un paso hacia la posición y la orientación indicadas; true cuando ya ha llegado. Con el agente activo
        /// solo gira: la posición es la suya en la NavMesh. Sin él anda (con la animación de andar) mirando hacia
        /// donde va y, al llegar, se vuelve a la orientación pedida.
        /// </summary>
        private bool StepTowards(Vector3 position, Quaternion rotation)
        {
            bool placed = true;
            _stepping = false;
            if (!_agent.enabled)
            {
                Vector3 toTarget = position - _basePosition;
                float speed = _agent.speed * SettleWalk;
                _basePosition = Vector3.MoveTowards(_basePosition, position, speed * Time.deltaTime);
                placed = toTarget.sqrMagnitude < 1e-4f;
                _stepping = !placed;
                Vector3 heading = toTarget.Flat();
                if (heading.magnitude > FaceTheWayFrom)
                {
                    rotation = Quaternion.LookRotation(heading);
                }
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, TurnSpeed * Time.deltaTime);
            return placed && Quaternion.Angle(transform.rotation, rotation) < FacingTolerance;
        }

        /// <summary>
        /// Coloca la silla entre su sitio junto a la mesa y separada de ella, según lo separada que está; solo si la
        /// está usando él.
        /// </summary>
        private void MoveChair()
        {
            if (ChairUser.TryGetValue(_visit.Chair, out Tourist user) && user == this)
            {
                _visit.Chair.position = Vector3.Lerp(_visit.ChairHome, _visit.ChairPulled, Mathf.SmoothStep(0f, 1f, _chairOut));
            }
        }

        /// <summary>Deja de usar la silla de la parada anterior.</summary>
        private void ReleaseChair()
        {
            if (_visit.Chair != null && ChairUser.TryGetValue(_visit.Chair, out Tourist user) && user == this)
            {
                ChairUser.Remove(_visit.Chair);
            }
        }
    }
}
