using System.Collections;
using UnityEngine;

/// <summary>
/// El saco donde se meten los encargos. Siempre se ve, esté vacío o lleno: los
/// objetos entran y salen de él, no se apilan sueltos por fuera.
///
/// No cambia de tamaño ni de color según lo que lleve —la mayoría de sacos ya
/// no tienen tope, así que "lo lleno que está" no significa nada— y su única
/// reacción es el golpecito de escala cada vez que entra o sale algo, que es lo
/// que hace que el trasiego se sienta.
/// </summary>
public class Sack : MonoBehaviour
{
    [Header("Cuerpo")]
    [SerializeField] private SpriteRenderer body;

    [Tooltip("Punto donde aterrizan los objetos que entran. Si es null, usa este transform")]
    [SerializeField] private Transform mouth;

    [Tooltip("Tamaño del saco. Es fijo: no crece con la carga")]
    [SerializeField] private Vector2 size = new(0.44f, 0.38f);

    [Header("Golpecito")]
    [Tooltip("Cuánto se hincha al entrar o salir un objeto")]
    [SerializeField] private float popScale = 1.22f;

    [Tooltip("Lo que dura el golpecito, en segundos")]
    [SerializeField] private float popDuration = 0.16f;

    private Coroutine _pop;

    /// <summary>Donde aterrizan los objetos que llegan dando un saltito.</summary>
    public Vector3 MouthPos => mouth != null ? mouth.position : transform.position;

    private void Awake() => ApplyScale(1f);

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
        body.transform.localScale = new Vector3(size.x * multiplier, size.y * multiplier, 1f);
    }
}
