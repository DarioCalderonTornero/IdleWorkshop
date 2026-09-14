using UnityEngine;

/// <summary>
/// Marca un mueble como algo que los carritos no pueden atravesar.
///
/// El rectángulo se saca del SpriteRenderer, así que si mueves o reescalas la
/// mesa en el editor el obstáculo se actualiza solo: no hay medidas copiadas a
/// mano que se queden desfasadas.
/// </summary>
[DisallowMultipleComponent]
public class NavObstacle : MonoBehaviour
{
    [Tooltip("De dónde se saca el rectángulo. Si es null, se busca en este GameObject")]
    [SerializeField] private SpriteRenderer sourceRenderer;

    [Tooltip("Usar este tamaño en vez del del sprite (0,0 = usar el del sprite)")]
    [SerializeField] private Vector2 manualSize = Vector2.zero;

    private SpriteRenderer _renderer;

    private void Awake() => _renderer = sourceRenderer != null ? sourceRenderer : GetComponent<SpriteRenderer>();

    private SpriteRenderer Renderer
    {
        get
        {
            // Se resuelve también fuera de play para que los Gizmos funcionen.
            if (_renderer == null)
                _renderer = sourceRenderer != null ? sourceRenderer : GetComponent<SpriteRenderer>();
            return _renderer;
        }
    }

    public Bounds WorldBounds
    {
        get
        {
            if (manualSize != Vector2.zero)
                return new Bounds(transform.position, new Vector3(manualSize.x, manualSize.y, 1f));

            return Renderer != null
                ? Renderer.bounds
                : new Bounds(transform.position, Vector3.one * 0.01f);
        }
    }

    /// <summary>
    /// ¿El segmento a→b choca con este mueble, contando el grosor del que se
    /// mueve? El rectángulo se infla por el radio y se comprueba la línea
    /// contra él, que equivale a barrer el volumen del carrito.
    /// </summary>
    public bool BlocksSegment(Vector3 a, Vector3 b, float agentRadius)
    {
        Bounds bounds = WorldBounds;
        bounds.Expand(new Vector3(agentRadius * 2f, agentRadius * 2f, 0f));

        return SegmentIntersectsBounds(a, b, bounds);
    }

    /// <summary>Liang-Barsky en 2D: recorta el segmento contra los 4 bordes.</summary>
    private static bool SegmentIntersectsBounds(Vector3 a, Vector3 b, Bounds bounds)
    {
        float dx = b.x - a.x;
        float dy = b.y - a.y;
        float t0 = 0f, t1 = 1f;

        return Clip(-dx, a.x - bounds.min.x, ref t0, ref t1)
            && Clip(dx, bounds.max.x - a.x, ref t0, ref t1)
            && Clip(-dy, a.y - bounds.min.y, ref t0, ref t1)
            && Clip(dy, bounds.max.y - a.y, ref t0, ref t1);
    }

    private static bool Clip(float p, float q, ref float t0, ref float t1)
    {
        // Paralelo a este borde: solo corta si empieza dentro de la franja.
        if (Mathf.Abs(p) < 1e-6f) return q >= 0f;

        float r = q / p;

        if (p < 0f)
        {
            if (r > t1) return false;
            if (r > t0) t0 = r;
        }
        else
        {
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Bounds bounds = WorldBounds;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}
