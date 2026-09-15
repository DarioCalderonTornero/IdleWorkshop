using UnityEngine;

public abstract class UpgradeableBase : MonoBehaviour, IUpgradeable
{
    [SerializeField] protected UpgradeData upgradeData;

    private int currentLevel = 1;
    private SpriteRenderer _spriteRenderer;
    private bool _dataChecked;

    public UpgradeData UpgradeData => upgradeData;
    public int CurrentLevel => currentLevel;

    /// <summary>
    /// El subtipo de UpgradeData que este mejorable necesita. Devolver
    /// typeof(UpgradeData) significa "me vale cualquiera".
    ///
    /// Es público y abstracto a posta: obliga a declararlo al añadir un
    /// mejorable nuevo, y deja que las herramientas del editor revisen la
    /// escena sin tener que entrar en Play.
    /// </summary>
    public abstract System.Type ExpectedDataType { get; }

    /// <summary>
    /// Se dispara al subir de nivel y al cargar partida, con el nivel nuevo.
    /// Lo usa quien necesita reaccionar sin ser el propio mejorable: por
    /// ejemplo la sala de espera, que va sacando asientos según el nivel.
    /// </summary>
    public event System.Action<int> OnLevelChanged;

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Devuelve el UpgradeData ya tipado, o null si falta o es de otro tipo.
    ///
    /// Cada mejorable espera su propia subclase —un carrito no sabe qué hacer
    /// con los datos de una mesa— y antes nada lo comprobaba: podías arrastrar
    /// el asset equivocado y el efecto simplemente no se aplicaba, en silencio.
    /// Ahora se avisa una sola vez, con los dos tipos en el mensaje.
    /// </summary>
    protected TData RequireData<TData>() where TData : UpgradeData
    {
        if (!_dataChecked)
        {
            _dataChecked = true;

            if (upgradeData == null)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] {name}: no tiene UpgradeData asignado, " +
                    $"la mejora no hará nada.", this);
            }
            else if (upgradeData is not TData)
            {
                Debug.LogError(
                    $"[{GetType().Name}] {name}: '{upgradeData.name}' es " +
                    $"{upgradeData.GetType().Name}, pero aquí hace falta " +
                    $"{typeof(TData).Name}. La mejora no hará nada.", this);
            }
        }

        return upgradeData as TData;
    }

    public bool CanUpgrade()
    {
        if (upgradeData == null) return false;
        if (currentLevel >= upgradeData.maxLevel) return false;
        return EconomyManager.Instance.CanAfford(
            upgradeData.GetCostForLevel(currentLevel));
    }

    public void Upgrade()
    {
        if (!CanUpgrade()) return;
        EconomyManager.Instance.SpendCoins(
            upgradeData.GetCostForLevel(currentLevel));
        currentLevel++;
        ApplyEvolutionVisual();
        OnUpgraded(currentLevel);
        OnLevelChanged?.Invoke(currentLevel);
    }

    public void LoadLevel(int level)
    {
        currentLevel = level;
        if (currentLevel > 0)
        {
            ApplyEvolutionVisual();
            OnUpgraded(currentLevel);
        }

        OnLevelChanged?.Invoke(currentLevel);
    }

    private void ApplyEvolutionVisual()
    {
        if (upgradeData == null || _spriteRenderer == null) return;

        Sprite evolutionSprite = upgradeData.GetCurrentVisual(currentLevel);
        if (evolutionSprite != null)
            _spriteRenderer.sprite = evolutionSprite;
    }

    protected abstract void OnUpgraded(int newLevel);
}
