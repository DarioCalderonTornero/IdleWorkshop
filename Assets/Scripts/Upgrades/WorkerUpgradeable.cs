using UnityEngine;

/// <summary>
/// Mejora de un trabajador: se mueve más rápido por nivel.
/// </summary>
public class WorkerUpgradeable : UpgradeableBase
{
    private WorkerBase _worker;

    public override System.Type ExpectedDataType => typeof(WorkerUpgradeData);

    /// <summary>
    /// El trabajador no se guarda por id, igual que la mesa a la que pertenece.
    ///
    /// Vive dentro de Worker.prefab, anidado en WorkDesk.prefab, del que hay
    /// una copia por mesa. Un id puesto en el prefab sería el mismo en las tres
    /// copias, así que las tres se pisarían el nivel entre ellas: es justo lo
    /// que pasaba, y el registro lo cazó al arrancar.
    ///
    /// Si algún día el nivel del trabajador tiene que persistir, su sitio es
    /// DeskSaveData —junto al nivel de su mesa—, que se guarda por stationId y
    /// deskIndex y por eso sí funciona con contenido de prefab.
    /// </summary>
    public override bool SavesItself => false;

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
