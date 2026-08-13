using UnityEngine;
using System.Collections.Generic;

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

    [Header("Evolución visual")]
    [Tooltip("Niveles en los que el elemento cambia de aspecto. Deben ir ordenados de menor a mayor.")]
    public List<EvolutionStage> evolutionStages = new();

    public double GetCostForLevel(int level)
    {
        return System.Math.Round(baseCost * System.Math.Pow(growthFactor, level - 1));
    }

    public float GetStarChanceForLevel(int level)
    {
        return Mathf.Min(maxStarChance, baseStarChance + (level - 1) * starChanceIncreasePerLevel);
    }

    /// <summary>
    /// Devuelve el rango (suelo, techo) del tramo de evolución actual para un nivel dado.
    /// El suelo es el último umbral ya superado (0 si ninguno), el techo es el siguiente
    /// umbral pendiente (o maxLevel si no quedan más evoluciones).
    /// </summary>
    public (int floor, int ceiling) GetCurrentStageRange(int currentLevel)
    {
        int floor = 0;
        int ceiling = maxLevel;

        foreach (var stage in evolutionStages)
        {
            if (stage.levelThreshold <= currentLevel)
            {
                floor = stage.levelThreshold;
            }
            else
            {
                ceiling = stage.levelThreshold;
                break;
            }
        }

        return (floor, ceiling);
    }

    /// <summary>
    /// Devuelve el sprite correspondiente al nivel actual, según el último
    /// umbral de evolución alcanzado. Null si aún no se alcanzó ninguno
    /// (en ese caso, usar el sprite base del propio elemento en la escena).
    /// </summary>
    public Sprite GetCurrentVisual(int currentLevel)
    {
        Sprite current = null;
        foreach (var stage in evolutionStages)
        {
            if (stage.levelThreshold <= currentLevel)
                current = stage.visual;
            else
                break;
        }
        return current;
    }
}

[System.Serializable]
public class EvolutionStage
{
    [Tooltip("Nivel en el que esta evolución se activa (el elemento cambia de visual al alcanzarlo).")]
    public int levelThreshold;
    public Sprite visual;
}