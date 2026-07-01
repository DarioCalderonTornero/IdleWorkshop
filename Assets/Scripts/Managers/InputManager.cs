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


    

    //Tap Event
    public event Action<Vector2> OnTap;

    private Vector2 currentPos;

    private IdleInputActions idleInputActions;

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

        idleInputActions = new IdleInputActions();

        // ── Bindings de cámara ──────────────────────────────────────
        idleInputActions.Camera.Drag.started += Drag_started;
        idleInputActions.Camera.Drag.canceled += Drag_canceled;

        //Tap interaction
        idleInputActions.Camera.Tap.performed += Tap_performed;

        idleInputActions.Camera.PointerPosition.performed += PointerPosition_performed;
    }

    private void PointerPosition_performed(InputAction.CallbackContext obj)
    {
        currentPos = obj.ReadValue<Vector2>();
        OnPointerPosition?.Invoke(currentPos);
    }

    private void Drag_canceled(InputAction.CallbackContext obj)
    {
        OnDragEnded?.Invoke();
    }

    private void Drag_started(InputAction.CallbackContext obj)
    {
        OnDragStarted?.Invoke();
    }

    private void Tap_performed(InputAction.CallbackContext obj)
    {
        OnTap?.Invoke(currentPos);
    }

    private void OnEnable() => idleInputActions?.Enable();
    private void OnDisable() => idleInputActions?.Disable();
    private void OnDestroy() => idleInputActions?.Dispose();
}
