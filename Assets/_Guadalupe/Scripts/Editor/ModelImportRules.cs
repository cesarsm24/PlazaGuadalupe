using UnityEditor;

namespace Guadalupe.Editor
{
    /// <summary>
    /// Normas de importación de los modelos propios (<c>Assets/_Guadalupe/Art/Models</c>): geometría tal cual se
    /// exportó (normales y tangentes del fichero, sin soldar ni reordenar vértices), escala 1, y sin cámaras, luces,
    /// animación ni colliders. Los materiales se quedan dentro del modelo; los del proyecto los asigna el prefab.
    /// </summary>
    /// <remarks>
    /// Hacen el papel de un preset de importación, pero en código para que se vean junto al resto del proyecto. Se
    /// aplican solo en la primera importación, de modo que los ajustes que se cambien después en el Inspector se
    /// conservan.
    /// </remarks>
    internal sealed class ModelImportRules : AssetPostprocessor
    {
        private const string ModelsFolder = ProjectPaths.Models + "/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.Import;
            importer.weldVertices = false;
            importer.meshOptimizationFlags = 0;
            importer.sortHierarchyByName = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }
    }
}
