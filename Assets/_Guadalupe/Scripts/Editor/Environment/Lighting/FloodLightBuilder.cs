using System.Collections.Generic;
using Guadalupe.Ambience;
using Guadalupe.Core;
using UnityEditor;
using UnityEngine;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Iluminación monumental nocturna (<see cref="NightFloodLights"/>): asigna al controlador de la escena los focos
    /// del monasterio colocado y las luces sumergidas de la fuente, que vienen en sus prefabs.
    /// </summary>
    internal static class FloodLightBuilder
    {
        /// <summary>
        /// Asigna al <see cref="NightFloodLights"/> de la escena los focos del monasterio colocado y las luces de
        /// la fuente. Su intensidad plena es la del prefab, porque la de la escena la modifica el controlador según
        /// la hora.
        /// </summary>
        internal static void Configure(GenerationContext ctx)
        {
            var placed = new List<GameObject>(ctx.Scene.InstancesOf(PropPrefabs.Load().Fountain)) { ctx.Scene.PlacedMonastery };
            var lights = new List<Light>();
            var intensities = new List<float>();
            foreach (GameObject instance in placed)
            {
                foreach (Light light in instance.GetComponentsInChildren<Light>(true))
                {
                    lights.Add(light);
                    // Una luz añadida a la instancia no tiene original en el prefab: vale la suya.
                    Light source = PrefabUtility.GetCorrespondingObjectFromSource(light);
                    intensities.Add(source != null ? source.intensity : light.intensity);
                }
            }

            ctx.Scene.FloodLights.Configure(ctx.Scene.Sun, lights.ToArray(), intensities.ToArray());
            EditorUtility.SetDirty(ctx.Scene.FloodLights);
        }
    }
}
