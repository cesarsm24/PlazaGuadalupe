using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Lo que comparten los constructores durante una generación del entorno, separado por su papel: los datos de
    /// entrada, lo colocado en la escena, la raíz que se genera y los resultados intermedios, más el terreno, del
    /// que todos leen la altura del suelo.
    /// </summary>
    internal sealed class GenerationContext
    {
        public GenerationData Data { get; private set; }

        public SceneServices Scene { get; private set; }

        public GeneratedRoot Output { get; } = new GeneratedRoot();

        public GenerationResults Results { get; } = new GenerationResults();

        /// <summary>Terreno ya creado (null hasta que se genera).</summary>
        public Terrain Terrain { get; set; }

        /// <summary>Lee los datos de entrada y localiza lo colocado en la escena.</summary>
        /// <exception cref="System.InvalidOperationException">Si falta algo en la escena.</exception>
        public void Load()
        {
            Data = GenerationData.Load();
            Scene = SceneServices.Find();
        }

        /// <summary>
        /// Localiza el monasterio colocado; su atrio cuenta como generado al lanzar rayos, porque algunas casas se
        /// apoyan en él.
        /// </summary>
        public void UsePlacedMonastery()
        {
            Scene.FindPlacedMonastery();
            Output.AlsoGenerated = Scene.PlacedAtrium;
        }

        /// <summary>Altura local del terreno en un punto (antes de crear el terreno usa el MDE).</summary>
        public float GroundY(float x, float z)
        {
            if (Terrain != null)
            {
                return Terrain.SampleHeight(new Vector3(x, 0f, z)) + Terrain.transform.position.y;
            }

            return Data.Dem.Sample(x, z) - Data.OriginElevation;
        }

        /// <summary>Altura mínima y máxima del terreno bajo los vértices de un polígono.</summary>
        public void GroundRange(IReadOnlyList<Vector2> polygon, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            foreach (Vector2 p in polygon)
            {
                float y = GroundY(p.x, p.y);
                min = Mathf.Min(min, y);
                max = Mathf.Max(max, y);
            }
        }

        /// <summary>
        /// Descarta lo generado que solo estaba en memoria (terreno y mallas) y reactiva el entorno anterior. Se usa
        /// si la generación falla y al terminar una construcción de trabajo.
        /// </summary>
        public void Discard()
        {
            if (Terrain != null && !EditorUtility.IsPersistent(Terrain.terrainData))
            {
                Object.DestroyImmediate(Terrain.terrainData);
            }

            Output.Discard();
        }
    }
}
