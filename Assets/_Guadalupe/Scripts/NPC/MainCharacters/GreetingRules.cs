using System;
using System.Collections.Generic;
using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>Lo que necesita un personaje para elegir qué decir al ver al jugador.</summary>
    public readonly struct GreetingContext
    {
        public GreetingContext(bool firstTime, bool seenRecently, string playerName, float hour, ScenarioOccasion occasion, float crowd)
        {
            FirstTime = firstTime;
            SeenRecently = seenRecently;
            PlayerName = playerName;
            Hour = hour;
            Occasion = occasion;
            Crowd = crowd;
        }

        /// <summary>Es la primera vez que le ve.</summary>
        public bool FirstTime { get; }

        /// <summary>Ya le ha saludado hace poco.</summary>
        public bool SeenRecently { get; }

        /// <summary>Nombre del jugador, o null si no lo sabe.</summary>
        public string PlayerName { get; }

        /// <summary>Hora del día (0-24).</summary>
        public float Hour { get; }

        public ScenarioOccasion Occasion { get; }

        /// <summary>Afluencia de la plaza (0-1).</summary>
        public float Crowd { get; }
    }

    /// <summary>
    /// Elige el saludo de un personaje según el momento: se presenta la primera vez; al volver a verle poco después
    /// dice algo breve o solo hace el gesto; si no, alterna entre lo propio del día, de la hora, de la gente que hay y
    /// del nombre del jugador, sin repetir las últimas frases.
    /// </summary>
    public static class GreetingRules
    {
        /// <summary>Afluencia a partir de la que comenta que la plaza está llena.</summary>
        public const float CrowdedFrom = 0.6f;

        /// <summary>Frases recientes que no repite.</summary>
        public const int RecentLines = 3;

        /// <summary>Probabilidad de saludar solo con el gesto al volver a verle poco después.</summary>
        public const float SilentWhenSeenRecently = 0.4f;

        /// <summary>Marcador del nombre del jugador en las frases.</summary>
        public const string NameToken = "{nombre}";

        /// <summary>Momento del día de una hora: mañana de 6 a 13, tarde hasta las 21 y noche el resto.</summary>
        public static LineMoment TimeOfDay(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            return hour >= 6f && hour < 13f ? LineMoment.Morning : hour >= 13f && hour < 21f ? LineMoment.Afternoon : LineMoment.Night;
        }

        /// <summary>Momentos que encajan con el contexto y su peso: lo propio del día y el nombre pesan más.</summary>
        public static IReadOnlyList<(LineMoment Moment, float Weight)> Candidates(GreetingContext context)
        {
            if (context.FirstTime)
            {
                return new[] { (LineMoment.Introduction, 1f) };
            }

            if (context.SeenRecently)
            {
                return new[] { (LineMoment.Again, 1f) };
            }

            var candidates = new List<(LineMoment, float)> { (LineMoment.General, 1f), (TimeOfDay(context.Hour), 2f) };
            if (context.Occasion == ScenarioOccasion.Christmas)
            {
                candidates.Add((LineMoment.Christmas, 3f));
            }
            else if (context.Occasion == ScenarioOccasion.Pilgrimage)
            {
                candidates.Add((LineMoment.Pilgrimage, 3f));
            }

            if (context.Crowd >= CrowdedFrom)
            {
                candidates.Add((LineMoment.Crowded, 1.5f));
            }

            if (!string.IsNullOrEmpty(context.PlayerName))
            {
                candidates.Add((LineMoment.Named, 2f));
            }

            return candidates;
        }

        /// <summary>
        /// Frase para el contexto, o null si solo saluda con el gesto o no tiene frases. Entre los momentos con frases
        /// elige uno según su peso, y dentro de él una que no esté entre <paramref name="recent"/>.
        /// </summary>
        /// <param name="linesFor">Frases del personaje para cada momento.</param>
        /// <param name="random">Números aleatorios entre 0 y 1.</param>
        public static string Pick(GreetingContext context, Func<LineMoment, IReadOnlyList<string>> linesFor,
            IReadOnlyCollection<string> recent, Func<float> random)
        {
            if (context.SeenRecently && !context.FirstTime && random() < SilentWhenSeenRecently)
            {
                return null;
            }

            var pool = new List<(IReadOnlyList<string> Lines, float Weight)>();
            float total = 0f;
            foreach ((LineMoment moment, float weight) in Candidates(context))
            {
                IReadOnlyList<string> lines = linesFor(moment);
                if (lines.Count > 0)
                {
                    pool.Add((lines, weight));
                    total += weight;
                }
            }

            if (pool.Count == 0)
            {
                return null;
            }

            float roll = random() * total;
            IReadOnlyList<string> chosen = pool[pool.Count - 1].Lines;
            foreach ((IReadOnlyList<string> lines, float weight) in pool)
            {
                if ((roll -= weight) < 0f)
                {
                    chosen = lines;
                    break;
                }
            }

            return PickLine(chosen, context.PlayerName, recent, random);
        }

        /// <summary>
        /// Una frase al azar, con el nombre del jugador puesto, que no esté entre las recientes; si lo están todas,
        /// cualquiera.
        /// </summary>
        public static string PickLine(IReadOnlyList<string> lines, string playerName, IReadOnlyCollection<string> recent, Func<float> random)
        {
            if (lines.Count == 0)
            {
                return null;
            }

            var fresh = new List<string>();
            var all = new List<string>();
            foreach (string line in lines)
            {
                string said = playerName != null ? line.Replace(NameToken, playerName) : line;
                all.Add(said);
                if (!Contains(recent, said))
                {
                    fresh.Add(said);
                }
            }

            List<string> from = fresh.Count > 0 ? fresh : all;
            return from[Mathf.Min((int)(random() * from.Count), from.Count - 1)];
        }

        private static bool Contains(IReadOnlyCollection<string> recent, string line)
        {
            foreach (string used in recent)
            {
                if (used == line)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Segundos que se ve una frase según lo larga que es.</summary>
        public static float Duration(string line) => Mathf.Clamp(1.8f + line.Length * 0.055f, 3f, 8f);
    }
}
