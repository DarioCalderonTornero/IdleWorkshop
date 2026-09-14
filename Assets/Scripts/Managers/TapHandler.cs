using UnityEngine;

/// <summary>
/// Centraliza toda la lógica de tap en la escena.
/// En vez de depender de la action "Tap" del Input Actions asset (que compite
/// con la action "Drag" por el mismo control y da lugar a comportamiento
/// inconsistente), decide por sí mismo si un gesto fue un toque o un arrastre
/// comparando la posición al iniciar y al soltar el press.
/// </summary>
public class TapHandler : MonoBehaviour
{
    public static TapHandler Instance { get; private set; }

    [Header("Raycast")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tapLayers;

    [Header("Detección de tap")]
    [Tooltip("Distancia máxima en píxeles de pantalla para considerar el gesto un tap y no un arrastre.")]
    [SerializeField] private float maxTapDistance = 20f;

    private bool _isPressed;
    private Vector2 _pressStartPos;
    private Vector2 _currentPos;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        InputManager.Instance.OnDragStarted += HandlePressStarted;
        InputManager.Instance.OnDragEnded += HandlePressEnded;
        InputManager.Instance.OnPointerPosition += HandlePointerPosition;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnDragStarted -= HandlePressStarted;
        InputManager.Instance.OnDragEnded -= HandlePressEnded;
        InputManager.Instance.OnPointerPosition -= HandlePointerPosition;
    }

    private void HandlePointerPosition(Vector2 pos)
    {
        _currentPos = pos;
    }

    private void HandlePressStarted()
    {
        _isPressed = true;
        _pressStartPos = _currentPos;
    }

    private void HandlePressEnded()
    {
        if (!_isPressed) return;
        _isPressed = false;

        float distance = Vector2.Distance(_pressStartPos, _currentPos);
        if (distance <= maxTapDistance)
        {
            ProcessTap(_currentPos);
        }
    }

    private void ProcessTap(Vector2 screenPosition)
    {
        if (BestiaryUI.IsOpen) return;
        if (RoomUpgradePanelUI.Instance != null && RoomUpgradePanelUI.Instance.IsVisible) return;
        if (RoomUpgradePanelUI.JustClosedThisFrame) return;

        // Boost de tap a todos los workers activos — mecánica core, independiente de la UI de mejoras.
        WorkerRegistry.Instance?.ApplyTapBoostToAll(0.1f);

        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, tapLayers);

        if (hit.collider == null) return;

        // Cada zona mejorable lleva su propio panel: el taller sus mesas, la
        // recepción sus carritos, los mostradores sus recepcionistas.
        UpgradeZone zone = hit.collider.GetComponentInParent<UpgradeZone>();
        if (zone == null) return;

        CameraController.Instance?.FocusOn(zone.CameraFocusPosition);
        RoomUpgradePanelUI.Instance?.Show(zone);
    }
}
