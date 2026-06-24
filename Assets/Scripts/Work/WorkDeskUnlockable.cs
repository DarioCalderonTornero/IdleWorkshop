using UnityEngine;

public class WorkDeskUnlockable : MonoBehaviour
{
    [Header("Desbloqueo")]
    [SerializeField] private double unlockCost = 500;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Estado inicial")]
    [SerializeField] private bool unlockedByDefault = false;

    public bool IsUnlocked { get; private set; } = false;
    public double UnlockCost => unlockCost;

    public System.Action<WorkDeskUnlockable> OnUnlocked;

    void Awake()
    {
        // Aplica visual inicial antes de que WorkStation se suscriba
        if (unlockedByDefault)
        {
            IsUnlocked = true;
            SetVisual(true);
        }
        else
        {
            SetVisual(false);
        }
    }

    public void Unlock()
    {
        if (IsUnlocked) return;
        IsUnlocked = true;
        SetVisual(true);
        OnUnlocked?.Invoke(this);
    }

    void SetVisual(bool unlocked)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = unlocked ? Color.white : lockedColor;
    }

    void OnMouseDown()
    {
        if (IsUnlocked)
        {
            IUpgradeable upgradeable = GetComponent<IUpgradeable>();
            if (upgradeable != null)
                UpgradePanelUI.Instance.Show(upgradeable);
        }
        else
        {
            // Solo abrir si es la siguiente en la cola
            WorkStation station = GetComponentInParent<WorkStation>();
            if (station != null && station.GetNextLockedDesk() == this)
                UpgradePanelUI.Instance.ShowUnlock(this);
        }
    }
}