using System.IO;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor
{
    /// <summary>
    /// Prepara los modelos hechos con MakeHuman (MPFB2 en Blender), los de los personajes principales y el del jugador:
    /// los importa como
    /// Humanoid y crea sus materiales URP. Cada personaje está en su carpeta con <c>&lt;personaje&gt;.fbx</c> y una
    /// carpeta <c>Textures</c>. Los materiales del modelo se llaman <c>&lt;personaje&gt;.&lt;pieza&gt;</c> y la
    /// textura de cada pieza es <c>Textures/&lt;pieza&gt;.png</c>; una pieza sin textura (las gafas) conserva el
    /// color de su material. Las texturas con canal alfa (pelo, cejas, pestañas, logos) se recortan por él.
    /// </summary>
    internal static class MakeHumanMaterials
    {
        private static readonly string[] Folders = { ProjectPaths.Characters + "/Main", ProjectPaths.Characters + "/Player" };
        private const string BodyPart = "body";
        private const float BodySmoothness = 0.3f;
        private const float ClothSmoothness = 0.15f;
        private const float PlainSmoothness = 0.6f;

        [MenuItem("Guadalupe/Personajes/Crear materiales del jugador y los personajes principales (MakeHuman)", priority = 31)]
        public static void CreateAll()
        {
            foreach (string root in Folders)
            {
                foreach (string folder in AssetDatabase.GetSubFolders(root))
                {
                    Create(folder);
                }
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Importa como Humanoid el modelo de <paramref name="folder"/>, crea o actualiza sus materiales y hace que
        /// el modelo los use en lugar de los suyos.
        /// </summary>
        public static void Create(string folder)
        {
            string character = Path.GetFileName(folder);
            string modelPath = $"{folder}/{character}.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (importer == null)
            {
                Debug.LogError($"{character}: no está el modelo {modelPath}.");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            // Sin las asignaciones de una pasada anterior, para que el modelo vuelva a traer todos sus materiales.
            foreach (AssetImporter.SourceAssetIdentifier remap in importer.GetExternalObjectMap().Keys)
            {
                importer.RemoveRemap(remap);
            }

            importer.SaveAndReimport();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is not Material source)
                {
                    continue;
                }

                string part = source.name.Substring(source.name.IndexOf('.') + 1);
                Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/Textures/{part}.png");
                Material material = Lit(folder, $"M_{character}_{Pascal(part)}", baseMap, source.GetColor("_BaseColor"), part);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(source), material);
            }

            importer.SaveAndReimport();
        }

        private static Material Lit(string folder, string name, Texture2D baseMap, Color plainColor, string part)
        {
            Material material = CharacterMaterials.LoadOrCreate($"{folder}/{name}.mat");
            material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", baseMap != null ? Color.white : plainColor);
            material.SetFloat("_Smoothness", baseMap == null ? PlainSmoothness : part == BodyPart ? BodySmoothness : ClothSmoothness);
            if (baseMap != null && HasAlpha(baseMap))
            {
                CharacterMaterials.MakeCutout(material);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool HasAlpha(Texture2D texture) =>
            AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) is TextureImporter importer
            && importer.DoesSourceTextureHaveAlpha();

        private static string Pascal(string part)
        {
            var words = part.Split('-', '_');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = words[i].Length == 0 ? words[i] : char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            }

            return string.Concat(words);
        }
    }
}
