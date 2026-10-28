namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Aspecto de una casa de la plaza: colores, zócalo, carpintería y elementos singulares. Se identifica por
    /// la referencia catastral de la finca (sus siete primeros caracteres).
    /// </summary>
    public sealed class HouseStyle
    {
        /// <summary>Referencia catastral de la finca (siete primeros caracteres).</summary>
        public string CadastralRef { get; set; } = string.Empty;

        /// <summary>Número de la plaza o dirección (solo informativo).</summary>
        public string Address { get; set; } = string.Empty;

        public WallFinish Wall { get; set; } = WallFinish.White;

        public ZocaloFinish Zocalo { get; set; } = ZocaloFinish.Grey;

        /// <summary>Altura del zócalo; la sillería pintada ocupa toda la planta baja.</summary>
        public float ZocaloHeight { get; set; } = 0.9f;

        /// <summary>Carpintería y contraventanas verdes en lugar de madera.</summary>
        public bool GreenShutters { get; set; }

        /// <summary>Probabilidad de contraventanas en las ventanas (0 = ninguna).</summary>
        public float ShutterChance { get; set; } = 0.45f;

        /// <summary>Probabilidad de balcón en las ventanas de la primera planta hacia la plaza.</summary>
        public float BalconyChance { get; set; } = 0.5f;

        /// <summary>Esquinas de sillería gris en las aristas de la fachada.</summary>
        public bool Quoins { get; set; }

        /// <summary>Recercados de cantería en los huecos, con frontón sobre las ventanas altas.</summary>
        public bool StoneSurrounds { get; set; }

        /// <summary>Balcón corrido de madera en la primera planta hacia la calle.</summary>
        public bool WoodenGallery { get; set; }

        /// <summary>Azotea con balaustrada de piedra en lugar de tejado.</summary>
        public bool Parapet { get; set; }

        /// <summary>Soportal de arcos de medio punto sobre pilares de piedra (no adintelado).</summary>
        public bool ArchedArcade { get; set; }

        /// <summary>Rótulo del comercio de la planta baja (null si no tiene).</summary>
        public string Sign { get; set; }

        public SignFont Font { get; set; }

        /// <summary>Segundo rótulo, al lado del primero (null si no tiene).</summary>
        public string Sign2 { get; set; }

        /// <summary>
        /// Metros que se desplazan los rótulos desde su sitio a lo largo de la fachada, hacia la derecha de quien la
        /// mira desde la calle (negativo: hacia la izquierda).
        /// </summary>
        public float SignShift { get; set; }

        /// <summary>Portada singular de la planta baja hacia la plaza.</summary>
        public PortalKind Portal { get; set; }


        /// <summary>Tejaroz de teja sobre canes encima de la planta baja, en lugar de toldos.</summary>
        public bool Tejaroz { get; set; }

        /// <summary>En el fondo del soportal, puertas y ventanas de vivienda en lugar de escaparates.</summary>
        public bool ArcadeDoors { get; set; }

        /// <summary>Plantas sobre rasante, cuando difieren de las que registra el Catastro.</summary>
        public int? Floors { get; set; }
    }

    /// <summary>Acabado del muro de una casa.</summary>
    public enum WallFinish
    {
        /// <summary>Encalado blanco.</summary>
        White,

        /// <summary>Pintura crema.</summary>
        Cream,

        /// <summary>Pintura ocre amarilla.</summary>
        Ochre,

        /// <summary>Pintura salmón (rosa anaranjado).</summary>
        Salmon,

        /// <summary>Sillería de granito dorado vista.</summary>
        Ashlar,
    }

    /// <summary>Acabado del zócalo de una casa.</summary>
    public enum ZocaloFinish
    {
        /// <summary>Gris azulado, el más frecuente en la plaza.</summary>
        Grey,

        /// <summary>Marrón oscuro.</summary>
        Brown,

        /// <summary>Rojo almagre.</summary>
        Red,

        /// <summary>Ocre, del mismo tono que el muro.</summary>
        Ochre,

        /// <summary>Ladrillo visto.</summary>
        Brick,

        /// <summary>Sillería pintada en gris con juntas blancas.</summary>
        PaintedAshlar,
    }

    /// <summary>Tipografía de un rótulo.</summary>
    public enum SignFont
    {
        /// <summary>Letra de palo seco.</summary>
        Plain,

        /// <summary>Letra gótica (mesones y posadas).</summary>
        Gothic,

        /// <summary>Letra pintada a mano (tiendas de productos típicos).</summary>
        Hand,

        /// <summary>Capitales clásicas (restaurantes).</summary>
        Roman,
    }

    /// <summary>Portada singular de la planta baja.</summary>
    public enum PortalKind
    {
        /// <summary>Sin portada singular.</summary>
        None,

        /// <summary>Puerta de madera bajo arco de medio punto con rosca de piedra.</summary>
        RoundArch,

        /// <summary>Arco de medio punto con arquivoltas labradas en piedra dorada.</summary>
        Archivolts,

        /// <summary>Arco de medio punto enmarcado por un alfiz labrado.</summary>
        Alfiz,
    }
}
