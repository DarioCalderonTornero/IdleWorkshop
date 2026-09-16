using UnityEngine;

/// <summary>
/// Mejora de un trabajador de sala: cada nivel le hace pagar más por tanda y
/// tardar un poco menos.
/// </summary>
public class IdleWorkerUpgradeable : UpgradeableBase
{
    [Tooltip("El trabajador a mejorar. Si es null, se busca en este GameObject y sus hijos")]
    [SerializeField] private IdleWorker worker;

    public override System.Type ExpectedDataType => typeof(IdleWorkerUpgradeData);

    private IdleWorkerUpgradeData Data => RequireData<IdleWorkerUpgradeData>();

    protected override void Awake()
    {
        base.Awake();

        if (worker == null)
            worker = GetComponentInChildren<IdleWorker>(includeInactive: true);

        if (worker == null)
            Debug.LogWarning($"[IdleWorkerUpgradeable] {name}: no encuentro ningún IdleWorker que mejorar.", this);

        _ = Data;
    }

    /// <summary>
    /// El nivel 1 no pasa por OnUpgraded, así que sin esto el trabajador se
    /// quedaría con los valores por defecto del componente en vez de con los
    /// de su ScriptableObject.
    /// </summary>
    private void Start() => Apply(CurrentLevel);

    protected override void OnUpgraded(int newLevel) => Apply(newLevel);

    private void Apply(int level)
    {
        IdleWorkerUpgradeData data = Data;
        if (worker == null || data == null) return;

        worker.Configure(data.GetCoinsForLevel(level), data.GetCycleTimeForLevel(level));
    }
}
