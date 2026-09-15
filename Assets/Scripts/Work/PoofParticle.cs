using System.Collections;
using UnityEngine;

/// <summary>
/// Una motita del puf: sale del centro hacia fuera, se hincha y se apaga.
///
/// Se anima sola, con su propia corrutina, en vez de que lo haga quien la ha
/// lanzado. Es a propósito: lo que desaparece se desactiva a mitad de la
/// animación, y una corrutina suya se cortaría ahí dejando las motitas
/// congeladas a media salida.
/// </summary>
public class PoofParticle : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Vector3 _from;
    private Vector3 _to;
    private float _size;
    private float _duration;

    /// <summary>
    /// Lanza un puñado de motitas repartidas en círculo alrededor de un punto.
    /// Van colgadas de <paramref name="parent"/>, que tiene que seguir activo
    /// mientras dure el efecto.
    /// </summary>
    public static void Burst(
        Transform parent, Vector3 center, Sprite sprite, Color color,
        int count, float size, float spread, float duration, int sortingOrder)
    {
        if (sprite == null || count <= 0) return;

        // Un giro al azar para que dos pufs seguidos no salgan calcados.
        float offset = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = offset + (360f / count) * i + Random.Range(-14f, 14f);
            float radians = angle * Mathf.Deg2Rad;
            float distance = spread * Random.Range(0.75f, 1.15f);

            Vector3 direction = new(Mathf.Cos(radians), Mathf.Sin(radians), 0f);

            GameObject go = new("Motita");
            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.position = center;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            PoofParticle particle = go.AddComponent<PoofParticle>();
            particle._renderer = sr;
            particle._from = center;
            particle._to = center + direction * distance;
            particle._size = size * Random.Range(0.7f, 1.25f);
            particle._duration = duration * Random.Range(0.85f, 1.15f);
        }
    }

    private void Start() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);

            // Sale rápido y se va frenando: es lo que le da el aire de dibujo
            // animado, en vez de una nube que se expande a velocidad constante.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.LerpUnclamped(_from, _to, eased);

            // Se hincha al principio y se deshincha al final.
            float puff = Mathf.Sin(t * Mathf.PI);
            float scale = _size * (0.45f + 0.75f * puff);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (_renderer != null)
            {
                Color c = _renderer.color;
                c.a = 1f - t * t;   // aguanta visible y se apaga al final
                _renderer.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
