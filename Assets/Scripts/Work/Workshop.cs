using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un taller entero: la raíz de todo lo que hay en él.
///
/// Existe porque ahora puede haber varios a la vez, y buena parte del juego se
/// escribió pensando en uno solo: una red de navegación, un gestor de clientes,
/// un bonus de decoraciones para todo. Con dos talleres, los carritos de uno
/// calcularían la ruta con los nodos del otro, y las decoraciones del primero
/// pagarían en el segundo. Esto les da a todos una forma de preguntar "¿de qué
/// taller soy?" y quedarse dentro de él.
///
/// Un taller sin comprar está apagado: no corre nada ni cobra nada. Lo que se
/// ve en gris en su sitio es una copia (ver <see cref="WorkshopPreview"/>).
/// Comprado, funciona y se mejora igual que cualquier otro, para siempre.
/// Quién decide qué está comprado es el <see cref="WorkStationUnlocker"/>.
/// </summary>
[DisallowMultipleComponent]
public class Workshop : MonoBehaviour
{
    [Tooltip("Como se llama en la interfaz")]
    [SerializeField] private string displayName = "Taller";

    [Header("Para comprarlo")]
    [Tooltip("Lo que cuesta abrir este taller. El primero no se compra, así que el suyo no se usa")]
    [SerializeField] private double unlockCost;

    [Tooltip("Cuánto tiene que sumar el taller ANTERIOR entre los niveles de todo lo que " +
             "tiene. Con menos, no se puede comprar este aunque sobre el dinero")]
    [SerializeField] private int requiredPreviousLevel;

    [Header("Cámara")]
    [Tooltip("A dónde va la cámara al visitar este taller")]
    [SerializeField] private Transform homePoint;

    [Tooltip("Rectángulo que ocupa el taller, medido desde su raíz. Sirve para que " +
             "la cámara pueda llegar a él entero y para saber cuál se está mirando")]
    [SerializeField] private Vector2 boundsCenter;
    [SerializeField] private Vector2 boundsSize = new(10f, 14f);

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public double UnlockCost => unlockCost;
    public int RequiredPreviousLevel => requiredPreviousLevel;

    public WorkStation Station => GetComponent<WorkStation>();

    public Vector3 HomePosition => homePoint != null ? homePoint.position : transform.position;

    /// <summary>Lo que ocupa el taller en el mundo.</summary>
    public Bounds WorldBounds =>
        new(transform.TransformPoint(boundsCenter), new Vector3(boundsSize.x, boundsSize.y, 0f));

    // ── A qué taller pertenece cada cosa ─────────────────────────────

    /// <summary>
    /// El taller en el que está <paramref name="component"/>, o null si no está
    /// dentro de ninguno.
    ///
    /// Null no es un error: es lo que pasa en una escena de pruebas sin talleres.
    /// Quien pregunta tiene que seguir funcionando igual que antes en ese caso.
    /// </summary>
    public static Workshop Of(Component component) =>
        component != null ? component.GetComponentInParent<Workshop>(includeInactive: true) : null;

    // ── Nivel total ──────────────────────────────────────────────────

    /// <summary>
    /// La suma de los niveles de todo lo que el jugador tiene en este taller.
    /// Es lo que se compara con el <see cref="RequiredPreviousLevel"/> del
    /// taller siguiente.
    ///
    /// Solo cuenta lo que ofrecen sus paneles de mejora, y solo lo que ya es
    /// suyo: una decoración sin comprar vale 0, no 1. Así comprar algo sube el
    /// total en cuanto se compra, que es lo que se espera; si contara desde el
    /// principio, un taller recién abierto ya sumaría diecisiete sin haber
    /// hecho nada.
    ///
    /// Que salga de los paneles no es un detalle: si contara todos los
    /// mejorables que cuelgan del taller, entraría alguno que el jugador no
    /// puede tocar desde ningún sitio, como el trabajador de cada mesa.
    /// </summary>
    public int TotalLevel
    {
        get
        {
            int total = 0;

            foreach (UpgradeZone zone in Zones)
            {
                // Lo que está dentro de una sala sin comprar no es del jugador
                // todavía, aunque su mejora ya exista en la escena.
                IUnlockable room = zone.ZoneUnlockable;
                if (room != null && !room.IsUnlocked) continue;

                // Con for y no foreach: recorrer una IReadOnlyList con foreach
                // empaqueta el enumerador y genera basura en cada consulta, y
                // esto se pregunta cada vez que cambian las monedas.
                IReadOnlyList<RoomUpgradeElement> elements = zone.UpgradeElements;
                for (int i = 0; i < elements.Count; i++)
                {
                    RoomUpgradeElement element = elements[i];

                    IUnlockable unlockable = element.Unlockable;
                    if (unlockable != null && !unlockable.IsUnlocked) continue;

                    if (element.upgradeableTarget is IUpgradeable upgradeable)
                        total += upgradeable.CurrentLevel;
                }
            }

            return total;
        }
    }

    private UpgradeZone[] _zones;

    /// <summary>
    /// Las zonas del taller, buscadas una vez. Las zonas no cambian en partida,
    /// así que no hay nada que invalidar.
    /// </summary>
    private UpgradeZone[] Zones
    {
        get
        {
            if (_zones == null) _zones = GetComponentsInChildren<UpgradeZone>(includeInactive: true);
            return _zones;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Bounds bounds = WorldBounds;
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}
