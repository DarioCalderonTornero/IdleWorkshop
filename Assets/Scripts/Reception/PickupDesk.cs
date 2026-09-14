using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mesa de recogida (pasos 12 y 13): el carrito de salida deja aquí la bolsa de
/// objetos ya restaurados y el recepcionista los va entregando de uno en uno.
///
/// Cada encargo va a su dueño, no al primero que pase: por eso se busca en la
/// bolsa el objeto del cliente que toca en vez de sacar el más antiguo.
/// </summary>
public class PickupDesk : MonoBehaviour
{
    [Header("Puntos de referencia")]
    [Tooltip("Donde se para el primer cliente de la cola de recogida")]
    [SerializeField] private Transform customerPoint;

    [Tooltip("Donde está el recepcionista que entrega los objetos")]
    [SerializeField] private Transform playerPoint;

    [Tooltip("Punto intermedio por el que pasa el objeto de camino al cliente: " +
             "las manos del recepcionista. Si es null, va directo de la bolsa al cliente")]
    [SerializeField] private Transform handoverPoint;

    [Header("Objetos terminados")]
    [Tooltip("Saco donde el carrito descarga y del que se entrega a los clientes")]
    [SerializeField] private ItemStack stack;

    [Header("Progreso")]
    [Tooltip("Círculo que se llena mientras el cliente recoge su objeto")]
    [SerializeField] private RepairProgressUI progressUI;
    public RepairProgressUI ProgressUI => progressUI;

    [Header("Velocidad de atención")]
    [Tooltip("Lo que baja la mejora de este mostrador. Si es null, no se mejora nada")]
    [SerializeField] private ServiceSpeed serviceSpeed;

    /// <summary>Lo que tarda de verdad una acción en este mostrador, ya mejorada.</summary>
    public float ServiceTime(float baseDuration) =>
        serviceSpeed != null ? serviceSpeed.Apply(baseDuration) : baseDuration;

    public Vector3 CustomerPointPos => customerPoint != null ? customerPoint.position : transform.position;
    public Vector3 PlayerPointPos => playerPoint != null ? playerPoint.position : transform.position;
    public ItemStack Stack => stack;
    public bool HasItem => stack != null && stack.Count > 0;

    /// <summary>Lo que hay en el saco, para saber a qué clientes hay que llamar.</summary>
    public IReadOnlyList<ItemOrder> Orders =>
        stack != null ? stack.Orders : System.Array.Empty<ItemOrder>();

    /// <summary>Coge del saco el encargo de este cliente concreto.</summary>
    public bool TryTakeOrderFor(int ticketId, out ItemOrder order)
    {
        if (stack == null)
        {
            order = null;
            return false;
        }

        return stack.TryTake(candidate => candidate.TicketId == ticketId, out order);
    }

    /// <summary>Punto por el que pasa el objeto antes de llegar al cliente.</summary>
    public bool TryGetHandoverPos(out Vector3 position)
    {
        if (handoverPoint == null)
        {
            position = Vector3.zero;
            return false;
        }

        position = handoverPoint.position;
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (customerPoint == null) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(customerPoint.position, 0.12f);
    }
#endif
}
