using UnityEngine;
using UnityEngine.InputSystem;

public class EconomyTester : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            EconomyManager.Instance.AddCoins(1143);
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
    }
}