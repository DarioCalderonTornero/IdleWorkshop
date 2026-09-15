using UnityEngine;

/// <summary>
/// Mejora de un trabajador: se mueve más rápido por nivel.
/// </summary>
[CreateAssetMenu(fileName = "WorkerUpgrade", menuName = "Idle/Mejoras/Trabajador")]
public class WorkerUpgradeData : UpgradeData
{
    [Header("Efecto por nivel")]
    [Tooltip("Incremento de velocidad de movimiento por nivel (0.1 = +10% por nivel)")]
    public float moveSpeedMultiplierPerLevel = 0.1f;
}
