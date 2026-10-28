using System.Collections.Generic;
using Guadalupe.Editor.Environment;
using UnityEngine;

namespace Guadalupe.Editor.MonasteryExport
{
    /// <summary>
    /// Focos nocturnos del modelo del monasterio: proyectores al pie de la fachada, de las torres y de los muros que
    /// dan a espacios abiertos, que los bañan de abajo arriba. La fachada gótica es la que más se ilumina, con una luz
    /// algo más blanca; las torres y los muros quedan en penumbra cálida.
    /// </summary>
    internal static class MonasteryFloodLights
    {
        private static readonly Color Warm = new Color(1f, 0.8f, 0.58f);           // ~3000 K
        private static readonly Color FacadeWhite = new Color(1f, 0.87f, 0.72f);   // ~3500 K

        private const float FacadeIntensity = 240f;
        private const float TowerIntensity = 120f;
        private const float WallIntensity = 90f;

        private const float Setback = 2.6f;      // Distancia de los proyectores a la fachada.
        private const float WallSpacing = 5.5f;
        private const float WallRadius = 80f;    // Solo se iluminan los muros a menos de esto de la plaza.

        /// <summary>Focos de la fachada, de la torre del reloj y de los muros del monasterio, para su modelo.</summary>
        internal static GameObject Build(GenerationContext ctx)
        {
            GameObject group = ctx.Output.CreateGroup(MonasteryPrefabNodes.FloodLights);

            // Torre de la portería y fachada gótica (X en el marco de la fachada; la torre empieza en −8,5).
            foreach (float x in new[] { -6.4f, -2.1f })
            {
                AddFacadeSpot(ctx, group, AtriumPlan.FacadeToPlan(new Vector2(x, -Setback)), AtriumPlan.FacadeToPlan(new Vector2(x, 1f)), TowerIntensity, Warm);
            }

            foreach (float x in new[] { 3.2f, 8.8f, 14.2f })
            {
                AddFacadeSpot(ctx, group, AtriumPlan.FacadeToPlan(new Vector2(x, -Setback)), AtriumPlan.FacadeToPlan(new Vector2(x, 1f)), FacadeIntensity, FacadeWhite);
            }

            // Torre del reloj, al este de la fachada.
            Vector3 tower = AtriumPlan.EastTower;
            foreach (float t in new[] { 0.28f, 0.72f })
            {
                float u = Mathf.Lerp(tower.x, tower.y, t);
                AddFacadeSpot(ctx, group, new Vector2(u, tower.z - Setback), new Vector2(u, tower.z), TowerIntensity, Warm);
            }

            AddAroundMonastery(ctx, group);
            return group;
        }

        /// <summary>
        /// Proyectores a lo largo de los muros del monasterio (su contorno y las partes del ala oeste) que dan a
        /// un espacio abierto: uno cada <see cref="WallSpacing"/> m, sobre el suelo transitable que haya delante y
        /// solo si el muro está justo enfrente; sin sombras, que ya las dan los de la fachada.
        /// </summary>
        private static void AddAroundMonastery(GenerationContext ctx, GameObject group)
        {
            Physics.SyncTransforms();
            var placed = new List<Vector3>();
            foreach (Transform spot in group.transform)
            {
                placed.Add(spot.position);
            }

            foreach (List<Vector2> wall in MonasteryWalls(ctx))
            {
                for (int i = 0; i < wall.Count; i++)
                {
                    Vector2 a = wall[i];
                    Vector2 b = wall[(i + 1) % wall.Count];
                    float length = Vector2.Distance(a, b);
                    if (length < 3f)
                    {
                        continue;
                    }

                    Vector2 dir = (b - a) / length;
                    var normal = new Vector2(dir.y, -dir.x);
                    if (PolygonUtils.Contains(wall, (a + b) * 0.5f + normal * 0.5f))
                    {
                        normal = -normal;
                    }

                    int count = Mathf.Max(1, Mathf.RoundToInt(length / WallSpacing));
                    for (int k = 0; k < count; k++)
                    {
                        Vector2 station = a + dir * (length * (k + 0.5f) / count) + normal * Setback;
                        if (TryPlaceFacingWall(ctx, station, normal, placed, out Vector3 position, out Vector3 target))
                        {
                            placed.Add(position);
                            AddSpot(group, position, target, false, WallIntensity, Warm);
                        }
                    }
                }
            }
        }

        /// <summary>Contorno del monasterio y partes catastrales del monasterio cercanas a la plaza.</summary>
        private static List<List<Vector2>> MonasteryWalls(GenerationContext ctx)
        {
            var walls = new List<List<Vector2>> { ctx.Data.Osm.MonasteryOutline };
            foreach (BuildingFootprint part in ctx.Data.Buildings)
            {
                if (part.BuildingId == PlazaLayout.MonasteryCadastralRef && PolygonUtils.Centroid(part.Footprint).magnitude < WallRadius)
                {
                    walls.Add(part.Footprint);
                }
            }

            return walls;
        }

        /// <summary>
        /// Busca el muro real delante de una estación (puede quedar detrás del contorno) y coloca el proyector a
        /// <see cref="Setback"/> de él, si hay suelo y no hay otro proyector a menos de 4 m.
        /// </summary>
        private static bool TryPlaceFacingWall(GenerationContext ctx, Vector2 station, Vector2 normal, List<Vector3> placed,
            out Vector3 position, out Vector3 target)
        {
            position = target = default;
            if (station.magnitude > WallRadius || !GroundInFront(ctx, station, out float probeFloor))
            {
                return false;
            }

            var probe = new Vector3(station.x, probeFloor + 3f, station.y);
            if (!FindWall(ctx, probe, new Vector3(-normal.x, 0f, -normal.y), out Vector3 onWall, out Vector3 wallNormal))
            {
                return false;
            }

            Vector3 foot3 = onWall + wallNormal * Setback;
            var foot = new Vector2(foot3.x, foot3.z);
            if (!GroundInFront(ctx, foot, out float floor))
            {
                return false;
            }

            var candidate = new Vector3(foot.x, floor + 0.15f, foot.y);
            if (placed.Exists(q => new Vector2(q.x - candidate.x, q.z - candidate.z).sqrMagnitude < 16f))
            {
                return false;
            }

            position = candidate;
            target = new Vector3(onWall.x, floor + 14f, onWall.z);
            return true;
        }

        /// <summary>Primer muro generado (no el terreno ni un suelo) a menos de 12 m en horizontal.</summary>
        private static bool FindWall(GenerationContext ctx, Vector3 from, Vector3 direction, out Vector3 point, out Vector3 normal)
        {
            foreach (RaycastHit hit in ctx.Output.Raycast(from, direction, 12f))
            {
                if (hit.collider is TerrainCollider || Mathf.Abs(hit.normal.y) > 0.3f)
                {
                    continue;
                }

                point = hit.point;
                normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                return true;
            }

            point = normal = Vector3.zero;
            return false;
        }

        /// <summary>Suelo transitable (calle, plaza o atrio, no un tejado) en un punto, con sitio para estar de pie.</summary>
        private static bool GroundInFront(GenerationContext ctx, Vector2 p, out float floor)
        {
            floor = 0f;
            Terrain terrain = ctx.Terrain;
            float ground = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
            foreach (RaycastHit hit in ctx.Output.Raycast(new Vector3(p.x, ground + 40f, p.y), Vector3.down, 60f))
            {
                if (hit.normal.y < 0.7f || hit.point.y > ground + 6f)
                {
                    continue;
                }

                if (!ctx.Output.CheckCapsule(hit.point + Vector3.up * 0.5f, hit.point + Vector3.up * 1.6f, 0.3f))
                {
                    floor = hit.point.y;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Proyector en el suelo que apunta hacia arriba, a lo alto del paño que tiene delante.</summary>
        private static void AddFacadeSpot(GenerationContext ctx, GameObject group, Vector2 planFoot, Vector2 planWall, float intensity, Color color)
        {
            float floor = PlanFrame.Y(ctx, AtriumPlan.AtriumLevel(planFoot));
            Vector2 foot = PlanFrame.ToWorld(planFoot);
            Vector2 wall = PlanFrame.ToWorld(planWall);
            AddSpot(group, new Vector3(foot.x, floor + 0.15f, foot.y), new Vector3(wall.x, floor + 16f, wall.y), true, intensity, color);
        }

        private static void AddSpot(GameObject group, Vector3 position, Vector3 target, bool shadows, float intensity, Color color)
        {
            var go = new GameObject(HierarchyNames.Numbered("Proyector", group.transform.childCount + 1));
            go.transform.SetParent(group.transform, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            Light light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 62f;
            light.innerSpotAngle = 25f;
            light.range = 34f;
            light.intensity = intensity;
            light.color = color;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.8f;
        }
    }
}
