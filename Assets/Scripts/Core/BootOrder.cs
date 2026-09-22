/// <summary>
/// Las capas de arranque del juego, en un solo sitio.
///
/// Unity no garantiza el orden de Awake() entre GameObjects distintos: sin
/// esto, que un manager encuentre vivo a otro depende del orden en que estén
/// serializados en la escena, y basta reordenar la jerarquía para romper el
/// arranque sin que nada lo avise.
///
/// Cada capa solo puede depender de las anteriores:
///
///   Registry -> Manager -> World -> Load
///
/// Un componente nuevo declara la suya con el atributo y ya está; no hay
/// ninguna lista global que mantener ni configuración fuera del código:
///
///   [DefaultExecutionOrder(BootOrder.Manager)]
///   public class MiManager : MonoBehaviour { ... }
/// </summary>
public static class BootOrder
{
    /// <summary>
    /// Las listas donde el resto se apunta. No dependen de nadie, así que
    /// existen antes que nadie las use.
    /// </summary>
    public const int Registry = -200;

    /// <summary>
    /// Servicios globales: economía, clientes, input, guardado... Pueden usar
    /// los registros ya en su Awake.
    /// </summary>
    public const int Manager = -100;

    /// <summary>
    /// Lo que hay montado en el taller: mesas, trabajadores, carritos,
    /// mostradores. Es el valor por defecto, así que no hace falta declararlo.
    /// </summary>
    public const int World = 0;

    /// <summary>
    /// La partida guardada se aplica la última, cuando la escena ya está
    /// montada entera y no queda ningún Start() por correr que pueda pisarla.
    /// </summary>
    public const int Load = 100;

    /// <summary>
    /// Lo que necesita la partida ya aplicada: la paga por el tiempo offline,
    /// el aviso de bienvenida. Va después de Load porque no puede calcular
    /// nada hasta que el guardado se ha leído.
    /// </summary>
    public const int PostLoad = 110;
}
