using TMPro;
using UnityEngine;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        EconomyManager.Instance.OnCoinsChanged += EconomyManager_OnCoinsChanged;
        coinText.text = EconomyManager.Instance.GetCurrentCoins().ToString();
    }

    private void EconomyManager_OnCoinsChanged(double obj)
    {
        coinText.text = obj.ToString();
    }

    private void OnDestroy()
    {
        EconomyManager.Instance.OnCoinsChanged -= EconomyManager_OnCoinsChanged;
    }
}
