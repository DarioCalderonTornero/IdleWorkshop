using UnityEngine;

public class WorkDeskUpgradeable : UpgradeableBase
{
    private WorkTable _workTable;

    void Awake()
    {
        _workTable = GetComponent<WorkTable>();
        if (_workTable == null)
            Debug.LogWarning("[WorkDeskUpgradeable] No se encontró WorkTable.");
    }

    protected override void OnUpgraded(int newLevel)
    {
        if (_workTable == null || UpgradeData == null) return;

        float timeMultiplier = Mathf.Max(0.1f,
            1f - (newLevel - 1) * UpgradeData.timeReductionPerLevel);

        float rewardMultiplier =
            1f + (newLevel - 1) * UpgradeData.rewardIncreasePerLevel;

        _workTable.ApplyMultipliers(timeMultiplier, rewardMultiplier);
        _workTable.ApplyUpgradeData(UpgradeData, newLevel);

        Debug.Log($"[WorkDeskUpgradeable] Nivel {newLevel} — " +
                  $"tiempo ×{timeMultiplier:F2} — recompensa ×{rewardMultiplier:F2} — " +
                  $"estrella {UpgradeData.GetStarChanceForLevel(newLevel):F1}%");
    }
}