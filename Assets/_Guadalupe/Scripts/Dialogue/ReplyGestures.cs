using System.Text.RegularExpressions;

namespace Guadalupe.Dialogue
{
    /// <summary>Gestos que acompañan a una respuesta según lo que dice.</summary>
    public static class ReplyGestures
    {
        private static readonly Regex Directions = new Regex(
            @"\b(allí|allá|ahí enfrente|por ahí|por aquella|por esa calle|subiendo|bajando|a la derecha|a la izquierda|al fondo|" +
            @"todo recto|desde aquí se ve|mirador|sendero|ruta)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Si indica un camino o un sitio, para señalarlo.</summary>
        public static bool GivesDirections(string reply) => !string.IsNullOrEmpty(reply) && Directions.IsMatch(reply);
    }
}
