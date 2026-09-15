using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona el ciclo completo de los clientes.
///
/// El cliente entrega su objeto en el mostrador y entonces, o se sienta a
/// esperar, o se marcha del mapa si no hay asiento. Solo vuelve a la cola de
/// recogida cuando su objeto llega a la mesa de entrega. Por eso la cola de
/// entrega nunca se queda bloqueada esperando a la de recogida.
///
/// La identidad de cada cliente es su ticket, un número, y no su GameObject:
/// el que se marcha se destruye y más tarde vuelve otro distinto con el mismo
/// ticket. Así cada objeto acaba en manos de quien lo trajo.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("Caminos")]
    [Tooltip("Camino de entrada, en orden: el último es el pie de la cola de entrega")]
    [SerializeField] private Transform[] pathWaypoints;

    [Tooltip("Camino de vuelta de quien viene a recoger: el último es el pie de la " +
             "cola de recogida. Si se deja vacío se reutiliza el de entrada, pero " +
             "entonces dan un rodeo absurdo por el lado de la cola de entrega")]
    [SerializeField] private Transform[] returnWaypoints;

    [Header("Cola de entrega (derecha, frente al mostrador de entrega)")]
    [Tooltip("Punto donde el primer cliente SE PARA a esperar")]
    [SerializeField] private Transform deskSlot;

    [Tooltip("Punto encima del mostrador donde el objeto cae y queda esperando al recepcionista")]
    [SerializeField] private Transform itemDropTarget;

    [Tooltip("Mostrador donde el cliente deja su objeto")]
    [SerializeField] private ReceptionDesk dropOffDesk;

    [Tooltip("Separación en Y entre clientes (negativo = cola hacia abajo)")]
    [SerializeField] private float slotSpacingY = -0.85f;

    [Header("Cola de recogida (izquierda)")]
    [Tooltip("Punto donde el primer cliente de la cola de recogida espera")]
    [SerializeField] private Transform pickupSlot;

    [Tooltip("Mesa de la que los clientes recogen su objeto restaurado")]
    [SerializeField] private PickupDesk pickupDesk;

    [Header("Sala de espera")]
    [Tooltip("Asientos donde esperan los clientes que ya han entregado. " +
             "Sin esto no se sienta nadie: todos se marchan y vuelven")]
    [SerializeField] private WaitingArea waitingArea;

    [Tooltip("Probabilidad de que un cliente que acaba de entregar se quede a esperar " +
             "sentado, si es que queda algún asiento. El resto se marcha del taller y " +
             "vuelve cuando su objeto está listo. A 1 se sientan todos los que pueden, " +
             "a 0 no se sienta nadie")]
    [Range(0f, 1f)]
    [SerializeField] private float sitChance = 0.5f;

    [Header("Spawn y salida")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private float spawnInterval = 4f;

    [Tooltip("Cuántos clientes caben a la vez en la cola de entrega")]
    [SerializeField] private int maxCustomers = 5;

    [Tooltip("Punto fuera del mapa al que se dirigen los clientes al marcharse")]
    [SerializeField] private Transform exitPoint;

    [Header("Tiempos de atención")]
    [Tooltip("Duración del salto del objeto desde el saco a las manos del recepcionista")]
    [SerializeField] private float handoverHopDuration = 0.35f;
    [SerializeField] private float handoverHopHeight = 0.35f;

    [Tooltip("Lo que tarda el cliente en recoger su objeto, con el círculo de progreso")]
    [SerializeField] private float customerPickupDuration = 0.6f;

    [Tooltip("Lo que tarda el cliente en entregar su objeto en el mostrador")]
    [SerializeField] private float customerDropDuration = 0.6f;

    [Header("WorkStations")]
    [Tooltip("Lista de todas las WorkStations disponibles en el taller")]
    [SerializeField] private List<WorkStation> workStations = new();

    // ── Estado ──────────────────────────────────────────────────────

    /// <summary>Lo que sabemos de un cliente que ya entregó y espera su objeto.</summary>
    private class Ticket
    {
        public int Id;
        public Customer Customer;   // null mientras está fuera del mapa
        public int SeatIndex = -1;  // -1 si no está sentado
        public bool Summoned;       // ya se le ha avisado de que su objeto está listo
    }

    private readonly List<Customer> _entryQueue = new();
    private readonly List<Customer> _pickupQueue = new();
    private readonly Dictionary<int, Ticket> _tickets = new();

    private int _nextTicketId = 1;

    // Evita que el bucle de recogida empiece una segunda entrega mientras la
    // anterior está en el aire.
    private bool _handingOver;

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

    private void Start()
    {
        ResolveWaitingArea();

        StartCoroutine(SpawnLoop());
        StartCoroutine(PickupLoop());
    }

    /// <summary>
    /// Si la sala de espera no está asignada se busca en la escena, y si
    /// tampoco aparece se avisa. Antes se quedaba en null en silencio y el
    /// resultado era que no se sentaba nadie sin que nada lo explicara.
    /// </summary>
    private void ResolveWaitingArea()
    {
        if (waitingArea == null)
            waitingArea = FindAnyObjectByType<WaitingArea>(FindObjectsInactive.Exclude);

        if (waitingArea == null)
        {
            Debug.LogWarning(
                "[CustomerManager] No hay WaitingArea en la escena: ningún cliente se sentará, " +
                "todos se irán a esperar fuera.", this);
            return;
        }

        if (waitingArea.SeatCount == 0)
            Debug.LogWarning(
                $"[CustomerManager] '{waitingArea.name}' no tiene asientos asignados: " +
                "nadie podrá sentarse.", waitingArea);
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
        if (!HasPath()) return;

        ItemDefinition itemDef = itemDatabase != null ? itemDatabase.GetRandom() : null;
        if (itemDef == null)
        {
            Debug.LogWarning("[CustomerManager] No se pudo obtener un ItemDefinition.");
            return;
        }

        Customer customer = SpawnCustomerObject(pathWaypoints[0].position);

        int ticketId = _nextTicketId++;
        _tickets[ticketId] = new Ticket { Id = ticketId, Customer = customer };

        int slotIndex = _entryQueue.Count;

        customer.InitArriving(
            this, pathWaypoints, GetSlotPosition(slotIndex), slotIndex,
            new ItemOrder(itemDef, ticketId), itemDropTarget.position, ExitPosition);

        _entryQueue.Add(customer);
    }

    private Customer SpawnCustomerObject(Vector3 position)
    {
        GameObject go = Instantiate(customerPrefab, position, Quaternion.identity);

        // Antes de Init, que es quien le pone el objeto en la cabeza: así el
        // objeto conserva su propio orden y se dibuja por encima del cliente.
        SortingOrders.Apply(go, SortingOrders.Actor);

        return go.GetComponent<Customer>();
    }

    private bool HasPath()
    {
        if (pathWaypoints != null && pathWaypoints.Length > 0) return true;

        Debug.LogWarning("[CustomerManager] No hay waypoints definidos.");
        return false;
    }

    private Vector3 ExitPosition =>
        exitPoint != null ? exitPoint.position : pathWaypoints[0].position;

    // ── Posiciones de cola ───────────────────────────────────────────
    public Vector3 GetSlotPosition(int index) => deskSlot.position + Vector3.up * slotSpacingY * index;
    public Vector3 GetPickupSlotPosition(int index) => pickupSlot.position + Vector3.up * slotSpacingY * index;

    // ── Mostrador de entrega ─────────────────────────────────────────

    /// <summary>
    /// Mostrador de entrega. Se prefiere el asignado en el Inspector; si no lo
    /// hay, se usa el de la primera WorkStation registrada.
    /// </summary>
    private ReceptionDesk DropOffDesk
    {
        get
        {
            if (dropOffDesk != null) return dropOffDesk;

            WorkStation station = GetAvailableStation();
            return station != null ? station.ReceptionDesk : null;
        }
    }

    public RepairProgressUI DropOffProgressUI => DropOffDesk != null ? DropOffDesk.ProgressUI : null;

    public float CustomerDropDuration =>
        DropOffDesk != null ? DropOffDesk.ServiceTime(customerDropDuration) : customerDropDuration;

    public bool ReceptionHasFreeSlot()
    {
        ReceptionDesk desk = DropOffDesk;
        return desk != null && desk.HasFreeSlot;
    }

    public void OnItemPlacedOnDesk(ItemOrder order, GameObject itemGO)
    {
        ReceptionDesk desk = DropOffDesk;

        if (desk == null)
        {
            Debug.LogWarning("[CustomerManager] No hay mostrador de entrega asignado.");
            return;
        }

        if (!desk.TryDepositFromCustomer(order, itemGO))
            Debug.LogWarning("[CustomerManager] El mostrador de entrega no tenía sitio libre pese a la comprobación previa.");
    }

    /// <summary>
    /// El cliente acaba de entregar. Sale de la cola de entrega y o se queda
    /// esperando sentado, o se marcha del taller; en ambos casos vuelve cuando
    /// su objeto llegue a la mesa de recogida.
    ///
    /// Quedarse no es obligatorio aunque haya sitio: se decide al azar con
    /// <see cref="sitChance"/>. Si todos los que pueden se sentaran, la sala se
    /// llenaría siempre igual y se vería demasiado ordenado.
    /// </summary>
    public void OnCustomerDelivered(Customer customer)
    {
        RemoveFromEntryQueue(customer);

        Ticket ticket = GetTicket(customer.TicketId);
        bool feelsLikeSitting = Random.value < sitChance;

        if (feelsLikeSitting && ticket != null && waitingArea != null &&
            waitingArea.TryClaimSeat(out int seatIndex, out Vector3 seatPos))
        {
            ticket.SeatIndex = seatIndex;
            customer.GoSit(seatPos);
            return;
        }

        customer.LeaveToWaitOutside();
    }

    private void RemoveFromEntryQueue(Customer customer)
    {
        if (!_entryQueue.Remove(customer)) return;

        for (int i = 0; i < _entryQueue.Count; i++)
            _entryQueue[i].MoveToEntrySlot(i, GetSlotPosition(i));
    }

    // ── Cola de recogida ─────────────────────────────────────────────

    private IEnumerator PickupLoop()
    {
        while (true)
        {
            SummonOwnersOfFinishedOrders();

            if (!_handingOver && pickupDesk != null && pickupDesk.HasItem)
                yield return ServeHeadOfPickupQueue();

            yield return null;
        }
    }

    /// <summary>
    /// Por cada objeto terminado que llega a la mesa de entrega, avisa a su
    /// dueño: si está sentado se levanta, y si se había ido vuelve al mapa.
    /// </summary>
    private void SummonOwnersOfFinishedOrders()
    {
        if (pickupDesk == null) return;

        foreach (ItemOrder order in pickupDesk.Orders)
        {
            Ticket ticket = GetTicket(order.TicketId);
            if (ticket == null || ticket.Summoned) continue;

            ticket.Summoned = true;
            SummonForPickup(ticket);
        }
    }

    private void SummonForPickup(Ticket ticket)
    {
        int slotIndex = _pickupQueue.Count;
        Vector3 slotPos = GetPickupSlotPosition(slotIndex);

        // Si estaba sentado, deja el asiento libre para el siguiente.
        if (ticket.SeatIndex >= 0)
        {
            waitingArea?.ReleaseSeat(ticket.SeatIndex);
            ticket.SeatIndex = -1;
        }

        if (ticket.Customer != null)
        {
            // Estaba sentado o aún andaba por el mapa: se va derecho a la cola.
            ticket.Customer.GoToPickupSlot(slotIndex, slotPos);
            _pickupQueue.Add(ticket.Customer);
            return;
        }

        // Se había marchado: vuelve uno nuevo con el mismo ticket, entrando
        // por el camino de vuelta para no dar el rodeo de la cola de entrega.
        Transform[] path = ReturnPath;
        if (path == null) return;

        Customer customer = SpawnCustomerObject(path[0].position);
        customer.InitReturning(this, path, slotPos, slotIndex, ticket.Id, ExitPosition);

        ticket.Customer = customer;
        _pickupQueue.Add(customer);
    }

    /// <summary>
    /// Camino por el que entran los que vienen a recoger. Va directo al pie de
    /// la cola de recogida; si no está puesto se cae al de entrada, que lleva
    /// al lado contrario y se nota mucho.
    /// </summary>
    private Transform[] ReturnPath
    {
        get
        {
            if (returnWaypoints != null && returnWaypoints.Length > 0) return returnWaypoints;
            return HasPath() ? pathWaypoints : null;
        }
    }

    /// <summary>
    /// Atiende al primero de la cola, dándole SU objeto. Solo se llama a la
    /// cola a quien ya tiene el objeto en la mesa, así que siempre está.
    /// </summary>
    private IEnumerator ServeHeadOfPickupQueue()
    {
        if (_pickupQueue.Count == 0) yield break;

        Customer head = _pickupQueue[0];
        if (!head.IsWaitingForItem) yield break;

        if (!pickupDesk.TryTakeOrderFor(head.TicketId, out ItemOrder order)) yield break;

        yield return HandOverRoutine(head, order);
    }

    private IEnumerator HandOverRoutine(Customer customer, ItemOrder order)
    {
        GameObject itemGO = ItemVisual.Spawn(order.Definition);

        if (itemGO == null)
        {
            Debug.LogError($"[CustomerManager] '{order.Definition.itemName}' no tiene itemPrefab: no se puede entregar.");
            pickupDesk.Stack.TryEnqueue(order);
            yield break;
        }

        _handingOver = true;

        Vector3 from = pickupDesk.Stack.ContentsPos;
        itemGO.transform.position = from;

        // Primero salta a las manos del recepcionista, que es quien lo entrega.
        if (pickupDesk.TryGetHandoverPos(out Vector3 handover))
            yield return ItemHop.Move(itemGO.transform, from, handover, handoverHopDuration, handoverHopHeight);

        // El cliente tarda lo suyo en recogerlo, con su círculo de progreso.
        yield return RunProgress(pickupDesk.ProgressUI, pickupDesk.ServiceTime(customerPickupDuration));

        // Pudo marcharse mientras tanto: el objeto vuelve al saco y su dueño
        // se volverá a llamar.
        if (customer == null || !customer.IsWaitingForItem)
        {
            Destroy(itemGO);
            pickupDesk.Stack.TryEnqueue(order);

            Ticket ticket = GetTicket(order.TicketId);
            if (ticket != null) ticket.Summoned = false;

            _handingOver = false;
            yield break;
        }

        BestiaryManager.Instance?.RegisterSold(order.Definition);
        customer.ReceiveFinishedItem(itemGO);

        _handingOver = false;
    }

    /// <summary>El cliente ha recibido su objeto. La cola avanza sin esperar a que salga.</summary>
    public void OnCustomerLeavingPickup(Customer customer)
    {
        if (!_pickupQueue.Remove(customer)) return;

        for (int i = 0; i < _pickupQueue.Count; i++)
            _pickupQueue[i].MoveToPickupSlot(i, GetPickupSlotPosition(i));
    }

    // ── Salida del mapa ──────────────────────────────────────────────

    /// <summary>
    /// El cliente ha cruzado el borde. Puede ser porque ya tiene su objeto, o
    /// porque se va a esperar fuera: en ese caso su ticket sigue vivo y más
    /// tarde volverá otro cliente con él.
    /// </summary>
    public void OnCustomerExited(Customer customer)
    {
        RemoveFromEntryQueue(customer);
        _pickupQueue.Remove(customer);

        Ticket ticket = GetTicket(customer.TicketId);

        if (ticket != null)
        {
            ticket.Customer = null;

            // Si ya se le entregó, el ticket se cierra; si no, sigue esperando
            // fuera y se le volverá a llamar.
            if (ticket.Summoned)
                _tickets.Remove(ticket.Id);
        }

        Destroy(customer.gameObject);
    }

    private Ticket GetTicket(int id) => _tickets.TryGetValue(id, out Ticket ticket) ? ticket : null;

    // ── Utilidades ───────────────────────────────────────────────────

    /// <summary>
    /// Llena un círculo de progreso durante el tiempo dado. Lo comparten la
    /// entrega y la recogida, que tienen su propio tiempo cada una.
    /// </summary>
    public static IEnumerator RunProgress(RepairProgressUI ui, float duration)
    {
        if (duration <= 0f) yield break;

        ui?.Show(0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ui?.SetFill(elapsed / duration);
            yield return null;
        }

        ui?.SetFill(1f);
        ui?.Hide();
    }

    // ── WorkStations ─────────────────────────────────────────────────

    private WorkStation GetAvailableStation() => workStations.Count > 0 ? workStations[0] : null;

    public void RegisterWorkStation(WorkStation workStation)
    {
        if (!workStations.Contains(workStation))
            workStations.Add(workStation);
    }

    public void UnregisterWorkStation(WorkStation workStation) => workStations.Remove(workStation);

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
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            for (int i = 0; i < maxCustomers; i++)
                Gizmos.DrawWireSphere(GetSlotPosition(i), 0.1f);
        }

        if (pickupSlot != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
            for (int i = 0; i < maxCustomers; i++)
                Gizmos.DrawWireSphere(GetPickupSlotPosition(i), 0.1f);
        }

        if (itemDropTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(itemDropTarget.position, 0.15f);
        }

        if (exitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(exitPoint.position, new Vector3(0.3f, 0.3f, 0f));
        }
    }
#endif
}
