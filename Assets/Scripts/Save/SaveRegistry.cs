using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dónde se apuntan los elementos que se guardan por id.
///
/// Es estático y no un MonoBehaviour a posta. Los registros que van en la
/// escena dependen de que su Awake corra antes que el de quien se apunta, y
/// eso Unity no lo garantiza — fue justo el fallo de arranque que hubo que
/// arreglar en el WorkStationRegistry. Aquí no hay orden que respetar: existe
/// desde antes que cualquier escena.
///
/// Sigue el mismo patrón que <see cref="CoinBonusRegistry"/>: cada elemento se
/// apunta al despertar y se borra al destruirse, así que la lista siempre
/// refleja lo que hay puesto de verdad.
/// </summary>
public static class SaveRegistry
{
    private static readonly List<ISaveableUnlock> _unlocks = new();
    private static readonly List<ISaveableUpgrade> _upgrades = new();

    /// <summary>Ids ya ocupados, para cazar duplicados en cuanto aparecen.</summary>
    private static readonly HashSet<string> _ids = new();

    /// <summary>
    /// Entradas de la partida que no ha reclamado nadie.
    ///
    /// Se conservan y se vuelven a escribir tal cual. Un elemento puede estar
    /// desactivado ahora mismo, o haberse quitado de la escena mientras se
    /// trabaja en otra cosa; si se descartaran, el siguiente guardado borraría
    /// ese progreso para siempre y sin aviso.
    /// </summary>
    private static readonly List<UnlockSaveData> _orphanUnlocks = new();
    private static readonly List<UpgradeSaveData> _orphanUpgrades = new();

    // ── Alta y baja ──────────────────────────────────────────────────

    public static void Register(ISaveableUnlock element)
    {
        if (Accept(element)) _unlocks.Add(element);
    }

    public static void Register(ISaveableUpgrade element)
    {
        if (Accept(element)) _upgrades.Add(element);
    }

    public static void Unregister(ISaveableUnlock element)
    {
        if (element != null && _unlocks.Remove(element)) _ids.Remove(element.SaveId);
    }

    public static void Unregister(ISaveableUpgrade element)
    {
        if (element != null && _upgrades.Remove(element)) _ids.Remove(element.SaveId);
    }

    /// <summary>
    /// Si este elemento puede entrar. Rechaza a los que no tienen id y a los
    /// repetidos, siempre con un error que dice cuál es y dónde está: un
    /// elemento que no se guarda en silencio es una compra perdida que nadie
    /// nota hasta que es tarde.
    /// </summary>
    private static bool Accept(ISaveableElement element)
    {
        if (element == null || !element.SavesItself) return false;

        Object context = element as Object;

        if (string.IsNullOrEmpty(element.SaveId))
        {
            Debug.LogError(
                $"[SaveRegistry] {Describe(element)} no tiene id de guardado, así que " +
                "no se guardará. Abre la escena y ejecuta 'Taller > Revisar guardado'.",
                context);
            return false;
        }

        if (!_ids.Add(element.SaveId))
        {
            Debug.LogError(
                $"[SaveRegistry] {Describe(element)} repite el id '{element.SaveId}', " +
                "que ya tiene otro elemento. Este no se guardará. Suele pasar al " +
                "duplicar un objeto con Ctrl+D. Ejecuta 'Taller > Revisar guardado'.",
                context);
            return false;
        }

        return true;
    }

    // ── Guardar y restaurar ──────────────────────────────────────────

    /// <summary>
    /// Vuelca el estado de todos los elementos en la partida.
    ///
    /// Solo se escribe lo que se aparta de lo de fábrica: lo comprado y lo que
    /// pasa del nivel 1. Así una partida recién empezada no escribe ni una
    /// línea y el JSON se puede leer de un vistazo.
    /// </summary>
    public static void Capture(SaveData data)
    {
        data.unlocks.Clear();
        data.unlocks.AddRange(_orphanUnlocks);

        foreach (ISaveableUnlock element in _unlocks)
            if (element.IsUnlocked)
                data.unlocks.Add(new UnlockSaveData { id = element.SaveId, unlocked = true });

        data.upgrades.Clear();
        data.upgrades.AddRange(_orphanUpgrades);

        foreach (ISaveableUpgrade element in _upgrades)
            if (element.CurrentLevel > 1)
                data.upgrades.Add(new UpgradeSaveData { id = element.SaveId, level = element.CurrentLevel });
    }

    /// <summary>
    /// Aplica la partida a los elementos de la escena.
    ///
    /// Primero los desbloqueos y luego los niveles: hay mejoras que no aportan
    /// nada mientras su elemento siga bloqueado, así que cuando les toca subir
    /// de nivel conviene que su sala ya esté abierta.
    /// </summary>
    public static void Restore(SaveData data)
    {
        _orphanUnlocks.Clear();
        _orphanUpgrades.Clear();

        if (data == null) return;

        foreach (UnlockSaveData entry in data.unlocks)
        {
            if (!entry.unlocked) continue;

            ISaveableUnlock element = FindUnlock(entry.id);

            if (element == null) { _orphanUnlocks.Add(entry); continue; }

            element.RestoreUnlocked();
        }

        foreach (UpgradeSaveData entry in data.upgrades)
        {
            ISaveableUpgrade element = FindUpgrade(entry.id);

            if (element == null) { _orphanUpgrades.Add(entry); continue; }

            element.LoadLevel(entry.level);
        }

        int orphans = _orphanUnlocks.Count + _orphanUpgrades.Count;

        if (orphans > 0)
            Debug.LogWarning(
                $"[SaveRegistry] {orphans} entrada(s) de la partida no corresponden a ningún " +
                "elemento de la escena. Se conservan por si el elemento vuelve; si lo has " +
                "quitado a propósito, se pueden ignorar.");
    }

    private static ISaveableUnlock FindUnlock(string id)
    {
        foreach (ISaveableUnlock e in _unlocks)
            if (e.SaveId == id) return e;
        return null;
    }

    private static ISaveableUpgrade FindUpgrade(string id)
    {
        foreach (ISaveableUpgrade e in _upgrades)
            if (e.SaveId == id) return e;
        return null;
    }

    private static string Describe(ISaveableElement element)
    {
        Component component = element as Component;
        return component != null
            ? $"'{component.gameObject.name}' ({element.GetType().Name})"
            : element.GetType().Name;
    }

    /// <summary>
    /// Al ser estático sobrevive al Play si está desactivado el domain reload,
    /// y arrastraría los elementos de la partida anterior. Se limpia al
    /// arrancar, igual que <see cref="CoinBonusRegistry"/>.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _unlocks.Clear();
        _upgrades.Clear();
        _ids.Clear();
        _orphanUnlocks.Clear();
        _orphanUpgrades.Clear();
    }
}
