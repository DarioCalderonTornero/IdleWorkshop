using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El saco que lleva el carrito. Se ve siempre y da un golpecito cada vez que
/// entra o sale algo, igual que los sacos fijos.
///
/// Es el único saco con tope: marca cuántos encargos se lleva por viaje, y por
/// tanto cuántos tiene que haber esperando para que le salga a cuenta salir.
/// </summary>
public class Cart : MonoBehaviour
{
    [Header("Saco")]
    [SerializeField] private Sack sack;

    [Header("Carga")]
    [Tooltip("Cuántos encargos se lleva por viaje")]
    [SerializeField] private int capacity = 5;

    public int Capacity => capacity;

    /// <summary>La boca del saco: destino de los saltitos al cargar.</summary>
    public Vector3 ContentsPos => sack != null ? sack.MouthPos : transform.position;

    public void SetLoad(IReadOnlyList<ItemOrder> orders) { }

    /// <summary>Avisa de que acaba de entrar o salir algo, para el golpecito.</summary>
    public void SetLoadWithPop(IReadOnlyList<ItemOrder> orders) => sack?.Pop();
}
