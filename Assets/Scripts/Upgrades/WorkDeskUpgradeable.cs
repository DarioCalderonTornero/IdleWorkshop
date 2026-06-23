using UnityEngine;

public class WorkDeskUpgradeable : UpgradeableBase
{
    [Header("Efecto por nivel")]
    [Tooltip("Reducción de tiempo de procesado por nivel (ej: 0.1 = -10% por nivel)")]
    [SerializeField] private float processTimeReductionPerLevel = 0.1f;

    private WorkTable _workTable;

    void Awake()
    {
        _workTable = GetComponent<WorkTable>();

        if (_workTable == null)
            Debug.LogWarning("[WorkDeskUpgradeable] No se encontró WorkTable en este GameObject.");
    }

    protected override void OnUpgraded(int newLevel)
    {
        if (_workTable == null) return;

        float multiplier = Mathf.Max(0.1f, 1f - (newLevel * processTimeReductionPerLevel));
        _workTable.ApplyProcessTimeMultiplier(multiplier);

        Debug.Log($"[WorkDeskUpgradeable] Nivel {newLevel} — procesado ×{multiplier:F2}");
    }
}