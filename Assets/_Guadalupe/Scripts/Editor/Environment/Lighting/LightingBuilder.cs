namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Luces de la plaza: genera las de los faroles de pared y las cortinas navideñas de los balcones, y asigna a
    /// los controladores de la escena las que van en prefabs (farolas, fuente, árbol y monasterio).
    /// </summary>
    internal static class LightingBuilder
    {
        /// <summary>Coloca y asigna las luces sobre el entorno ya generado.</summary>
        public static void Build(GenerationContext ctx)
        {
            StreetLightBuilder.Build(ctx);
            FloodLightBuilder.Configure(ctx);
            ChristmasBuilder.Build(ctx);
        }
    }
}
