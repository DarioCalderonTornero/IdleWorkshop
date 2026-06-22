using UnityEngine;
using UnityEngine.UI;

public class WorkUpgradeButtonTest : MonoBehaviour
{
    [SerializeField] private UnlockTest unlockTest;

    private Button upgradeButton;

    private void Awake()
    {
        upgradeButton = GetComponent<Button>();

        upgradeButton.onClick.AddListener(() =>
        {
            unlockTest.TogglePanel();
        });
    }
}
