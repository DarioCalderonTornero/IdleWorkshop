using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lo rápido que atiende un mostrador. Todo lo que lleva tiempo ahí —el cliente
/// entregando o recogiendo, y el recepcionista manejando el objeto— multiplica
/// su duración base por este multiplicador.
///
/// Admite varias fuentes a la vez y las combina multiplicando: la mejora del
/// propio mostrador en el hall y la de la habitación tiran las dos del mismo
/// mostrador. Antes esto era un solo número y la última mejora que subiera
/// borraba a la otra sin avisar.
///
/// Está en su propio componente y no dentro de cada mostrador para que los dos
/// tipos (entrega y recogida) compartan exactamente la misma mecánica.
/// </summary>
public class ServiceSpeed : MonoBehaviour
{
    [Tooltip("Punto de partida. 1 = velocidad base")]
    [SerializeField] private float baseMultiplier = 1f;

    [Tooltip("Por muchas mejoras que se acumulen, nunca baja de aquí")]
    [SerializeField] private float minMultiplier = 0.15f;

    private readonly Dictionary<Object, float> _sources = new();

    /// <summary>Lo que multiplica ahora mismo a cada duración.</summary>
    public float TimeMultiplier { get; private set; } = 1f;

    private void Awake() => Recalculate();

    /// <summary>
    /// Apunta lo que aporta una fuente. Cada mejora se identifica por sí misma,
    /// así que puede llamar cuantas veces quiera sin sumar de más.
    /// </summary>
    public void SetMultiplier(Object source, float multiplier)
    {
        if (source == null) return;

        _sources[source] = Mathf.Clamp(multiplier, 0.01f, 10f);
        Recalculate();
    }

    public void RemoveSource(Object source)
    {
        if (source == null) return;

        if (_sources.Remove(source)) Recalculate();
    }

    private void Recalculate()
    {
        float total = baseMultiplier;
        foreach (float m in _sources.Values) total *= m;

        TimeMultiplier = Mathf.Max(minMultiplier, total);
    }

    /// <summary>Duración real de una acción que tarda <paramref name="baseDuration"/> sin mejorar.</summary>
    public float Apply(float baseDuration) => baseDuration * TimeMultiplier;
}
