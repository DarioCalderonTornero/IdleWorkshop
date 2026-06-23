using UnityEngine;

/// <summary>
/// Mejoras de la mesa de recepción.
/// Por ahora el nivel controla la velocidad con la que acepta clientes.
/// </summary>
public class ReceptionDeskUpgradeable : UpgradeableBase
{
    [Header("Efecto por nivel")]
    [Tooltip("Reducción de tiempo de espera por nivel (en segundos)")]
    [SerializeField] private float waitReductionPerLevel = 0.5f;

    public float CurrentWaitReduction => CurrentLevel * waitReductionPerLevel;

    protected override void OnUpgraded(int newLevel)
    {
        // Aquí aplicarás el efecto real cuando lo tengas implementado
        // Ej: workTable.SetWaitReduction(CurrentWaitReduction);
        Debug.Log($"[ReceptionDesk] Nivel {newLevel} — reducción espera: {CurrentWaitReduction}s");
    }
}