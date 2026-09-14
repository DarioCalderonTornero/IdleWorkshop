using System.Collections;
using UnityEngine;

/// <summary>
/// El saco donde se meten los encargos. Siempre se ve, esté vacío o lleno: los
/// objetos entran y salen de él, no se apilan sueltos por fuera.
///
/// Crece según lo lleno que está y da un golpecito de escala cada vez que entra
/// o sale algo, que es lo que hace que el trasiego se sienta.
/// </summary>
public class Sack : MonoBehaviour
{
    [Header("Cuerpo")]
    [SerializeField] private SpriteRenderer body;

    [Tooltip("Punto donde aterrizan los objetos que entran. Si es null, usa este transform")]
    [SerializeField] private Transform mouth;

    [Header("Tamaño")]
    [Tooltip("Escala con el saco vacío")]
    [SerializeField] private Vector2 emptyScale = new(0.34f, 0.28f);

    [Tooltip("Escala con el saco lleno")]
    [SerializeField] private Vector2 fullScale = new(0.52f, 0.46f);

    [Header("Color")]
    [SerializeField] private Color emptyColor = new(0.42f, 0.72f, 0.42f);
    [SerializeField] private Color fullColor = new(0.15f, 0.52f, 0.18f);

    [Header("Golpecito")]
    [Tooltip("Cuánto se hincha al entrar o salir un objeto")]
    [SerializeField] private float popScale = 1.22f;

    [Tooltip("Lo que dura el golpecito, en segundos")]
    [SerializeField] private float popDuration = 0.16f;

    private Vector3 _baseScale;
    private Coroutine _pop;

    /// <summary>Donde aterrizan los objetos que llegan dando un saltito.</summary>
    public Vector3 MouthPos => mouth != null ? mouth.position : transform.position;

    private void Awake()
    {
        _baseScale = ScaleFor(0, 1);
        ApplyScale(1f);
    }

    /// <summary>Actualiza el tamaño y el color según lo lleno que esté.</summary>
    public void SetFill(int count, int capacity)
    {
        _baseScale = ScaleFor(count, capacity);

        if (body != null)
            body.color = Color.Lerp(emptyColor, fullColor, Fraction(count, capacity));

        // Si hay un golpecito en marcha se deja terminar: él ya aplica la
        // escala base nueva al acabar.
        if (_pop == null) ApplyScale(1f);
    }

    /// <summary>El golpecito de cuando entra o sale un objeto.</summary>
    public void Pop()
    {
        if (!isActiveAndEnabled) return;

        if (_pop != null) StopCoroutine(_pop);
        _pop = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);

            // Sube y baja: seno completo, sin tirones en los extremos.
            float punch = 1f + (popScale - 1f) * Mathf.Sin(t * Mathf.PI);
            ApplyScale(punch);

            yield return null;
        }

        ApplyScale(1f);
        _pop = null;
    }

    private void ApplyScale(float multiplier)
    {
        if (body == null) return;
        body.transform.localScale = _baseScale * multiplier;
    }

    private Vector3 ScaleFor(int count, int capacity)
    {
        float t = Fraction(count, capacity);
        return new Vector3(
            Mathf.Lerp(emptyScale.x, fullScale.x, t),
            Mathf.Lerp(emptyScale.y, fullScale.y, t),
            1f);
    }

    private static float Fraction(int count, int capacity) =>
        capacity > 0 ? Mathf.Clamp01((float)count / capacity) : 0f;
}
