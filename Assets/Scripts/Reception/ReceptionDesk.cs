using System;
using UnityEngine;

public class ReceptionDesk : MonoBehaviour
{
    [Header("Puntos de referencia")]
    [SerializeField] private Transform customerPoint;
    [SerializeField] private Transform playerPoint;
    [SerializeField] private Transform objectPoint;
    [SerializeField] private Transform finalBoxPoint;

    public Vector3 CustomerPointPos => customerPoint.position;
    public Vector3 PlayerPointPos => playerPoint.position;
    public Vector3 ObjectPointPos => objectPoint.position;
    public Vector3 FinalBoxPointPos => finalBoxPoint.position;

    // ── Slot de entrada: objeto dejado por el cliente, esperando al Receptionist ──
    private ItemDefinition _pendingItem;
    private GameObject _pendingItemGO;
    private bool _hasPendingItem;

    public bool HasFreeSlot => !_hasPendingItem;

    public event Action OnItemWaiting;
    public event Action OnSlotFreed;

    public bool TryDepositFromCustomer(ItemDefinition itemDef, GameObject itemGO)
    {
        if (_hasPendingItem) return false;

        _pendingItem = itemDef;
        _pendingItemGO = itemGO;
        _hasPendingItem = true;
        OnItemWaiting?.Invoke();
        return true;
    }

    public bool TryTakePendingItem(out ItemDefinition itemDef, out GameObject itemGO)
    {
        if (!_hasPendingItem)
        {
            itemDef = null;
            itemGO = null;
            return false;
        }

        itemDef = _pendingItem;
        itemGO = _pendingItemGO;
        _hasPendingItem = false;
        _pendingItem = null;
        _pendingItemGO = null;
        OnSlotFreed?.Invoke();
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (customerPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(customerPoint.position, 0.12f); }
        if (playerPoint != null) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(playerPoint.position, 0.12f); }
        if (objectPoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(objectPoint.position, 0.12f); }
        if (finalBoxPoint != null) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(finalBoxPoint.position, 0.12f); }
    }
#endif
}