using UnityEngine;

/// <summary>
/// Mejora decorativa que sube lo que paga cada objeto: los asientos del hall.
///
/// Cuánto suma por nivel lo dice su <see cref="DecorationUpgradeData"/>, igual
/// que el coste y los umbrales a los que van apareciendo las piezas.
///
/// Mientras esté bloqueada no aporta nada.
/// </summary>
public class CoinBonusUpgradeable : UpgradeableBase
{
    [Tooltip("Mientras esté bloqueado no da bonus. Si es null, cuenta desde el principio")]
    [SerializeField] private Unlockable unlockable;

    public override System.Type ExpectedDataType => typeof(DecorationUpgradeData);

    private DecorationUpgradeData Data => RequireData<DecorationUpgradeData>();

    /// <summary>Lo que aporta ahora mismo. 0 si sigue bloqueado.</summary>
    public int CurrentBonus
    {
        get
        {
            if (unlockable != null && !unlockable.IsUnlocked) return 0;

            DecorationUpgradeData data = Data;
            return data == null ? 0 : CurrentLevel * data.coinsPerLevel;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (unlockable != null)
            unlockable.OnUnlocked += HandleUnlocked;

        _ = Data;
    }

    private void Start() => ApplyBonus();

    private void HandleUnlocked(Unlockable _) => ApplyBonus();

    protected override void OnUpgraded(int newLevel) => ApplyBonus();

    private void ApplyBonus() => CoinBonusRegistry.Set(this, CurrentBonus);

    private void OnDestroy()
    {
        if (unlockable != null)
            unlockable.OnUnlocked -= HandleUnlocked;

        CoinBonusRegistry.Remove(this);
    }
}
