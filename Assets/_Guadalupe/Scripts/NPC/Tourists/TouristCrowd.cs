using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Reglas de los turistas sin depender de la escena: cuántos hay a cada hora, quién sale según la época y lo que
    /// ya hay en la plaza, a qué destino van, qué hacen sentados y cómo apoyan los pies. Las aplican
    /// <see cref="TouristSpawner"/> y <see cref="Tourist"/>.
    /// </summary>
    internal static class TouristCrowd
    {
        /// <summary>Turistas que debe haber con una afluencia (0-1) y un máximo dados.</summary>
        public static int TargetCount(float crowd, int max) => Mathf.RoundToInt(Mathf.Clamp01(crowd) * Mathf.Max(0, max));

        /// <summary>
        /// Indica si una entrada sirve para que aparezca o desaparezca un turista sin que el jugador lo vea: lejos
        /// de él o fuera de su campo de visión.
        /// </summary>
        public static bool IsHiddenFromPlayer(float distance, bool inView, float minDistance) => distance >= minDistance || !inView;

        /// <summary>Probabilidad de que un grupo sentado a la mesa esté charlando y no solo sentado.</summary>
        public const float ChatWhenSeatedInGroup = 0.75f;

        /// <summary>Qué hace un turista sentado: solo descansa; en grupo, casi siempre charla.</summary>
        /// <param name="roll">Número aleatorio entre 0 y 1.</param>
        public static TouristActivity SeatedActivity(int groupSize, float roll) =>
            groupSize > 1 && roll < ChatWhenSeatedInGroup ? TouristActivity.SitAndTalk : TouristActivity.Sit;

        /// <summary>
        /// Elige un índice al azar con probabilidad proporcional a su peso entre los <paramref name="count"/>
        /// primeros; -1 si todos pesan 0.
        /// </summary>
        /// <param name="roll">Número aleatorio entre 0 y 1.</param>
        public static int PickWeighted(float[] weights, int count, float roll)
        {
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                total += Mathf.Max(0f, weights[i]);
            }

            if (total <= 0f)
            {
                return -1;
            }

            float target = Mathf.Clamp01(roll) * total;
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                last = i;
                target -= weights[i];
                if (target < 0f)
                {
                    return i;
                }
            }

            return last;
        }

        /// <summary>
        /// Tamaño de un grupo que llega a la plaza: sobre todo parejas y gente sola, y algún grupo de tres o cuatro.
        /// </summary>
        /// <param name="roll">Número aleatorio entre 0 y 1.</param>
        public static int GroupSize(float roll) => roll < 0.35f ? 1 : roll < 0.75f ? 2 : roll < 0.9f ? 3 : 4;

        /// <summary>Paradas que hace un grupo antes de marcharse (entre una y tres).</summary>
        /// <param name="roll">Número aleatorio entre 0 y 1.</param>
        public static int Stops(float roll) => roll < 0.4f ? 1 : roll < 0.8f ? 2 : 3;

        /// <summary>
        /// Desplazamiento lateral de la persona <paramref name="index"/> de un grupo de <paramref name="count"/> que se
        /// para en fila, centrado en el sitio.
        /// </summary>
        public static float SideBySideOffset(int index, int count, float spacing) => (index - (count - 1) * 0.5f) * spacing;

        /// <summary>
        /// Escala de la animación de andar para que los pies no resbalen: la velocidad del agente entre la que da la
        /// animación, que crece con el tamaño del personaje.
        /// </summary>
        /// <remarks>Se limita a <see cref="WalkScaleRange"/>: fuera de él la animación se nota acelerada o a cámara lenta.</remarks>
        public static float WalkAnimationScale(float agentSpeed, float naturalSpeed, float scale) =>
            naturalSpeed > 0f && scale > 0f
                ? Mathf.Clamp(agentSpeed / (naturalSpeed * scale), WalkScaleRange.x, WalkScaleRange.y)
                : 1f;

        /// <summary>Escala mínima y máxima de la animación de andar.</summary>
        public static readonly Vector2 WalkScaleRange = new Vector2(0.5f, 1.8f);

        /// <summary>
        /// Avance (0-1) del deslizamiento hasta el asiento o desde él en el instante <paramref name="t"/> (0-1) de la
        /// animación de sentarse o levantarse, que ocurre dentro de <paramref name="window"/>.
        /// </summary>
        public static float SlideProgress(float t, Vector2 window) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(window.x, window.y, t));

        /// <summary>
        /// Altura a la que va la suela sobre el suelo real: la del pie apoyado (por debajo de <paramref name="planted"/>)
        /// es cero, y la del que va en el aire, la que le da la animación.
        /// </summary>
        public static float FootLift(float animatedSole, float planted) => animatedSole < planted ? 0f : animatedSole;

        /// <summary>
        /// Cuánto sube o baja la cadera para que los dos pies lleguen a su sitio: lo que necesite el que tiene que
        /// bajar más (o subir menos), para que ninguna pierna quede estirada de más.
        /// </summary>
        public static float BodyOffset(float leftDelta, float rightDelta) => Mathf.Min(leftDelta, rightDelta);

        /// <summary>
        /// Preferencia (0 la mejor) para sacar a un personaje vestido para <paramref name="season"/>: en Navidad solo
        /// los de abrigo o entretiempo; el resto del año primero los de verano y entretiempo, y los de abrigo solo si
        /// no queda otro sin repetir. -1 si no puede salir.
        /// </summary>
        public static int SeasonRank(TouristSeason season, bool christmas) => christmas
            ? season == TouristSeason.Summer ? -1 : 0
            : season == TouristSeason.Winter ? 1 : 0;

        /// <summary>
        /// Variante de ropa para otra copia de un personaje que ya está en la plaza: la primera que no lleve ninguna de
        /// las copias; si las llevan todas, la menos usada.
        /// </summary>
        public static int UnusedClothes(int count, IReadOnlyCollection<int> inUse)
        {
            int best = 0, bestUses = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                int uses = 0;
                foreach (int used in inUse)
                {
                    if (used == i)
                    {
                        uses++;
                    }
                }

                if (uses < bestUses)
                {
                    best = i;
                    bestUses = uses;
                }
            }

            return best;
        }

        /// <summary>Indica si el agente ha llegado al final de su camino.</summary>
        public static bool HasArrived(bool pathPending, float remainingDistance, float stoppingDistance) =>
            !pathPending && remainingDistance <= stoppingDistance + 0.05f;
    }
}
