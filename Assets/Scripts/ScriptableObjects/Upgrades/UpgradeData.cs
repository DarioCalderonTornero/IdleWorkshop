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

    [Header("Mejora de tiempo (solo mesas de trabajo)")]
    [Tooltip("Reducción del multiplicador de tiempo por nivel (ej: 0.05 = -5% por nivel)")]
    public float timeReductionPerLevel = 0.05f;

    [Header("Mejora de recompensa (solo mesas de trabajo)")]
    [Tooltip("Incremento del multiplicador de dinero por nivel (ej: 0.1 = +10% por nivel)")]
    public float rewardIncreasePerLevel = 0.1f;

    public double GetCostForLevel(int level)
    {
        return System.Math.Round(baseCost * System.Math.Pow(growthFactor, level));
    }
}