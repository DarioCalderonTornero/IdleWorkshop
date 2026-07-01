using UnityEngine;

/// <summary>
/// Centraliza toda la lógica de tap en la escena.
/// Se suscribe a InputManager.OnTap, hace un raycast y decide qué hacer
/// según el componente que encuentre en el GameObject tocado.
/// </summary>
public class TapHandler : MonoBehaviour
{
    public static TapHandler Instance { get; private set; }

    [Header("Raycast")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tapLayers;

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
        InputManager.Instance.OnTap += HandleTap;
    }

    private void OnDestroy()
    {
        InputManager.Instance.OnTap -= HandleTap;
    }
    
    //Tap
    private void HandleTap(Vector2 screenPosition)
    {
        if (BestiaryUI.IsOpen) return;
        if (UpgradePanelUI.Instance.IsVisible) return;

        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, tapLayers);

        if (hit.collider == null) return;

        GameObject tapped = hit.collider.gameObject;

        // ¿Es el Worker?
        WorkerUpgradeable workerUpgradeable = tapped.GetComponent<WorkerUpgradeable>();
        if (workerUpgradeable != null)
        {
            UpgradePanelUI.Instance.Show(workerUpgradeable);
            return;
        }

        // ¿Es una WorkDesk?
        WorkDeskUnlockable unlockable = tapped.GetComponent<WorkDeskUnlockable>();
        if (unlockable != null)
        {
            if (unlockable.IsUnlocked)
            {
                IUpgradeable upgradeable = tapped.GetComponent<IUpgradeable>();
                if (upgradeable != null)
                    UpgradePanelUI.Instance.Show(upgradeable);
            }
            else
            {
                WorkStation station = tapped.GetComponentInParent<WorkStation>();
                if (station != null && station.GetNextLockedDesk() == unlockable)
                    UpgradePanelUI.Instance.ShowUnlock(unlockable);
            }
            return;
        }
    }
}