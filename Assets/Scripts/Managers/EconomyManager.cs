using System;
using UnityEngine;

/// <summary>
/// Gestor centralizado de la economía del juego.
/// Es el ÚNICO sitio donde existe el saldo del jugador.
/// Ningún otro sistema guarda monedas directamente.
/// 
/// Uso:
///   EconomyManager.Instance.AddCoins(100);
///   EconomyManager.Instance.SpendCoins(50);
///   EconomyManager.Instance.CanAfford(200);
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    // ── Eventos ─────────────────────────────────────────────────────
    /// <summary>
    /// Se dispara cada vez que el saldo cambia.
    /// La UI y cualquier otro sistema se suscriben aquí.
    /// </summary>
    public event Action<double> OnCoinsChanged;

    // ── Estado ──────────────────────────────────────────────────────
    [Header("Configuración inicial")]
    [SerializeField] private double startingCoins = 0;

    private double _currentCoins;

    // ── Propiedades públicas ────────────────────────────────────────
    public double CurrentCoins => _currentCoins;

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

        _currentCoins = startingCoins;
    }

    private void Update()
    {
         /////TEST/////
         if (UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame)
        {
            Time.timeScale = 2.0f;
        }

         if (UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame)
        {
            Time.timeScale = 1.0f;
        }
    }

    // ── API pública ─────────────────────────────────────────────────

    /// <summary>
    /// Añade monedas al saldo y notifica a los suscriptores.
    /// </summary>
    public void AddCoins(double amount)
    {
        if (amount <= 0) return;

        _currentCoins += amount;
        OnCoinsChanged?.Invoke(_currentCoins);

        Debug.Log($"[EconomyManager] +{amount} monedas. Total: {_currentCoins}");
    }

    /// <summary>
    /// Resta monedas si hay suficiente saldo.
    /// Devuelve true si se pudo gastar, false si no había suficiente.
    /// </summary>
    public bool SpendCoins(double amount)
    {
        if (!CanAfford(amount))
        {
            Debug.LogWarning($"[EconomyManager] Saldo insuficiente. Necesario: {amount}, Disponible: {_currentCoins}");
            return false;
        }

        _currentCoins -= amount;
        OnCoinsChanged?.Invoke(_currentCoins);

        Debug.Log($"[EconomyManager] -{amount} monedas. Total: {_currentCoins}");
        return true;
    }

    /// <summary>
    /// Devuelve true si el jugador puede permitirse el gasto.
    /// Útil para pintar botones en gris en la UI.
    /// </summary>
    public bool CanAfford(double amount)
    {
        return _currentCoins >= amount;
    }
}
