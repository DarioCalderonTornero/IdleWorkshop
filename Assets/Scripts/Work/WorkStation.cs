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


    // ── Estado ───────────────────────────────────────────────────────
    private bool _isBusy;
    private ItemDefinition _currentItemDef;
    private CustomerManager _customerManager;
    private readonly List<WorkTable> _unlockedDesks = new();

    // ── Propiedades públicas ─────────────────────────────────────────
    public bool IsBusy => _isBusy;
    public WorkTable ReceptionDesk => receptionDesk;
    public Transform ReceptionItemPoint => receptionItemPoint;

    // ── Unity ────────────────────────────────────────────────────────
    private void Awake()
    {
        _customerManager = FindAnyObjectByType<CustomerManager>();

        if (worker != null)
            worker.Init(this);

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

    // Devuelve la siguiente mesa bloqueada (para el botón de desbloqueo)
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
}