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
    [Tooltip("Velocidad del mostrador a mejorar. Si es null, se busca en este GameObject y sus hijos")]
    [SerializeField] private ServiceSpeed serviceSpeed;

    public override System.Type ExpectedDataType => typeof(ReceptionUpgradeData);

    private ReceptionUpgradeData Data => RequireData<ReceptionUpgradeData>();

    protected override void Awake()
    {
        base.Awake();

        if (serviceSpeed == null)
            serviceSpeed = GetComponentInChildren<ServiceSpeed>(includeInactive: true);

        if (serviceSpeed == null)
            Debug.LogWarning($"[ReceptionUpgradeable] {name}: no encuentro ningún ServiceSpeed que mejorar.", this);

        _ = Data;
    }

    protected override void OnUpgraded(int newLevel)
    {
        ReceptionUpgradeData data = Data;
        if (serviceSpeed == null || data == null) return;

        // Nivel 1 es el de partida, así que no descuenta nada todavía.
        serviceSpeed.SetMultiplier(1f - (newLevel - 1) * data.timeReductionPerLevel);
    }
}
