using UnityEngine;
using UnityEngine.UI;

public class RoomUpgradeButtonUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image lockedOverlay;
    [SerializeField] private Button button;

    public void Setup(Sprite icon, bool isLocked, System.Action onClick)
    {
        iconImage.sprite = icon;

        if (lockedOverlay != null)
            lockedOverlay.gameObject.SetActive(isLocked);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}