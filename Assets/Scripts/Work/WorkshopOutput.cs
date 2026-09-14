using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La salida del taller, vista desde fuera. No guarda nada: redirige a la parte
/// contraria de la última mesa desbloqueada.
///
/// Existe porque esa mesa cambia según lo que el jugador haya comprado — con
/// una sola mesa, los objetos terminados quedan en la mesa 1; al desbloquear la
/// 2, pasan a quedar en la 2. El carrito de salida apunta aquí y así va a
/// recogerlos donde toque sin tener que enterarse de los desbloqueos.
/// </summary>
public class WorkshopOutput : MonoBehaviour, IItemContainer
{
    [SerializeField] private WorkStation workStation;

    private ItemStack Current
    {
        get
        {
            if (workStation == null) return null;

            // Primero lo que haya quedado pendiente en mesas anteriores: al
            // desbloquear una mesa nueva la salida se muda a ella, y si no se
            // vaciase la anterior esos objetos no los recogería nadie.
            WorkTable pending = workStation.GetDeskWithPendingOutput();
            if (pending != null) return pending.OutStack;

            WorkTable lastDesk = workStation.GetLastUnlockedDesk();
            return lastDesk != null ? lastDesk.OutStack : null;
        }
    }

    public int Count => Current != null ? Current.Count : 0;
    public int Capacity => Current != null ? Current.Capacity : 0;
    public bool HasSpace => Current != null && Current.HasSpace;
    public bool IsFull => Current != null && Current.IsFull;

    public Vector3 AccessPointPos
    {
        get
        {
            ItemStack current = Current;
            return current != null ? current.AccessPointPos : transform.position;
        }
    }

    public Vector3 ContentsPos
    {
        get
        {
            ItemStack current = Current;
            return current != null ? current.ContentsPos : transform.position;
        }
    }

    public bool TryEnqueue(ItemOrder order)
    {
        ItemStack current = Current;
        return current != null && current.TryEnqueue(order);
    }

    public bool TryDequeue(out ItemOrder order)
    {
        ItemStack current = Current;

        if (current == null)
        {
            order = null;
            return false;
        }

        return current.TryDequeue(out order);
    }
}
