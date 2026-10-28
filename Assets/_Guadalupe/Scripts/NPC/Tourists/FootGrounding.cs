using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Apoya los pies del turista en el suelo real por IK: la NavMesh va unos centímetros por encima y en las escaleras
    /// es una rampa. El pie apoyado se planta en el suelo y el que va en el aire conserva su altura sobre él; la cadera
    /// baja o sube lo que haga falta para que las piernas lleguen.
    /// </summary>
    internal sealed class FootGrounding
    {
        // Por debajo de esta altura de la suela (m) la animación tiene el pie apoyado y se planta en el suelo; por
        // encima conserva su altura, contada desde el suelo.
        private const float PlantedHeight = 0.06f;

        private const float GroundProbeUp = 0.5f;
        private const float GroundProbeDown = 1f;
        private const float BodyFollow = 12f; // 1/s

        private static int _groundMask;

        private readonly Animator _animator;
        private readonly Transform _body;
        private float _bodyOffset;

        public FootGrounding(Animator animator, Transform body)
        {
            _animator = animator;
            _body = body;
            if (_groundMask == 0)
            {
                // También el mobiliario: la base de la fuente es un escalón por el que se anda.
                _groundMask = LayerMask.GetMask("Environment", "Props");
            }
        }

        /// <summary>Con la cadera en su altura de la animación, para una visita nueva.</summary>
        public void Reset() => _bodyOffset = 0f;

        /// <summary>
        /// Apoya los pies si <paramref name="grounded"/>; si no (sentado, o al sentarse o levantarse, que el cuerpo ya
        /// se coloca sobre la silla), deja los de la animación.
        /// </summary>
        public void ApplyIK(bool grounded)
        {
            if (!grounded)
            {
                _bodyOffset = 0f;
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
                _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
                return;
            }

            float plane = _body.position.y;
            float scale = _body.localScale.y;
            bool left = TryPlaceFoot(AvatarIKGoal.LeftFoot, _animator.leftFeetBottomHeight * scale, plane, out Vector3 leftTarget, out Quaternion leftRotation, out float leftDelta);
            bool right = TryPlaceFoot(AvatarIKGoal.RightFoot, _animator.rightFeetBottomHeight * scale, plane, out Vector3 rightTarget, out Quaternion rightRotation, out float rightDelta);

            float wanted = TouristCrowd.BodyOffset(left ? leftDelta : 0f, right ? rightDelta : 0f);
            _bodyOffset = Mathf.Lerp(_bodyOffset, wanted, 1f - Mathf.Exp(-BodyFollow * Time.deltaTime));
            _animator.bodyPosition += Vector3.up * _bodyOffset;

            SetFoot(AvatarIKGoal.LeftFoot, left, leftTarget, leftRotation);
            SetFoot(AvatarIKGoal.RightFoot, right, rightTarget, rightRotation);
        }

        /// <summary>
        /// Dónde debería ir un pie sobre el suelo real y cuánto hay que moverlo respecto a la animación; false si no
        /// hay suelo debajo.
        /// </summary>
        private bool TryPlaceFoot(AvatarIKGoal foot, float soleBelowAnkle, float plane, out Vector3 target, out Quaternion rotation, out float delta)
        {
            Vector3 animated = _animator.GetIKPosition(foot);
            rotation = _animator.GetIKRotation(foot);
            target = animated;
            delta = 0f;
            if (!Physics.Raycast(new Vector3(animated.x, plane + GroundProbeUp, animated.z), Vector3.down, out RaycastHit hit,
                    GroundProbeUp + GroundProbeDown, _groundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float sole = animated.y - soleBelowAnkle - plane;
            target.y = hit.point.y + TouristCrowd.FootLift(sole, PlantedHeight) + soleBelowAnkle;
            delta = target.y - animated.y;
            if (sole < PlantedHeight)
            {
                // El pie apoyado sigue la inclinación del escalón o de la cuesta.
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rotation;
            }

            return true;
        }

        private void SetFoot(AvatarIKGoal foot, bool placed, Vector3 target, Quaternion rotation)
        {
            float weight = placed ? 1f : 0f;
            _animator.SetIKPositionWeight(foot, weight);
            _animator.SetIKRotationWeight(foot, weight);
            _animator.SetIKPosition(foot, target);
            _animator.SetIKRotation(foot, rotation);
        }
    }
}
