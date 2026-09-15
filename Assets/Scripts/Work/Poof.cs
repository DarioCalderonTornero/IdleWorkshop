using System.Collections;
using UnityEngine;

/// <summary>
/// Hace que algo aparezca y desaparezca con un puf de motitas, en vez de
/// encenderse y apagarse de golpe.
///
/// Lo usan las bolsas de terminados, que se esconden al desbloquear una mesa
/// nueva, y las piezas de las decoraciones, que van saliendo al mejorarlas.
///
/// El orden de la animación al desaparecer es el de toda la vida en dibujos:
/// primero un golpecito hacia fuera (anticipación) y después el encogido
/// rápido hasta nada, con las motitas saliendo en ese mismo instante.
/// </summary>
public class Poof : MonoBehaviour
{
    [Tooltip("Lo que se encoge. Si es null, se encoge este mismo objeto")]
    [SerializeField] private Transform visual;

    [Header("Motitas")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Color particleColor = new(1f, 1f, 1f, 0.95f);
    [SerializeField] private int particleCount = 8;
    [SerializeField] private float particleSize = 0.13f;

    [Tooltip("Cuánto se alejan del centro")]
    [SerializeField] private float spread = 0.45f;

    [Header("Tiempos")]
    [SerializeField] private float duration = 0.3f;

    [Tooltip("Cuánto se hincha justo antes de encogerse")]
    [SerializeField] private float anticipation = 1.2f;

    [SerializeField] private int sortingOrder = SortingOrders.Popup;

    private Vector3 _baseScale;
    private Coroutine _running;
    private bool _captured;

    public bool IsVisible => gameObject.activeSelf;

    private void Awake() => Capture();

    /// <summary>
    /// La escala buena hay que guardarla antes de tocarla, y una sola vez: si
    /// se recogiera a mitad de una animación se quedaría con el valor encogido
    /// y el objeto no volvería a salir a su tamaño.
    /// </summary>
    private void Capture()
    {
        if (_captured) return;

        _baseScale = Target.localScale;
        _captured = true;
    }

    private Transform Target => visual != null ? visual : transform;

    /// <summary>
    /// Enseña o esconde el objeto. Con <paramref name="animate"/> a false el
    /// cambio es instantáneo, que es lo que hace falta al montar la escena:
    /// si no, todo lo que empieza escondido soltaría un puf al arrancar.
    /// </summary>
    public void SetVisible(bool visible, bool animate)
    {
        Capture();

        if (visible == IsVisible && _running == null) return;

        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
            Target.localScale = _baseScale;
        }

        if (!animate || !gameObject.activeInHierarchy)
        {
            Target.localScale = _baseScale;
            gameObject.SetActive(visible);
            return;
        }

        if (visible)
        {
            gameObject.SetActive(true);
            _running = StartCoroutine(AppearRoutine());
        }
        else
        {
            _running = StartCoroutine(DisappearRoutine());
        }
    }

    private IEnumerator DisappearRoutine()
    {
        Burst();

        float elapsed = 0f;
        float shrink = duration * 0.7f;

        while (elapsed < shrink)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrink);

            // Primer tercio hacia fuera, resto hacia dentro.
            float scale = t < 0.3f
                ? Mathf.Lerp(1f, anticipation, t / 0.3f)
                : Mathf.Lerp(anticipation, 0f, (t - 0.3f) / 0.7f);

            Target.localScale = _baseScale * scale;
            yield return null;
        }

        // Se deja el tamaño bueno puesto antes de apagarlo, para que la
        // próxima vez que salga no aparezca del tamaño de un alfiler.
        Target.localScale = _baseScale;
        _running = null;
        gameObject.SetActive(false);
    }

    private IEnumerator AppearRoutine()
    {
        Burst();

        float elapsed = 0f;
        Target.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Se pasa de tamaño y vuelve: rebote suave al aparecer.
            float scale = t < 0.6f
                ? Mathf.Lerp(0f, anticipation, t / 0.6f)
                : Mathf.Lerp(anticipation, 1f, (t - 0.6f) / 0.4f);

            Target.localScale = _baseScale * scale;
            yield return null;
        }

        Target.localScale = _baseScale;
        _running = null;
    }

    private void Burst()
    {
        PoofParticle.Burst(
            transform.parent, Target.position, ParticleSprite(), particleColor,
            particleCount, particleSize, spread, duration, sortingOrder);
    }

    /// <summary>
    /// Si no le han puesto sprite, se coge el del propio objeto. Así funciona
    /// sin depender de ningún asset suelto: todo lo que se esconde ya tiene su
    /// cuadrado.
    /// </summary>
    private Sprite ParticleSprite()
    {
        if (particleSprite != null) return particleSprite;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(includeInactive: true);
        return sr != null ? sr.sprite : null;
    }
}
