using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Móvil del turista para hacer fotos. Al empezar a fotografiar lo saca y lo mira a la altura del pecho; cada pocos
    /// segundos lo levanta delante de los ojos, en horizontal, hacia lo que fotografía; al terminar lo guarda. Las
    /// manos lo siguen por IK y la mirada pasa de la pantalla a lo fotografiado.
    /// </summary>
    internal sealed class TouristPhone
    {
        // Segundos mínimos y máximos con el móvil bajado y levantado.
        private static readonly Vector2 Lowered = new Vector2(2f, 4.5f);
        private static readonly Vector2 Raised = new Vector2(1.5f, 3f);

        private const float MoveSeconds = 0.5f;

        // Respecto a los ojos (adelante, arriba), en metros.
        private static readonly Vector2 RaisedOffset = new Vector2(0.33f, -0.03f);
        private static readonly Vector2 LoweredOffset = new Vector2(0.27f, -0.4f);

        // Muñecas en el sistema del móvil (+Z hacia lo que fotografía): a los lados en vertical y bajo los extremos en
        // horizontal, para que los dedos lo rodeen.
        private static readonly Vector3 PortraitRight = new Vector3(0.05f, -0.03f, -0.02f);
        private static readonly Vector3 PortraitLeft = new Vector3(-0.05f, -0.03f, -0.02f);
        private static readonly Vector3 LandscapeRight = new Vector3(-0.06f, -0.065f, -0.03f);
        private static readonly Vector3 LandscapeLeft = new Vector3(-0.06f, 0.065f, -0.03f);

        // El modelo tiene la pantalla hacia +Z.
        private static readonly Quaternion ModelRotation = Quaternion.Euler(0f, 180f, 0f);

        private readonly Animator _animator;
        private readonly Transform _head;
        private Transform _phone;
        private float _handsWeight;
        private float _raise;
        private bool _raised;
        private float _toggleAt;

        public TouristPhone(Animator animator)
        {
            _animator = animator;
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        /// <summary>Crea el móvil la primera vez, guardado hasta que lo saque.</summary>
        public void Attach(GameObject prefab, Transform owner)
        {
            if (_phone == null && prefab != null)
            {
                _phone = Object.Instantiate(prefab, owner).transform;
                _phone.gameObject.SetActive(false);
            }
        }

        /// <summary>Con las manos libres, para una visita nueva.</summary>
        public void Reset()
        {
            _handsWeight = 0f;
            _raise = 0f;
        }

        /// <summary>
        /// Cuánto tiene el móvil en las manos y cuánto lo tiene levantado: lo saca al empezar a hacer fotos, lo sube y
        /// lo baja por turnos y lo guarda al terminar.
        /// </summary>
        public void Update(bool photographing)
        {
            float step = Time.deltaTime / MoveSeconds;
            _handsWeight = Mathf.MoveTowards(_handsWeight, photographing ? 1f : 0f, step);
            if (photographing && Time.time >= _toggleAt)
            {
                _raised = !_raised;
                Vector2 range = _raised ? Raised : Lowered;
                _toggleAt = Time.time + Random.Range(range.x, range.y);
            }

            _raise = Mathf.MoveTowards(_raise, photographing && _raised ? 1f : 0f, step);
            if (_phone != null)
            {
                _phone.gameObject.SetActive(_handsWeight > 0.3f);
            }
        }

        /// <summary>Coloca el móvil y lleva a él las manos y la mirada; sin móvil en las manos, no mira a nada.</summary>
        /// <param name="focus">Lo que fotografía.</param>
        /// <param name="fallbackForward">Hacia dónde lo sostiene si lo fotografiado queda justo encima o debajo.</param>
        public void ApplyIK(Vector3 focus, Vector3 fallbackForward)
        {
            if (_handsWeight <= 0f || _phone == null)
            {
                _animator.SetLookAtWeight(0f);
                return;
            }

            float raise = Mathf.SmoothStep(0f, 1f, _raise);
            Vector3 eyes = _head.position;
            Vector3 forward = (focus - eyes).Flat();
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : fallbackForward;
            Vector2 offset = Vector2.Lerp(LoweredOffset, RaisedOffset, raise);
            Vector3 position = eyes + forward * offset.x + Vector3.up * offset.y;

            // La trasera (+Z) apunta en la dirección de los ojos al móvil, así que la pantalla mira a la cara; al
            // encuadrar gira a horizontal.
            Vector3 up = Vector3.Slerp(forward, Vector3.up, raise);
            Quaternion rotation = Quaternion.LookRotation(position - eyes, up) * Quaternion.Euler(0f, 0f, 90f * raise);
            _phone.SetPositionAndRotation(position, rotation * ModelRotation);

            SetHand(AvatarIKGoal.RightHand, position + rotation * Vector3.Lerp(PortraitRight, LandscapeRight, raise));
            SetHand(AvatarIKGoal.LeftHand, position + rotation * Vector3.Lerp(PortraitLeft, LandscapeLeft, raise));
            _animator.SetLookAtWeight(_handsWeight, 0.2f, 0.8f);
            _animator.SetLookAtPosition(Vector3.Lerp(position, focus, raise));
        }

        private void SetHand(AvatarIKGoal hand, Vector3 position)
        {
            _animator.SetIKPositionWeight(hand, _handsWeight);
            _animator.SetIKPosition(hand, position);
        }
    }
}
