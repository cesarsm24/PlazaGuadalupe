using UnityEditor;

namespace Guadalupe.Editor
{
    /// <summary>
    /// Normas de importación de los personajes (<c>Art/Characters</c>): Humanoid con su propio avatar, sin cámaras,
    /// luces, animaciones ni expresiones faciales. Los materiales los ponen <see cref="RocketboxMaterials"/> y
    /// <see cref="MakeHumanMaterials"/>.
    /// </summary>
    /// <remarks>Como <see cref="ModelImportRules"/>, se aplican solo en la primera importación.</remarks>
    internal sealed class CharacterImportRules : AssetPostprocessor
    {
        private const string CharactersFolder = ProjectPaths.Characters + "/";
        private const int MaxTextureSize = 1024;
        private const string OpacitySuffix = "_opacity_color.png";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(CharactersFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            // Avatar propio para que les sirvan las animaciones de cualquier otro humanoide.
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(CharactersFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = MaxTextureSize;
            if (assetPath.EndsWith("_normal.png"))
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
            else if (assetPath.EndsWith(OpacitySuffix))
            {
                // Pelo: sin extender el color a lo transparente el borde de cada mechón sale oscuro, y sin conservar
                // la cobertura en las mipmaps clarea de lejos.
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = CharacterMaterials.HairCutoff;
            }
        }
    }
}
