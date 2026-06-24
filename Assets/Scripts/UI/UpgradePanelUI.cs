using System.Collections;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UpgradePanelUI : MonoBehaviour
{
    public static UpgradePanelUI Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image elementImage;
    [SerializeField] private TextMeshProUGUI elementNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    [Header("Animación")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private float hiddenY = -384f;
    [SerializeField] private float shownY = 224f;

    private IUpgradeable currentTarget;

    private enum PanelState { Hidden, Showing, Visible, Hiding }
    private PanelState state = PanelState.Hidden;

    private Coroutine animCoroutine;

    [Header("Modo desbloqueo")]
    [SerializeField] private GameObject upgradeContent;   // todo el contenido normal
    [SerializeField] private Button unlockButton;          // botón grande de desbloquear
    [SerializeField] private TextMeshProUGUI unlockCostText;

    private WorkDeskUnlockable currentUnlockable;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Empieza siempre oculto
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, hiddenY);
        state = PanelState.Hidden;

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

        // Muestra solo el botón de desbloquear
        upgradeContent.SetActive(false);
        unlockButton.gameObject.SetActive(true);

        double cost = unlockable.UnlockCost;
        unlockCostText.text = $"Desbloquear\n{cost} monedas";
        unlockButton.interactable = EconomyManager.Instance.CanAfford(cost);

        unlockButton.onClick.RemoveAllListeners();
        unlockButton.onClick.AddListener(() =>
        {
            if (!EconomyManager.Instance.SpendCoins(unlockable.UnlockCost)) return;
            unlockable.Unlock();
            Hide();
        });

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        state = PanelState.Showing;
        animCoroutine = StartCoroutine(AnimateTo(shownY, () => state = PanelState.Visible));
    }

    public void Show(IUpgradeable target)
    {
        if (state == PanelState.Visible && currentTarget == target) return;

        currentTarget = target;
        currentUnlockable = null;

        // Muestra contenido normal, oculta botón de desbloquear
        upgradeContent.SetActive(true);
        unlockButton.gameObject.SetActive(false);

        RefreshUI();

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        state = PanelState.Showing;
        animCoroutine = StartCoroutine(AnimateTo(shownY, () => state = PanelState.Visible));
    }

    public void Hide()
    {
        // Solo baja si está visible
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        state = PanelState.Hiding;
        animCoroutine = StartCoroutine(AnimateTo(hiddenY, () =>
        {
            state = PanelState.Hidden;
            currentTarget = null;
        }));
    }

    // ── Detección de toque fuera ─────────────────────────────────────

    void Update()
    {
        // Solo comprueba si el panel está completamente visible
        if (state != PanelState.Visible) return;

        bool clicked = false;
        Vector2 screenPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null &&
                 Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!clicked) return;

        // Screen Space Overlay → cámara null
        bool insidePanel = RectTransformUtility.RectangleContainsScreenPoint(
            panelRect, screenPos, null);

        if (!insidePanel) Hide();
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

        bool maxLevel = level >= data.maxLevel;
        upgradeButton.interactable = !maxLevel && currentTarget.CanUpgrade();

        upgradeCostText.text = maxLevel
            ? "Nivel máximo"
            : $"{data.GetCostForLevel(level)} monedas";
    }

    void OnUpgradeClicked()
    {
        if (currentTarget == null) return;
        currentTarget.Upgrade();
        RefreshUI();
    }

    // ── Animación ────────────────────────────────────────────────────

    IEnumerator AnimateTo(float targetY, System.Action onComplete)
    {
        float startY = panelRect.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            panelRect.anchoredPosition = new Vector2(
                panelRect.anchoredPosition.x,
                Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        panelRect.anchoredPosition = new Vector2(
            panelRect.anchoredPosition.x, targetY);

        onComplete?.Invoke();
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
            // Panel de mejora normal
            RefreshUI();
        }
        else if (currentUnlockable != null)
        {
            // Panel de desbloqueo
            unlockButton.interactable = EconomyManager.Instance.CanAfford(currentUnlockable.UnlockCost);
        }
    }
}