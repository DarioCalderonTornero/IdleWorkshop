using UnityEngine;

public class WorkTable : MonoBehaviour
{
    [Tooltip("Punto donde el jugador se para mientras trabaja")]
    [SerializeField] private Transform playerSlot;
    [Tooltip("Punto encima de la mesa donde se deposita el objeto")]
    [SerializeField] private Transform itemSlot;

    [Header("Multiplicadores base")]
    [SerializeField] private float baseTimeMultiplier = 1f;
    [SerializeField] private float baseRewardMultiplier = 1f;

    // Multiplicador propio de nivel (ya existía, viene de WorkDeskUpgradeable)
    private float _levelTimeMultiplier;
    private float _levelRewardMultiplier;

    // Bonus de zona, aportado por los DecorativeUpgradeable de la misma WorkStation
    private float _zoneTimeMultiplier = 1f;
    private float _zoneRewardMultiplier = 1f;

    private float _currentStarChance;
    private int _currentLevel = 1;
    private UpgradeData _upgradeData;

    void Awake()
    {
        _levelTimeMultiplier = baseTimeMultiplier;
        _levelRewardMultiplier = baseRewardMultiplier;
        _currentStarChance = 0f;
    }

    public Vector3 PlayerSlotPos => playerSlot.position;
    public Vector3 ItemSlotPos => itemSlot.position;

    // Combina nivel propio + bonus de zona en el cálculo final
    public float GetProcessTime(ItemDefinition item)
        => Mathf.Max(0.1f, item.baseRepairTime * _levelTimeMultiplier * _zoneTimeMultiplier);

    public int GetReward(ItemDefinition item)
        => Mathf.RoundToInt(item.rewardCoins * _levelRewardMultiplier * _zoneRewardMultiplier);

    // Llamado desde WorkDeskUpgradeable al mejorar (nivel propio de la mesa)
    public void ApplyMultipliers(float timeMultiplier, float rewardMultiplier)
    {
        _levelTimeMultiplier = timeMultiplier;
        _levelRewardMultiplier = rewardMultiplier;
    }

    // Llamado desde WorkStation al recalcular el bonus decorativo de la zona
    public void ApplyZoneMultipliers(float zoneTimeMultiplier, float zoneRewardMultiplier)
    {
        _zoneTimeMultiplier = zoneTimeMultiplier;
        _zoneRewardMultiplier = zoneRewardMultiplier;
    }

    public void ApplyUpgradeData(UpgradeData data, int level)
    {
        _upgradeData = data;
        _currentLevel = level;
        _currentStarChance = data.GetStarChanceForLevel(level);
    }

    public bool RollStar()
    {
        if (_currentStarChance <= 0f) return false;
        return Random.Range(0f, 100f) < _currentStarChance;
    }

    public float CurrentStarChance => _currentStarChance;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerSlot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(playerSlot.position, 0.12f);
        }
        if (itemSlot != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(itemSlot.position, 0.12f);
        }
    }
#endif
}