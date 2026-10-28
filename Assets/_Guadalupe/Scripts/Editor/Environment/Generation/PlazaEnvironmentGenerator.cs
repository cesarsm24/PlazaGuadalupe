using Guadalupe.Ambience;
using Guadalupe.Core;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Genera el entorno de la Plaza de Santa María de Guadalupe en la escena activa a partir de
    /// datos abiertos: terreno (EU-DEM), casas (Catastro) y plaza (OpenStreetMap), con el aspecto de cada casa y
    /// el trazado de soportales y arcos de los ficheros de diseño.
    /// </summary>
    /// <remarks>
    /// La generación es determinista y atómica: el entorno nuevo, su terreno, sus mallas y la NavMesh se crean en
    /// memoria, y solo cuando todo eso ha terminado se guardan en el proyecto y sustituyen al entorno anterior
    /// (la raíz marcada con <see cref="GeneratedEnvironment"/>). Si algo falla antes, la escena y los recursos
    /// quedan como estaban. El monasterio, el mobiliario, el límite jugable, la iluminación, la navegación y la
    /// interfaz están colocados en la escena; el generador solo les asigna lo que depende de lo generado (el reloj
    /// de la torre, las luces de los controladores y los adornos de Navidad).
    /// </remarks>
    public static class PlazaEnvironmentGenerator
    {
        /// <summary>Nombre del objeto raíz que contiene todo el entorno generado.</summary>
        public const string RootName = "Entorno";

        private const string Title = "Entorno de la plaza";

        [MenuItem("Guadalupe/Entorno/Generar entorno de la plaza", priority = 20)]
        private static void GenerateFromMenu()
        {
            if (EditorUtility.DisplayDialog(Title,
                    "Se regenerará el entorno a partir de los datos y se guardará la escena. Lo que está fuera de " +
                    $"«{RootName}» no cambia.", "Generar", "Cancelar"))
            {
                Generate();
            }
        }

        /// <summary>Genera el entorno, hornea la navegación y guarda la escena.</summary>
        public static void Generate()
        {
            var ctx = new GenerationContext();
            bool saving = false;
            bool completed = false;
            try
            {
                NavigationBaker.FindSurface();
                Build(ctx);
                ctx.Output.Finish();

                Progress("NavMesh", 0.9f);
                NavMeshSurface surface = NavigationBaker.Build();

                // A partir de aquí se sustituyen los recursos del proyecto: si algo fallase ya no se puede volver
                // al entorno anterior, así que se deja el nuevo en la escena sin guardarla.
                Progress("Guardando", 0.95f);
                saving = true;
                TerrainBuilder.Save(ctx.Terrain);
                ctx.Output.SaveMeshes();
                NavigationBaker.Save(surface);
                ctx.Output.ReplacePrevious();

                EditorSceneManager.MarkSceneDirty(ctx.Output.Root.scene);
                EditorSceneManager.SaveScene(ctx.Output.Root.scene);
                completed = true;
                Debug.Log($"[Entorno] Generado: {ctx.Results.BuildingCount} partes de edificio ({ctx.Results.PorticoCount} con soportal).");
            }
            finally
            {
                if (!completed && !saving)
                {
                    ctx.Discard();
                }

                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Construye el entorno en una raíz nueva sobre el monasterio colocado en la escena y asigna las luces a los
        /// controladores.
        /// </summary>
        internal static void Build(GenerationContext ctx)
        {
            Prepare(ctx, includeMonasteryModel: false);
            ctx.UsePlacedMonastery();
            TowerClock clock = ctx.Scene.PlacedMonastery.GetComponentInChildren<TowerClock>(true);
            clock.SetClock(ctx.Scene.Clock);
            PrefabUtility.RecordPrefabInstancePropertyModifications(clock);

            Progress("Terreno", 0.2f);
            ctx.Terrain = TerrainBuilder.Build(ctx);
            Progress("Casas y soportales", 0.5f);
            TownBuilder.Build(ctx);
            Progress("Iluminación", 0.85f);
            LightingBuilder.Build(ctx);
        }

        /// <summary>
        /// Comprueba los datos, los carga, localiza lo colocado en la escena y prepara una raíz nueva. Lo usan el
        /// generador y la herramienta que exporta el monasterio.
        /// </summary>
        internal static void Prepare(GenerationContext ctx, bool includeMonasteryModel)
        {
            Progress("Cargando datos geográficos", 0.05f);
            DesignDataValidator.ThrowIfInvalid(includeMonasteryModel);
            ctx.Load();
            ctx.Output.Prepare(RootName);
        }

        private static void Progress(string step, float value) => EditorUtility.DisplayProgressBar(Title, step, value);
    }
}
