using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// El panel para comprar un taller. Sale abajo, encima de los puntitos, solo
/// cuando la cámara está sobre un taller sin comprar —el que se ve en gris—, y
/// dice siempre qué hace falta:
///
///   Taller 2
///   10.000 monedas
///   Nivel del Taller 1: 57 / 100
///   [ Desbloquear ]
///
/// Lo que falta se pinta en rojo y el botón solo se puede pulsar cuando se
/// cumple todo. Si el taller que se mira no es el siguiente (hay otro sin
/// comprar antes), lo dice en vez de enseñar un precio que aún no se puede pagar.
///
/// Este objeto no se apaga nunca: lo que se enciende y se apaga es
/// <see cref="content"/>. Si se apagara a sí mismo tendría que fiarse de que
/// alguien lo vuelva a encender, y un objeto apagado no mira nada.
///
/// Tampoco depende de avisos. Mira el estado cada frame y solo redibuja si ha
/// cambiado algo: son un puñado de comparaciones, y así no hay aviso que se
/// pierda —el nivel del taller anterior, por ejemplo, cambia al subir
/// cualquier cosa, y no hay un evento único para eso—.
/// </summary>
public class WorkStationUnlockUI : MonoBehaviour
{
    [Tooltip("Lo que se enciende y se apaga. No puede ser este mismo objeto")]
    [SerializeField] private GameObject content;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI levelText;

    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI buttonLabel;

    [Header("Colores")]
    [SerializeField] private Color metColor = Color.white;

    [Tooltip("El requisito que falta, para que se vea de un vistazo qué es lo que no llega")]
    [SerializeField] private Color missingColor = new(1f, 0.45f, 0.4f);

    [SerializeField] private Color buttonReadyColor = new(0.30f, 0.72f, 0.36f);
    [SerializeField] private Color buttonBlockedColor = new(0.45f, 0.45f, 0.45f);

    // Lo último que se dibujó. Si no cambia nada, no se toca el texto: TMP
    // regenera la malla cada vez que se le asigna uno.
    private int _shownIndex = int.MinValue;
    private int _shownUnlocked = -1;
    private int _shownLevel = -1;
    private bool _shownAfford;

    private void Awake()
    {
        if (content == gameObject)
        {
            Debug.LogError(
                "[WorkStationUnlockUI] 'content' no puede ser el propio objeto: se apagaría " +
                "y no volvería a encenderse. Ejecuta 'Taller > Construir talleres'.", this);
            content = null;
        }

        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnUnlockClicked);
        else
            Debug.LogError("[WorkStationUnlockUI] Falta el botón. Ejecuta 'Taller > Construir talleres'.", this);

        SetVisible(false);
    }

    private void LateUpdate()
    {
        WorkStationUnlocker unlocker = WorkStationUnlocker.Instance;
        int index = unlocker != null ? unlocker.ViewingIndex : -1;
        Workshop workshop = unlocker != null ? unlocker.At(index) : null;

        bool show = workshop != null && !unlocker.IsUnlocked(index);
        SetVisible(show);

        if (!show)
        {
            _shownIndex = int.MinValue;
            return;
        }

        // Solo lo que puede cambiar lo que se ve.
        int unlocked = unlocker.UnlockedCount;
        int level = unlocker.IsNextInLine(index) ? unlocker.PreviousLevel(index) : -1;
        bool afford = unlocker.CanAfford(index);

        if (index == _shownIndex && unlocked == _shownUnlocked &&
            level == _shownLevel && afford == _shownAfford)
            return;

        _shownIndex = index;
        _shownUnlocked = unlocked;
        _shownLevel = level;
        _shownAfford = afford;

        Redraw(unlocker, index, workshop);
    }

    private void Redraw(WorkStationUnlocker unlocker, int index, Workshop workshop)
    {
        SetText(titleText, workshop.DisplayName, metColor);

        Workshop previous = unlocker.At(index - 1);

        if (!unlocker.IsNextInLine(index))
        {
            // Hay otro sin comprar antes: el precio de este todavía no importa.
            Workshop before = unlocker.At(unlocker.UnlockedCount);
            SetText(costText, $"Primero desbloquea {before?.DisplayName}", missingColor);
            SetText(levelText, "", metColor);
            SetButton(false, "Bloqueado");
            return;
        }

        bool afford = unlocker.CanAfford(index);
        bool levelMet = unlocker.LevelRequirementMet(index);

        SetText(costText,
            $"{CurrencyFormatter.Format(workshop.UnlockCost)} monedas",
            afford ? metColor : missingColor);

        SetText(levelText,
            $"Nivel del {previous?.DisplayName}: {unlocker.PreviousLevel(index)} / {workshop.RequiredPreviousLevel}",
            levelMet ? metColor : missingColor);

        SetButton(unlocker.CanUnlock(index), "Desbloquear");
    }

    private void SetVisible(bool visible)
    {
        if (content != null && content.activeSelf != visible)
            content.SetActive(visible);
    }

    private static void SetText(TextMeshProUGUI text, string value, Color color)
    {
        if (text == null) return;

        text.text = value;
        text.color = color;
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private void SetButton(bool interactable, string label)
    {
        if (unlockButton == null) return;

        unlockButton.interactable = interactable;

        // El color va en la imagen y no en el tinte del botón: así se ve igual
        // de claro en gris que en verde, sin depender de cómo esté configurada
        // la transición.
        if (unlockButton.targetGraphic != null)
            unlockButton.targetGraphic.color = interactable ? buttonReadyColor : buttonBlockedColor;

        if (buttonLabel != null) buttonLabel.text = label;
    }

    private void OnUnlockClicked()
    {
        WorkStationUnlocker unlocker = WorkStationUnlocker.Instance;
        if (unlocker == null) return;

        unlocker.Unlock(unlocker.ViewingIndex);

        // Que el siguiente frame redibuje sí o sí, haya comprado o no.
        _shownIndex = int.MinValue;
    }
}
