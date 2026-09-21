using UnityEngine;

/// <summary>
/// Mejora de una mesa de limpieza: más rápida, paga más y saca más estrellas.
/// </summary>
public class WorkDeskUpgradeable : UpgradeableBase
{
    private WorkTable _workTable;

    public override System.Type ExpectedDataType => typeof(WorkDeskUpgradeData);

    /// <summary>
    /// La mesa no se guarda por id, la guarda su taller por posición
    /// (stationId + deskIndex).
    ///
    /// Es la única forma que funciona para ella: las mesas viven dentro de
    /// WorkDesk.prefab y los talleres 2 en adelante se instancian desde
    /// prefab en tiempo de ejecución, así que un id autorizado sería el mismo
    /// en las mesas de todos los talleres y se pisarían el nivel entre ellas.
    /// </summary>
    public override bool SavesItself => false;

    private WorkDeskUpgradeData Data => RequireData<WorkDeskUpgradeData>();

    protected override void Awake()
    {
        base.Awake();

        _workTable = GetComponent<WorkTable>();
        if (_workTable == null)
            Debug.LogWarning($"[WorkDeskUpgradeable] {name}: no encuentro ninguna WorkTable.", this);

        _ = Data;   // valida el tipo del asset al arrancar, no al primer nivel
    }

    protected override void OnUpgraded(int newLevel)
    {
        WorkDeskUpgradeData data = Data;
        if (_workTable == null || data == null) return;

        float timeMultiplier = Mathf.Max(0.1f,
            1f - (newLevel - 1) * data.timeReductionPerLevel);

        float rewardMultiplier =
            1f + (newLevel - 1) * data.rewardIncreasePerLevel;

        _workTable.ApplyMultipliers(timeMultiplier, rewardMultiplier);
        _workTable.ApplyUpgradeData(data, newLevel);

        Debug.Log($"[WorkDeskUpgradeable] Nivel {newLevel} - " +
                  $"tiempo x{timeMultiplier:F2}, recompensa x{rewardMultiplier:F2}, " +
                  $"estrella {data.GetStarChanceForLevel(newLevel):F1}%");
    }
}
