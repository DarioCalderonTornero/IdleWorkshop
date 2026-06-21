using UnityEngine;

/// <summary>
/// Controlador de cámara 2D para juego idle.
/// Se suscribe a InputManager en vez de acceder al hardware directamente.
/// Permite mover la cámara arrastrando (ratón o dedo) dentro de unos límites definidos.
/// Incluye inercia suave al soltar.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Límites del mundo")]
    [SerializeField] private float xMin = -10f;
    [SerializeField] private float xMax = 10f;
    [SerializeField] private float yMin = -10f;
    [SerializeField] private float yMax = 10f;

    [Header("Movimiento")]
    [SerializeField] private float dragSpeed = 1f;

    [Header("Inercia")]
    [SerializeField] private bool useInertia = true;
    [SerializeField] private float inertiaFriction = 8f;

    // ── Estado interno ──────────────────────────────────────────────
    private Camera _cam;
    private bool _isDragging;
    private Vector3 _dragOriginWorld;
    private Vector2 _currentScreenPos;
    private Vector2 _velocity;

    // ── Unity ───────────────────────────────────────────────────────
    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (InputManager.Instance == null) return;

        InputManager.Instance.OnDragStarted += HandleDragStarted;
        InputManager.Instance.OnDragEnded += HandleDragEnded;
        InputManager.Instance.OnPointerPosition += HandlePointerPosition;
    }

    private void OnDisable()
    {
        if (InputManager.Instance == null) return;

        InputManager.Instance.OnDragStarted -= HandleDragStarted;
        InputManager.Instance.OnDragEnded -= HandleDragEnded;
        InputManager.Instance.OnPointerPosition -= HandlePointerPosition;
    }

    private void Update()
    {
        if (_isDragging)
            ApplyDrag();
        else if (useInertia)
            ApplyInertia();

        ClampPosition();
    }

    // ── Callbacks de InputManager ────────────────────────────────────
    private void HandleDragStarted()
    {
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

    // ── Drag ─────────────────────────────────────────────────────────
    private void ApplyDrag()
    {
        Vector3 currentWorld = GetWorldPoint(_currentScreenPos);
        Vector3 delta = _dragOriginWorld - currentWorld;

        _velocity = delta / Time.deltaTime;

        transform.position += delta * dragSpeed;
        _dragOriginWorld = GetWorldPoint(_currentScreenPos);
    }

    // ── Inercia ──────────────────────────────────────────────────────
    private void ApplyInertia()
    {
        if (_velocity.sqrMagnitude < 0.01f)
        {
            _velocity = Vector2.zero;
            return;
        }

        transform.position += (Vector3)(_velocity * Time.deltaTime);
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, inertiaFriction * Time.deltaTime);
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

    // ── Gizmos ───────────────────────────────────────────────────────
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
