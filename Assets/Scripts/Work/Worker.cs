using System.Collections;
using UnityEngine;

/// <summary>
/// Trabajador de una mesa concreta. Espera a que su mesa (WorkTable) tenga
/// un ítem en cola, lo recoge de su propio BoxPoint, lo procesa, y lo
/// transporta hasta la siguiente mesa desbloqueada. Si es la última mesa,
/// camina hasta el primer cliente esperando en la cola de recogida y le
/// entrega el objeto directamente en la mano.
/// </summary>
public class Worker : WorkerBase
{
    [Header("UI")]
    [SerializeField] private RepairProgressUI progressUI;

    private WorkTable _myTable;
    private WorkStation _workStation;
    private bool _isWorking;

    public void Init(WorkTable myTable, WorkStation workStation)
    {
        _myTable = myTable;
        _workStation = workStation;

        if (idlePosition != null)
            transform.position = idlePosition.position;

        WorkerRegistry.Instance?.Register(this);

        _myTable.OnItemEnqueued += HandleItemEnqueued;

        TryStartNextItem();
    }

    private void HandleItemEnqueued()
    {
        if (!_isWorking)
            TryStartNextItem();
    }

    private void TryStartNextItem()
    {
        if (_isWorking) return;
        if (!_myTable.TryDequeueItem(out ItemDefinition itemDef)) return;

        _isWorking = true;
        StartCoroutine(WorkRoutine(itemDef));
    }

    private IEnumerator WorkRoutine(ItemDefinition itemDef)
    {
        // 1. Ir a la caja de entrada de esta mesa y recoger (instanciar) el objeto
        yield return MoveTo(_myTable.BoxPointPos);

        GameObject itemGO = SpawnItemVisual(itemDef);
        PickUpInstant(itemGO);

        // 2. Llevarlo al puesto de trabajo y procesarlo
        yield return MoveTo(_myTable.PlayerSlotPos);
        PutDown(itemGO, _myTable.ItemSlotPos);

        bool gotStar = false;
        yield return ProcessRoutine(itemDef, result => gotStar = result);

        BestiaryManager.Instance?.RegisterItem(itemDef, gotStar ? 1 : 0);

        yield return PickUpAnim(itemGO);

        // 3. Determinar destino: la siguiente mesa desbloqueada, o entrega directa al cliente
        WorkTable nextTable = _workStation.GetNextUnlockedDesk(_myTable);

        if (nextTable != null)
        {
            yield return MoveTo(nextTable.BoxPointPos);
            nextTable.EnqueueItem(itemDef);
            Destroy(itemGO);
        }
        else
        {
            // Espera hasta que haya un cliente esperando en la cola de recogida
            Vector3 pickupPos;
            while (!CustomerManager.Instance.TryGetFirstPickupPosition(out pickupPos))
                yield return null;

            yield return MoveTo(pickupPos);

            itemGO.transform.SetParent(null);
            CustomerManager.Instance.DeliverToFirstPickupCustomer(itemGO);

            _workStation.OnWorkCompleted();
        }

        // 4. Volver a esperar en su puesto
        yield return MoveTo(idlePosition.position);

        _isWorking = false;
        TryStartNextItem();
    }

    private GameObject SpawnItemVisual(ItemDefinition itemDef)
    {
        GameObject go = Instantiate(itemDef.itemPrefab);

        if (go.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.sprite = itemDef.sprite;
            sr.sortingOrder = 2;
        }

        go.transform.localScale = new Vector3(itemDef.displayScale.x, itemDef.displayScale.y, 1f);
        return go;
    }

    private IEnumerator ProcessRoutine(ItemDefinition itemDef, System.Action<bool> onComplete)
    {
        float processTime = _myTable.GetProcessTime(itemDef);
        float elapsed = 0f;

        progressUI?.Show(0f);

        while (elapsed < processTime)
        {
            elapsed += ConsumeTapBoost();
            elapsed += Time.deltaTime;
            elapsed = Mathf.Min(elapsed, processTime);
            progressUI?.SetFill(elapsed / processTime);
            yield return null;
        }

        progressUI?.SetFill(1f);
        progressUI?.Hide();

        bool gotStar = _myTable.RollStar();
        if (gotStar)
            StarPopupSpawner.Instance?.Spawn(_myTable.ItemSlotPos);

        int reward = _myTable.GetReward(itemDef);
        EconomyManager.Instance?.AddCoins(reward);

        onComplete?.Invoke(gotStar);
    }

    private void OnDestroy()
    {
        WorkerRegistry.Instance?.Unregister(this);
        if (_myTable != null)
            _myTable.OnItemEnqueued -= HandleItemEnqueued;
    }
}