using UnityEngine;

/// <summary>
/// Mejora de un trabajador: se mueve más rápido por nivel.
/// </summary>
public class WorkerUpgradeable : UpgradeableBase
{
    private WorkerBase _worker;

    public override System.Type ExpectedDataType => typeof(WorkerUpgradeData);

    private WorkerUpgradeData Data => RequireData<WorkerUpgradeData>();

    protected override void Awake()
    {
        base.Awake();

        _worker = GetComponentInChildren<WorkerBase>();
        if (_worker == null)
            Debug.LogWarning($"[WorkerUpgradeable] {name}: no encuentro ningún WorkerBase.", this);

        _ = Data;
    }

    protected override void OnUpgraded(int newLevel)
    {
        WorkerUpgradeData data = Data;
        if (_worker == null || data == null) return;

        float multiplier = 1f + newLevel * data.moveSpeedMultiplierPerLevel;
        _worker.ApplyMoveSpeedMultiplier(multiplier);

        Debug.Log($"[WorkerUpgradeable] Nivel {newLevel} - velocidad x{multiplier:F2}");
    }
}
