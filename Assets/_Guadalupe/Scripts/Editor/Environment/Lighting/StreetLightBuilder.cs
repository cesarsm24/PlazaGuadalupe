using System.Collections.Generic;
using Guadalupe.Ambience;
using Guadalupe.Core;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Luces de los faroles cercanos a la plaza, que se encienden al anochecer (<see cref="StreetLights"/>).
    /// </summary>
    internal static class StreetLightBuilder
    {
        /// <summary>
        /// Luces puntuales en los faroles de pared más cercanos a la plaza, cálidas y sin sombras, que enciende
        /// <see cref="StreetLights"/> al ponerse el sol junto con las luces de las farolas.
        /// </summary>
        internal static void Build(GenerationContext ctx)
        {
            // Las farolas de la escena llevan su propia luz; cuentan para no poner otra en un farol de pared
            // demasiado cercano.
            var lampLights = new List<Light>();
            foreach (GameObject lamp in ctx.Scene.InstancesOf(PropPrefabs.Load().StreetLamp))
            {
                lampLights.AddRange(lamp.GetComponentsInChildren<Light>(true));
            }

            var spots = new List<Vector3>(ctx.Results.LanternPositions);
            spots.RemoveAll(p => new Vector2(p.x, p.z).magnitude > ctx.Data.Settings.StreetLightRadius);
            spots.Sort((a, b) => new Vector2(a.x, a.z).sqrMagnitude.CompareTo(new Vector2(b.x, b.z).sqrMagnitude));

            GameObject group = ctx.Output.CreateGroup("Luces");
            var placed = lampLights.ConvertAll(l => l.transform.position);
            foreach (Vector3 p in spots)
            {
                if (placed.Count >= ctx.Data.Settings.MaxStreetLights)
                {
                    break;
                }

                // Faroles muy próximos comparten luz.
                if (placed.Exists(q => (q - p).sqrMagnitude < 9f))
                {
                    continue;
                }

                placed.Add(p);
                var go = new GameObject(HierarchyNames.Numbered("LuzFarol", group.transform.childCount + 1));
                go.transform.SetParent(group.transform, false);
                go.transform.position = p;
                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 13f;
                light.color = new Color(1f, 0.74f, 0.42f);
                light.shadows = LightShadows.None;
                light.intensity = 0f;
                light.enabled = false;
            }

            lampLights.AddRange(group.GetComponentsInChildren<Light>(true));
            ctx.Scene.StreetLights.Configure(ctx.Scene.Sun, lampLights.ToArray(), ctx.Data.Materials.WindowLit, ctx.Data.Materials.LanternGlass,
                EmissiveRenderers(ctx, ctx.Data.Materials.WindowLit, ctx.Data.Materials.LanternGlass));
            EditorUtility.SetDirty(ctx.Scene.StreetLights);
        }

        /// <summary>Renderers de la escena, generados o colocados a mano, que usan alguno de los materiales.</summary>
        private static Renderer[] EmissiveRenderers(GenerationContext ctx, params Material[] materials)
        {
            var found = new List<Renderer>();
            foreach (GameObject sceneRoot in ctx.Output.Root.scene.GetRootGameObjects())
            {
                foreach (Renderer r in sceneRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (System.Array.Exists(r.sharedMaterials, m => m != null && System.Array.IndexOf(materials, m) >= 0))
                    {
                        found.Add(r);
                    }
                }
            }

            return found.ToArray();
        }
    }
}
