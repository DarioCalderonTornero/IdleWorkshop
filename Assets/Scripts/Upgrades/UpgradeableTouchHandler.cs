using UnityEngine;

public class UpgradeableTouchHandler : MonoBehaviour
{
    void OnMouseDown()
    {
        IUpgradeable upgradeable = GetComponent<IUpgradeable>();
        if (upgradeable != null)
            UpgradePanelUI.Instance.Show(upgradeable);
    }
}