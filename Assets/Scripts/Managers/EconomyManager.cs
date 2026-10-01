using System;
using UnityEngine;

/// <summary>
/// Gestor centralizado de la economía del juego.
///
/// Es el ÚNICO sitio donde existe el saldo del jugador, y `currentCoins` es
/// privado a propósito: no es un detalle de estilo, es de lo que depende que la
/// producción offline sea correcta.
///
/// La paga por estar fuera no se simula, se mide: aquí se lleva la cuenta de
/// cuánto gana el jugador por segundo, y esa tasa es la que se le paga al
/// volver. Si algún día alguien suma monedas por otro lado, esa producción no
/// se vería y el juego pagaría de menos sin dar ningún error. Por eso todo
/// entra por <see cref="AddCoins"/> y por eso cada ingreso dice de dónde viene.
///
/// Uso:
///   EconomyManager.Instance.AddCoins(100);                        // producción
///   EconomyManager.Instance.AddCoins(500, CoinSource.OneOff);     // regalo, anuncio...
///   EconomyManager.Instance.SpendCoins(50);
///   EconomyManager.Instance.CanAfford(200);
/// </summary>
[DefaultExecutionOrder(BootOrder.Manager)]
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public event Action<double> OnCoinsChanged;

    // ── Estado ──────────────────────────────────────────────────────
    [Header("Configuración inicial")]
    [SerializeField] private double startingCoins = 0;

    [Header("Medición de producción")]
    [Tooltip("Sobre cuántos segundos se mide la tasa. Tiene que cubrir varios ciclos " +
             "completos del cobro más lento (el trabajador de sala, 90 s) o la tasa " +
             "saldría a saltos según el instante en que se guarde")]
    [SerializeField] private float rateWindowSeconds = 300f;

    [Tooltip("Suelo del divisor. Sin esto, cobrar 1.200 a los tres segundos de arrancar " +
             "leería 400 monedas/s y por 24 horas serían millones por haber jugado un momento")]
    [SerializeField] private float rateMinSampleSeconds = 60f;

    private double currentCoins;
    private EarningsRateMeter _rateMeter;

    /// <summary>
    /// Tasa que traía la partida guardada. Se usa mientras la sesión actual sea
    /// demasiado corta para fiarse de lo medido: si no, entrar un momento y
    /// salir machacaría una tasa buena con una mala y el jugador perdería su
    /// producción offline sin saber por qué.
    /// </summary>
    private double _loadedRate;

    // ── Propiedades públicas ────────────────────────────────────────
    public double CurrentCoins => currentCoins;

    /// <summary>
    /// Monedas por segundo que produce el montaje del jugador. Es lo que se
    /// guarda y lo que luego paga el tiempo offline.
    /// </summary>
    public double CoinsPerSecond =>
        _rateMeter != null && _rateMeter.HasEnoughSample ? _rateMeter.CoinsPerSecond : _loadedRate;

    // ── Unity ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentCoins = startingCoins;
        _rateMeter = new EarningsRateMeter(rateWindowSeconds, 60, rateMinSampleSeconds);
    }

    // ── API pública ─────────────────────────────────────────────────

    /// <summary>
    /// Añade monedas al saldo y notifica a los suscriptores.
    /// </summary>
    /// <param name="source">
    /// De dónde vienen. Solo <see cref="CoinSource.Production"/> cuenta para la
    /// tasa de producción; un regalo o una recompensa por anuncio la dispararía
    /// y el juego pagaría 24 horas calculadas sobre ese pico.
    /// </param>
    public void AddCoins(double amount, CoinSource source = CoinSource.Production)
    {
        if (amount <= 0) return;

        currentCoins += amount;

        if (source == CoinSource.Production)
            _rateMeter.Record(amount);

        OnCoinsChanged?.Invoke(currentCoins);
    }

    /// <summary>
    /// Resta monedas si hay suficiente saldo.
    /// Devuelve true si se pudo gastar, false si no había suficiente.
    /// </summary>
    public bool SpendCoins(double amount)
    {
        if (!CanAfford(amount))
        {
            Debug.LogWarning($"[EconomyManager] Saldo insuficiente. Necesario: {amount}, Disponible: {currentCoins}");
            return false;
        }

        currentCoins -= amount;
        OnCoinsChanged?.Invoke(currentCoins);
        return true;
    }

    /// <summary>
    /// Devuelve true si el jugador puede permitirse el gasto.
    /// Útil para pintar botones en gris en la UI.
    /// </summary>
    public bool CanAfford(double amount)
    {
        return currentCoins >= amount;
    }

    public void LoadCoins(double amount)
    {
        currentCoins = amount;
        OnCoinsChanged?.Invoke(currentCoins);
    }

    /// <summary>Recupera la tasa de producción de la partida guardada.</summary>
    public void LoadRate(double coinsPerSecond)
    {
        _loadedRate = Math.Max(0, coinsPerSecond);
    }
}
