using UnityEngine;

public class UpgradeableTouchHandler : MonoBehaviour
{
    void OnMouseDown()
    {
        // Si tiene WorkDeskUnlockable y está bloqueada
        WorkDeskUnlockable unlockable = GetComponent<WorkDeskUnlockable>();
        if (unlockable != null && !unlockable.IsUnlocked)
        {
            // Solo muestra panel si es la siguiente en la cola
            WorkDeskManager manager = GetComponentInParent<WorkDeskManager>();
            if (manager != null && manager.GetNextLocked() == unlockable)
                UpgradePanelUI.Instance.ShowUnlock(unlockable);
            return;
        }

        // Si tiene IUpgradeable y está desbloqueada
        IUpgradeable upgradeable = GetComponent<IUpgradeable>();
        if (upgradeable != null)
            UpgradePanelUI.Instance.Show(upgradeable);
    }
}