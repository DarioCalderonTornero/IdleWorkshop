// Receptionist.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Transporta objetos desde la recepción hasta la caja de entrada de la
/// primera mesa desbloqueada del taller. No procesa nada — solo recoge
/// del slot de espera de ReceptionDesk y entrega, en bucle continuo.
/// </summary>
public class Receptionist : WorkerBase
{
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
        if (!_receptionDesk.TryTakePendingItem(out ItemDefinition itemDef, out GameObject itemGO)) return;

        _isWorking = true;
        StartCoroutine(DeliverRoutine(itemDef, itemGO));
    }

    private IEnumerator DeliverRoutine(ItemDefinition itemDef, GameObject itemGO)
    {
        // 1. Ir a recoger el objeto dejado por el cliente
        yield return MoveTo(_receptionDesk.ObjectPointPos);
        PickUpInstant(itemGO);

        // 2. Llevarlo hasta la caja de la primera mesa desbloqueada
        WorkTable firstDesk = GetFirstUnlockedDesk();

        if (firstDesk != null)
        {
            yield return MoveTo(firstDesk.BoxPointPos);
            firstDesk.EnqueueItem(itemDef);
        }

        Destroy(itemGO);

        // 3. Volver a esperar (libera el slot de recepción para el siguiente cliente)
        yield return MoveTo(idlePosition.position);

        _isWorking = false;
        TryStartNextItem();
    }

    private WorkTable GetFirstUnlockedDesk()
    {
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