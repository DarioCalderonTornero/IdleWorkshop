using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una zona del mapa que se puede tocar para abrir su panel de mejoras.
///
/// Antes solo el taller era tocable, porque el panel salía de la WorkStation.
/// Ahora cada zona lleva su propia lista: la sala naranja mejora sus mesas, la
/// amarilla sus carritos y los mostradores sus recepciones.
///
/// El collider que la hace tocable va en este mismo GameObject (pueden ser
/// varios, por ejemplo uno por mostrador).
/// </summary>
public class UpgradeZone : MonoBehaviour
{
    [Tooltip("Nombre de la zona, solo para identificarla en el editor")]
    [SerializeField] private string zoneName;

    [Tooltip("Punto al que se lleva la cámara al tocar la zona. Si es null, usa este transform")]
    [SerializeField] private Transform cameraFocusPoint;

    [Tooltip("Lo que sale en el panel de mejoras de esta zona")]
    [SerializeField] private List<RoomUpgradeElement> upgradeElements = new();

    [Header("Salas que hay que comprar")]
    [Tooltip("Si la sala entera se compra, el componente IUnlockable que la abre. " +
             "Mientras siga bloqueada, el panel solo ofrece desbloquearla; sus " +
             "mejoras no aparecen hasta después. Vacío = sala abierta desde el principio")]
    [SerializeField] private MonoBehaviour zoneUnlockableTarget;

    [Tooltip("Icono del botón de desbloquear la sala")]
    [SerializeField] private Sprite lockedIcon;

    public string ZoneName => string.IsNullOrEmpty(zoneName) ? name : zoneName;

    /// <summary>Lo que hay que comprar para abrir la sala, o null si es gratis.</summary>
    public IUnlockable ZoneUnlockable => zoneUnlockableTarget as IUnlockable;

    /// <summary>Si la sala sigue sin comprarse.</summary>
    public bool IsLocked => ZoneUnlockable != null && !ZoneUnlockable.IsUnlocked;

    public Sprite LockedIcon => lockedIcon;

    public Vector3 CameraFocusPosition =>
        cameraFocusPoint != null ? cameraFocusPoint.position : transform.position;

    public IReadOnlyList<RoomUpgradeElement> UpgradeElements => upgradeElements;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);

        foreach (BoxCollider2D box in GetComponents<BoxCollider2D>())
            Gizmos.DrawWireCube(
                transform.position + (Vector3)box.offset,
                new Vector3(box.size.x, box.size.y, 0f));
    }
#endif
}
