using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Lado principal de una casa hacia la plaza, donde van su rótulo y su portada.</summary>
    internal sealed class Frontage
    {
        public Vector2 Mid;
        public Vector2 Normal;
        public Vector2 Along;
        public float Length;
        public float SignY;
        public float Score;
        public HouseStyle Style;
    }
}
