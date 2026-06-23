using UnityEngine;

/// <summary>
/// Unidad completa de trabajo: agrupa mesa de recepción, mesa de trabajo y worker.
/// Es la pieza central del loop de producción.
/// Cuando el trabajo termina avisa al EconomyManager y al CustomerManager.
/// 
/// JERARQUÍA RECOMENDADA EN UNITY:
/// WorkStation
/// ??? ReceptionDesk    (WorkTable)
/// ??? WorkDesk         (WorkTable)
/// ??? Worker           (Worker)
/// </summary>
public class WorkStation : MonoBehaviour, IUpgradeable
{
    [Header("Mesas")]
    [Tooltip("Mesa donde el cliente deja el objeto")]
    [SerializeField] private WorkTable receptionDesk;

    [Tooltip("Mesa donde el worker procesa el objeto")]
    [SerializeField] private WorkTable workDesk;

    [Header("Worker")]
    [SerializeField] private Worker worker;

    [Header("Punto de entrega al cliente")]
    [Tooltip("Punto encima de la mesa de recepción donde queda el objeto para que el cliente lo recoja")]
    [SerializeField] private Transform receptionItemPoint;

    // ?? Estado ??????????????????????????????????????????????????????
    private bool _isBusy;
    private ItemDefinition _currentItemDef;
    private CustomerManager _customerManager;

    // ?? Propiedades públicas ????????????????????????????????????????
    public bool IsBusy => _isBusy;
    public WorkTable ReceptionDesk => receptionDesk;
    public WorkTable WorkDesk => workDesk;
    public Transform ReceptionItemPoint => receptionItemPoint;

    [Header("Mejoras")]
    [SerializeField] private UpgradeData upgradeData;

    // Añadir la variable de nivel
    private int currentLevel = 0;

    // Implementar la interfaz
    public UpgradeData UpgradeData => upgradeData;
    public int CurrentLevel => currentLevel;

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
        Debug.Log($"[WorkStation] Mejorado a nivel {currentLevel}");
    }

    // ?? Unity ???????????????????????????????????????????????????????
    private void Awake()
    {
        _customerManager = FindAnyObjectByType<CustomerManager>();

        if (worker != null)
            worker.Init(this);
    }

    // ?? API pública ?????????????????????????????????????????????????

    /// <summary>
    /// El CustomerManager llama a este método cuando hay un objeto disponible.
    /// Si la estación está libre, arranca el proceso del worker.
    /// </summary>
    public void RequestWork(GameObject itemGO, ItemDefinition itemDef)
    {
        if (_isBusy) return;

        _isBusy = true;
        _currentItemDef = itemDef;
        worker.StartWork(itemGO, itemDef);
    }

    /// <summary>
    /// El Worker llama a este método cuando ha terminado el proceso completo.
    /// Añade las monedas a la economía y avisa al CustomerManager.
    /// </summary>
    public void OnWorkCompleted()
    {
        // Pagar al jugador
        if (_currentItemDef != null)
            EconomyManager.Instance?.AddCoins(_currentItemDef.rewardCoins);

        // Avisar al CustomerManager para que el cliente recoja el objeto
        _customerManager?.ServeNextCustomer(this);

        _isBusy = false;
        _currentItemDef = null;
    }
}
