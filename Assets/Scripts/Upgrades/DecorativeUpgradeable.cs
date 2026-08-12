using UnityEngine;

public class DecorativeUpgradeable : UpgradeableBase
{
    public enum DecorativeEffectType { CoinMultiplier, SpeedMultiplier }

    [Header("Tipo de efecto")]
    [SerializeField] private DecorativeEffectType effectType = DecorativeEffectType.CoinMultiplier;

    [Header("Efecto por nivel")]
    [Tooltip("Incremento de multiplicador por nivel (ej: 0.05 = +5% por nivel)")]
    [SerializeField] private float bonusPerLevel = 0.05f;

    private WorkStation _workStation;

    public DecorativeEffectType EffectType => effectType;
    public float CurrentBonus => (CurrentLevel - 1) * bonusPerLevel;

    public void Init(WorkStation workStation)
    {
        _workStation = workStation;
    }

    protected override void OnUpgraded(int newLevel)
    {
        if (_workStation == null)
        {
            Debug.LogWarning("[DecorativeUpgradeable] No se ha asignado WorkStation.");
            return;
        }

        _workStation.RecalculateDecorativeBonus();
        Debug.Log($"[DecorativeUpgradeable] Nivel {newLevel} — bonus {effectType}: +{CurrentBonus:P0}");
    }
}