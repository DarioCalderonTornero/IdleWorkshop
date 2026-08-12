using System.Collections.Generic;
using UnityEngine;

public class WorkStation : MonoBehaviour
{
    [Header("Mesa de recepción")]
    [SerializeField] private WorkTable receptionDesk;

    [Header("Mesas de trabajo en orden")]
    [SerializeField] private List<WorkDeskUnlockable> workDesks;

    [Header("Worker")]
    [SerializeField] private Worker worker;

    [Header("Punto de entrega al cliente")]
    [SerializeField] private Transform receptionItemPoint;

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
    private ItemDefinition _currentItemDef;
    private CustomerManager _customerManager;
    private readonly List<WorkTable> _unlockedDesks = new();

    public bool IsBusy => _isBusy;
    public WorkTable ReceptionDesk => receptionDesk;
    public Transform ReceptionItemPoint => receptionItemPoint;

    private void Awake()
    {
        _customerManager = FindAnyObjectByType<CustomerManager>();

        if (worker != null)
            worker.Init(this);
    }

    private void Start()
    {
        // En Start() para garantizar que WorkDeskUnlockable.Awake() ya ha
        // establecido IsUnlocked (el orden de Awake entre hermanos no está garantizado).
        foreach (var desk in workDesks)
        {
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

    private void RegisterDesk(WorkDeskUnlockable unlockable)
    {
        WorkTable table = unlockable.GetComponent<WorkTable>();
        if (table != null && !_unlockedDesks.Contains(table))
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

    public bool AllDesksUnlocked()
    {
        foreach (var desk in workDesks)
            if (!desk.IsUnlocked) return false;
        return true;
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
            WorkTable table = desk.GetComponent<WorkTable>();
            table?.ApplyZoneMultipliers(timeMultiplier, rewardMultiplier);
        }
    }

    // ── API pública ──────────────────────────────────────────────────
    public void RequestWork(GameObject itemGO, ItemDefinition itemDef)
    {
        if (_isBusy) return;
        _isBusy = true;
        _currentItemDef = itemDef;
        worker.StartWork(itemGO, itemDef);
    }

    public void OnWorkCompleted()
    {
        _customerManager?.ServeNextCustomer(this);
        _isBusy = false;
        _currentItemDef = null;
    }

    public WorkStationSaveData GetSaveData()
    {
        WorkStationSaveData workStationSaveData = new WorkStationSaveData();
        workStationSaveData.stationId = stationId;

        WorkerUpgradeable workerUpgradeable = worker.GetComponent<WorkerUpgradeable>();
        workStationSaveData.workerLevel = workerUpgradeable != null ? workerUpgradeable.CurrentLevel : 0;

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
        WorkerUpgradeable workerUpgradeable = worker.GetComponent<WorkerUpgradeable>();
        if (workerUpgradeable != null)
            workerUpgradeable.LoadLevel(data.workerLevel);

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