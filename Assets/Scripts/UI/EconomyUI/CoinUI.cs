using TMPro;
using UnityEngine;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        EconomyManager.Instance.OnCoinsChanged += EconomyManager_OnCoinsChanged;
        coinText.text = CurrencyFormatter.Format(EconomyManager.Instance.CurrentCoins);
    }

    private void EconomyManager_OnCoinsChanged(double obj)
    {
        coinText.text = CurrencyFormatter.Format(obj);
    }

    private void OnDestroy()
    {
        EconomyManager.Instance.OnCoinsChanged -= EconomyManager_OnCoinsChanged;
    }
}
