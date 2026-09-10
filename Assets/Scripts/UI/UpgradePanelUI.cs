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

    private WorkDeskUnlockable currentUnlockable;

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

    public void ShowUnlock(WorkDeskUnlockable unlockable)
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

    void RefreshUI()
    {
        if (currentTarget == null) return;

        UpgradeData data = currentTarget.UpgradeData;
        int level = currentTarget.CurrentLevel;

        elementImage.sprite = data.elementImage;
        elementNameText.text = data.elementName;
        levelText.text = $"Nivel {level}";
        descriptionText.text = data.description;

        var (floor, ceiling) = data.GetCurrentStageRange(level);
        float progress = ceiling > floor ? (float)(level - floor) / (ceiling - floor) : 1f;
        if (evolutionBarFill != null)
            evolutionBarFill.fillAmount = Mathf.Clamp01(progress);

        bool maxLevel = level >= data.maxLevel;
        upgradeButton.interactable = !maxLevel && currentTarget.CanUpgrade();

        upgradeCostText.text = maxLevel
            ? "Nivel máximo"
            : $"{CurrencyFormatter.Format(data.GetCostForLevel(level))}";
    }

    void OnUpgradeClicked()
    {
        if (currentTarget == null) return;
        currentTarget.Upgrade();
        RefreshUI();
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