// RoomUpgradePanelUI.cs
using System.Collections.Generic;
using UnityEngine;

public class RoomUpgradePanelUI : SlidingPanelUI
{
    public static RoomUpgradePanelUI Instance { get; private set; }

    [Header("Botones")]
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Fantasma de lo que va a aparecer")]
    [Tooltip("Hacia qué color tira la copia semitransparente")]
    [SerializeField] private Color ghostTint = new(0.75f, 0.95f, 1f);

    [Range(0.1f, 1f)]
    [SerializeField] private float ghostAlpha = 0.45f;

    private GameObject _ghost;

    // De quién es el fantasma que hay puesto, para poder rehacerlo al mejorar.
    private MonoBehaviour _ghostHost;

    private UpgradeZone currentZone;
    private readonly List<GameObject> spawnedButtons = new();

    // Cerrándose: mientras tanto no se acepta ninguna petición de reencuadre,
    // porque la cámara ya está volviendo al control del jugador.
    private bool _closing;

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

        // Se marca antes de cerrar el detalle: al cerrarse, este pediría volver
        // al encuadre de la sala, y estamos justo saliendo de ella.
        _closing = true;
        ClearGhost();

        if (UpgradePanelUI.Instance != null && UpgradePanelUI.Instance.IsVisible)
            UpgradePanelUI.Instance.Hide();

        AnimateToHidden(() =>
        {
            currentZone = null;
            _closing = false;
        });

        CameraController.Instance?.Unlock();
    }

    /// <summary>
    /// Acerca la cámara a lo que el jugador acaba de tocar y, si esa mejora va
    /// a sacar algo nuevo, deja puesto su fantasma.
    ///
    /// Si el elemento no sabe dónde está se queda como está, en el encuadre de
    /// la sala: es mejor no moverse que dar un salto a un sitio cualquiera.
    /// </summary>
    private void FocusOnElement(RoomUpgradeElement element)
    {
        ClearGhost();

        if (element == null || CameraController.Instance == null) return;
        if (!element.TryGetFocusPosition(out Vector3 position)) return;

        if (TryShowGhost(element, out Vector3 ghostAt))
        {
            // Lo nuevo puede salir lejos de la propia mejora. Se encuadran los
            // dos, que si no el fantasma se queda fuera de pantalla.
            CameraController.Instance.FocusOnElementAnd(position, ghostAt);
            return;
        }

        CameraController.Instance.FocusOnElement(position);
    }

    // ── Fantasma de lo que va a aparecer ─────────────────────────────

    /// <summary>
    /// Pone una copia semitransparente de lo próximo que va a salir.
    ///
    /// Quien sabe qué viene es el propio elemento: se busca un IUpgradePreview
    /// en él o dentro de él. Lo segundo es lo que hace que comprar una sala
    /// entera enseñe a su primer trabajador, sin tener que cablear nada.
    /// </summary>
    private bool TryShowGhost(RoomUpgradeElement element, out Vector3 position)
    {
        _ghostHost = element?.upgradeableTarget != null
            ? element.upgradeableTarget
            : element?.unlockableTarget;

        return TryShowGhostFor(_ghostHost, out position);
    }

    private bool TryShowGhostFor(MonoBehaviour host, out Vector3 position)
    {
        position = Vector3.zero;
        if (host == null) return false;

        IUpgradePreview preview = host.GetComponentInChildren<IUpgradePreview>(includeInactive: true);
        if (preview == null) return false;

        if (!preview.TryGetPreview(out GameObject sample, out position)) return false;

        _ghost = UpgradeGhost.Build(sample, position, ghostTint, ghostAlpha);
        return _ghost != null;
    }

    /// <summary>
    /// Vuelve a poner el fantasma sin tocar la cámara. Se llama tras mejorar o
    /// desbloquear: lo que se estaba adelantando ya existe, así que ahora toca
    /// enseñar lo siguiente — o nada, si ya no queda.
    /// </summary>
    public void RefreshGhost()
    {
        MonoBehaviour host = _ghostHost;

        ClearGhost();

        if (host == null) return;
        _ghostHost = host;

        TryShowGhostFor(host, out _);
    }

    private void ClearGhost()
    {
        _ghostHost = null;

        if (_ghost == null) return;

        Destroy(_ghost);
        _ghost = null;
    }

    /// <summary>
    /// Vuelve al encuadre de la sala entera. Lo pide el panel de detalle al
    /// cerrarse, para deshacer el acercamiento a la mejora.
    /// </summary>
    public void FocusOnZone()
    {
        // Al soltar la mejora se va también su fantasma: dejarlo puesto
        // mientras se ve la sala entera confundiría con algo ya construido.
        ClearGhost();

        if (_closing || currentZone == null) return;
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        CameraController.Instance?.FocusOn(currentZone.CameraFocusPosition);
    }

    public void RefreshButtons()
    {
        if (currentZone == null) return;

        PopulateButtons();

        // Lo que se estaba adelantando puede haberse comprado ya: el fantasma
        // pasa a la siguiente pieza, o desaparece si no queda ninguna.
        RefreshGhost();
    }

    private void PopulateButtons()
    {
        foreach (var go in spawnedButtons)
            Destroy(go);
        spawnedButtons.Clear();

        if (currentZone == null) return;

        // Sala sin comprar: lo único que se ofrece es abrirla. Sus mejoras no
        // pintan nada todavía, y enseñarlas solo invitaría a tocar botones que
        // no hacen nada.
        if (currentZone.IsLocked)
        {
            IUnlockable room = currentZone.ZoneUnlockable;

            // Comprar la sala entera no acerca la cámara: la sala ya está
            // encuadrada. Sí se enseña el fantasma de lo que va a haber
            // dentro, que es lo que el jugador está comprando.
            GameObject roomButton = Instantiate(buttonPrefab, buttonsContainer);
            roomButton.GetComponent<RoomUpgradeButtonUI>()
                .Setup(currentZone.LockedIcon, true, () =>
                {
                    ClearGhost();
                    _ghostHost = currentZone.ZoneUnlockable as MonoBehaviour;
                    TryShowGhostFor(_ghostHost, out _);

                    UpgradePanelUI.Instance.ShowUnlock(room);
                });

            spawnedButtons.Add(roomButton);
            return;
        }

        foreach (var element in currentZone.UpgradeElements)
        {
            GameObject buttonGO = Instantiate(buttonPrefab, buttonsContainer);
            RoomUpgradeButtonUI buttonUI = buttonGO.GetComponent<RoomUpgradeButtonUI>();

            IUnlockable unlockable = element.Unlockable;

            // Cada botón se acerca primero a lo que se va a mejorar y luego
            // abre su panel: la sala ya está encuadrada, así que este es el
            // segundo salto, más corto y más cerca.
            RoomUpgradeElement current = element;

            if (unlockable != null)
            {
                if (!unlockable.IsUnlocked)
                {
                    buttonUI.Setup(element.icon, true, () =>
                    {
                        FocusOnElement(current);
                        UpgradePanelUI.Instance.ShowUnlock(unlockable);
                    });
                }
                else
                {
                    // Ya desbloqueado: se muestra su mejora. Se prefiere la
                    // asignada a mano y, si no la hay, la del mismo GameObject.
                    IUpgradeable upgradeable = element.upgradeableTarget as IUpgradeable
                                            ?? element.unlockableTarget.GetComponent<IUpgradeable>();

                    buttonUI.Setup(element.icon, false, () =>
                    {
                        FocusOnElement(current);
                        UpgradePanelUI.Instance.Show(upgradeable);
                    });
                }
            }
            else if (element.upgradeableTarget is IUpgradeable upgradeable)
            {
                buttonUI.Setup(element.icon, false, () =>
                {
                    FocusOnElement(current);
                    UpgradePanelUI.Instance.Show(upgradeable);
                });
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