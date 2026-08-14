using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomUpgradePanelUI : MonoBehaviour
{
    public static RoomUpgradePanelUI Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Animación")]
    [SerializeField] private float animDuration = 0.3f;
    [SerializeField] private float hiddenY = -384f;
    [SerializeField] private float shownY = 0f;

    private enum PanelState { Hidden, Showing, Visible, Hiding }
    private PanelState state = PanelState.Hidden;
    private Coroutine animCoroutine;

    private WorkStation currentStation;
    private readonly List<GameObject> spawnedButtons = new();

    public bool IsVisible => state == PanelState.Visible || state == PanelState.Showing || state == PanelState.Hiding;

    /// <summary>
    /// True durante el frame en el que el panel se ha cerrado por un toque fuera.
    /// Permite a otros sistemas (TapHandler) ignorar ese mismo toque físico
    /// para que no vuelva a abrir algo en el mismo gesto que lo cerró.
    /// </summary>
    public static bool JustClosedThisFrame { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, hiddenY);
        state = PanelState.Hidden;
    }

    public void Show(WorkStation station)
    {
        if (state == PanelState.Visible && currentStation == station) return;

        currentStation = station;
        PopulateButtons();

        state = PanelState.Showing;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateTo(shownY, () => state = PanelState.Visible));
    }

    public void Hide()
    {
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        if (UpgradePanelUI.Instance != null && UpgradePanelUI.Instance.IsVisible)
            UpgradePanelUI.Instance.Hide();

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        state = PanelState.Hiding;
        animCoroutine = StartCoroutine(AnimateTo(hiddenY, () =>
        {
            state = PanelState.Hidden;
            currentStation = null;
        }));

        CameraController.Instance?.Unlock();
    }

    public void RefreshButtons()
    {
        if (currentStation == null) return;
        PopulateButtons();
    }

    private void PopulateButtons()
    {
        foreach (var go in spawnedButtons)
            Destroy(go);
        spawnedButtons.Clear();

        if (currentStation == null) return;

        foreach (var element in currentStation.UpgradeElements)
        {
            GameObject buttonGO = Instantiate(buttonPrefab, buttonsContainer);
            RoomUpgradeButtonUI buttonUI = buttonGO.GetComponent<RoomUpgradeButtonUI>();

            if (element.unlockableTarget != null)
            {
                WorkDeskUnlockable unlockable = element.unlockableTarget;

                if (!unlockable.IsUnlocked)
                {
                    buttonUI.Setup(element.icon, true, () => UpgradePanelUI.Instance.ShowUnlock(unlockable));
                }
                else
                {
                    IUpgradeable upgradeable = unlockable.GetComponent<IUpgradeable>();
                    buttonUI.Setup(element.icon, false, () => UpgradePanelUI.Instance.Show(upgradeable));
                }
            }
            else if (element.upgradeableTarget is IUpgradeable upgradeable)
            {
                buttonUI.Setup(element.icon, false, () => UpgradePanelUI.Instance.Show(upgradeable));
            }
            else
            {
                Debug.LogWarning($"[RoomUpgradePanelUI] Elemento sin configurar correctamente: {element.unlockableTarget} / {element.upgradeableTarget}");
                Destroy(buttonGO);
                continue;
            }

            spawnedButtons.Add(buttonGO);
        }
    }

    private void Update()
    {
        if (state != PanelState.Visible) return;

        bool clicked = false;
        Vector2 screenPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!clicked) return;

        bool insideThis = RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPos, null);
        bool insideDetail = UpgradePanelUI.Instance != null && UpgradePanelUI.Instance.IsVisible &&
                             RectTransformUtility.RectangleContainsScreenPoint(UpgradePanelUI.Instance.PanelRect, screenPos, null);

        if (!insideThis && !insideDetail)
        {
            Hide();
            JustClosedThisFrame = true;
        }
    }

    private void LateUpdate()
    {
        JustClosedThisFrame = false;
    }

    private IEnumerator AnimateTo(float targetY, System.Action onComplete)
    {
        float startY = panelRect.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, targetY);
        onComplete?.Invoke();
    }
}