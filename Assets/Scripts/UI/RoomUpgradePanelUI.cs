// RoomUpgradePanelUI.cs
using System.Collections.Generic;
using UnityEngine;

public class RoomUpgradePanelUI : SlidingPanelUI
{
    public static RoomUpgradePanelUI Instance { get; private set; }

    [Header("Botones")]
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject buttonPrefab;

    private UpgradeZone currentZone;
    private readonly List<GameObject> spawnedButtons = new();

    /// <summary>
    /// True durante el frame en el que el panel se ha cerrado por un toque fuera.
    /// Permite a otros sistemas (TapHandler) ignorar ese mismo toque físico
    /// para que no vuelva a abrir algo en el mismo gesto que lo cerró.
    /// </summary>
    public static bool JustClosedThisFrame { get; private set; }

    protected override void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();
    }

    public void Show(UpgradeZone zone)
    {
        if (state == PanelState.Visible && currentZone == zone) return;

        currentZone = zone;
        PopulateButtons();

        AnimateToShown();
    }

    public void Hide()
    {
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        if (UpgradePanelUI.Instance != null && UpgradePanelUI.Instance.IsVisible)
            UpgradePanelUI.Instance.Hide();

        AnimateToHidden(() => currentZone = null);

        CameraController.Instance?.Unlock();
    }

    public void RefreshButtons()
    {
        if (currentZone == null) return;
        PopulateButtons();
    }

    private void PopulateButtons()
    {
        foreach (var go in spawnedButtons)
            Destroy(go);
        spawnedButtons.Clear();

        if (currentZone == null) return;

        foreach (var element in currentZone.UpgradeElements)
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

        if (!TryGetClickScreenPos(out Vector2 screenPos)) return;

        bool insideThis = IsInsidePanel(screenPos);
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
}