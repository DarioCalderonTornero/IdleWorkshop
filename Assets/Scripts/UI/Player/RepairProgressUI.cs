using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Círculo de progreso sobre el jugador usando Image Filled Radial 360 en World Space Canvas.
///
/// SETUP:
///  Hijo del jugador llamado "ProgressUI":
///  └── Canvas (World Space, sort order alto, escala ~0.01)
///      └── Background  (Image, sprite círculo gris)
///          └── Fill    (Image, sprite círculo de color, Image Type = Filled,
///                       Fill Method = Radial 360, Fill Origin = Top, Clockwise = true)
///
/// Arrastra el componente Image del Fill al campo "fillImage" en el inspector.
/// Arrastra el GameObject raíz del Canvas a "root" para mostrarlo/ocultarlo.
/// </summary>
public class RepairProgressUI : MonoBehaviour
{
    [Tooltip("La Image con Fill Method = Radial 360")]
    [SerializeField] private Image fillImage;

    [Tooltip("GameObject raíz del Canvas (para activar/desactivar)")]
    [SerializeField] private GameObject root;

    [Tooltip("Offset respecto al jugador")]
    [SerializeField] private Vector3 offset = new(0f, 1.2f, 0f);

    private Transform _playerTransform;

    // ── Init ───────────────────────────────────────────────────────
    private void Awake()
    {
        _playerTransform = transform.parent;

        if (fillImage != null)
            fillImage.fillAmount = 0f;

        Hide();
    }

    // ── Unity ──────────────────────────────────────────────────────
    private void LateUpdate()
    {
        transform.position = _playerTransform.position + offset;
    }

    // ── API pública ────────────────────────────────────────────────
    public void Show(float progress = 0f)
    {
        root?.SetActive(true);
        SetFill(progress);
    }

    public void Hide()
    {
        root?.SetActive(false);
    }

    public void SetFill(float t)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(t);
    }
}