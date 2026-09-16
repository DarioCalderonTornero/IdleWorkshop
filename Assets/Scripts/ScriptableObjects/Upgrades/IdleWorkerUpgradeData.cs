using UnityEngine;

/// <summary>
/// Mejora de un trabajador de sala: los que no procesan objetos de nadie sino
/// que están a lo suyo en bucle —ordenando el almacén, arreglando la
/// habitación— y cobran cada vez que terminan una tanda.
///
/// Es su propio tipo porque no se parece a nada de lo que ya había: no hay
/// objeto que pase por aquí, así que no tiene sentido hablar de tiempo de
/// reparación ni de recompensa por objeto. Lo que se mejora es cuánto paga la
/// tanda y cuánto tarda.
/// </summary>
[CreateAssetMenu(fileName = "IdleWorkerUpgrade", menuName = "Idle/Mejoras/Trabajador de sala")]
public class IdleWorkerUpgradeData : UpgradeData
{
    [Header("Lo que paga cada tanda")]
    [Tooltip("Monedas al terminar una tanda, en el nivel 1")]
    public int baseCoins = 1200;

    [Tooltip("Monedas más por cada nivel")]
    public int coinsPerLevel = 320;

    [Header("Lo que dura cada tanda")]
    [Tooltip("Segundos que tarda una tanda en el nivel 1. Son tandas largas a propósito")]
    public float baseCycleTime = 90f;

    [Tooltip("Segundos que se recorta por nivel. Poco: la gracia está en lo que paga, " +
             "no en acelerarlo")]
    public float timeReductionPerLevel = 1.2f;

    [Tooltip("Por rápido que se mejore, una tanda nunca baja de aquí")]
    public float minCycleTime = 30f;

    public int GetCoinsForLevel(int level)
        => baseCoins + Mathf.Max(0, level - 1) * coinsPerLevel;

    public float GetCycleTimeForLevel(int level)
        => Mathf.Max(minCycleTime, baseCycleTime - Mathf.Max(0, level - 1) * timeReductionPerLevel);

    /// <summary>
    /// Monedas por segundo al nivel dado. No lo usa el juego: sirve para ver de
    /// un vistazo, al ajustar los números, si una mejora compensa.
    /// </summary>
    public float GetCoinsPerSecond(int level)
        => GetCoinsForLevel(level) / Mathf.Max(0.01f, GetCycleTimeForLevel(level));
}
