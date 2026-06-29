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

    private float _currentTimeMultiplier;
    private float _currentRewardMultiplier;
    private float _currentStarChance;

    // Referencia al nivel actual para calcular la probabilidad
    private int _currentLevel = 1;
    private UpgradeData _upgradeData;

    void Awake()
    {
        _currentTimeMultiplier = baseTimeMultiplier;
        _currentRewardMultiplier = baseRewardMultiplier;
        _currentStarChance = 0f;
    }

    public Vector3 PlayerSlotPos => playerSlot.position;
    public Vector3 ItemSlotPos => itemSlot.position;

    public float GetProcessTime(ItemDefinition item)
        => Mathf.Max(0.1f, item.baseRepairTime * _currentTimeMultiplier);

    public int GetReward(ItemDefinition item)
        => Mathf.RoundToInt(item.rewardCoins * _currentRewardMultiplier);

    public void ApplyMultipliers(float timeMultiplier, float rewardMultiplier)
    {
        _currentTimeMultiplier = timeMultiplier;
        _currentRewardMultiplier = rewardMultiplier;
    }

    // Llamado desde WorkDeskUpgradeable al mejorar
    public void ApplyUpgradeData(UpgradeData data, int level)
    {
        _upgradeData = data;
        _currentLevel = level;
        _currentStarChance = data.GetStarChanceForLevel(level);
    }

    // Devuelve true si se consigue estrella en esta mesa
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