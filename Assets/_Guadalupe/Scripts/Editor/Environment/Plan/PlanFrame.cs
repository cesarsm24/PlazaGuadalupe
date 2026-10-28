using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Marco local del monasterio: (u, v) en metros con origen en la fuente, u hacia el este y v hacia la
    /// fachada. Las alturas van como cotas de nivel.
    /// </summary>
    /// <remarks>
    /// El marco está girado 6° respecto al norte para alinearse con el Catastro. La relación entre cotas y
    /// alturas del mundo (<see cref="GenerationContext.PlanDatum"/>) se fija al generar el terreno.
    /// </remarks>
    internal static class PlanFrame
    {
        /// <summary>Posición en el mundo de la fuente, origen del marco.</summary>
        public static readonly Vector2 Origin = new Vector2(9.52f, 16.12f);

        /// <summary>Giro antihorario del marco respecto a los ejes del mundo, en grados.</summary>
        public const float RotationDegrees = 6f;

        private static readonly float Cos = Mathf.Cos(RotationDegrees * Mathf.Deg2Rad);
        private static readonly float Sin = Mathf.Sin(RotationDegrees * Mathf.Deg2Rad);

        /// <summary>Giro del marco como rotación de Unity (gira el eje u hacia el norte).</summary>
        public static Quaternion Rotation => Quaternion.Euler(0f, -RotationDegrees, 0f);

        /// <summary>Convierte un punto (u, v) del marco a coordenadas horizontales del mundo (x, z).</summary>
        public static Vector2 ToWorld(Vector2 plan)
        {
            return Origin + new Vector2(plan.x * Cos - plan.y * Sin, plan.x * Sin + plan.y * Cos);
        }

        /// <inheritdoc cref="ToWorld(Vector2)"/>
        public static Vector2 ToWorld(float u, float v) => ToWorld(new Vector2(u, v));

        /// <summary>Convierte un punto del mundo (x, z) a (u, v).</summary>
        public static Vector2 ToPlan(Vector2 world)
        {
            Vector2 d = world - Origin;
            return new Vector2(d.x * Cos + d.y * Sin, -d.x * Sin + d.y * Cos);
        }

        /// <summary>Altura del mundo de una cota de nivel.</summary>
        public static float Y(GenerationContext ctx, float level) => level + ctx.Results.PlanDatum;

        public static float GroundY(GenerationContext ctx, Vector2 plan)
        {
            Vector2 w = ToWorld(plan);
            return ctx.GroundY(w.x, w.y);
        }

        /// <summary>
        /// Crea un grupo de la jerarquía colocado en el marco: las mallas de sus hijos se construyen
        /// directamente en coordenadas (u, altura del mundo, v).
        /// </summary>
        public static GameObject CreateGroup(GenerationContext ctx, string name)
        {
            GameObject group = ctx.Output.CreateGroup(name);
            group.transform.localPosition = new Vector3(Origin.x, 0f, Origin.y);
            group.transform.localRotation = Rotation;
            return group;
        }

        /// <summary>Crea, dentro de un grupo sin giro, un hijo colocado en el marco.</summary>
        public static GameObject CreateFrame(GameObject parent, string name)
        {
            var frame = new GameObject(name);
            frame.transform.SetParent(parent.transform, false);
            frame.transform.localPosition = new Vector3(Origin.x, 0f, Origin.y);
            frame.transform.localRotation = Rotation;
            return frame;
        }

        /// <summary>Crea un subgrupo con origen en un punto (u, v) y girado <paramref name="yawDegrees"/> (antihorario) dentro de él.</summary>
        public static GameObject CreateChildFrame(GameObject parent, string name, Vector2 plan, float yawDegrees)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = new Vector3(plan.x, 0f, plan.y);
            child.transform.localRotation = Quaternion.Euler(0f, -yawDegrees, 0f);
            return child;
        }
    }
}
