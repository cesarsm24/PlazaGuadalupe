using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Guadalupe.Ambience;
using Guadalupe.Core;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Comprueba los datos de los que parte el generador antes de usarlos, para que un error en un fichero de
    /// diseño o un recurso sin asignar se vea con un mensaje claro y no como una plaza mal generada.
    /// </summary>
    /// <remarks>
    /// Revisa los datos geográficos (MDE, OpenStreetMap y Catastro), el catálogo de casas (<c>houses.json</c>), el
    /// trazado de la plaza (<c>plaza_layout.json</c>), los ajustes del generador, los recursos de materiales y
    /// prefabs y las piezas del monasterio que el generador busca. Lo que solo necesita la reexportación del
    /// monasterio (la fachada de la portería y la casa del atrio) se comprueba solo para ella.
    /// </remarks>
    public static class DesignDataValidator
    {
        [MenuItem("Guadalupe/Entorno/Validar datos de diseño", priority = 22)]
        private static void ValidateFromMenu()
        {
            List<string> errors = Validate(includeMonasteryModel: true);
            foreach (string error in errors)
            {
                Debug.LogError($"[Datos de diseño] {error}");
            }

            EditorUtility.DisplayDialog("Datos de diseño",
                errors.Count == 0 ? "Los datos de diseño son correctos." : $"Hay {errors.Count} errores; se muestran en la consola.", "Aceptar");
        }

        /// <summary>Devuelve los errores encontrados; vacía si todo es correcto.</summary>
        /// <param name="includeMonasteryModel">Comprueba también los datos que solo usa la reexportación del monasterio.</param>
        public static List<string> Validate(bool includeMonasteryModel = true)
        {
            var errors = new List<string>();
            var projection = new GeoProjection(PlazaLayout.OriginLat, PlazaLayout.OriginLon);
            Check(errors, "dem_plaza.json", () => ValidateDem(DemGrid.FromJson(File.ReadAllText(ProjectPaths.DemFile)), errors));
            Check(errors, "osm_plaza.json", () =>
                ValidateOsm(OsmDataset.Parse(File.ReadAllText(ProjectPaths.OsmFile), projection, PlazaLayout.PlazaWayId), errors));

            HashSet<string> parts = null;
            Check(errors, "catastro_plaza.json", () =>
            {
                parts = new HashSet<string>(CatastroDataset.Parse(File.ReadAllText(ProjectPaths.CadastreFile), projection).Select(b => b.Id));
                ValidateCadastre(parts, includeMonasteryModel, errors);
            });
            if (parts != null)
            {
                Check(errors, "houses.json", () => ValidateHouses(DesignData.Read<List<HouseStyle>>(ProjectPaths.HousesFile), parts, errors));
            }

            Check(errors, "plaza_layout.json", () =>
                ValidateLayout(DesignData.Read<PlazaLayout.LayoutFile>(ProjectPaths.LayoutFile), includeMonasteryModel, errors));
            Check(errors, "GeneratorSettings", () => ValidateSettings(errors));
            Check(errors, "EnvironmentMaterials", () => ValidateAssigned(EnvironmentMaterials.Load(), errors));
            Check(errors, "PropPrefabs", () =>
            {
                PropPrefabs prefabs = PropPrefabs.Load();
                ValidateAssigned(prefabs, errors);
                if (prefabs.Monastery != null)
                {
                    ValidateMonasteryPrefab(prefabs.Monastery, errors);
                }
            });
            return errors;
        }

        /// <summary>Lanza una excepción con todos los errores si los datos no son correctos.</summary>
        /// <param name="includeMonasteryModel">Comprueba también los datos que solo usa la reexportación del monasterio.</param>
        /// <exception cref="System.InvalidOperationException">Si hay algún error.</exception>
        public static void ThrowIfInvalid(bool includeMonasteryModel)
        {
            List<string> errors = Validate(includeMonasteryModel);
            if (errors.Count > 0)
            {
                throw new System.InvalidOperationException("Datos de diseño no válidos:\n" + string.Join("\n", errors));
            }
        }

        internal static void ValidateDem(DemGrid dem, List<string> errors)
        {
            if (dem.Samples < 2 || dem.SizeMeters <= 0f || dem.MaxHeight <= dem.MinHeight)
            {
                errors.Add("dem_plaza.json: el modelo de elevaciones está vacío o es plano.");
            }
        }

        internal static void ValidateOsm(OsmDataset osm, List<string> errors)
        {
            if (osm.PlazaOutline.Count < 3)
            {
                errors.Add($"osm_plaza.json: falta el contorno de la plaza (vía {PlazaLayout.PlazaWayId}).");
            }
        }

        /// <param name="parts">Identificadores de las partes de edificio del Catastro.</param>
        internal static void ValidateCadastre(ICollection<string> parts, bool includeMonasteryModel, List<string> errors)
        {
            if (!parts.Any(id => id.StartsWith(PlazaLayout.MonasteryCadastralRef, System.StringComparison.Ordinal)))
            {
                errors.Add($"catastro_plaza.json: falta el monasterio ({PlazaLayout.MonasteryCadastralRef}).");
            }

            if (includeMonasteryModel && !parts.Contains(PlazaLayout.AtriumHousePart))
            {
                errors.Add($"catastro_plaza.json: falta la casa del atrio ({PlazaLayout.AtriumHousePart}).");
            }
        }

        /// <param name="parts">Identificadores de las partes de edificio del Catastro.</param>
        internal static void ValidateHouses(IEnumerable<HouseStyle> houses, ICollection<string> parts, List<string> errors)
        {
            var fincas = new HashSet<string>(parts.Select(BuildingFootprint.ParcelOf));
            var seen = new HashSet<string>();
            foreach (HouseStyle house in houses)
            {
                string id = $"houses.json, {house.CadastralRef} ({house.Address})";
                if (house.CadastralRef.Length != 7)
                {
                    errors.Add($"{id}: la referencia catastral debe tener los 7 primeros caracteres de la finca.");
                }
                else if (!fincas.Contains(house.CadastralRef))
                {
                    errors.Add($"{id}: la referencia no está en el Catastro descargado.");
                }

                if (!seen.Add(house.CadastralRef))
                {
                    errors.Add($"{id}: referencia repetida.");
                }

                if (house.Floors.HasValue && house.Floors.Value <= 0)
                {
                    errors.Add($"{id}: el número de plantas debe ser positivo.");
                }

                if (house.BalconyChance < 0f || house.BalconyChance > 1f || house.ShutterChance < 0f || house.ShutterChance > 1f)
                {
                    errors.Add($"{id}: las probabilidades deben estar entre 0 y 1.");
                }

                if (house.ZocaloHeight <= 0f)
                {
                    errors.Add($"{id}: la altura del zócalo debe ser positiva.");
                }

                if (!string.IsNullOrEmpty(house.Sign2) && string.IsNullOrEmpty(house.Sign))
                {
                    errors.Add($"{id}: tiene un segundo rótulo sin el primero.");
                }
            }
        }

        internal static void ValidateLayout(PlazaLayout.LayoutFile layout, bool includeMonasteryModel, List<string> errors)
        {
            if (layout.MarquesRomana == null || layout.MarquesRomana.Length < 2)
            {
                errors.Add("plaza_layout.json: el eje de Marqués de la Romana necesita al menos 2 puntos.");
            }

            if (includeMonasteryModel && (layout.PorteriaFront == null || layout.PorteriaFront.Length != 2))
            {
                errors.Add("plaza_layout.json: la fachada de la portería debe tener sus 2 extremos.");
            }

            var names = new HashSet<string>();
            foreach (PlazaLayout.ArcadeEntry arcade in layout.Arcades ?? new PlazaLayout.ArcadeEntry[0])
            {
                CheckLine(errors, arcade.Name, arcade.Line, names);
            }

            foreach (PlazaLayout.PassageEntry passage in layout.Passages ?? new PlazaLayout.PassageEntry[0])
            {
                CheckLine(errors, passage.Name, passage.Line, names);
                if (passage.Width <= 0f || passage.ClearHeight <= 0f)
                {
                    errors.Add($"plaza_layout.json, {passage.Name}: anchura y altura libre deben ser positivas.");
                }
            }

            foreach (PlazaLayout.BridgeEntry bridge in layout.Bridges ?? new PlazaLayout.BridgeEntry[0])
            {
                if (!names.Add(bridge.Name ?? string.Empty))
                {
                    errors.Add($"plaza_layout.json: nombre repetido o vacío «{bridge.Name}».");
                }

                if (bridge.Length <= 0f || bridge.Floors <= 0 || bridge.ClearHeight <= 0f)
                {
                    errors.Add($"plaza_layout.json, {bridge.Name}: longitud, plantas y altura libre deben ser positivas.");
                }
            }

            if (!names.Contains(PlazaLayout.MarquesArcade))
            {
                errors.Add($"plaza_layout.json: falta el soportal «{PlazaLayout.MarquesArcade}».");
            }
        }

        /// <summary>Piezas del prefab del monasterio a las que el generador asigna el reloj, los focos y los adornos.</summary>
        internal static void ValidateMonasteryPrefab(GameObject monastery, List<string> errors)
        {
            string[] nodes =
            {
                MonasteryPrefabNodes.Monastery, MonasteryPrefabNodes.FloodLights, MonasteryPrefabNodes.Decorations,
            };
            foreach (string node in nodes)
            {
                if (monastery.transform.Find(node) == null)
                {
                    errors.Add($"{monastery.name}: falta el nodo «{node}».");
                }
            }

            if (monastery.GetComponentInChildren<TowerClock>(true) == null)
            {
                errors.Add($"{monastery.name}: falta el reloj de la torre (TowerClock).");
            }
        }

        private static void Check(List<string> errors, string source, System.Action validate)
        {
            try
            {
                validate();
            }
            catch (System.Exception e)
            {
                errors.Add($"{source}: no se puede leer ({e.GetType().Name}: {e.Message}).");
            }
        }

        private static void CheckLine(List<string> errors, string name, Vector2[] line, HashSet<string> names)
        {
            if (!names.Add(name ?? string.Empty))
            {
                errors.Add($"plaza_layout.json: nombre repetido o vacío «{name}».");
            }

            if (line == null || line.Length < 2)
            {
                errors.Add($"plaza_layout.json, {name}: la línea necesita al menos 2 puntos.");
            }
        }

        private static void ValidateSettings(List<string> errors)
        {
            GeneratorSettings s = GeneratorSettings.Load();
            if (s.ReliefInnerRadius > s.ReliefFullRadius)
            {
                errors.Add("GeneratorSettings: el radio interior del relieve no puede superar al radio del relieve real.");
            }

            if (s.DetailRadius > s.WorkRadius || s.ShopRadius > s.WorkRadius)
            {
                errors.Add("GeneratorSettings: los radios de detalle y de comercios no pueden superar el radio de trabajo.");
            }
        }

        /// <summary>Todos los campos serializados que referencian recursos deben estar asignados.</summary>
        private static void ValidateAssigned(ScriptableObject asset, List<string> errors)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (FieldInfo field in asset.GetType().GetFields(flags))
            {
                if (field.GetCustomAttribute<SerializeField>() != null && typeof(Object).IsAssignableFrom(field.FieldType) &&
                    (Object)field.GetValue(asset) == null)
                {
                    errors.Add($"{asset.name}: falta asignar {field.Name.TrimStart('_')}.");
                }
            }
        }
    }
}
