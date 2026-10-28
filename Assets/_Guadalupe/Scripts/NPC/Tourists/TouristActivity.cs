namespace Guadalupe.NPC
{
    /// <summary>
    /// Lo que hace un turista parado, con el mismo valor que el parámetro <c>Activity</c> de su controlador de
    /// animación (<c>AC_Tourist</c>).
    /// </summary>
    public enum TouristActivity
    {
        /// <summary>Andando o colocándose: aún no hace nada.</summary>
        None = 0,
        LookAround = 1,
        Photo = 2,
        Point = 3,
        Talk = 4,
        Sit = 5,
        SitAndTalk = 6,
    }
}
