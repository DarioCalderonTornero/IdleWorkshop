using UnityEngine;

public abstract class UpgradeableBase : MonoBehaviour, IUpgradeable
{
    [SerializeField] protected UpgradeData upgradeData;

    private int currentLevel = 1;

    public UpgradeData UpgradeData => upgradeData;
    public int CurrentLevel => currentLevel;

    public bool CanUpgrade()
    {
        if (upgradeData == null) return false;
        if (currentLevel >= upgradeData.maxLevel) return false;  // maxLevel sigue siendo el tope
        return EconomyManager.Instance.CanAfford(
            upgradeData.GetCostForLevel(currentLevel));
    }

    public void Upgrade()
    {
        if (!CanUpgrade()) return;
        EconomyManager.Instance.SpendCoins(
            upgradeData.GetCostForLevel(currentLevel));
        currentLevel++;
        OnUpgraded(currentLevel);
    }

    public void LoadLevel(int level)
    {
        currentLevel = level;
        if (currentLevel > 0)
            OnUpgraded(currentLevel);
    }

    protected abstract void OnUpgraded(int newLevel);
}