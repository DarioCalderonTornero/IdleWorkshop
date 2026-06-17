using UnityEngine;

public class EconomyTester : MonoBehaviour
{
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.aKey.wasPressedThisFrame)
        {
            EconomyManager.Instance.AddCoins(100);
        }

        if (UnityEngine.InputSystem.Keyboard.current.sKey.wasPressedThisFrame)
        {
            bool success = EconomyManager.Instance.SpendCoins(50);
            Debug.Log(success ? "Gasto exitoso." : "No hay suficientes monedas.");
        }

        if (UnityEngine.InputSystem.Keyboard.current.dKey.wasPressedThisFrame)
        {
            Debug.Log($"[EconomyTester] Saldo actual: {EconomyManager.Instance.CurrentCoins}");
        }
    }
}