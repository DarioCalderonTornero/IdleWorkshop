using UnityEngine;

/// <summary>
/// Si el juego todavía se está montando.
///
/// Arrancar y aplicar la partida guardada enciende y apaga medio taller
/// —salas por comprar, piezas de decoración, sacos de terminados— y nada de
/// eso debe animarse: es el estado con el que empieza la partida, no cosas que
/// acaben de pasar. Sin esto, una sala restaurada del guardado soltaría un puf
/// como si el jugador la acabara de comprar.
///
/// Quien aparece con animación pregunta aquí:
///
///   poof.SetVisible(visible, animate: !BootPhase.IsBooting);
///
/// Es un solo interruptor y no un flag por componente a propósito: antes había
/// cuatro copias privadas de esta misma idea, cada una decidiéndolo en su
/// propio Start(), y con la carga de partida de por medio no coincidían —que
/// el Almacén soltara un puf al cargar o no dependía del orden en que Unity
/// llamara a los Start(), que es arbitrario.
///
/// Lo apaga la fase de carga, que es la última en correr
/// (ver <see cref="BootOrder.Load"/>): el arranque termina cuando la partida
/// está aplicada, no en un frame concreto. Si algún día la carga pasa a ser
/// asíncrona, End() se mueve al final de ese proceso y todo lo demás sigue
/// igual.
/// </summary>
public static class BootPhase
{
    /// <summary>
    /// True mientras se monta la escena y se aplica la partida guardada.
    /// </summary>
    public static bool IsBooting { get; private set; } = true;

    /// <summary>
    /// El arranque ha terminado. Lo llama el SaveManager al acabar de cargar.
    /// </summary>
    public static void End() => IsBooting = false;

    /// <summary>
    /// Al ser estático sobrevive al Play si está desactivado el domain reload,
    /// y la segunda partida arrancaría creyendo que ya está montada. Se
    /// reinicia antes de que cargue la escena, o sea antes de cualquier Awake.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetOnPlay() => IsBooting = true;
}
