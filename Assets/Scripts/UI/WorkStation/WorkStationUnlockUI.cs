using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkStationUnlockUI : MonoBehaviour
{

    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockText;

    private void Awake()
    {
        unlockButton.onClick.AddListener(OnUnlockClicked);
    }

    private void Start()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnCoinsChanged += OnCoinsChanged;
        }

        if (WorkStationUnlocker.Instance != null)
        {
            WorkStationUnlocker.Instance.OnWorkStationUnlocked += OnStateChanged;
        }
    }

    private void OnStateChanged()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        var unlocker = WorkStationUnlocker.Instance;
        if (unlocker == null) return;

        if (!unlocker.HasNext)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        unlockText.text = $"Desbloquear taller\n{CurrencyFormatter.Format(unlocker.NextCost)}monedas";
        unlockButton.interactable = unlocker.CanUnlockNext;
    }

    private void OnCoinsChanged(double obj)
    {
        RefreshUI();
    }

    private void OnUnlockClicked()
    {
        WorkStationUnlocker.Instance?.UnlockNextWorkStation();
    }
}
