namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Hash FNV-1a de 32 bits, estable entre ejecuciones y plataformas (a diferencia de <c>string.GetHashCode</c>).
    /// De él salen las decisiones «aleatorias» del generador, que así son variadas pero siempre las mismas.
    /// </summary>
    internal static class StableHash
    {
        private const uint OffsetBasis = 2166136261;
        private const uint Prime = 16777619;

        /// <summary>Hash de un texto; <paramref name="salt"/> separa usos distintos del mismo texto.</summary>
        public static uint Fnv1a(string text, uint salt = 0)
        {
            uint hash = OffsetBasis ^ salt;
            foreach (char c in text ?? string.Empty)
            {
                hash = (hash ^ c) * Prime;
            }

            return hash;
        }

        /// <summary>Añade un valor a un hash ya calculado.</summary>
        public static uint Mix(uint hash, uint value) => (hash ^ value) * Prime;
    }
}
