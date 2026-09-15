using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monedas extra que se suman a cada cobro del taller, vengan de donde vengan.
///
/// Es un registro global y no un campo de la WorkStation porque el bonus lo dan
/// cosas que están fuera del taller —los asientos del hall— y afecta a todos
/// los objetos en todas las mesas.
///
/// Cada fuente apunta aquí su aportación y la retira al destruirse, así que el
/// total siempre refleja lo que hay puesto en la escena.
/// </summary>
public static class CoinBonusRegistry
{
    private static readonly Dictionary<Object, int> _sources = new();

    /// <summary>Monedas extra por cada parte del proceso que paga.</summary>
    public static int FlatPerProcess { get; private set; }

    public static void Set(Object source, int bonus)
    {
        if (source == null) return;

        _sources[source] = Mathf.Max(0, bonus);
        Recalculate();
    }

    public static void Remove(Object source)
    {
        if (source == null) return;

        if (_sources.Remove(source))
            Recalculate();
    }

    private static void Recalculate()
    {
        int total = 0;
        foreach (int bonus in _sources.Values) total += bonus;

        FlatPerProcess = total;
    }

    /// <summary>
    /// Al ser estático sobrevive al Play si está desactivado el domain reload,
    /// y arrastraría bonus de la partida anterior. Se limpia al arrancar.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _sources.Clear();
        FlatPerProcess = 0;
    }
}
