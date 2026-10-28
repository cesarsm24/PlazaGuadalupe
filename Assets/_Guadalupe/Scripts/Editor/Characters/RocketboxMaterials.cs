using System.IO;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor
{
    /// <summary>
    /// Crea los materiales URP de los personajes de Microsoft Rocketbox y se los asigna a su modelo. Cada personaje
    /// está en su carpeta con el modelo y las texturas <c>&lt;id&gt;_body_*</c>, <c>&lt;id&gt;_head_*</c> y
    /// <c>&lt;id&gt;_opacity_color</c>, y su modelo trae materiales con esos mismos nombres: cuerpo, cabeza y, si lo
    /// tiene, pelo con pestañas, que lleva la transparencia en el canal alfa. Las texturas
    /// <c>&lt;id&gt;_body_color_&lt;v&gt;</c> son variantes del cuerpo con la ropa de otro color, y cada una tiene su
    /// material <c>M_&lt;personaje&gt;_Body_&lt;V&gt;</c>.
    /// </summary>
    internal static class RocketboxMaterials
    {
        private const string Folder = ProjectPaths.Characters + "/Rocketbox";
        private const float BodySmoothness = 0.2f;
        private const float HeadSmoothness = 0.2f;
        private const float HairSmoothness = 0.1f;

        [MenuItem("Guadalupe/Personajes/Crear materiales de los turistas (Rocketbox)", priority = 30)]
        public static void CreateAll()
        {
            foreach (string folder in AssetDatabase.GetSubFolders(Folder))
            {
                Create(folder);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Crea o actualiza los materiales del personaje de <paramref name="folder"/> y hace que su modelo los use
        /// en lugar de los suyos.
        /// </summary>
        public static void Create(string folder)
        {
            string character = Path.GetFileName(folder);
            string[] colors = Directory.GetFiles(folder, "*_body_color.png");
            if (colors.Length != 1)
            {
                Debug.LogError($"{character}: se esperaba una textura *_body_color.png y hay {colors.Length}.");
                return;
            }

            string id = Path.GetFileName(colors[0]).Replace("_body_color.png", "");
            Material body = Lit(folder, $"M_{character}_Body", Texture(folder, id + "_body_color"), Texture(folder, id + "_body_normal"), BodySmoothness);
            Material head = Lit(folder, $"M_{character}_Head", Texture(folder, id + "_head_color"), Texture(folder, id + "_head_normal"), HeadSmoothness);
            foreach (string variant in Directory.GetFiles(folder, id + "_body_color_*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(variant);
                string suffix = name.Substring((id + "_body_color_").Length).ToUpperInvariant();
                Lit(folder, $"M_{character}_Body_{suffix}", Texture(folder, name), Texture(folder, id + "_body_normal"), BodySmoothness);
            }

            var importer = (ModelImporter)AssetImporter.GetAtPath($"{folder}/{character}.fbx");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), id + "_body"), body);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), id + "_head"), head);

            Texture2D opacity = Texture(folder, id + "_opacity_color");
            if (opacity != null)
            {
                Material hair = Lit(folder, $"M_{character}_Hair", opacity, null, HairSmoothness);
                CharacterMaterials.MakeCutout(hair);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), id + "_opacity"), hair);
            }

            importer.SaveAndReimport();
        }

        private static Material Lit(string folder, string name, Texture2D baseMap, Texture2D normalMap, float smoothness)
        {
            Material material = CharacterMaterials.LoadOrCreate($"{folder}/{name}.mat");
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_BumpMap", normalMap);
            if (normalMap != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }

            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D Texture(string folder, string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{name}.png");
    }
}
