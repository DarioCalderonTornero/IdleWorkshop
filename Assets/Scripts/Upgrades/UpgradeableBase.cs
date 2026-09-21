using UnityEngine;

public abstract class UpgradeableBase : MonoBehaviour, IUpgradeable, ISaveableUpgrade
{
    [SerializeField] protected UpgradeData upgradeData;

    [Header("Guardado")]
    [Tooltip("Identidad de este elemento en la partida guardada. Se asigna sola: " +
             "no la escribas a mano ni la copies de otro objeto. Si dos elementos " +
             "acaban con el mismo id, 'Taller > Revisar guardado' lo detecta y lo arregla")]
    [SerializeField] private string saveId;

    private int currentLevel = 1;
    private SpriteRenderer _spriteRenderer;
    private bool _dataChecked;

    public UpgradeData UpgradeData => upgradeData;
    public int CurrentLevel => currentLevel;

    // ── ISaveableUpgrade ─────────────────────────────────────────────
    public string SaveId => saveId;

    /// <summary>
    /// Si este mejorable se guarda por id en el registro global.
    ///
    /// Las mesas dicen que no (ver <see cref="WorkDeskUpgradeable"/>): las
    /// guarda su taller por posición, porque los talleres 2 en adelante se
    /// instancian desde un prefab y un id puesto en el prefab se repetiría
    /// idéntico en cada copia.
    /// </summary>
    public virtual bool SavesItself => true;

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

        SaveRegistry.Register(this);
    }

    /// <summary>
    /// Virtual porque Unity solo llama al OnDestroy más derivado: si una
    /// subclase declarara el suyo sin más, este no correría y el elemento se
    /// quedaría apuntado en el registro después de morir.
    /// </summary>
    protected virtual void OnDestroy()
    {
        SaveRegistry.Unregister(this);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        // Las mesas no llevan id: se guardan por su taller, y darles uno solo
        // confundiría a quien lo viera en el Inspector.
        if (SavesItself) SaveIdentity.EnsureAssigned(this, ref saveId);
    }
#endif

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
