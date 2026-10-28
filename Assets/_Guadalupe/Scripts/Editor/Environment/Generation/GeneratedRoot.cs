using System.Collections.Generic;
using System.IO;
using Guadalupe.Core;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Raíz del entorno que se está generando: sus grupos y objetos con malla, las consultas físicas contra lo
    /// generado y el ciclo de vida que la hace atómica (se prepara junto a la anterior, se termina y la sustituye, o
    /// se descarta). Guarda también las mallas en el proyecto, agrupadas como la jerarquía.
    /// </summary>
    internal sealed class GeneratedRoot
    {
        /// <summary>
        /// Recurso de mallas de cada grupo de la raíz. Un grupo nuevo que no esté aquí guarda sus mallas en
        /// <see cref="OtherMeshesAsset"/>, para no perderlas.
        /// </summary>
        private static readonly Dictionary<string, string> MeshAssetByGroup = new Dictionary<string, string>
        {
            { TownBuilder.GroupName, "SM_Buildings" },
            { ShopfrontBuilder.GroupName, "SM_Signs" },
            { ChristmasBuilder.GroupName, "SM_BalconyCurtains" },
        };
        private const string OtherMeshesAsset = "SM_Other";
        private const string EnvironmentLayer = "Environment";

        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Collider> _decorativeColliders = new List<Collider>();
        private GameObject _previous;

        public GameObject Root { get; private set; }

        /// <summary>
        /// Objeto colocado en la escena cuyos colliders cuentan como generados al lanzar rayos (el atrio del
        /// monasterio, en el que se apoyan algunas casas).
        /// </summary>
        public Transform AlsoGenerated { get; set; }

        /// <summary>
        /// Crea una raíz nueva y vacía y desactiva la anterior (la marcada con <see cref="GeneratedEnvironment"/>),
        /// que se conserva hasta <see cref="ReplacePrevious"/> por si la generación falla.
        /// </summary>
        public void Prepare(string rootName)
        {
            GeneratedEnvironment previous = Object.FindFirstObjectByType<GeneratedEnvironment>(FindObjectsInactive.Include);
            _previous = previous != null ? previous.gameObject : null;
            if (_previous != null)
            {
                _previous.SetActive(false);
            }

            Root = new GameObject(rootName, typeof(GeneratedEnvironment));
            if (_previous != null)
            {
                Root.transform.SetSiblingIndex(_previous.transform.GetSiblingIndex());
            }
        }

        /// <summary>
        /// Deja lo generado listo para la escena: quita los colliders de los elementos decorativos, que solo
        /// servían para colocar otros elementos con rayos, y pasa todo a la capa Environment.
        /// </summary>
        public void Finish()
        {
            foreach (Collider collider in _decorativeColliders)
            {
                Object.DestroyImmediate(collider);
            }

            int layer = LayerMask.NameToLayer(EnvironmentLayer);
            foreach (Transform t in Root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }
        }

        /// <summary>Destruye la raíz anterior; la nueva pasa a ser el entorno de la escena.</summary>
        public void ReplacePrevious()
        {
            if (_previous != null)
            {
                Object.DestroyImmediate(_previous);
                _previous = null;
            }
        }

        /// <summary>Descarta la raíz nueva y las mallas que solo estaban en memoria, y reactiva la anterior.</summary>
        public void Discard()
        {
            foreach (Mesh mesh in _meshes)
            {
                if (mesh != null && !EditorUtility.IsPersistent(mesh))
                {
                    Object.DestroyImmediate(mesh);
                }
            }

            if (Root != null)
            {
                Object.DestroyImmediate(Root);
            }

            if (_previous != null)
            {
                _previous.SetActive(true);
            }
        }

        /// <summary>Crea un objeto vacío hijo de la raíz para agrupar elementos.</summary>
        public GameObject CreateGroup(string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(Root.transform, false);
            return group;
        }

        /// <summary>
        /// Crea un objeto estático con malla, materiales y collider de malla. Los decorativos (rótulos, placas,
        /// portadas) conservan el collider mientras se genera, para que los rayos los encuentren al colocar otros
        /// elementos, y lo pierden en <see cref="Finish"/>.
        /// </summary>
        public GameObject AddStatic(string name, Mesh mesh, Material[] materials, GameObject parent = null, bool decorative = false)
        {
            _meshes.Add(mesh);
            var go = new GameObject(name);
            go.transform.SetParent((parent != null ? parent : Root).transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            if (decorative)
            {
                _decorativeColliders.Add(collider);
            }

            GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        /// <summary>
        /// Construye la malla con las submallas usadas de <paramref name="builder"/> y crea con ella un objeto
        /// estático, como <see cref="AddStatic(string, Mesh, Material[], GameObject, bool)"/>.
        /// </summary>
        public GameObject AddStatic(string name, MeshBuilder builder, string meshName, Material[] slotMaterials,
            GameObject parent = null, bool decorative = false)
        {
            Mesh mesh = builder.BuildCompact(meshName, slotMaterials, out Material[] used);
            return AddStatic(name, mesh, used, parent, decorative);
        }

        /// <summary>
        /// Crea un objeto con malla que se mueve en juego (agujas del reloj): sin collider ni marcas de estático,
        /// para que el batching estático no lo congele. La malla se guarda con el resto.
        /// </summary>
        public GameObject AddMoving(string name, Mesh mesh, Material[] materials, GameObject parent)
        {
            _meshes.Add(mesh);
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }

        /// <summary>Añade una malla creada fuera de <see cref="AddStatic"/> para guardarla con las demás.</summary>
        public void RegisterMesh(Mesh mesh) => _meshes.Add(mesh);

        /// <summary>
        /// Impactos de un rayo contra lo generado, ordenados por distancia. Ignora lo colocado a mano en la escena,
        /// de modo que el resultado no depende del mobiliario.
        /// </summary>
        public RaycastHit[] Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            RaycastHit[] hits = System.Array.FindAll(Physics.RaycastAll(origin, direction, maxDistance), hit => IsGenerated(hit.collider));
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            return hits;
        }

        /// <summary>Impacto más cercano de un rayo contra lo generado.</summary>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
        {
            RaycastHit[] hits = Raycast(origin, direction, maxDistance);
            hit = hits.Length > 0 ? hits[0] : default;
            return hits.Length > 0;
        }

        /// <summary>Indica si una cápsula toca algo generado.</summary>
        public bool CheckCapsule(Vector3 start, Vector3 end, float radius)
        {
            foreach (Collider collider in Physics.OverlapCapsule(start, end, radius))
            {
                if (IsGenerated(collider))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Guarda las mallas generadas, agrupadas por el grupo de la raíz al que pertenecen (edificios, rótulos,
        /// cortinas de luz), como subrecursos de un asset por grupo. Sustituye los de la generación anterior.
        /// </summary>
        public void SaveMeshes()
        {
            var groups = new Dictionary<Mesh, string>();
            foreach (MeshFilter filter in Root.GetComponentsInChildren<MeshFilter>(true))
            {
                Transform group = filter.transform;
                while (group.parent != Root.transform)
                {
                    group = group.parent;
                }

                groups[filter.sharedMesh] = MeshAssetByGroup.TryGetValue(group.name, out string asset) ? asset : OtherMeshesAsset;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { ProjectPaths.GeneratedMeshes }))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }

            Directory.CreateDirectory(ProjectPaths.GeneratedMeshes);
            var created = new HashSet<string>();
            foreach (Mesh mesh in _meshes)
            {
                string path = $"{ProjectPaths.GeneratedMeshes}/{groups[mesh]}.asset";
                if (created.Add(path))
                {
                    AssetDatabase.CreateAsset(mesh, path);
                }
                else
                {
                    AssetDatabase.AddObjectToAsset(mesh, path);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private bool IsGenerated(Collider collider)
        {
            Transform t = collider.transform;
            return t.IsChildOf(Root.transform) || (AlsoGenerated != null && t.IsChildOf(AlsoGenerated));
        }
    }
}
