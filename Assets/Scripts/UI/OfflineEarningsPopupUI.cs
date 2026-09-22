using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// El aviso de "mientras no estabas": cuánto tiempo ha pasado fuera el jugador
/// y qué ha producido su taller mientras tanto.
///
/// Las monedas ya están abonadas cuando esto aparece (lo hace el
/// <see cref="GameBootstrap"/>), así que el botón solo cierra. Se hizo así para
/// no tener que decidir qué pasa si el jugador cierra la app sin pulsarlo: no
/// hay recompensa pendiente que perder. Si algún día quieres el clásico "ver
/// anuncio para duplicar", eso se añade encima como un abono extra.
///
/// Hereda de <see cref="SlidingPanelUI"/>, la misma base que los paneles de
/// mejora, para que entre y salga igual que el resto de la interfaz.
/// </summary>
public class OfflineEarningsPopupUI : SlidingPanelUI
{
    public static OfflineEarningsPopupUI Instance { get; private set; }

    [Header("Contenido")]
    [Tooltip("Cuánto tiempo ha estado fuera")]
    [SerializeField] private TextMeshProUGUI timeAwayText;

    [Tooltip("Las monedas producidas mientras tanto")]
    [SerializeField] private TextMeshProUGUI earningsText;

    [Tooltip("Cierra el aviso. Las monedas ya están abonadas, así que solo cierra")]
    [SerializeField] private Button claimButton;

    [Header("Animación")]
    [Tooltip("Lo que espera antes de entrar. Sin esto sale pegado al arranque, encima de " +
             "la escena montándose, y no da tiempo ni a ver cómo entra")]
    [Min(0f)]
    [SerializeField] private float showDelay = 0.8f;

    [Tooltip("Cuánto se pasa de largo al entrar y cuánta carrerilla toma al salir. " +
             "0 = sin rebote, igual que los paneles de mejora. 1.6 se nota bastante")]
    [Min(0f)]
    [SerializeField] private float overshoot = 1.6f;

    [Header("Fondo")]
    [Tooltip("El velo oscuro que tapa el juego. Se enciende solo mientras el aviso está " +
             "visible, así que déjalo DESACTIVADO en la escena")]
    [SerializeField] private GameObject dimmer;

    protected override void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // SlidingPanelUI coloca el panel en su sitio oculto nada más despertar;
        // sin referencia no puede, y el fallo saldría lejos de su causa.
        if (panelRect == null)
        {
            Debug.LogError(
                $"[OfflineEarningsPopupUI] {name}: falta asignar 'panelRect'. " +
                "El aviso de producción offline no se mostrará.", this);

            enabled = false;
            return;
        }

        base.Awake();

        if (dimmer != null) dimmer.SetActive(false);
        if (claimButton != null) claimButton.onClick.AddListener(Hide);

        WarnIfDimmerMoves();
    }

    /// <summary>
    /// El velo tiene que quedarse quieto mientras el panel entra y sale. Si
    /// cuelga del propio panel se mueve con él, y lo que se ve es toda la
    /// pantalla deslizándose en vez del aviso.
    ///
    /// Pasa con solo arrastrar el objeto equivocado a 'panelRect' —el
    /// contenedor en vez del panel— y no da ningún error: simplemente se ve
    /// mal. Por eso se avisa aquí.
    /// </summary>
    private void WarnIfDimmerMoves()
    {
        if (dimmer == null || panelRect == null) return;
        if (!dimmer.transform.IsChildOf(panelRect)) return;

        Debug.LogError(
            $"[OfflineEarningsPopupUI] '{dimmer.name}' cuelga de '{panelRect.name}', que es " +
            "lo que se anima, así que el velo se deslizará con el panel. " +
            "En 'panelRect' va el panel que se mueve, no el contenedor que lo agrupa.", this);
    }

    private void OnDestroy()
    {
        if (claimButton != null) claimButton.onClick.RemoveListener(Hide);
        if (Instance == this) Instance = null;
    }

    // ── Mostrar y cerrar ─────────────────────────────────────────────

    public void Show(OfflineEarnings.Result result)
    {
        if (!enabled) return;

        if (timeAwayText != null) timeAwayText.text = FormatTimeAway(result);
        if (earningsText != null) earningsText.text = "+" + CurrencyFormatter.Format(result.Coins);

        StartCoroutine(ShowRoutine());
    }

    /// <summary>
    /// Deja respirar un momento antes de entrar.
    ///
    /// El velo se enciende al empezar el movimiento y no antes: si se encendiera
    /// durante la espera, el jugador vería la pantalla oscurecerse sin que
    /// apareciera nada.
    ///
    /// En tiempo real y no de juego, para que las teclas de prueba que cambian
    /// timeScale no lo alarguen ni lo acorten.
    /// </summary>
    private IEnumerator ShowRoutine()
    {
        if (showDelay > 0f) yield return new WaitForSecondsRealtime(showDelay);

        if (dimmer != null) dimmer.SetActive(true);

        AnimateToShown();
    }

    public void Hide()
    {
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        AnimateToHidden(() =>
        {
            // Al terminar la animación, no al empezarla: si se apagara antes,
            // el panel bajaría sobre el juego ya destapado.
            if (dimmer != null) dimmer.SetActive(false);

            // Este aviso es de una sola vez: no vuelve a salir en toda la
            // sesión. Apagarlo lo quita de en medio del todo, así que deja de
            // capturar toques aunque su sitio oculto quedara mal puesto.
            gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// Entra pasándose de largo y volviendo, y sale tomando carrerilla hacia
    /// arriba antes de caer. Es un aviso que aparece una vez al entrar al
    /// juego: conviene que se note más que un panel de mejoras cualquiera.
    /// </summary>
    protected override float Ease(float t)
    {
        if (overshoot <= 0f) return base.Ease(t);

        // Al salir el rebote es menor: pasarse de largo tanto como al entrar
        // se ve raro cuando lo que hace el panel es marcharse.
        return state == PanelState.Hiding
            ? EaseInBack(t, overshoot * 0.7f)
            : EaseOutBack(t, overshoot);
    }

    /// <summary>Llega, se pasa y vuelve.</summary>
    private static float EaseOutBack(float t, float k)
    {
        float u = t - 1f;
        return 1f + (k + 1f) * u * u * u + k * u * u;
    }

    /// <summary>Retrocede un poco para coger impulso y se va.</summary>
    private static float EaseInBack(float t, float k)
    {
        return (k + 1f) * t * t * t - k * t * t;
    }

    // ── Texto ────────────────────────────────────────────────────────

    /// <summary>
    /// El tiempo fuera, en lenguaje normal.
    ///
    /// Se enseña lo que ha estado fuera de verdad, no lo que se le paga: el
    /// jugador sabe cuánto tiempo ha pasado y decirle otra cosa parecería un
    /// error. Cuando se le ha recortado, se dice.
    /// </summary>
    private static string FormatTimeAway(OfflineEarnings.Result result)
    {
        string away = Describe(result.TimeAway);

        if (!result.WasCapped) return $"Has estado fuera {away}";

        return $"Has estado fuera {away}\n(se pagan {Describe(result.PaidTime)})";
    }

    private static string Describe(System.TimeSpan span)
    {
        if (span.TotalDays >= 1)
        {
            int days = (int)span.TotalDays;
            int hours = span.Hours;

            string d = days == 1 ? "1 día" : $"{days} días";
            return hours > 0 ? $"{d} y {hours} h" : d;
        }

        if (span.TotalHours >= 1)
        {
            int hours = (int)span.TotalHours;
            int minutes = span.Minutes;

            return minutes > 0 ? $"{hours} h {minutes} min" : $"{hours} h";
        }

        int mins = Mathf.Max(1, (int)span.TotalMinutes);
        return mins == 1 ? "1 minuto" : $"{mins} minutos";
    }
}
