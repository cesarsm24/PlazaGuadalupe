using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Apretón de manos entre dos humanoides con la misma animación, con la mano derecha. El primero se queda quieto
    /// y el segundo se coloca frente a él; un IK junta las palmas cuando la animación acerca las manos.
    /// </summary>
    public static class Handshake
    {
        private const float BlendSeconds = 0.3f;
        private const float ApproachSeconds = 0.35f;

        // Distancias entre las manos animadas a las que el IK empieza a juntarlas y las junta del todo (m).
        private const float GripReach = 0.35f;
        private const float GripFull = 0.12f;

        /// <summary>De la muñeca al centro de la palma: cada muñeca queda a esta distancia del punto de contacto.</summary>
        internal const float PalmReach = 0.08f;

        // Medio grosor de la mano, para que las palmas queden una contra otra y no una dentro de otra.
        private const float PalmHalfThickness = 0.015f;

        private const int GripSamples = 24;

        /// <summary>
        /// Dónde se coloca el segundo para que su mano derecha coincida con la del primero, ambos con la misma
        /// animación y mirándose. Las manos están en el espacio local de cada uno (x a la derecha, z adelante) en el
        /// momento del apretón: si el primero la tiene en (x₁, z₁) y el segundo en (x₂, z₂), el segundo va a
        /// (x₁ + x₂, z₁ + z₂) en el espacio del primero, girado 180°.
        /// </summary>
        public static Pose PartnerPose(Pose first, Vector3 firstHand, Vector3 secondHand)
        {
            var local = new Vector3(firstHand.x + secondHand.x, 0f, firstHand.z + secondHand.z);
            return new Pose(first.position + first.rotation * local, first.rotation * Quaternion.Euler(0f, 180f, 0f));
        }

        /// <summary>
        /// Momento del apretón (cuando la mano derecha está más adelantada) y posición de la mano en ese momento,
        /// en el espacio local del personaje. Evalúa la animación sobre el propio personaje, que vuelve a su
        /// animación en el fotograma siguiente.
        /// </summary>
        public static Vector3 GripHand(Animator animator, AnimationClip clip, out float gripTime)
        {
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            PlayableGraph graph = PlayableGraph.Create("Handshake.Grip");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(graph, "Grip", animator).SetSourcePlayable(playable);

            gripTime = 0f;
            Vector3 best = Vector3.zero;
            float bestForward = float.MinValue;
            for (int i = 0; i <= GripSamples; i++)
            {
                float t = clip.length * i / GripSamples;
                playable.SetTime(t);
                graph.Evaluate();
                Vector3 local = animator.transform.InverseTransformPoint(hand.position);
                if (local.z > bestForward)
                {
                    bestForward = local.z;
                    best = local;
                    gripTime = t;
                }
            }

            graph.Destroy();
            return best;
        }

        /// <summary>
        /// Da la mano: coloca al segundo frente al primero, reproduce la animación en los dos y, al acabar, les
        /// devuelve el control. <paramref name="paused"/> son los comportamientos que no deben actuar mientras tanto
        /// (control del jugador, rutina del personaje); un NavMeshAgent del segundo se pausa siempre.
        /// </summary>
        public static IEnumerator Play(Animator first, Animator second, AnimationClip clip, IReadOnlyList<Behaviour> paused = null)
        {
            var disabled = new List<Behaviour>();
            void Pause(Behaviour behaviour)
            {
                if (behaviour != null && behaviour.enabled)
                {
                    behaviour.enabled = false;
                    disabled.Add(behaviour);
                }
            }

            if (paused != null)
            {
                foreach (Behaviour behaviour in paused)
                {
                    Pause(behaviour);
                }
            }

            var agent = second.GetComponent<NavMeshAgent>();
            Pause(agent);

            Vector3 firstHand = GripHand(first, clip, out _);
            Vector3 secondHand = GripHand(second, clip, out _);
            Transform a = first.transform;
            Transform b = second.transform;
            Pose target = PartnerPose(new Pose(a.position, Quaternion.Euler(0f, a.eulerAngles.y, 0f)), firstHand, secondHand);
            target.position.y = b.position.y;

            // Cada personaje deja en la tabla compartida dónde y cómo tiene la mano animada; el otro lo lee para
            // juntarlas (con un fotograma de retraso, imperceptible).
            var hands = new NativeArray<Vector3>(2, Allocator.Persistent);
            var turns = new NativeArray<Quaternion>(2, Allocator.Persistent);
            Vector3 toPartner = Vector3.ProjectOnPlane(target.position - a.position, Vector3.up).normalized;
            PlayableGraph graphA = Blend(first, clip, new GripJob(hands, turns, 0, true, toPartner), out AnimationMixerPlayable mixerA, out AnimationScriptPlayable gripA);
            PlayableGraph graphB = Blend(second, clip, new GripJob(hands, turns, 1, false, -toPartner), out AnimationMixerPlayable mixerB, out AnimationScriptPlayable gripB);

            // El segundo se acerca a su sitio a la vez que empieza el gesto (el apretón llega bastante después).
            Vector3 startPosition = b.position;
            Quaternion startRotation = b.rotation;
            float elapsed = 0f;
            while (elapsed < clip.length)
            {
                elapsed += Time.deltaTime;
                float move = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ApproachSeconds));
                b.SetPositionAndRotation(Vector3.Lerp(startPosition, target.position, move), Quaternion.Slerp(startRotation, target.rotation, move));
                float weight = Mathf.Clamp01(Mathf.Min(elapsed, clip.length - elapsed) / BlendSeconds);
                SetClipWeight(mixerA, gripA, weight);
                SetClipWeight(mixerB, gripB, weight);
                yield return null;
            }

            graphA.Destroy();
            graphB.Destroy();
            hands.Dispose();
            turns.Dispose();
            if (agent != null)
            {
                agent.Warp(b.position);
            }

            foreach (Behaviour behaviour in disabled)
            {
                behaviour.enabled = true;
            }
        }

        /// <summary>
        /// Grafo que mezcla el controlador del personaje (con sus parámetros actuales) y la animación del apretón;
        /// sustituye a la salida del Animator mientras existe.
        /// </summary>
        private static PlayableGraph Blend(Animator animator, AnimationClip clip, GripJob job, out AnimationMixerPlayable mixer,
            out AnimationScriptPlayable grip)
        {
            PlayableGraph graph = PlayableGraph.Create($"Handshake.{animator.name}");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var controller = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        controller.SetFloat(parameter.nameHash, animator.GetFloat(parameter.nameHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        controller.SetInteger(parameter.nameHash, animator.GetInteger(parameter.nameHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        controller.SetBool(parameter.nameHash, animator.GetBool(parameter.nameHash));
                        break;
                }
            }

            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            graph.Connect(controller, 0, mixer, 0);
            graph.Connect(clipPlayable, 0, mixer, 1);
            grip = AnimationScriptPlayable.Create(graph, job, 1);
            graph.Connect(mixer, 0, grip, 0);
            SetClipWeight(mixer, grip, 0f);
            AnimationPlayableOutput.Create(graph, "Handshake", animator).SetSourcePlayable(grip);
            graph.Play();
            return graph;
        }

        private static void SetClipWeight(AnimationMixerPlayable mixer, AnimationScriptPlayable grip, float weight)
        {
            mixer.SetInputWeight(0, 1f - weight);
            mixer.SetInputWeight(1, weight);
            GripJob job = grip.GetJobData<GripJob>();
            job.Blend = weight;
            grip.SetJobData(job);
        }

        /// <summary>Peso del IK según lo cerca que la animación tiene ya las dos manos.</summary>
        private static float GripWeight(float distance) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(GripFull, GripReach, distance));

        /// <summary>
        /// IK de la mano derecha durante el apretón: publica la mano animada de este personaje y, con la del otro,
        /// deja la muñeca a medio palmo del punto medio, hacia su lado y un poco a su derecha, de modo que las
        /// palmas se juntan. La del primero copia la orientación de la del segundo girada 180°.
        /// </summary>
        private struct GripJob : IAnimationJob
        {
            [NativeDisableContainerSafetyRestriction] private NativeArray<Vector3> _hands;
            [NativeDisableContainerSafetyRestriction] private NativeArray<Quaternion> _turns;
            private readonly int _self;
            private readonly bool _mirrorPartner;
            private readonly Vector3 _toPartner;

            /// <summary>Peso de la animación del apretón en la mezcla (el IK no actúa durante los fundidos).</summary>
            public float Blend;

            public GripJob(NativeArray<Vector3> hands, NativeArray<Quaternion> turns, int self, bool mirrorPartner, Vector3 toPartner)
            {
                _toPartner = toPartner;
                _hands = hands;
                _turns = turns;
                _self = self;
                _mirrorPartner = mirrorPartner;
                Blend = 0f;
            }

            public void ProcessRootMotion(AnimationStream stream)
            {
            }

            public void ProcessAnimation(AnimationStream stream)
            {
                if (!stream.isHumanStream)
                {
                    return;
                }

                AnimationHumanStream human = stream.AsHuman();
                Vector3 own = human.GetGoalPosition(AvatarIKGoal.RightHand);
                _hands[_self] = own;
                _turns[_self] = human.GetGoalRotation(AvatarIKGoal.RightHand);
                Vector3 other = _hands[1 - _self];
                if (other == Vector3.zero)
                {
                    return;
                }

                float weight = GripWeight(Vector3.Distance(own, other)) * Blend;
                Quaternion rotation = _mirrorPartner ? Quaternion.AngleAxis(180f, Vector3.up) * _turns[1 - _self] : _turns[_self];
                Vector3 right = Vector3.Cross(Vector3.up, _toPartner);
                Vector3 wrist = (own + other) * 0.5f - _toPartner * PalmReach + right * PalmHalfThickness;
                human.SetGoalPosition(AvatarIKGoal.RightHand, wrist);
                human.SetGoalRotation(AvatarIKGoal.RightHand, rotation);
                human.SetGoalWeightPosition(AvatarIKGoal.RightHand, weight);
                human.SetGoalWeightRotation(AvatarIKGoal.RightHand, weight);
                human.SolveIK();
            }
        }
    }
}
