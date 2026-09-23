using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lleva la cuenta de qué talleres ha comprado el jugador, deja comprar el
/// siguiente y sabe cuál se está mirando.
///
/// Para comprar el siguiente hacen falta dos cosas: su precio, que se puede
/// pagar en cuanto se tenga, y que el taller anterior sume un mínimo de niveles
/// entre todo lo que tiene (<see cref="Workshop.RequiredPreviousLevel"/>). Con
/// el dinero pero sin los niveles, no se puede.
///
/// Los talleres comprados funcionan y se mejoran siempre, todos a la vez: el
/// jugador va al que quiera cuando quiera. Los que no, se ven en gris en su
/// sitio (ver <see cref="WorkshopPreview"/>), y es ahí donde se compran.
///
/// Los talleres están ya puestos en la escena, apagados hasta que se compran.
/// Antes la idea era instanciarlos desde un prefab, pero eso no casa con el
/// guardado: salas, decoraciones, carritos y mostradores se guardan por un id
/// que se asigna en el Editor y que no puede vivir en un prefab —todas las
/// copias lo compartirían—, así que el progreso del segundo taller en todo eso
/// se habría perdido al cerrar el juego.
/// </summary>
[DefaultExecutionOrder(BootOrder.Manager)]
public class WorkStationUnlocker : MonoBehaviour
{
    public static WorkStationUnlocker Instance { get; private set; }

    /// <summary>Se ha comprado un taller nuevo.</summary>
    public event Action OnWorkStationUnlocked;

    /// <summary>
    /// Ha cambiado cuáles están comprados, sea por comprar uno o por cargar la
    /// partida.
    /// </summary>
    public event Action OnProgressionChanged;

    /// <summary>La cámara ha pasado a otro taller.</summary>
    public event Action OnViewChanged;

    [Tooltip("Los talleres en orden. El primero es el de partida y viene comprado; " +
             "los demás están apagados hasta que se compran")]
    [SerializeField] private List<Workshop> workshops = new();

    [Header("Vista en gris de los talleres sin comprar")]
    [Tooltip("Cuánto se oscurecen. 0 = gris con la misma luz que el original")]
    [Range(0f, 1f)]
    [SerializeField] private float previewDarkness = 0.35f;

    [Range(0.1f, 1f)]
    [SerializeField] private float previewAlpha = 0.9f;

    /// <summary>Cuántos talleres hay comprados, contando el primero.</summary>
    public int UnlockedCount { get; private set; } = 1;

    /// <summary>El taller que tiene la cámara encima.</summary>
    public int ViewingIndex { get; private set; }

    public IReadOnlyList<Workshop> Workshops => workshops;

    // Lo que decía la partida, aunque aquí haya menos talleres. Se devuelve tal
    // cual al guardar para no recortar el progreso de alguien que abrió la
    // partida con una build que tiene menos talleres.
    private int _savedUnlockedCount = 1;

    // Los que ya pasaron por WorkStation.Init. Aplicar el estado dos veces es
    // normal —una al despertar y otra al cargar la partida— e Init no admite
    // repetirse.
    private readonly HashSet<Workshop> _initialized = new();

    // La copia en gris de cada taller sin comprar.
    private readonly Dictionary<Workshop, GameObject> _previews = new();

    public Workshop At(int index) =>
        index >= 0 && index < workshops.Count ? workshops[index] : null;

    public bool IsUnlocked(int index) => index >= 0 && index < UnlockedCount;

    // ── Lo de antes, para el resto del juego ─────────────────────────

    /// <summary>El siguiente por comprar, o null si ya están todos.</summary>
    public Workshop Next => At(UnlockedCount);

    public bool HasNext => Next != null;
    public double NextCost => Next != null ? Next.UnlockCost : 0;
    public bool CanUnlockNext => CanUnlock(UnlockedCount);

    // ── Requisitos ───────────────────────────────────────────────────

    /// <summary>Si ese taller es el siguiente en orden: se compran uno detrás de otro.</summary>
    public bool IsNextInLine(int index) => index == UnlockedCount && At(index) != null;

    /// <summary>La suma de niveles del taller anterior a <paramref name="index"/>.</summary>
    public int PreviousLevel(int index)
    {
        Workshop previous = At(index - 1);
        return previous != null ? previous.TotalLevel : 0;
    }

    /// <summary>Si el taller anterior ya suma los niveles que pide este.</summary>
    public bool LevelRequirementMet(int index)
    {
        Workshop workshop = At(index);
        return workshop != null && PreviousLevel(index) >= workshop.RequiredPreviousLevel;
    }

    public bool CanAfford(int index)
    {
        Workshop workshop = At(index);
        return workshop != null &&
               EconomyManager.Instance != null &&
               EconomyManager.Instance.CanAfford(workshop.UnlockCost);
    }

    /// <summary>
    /// Si se puede comprar ya: es el siguiente, el anterior suma los niveles y
    /// hay dinero. Las tres cosas; con dos no basta.
    /// </summary>
    public bool CanUnlock(int index) =>
        IsNextInLine(index) && LevelRequirementMet(index) && CanAfford(index);

    // ── Arranque ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!Validate())
        {
            InitLegacyStation();
            return;
        }

        // Estado de partida nueva. Si hay partida guardada, el SaveManager
        // llama a RestoreProgression y lo corrige antes de aplicar nada más.
        Apply(1);
    }

    /// <summary>
    /// Este componente despierta antes que la cámara (va en la capa de
    /// managers), así que en el Apply de Awake todavía no hay cámara a la que
    /// ampliarle los límites. Aquí ya la hay.
    /// </summary>
    private void Start()
    {
        IncludeAllInCameraBounds();
    }

    private void IncludeAllInCameraBounds()
    {
        if (CameraController.Instance == null) return;

        foreach (Workshop workshop in workshops)
            if (workshop != null)
                CameraController.Instance.IncludeInBounds(workshop.WorldBounds);
    }

    /// <summary>
    /// Comprueba la lista antes de tocar nada. Un hueco vacío aquí dejaría al
    /// jugador pagando por un taller que no aparece.
    /// </summary>
    private bool Validate()
    {
        // Lista vacía: la escena es de antes de esto. No es un error —se
        // sigue funcionando como antes, ver InitLegacyStation—, y el aviso lo
        // da quien se encarga de ese caso.
        if (workshops.Count == 0) return false;

        for (int i = 0; i < workshops.Count; i++)
        {
            if (workshops[i] != null) continue;

            Debug.LogError(
                $"[WorkStationUnlocker] El taller {i + 1} de la lista está vacío. " +
                "Ejecuta 'Taller > Construir talleres'.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sin talleres en la lista —la escena todavía no se ha reconstruido— se
    /// hace lo que se hacía antes: registrar el único taller que haya.
    ///
    /// No es un detalle. Sin esto el taller no se registra, el guardado no lo
    /// ve, y el autoguardado de quince segundos escribiría la partida sin las
    /// mesas: el jugador perdería todo lo comprado en ellas por darle a Play
    /// antes de reconstruir.
    /// </summary>
    private void InitLegacyStation()
    {
        WorkStation station = FindAnyObjectByType<WorkStation>(FindObjectsInactive.Exclude);
        if (station == null) return;

        station.Init(0);

        Debug.LogWarning(
            $"[WorkStationUnlocker] Funcionando como antes, con un solo taller ('{station.name}'). " +
            "Ejecuta 'Taller > Construir talleres' para montar los siguientes.", this);
    }

    // ── Aplicar un estado ────────────────────────────────────────────

    /// <summary>
    /// Deja los talleres como tienen que estar con <paramref name="unlockedCount"/>
    /// comprados: esos encendidos y registrados; el resto apagados y con su
    /// copia en gris en el sitio.
    ///
    /// Encender uno despierta sus elementos en el acto, que se apuntan en el
    /// registro de guardado. Por eso el SaveManager llama a esto antes de
    /// aplicar nada más: cuando le toca restaurar las salas y decoraciones del
    /// segundo taller, tienen que estar ya ahí para reclamar lo suyo.
    /// </summary>
    private void Apply(int unlockedCount)
    {
        UnlockedCount = Mathf.Clamp(unlockedCount, 1, Mathf.Max(1, workshops.Count));

        // La cámara tiene que llegar a todos, también a los que no se han
        // comprado: es ahí donde se compran.
        IncludeAllInCameraBounds();

        for (int i = 0; i < workshops.Count; i++)
        {
            Workshop workshop = workshops[i];
            if (workshop == null) continue;

            bool open = i < UnlockedCount;

            if (!open)
            {
                if (workshop.gameObject.activeSelf) workshop.gameObject.SetActive(false);
                ShowPreview(workshop);
                continue;
            }

            HidePreview(workshop);
            if (!workshop.gameObject.activeSelf) workshop.gameObject.SetActive(true);

            // El stationId es su puesto en la lista: el primero es el 0, como
            // antes, así que las partidas ya guardadas siguen encontrando sus
            // mesas.
            WorkStation station = workshop.Station;
            if (station != null && _initialized.Add(workshop))
                station.Init(i);
        }
    }

    private void ShowPreview(Workshop workshop)
    {
        if (_previews.ContainsKey(workshop)) return;

        GameObject preview = WorkshopPreview.Build(workshop, previewDarkness, previewAlpha);
        if (preview != null) _previews[workshop] = preview;
    }

    private void HidePreview(Workshop workshop)
    {
        if (!_previews.TryGetValue(workshop, out GameObject preview)) return;

        if (preview != null) Destroy(preview);
        _previews.Remove(workshop);
    }

    /// <summary>
    /// Lo llama el SaveManager antes de aplicar la partida.
    /// </summary>
    public void RestoreProgression(int unlockedCount)
    {
        if (workshops.Count == 0) return;

        if (unlockedCount > workshops.Count)
        {
            // Partida con más talleres de los que hay en esta build. No se
            // pierde nada —el número se vuelve a escribir tal cual si no se
            // compra otro—, pero hay que saberlo.
            Debug.LogWarning(
                $"[WorkStationUnlocker] La partida tiene {unlockedCount} talleres comprados y en " +
                $"la escena solo hay {workshops.Count}. Se abren los que hay.", this);
        }

        _savedUnlockedCount = unlockedCount;

        Apply(unlockedCount);
        OnProgressionChanged?.Invoke();
    }

    /// <summary>Lo que se escribe en la partida.</summary>
    public int UnlockedCountToSave => Mathf.Max(UnlockedCount, _savedUnlockedCount);

    // ── Comprar ──────────────────────────────────────────────────────

    /// <summary>Compra el siguiente taller, si se puede.</summary>
    public void UnlockNextWorkStation() => Unlock(UnlockedCount);

    public void Unlock(int index)
    {
        if (!CanUnlock(index)) return;

        Workshop workshop = At(index);
        if (!EconomyManager.Instance.SpendCoins(workshop.UnlockCost)) return;

        Apply(index + 1);
        _savedUnlockedCount = Mathf.Max(_savedUnlockedCount, UnlockedCount);

        OnWorkStationUnlocked?.Invoke();
        OnProgressionChanged?.Invoke();

        // Es la compra más cara del juego: se guarda en el acto, sin esperar al
        // autoguardado, para que un cierre justo después no la pierda.
        SaveManager.Instance?.SaveGame();

        Debug.Log($"[WorkStationUnlocker] Comprado {workshop.DisplayName}. " +
                  $"Talleres comprados: {UnlockedCount}.");
    }

    // ── Ir de un taller a otro ───────────────────────────────────────

    /// <summary>
    /// Lleva la cámara a un taller, esté comprado o no: a los que no, se va
    /// precisamente a comprarlos.
    /// </summary>
    public void Visit(int index)
    {
        Workshop workshop = At(index);
        if (workshop == null) return;

        RoomUpgradePanelUI.Instance?.Hide();
        CameraController.Instance?.TravelTo(workshop.HomePosition);

        // Mientras la cámara viaja, el taller que se mira es ya el de destino.
        // Si no, al salir la cámara todavía está sobre el de partida y el
        // seguimiento de abajo lo devolvería a ese: el punto relleno y el panel
        // de compra irían y volverían durante el viaje.
        _travelTarget = index;
        _travelDeadline = Time.unscaledTime + TravelGrace;

        SetViewing(index);
    }

    // El taller al que se está viajando, o -1. Se suelta al llegar o, si la
    // cámara no pudiera llegar por lo que fuera, al pasar el plazo: así un
    // viaje que no termina no deja el seguimiento bloqueado para siempre.
    private int _travelTarget = -1;
    private float _travelDeadline;
    private const float TravelGrace = 2f;

    /// <summary>
    /// Mira sobre qué taller está la cámara. Así, si el jugador llega a otro
    /// taller arrastrando, los puntitos y el botón de comprar se enteran igual
    /// que si hubiera pulsado. Son un par de comparaciones por frame.
    /// </summary>
    private void LateUpdate()
    {
        CameraController cam = CameraController.Instance;
        if (cam == null) return;

        float x = cam.transform.position.x;

        if (_travelTarget >= 0)
        {
            Workshop target = At(_travelTarget);
            bool arrived = target == null ||
                           (x >= target.WorldBounds.min.x && x <= target.WorldBounds.max.x);

            if (!arrived && Time.unscaledTime < _travelDeadline) return;
            _travelTarget = -1;
        }

        for (int i = 0; i < workshops.Count; i++)
        {
            Workshop workshop = workshops[i];
            if (workshop == null) continue;

            Bounds bounds = workshop.WorldBounds;
            if (x < bounds.min.x || x > bounds.max.x) continue;

            SetViewing(i);
            return;
        }
    }

    private void SetViewing(int index)
    {
        if (index == ViewingIndex) return;

        ViewingIndex = index;
        OnViewChanged?.Invoke();
    }

    // ── Compatibilidad ───────────────────────────────────────────────

    /// <summary>
    /// Lo llama el WorkStationRegistry cuando la partida trae un taller que no
    /// está registrado.
    ///
    /// Antes instanciaba el prefab del taller aquí. Ya no hace falta: los
    /// talleres comprados se encienden en <see cref="RestoreProgression"/>,
    /// que corre antes, así que si llega uno aquí es que la partida habla de
    /// un taller que esta escena no tiene. Se avisa y se sigue.
    /// </summary>
    public void RestoreWorkStation(WorkStationSaveData data)
    {
        Debug.LogWarning(
            $"[WorkStationUnlocker] La partida trae datos del taller {data.stationId} y no " +
            "hay ningún taller comprado con ese número en la escena. Se ignoran.", this);
    }
}
