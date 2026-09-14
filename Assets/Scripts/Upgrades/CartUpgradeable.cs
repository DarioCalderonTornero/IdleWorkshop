using UnityEngine;

/// <summary>
/// Mejora de un carrito: va más rápido por nivel.
///
/// Es su propio tipo y no un WorkerUpgradeable aunque la fórmula se parezca,
/// porque lleva su propio UpgradeData: el jugador mejora "el carrito", con su
/// coste y su nombre, no al trabajador que lo empuja.
/// </summary>
public class CartUpgradeable : UpgradeableBase
{
    [Header("Efecto por nivel")]
    [Tooltip("Incremento de velocidad por nivel (ej: 0.12 = +12% por nivel)")]
    [SerializeField] private float moveSpeedMultiplierPerLevel = 0.12f;

    [Tooltip("El carrito a mejorar. Si es null, se busca en este GameObject y sus hijos")]
    [SerializeField] private CartWorker cartWorker;

    protected override void Awake()
    {
        base.Awake();

        if (cartWorker == null)
            cartWorker = GetComponentInChildren<CartWorker>(includeInactive: true);

        if (cartWorker == null)
            Debug.LogWarning($"[CartUpgradeable] {name}: no encuentro ningún CartWorker que mejorar.", this);
    }

    protected override void OnUpgraded(int newLevel)
    {
        if (cartWorker == null) return;

        float multiplier = 1f + newLevel * moveSpeedMultiplierPerLevel;
        cartWorker.ApplyMoveSpeedMultiplier(multiplier);
    }
}
