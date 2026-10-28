using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Submallas de la malla de una casa. Cada casa se dibuja con una sola malla y cada submalla lleva un
    /// material distinto (muro, tejado, zócalo…); estos índices dicen en qué submalla va cada pieza.
    /// </summary>
    internal static class HouseMeshSlots
    {
        public const int Wall = 0;
        public const int Roof = 1;
        public const int Zocalo = 2;
        public const int Window = 3;
        public const int Door = 4;
        public const int Wood = 5;
        public const int Metal = 6;
        public const int Glass = 7;
        public const int AwningRed = 8;
        public const int AwningGreen = 9;
        public const int Stone = 10;
        public const int Frame = 11;
        public const int Beam = 12;
        public const int Brick = 13;
        public const int Leaves = 14;
        public const int Grime = 15;
        public const int WindowLit = 16;

        public const int Count = 17;

        /// <summary>Materiales de una casa en el orden de las submallas.</summary>
        public static Material[] MaterialsFor(EnvironmentMaterials m, HouseStyle style, string buildingId)
        {
            return new[]
            {
                m.WallFor(style, buildingId), m.RoofFor(buildingId), m.ZocaloFor(style), m.Window, m.Door, m.Wood,
                m.Metal, m.LanternGlass, m.AwningRed, m.AwningGreen, m.Granite, m.CarpentryFor(style), m.Beam, m.Terracotta,
                m.Foliage, m.Grime, m.WindowLit,
            };
        }
    }
}
