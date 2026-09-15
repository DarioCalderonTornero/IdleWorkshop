using UnityEngine;

/// <summary>
/// Mejora decorativa: sube lo que paga cada objeto y, según el nivel, va
/// revelando las piezas de la decoración.
///
/// Suma una cantidad fija por nivel a CADA parte del proceso que cobra, no un
/// porcentaje. Con <see cref="coinsPerLevel"/> = 1 y nivel 3, un objeto que
/// daba 15 monedas por fase pasa a dar 18 en cada una.
///
/// Los tramos de evolución se usan aquí como umbrales de aparición: un tramo
/// por pieza, y <see cref="UpgradeData.GetReachedStageCount"/> dice cuántas se
/// ven. El campo "visual" de cada tramo no se usa en este tipo de mejora —la
/// decoración no cambia de sprite, aparece a trozos— así que se deja vacío.
/// </summary>
[CreateAssetMenu(fileName = "DecorationUpgrade", menuName = "Idle/Mejoras/Decoración")]
public class DecorationUpgradeData : UpgradeData
{
    [Header("Efecto por nivel")]
    [Tooltip("Monedas extra por nivel que se suman a CADA parte del proceso que paga")]
    public int coinsPerLevel = 1;

    /// <summary>
    /// Cuántas piezas de la decoración se ven con este nivel. Es el mismo
    /// número que marca la barra del panel, así que lo que se ve y lo que
    /// anuncia la barra no pueden descuadrarse.
    /// </summary>
    public int VisiblePieces(int currentLevel) => GetReachedStageCount(currentLevel);
}
