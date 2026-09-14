using System.Collections;
using UnityEngine;

/// <summary>
/// Un cliente. Tiene dos vidas distintas:
///
///  - Llega con su objeto, hace cola y lo entrega. Después se sienta a esperar
///    si queda algún asiento libre, y si no se marcha del mapa.
///  - Cuando su objeto está listo va a la cola de recogida: el que estaba
///    sentado se levanta, y el que se había ido vuelve a entrar, ya directo
///    hacia esa cola en vez de repetir el camino de la de entrega.
///
/// Quién es cada uno lo lleva el ticket, no este GameObject, precisamente
/// porque el que se marcha deja de existir mientras su objeto se restaura.
/// </summary>
public class Customer : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Objeto en cabeza")]
    [Tooltip("Transform hijo posicionado encima de la cabeza del sprite")]
    [SerializeField] private Transform headAnchor;

    [Tooltip("Duración de la animación del objeto cayendo a la mesa")]
    [SerializeField] private float dropDuration = 0.35f;

    [Tooltip("Duración de la animación del objeto subiendo de la mesa a la cabeza")]
    [SerializeField] private float pickupDuration = 0.35f;

    // ── Estado ─────────────────────────────────────────────────────
    private enum State
    {
        FollowingPath,
        MovingToEntrySlot,
        WaitingInEntryQueue,
        Dropping,
        MovingToSeat,
        Seated,
        MovingToPickupSlot,
        WaitingForItem,
        PickingUp,
        Leaving
    }
    private State _state = State.FollowingPath;

    private CustomerManager _manager;
    private Transform[] _waypoints;
    private int _waypointIndex;
    private Vector3 _targetPos;
    private Vector3 _dropTargetPos;
    private Vector3 _exitPos;
    private int _slotIndex;

    private ItemOrder _order;
    private GameObject _itemGO;

    /// <summary>Si al llegar al final del camino va a la cola de recogida en vez de a la de entrega.</summary>
    private bool _returningForPickup;

    public int TicketId { get; private set; }
    public ItemOrder Order => _order;
    public bool IsWaitingForItem => _state == State.WaitingForItem;
    public bool IsSeated => _state == State.Seated;

    // ── Init ───────────────────────────────────────────────────────

    /// <summary>Cliente que llega con su objeto a entregar.</summary>
    public void InitArriving(
        CustomerManager manager,
        Transform[] waypoints,
        Vector3 slotPos,
        int slotIndex,
        ItemOrder order,
        Vector3 dropTargetPos,
        Vector3 exitPos)
    {
        Setup(manager, waypoints, slotPos, exitPos, order.TicketId);

        _slotIndex = slotIndex;
        _order = order;
        _dropTargetPos = dropTargetPos;
        _returningForPickup = false;

        SpawnItemOnHead();
    }

    /// <summary>Cliente que vuelve a por su objeto, ya sin nada en las manos.</summary>
    public void InitReturning(
        CustomerManager manager,
        Transform[] waypoints,
        Vector3 pickupSlotPos,
        int slotIndex,
        int ticketId,
        Vector3 exitPos)
    {
        Setup(manager, waypoints, pickupSlotPos, exitPos, ticketId);

        _slotIndex = slotIndex;
        _returningForPickup = true;
    }

    private void Setup(
        CustomerManager manager, Transform[] waypoints, Vector3 targetPos, Vector3 exitPos, int ticketId)
    {
        _manager = manager;
        _waypoints = waypoints;
        _targetPos = targetPos;
        _exitPos = exitPos;
        TicketId = ticketId;
        _waypointIndex = 1;
        _state = State.FollowingPath;
    }

    // ── Objeto en cabeza ───────────────────────────────────────────
    private void SpawnItemOnHead()
    {
        _itemGO = ItemVisual.Spawn(_order?.Definition);
        if (_itemGO == null) return;

        AttachToHead(_itemGO);
    }

    private void AttachToHead(GameObject itemGO)
    {
        Transform anchor = headAnchor != null ? headAnchor : transform;
        itemGO.transform.SetParent(anchor);
        itemGO.transform.localPosition = Vector3.zero;
    }

    // ── Unity ──────────────────────────────────────────────────────
    private void Update()
    {
        switch (_state)
        {
            case State.FollowingPath: UpdateFollowPath(); break;
            case State.MovingToEntrySlot: UpdateMoveTo(OnReachedEntrySlot); break;
            case State.MovingToSeat: UpdateMoveTo(() => _state = State.Seated); break;
            case State.MovingToPickupSlot: UpdateMoveTo(() => _state = State.WaitingForItem); break;
            case State.Leaving: UpdateLeaving(); break;
        }
    }

    private void UpdateMoveTo(System.Action onArrived)
    {
        MoveTo(_targetPos);
        if (!Reached(_targetPos)) return;

        transform.position = _targetPos;
        onArrived();
    }

    // ── Seguir camino ──────────────────────────────────────────────
    private void UpdateFollowPath()
    {
        if (_waypointIndex >= _waypoints.Length)
        {
            _state = _returningForPickup ? State.MovingToPickupSlot : State.MovingToEntrySlot;
            return;
        }

        MoveTo(_waypoints[_waypointIndex].position);

        if (Reached(_waypoints[_waypointIndex].position))
        {
            transform.position = _waypoints[_waypointIndex].position;
            _waypointIndex++;
        }
    }

    private void OnReachedEntrySlot()
    {
        if (_slotIndex == 0)
        {
            _state = State.Dropping;
            StartCoroutine(DropItemRoutine());
        }
        else
        {
            _state = State.WaitingInEntryQueue;
        }
    }

    // ── Entregar el objeto ──────────────────────────────────────────
    private IEnumerator DropItemRoutine()
    {
        if (_itemGO == null)
        {
            _state = State.Leaving;
            yield break;
        }

        // Solo espera a que haya sitio en el mostrador. La cola de recogida ya
        // no bloquea nada: quien entrega se va a sentar o se marcha, así que
        // la cola de entrega puede seguir moviéndose.
        while (!_manager.ReceptionHasFreeSlot())
            yield return null;

        // Entregar lleva su tiempo, y ese tiempo es ANTES de soltar el objeto:
        // el círculo sale mientras el cliente todavía lo tiene en las manos.
        yield return CustomerManager.RunProgress(_manager.DropOffProgressUI, _manager.CustomerDropDuration);

        _itemGO.transform.SetParent(null);
        yield return AnimateItem(_itemGO, _itemGO.transform.position, _dropTargetPos, dropDuration);

        _manager.OnItemPlacedOnDesk(_order, _itemGO);
        _itemGO = null; // el objeto entregado ya no pertenece a este cliente

        _manager.OnCustomerDelivered(this);
    }

    // ── Esperar: sentado, o fuera del mapa ──────────────────────────

    /// <summary>Hay asiento libre: se va a sentar y espera ahí.</summary>
    public void GoSit(Vector3 seatPosition)
    {
        _targetPos = seatPosition;
        _state = State.MovingToSeat;
    }

    /// <summary>No hay asiento: se marcha y volverá cuando su objeto esté listo.</summary>
    public void LeaveToWaitOutside()
    {
        _state = State.Leaving;
    }

    // ── Volver a por el objeto ──────────────────────────────────────

    /// <summary>Su objeto está listo: se levanta (o llega) y se pone en la cola.</summary>
    public void GoToPickupSlot(int index, Vector3 position)
    {
        _slotIndex = index;
        _targetPos = position;

        // Si aún viene de camino por los waypoints, que los termine: al acabar
        // se irá solo a la cola de recogida.
        if (_state == State.FollowingPath) return;

        _state = State.MovingToPickupSlot;
    }

    /// <summary>Actualiza el sitio en la cola de recogida cuando esta avanza.</summary>
    public void MoveToPickupSlot(int newIndex, Vector3 newPos)
    {
        _slotIndex = newIndex;
        _targetPos = newPos;

        if (_state == State.WaitingForItem)
            _state = State.MovingToPickupSlot;
    }

    /// <summary>Actualiza el slot de la cola de entrega. Si estaba esperando, vuelve a moverse.</summary>
    public void MoveToEntrySlot(int newIndex, Vector3 newPos)
    {
        _slotIndex = newIndex;
        _targetPos = newPos;

        if (_state == State.WaitingInEntryQueue)
            _state = State.MovingToEntrySlot;
    }

    // ── Recibir el objeto terminado ─────────────────────────────────
    public void ReceiveFinishedItem(GameObject itemGO)
    {
        if (_state != State.WaitingForItem)
            Debug.LogWarning($"[Customer] Objeto entregado a un cliente que no estaba esperando (estado: {_state}).");

        _itemGO = itemGO; // se destruirá con el cliente al salir (OnDestroy)

        // Sale de WaitingForItem ya, antes de la animación, para que el
        // CustomerManager no le asigne un segundo objeto mientras la reproduce.
        _state = State.PickingUp;

        // La cola avanza sin esperar a que este cliente salga del mapa.
        _manager.OnCustomerLeavingPickup(this);

        StartCoroutine(PickUpItemRoutine(itemGO));
    }

    private IEnumerator PickUpItemRoutine(GameObject itemGO)
    {
        Transform anchor = headAnchor != null ? headAnchor : transform;
        yield return AnimateItem(itemGO, itemGO.transform.position, anchor.position, pickupDuration);

        AttachToHead(itemGO);
        _state = State.Leaving;
    }

    // ── Salida ─────────────────────────────────────────────────────
    private void UpdateLeaving()
    {
        MoveTo(_exitPos);

        if (Reached(_exitPos))
            _manager.OnCustomerExited(this);
    }

    // ── Helpers ────────────────────────────────────────────────────
    private void MoveTo(Vector3 target) =>
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

    private bool Reached(Vector3 target) =>
        Vector3.Distance(transform.position, target) < 0.05f;

    private IEnumerator AnimateItem(GameObject go, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            go.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        go.transform.position = to;
    }

    private void OnDestroy()
    {
        if (_itemGO != null) Destroy(_itemGO);
    }
}
