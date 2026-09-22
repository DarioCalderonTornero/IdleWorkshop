// Worker.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Trabajador de una mesa concreta. No se mueve de su puesto: es el objeto el
/// que va solo, dando un saltito de la zona de espera al punto de proceso y,
/// al terminar, a la mesa siguiente.
///
/// Si esta es la última mesa desbloqueada, el objeto salta a la parte contraria
/// de la mesa y se queda ahí hasta que pase el carrito de salida.
/// </summary>
public class Worker : WorkerBase
{
    [Header("UI")]
    [SerializeField] private RepairProgressUI progressUI;

    [Header("Saltito del objeto")]
    [Tooltip("Cuánto dura el desplazamiento del objeto, en segundos")]
    [SerializeField] private float hopDuration = 0.35f;

    [Tooltip("Altura del arco que describe el objeto al saltar")]
    [SerializeField] private float hopHeight = 0.35f;

    private WorkTable _myTable;
    private WorkStation _workStation;
    private bool _isWorking;

    public void Init(WorkTable myTable, WorkStation workStation)
    {
        _myTable = myTable;
        _workStation = workStation;

        // La posición idle es el PlayerSlot de la propia mesa: al ser Worker
        // un prefab anidado dentro de WorkDesk, no puede referenciarlo por
        // Inspector (contextos de prefab distintos), así que se asigna aquí.
        idlePosition = myTable.PlayerSlotTransform;

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
        if (!_myTable.TryDequeue(out ItemOrder order)) return;

        _isWorking = true;
        StartCoroutine(WorkRoutine(order));
    }

    private IEnumerator WorkRoutine(ItemOrder order)
    {
        // 1. El objeto sale de la zona de espera y salta al punto de proceso
        GameObject itemGO = ItemVisual.Spawn(order.Definition);

        if (itemGO == null)
        {
            Debug.LogError($"[Worker] '{order.Definition.itemName}' no tiene itemPrefab: no se puede procesar.", this);
            _isWorking = false;
            yield break;
        }

        Vector3 waitingPos = _myTable.ContentsPos;
        itemGO.transform.position = waitingPos;

        yield return ItemHop.Move(itemGO.transform, waitingPos, _myTable.ItemSlotPos, hopDuration, hopHeight);

        // 2. Procesarlo
        bool gotStar = false;
        yield return ProcessRoutine(order.Definition, result => gotStar = result);

        BestiaryManager.Instance?.RegisterItem(order.Definition, gotStar ? 1 : 0);

        // 3. Saltar al destino: la mesa siguiente, o la parte contraria de esta
        IItemContainer destination = ResolveDestination(out bool isWorkshopExit);

        if (destination == null)
        {
            // Sin sitio donde dejarlo el objeto se perdería, y con él su dueño
            // esperando para siempre. Mejor dejarlo encima de la mesa y avisar.
            Debug.LogError(
                $"[Worker] {name}: la mesa no tiene zona de terminados ni mesa siguiente. " +
                "Asigna 'outStack' en la WorkTable o el objeto no llegará nunca al cliente.", this);

            _isWorking = false;
            yield break;
        }

        yield return ItemHop.Move(
            itemGO.transform, _myTable.ItemSlotPos, destination.ContentsPos, hopDuration, hopHeight);

        while (!destination.HasSpace)
            yield return null;

        destination.TryEnqueue(order);
        Destroy(itemGO);

        if (isWorkshopExit)
            _workStation.OnWorkCompleted();

        _isWorking = false;
        TryStartNextItem();
    }

    /// <summary>
    /// La siguiente mesa desbloqueada; si no hay ninguna, la parte contraria de
    /// esta mesa, que es de donde recoge el carrito de salida.
    /// </summary>
    private IItemContainer ResolveDestination(out bool isWorkshopExit)
    {
        WorkTable nextTable = _workStation.GetNextUnlockedDesk(_myTable);

        if (nextTable != null)
        {
            isWorkshopExit = false;
            return nextTable;
        }

        isWorkshopExit = true;
        return _myTable.OutStack;
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
        EconomyManager.Instance?.AddCoins(reward, CoinSource.Production);

        onComplete?.Invoke(gotStar);
    }

    private void OnDestroy()
    {
        WorkerRegistry.Instance?.Unregister(this);
        if (_myTable != null)
            _myTable.OnItemEnqueued -= HandleItemEnqueued;
    }
}
