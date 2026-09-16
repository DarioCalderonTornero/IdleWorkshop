using UnityEngine;

/// <summary>
/// Mejora de lo rápido que los carritos cargan y descargan.
///
/// No es lo mismo que <see cref="CartUpgradeData"/>: aquella hace que el
/// carrito se mueva más rápido entre sitios, y esta que tarde menos en meter y
/// sacar cada encargo. Afecta solo a los carritos, y a las dos operaciones por
/// igual.
/// </summary>
[CreateAssetMenu(fileName = "CartHandlingUpgrade", menuName = "Idle/Mejoras/Carga y descarga")]
public class CartHandlingUpgradeData : UpgradeData
{
    [Header("Efecto por nivel")]
    [Tooltip("Reducción del tiempo de carga y descarga por nivel (0.05 = -5% por nivel)")]
    public float timeReductionPerLevel = 0.05f;

    [Tooltip("Por muchos niveles que se suban, nunca baja de aquí")]
    [Range(0.05f, 1f)]
    public float minMultiplier = 0.2f;

    /// <summary>Lo que multiplica a los tiempos de carga y descarga en este nivel.</summary>
    public float GetMultiplierForLevel(int level)
        => Mathf.Max(minMultiplier, 1f - Mathf.Max(0, level - 1) * timeReductionPerLevel);
}
