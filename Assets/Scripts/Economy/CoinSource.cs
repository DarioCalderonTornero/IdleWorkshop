/// <summary>
/// De dónde vienen unas monedas.
///
/// Importa porque la producción offline no se simula, se mide: se lleva la
/// cuenta de lo que el jugador gana por segundo mientras juega y esa tasa es la
/// que se le paga por el rato que ha estado fuera.
///
/// Si todo lo que entra contara para esa tasa, un ingreso puntual la
/// dispararía. Un anuncio de 50.000 monedas visto justo antes de cerrar haría
/// que el juego pagara 24 horas calculadas sobre ese pico. No es un bug que se
/// arregle luego: es un exploit, y por eso se distingue desde el primer día.
/// </summary>
public enum CoinSource
{
    /// <summary>
    /// Producción del taller: mesas, trabajadores de sala, lo que el montaje
    /// del jugador genera solo. Es lo único que cuenta para la tasa.
    /// </summary>
    Production,

    /// <summary>
    /// Ingresos puntuales que no representan producción: recompensas por
    /// anuncio, regalos, misiones, la propia paga offline y las teclas de
    /// pruebas. No cuentan.
    /// </summary>
    OneOff,

    /// <summary>
    /// Fuentes que sabrán calcular ellas mismas lo que produjeron estando el
    /// juego cerrado, en vez de estimarse con la tasa.
    ///
    /// Todavía no hay ninguna. Existe para que el día que la haya no cuente
    /// dos veces: una en su cálculo exacto y otra dentro de la tasa medida.
    /// </summary>
    SelfAccounted,
}
