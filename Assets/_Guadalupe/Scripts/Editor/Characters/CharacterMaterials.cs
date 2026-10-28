using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor
{
    /// <summary>
    /// Materiales URP de los personajes, comunes a los turistas (<see cref="RocketboxMaterials"/>) y a los personajes
    /// principales (<see cref="MakeHumanMaterials"/>).
    /// </summary>
    internal static class CharacterMaterials
    {
        public const string LitShader = "Universal Render Pipeline/Lit";

        /// <summary>Umbral del recorte del pelo, las cejas y las pestañas; el mismo que usan sus mipmaps al importar.</summary>
        public const float HairCutoff = 0.5f;

        /// <summary>El material de <paramref name="path"/>, creado con el shader Lit si aún no existe.</summary>
        public static Material LoadOrCreate(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(material, path);
            }

            return material;
        }

        /// <summary>
        /// Recorte por el alfa, por una cara y sin reflejos ni brillos: así el pelo queda mate y la cara trasera de
        /// los mechones no se ilumina con la normal al revés.
        /// </summary>
        public static void MakeCutout(Material material)
        {
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", HairCutoff);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
            material.SetFloat("_EnvironmentReflections", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.doubleSidedGI = false;
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
    }
}
