using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// El menú del juego, el que se abre con el engranaje y se cierra con Resume.
///
/// No pausa nada a propósito, aunque el botón lo sugiera. Este juego produce
/// con la aplicación cerrada —de eso va todo el sistema de ganancias offline—,
/// así que congelar el tiempo porque el jugador ha abierto un menú sería
/// incoherente con lo que hace al salir del todo. Además pararía a los clientes
/// y los carritos a mitad de sus corrutinas, y envenenaría la medición de
/// producción, que cuenta el tiempo aunque no se gane nada.
///
/// Hereda de <see cref="SlidingPanelUI"/>, la misma base que los paneles de
/// mejora y el aviso de bienvenida, para que entre y salga igual que el resto
/// de la interfaz.
/// </summary>
public class GameMenuUI : SlidingPanelUI
{
    public static GameMenuUI Instance { get; private set; }

    [Header("Buttons")]
    [Tooltip("El engranaje del HUD. Abre y cierra")]
    [SerializeField] private Button gamePauseButton;

    [Tooltip("El botón de dentro del panel. Solo cierra")]
    [SerializeField] private Button resumeButton;

    [Header("Fondo")]
    [Tooltip("El velo oscuro que tapa el juego. Se enciende solo mientras el menú está " +
             "abierto, así que déjalo DESACTIVADO en la escena")]
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
                $"[GameMenuUI] {name}: falta asignar 'panelRect'. El menú no se abrirá.", this);

            enabled = false;
            return;
        }

        base.Awake();

        // El velo no puede colgar de lo que se anima: se deslizaría con el
        // panel en vez de quedarse tapando el juego.
        if (dimmer != null)
        {
            if (dimmer.transform.IsChildOf(panelRect))
                Debug.LogError(
                    $"[GameMenuUI] '{dimmer.name}' cuelga de '{panelRect.name}', que es lo que " +
                    "se anima, así que el velo se deslizará con el panel.", this);

            dimmer.SetActive(false);
        }

        if (gamePauseButton != null) gamePauseButton.onClick.AddListener(Toggle);
        if (resumeButton != null) resumeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        if (gamePauseButton != null) gamePauseButton.onClick.RemoveListener(Toggle);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Hide);

        if (Instance == this) Instance = null;
    }

    // ── Abrir y cerrar ───────────────────────────────────────────────

    /// <summary>
    /// Pregunta si el panel está abierto en vez de llevar la cuenta por su lado.
    ///
    /// Tener un bool propio parece más simple y no lo es: se desincroniza en
    /// cuanto algo cierre el panel por otro camino —el botón de Resume, sin ir
    /// más lejos— y a partir de ahí el engranaje necesita dos toques, porque el
    /// primero solo sirve para volver a cuadrar la cuenta.
    /// </summary>
    public void Toggle()
    {
        if (IsVisible) Hide();
        else Show();
    }

    public void Show()
    {
        if (!enabled || IsVisible) return;

        // Buen momento para guardar: el jugador va a toquetear ajustes o a
        // salir del juego.
        SaveManager.Instance?.SaveGame();

        if (dimmer != null) dimmer.SetActive(true);

        AnimateToShown();
    }

    public void Hide()
    {
        // Solo si está puesto o poniéndose. Sin esto, cerrar dos veces seguidas
        // relanzaría la animación de salida desde donde estuviera.
        if (state != PanelState.Visible && state != PanelState.Showing) return;

        AnimateToHidden(() =>
        {
            // Al terminar la animación, no al empezarla: si se apagara antes,
            // el panel bajaría sobre el juego ya destapado.
            if (dimmer != null) dimmer.SetActive(false);
        });
    }
}
