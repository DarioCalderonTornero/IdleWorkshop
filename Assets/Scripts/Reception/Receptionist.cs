// Receptionist.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Atiende el mostrador de entrega. No se mueve del sitio: el objeto que deja
/// el cliente salta solo al escritorio trasero, donde se va acumulando
/// (paso 3 de la hoja de diseño).
///
/// Si la ReceptionDesk no tiene escritorio trasero asignado, cae al
/// comportamiento antiguo: llevarlo a la caja de la primera mesa desbloqueada.
/// </summary>
public class Receptionist : WorkerBase
{
    [Header("Saltito del objeto")]
    [Tooltip("Lo que tarda en atender el objeto antes de mandarlo al escritorio")]
    [SerializeField] private float handleDelay = 0.25f;

    [Tooltip("Cuánto dura el desplazamiento del objeto, en segundos")]
    [SerializeField] private float hopDuration = 0.4f;

    [Tooltip("Altura del arco que describe el objeto al saltar")]
    [SerializeField] private float hopHeight = 0.4f;

    private ReceptionDesk _receptionDesk;
    private WorkStation _workStation;
    private bool _isWorking;

    public void Init(ReceptionDesk receptionDesk, WorkStation workStation)
    {
        _receptionDesk = receptionDesk;
        _workStation = workStation;

        // Misma razón que en Worker.Init: Receptionist es un prefab anidado
        // dentro de ReceptionDesk y no puede referenciar su PlayerPoint por
        // Inspector, así que se asigna aquí en tiempo de ejecución.
        idlePosition = receptionDesk.PlayerPointTransform;

        if (idlePosition != null)
            transform.position = idlePosition.position;

        WorkerRegistry.Instance?.Register(this);

        _receptionDesk.OnItemWaiting += HandleItemWaiting;

        TryStartNextItem();
    }

    private void HandleItemWaiting()
    {
        if (!_isWorking)
            TryStartNextItem();
    }

    private void TryStartNextItem()
    {
        if (_isWorking) return;
        if (!_receptionDesk.TryTakePendingItem(out ItemOrder order, out GameObject itemGO)) return;

        _isWorking = true;
        StartCoroutine(DeliverRoutine(order, itemGO));
    }

    private IEnumerator DeliverRoutine(ItemOrder order, GameObject itemGO)
    {
        yield return new WaitForSeconds(_receptionDesk.ServiceTime(handleDelay));

        IItemContainer destination = GetDestination();

        if (destination != null && itemGO != null)
        {
            while (!destination.HasSpace)
                yield return null;

            yield return ItemHop.Move(
                itemGO.transform, itemGO.transform.position, destination.ContentsPos,
                _receptionDesk.ServiceTime(hopDuration), hopHeight);

            destination.TryEnqueue(order);
        }

        if (itemGO != null) Destroy(itemGO);

        _isWorking = false;
        TryStartNextItem();
    }

    private IItemContainer GetDestination()
    {
        if (_receptionDesk.BackStack != null)
            return _receptionDesk.BackStack;

        var desks = _workStation.GetUnlockedDesks();
        return desks.Count > 0 ? desks[0] : null;
    }

    private void OnDestroy()
    {
        WorkerRegistry.Instance?.Unregister(this);
        if (_receptionDesk != null)
            _receptionDesk.OnItemWaiting -= HandleItemWaiting;
    }
}
