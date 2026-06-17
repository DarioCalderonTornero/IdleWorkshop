using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // ── Eventos de cámara ───────────────────────────────────────────
    public event Action OnDragStarted;

    public event Action OnDragEnded;

    public event Action<Vector2> OnPointerPosition;

    public event Action OnTap;

    private IdleInputActions _idleInputActions;

    // ── Unity ───────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _idleInputActions = new IdleInputActions();

        // ── Bindings de cámara ──────────────────────────────────────
        _idleInputActions.Camera.Drag.started += _ => OnDragStarted?.Invoke();
        _idleInputActions.Camera.Drag.canceled += _ => OnDragEnded?.Invoke();

        _idleInputActions.Camera.PointerPosition.performed += ctx =>
            OnPointerPosition?.Invoke(ctx.ReadValue<Vector2>());

        _idleInputActions.Camera.Drag.started += _ => OnTap?.Invoke();
    }

    private void OnEnable() => _idleInputActions?.Enable();
    private void OnDisable() => _idleInputActions?.Disable();
    private void OnDestroy() => _idleInputActions?.Dispose();
}
