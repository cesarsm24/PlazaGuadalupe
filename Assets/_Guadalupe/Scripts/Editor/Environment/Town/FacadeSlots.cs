namespace Guadalupe.Editor.Environment
{
    /// <summary>Índices de submalla que usa el detallado de fachadas.</summary>
    internal readonly struct FacadeSlots
    {
        public FacadeSlots(int zocalo, int window, int door, int metal, int glass, int awningRed, int awningGreen, int stone, int frame,
            int pot = -1, int foliage = -1, int windowLit = -1)
        {
            WindowLit = windowLit;
            Pot = pot;
            Foliage = foliage;
            Frame = frame;
            Zocalo = zocalo;
            Window = window;
            Door = door;
            Metal = metal;
            Glass = glass;
            AwningRed = awningRed;
            AwningGreen = awningGreen;
            Stone = stone;
        }

        public int Zocalo { get; }

        /// <summary>Hueco oscuro de las ventanas.</summary>
        public int Window { get; }

        public int Door { get; }

        /// <summary>Rejas, balcones y faroles de hierro.</summary>
        public int Metal { get; }

        /// <summary>Cristal de los faroles de pared.</summary>
        public int Glass { get; }

        public int AwningRed { get; }

        public int AwningGreen { get; }

        /// <summary>Piedra de recercados y alféizares.</summary>
        public int Stone { get; }

        /// <summary>Carpintería: marcos y contraventanas.</summary>
        public int Frame { get; }

        /// <summary>Cristal de ventana que se ilumina de noche (-1 si no hay).</summary>
        public int WindowLit { get; }

        /// <summary>Barro de las macetas (-1 si no hay).</summary>
        public int Pot { get; }

        /// <summary>Hojas de las plantas (-1 si no hay).</summary>
        public int Foliage { get; }
    }
}
