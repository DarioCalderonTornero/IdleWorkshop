using UnityEngine;

/// <summary>
/// Cuánto se paga por el rato que el jugador ha estado fuera.
///
/// Va en un ScriptableObject y no como constantes porque estos dos números se
/// tocan mucho al equilibrar, y porque el tope acabará siendo una mejora
/// comprable ("+4 horas de producción offline"): con el asset, eso es leer el
/// valor de otro sitio en vez de recompilar.
/// </summary>
[CreateAssetMenu(fileName = "OfflineEarnings", menuName = "Idle/Ganancias offline")]
public class OfflineEarningsConfig : ScriptableObject
{
    [Header("Cuánto se paga")]
    [Tooltip("Parte de la producción normal que se paga estando fuera. 0.5 = la mitad. " +
             "Al 100% jugar activamente dejaría de aportar nada")]
    [Range(0f, 1f)]
    [SerializeField] private float offlineFactor = 0.5f;

    [Header("Cuánto tiempo como mucho")]
    [Tooltip("Horas que se pagan por muy larga que sea la ausencia. Además de equilibrio, " +
             "es lo único que limita lo que se puede sacar adelantando el reloj del aparato")]
    [Min(0f)]
    [SerializeField] private float maxHours = 24f;

    public float OfflineFactor => offlineFactor;
    public float MaxHours => maxHours;
}
