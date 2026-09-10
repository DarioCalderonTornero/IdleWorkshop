// SlidingPanelUI.cs — NUEVO
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base compartida por los paneles deslizantes de la UI (RoomUpgradePanelUI,
/// UpgradePanelUI): gestiona el estado de animación (oculto/mostrando/visible/
/// ocultando) y la animación de deslizamiento vertical sobre un RectTransform.
/// Las subclases deciden qué mostrar y cuándo llamar a AnimateToShown/Hidden.
/// </summary>
public abstract class SlidingPanelUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] protected RectTransform panelRect;
    public RectTransform PanelRect => panelRect;

    [Header("Animación")]
    [SerializeField] protected float animDuration = 0.3f;
    [SerializeField] protected float hiddenY = -384f;
    [SerializeField] protected float shownY = 0f;

    protected enum PanelState { Hidden, Showing, Visible, Hiding }
    protected PanelState state = PanelState.Hidden;
    private Coroutine animCoroutine;

    public bool IsVisible => state == PanelState.Visible || state == PanelState.Showing || state == PanelState.Hiding;

    protected virtual void Awake()
    {
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, hiddenY);
        state = PanelState.Hidden;
    }

    protected void AnimateToShown(System.Action onComplete = null)
    {
        state = PanelState.Showing;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateTo(shownY, () =>
        {
            state = PanelState.Visible;
            onComplete?.Invoke();
        }));
    }

    protected void AnimateToHidden(System.Action onComplete = null)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        state = PanelState.Hiding;
        animCoroutine = StartCoroutine(AnimateTo(hiddenY, () =>
        {
            state = PanelState.Hidden;
            onComplete?.Invoke();
        }));
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

    /// <summary>
    /// Detección de click/tap este frame. Sigue leyendo Mouse/Touchscreen
    /// directamente, igual que antes en ambos paneles — pendiente de migrar
    /// a InputManager en un paso posterior de la lista.
    /// </summary>
    protected static bool TryGetClickScreenPos(out Vector2 screenPos)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPos = Vector2.zero;
        return false;
    }

    protected bool IsInsidePanel(Vector2 screenPos) =>
        RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPos, null);
}