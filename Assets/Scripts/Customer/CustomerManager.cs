using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner y gestor de la cola de clientes.
/// No conoce a ningún Worker directamente.
/// Se comunica con las WorkStations disponibles para repartir trabajo.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("Camino compartido")]
    [Tooltip("Transforms en orden: el último es el pie de la columna de espera")]
    [SerializeField] private Transform[] pathWaypoints;

    [Header("Cola (frente a la mesa de recepción)")]
    [Tooltip("Punto donde el primer cliente SE PARA a esperar")]
    [SerializeField] private Transform deskSlot;

    [Tooltip("Punto encima de la mesa donde el objeto cae y queda para el jugador")]
    [SerializeField] private Transform itemDropTarget;

    [Tooltip("Separación en Y entre clientes (negativo = cola hacia abajo)")]
    [SerializeField] private float slotSpacingY = -1.2f;

    [Header("Spawn")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private int maxCustomers = 5;

    [Header("WorkStations")]
    [Tooltip("Lista de todas las WorkStations disponibles en el taller")]
    [SerializeField] private List<WorkStation> workStations = new();

    // ── Estado ──────────────────────────────────────────────────────
    private readonly List<Customer> _queue = new();

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
            if (_queue.Count < maxCustomers)
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

        int slotIndex = _queue.Count;
        Vector3 slotPos = GetSlotPosition(slotIndex);

        customer.Init(this, pathWaypoints, slotPos, slotIndex, itemDef, itemDropTarget.position);
        _queue.Add(customer);
    }

    // ── Posiciones de cola ───────────────────────────────────────────
    public Vector3 GetSlotPosition(int index)
    {
        return deskSlot.position + Vector3.up * slotSpacingY * index;
    }

    // ── Callbacks desde Customer ─────────────────────────────────────

    /// <summary>
    /// El cliente ha dejado el objeto en la mesa.
    /// Busca la primera WorkStation libre y le manda el trabajo.
    /// </summary>
    public void OnItemPlacedOnDesk(ItemDefinition itemDef, GameObject itemGO)
    {
        WorkStation freeStation = GetFreeWorkStation();

        if (freeStation == null)
        {
            Debug.LogWarning("[CustomerManager] No hay WorkStations libres.");
            return;
        }

        freeStation.RequestWork(itemGO, itemDef);
    }

    /// <summary>
    /// La WorkStation ha terminado el trabajo.
    /// El primer cliente de la cola puede recoger su objeto y marcharse.
    /// </summary>
    public void ServeNextCustomer(WorkStation workStation)
    {
        if (_queue.Count == 0) return;
        _queue[0].BeServed();
    }

    /// <summary>
    /// El cliente ha recogido su objeto y va a salir.
    /// Avanza la cola.
    /// </summary>
    public void OnCustomerLeaving(Customer customer)
    {
        _queue.Remove(customer);

        for (int i = 0; i < _queue.Count; i++)
            _queue[i].MoveToSlot(i, GetSlotPosition(i));
    }

    /// <summary>El cliente ha cruzado el borde. Destruir el GameObject.</summary>
    public void OnCustomerDone(Customer customer)
    {
        Destroy(customer.gameObject);
    }

    // ── WorkStations ─────────────────────────────────────────────────

    /// <summary>Devuelve la primera WorkStation libre, o null si todas están ocupadas.</summary>
    private WorkStation GetFreeWorkStation()
    {
        foreach (WorkStation ws in workStations)
        {
            if (!ws.IsBusy) return ws;
        }
        return null;
    }

    /// <summary>
    /// Registra una nueva WorkStation en tiempo de ejecución.
    /// Útil cuando se desbloquea una estación nueva al subir de nivel.
    /// </summary>
    public void RegisterWorkStation(WorkStation workStation)
    {
        if (!workStations.Contains(workStation))
            workStations.Add(workStation);
    }

    /// <summary>
    /// Elimina una WorkStation del sistema.
    /// </summary>
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

        if (itemDropTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(itemDropTarget.position, 0.15f);
        }
    }
#endif
}
