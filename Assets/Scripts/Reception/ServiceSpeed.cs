using UnityEngine;

/// <summary>
/// Lo rápido que atiende un mostrador. La mejora de recepción baja este
/// multiplicador, y todo lo que lleva tiempo en ese mostrador —el cliente
/// entregando o recogiendo, y el recepcionista manejando el objeto— multiplica
/// su duración base por él.
///
/// Está en su propio componente y no dentro de cada mostrador para que los dos
/// tipos (entrega y recogida) compartan exactamente la misma mecánica.
/// </summary>
public class ServiceSpeed : MonoBehaviour
{
    [Tooltip("1 = velocidad base. Menos de 1 = atiende más rápido")]
    [SerializeField] private float timeMultiplier = 1f;

    [Tooltip("Por rápido que se mejore, nunca baja de aquí")]
    [SerializeField] private float minMultiplier = 0.15f;

    public float TimeMultiplier => timeMultiplier;

    public void SetMultiplier(float multiplier) =>
        timeMultiplier = Mathf.Max(minMultiplier, multiplier);

    /// <summary>Duración real de una acción que tarda <paramref name="baseDuration"/> sin mejorar.</summary>
    public float Apply(float baseDuration) => baseDuration * timeMultiplier;
}
