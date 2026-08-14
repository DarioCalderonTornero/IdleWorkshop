using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner y gestor de la cola de clientes.
/// El cliente entrega el objeto en la ReceptionDesk y se va de inmediato,
/// sin esperar a que el objeto se procese en el taller — el ciclo del
/// cliente y el ciclo del objeto son independientes.
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
    /// True si la recepción de la WorkStation disponible tiene sitio libre
    /// para recibir un objeto nuevo. El Customer debe esperar mientras esto
    /// sea false antes de animar la entrega.
    /// </summary>
    public bool ReceptionHasFreeSlot()
    {
        WorkStation station = GetAvailableStation();
        return station != null && station.ReceptionDesk.HasFreeSlot;
    }

    /// <summary>
    /// El cliente ha dejado el objeto. Se deposita en el slot de espera de
    /// la ReceptionDesk — el Receptionist lo recogerá de ahí y lo llevará
    /// hasta la primera mesa desbloqueada.
    /// </summary>
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

    /// <summary>
    /// El cliente ha entregado su objeto y se va. Avanza la cola.
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

    /// <summary>
    /// Devuelve la WorkStation a la que entregar el próximo objeto.
    /// Por ahora, simplemente la primera registrada — con un único taller
    /// en el proyecto, es funcionalmente correcto. Cuando haya varios talleres
    /// activos a la vez, este método necesitará un criterio de reparto real
    /// (por ejemplo, menor acumulación en la cola de entrada).
    /// </summary>
    private WorkStation GetAvailableStation()
    {
        if (workStations.Count == 0) return null;
        return workStations[0];
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