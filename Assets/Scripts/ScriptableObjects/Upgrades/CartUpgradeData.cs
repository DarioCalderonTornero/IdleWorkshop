using UnityEngine;

/// <summary>
/// Mejora de un carrito: recorre el taller más rápido por nivel.
/// </summary>
[CreateAssetMenu(fileName = "CartUpgrade", menuName = "Idle/Mejoras/Carrito")]
public class CartUpgradeData : UpgradeData
{
    [Header("Efecto por nivel")]
    [Tooltip("Incremento de velocidad por nivel (0.12 = +12% por nivel)")]
    public float moveSpeedMultiplierPerLevel = 0.12f;
}
