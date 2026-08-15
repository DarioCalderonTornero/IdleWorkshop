using System.Collections;
using UnityEngine;

/// <summary>
/// Comportamiento de un cliente individual. Entrega su objeto, se mueve
/// lateralmente a esperar en la cola de recogida, y recibe su objeto
/// terminado directamente de manos del worker antes de salir del mapa.
/// Ambas colas avanzan correctamente: si un cliente esperando cambia de
/// slot (porque otro se fue), vuelve a moverse hacia su nueva posición.
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

    [Header("Salida")]
    [SerializeField] private float exitX = 12f;

    // ── Estado ─────────────────────────────────────────────────────
    private enum State
    {
        FollowingPath,
        MovingToEntrySlot,
        WaitingInEntryQueue,
        Dropping,
        MovingToPickup,
        WaitingForItem,
        Leaving
    }
    private State _state = State.FollowingPath;

    private CustomerManager _manager;
    private Transform[] _waypoints;
    private int _waypointIndex;
    private Vector3 _slotPos;
    private Vector3 _dropTargetPos;
    private int _slotIndex;

    private ItemDefinition _itemDef;
    private GameObject _itemGO;

    // ── Init ───────────────────────────────────────────────────────
    public void Init(
        CustomerManager manager,
        Transform[] waypoints,
        Vector3 slotPos,
        int slotIndex,
        ItemDefinition itemDef,
        Vector3 dropTargetPos)
    {
        _manager = manager;
        _waypoints = waypoints;
        _slotPos = slotPos;
        _slotIndex = slotIndex;
        _itemDef = itemDef;
        _dropTargetPos = dropTargetPos;
        _waypointIndex = 1;

        SpawnItemOnHead();
    }

    // ── Objeto en cabeza ───────────────────────────────────────────
    private void SpawnItemOnHead()
    {
        if (_itemDef?.itemPrefab == null) return;

        _itemGO = Instantiate(_itemDef.itemPrefab);

        if (_itemGO.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.sprite = _itemDef.sprite;
            sr.sortingOrder = 2;
        }

        _itemGO.transform.localScale = new Vector3(
            _itemDef.displayScale.x,
            _itemDef.displayScale.y,
            1f);

        Transform anchor = headAnchor != null ? headAnchor : transform;
        _itemGO.transform.SetParent(anchor);
        _itemGO.transform.localPosition = Vector3.zero;
    }

    // ── Unity ──────────────────────────────────────────────────────
    private void Update()
    {
        switch (_state)
        {
            case State.FollowingPath: UpdateFollowPath(); break;
            case State.MovingToEntrySlot: UpdateMoveToEntrySlot(); break;
            case State.MovingToPickup: UpdateMoveToPickup(); break;
            case State.Leaving: UpdateLeaving(); break;
        }
    }

    // ── Seguir camino ──────────────────────────────────────────────
    private void UpdateFollowPath()
    {
        if (_waypointIndex >= _waypoints.Length)
        {
            _state = State.MovingToEntrySlot;
            return;
        }

        MoveTo(_waypoints[_waypointIndex].position);

        if (Reached(_waypoints[_waypointIndex].position))
        {
            transform.position = _waypoints[_waypointIndex].position;
            _waypointIndex++;
        }
    }

    private void UpdateMoveToEntrySlot()
    {
        MoveTo(_slotPos);

        if (Reached(_slotPos))
        {
            transform.position = _slotPos;
            OnReachedEntrySlot();
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

    // ── Dejar objeto y moverse a la cola de recogida ────────────────
    private IEnumerator DropItemRoutine()
    {
        if (_itemGO == null)
        {
            _state = State.Leaving;
            yield break;
        }

        while (!_manager.ReceptionHasFreeSlot())
            yield return null;

        while (!_manager.PickupQueueHasSpace)
            yield return null;

        _itemGO.transform.SetParent(null);
        yield return AnimateItem(_itemGO, _itemGO.transform.position, _dropTargetPos, dropDuration);

        _manager.OnItemPlacedOnDesk(_itemDef, _itemGO);
        _itemGO = null; // el objeto entregado ya no pertenece a este cliente

        _state = State.MovingToPickup;
        _manager.OnCustomerMovedToPickup(this);
    }

    private void UpdateMoveToPickup()
    {
        MoveTo(_slotPos);

        if (Reached(_slotPos))
        {
            transform.position = _slotPos;
            _state = State.WaitingForItem;
        }
    }

    // ── Recibir objeto terminado directamente del worker ────────────
    public void ReceiveFinishedItem(GameObject itemGO)
    {
        if (_state != State.WaitingForItem)
        {
            Debug.LogWarning($"[Customer] Se intentó entregar un objeto a un cliente que no estaba esperando (estado: {_state}).");
        }

        Transform anchor = headAnchor != null ? headAnchor : transform;
        itemGO.transform.SetParent(anchor);
        itemGO.transform.localPosition = Vector3.zero;
        _itemGO = itemGO; // se destruirá con el cliente al salir (OnDestroy)

        _manager.OnCustomerLeavingPickup(this);
        _state = State.Leaving;
    }

    // ── Salida ─────────────────────────────────────────────────────
    private void UpdateLeaving()
    {
        Vector3 exitPos = new Vector3(exitX, transform.position.y, transform.position.z);
        MoveTo(exitPos);

        if (transform.position.x >= exitX)
            _manager.OnCustomerDone(this);
    }

    // ── API pública ────────────────────────────────────────────────

    /// <summary>Actualiza el slot de la cola de entrega. Si el cliente estaba esperando, vuelve a moverse.</summary>
    public void MoveToEntrySlot(int newIndex, Vector3 newPos)
    {
        _slotIndex = newIndex;
        _slotPos = newPos;

        if (_state == State.WaitingInEntryQueue)
            _state = State.MovingToEntrySlot;
    }

    /// <summary>Actualiza el slot de la cola de recogida. Si el cliente estaba esperando, vuelve a moverse.</summary>
    public void MoveToPickupSlot(int newIndex, Vector3 newPos)
    {
        _slotIndex = newIndex;
        _slotPos = newPos;

        if (_state == State.WaitingForItem)
            _state = State.MovingToPickup;
    }

    public ItemDefinition ItemDef => _itemDef;

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