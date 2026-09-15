using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lo que toda mejora tiene en común: cómo se llama, cuánto cuesta subirla y en
/// qué niveles cambia de aspecto. Es lo único que consume el panel de mejoras,
/// así que la UI funciona igual con cualquier tipo de mejora.
///
/// Es abstracta a propósito. Lo que cada mejora *hace* —la velocidad de un
/// carrito, el tiempo de una recepción, las monedas de una decoración— vive en
/// su subclase. Así en el inspector de cada asset solo aparecen los campos que
/// ese elemento usa de verdad, y no se puede crear una mejora "de nada" que no
/// sepa a qué afecta.
/// </summary>
public abstract class UpgradeData : ScriptableObject
{
    [Header("Identidad")]
    public string elementName;
    public string description;
    public Sprite elementImage;

    [Header("Coste de mejora")]
    public double baseCost = 100;
    public float growthFactor = 1.5f;
    public int maxLevel = 20;

    [Header("Tramos de evolución")]
    [Tooltip("Niveles en los que el elemento cambia. Deben ir ordenados de menor a mayor.")]
    public List<EvolutionStage> evolutionStages = new();

    public double GetCostForLevel(int level)
    {
        return System.Math.Round(baseCost * System.Math.Pow(growthFactor, level - 1));
    }

    /// <summary>
    /// Devuelve el rango (suelo, techo) del tramo de evolución actual para un
    /// nivel dado. El suelo es el último umbral ya superado (0 si ninguno), el
    /// techo es el siguiente umbral pendiente (o maxLevel si no quedan más).
    ///
    /// Es lo que rellena la barra del panel: cuánto falta para el próximo
    /// cambio, no para el nivel máximo.
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
    /// Cuántos tramos de evolución se han alcanzado ya con este nivel.
    ///
    /// Sirve para elementos cuya evolución no es cambiar de sprite sino ir
    /// apareciendo por partes: los asientos del hall usan un tramo por asiento,
    /// así que este número es directamente cuántos se ven.
    /// </summary>
    public int GetReachedStageCount(int currentLevel)
    {
        int count = 0;

        foreach (var stage in evolutionStages)
        {
            if (stage.levelThreshold > currentLevel) break;
            count++;
        }

        return count;
    }

    /// <summary>
    /// Devuelve el sprite correspondiente al nivel actual, según el último
    /// umbral de evolución alcanzado. Null si aún no se alcanzó ninguno, o si
    /// esta mejora no cambia de sprite (en ese caso, se usa el sprite base del
    /// propio elemento en la escena).
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
    [Tooltip("Nivel en el que este tramo se activa.")]
    public int levelThreshold;

    [Tooltip("Sprite al que cambia el elemento. Opcional: las mejoras que no " +
             "cambian de sprite (por ejemplo las decorativas, que revelan piezas) " +
             "lo dejan vacío y solo usan el umbral.")]
    public Sprite visual;
}
