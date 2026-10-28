using System.Collections.Generic;
using System.IO;
using Guadalupe.Ambience;
using Guadalupe.Core;
using Guadalupe.Editor.Environment;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Construye el monasterio y su atrio con las medidas de <see cref="AtriumPlan"/>, los exporta como modelos
    /// FBX y monta el prefab que se coloca en la escena, con sus colliders, el reloj de la torre, los focos
    /// nocturnos y los adornos de Navidad.
    /// </summary>
    /// <remarks>
    /// Solo hace falta volver a ejecutarla si cambian las medidas o el código del monasterio. Construye el entorno
    /// completo en una raíz temporal, porque las cotas del monasterio dependen del terreno y sus balcones y focos
    /// se colocan sobre lo construido, y después la descarta: el entorno de la escena no cambia.
    /// </remarks>
    public static class MonasteryModelExporter
    {
        private const string ModelFolder = ProjectPaths.MonasteryModels;
        private const string PrefabPath = ProjectPaths.MonasteryPrefab;
        private const string SceneObjectName = "Monasterio";
        private const string Title = "Modelo del monasterio";

        /// <summary>Lo que el modelo FBX no guarda de cada objeto y hay que reponer en el prefab.</summary>
        private sealed class NodeSettings
        {
            public bool HasCollider;
            public StaticEditorFlags StaticFlags;
            public UnityEngine.Rendering.ShadowCastingMode Shadows;
            public bool ReceiveShadows;
            public Color? BulbColor;
        }

        [MenuItem("Guadalupe/Entorno/Reexportar modelo del monasterio", priority = 33)]
        private static void ExportFromMenu()
        {
            if (EditorUtility.DisplayDialog(Title,
                    "Se volverán a construir el monasterio y el atrio, se exportarán como FBX y se actualizará su " +
                    "prefab. El entorno de la escena no cambia.", "Exportar", "Cancelar"))
            {
                Export();
            }
        }

        // Las casas hacen falta porque algunas piezas del atrio y del monasterio se apoyan en ellas.
        private static void Build(GenerationContext ctx)
        {
            EditorUtility.DisplayProgressBar(Title, "Datos", 0.05f);
            PlazaEnvironmentGenerator.Prepare(ctx, includeMonasteryModel: true);
            EditorUtility.DisplayProgressBar(Title, "Terreno", 0.2f);
            ctx.Terrain = TerrainBuilder.Build(ctx);
            EditorUtility.DisplayProgressBar(Title, "Atrio, escalinata y pasarela", 0.4f);
            AtriumBuilder.Build(ctx);
            EditorUtility.DisplayProgressBar(Title, "Casas", 0.5f);
            TownBuilder.Build(ctx);
            EditorUtility.DisplayProgressBar(Title, "Monasterio", 0.75f);
            MonasteryBuilder.BuildMonastery(ctx);
            MonasteryWingBuilder.Build(ctx);
        }

        /// <summary>Construye, exporta y monta el prefab del monasterio, y lo coloca en la escena si no está.</summary>
        public static void Export()
        {
            var ctx = new GenerationContext();
            var temporary = new List<GameObject>();
            try
            {
                Build(ctx);
                EditorUtility.DisplayProgressBar(Title, "Iluminación del monasterio", 0.85f);
                var bulbs = new Dictionary<Renderer, Color>();
                GameObject floodLights = MonasteryFloodLights.Build(ctx);
                GameObject decorations = MonasteryDecorations.Build(ctx, bulbs);
                ctx.Output.Finish();

                EditorUtility.DisplayProgressBar(Title, "Exportando modelos", 0.9f);
                Transform root = ctx.Output.Root.transform;
                GameObject monastery = Group(MonasteryPrefabNodes.Monastery, root.Find(MonasteryBuilder.GroupName), root.Find(MonasteryWingBuilder.GroupName), decorations.transform);
                GameObject atrium = Group(MonasteryPrefabNodes.Atrium, root.Find(AtriumBuilder.GroupName));
                temporary.AddRange(new[] { monastery, atrium });
                Dictionary<string, NodeSettings> settings = ReadSettings(monastery, bulbs);
                foreach (KeyValuePair<string, NodeSettings> pair in ReadSettings(atrium, bulbs))
                {
                    settings.Add(pair.Key, pair.Value);
                }

                GameObject monasteryModel = ExportModel(monastery, "SM_Monastery");
                GameObject atriumModel = ExportModel(atrium, "SM_Atrium");

                EditorUtility.DisplayProgressBar(Title, "Prefab", 0.95f);
                GameObject prefab = SavePrefab(monasteryModel, atriumModel, floodLights, settings);
                PlaceInScene(prefab, ctx);
            }
            finally
            {
                foreach (GameObject go in temporary)
                {
                    Object.DestroyImmediate(go);
                }

                ctx.Discard();
                EditorUtility.ClearProgressBar();
            }

            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Monasterio] Modelo exportado en {ModelFolder} y prefab actualizado en {PrefabPath}.");
        }

        /// <summary>Agrupa bajo un objeto nuevo en el origen los objetos que forman un modelo.</summary>
        private static GameObject Group(string name, params Transform[] parts)
        {
            var group = new GameObject(name);
            foreach (Transform part in parts)
            {
                part.SetParent(group.transform, true);
            }

            return group;
        }

        /// <summary>Colliders, marcas de estático, sombras y color de las luces de cada objeto, por su ruta.</summary>
        private static Dictionary<string, NodeSettings> ReadSettings(GameObject model, Dictionary<Renderer, Color> bulbs)
        {
            var settings = new Dictionary<string, NodeSettings>();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                settings[model.name + "/" + IndexPath(renderer.transform, model.transform)] = new NodeSettings
                {
                    HasCollider = renderer.GetComponent<MeshCollider>() != null,
                    StaticFlags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject),
                    Shadows = renderer.shadowCastingMode,
                    ReceiveShadows = renderer.receiveShadows,
                    BulbColor = bulbs.TryGetValue(renderer, out Color color) ? color : (Color?)null,
                };
            }

            return settings;
        }

        /// <summary>
        /// Exporta un grupo como FBX binario en posiciones de mundo y lo importa con los materiales del proyecto y
        /// la geometría tal cual (sin recalcular normales ni soldar vértices).
        /// </summary>
        private static GameObject ExportModel(GameObject group, string fileName)
        {
            Directory.CreateDirectory(ModelFolder);
            string path = $"{ModelFolder}/{fileName}.fbx";
            var options = new ExportModelOptions
            {
                ExportFormat = ExportFormat.Binary,
                ModelAnimIncludeOption = Include.Model,
                ObjectPosition = ObjectPosition.WorldAbsolute,
                UseMayaCompatibleNames = false,
            };
            ModelExporter.ExportObject(Path.GetFullPath(path), group, options);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            // Las normas de importación las pone ModelImportRules; aquí solo se asignan los materiales del proyecto.
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            foreach (Renderer renderer in group.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
                    }
                }
            }

            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>
        /// Monta el prefab con los dos modelos y repone lo que el FBX no guarda: colliders, estático, sombras, luces de
        /// Navidad, el reloj de la torre y los focos.
        /// </summary>
        private static GameObject SavePrefab(GameObject monasteryModel, GameObject atriumModel, GameObject floodLights,
            Dictionary<string, NodeSettings> settings)
        {
            var root = new GameObject(SceneObjectName);
            try
            {
                foreach ((GameObject model, string node) in new[] { (monasteryModel, MonasteryPrefabNodes.Monastery), (atriumModel, MonasteryPrefabNodes.Atrium) })
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    instance.name = node;
                }

                ApplySettings(root, settings);

                Transform clock = FindDeep(root.transform, MonasteryBuilder.ClockNode);
                clock.gameObject.AddComponent<TowerClock>().Configure(null, clock.Find(MonasteryBuilder.HourHandNode), clock.Find(MonasteryBuilder.MinuteHandNode));

                var lights = new GameObject(MonasteryPrefabNodes.FloodLights);
                lights.transform.SetParent(root.transform, false);
                foreach (Light source in floodLights.GetComponentsInChildren<Light>(true))
                {
                    var light = new GameObject(source.name);
                    light.transform.SetParent(lights.transform, false);
                    light.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                    EditorUtility.CopySerialized(source, light.AddComponent<Light>());
                }

                int layer = LayerMask.NameToLayer("Environment");
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = layer;
                }

                root.transform.Find(MonasteryPrefabNodes.Decorations).gameObject.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ApplySettings(GameObject root, Dictionary<string, NodeSettings> settings)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Transform model = renderer.transform;
                while (model.parent != root.transform)
                {
                    model = model.parent;
                }

                if (!settings.TryGetValue(model.name + "/" + IndexPath(renderer.transform, model), out NodeSettings node))
                {
                    throw new System.InvalidOperationException($"El modelo tiene un objeto inesperado: {renderer.name}.");
                }

                GameObject go = renderer.gameObject;
                GameObjectUtility.SetStaticEditorFlags(go, node.StaticFlags);
                renderer.shadowCastingMode = node.Shadows;
                renderer.receiveShadows = node.ReceiveShadows;
                if (node.HasCollider)
                {
                    go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                }

                if (node.BulbColor.HasValue)
                {
                    var bulbs = new SerializedObject(go.AddComponent<FestiveBulbs>());
                    bulbs.FindProperty("_color").colorValue = node.BulbColor.Value;
                    bulbs.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        /// <summary>
        /// Coloca el prefab en la escena si no está, lo registra para el generador de la plaza y vuelve a asignar sus
        /// focos al controlador de la escena.
        /// </summary>
        private static void PlaceInScene(GameObject prefab, GenerationContext ctx)
        {
            var props = new SerializedObject(PropPrefabs.Load());
            props.FindProperty("_monastery").objectReferenceValue = prefab;
            props.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            if (ctx.Scene.InstancesOf(prefab).Count == 0)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.Output.Root.scene);
                instance.name = SceneObjectName;
            }

            foreach (GameObject instance in ctx.Scene.InstancesOf(prefab))
            {
                TowerClock clock = instance.GetComponentInChildren<TowerClock>(true);
                clock.SetClock(ctx.Scene.Clock);
                EditorUtility.SetDirty(clock);
            }

            // Los focos del prefab son objetos nuevos y el controlador aún apunta a los anteriores.
            ctx.Scene.FindPlacedMonastery();
            FloodLightBuilder.Configure(ctx);
        }

        /// <summary>
        /// Ruta de índices de hermano desde <paramref name="root"/> (por ejemplo, <c>0/2/1</c>). Identifica cada objeto
        /// aunque la importación del FBX cambie nombres repetidos, porque conserva el orden de la jerarquía.
        /// </summary>
        private static string IndexPath(Transform t, Transform root)
        {
            string path = t.GetSiblingIndex().ToString();
            for (Transform p = t.parent; p != null && p != root; p = p.parent)
            {
                path = p.GetSiblingIndex() + "/" + path;
            }

            return path;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            throw new System.InvalidOperationException($"El modelo no tiene ningún objeto «{name}».");
        }
    }
}
