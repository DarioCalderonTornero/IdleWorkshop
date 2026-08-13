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


    //---ZOOM---
    public event Action<float, Vector2> OnZoom;

    [Header("Pinch Tactil")]
    [SerializeField] private float pinchSensivity;

    private bool isPinched;
    private float previousZoomDistance;

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

        //Zoom
        idleInputActions.Camera.Zoom.performed += Zoom_performed;
    }

    private void Update()
    {
        HandlePinchZoom();
    }

    private void HandlePinchZoom()
    {
        bool touch0Active = idleInputActions.Camera.Touch0Contact.IsPressed();
        bool touch1Active = idleInputActions.Camera.Touch1Contact.IsPressed();

        if (!touch0Active || !touch1Active)
        {
            isPinched = false;
            return;
        }

        Vector2 posA = idleInputActions.Camera.Touch0Position.ReadValue<Vector2>();
        Vector2 posB = idleInputActions.Camera.Touch1Position.ReadValue<Vector2>();

        float currentDistance = Vector2.Distance(posA, posB);

        if (!isPinched)
        {
            isPinched = true;
            previousZoomDistance = currentDistance;
            return;
        }

        float distanceDelta = currentDistance - previousZoomDistance;
        previousZoomDistance = currentDistance;

        if (Mathf.Approximately(distanceDelta, 0.0f))
            return;

        Vector2 pivot = (posA + posB) * 0.5f;
        OnZoom.Invoke(distanceDelta * pinchSensivity, pivot);
    }

    private void Zoom_performed(InputAction.CallbackContext obj)
    {
        float scrollDelta = obj.ReadValue<float>();
        if (Mathf.Approximately(scrollDelta, 0))
        {
            return;
        }

        OnZoom?.Invoke(scrollDelta, currentPos);
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
