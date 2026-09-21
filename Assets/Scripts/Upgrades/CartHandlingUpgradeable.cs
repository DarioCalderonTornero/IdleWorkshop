using UnityEngine;

/// <summary>
/// Mejora que hace que los carritos carguen y descarguen más rápido.
///
/// Afecta solo a los carritos, y a las dos operaciones por igual: lo que tarda
/// cada encargo en subir al carrito y lo que tarda en bajar. No toca ni la
/// velocidad a la que se mueven ni nada de los mostradores.
///
/// Los carritos no tienen por qué estar aquí al lado —esta mejora vive en la
/// habitación— así que se le pasan por referencia. Cada mejora se apunta en el
/// carrito como fuente propia, de modo que varias pueden convivir.
/// </summary>
public class CartHandlingUpgradeable : UpgradeableBase
{
    [Tooltip("Los carritos a los que afecta. Si se deja vacío, se buscan todos en la escena")]
    [SerializeField] private CartWorker[] carts;

    public override System.Type ExpectedDataType => typeof(CartHandlingUpgradeData);

    private CartHandlingUpgradeData Data => RequireData<CartHandlingUpgradeData>();

    protected override void Awake()
    {
        base.Awake();

        if (carts == null || carts.Length == 0)
            carts = FindObjectsByType<CartWorker>(FindObjectsInactive.Include);

        if (carts.Length == 0)
            Debug.LogWarning($"[CartHandlingUpgradeable] {name}: no encuentro ningún carrito que mejorar.", this);

        _ = Data;
    }

    /// <summary>
    /// El nivel 1 no pasa por OnUpgraded, así que sin esto los carritos se
    /// quedarían sin el multiplicador de partida.
    /// </summary>
    private void Start() => Apply(CurrentLevel);

    protected override void OnUpgraded(int newLevel) => Apply(newLevel);

    private void Apply(int level)
    {
        CartHandlingUpgradeData data = Data;
        if (data == null || carts == null) return;

        float multiplier = data.GetMultiplierForLevel(level);

        foreach (CartWorker cart in carts)
            if (cart != null) cart.SetHandlingMultiplier(this, multiplier);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (carts == null) return;

        foreach (CartWorker cart in carts)
            if (cart != null) cart.RemoveHandlingSource(this);
    }
}
