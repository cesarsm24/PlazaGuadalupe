using System.Globalization;
using System.Text;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Nombres de los objetos de la jerarquía: en español y en ASCII, sin espacios, tildes ni comillas, con el
    /// formato <c>Tipo_Identificador</c> (por ejemplo, <c>Rotulo_CafeBarCerezo</c> o <c>Proyector_01</c>).
    /// </summary>
    public static class HierarchyNames
    {
        /// <summary>
        /// Convierte un texto libre en PascalCase ASCII: quita tildes, signos y espacios y pone en mayúscula la
        /// primera letra de cada palabra. Conserva los dígitos y los guiones de los intervalos (<c>6-8</c>).
        /// </summary>
        public static string ToPascal(string text)
        {
            var result = new StringBuilder();
            bool upper = true;
            foreach (char c in RemoveDiacritics(text ?? string.Empty))
            {
                if (char.IsLetterOrDigit(c) && c < 128)
                {
                    result.Append(upper ? char.ToUpperInvariant(c) : c);
                    upper = false;
                }
                else if (c == '-' && result.Length > 0 && char.IsDigit(result[result.Length - 1]))
                {
                    result.Append(c);
                    upper = true;
                }
                else
                {
                    upper = true;
                }
            }

            return result.ToString();
        }

        /// <summary>Nombre <c>Tipo_Identificador</c>; el identificador se pasa a PascalCase ASCII.</summary>
        public static string Of(string type, string identifier)
        {
            string id = ToPascal(identifier);
            return id.Length > 0 ? $"{type}_{id}" : type;
        }

        /// <summary>Nombre <c>Tipo_NN</c> con el número de orden en dos cifras.</summary>
        public static string Numbered(string type, int number) => $"{type}_{number:00}";

        private static string RemoveDiacritics(string text)
        {
            var result = new StringBuilder(text.Length);
            foreach (char c in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return result.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
