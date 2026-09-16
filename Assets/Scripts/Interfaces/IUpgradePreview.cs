using UnityEngine;

/// <summary>
/// Algo que puede enseñar por adelantado lo que va a aparecer o cambiar.
///
/// Lo implementa lo que está escondido hasta que se compra o se sube de nivel:
/// las piezas de una decoración y los trabajadores que se añaden después. Las
/// mesas no lo necesitan, porque bloqueadas ya se ven en gris.
/// </summary>
public interface IUpgradePreview
{
    /// <summary>
    /// Lo próximo que va a salir, y dónde.
    ///
    /// Devuelve false cuando no hay nada que enseñar: o está todo fuera ya, o
    /// el siguiente nivel no cambia nada que se vea.
    /// </summary>
    bool TryGetPreview(out GameObject sample, out Vector3 position);
}
