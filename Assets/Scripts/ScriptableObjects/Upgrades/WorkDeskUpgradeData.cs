using UnityEngine;

/// <summary>
/// Mejora de una mesa de limpieza: cada nivel la hace más rápida, pagar más y
/// sacar estrellas más a menudo.
/// </summary>
[CreateAssetMenu(fileName = "WorkDeskUpgrade", menuName = "Idle/Mejoras/Mesa de limpieza")]
public class WorkDeskUpgradeData : UpgradeData
{
    [Header("Efecto por nivel")]
    [Tooltip("Reducción de tiempo por nivel (0.05 = -5% por nivel)")]
    public float timeReductionPerLevel = 0.05f;

    [Tooltip("Incremento de recompensa por nivel (0.1 = +10% por nivel)")]
    public float rewardIncreasePerLevel = 0.1f;

    [Header("Estrellas")]
    [Tooltip("Probabilidad base de dar estrella (0-100)")]
    [Range(0f, 100f)]
    public float baseStarChance = 10f;

    [Tooltip("Incremento de probabilidad por nivel")]
    [Range(0f, 10f)]
    public float starChanceIncreasePerLevel = 1f;

    [Tooltip("Probabilidad máxima de dar estrella (0-100)")]
    [Range(0f, 100f)]
    public float maxStarChance = 50f;

    public float GetStarChanceForLevel(int level)
    {
        return Mathf.Min(maxStarChance, baseStarChance + (level - 1) * starChanceIncreasePerLevel);
    }
}
