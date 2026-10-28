using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>Casa puente sobre una calle abierta: un cuerpo de casa que pasa por encima con un arco.</summary>
    public sealed class StreetBridge
    {
        public StreetBridge(string name, Vector2 center, Vector2 streetDirection, float length, int floors, float clearHeight)
        {
            Name = name;
            Center = center;
            StreetDirection = streetDirection.normalized;
            Length = length;
            Floors = floors;
            ClearHeight = clearHeight;
        }

        public string Name { get; }

        /// <summary>Punto de la calle bajo el centro del puente.</summary>
        public Vector2 Center { get; }

        /// <summary>Dirección de la calle (el arco se ve al mirar a lo largo de ella).</summary>
        public Vector2 StreetDirection { get; }

        /// <summary>Fondo del puente a lo largo de la calle.</summary>
        public float Length { get; }

        /// <summary>Plantas por encima del arco.</summary>
        public int Floors { get; }

        /// <summary>Altura libre en la clave del arco.</summary>
        public float ClearHeight { get; }
    }
}
