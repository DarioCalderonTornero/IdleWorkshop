using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Globalization;

public class EconomyTester : MonoBehaviour
{
    [SerializeField] private TMP_InputField coinInputField;

    private bool isCoinInputFieldActive = false;

    private void Start()
    {
        HideInputField();   
    }

    private void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            // OneOff: el dinero de pruebas no es producci√≥n. Si contara, teclear
            // monedas disparar√≠a la tasa y el tiempo offline pagar√≠a sobre ella.
            EconomyManager.Instance.AddCoins(1143, CoinSource.OneOff);
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            bool success = EconomyManager.Instance.SpendCoins(10534);
            Debug.Log(success ? "Gasto exitoso." : "No hay suficientes monedas.");
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            SaveManager.Instance.DeleteSave();
        }

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            if (!isCoinInputFieldActive)
            {
                ShowInputField();
            }

            else if (isCoinInputFieldActive)
            {
                HideInputField();
            }

            isCoinInputFieldActive = !isCoinInputFieldActive;
        }  

        if (isCoinInputFieldActive  && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            TryAddCoinsFromInput();
            coinInputField.text = "0";
        }
    }

    private void TryAddCoinsFromInput()
    {
        string rawText = coinInputField.text;

        if (double.TryParse(rawText, NumberStyles.Number, CultureInfo.InvariantCulture, out double amount))
        {
            EconomyManager.Instance.AddCoins(amount, CoinSource.OneOff);
            Debug.Log($"AÒadidas {amount} al juego");
        }

        else
        {
            Debug.LogWarning($"No se ha podido aÒadir {amount}");
        }
    }

    void ShowInputField()
    {
        coinInputField.gameObject.SetActive(true);  
    }

    void HideInputField()
    {
        coinInputField.gameObject.SetActive(false);
    }
}