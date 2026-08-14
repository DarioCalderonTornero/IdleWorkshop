using UnityEngine;

public class WorkerUpgradeable : UpgradeableBase
{
    [Header("Efecto por nivel")]
    [Tooltip("Incremento de velocidad de movimiento por nivel (ej: 0.1 = +10% por nivel)")]
    [SerializeField] private float moveSpeedMultiplierPerLevel = 0.1f;

    private WorkerBase _worker;

    protected override void Awake()
    {
        base.Awake();

        _worker = GetComponentInChildren<WorkerBase>();
        if (_worker == null)
            Debug.LogWarning("[WorkerUpgradeable] No se encontró WorkerBase.");
    }

    protected override void OnUpgraded(int newLevel)
    {
        if (_worker == null) return;

        float multiplier = 1f + (newLevel * moveSpeedMultiplierPerLevel);
        _worker.ApplyMoveSpeedMultiplier(multiplier);
        Debug.Log($"[WorkerUpgradeable] Nivel {newLevel} — velocidad ×{multiplier:F2}");
    }
}