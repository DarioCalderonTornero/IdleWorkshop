using UnityEngine;

/// <summary>
/// Mejora de un mostrador de recepción: atiende más rápido por nivel.
///
/// Afecta a todo lo que tarda en ese mostrador — el cliente entregando o
/// recogiendo, y el recepcionista manejando el objeto — porque todos pasan su
/// duración base por el mismo <see cref="ServiceSpeed"/>.
/// </summary>
public class ReceptionUpgradeable : UpgradeableBase
{
    [Header("Efecto por nivel")]
    [Tooltip("Reducción de tiempo por nivel (ej: 0.06 = -6% por nivel)")]
    [SerializeField] private float timeReductionPerLevel = 0.06f;

    [Tooltip("Velocidad del mostrador a mejorar. Si es null, se busca en este GameObject y sus hijos")]
    [SerializeField] private ServiceSpeed serviceSpeed;

    protected override void Awake()
    {
        base.Awake();

        if (serviceSpeed == null)
            serviceSpeed = GetComponentInChildren<ServiceSpeed>(includeInactive: true);

        if (serviceSpeed == null)
            Debug.LogWarning($"[ReceptionUpgradeable] {name}: no encuentro ningún ServiceSpeed que mejorar.", this);
    }

    protected override void OnUpgraded(int newLevel)
    {
        if (serviceSpeed == null) return;

        // Nivel 1 es el de partida, así que no descuenta nada todavía.
        serviceSpeed.SetMultiplier(1f - (newLevel - 1) * timeReductionPerLevel);
    }
}
