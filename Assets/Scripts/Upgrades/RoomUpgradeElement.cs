using UnityEngine;

[System.Serializable]
public class RoomUpgradeElement
{
    [Tooltip("Rellena esto SOLO si el elemento empieza bloqueado. El componente " +
             "debe implementar IUnlockable (WorkDeskUnlockable, Unlockable...).")]
    public MonoBehaviour unlockableTarget;

    [Tooltip("Rellena esto SOLO si es un elemento sin bloqueo: worker, carrito, recepción...")]
    public MonoBehaviour upgradeableTarget;

    public Sprite icon;

    [Tooltip("A dónde se acerca la cámara al tocar esta mejora. Si se deja vacío, " +
             "se usa la posición del propio componente, que no siempre es la buena: " +
             "las decoraciones llevan su lógica en un grupo que está en el origen, " +
             "y sus piezas repartidas por la sala.")]
    public Transform focusPoint;

    /// <summary>El objetivo bloqueable, o null si este elemento no se desbloquea.</summary>
    public IUnlockable Unlockable => unlockableTarget as IUnlockable;

    /// <summary>
    /// Dónde mira la cámara al tocar esta mejora.
    ///
    /// Se prefiere el punto puesto a mano; si no lo hay, el componente que se
    /// mejora, y como último recurso el que se desbloquea.
    /// </summary>
    public bool TryGetFocusPosition(out Vector3 position)
    {
        if (focusPoint != null)
        {
            position = focusPoint.position;
            return true;
        }

        MonoBehaviour target = upgradeableTarget != null ? upgradeableTarget : unlockableTarget;

        if (target != null)
        {
            position = target.transform.position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }
}
