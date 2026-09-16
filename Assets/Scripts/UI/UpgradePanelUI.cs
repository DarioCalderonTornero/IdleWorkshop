// UpgradePanelUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanelUI : SlidingPanelUI
{
    public static UpgradePanelUI Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private Image elementImage;
    [SerializeField] private TextMeshProUGUI elementNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    private IUpgradeable currentTarget;

    [Header("Modo desbloqueo")]
    [SerializeField] private GameObject upgradeContent;   // todo el contenido normal
    [SerializeField] private Button unlockButton;          // botón grande de desbloquear
    [SerializeField] private TextMeshProUGUI unlockCostText;

    private IUnlockable currentUnlockable;

    [Header("Barra de evolución")]
    [SerializeField] private Image evolutionBarFill;

    protected override void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();

        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    void Start()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnCoinsChanged += OnCoinsChanged;
        else
            Debug.LogWarning("[UpgradePanelUI] EconomyManager no encontrado en Start");
    }

    // ── API pública ──────────────────────────────────────────────────

    public void ShowUnlock(IUnlockable unlockable)
    {
        if (state == PanelState.Visible && currentUnlockable == unlockable) return;

        currentUnlockable = unlockable;
        currentTarget = null;

        upgradeContent.SetActive(false);
        unlockButton.gameObject.SetActive(true);

        bool meetsReqs = unlockable.MeetsRequirements();
        double cost = unlockable.UnlockCost;

        if (meetsReqs)
        {
            unlockCostText.text = $"Desbloquear\n{cost} monedas";
            unlockButton.interactable = EconomyManager.Instance.CanAfford(cost);
        }
        else
        {
            unlockCostText.text = $"Requisitos pendientes:\n{unlockable.GetMissingRequirementsText()}";
            unlockButton.interactable = false;
        }

        unlockButton.onClick.RemoveAllListeners();
        unlockButton.onClick.AddListener(() =>
        {
            if (!unlockable.MeetsRequirements()) return;
            if (!EconomyManager.Instance.SpendCoins(unlockable.UnlockCost)) return;
            unlockable.Unlock();
            RoomUpgradePanelUI.Instance?.RefreshButtons();
            Hide();
        });

        AnimateToShown();
    }

    public void Show(IUpgradeable target)
    {
        if (state == PanelState.Visible && currentTarget == target) return;

        // Un panel que no se puede rellenar no se abre. Antes esto entraba
        // igual y petaba dentro de RefreshUI con un NullReference, y al caerse
        // ahí se quedaban todos los menús inservibles.
        if (!CanShow(target)) return;

        currentTarget = target;
        currentUnlockable = null;

        upgradeContent.SetActive(true);
        unlockButton.gameObject.SetActive(false);

        RefreshUI();

        AnimateToShown();
    }

    public void Hide()
    {
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        AnimateToHidden(() => currentTarget = null);

        // Se deshace el acercamiento: volvemos a ver la sala entera. Si el
        // panel de sala también se está cerrando, él lo ignora y deja la
        // cámara al jugador.
        RoomUpgradePanelUI.Instance?.FocusOnZone();
    }

    // ── Detección de toque fuera ─────────────────────────────────────

    void Update()
    {
        if (state != PanelState.Visible) return;
        if (RoomUpgradePanelUI.Instance != null && RoomUpgradePanelUI.Instance.IsVisible) return;

        if (!TryGetClickScreenPos(out Vector2 screenPos)) return;

        if (!IsInsidePanel(screenPos)) Hide();
    }

    // ── UI ───────────────────────────────────────────────────────────

    /// <summary>
    /// Si se puede abrir el panel para este objetivo. Un mejorable sin
    /// UpgradeData no es un caso raro de runtime: pasa cuando algo quedó mal
    /// cableado en el inspector o en el builder, y conviene que se vea en la
    /// consola señalando al objeto culpable en vez de caerse.
    /// </summary>
    private bool CanShow(IUpgradeable target)
    {
        if (target == null) return false;

        if (target.UpgradeData == null)
        {
            Debug.LogError(
                $"[UpgradePanelUI] '{(target as Object)?.name ?? target.GetType().Name}' " +
                $"no tiene UpgradeData asignado, así que no hay nada que mostrar. " +
                $"Asígnaselo en el inspector o reconstruye el taller.",
                target as Object);
            return false;
        }

        return true;
    }

    void RefreshUI()
    {
        if (!CanShow(currentTarget)) return;

        UpgradeData data = currentTarget.UpgradeData;
        int level = currentTarget.CurrentLevel;

        if (elementImage != null) elementImage.sprite = data.elementImage;
        if (elementNameText != null) elementNameText.text = data.elementName;
        if (levelText != null) levelText.text = $"Nivel {level}";
        if (descriptionText != null) descriptionText.text = data.description;

        var (floor, ceiling) = data.GetCurrentStageRange(level);
        float progress = ceiling > floor ? (float)(level - floor) / (ceiling - floor) : 1f;
        if (evolutionBarFill != null)
            evolutionBarFill.fillAmount = Mathf.Clamp01(progress);

        bool maxLevel = level >= data.maxLevel;
        if (upgradeButton != null)
            upgradeButton.interactable = !maxLevel && currentTarget.CanUpgrade();

        if (upgradeCostText != null)
            upgradeCostText.text = maxLevel
                ? "Nivel máximo"
                : $"{CurrencyFormatter.Format(data.GetCostForLevel(level))}";
    }

    void OnUpgradeClicked()
    {
        if (currentTarget == null) return;

        currentTarget.Upgrade();
        RefreshUI();

        // Si esa subida ha sacado una pieza nueva, el fantasma pasa a la
        // siguiente; y si era la última, desaparece.
        RoomUpgradePanelUI.Instance?.RefreshGhost();
    }

    void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    void OnCoinsChanged(double newAmount)
    {
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        if (currentTarget != null)
        {
            RefreshUI();
        }
        else if (currentUnlockable != null)
        {
            bool meetsReqs = currentUnlockable.MeetsRequirements();
            if (meetsReqs)
            {
                unlockCostText.text = $"Desbloquear\n{currentUnlockable.UnlockCost} monedas";
                unlockButton.interactable = EconomyManager.Instance.CanAfford(
                    currentUnlockable.UnlockCost);
            }
            else
            {
                unlockCostText.text = $"Requisitos pendientes:\n" +
                                      $"{currentUnlockable.GetMissingRequirementsText()}";
                unlockButton.interactable = false;
            }
        }
    }
}