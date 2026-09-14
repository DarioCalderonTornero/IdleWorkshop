// ReceptionDesk.cs
using System;
using UnityEngine;

/// <summary>
/// Mostrador de entrega: el cliente deja aquí su objeto y el recepcionista lo
/// pasa a la bolsa del escritorio de detrás.
/// </summary>
public class ReceptionDesk : MonoBehaviour
{
    [Header("Puntos de referencia")]
    [SerializeField] private Transform customerPoint;
    [SerializeField] private Transform playerPoint;
    [SerializeField] private Transform objectPoint;
    [SerializeField] private Transform finalBoxPoint;

    [Header("Escritorio trasero (paso 3)")]
    [Tooltip("Bolsa donde el recepcionista acumula los encargos y de la que carga el carrito. " +
             "Si se deja vacía, el recepcionista los lleva directamente a la primera mesa.")]
    [SerializeField] private ItemStack backStack;
    public ItemStack BackStack => backStack;

    [Header("Progreso")]
    [Tooltip("Círculo que se llena mientras el cliente entrega su objeto en el mostrador")]
    [SerializeField] private RepairProgressUI progressUI;
    public RepairProgressUI ProgressUI => progressUI;

    [Header("Velocidad de atención")]
    [Tooltip("Lo que baja la mejora de este mostrador. Si es null, no se mejora nada")]
    [SerializeField] private ServiceSpeed serviceSpeed;

    /// <summary>Lo que tarda de verdad una acción en este mostrador, ya mejorada.</summary>
    public float ServiceTime(float baseDuration) =>
        serviceSpeed != null ? serviceSpeed.Apply(baseDuration) : baseDuration;

    public Vector3 CustomerPointPos => customerPoint.position;
    public Vector3 PlayerPointPos => playerPoint.position;
    public Vector3 ObjectPointPos => objectPoint.position;
    public Vector3 FinalBoxPointPos => finalBoxPoint.position;

    // Expuesto para que el Receptionist pueda usar este mismo Transform
    // como su posición idle (ver Receptionist.Init).
    public Transform PlayerPointTransform => playerPoint;

    // ── Slot de entrada: objeto dejado por el cliente, esperando al Receptionist ──
    private ItemOrder _pendingOrder;
    private GameObject _pendingItemGO;
    private bool _hasPendingItem;

    public bool HasFreeSlot => !_hasPendingItem;

    public event Action OnItemWaiting;
    public event Action OnSlotFreed;

    public bool TryDepositFromCustomer(ItemOrder order, GameObject itemGO)
    {
        if (_hasPendingItem) return false;

        _pendingOrder = order;
        _pendingItemGO = itemGO;
        _hasPendingItem = true;
        OnItemWaiting?.Invoke();
        return true;
    }

    public bool TryTakePendingItem(out ItemOrder order, out GameObject itemGO)
    {
        if (!_hasPendingItem)
        {
            order = null;
            itemGO = null;
            return false;
        }

        order = _pendingOrder;
        itemGO = _pendingItemGO;
        _hasPendingItem = false;
        _pendingOrder = null;
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
