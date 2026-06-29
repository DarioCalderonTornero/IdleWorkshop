using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Idle/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string elementName;
    public string description;
    public Sprite elementImage;

    [Header("Coste de mejora")]
    public double baseCost = 100;
    public float growthFactor = 1.5f;
    public int maxLevel = 20;

    [Header("Mejora de tiempo (mesas de trabajo)")]
    public float timeReductionPerLevel = 0.05f;

    [Header("Mejora de recompensa (mesas de trabajo)")]
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

    public double GetCostForLevel(int level)
    {
        return System.Math.Round(baseCost * System.Math.Pow(growthFactor, level - 1));
    }

    public float GetStarChanceForLevel(int level)
    {
        return Mathf.Min(maxStarChance, baseStarChance + (level - 1) * starChanceIncreasePerLevel);
    }
}