using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El saco que lleva el carrito. Funciona igual que los sacos fijos: siempre se
/// ve, crece con la carga y da un golpecito cada vez que entra o sale algo.
/// </summary>
public class Cart : MonoBehaviour
{
    [Header("Saco")]
    [SerializeField] private Sack sack;

    [Header("Carga")]
    [Tooltip("Cuántos encargos caben. Debe coincidir con la capacidad de los sacos que transporta")]
    [SerializeField] private int capacity = 5;

    public int Capacity => capacity;

    /// <summary>La boca del saco: destino de los saltitos al cargar.</summary>
    public Vector3 ContentsPos => sack != null ? sack.MouthPos : transform.position;

    private void Awake() => Refresh(0, pop: false);

    public void SetLoad(IReadOnlyList<ItemOrder> orders) => Refresh(orders?.Count ?? 0, pop: false);

    /// <summary>Actualiza la carga marcando que acaba de entrar o salir algo.</summary>
    public void SetLoadWithPop(IReadOnlyList<ItemOrder> orders) => Refresh(orders?.Count ?? 0, pop: true);

    private void Refresh(int count, bool pop)
    {
        if (sack == null) return;

        sack.SetFill(count, capacity);
        if (pop) sack.Pop();
    }
}
