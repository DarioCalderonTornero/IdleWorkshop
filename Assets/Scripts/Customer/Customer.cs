using System.Collections;
using UnityEngine;

/// <summary>
/// Comportamiento de un cliente individual.
/// La cola avanza 2 segundos después de que el cliente recoge el objeto reparado,
/// sin esperar a que salga del mapa.
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

    [Tooltip("Duración de la animación del objeto subiendo de vuelta a la cabeza")]
    [SerializeField] private float pickupDuration = 0.35f;

    [Tooltip("Segundos tras coger el objeto antes de que avance la cola")]
    [SerializeField] private float queueAdvanceDelay = 2f;

    [Header("Salida")]
    [SerializeField] private float exitX = 12f;

    // ── Estado ─────────────────────────────────────────────────────
    private enum State { FollowingPath, MovingToSlot, Dropping, Waiting, PickingUp, Leaving }
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
            case State.MovingToSlot: UpdateMoveToSlot(); break;
            case State.Leaving: UpdateLeaving(); break;
        }
    }

    // ── Seguir camino ──────────────────────────────────────────────
    private void UpdateFollowPath()
    {
        if (_waypointIndex >= _waypoints.Length)
        {
            _state = State.MovingToSlot;
            return;
        }

        MoveTo(_waypoints[_waypointIndex].position);

        if (Reached(_waypoints[_waypointIndex].position))
        {
            transform.position = _waypoints[_waypointIndex].position;
            _waypointIndex++;
        }
    }

    private void UpdateMoveToSlot()
    {
        MoveTo(_slotPos);

        if (Reached(_slotPos))
        {
            transform.position = _slotPos;
            OnReachedSlot();
        }
    }

    private void OnReachedSlot()
    {
        if (_slotIndex == 0)
        {
            _state = State.Dropping;
            StartCoroutine(DropItemRoutine());
        }
        else
        {
            _state = State.Waiting;
        }
    }

    // ── Dejar objeto en mesa ───────────────────────────────────────
    private IEnumerator DropItemRoutine()
    {
        if (_itemGO == null) yield break;

        _itemGO.transform.SetParent(null);
        yield return AnimateItem(_itemGO, _itemGO.transform.position, _dropTargetPos, dropDuration);

        _manager.OnItemPlacedOnDesk(_itemDef, _itemGO);
        _state = State.Waiting;
    }

    // ── Recoger objeto reparado ────────────────────────────────────
    private IEnumerator PickUpItemRoutine()
    {
        if (_itemGO == null) yield break;

        Transform anchor = headAnchor != null ? headAnchor : transform;
        Vector3 startPos = _itemGO.transform.position;

        yield return AnimateItem(_itemGO, startPos, anchor.position, pickupDuration);

        _itemGO.transform.SetParent(anchor);
        _itemGO.transform.localPosition = Vector3.zero;

        // La cola avanza inmediatamente (con pequeño delay de cortesía)
        yield return new WaitForSeconds(queueAdvanceDelay);
        _manager.OnCustomerLeaving(this);

        // El cliente sigue su camino hacia la salida en paralelo
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
    public void MoveToSlot(int newIndex, Vector3 newPos)
    {
        _slotIndex = newIndex;
        _slotPos = newPos;

        if (_state == State.Waiting)
            _state = State.MovingToSlot;
    }

    public void BeServed()
    {
        if (_state != State.Waiting) return;
        _state = State.PickingUp;
        StartCoroutine(PickUpItemRoutine());
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