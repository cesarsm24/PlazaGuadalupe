using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Vaso que un personaje sentado coge de su mesa al beber. Está en la mesa salvo durante el gesto de beber: la mano
    /// va a él, lo coge, se lleva el borde a los labios y lo vuelve a dejar en el mismo sitio y derecho. Al cogerlo y
    /// al dejarlo la mano está sobre él y el vaso pasa poco a poco de la mesa a la mano, así que no salta.
    /// </summary>
    internal sealed class MainCharacterDrink
    {
        /// <summary>Mano con la que bebe.</summary>
        private const HumanBodyBones Hand = HumanBodyBones.LeftHand;

        /// <summary>Labios respecto a la cabeza (hacia delante y hacia arriba), adonde llega el borde del vaso.</summary>
        private const float MouthForward = 0.12f;
        private const float MouthUp = 0.05f;

        /// <summary>Lo más que inclina el vaso al beber, para que sea un trago y no se lo vacíe de golpe.</summary>
        private const float MaxTilt = 70f;

        private readonly Animator _animator;
        private readonly Transform _glass;
        private readonly Transform _hand;
        private readonly Transform _head;
        private readonly Transform _owner;
        private readonly Vector3 _center;
        private readonly float _height;
        private Pose _rest;
        private Vector3 _gripPosition;
        private Quaternion _gripRotation;
        private bool _hasGrip;
        private float _time = -1f;
        private float _reach;
        private float _sip;

        public MainCharacterDrink(Animator animator, Transform glass, Transform owner)
        {
            _animator = animator;
            _glass = glass;
            _owner = owner;
            _hand = animator.GetBoneTransform(Hand);
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
            Renderer renderer = glass.GetComponentInChildren<Renderer>();
            _center = renderer != null ? glass.InverseTransformPoint(renderer.bounds.center) : Vector3.zero;
            _height = renderer != null ? renderer.bounds.size.y : 0f;
            RememberRest();
        }

        /// <summary>El vaso y lo que le acompaña en la mesa (la botella).</summary>
        public Transform Group => _glass.parent;

        /// <summary>True mientras lo está usando: no se puede cambiar de mesa.</summary>
        public bool InUse => _time >= 0f;

        /// <summary>Toma como sitio del vaso en la mesa donde está ahora (al empezar o tras mudarse de mesa).</summary>
        public void RememberRest() => _rest = new Pose(_glass.position, _glass.rotation);

        /// <summary>
        /// Avance del gesto de beber (0-1, o negativo si no está bebiendo): cuánto va la mano hacia el vaso y cuánto se
        /// lo lleva a la boca.
        /// </summary>
        public void Update(float gestureTime)
        {
            _time = gestureTime;
            _reach = _time >= 0f ? MainCharacterRules.DrinkReach(_time) : 0f;
            _sip = _time >= 0f ? MainCharacterRules.DrinkSip(_time) : 0f;
            if (_time < 0f)
            {
                _hasGrip = false;
            }
        }

        /// <summary>Coloca el vaso: en la mesa, o en la mano mientras lo sujeta.</summary>
        /// <param name="inTransition">La capa del gesto está entrando o saliendo: aún no se mide el agarre.</param>
        public void Place(bool inTransition)
        {
            if (_hand == null)
            {
                return;
            }

            if (_time >= 0f && !_hasGrip && !inTransition)
            {
                // El agarre se mide al empezar el gesto, con la mano ya cerrada como si sujetara un vaso: el vaso va
                // entre el pulgar y los dedos, con el fondo hacia el meñique y la boca hacia el índice, como se coge un
                // vaso; así, al llevárselo a la boca, se inclina hacia ella.
                Transform thumb = _animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
                Transform finger = _animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
                Transform index = _animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
                Transform little = _animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
                Vector3 grip = thumb != null && finger != null ? (thumb.position + finger.position) * 0.5f : _hand.position;
                Vector3 up = index != null && little != null ? (index.position - little.position).normalized : Vector3.up;
                Vector3 forward = Vector3.ProjectOnPlane(grip - _hand.position, up);
                Quaternion held = forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(forward, up) : _rest.rotation;
                _gripPosition = _hand.InverseTransformPoint(grip);
                _gripRotation = Quaternion.Inverse(_hand.rotation) * held;
                _hasGrip = true;
            }

            Vector3 restCenter = _rest.position + _rest.rotation * _center;
            if (!_hasGrip || !MainCharacterRules.HoldsDrink(_time))
            {
                _glass.SetPositionAndRotation(_rest.position, _rest.rotation);
                return;
            }

            Vector3 center = Vector3.Lerp(_hand.TransformPoint(_gripPosition), restCenter, _reach);
            Quaternion rotation = Quaternion.Slerp(HeldRotation(_hand.rotation), _rest.rotation, _reach);
            _glass.SetPositionAndRotation(center - rotation * _center, rotation);
        }

        /// <summary>
        /// Al coger y dejar el vaso, lleva la mano a él (el centro del vaso en la mano sobre el de la mesa); al dar el
        /// trago, lleva el borde del vaso a los labios. La IK solo mueve la mano, no la gira, así que dónde queda el
        /// vaso respecto a la mano depende solo del giro que le da la animación.
        /// </summary>
        public void ApplyIK()
        {
            if (!_hasGrip)
            {
                return;
            }

            Vector3 toCenter = _hand.TransformPoint(_gripPosition) - _hand.position;
            Vector3 target = Vector3.zero;
            float weight = 0f;
            if (_reach > 0f)
            {
                target = _rest.position + _rest.rotation * _center - toCenter;
                weight = _reach;
            }
            else if (_sip > 0f)
            {
                Vector3 toRim = toCenter + HeldRotation(_hand.rotation) * Vector3.up * (_height * 0.5f);
                target = _head.position + _owner.forward * MouthForward + Vector3.up * MouthUp - toRim;
                weight = _sip;
            }

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, target);
        }

        /// <summary>Giro del vaso en la mano según el de la mano, sin pasar de <see cref="MaxTilt"/>.</summary>
        private Quaternion HeldRotation(Quaternion hand)
        {
            Quaternion held = hand * _gripRotation;
            Vector3 up = held * Vector3.up;
            float tilt = Vector3.Angle(up, Vector3.up);
            return tilt <= MaxTilt ? held : Quaternion.FromToRotation(up, Vector3.Slerp(Vector3.up, up, MaxTilt / tilt)) * held;
        }
    }
}
