using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de cámara 2D para juego idle.
/// Compatible con el New Input System de Unity 6.
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
    private Vector2 _velocity;

    // ── Unity ───────────────────────────────────────────────────────
    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Update()
    {
        HandleInput();

        if (!_isDragging && useInertia)
            ApplyInertia();

        ClampPosition();
    }

    // ── Input (New Input System) ─────────────────────────────────────
    private void HandleInput()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current;

        bool pressedThisFrame = false;
        bool heldThisFrame = false;
        bool releasedThisFrame = false;
        Vector2 screenPos = Vector2.zero;

        // -- Ratón --
        if (mouse != null)
        {
            pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
            heldThisFrame = mouse.leftButton.isPressed;
            releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
            screenPos = mouse.position.ReadValue();
        }

        // -- Táctil (un dedo) --
        if (touch != null && touch.touches.Count > 0)
        {
            var finger = touch.touches[0];
            pressedThisFrame = pressedThisFrame || finger.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began;
            heldThisFrame = heldThisFrame || finger.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved
                                                  || finger.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary;
            releasedThisFrame = releasedThisFrame || finger.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended
                                                  || finger.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;
            screenPos = finger.position.ReadValue();
        }

        // ---- Inicio del drag ----
        if (pressedThisFrame)
        {
            _isDragging = true;
            _velocity = Vector2.zero;
            _dragOriginWorld = GetWorldPoint(screenPos);
        }

        // ---- Durante el drag ----
        if (heldThisFrame && _isDragging)
        {
            Vector3 currentWorld = GetWorldPoint(screenPos);
            Vector3 delta = _dragOriginWorld - currentWorld;

            _velocity = delta / Time.deltaTime;

            transform.position += delta * dragSpeed;
            _dragOriginWorld = GetWorldPoint(screenPos);
        }

        // ---- Fin del drag ----
        if (releasedThisFrame)
        {
            _isDragging = false;
        }
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