using System;
using UnityEngine;

/// <summary>
/// Lo que pasa al entrar al juego, una vez la partida ya está aplicada.
///
/// Hoy hace una sola cosa: pagar lo que el jugador produjo mientras el juego
/// estaba cerrado y enseñárselo. Vive aparte del <see cref="SaveManager"/>
/// porque calcular ganancias no es asunto de quien guarda archivos, y porque
/// el arranque va a seguir creciendo — aquí es donde se encadenan los pasos
/// que vengan sin que el guardado engorde.
///
/// Corre en <see cref="BootOrder.PostLoad"/>: después de que la partida se haya
/// leído y aplicado, que es de donde saca la marca de tiempo y la tasa.
///
/// Si falta en la escena, el juego funciona igual —carga, guarda y se juega—
/// pero no se paga el tiempo offline. Por eso avisa en voz alta si le falta su
/// configuración, en vez de quedarse callado pagando cero.
/// </summary>
[DefaultExecutionOrder(BootOrder.PostLoad)]
public class GameBootstrap : MonoBehaviour
{
    [Header("Ganancias offline")]
    [Tooltip("Cuánto se paga y durante cuántas horas como mucho. Sin esto no se paga nada")]
    [SerializeField] private OfflineEarningsConfig offlineConfig;

    private void Start()
    {
        GrantOfflineEarnings();
    }

    private void GrantOfflineEarnings()
    {
        if (offlineConfig == null)
        {
            Debug.LogWarning(
                "[GameBootstrap] No tiene OfflineEarningsConfig asignado: no se pagará " +
                "el tiempo offline. Crea uno con Create > Idle > Ganancias offline.", this);
            return;
        }

        SaveData data = SaveManager.Instance != null ? SaveManager.Instance.LoadedData : null;

        // Partida nueva: no hay marca de tiempo anterior ni tasa medida, así
        // que no hay nada que pagar. La primera ausencia sí contará.
        if (data == null) return;

        OfflineEarnings.Result result = OfflineEarnings.Calculate(
            data.lastTimeSaved,
            DateTime.UtcNow,
            data.coinsPerSecond,
            offlineConfig.OfflineFactor,
            offlineConfig.MaxHours);

        if (!result.HasEarnings) return;

        // OneOff y no Production: esto no lo ha producido el taller ahora
        // mismo. Si contara para la tasa, una paga offline grande inflaría la
        // medición y la siguiente ausencia cobraría sobre ese pico, y así
        // hasta el infinito.
        EconomyManager.Instance.AddCoins(result.Coins, CoinSource.OneOff);

        Debug.Log(
            $"[GameBootstrap] Producción offline: {result.Coins:N0} monedas por " +
            $"{result.PaidTime.TotalHours:F1} h de las {result.TimeAway.TotalHours:F1} h fuera.");

        OfflineEarningsPopupUI.Instance?.Show(result);
    }
}
