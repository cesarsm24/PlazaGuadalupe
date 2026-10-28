using System.Collections.Generic;
using System.Text;

namespace Guadalupe.Dialogue
{
    /// <summary>
    /// Lee una respuesta en Server-Sent Events según va llegando y devuelve el contenido (las líneas <c>data:</c>)
    /// de cada evento completo. Los trozos pueden cortar líneas o caracteres UTF-8 por la mitad.
    /// </summary>
    public sealed class SseReader
    {
        private const string DataField = "data:";

        private readonly Decoder _decoder = new UTF8Encoding(false).GetDecoder();
        private readonly StringBuilder _line = new StringBuilder();
        private readonly StringBuilder _data = new StringBuilder();
        private readonly List<string> _events = new List<string>();
        private char[] _chars = new char[1024];
        private bool _hasData;
        private bool _afterCarriageReturn;

        /// <summary>Añade un trozo de la respuesta y devuelve los eventos que quedan completos con él.</summary>
        public IReadOnlyList<string> Push(byte[] bytes, int count)
        {
            _events.Clear();
            int needed = _decoder.GetCharCount(bytes, 0, count);
            if (needed > _chars.Length)
            {
                _chars = new char[needed];
            }

            int decoded = _decoder.GetChars(bytes, 0, count, _chars, 0);
            for (int i = 0; i < decoded; i++)
            {
                char c = _chars[i];
                // El fin de línea puede ser \n, \r o \r\n.
                if (c == '\n' && _afterCarriageReturn)
                {
                    _afterCarriageReturn = false;
                    continue;
                }

                _afterCarriageReturn = c == '\r';
                if (c == '\n' || c == '\r')
                {
                    EndLine();
                }
                else
                {
                    _line.Append(c);
                }
            }

            return _events;
        }

        /// <summary>Al acabar la respuesta: el último evento si no terminaba con línea en blanco, o null.</summary>
        public string Flush()
        {
            _events.Clear();
            if (_line.Length > 0)
            {
                EndLine();
            }

            EndLine();
            return _events.Count > 0 ? _events[0] : null;
        }

        private void EndLine()
        {
            if (_line.Length == 0)
            {
                if (_hasData)
                {
                    _events.Add(_data.ToString());
                    _data.Clear();
                    _hasData = false;
                }

                return;
            }

            string line = _line.ToString();
            _line.Clear();
            if (!line.StartsWith(DataField))
            {
                // Comentarios (:) y otros campos (event, id, retry) no se usan.
                return;
            }

            if (_hasData)
            {
                _data.Append('\n');
            }

            int start = line.Length > DataField.Length && line[DataField.Length] == ' ' ? DataField.Length + 1 : DataField.Length;
            _data.Append(line, start, line.Length - start);
            _hasData = true;
        }
    }
}
