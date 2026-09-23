using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Algo que, en un taller recién abierto, todavía no se ve porque se compra
/// aparte: las piezas de una decoración, el trabajador de una sala, el jefe.
///
/// Lo usa la vista en gris de un taller sin comprar para no enseñarlo. Esa
/// vista tiene que parecerse a lo que el jugador va a tener al pagar; si
/// enseñara también esto, vería un taller lleno de plantas y asientos que
/// luego no le dan.
///
/// Si añades algo nuevo que empieza escondido hasta comprarlo, impleméntalo y
/// la vista en gris lo dejará fuera sola.
/// </summary>
public interface IHiddenUntilBought
{
    /// <summary>Las partes que no se ven hasta que se compran.</summary>
    IEnumerable<Transform> HiddenParts { get; }
}
