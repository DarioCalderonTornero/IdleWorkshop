using UnityEngine;

public class WorkTable : MonoBehaviour
{
    [Tooltip("Punto donde el jugador se para mientras trabaja")]
    [SerializeField] private Transform playerSlot;

    [Tooltip("Punto encima de la mesa donde se deposita el objeto")]
    [SerializeField] private Transform itemSlot;

    [Header("Multiplicadores base")]
    [Tooltip("Multiplica el baseRepairTime del objeto. <1 = más rápido")]
    [SerializeField] private float baseTimeMultiplier = 1f;

    [Tooltip("Multiplica el rewardCoins del objeto")]
    [SerializeField] private float baseRewardMultiplier = 1f;

    private float _currentTimeMultiplier;
    private float _currentRewardMultiplier;

    void Awake()
    {
        _currentTimeMultiplier = baseTimeMultiplier;
        _currentRewardMultiplier = baseRewardMultiplier;
    }

    public Vector3 PlayerSlotPos => playerSlot.position;
    public Vector3 ItemSlotPos => itemSlot.position;

    // Tiempo real de proceso para un objeto concreto
    public float GetProcessTime(ItemDefinition item)
        => Mathf.Max(0.1f, item.baseRepairTime * _currentTimeMultiplier);

    // Recompensa real para un objeto concreto
    public int GetReward(ItemDefinition item)
        => Mathf.RoundToInt(item.rewardCoins * _currentRewardMultiplier);

    // Llamado desde WorkDeskUpgradeable al mejorar
    public void ApplyMultipliers(float timeMultiplier, float rewardMultiplier)
    {
        _currentTimeMultiplier = timeMultiplier;
        _currentRewardMultiplier = rewardMultiplier;
    }

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