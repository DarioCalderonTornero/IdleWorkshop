using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Idle/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string elementName;
    public string description;
    public Sprite elementImage;

    [Header("Fórmula de coste")]
    public double baseCost = 100;
    public float growthFactor = 1.5f;
    public int maxLevel = 20;

    public double GetCostForLevel(int level)
    {
        return System.Math.Round(baseCost * System.Math.Pow(growthFactor, level));
    }
}