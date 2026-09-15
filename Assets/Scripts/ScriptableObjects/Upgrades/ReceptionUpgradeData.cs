using UnityEngine;

/// <summary>
/// Mejora de un mostrador de recepción: atiende más rápido por nivel.
///
/// Afecta a todo lo que tarda en ese mostrador —el cliente entregando o
/// recogiendo, y el recepcionista manejando el objeto— porque todos pasan su
/// duración base por el mismo ServiceSpeed.
/// </summary>
[CreateAssetMenu(fileName = "ReceptionUpgrade", menuName = "Idle/Mejoras/Recepción")]
public class ReceptionUpgradeData : UpgradeData
{
    [Header("Efecto por nivel")]
    [Tooltip("Reducción de tiempo de atención por nivel (0.06 = -6% por nivel)")]
    public float timeReductionPerLevel = 0.06f;
}
