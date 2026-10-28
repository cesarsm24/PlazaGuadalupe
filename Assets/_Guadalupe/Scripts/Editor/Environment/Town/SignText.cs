using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Letras de los rótulos: carga de las tipografías y colocación del texto (TextMesh) encajado en un ancho.
    /// </summary>
    /// <remarks>
    /// Se usa <see cref="TextMesh"/> con el shader propio <c>Guadalupe/SignText</c> en lugar de TextMeshPro: el
    /// texto forma parte del entorno generado, necesita profundidad y desvanecerse con la distancia, y así no
    /// hace falta generar atlas SDF para cada tipografía.
    /// </remarks>
    internal static class SignText
    {
        /// <summary>
        /// Fuente y material de letras para una tipografía. El material respeta la profundidad, solo se dibuja por
        /// delante y se desvanece con la distancia (las letras solo se leen al mirar el rótulo de cerca).
        /// </summary>
        private static (Font Font, Material Material) SignFontFor(SignFont kind)
        {
            (Font font, Material material) = EnvironmentMaterials.Load().FontFor(kind);
            if (font.dynamic)
            {
                // La fuente integrada es dinámica: hay que pedir los glifos antes de medir el texto.
                font.RequestCharactersInTexture(Characters, 64, FontStyle.Bold);
            }

            return (font, material);
        }

        /// <summary>Caracteres que pueden aparecer en un rótulo.</summary>
        private const string Characters =
            " !\"&'(),-./0123456789:;?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚÜÑáéíóúüñ¡¿ºª→";

        /// <summary>Ancho en metros de un texto con un tamaño de carácter dado (una unidad de fuente = 0,1 · tamaño).</summary>
        private static float TextWidth(Font font, string text, int size, FontStyle fontStyle, float characterSize)
        {
            float advance = 0f;
            foreach (char c in text)
            {
                if (font.dynamic ? font.GetCharacterInfo(c, out CharacterInfo info, size, fontStyle) : font.GetCharacterInfo(c, out info))
                {
                    advance += info.advance;
                }
                else
                {
                    advance += size * 0.5f;
                }
            }

            return advance * characterSize * 0.1f;
        }

        /// <summary>Letras sobre un rótulo, centradas y encajadas en el ancho dado.</summary>
        internal static void AddLabel(GameObject parent, string text, SignFont kind, Vector3 position, Vector3 facing, float maxWidth,
            float maxCharacterSize, Color color)
        {
            (Font font, Material material) = SignFontFor(kind);
            var label = new GameObject("Texto");
            label.transform.SetParent(parent.transform, false);
            label.transform.position = position;
            label.transform.rotation = Quaternion.LookRotation(-facing, Vector3.up);
            TextMesh tm = label.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = font.dynamic ? 64 : 0;
            tm.fontStyle = font.dynamic ? FontStyle.Bold : FontStyle.Normal;
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            label.GetComponent<MeshRenderer>().sharedMaterial = material;
            float natural = TextWidth(font, text, 64, tm.fontStyle, 1f);
            tm.characterSize = natural > 0.01f ? Mathf.Min(maxCharacterSize, maxWidth / natural) : maxCharacterSize;
        }
    }
}
