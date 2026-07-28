using System.Collections.Generic;
using UnityEngine;

public class WorkStation : MonoBehaviour
{
    [Header("Mesa de recepción")]
    [SerializeField] private WorkTable receptionDesk;

    [Header("Mesas de trabajo en orden (1 a 5)")]
    [SerializeField] private List<WorkDeskUnlockable> workDesks;

    [Header("Worker")]
    [SerializeField] private Worker worker;

    [Header("Punto de entrega al cliente")]
    [SerializeField] private Transform receptionItemPoint;

    [Header("Material de la sección")]
    [SerializeField] private ItemMaterial material = ItemMaterial.Cloth;
    public ItemMaterial Material => material;

    [Header("Desbloqueo sección madera")]
    [SerializeField] private double woodSectionCost = 1000;
    [SerializeField] private int requiredClothLevel = 10;
    [Tooltip("Primera mesa de madera, se desbloquea sola al pagar")]
    [SerializeField] private WorkDeskUnlockable firstWoodDesk;

    [Header("Visual bloqueado (opcional)")]
    [Tooltip("GameObject con el texto 'Cerrado' u otra indicación visual sobre la sección de madera. Se desactiva solo al desbloquear.")]
    [SerializeField] private GameObject lockedVisual;

    [Header("Identificador")]
    private int stationId;
    public int StationId => stationId;

    private bool isInitialized = false;

    // ── Estado ───────────────────────────────────────────────────────
    private bool _isBusy;
    private ItemDefinition _currentItemDef;
    private CustomerManager _customerManager;
    private readonly List<WorkTable> _unlockedDesks = new();
    private bool _isWoodUnlocked = false;

    // ── Propiedades públicas ─────────────────────────────────────────
    public bool IsBusy => _isBusy;
    public WorkTable ReceptionDesk => receptionDesk;
    public Transform ReceptionItemPoint => receptionItemPoint;
    public bool IsWoodUnlocked => _isWoodUnlocked;
    public double WoodSectionCost => woodSectionCost;

    public event System.Action OnWoodSectionUnlocked;

    public bool IsMaterialUnlocked(ItemMaterial material) =>
        material == ItemMaterial.Cloth || _isWoodUnlocked;

    public bool WoodRequirementMet =>
        !_isWoodUnlocked && GetTotalDeskLevel(ItemMaterial.Cloth) >= requiredClothLevel;

    public bool CanUnlockWoodSection =>
        WoodRequirementMet && EconomyManager.Instance.CanAfford(woodSectionCost);

    // ── Unity ────────────────────────────────────────────────────────
    private void Awake()
    {
        _customerManager = FindAnyObjectByType<CustomerManager>();

        if (worker != null)
            worker.Init(this);

        // Esta WorkStation es fija en la escena (no se instancia dinámicamente),
        // así que se registra a sí misma con id 0 en vez de esperar a que
        // WorkStationUnlocker la inicialice.
        Init(0);

        if (lockedVisual != null)
            lockedVisual.SetActive(!_isWoodUnlocked);

        // Registra las mesas ya desbloqueadas por defecto
        // y suscribe el evento de las bloqueadas
        foreach (var desk in workDesks)
        {
            if (desk.IsUnlocked)
                RegisterDesk(desk);
            else
                desk.OnUnlocked += RegisterDesk;
        }
    }

    private void Update()
    {
        if (_isWoodUnlocked) return;
        if (CanUnlockWoodSection)
            UnlockWoodSection();
    }

    private void UnlockWoodSection()
    {
        EconomyManager.Instance.SpendCoins(woodSectionCost);
        _isWoodUnlocked = true;

        if (firstWoodDesk != null && !firstWoodDesk.IsUnlocked)
            firstWoodDesk.Unlock();

        if (lockedVisual != null)
            lockedVisual.SetActive(false);

        OnWoodSectionUnlocked?.Invoke();
    }

    public int GetTotalDeskLevel(ItemMaterial material)
    {
        int total = 0;
        foreach (var desk in workDesks)
        {
            if (!desk.IsUnlocked || desk.Material != material) continue;

            WorkDeskUpgradeable upgradeable = desk.GetComponent<WorkDeskUpgradeable>();
            if (upgradeable != null)
                total += upgradeable.CurrentLevel;
        }
        return total;
    }

    // ── Registro de mesas ────────────────────────────────────────────
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

    /// <summary>Mesas desbloqueadas de un material concreto (usado por el Worker).</summary>
    public List<WorkTable> GetUnlockedDesks(ItemMaterial material)
    {
        List<WorkTable> result = new List<WorkTable>();
        foreach (var desk in workDesks)
        {
            if (!desk.IsUnlocked || desk.Material != material) continue;
            WorkTable table = desk.GetComponent<WorkTable>();
            if (table != null) result.Add(table);
        }
        return result;
    }

    // Devuelve la siguiente mesa bloqueada (para el botón de desbloqueo individual de mesa)
    public WorkDeskUnlockable GetNextLockedDesk()
    {
        foreach (var desk in workDesks)
            if (!desk.IsUnlocked) return desk;
        return null;
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