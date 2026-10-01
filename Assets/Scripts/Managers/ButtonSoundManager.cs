using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// El sonido de pulsar, para todos los botones a la vez.
///
/// No busca botones ni se engancha a ninguno. Mira qué hay bajo el dedo en el
/// momento de pulsar, y si es un botón, suena. Así funciona con los botones
/// que ya existen, con los que se crean en marcha —los del panel de mejoras se
/// instancian y se destruyen cada vez que abres una zona— y con los que están
/// dentro de paneles apagados.
///
/// Un botón se calla poniéndole un <see cref="NoClickSound"/>.
/// </summary>
[DefaultExecutionOrder(BootOrder.Manager)]
public class ButtonSoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickAudioClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private Vector2 _pointerPos;
    private Button _pressedButton;

    private void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("[ButtonSoundManager] No hay InputManager en la escena.", this);
            enabled = false;
            return;
        }

        InputManager.Instance.OnPointerPosition += HandlePointerPosition;
        InputManager.Instance.OnDragStarted += HandlePressStarted;
        InputManager.Instance.OnDragEnded += HandlePressEnded;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance == null) return;

        InputManager.Instance.OnPointerPosition -= HandlePointerPosition;
        InputManager.Instance.OnDragStarted -= HandlePressStarted;
        InputManager.Instance.OnDragEnded -= HandlePressEnded;
    }

    private void HandlePointerPosition(Vector2 pos) => _pointerPos = pos;

    private void HandlePressStarted() => _pressedButton = ButtonUnderPointer();

    private void HandlePressEnded()
    {
        Button released = ButtonUnderPointer();

        // El mismo botón al pulsar y al soltar: eso es lo que Unity considera
        // un clic. Si has arrastrado fuera, no ha habido clic y no suena.
        if (released != null && released == _pressedButton)
            AudioManager.Instance?.PlaySFX(buttonClickAudioClip, volume);

        _pressedButton = null;
    }

    private Button ButtonUnderPointer()
    {
        Button button = UIPointer.TopmostUI<Button>(_pointerPos);

        if (button == null) return null;
        if (!button.interactable) return null;
        if (button.GetComponent<NoClickSound>() != null) return null;

        return button;
    }
}