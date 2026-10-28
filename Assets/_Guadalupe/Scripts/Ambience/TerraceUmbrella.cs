using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>Sombrilla de una mesa de terraza, que <see cref="TerraceUmbrellas"/> abre o recoge.</summary>
    /// <remarks>
    /// Va en el objeto que agrupa el poste y la lona dentro del prefab de la terraza; al recogerla solo se ocultan
    /// sus renderers, de modo que la mesa y las sillas siguen en su sitio.
    /// </remarks>
    public sealed class TerraceUmbrella : MonoBehaviour
    {
        private Renderer[] _parts;

        /// <summary>Muestra la sombrilla abierta o la oculta.</summary>
        public void SetOpen(bool open)
        {
            _parts ??= GetComponentsInChildren<Renderer>(true);
            foreach (Renderer part in _parts)
            {
                part.enabled = open;
            }
        }
    }
}
