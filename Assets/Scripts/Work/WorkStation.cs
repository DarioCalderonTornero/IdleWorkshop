using System.Collections.Generic;
using UnityEngine;

public class WorkStation : MonoBehaviour
{
    [Header("Mesa de entrega (el cliente deja el objeto)")]
    [SerializeField] private ReceptionDesk receptionDesk;

    [Header("Mesa de recogida (el cliente recoge el objeto restaurado)")]
    [SerializeField] private PickupDesk pickupDesk;

    [Header("Transportistas con carrito")]
    [SerializeField] private List<CartWorker> cartWorkers = new();

    [Header("Mesas de trabajo en orden")]
    [SerializeField] private List<WorkDeskUnlockable> workDesks;

    [Header("Elementos mejorables (panel inferior)")]
    [SerializeField] private List<RoomUpgradeElement> upgradeElements;
    public List<RoomUpgradeElement> UpgradeElements => upgradeElements;

    [Header("Centrado de cámara")]
    [Tooltip("Punto al que se mueve la cámara al seleccionar esta habitación. Si es null, usa la posición del propio taller.")]
    [SerializeField] private Transform cameraFocusPoint;
    public Vector3 CameraFocusPosition => cameraFocusPoint != null ? cameraFocusPoint.position : transform.position;

    [Header("Identificador")]
    private int stationId;
    public int StationId => stationId;

    private bool isInitialized = false;

    private bool _isBusy;
    private readonly List<WorkTable> _unlockedDesks = new();

    // Cache de la relación WorkDeskUnlockable → WorkTable, construida una
    // sola vez en Awake para evitar GetComponent repetido en cada llamada.
    private readonly Dictionary<WorkDeskUnlockable, WorkTable> _deskTables = new();

    public bool IsBusy => _isBusy;
    public ReceptionDesk ReceptionDesk => receptionDesk;
    public PickupDesk PickupDesk => pickupDesk;
    public IReadOnlyList<CartWorker> CartWorkers => cartWorkers;

    private void Awake()
    {
        foreach (var desk in workDesks)
        {
            if (desk == null) continue;
            _deskTables[desk] = desk.GetComponent<WorkTable>();
        }
    }

    private void Start()
    {
        if (receptionDesk == null)
        {
            Debug.LogWarning($"[WorkStation] {name} no tiene ReceptionDesk asignada.", this);
        }
        else
        {
            Receptionist receptionist = receptionDesk.GetComponentInChildren<Receptionist>();
            if (receptionist != null)
                receptionist.Init(receptionDesk, this);
            else
                Debug.LogWarning("[WorkStation] No se encontró ningún Receptionist dentro de ReceptionDesk.");
        }

        // En Start() para garantizar que WorkDeskUnlockable.Awake() ya ha
        // establecido IsUnlocked (el orden de Awake entre hermanos no está garantizado).
        foreach (var desk in workDesks)
        {
            if (desk == null) continue;

            if (desk.IsUnlocked)
                RegisterDesk(desk);
            else
                desk.OnUnlocked += RegisterDesk;
        }

        foreach (var element in upgradeElements)
        {
            if (element.upgradeableTarget is DecorativeUpgradeable decorative)
                decorative.Init(this);
        }
    }

    // ── Registro de mesas ────────────────────────────────────────────
    private void RegisterDesk(WorkDeskUnlockable unlockable)
    {
        if (!_deskTables.TryGetValue(unlockable, out WorkTable table) || table == null) return;

        if (!_unlockedDesks.Contains(table))
        {
            _unlockedDesks.Add(table);
            Debug.Log($"[WorkStation] Mesa registrada. Total activas: {_unlockedDesks.Count}");
        }
    }

    public List<WorkTable> GetUnlockedDesks() => _unlockedDesks;

    public WorkDeskUnlockable GetNextLockedDesk()
    {
        foreach (var desk in workDesks)
            if (!desk.IsUnlocked) return desk;
        return null;
    }

    /// <summary>
    /// La última mesa desbloqueada en el orden de workDesks. Es la que hace de
    /// salida del taller: sus objetos terminados esperan al carrito.
    /// </summary>
    public WorkTable GetLastUnlockedDesk()
    {
        WorkTable last = null;

        foreach (var desk in workDesks)
        {
            if (desk == null || !desk.IsUnlocked) continue;

            if (_deskTables.TryGetValue(desk, out WorkTable table) && table != null)
                last = table;
        }

        return last;
    }

    /// <summary>
    /// La primera mesa que tenga objetos terminados esperando en su parte
    /// contraria. Al desbloquear una mesa nueva la salida del taller se muda a
    /// ella, así que sin esto los objetos que hubiera en la mesa anterior no
    /// los recogería nadie nunca.
    /// </summary>
    public WorkTable GetDeskWithPendingOutput()
    {
        foreach (var desk in workDesks)
        {
            if (desk == null) continue;
            if (!_deskTables.TryGetValue(desk, out WorkTable table) || table == null) continue;

            if (table.OutStack != null && table.OutStack.Count > 0)
                return table;
        }

        return null;
    }

    public bool AllDesksUnlocked()
    {
        foreach (var desk in workDesks)
            if (!desk.IsUnlocked) return false;
        return true;
    }

    /// <summary>
    /// Devuelve la siguiente mesa desbloqueada después de "current", en el
    /// orden definido por workDesks. Null si "current" es la última desbloqueada
    /// (el objeto debe ir a la recepción en ese caso).
    /// </summary>
    public WorkTable GetNextUnlockedDesk(WorkTable current)
    {
        int currentIndex = -1;
        for (int i = 0; i < workDesks.Count; i++)
        {
            if (_deskTables.TryGetValue(workDesks[i], out WorkTable table) && table == current)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex == -1) return null;

        for (int i = currentIndex + 1; i < workDesks.Count; i++)
        {
            if (!workDesks[i].IsUnlocked) continue;
            return _deskTables.TryGetValue(workDesks[i], out WorkTable nextTable) ? nextTable : null;
        }

        return null;
    }

    // ── Bonus decorativo de zona ───────────────────────────────────────
    public void RecalculateDecorativeBonus()
    {
        float coinBonus = 0f;
        float speedBonus = 0f;

        foreach (var element in upgradeElements)
        {
            if (element.upgradeableTarget is DecorativeUpgradeable decorative)
            {
                if (decorative.EffectType == DecorativeUpgradeable.DecorativeEffectType.CoinMultiplier)
                    coinBonus += decorative.CurrentBonus;
                else
                    speedBonus += decorative.CurrentBonus;
            }
        }

        float rewardMultiplier = 1f + coinBonus;
        float timeMultiplier = Mathf.Max(0.1f, 1f - speedBonus);

        foreach (var desk in workDesks)
        {
            if (_deskTables.TryGetValue(desk, out WorkTable table))
                table?.ApplyZoneMultipliers(timeMultiplier, rewardMultiplier);
        }
    }

    // ── API pública ──────────────────────────────────────────────────

    /// <summary>
    /// Llamado por el worker de la última mesa cuando deja el objeto terminado
    /// en la zona de salida. A partir de ahí el objeto ya no es asunto del
    /// taller: el carrito de salida lo lleva a la mesa de recogida y el cliente
    /// lo recoge de ahí.
    /// </summary>
    public void OnWorkCompleted()
    {
        // Punto de enganche para estadísticas o eventos de "objeto terminado".
    }

    public WorkStationSaveData GetSaveData()
    {
        WorkStationSaveData workStationSaveData = new WorkStationSaveData();
        workStationSaveData.stationId = stationId;

        for (int i = 0; i < workDesks.Count; i++)
        {
            WorkDeskUpgradeable deskUpgradeable = workDesks[i].GetComponent<WorkDeskUpgradeable>();

            DeskSaveData deskSaveData = new DeskSaveData();
            deskSaveData.deskIndex = i;
            deskSaveData.isUnlocked = workDesks[i].IsUnlocked;
            deskSaveData.level = deskUpgradeable != null ? deskUpgradeable.CurrentLevel : 0;

            workStationSaveData.desks.Add(deskSaveData);
        }

        return workStationSaveData;
    }

    public void LoadSaveData(WorkStationSaveData data)
    {
        for (int i = 0; i < workDesks.Count && i < data.desks.Count; i++)
        {
            DeskSaveData deskData = data.desks[i];

            if (deskData.isUnlocked && !workDesks[i].IsUnlocked)
                workDesks[i].Unlock();

            WorkDeskUpgradeable deskUpgradeable = workDesks[i].GetComponent<WorkDeskUpgradeable>();
            if (deskUpgradeable != null)
                deskUpgradeable.LoadLevel(deskData.level);
        }

        RecalculateDecorativeBonus();
    }

    public void Init(int id)
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[WorkStation] Init() llamado más de una vez en {gameObject.name}. Ignorado.");
            return;
        }

        stationId = id;
        isInitialized = true;
        WorkStationRegistry.Instance.Register(this);
    }
}