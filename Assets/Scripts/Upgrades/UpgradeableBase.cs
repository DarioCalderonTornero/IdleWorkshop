using UnityEngine;

public abstract class UpgradeableBase : MonoBehaviour, IUpgradeable
{
    [SerializeField] protected UpgradeData upgradeData;

    private int currentLevel = 1;
    private SpriteRenderer _spriteRenderer;

    public UpgradeData UpgradeData => upgradeData;
    public int CurrentLevel => currentLevel;

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public bool CanUpgrade()
    {
        if (upgradeData == null) return false;
        if (currentLevel >= upgradeData.maxLevel) return false;
        return EconomyManager.Instance.CanAfford(
            upgradeData.GetCostForLevel(currentLevel));
    }

    public void Upgrade()
    {
        if (!CanUpgrade()) return;
        EconomyManager.Instance.SpendCoins(
            upgradeData.GetCostForLevel(currentLevel));
        currentLevel++;
        ApplyEvolutionVisual();
        OnUpgraded(currentLevel);
    }

    public void LoadLevel(int level)
    {
        currentLevel = level;
        if (currentLevel > 0)
        {
            ApplyEvolutionVisual();
            OnUpgraded(currentLevel);
        }
    }

    private void ApplyEvolutionVisual()
    {
        if (upgradeData == null || _spriteRenderer == null) return;

        Sprite evolutionSprite = upgradeData.GetCurrentVisual(currentLevel);
        if (evolutionSprite != null)
            _spriteRenderer.sprite = evolutionSprite;
    }

    protected abstract void OnUpgraded(int newLevel);
}