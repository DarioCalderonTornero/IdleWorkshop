using UnityEngine;

/// <summary>
/// Mejora que hace que uno o varios mostradores atiendan más rápido por nivel.
///
/// Afecta a todo lo que tarda en ese mostrador — el cliente entregando o
/// recogiendo, y el recepcionista manejando el objeto — porque todos pasan su
/// duración base por el mismo <see cref="ServiceSpeed"/>.
///
/// Admite varios porque no todas las mejoras son de un mostrador concreto: las
/// del hall mejoran el suyo, y la de la habitación mejora los dos a la vez
/// (dar el objeto y recogerlo). Y el mostrador no tiene por qué estar aquí al
/// lado: cada mejora se apunta en el ServiceSpeed como fuente propia, así que
/// conviven sin pisarse.
/// </summary>
public class ReceptionUpgradeable : UpgradeableBase
{
    [Tooltip("Los mostradores a mejorar. Si se deja vacío, se busca uno en este " +
             "GameObject y sus hijos")]
    [SerializeField] private ServiceSpeed[] serviceSpeeds;

    public override System.Type ExpectedDataType => typeof(ReceptionUpgradeData);

    private ReceptionUpgradeData Data => RequireData<ReceptionUpgradeData>();

    protected override void Awake()
    {
        base.Awake();

        if (serviceSpeeds == null || serviceSpeeds.Length == 0)
        {
            ServiceSpeed own = GetComponentInChildren<ServiceSpeed>(includeInactive: true);
            serviceSpeeds = own != null ? new[] { own } : System.Array.Empty<ServiceSpeed>();
        }

        if (serviceSpeeds.Length == 0)
            Debug.LogWarning($"[ReceptionUpgradeable] {name}: no encuentro ningún ServiceSpeed que mejorar.", this);

        _ = Data;
    }

    protected override void OnUpgraded(int newLevel)
    {
        ReceptionUpgradeData data = Data;
        if (data == null || serviceSpeeds == null) return;

        // Nivel 1 es el de partida, así que no descuenta nada todavía.
        float multiplier = Mathf.Max(0.05f, 1f - (newLevel - 1) * data.timeReductionPerLevel);

        foreach (ServiceSpeed speed in serviceSpeeds)
            if (speed != null) speed.SetMultiplier(this, multiplier);
    }

    private void OnDestroy()
    {
        if (serviceSpeeds == null) return;

        foreach (ServiceSpeed speed in serviceSpeeds)
            if (speed != null) speed.RemoveSource(this);
    }
}
