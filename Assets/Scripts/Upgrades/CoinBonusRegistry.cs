using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monedas extra que se suman a cada cobro del taller.
///
/// Es un registro y no un campo de la WorkStation porque el bonus lo dan cosas
/// que están fuera de las mesas —los asientos del hall, las decoraciones, la
/// habitación— y afecta a todos los objetos en todas las mesas.
///
/// Cada fuente cuenta solo en su propio taller. Antes el total era uno para
/// toda la escena, y con dos talleres las decoraciones del primero habrían
/// pagado también en el segundo, que se supone que empieza de cero. Por eso se
/// pregunta con <see cref="FlatPerProcessFor"/>, diciendo quién cobra.
///
/// Cada fuente apunta aquí su aportación y la retira al destruirse, así que el
/// total siempre refleja lo que hay puesto en la escena.
/// </summary>
public static class CoinBonusRegistry
{
    private readonly struct Entry
    {
        public readonly Workshop Workshop;
        public readonly int Bonus;

        public Entry(Workshop workshop, int bonus)
        {
            Workshop = workshop;
            Bonus = bonus;
        }
    }

    private static readonly Dictionary<Component, Entry> _sources = new();

    /// <summary>
    /// Lo que aporta <paramref name="source"/>. Su taller se apunta ahora, al
    /// registrarse, y no en cada cobro: una fuente no cambia de taller.
    /// </summary>
    public static void Set(Component source, int bonus)
    {
        if (source == null) return;

        _sources[source] = new Entry(Workshop.Of(source), Mathf.Max(0, bonus));
    }

    public static void Remove(Component source)
    {
        if (source == null) return;

        _sources.Remove(source);
    }

    /// <summary>
    /// Monedas extra por cada parte del proceso que paga, para quien cobra en
    /// el taller de <paramref name="payer"/>.
    ///
    /// Lo que no está en ningún taller —una escena de pruebas— suma con lo que
    /// tampoco lo está, que es lo que hacía todo antes de haber varios.
    /// </summary>
    public static int FlatPerProcessFor(Component payer)
    {
        Workshop workshop = Workshop.Of(payer);

        int total = 0;
        foreach (Entry entry in _sources.Values)
            if (entry.Workshop == workshop) total += entry.Bonus;

        return total;
    }

    /// <summary>
    /// El total de todos los talleres juntos. No lo usa ningún cobro —cada uno
    /// usa el de su taller—; queda para pruebas y para enseñarlo en conjunto.
    /// </summary>
    public static int FlatPerProcessAllWorkshops
    {
        get
        {
            int total = 0;
            foreach (Entry entry in _sources.Values) total += entry.Bonus;
            return total;
        }
    }

    /// <summary>
    /// Al ser estático sobrevive al Play si está desactivado el domain reload,
    /// y arrastraría bonus de la partida anterior. Se limpia al arrancar.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => _sources.Clear();
}
