using System.IO;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor
{
    /// <summary>
    /// Carpetas, ficheros de datos y recursos que el código del Editor usa por su ruta. Están juntos para que mover
    /// una carpeta del proyecto obligue a cambiar una sola línea.
    /// </summary>
    public static class ProjectPaths
    {
        public const string Root = "Assets/_Guadalupe";

        public const string Scenes = Root + "/Scenes";
        public const string MainScene = Scenes + "/PlazaGuadalupe.unity";

        public const string Models = Root + "/Art/Models";
        public const string GeneratedMeshes = Models + "/Generated";
        public const string MonasteryModels = Models + "/Monastery";
        public const string Characters = Root + "/Art/Characters";
        public const string Animations = Root + "/Art/Animations";
        public const string NpcProfiles = Root + "/ScriptableObjects/NpcProfiles";
        public const string TerrainData = Root + "/Art/Terrain/TD_Guadalupe.asset";
        public const string MonasteryPrefab = Root + "/Prefabs/Environment/PF_Monastery.prefab";

        public const string GeoData = Root + "/Data/Geo";
        public const string DemFile = GeoData + "/dem_plaza.json";
        public const string OsmFile = GeoData + "/osm_plaza.json";
        public const string CadastreFile = GeoData + "/catastro_plaza.json";

        /// <summary>Carpeta de los ficheros de diseño; <see cref="HousesFile"/> y <see cref="LayoutFile"/> van en ella.</summary>
        public const string DesignData = Root + "/Data/Design";
        public const string HousesFile = "houses.json";
        public const string LayoutFile = "plaza_layout.json";

        public const string GeneratorSettings = Root + "/Settings/Generation/SO_GeneratorSettings.asset";
        public const string EnvironmentMaterials = Root + "/Settings/Generation/SO_EnvironmentMaterials.asset";
        public const string PropPrefabs = Root + "/Settings/Generation/SO_PropPrefabs.asset";

        /// <summary>Carga un recurso del proyecto por su ruta.</summary>
        /// <exception cref="FileNotFoundException">Si no está en esa ruta.</exception>
        public static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new FileNotFoundException($"No se encuentra {path}.");
            }

            return asset;
        }
    }
}
