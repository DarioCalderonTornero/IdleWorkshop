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

    public string ZoneName => string.IsNullOrEmpty(zoneName) ? name : zoneName;

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
