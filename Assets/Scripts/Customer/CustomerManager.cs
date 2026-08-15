using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner y gestor de las colas de clientes: una de entrega (entryQueue)
/// y una de recogida (pickupQueue), colocada al lado de la primera.
/// El cliente entrega, se mueve lateralmente a esperar su objeto terminado,
/// y se va cuando el worker de la última mesa se lo entrega directamente.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("Camino compartido")]
    [Tooltip("Transforms en orden: el último es el pie de la columna de espera")]
    [SerializeField] private Transform[] pathWaypoints;

    [Header("Cola de entrega (frente a la mesa de recepción)")]
    [Tooltip("Punto donde el primer cliente SE PARA a esperar")]
    [SerializeField] private Transform deskSlot;

    [Tooltip("Punto encima de la mesa donde el objeto cae y queda para el jugador")]
    [SerializeField] private Transform itemDropTarget;

    [Tooltip("Separación en Y entre clientes (negativo = cola hacia abajo)")]
    [SerializeField] private float slotSpacingY = -1.2f;

    [Header("Cola de recogida (al lado de la de entrega)")]
    [Tooltip("Punto donde el primer cliente de la cola de recogida espera")]
    [SerializeField] private Transform pickupSlot;

    [Header("Spawn")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private int maxCustomers = 5;

    [Header("WorkStations")]
    [Tooltip("Lista de todas las WorkStations disponibles en el taller")]
    [SerializeField] private List<WorkStation> workStations = new();

    // ── Estado ──────────────────────────────────────────────────────
    private readonly List<Customer> _entryQueue = new();
    private readonly List<Customer> _pickupQueue = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Unity ───────────────────────────────────────────────────────
    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    // ── Spawn ────────────────────────────────────────────────────────
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (_entryQueue.Count < maxCustomers)
                SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning("[CustomerManager] No hay waypoints definidos.");
            return;
        }

        ItemDefinition itemDef = itemDatabase != null ? itemDatabase.GetRandom() : null;
        if (itemDef == null)
        {
            Debug.LogWarning("[CustomerManager] No se pudo obtener un ItemDefinition.");
            return;
        }

        GameObject go = Instantiate(customerPrefab, pathWaypoints[0].position, Quaternion.identity);
        Customer customer = go.GetComponent<Customer>();

        int slotIndex = _entryQueue.Count;
        Vector3 slotPos = GetSlotPosition(slotIndex);

        customer.Init(this, pathWaypoints, slotPos, slotIndex, itemDef, itemDropTarget.position);
        _entryQueue.Add(customer);
    }

    // ── Posiciones de cola ───────────────────────────────────────────
    public Vector3 GetSlotPosition(int index)
    {
        return deskSlot.position + Vector3.up * slotSpacingY * index;
    }

    public Vector3 GetPickupSlotPosition(int index)
    {
        return pickupSlot.position + Vector3.up * slotSpacingY * index;
    }

    // ── Callbacks desde Customer: entrega ─────────────────────────────

    public bool ReceptionHasFreeSlot()
    {
        WorkStation station = GetAvailableStation();
        return station != null && station.ReceptionDesk.HasFreeSlot;
    }

    public bool PickupQueueHasSpace => _pickupQueue.Count < maxCustomers;

    public void OnItemPlacedOnDesk(ItemDefinition itemDef, GameObject itemGO)
    {
        WorkStation station = GetAvailableStation();

        if (station == null)
        {
            Debug.LogWarning("[CustomerManager] No hay WorkStations disponibles.");
            return;
        }

        if (!station.ReceptionDesk.TryDepositFromCustomer(itemDef, itemGO))
        {
            Debug.LogWarning("[CustomerManager] La ReceptionDesk no tenía sitio libre pese a la comprobación previa.");
        }
    }

    /// <summary>El cliente ha entregado y se mueve a la cola de recogida.</summary>
    public void OnCustomerMovedToPickup(Customer customer)
    {
        _entryQueue.Remove(customer);
        for (int i = 0; i < _entryQueue.Count; i++)
            _entryQueue[i].MoveToEntrySlot(i, GetSlotPosition(i));

        int pickupIndex = _pickupQueue.Count;
        _pickupQueue.Add(customer);
        customer.MoveToPickupSlot(pickupIndex, GetPickupSlotPosition(pickupIndex));
    }

    // ── Callbacks: recogida y entrega directa ─────────────────────────

    /// <summary>Devuelve la posición actual del primer cliente esperando en la cola de recogida.</summary>
    public bool TryGetFirstPickupPosition(out Vector3 pos)
    {
        if (_pickupQueue.Count == 0)
        {
            pos = Vector3.zero;
            return false;
        }
        pos = _pickupQueue[0].transform.position;
        return true;
    }

    /// <summary>Entrega el objeto terminado directamente al primer cliente de la cola de recogida.</summary>
    public void DeliverToFirstPickupCustomer(GameObject itemGO)
    {
        if (_pickupQueue.Count == 0)
        {
            Debug.LogWarning("[CustomerManager] No había ningún cliente en la cola de recogida al entregar. Se destruye el objeto.");
            Destroy(itemGO);
            return;
        }

        _pickupQueue[0].ReceiveFinishedItem(itemGO);
    }

    /// <summary>El cliente ha recibido su objeto y sale. Avanza la cola de recogida.</summary>
    public void OnCustomerLeavingPickup(Customer customer)
    {
        _pickupQueue.Remove(customer);
        for (int i = 0; i < _pickupQueue.Count; i++)
            _pickupQueue[i].MoveToPickupSlot(i, GetPickupSlotPosition(i));
    }

    /// <summary>El cliente ha cruzado el borde. Destruir el GameObject (y su objeto en la cabeza, si tiene).</summary>
    public void OnCustomerDone(Customer customer)
    {
        Destroy(customer.gameObject);
    }

    // ── WorkStations ─────────────────────────────────────────────────

    private WorkStation GetAvailableStation()
    {
        if (workStations.Count == 0) return null;
        return workStations[0];
    }

    public void RegisterWorkStation(WorkStation workStation)
    {
        if (!workStations.Contains(workStation))
            workStations.Add(workStation);
    }

    public void UnregisterWorkStation(WorkStation workStation)
    {
        workStations.Remove(workStation);
    }

    // ── Gizmos ───────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (pathWaypoints != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < pathWaypoints.Length; i++)
            {
                if (pathWaypoints[i] == null) continue;
                Gizmos.DrawSphere(pathWaypoints[i].position, 0.1f);
                if (i > 0 && pathWaypoints[i - 1] != null)
                    Gizmos.DrawLine(pathWaypoints[i - 1].position, pathWaypoints[i].position);
            }
        }

        if (deskSlot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(deskSlot.position, 0.15f);
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            for (int i = 0; i < maxCustomers; i++)
                Gizmos.DrawWireSphere(GetSlotPosition(i), 0.1f);
        }

        if (pickupSlot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(pickupSlot.position, 0.15f);
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            for (int i = 0; i < maxCustomers; i++)
                Gizmos.DrawWireSphere(GetPickupSlotPosition(i), 0.1f);
        }

        if (itemDropTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(itemDropTarget.position, 0.15f);
        }
    }
#endif
}