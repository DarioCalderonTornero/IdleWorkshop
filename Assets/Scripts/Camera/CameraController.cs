using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Límites del mundo")]
    [SerializeField] private float xMin = -10f;
    [SerializeField] private float xMax = 10f;
    [SerializeField] private float yMin = -10f;
    [SerializeField] private float yMax = 10f;

    [Header("Movimiento")]
    [SerializeField] private float dragSpeed = 1f;
    [Tooltip("Desplazamiento máximo (en unidades de mundo) permitido en un solo frame de arrastre. Evita saltos bruscos si el puntero llega con una posición desactualizada (por ejemplo, tras perder el foco de la ventana).")]
    [SerializeField] private float maxDragDeltaPerFrame = 3f;
    [Tooltip("Cuánto aumenta la velocidad de movimiento al alejarse del zoom por defecto, en cualquiera de las dos direcciones (zoom in o zoom out). 0 = sin efecto, 1 = hasta el doble de velocidad en los extremos.")]
    [Range(0f, 2f)]
    [SerializeField] private float speedZoomInfluence = 0.3f;

    [Header("Inercia")]
    [SerializeField] private bool useInertia = true;
    [Tooltip("Mayor valor = frenado más gradual y perceptible al soltar.")]
    [SerializeField] private float inertiaSmoothTime = 0.4f;
    [Tooltip("Cuánto se promedia la velocidad mientras arrastras (0 = solo el último frame, 1 = casi no cambia). Evita que soltar 'suavemente' anule la inercia.")]
    [Range(0f, 0.95f)]
    [SerializeField] private float velocitySmoothing = 0.6f;

    [Header("Zoom")]
    [SerializeField] private float zoomMin = 2f;
    [SerializeField] private float zoomMax = 8f;
    [SerializeField] private float defaultZoom = 5f;
    [Tooltip("Sensibilidad del zoom por rueda de ratón (el pinch tiene su propia sensibilidad en InputManager).")]
    [SerializeField] private float scrollZoomSensitivity = 0.5f;

    [Header("Enfoque de habitación")]
    [SerializeField] private float focusDuration = 0.5f;

    private Camera _cam;
    private bool _isDragging;
    private bool _isLocked;
    private Vector3 _dragOriginWorld;
    private Vector2 _currentScreenPos;
    private Vector2 _velocity;
    private Vector2 _inertiaVelocityRef;
    private Coroutine _focusCoroutine;

    private void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();

        if (_cam.orthographicSize <= 0f)
            _cam.orthographicSize = defaultZoom;
    }

    private void OnEnable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnDragStarted += HandleDragStarted;
        InputManager.Instance.OnDragEnded += HandleDragEnded;
        InputManager.Instance.OnPointerPosition += HandlePointerPosition;
        InputManager.Instance.OnZoom += HandleZoom;
    }

    private void OnDisable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OnDragStarted -= HandleDragStarted;
        InputManager.Instance.OnDragEnded -= HandleDragEnded;
        InputManager.Instance.OnPointerPosition -= HandlePointerPosition;
        InputManager.Instance.OnZoom -= HandleZoom;
    }

    private void Update()
    {
        if (_isLocked) return;

        if (_isDragging)
            ApplyDrag();
        else if (useInertia)
            ApplyInertia();

        ClampPosition();
    }

    // ── Callbacks de InputManager ────────────────────────────────────
    private void HandleDragStarted()
    {
        if (_isLocked) return;
        _isDragging = true;
        _velocity = Vector2.zero;
        _dragOriginWorld = GetWorldPoint(_currentScreenPos);
    }

    private void HandleDragEnded()
    {
        _isDragging = false;
    }

    private void HandlePointerPosition(Vector2 screenPos)
    {
        _currentScreenPos = screenPos;
    }

    private void HandleZoom(float delta, Vector2 screenPosition)
    {
        if (_isLocked) return;

        Vector3 worldBefore = GetWorldPoint(screenPosition);

        float newSize = _cam.orthographicSize - delta * scrollZoomSensitivity;
        _cam.orthographicSize = Mathf.Clamp(newSize, zoomMin, zoomMax);

        Vector3 worldAfter = GetWorldPoint(screenPosition);
        transform.position += worldBefore - worldAfter;

        ClampPosition();
    }

    // ── Drag ─────────────────────────────────────────────────────────
    private void ApplyDrag()
    {
        Vector3 currentWorld = GetWorldPoint(_currentScreenPos);
        Vector3 delta = _dragOriginWorld - currentWorld;

        // Blindaje contra saltos bruscos (ej. puntero desactualizado tras perder el foco).
        if (delta.magnitude > maxDragDeltaPerFrame)
            delta = delta.normalized * maxDragDeltaPerFrame;

        float zoomSpeedMultiplier = GetZoomSpeedMultiplier();
        transform.position += delta * dragSpeed * zoomSpeedMultiplier;

        // Velocidad suavizada (media móvil): evita que un frame final casi
        // estático (dedo desacelerando antes de soltar, algo natural al usar
        // el juego) anule por completo la inercia posterior.
        Vector2 instantVelocity = (Vector2)delta / Time.deltaTime;
        _velocity = Vector2.Lerp(instantVelocity, _velocity, velocitySmoothing);

        _dragOriginWorld = GetWorldPoint(_currentScreenPos);
    }

    /// <summary>
    /// Multiplicador de velocidad según distancia al zoom por defecto.
    /// 1 en el zoom por defecto, aumentando hacia cualquiera de los dos extremos (zoomMin o zoomMax).
    /// </summary>
    private float GetZoomSpeedMultiplier()
    {
        float normalizedDistance;

        if (_cam.orthographicSize < defaultZoom)
        {
            float range = defaultZoom - zoomMin;
            normalizedDistance = range > 0f ? (defaultZoom - _cam.orthographicSize) / range : 0f;
        }
        else
        {
            float range = zoomMax - defaultZoom;
            normalizedDistance = range > 0f ? (_cam.orthographicSize - defaultZoom) / range : 0f;
        }

        normalizedDistance = Mathf.Clamp01(normalizedDistance);
        return 1f + normalizedDistance * speedZoomInfluence;
    }

    // ── Inercia ──────────────────────────────────────────────────────
    private void ApplyInertia()
    {
        if (_velocity.sqrMagnitude < 0.0001f)
        {
            _velocity = Vector2.zero;
            return;
        }

        transform.position += (Vector3)(_velocity * Time.deltaTime);
        _velocity = Vector2.SmoothDamp(_velocity, Vector2.zero, ref _inertiaVelocityRef, inertiaSmoothTime);
    }

    // ── Límites ───────────────────────────────────────────────────────
    private void ClampPosition()
    {
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;

        float clampedX = Mathf.Clamp(transform.position.x, xMin + halfW, xMax - halfW);
        float clampedY = Mathf.Clamp(transform.position.y, yMin + halfH, yMax - halfH);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    // ── Utilidades ────────────────────────────────────────────────────
    private Vector3 GetWorldPoint(Vector2 screenPos)
    {
        Vector3 pos = screenPos;
        pos.z = -_cam.transform.position.z;
        return _cam.ScreenToWorldPoint(pos);
    }

    // ── Foco de habitación ──────────────────────────────────────────
    public void FocusOn(Vector3 targetPosition)
    {
        _isLocked = true;
        _isDragging = false;
        _velocity = Vector2.zero;

        if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);
        _focusCoroutine = StartCoroutine(FocusRoutine(targetPosition));
    }

    public void Unlock()
    {
        _isLocked = false;
    }

    private IEnumerator FocusRoutine(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);

        float startSize = _cam.orthographicSize;
        float targetSize = defaultZoom;

        float elapsed = 0f;

        while (elapsed < focusDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / focusDuration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            _cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            ClampPosition();

            yield return null;
        }

        transform.position = targetPos;
        _cam.orthographicSize = targetSize;
        ClampPosition();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Vector3 center = new Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f, 0f);
        Vector3 size = new Vector3(xMax - xMin, yMax - yMin, 0f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.08f);
        Gizmos.DrawCube(center, size);
    }
#endif
}