using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un saco de encargos: el escritorio de recepción (paso 3), la zona de espera
/// de una mesa (paso 6), la parte contraria de la última mesa (paso 9) y la
/// mesa de entrega al cliente (paso 12).
///
/// El saco siempre se ve. Los objetos entran y salen de él dando un saltito y
/// el saco da un golpecito en cada movimiento; no se apilan sueltos por fuera.
///
/// No tiene tope: el único que lo tiene es el carrito, que se lleva de una vez
/// tantos como quepan en él. Así los encargos se pueden ir acumulando mientras
/// el carrito está de viaje, y nadie se queda bloqueado esperando sitio.
/// </summary>
public class ItemStack : MonoBehaviour, IItemContainer
{
    [Header("Puntos")]
    [Tooltip("Punto al que se acerca quien carga o descarga. Si es null, usa este transform")]
    [SerializeField] private Transform accessPoint;

    [Header("Visual")]
    [SerializeField] private Sack sack;

    private readonly List<ItemOrder> _orders = new();

    public int Count => _orders.Count;

    /// <summary>Sin tope.</summary>
    public int Capacity => 0;
    public bool HasSpace => true;

    /// <summary>Lo que hay dentro, para poder buscar un encargo concreto.</summary>
    public IReadOnlyList<ItemOrder> Orders => _orders;

    public Vector3 AccessPointPos => accessPoint != null ? accessPoint.position : transform.position;

    /// <summary>La boca del saco: donde aterrizan los objetos que llegan.</summary>
    public Vector3 ContentsPos => sack != null ? sack.MouthPos : transform.position;

    /// <summary>
    /// Ha entrado o salido algo. Lo escucha la mesa para decidir si puede
    /// esconder su bolsa de terminados: mientras queden objetos dentro tiene
    /// que seguir viéndose, aunque ya no se use.
    /// </summary>
    public event Action OnChanged;

    private void Awake() => RefreshVisual(pop: false);

    // ── IItemContainer ───────────────────────────────────────────────
    public bool TryEnqueue(ItemOrder order)
    {
        if (order == null) return false;

        _orders.Add(order);
        RefreshVisual(pop: true);
        return true;
    }

    public bool TryDequeue(out ItemOrder order)
    {
        if (_orders.Count == 0)
        {
            order = null;
            return false;
        }

        order = _orders[0];
        _orders.RemoveAt(0);
        RefreshVisual(pop: true);
        return true;
    }

    /// <summary>
    /// Saca el encargo más antiguo que cumpla la condición. Lo usa la mesa de
    /// entrega para buscar el objeto del cliente que toca, en vez de dar el
    /// primero que haya.
    /// </summary>
    public bool TryTake(Predicate<ItemOrder> match, out ItemOrder order)
    {
        for (int i = 0; i < _orders.Count; i++)
        {
            if (!match(_orders[i])) continue;

            order = _orders[i];
            _orders.RemoveAt(i);
            RefreshVisual(pop: true);
            return true;
        }

        order = null;
        return false;
    }

    private void RefreshVisual(bool pop)
    {
        if (!pop) return;

        sack?.Pop();
        OnChanged?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.2f);
        Gizmos.DrawWireCube(AccessPointPos, new Vector3(0.3f, 0.3f, 0f));
    }
#endif
}
