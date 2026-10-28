using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Genera el terreno a partir del modelo de elevaciones: calcula la forma del suelo (<see cref="TerrainShape"/>),
    /// rellena el mapa de alturas con rellanos horizontales bajo la fuente y lo pinta (<see cref="TerrainPainter"/>).
    /// </summary>
    internal static class TerrainBuilder
    {
        private const string TerrainDataPath = ProjectPaths.TerrainData;

        private const int HeightmapResolution = 257;

        /// <summary>Radio del rellano horizontal bajo la fuente (su andén mide 3,7 m de radio).</summary>
        private const float FountainPadRadius = 4.4f;

        /// <summary>Distancia en la que los rellanos se funden con la pendiente de la plaza.</summary>
        private const float PadBlend = 4f;

        /// <summary>Rellano ante el arco de la lonja: distancia de su centro a la bóveda y radio.</summary>
        private const float ArchPadSetback = 1.6f;
        private const float ArchPadRadius = 2.2f;

        /// <summary>
        /// Crea el terreno y lo añade a la raíz del entorno: ajusta la plaza a un plano, calcula la forma del
        /// suelo en cada punto, rellena el mapa de alturas y pinta las capas de textura. Sus datos quedan en
        /// memoria hasta <see cref="Save"/>, para que una generación fallida no toque el recurso del proyecto.
        /// </summary>
        public static Terrain Build(GenerationContext ctx)
        {
            DemGrid dem = ctx.Data.Dem;
            TerrainData data = CreateTerrainData(dem);

            PlazaSurface plaza = PlazaSurface.Fit(ctx);
            float stairsFoot = TerrainShape.StairsFootElevation(dem, plaza);

            // Las elevaciones del terreno son altitudes; las alturas del mundo se miden desde el origen.
            ctx.Results.PlanDatum = stairsFoot - ctx.Data.OriginElevation - AtriumPlan.StairsFoot;

            System.Func<Vector2, float> shape = TerrainShape.GroundShape(ctx, plaza, stairsFoot);
            List<FlatPad> pads = FlatPads(ctx, shape);
            data.SetHeights(0, 0, SampleHeights(dem, shape, pads));
            data.terrainLayers = new[] { ctx.Data.Materials.GroundLayer, ctx.Data.Materials.StreetLayer, ctx.Data.Materials.PavingLayer, ctx.Data.Materials.PavingAltLayer };
            TerrainPainter.Paint(ctx, data);
            return CreateTerrainObject(ctx, data);
        }

        /// <summary>
        /// Copia el terreno generado en <c>TD_Guadalupe</c> y lo asigna al objeto. Se reutiliza el recurso
        /// existente para conservar su GUID; los mapas de mezcla son subrecursos que Unity crea al pintar un
        /// <see cref="TerrainData"/> que ya es recurso, por eso no se guarda directamente el de memoria.
        /// </summary>
        public static void Save(Terrain terrain)
        {
            TerrainData built = terrain.terrainData;
            TerrainData asset = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (asset == null)
            {
                asset = new TerrainData();
                AssetDatabase.CreateAsset(asset, TerrainDataPath);
            }

            asset.name = built.name;
            asset.heightmapResolution = built.heightmapResolution;
            asset.alphamapResolution = built.alphamapResolution;
            asset.size = built.size;
            asset.SetHeights(0, 0, built.GetHeights(0, 0, built.heightmapResolution, built.heightmapResolution));
            asset.terrainLayers = built.terrainLayers;
            asset.SetAlphamaps(0, 0, built.GetAlphamaps(0, 0, built.alphamapResolution, built.alphamapResolution));
            EditorUtility.SetDirty(asset);

            terrain.terrainData = asset;
            AssignCollider(terrain.GetComponent<TerrainCollider>(), asset);
            Object.DestroyImmediate(built);
        }

        /// <summary>Crea en memoria el <see cref="TerrainData"/> con el tamaño y la resolución del modelo de elevaciones.</summary>
        private static TerrainData CreateTerrainData(DemGrid dem)
        {
            var data = new TerrainData { name = "TD_Guadalupe" };
            data.heightmapResolution = HeightmapResolution;
            data.alphamapResolution = TerrainPainter.AlphamapResolution;
            data.size = new Vector3(dem.SizeMeters, Mathf.Max(1f, dem.MaxHeight - dem.MinHeight), dem.SizeMeters);
            return data;
        }

        /// <summary>Rellanos horizontales bajo la fuente y ante el arco, donde la pendiente de la plaza sería visible.</summary>
        private static List<FlatPad> FlatPads(GenerationContext ctx, System.Func<Vector2, float> shape)
        {
            var pads = new List<FlatPad>();
            if (ctx.Data.Osm.Fountains.Count > 0)
            {
                Vector2 fountain = ctx.Data.Osm.Fountains[0];
                foreach (Vector2 f in ctx.Data.Osm.Fountains)
                {
                    fountain = f.sqrMagnitude < fountain.sqrMagnitude ? f : fountain;
                }

                pads.Add(new FlatPad(fountain, FountainPadRadius, PadBlend, shape(fountain)));
            }

            Vector2 archEntry = PlanFrame.ToWorld(AtriumPlan.ArchCenter, AtriumPlan.VaultFront - ArchPadSetback);
            pads.Add(new FlatPad(archEntry, ArchPadRadius, PadBlend, shape(archEntry)));
            return pads;
        }

        /// <summary>
        /// Mapa de alturas: la elevación de cada punto de la rejilla, normalizada entre la mínima y la máxima del
        /// modelo como pide <see cref="TerrainData.SetHeights"/>.
        /// </summary>
        private static float[,] SampleHeights(DemGrid dem, System.Func<Vector2, float> shape, List<FlatPad> pads)
        {
            float range = Mathf.Max(1f, dem.MaxHeight - dem.MinHeight);
            float half = dem.SizeMeters * 0.5f;
            var heights = new float[HeightmapResolution, HeightmapResolution];
            for (int iz = 0; iz < HeightmapResolution; iz++)
            {
                for (int ix = 0; ix < HeightmapResolution; ix++)
                {
                    float x = -half + ix * dem.SizeMeters / (HeightmapResolution - 1);
                    float z = -half + iz * dem.SizeMeters / (HeightmapResolution - 1);
                    var p = new Vector2(x, z);
                    float elevation = shape(p);
                    foreach (FlatPad pad in pads)
                    {
                        elevation = pad.Apply(p, elevation);
                    }

                    heights[iz, ix] = (elevation - dem.MinHeight) / range;
                }
            }

            return heights;
        }

        /// <summary>Crea el objeto de terreno centrado en el origen y con su collider actualizado.</summary>
        private static Terrain CreateTerrainObject(GenerationContext ctx, TerrainData data)
        {
            DemGrid dem = ctx.Data.Dem;
            float half = dem.SizeMeters * 0.5f;
            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terreno";
            go.transform.SetParent(ctx.Output.Root.transform, false);
            go.transform.position = new Vector3(-half, dem.MinHeight - ctx.Data.OriginElevation, -half);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

            AssignCollider(go.GetComponent<TerrainCollider>(), data);

            // Más detalle de malla cerca de la cámara: la plaza se ve a ras de suelo.
            var terrain = go.GetComponent<Terrain>();
            terrain.heightmapPixelError = 2f;
            terrain.basemapDistance = 250f;
            return terrain;
        }

        /// <summary>
        /// Asigna los datos al collider forzando a reconstruirlo: si no, puede quedarse con el campo de alturas
        /// anterior y la física (y la NavMesh) no verían las alturas nuevas.
        /// </summary>
        private static void AssignCollider(TerrainCollider collider, TerrainData data)
        {
            collider.terrainData = null;
            collider.terrainData = data;
            Physics.SyncTransforms();
        }

        /// <summary>Rellano horizontal circular que se funde con el terreno de alrededor.</summary>
        private readonly struct FlatPad
        {
            private readonly Vector2 _center;
            private readonly float _radius;
            private readonly float _blend;
            private readonly float _level;

            public FlatPad(Vector2 center, float radius, float blend, float level)
            {
                _center = center;
                _radius = radius;
                _blend = blend;
                _level = level;
            }

            public float Apply(Vector2 p, float elevation)
            {
                float d = Vector2.Distance(p, _center) - _radius;
                return d >= _blend ? elevation : Mathf.Lerp(_level, elevation, Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, d) / _blend));
            }
        }
    }
}
