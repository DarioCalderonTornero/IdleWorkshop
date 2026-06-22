using System;
using UnityEngine;
using UnityEngine.UI;

public class UnlockTest : MonoBehaviour
{
    [SerializeField] private Button unlockButton;
    [SerializeField] private Image backgroundImage;

    private bool togglePanel = true;

    private void Awake()
    {
        unlockButton.onClick.AddListener(() =>
        {
            TryUnlock();
        });
    }

    private void Start()
    {
        //HideUnlockPanel();
    }

    private void TryUnlock()
    { 
         WorkStationUnlocker.Instance.UnlockNextWorkStation();
         //EconomyManager.Instance.SpendCoins(500);  
    }

    public void TogglePanel()
    {
        if (togglePanel)
        {
            ShowUnlockPanel();
        }

        else if (!togglePanel)
        {
            HideUnlockPanel();
        }

        togglePanel = !togglePanel;
    }

    private void ShowUnlockPanel()
    {
        gameObject.SetActive(true);
    }

    private void HideUnlockPanel()
    {
        gameObject.SetActive(false);
    }
}
