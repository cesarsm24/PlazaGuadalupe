using System.Collections.Generic;
using System.Linq;
using Guadalupe.Ambience;
using Guadalupe.Core;
using UnityEditor;
using UnityEngine;
using static Guadalupe.Editor.Environment.PlanarGeometry;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Iluminación del escenario de Navidad: cortinas de luz en los balcones de la plaza. Las controla
    /// <see cref="FestiveLights"/>, junto con las del árbol de luces y del monasterio colocados en la plaza. Los
    /// adornos del propio monasterio los construye la herramienta que exporta su modelo.
    /// </summary>
    /// <remarks>
    /// Las bombillas son texturas sobre el shader aditivo <c>Guadalupe/FestiveLights</c>. Los adornos cuelgan de
    /// grupos inactivos que se activan al elegir el escenario de Navidad.
    /// </remarks>
    internal static class ChristmasBuilder
    {
        internal const string GroupName = "IluminacionNavidad";

        /// <summary>Dorado cálido (HDR) de las guirnaldas, cortinas y columnas de luz.</summary>
        internal static readonly Color WarmGold = new Color(1f, 0.76f, 0.42f) * 3.6f;

        /// <summary>
        /// Adornos de la plaza (cortinas de los balcones de las casas) y controlador de la escena: le asigna también
        /// las luces del árbol y del monasterio colocados, marcadas con <see cref="FestiveBulbs"/>.
        /// </summary>
        public static void Build(GenerationContext ctx)
        {
            var root = new GameObject(GroupName);
            root.transform.SetParent(ctx.Output.Root.transform, false);
            var decorations = new GameObject("Adornos");
            decorations.transform.SetParent(root.transform, false);

            var lights = new List<Renderer>();
            var colors = new List<Color>();
            void Register(Renderer r, Color c)
            {
                lights.Add(r);
                colors.Add(c);
            }

            BuildBalconies(ctx, ctx.Results.Balconies, decorations, ctx.Data.Materials.LightsCurtain, "SM_CortinasBalcones", Register);
            List<GameObject> trees = ctx.Scene.InstancesOf(PropPrefabs.Load().ChristmasTree);
            foreach (GameObject tree in trees)
            {
                RegisterBulbs(tree, Register);
                HideInstance(tree);
            }

            RegisterBulbs(ctx.Scene.PlacedMonastery, Register);
            GameObject monasteryDecorations = ctx.Scene.PlacedMonastery.transform.Find(MonasteryPrefabNodes.Decorations).gameObject;
            HideInstance(monasteryDecorations);

            decorations.SetActive(false);
            var shownWithDecorations = new List<GameObject>(trees) { monasteryDecorations };
            ctx.Scene.FestiveLights.Configure(ctx.Scene.Sun, decorations, lights.ToArray(), colors.ToArray(), TablesNearTrees(ctx, trees),
                shownWithDecorations.ToArray());
            EditorUtility.SetDirty(ctx.Scene.FestiveLights);
        }

        /// <summary>Oculta un objeto de una instancia de prefab y lo registra como cambio de la instancia.</summary>
        private static void HideInstance(GameObject go)
        {
            go.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        }

        /// <summary>
        /// Cortinas de luz que cubren todo el ancho de cada balcón (de hierro y corridos de madera) a menos de
        /// <see cref="GeneratorSettings.BalconyLightsRadius"/> de la plaza, desde el pasamanos hasta algo por debajo de la losa.
        /// </summary>
        internal static void BuildBalconies(GenerationContext ctx, List<BalconyAnchor> balconies, GameObject parent,
            Material curtain, string meshName, System.Action<Renderer, Color> register)
        {
            var curtains = new CurtainMesh();
            foreach (BalconyAnchor balcony in balconies)
            {
                if (new Vector2(balcony.RailCenter.x, balcony.RailCenter.z).magnitude > ctx.Data.Settings.BalconyLightsRadius)
                {
                    continue;
                }

                Vector3 along = ToWorld(balcony.Along, 0f) * (balcony.Width * 0.5f + 0.05f);
                Vector3 front = balcony.RailCenter + ToWorld(balcony.Outward, 0f) * 0.06f;
                curtains.Add(front - along, front + along, front.y + 0.05f, front.y - 1.55f);
            }

            var holder = new GameObject("Balcones");
            holder.transform.SetParent(parent.transform, false);
            register(AddRenderer(ctx, holder, "CortinasBalcones", curtains.Build(meshName), curtain), WarmGold);
        }

        /// <summary>
        /// Malla de cortinas de luz: rectángulos verticales cuyas UV empiezan en su borde izquierdo, de modo que
        /// los hilos de bombillas quedan repartidos igual en todas las cortinas y llegan hasta los extremos.
        /// </summary>
        internal sealed class CurtainMesh
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            public void Add(Vector3 left, Vector3 right, float top, float bottom)
            {
                int s = _vertices.Count;
                float width = Vector3.Distance(new Vector3(left.x, 0f, left.z), new Vector3(right.x, 0f, right.z));
                // Las UV van en metros × 0,25, como las del MeshBuilder: el material repite la textura cada metro.
                _vertices.Add(new Vector3(left.x, bottom, left.z));
                _vertices.Add(new Vector3(right.x, bottom, right.z));
                _vertices.Add(new Vector3(right.x, top, right.z));
                _vertices.Add(new Vector3(left.x, top, left.z));
                _uvs.Add(new Vector2(0f, 0f));
                _uvs.Add(new Vector2(width * MeshBuilder.UvScale, 0f));
                _uvs.Add(new Vector2(width * MeshBuilder.UvScale, (top - bottom) * MeshBuilder.UvScale));
                _uvs.Add(new Vector2(0f, (top - bottom) * MeshBuilder.UvScale));
                _triangles.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>Luces de un prefab colocado (árbol, monasterio) marcadas con <see cref="FestiveBulbs"/>.</summary>
        private static void RegisterBulbs(GameObject placed, System.Action<Renderer, Color> register)
        {
            foreach (FestiveBulbs bulbs in placed.GetComponentsInChildren<FestiveBulbs>(true))
            {
                register(bulbs.GetComponent<Renderer>(), bulbs.Color);
            }
        }

        /// <summary>Mesas de terraza a menos de <see cref="GeneratorSettings.TreeClearance"/> de algún árbol, que se ocultan con él.</summary>
        private static GameObject[] TablesNearTrees(GenerationContext ctx, List<GameObject> trees)
        {
            PropPrefabs props = PropPrefabs.Load();
            var found = new List<GameObject>();
            foreach (GameObject table in ctx.Scene.InstancesOf(props.TerraceSet).Concat(ctx.Scene.InstancesOf(props.TerraceTable)))
            {
                Vector3 p = table.transform.position;
                if (trees.Exists(t => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(t.transform.position.x, t.transform.position.z)) < ctx.Data.Settings.TreeClearance))
                {
                    found.Add(table);
                }
            }

            return found.ToArray();
        }

        /// <param name="name">Nombre del objeto en la jerarquía.</param>
        /// <param name="meshName">Nombre de la malla, sin el prefijo <c>SM_</c>.</param>
        internal static Renderer AddRenderer(GenerationContext ctx, GameObject parent, string name, string meshName, MeshBuilder builder, Material material)
        {
            return AddRenderer(ctx, parent, name, builder.Build($"SM_{meshName}"), material);
        }

        internal static Renderer AddRenderer(GenerationContext ctx, GameObject parent, string name, Mesh mesh, Material material)
        {
            ctx.Output.RegisterMesh(mesh);
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }
}
