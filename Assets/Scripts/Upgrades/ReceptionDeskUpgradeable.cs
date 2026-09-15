using UnityEngine;

/// <summary>
/// Mejoras de la mesa de recepci�n.
/// Por ahora el nivel controla la velocidad con la que acepta clientes.
/// </summary>
public class ReceptionDeskUpgradeable : UpgradeableBase
{
    [Header("Efecto por nivel")]
    [Tooltip("Reducci�n de tiempo de espera por nivel (en segundos)")]
    [SerializeField] private float waitReductionPerLevel = 0.5f;

    // No lee nada específico del SO: le vale cualquiera.
    public override System.Type ExpectedDataType => typeof(UpgradeData);

    public float CurrentWaitReduction => CurrentLevel * waitReductionPerLevel;

    protected override void OnUpgraded(int newLevel)
    {
        // Aqu� aplicar�s el efecto real cuando lo tengas implementado
        // Ej: workTable.SetWaitReduction(CurrentWaitReduction);
        Debug.Log($"[ReceptionDesk] Nivel {newLevel} � reducci�n espera: {CurrentWaitReduction}s");
    }
}